using Chummer.Play.Core.Application;
using Chummer.Play.Core.Offline;
using Chummer.Play.Core.PlayApi;
using Chummer.Play.Core.Sync;
using Chummer.Play.Gm.TacticalShell;
using Chummer.Play.Player.PlayerShell;
using Chummer.Play.Web.BrowserState;

namespace Chummer.Play.Web;

public sealed record PlayTurnCompanionReplayRequest(
    IReadOnlyList<string> Events
);

public sealed record PlayTurnCompanionQueueStatusResponse(
    bool Accepted,
    bool Stale,
    string Message,
    int AcceptedEventCount,
    int PendingQueueCount,
    string CurrentSceneSummary,
    PlayTurnTrustSurface Trust,
    PlayTurnSyncSurface Sync
);

public sealed class PlayTurnCompanionService
{
    private readonly IBrowserKeyValueStore _browserStore;
    private readonly IPlayEventLogStore _eventLogStore;
    private readonly IPlayOfflineCacheService _offlineCacheService;
    private readonly IPlayOfflineQueueService _offlineQueueService;

    public PlayTurnCompanionService(
        IBrowserKeyValueStore browserStore,
        IPlayEventLogStore eventLogStore,
        IPlayOfflineCacheService offlineCacheService,
        IPlayOfflineQueueService offlineQueueService)
    {
        _browserStore = browserStore;
        _eventLogStore = eventLogStore;
        _offlineCacheService = offlineCacheService;
        _offlineQueueService = offlineQueueService;
    }

    public async Task<PlayTurnCompanionProjection> GetProjectionAsync(
        string sessionId,
        PlaySurfaceRole role,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        PlayTurnCompanionContext context = await BuildContextAsync(sessionId, role, cancellationToken);
        PlayTurnCompanionState state = await GetOrCreateStateAsync(sessionId, role, deviceId, cancellationToken);
        return PlayTurnCompanionProjector.Project(context, state);
    }

    public async Task<PlayTurnCompanionProjection> SelectActionAsync(
        string sessionId,
        PlaySurfaceRole role,
        string actionId,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        PlayTurnCompanionState state = await GetOrCreateStateAsync(sessionId, role, deviceId, cancellationToken);
        if (role == PlaySurfaceRole.Observer)
        {
            return await GetProjectionAsync(sessionId, role, deviceId, cancellationToken);
        }

        PlayTurnCompanionState updated = PlayTurnCompanionProjector.SelectAction(state, role, actionId);
        await SaveStateAsync(sessionId, role, deviceId, updated, cancellationToken);
        return await GetProjectionAsync(sessionId, role, deviceId, cancellationToken);
    }

    public async Task<PlayTurnCompanionProjection> SelectAnchorAsync(
        string sessionId,
        PlaySurfaceRole role,
        string? anchorId,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        PlayTurnCompanionState state = await GetOrCreateStateAsync(sessionId, role, deviceId, cancellationToken);
        if (role == PlaySurfaceRole.Observer)
        {
            return await GetProjectionAsync(sessionId, role, deviceId, cancellationToken);
        }

        PlayTurnCompanionState updated = PlayTurnCompanionProjector.SelectAnchor(state, anchorId);
        await SaveStateAsync(sessionId, role, deviceId, updated, cancellationToken);
        return await GetProjectionAsync(sessionId, role, deviceId, cancellationToken);
    }

    public async Task<PlayTurnCompanionProjection> ToggleModifierAsync(
        string sessionId,
        PlaySurfaceRole role,
        string modifierId,
        bool enabled,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        PlayTurnCompanionState state = await GetOrCreateStateAsync(sessionId, role, deviceId, cancellationToken);
        if (role == PlaySurfaceRole.Observer)
        {
            return await GetProjectionAsync(sessionId, role, deviceId, cancellationToken);
        }

        PlayTurnCompanionState updated = PlayTurnCompanionProjector.ToggleModifier(state, modifierId, enabled);
        await SaveStateAsync(sessionId, role, deviceId, updated, cancellationToken);
        return await GetProjectionAsync(sessionId, role, deviceId, cancellationToken);
    }

