using Chummer.Play.Core.Application;
using Chummer.Play.Core.Offline;
using Chummer.Play.Core.PlayApi;
using Chummer.Play.Core.Sync;
using Chummer.Play.Web.BrowserState;
using Microsoft.AspNetCore.Http;

namespace Chummer.Play.Web;

public static class PlayRouteHandlers
{
    public static async Task<IResult> HandleReconnectAsync(
        PlayReconnectRequest request,
        IPlayEventLogStore eventLogStore,
        IPlayOfflineCacheService offlineCacheService,
        CancellationToken cancellationToken
    )
    {
        if (!TryValidateCursor(request.Cursor, out var cursorError))
        {
            return Results.BadRequest(new { error = cursorError });
        }

        var requestSession = request.Cursor.Session;
        var checkpoint = await offlineCacheService.GetCheckpointAsync(requestSession.SessionId, cancellationToken);
        var existingLedger = await eventLogStore.GetExistingAsync(requestSession.SessionId, cancellationToken);
        if (!SessionLineage.IsStoredLineageAligned(requestSession, checkpoint, existingLedger))
        {
            var (staleProjection, staleCheckpoint) = BuildStoredStaleState(requestSession, request.Cursor, checkpoint, existingLedger);
            return Results.Conflict(
                new
                {
                    error = "session lineage changed",
                    stale = true,
                    projection = staleProjection,
                    checkpoint = staleCheckpoint,
                }
            );
        }

        var effectiveSession = ResolveStoredSession(requestSession, checkpoint, existingLedger);
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
        var appliedThroughSequence = Math.Max(request.Cursor.AppliedThroughSequence, ledger.LastKnownSequence);
        var reconnectCheckpoint = CreateAlignedCheckpoint(effectiveSession, appliedThroughSequence, checkpoint);
        await offlineCacheService.SetCheckpointAsync(reconnectCheckpoint, cancellationToken);

        return Results.Json(
            new PlayReconnectResponse(
                BuildProjection(
                    effectiveSession,
                    appliedThroughSequence,
                    ledger.PendingEvents
                ),
                reconnectCheckpoint,
                ledger
            )
        );
    }

    public static async Task<IResult> HandleContinuityClaimAsync(
        PlayContinuityClaimRequest request,
        IPlayEventLogStore eventLogStore,
        IPlayOfflineCacheService offlineCacheService,
        IBrowserKeyValueStore browserStore,
        CancellationToken cancellationToken
    )
    {
        if (!TryValidateCursor(request.Cursor, out var cursorError))
        {
            return Results.BadRequest(new { error = cursorError });
        }

        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return Results.BadRequest(new { error = "device id is required." });
        }

        if (string.IsNullOrWhiteSpace(request.ObserverId))
        {
            return Results.BadRequest(new { error = "observer id is required." });
        }

        var requestSession = request.Cursor.Session;
        var checkpoint = await offlineCacheService.GetCheckpointAsync(requestSession.SessionId, cancellationToken);
        var existingLedger = await eventLogStore.GetExistingAsync(requestSession.SessionId, cancellationToken);
        var hasStoredLineage = checkpoint is not null || existingLedger is not null;

        if (!hasStoredLineage)
        {
            var continuity = new PlayObserverContinuity(
                request.ObserverId,
                request.DeviceId,
                request.Role,
                request.Cursor.AppliedThroughSequence,
                DateTimeOffset.UtcNow,
                BuildContinuityToken(requestSession.SessionId, request.Cursor.AppliedThroughSequence)
            );
            var emptyProjection = BuildProjection(requestSession, request.Cursor.AppliedThroughSequence, Array.Empty<string>());
            return Results.Json(
                new PlayContinuityClaimResponse(
                    false,
                    false,
                    "session not bootstrapped",
                    emptyProjection,
                    CreateAlignedCheckpoint(requestSession, request.Cursor.AppliedThroughSequence, checkpoint),
                    continuity,
                    "/play/{sessionId}"
                )
            );
        }

