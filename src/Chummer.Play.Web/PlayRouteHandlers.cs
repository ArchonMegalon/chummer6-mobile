using Chummer.Play.Core.Application;
using Chummer.Play.Core.Offline;
using Chummer.Play.Core.PlayApi;
using Chummer.Play.Core.Sync;
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
}