    public async Task<PlayTurnCompanionProjection> AdjustMetricAsync(
        string sessionId,
        PlaySurfaceRole role,
        string metricId,
        int delta,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        PlayTurnCompanionState state = await GetOrCreateStateAsync(sessionId, role, deviceId, cancellationToken);
        if (role == PlaySurfaceRole.Observer)
        {
            return await GetProjectionAsync(sessionId, role, deviceId, cancellationToken);
        }

        PlayTurnCompanionState updated = PlayTurnCompanionProjector.AdjustMetric(state, metricId, delta);
        await SaveStateAsync(sessionId, role, deviceId, updated, cancellationToken);
        return await GetProjectionAsync(sessionId, role, deviceId, cancellationToken);
    }

    public async Task<PlayTurnCompanionProjection> ResolveActionAsync(
        string sessionId,
        PlaySurfaceRole role,
        PlayTurnResolveRequest request,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        PlayTurnCompanionState state = await GetOrCreateStateAsync(sessionId, role, deviceId, cancellationToken);
        if (role == PlaySurfaceRole.Observer)
        {
            return await GetProjectionAsync(sessionId, role, deviceId, cancellationToken);
        }

        PlayTurnCompanionContext context = await BuildContextAsync(sessionId, role, cancellationToken);
        PlayTurnCompanionState updated = PlayTurnCompanionProjector.ResolveAction(context, state, request);
        await SaveStateAsync(sessionId, role, deviceId, updated, cancellationToken);
        return PlayTurnCompanionProjector.Project(context, updated);
    }

