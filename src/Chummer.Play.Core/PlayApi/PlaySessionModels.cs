using Chummer.Play.Core.Application;

namespace Chummer.Play.Core.PlayApi;

public static class PlayApiRoutes
{
    public const string Bootstrap = "/api/play/bootstrap";
    public const string Projection = "/api/play/projection/{sessionId}";
    public const string Reconnect = "/api/play/reconnect";
    public const string Sync = "/api/play/sync";
    public const string QuickAction = "/api/play/quick-action";
    public const string Resume = "/api/play/resume/{sessionId}";
    public const string CachePressure = "/api/play/cache-pressure/{sessionId}";
}

public sealed record EngineSessionEnvelope(
    string SessionId,
    string SceneId,
    string SceneRevision,
    string RuntimeFingerprint
);

public sealed record EngineSessionCursor(
    EngineSessionEnvelope Session,
    long AppliedThroughSequence
);

public sealed record PlayBootstrapRequest(
    EngineSessionEnvelope Session,
    PlaySurfaceRole Role
);

public sealed record PlayBootstrapResponse(
    string Project,
    PlaySessionProjection Projection,
    PlayShellSnapshot ActiveShell,
    IReadOnlyList<PlayShellSnapshot> AvailableShells,
    BrowserSessionShellProbe ShellProbe,
    IReadOnlyList<string> RoleCapabilities,
    IReadOnlyList<PlayTacticalSpiderCard> TacticalSpiderCards,
    IReadOnlyList<PlayCoachHint> CoachHints,
    IReadOnlyList<PlayQuickAction> QuickActions
);

public sealed record PlayShellSnapshot(
    PlaySurfaceRole Role,
    string ShellName,
    string Summary,
    IReadOnlyList<string> RequiredCapabilities
);

public sealed record PlayTacticalSpiderCard(
    string CardId,
    string Title,
    string Summary,
    string RequiredCapability
);

public sealed record PlayCoachHint(
    string HintId,
    string Message
);

public sealed record PlayQuickAction(
    string ActionId,
    string Label,
    string RequiredCapability,
    bool StaleProtected
);

public sealed record PlaySessionProjection(
    EngineSessionCursor Cursor,
    IReadOnlyList<string> Timeline,
    DateTimeOffset GeneratedAtUtc
);

public sealed record PlayReconnectRequest(
    EngineSessionCursor Cursor
);

public sealed record PlayReconnectResponse(
    PlaySessionProjection Projection,
    Sync.SyncCheckpoint ResumeCheckpoint,
    Offline.OfflineLedgerEnvelope Ledger
);

public sealed record PlaySyncRequest(
    EngineSessionCursor Cursor,
    IReadOnlyList<string> PendingEvents
);

public sealed record PlaySyncResponse(
    bool Accepted,
    bool Stale,
    PlaySessionProjection Projection,
    Sync.SyncCheckpoint Checkpoint,
    int AcceptedEventCount
);

public sealed record PlayQuickActionRequest(
    EngineSessionCursor Cursor,
    PlaySurfaceRole Role,
    string ActionId
);

public sealed record PlayQuickActionResponse(
    bool Accepted,
    bool Stale,
    string Reason,
    PlaySessionProjection Projection,
    Sync.SyncCheckpoint Checkpoint
);

public sealed record PlayRuntimeBundleMetadata(
    string RuntimeFingerprint,
    string SceneRevision,
    string BundleTag,
    DateTimeOffset CachedAtUtc,
    DateTimeOffset LastValidatedAtUtc
);

public sealed record PlayCachePressureSnapshot(
    int RuntimeBundleCount,
    int RuntimeBundleQuota,
    bool BackpressureActive,
    int EvictedEntryCount,
    IReadOnlyList<string> EvictedSessionIds,
    DateTimeOffset MeasuredAtUtc
);

public sealed record PlayResumeResponse(
    string SessionId,
    PlaySurfaceRole Role,
    string DeepLinkOwnerRoute,
    PlayBootstrapResponse Bootstrap,
    Sync.SyncCheckpoint? Checkpoint,
    PlayRuntimeBundleMetadata? RuntimeBundle,
    PlayCachePressureSnapshot CachePressure
);
