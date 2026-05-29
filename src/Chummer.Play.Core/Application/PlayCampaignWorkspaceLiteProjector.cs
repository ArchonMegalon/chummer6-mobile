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
    string PlayerTableCardsSummary,
    IReadOnlyList<string> PlayerTableCardLabels,
    string BetweenTurnAffordancesSummary,
    IReadOnlyList<string> BetweenTurnAffordanceLabels,
    string GmLiteContinuitySummary,
    IReadOnlyList<string> GmLiteContinuityLabels,
    string QuickExplainSummary,
    IReadOnlyList<string> QuickExplainLabels,
    string SourceAnchorSummary,
    IReadOnlyList<string> SourceAnchorLabels,
    string StaleStatePosture,
    string GroundedFollowUpSummary,
    IReadOnlyList<string> GroundedFollowUpLabels,
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
    string ArtifactShelfSelectionSummary,
    string SelectedRecapArtifactSummary,
    string SelectedRecapArtifactHref,
    IReadOnlyList<PlayArtifactShelfViewLink> ArtifactShelfViews,
    string LaunchPrimerSummary,
    string LaunchPrimerProvenanceSummary,
    string LaunchPrimerHref,
    string FirstSessionBriefingSummary,
    string FirstSessionBriefingProvenanceSummary,
    string FirstSessionBriefingHref,
    string StarterArtifactContinuitySummary,
    IReadOnlyList<string> StarterArtifactContinuityLabels,
    string RunnerGoalUpdatesSummary,
    IReadOnlyList<string> RunnerGoalUpdateLabels,
    string PlayerSafeConsequenceFeedSummary,
    IReadOnlyList<string> PlayerSafeConsequenceFeedLabels,
    string CampaignMemorySummary,
    string CampaignMemoryReturnSummary,
    string TablePulseSummary,
    string TablePulseHeatSummary,
    string TablePulseNotificationSummary,
    string TablePulseRemoteReactionSummary,
    string TablePulseSignalDeckSummary,
    string RunnerPassportSummary,
    string TablePulseAdjudicationSummary,
    string TablePulseLivingNewsroomSummary,
    string TablePulseLivingNewsroomHref,
    string TablePulseAftermathSummary,
    string TablePulseAftermathHref,
    string TablePulseFollowThroughSummary,
    string TablePulseFollowThroughHref,
    string TablePulseEntryHref,
    IReadOnlyList<string> TablePulseLabels,
    string ContinuityRailSummary,
    IReadOnlyList<string> ContinuityRailLabels,
    string GmOperationsSummary,
    IReadOnlyList<string> GmOperationsLabels,
    string OfflineTruthSummary,
    IReadOnlyList<string> OfflineTruthLabels,
    string ActionRequiredSummary,
    IReadOnlyList<string> ActionRequiredLabels,
    string MobileCampaignCurrentState,
    string MobileCampaignStateSummary,
    string MobileCampaignCachedState,
    string MobileCampaignStaleState,
    string MobileCampaignActionRequired,
    IReadOnlyList<string> MobileCampaignStateLabels,
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
    public static PlayCampaignWorkspaceLiteProjection Create(
        PlayResumeResponse resume,
        string? artifactView = null,
        string? artifactId = null)
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
        string playerTableCardsSummary = BuildPlayerTableCardsSummary(resume, session, latestTimeline, roleLabel);
        string[] playerTableCardLabels = BuildPlayerTableCardLabels(resume, session, latestTimeline, roleLabel);
        string betweenTurnAffordancesSummary = BuildBetweenTurnAffordancesSummary(resume, session, roleLabel);
        string[] betweenTurnAffordanceLabels = BuildBetweenTurnAffordanceLabels(resume, session, roleLabel);
        string gmLiteContinuitySummary = BuildGmLiteContinuitySummary(resume, session, serverPlane, roleLabel);
        string[] gmLiteContinuityLabels = BuildGmLiteContinuityLabels(resume, session, serverPlane, roleLabel);
        string quickExplainSummary = BuildQuickExplainSummary(resume, session, latestTimeline, roleLabel);
        string[] quickExplainLabels = BuildQuickExplainLabels(resume, session, latestTimeline, roleLabel);
        string sourceAnchorSummary = BuildSourceAnchorSummary(resume, session);
        string[] sourceAnchorLabels = BuildSourceAnchorLabels(resume, session, latestTimeline);
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
        string selectedArtifactView = SelectArtifactShelfView(resume.Role, recapEntry, artifactView);
        string artifactShelfSelectionSummary = BuildArtifactShelfSelectionSummary(
            selectedArtifactView,
            recapEntry,
            replayEntry,
            offlinePrefetchSummary,
            travelPosture);
        string selectedRecapArtifactSummary = BuildSelectedRecapArtifactSummary(
            selectedArtifactView,
            artifactId,
            recapEntry);
        string selectedRecapArtifactHref = BuildSelectedRecapArtifactHref(
            resume.SessionId,
            resume.Role,
            selectedArtifactView,
            artifactId);
        PlayArtifactShelfViewLink[] artifactShelfViews = BuildArtifactShelfViews(
            resume.SessionId,
            resume.Role,
            recapEntry,
            selectedArtifactView,
            offlinePrefetchSummary,
            travelPosture);
        RecapShelfEntry? primerEntry = FindLeadPrimerEntry(serverPlane);
        RecapShelfEntry? firstSessionBriefingEntry = FindLeadFirstSessionBriefingEntry(serverPlane);
        string launchPrimerSummary = BuildStarterArtifactSummary("Starter primer", primerEntry, "No starter primer is attached to this workspace yet.");
        string launchPrimerProvenanceSummary = primerEntry?.ProvenanceSummary
            ?? "Starter primer provenance: no governed starter primer is attached yet.";
        string launchPrimerHref = BuildStarterArtifactHref(resume.SessionId, resume.Role, primerEntry, selectedArtifactView);
        string firstSessionBriefingSummary = BuildStarterArtifactSummary("First-session briefing", firstSessionBriefingEntry, "No first-session briefing is attached to this workspace yet.");
        string firstSessionBriefingProvenanceSummary = firstSessionBriefingEntry?.ProvenanceSummary
            ?? "First-session briefing provenance: no governed first-session briefing is attached yet.";
        string firstSessionBriefingHref = BuildStarterArtifactHref(resume.SessionId, resume.Role, firstSessionBriefingEntry, "travel");
        string starterArtifactContinuitySummary = BuildStarterArtifactContinuitySummary(
            primerEntry,
            firstSessionBriefingEntry,
            travelPosture,
            offlinePrefetchSummary);
        string[] starterArtifactContinuityLabels = BuildStarterArtifactContinuityLabels(
            primerEntry,
            firstSessionBriefingEntry,
            selectedArtifactView,
            launchPrimerHref,
            firstSessionBriefingHref);
        string runnerGoalUpdatesSummary = BuildRunnerGoalUpdatesSummary(
            resume,
            session,
            serverPlane,
            roleLabel,
            latestTimeline);
        string[] runnerGoalUpdateLabels = BuildRunnerGoalUpdateLabels(
            resume,
            session,
            serverPlane,
            roleLabel,
            latestTimeline);
        string playerSafeConsequenceFeedSummary = BuildPlayerSafeConsequenceFeedSummary(
            resume,
            session,
            serverPlane,
            roleLabel,
            latestTimeline);
        string[] playerSafeConsequenceFeedLabels = BuildPlayerSafeConsequenceFeedLabels(
            resume,
            session,
            serverPlane,
            roleLabel,
            latestTimeline);
        string campaignMemorySummary = BuildCampaignMemorySummary(resume, serverPlane, roleLabel, latestTimeline);
        string campaignMemoryReturnSummary = BuildCampaignMemoryReturnSummary(resume, serverPlane, roleLabel);
        string tablePulseSummary = BuildTablePulseSummary(resume, session, roleLabel, latestTimeline);
        string tablePulseHeatSummary = BuildTablePulseHeatSummary(resume, session, roleLabel);
        string tablePulseNotificationSummary = BuildTablePulseNotificationSummary(resume, roleLabel);
        string tablePulseRemoteReactionSummary = BuildTablePulseRemoteReactionSummary(resume, roleLabel);
        string tablePulseSignalDeckSummary = BuildTablePulseSignalDeckSummary(resume, roleLabel);
        string runnerPassportSummary = BuildRunnerPassportSummary(resume, roleLabel);
        string tablePulseAdjudicationSummary = BuildTablePulseAdjudicationSummary(serverPlane, roleLabel);
        string tablePulseLivingNewsroomSummary = BuildTablePulseLivingNewsroomSummary(resume, session, serverPlane, roleLabel, latestTimeline);
        string tablePulseLivingNewsroomHref = BuildTablePulseLivingNewsroomHref();
        string tablePulseAftermathSummary = BuildTablePulseAftermathSummary(serverPlane, roleLabel);
        string tablePulseAftermathHref = BuildTablePulseAftermathHref();
        string tablePulseFollowThroughSummary = BuildTablePulseFollowThroughSummary(serverPlane, roleLabel);
        string tablePulseFollowThroughHref = BuildTablePulseFollowThroughHref(serverPlane);
        string tablePulseEntryHref = BuildTablePulseEntryHref(resume);
        string[] tablePulseLabels = BuildTablePulseLabels(
            resume,
            session,
            roleLabel,
            latestTimeline,
            tablePulseEntryHref);
        string continuityRailSummary = BuildContinuityRailSummary(resume, session, serverPlane, roleLabel, latestTimeline);
        string[] continuityRailLabels = BuildContinuityRailLabels(resume, session, serverPlane, roleLabel, latestTimeline);
        string gmOperationsSummary = BuildGmOperationsSummary(resume, session, serverPlane, roleLabel);
        string[] gmOperationsLabels = BuildGmOperationsLabels(resume, session, serverPlane, roleLabel);
        string offlineTruthSummary = BuildOfflineTruthSummary(resume, session);
        string[] offlineTruthLabels = BuildOfflineTruthLabels(resume, session, roleLabel);
        string actionRequiredSummary = BuildActionRequiredSummary(resume, session, roleLabel);
        string[] actionRequiredLabels = BuildActionRequiredLabels(resume, session, roleLabel);
        string staleStatePosture = BuildStaleStatePosture(resume, session, roleLabel);
        string mobileCampaignCurrentState = BuildMobileCampaignCurrentState(resume, session, roleLabel, continuityPosture, travelPosture);
        string mobileCampaignStateSummary = BuildMobileCampaignStateSummary(resume, session, roleLabel, offlinePrefetchSummary, cachePosture);
        string mobileCampaignCachedState = BuildMobileCampaignCachedState(resume, session, roleLabel, offlineTruthLabels);
        string mobileCampaignStaleState = BuildMobileCampaignStaleState(resume, session, roleLabel, staleStatePosture);
        string mobileCampaignActionRequired = BuildMobileCampaignActionRequired(resume, session, roleLabel, actionRequiredSummary);
        string[] mobileCampaignStateLabels = BuildMobileCampaignStateLabels(
            resume,
            session,
            roleLabel,
            mobileCampaignCurrentState,
            offlineTruthLabels,
            actionRequiredLabels);
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
        string groundedFollowUpSummary = BuildGroundedFollowUpSummary(
            resume,
            session,
            roleLabel,
            safeNextAction,
            updateFollowThrough,
            supportFollowThrough);
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
        string[] groundedFollowUpLabels = BuildGroundedFollowUpLabels(
            resume,
            session,
            roleLabel,
            safeNextAction,
            updateFollowThrough,
            supportFollowThrough,
            roleFollowThrough,
            followThroughLabels);

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
            PlayerTableCardsSummary: playerTableCardsSummary,
            PlayerTableCardLabels: playerTableCardLabels,
            BetweenTurnAffordancesSummary: betweenTurnAffordancesSummary,
            BetweenTurnAffordanceLabels: betweenTurnAffordanceLabels,
            GmLiteContinuitySummary: gmLiteContinuitySummary,
            GmLiteContinuityLabels: gmLiteContinuityLabels,
            QuickExplainSummary: quickExplainSummary,
            QuickExplainLabels: quickExplainLabels,
            SourceAnchorSummary: sourceAnchorSummary,
            SourceAnchorLabels: sourceAnchorLabels,
            StaleStatePosture: staleStatePosture,
            GroundedFollowUpSummary: groundedFollowUpSummary,
            GroundedFollowUpLabels: groundedFollowUpLabels,
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
            ArtifactShelfSelectionSummary: artifactShelfSelectionSummary,
            SelectedRecapArtifactSummary: selectedRecapArtifactSummary,
            SelectedRecapArtifactHref: selectedRecapArtifactHref,
            ArtifactShelfViews: artifactShelfViews,
            LaunchPrimerSummary: launchPrimerSummary,
            LaunchPrimerProvenanceSummary: launchPrimerProvenanceSummary,
            LaunchPrimerHref: launchPrimerHref,
            FirstSessionBriefingSummary: firstSessionBriefingSummary,
            FirstSessionBriefingProvenanceSummary: firstSessionBriefingProvenanceSummary,
            FirstSessionBriefingHref: firstSessionBriefingHref,
            StarterArtifactContinuitySummary: starterArtifactContinuitySummary,
            StarterArtifactContinuityLabels: starterArtifactContinuityLabels,
            RunnerGoalUpdatesSummary: runnerGoalUpdatesSummary,
            RunnerGoalUpdateLabels: runnerGoalUpdateLabels,
            PlayerSafeConsequenceFeedSummary: playerSafeConsequenceFeedSummary,
            PlayerSafeConsequenceFeedLabels: playerSafeConsequenceFeedLabels,
            CampaignMemorySummary: campaignMemorySummary,
            CampaignMemoryReturnSummary: campaignMemoryReturnSummary,
            TablePulseSummary: tablePulseSummary,
            TablePulseHeatSummary: tablePulseHeatSummary,
            TablePulseNotificationSummary: tablePulseNotificationSummary,
            TablePulseRemoteReactionSummary: tablePulseRemoteReactionSummary,
            TablePulseSignalDeckSummary: tablePulseSignalDeckSummary,
            RunnerPassportSummary: runnerPassportSummary,
            TablePulseAdjudicationSummary: tablePulseAdjudicationSummary,
            TablePulseLivingNewsroomSummary: tablePulseLivingNewsroomSummary,
            TablePulseLivingNewsroomHref: tablePulseLivingNewsroomHref,
            TablePulseAftermathSummary: tablePulseAftermathSummary,
            TablePulseAftermathHref: tablePulseAftermathHref,
            TablePulseFollowThroughSummary: tablePulseFollowThroughSummary,
            TablePulseFollowThroughHref: tablePulseFollowThroughHref,
            TablePulseEntryHref: tablePulseEntryHref,
            TablePulseLabels: tablePulseLabels,
            ContinuityRailSummary: continuityRailSummary,
            ContinuityRailLabels: continuityRailLabels,
            GmOperationsSummary: gmOperationsSummary,
            GmOperationsLabels: gmOperationsLabels,
            OfflineTruthSummary: offlineTruthSummary,
            OfflineTruthLabels: offlineTruthLabels,
            ActionRequiredSummary: actionRequiredSummary,
            ActionRequiredLabels: actionRequiredLabels,
            MobileCampaignCurrentState: mobileCampaignCurrentState,
            MobileCampaignStateSummary: mobileCampaignStateSummary,
            MobileCampaignCachedState: mobileCampaignCachedState,
            MobileCampaignStaleState: mobileCampaignStaleState,
            MobileCampaignActionRequired: mobileCampaignActionRequired,
            MobileCampaignStateLabels: mobileCampaignStateLabels,
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

    private static string BuildTablePulseSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel,
        string latestTimeline)
        => $"Table Pulse is live for {session.SceneId}: '{latestTimeline}' is the current heat-facing cue, remote reactions stay packet-backed, and this {roleLabel} shell only exposes bounded follow-through.";

    private static string BuildTablePulseHeatSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel)
    {
        if (resume.CachePressure.BackpressureActive)
        {
            return $"Heat lane: degraded on this {roleLabel} shell until cache pressure clears, so only notification-safe summaries should be trusted for {session.SceneId}.";
        }

        return $"Heat lane: stable on this {roleLabel} shell. Route back through the claimed-device packet before you widen consequences beyond {session.SceneId}.";
    }

    private static string BuildTablePulseNotificationSummary(PlayResumeResponse resume, string roleLabel)
        => resume.Checkpoint is null
            ? $"Notification rail: no claimed-device anchor is pinned yet, so delivery stays inbox-safe and review-first for the {roleLabel}."
            : $"Notification rail: checkpoint {resume.Checkpoint.AppliedThroughSequence} keeps the inbox, push handoff, and rejoin route attached to one claimed-device packet.";

    private static string BuildTablePulseRemoteReactionSummary(PlayResumeResponse resume, string roleLabel)
        => $"Remote reactions: Intercept, Cover Story, Scramble, Temptation, and Shadow Reply stay advisory-only on the {roleLabel} shell until the GM adjudication route accepts them.";

    private static string BuildTablePulseSignalDeckSummary(PlayResumeResponse resume, string roleLabel)
        => $"Signal Deck: use the mobile shell as the low-noise dispatch rail for heat, rumors, and faction prompts instead of scattering alerts across unrelated surfaces. This stays bounded to the {roleLabel}.";

    private static string BuildRunnerPassportSummary(PlayResumeResponse resume, string roleLabel)
        => $"Runner Passport: keep return posture, participation proof, and cross-table trust visible on the {roleLabel} shell without turning identity into a public ranking surface.";

    private static string BuildTablePulseAdjudicationSummary(PlayCampaignWorkspaceServerPlane serverPlane, string roleLabel)
        => $"Adjudication lane: {serverPlane.Campaign.SessionReadinessSummary} Governed consequences and next-safe action stay attached to the {roleLabel} instead of splitting into a side-channel minigame log.";

    private static string BuildTablePulseLivingNewsroomSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        PlayCampaignWorkspaceServerPlane serverPlane,
        string roleLabel,
        string latestTimeline)
        => $"Living Newsroom: '{latestTimeline}' is the current public-safe bulletin cue for {session.SceneId}, and the {roleLabel} shell keeps it tied to the same governed consequence rail instead of splitting command mood from watchable fallout.";

    private static string BuildTablePulseLivingNewsroomHref()
        => "/ledger/turns/1";

    private static string BuildTablePulseAftermathSummary(PlayCampaignWorkspaceServerPlane serverPlane, string roleLabel)
        => $"Aftermath lane: {serverPlane.AftermathPackages.Count} governed package(s) currently sit behind the {roleLabel} shell, so remote reactions can return as receipts and next-safe action instead of vanishing after adjudication.";

    private static string BuildTablePulseAftermathHref()
        => "/account/work#aftermath-packages";

    private static string BuildTablePulseFollowThroughSummary(PlayCampaignWorkspaceServerPlane serverPlane, string roleLabel)
        => $"Follow-through lane: {serverPlane.NextSafeAction.Summary} Signal Deck, Runner Passport, and aftermath packages stay on one bounded {roleLabel} rail.";

    private static string BuildTablePulseFollowThroughHref(PlayCampaignWorkspaceServerPlane serverPlane)
        => "/account/work";

    private static string BuildTablePulseEntryHref(PlayResumeResponse resume)
        => $"/play/{Uri.EscapeDataString(resume.SessionId)}?role={Uri.EscapeDataString(resume.Role.ToString())}&view=table-pulse";

    private static string[] BuildTablePulseLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel,
        string latestTimeline,
        string tablePulseEntryHref)
    {
        string checkpointLabel = resume.Checkpoint is null
            ? "No claimed-device checkpoint is pinned yet, so reactions stay review-only."
            : $"Claimed-device checkpoint {resume.Checkpoint.AppliedThroughSequence} governs return and delivery truth.";
        string bundleLabel = resume.RuntimeBundle is null
            ? "Runtime bundle proof is still pending, so heat should be treated as advisory."
            : $"Runtime bundle {resume.RuntimeBundle.BundleTag} keeps Table Pulse copy grounded on this shell.";
        return
        [
            $"Heat cue: {latestTimeline}",
            $"Remote reactions: Intercept, Cover Story, Scramble, Temptation, Shadow Reply",
            $"Signal Deck route: {tablePulseEntryHref}",
            $"Runner Passport lane: private trust posture only for {roleLabel}",
            $"Session boundary: {session.SceneId}",
            checkpointLabel,
            bundleLabel
        ];
    }

    private static string SelectArtifactShelfView(PlaySurfaceRole role, RecapShelfEntry? recapEntry, string? requestedArtifactView)
    {
        HashSet<string> availableViews = GetArtifactAudienceKinds(recapEntry?.Audience);
        if (!string.IsNullOrWhiteSpace(requestedArtifactView))
        {
            string normalizedRequestedView = requestedArtifactView.Trim().ToLowerInvariant();
            if (availableViews.Contains(normalizedRequestedView))
            {
                return normalizedRequestedView;
            }
        }

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

        foreach (string fallbackView in new[] { "campaign", "travel", "personal", "creator" })
        {
            if (availableViews.Contains(fallbackView))
            {
                return fallbackView;
            }
        }

        return "campaign";
    }

    private static string BuildArtifactShelfSelectionSummary(
        string selectedArtifactView,
        RecapShelfEntry? recapEntry,
        RecapShelfEntry? replayEntry,
        string offlinePrefetchSummary,
        string travelPosture)
        => selectedArtifactView switch
        {
            "travel" => $"Travel shelf: {travelPosture} {offlinePrefetchSummary}",
            "campaign" => $"Campaign shelf: {recapEntry?.Summary ?? "No shared campaign recap packet is attached yet."} {replayEntry?.Summary ?? "No replay-safe package is attached yet."}",
            "creator" => recapEntry?.Discoverable == true
                ? $"Published shelf: {recapEntry.PublicationSummary}"
                : $"Published shelf: {recapEntry?.NextSafeAction ?? "Review creator publication status before you widen the artifact audience."}",
            _ => $"My stuff shelf: {recapEntry?.OwnershipSummary ?? "No dossier-safe return lane is attached yet, but this view stays reserved for personal artifact truth."}"
        };

    private static string BuildSelectedRecapArtifactSummary(
        string selectedArtifactView,
        string? artifactId,
        RecapShelfEntry? recapEntry)
        => string.IsNullOrWhiteSpace(artifactId)
            ? "Selected recap artifact: no recap artifact is pinned yet."
            : recapEntry is null
                ? $"Selected recap artifact: {artifactId} is pinned on the {HumanizeArtifactShelfViewId(selectedArtifactView)}, but the recap-safe packet has not hydrated yet."
                : $"Selected recap artifact: {artifactId} keeps {recapEntry.Label} on the {HumanizeArtifactShelfViewId(selectedArtifactView)}.";

    private static string BuildSelectedRecapArtifactHref(
        string sessionId,
        PlaySurfaceRole role,
        string selectedArtifactView,
        string? artifactId)
        => string.IsNullOrWhiteSpace(artifactId)
            ? BuildArtifactShelfHref(sessionId, role, selectedArtifactView)
            : $"/artifacts/{Uri.EscapeDataString(sessionId)}/{Uri.EscapeDataString(artifactId)}?role={Uri.EscapeDataString(role.ToString())}&view={Uri.EscapeDataString(selectedArtifactView)}";

    private static PlayArtifactShelfViewLink[] BuildArtifactShelfViews(
        string sessionId,
        PlaySurfaceRole role,
        RecapShelfEntry? recapEntry,
        string selectedArtifactView,
        string offlinePrefetchSummary,
        string travelPosture)
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
                Href: BuildArtifactShelfHref(sessionId, role, "personal"),
                IsSelected: string.Equals(selectedArtifactView, "personal", StringComparison.Ordinal)),
            new(
                ViewId: "campaign",
                Label: "Campaign stuff",
                Summary: availableViews.Contains("campaign")
                    ? $"Browse the live campaign recap packet on the shared lane. {recapEntry?.Summary ?? "This view keeps the table-facing recap artifact attached to the current workspace."}"
                    : "No shared campaign recap packet is attached yet, but this view stays reserved for campaign artifact truth.",
                Href: BuildArtifactShelfHref(sessionId, role, "campaign"),
                IsSelected: string.Equals(selectedArtifactView, "campaign", StringComparison.Ordinal)),
            new(
                ViewId: "travel",
                Label: "Travel cache",
                Summary: $"Keep the bounded travel shelf tied to the same claimed-device packet. {travelPosture} {offlinePrefetchSummary}",
                Href: BuildArtifactShelfHref(sessionId, role, "travel"),
                IsSelected: string.Equals(selectedArtifactView, "travel", StringComparison.Ordinal)),
            new(
                ViewId: "creator",
                Label: "Published stuff",
                Summary: availableViews.Contains("creator")
                    ? recapEntry?.Discoverable == true
                        ? $"Browse the governed creator packet directly from the same recap-safe artifact. {recapEntry.PublicationSummary}"
                        : $"The same creator packet is still bounded until publication clears review. {recapEntry?.NextSafeAction ?? "Review creator publication status before you widen the audience."}"
                    : "No creator-safe packet is attached yet, but this view stays reserved for published artifact truth.",
                Href: BuildArtifactShelfHref(sessionId, role, "creator"),
                IsSelected: string.Equals(selectedArtifactView, "creator", StringComparison.Ordinal))
        ];
    }

    private static string HumanizeArtifactShelfView(string? audience, bool discoverable)
    {
        HashSet<string> availableViews = GetArtifactAudienceKinds(audience);
        if (discoverable && availableViews.Contains("creator"))
        {
            return "published shelf";
        }

        if (availableViews.Contains("travel"))
        {
            return "travel shelf";
        }

        if (availableViews.Contains("campaign"))
        {
            return "campaign shelf";
        }

        return "my stuff shelf";
    }

    private static string HumanizeArtifactShelfViewId(string selectedArtifactView)
        => selectedArtifactView switch
        {
            "travel" => "travel shelf",
            "campaign" => "campaign shelf",
            "creator" => "published shelf",
            _ => "my stuff shelf"
        };

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

        kinds.Add("travel");
        return kinds;
    }

    private static string BuildArtifactShelfHref(string sessionId, PlaySurfaceRole role, string viewId)
        => $"/artifacts/{Uri.EscapeDataString(sessionId)}?role={Uri.EscapeDataString(role.ToString())}&view={Uri.EscapeDataString(viewId)}";

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
        => serverPlane.RecapShelf.FirstOrDefault(static item => IsRecapEntry(item))
            ?? serverPlane.RecapShelf.FirstOrDefault();

    private static RecapShelfEntry? FindLeadReplayEntry(PlayCampaignWorkspaceServerPlane serverPlane)
        => serverPlane.RecapShelf.FirstOrDefault(IsReplayEntry);

    private static RecapShelfEntry? FindLeadPrimerEntry(PlayCampaignWorkspaceServerPlane serverPlane)
        => serverPlane.RecapShelf.FirstOrDefault(static item => item.Kind.Contains("primer", StringComparison.OrdinalIgnoreCase));

    private static RecapShelfEntry? FindLeadFirstSessionBriefingEntry(PlayCampaignWorkspaceServerPlane serverPlane)
        => serverPlane.RecapShelf.FirstOrDefault(static item => item.Kind.Contains("briefing", StringComparison.OrdinalIgnoreCase));

    private static bool IsRecapEntry(RecapShelfEntry item)
        => item.Kind.Contains("recap", StringComparison.OrdinalIgnoreCase);

    private static bool IsReplayEntry(RecapShelfEntry item)
        => item.Kind.Contains("replay", StringComparison.OrdinalIgnoreCase);

    private static string BuildStarterArtifactSummary(string prefix, RecapShelfEntry? entry, string fallback)
        => entry is null ? fallback : $"{prefix}: {entry.Summary}";

    private static string BuildStarterArtifactHref(
        string sessionId,
        PlaySurfaceRole role,
        RecapShelfEntry? entry,
        string selectedArtifactView)
        => string.IsNullOrWhiteSpace(entry?.ArtifactId)
            ? BuildArtifactShelfHref(sessionId, role, selectedArtifactView)
            : $"/artifacts/{Uri.EscapeDataString(sessionId)}/{Uri.EscapeDataString(entry.ArtifactId)}?role={Uri.EscapeDataString(role.ToString())}&view={Uri.EscapeDataString(selectedArtifactView)}";

    private static string BuildStarterArtifactContinuitySummary(
        RecapShelfEntry? primerEntry,
        RecapShelfEntry? firstSessionBriefingEntry,
        string travelPosture,
        string offlinePrefetchSummary)
    {
        string primer = primerEntry?.NextSafeAction ?? "No starter primer follow-through is attached yet.";
        string briefing = firstSessionBriefingEntry?.NextSafeAction ?? "No first-session briefing follow-through is attached yet.";
        return $"Starter continuity: {travelPosture} {offlinePrefetchSummary} Primer lane: {primer} Briefing lane: {briefing}";
    }

    private static string[] BuildStarterArtifactContinuityLabels(
        RecapShelfEntry? primerEntry,
        RecapShelfEntry? firstSessionBriefingEntry,
        string selectedArtifactView,
        string launchPrimerHref,
        string firstSessionBriefingHref)
    {
        List<string> labels =
        [
            $"Starter primer lane: {primerEntry?.Label ?? "No starter primer artifact is attached yet."}",
            $"Starter primer provenance: {primerEntry?.ProvenanceSummary ?? "No starter primer provenance is attached yet."}",
            $"Starter primer route: {launchPrimerHref}",
            $"First-session briefing lane: {firstSessionBriefingEntry?.Label ?? "No first-session briefing artifact is attached yet."}",
            $"First-session briefing provenance: {firstSessionBriefingEntry?.ProvenanceSummary ?? "No first-session briefing provenance is attached yet."}",
            $"First-session briefing route: {firstSessionBriefingHref}",
            $"Starter artifact shelf: selected view remains {selectedArtifactView} while primer and briefing stay on the same claimed-device shell."
        ];

        if (!string.IsNullOrWhiteSpace(primerEntry?.NextSafeAction))
        {
            labels.Add($"Starter primer next: {primerEntry.NextSafeAction}");
        }

        if (!string.IsNullOrWhiteSpace(firstSessionBriefingEntry?.NextSafeAction))
        {
            labels.Add($"First-session briefing next: {firstSessionBriefingEntry.NextSafeAction}");
        }

        return labels.ToArray();
    }

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

    private static string BuildPlayerTableCardsSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string latestTimeline,
        string roleLabel)
    {
        string initiativeCue = resume.Bootstrap.QuickActions.FirstOrDefault()?.Label
            ?? "Review-only posture while the next table cue is confirmed";
        string actionBudgetCue = resume.CachePressure.BackpressureActive
            ? "warning-only until cache pressure clears"
            : resume.RuntimeBundle is null || resume.Checkpoint is null
                ? "provisional until bundle proof and checkpoint truth are grounded"
                : "grounded on the current claimed-device packet";
        return $"Player table cards: Initiative cue: {latestTimeline}. Action budget cue: {initiativeCue}. Confidence cue: {actionBudgetCue}. This stays a mobile {roleLabel} receipt, not a second rules authority for {session.SceneId}.";
    }

    private static string[] BuildPlayerTableCardLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string latestTimeline,
        string roleLabel)
    {
        string initiativeLane = $"Initiative lane: '{latestTimeline}' is the current table cue on the {roleLabel}.";
        string actionBudgetLane = resume.Bootstrap.QuickActions.Count == 0
            ? $"Action-budget lane: no role-safe quick actions are exposed yet, so this shell stays review-first for {session.SceneId}."
            : $"Action-budget lane: {string.Join(", ", resume.Bootstrap.QuickActions.Select(action => action.Label))} stay visible as the next bounded table actions.";
        string conditionLane = resume.RuntimeBundle is null
            ? $"Condition/effect lane: reconnect {session.SceneId} once before you trust table-card condition carry-forward."
            : $"Condition/effect lane: bundle {resume.RuntimeBundle.BundleTag} keeps visible condition/effect carry-forward grounded on this mobile shell.";
        string receiptLane = resume.Checkpoint is null
            ? "Receipt lane: no claimed return anchor is pinned yet."
            : $"Receipt lane: checkpoint {resume.Checkpoint.AppliedThroughSequence} keeps the current table card attached to the install-local return anchor.";
        return [initiativeLane, actionBudgetLane, conditionLane, receiptLane];
    }

    private static string BuildBetweenTurnAffordancesSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel)
    {
        string nextAction = BuildSafeNextAction(resume, session);
        string reconnectCue = resume.Checkpoint is null
            ? "seed a claimed return anchor first"
            : $"return through checkpoint {resume.Checkpoint.AppliedThroughSequence}";
        return $"Between-turn affordances: {nextAction} Rejoin cue: {reconnectCue}. Support cue: keep escalation on one install-local route if the table stalls. This keeps the between-turn lane calm for the claimed {roleLabel}.";
    }

    private static string[] BuildBetweenTurnAffordanceLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel)
    {
        string readyLane = resume.Bootstrap.QuickActions.Count == 0
            ? $"Ready lane: no role-safe quick action is attached yet, so the between-turn pause stays review-first for the {roleLabel}."
            : $"Ready lane: {resume.Bootstrap.QuickActions.First().Label} remains the next one-tap action on this claimed shell.";
        string recapLane = $"Recap lane: keep replay-safe and recap-safe review on {resume.DeepLinkOwnerRoute}.";
        string travelLane = resume.RuntimeBundle is null || resume.Checkpoint is null
            ? $"Travel lane: hold safehouse handoff for {session.SceneId} until grounded continuity returns."
            : $"Travel lane: bounded safehouse handoff is ready after the next online sync for {session.SceneId}.";
        string supportLane = $"Support lane: {BuildSupportFollowThrough(resume, session)}";
        return [readyLane, recapLane, travelLane, supportLane];
    }

    private static string BuildGmLiteContinuitySummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        PlayCampaignWorkspaceServerPlane serverPlane,
        string roleLabel)
    {
        string rosterCue = serverPlane.Roster.Summary;
        string runboardCue = serverPlane.Runboard.ObjectiveSummary;
        string decisionCue = serverPlane.DecisionNotices.FirstOrDefault()?.Summary
            ?? serverPlane.NextSafeAction.Summary;
        return $"GM-lite continuity: {rosterCue} Runboard cue: {runboardCue} Decision cue: {decisionCue} This view keeps live combat confidence visible on the {roleLabel} without turning mobile into a second GM truth source.";
    }

    private static string[] BuildGmLiteContinuityLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        PlayCampaignWorkspaceServerPlane serverPlane,
        string roleLabel)
    {
        string initiativeLane = $"Initiative lane: {serverPlane.Runboard.ActiveSceneSummary}";
        string rosterLane = $"Roster lane: {serverPlane.Roster.Summary}";
        string objectiveLane = $"Objective lane: {serverPlane.Runboard.ObjectiveSummary}";
        string governanceLane = $"GM-lite lane: bounded to the mobile {roleLabel}; use {serverPlane.NextSafeAction.Summary.ToLowerInvariant()} before widening changes beyond {session.SceneId}.";
        return [initiativeLane, rosterLane, objectiveLane, governanceLane];
    }

    private static string BuildQuickExplainSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string latestTimeline,
        string roleLabel)
    {
        string checkpointSummary = resume.Checkpoint is null
            ? "no local return anchor is pinned yet"
            : $"checkpoint {resume.Checkpoint.AppliedThroughSequence} is the local return anchor";
        string bundleSummary = resume.RuntimeBundle is null
            ? "runtime bundle proof is still pending"
            : $"bundle {resume.RuntimeBundle.BundleTag} grounds the visible runtime values";
        return $"Quick explain: scene {session.SceneId}, revision {session.SceneRevision}, and '{latestTimeline}' are visible because the current scene packet, {checkpointSummary}, and {bundleSummary} are aligned for the {roleLabel}.";
    }

    private static string[] BuildQuickExplainLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string latestTimeline,
        string roleLabel)
    {
        string sceneValue = $"Scene value: {session.SceneId} comes from the active scene packet at revision {session.SceneRevision}.";
        string timelineValue = $"Latest signal: '{latestTimeline}' is the newest replay-safe timeline event on this shell.";
        string checkpointValue = resume.Checkpoint is null
            ? "Return anchor value: no checkpoint is pinned yet, so the shell stays review-first until this device seeds one."
            : $"Return anchor value: checkpoint {resume.Checkpoint.AppliedThroughSequence} is the install-local continuity anchor for this {roleLabel}.";
        string bundleValue = resume.RuntimeBundle is null
            ? $"Bundle value: runtime {session.RuntimeFingerprint} is loaded, but grounded bundle proof is still pending."
            : $"Bundle value: {resume.RuntimeBundle.BundleTag} is the grounded bundle proof for runtime {session.RuntimeFingerprint}.";
        return [sceneValue, timelineValue, checkpointValue, bundleValue];
    }

    private static string BuildSourceAnchorSummary(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        string checkpointSummary = resume.Checkpoint is null
            ? "checkpoint pending"
            : $"checkpoint {resume.Checkpoint.AppliedThroughSequence}";
        string bundleSummary = resume.RuntimeBundle is null
            ? "bundle proof pending"
            : $"bundle {resume.RuntimeBundle.BundleTag}";
        string route = string.IsNullOrWhiteSpace(resume.DeepLinkOwnerRoute)
            ? $"/play?sessionId={Uri.EscapeDataString(resume.SessionId)}&role={Uri.EscapeDataString(resume.Role.ToString())}"
            : resume.DeepLinkOwnerRoute!;
        return $"Source anchors: packet {session.SceneId}/{session.SceneRevision}, sequence {resume.Bootstrap.Projection.Cursor.AppliedThroughSequence}, {checkpointSummary}, {bundleSummary}, and owner route {route}.";
    }

    private static string[] BuildSourceAnchorLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string latestTimeline)
    {
        string scenePacket = $"Scene packet anchor: {session.SceneId} · {session.SceneRevision}.";
        string timelinePacket = $"Replay anchor: '{latestTimeline}' stays attached to applied sequence {resume.Bootstrap.Projection.Cursor.AppliedThroughSequence}.";
        string runtimeAnchor = $"Runtime anchor: {session.RuntimeFingerprint}.";
        string returnAnchor = resume.Checkpoint is null
            ? "Return anchor: pending until this device captures its first local checkpoint."
            : $"Return anchor: checkpoint {resume.Checkpoint.AppliedThroughSequence} on {resume.Checkpoint.SceneRevision}.";
        string bundleAnchor = resume.RuntimeBundle is null
            ? "Bundle anchor: reconnect once to ground a local runtime bundle."
            : $"Bundle anchor: {resume.RuntimeBundle.BundleTag} validated at {resume.RuntimeBundle.LastValidatedAtUtc:yyyy-MM-dd HH:mm} UTC.";
        return [scenePacket, timelinePacket, runtimeAnchor, returnAnchor, bundleAnchor];
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

    private static string BuildRunnerGoalUpdatesSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        PlayCampaignWorkspaceServerPlane serverPlane,
        string roleLabel,
        string latestTimeline)
    {
        string checkpointSummary = resume.Checkpoint is null
            ? "checkpoint pending"
            : $"checkpoint {resume.Checkpoint.AppliedThroughSequence}";
        string supportCue = serverPlane.SupportClosures.FirstOrDefault()?.NextSafeAction
            ?? "support follow-through remains available if goal-return posture drifts";
        return $"Runner goal updates: {serverPlane.Workspace.CampaignName} keeps one goal-update lane on '{latestTimeline}' with {checkpointSummary} for the {roleLabel}. Return moments stay player-safe, install-local, and tied to {supportCue}.";
    }

    private static string[] BuildRunnerGoalUpdateLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        PlayCampaignWorkspaceServerPlane serverPlane,
        string roleLabel,
        string latestTimeline)
    {
        string checkpointLane = resume.Checkpoint is null
            ? $"Goal checkpoint lane: {session.SceneId} still needs a local checkpoint before you trust a goal-update return moment."
            : $"Goal checkpoint lane: checkpoint {resume.Checkpoint.AppliedThroughSequence} anchors goal-update return posture for the {roleLabel}.";
        string signalLane = $"Goal signal lane: '{latestTimeline}' is the newest replay-safe cue attached to the runner-goal return moment.";
        string routeLane = $"Goal route lane: {serverPlane.Workspace.ReturnSummary}";
        string boundaryLane = resume.RuntimeBundle is null
            ? "Goal boundary lane: reconnect once before you trust goal-update follow-through beyond review-only posture."
            : $"Goal boundary lane: bundle {resume.RuntimeBundle.BundleTag} keeps goal-update copy grounded without inventing a second campaign authority.";
        return [checkpointLane, signalLane, routeLane, boundaryLane];
    }

    private static string BuildPlayerSafeConsequenceFeedSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        PlayCampaignWorkspaceServerPlane serverPlane,
        string roleLabel,
        string latestTimeline)
    {
        string consequenceSource = resume.RuntimeBundle is null
            ? "player-safe consequence copy is still review-first until bundle proof is grounded"
            : $"bundle {resume.RuntimeBundle.BundleTag} keeps the visible consequence feed grounded on this shell";
        return $"Player-safe consequence feed: {serverPlane.Workspace.CampaignName} turns '{latestTimeline}' into one bounded consequence cue for the {roleLabel}. {consequenceSource}, spoilers stay bounded, and the feed remains a view instead of BLACK LEDGER world truth.";
    }

    private static string[] BuildPlayerSafeConsequenceFeedLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        PlayCampaignWorkspaceServerPlane serverPlane,
        string roleLabel,
        string latestTimeline)
    {
        string feedLane = $"Consequence lane: '{latestTimeline}' is the visible player-safe feed item for {serverPlane.Workspace.CampaignName}.";
        string spoilerLane = "Spoiler lane: only player-safe consequence copy is shown here; approval, world-state authority, and operator-only detail stay outside mobile.";
        string returnLane = $"Return lane: {serverPlane.NextSafeAction.Summary}";
        string trustLane = resume.RuntimeBundle is null
            ? $"Trust lane: reconnect {session.SceneId} before you treat this consequence feed as a grounded return cue."
            : $"Trust lane: {roleLabel} consequence copy stays grounded by bundle {resume.RuntimeBundle.BundleTag} and checkpoint {resume.Checkpoint?.AppliedThroughSequence.ToString() ?? "pending"}.";
        return [feedLane, spoilerLane, returnLane, trustLane];
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

    private static string BuildStaleStatePosture(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel)
    {
        if (resume.CachePressure.BackpressureActive)
        {
            return $"Stale-state posture: warning for the {roleLabel} because cache pressure is active ({resume.CachePressure.RuntimeBundleCount}/{resume.CachePressure.RuntimeBundleQuota}) and already touched {resume.CachePressure.EvictedEntryCount} session(s).";
        }

        if (resume.RuntimeBundle is null || resume.Checkpoint is null)
        {
            return $"Stale-state posture: reconnect-first for the {roleLabel}; this shell still needs {(resume.RuntimeBundle is null ? "grounded bundle proof" : "a local checkpoint")} before stale-safe continuity can be trusted on {session.SceneId}.";
        }

        return $"Stale-state posture: green for the {roleLabel}; checkpoint {resume.Checkpoint.AppliedThroughSequence} and bundle {resume.RuntimeBundle.BundleTag} keep stale-safe continuity grounded on {session.SceneId}.";
    }

    private static string BuildActionRequiredSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel)
    {
        if (resume.RuntimeBundle is null && resume.Checkpoint is null)
        {
            return $"Action required: reconnect {session.SceneId} to ground both bundle proof and a local checkpoint before this {roleLabel} can carry continuity-critical campaign state.";
        }

        if (resume.RuntimeBundle is null)
        {
            return $"Action required: reconnect {session.SceneId} so this {roleLabel} caches grounded bundle proof before you trust stale-safe campaign mutations.";
        }

        if (resume.Checkpoint is null)
        {
            return $"Action required: seed a local checkpoint for {session.SceneId} before you trust this {roleLabel} as the return anchor for travel or safehouse continuity.";
        }

        if (resume.CachePressure.BackpressureActive)
        {
            return $"Action required: clear cache pressure on this {roleLabel} before you trust stale campaign state or pin more travel continuity on {session.SceneId}.";
        }

        return $"Action required: keep mutations on the claimed {roleLabel}, then finish the next online sync before you widen {session.SceneId} beyond bounded install-local continuity.";
    }

    private static string[] BuildActionRequiredLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel)
    {
        string grounding = resume.RuntimeBundle is null && resume.Checkpoint is null
            ? $"Action-required lane: reconnect {session.SceneId} to ground bundle proof plus checkpoint truth on this {roleLabel}."
            : resume.RuntimeBundle is null
                ? $"Action-required lane: cache grounded bundle proof for {session.SceneId} before you trust continuity-critical state."
                : resume.Checkpoint is null
                    ? $"Action-required lane: seed a local checkpoint before this {roleLabel} becomes the return anchor."
                    : $"Action-required lane: keep continuity mutations on the claimed {roleLabel} and sync them before cross-device fan-out.";
        string staleHandling = resume.CachePressure.BackpressureActive
            ? $"Stale-action lane: clear cache pressure ({resume.CachePressure.RuntimeBundleCount}/{resume.CachePressure.RuntimeBundleQuota}) before you trust stale campaign state on this device."
            : "Stale-action lane: no extra stale-state remediation is required right now.";
        string travelHandling = resume.RuntimeBundle is null || resume.Checkpoint is null
            ? $"Travel-action lane: hold travel and safehouse propagation for {session.SceneId} until this device regrounds continuity."
            : "Travel-action lane: bounded travel continuity is ready, but final handoff propagation still stays online-only.";

        return [grounding, staleHandling, travelHandling];
    }

    private static string BuildMobileCampaignCurrentState(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel,
        string continuityPosture,
        string travelPosture)
        => $"Current continuity posture: {continuityPosture} Travel lane: {travelPosture} This mobile shell stays the claimed {roleLabel} for {session.SceneId}.";

    private static string BuildMobileCampaignStateSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel,
        string offlinePrefetchSummary,
        string cachePosture)
        => $"Cached state: {cachePosture} {offlinePrefetchSummary} This keeps stale-safe campaign continuity explicit on the claimed {roleLabel} for {session.SceneId}.";

    private static string BuildMobileCampaignCachedState(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel,
        IReadOnlyList<string> offlineTruthLabels)
    {
        string cachedLabel = offlineTruthLabels.FirstOrDefault(static item => item.Contains("Cached lane:", StringComparison.Ordinal))
            ?? $"Cached lane: no bounded cached campaign packet is attached to {session.SceneId} yet.";
        return $"Cached state: {cachedLabel} Mobile shell owner: {roleLabel}.";
    }

    private static string BuildMobileCampaignStaleState(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel,
        string staleStatePosture)
        => $"Stale state: {staleStatePosture} This keeps stale campaign posture explicit before {roleLabel} mutations continue on {session.SceneId}.";

    private static string BuildMobileCampaignActionRequired(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel,
        string actionRequiredSummary)
    {
        string normalized = actionRequiredSummary.StartsWith("Action required:", StringComparison.Ordinal)
            ? actionRequiredSummary
            : $"Action required: {actionRequiredSummary}";
        return $"{normalized} Mobile shell owner: {roleLabel}. Session: {session.SceneId}.";
    }

    private static string[] BuildMobileCampaignStateLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel,
        string mobileCampaignCurrentState,
        IReadOnlyList<string> offlineTruthLabels,
        IReadOnlyList<string> actionRequiredLabels)
    {
        List<string> labels =
        [
            $"Current lane: {mobileCampaignCurrentState}",
            .. offlineTruthLabels,
            .. actionRequiredLabels
        ];

        return labels
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
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

    private static string BuildGroundedFollowUpSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel,
        string safeNextAction,
        string updateFollowThrough,
        string supportFollowThrough)
    {
        string route = string.IsNullOrWhiteSpace(resume.DeepLinkOwnerRoute)
            ? $"/play?sessionId={Uri.EscapeDataString(resume.SessionId)}&role={Uri.EscapeDataString(resume.Role.ToString())}"
            : resume.DeepLinkOwnerRoute!;
        return $"Grounded follow-up: stay on {route} for the next stale-protected {roleLabel} step. Next: {safeNextAction} Text-first fallback stays bounded to update posture ({updateFollowThrough}) and support posture ({supportFollowThrough}) instead of widening into a new control surface for {session.SceneId}.";
    }

    private static string[] BuildGroundedFollowUpLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel,
        string safeNextAction,
        string updateFollowThrough,
        string supportFollowThrough,
        string roleFollowThrough,
        IReadOnlyList<string> followThroughLabels)
    {
        string continueLane = $"Continue lane: {safeNextAction}";
        string roleLane = $"Role lane: {roleFollowThrough}";
        string updateLane = $"Update lane: {updateFollowThrough}";
        string supportLane = $"Support lane: {supportFollowThrough}";
        string boundaryLane = $"Boundary lane: keep follow-up text-first, packet-backed, and bounded to {session.SceneId} on the claimed {roleLabel}.";
        return new[] { continueLane, roleLane, updateLane, supportLane, boundaryLane }
            .Concat(followThroughLabels.Take(2))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
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
