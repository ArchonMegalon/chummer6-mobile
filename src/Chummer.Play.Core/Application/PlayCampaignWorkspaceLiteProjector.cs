using System;
using System.Collections.Generic;
using System.Linq;
using Chummer.Campaign.Contracts;
using Chummer.Play.Core.PlayApi;

namespace Chummer.Play.Core.Application;

public sealed record PlayCampaignWorkspaceLiteProjection(
    string SessionId,
    PlaySurfaceRole Role,
    string Summary,
    string CurrentSceneSummary,
    string ChangePacketSummary,
    string ServerPlaneSummary,
    string RunboardSummary,
    string RosterSummary,
    string DecisionNotice,
    string DecisionNoticeHref,
    string RecapSummary,
    string RecapAudienceSummary,
    string RecapOwnershipSummary,
    string RecapPublicationSummary,
    string RecapProvenanceSummary,
    string RecapAuditSummary,
    string RecapLineageSummary,
    string RecapNextAction,
    string RecapPublicationHref,
    string ReplaySummary,
    string ReplayAudienceSummary,
    string ReplayOwnershipSummary,
    string ReplayPublicationSummary,
    string ReplayProvenanceSummary,
    string ReplayAuditSummary,
    string ReplayLineageSummary,
    string ReplayNextAction,
    string ReplayPublicationHref,
    string SelectedArtifactView,
    IReadOnlyList<PlayArtifactShelfViewLink> ArtifactShelfViews,
    string CampaignMemorySummary,
    string CampaignMemoryReturnSummary,
    string ContinuityRailSummary,
    IReadOnlyList<string> ContinuityRailLabels,
    string GmOperationsSummary,
    IReadOnlyList<string> GmOperationsLabels,
    string OfflineTruthSummary,
    IReadOnlyList<string> OfflineTruthLabels,
    string RolePosture,
    string RulePosture,
    string LegalRunnerSummary,
    string UnderstandableReturnSummary,
    string CampaignReadySummary,
    string SafeNextAction,
    string ContinuityPosture,
    string CachePosture,
    string TravelPosture,
    string OfflinePrefetchSummary,
    string UpdatePosture,
    string SupportPosture,
    string SupportStatus,
    string KnownIssueSummary,
    string FixAvailabilitySummary,
    string CurrentCautionSummary,
    string UpdateFollowThrough,
    string UpdateFollowThroughHref,
    string SupportFollowThrough,
    string SupportFollowThroughHref,
    string RoleFollowThrough,
    string RoleFollowThroughHref,
    string RejoinCommand,
    string RejoinCommandHref,
    string ContinueCommand,
    string ContinueCommandHref,
    string SupportCommand,
    string SupportCommandHref,
    string DisconnectRecoveryCopy,
    string RoleChangeRecoveryCopy,
    string ObserverTransitionRecoveryCopy,
    string LongRunningDecisionReceiptSummary,
    IReadOnlyList<string> LongRunningDecisionReceipts,
    IReadOnlyList<string> LowNoiseGuidance,
    IReadOnlyList<string> AttentionItems,
    IReadOnlyList<string> ChangePacketLabels,
    IReadOnlyList<string> QuickActionLabels,
    IReadOnlyList<string> FollowThroughLabels,
    IReadOnlyList<string> CoachHints);

public sealed record PlayArtifactShelfViewLink(
    string ViewId,
    string Label,
    string Summary,
    string Href,
    bool IsSelected);

