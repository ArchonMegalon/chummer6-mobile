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
    string PrefetchReadinessSummary,
    string LocalCacheBoundarySummary,
    IReadOnlyList<string> PrefetchLabels,
    string? ReturnTargetCampaignName,
    string ResumeFollowThrough,
    string ResumeFollowThroughHref,
    string SupportFollowThrough,
    string SupportFollowThroughHref,
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
        var prefetchReadinessSummary = BuildPrefetchReadinessSummary(
            restore,
            targetDevice,
            primaryCampaign,
            primaryDossier,
            primaryEnvironment,
            conflictSummaries,
            canResume);
        var localCacheBoundarySummary = BuildLocalCacheBoundarySummary(localOnlyNotes);
        var prefetchLabels = BuildPrefetchLabels(restore, targetDevice);
        var resumeFollowThrough = BuildResumeFollowThrough(targetDevice, primaryCampaign, primaryDossier, conflictSummaries, canResume);
        var resumeFollowThroughHref = BuildResumeFollowThroughHref(targetDevice, primaryCampaign, primaryDossier);
        var supportFollowThrough = BuildSupportFollowThrough(targetDevice, primaryCampaign, primaryDossier, primaryEnvironment, conflictSummaries, localOnlyNotes);
        var supportFollowThroughHref = BuildSupportFollowThroughHref(targetDevice, primaryCampaign, primaryDossier, primaryEnvironment, conflictSummaries, localOnlyNotes);
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
            PrefetchReadinessSummary: prefetchReadinessSummary,
            LocalCacheBoundarySummary: localCacheBoundarySummary,
            PrefetchLabels: prefetchLabels,
            ReturnTargetCampaignName: primaryCampaign?.Name,
            ResumeFollowThrough: resumeFollowThrough,
            ResumeFollowThroughHref: resumeFollowThroughHref,
            SupportFollowThrough: supportFollowThrough,
            SupportFollowThroughHref: supportFollowThroughHref,
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

    private static string BuildPrefetchReadinessSummary(
        WorkspaceRestoreProjection restore,
        ClaimedDeviceRestoreProjection targetDevice,
        CampaignProjection? primaryCampaign,
        RunnerDossierProjection? primaryDossier,
        RuleEnvironmentRef? primaryEnvironment,
        IReadOnlyList<string> conflictSummaries,
        bool canResume)
    {
        string inventory = DescribePrefetchInventory(restore);
        string targetLabel = primaryCampaign?.Name ?? primaryDossier?.DisplayName ?? targetDevice.DeviceRole;

        if (!canResume)
        {
            return $"Prefetch readiness is empty for {targetDevice.DeviceRole}: claim or reconnect this device before you seed bounded offline campaign state.";
        }

        if (conflictSummaries.Count > 0)
        {
            return $"Prefetch readiness is warning-only for {targetLabel} on {targetDevice.DeviceRole}: {inventory} are staged, but restore conflicts still need review before bounded offline use.";
        }

        if (primaryEnvironment is not null && !string.Equals(primaryEnvironment.ApprovalState, "approved", StringComparison.OrdinalIgnoreCase))
        {
            return $"Prefetch readiness is partial for {targetLabel} on {targetDevice.DeviceRole}: {inventory} are staged, but {primaryEnvironment.CompatibilityFingerprint} is not approved yet.";
        }

        return $"Prefetch readiness is green for {targetLabel} on {targetDevice.DeviceRole}: {inventory} are staged for bounded offline use on this claimed device.";
    }

    private static string BuildLocalCacheBoundarySummary(IReadOnlyList<string> localOnlyNotes)
    {
        string localNote = localOnlyNotes.FirstOrDefault(static item => !string.IsNullOrWhiteSpace(item))
            ?? "install-local caches remain device-bound.";
        return $"Install-local boundary: {localNote}";
    }

    private static IReadOnlyList<string> BuildPrefetchLabels(
        WorkspaceRestoreProjection restore,
        ClaimedDeviceRestoreProjection targetDevice)
    {
        List<string> labels =
        [
            $"Prefetch inventory: {DescribePrefetchInventory(restore)}",
            .. BuildPrefetchScopeLabels(restore),
            $"Target device: {targetDevice.InstallationId} · {targetDevice.DeviceRole} · {targetDevice.Platform} · {targetDevice.Channel}"
        ];

        foreach (ClaimedDeviceRestoreProjection companion in restore.ClaimedDevices.Where(item => !string.Equals(item.InstallationId, targetDevice.InstallationId, StringComparison.OrdinalIgnoreCase)))
        {
            string prefix = companion.DeviceRole.Contains("travel", StringComparison.OrdinalIgnoreCase)
                ? "Travel cache"
                : "Companion device";
            labels.Add($"{prefix}: {companion.InstallationId} · {companion.RestoreSummary}");
        }

        return labels
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> BuildPrefetchScopeLabels(WorkspaceRestoreProjection restore)
    {
        if (restore.RecentDossiers.Count > 0)
        {
            yield return $"Prefetch dossiers: {string.Join(", ", restore.RecentDossiers.Select(static dossier => $"{dossier.DisplayName} ({dossier.DossierId})"))}";
        }

        if (restore.RecentCampaigns.Count > 0)
        {
            yield return $"Prefetch campaigns: {string.Join(", ", restore.RecentCampaigns.Select(static campaign => $"{campaign.Name} ({campaign.CampaignId})"))}";
        }

        if (restore.RecentRuleEnvironments.Count > 0)
        {
            yield return $"Prefetch rules: {string.Join(", ", restore.RecentRuleEnvironments.Select(static environment => $"{environment.CompatibilityFingerprint} [{environment.ApprovalState}]"))}";
        }

        if (restore.RecentArtifacts.Count > 0)
        {
            yield return $"Prefetch artifacts: {string.Join(", ", restore.RecentArtifacts.Select(static artifact => $"{artifact.Label} ({artifact.ArtifactId})"))}";
        }
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

    private static string BuildResumeFollowThrough(
        ClaimedDeviceRestoreProjection targetDevice,
        CampaignProjection? primaryCampaign,
        RunnerDossierProjection? primaryDossier,
        IReadOnlyList<string> conflictSummaries,
        bool canResume)
    {
        if (conflictSummaries.Count > 0)
        {
            return $"Open restore review for {targetDevice.DeviceRole} before you resume the claimed campaign lane.";
        }

        if (!canResume)
        {
            return $"Open the claimed-device work route for {targetDevice.DeviceRole} before you trust this shell as a return target.";
        }

        if (primaryCampaign is not null)
        {
            return $"Resume {primaryCampaign.Name} on {targetDevice.DeviceRole} from the claimed-device route.";
        }

        if (primaryDossier is not null)
        {
            return $"Resume {primaryDossier.DisplayName} on {targetDevice.DeviceRole} from the claimed-device route.";
        }

        return $"Open the claimed-device work route for {targetDevice.DeviceRole} and reconnect the latest artifacts first.";
    }

    private static string BuildResumeFollowThroughHref(
        ClaimedDeviceRestoreProjection targetDevice,
        CampaignProjection? primaryCampaign,
        RunnerDossierProjection? primaryDossier)
    {
        string? sessionId = primaryCampaign?.LatestContinuity?.SessionId
            ?? primaryDossier?.LatestContinuity?.SessionId;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return $"/play?deviceId={Uri.EscapeDataString(targetDevice.InstallationId)}";
        }

        return $"/play/{Uri.EscapeDataString(sessionId)}?deviceId={Uri.EscapeDataString(targetDevice.InstallationId)}&role={Uri.EscapeDataString(ResolvePlayRole(targetDevice.DeviceRole))}";
    }

    private static string BuildSupportFollowThrough(
        ClaimedDeviceRestoreProjection targetDevice,
        CampaignProjection? primaryCampaign,
        RunnerDossierProjection? primaryDossier,
        RuleEnvironmentRef? primaryEnvironment,
        IReadOnlyList<string> conflictSummaries,
        IReadOnlyList<string> localOnlyNotes)
    {
        string targetLabel = primaryCampaign?.Name ?? primaryDossier?.DisplayName ?? targetDevice.DeviceRole;
        if (conflictSummaries.Count > 0)
        {
            return $"Prepare restore support for {targetLabel} on {targetDevice.DeviceRole} and include the active conflict summary.";
        }

        string fingerprint = primaryEnvironment?.CompatibilityFingerprint ?? "no active rule fingerprint";
        string localNote = localOnlyNotes.FirstOrDefault(static item => !string.IsNullOrWhiteSpace(item))
            ?? "install-local guardrails still apply on the target device.";
        return $"Prepare restore support for {targetLabel} on {targetDevice.DeviceRole} with {fingerprint} and note that {localNote}";
    }

    private static string BuildSupportFollowThroughHref(
        ClaimedDeviceRestoreProjection targetDevice,
        CampaignProjection? primaryCampaign,
        RunnerDossierProjection? primaryDossier,
        RuleEnvironmentRef? primaryEnvironment,
        IReadOnlyList<string> conflictSummaries,
        IReadOnlyList<string> localOnlyNotes)
    {
        string targetLabel = primaryCampaign?.Name ?? primaryDossier?.DisplayName ?? targetDevice.DeviceRole;
        string? sessionId = primaryCampaign?.LatestContinuity?.SessionId
            ?? primaryDossier?.LatestContinuity?.SessionId;
        string? sceneId = primaryCampaign?.LatestContinuity?.SceneId
            ?? primaryDossier?.LatestContinuity?.SceneId;
        string fingerprint = primaryEnvironment?.CompatibilityFingerprint ?? "unknown";
        string firstConflict = conflictSummaries.FirstOrDefault(static item => !string.IsNullOrWhiteSpace(item)) ?? "none";
        string firstLocalNote = localOnlyNotes.FirstOrDefault(static item => !string.IsNullOrWhiteSpace(item)) ?? "none";
        string title = conflictSummaries.Count > 0
            ? $"Restore follow-through needs conflict review for {targetLabel}"
            : $"Restore follow-through needs support review for {targetLabel}";
        string summary = conflictSummaries.Count > 0
            ? $"Claimed-device restore for {targetLabel} on {targetDevice.DeviceRole} is blocked by restore conflicts."
            : $"Claimed-device restore for {targetLabel} on {targetDevice.DeviceRole} is ready, but support should keep the grounded context nearby.";
        string detail = string.Join(
            "\n",
            new[]
            {
                $"Target device: {targetDevice.InstallationId}",
                $"Device role: {targetDevice.DeviceRole}",
                $"Campaign: {primaryCampaign?.CampaignId ?? "none"}",
                $"Dossier: {primaryDossier?.DossierId ?? "none"}",
                $"Session: {sessionId ?? "none"}",
                $"Scene: {sceneId ?? "none"}",
                $"Rule fingerprint: {fingerprint}",
                $"Conflict summary: {firstConflict}",
                $"Install-local note: {firstLocalNote}"
            });
        return $"/contact?kind=install_help&title={Uri.EscapeDataString(title)}&summary={Uri.EscapeDataString(summary)}&detail={Uri.EscapeDataString(detail)}&campaignId={Uri.EscapeDataString(primaryCampaign?.CampaignId ?? string.Empty)}&dossierId={Uri.EscapeDataString(primaryDossier?.DossierId ?? string.Empty)}&sessionId={Uri.EscapeDataString(sessionId ?? string.Empty)}&sceneId={Uri.EscapeDataString(sceneId ?? string.Empty)}&runtime={Uri.EscapeDataString(fingerprint)}&deviceId={Uri.EscapeDataString(targetDevice.InstallationId)}";
    }

    private static string ResolvePlayRole(string deviceRole)
    {
        if (deviceRole.Contains("observer", StringComparison.OrdinalIgnoreCase))
        {
            return "Observer";
        }

        if (deviceRole.Contains("gm", StringComparison.OrdinalIgnoreCase))
        {
            return "GameMaster";
        }

        return "Player";
    }

    private static string DescribePrefetchInventory(WorkspaceRestoreProjection restore)
        => $"{restore.RecentDossiers.Count} dossier(s), {restore.RecentCampaigns.Count} campaign(s), {restore.RecentRuleEnvironments.Count} rule environment(s), and {restore.RecentArtifacts.Count} artifact(s)";
}
