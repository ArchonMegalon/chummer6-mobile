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
    string ResumeSummary,
    string SafeNextAction,
    string RuleEnvironmentSummary,
    string? ReturnTargetCampaignName,
    IReadOnlyList<string> AttentionItems,
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
        var primaryCampaign = restore.RecentCampaigns
            .OrderByDescending(static campaign => campaign.UpdatedAtUtc)
            .FirstOrDefault();
        var primaryDossier = restore.RecentDossiers
            .OrderByDescending(static dossier => dossier.UpdatedAtUtc)
            .FirstOrDefault();
        var primaryEnvironment = primaryCampaign?.RuleEnvironment
            ?? primaryDossier?.RuleEnvironment
            ?? restore.RecentRuleEnvironments.FirstOrDefault();
        var canResume = restore.RecentDossiers.Count > 0
            || restore.RecentCampaigns.Count > 0
            || restore.RecentRuleEnvironments.Count > 0
            || restore.RecentArtifacts.Count > 0
            || restore.Entitlements.Count > 0;
        var ruleEnvironmentSummary = primaryEnvironment is null
            ? "No approved rule environment is attached to this roaming restore packet yet."
            : $"{primaryEnvironment.CompatibilityFingerprint} · {primaryEnvironment.ApprovalState} · {primaryEnvironment.OwnerScope}";
        var resumeSummary = BuildResumeSummary(targetDevice, primaryCampaign, primaryDossier, primaryEnvironment, canResume);
        var safeNextAction = BuildSafeNextAction(targetDevice, primaryCampaign, primaryDossier, conflictSummaries, canResume);
        var attentionItems = BuildAttentionItems(conflictSummaries, localOnlyNotes, primaryEnvironment);

        return new RoamingWorkspaceRestorePlan(
            RestoreId: restore.RestoreId,
            TargetDeviceId: targetDevice.InstallationId,
            DeviceRole: targetDevice.DeviceRole,
            Dossiers: restore.RecentDossiers,
            Campaigns: restore.RecentCampaigns,
            RuleEnvironments: restore.RecentRuleEnvironments,
            Artifacts: restore.RecentArtifacts,
            Entitlements: restore.Entitlements,
            ResumeSummary: resumeSummary,
            SafeNextAction: safeNextAction,
            RuleEnvironmentSummary: ruleEnvironmentSummary,
            ReturnTargetCampaignName: primaryCampaign?.Name,
            AttentionItems: attentionItems,
            ConflictSummaries: conflictSummaries,
            LocalOnlyNotes: localOnlyNotes,
            RequiresConflictReview: conflictSummaries.Length > 0,
            CanResume: canResume);
    }

    private static string BuildResumeSummary(
        ClaimedDeviceRestoreProjection targetDevice,
        CampaignProjection? primaryCampaign,
        RunnerDossierProjection? primaryDossier,
        RuleEnvironmentRef? primaryEnvironment,
        bool canResume)
    {
        if (!canResume)
        {
            return $"No roaming continuity is cached for {targetDevice.DeviceRole} yet.";
        }

        if (primaryCampaign is not null)
        {
            string environment = primaryEnvironment?.CompatibilityFingerprint ?? "no active rule fingerprint";
            return $"Resume {primaryCampaign.Name} on {targetDevice.DeviceRole} with {environment} as the grounded rule posture.";
        }

        if (primaryDossier is not null)
        {
            string environment = primaryEnvironment?.CompatibilityFingerprint ?? "no active rule fingerprint";
            return $"Resume {primaryDossier.DisplayName} on {targetDevice.DeviceRole} and keep {environment} attached before the next campaign handoff.";
        }

        return $"Restore artifacts and entitlements on {targetDevice.DeviceRole} before you reopen campaign work.";
    }

    private static string BuildSafeNextAction(
        ClaimedDeviceRestoreProjection targetDevice,
        CampaignProjection? primaryCampaign,
        RunnerDossierProjection? primaryDossier,
        IReadOnlyList<string> conflictSummaries,
        bool canResume)
    {
        if (conflictSummaries.Count > 0)
        {
            return $"Review restore conflicts on {targetDevice.DeviceRole} before you resume any dossier or campaign lane.";
        }

        if (!canResume)
        {
            return $"Claim or reconnect {targetDevice.DeviceRole} first so the roaming workspace has a continuity target.";
        }

        if (primaryCampaign is not null)
        {
            return $"Open {primaryCampaign.Name} and confirm the current run, scene, and rule posture before continuing play.";
        }

        if (primaryDossier is not null)
        {
            return $"Open {primaryDossier.DisplayName} and confirm its rule environment before you seed or rejoin a campaign.";
        }

        return $"Reconnect the latest artifact and entitlement state on {targetDevice.DeviceRole} before you resume.";
    }

    private static IReadOnlyList<string> BuildAttentionItems(
        IReadOnlyList<string> conflictSummaries,
        IReadOnlyList<string> localOnlyNotes,
        RuleEnvironmentRef? primaryEnvironment)
    {
        List<string> items = [];
        items.AddRange(conflictSummaries);

        if (primaryEnvironment is not null && !string.Equals(primaryEnvironment.ApprovalState, "approved", StringComparison.OrdinalIgnoreCase))
        {
            items.Add($"Rule environment {primaryEnvironment.CompatibilityFingerprint} is not approved yet.");
        }

        if (localOnlyNotes.Count > 0)
        {
            items.Add(localOnlyNotes[0]);
        }

        return items
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