public static class PlayCampaignWorkspaceLiteProjector
{
    public static PlayCampaignWorkspaceLiteProjection Create(PlayResumeResponse resume)
    {
        ArgumentNullException.ThrowIfNull(resume);

        EngineSessionEnvelope session = resume.Bootstrap.Projection.Cursor.Session;
        PlayCampaignWorkspaceServerPlane serverPlane = PlayCampaignWorkspaceServerPlaneProjector.Create(resume);
        string roleLabel = resume.Role switch
        {
            PlaySurfaceRole.GameMaster => "GM runboard",
            PlaySurfaceRole.Observer => "observer lane",
            _ => "player lane"
        };
        string latestTimeline = resume.Bootstrap.Projection.Timeline.LastOrDefault()
            ?? "No timeline events are cached yet.";
        string continuityPosture = resume.Checkpoint is null
            ? "No local checkpoint is cached yet, so this device still needs an owned continuity anchor."
            : $"Checkpoint {resume.Checkpoint.AppliedThroughSequence} stays aligned to {resume.Checkpoint.SceneRevision} for {roleLabel}.";
        string cachePosture = resume.CachePressure.BackpressureActive
            ? $"Cache pressure is active: {resume.CachePressure.RuntimeBundleCount}/{resume.CachePressure.RuntimeBundleQuota} runtime bundles are pinned and eviction already touched {resume.CachePressure.EvictedEntryCount} session(s)."
            : $"Cache pressure is calm: {resume.CachePressure.RuntimeBundleCount}/{resume.CachePressure.RuntimeBundleQuota} runtime bundles are pinned for this shell.";
        string travelPosture = BuildTravelPosture(resume, session, serverPlane);
        string offlinePrefetchSummary = BuildOfflinePrefetchSummary(resume, serverPlane, roleLabel);
        string runtimeBundleSummary = resume.RuntimeBundle is null
            ? "No runtime bundle is cached locally yet."
            : $"Bundle {resume.RuntimeBundle.BundleTag} was validated at {resume.RuntimeBundle.LastValidatedAtUtc:yyyy-MM-dd HH:mm} UTC.";
        string changePacketSummary = BuildChangePacketSummary(resume, session, latestTimeline);
        string serverPlaneSummary = $"{serverPlane.Campaign.SessionReadinessSummary} {serverPlane.Campaign.RestoreSummary}";
        string runboardSummary = $"{serverPlane.Runboard.Title}: {serverPlane.Runboard.ObjectiveSummary}";
        string rosterSummary = serverPlane.Roster.Summary;
        PlayDecisionNotice? decisionNotice = serverPlane.DecisionNotices.FirstOrDefault();
        string decisionNoticeSummary = decisionNotice is null
            ? "No campaign decision notices are active for this shell."
            : $"{decisionNotice.Summary} {decisionNotice.ActionLabel}.";
        string decisionNoticeHref = decisionNotice?.ActionHref ?? "/";
        RecapShelfEntry? recapEntry = FindLeadRecapEntry(serverPlane);
        RecapShelfEntry? replayEntry = FindLeadReplayEntry(serverPlane);
        string recapSummary = recapEntry is { } boundedRecapEntry
            ? $"{boundedRecapEntry.Label}: {boundedRecapEntry.Summary}"
            : "No recap-safe packet is available yet.";
        string recapAudienceSummary = recapEntry is null
            ? "Artifact audience: no recap-safe packet is attached yet."
            : $"Artifact audience: {HumanizeAudience(recapEntry.Audience)}.";
        string recapOwnershipSummary = recapEntry?.OwnershipSummary
            ?? "Artifact ownership: no shared recap-safe packet is attached yet.";
        string recapPublicationSummary = recapEntry is null
            ? "Artifact publication: no creator-shelf posture is attached yet."
            : $"Artifact publication: {HumanizeState(recapEntry.PublicationState, "Ready")}. Trust ranking: {HumanizeState(recapEntry.TrustBand, "Draft")}. Discoverable now: {(recapEntry.Discoverable ? "Eligible now" : "Still bounded")}. {recapEntry.PublicationSummary}";
        string recapProvenanceSummary = recapEntry?.ProvenanceSummary
            ?? "Artifact provenance: no recap-safe provenance summary is attached yet.";
        string recapAuditSummary = recapEntry?.AuditSummary
            ?? "Artifact audit: no recap-safe audit summary is attached yet.";
        string recapLineageSummary = recapEntry is null
            ? "Artifact lineage: no creator-publication lineage is attached yet."
            : BuildArtifactLineageSummary(recapEntry);
        string recapNextAction = recapEntry is null
            ? $"Artifact next: {serverPlane.NextSafeAction.Summary}"
            : $"Artifact next: {recapEntry.NextSafeAction ?? serverPlane.NextSafeAction.Summary}";
        string recapPublicationHref = string.IsNullOrWhiteSpace(recapEntry?.CreatorPublicationId)
            ? "/account/work"
            : $"/account/work/publications/{Uri.EscapeDataString(recapEntry.CreatorPublicationId!)}";
        string replaySummary = replayEntry is { } boundedReplayEntry
            ? $"{boundedReplayEntry.Label}: {boundedReplayEntry.Summary}"
            : "No replay-safe package is available yet.";
        string replayAudienceSummary = replayEntry is null
            ? "Artifact audience: no replay-safe package is attached yet."
            : $"Artifact audience: {HumanizeAudience(replayEntry.Audience)}.";
        string replayOwnershipSummary = replayEntry?.OwnershipSummary
            ?? "Artifact ownership: no shared replay-safe package is attached yet.";
        string replayPublicationSummary = replayEntry is null
            ? "Artifact publication: no replay-safe creator-shelf posture is attached yet."
            : $"Artifact publication: {HumanizeState(replayEntry.PublicationState, "Ready")}. Trust ranking: {HumanizeState(replayEntry.TrustBand, "Draft")}. Discoverable now: {(replayEntry.Discoverable ? "Eligible now" : "Still bounded")}. {replayEntry.PublicationSummary}";
        string replayProvenanceSummary = replayEntry?.ProvenanceSummary
            ?? "Artifact provenance: no replay-safe provenance summary is attached yet.";
        string replayAuditSummary = replayEntry?.AuditSummary
            ?? "Artifact audit: no replay-safe audit summary is attached yet.";
        string replayLineageSummary = replayEntry is null
            ? "Artifact lineage: no replay-safe creator-publication lineage is attached yet."
            : BuildArtifactLineageSummary(replayEntry);
        string replayNextAction = replayEntry is null
            ? $"Artifact next: {serverPlane.NextSafeAction.Summary}"
            : $"Artifact next: {replayEntry.NextSafeAction ?? serverPlane.NextSafeAction.Summary}";
        string replayPublicationHref = string.IsNullOrWhiteSpace(replayEntry?.CreatorPublicationId)
            ? "/account/work"
            : $"/account/work/publications/{Uri.EscapeDataString(replayEntry.CreatorPublicationId!)}";
        string selectedArtifactView = SelectArtifactShelfView(resume.Role, recapEntry);
        PlayArtifactShelfViewLink[] artifactShelfViews = BuildArtifactShelfViews(recapEntry, selectedArtifactView);
        string campaignMemorySummary = BuildCampaignMemorySummary(resume, serverPlane, roleLabel, latestTimeline);
        string campaignMemoryReturnSummary = BuildCampaignMemoryReturnSummary(resume, serverPlane, roleLabel);
        string continuityRailSummary = BuildContinuityRailSummary(resume, session, serverPlane, roleLabel, latestTimeline);
        string[] continuityRailLabels = BuildContinuityRailLabels(resume, session, serverPlane, roleLabel, latestTimeline);
        string gmOperationsSummary = BuildGmOperationsSummary(resume, session, serverPlane, roleLabel);
        string[] gmOperationsLabels = BuildGmOperationsLabels(resume, session, serverPlane, roleLabel);
        string offlineTruthSummary = BuildOfflineTruthSummary(resume, session);
        string[] offlineTruthLabels = BuildOfflineTruthLabels(resume, session, roleLabel);
        string rolePosture = BuildRolePosture(resume, session);
        string legalRunnerSummary = BuildLegalRunnerSummary(resume, session);
        string understandableReturnSummary = BuildUnderstandableReturnSummary(serverPlane, continuityPosture);
        string campaignReadySummary = BuildCampaignReadySummary(serverPlane);
        string safeNextAction = serverPlane.NextSafeAction.Summary;
        string updatePosture = BuildUpdatePosture(resume, session);
        string supportPosture = BuildSupportPosture(resume, serverPlane);
        string supportStatus = BuildSupportStatus(serverPlane);
        string knownIssueSummary = BuildKnownIssueSummary(resume, session, serverPlane);
        string fixAvailabilitySummary = BuildFixAvailabilitySummary(resume, session, serverPlane);
        string currentCautionSummary = BuildCurrentCautionSummary(resume, session);
        string updateFollowThrough = BuildUpdateFollowThrough(resume, session);
        string updateFollowThroughHref = BuildUpdateFollowThroughHref(resume, session);
        string supportFollowThrough = BuildSupportFollowThrough(resume, session);
        string supportFollowThroughHref = BuildSupportFollowThroughHref(resume, session);
        string roleFollowThrough = BuildRoleFollowThrough(resume, session);
        string roleFollowThroughHref = BuildRoleFollowThroughHref(resume, session);
        string rejoinCommand = $"Rejoin {session.SceneId} on the {roleLabel}.";
        string rejoinCommandHref = resume.DeepLinkOwnerRoute;
        string continueCommand = safeNextAction;
        string continueCommandHref = decisionNoticeHref;
        string supportCommand = supportFollowThrough;
        string supportCommandHref = supportFollowThroughHref;
        string disconnectRecoveryCopy = BuildDisconnectRecoveryCopy(resume, session, roleLabel);
        string roleChangeRecoveryCopy = BuildRoleChangeRecoveryCopy(resume, session, roleLabel);
        string observerTransitionRecoveryCopy = BuildObserverTransitionRecoveryCopy(resume, session);
        string[] longRunningDecisionReceipts = BuildLongRunningDecisionReceipts(
            resume,
            session,
            roleLabel,
            decisionNoticeHref,
            supportFollowThroughHref);
        string longRunningDecisionReceiptSummary =
            $"Decision receipts are active for rejoin, quick actions, and resume on {session.SceneId}. Canonical support escalation: {supportFollowThroughHref}.";
        string[] lowNoiseGuidance =
        [
            $"Rejoin route: {resume.DeepLinkOwnerRoute}",
            $"Continue route: {decisionNoticeHref}",
            $"Support route: {supportFollowThroughHref}"
        ];
        string[] changePacketLabels = BuildChangePacketLabels(resume, session, latestTimeline, serverPlane);
        string[] followThroughLabels = BuildFollowThroughLabels(
            resume,
            session,
            replayEntry,
            recapPublicationSummary,
            recapLineageSummary,
            recapNextAction,
            replayPublicationSummary,
            replayLineageSummary,
            replayNextAction,
            currentCautionSummary,
            updateFollowThrough,
            supportFollowThrough,
            roleFollowThrough);

        List<string> attentionItems = [];
        if (resume.RuntimeBundle is null)
        {
            attentionItems.Add("Download the current runtime bundle before you trust offline continuation.");
        }

        if (resume.CachePressure.BackpressureActive)
        {
            attentionItems.Add("Cache pressure is active, so unpin stale sessions before you seed more travel or observer state.");
        }

        foreach (RuleEnvironmentHealthCue cue in serverPlane.RuleHealth.Where(static cue => !string.Equals(cue.Severity, "info", StringComparison.OrdinalIgnoreCase)))
        {
            attentionItems.Add(cue.Summary);
        }

        foreach (ContinuityConflictCue cue in serverPlane.ContinuityConflicts)
        {
            attentionItems.Add($"{cue.Summary} {cue.ResolutionAction}");
        }

        if (resume.Bootstrap.QuickActions.Count == 0)
        {
            attentionItems.Add("No quick actions are available for this role yet, so the table shell is still in a review-only posture.");
        }

        if (resume.Role == PlaySurfaceRole.Observer)
        {
            attentionItems.Add("Observer continuity should stay read-mostly until the owner lane confirms the latest scene revision.");
        }

        return new PlayCampaignWorkspaceLiteProjection(
            SessionId: resume.SessionId,
            Role: resume.Role,
            Summary: $"Resume {resume.SessionId} on the {roleLabel}. Scene {session.SceneId} is pinned at {session.SceneRevision}, and the latest table signal is '{latestTimeline}'.",
            CurrentSceneSummary: $"{session.SceneId} · revision {session.SceneRevision} · sequence {resume.Bootstrap.Projection.Cursor.AppliedThroughSequence}",
            ChangePacketSummary: changePacketSummary,
            ServerPlaneSummary: serverPlaneSummary,
            RunboardSummary: runboardSummary,
            RosterSummary: rosterSummary,
            DecisionNotice: decisionNoticeSummary,
            DecisionNoticeHref: decisionNoticeHref,
            RecapSummary: recapSummary,
            RecapAudienceSummary: recapAudienceSummary,
            RecapOwnershipSummary: recapOwnershipSummary,
            RecapPublicationSummary: recapPublicationSummary,
            RecapProvenanceSummary: recapProvenanceSummary,
            RecapAuditSummary: recapAuditSummary,
            RecapLineageSummary: recapLineageSummary,
            RecapNextAction: recapNextAction,
            RecapPublicationHref: recapPublicationHref,
            ReplaySummary: replaySummary,
            ReplayAudienceSummary: replayAudienceSummary,
            ReplayOwnershipSummary: replayOwnershipSummary,
            ReplayPublicationSummary: replayPublicationSummary,
            ReplayProvenanceSummary: replayProvenanceSummary,
            ReplayAuditSummary: replayAuditSummary,
            ReplayLineageSummary: replayLineageSummary,
            ReplayNextAction: replayNextAction,
            ReplayPublicationHref: replayPublicationHref,
            SelectedArtifactView: selectedArtifactView,
            ArtifactShelfViews: artifactShelfViews,
            CampaignMemorySummary: campaignMemorySummary,
            CampaignMemoryReturnSummary: campaignMemoryReturnSummary,
            ContinuityRailSummary: continuityRailSummary,
            ContinuityRailLabels: continuityRailLabels,
            GmOperationsSummary: gmOperationsSummary,
            GmOperationsLabels: gmOperationsLabels,
            OfflineTruthSummary: offlineTruthSummary,
            OfflineTruthLabels: offlineTruthLabels,
            RolePosture: rolePosture,
            RulePosture: $"{session.RuntimeFingerprint}. {runtimeBundleSummary}",
            LegalRunnerSummary: legalRunnerSummary,
            UnderstandableReturnSummary: understandableReturnSummary,
            CampaignReadySummary: campaignReadySummary,
            SafeNextAction: safeNextAction,
            ContinuityPosture: continuityPosture,
            CachePosture: cachePosture,
            TravelPosture: travelPosture,
            OfflinePrefetchSummary: offlinePrefetchSummary,
            UpdatePosture: updatePosture,
            SupportPosture: supportPosture,
            SupportStatus: supportStatus,
            KnownIssueSummary: knownIssueSummary,
            FixAvailabilitySummary: fixAvailabilitySummary,
            CurrentCautionSummary: currentCautionSummary,
            UpdateFollowThrough: updateFollowThrough,
            UpdateFollowThroughHref: updateFollowThroughHref,
            SupportFollowThrough: supportFollowThrough,
            SupportFollowThroughHref: supportFollowThroughHref,
            RoleFollowThrough: roleFollowThrough,
            RoleFollowThroughHref: roleFollowThroughHref,
            RejoinCommand: rejoinCommand,
            RejoinCommandHref: rejoinCommandHref,
            ContinueCommand: continueCommand,
            ContinueCommandHref: continueCommandHref,
            SupportCommand: supportCommand,
            SupportCommandHref: supportCommandHref,
            DisconnectRecoveryCopy: disconnectRecoveryCopy,
            RoleChangeRecoveryCopy: roleChangeRecoveryCopy,
            ObserverTransitionRecoveryCopy: observerTransitionRecoveryCopy,
            LongRunningDecisionReceiptSummary: longRunningDecisionReceiptSummary,
            LongRunningDecisionReceipts: longRunningDecisionReceipts,
            LowNoiseGuidance: lowNoiseGuidance,
            AttentionItems: attentionItems.Count == 0
                ? ["No blocking continuity issues are active on this device."]
                : attentionItems,
            ChangePacketLabels: changePacketLabels,
            QuickActionLabels: resume.Bootstrap.QuickActions.Select(action => action.Label).ToArray(),
            FollowThroughLabels: followThroughLabels,
            CoachHints: resume.Bootstrap.CoachHints.Select(hint => hint.Message).ToArray());
    }