    public Task<PlayTurnCompanionQueueStatusResponse> GetQueueStatusAsync(
        string sessionId,
        PlaySurfaceRole role,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
        => BuildQueueStatusResponseAsync(
            sessionId,
            role,
            accepted: true,
            stale: false,
            message: "Queue status refreshed.",
            acceptedEventCount: 0,
            deviceId,
            cancellationToken);

    public async Task<PlayTurnCompanionQueueStatusResponse> ReplayClientQueueAsync(
        string sessionId,
        PlaySurfaceRole role,
        IReadOnlyList<string> queuedEvents,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        if (role == PlaySurfaceRole.Observer)
        {
            return await BuildQueueStatusResponseAsync(
                sessionId,
                role,
                accepted: false,
                stale: false,
                message: "Observer lane must stay read-only; replay belongs to the player or GM shell.",
                acceptedEventCount: 0,
                deviceId,
                cancellationToken);
        }

        IReadOnlyList<string> normalizedEvents = NormalizeReplayEvents(role, queuedEvents);
        if (normalizedEvents.Count == 0)
        {
            return await BuildQueueStatusResponseAsync(
                sessionId,
                role,
                accepted: true,
                stale: false,
                message: "No local replay receipts were queued on this device.",
                acceptedEventCount: 0,
                deviceId,
                cancellationToken);
        }

        try
        {
            PlayResumeState resumeState = await PlayWebApplication.ResolveResumeStateAsync(
                sessionId,
                _eventLogStore,
                _offlineCacheService,
                cancellationToken);
            long appliedThroughSequence = Math.Max(
                resumeState.Ledger.LastKnownSequence,
                resumeState.Checkpoint?.AppliedThroughSequence ?? 0);

            foreach (string queuedEvent in normalizedEvents)
            {
                OfflineQueueEnqueueResult result = await _offlineQueueService.EnqueueAsync(
                    new EngineSessionCursor(resumeState.Session, appliedThroughSequence),
                    queuedEvent,
                    cancellationToken);
                appliedThroughSequence = result.AppliedThroughSequence;
            }

            return await BuildQueueStatusResponseAsync(
                sessionId,
                role,
                accepted: true,
                stale: false,
                message: $"Replayed {normalizedEvents.Count} local receipt(s) into the server queue.",
                acceptedEventCount: normalizedEvents.Count,
                deviceId,
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return await BuildQueueStatusResponseAsync(
                sessionId,
                role,
                accepted: false,
                stale: true,
                message: "Session lineage moved while replaying local receipts. Refresh trust, then replay on the current owner route.",
                acceptedEventCount: 0,
                deviceId,
                cancellationToken);
        }
    }

    public async Task<PlayTurnCompanionQueueStatusResponse> AcknowledgePendingQueueAsync(
        string sessionId,
        PlaySurfaceRole role,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        if (role == PlaySurfaceRole.Observer)
        {
            return await BuildQueueStatusResponseAsync(
                sessionId,
                role,
                accepted: false,
                stale: false,
                message: "Observer lane must stay read-only; queue acknowledgement belongs to the player or GM shell.",
                acceptedEventCount: 0,
                deviceId,
                cancellationToken);
        }

        try
        {
            PlayResumeState resumeState = await PlayWebApplication.ResolveResumeStateAsync(
                sessionId,
                _eventLogStore,
                _offlineCacheService,
                cancellationToken);
            if (resumeState.Ledger.PendingEvents.Count == 0)
            {
                return await BuildQueueStatusResponseAsync(
                    sessionId,
                    role,
                    accepted: true,
                    stale: false,
                    message: "Server replay queue is already empty.",
                    acceptedEventCount: 0,
                    deviceId,
                    cancellationToken);
            }

            long appliedThroughSequence = Math.Max(
                resumeState.Ledger.LastKnownSequence,
                resumeState.Checkpoint?.AppliedThroughSequence ?? 0);
            OfflineQueueSyncResult result = await _offlineQueueService.SyncReplayAsync(
                new PlaySyncRequest(
                    new EngineSessionCursor(resumeState.Session, appliedThroughSequence),
                    resumeState.Ledger.PendingEvents),
                cancellationToken);

            return await BuildQueueStatusResponseAsync(
                sessionId,
                role,
                accepted: true,
                stale: false,
                message: $"Acknowledged {result.AcceptedEventCount} queued server event(s) after reconnect confirmation.",
                acceptedEventCount: result.AcceptedEventCount,
                deviceId,
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return await BuildQueueStatusResponseAsync(
                sessionId,
                role,
                accepted: false,
                stale: true,
                message: "Queue acknowledgement lost lineage alignment. Refresh the current owner route before confirming sync.",
                acceptedEventCount: 0,
                deviceId,
                cancellationToken);
        }
    }

    private async Task<PlayTurnCompanionContext> BuildContextAsync(
        string sessionId,
        PlaySurfaceRole role,
        CancellationToken cancellationToken)
    {
        PlayResumeState resumeState = await PlayWebApplication.ResolveResumeStateAsync(
            sessionId,
            _eventLogStore,
            _offlineCacheService,
            cancellationToken);

        PlaySessionProjection projection = PlayRouteHandlers.BuildProjection(
            resumeState.Session,
            resumeState.Ledger.LastKnownSequence,
            resumeState.Ledger.PendingEvents);

        IReadOnlyList<string> roleCapabilities = role switch
        {
            PlaySurfaceRole.GameMaster => PlayRouteHandlers.ResolveRoleCapabilities(PlayRouteHandlers.ToSnapshot(GmTacticalShellModule.CreateDescriptor())),
            PlaySurfaceRole.Observer => ["play.session.read"],
            _ => PlayRouteHandlers.ResolveRoleCapabilities(PlayRouteHandlers.ToSnapshot(PlayerShellModule.CreateDescriptor()))
        };
        IReadOnlyList<PlayQuickAction> quickActions = PlayRouteHandlers.BuildQuickActions(role, roleCapabilities);
        PlayCachePressureSnapshot cachePressure = await _offlineCacheService.GetCachePressureAsync(cancellationToken);

        return new PlayTurnCompanionContext(
            sessionId,
            role,
            resumeState.Session,
            resumeState.Checkpoint,
            resumeState.RuntimeBundle,
            cachePressure,
            projection.Timeline,
            quickActions,
            PlayRouteHandlers.BuildOwnerRoute(sessionId, role),
            resumeState.Ledger.PendingEvents.Count
        );
    }

    private async Task<PlayTurnCompanionState> GetOrCreateStateAsync(
        string sessionId,
        PlaySurfaceRole role,
        string? deviceId,
        CancellationToken cancellationToken)
    {
        string scopedKey = PlayBrowserStateKeys.TurnCompanion(sessionId, role, deviceId);
        PlayTurnCompanionState? state = await _browserStore.GetAsync<PlayTurnCompanionState>(
            scopedKey,
            cancellationToken);

        if (state is not null)
        {
            return state;
        }

        PlayTurnCompanionState? legacyState = await _browserStore.GetAsync<PlayTurnCompanionState>(
            PlayBrowserStateKeys.TurnCompanion(sessionId),
            cancellationToken);
        if (legacyState is not null)
        {
            await SaveStateAsync(sessionId, role, deviceId, legacyState, cancellationToken);
            return legacyState;
        }

        PlayTurnCompanionState seeded = PlayTurnCompanionProjector.CreateDefaultState(role);
        await SaveStateAsync(sessionId, role, deviceId, seeded, cancellationToken);
        return seeded;
    }

    private Task SaveStateAsync(
        string sessionId,
        PlaySurfaceRole role,
        string? deviceId,
        PlayTurnCompanionState state,
        CancellationToken cancellationToken)
        => _browserStore.SetAsync(PlayBrowserStateKeys.TurnCompanion(sessionId, role, deviceId), state, cancellationToken);

    private async Task<PlayTurnCompanionQueueStatusResponse> BuildQueueStatusResponseAsync(
        string sessionId,
        PlaySurfaceRole role,
        bool accepted,
        bool stale,
        string message,
        int acceptedEventCount,
        string? deviceId,
        CancellationToken cancellationToken)
    {
        PlayTurnCompanionContext context = await BuildContextAsync(sessionId, role, cancellationToken);
        PlayTurnCompanionProjection projection = PlayTurnCompanionProjector.Project(
            context,
            await GetOrCreateStateAsync(sessionId, role, deviceId, cancellationToken));
        return new PlayTurnCompanionQueueStatusResponse(
            accepted,
            stale,
            message,
            acceptedEventCount,
            context.PendingQueueCount,
            projection.CurrentSceneSummary,
            projection.Trust,
            projection.Sync);
    }

    private static IReadOnlyList<string> NormalizeReplayEvents(
        PlaySurfaceRole role,
        IReadOnlyList<string> queuedEvents)
    {
        if (queuedEvents.Count == 0)
        {
            return Array.Empty<string>();
        }

        HashSet<string> allowedQuickActions = GetAllowedQuickActionIds(role);
        List<string> normalized = new(queuedEvents.Count);
        foreach (string queuedEvent in queuedEvents)
        {
            if (string.IsNullOrWhiteSpace(queuedEvent))
            {
                continue;
            }

            string candidate = queuedEvent.Trim();
            if (candidate.Length > 160 || !candidate.All(IsReplayEventCharacterAllowed))
            {
                throw new ArgumentException($"Replay event '{candidate}' is outside the bounded event grammar.", nameof(queuedEvents));
            }

            if (candidate.StartsWith("quick-action:", StringComparison.Ordinal))
            {
                string actionId = candidate["quick-action:".Length..];
                if (!allowedQuickActions.Contains(actionId))
                {
                    throw new ArgumentException($"Quick action '{actionId}' is not permitted for role '{role}'.", nameof(queuedEvents));
                }

                normalized.Add(candidate);
                continue;
            }

            if (candidate.StartsWith("turn:", StringComparison.Ordinal)
                && IsTurnReplayEventPrefixAllowed(candidate))
            {
                normalized.Add(candidate);
                continue;
            }

            throw new ArgumentException($"Replay event '{candidate}' is not permitted for the mobile turn companion queue.", nameof(queuedEvents));
        }

        return normalized;
    }

    private static bool IsReplayEventCharacterAllowed(char value)
        => char.IsAsciiLetterOrDigit(value)
           || value is ':' or '-' or '_' or '+';

    private static bool IsTurnReplayEventPrefixAllowed(string candidate)
        => candidate.StartsWith("turn:metric:", StringComparison.Ordinal)
           || candidate.StartsWith("turn:inventory:", StringComparison.Ordinal)
           || candidate.StartsWith("turn:modifier:", StringComparison.Ordinal)
           || candidate.StartsWith("turn:action:", StringComparison.Ordinal)
           || candidate.StartsWith("turn:anchor:", StringComparison.Ordinal)
           || candidate.StartsWith("turn:resolve:", StringComparison.Ordinal);

    private static HashSet<string> GetAllowedQuickActionIds(PlaySurfaceRole role)
    {
        IReadOnlyList<string> roleCapabilities = role switch
        {
            PlaySurfaceRole.GameMaster => PlayRouteHandlers.ResolveRoleCapabilities(PlayRouteHandlers.ToSnapshot(GmTacticalShellModule.CreateDescriptor())),
            PlaySurfaceRole.Observer => ["play.session.read"],
            _ => PlayRouteHandlers.ResolveRoleCapabilities(PlayRouteHandlers.ToSnapshot(PlayerShellModule.CreateDescriptor()))
        };

        return PlayRouteHandlers.BuildQuickActions(role, roleCapabilities)
            .Select(action => action.ActionId)
            .ToHashSet(StringComparer.Ordinal);
    }
}
