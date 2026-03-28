using Chummer.Campaign.Contracts;
using Chummer.Play.Core.PlayApi;
using Chummer.Play.Core.Roaming;

namespace Chummer.Play.Core.Application;

public interface IPlayRoamingRestoreService
{
    RoamingWorkspaceRestorePlan CreatePlan(PlayResumeResponse resume, string? targetDeviceId = null);
}

public sealed class PlayRoamingRestoreService : IPlayRoamingRestoreService
{
    private readonly IRoamingWorkspaceSyncPlanner _planner;

    public PlayRoamingRestoreService(IRoamingWorkspaceSyncPlanner planner)
    {
        _planner = planner;
    }

    public RoamingWorkspaceRestorePlan CreatePlan(PlayResumeResponse resume, string? targetDeviceId = null)
    {
        ArgumentNullException.ThrowIfNull(resume);

        string effectiveTargetDeviceId = string.IsNullOrWhiteSpace(targetDeviceId)
            ? BuildDefaultDeviceId(resume)
            : targetDeviceId.Trim();
        WorkspaceRestoreProjection restore = BuildRestoreProjection(resume, effectiveTargetDeviceId);
        return _planner.CreatePlan(restore, effectiveTargetDeviceId);
    }

    private static WorkspaceRestoreProjection BuildRestoreProjection(PlayResumeResponse resume, string targetDeviceId)
    {
        EngineSessionEnvelope session = resume.Bootstrap.Projection.Cursor.Session;
        DateTimeOffset generatedAtUtc = resume.RuntimeBundle?.CachedAtUtc
            ?? resume.Checkpoint?.CapturedAtUtc
            ?? resume.Bootstrap.Projection.GeneratedAtUtc;
        RuleEnvironmentRef environment = BuildRuleEnvironment(resume, session);
        ContinuitySnapshotRef continuity = new(
            SnapshotId: $"snapshot:{resume.SessionId}",
            CapturedAtUtc: generatedAtUtc,
            Summary: $"Resume {session.SceneId} on the claimed {ResolveDeviceRole(resume.Role)} lane.",
            RestoreState: resume.Checkpoint is null ? "needs_local_checkpoint" : "checkpoint_aligned",
            SessionId: resume.SessionId,
            SceneId: session.SceneId,
            RecapArtifactId: $"artifact:{resume.SessionId}:resume");
        RunnerDossierProjection dossier = new(
            DossierId: $"dossier:{resume.SessionId}",
            RunnerHandle: session.SceneId,
            DisplayName: $"{session.SceneId} return dossier",
            Status: DossierStatuses.Active,
            OwnerUserId: "play-shell",
            CrewId: null,
            CampaignId: $"campaign:{resume.SessionId}",
            CurrentRunId: $"run:{resume.SessionId}",
            CurrentSceneId: session.SceneId,
            RuleEnvironment: environment,
            LatestContinuity: continuity,
            BuildReceiptIds:
            [
                $"build-path:{session.RuntimeFingerprint}"
            ],
            SnapshotIds:
            [
                continuity.SnapshotId
            ],
            Projections:
            [
                new PublicationSafeProjection(
                    ProjectionId: $"projection:{resume.SessionId}:recap",
                    Kind: "recap",
                    Label: "Session recap-safe packet",
                    Summary: $"Grounded recap for {session.SceneId}.",
                    ArtifactId: continuity.RecapArtifactId)
            ],
            CreatedAtUtc: generatedAtUtc,
            UpdatedAtUtc: generatedAtUtc);
        CampaignProjection campaign = new(
            CampaignId: $"campaign:{resume.SessionId}",
            GroupId: "group:mobile-shell",
            Name: $"{session.SceneId} mobile return",
            Status: CampaignStatuses.Active,
            Visibility: "private",
            Summary: $"Claimed-device restore for {resume.SessionId} stays anchored on {session.SceneId}.",
            RuleEnvironment: environment,
            ActiveRunId: $"run:{resume.SessionId}",
            CrewIds: [],
            DossierIds:
            [
                dossier.DossierId
            ],
            RunIds:
            [
                $"run:{resume.SessionId}"
            ],
            LatestContinuity: continuity,
            CreatedAtUtc: generatedAtUtc,
            UpdatedAtUtc: generatedAtUtc);

        List<RestoreArtifactProjection> artifacts =
        [
            new(
                ArtifactId: $"artifact:{resume.SessionId}:resume",
                Label: $"{session.SceneId} resume packet",
                Kind: "resume",
                Summary: $"Resume truth for {resume.SessionId} on the claimed mobile shell.",
                Channel: "preview",
                Version: resume.RuntimeBundle?.BundleTag)
        ];
        if (resume.RuntimeBundle is not null)
        {
            artifacts.Add(
                new RestoreArtifactProjection(
                    ArtifactId: $"artifact:{resume.SessionId}:bundle",
                    Label: $"{resume.RuntimeBundle.BundleTag} runtime bundle",
                    Kind: "runtime-bundle",
                    Summary: $"Validated runtime bundle for {session.RuntimeFingerprint}.",
                    Channel: "preview",
                    Version: resume.RuntimeBundle.BundleTag));
        }

        List<RestoreEntitlementProjection> entitlements =
        [
            new(
                EntitlementId: "entitlement:mobile-shell",
                Label: "Mobile campaign continuity",
                Scope: "device",
                Status: "active",
                Summary: "This claimed device can resume the current campaign continuity packet.")
        ];

        if (resume.RuntimeBundle is null)
        {
            entitlements.Add(
                new RestoreEntitlementProjection(
                    EntitlementId: "entitlement:bundle-review",
                    Label: "Runtime bundle review",
                    Scope: "device",
                    Status: "needs_attention",
                    Summary: "Reconnect once to validate the current runtime bundle before offline restore."));
        }

        List<ClaimedDeviceRestoreProjection> devices =
        [
            new(
                InstallationId: targetDeviceId,
                DeviceRole: ResolveDeviceRole(resume.Role),
                Platform: "mobile-web",
                HeadId: ResolveHeadId(resume.Role),
                Channel: "preview",
                HostLabel: resume.Role.ToString(),
                RestoreSummary: $"Resume {session.SceneId} on the {ResolveDeviceRole(resume.Role)} lane."),
            new(
                InstallationId: $"{targetDeviceId}:travel",
                DeviceRole: "travel_cache",
                Platform: "mobile-web",
                HeadId: "pwa",
                Channel: "preview",
                HostLabel: "Travel cache",
                RestoreSummary: $"Keep a travel-safe cache for {session.SceneId} when the primary lane is offline.")
        ];

        List<string> conflictSummaries = [];
        if (resume.CachePressure.BackpressureActive)
        {
            conflictSummaries.Add("Runtime-bundle cache pressure is active; clear stale travel caches before you trust this restore packet.");
        }

        List<string> localOnlyNotes =
        [
            "Secrets, grant tokens, and runtime caches stay install-local on this device and are never mirrored into the roaming restore packet."
        ];
        if (resume.Checkpoint is null)
        {
            localOnlyNotes.Add("Seed a local continuity checkpoint on this device before you treat it as the default return path.");
        }

        if (resume.RuntimeBundle is null)
        {
            localOnlyNotes.Add("Reconnect once while online so this device can validate and cache the current runtime bundle.");
        }

        return new WorkspaceRestoreProjection(
            RestoreId: $"restore:{resume.SessionId}",
            UserId: "play-shell",
            RecentDossiers:
            [
                dossier
            ],
            RecentCampaigns:
            [
                campaign
            ],
            RecentRuleEnvironments:
            [
                environment
            ],
            RecentArtifacts: artifacts,
            Entitlements: entitlements,
            ClaimedDevices: devices,
            ConflictSummaries: conflictSummaries,
            LocalOnlyNotes: localOnlyNotes,
            GeneratedAtUtc: generatedAtUtc);
    }

    private static RuleEnvironmentRef BuildRuleEnvironment(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        return new RuleEnvironmentRef(
            EnvironmentId: $"environment:{session.RuntimeFingerprint}",
            OwnerScope: "campaign",
            CompatibilityFingerprint: session.RuntimeFingerprint,
            ApprovalState: resume.RuntimeBundle is null ? "candidate" : "approved",
            SourcePacks:
            [
                $"{session.RuntimeFingerprint}@current"
            ],
            HouseRulePacks: [],
            OptionToggles:
            [
                "campaign_continuity",
                "mobile_restore"
            ]);
    }

    private static string BuildDefaultDeviceId(PlayResumeResponse resume)
        => $"install-{ResolveDeviceRole(resume.Role)}";

    private static string ResolveDeviceRole(PlaySurfaceRole role)
        => role switch
        {
            PlaySurfaceRole.GameMaster => "workstation",
            PlaySurfaceRole.Observer => "observer_screen",
            _ => "play_tablet"
        };

    private static string ResolveHeadId(PlaySurfaceRole role)
        => role switch
        {
            PlaySurfaceRole.GameMaster => "gm-shell",
            PlaySurfaceRole.Observer => "observer-shell",
            _ => "player-shell"
        };
}
