using Chummer.Play.Core.Application;
using Chummer.Play.Core.Offline;
using Chummer.Play.Core.PlayApi;
using Chummer.Play.Core.Sync;
using Chummer.Play.Gm.TacticalShell;
using Chummer.Play.Player.PlayerShell;
using Chummer.Play.Web.BrowserState;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Chummer.Play.Web;

public static class PlayWebApplication
{
    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureServices(builder.Services);

        var app = builder.Build();
        Configure(app);
        return app;
    }

    internal static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IBrowserKeyValueStore, InMemoryBrowserKeyValueStore>();
        services.AddSingleton<BrowserSessionEventLogStore>();
        services.AddSingleton<BrowserSessionOfflineCacheService>();
        services.AddSingleton<BrowserSessionOfflineQueueService>();
        services.AddSingleton<IPlayEventLogStore>(serviceProvider => serviceProvider.GetRequiredService<BrowserSessionEventLogStore>());
        services.AddSingleton<IPlayOfflineCacheService>(serviceProvider => serviceProvider.GetRequiredService<BrowserSessionOfflineCacheService>());
        services.AddSingleton<IPlayOfflineQueueService>(serviceProvider => serviceProvider.GetRequiredService<BrowserSessionOfflineQueueService>());
    }

    internal static void Configure(WebApplication app)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();

        var playerShell = PlayerShellModule.CreateDescriptor();
        var gmShell = GmTacticalShellModule.CreateDescriptor();

        app.MapGet("/health", () => Results.Text("ok"));
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

                return Results.Json(
                    new PlayResumeResponse(
                        sessionId,
                        role,
                        "/play/{sessionId}",
                        bootstrap,
                        resumeState.Checkpoint,
                        resumeState.RuntimeBundle,
                        cachePressure
                    )
                );
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
            "/play/{sessionId}",
            (string sessionId, PlaySurfaceRole role = PlaySurfaceRole.Player) =>
                Results.Redirect(
                    $"/index.html?sessionId={Uri.EscapeDataString(sessionId)}&role={Uri.EscapeDataString(role.ToString())}",
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
    }

    private static PlayShellSnapshot SelectShell(
        PlaySurfaceRole role,
        Chummer.Play.Components.Shell.PlayShellDescriptor playerShell,
        Chummer.Play.Components.Shell.PlayShellDescriptor gmShell
    ) =>
        role == PlaySurfaceRole.GameMaster ? PlayRouteHandlers.ToSnapshot(gmShell) : PlayRouteHandlers.ToSnapshot(playerShell);

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
        role == PlaySurfaceRole.GameMaster
            ? [
                new PlayCoachHint("coach-gm-stale", "Use stale-protected actions when scene revision changes."),
                new PlayCoachHint("coach-gm-evidence", "Keep tactical reveals aligned with evidence lineage.")
            ]
            : [
                new PlayCoachHint("coach-player-sync", "Sync before submitting quick actions after reconnect."),
                new PlayCoachHint("coach-player-focus", "Use mobile quick actions to keep turns concise.")
            ];

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

}

public sealed record PlayResumeState(
    EngineSessionEnvelope Session,
    OfflineLedgerEnvelope Ledger,
    SyncCheckpoint? Checkpoint,
    PlayRuntimeBundleMetadata? RuntimeBundle
);