        if (!SessionLineage.IsStoredLineageAligned(requestSession, checkpoint, existingLedger))
        {
            var (staleProjection, staleCheckpoint) = BuildStoredStaleState(requestSession, request.Cursor, checkpoint, existingLedger);
            var staleContinuity = new PlayObserverContinuity(
                request.ObserverId,
                request.DeviceId,
                request.Role,
                staleProjection.Cursor.AppliedThroughSequence,
                DateTimeOffset.UtcNow,
                BuildContinuityToken(requestSession.SessionId, staleProjection.Cursor.AppliedThroughSequence)
            );
            return Results.Json(
                new PlayContinuityClaimResponse(
                    false,
                    true,
                    "session lineage changed",
                    staleProjection,
                    staleCheckpoint,
                    staleContinuity,
                    "/play/{sessionId}"
                )
            );
        }

        var effectiveSession = ResolveStoredSession(requestSession, checkpoint, existingLedger);
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
        var continuitySequence = Math.Max(ledger.LastKnownSequence, request.Cursor.AppliedThroughSequence);
        var alignedCheckpoint = CreateAlignedCheckpoint(effectiveSession, continuitySequence, checkpoint);
        await offlineCacheService.SetCheckpointAsync(alignedCheckpoint, cancellationToken);

        var continuityToken = BuildContinuityToken(effectiveSession.SessionId, continuitySequence);
        var continuityEntry = new ObserverContinuityEntry(
            effectiveSession.SessionId,
            effectiveSession.SceneId,
            effectiveSession.SceneRevision,
            effectiveSession.RuntimeFingerprint,
            request.ObserverId,
            request.DeviceId,
            request.Role,
            continuitySequence,
            DateTimeOffset.UtcNow,
            continuityToken
        );
        await browserStore.SetAsync(
            PlayBrowserStateKeys.Continuity(effectiveSession.SessionId),
            continuityEntry,
            cancellationToken
        );