    private static string SelectArtifactShelfView(PlaySurfaceRole role, RecapShelfEntry? recapEntry)
    {
        HashSet<string> availableViews = GetArtifactAudienceKinds(recapEntry?.Audience);
        string preferredView = role switch
        {
            PlaySurfaceRole.GameMaster => "campaign",
            PlaySurfaceRole.Observer when recapEntry?.Discoverable == true => "creator",
            PlaySurfaceRole.Observer => "campaign",
            _ => "personal"
        };

        if (availableViews.Contains(preferredView))
        {
            return preferredView;
        }

        foreach (string fallbackView in new[] { "campaign", "personal", "creator" })
        {
            if (availableViews.Contains(fallbackView))
            {
                return fallbackView;
            }
        }

        return "campaign";
    }

    private static PlayArtifactShelfViewLink[] BuildArtifactShelfViews(RecapShelfEntry? recapEntry, string selectedArtifactView)
    {
        HashSet<string> availableViews = GetArtifactAudienceKinds(recapEntry?.Audience);

        return
        [
            new(
                ViewId: "personal",
                Label: "My stuff",
                Summary: availableViews.Contains("personal")
                    ? $"Reuse the same dossier-safe return lane without duplicating records. {recapEntry?.OwnershipSummary ?? "This view keeps install-local ownership explicit."}"
                    : "No dossier-safe return lane is attached yet, but this view stays reserved for personal artifact truth.",
                Href: "/artifacts?view=personal",
                IsSelected: string.Equals(selectedArtifactView, "personal", StringComparison.Ordinal)),
            new(
                ViewId: "campaign",
                Label: "Campaign stuff",
                Summary: availableViews.Contains("campaign")
                    ? $"Browse the live campaign recap packet on the shared lane. {recapEntry?.Summary ?? "This view keeps the table-facing recap artifact attached to the current workspace."}"
                    : "No shared campaign recap packet is attached yet, but this view stays reserved for campaign artifact truth.",
                Href: "/artifacts?view=campaign",
                IsSelected: string.Equals(selectedArtifactView, "campaign", StringComparison.Ordinal)),
            new(
                ViewId: "creator",
                Label: "Published stuff",
                Summary: availableViews.Contains("creator")
                    ? recapEntry?.Discoverable == true
                        ? $"Browse the governed creator packet directly from the same recap-safe artifact. {recapEntry.PublicationSummary}"
                        : $"The same creator packet is still bounded until publication clears review. {recapEntry?.NextSafeAction ?? "Review creator publication status before you widen the audience."}"
                    : "No creator-safe packet is attached yet, but this view stays reserved for published artifact truth.",
                Href: "/artifacts?view=creator",
                IsSelected: string.Equals(selectedArtifactView, "creator", StringComparison.Ordinal))
        ];
    }

