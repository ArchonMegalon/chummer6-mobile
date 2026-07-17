using Chummer.Play.Core.Application;
using Chummer.Play.Core.Offline;
using Chummer.Play.Core.PlayApi;
using Chummer.Play.Core.Roaming;
using Chummer.Play.Core.Sync;
using Chummer.Play.Gm.TacticalShell;
using Chummer.Play.Player.PlayerShell;
using Chummer.Play.Web.BrowserState;
using Chummer.Play.Web.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Chummer.Play.Web;

public static class PlayWebApplication
{
    private static readonly HashSet<string> PublicPwaStaticAssetPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/mobile.css",
        "/mobile-install-shell.js",
        "/manifest.webmanifest",
        "/manifest.player.webmanifest",
        "/manifest.gm.webmanifest",
        "/manifest.observer.webmanifest",
        "/icons/icon-192.png",
        "/icons/icon-512.png",
        "/icons/icon-192.svg",
        "/icons/icon-512.svg"
    };

    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(PlayWebApplication).Assembly.GetName().Name
        });
        PlayServiceKeyPolicy.ValidateProductionReadiness(builder.Configuration, builder.Environment);
        ConfigureServices(builder.Services, builder.Configuration, builder.Environment);

        var app = builder.Build();
        Configure(app);
        return app;
    }

    internal static void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        services.AddRazorComponents()
            .AddInteractiveServerComponents();
        services.AddHttpContextAccessor();
        services.AddSingleton<IBrowserKeyValueStore>(_ =>
            new FileSystemBrowserKeyValueStore(ResolveBrowserStateRoot(configuration, hostEnvironment)));
        services.AddSingleton<BrowserSessionEventLogStore>();
        services.AddSingleton<BrowserSessionOfflineCacheService>();
        services.AddSingleton<BrowserSessionOfflineQueueService>();
        services.AddSingleton<IPlayEventLogStore>(serviceProvider => serviceProvider.GetRequiredService<BrowserSessionEventLogStore>());
        services.AddSingleton<IPlayOfflineCacheService>(serviceProvider => serviceProvider.GetRequiredService<BrowserSessionOfflineCacheService>());
        services.AddSingleton<IPlayOfflineQueueService>(serviceProvider => serviceProvider.GetRequiredService<BrowserSessionOfflineQueueService>());
        services.AddSingleton<IRoamingWorkspaceSyncPlanner, RoamingWorkspaceSyncPlanner>();
        services.AddSingleton<IPlayRoamingRestoreService, PlayRoamingRestoreService>();
        services.AddSingleton<PlayTurnCompanionService>();
    }

    internal static void Configure(WebApplication app)
    {
        app.Use(RequireTrustedMobileLiveGrantBoundaryAsync);
        app.Use(RequirePrivateMobileDocumentBoundaryAsync);
        app.Use(ApplyServiceWorkerHeadersAsync);
        app.Use(ApplyPublicPwaStaticAssetHeadersAsync);
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.Use(RequireTrustedPlayApiBoundaryAsync);
        app.UseAntiforgery();

        var playerShell = PlayerShellModule.CreateDescriptor();
        var gmShell = GmTacticalShellModule.CreateDescriptor();

        app.MapGet("/health", () => Results.Text("ok"));
        app.MapGet(
            "/api/play/turn-companion/{sessionId}",
            async (
                string sessionId,
                PlaySurfaceRole role,
                string? deviceId,
                PlayTurnCompanionService turnCompanionService,
                CancellationToken cancellationToken) =>
                Results.Json(await turnCompanionService.GetProjectionAsync(sessionId, role, deviceId, cancellationToken))
        );
        app.MapGet(
            "/api/play/turn-companion/{sessionId}/queue-status",
            async (
                string sessionId,
                PlaySurfaceRole role,
                string? deviceId,
                PlayTurnCompanionService turnCompanionService,
                CancellationToken cancellationToken) =>
                Results.Json(await turnCompanionService.GetQueueStatusAsync(sessionId, role, deviceId, cancellationToken))
        );
        app.MapPost(
            "/api/play/turn-companion/{sessionId}/replay",
            async (
                string sessionId,
                PlaySurfaceRole role,
                string? deviceId,
                PlayTurnCompanionReplayRequest request,
                PlayTurnCompanionService turnCompanionService,
                CancellationToken cancellationToken) =>
                Results.Json(await turnCompanionService.ReplayClientQueueAsync(sessionId, role, request.Events ?? Array.Empty<string>(), deviceId, cancellationToken))
        );
        app.MapPost(
            "/api/play/turn-companion/{sessionId}/acknowledge",
            async (
                string sessionId,
                PlaySurfaceRole role,
                string? deviceId,
                PlayTurnCompanionService turnCompanionService,
                CancellationToken cancellationToken) =>
                Results.Json(await turnCompanionService.AcknowledgePendingQueueAsync(sessionId, role, deviceId, cancellationToken))
        );
        app.MapGet(
            PlayApiRoutes.Bootstrap,
            async (
                string sessionId,
                PlaySurfaceRole role,
                string sceneId,
                string sceneRevision,
                string runtimeFingerprint,
                IPlayEventLogStore eventLogStore,
                IPlayOfflineCacheService offlineCacheService,
                CancellationToken cancellationToken
            ) =>
            {
                var session = new EngineSessionEnvelope(sessionId, sceneId, sceneRevision, runtimeFingerprint);
                var bootstrapRequest = new PlayBootstrapRequest(session, role);
                var activeShell = SelectShell(bootstrapRequest.Role, playerShell, gmShell);
                var roleCapabilities = PlayRouteHandlers.ResolveRoleCapabilities(activeShell);
                var checkpoint = await offlineCacheService.GetCheckpointAsync(sessionId, cancellationToken);
                var existingLedger = await eventLogStore.GetExistingAsync(sessionId, cancellationToken);
                var effectiveSession = PlayRouteHandlers.ResolveStoredSession(bootstrapRequest.Session, checkpoint, existingLedger);
                var ledger = existingLedger is not null
                    && SessionLineage.IsLedgerAligned(
                        existingLedger,
                        effectiveSession.SessionId,
                        effectiveSession.SceneId,
                        effectiveSession.SceneRevision,
                        effectiveSession.RuntimeFingerprint
                    )
                        ? existingLedger
                        : await eventLogStore.GetOrCreateAsync(
                            effectiveSession.SessionId,
                            effectiveSession.SceneId,
                            effectiveSession.SceneRevision,
                            effectiveSession.RuntimeFingerprint,
                            cancellationToken
                        );
                var projection = PlayRouteHandlers.BuildProjection(
                    new EngineSessionEnvelope(
                        ledger.SessionId,
                        ledger.SceneId,
                        ledger.SceneRevision,
                        ledger.RuntimeFingerprint
                    ),
                    ledger.LastKnownSequence,
                    ledger.PendingEvents
                );
                var alignedCheckpoint = PlayRouteHandlers.CreateAlignedCheckpoint(
                    projection.Cursor.Session,
                    ledger.LastKnownSequence,
                    checkpoint
                );
                checkpoint = alignedCheckpoint;
                await offlineCacheService.SetCheckpointAsync(checkpoint, cancellationToken);
                await offlineCacheService.CacheRuntimeBundleAsync(
                    projection.Cursor.Session.SessionId,
                    projection.Cursor.Session.RuntimeFingerprint,
                    projection.Cursor.Session.SceneRevision,
                    $"bundle:{projection.Cursor.Session.SceneId}:{projection.Cursor.Session.RuntimeFingerprint}",
                    cancellationToken
                );

                return Results.Json(
                    new PlayBootstrapResponse(
                        "chummer6-mobile",
                        projection,
                        activeShell,
                        [activeShell],
                        new BrowserSessionShellProbe(true, true, true),
                        roleCapabilities,
                        BuildSpiderCards(roleCapabilities),
                        BuildCoachHints(bootstrapRequest.Role),
                        PlayRouteHandlers.BuildQuickActions(bootstrapRequest.Role, roleCapabilities)
                    )
                );
            }
        );
        app.MapGet(
            PlayApiRoutes.Projection,
            async (
                string sessionId,
                IPlayEventLogStore eventLogStore,
                IPlayOfflineCacheService offlineCacheService,
                CancellationToken cancellationToken
            ) =>
            {
                var (session, ledger) = await ResolveProjectionSessionAsync(
                    sessionId,
                    eventLogStore,
                    offlineCacheService,
                    cancellationToken
                );

                return Results.Json(
                    PlayRouteHandlers.BuildProjection(
                        session,
                        ledger.LastKnownSequence,
                        ledger.PendingEvents
                    )
                );
            }
        );
        app.MapPost(
            PlayApiRoutes.Reconnect,
            (PlayReconnectRequest request,
                IPlayEventLogStore eventLogStore,
                IPlayOfflineCacheService offlineCacheService,
                CancellationToken cancellationToken) =>
                PlayRouteHandlers.HandleReconnectAsync(
                    request,
                    eventLogStore,
                    offlineCacheService,
                    cancellationToken
                )
        );
        app.MapPost(
            PlayApiRoutes.ContinuityClaim,
            (PlayContinuityClaimRequest request,
                IPlayEventLogStore eventLogStore,
                IPlayOfflineCacheService offlineCacheService,
                IBrowserKeyValueStore browserStore,
                CancellationToken cancellationToken) =>
                PlayRouteHandlers.HandleContinuityClaimAsync(
                    request,
                    eventLogStore,
                    offlineCacheService,
                    browserStore,
                    cancellationToken
                )
        );
        app.MapGet(
            PlayApiRoutes.Observe,
            (string sessionId,
                IPlayEventLogStore eventLogStore,
                IPlayOfflineCacheService offlineCacheService,
                IBrowserKeyValueStore browserStore,
                CancellationToken cancellationToken) =>
                PlayRouteHandlers.HandleObserveAsync(
                    sessionId,
                    eventLogStore,
                    offlineCacheService,
                    browserStore,
                    cancellationToken
                )
        );
        app.MapGet(
            PlayApiRoutes.Resume,
            async (
                string sessionId,
                PlaySurfaceRole role,
                IPlayEventLogStore eventLogStore,
                IPlayOfflineCacheService offlineCacheService,
                CancellationToken cancellationToken
            ) =>
            {
                PlayResumeResponse response = await BuildResumeResponseAsync(
                    sessionId,
                    role,
                    eventLogStore,
                    offlineCacheService,
                    playerShell,
                    gmShell,
                    cancellationToken
                );
                return Results.Json(response);
            }
        );
        app.MapGet(
            "/api/play/workspace-lite/{sessionId}",
            async (
                string sessionId,
                PlaySurfaceRole role,
                string? artifactView,
                string? artifactId,
                IPlayEventLogStore eventLogStore,
                IPlayOfflineCacheService offlineCacheService,
                CancellationToken cancellationToken
            ) =>
            {
                PlayResumeResponse response = await BuildResumeResponseAsync(
                    sessionId,
                    role,
                    eventLogStore,
                    offlineCacheService,
                    playerShell,
                    gmShell,
                    cancellationToken
                );
                return Results.Json(PlayCampaignWorkspaceLiteProjector.Create(response, artifactView, artifactId));
            }
        );
        app.MapGet(
            "/api/play/restore-plan/{sessionId}",
            async (
                string sessionId,
                PlaySurfaceRole role,
                string? deviceId,
                IPlayEventLogStore eventLogStore,
                IPlayOfflineCacheService offlineCacheService,
                IPlayRoamingRestoreService restoreService,
                CancellationToken cancellationToken
            ) =>
                {
                PlayResumeResponse response = await BuildResumeResponseAsync(
                    sessionId,
                    role,
                    eventLogStore,
                    offlineCacheService,
                    playerShell,
                    gmShell,
                    cancellationToken
                );
                if (!TryResolveTrustedRestoreDeviceId(role, deviceId, out string? trustedDeviceId, out string[] allowedDeviceIds))
                {
                    return Results.Json(
                        new PlayRouteValidationError(
                            Error: "invalid_device_id",
                            Message: "The requested deviceId is not a trusted claimed-device target for this role.",
                            ProvidedDeviceId: deviceId ?? string.Empty,
                            AllowedDeviceIds: allowedDeviceIds),
                        statusCode: StatusCodes.Status400BadRequest);
                }

                return Results.Json(restoreService.CreatePlan(response, trustedDeviceId));
            }
        );
        app.MapGet(
            "/api/play/onboarding-recovery/{sessionId}",
            async (
                string sessionId,
                PlaySurfaceRole role,
                string? deviceId,
                IPlayEventLogStore eventLogStore,
                IPlayOfflineCacheService offlineCacheService,
                IPlayRoamingRestoreService restoreService,
                CancellationToken cancellationToken
            ) =>
                {
                PlayResumeResponse response = await BuildResumeResponseAsync(
                    sessionId,
                    role,
                    eventLogStore,
                    offlineCacheService,
                    playerShell,
                    gmShell,
                    cancellationToken
                );
                if (!TryResolveTrustedRestoreDeviceId(role, deviceId, out string? trustedDeviceId, out string[] allowedDeviceIds))
                {
                    return Results.Json(
                        new PlayRouteValidationError(
                            Error: "invalid_device_id",
                            Message: "The requested deviceId is not a trusted claimed-device target for this role.",
                            ProvidedDeviceId: deviceId ?? string.Empty,
                            AllowedDeviceIds: allowedDeviceIds),
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var restorePlan = restoreService.CreatePlan(response, trustedDeviceId);
                return Results.Json(PlayEntryRecoveryProjector.Create(response, restorePlan));
            }
        );
        app.MapGet(
            PlayApiRoutes.CachePressure,
            async (string sessionId, IPlayOfflineCacheService offlineCacheService, CancellationToken cancellationToken) =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
                var pressure = await offlineCacheService.GetCachePressureAsync(cancellationToken);
                return Results.Json(pressure);
            }
        );
        app.MapGet(
            "/artifacts/{sessionId}",
            (string sessionId, string? deviceId, string? view, PlaySurfaceRole role = PlaySurfaceRole.Player) =>
                Results.Redirect(
                    BuildPlayIndexHref(sessionId, deviceId, role, artifactView: view),
                    permanent: false
                )
        );
        app.MapGet(
            "/artifacts/{sessionId}/{artifactId}",
            (string sessionId, string artifactId, string? deviceId, string? view, PlaySurfaceRole role = PlaySurfaceRole.Player) =>
                Results.Redirect(
                    BuildPlayIndexHref(sessionId, deviceId, role, artifactView: view, artifactId: artifactId),
                    permanent: false
                )
        );
        app.MapGet(
            "/play",
            (string? sessionId, string? deviceId, PlaySurfaceRole role = PlaySurfaceRole.Player) =>
                Results.Redirect(
                    BuildPlayIndexHref(sessionId, deviceId, role),
                    permanent: false
                )
        );
        app.MapGet(
            "/play/{sessionId}",
            (string sessionId, PlaySurfaceRole role = PlaySurfaceRole.Player) =>
                Results.Redirect(
                    BuildPlayIndexHref(sessionId, deviceId: null, role),
                    permanent: false
                )
        );
        app.MapPost(
            PlayApiRoutes.QuickAction,
            (PlayQuickActionRequest request,
                IPlayOfflineCacheService offlineCacheService,
                IPlayEventLogStore eventLogStore,
                IPlayOfflineQueueService offlineQueueService,
                CancellationToken cancellationToken) =>
                PlayRouteHandlers.HandleQuickActionAsync(
                    request,
                    offlineCacheService,
                    eventLogStore,
                    offlineQueueService,
                    playerShell,
                    gmShell,
                    cancellationToken
                )
        );
        app.MapPost(
            PlayApiRoutes.Sync,
            (PlaySyncRequest request,
                IPlayOfflineCacheService offlineCacheService,
                IPlayEventLogStore eventLogStore,
                IPlayOfflineQueueService offlineQueueService,
                CancellationToken cancellationToken) =>
                PlayRouteHandlers.HandleSyncAsync(
                    request,
                    offlineCacheService,
                    eventLogStore,
                    offlineQueueService,
                    cancellationToken
                )
        );
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
    }

    internal static async Task ApplyServiceWorkerHeadersAsync(HttpContext context, RequestDelegate next)
    {
        bool isWorker = context.Request.Path.Equals("/service-worker.js", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.Equals("/mobile/service-worker.js", StringComparison.OrdinalIgnoreCase);
        if (isWorker)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.ContentType = "application/javascript; charset=utf-8";
                context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
                context.Response.Headers.Pragma = "no-cache";
                context.Response.Headers.Expires = "0";
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                return Task.CompletedTask;
            });
        }

        await next(context);
    }

    internal static async Task ApplyPublicPwaStaticAssetHeadersAsync(HttpContext context, RequestDelegate next)
    {
        string requestPath = context.Request.Path.Value ?? string.Empty;
        bool isPublicPwaAsset = (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
            && PublicPwaStaticAssetPaths.Contains(requestPath);
        if (isPublicPwaAsset)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.CacheControl = "public, max-age=300, must-revalidate";
                context.Response.Headers.Remove("Pragma");
                context.Response.Headers.Remove("Expires");
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                return Task.CompletedTask;
            });
        }

        await next(context);
    }

    private static string ResolveBrowserStateRoot(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        string configuredRoot = configuration["CHUMMER_PLAY_BROWSER_STATE_DIR"]
            ?? Path.Combine(hostEnvironment.ContentRootPath, ".artifacts", "browser-state");
        string fullRoot = Path.GetFullPath(configuredRoot);

        bool isolatePerApp = bool.TryParse(configuration["CHUMMER_PLAY_BROWSER_STATE_ISOLATE_PER_APP"], out bool isolated)
            && isolated;
        return isolatePerApp
            ? Path.Combine(fullRoot, Guid.NewGuid().ToString("N"))
            : fullRoot;
    }

    internal static async Task RequireTrustedPlayApiBoundaryAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Path.StartsWithSegments("/api/play"))
        {
            await next(context);
            return;
        }

        ApplyPrivateMobileDocumentHeaders(context.Response);
        context.Response.OnStarting(() =>
        {
            ApplyPrivateMobileDocumentHeaders(context.Response);
            return Task.CompletedTask;
        });

        if (!IsTrustedPlayApiRequest(context))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "play_api_forbidden",
                detail = "Mobile play APIs require a local development context, loopback request, or configured play API key."
            });
            return;
        }

        await next(context);
    }

    internal static async Task RequirePrivateMobileDocumentBoundaryAsync(HttpContext context, RequestDelegate next)
    {
        if (!IsPrivateMobileDocumentPath(context.Request.Path)
            || IsMobileLiveDocumentPath(context.Request.Path))
        {
            await next(context);
            return;
        }

        ApplyPrivateMobileDocumentHeaders(context.Response);
        context.Response.OnStarting(() =>
        {
            ApplyPrivateMobileDocumentHeaders(context.Response);
            return Task.CompletedTask;
        });

        await next(context);
    }

    internal static async Task RequireTrustedMobileLiveGrantBoundaryAsync(
        HttpContext context,
        RequestDelegate next)
    {
        if (!IsMobileLiveDocumentPath(context.Request.Path))
        {
            await next(context);
            return;
        }

        ApplyPrivateMobileLiveHeaders(context.Response);
        context.Response.OnStarting(() =>
        {
            ApplyPrivateMobileLiveHeaders(context.Response);
            return Task.CompletedTask;
        });

        if (!PlaySessionGrantPolicy.TryResolve(context, out PlaySessionGrant? grant, out string error))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "play_session_grant_required",
                detail = error
            });
            return;
        }

        context.Items[PlaySessionGrantPolicy.HttpContextItemKey] = grant;
        await next(context);
    }

    internal static bool IsMobileLiveDocumentPath(PathString path)
    {
        string value = (path.Value ?? string.Empty).TrimEnd('/');
        return value.Equals("/mobile/live", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsPrivateMobileDocumentPath(PathString path)
    {
        string value = (path.Value ?? string.Empty).TrimEnd('/');
        if (value.Equals("/mobile/service-worker.js", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return value.Equals("/mobile", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/mobile/", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyPrivateMobileDocumentHeaders(HttpResponse response)
    {
        ApplyPrivateNoStoreHeaders(response);
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers["Content-Security-Policy"] = "default-src 'none'; base-uri 'none'; connect-src 'none'; form-action 'self'; frame-ancestors 'none'; img-src 'self' data:; manifest-src 'self'; script-src 'self'; style-src 'self'; worker-src 'self'";
        response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
    }

    private static void ApplyPrivateMobileLiveHeaders(HttpResponse response)
    {
        ApplyPrivateNoStoreHeaders(response);
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers["Content-Security-Policy"] = "default-src 'none'; base-uri 'none'; connect-src 'self'; form-action 'self'; frame-ancestors 'none'; img-src 'self' data:; manifest-src 'self'; script-src 'self'; style-src 'self'; worker-src 'self'";
        response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
    }

    private static void ApplyPrivateNoStoreHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "private, no-store";
        response.Headers.Pragma = "no-cache";
        response.Headers.Expires = "0";
    }

    public static bool IsTrustedPlayApiRequest(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IPAddress? remoteAddress = context.Connection.RemoteIpAddress;
        if (remoteAddress is null)
        {
            return false;
        }

        if (context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            return true;
        }

        if (IPAddress.IsLoopback(remoteAddress))
        {
            return true;
        }

        string configuredKey = context.RequestServices.GetRequiredService<IConfiguration>()["CHUMMER_PLAY_API_KEY"]?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            return false;
        }

        string suppliedKey = context.Request.Headers["X-Chummer-Play-Api-Key"].ToString().Trim();
        return FixedTimeEquals(suppliedKey, configuredKey);
    }

    private static bool FixedTimeEquals(string supplied, string expected)
    {
        byte[] suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        return suppliedBytes.Length == expectedBytes.Length
               && CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes);
    }

    private static async Task<PlayResumeResponse> BuildResumeResponseAsync(
        string sessionId,
        PlaySurfaceRole role,
        IPlayEventLogStore eventLogStore,
        IPlayOfflineCacheService offlineCacheService,
        Chummer.Play.Components.Shell.PlayShellDescriptor playerShell,
        Chummer.Play.Components.Shell.PlayShellDescriptor gmShell,
        CancellationToken cancellationToken)
    {
        var resumeState = await ResolveResumeStateAsync(
            sessionId,
            eventLogStore,
            offlineCacheService,
            cancellationToken
        );

        var activeShell = SelectShell(role, playerShell, gmShell);
        var roleCapabilities = PlayRouteHandlers.ResolveRoleCapabilities(activeShell);
        var projection = PlayRouteHandlers.BuildProjection(
            resumeState.Session,
            resumeState.Ledger.LastKnownSequence,
            resumeState.Ledger.PendingEvents
        );
        var bootstrap = new PlayBootstrapResponse(
            "chummer6-mobile",
            projection,
            activeShell,
            [activeShell],
            new BrowserSessionShellProbe(true, resumeState.RuntimeBundle is not null, true),
            roleCapabilities,
            BuildSpiderCards(roleCapabilities),
            BuildCoachHints(role),
            PlayRouteHandlers.BuildQuickActions(role, roleCapabilities)
        );
        var cachePressure = await offlineCacheService.GetCachePressureAsync(cancellationToken);
        var supportNotice = BuildSupportClosureNotice(
            sessionId,
            resumeState.Session,
            resumeState.RuntimeBundle,
            cachePressure);

        return new PlayResumeResponse(
            sessionId,
            role,
            PlayRouteHandlers.BuildOwnerRoute(sessionId, role),
            bootstrap,
            resumeState.Checkpoint,
            resumeState.RuntimeBundle,
            cachePressure,
            supportNotice
        );
    }

    private static PlaySupportClosureNotice BuildSupportClosureNotice(
        string sessionId,
        EngineSessionEnvelope session,
        PlayRuntimeBundleMetadata? runtimeBundle,
        PlayCachePressureSnapshot cachePressure)
    {
        string bundle = runtimeBundle?.BundleTag ?? string.Empty;
        string followThroughHref = runtimeBundle is null
            ? $"/contact?kind=install_help&title={Uri.EscapeDataString($"Mobile runtime proof is missing for {session.SceneId}")}&summary={Uri.EscapeDataString($"This mobile shell resumed {sessionId}/{session.SceneId} on {session.RuntimeFingerprint} without a validated local bundle.")}&detail={Uri.EscapeDataString($"Session: {sessionId}\nScene: {session.SceneId}\nRuntime: {session.RuntimeFingerprint}\nBundle: none cached locally\n\nWhat happened:\n- This device resumed without a validated runtime bundle.\n- Please ground the next recovery or support step against the reconnect path.")}&sessionId={Uri.EscapeDataString(sessionId)}&sceneId={Uri.EscapeDataString(session.SceneId)}&runtime={Uri.EscapeDataString(session.RuntimeFingerprint)}"
            : $"/contact?kind=install_help&title={Uri.EscapeDataString($"Mobile fix review for {session.SceneId}")}&summary={Uri.EscapeDataString($"This mobile shell resumed {sessionId}/{session.SceneId} on {session.RuntimeFingerprint} with bundle {bundle}.")}&detail={Uri.EscapeDataString($"Session: {sessionId}\nScene: {session.SceneId}\nRuntime: {session.RuntimeFingerprint}\nBundle: {bundle}\n\nWhat happened:\n- This device resumed with a validated runtime bundle.\n- Please ground the next support or fix-verification step against the current mobile shell.")}&sessionId={Uri.EscapeDataString(sessionId)}&sceneId={Uri.EscapeDataString(session.SceneId)}&runtime={Uri.EscapeDataString(session.RuntimeFingerprint)}&bundle={Uri.EscapeDataString(bundle)}";

        if (runtimeBundle is null)
        {
            return new PlaySupportClosureNotice(
                StatusLabel: "Runtime proof missing",
                KnownIssueSummary: $"This device resumed {sessionId}/{session.SceneId} without a validated local bundle, so offline trust and support closure are still provisional.",
                FixAvailabilitySummary: $"No grounded local fix target is available yet for {session.RuntimeFingerprint}.",
                NextSafeAction: $"Reconnect {session.SceneId} and validate a grounded runtime bundle before you trust offline continuation or fix closure on this device.",
                FollowThroughHref: followThroughHref);
        }

        if (cachePressure.BackpressureActive)
        {
            return new PlaySupportClosureNotice(
                StatusLabel: "Watch cache drift",
                KnownIssueSummary: $"Cache pressure already touched {cachePressure.EvictedEntryCount} session(s), so bundle {bundle} may need revalidation before you trust support closure on this shell.",
                FixAvailabilitySummary: $"Bundle {bundle} is validated now, but cache pressure can still evict the active local fix target.",
                NextSafeAction: $"Clear cache pressure, then re-check bundle {bundle} before you verify a fix or trust the next offline session.",
                FollowThroughHref: followThroughHref);
        }

        return new PlaySupportClosureNotice(
            StatusLabel: "Ready to verify",
            KnownIssueSummary: $"If {sessionId}/{session.SceneId} still reproduces the same problem, report it against bundle {bundle} so support can ground the case against this mobile shell.",
            FixAvailabilitySummary: $"Bundle {bundle} is the grounded local fix and update target for {session.RuntimeFingerprint}.",
            NextSafeAction: $"Use the current bundle proof for {session.SceneId} if you verify a fix or reopen support on this device.",
            FollowThroughHref: followThroughHref);
    }

    private static bool TryResolveTrustedRestoreDeviceId(
        PlaySurfaceRole role,
        string? requestedDeviceId,
        out string? trustedDeviceId,
        out string[] allowedDeviceIds)
    {
        string primary = BuildPrimaryRestoreDeviceId(role);
        allowedDeviceIds = [primary, $"{primary}:travel"];

        if (string.IsNullOrWhiteSpace(requestedDeviceId))
        {
            trustedDeviceId = null;
            return true;
        }

        string normalized = requestedDeviceId.Trim();
        if (allowedDeviceIds.Any(allowed => string.Equals(allowed, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            trustedDeviceId = primary;
            return true;
        }

        trustedDeviceId = null;
        return false;
    }

    private static string BuildPrimaryRestoreDeviceId(PlaySurfaceRole role)
        => role switch
        {
            PlaySurfaceRole.GameMaster => "install-workstation",
            PlaySurfaceRole.Observer => "install-observer_screen",
            _ => "install-play_tablet"
        };

    private static PlayShellSnapshot SelectShell(
        PlaySurfaceRole role,
        Chummer.Play.Components.Shell.PlayShellDescriptor playerShell,
        Chummer.Play.Components.Shell.PlayShellDescriptor gmShell
    ) =>
        role switch
        {
            PlaySurfaceRole.GameMaster => PlayRouteHandlers.ToSnapshot(gmShell),
            PlaySurfaceRole.Observer => new PlayShellSnapshot(
                PlaySurfaceRole.Observer,
                "Observer Shell",
                "Read-mostly observe surface for continuity review and tactical mirroring.",
                ["play.session.read"]
            ),
            _ => PlayRouteHandlers.ToSnapshot(playerShell)
        };

    private static IReadOnlyList<PlayTacticalSpiderCard> BuildSpiderCards(IReadOnlyList<string> roleCapabilities)
    {
        var cards = new List<PlayTacticalSpiderCard>();
        if (roleCapabilities.Contains("play.spider.cards", StringComparer.Ordinal))
        {
            cards.Add(
                new PlayTacticalSpiderCard(
                    "spider-line-of-fire",
                    "Line Of Fire",
                    "Highlights threatened lanes and contested cover for the next exchange.",
                    "play.spider.cards"
                )
            );
            cards.Add(
                new PlayTacticalSpiderCard(
                    "spider-exposure-window",
                    "Exposure Window",
                    "Surfaces open windows where team movement could overextend evidence position.",
                    "play.spider.cards"
                )
            );
        }

        return cards;
    }

    private static IReadOnlyList<PlayCoachHint> BuildCoachHints(PlaySurfaceRole role) =>
        role switch
        {
            PlaySurfaceRole.GameMaster => [
                new PlayCoachHint("coach-gm-stale", "Use stale-protected actions when scene revision changes."),
                new PlayCoachHint("coach-gm-evidence", "Keep tactical reveals aligned with evidence lineage.")
            ],
            PlaySurfaceRole.Observer => [
                new PlayCoachHint("coach-observer-continuity", "Confirm continuity before mirroring tactical updates."),
                new PlayCoachHint("coach-observer-readonly", "Keep the observer lane read-mostly until the owner lane confirms the next revision.")
            ],
            _ => [
                new PlayCoachHint("coach-player-sync", "Sync before submitting quick actions after reconnect."),
                new PlayCoachHint("coach-player-focus", "Use mobile quick actions to keep turns concise.")
            ]
        };

    private sealed record PlayRouteValidationError(
        string Error,
        string Message,
        string ProvidedDeviceId,
        IReadOnlyList<string> AllowedDeviceIds);

    public static async Task<(EngineSessionEnvelope Session, OfflineLedgerEnvelope Ledger)> ResolveProjectionSessionAsync(
        string sessionId,
        IPlayEventLogStore eventLogStore,
        IPlayOfflineCacheService offlineCacheService,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var checkpoint = await offlineCacheService.GetCheckpointAsync(sessionId, cancellationToken);
        var existingLedger = await eventLogStore.GetExistingAsync(sessionId, cancellationToken);
        var preferredSession = ResolvePreferredSession(sessionId, checkpoint, runtimeBundle: null, existingLedger);
        var ledger = existingLedger is not null
            && SessionLineage.IsLedgerAligned(
                existingLedger,
                preferredSession.SessionId,
                preferredSession.SceneId,
                preferredSession.SceneRevision,
                preferredSession.RuntimeFingerprint
            )
                ? existingLedger
                : await eventLogStore.GetOrCreateAsync(
                    preferredSession.SessionId,
                    preferredSession.SceneId,
                    preferredSession.SceneRevision,
                    preferredSession.RuntimeFingerprint,
                    cancellationToken
                );

        return (
            new EngineSessionEnvelope(
                ledger.SessionId,
                ledger.SceneId,
                ledger.SceneRevision,
                ledger.RuntimeFingerprint
            ),
            ledger
        );
    }

    public static async Task<PlayResumeState> ResolveResumeStateAsync(
        string sessionId,
        IPlayEventLogStore eventLogStore,
        IPlayOfflineCacheService offlineCacheService,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var checkpoint = await offlineCacheService.GetCheckpointAsync(sessionId, cancellationToken);
        var runtimeBundle = await offlineCacheService.GetRuntimeBundleAsync(sessionId, cancellationToken);
        var existingLedger = await eventLogStore.GetExistingAsync(sessionId, cancellationToken);
        var session = ResolvePreferredSession(sessionId, checkpoint, runtimeBundle, existingLedger);
        var ledger = existingLedger is not null
            && SessionLineage.IsLedgerAligned(
                existingLedger,
                session.SessionId,
                session.SceneId,
                session.SceneRevision,
                session.RuntimeFingerprint
            )
                ? existingLedger
                : await eventLogStore.GetOrCreateAsync(
                    session.SessionId,
                    session.SceneId,
                    session.SceneRevision,
                    session.RuntimeFingerprint,
                    cancellationToken
                );

        var effectiveSession = new EngineSessionEnvelope(
            ledger.SessionId,
            ledger.SceneId,
            ledger.SceneRevision,
            ledger.RuntimeFingerprint
        );
        var alignedCheckpoint = PlayRouteHandlers.CreateAlignedCheckpoint(
            effectiveSession,
            Math.Max(ledger.LastKnownSequence, checkpoint?.AppliedThroughSequence ?? 0),
            checkpoint
        );
        await offlineCacheService.SetCheckpointAsync(alignedCheckpoint, cancellationToken);

        return new PlayResumeState(
            effectiveSession,
            ledger,
            alignedCheckpoint,
            runtimeBundle
        );
    }

    private static EngineSessionEnvelope ResolvePreferredSession(
        string sessionId,
        SyncCheckpoint? checkpoint,
        PlayRuntimeBundleMetadata? runtimeBundle,
        OfflineLedgerEnvelope? ledger
    )
    {
        var checkpointSession = checkpoint is null
            ? null
            : new EngineSessionEnvelope(
                checkpoint.SessionId,
                checkpoint.SceneId,
                checkpoint.SceneRevision,
                checkpoint.ProjectionFingerprint
            );
        var ledgerSession = ledger is null
            ? null
            : new EngineSessionEnvelope(
                ledger.SessionId,
                ledger.SceneId,
                ledger.SceneRevision,
                ledger.RuntimeFingerprint
            );
        var runtimeSession = runtimeBundle is null
            ? null
            : new EngineSessionEnvelope(
                sessionId,
                checkpoint?.SceneId ?? ledger?.SceneId ?? "scene-main",
                runtimeBundle.SceneRevision,
                runtimeBundle.RuntimeFingerprint
            );

        if (checkpointSession is not null
            && (ledgerSession is null || SessionLineage.IsSessionAligned(ledgerSession, checkpointSession)))
        {
            return checkpointSession;
        }

        if (ledgerSession is not null)
        {
            return ledgerSession;
        }

        if (runtimeSession is not null)
        {
            return runtimeSession;
        }

        if (checkpointSession is not null)
        {
            return checkpointSession;
        }

        return new EngineSessionEnvelope(sessionId, "scene-main", "scene-r1", "runtime-local");
    }

    private static string BuildPlayIndexHref(
        string? sessionId,
        string? deviceId,
        PlaySurfaceRole role,
        string? artifactView = null,
        string? artifactId = null)
    {
        List<string> queryParts =
        [
            $"role={Uri.EscapeDataString(role.ToString())}"
        ];

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            queryParts.Insert(0, $"sessionId={Uri.EscapeDataString(sessionId)}");
        }

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            queryParts.Add($"deviceId={Uri.EscapeDataString(deviceId)}");
        }

        if (!string.IsNullOrWhiteSpace(artifactView))
        {
            queryParts.Add($"artifactView={Uri.EscapeDataString(artifactView)}");
        }

        if (!string.IsNullOrWhiteSpace(artifactId))
        {
            queryParts.Add($"artifactId={Uri.EscapeDataString(artifactId)}");
        }

        return $"/index.html?{string.Join("&", queryParts)}";
    }

}

public sealed record PlayResumeState(
    EngineSessionEnvelope Session,
    OfflineLedgerEnvelope Ledger,
    SyncCheckpoint? Checkpoint,
    PlayRuntimeBundleMetadata? RuntimeBundle
);
