using Chummer.Campaign.Contracts;

namespace Chummer.Play.Core.Roaming;

public sealed record RoamingWorkspaceRestorePlan(
    string RestoreId,
    string TargetDeviceId,
    string DeviceRole,
    IReadOnlyList<RunnerDossierProjection> Dossiers,
    IReadOnlyList<CampaignProjection> Campaigns,
    IReadOnlyList<RuleEnvironmentRef> RuleEnvironments,
    IReadOnlyList<RestoreArtifactProjection> Artifacts,
    IReadOnlyList<RestoreEntitlementProjection> Entitlements,
    IReadOnlyList<string> ConflictSummaries,
    IReadOnlyList<string> LocalOnlyNotes,
    bool RequiresConflictReview,
    bool CanResume);

public interface IRoamingWorkspaceSyncPlanner
{
    RoamingWorkspaceRestorePlan CreatePlan(WorkspaceRestoreProjection restore, string targetDeviceId);
}

public sealed class RoamingWorkspaceSyncPlanner : IRoamingWorkspaceSyncPlanner
{
    public RoamingWorkspaceRestorePlan CreatePlan(WorkspaceRestoreProjection restore, string targetDeviceId)
    {
        ArgumentNullException.ThrowIfNull(restore);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDeviceId);

        var targetDevice = restore.ClaimedDevices.FirstOrDefault(item => string.Equals(item.InstallationId, targetDeviceId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Unknown claimed device '{targetDeviceId}'.");

        var conflictSummaries = restore.ConflictSummaries
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var localOnlyNotes = restore.LocalOnlyNotes
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var canResume = restore.RecentDossiers.Count > 0
            || restore.RecentCampaigns.Count > 0
            || restore.RecentRuleEnvironments.Count > 0
            || restore.RecentArtifacts.Count > 0
            || restore.Entitlements.Count > 0;

        return new RoamingWorkspaceRestorePlan(
            RestoreId: restore.RestoreId,
            TargetDeviceId: targetDevice.InstallationId,
            DeviceRole: targetDevice.DeviceRole,
            Dossiers: restore.RecentDossiers,
            Campaigns: restore.RecentCampaigns,
            RuleEnvironments: restore.RecentRuleEnvironments,
            Artifacts: restore.RecentArtifacts,
            Entitlements: restore.Entitlements,
            ConflictSummaries: conflictSummaries,
            LocalOnlyNotes: localOnlyNotes,
            RequiresConflictReview: conflictSummaries.Length > 0,
            CanResume: canResume);
    }
}