    private static HashSet<string> GetArtifactAudienceKinds(string? audience)
    {
        if (string.IsNullOrWhiteSpace(audience))
        {
            return ["campaign"];
        }

        HashSet<string> kinds = audience
            .Split([',', ';', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static value => value.Trim().ToLowerInvariant())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (kinds.Count == 0)
        {
            kinds.Add("campaign");
        }

        return kinds;
    }

    private static string HumanizeAudience(string? audience)
    {
        if (string.IsNullOrWhiteSpace(audience))
        {
            return "Campaign";
        }

        var labels = audience
            .Split([',', ';', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static value => value.Trim())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.ToLowerInvariant() switch
            {
                "personal" => "My stuff",
                "campaign" => "Campaign stuff",
                "creator" => "Published stuff",
                _ => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Replace('_', ' ').Replace('-', ' '))
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return labels.Length == 0 ? "Campaign" : string.Join(", ", labels);
    }

    private static string HumanizeState(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
            value.Replace('_', ' ').Replace('-', ' '));
    }

    private static RecapShelfEntry? FindLeadRecapEntry(PlayCampaignWorkspaceServerPlane serverPlane)
        => serverPlane.RecapShelf.FirstOrDefault(static item => !IsReplayEntry(item))
            ?? serverPlane.RecapShelf.FirstOrDefault();

    private static RecapShelfEntry? FindLeadReplayEntry(PlayCampaignWorkspaceServerPlane serverPlane)
        => serverPlane.RecapShelf.FirstOrDefault(IsReplayEntry);

    private static bool IsReplayEntry(RecapShelfEntry item)
        => item.Kind.Contains("replay", StringComparison.OrdinalIgnoreCase);

    private static string BuildChangePacketSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string latestTimeline)
    {
        string checkpointSummary = resume.Checkpoint is null
            ? "No local return anchor is pinned yet."
            : $"Return anchor stays on checkpoint {resume.Checkpoint.AppliedThroughSequence}.";
        string bundleSummary = resume.RuntimeBundle is null
            ? "No validated bundle is cached locally."
            : $"Bundle {resume.RuntimeBundle.BundleTag} is already validated.";
        return $"Scene {session.SceneId} remains pinned at {session.SceneRevision}. Latest table signal: {latestTimeline}. {checkpointSummary} {bundleSummary}";
    }

    private static string BuildRolePosture(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        string route = string.IsNullOrWhiteSpace(resume.DeepLinkOwnerRoute)
            ? $"/play/{resume.SessionId}"
            : resume.DeepLinkOwnerRoute;

        return resume.Role switch
        {
            PlaySurfaceRole.GameMaster => $"Role posture: GM runboard on {route}. This device is the coordination lane for {session.SceneId}, so scene changes should originate here before they fan out.",
            PlaySurfaceRole.Observer => $"Role posture: observer lane on {route}. Keep this shell read-mostly until the owner lane confirms the next scene revision.",
            _ => $"Role posture: player lane on {route}. Keep this shell focused on one grounded move at a time and leave prep-heavy changes to the workbench."
        };
    }

    private static string BuildLegalRunnerSummary(PlayResumeResponse resume, EngineSessionEnvelope session)
        => resume.RuntimeBundle is null
            ? $"Legal runner: {session.RuntimeFingerprint} is loaded, but this shell still needs grounded bundle proof before you trust the first playable session."
            : $"Legal runner: bundle {resume.RuntimeBundle.BundleTag} keeps {session.RuntimeFingerprint} grounded on this shell.";

    private static string BuildUnderstandableReturnSummary(
        PlayCampaignWorkspaceServerPlane serverPlane,
        string continuityPosture)
        => $"Understandable return: {continuityPosture} {serverPlane.Campaign.RestoreSummary}";

    private static string BuildCampaignReadySummary(PlayCampaignWorkspaceServerPlane serverPlane)
        => $"Campaign-ready lane: {serverPlane.Campaign.SessionReadinessSummary} {serverPlane.Roster.Summary}";

