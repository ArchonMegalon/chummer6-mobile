namespace Chummer.Campaign.Contracts;

public static class DossierStatuses
{
    public const string Active = "active";
}

public static class CampaignStatuses
{
    public const string Active = "active";
}

public static class RunStatuses
{
    public const string Planned = "planned";
    public const string Active = "active";
}

public sealed record RuleEnvironmentRef(
    string EnvironmentId,
    string OwnerScope,
    string CompatibilityFingerprint,
    string ApprovalState,
    IReadOnlyList<string> SourcePacks,
    IReadOnlyList<string> HouseRulePacks,
    IReadOnlyList<string> OptionToggles);

public sealed record ContinuitySnapshotRef(
    string SnapshotId,
    DateTimeOffset CapturedAtUtc,
    string Summary,
    string RestoreState,
    string SessionId,
    string SceneId,
    string RecapArtifactId);

public sealed record PublicationSafeProjection(
    string ProjectionId,
    string Kind,
    string Label,
    string Summary,
    string ArtifactId);

public sealed record RunnerDossierProjection(
    string DossierId,
    string RunnerHandle,
    string DisplayName,
    string Status,
    string OwnerUserId,
    string? CrewId,
    string CampaignId,
    string? CurrentRunId,
    string? CurrentSceneId,
    RuleEnvironmentRef RuleEnvironment,
    ContinuitySnapshotRef LatestContinuity,
    IReadOnlyList<string> BuildReceiptIds,
    IReadOnlyList<string> SnapshotIds,
    IReadOnlyList<PublicationSafeProjection> Projections,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CampaignProjection(
    string CampaignId,
    string GroupId,
    string Name,
    string Status,
    string Visibility,
    string Summary,
    RuleEnvironmentRef RuleEnvironment,
    string? ActiveRunId,
    IReadOnlyList<string> CrewIds,
    IReadOnlyList<string> DossierIds,
    IReadOnlyList<string> RunIds,
    ContinuitySnapshotRef LatestContinuity,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record RestoreArtifactProjection(
    string ArtifactId,
    string Label,
    string Kind,
    string Summary,
    string Channel,
    string? Version);

public sealed record RestoreEntitlementProjection(
    string EntitlementId,
    string Label,
    string Scope,
    string Status,
    string Summary);

public sealed record ClaimedDeviceRestoreProjection(
    string InstallationId,
    string DeviceRole,
    string Platform,
    string HeadId,
    string Channel,
    string HostLabel,
    string RestoreSummary);

public sealed record WorkspaceRestoreProjection(
    string RestoreId,
    string UserId,
    IReadOnlyList<RunnerDossierProjection> RecentDossiers,
    IReadOnlyList<CampaignProjection> RecentCampaigns,
    IReadOnlyList<RuleEnvironmentRef> RecentRuleEnvironments,
    IReadOnlyList<RestoreArtifactProjection> RecentArtifacts,
    IReadOnlyList<RestoreEntitlementProjection> Entitlements,
    IReadOnlyList<ClaimedDeviceRestoreProjection> ClaimedDevices,
    IReadOnlyList<string> ConflictSummaries,
    IReadOnlyList<string> LocalOnlyNotes,
    DateTimeOffset GeneratedAtUtc);

public sealed record WorkspaceSummary(
    string WorkspaceId,
    string CampaignId,
    string CampaignName,
    string Visibility,
    string ReturnSummary,
    string DeviceRoleSummary,
    string SupportClosureSummary,
    string ActiveSceneSummary,
    DateTimeOffset UpdatedAtUtc);

public sealed record CampaignWorkspaceSummary(
    string WorkspaceId,
    string CampaignId,
    string CampaignName,
    string RuleEnvironmentSummary,
    string SessionReadinessSummary,
    string RestoreSummary,
    string PublicationSummary,
    string NextSafeAction,
    DateTimeOffset UpdatedAtUtc);

public sealed record RosterReadinessSummary(
    string Summary,
    int ReadyDossierCount,
    int NeedsAttentionCount,
    int CrewCount,
    int RunCount,
    IReadOnlyList<string> Highlights);

public sealed record RunboardSummary(
    string RunId,
    string Title,
    string Status,
    string ActiveSceneId,
    string ActiveSceneSummary,
    string ObjectiveSummary,
    IReadOnlyList<string> Blockers,
    string ReturnSummary);

public sealed record RecapShelfEntry(
    string EntryId,
    string Kind,
    string Label,
    string Summary,
    string ArtifactId,
    DateTimeOffset UpdatedAtUtc,
    string Audience,
    string OwnershipSummary,
    string PublicationState,
    string TrustBand,
    bool Discoverable,
    string PublicationSummary,
    string CreatorPublicationId,
    string NextSafeAction,
    string ProvenanceSummary,
    string AuditSummary);

public sealed record RuleEnvironmentHealthCue(
    string EnvironmentId,
    string Severity,
    string Title,
    string Summary);

public sealed record ContinuityConflictCue(
    string CueId,
    string Severity,
    string Summary,
    string ResolutionAction);