        return Results.Json(
            new PlayContinuityClaimResponse(
                true,
                false,
                "accepted",
                BuildProjection(effectiveSession, continuitySequence, ledger.PendingEvents),
                alignedCheckpoint,
                new PlayObserverContinuity(
                    continuityEntry.ObserverId,
                    continuityEntry.DeviceId,
                    continuityEntry.Role,
                    continuityEntry.ObservedThroughSequence,
                    continuityEntry.ObservedAtUtc,
                    continuityEntry.ContinuityToken
                ),
                "/play/{sessionId}"
            )
        );
    }

    public static async Task<IResult> HandleObserveAsync(
        string sessionId,
        IPlayEventLogStore eventLogStore,
        IPlayOfflineCacheService offlineCacheService,
        IBrowserKeyValueStore browserStore,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        var runtimeBundle = await offlineCacheService.GetRuntimeBundleAsync(sessionId, cancellationToken);
        var checkpoint = await offlineCacheService.GetCheckpointAsync(sessionId, cancellationToken);
        var existingLedger = await eventLogStore.GetExistingAsync(sessionId, cancellationToken);
        var fallbackSession = new EngineSessionEnvelope(
            sessionId,
            checkpoint?.SceneId ?? existingLedger?.SceneId ?? "scene-main",
            checkpoint?.SceneRevision ?? existingLedger?.SceneRevision ?? runtimeBundle?.SceneRevision ?? "scene-r1",
            checkpoint?.ProjectionFingerprint ?? existingLedger?.RuntimeFingerprint ?? runtimeBundle?.RuntimeFingerprint ?? "runtime-local"
        );
        var storedSession = ResolveStoredSession(fallbackSession, checkpoint, existingLedger);
        var effectiveSession = new EngineSessionEnvelope(
            sessionId,
            storedSession.SceneId,
            storedSession.SceneRevision,
            storedSession.RuntimeFingerprint
        );
        var hasStoredState = checkpoint is not null || existingLedger is not null;
        var observedRuntimeBundle = SelectObservedRuntimeBundle(effectiveSession, runtimeBundle);
        OfflineLedgerEnvelope? ledger = null;
        SyncCheckpoint alignedCheckpoint;
        long appliedThroughSequence;

        if (hasStoredState)
        {
            ledger = existingLedger;
            appliedThroughSequence = Math.Max(ledger?.LastKnownSequence ?? 0, checkpoint?.AppliedThroughSequence ?? 0);
            alignedCheckpoint = CreateAlignedCheckpoint(effectiveSession, appliedThroughSequence, checkpoint);
        }
        else
        {
            appliedThroughSequence = 0;
            alignedCheckpoint = new SyncCheckpoint(
                effectiveSession.SessionId,
                effectiveSession.SceneId,
                effectiveSession.SceneRevision,
                effectiveSession.RuntimeFingerprint,
                appliedThroughSequence,
                DateTimeOffset.UtcNow
            );
        }
        var continuity = await GetStoredContinuityAsync(
            alignedCheckpoint.SessionId,
            alignedCheckpoint.ToSessionEnvelope(),
            alignedCheckpoint.AppliedThroughSequence,
            browserStore,
            cancellationToken
        );

        return Results.Json(
            new PlayObserveResponse(
                sessionId,
                BuildProjection(alignedCheckpoint.ToSessionEnvelope(), appliedThroughSequence, ledger?.PendingEvents ?? Array.Empty<string>()),
                alignedCheckpoint,
                continuity,
                observedRuntimeBundle
            )
        );
    }

    public static async Task<IResult> HandleQuickActionAsync(
        PlayQuickActionRequest request,
        IPlayOfflineCacheService offlineCacheService,
        IPlayEventLogStore eventLogStore,
        IPlayOfflineQueueService offlineQueueService,
        Chummer.Play.Components.Shell.PlayShellDescriptor playerShell,
        Chummer.Play.Components.Shell.PlayShellDescriptor gmShell,
        CancellationToken cancellationToken
    )
    {
        if (!TryValidateCursor(request.Cursor, out var cursorError))
        {
            return Results.BadRequest(new { error = cursorError });
        }

        if (string.IsNullOrWhiteSpace(request.ActionId))
        {
            return Results.BadRequest(new { error = "quick action id is required." });
        }

        var requestSession = request.Cursor.Session;
        var checkpoint = await offlineCacheService.GetCheckpointAsync(requestSession.SessionId, cancellationToken);
        var existingLedger = await eventLogStore.GetExistingAsync(requestSession.SessionId, cancellationToken);
        if (!SessionLineage.IsStoredLineageAligned(requestSession, checkpoint, existingLedger))
        {
            var (staleProjection, staleCheckpoint) = BuildStoredStaleState(requestSession, request.Cursor, checkpoint, existingLedger);
            return Results.Json(
                new PlayQuickActionResponse(
                    false,
                    true,
                    "session lineage changed",
                    staleProjection,
                    staleCheckpoint
                )
            );
        }

        var roleCapabilities = request.Role == PlaySurfaceRole.GameMaster
            ? ResolveRoleCapabilities(ToSnapshot(gmShell))
            : ResolveRoleCapabilities(ToSnapshot(playerShell));
        var quickAction = BuildQuickActions(request.Role, roleCapabilities).FirstOrDefault(action =>
            StringComparer.Ordinal.Equals(action.ActionId, request.ActionId)
        );
        if (quickAction is null)
        {
            var fallbackCheckpoint = checkpoint
                ?? new SyncCheckpoint(
                    requestSession.SessionId,
                    requestSession.SceneId,
                    requestSession.SceneRevision,
                    requestSession.RuntimeFingerprint,
                    request.Cursor.AppliedThroughSequence,
                    DateTimeOffset.UtcNow
                );
            return Results.Json(
                new PlayQuickActionResponse(
                    false,
                    false,
                    "action not permitted for role capabilities",
                    BuildProjection(requestSession, fallbackCheckpoint.AppliedThroughSequence, Array.Empty<string>()),
                    fallbackCheckpoint
                )
            );
        }

        OfflineQueueEnqueueResult queueResult;
        try
        {
            queueResult = await offlineQueueService.EnqueueAsync(
                request.Cursor,
                $"quick-action:{quickAction.ActionId}",
                cancellationToken
            );
        }
        catch (InvalidOperationException)
        {
            var storedCheckpoint = await offlineCacheService.GetCheckpointAsync(requestSession.SessionId, cancellationToken);
            var storedLedger = await eventLogStore.GetExistingAsync(requestSession.SessionId, cancellationToken);
            if (!SessionLineage.IsStoredLineageAligned(requestSession, storedCheckpoint, storedLedger))
            {
                var (staleProjection, staleCheckpoint) = BuildStoredStaleState(
                    requestSession,
                    request.Cursor,
                    storedCheckpoint,
                    storedLedger
                );
                return Results.Json(
                    new PlayQuickActionResponse(
                        false,
                        true,
                        "session lineage changed",
                        staleProjection,
                        staleCheckpoint
                    )
                );
            }

            throw;
        }

        return Results.Json(
            new PlayQuickActionResponse(
                true,
                false,
                "accepted",
                BuildProjection(requestSession, queueResult.AppliedThroughSequence, queueResult.Ledger.PendingEvents),
                queueResult.Checkpoint
            )
        );
    }

    public static async Task<IResult> HandleSyncAsync(
        PlaySyncRequest request,
        IPlayOfflineCacheService offlineCacheService,
        IPlayEventLogStore eventLogStore,
        IPlayOfflineQueueService offlineQueueService,
        CancellationToken cancellationToken
    )
    {
        if (!TryValidateCursor(request.Cursor, out var cursorError))
        {
            return Results.BadRequest(new { error = cursorError });
        }

        if (request.PendingEvents is null)
        {
            return Results.BadRequest(new { error = "pending events payload is required." });
        }

        if (request.PendingEvents.Any(static pendingEvent => string.IsNullOrWhiteSpace(pendingEvent)))
        {
            return Results.BadRequest(new { error = "pending events cannot contain blank values." });
        }

        var requestSession = request.Cursor.Session;
        var checkpoint = await offlineCacheService.GetCheckpointAsync(requestSession.SessionId, cancellationToken);
        var existingLedger = await eventLogStore.GetExistingAsync(requestSession.SessionId, cancellationToken);
        if (!SessionLineage.IsStoredLineageAligned(requestSession, checkpoint, existingLedger))
        {
            var (staleProjection, staleCheckpoint) = BuildStoredStaleState(requestSession, request.Cursor, checkpoint, existingLedger);
            return Results.Json(
                new PlaySyncResponse(
                    false,
                    true,
                    staleProjection,
                    staleCheckpoint,
                    0
                )
            );
        }

        OfflineQueueSyncResult syncResult;
        try
        {
            syncResult = await offlineQueueService.SyncReplayAsync(request, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            var storedCheckpoint = await offlineCacheService.GetCheckpointAsync(requestSession.SessionId, cancellationToken);
            var storedLedger = await eventLogStore.GetExistingAsync(requestSession.SessionId, cancellationToken);
            if (!SessionLineage.IsStoredLineageAligned(requestSession, storedCheckpoint, storedLedger))
            {
                var (staleProjection, staleCheckpoint) = BuildStoredStaleState(
                    requestSession,
                    request.Cursor,
                    storedCheckpoint,
                    storedLedger
                );
                return Results.Json(
                    new PlaySyncResponse(
                        false,
                        true,
                        staleProjection,
                        staleCheckpoint,
                        0
                    )
                );
            }

            throw;
        }

        return Results.Json(
            new PlaySyncResponse(
                true,
                false,
                BuildProjection(
                    requestSession,
                    syncResult.AppliedThroughSequence,
                    syncResult.Ledger.PendingEvents
                ),
                syncResult.Checkpoint,
                syncResult.AcceptedEventCount
            )
        );
    }

    public static PlaySessionProjection BuildProjection(
        EngineSessionEnvelope session,
        long appliedThroughSequence,
        IReadOnlyList<string> pendingEvents
    ) =>
        new(
            new EngineSessionCursor(session, appliedThroughSequence),
            pendingEvents.Count == 0
                ? ["projection ready", "local replay idle"]
                : ["projection ready", .. pendingEvents.Select(evt => $"pending:{evt}")],
            DateTimeOffset.UtcNow
        );

    public static PlayShellSnapshot ToSnapshot(Chummer.Play.Components.Shell.PlayShellDescriptor descriptor) =>
        new(descriptor.Role, descriptor.ShellName, descriptor.Summary, descriptor.RequiredCapabilities);

    public static IReadOnlyList<string> ResolveRoleCapabilities(PlayShellSnapshot shell) =>
        shell.RequiredCapabilities;

    public static IReadOnlyList<PlayQuickAction> BuildQuickActions(PlaySurfaceRole role, IReadOnlyList<string> roleCapabilities)
    {
        var actions = new List<PlayQuickAction>();
        if (role == PlaySurfaceRole.Player && roleCapabilities.Contains("play.session.sync", StringComparer.Ordinal))
        {
            actions.Add(new PlayQuickAction("player-mark-ready", "Mark Ready", "play.session.sync", true));
            actions.Add(new PlayQuickAction("player-request-cover", "Request Cover", "play.session.sync", true));
        }

        if (role == PlaySurfaceRole.GameMaster && roleCapabilities.Contains("play.gm.actions", StringComparer.Ordinal))
        {
            actions.Add(new PlayQuickAction("gm-advance-initiative", "Advance Initiative", "play.gm.actions", true));
        }

        if (role == PlaySurfaceRole.GameMaster && roleCapabilities.Contains("play.spider.cards", StringComparer.Ordinal))
        {
            actions.Add(new PlayQuickAction("gm-publish-spider-card", "Publish Spider Card", "play.spider.cards", true));
        }

        return actions;
    }

    public static bool TryValidateCursor(EngineSessionCursor cursor, out string error)
    {
        if (cursor is null)
        {
            error = "session cursor is required.";
            return false;
        }

        if (cursor.AppliedThroughSequence < 0)
        {
            error = "applied through sequence cannot be negative.";
            return false;
        }

        return TryValidateSession(cursor.Session, out error);
    }

    public static EngineSessionEnvelope ResolveStoredSession(
        EngineSessionEnvelope requestSession,
        SyncCheckpoint? checkpoint,
        OfflineLedgerEnvelope? existingLedger
    )
    {
        ArgumentNullException.ThrowIfNull(requestSession);

        if (existingLedger is not null)
        {
            return new EngineSessionEnvelope(
                existingLedger.SessionId,
                existingLedger.SceneId,
                existingLedger.SceneRevision,
                existingLedger.RuntimeFingerprint
            );
        }

        if (checkpoint is not null)
        {
            return new EngineSessionEnvelope(
                checkpoint.SessionId,
                checkpoint.SceneId,
                checkpoint.SceneRevision,
                checkpoint.ProjectionFingerprint
            );
        }

        return requestSession;
    }

    public static (PlaySessionProjection Projection, SyncCheckpoint Checkpoint) BuildStoredStaleState(
        EngineSessionEnvelope requestSession,
        EngineSessionCursor requestCursor,
        SyncCheckpoint? checkpoint,
        OfflineLedgerEnvelope? existingLedger
    )
    {
        ArgumentNullException.ThrowIfNull(requestSession);
        ArgumentNullException.ThrowIfNull(requestCursor);

        var staleSession = ResolveStoredSession(requestSession, checkpoint, existingLedger);
        var staleSequence = existingLedger?.LastKnownSequence
            ?? checkpoint?.AppliedThroughSequence
            ?? requestCursor.AppliedThroughSequence;
        var stalePendingEvents = existingLedger?.PendingEvents ?? Array.Empty<string>();
        var staleProjection = BuildProjection(staleSession, staleSequence, stalePendingEvents);
        var staleCheckpoint = CreateAlignedCheckpoint(staleSession, staleSequence, checkpoint);
        return (staleProjection, staleCheckpoint);
    }

    public static SyncCheckpoint CreateAlignedCheckpoint(
        EngineSessionEnvelope session,
        long appliedThroughSequence,
        SyncCheckpoint? existingCheckpoint = null
    ) =>
        new(
            session.SessionId,
            session.SceneId,
            session.SceneRevision,
            session.RuntimeFingerprint,
            existingCheckpoint is null
                ? appliedThroughSequence
                : Math.Max(existingCheckpoint.AppliedThroughSequence, appliedThroughSequence),
            DateTimeOffset.UtcNow
        );

    public static bool TryValidateSession(EngineSessionEnvelope session, out string error)
    {
        if (session is null)
        {
            error = "session envelope is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(session.SessionId))
        {
            error = "session id is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(session.SceneId))
        {
            error = "scene id is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(session.SceneRevision))
        {
            error = "scene revision is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(session.RuntimeFingerprint))
        {
            error = "runtime fingerprint is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static async Task<PlayObserverContinuity?> GetStoredContinuityAsync(
        string sessionId,
        EngineSessionEnvelope session,
        long maxObservedThroughSequence,
        IBrowserKeyValueStore browserStore,
        CancellationToken cancellationToken
    )
    {
        var key = PlayBrowserStateKeys.Continuity(sessionId);
        var continuity = await browserStore.GetAsync<ObserverContinuityEntry>(key, cancellationToken);
        if (continuity is null)
        {
            await browserStore.RemoveAsync(key, cancellationToken);
            return null;
        }

        var lineageMatches = string.Equals(continuity.SessionId, sessionId, StringComparison.Ordinal)
            && string.Equals(continuity.SceneId, session.SceneId, StringComparison.Ordinal)
            && string.Equals(continuity.SceneRevision, session.SceneRevision, StringComparison.Ordinal)
            && string.Equals(continuity.RuntimeFingerprint, session.RuntimeFingerprint, StringComparison.Ordinal);
        if (!lineageMatches || continuity.ObservedThroughSequence > maxObservedThroughSequence)
        {
            await browserStore.RemoveAsync(key, cancellationToken);
            return null;
        }

        return new PlayObserverContinuity(
            continuity.ObserverId,
            continuity.DeviceId,
            continuity.Role,
            continuity.ObservedThroughSequence,
            continuity.ObservedAtUtc,
            continuity.ContinuityToken
        );
    }

    private static string BuildContinuityToken(string sessionId, long observedThroughSequence)
    {
        var safeSessionId = sessionId.Replace(':', '-');
        return $"{safeSessionId}:{observedThroughSequence}:{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    }

    private static PlayRuntimeBundleMetadata? SelectObservedRuntimeBundle(
        EngineSessionEnvelope session,
        PlayRuntimeBundleMetadata? runtimeBundle
    )
    {
        ArgumentNullException.ThrowIfNull(session);
        if (runtimeBundle is null)
        {
            return null;
        }

        return StringComparer.Ordinal.Equals(runtimeBundle.SceneRevision, session.SceneRevision)
            && StringComparer.Ordinal.Equals(runtimeBundle.RuntimeFingerprint, session.RuntimeFingerprint)
                ? runtimeBundle
                : null;
    }
}

file static class SyncCheckpointExtensions
{
    public static EngineSessionEnvelope ToSessionEnvelope(this SyncCheckpoint checkpoint) =>
        new(
            checkpoint.SessionId,
            checkpoint.SceneId,
            checkpoint.SceneRevision,
            checkpoint.ProjectionFingerprint
        );
}