    private static string BuildSafeNextAction(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        if (resume.CachePressure.BackpressureActive)
        {
            return "Clear cache pressure before you pin additional offline state or trust this device as a travel-safe shell.";
        }

        if (resume.RuntimeBundle is null)
        {
            return $"Reopen {session.SceneId} once while connected so this device can cache the grounded runtime bundle before the next table session.";
        }

        return resume.Role switch
        {
            PlaySurfaceRole.GameMaster => $"Open the GM shell, confirm scene {session.SceneId}, then use the next stale-protected action from the runboard.",
            PlaySurfaceRole.Observer => $"Resume the observer lane for {session.SceneId} and confirm continuity before you mirror any tactical update.",
            _ => $"Sync before taking the next quick action in {session.SceneId}, then keep the player lane focused on one grounded move."
        };
    }

    private static string BuildUpdatePosture(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        if (resume.RuntimeBundle is null)
        {
            return $"Update posture: reconnect {session.SceneId} before you trust offline play on this device; no validated runtime bundle is cached locally yet.";
        }

        return $"Update posture: bundle {resume.RuntimeBundle.BundleTag} for {session.RuntimeFingerprint} was validated at {resume.RuntimeBundle.LastValidatedAtUtc:yyyy-MM-dd HH:mm} UTC and is the grounded local update target.";
    }

    private static string BuildTravelPosture(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        PlayCampaignWorkspaceServerPlane serverPlane)
    {
        if (resume.RuntimeBundle is null)
        {
            return $"Travel posture: reconnect {session.SceneId} once so this claimed device can prefetch dossier, campaign, rule-environment, replay, and recap truth before the next safehouse or travel handoff.";
        }

        if (resume.CachePressure.BackpressureActive)
        {
            return $"Travel posture: bundle {resume.RuntimeBundle.BundleTag} is grounded, but cache pressure already touched {resume.CachePressure.EvictedEntryCount} session(s), so bounded offline prefetch is degraded and can still drift before the next travel handoff.";
        }

        if (resume.Checkpoint is null)
        {
            return $"Travel posture: bundle {resume.RuntimeBundle.BundleTag} is grounded, but seed a local checkpoint before you trust this shell as a safehouse return anchor.";
        }

        return $"Travel posture: checkpoint {resume.Checkpoint.AppliedThroughSequence}, bundle {resume.RuntimeBundle.BundleTag}, and {BuildAftermathCoverageSummary(serverPlane)} are ready for bounded offline use on this claimed device.";
    }

    private static string BuildOfflinePrefetchSummary(
        PlayResumeResponse resume,
        PlayCampaignWorkspaceServerPlane serverPlane,
        string roleLabel)
    {
        string checkpointSummary = resume.Checkpoint is null
            ? "no local checkpoint yet"
            : $"checkpoint {resume.Checkpoint.AppliedThroughSequence}";
        string bundleSummary = resume.RuntimeBundle is null
            ? "runtime proof pending"
            : $"bundle {resume.RuntimeBundle.BundleTag}";
        string dossierSummary = $"{resume.Bootstrap.Projection.Cursor.Session.SceneId} return dossier";
        string campaignSummary = serverPlane.Campaign.CampaignName;
        string ruleSummary = resume.Bootstrap.Projection.Cursor.Session.RuntimeFingerprint;
        string aftermathSummary = BuildAftermathCoverageSummary(serverPlane);
        return $"Offline prefetch: {checkpointSummary}, {bundleSummary}, dossier '{dossierSummary}', campaign '{campaignSummary}', rules '{ruleSummary}', {aftermathSummary}, and the {roleLabel} return lane stay install-local and bounded to this device.";
    }

    private static string BuildCampaignMemorySummary(
        PlayResumeResponse resume,
        PlayCampaignWorkspaceServerPlane serverPlane,
        string roleLabel,
        string latestTimeline)
    {
        string checkpointSummary = resume.Checkpoint is null
            ? "checkpoint pending"
            : $"checkpoint {resume.Checkpoint.AppliedThroughSequence}";
        string aftermathLabel = BuildAftermathCoverageSummary(serverPlane);
        return $"Campaign memory: {serverPlane.Workspace.CampaignName}, {checkpointSummary}, '{latestTimeline}', and {aftermathLabel} stay on one governed memory lane for the {roleLabel}.";
    }

    private static string BuildCampaignMemoryReturnSummary(
        PlayResumeResponse resume,
        PlayCampaignWorkspaceServerPlane serverPlane,
        string roleLabel)
    {
        string returnSummary = serverPlane.Workspace.ReturnSummary;
        string nextSafeAction = serverPlane.NextSafeAction.Summary;
        return $"Memory return: {returnSummary} Next: {nextSafeAction} Keep the {roleLabel} on the same install-local continuity lane.";
    }

    private static string BuildContinuityRailSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        PlayCampaignWorkspaceServerPlane serverPlane,
        string roleLabel,
        string latestTimeline)
    {
        string downtime = resume.RuntimeBundle is null
            ? $"Downtime: reconnect {session.SceneId} once before you trust offline training or acquisition carry-forward."
            : $"Downtime: checkpoint {resume.Checkpoint?.AppliedThroughSequence.ToString() ?? "pending"} plus bundle {resume.RuntimeBundle.BundleTag} keep training/acquisition carry-forward grounded.";
        string diary = $"Diary: '{latestTimeline}' stays on the same governed campaign memory lane.";
        string contacts = $"Contacts: support follow-through stays on one account-linked lane for the {roleLabel} via {serverPlane.SupportClosures.FirstOrDefault()?.StageLabel ?? "workspace support posture"}.";
        string heat = resume.CachePressure.BackpressureActive
            ? $"Heat: degraded warning posture is active because cache pressure already touched {resume.CachePressure.EvictedEntryCount} session(s)."
            : "Heat: no continuity warning is active for this lane right now.";
        string aftermath = $"Aftermath: {BuildAftermathCoverageSummary(serverPlane)}.";
        string ret = $"Return: {serverPlane.Workspace.ReturnSummary}";
        return $"{downtime} {diary} {contacts} {heat} {aftermath} {ret}";
    }

    private static string[] BuildContinuityRailLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        PlayCampaignWorkspaceServerPlane serverPlane,
        string roleLabel,
        string latestTimeline)
    {
        string downtime = resume.RuntimeBundle is null
            ? $"Downtime lane: reconnect {session.SceneId} once before you trust offline carry-forward."
            : $"Downtime lane: checkpoint {resume.Checkpoint?.AppliedThroughSequence.ToString() ?? "pending"} and bundle {resume.RuntimeBundle.BundleTag} keep post-run carry-forward grounded.";
        string diary = $"Diary lane: '{latestTimeline}' remains the newest governed event signal.";
        string contacts = $"Contacts lane: keep support/contact follow-through tied to this {roleLabel} and the same claimed-device continuity packet.";
        string heat = resume.CachePressure.BackpressureActive
            ? $"Heat lane: degraded warning-only while cache pressure is active ({resume.CachePressure.RuntimeBundleCount}/{resume.CachePressure.RuntimeBundleQuota})."
            : "Heat lane: calm continuity posture; no active cache-pressure warning.";
        string aftermath = $"Aftermath lane: {BuildAftermathCoverageSummary(serverPlane)}.";
        string ret = $"Return lane: {serverPlane.Workspace.ReturnSummary}";
        return [downtime, diary, contacts, heat, aftermath, ret];
    }

    private static string BuildGmOperationsSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        PlayCampaignWorkspaceServerPlane serverPlane,
        string roleLabel)
    {
        string opposition = resume.RuntimeBundle is null
            ? $"Opposition: pending grounded runtime proof for {session.SceneId} before opposition packets are travel-safe."
            : $"Opposition: {session.SceneId} opposition packet remains governed on bundle {resume.RuntimeBundle.BundleTag}.";
        string roster = $"Roster movement: {serverPlane.Roster.Summary}";
        string prep = resume.CachePressure.BackpressureActive
            ? "Prep library: warning-only while cache pressure is active; keep prep packets review-first."
            : "Prep library: governed prep packets stay aligned to the current runboard lane.";
        string events = $"Event controls: {serverPlane.NextSafeAction.Summary}";
        return $"{opposition} {roster} {prep} {events} Keep GM and organizer actions on the same account-backed, audit-visible, support-linked {roleLabel} lane.";
    }

    private static string[] BuildGmOperationsLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        PlayCampaignWorkspaceServerPlane serverPlane,
        string roleLabel)
    {
        string opposition = resume.RuntimeBundle is null
            ? $"Opposition lane: reconnect {session.SceneId} once before you trust offline opposition packets."
            : $"Opposition lane: bundle {resume.RuntimeBundle.BundleTag} keeps opposition packet posture grounded.";
        string roster = $"Roster movement lane: {serverPlane.Roster.Summary}";
        string prep = resume.CachePressure.BackpressureActive
            ? $"Prep library lane: degraded while cache pressure is active ({resume.CachePressure.RuntimeBundleCount}/{resume.CachePressure.RuntimeBundleQuota})."
            : "Prep library lane: governed and aligned to the current runboard objective.";
        string events = $"Event controls lane: {serverPlane.NextSafeAction.Summary}";
        string governance = $"Governance lane: keep GM and organizer actions on the same account-backed, audit-visible, support-linked {roleLabel} rail.";
        return [opposition, roster, prep, events, governance];
    }

    private static string BuildOfflineTruthSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session)
    {
        string cached = resume.RuntimeBundle is null
            ? $"Cached: bundle proof for {session.SceneId} is not stored locally yet."
            : $"Cached: bundle {resume.RuntimeBundle.BundleTag} is validated and install-local on this shell.";
        string stale = resume.CachePressure.BackpressureActive
            ? $"Stale: degraded warning posture is active because cache pressure already touched {resume.CachePressure.EvictedEntryCount} session(s)."
            : "Stale: no active cache-pressure warning is published.";
        string action = resume.RuntimeBundle is null || resume.Checkpoint is null
            ? $"Offline actions: read-only review is safe; reconnect {session.SceneId} before mutating continuity-critical state."
            : resume.Role switch
            {
                PlaySurfaceRole.GameMaster => $"Offline actions: GM tactical continuity is allowed for {session.SceneId}; keep runboard mutations stale-protected.",
                PlaySurfaceRole.Observer => $"Offline actions: observer watch and recap review are allowed; keep this shell read-mostly until owner confirmation.",
                _ => $"Offline actions: player return cues and recap review are allowed for {session.SceneId}; sync before queueing new mutations."
            };
        string canDoNow = resume.RuntimeBundle is null || resume.Checkpoint is null
            ? "Can do now: recap-safe and replay-safe review only."
            : resume.Role switch
            {
                PlaySurfaceRole.GameMaster => "Can do now: review opposition packet, queue roster movement notes, run event-control checklists, and stage safehouse handoff notes on this install-local lane.",
                PlaySurfaceRole.Observer => "Can do now: observer watch, recap review, prep packet read-through, and safehouse travel readiness review on this install-local lane.",
                _ => "Can do now: player return cues, downtime notes, diary review, contacts follow-through, and safehouse return prep on this install-local lane."
            };
        string needsOnline = resume.RuntimeBundle is null || resume.Checkpoint is null
            ? $"Needs online: reconnect {session.SceneId} to ground checkpoint plus bundle proof before continuity mutations."
            : "Needs online: publish-facing promotion, cross-device fan-out, safehouse travel handoff propagation, and final mutation sync confirmation.";
        return $"{cached} {stale} {action} {canDoNow} {needsOnline}";
    }

    private static string[] BuildOfflineTruthLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel)
    {
        string cached = resume.RuntimeBundle is null
            ? $"Cached lane: runtime proof missing for {session.SceneId}; reconnect once to seed local bundle truth."
            : $"Cached lane: {resume.RuntimeBundle.BundleTag} is validated and pinned install-local for {roleLabel}.";
        string stale = resume.CachePressure.BackpressureActive
            ? $"Stale lane: degraded warning-only because cache pressure is active ({resume.CachePressure.RuntimeBundleCount}/{resume.CachePressure.RuntimeBundleQuota})."
            : "Stale lane: green; no active cache-pressure warning.";
        string action = resume.RuntimeBundle is null || resume.Checkpoint is null
            ? $"Offline action lane: stay read-only until checkpoint and runtime proof are both grounded on this device."
            : $"Offline action lane: checkpoint {resume.Checkpoint.AppliedThroughSequence} anchors bounded offline and safehouse follow-through for {roleLabel}.";
        string canDoNow = resume.RuntimeBundle is null || resume.Checkpoint is null
            ? "Can-do-now lane: recap/replay review only until runtime and checkpoint truth are grounded."
            : "Can-do-now lane: continuity-safe notes, safehouse-ready prep, and bounded role actions are allowed install-local.";
        string needsOnline = resume.RuntimeBundle is null || resume.Checkpoint is null
            ? $"Needs-online lane: reconnect {session.SceneId} for grounded bundle and checkpoint truth."
            : "Needs-online lane: publish promotion, safehouse handoff propagation, cross-device propagation, and final mutation sync remain online-only.";
        return [cached, stale, action, canDoNow, needsOnline];
    }

    private static string BuildSupportPosture(PlayResumeResponse resume, PlayCampaignWorkspaceServerPlane serverPlane)
    {
        var cue = serverPlane.SupportClosures.FirstOrDefault();
        return cue is null
            ? "Support posture: no support closure cue is attached yet."
            : $"Support posture: {cue.StageLabel}. {cue.Summary}";
    }

    private static string BuildSupportStatus(PlayCampaignWorkspaceServerPlane serverPlane)
    {
        var cue = serverPlane.SupportClosures.FirstOrDefault();
        return cue is null
            ? "Support status: no support closure cue is attached yet."
            : $"Support status: {cue.StageLabel}.";
    }

    private static string BuildKnownIssueSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        PlayCampaignWorkspaceServerPlane serverPlane)
        => serverPlane.KnownIssues.FirstOrDefault() is { } issue
            ? $"Known issue: {issue.Summary}"
            : resume.RuntimeBundle is null
                ? $"Known issue: {resume.SessionId}/{session.SceneId} resumed without a validated local bundle, so offline trust is still provisional."
                : $"Known issue: if {resume.SessionId}/{session.SceneId} still reproduces the same problem, support should cite the current bundle proof rather than reopen without runtime context.";

    private static string BuildFixAvailabilitySummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        PlayCampaignWorkspaceServerPlane serverPlane)
    {
        var cue = serverPlane.SupportClosures.FirstOrDefault();
        if (cue is not null && !string.IsNullOrWhiteSpace(cue.FixedReleaseLabel))
        {
            return $"Fix availability: {cue.FixedReleaseLabel} is the grounded local fix and update target for {session.RuntimeFingerprint}.";
        }

        return resume.RuntimeBundle is null
            ? $"Fix availability: no validated local fix target is cached yet for {session.RuntimeFingerprint}."
            : $"Fix availability: bundle {resume.RuntimeBundle.BundleTag} is the grounded local fix and update target for {session.RuntimeFingerprint}.";
    }

    private static string BuildCurrentCautionSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session)
    {
        if (resume.RuntimeBundle is null)
        {
            string fallback = $"Reconnect {session.SceneId} and validate a grounded runtime bundle before you trust offline continuation or fix closure on this device.";
            return $"Current caution: {resume.SupportNotice?.NextSafeAction ?? fallback}";
        }

        if (resume.CachePressure.BackpressureActive)
        {
            return $"Current caution: Clear cache pressure, then re-check bundle {resume.RuntimeBundle.BundleTag} before you verify a fix or trust the next offline session.";
        }

        return $"Current caution: no extra caution is published for {session.SceneId} right now; use bundle {resume.RuntimeBundle.BundleTag} when you verify the fix or reopen support on this device.";
    }

    private static string BuildUpdateFollowThrough(PlayResumeResponse resume, EngineSessionEnvelope session)
        => resume.RuntimeBundle is null
            ? $"Reconnect {session.SceneId} and validate a grounded runtime bundle before trusting offline updates on this device."
            : $"Review update posture for bundle {resume.RuntimeBundle.BundleTag} before the next offline or travel session.";

    private static string BuildUpdateFollowThroughHref(PlayResumeResponse resume, EngineSessionEnvelope session)
        => resume.RuntimeBundle is null
            ? $"/downloads?runtime={Uri.EscapeDataString(session.RuntimeFingerprint)}&sessionId={Uri.EscapeDataString(resume.SessionId)}"
            : $"/downloads?bundle={Uri.EscapeDataString(resume.RuntimeBundle.BundleTag)}&runtime={Uri.EscapeDataString(session.RuntimeFingerprint)}";

    private static string BuildSupportFollowThrough(PlayResumeResponse resume, EngineSessionEnvelope session)
        => resume.SupportNotice is not null
            ? $"Support follow-through for {resume.SessionId}/{session.SceneId}: {resume.SupportNotice.NextSafeAction}"
            : resume.RuntimeBundle is null
                ? $"Prepare support context for {resume.SessionId}/{session.SceneId} with runtime {session.RuntimeFingerprint} and note that this device still lacks a local bundle."
                : $"Prepare support context for {resume.SessionId}/{session.SceneId} with runtime {session.RuntimeFingerprint} and bundle {resume.RuntimeBundle.BundleTag}.";

    private static string BuildArtifactLineageSummary(RecapShelfEntry item)
    {
        string laneLabel = IsReplayEntry(item) ? "replay" : "recap";
        return string.IsNullOrWhiteSpace(item.CreatorPublicationId)
            ? $"Artifact lineage: {item.Label} stays on one governed {laneLabel} lane until a successor publication replaces it."
            : $"Artifact lineage: {item.Label} stays attached to {item.CreatorPublicationId} until a governed successor publication replaces it.";
    }

    private static string BuildSupportFollowThroughHref(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        if (!string.IsNullOrWhiteSpace(resume.SupportNotice?.FollowThroughHref))
        {
            return resume.SupportNotice.FollowThroughHref!;
        }

        string bundle = resume.RuntimeBundle?.BundleTag ?? string.Empty;
        string title = resume.RuntimeBundle is null
            ? $"Mobile follow-through needs grounded runtime for {session.SceneId}"
            : $"Mobile follow-through needs support review for {session.SceneId}";
        string summary = resume.RuntimeBundle is null
            ? $"This mobile shell resumed {resume.SessionId}/{session.SceneId} on {session.RuntimeFingerprint} without a validated local bundle."
            : $"This mobile shell resumed {resume.SessionId}/{session.SceneId} on {session.RuntimeFingerprint} with bundle {bundle}.";
        string detail = resume.RuntimeBundle is null
            ? $"Session: {resume.SessionId}\nScene: {session.SceneId}\nRuntime: {session.RuntimeFingerprint}\nBundle: none cached locally\n\nWhat happened:\n- This device resumed the scene without a validated local runtime bundle.\n- Please ground the next support step against the mobile shell and reconnect path."
            : $"Session: {resume.SessionId}\nScene: {session.SceneId}\nRuntime: {session.RuntimeFingerprint}\nBundle: {bundle}\n\nWhat happened:\n- This device resumed the scene with a validated local runtime bundle.\n- Please ground the next support step against the current mobile shell and bundle.";
        return $"/contact?kind=install_help&title={Uri.EscapeDataString(title)}&summary={Uri.EscapeDataString(summary)}&detail={Uri.EscapeDataString(detail)}&sessionId={Uri.EscapeDataString(resume.SessionId)}&sceneId={Uri.EscapeDataString(session.SceneId)}&runtime={Uri.EscapeDataString(session.RuntimeFingerprint)}&bundle={Uri.EscapeDataString(bundle)}";
    }

    private static string BuildRoleFollowThrough(PlayResumeResponse resume, EngineSessionEnvelope session)
        => resume.Role switch
        {
            PlaySurfaceRole.GameMaster => $"Keep GM changes anchored on {session.SceneId} before they fan out to other devices.",
            PlaySurfaceRole.Observer => $"Keep the observer lane read-mostly until the owner lane confirms the next scene revision.",
            _ => $"Keep the player lane focused on one grounded move before you reopen build or support follow-through elsewhere."
        };

    private static string BuildRoleFollowThroughHref(PlayResumeResponse resume, EngineSessionEnvelope session)
        => string.IsNullOrWhiteSpace(resume.DeepLinkOwnerRoute)
            ? $"/play?sessionId={Uri.EscapeDataString(resume.SessionId)}&role={Uri.EscapeDataString(resume.Role.ToString())}"
            : resume.DeepLinkOwnerRoute!;

    private static string[] BuildChangePacketLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string latestTimeline,
        PlayCampaignWorkspaceServerPlane serverPlane)
    {
        List<string> labels =
        [
            $"Scene packet: {session.SceneId} · {session.SceneRevision}",
            $"Latest signal: {latestTimeline}"
        ];

        if (resume.Checkpoint is not null)
        {
            labels.Add($"Return anchor: checkpoint {resume.Checkpoint.AppliedThroughSequence}");
        }

        if (resume.RuntimeBundle is not null)
        {
            labels.Add($"Bundle proof: {resume.RuntimeBundle.BundleTag}");
        }

        if (FindLeadReplayEntry(serverPlane) is { } replayEntry)
        {
            labels.Add($"Replay-safe packet: {replayEntry.Label}");
        }

        labels.Add(
            resume.RuntimeBundle is not null && resume.Checkpoint is not null
                ? $"Travel-safe packet: checkpoint {resume.Checkpoint.AppliedThroughSequence} + {resume.RuntimeBundle.BundleTag}"
                : "Travel-safe packet: reconnect required");

        PlayQuickAction? nextAction = resume.Bootstrap.QuickActions.FirstOrDefault();
        if (nextAction is not null)
        {
            labels.Add($"Quick action ready: {nextAction.Label}");
        }

        return labels.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string[] BuildFollowThroughLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        RecapShelfEntry? replayEntry,
        string recapPublicationSummary,
        string recapLineageSummary,
        string recapNextAction,
        string replayPublicationSummary,
        string replayLineageSummary,
        string replayNextAction,
        string currentCautionSummary,
        string updateFollowThrough,
        string supportFollowThrough,
        string roleFollowThrough)
    {
        List<string> labels =
        [
            recapPublicationSummary,
            recapLineageSummary,
            recapNextAction,
            currentCautionSummary,
            updateFollowThrough,
            supportFollowThrough,
            roleFollowThrough
        ];

        if (replayEntry is not null)
        {
            labels.InsertRange(3, [replayPublicationSummary, replayLineageSummary, replayNextAction]);
        }

        if (resume.CachePressure.BackpressureActive)
        {
            labels.Insert(0, "Clear cache pressure before you pin more travel or observer state on this device.");
        }

        if (resume.Checkpoint is null)
        {
            labels.Add("Seed a local continuity checkpoint before you trust this device as a stable return path.");
        }

        return labels.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string[] BuildLongRunningDecisionReceipts(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel,
        string continueCommandHref,
        string supportFollowThroughHref)
    {
        string rejoinReceipt = resume.Checkpoint is null
            ? $"Rejoin receipt: deferred until this device captures a local checkpoint for {session.SceneId}; no replay state was lost and the route remains {resume.DeepLinkOwnerRoute}."
            : $"Rejoin receipt: resumed on checkpoint {resume.Checkpoint.AppliedThroughSequence} for the {roleLabel}; retries were skipped because stored lineage already matched.";

        string quickActionReceipt = resume.Bootstrap.QuickActions.Count == 0
            ? $"Quick-action receipt: skipped because {resume.Role} currently has no permitted quick actions on this shell; continue on {continueCommandHref} or escalate via {supportFollowThroughHref}."
            : $"Quick-action receipt: {resume.Bootstrap.QuickActions.Count} action(s) are ready after reconnect; stale retries are deferred to preserve no-loss lineage and support escalation stays {supportFollowThroughHref}.";

        string resumeReceipt = resume.RuntimeBundle is null
            ? $"Resume receipt: deferred because runtime bundle proof is not cached locally; reconnect {session.SceneId} once, then resume without discarding queued replay events."
            : resume.CachePressure.BackpressureActive
                ? $"Resume receipt: resumed with warning because cache pressure is active ({resume.CachePressure.RuntimeBundleCount}/{resume.CachePressure.RuntimeBundleQuota}); retries stay bounded and deferred work remains replay-safe."
                : $"Resume receipt: resumed with grounded bundle {resume.RuntimeBundle.BundleTag}; retried or skipped steps are not required and this shell remains continuity-safe.";

        return [rejoinReceipt, quickActionReceipt, resumeReceipt];
    }

    private static string BuildDisconnectRecoveryCopy(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel)
    {
        string route = string.IsNullOrWhiteSpace(resume.DeepLinkOwnerRoute)
            ? $"/play/{Uri.EscapeDataString(resume.SessionId)}"
            : resume.DeepLinkOwnerRoute!;

        return resume.Checkpoint is null
            ? $"Disconnect recovery: this shell lost transport but preserved replay state; reconnect {session.SceneId} on {route} to seed a fresh checkpoint before mutating continuity-critical state on the {roleLabel}."
            : $"Disconnect recovery: this shell resumed from checkpoint {resume.Checkpoint.AppliedThroughSequence}; replay state stayed intact and {route} remains the no-loss rejoin path for the {roleLabel}.";
    }

    private static string BuildRoleChangeRecoveryCopy(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel)
    {
        string targetRole = resume.Role switch
        {
            PlaySurfaceRole.GameMaster => "GM runboard",
            PlaySurfaceRole.Observer => "observer lane",
            _ => "player lane"
        };

        return $"Role-change recovery: continue this session on the {targetRole}; if role posture changed mid-reconnect, reuse the same session ({resume.SessionId}/{session.SceneId}) and follow the role-safe route without clearing stored replay context on the {roleLabel}.";
    }

    private static string BuildObserverTransitionRecoveryCopy(
        PlayResumeResponse resume,
        EngineSessionEnvelope session)
        => resume.Role == PlaySurfaceRole.Observer
            ? $"Observer transition recovery: remain read-mostly on {session.SceneId}, keep owner confirmation as the next gate, and preserve stored replay context before mirroring tactical changes."
            : $"Observer transition recovery: when switching into observe mode for {session.SceneId}, keep the observer lane read-mostly and carry forward the same stored replay context instead of booting a fresh session.";

    private static string BuildAftermathCoverageSummary(PlayCampaignWorkspaceServerPlane serverPlane)
    {
        RecapShelfEntry? recapEntry = FindLeadRecapEntry(serverPlane);
        RecapShelfEntry? replayEntry = FindLeadReplayEntry(serverPlane);

        return (replayEntry, recapEntry) switch
        {
            ({ } replay, { } recap) => $"{replay.Label} and {recap.Label}",
            ({ } replay, null) => replay.Label,
            (null, { } recap) => recap.Label,
            _ => "no replay-safe or recap-safe packages yet"
        };
    }
}
