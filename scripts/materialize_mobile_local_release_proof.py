#!/usr/bin/env python3
from __future__ import annotations

import datetime as dt
import json
import sys
from copy import deepcopy
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CHECKOUT_ROOT = str(ROOT)
REPO_LABEL = "chummer6-mobile"
REGRESSION_SOURCE = ROOT / "src" / "Chummer.Play.RegressionChecks" / "Program.cs"
WEB_SOURCE = ROOT / "src" / "Chummer.Play.Web" / "wwwroot" / "index.html"
MIGRATION_MAP = ROOT / "docs" / "migration-map.md"
PLAY_SIGNOFF = ROOT / "docs" / "PLAY_RELEASE_SIGNOFF.md"
M112_PROOF_DOC = ROOT / "docs" / "next90-m112-mobile-campaign-continuity.proof.md"
M112_VERIFIER = ROOT / "scripts" / "verify_next90_m112_mobile_campaign_continuity.py"
M119_PROOF_DOC = ROOT / "docs" / "next90-m119-mobile-onboarding-continuity.proof.md"
M119_VERIFIER = ROOT / "scripts" / "verify_next90_m119_mobile_onboarding_continuity.py"
M117_PROOF_DOC = ROOT / "docs" / "next90-m117-mobile-artifact-shelf.proof.md"
M117_VERIFIER = ROOT / "scripts" / "verify_next90_m117_mobile_artifact_shelf.py"
M117_SUCCESSOR_FRONTIER_ID = "3440617449"
M117_ACTIVE_FLAGSHIP_FRONTIER_ID = "3371889980"
M117_FRONTIER_IDS = [M117_SUCCESSOR_FRONTIER_ID, M117_ACTIVE_FLAGSHIP_FRONTIER_ID]
M121_PROOF_DOC = ROOT / "docs" / "next90-m121-mobile-live-combat-confidence.proof.md"
M121_VERIFIER = ROOT / "scripts" / "verify_next90_m121_mobile_live_combat_confidence.py"
M122_PROOF_DOC = ROOT / "docs" / "next90-m122-mobile-runner-goal-updates-and-consequence-feed.proof.md"
M122_VERIFIER = ROOT / "scripts" / "verify_next90_m122_mobile_runner_goal_updates.py"
M145_PROOF_DOC = ROOT / "docs" / "next90-m145-mobile-quick-explain-and-follow-up.proof.md"
M145_VERIFIER = ROOT / "scripts" / "verify_next90_m145_mobile_quick_explain_and_follow_up.py"
OUT = ROOT / ".codex-studio" / "published" / "MOBILE_LOCAL_RELEASE_PROOF.generated.json"
M112_ACTIVE_FLAGSHIP_FRONTIER_ID = "1033794907"

REQUIRED_MARKERS = {
    "install_claim_restore_continue": [
        "VerifyRoamingWorkspaceRestorePlanRestoresPackageOwnedCampaignState",
        'Assert(plan.ResumeFollowThrough.Contains("Resume Redmond Patrol"',
        'Assert(plan.ResumeFollowThroughHref.Contains("/play/session-redmond"',
        'Assert(plan.SupportFollowThroughHref.Contains("/contact"',
        'Assert(plan.SupportFollowThroughHref.Contains("campaignId=campaign-redmond"',
        'Assert(projection.SupportFollowThroughHref.Contains("kind=install_help"',
        'Assert(projection.SupportFollowThroughHref.Contains("sessionId=session-redmond"',
    ],
    "campaign_session_recover_recap": [
        "VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary",
        "VerifyCachePressureDecisionNoticeUsesSupportNextActionCopy",
        "VerifyIndexShellBindsContextualActionLabelsAsync",
        "VerifyPlayRoamingRestoreServiceProjectsClaimedDeviceRecovery",
        'Assert(projection.ContinuityRailSummary.Contains("Downtime:", StringComparison.Ordinal)',
        'Assert(projection.ContinuityRailSummary.Contains("Diary:", StringComparison.Ordinal)',
        'Assert(projection.ContinuityRailSummary.Contains("Contacts:", StringComparison.Ordinal)',
        'Assert(projection.ContinuityRailSummary.Contains("Heat:", StringComparison.Ordinal)',
        'Assert(projection.ContinuityRailSummary.Contains("Aftermath:", StringComparison.Ordinal)',
        'Assert(projection.ContinuityRailSummary.Contains("Return:", StringComparison.Ordinal)',
        'Assert(projection.GmOperationsSummary.Contains("Opposition:", StringComparison.Ordinal)',
        'Assert(projection.GmOperationsSummary.Contains("Roster movement:", StringComparison.Ordinal)',
        'Assert(projection.GmOperationsSummary.Contains("Prep library:", StringComparison.Ordinal)',
        'Assert(projection.GmOperationsSummary.Contains("Event controls:", StringComparison.Ordinal)',
        'Assert(projection.GmOperationsSummary.Contains("audit-visible", StringComparison.Ordinal)',
        'Assert(projection.GmOperationsSummary.Contains("support-linked", StringComparison.Ordinal)',
        'Assert(projection.GmOperationsLabels.Any(item => item.Contains("Opposition lane:", StringComparison.Ordinal))',
        'Assert(projection.GmOperationsLabels.Any(item => item.Contains("Roster movement lane:", StringComparison.Ordinal))',
        'Assert(projection.GmOperationsLabels.Any(item => item.Contains("Prep library lane:", StringComparison.Ordinal))',
        'Assert(projection.GmOperationsLabels.Any(item => item.Contains("Event controls lane:", StringComparison.Ordinal))',
        'Assert(projection.GmOperationsLabels.Any(item => item.Contains("Governance lane:", StringComparison.Ordinal))',
        'Assert(projection.OfflineTruthSummary.Contains("Stale:", StringComparison.Ordinal)',
        'Assert(projection.OfflineTruthSummary.Contains("Can do now:", StringComparison.Ordinal)',
        'Assert(projection.OfflineTruthSummary.Contains("safehouse", StringComparison.OrdinalIgnoreCase)',
        'Assert(projection.OfflineTruthSummary.Contains("Needs online:", StringComparison.Ordinal)',
        'Assert(projection.OfflineTruthLabels.Any(item => item.Contains("Stale lane:", StringComparison.Ordinal))',
        'Assert(projection.OfflineTruthLabels.Any(item => item.Contains("Can-do-now lane:", StringComparison.Ordinal))',
        'Assert(projection.OfflineTruthLabels.Any(item => item.Contains("Needs-online lane:", StringComparison.Ordinal))',
        'Assert(projection.OfflineTruthLabels.Any(item => item.Contains("safehouse", StringComparison.OrdinalIgnoreCase))',
        'Assert(plan.TravelCompanionSummary.Contains("Cached:", StringComparison.Ordinal)',
        'Assert(plan.TravelCompanionSummary.Contains("Stale:", StringComparison.Ordinal)',
        'Assert(plan.TravelCompanionSummary.Contains("Offline actions:", StringComparison.Ordinal)',
        'Assert(plan.TravelCompanionLabels.Any(item => item.Contains("Cached lane:", StringComparison.Ordinal))',
        'Assert(plan.TravelCompanionLabels.Any(item => item.Contains("Stale lane:", StringComparison.Ordinal))',
        'Assert(plan.TravelCompanionLabels.Any(item => item.Contains("Offline action lane:", StringComparison.Ordinal))',
        'Assert(plan.SafeNextAction.Contains("Open scene-redmond mobile return"',
        'Assert(projection.ChangePacketSummary.Contains("Return anchor stays on checkpoint 12"',
        'Assert(projection.ChangePacketLabels.Any(item => item.Contains("Scene packet: scene-redmond"',
        'Assert(projection.RolePosture.Contains("player lane"',
        'Assert(projection.RoleFollowThroughHref.Contains("/play/session-redmond?role=Player"',
        'Assert(projection.DecisionNotice.Contains("Use the current bundle proof for scene-redmond"',
        'setLink("workspace-decision-notice-link", payload.decisionNoticeHref, payload.decisionNotice, "/", "Decision notice follow-through");',
        'setLink("follow-through-update-link", payload.updateFollowThroughHref, payload.updateFollowThrough, "/downloads", "Update follow-through");',
        'setLink("follow-through-support-link", payload.supportFollowThroughHref, payload.supportFollowThrough, "/contact", "Support follow-through");',
        'setLink("follow-through-role-link", payload.roleFollowThroughHref, payload.roleFollowThrough, "/", "Role follow-through");',
        'setLink("restore-follow-through-link", payload.resumeFollowThroughHref, payload.resumeFollowThrough, "/play", "Claimed-device follow-through");',
        'setLink("restore-support-follow-through-link", payload.supportFollowThroughHref, payload.supportFollowThrough, "/contact", "Restore support follow-through");',
        'const explicitDeviceId = params.get("deviceId") || "";',
        'const resolvedDeviceId = resolveStableDeviceId(role, explicitDeviceId);',
        'const observerId = resolveObserverId();',
        'const observeResponse = await fetch(`/api/play/observe/${encodeURIComponent(sessionId)}`);',
        'fetch("/api/play/continuity/claim", {',
        'renderContinuityClaimStatus({',
        'document.getElementById("shell-continuity-claim-button").addEventListener("click", claimContinuityOnThisDevice);',
        'id="shell-continuity-claim-status"',
        'id="shell-owner-route-link"',
        'id="shell-continuity-claim-button"',
        'id="change-packet-summary"',
        'id="change-packet-list"',
        'id="workspace-continuity-rail"',
        'id="workspace-continuity-rail-list"',
        'id="workspace-gm-ops"',
        'id="workspace-gm-ops-list"',
        'id="workspace-offline-truth"',
        'id="workspace-offline-truth-list"',
        'id="workspace-role"',
        'id="workspace-update"',
        'id="workspace-support"',
        'id="workspace-support-status"',
        'id="workspace-known-issue"',
        'id="workspace-fix-state"',
        'id="restore-follow-through"',
        'id="restore-offline-truth"',
        'id="restore-offline-truth-labels"',
        'id="restore-travel-companion"',
        'id="restore-travel-companion-labels"',
        'id="restore-prefetch-labels"',
    ],
    "recover_from_sync_conflict": [
        "VerifyRoamingWorkspaceRestorePlanPreservesConflictAndInstallLocalGuardrails",
        "Assert(plan.RequiresConflictReview",
        'Assert(plan.ResumeFollowThrough.Contains("restore review"',
        'Assert(plan.SupportFollowThroughHref.Contains("different%20channels"',
        'Assert(plan.AttentionItems.Any(item => item.Contains("different channels"',
        'id="restore-support-follow-through"',
    ],
    "quality_release_hardening": [
        "VerifyIndexShellAccessibilityContractAsync",
        "VerifyCachePressureBudgetContractAsync",
        "VerifyCachePressureDecisionNoticeUsesSupportNextActionCopy",
        "VerifyOfflineQueueRejectsMalformedSessionEnvelopeAsync",
        "VerifyOfflineQueueRejectsStaleLineageAsync",
        "VerifySyncPrefixAcknowledgementAsync",
        "VerifyResumePreservesLedgerWhenRuntimeBundleDriftsAsync",
        "VerifyResumeNormalizesCheckpointToLedgerLineageAsync",
        "VerifyReconnectLineageTransitionContinuityAsync",
        "VerifyResumeAndWorkspaceLiteRoutesStayRoleConcreteAsync",
        "VerifyRuntimeBundleSessionLockReleasesOnCanceledAcquireAsync",
        '<html lang="en">',
        'role="status" aria-live="polite" aria-atomic="true"',
        "navigator.serviceWorker.register",
        "Post-closure completion criteria (M12)",
        "Post-closure hardening criteria (M13)",
        "Post-closure role-depth criteria (M14)",
        "Release-proof cadence criteria:",
    ],
    "migration_boundary_evidence": [
        "`Chummer.Session.Web/Program.cs` -> `src/Chummer.Play.Web/Program.cs`",
        "`Chummer.Session.Web/BrowserSessionApiClient.cs` -> `src/Chummer.Play.Web/BrowserSessionApiClient.cs`",
        "`Chummer.Session.Web` bootstrap/sync/session DTOs -> `Chummer.Play.Contracts` package-owned play transport surface",
        "replace old `Chummer.Presentation` project references with package-only dependencies",
        "preserve local-first event log, runtime bundle, and offline cache ownership here",
        "keep DTO canon in shared packages instead of introducing repo-local copies",
    ],
    "mobile_campaign_continuity": [
        "Mobile campaign continuity criteria (M112)",
        "next90-m112-mobile-campaign-continuity",
        'Assert(projection.MobileCampaignCurrentState.Contains("Current continuity posture:", StringComparison.Ordinal)',
        'Assert(projection.MobileCampaignStateSummary.Contains("Cached state:", StringComparison.Ordinal)',
        'Assert(projection.MobileCampaignCachedState.Contains("Cached state:", StringComparison.Ordinal)',
        'Assert(projection.MobileCampaignStaleState.Contains("Stale state:", StringComparison.Ordinal)',
        'Assert(projection.MobileCampaignActionRequired.Contains("Action required:", StringComparison.Ordinal)',
        'Assert(projection.MobileCampaignActionRequired.Contains("Mobile shell owner: player lane. Session: scene-redmond.", StringComparison.Ordinal)',
        'Assert(projection.MobileCampaignStateLabels.Any(item => item.Contains("Cached lane:", StringComparison.Ordinal))',
        'Assert(projection.MobileCampaignStateLabels.Any(item => item.Contains("Action-required lane:", StringComparison.Ordinal))',
        'Assert(plan.TravelCampaignCurrentState.Contains("Current continuity posture:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignStateSummary.Contains("Cached state:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignCachedState.Contains("Cached state:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignStaleState.Contains("Stale state:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignActionRequired.Contains("Action required:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignActionRequired.Contains("Travel lane: play_tablet on install-tablet.", StringComparison.Ordinal)',
        'Assert(postFailureProjection.EntryStateSummary.Contains("cached, stale, and action-required travel continuity", StringComparison.Ordinal)',
        'Assert(postFailureProjection.RecoveryActions.Any(item => item.Contains("Travel continuity cached state:", StringComparison.Ordinal))',
        'Assert(postFailureProjection.RecoveryActions.Any(item => item.Contains("Travel continuity stale state:", StringComparison.Ordinal))',
        'Assert(postFailureProjection.RecoveryActions.Any(item => item.Contains("Travel continuity action required:", StringComparison.Ordinal))',
        'Assert(plan.TravelCampaignStateLabels.Any(item => item.Contains("Cached lane:", StringComparison.Ordinal))',
        'Assert(plan.TravelCampaignStateLabels.Any(item => item.Contains("Travel companion lane:", StringComparison.Ordinal))',
        'setText("workspace-mobile-campaign-current-state", payload.mobileCampaignCurrentState, "No mobile campaign continuity posture is available yet.");',
        'setText("workspace-mobile-campaign-state", payload.mobileCampaignStateSummary, "No mobile campaign-state summary is available yet.");',
        'setList("workspace-mobile-campaign-state-list", payload.mobileCampaignStateLabels);',
        'setText("workspace-action-required", payload.actionRequiredSummary, "No action-required summary is available yet.");',
        'setList("workspace-action-required-list", payload.actionRequiredLabels);',
        'setText("workspace-mobile-campaign-cached-state", payload.mobileCampaignCachedState, "No mobile cached campaign state is available yet.");',
        'setText("workspace-mobile-campaign-stale-state", payload.mobileCampaignStaleState, "No mobile stale campaign state is available yet.");',
        'setText("workspace-mobile-campaign-action-required", payload.mobileCampaignActionRequired, "No mobile campaign action-required posture is available yet.");',
        'setText("restore-travel-campaign-current-state", payload.travelCampaignCurrentState, "No restore travel campaign continuity posture is available yet.");',
        'setText("restore-travel-campaign-state", payload.travelCampaignStateSummary, "No restore travel campaign-state summary is available yet.");',
        'setList("restore-travel-campaign-state-labels", payload.travelCampaignStateLabels);',
        'setText("restore-action-required", payload.actionRequiredSummary, "No restore action-required summary is available yet.");',
        'setList("restore-action-required-labels", payload.actionRequiredLabels);',
        'setText("restore-travel-campaign-cached-state", payload.travelCampaignCachedState, "No restore travel cached campaign state is available yet.");',
        'setText("restore-travel-campaign-stale-state", payload.travelCampaignStaleState, "No restore travel stale campaign state is available yet.");',
        'setText("restore-travel-campaign-action-required", payload.travelCampaignActionRequired, "No restore travel action-required posture is available yet.");',
        'if (lowered.includes("action required") || lowered.includes("action-required")) {',
        'function syncContinuityCardTone(cardId, currentStateId, summaryId, currentStateText, actionId)',
        'const actionState = actionId ? (document.getElementById(actionId).textContent || "") : "";',
        'card.dataset.tone = inferContinuityTone(actionState || currentStateText || currentState || summary);',
        'const continuityTone = inferContinuityTone(`${payload.mobileCampaignActionRequired || ""} ${payload.mobileCampaignStaleState || ""}`);',
        'document.getElementById("shell-continuity-status").dataset.tone = continuityTone;',
        '`${payload.mobileCampaignStaleState || ""} ${payload.mobileCampaignCurrentState || ""}`',
        '`${payload.travelCampaignStaleState || ""} ${payload.travelCampaignCurrentState || ""}`',
        'syncContinuityStateBreakdown(\n      "workspace-mobile-campaign-cached-state",',
        'syncContinuityStateBreakdown(\n      "restore-travel-campaign-cached-state",',
        'id="workspace-mobile-campaign-card"',
        'id="workspace-mobile-campaign-current-state"',
        'id="workspace-mobile-campaign-state"',
        'id="restore-travel-campaign-card"',
        'id="restore-travel-campaign-current-state"',
        'id="restore-travel-campaign-state"',
        'id="workspace-mobile-campaign-cached-state"',
        'id="workspace-mobile-campaign-stale-state"',
        'id="workspace-mobile-campaign-action-required"',
        'id="workspace-mobile-campaign-state-list"',
        'id="restore-travel-campaign-cached-state"',
        'id="restore-travel-campaign-stale-state"',
        'id="restore-travel-campaign-action-required"',
        'id="restore-travel-campaign-state-labels"',
        'campaign_memory:travel',
        'campaign_state:mobile',
        "scripts/verify_next90_m112_mobile_campaign_continuity.py",
    ],
    "mobile_onboarding_continuity": [
        "Mobile onboarding continuity criteria (M119)",
        "next90-m119-mobile-onboarding-continuity",
        'Assert(projection.LaunchPrimerSummary.Contains("Starter primer:", StringComparison.Ordinal)',
        'Assert(projection.LaunchPrimerHref == "/artifacts/session-redmond/artifact%3Asession-redmond%3Astarter-primer?role=Player&view=personal"',
        'Assert(projection.FirstSessionBriefingSummary.Contains("First-session briefing:", StringComparison.Ordinal)',
        'Assert(projection.FirstSessionBriefingHref == "/artifacts/session-redmond/artifact%3Asession-redmond%3Afirst-session-briefing?role=Player&view=travel"',
        'Assert(projection.StarterArtifactContinuitySummary.Contains("Starter continuity:", StringComparison.Ordinal)',
        'Assert(projection.StarterArtifactContinuityLabels.Any(item => item.Contains("Starter primer lane:", StringComparison.Ordinal))',
        'Assert(plan.StarterPrimerFollowThrough.Contains("starter primer", StringComparison.OrdinalIgnoreCase)',
        'Assert(plan.StarterPrimerFollowThroughHref.Contains("artifact%3Asession-redmond%3Astarter-primer", StringComparison.Ordinal)',
        'Assert(plan.StarterPrimerFollowThroughHref.Contains("deviceId=install-tablet", StringComparison.Ordinal)',
        'Assert(plan.FirstSessionBriefingFollowThrough.Contains("first-session briefing", StringComparison.OrdinalIgnoreCase)',
        'Assert(plan.FirstSessionBriefingFollowThroughHref.Contains("artifact%3Asession-redmond%3Afirst-session-briefing", StringComparison.Ordinal)',
        'Assert(plan.FirstSessionBriefingFollowThroughHref.Contains("view=travel", StringComparison.Ordinal)',
        'Assert(noSessionProjection.RecommendedActionLabel.Contains("starter primer", StringComparison.OrdinalIgnoreCase)',
        'Assert(noCampaignProjection.RecommendedActionHref.Contains("starter-primer", StringComparison.OrdinalIgnoreCase)',
        'Assert(restorePlanTravel.StarterPrimerFollowThroughHref.Contains($"deviceId={Uri.EscapeDataString(expectedDeviceId)}", StringComparison.Ordinal)',
        'Assert(restorePlanTravel.FirstSessionBriefingFollowThroughHref.Contains("view=travel", StringComparison.Ordinal)',
        'setText("workspace-launch-primer", payload.launchPrimerSummary, "No starter primer summary is available yet.");',
        'setLink("workspace-launch-primer-link", payload.launchPrimerHref, "Open starter primer", "/artifacts", "Open starter primer");',
        'setText("workspace-first-session-briefing", payload.firstSessionBriefingSummary, "No first-session briefing summary is available yet.");',
        'setLink("workspace-first-session-briefing-link", payload.firstSessionBriefingHref, "Open first-session briefing", "/artifacts", "Open first-session briefing");',
        'setText("workspace-starter-artifact-continuity", payload.starterArtifactContinuitySummary, "No starter artifact continuity summary is available yet.");',
        'setText("restore-starter-primer-follow-through", payload.starterPrimerFollowThrough, "No travel starter-primer follow-through is available yet.");',
        'setText("restore-first-session-briefing-follow-through", payload.firstSessionBriefingFollowThrough, "No travel first-session briefing follow-through is available yet.");',
        "starter_onboarding:mobile",
        "first_session_briefing:mobile",
        "scripts/verify_next90_m119_mobile_onboarding_continuity.py",
    ],
    "mobile_artifact_shelf": [
        "Mobile artifact shelf criteria (M117)",
        "next90-m117-mobile-artifact-shelf",
        'Assert(projection.ArtifactShelfSelectionSummary.Contains("My stuff shelf:", StringComparison.Ordinal)',
        'Assert(projection.ArtifactShelfViews.Count == 4',
        'Assert(projection.ArtifactShelfViews.Any(item => item.ViewId == "travel" && item.Label == "Travel cache" && item.Href == "/artifacts/session-redmond?role=Player&view=travel")',
        'Assert(travelProjection.SelectedArtifactView == "travel"',
        'Assert(travelProjection.ArtifactShelfSelectionSummary.Contains("Travel shelf:", StringComparison.Ordinal)',
        'Assert(observerProjection.ArtifactShelfViews.Any(item => item.ViewId == "travel" && item.Href.Contains("role=Observer", StringComparison.Ordinal))',
        'Assert(gmProjection.ArtifactShelfViews.Any(item => item.ViewId == "travel" && item.Href.Contains("role=GameMaster", StringComparison.Ordinal))',
        'Assert(recapProjection.SelectedRecapArtifactSummary.Contains("travel shelf", StringComparison.OrdinalIgnoreCase)',
        'setText("workspace-artifact-shelf-summary", payload.artifactShelfSelectionSummary, "No mobile artifact shelf summary is available yet.");',
        'setText("workspace-artifact-selection", payload.selectedRecapArtifactSummary, "Selected recap artifact: no recap artifact is pinned yet.");',
        'setLink("workspace-artifact-selection-link", payload.selectedRecapArtifactHref || selectedArtifactView?.href, artifactId',
        'document.getElementById("workspace-artifact-shelf-link").href = selectedArtifactView?.href || "/artifacts";',
        '? `Browse ${selectedArtifactView.label}`',
        'const artifactView = params.get("artifactView") || "";',
        'const artifactId = params.get("artifactId") || "";',
        'workspaceQuery.set("artifactView", artifactView);',
        'workspaceQuery.set("artifactId", artifactId);',
        'renderWorkspace(payload, artifactId);',
        'Assert(artifactShelfBrowseRedirect.Location == $"/index.html?sessionId={Uri.EscapeDataString(sessionId)}&role={Uri.EscapeDataString(role.ToString())}&artifactView=campaign"',
        'Assert(artifactShelfRedirect.Location == $"/index.html?sessionId={Uri.EscapeDataString(sessionId)}&role={Uri.EscapeDataString(role.ToString())}&artifactView=travel&artifactId=artifact-recap"',
        "artifact_shelf:mobile",
        "artifact_recap_view:mobile",
        "scripts/verify_next90_m117_mobile_artifact_shelf.py",
    ],
    "mobile_live_combat_confidence": [
        "Mobile live combat confidence criteria (M121)",
        "next90-m121-mobile-add-player-table-cards-between-turn-affordances-and-gm-l",
        'Assert(projection.PlayerTableCardsSummary.Contains("Player table cards:", StringComparison.Ordinal)',
        'Assert(projection.PlayerTableCardLabels.Any(item => item.Contains("Initiative lane:", StringComparison.Ordinal))',
        'Assert(projection.BetweenTurnAffordancesSummary.Contains("Between-turn affordances:", StringComparison.Ordinal)',
        'Assert(projection.BetweenTurnAffordanceLabels.Any(item => item.Contains("Ready lane:", StringComparison.Ordinal))',
        'Assert(projection.GmLiteContinuitySummary.Contains("GM-lite continuity:", StringComparison.Ordinal)',
        'Assert(projection.GmLiteContinuityLabels.Any(item => item.Contains("Objective lane:", StringComparison.Ordinal))',
        'Assert(observerProjection.GmLiteContinuitySummary.Contains("observer lane", StringComparison.OrdinalIgnoreCase)',
        'Assert(gmProjection.PlayerTableCardsSummary.Contains("Advance Initiative", StringComparison.Ordinal)',
        'Assert(gmProjection.GmLiteContinuitySummary.Contains("GM runboard", StringComparison.Ordinal)',
        'setText("workspace-player-table-cards", payload.playerTableCardsSummary, "No player table-card summary is available yet.");',
        'setList("workspace-player-table-cards-list", payload.playerTableCardLabels);',
        'setText("workspace-between-turn-affordances", payload.betweenTurnAffordancesSummary, "No between-turn affordance summary is available yet.");',
        'setList("workspace-between-turn-affordances-list", payload.betweenTurnAffordanceLabels);',
        'setText("workspace-gm-lite-continuity", payload.gmLiteContinuitySummary, "No GM-lite continuity summary is available yet.");',
        'setList("workspace-gm-lite-continuity-list", payload.gmLiteContinuityLabels);',
        'id="workspace-player-table-cards"',
        'id="workspace-player-table-cards-list"',
        'id="workspace-between-turn-affordances"',
        'id="workspace-between-turn-affordances-list"',
        'id="workspace-gm-lite-continuity"',
        'id="workspace-gm-lite-continuity-list"',
        "player table cards",
        "between-turn affordances",
        "GM-lite continuity views",
        "add_player_table_cards_between:mobile",
        "scripts/verify_next90_m121_mobile_live_combat_confidence.py",
    ],
    "mobile_runner_goal_updates": [
        "Mobile runner-goal updates and consequence-feed criteria (M122)",
        "next90-m122-mobile-add-mobile-runner-goal-updates-and-player-safe-consequen",
        'Assert(projection.RunnerGoalUpdatesSummary.Contains("Runner goal updates:", StringComparison.Ordinal)',
        'Assert(projection.RunnerGoalUpdatesSummary.Contains("Return moments stay player-safe", StringComparison.Ordinal)',
        'Assert(projection.RunnerGoalUpdateLabels.Any(item => item.Contains("Goal checkpoint lane:", StringComparison.Ordinal))',
        'Assert(projection.RunnerGoalUpdateLabels.Any(item => item.Contains("Goal signal lane:", StringComparison.Ordinal))',
        'Assert(projection.RunnerGoalUpdateLabels.Any(item => item.Contains("Goal route lane:", StringComparison.Ordinal))',
        'Assert(projection.RunnerGoalUpdateLabels.Any(item => item.Contains("Goal boundary lane:", StringComparison.Ordinal))',
        'Assert(projection.PlayerSafeConsequenceFeedSummary.Contains("Player-safe consequence feed:", StringComparison.Ordinal)',
        'Assert(projection.PlayerSafeConsequenceFeedSummary.Contains("BLACK LEDGER world truth", StringComparison.Ordinal)',
        'Assert(projection.PlayerSafeConsequenceFeedLabels.Any(item => item.Contains("Consequence lane:", StringComparison.Ordinal))',
        'Assert(projection.PlayerSafeConsequenceFeedLabels.Any(item => item.Contains("Spoiler lane:", StringComparison.Ordinal))',
        'Assert(projection.PlayerSafeConsequenceFeedLabels.Any(item => item.Contains("Return lane:", StringComparison.Ordinal))',
        'Assert(projection.PlayerSafeConsequenceFeedLabels.Any(item => item.Contains("Trust lane:", StringComparison.Ordinal))',
        'Assert(observerProjection.RunnerGoalUpdatesSummary.Contains("Runner goal updates:", StringComparison.Ordinal)',
        'Assert(observerProjection.PlayerSafeConsequenceFeedSummary.Contains("Player-safe consequence feed:", StringComparison.Ordinal)',
        'Assert(gmProjection.RunnerGoalUpdatesSummary.Contains("GM runboard", StringComparison.Ordinal)',
        'Assert(gmProjection.PlayerSafeConsequenceFeedLabels.Any(item => item.Contains("Trust lane:", StringComparison.Ordinal))',
        'setText("workspace-runner-goal-updates", payload.runnerGoalUpdatesSummary, "No runner-goal update summary is available yet.");',
        'setList("workspace-runner-goal-update-list", payload.runnerGoalUpdateLabels);',
        'setText("workspace-player-safe-consequence-feed", payload.playerSafeConsequenceFeedSummary, "No player-safe consequence feed summary is available yet.");',
        'setList("workspace-player-safe-consequence-feed-list", payload.playerSafeConsequenceFeedLabels);',
        'id="workspace-runner-goal-updates"',
        'id="workspace-runner-goal-update-list"',
        'id="workspace-player-safe-consequence-feed"',
        'id="workspace-player-safe-consequence-feed-list"',
        "runner-goal return updates",
        "player-safe consequence feed views",
        "add_mobile_runner_goal_updates:mobile",
        "scripts/verify_next90_m122_mobile_runner_goal_updates.py",
    ],
    "quick_explain_follow_up": [
        "Successor-wave explain and follow-up criteria (M145)",
        'Assert(projection.QuickExplainSummary.Contains("Quick explain:", StringComparison.Ordinal)',
        'Assert(projection.SourceAnchorSummary.Contains("Source anchors:", StringComparison.Ordinal)',
        'Assert(projection.StaleStatePosture.Contains("Stale-state posture: green", StringComparison.Ordinal)',
        'Assert(projection.GroundedFollowUpSummary.Contains("Grounded follow-up:", StringComparison.Ordinal)',
        'Assert(projection.GroundedFollowUpLabels.Any(item => item.Contains("Boundary lane:", StringComparison.Ordinal))',
        'setText("workspace-quick-explain", payload.quickExplainSummary, "No quick explain summary is available yet.");',
        'setList("workspace-quick-explain-list", payload.quickExplainLabels);',
        'setText("workspace-source-anchor", payload.sourceAnchorSummary, "No source-anchor context is available yet.");',
        'setList("workspace-source-anchor-list", payload.sourceAnchorLabels);',
        'setText("workspace-stale-posture", payload.staleStatePosture, "No stale-state posture is available yet.");',
        'setText("workspace-grounded-follow-up", payload.groundedFollowUpSummary, "No grounded follow-up summary is available yet.");',
        'setList("workspace-grounded-follow-up-list", payload.groundedFollowUpLabels);',
        'id="workspace-quick-explain"',
        'id="workspace-source-anchor"',
        'id="workspace-stale-posture"',
        'id="workspace-grounded-follow-up"',
        "packet-backed quick explain",
        "source-anchor context",
        "grounded text-first follow-up bounded to the claimed live-play shell",
    ],
}

PACKAGE_RECEIPTS = [
    {
        "package_id": "next90-m112-mobile-campaign-continuity",
        "title": "Make travel and mobile campaign continuity explicit for stale, cached, and action-required campaign state.",
        "milestone_id": "112",
        "work_task_id": "112.4",
        "frontier_id": "3720982159",
        "active_flagship_frontier_id": M112_ACTIVE_FLAGSHIP_FRONTIER_ID,
        "repo": REPO_LABEL,
        "checkout_root": CHECKOUT_ROOT,
        "allowed_paths": [
            "src",
            "tests",
            "docs",
            "scripts",
        ],
        "status": "implemented",
        "proof_marker_set": "mobile_campaign_continuity",
        "owned_surfaces": [
            "campaign_memory:travel",
            "campaign_state:mobile",
        ],
        "proof_receipt": "docs/next90-m112-mobile-campaign-continuity.proof.md",
    },
    {
        "package_id": "next90-m119-mobile-onboarding-continuity",
        "title": "Add travel and mobile starter continuity for primer and briefing artifacts.",
        "milestone_id": "119",
        "work_task_id": "119.3",
        "frontier_id": "2766704797",
        "repo": REPO_LABEL,
        "checkout_root": CHECKOUT_ROOT,
        "allowed_paths": [
            "src",
            "tests",
            "docs",
            "scripts",
        ],
        "status": "implemented",
        "proof_marker_set": "mobile_onboarding_continuity",
        "owned_surfaces": [
            "starter_onboarding:mobile",
            "first_session_briefing:mobile",
        ],
        "proof_receipt": "docs/next90-m119-mobile-onboarding-continuity.proof.md",
    },
    {
        "package_id": "next90-m121-mobile-add-player-table-cards-between-turn-affordances-and-gm-l",
        "title": "Add player table cards, between-turn affordances, and GM-lite continuity views for live combat confidence.",
        "milestone_id": "121",
        "work_task_id": "121.4",
        "frontier_id": "6121780841",
        "repo": REPO_LABEL,
        "checkout_root": CHECKOUT_ROOT,
        "allowed_paths": [
            "src",
            "tests",
            "docs",
            "scripts",
        ],
        "status": "implemented",
        "proof_marker_set": "mobile_live_combat_confidence",
        "owned_surfaces": [
            "add_player_table_cards_between:mobile",
        ],
        "proof_receipt": "docs/next90-m121-mobile-live-combat-confidence.proof.md",
    },
    {
        "package_id": "next90-m145-mobile-quick-explain-and-follow-up",
        "milestone_id": "145",
        "work_task_id": "145.3",
        "frontier_id": "1453045303",
        "status": "closed",
        "proof_marker_set": "quick_explain_follow_up",
        "owned_surfaces": [
            "quick_explain:mobile",
            "grounded_follow_up:mobile",
        ],
        "proof_receipt": "docs/next90-m145-mobile-quick-explain-and-follow-up.proof.md",
    },
    {
        "package_id": "next90-m122-mobile-add-mobile-runner-goal-updates-and-player-safe-consequen",
        "title": "Add mobile runner-goal updates and player-safe consequence feed views for campaign return moments.",
        "milestone_id": "122",
        "work_task_id": "122.4",
        "frontier_id": "8138838792",
        "repo": REPO_LABEL,
        "checkout_root": CHECKOUT_ROOT,
        "allowed_paths": [
            "src",
            "tests",
            "docs",
            "scripts",
        ],
        "status": "implemented",
        "proof_marker_set": "mobile_runner_goal_updates",
        "owned_surfaces": [
            "add_mobile_runner_goal_updates:mobile",
        ],
        "proof_receipt": "docs/next90-m122-mobile-runner-goal-updates-and-consequence-feed.proof.md",
    },
    {
        "package_id": "next90-m117-mobile-artifact-shelf",
        "milestone_id": "117",
        "work_task_id": "117.4",
        "title": "Add mobile artifact shelf views for campaign, travel, and recap artifacts.",
        "frontier_id": M117_SUCCESSOR_FRONTIER_ID,
        "frontier_ids": M117_FRONTIER_IDS,
        "active_flagship_frontier_id": M117_ACTIVE_FLAGSHIP_FRONTIER_ID,
        "repo": REPO_LABEL,
        "checkout_root": CHECKOUT_ROOT,
        "allowed_paths": [
            "src",
            "tests",
            "docs",
            "scripts",
        ],
        "status": "closed",
        "proof_marker_set": "mobile_artifact_shelf",
        "owned_surfaces": [
            "artifact_shelf:mobile",
            "artifact_recap_view:mobile",
        ],
        "proof_receipt": "docs/next90-m117-mobile-artifact-shelf.proof.md",
    },
]


def iso_now() -> str:
    return dt.datetime.now(dt.timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def strip_generated_timestamps(payload: dict[str, object]) -> dict[str, object]:
    normalized = deepcopy(payload)
    normalized.pop("generated_at", None)
    normalized.pop("generatedAt", None)
    return normalized


def load_existing_payload(path: Path) -> dict[str, object] | None:
    if not path.is_file():
        return None

    try:
        loaded = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return None

    return loaded if isinstance(loaded, dict) else None


def main() -> int:
    if not REGRESSION_SOURCE.is_file():
        print(f"missing regression source: {REGRESSION_SOURCE}", file=sys.stderr)
        return 1
    if not WEB_SOURCE.is_file():
        print(f"missing web source: {WEB_SOURCE}", file=sys.stderr)
        return 1
    if not MIGRATION_MAP.is_file():
        print(f"missing migration map: {MIGRATION_MAP}", file=sys.stderr)
        return 1
    if not PLAY_SIGNOFF.is_file():
        print(f"missing play signoff: {PLAY_SIGNOFF}", file=sys.stderr)
        return 1
    if not M112_PROOF_DOC.is_file():
        print(f"missing M112 proof doc: {M112_PROOF_DOC}", file=sys.stderr)
        return 1
    if not M112_VERIFIER.is_file():
        print(f"missing M112 verifier: {M112_VERIFIER}", file=sys.stderr)
        return 1
    if not M119_PROOF_DOC.is_file():
        print(f"missing M119 proof doc: {M119_PROOF_DOC}", file=sys.stderr)
        return 1
    if not M119_VERIFIER.is_file():
        print(f"missing M119 verifier: {M119_VERIFIER}", file=sys.stderr)
        return 1
    if not M117_PROOF_DOC.is_file():
        print(f"missing M117 proof doc: {M117_PROOF_DOC}", file=sys.stderr)
        return 1
    if not M117_VERIFIER.is_file():
        print(f"missing M117 verifier: {M117_VERIFIER}", file=sys.stderr)
        return 1
    if not M121_PROOF_DOC.is_file():
        print(f"missing M121 proof doc: {M121_PROOF_DOC}", file=sys.stderr)
        return 1
    if not M121_VERIFIER.is_file():
        print(f"missing M121 verifier: {M121_VERIFIER}", file=sys.stderr)
        return 1
    if not M122_PROOF_DOC.is_file():
        print(f"missing M122 proof doc: {M122_PROOF_DOC}", file=sys.stderr)
        return 1
    if not M122_VERIFIER.is_file():
        print(f"missing M122 verifier: {M122_VERIFIER}", file=sys.stderr)
        return 1
    if not M145_PROOF_DOC.is_file():
        print(f"missing M145 proof doc: {M145_PROOF_DOC}", file=sys.stderr)
        return 1
    if not M145_VERIFIER.is_file():
        print(f"missing M145 verifier: {M145_VERIFIER}", file=sys.stderr)
        return 1

    regression_text = REGRESSION_SOURCE.read_text(encoding="utf-8")
    web_text = WEB_SOURCE.read_text(encoding="utf-8")
    migration_map_text = MIGRATION_MAP.read_text(encoding="utf-8")
    play_signoff_text = PLAY_SIGNOFF.read_text(encoding="utf-8")
    m112_proof_doc_text = M112_PROOF_DOC.read_text(encoding="utf-8")
    m112_verifier_text = M112_VERIFIER.read_text(encoding="utf-8")
    m119_proof_doc_text = M119_PROOF_DOC.read_text(encoding="utf-8")
    m119_verifier_text = M119_VERIFIER.read_text(encoding="utf-8")
    m117_proof_doc_text = M117_PROOF_DOC.read_text(encoding="utf-8")
    m117_verifier_text = M117_VERIFIER.read_text(encoding="utf-8")
    m121_proof_doc_text = M121_PROOF_DOC.read_text(encoding="utf-8")
    m121_verifier_text = M121_VERIFIER.read_text(encoding="utf-8")
    m122_proof_doc_text = M122_PROOF_DOC.read_text(encoding="utf-8")
    m122_verifier_text = M122_VERIFIER.read_text(encoding="utf-8")
    m145_proof_doc_text = M145_PROOF_DOC.read_text(encoding="utf-8")
    m145_verifier_text = M145_VERIFIER.read_text(encoding="utf-8")
    combined_text = "\n".join(
        [
            regression_text,
            web_text,
            migration_map_text,
            play_signoff_text,
            m112_proof_doc_text,
            m112_verifier_text,
            m119_proof_doc_text,
            m119_verifier_text,
            m117_proof_doc_text,
            m117_verifier_text,
            m121_proof_doc_text,
            m121_verifier_text,
            m122_proof_doc_text,
            m122_verifier_text,
            m145_proof_doc_text,
            m145_verifier_text,
        ])

    missing: list[str] = []
    journeys_passed: list[str] = []
    for journey_id, markers in REQUIRED_MARKERS.items():
        journey_missing = [marker for marker in markers if marker not in combined_text]
        if journey_missing:
            for marker in journey_missing:
                missing.append(f"{journey_id}: {marker}")
            continue
        journeys_passed.append(journey_id)

    if missing:
        for item in missing:
            print(f"mobile_local_release_proof_missing: {item}", file=sys.stderr)
        return 1

    existing_payload = load_existing_payload(OUT)
    payload = {
        "contract_name": "chummer6-mobile.local_release_proof",
        "status": "passed",
        "proof_kind": "source_backed_local_regression_contract",
        "source_files": [
            str(REGRESSION_SOURCE.relative_to(ROOT)),
            str(WEB_SOURCE.relative_to(ROOT)),
            str(MIGRATION_MAP.relative_to(ROOT)),
            str(PLAY_SIGNOFF.relative_to(ROOT)),
            str(M112_PROOF_DOC.relative_to(ROOT)),
            str(M112_VERIFIER.relative_to(ROOT)),
            str(M119_PROOF_DOC.relative_to(ROOT)),
            str(M119_VERIFIER.relative_to(ROOT)),
            str(M117_PROOF_DOC.relative_to(ROOT)),
            str(M117_VERIFIER.relative_to(ROOT)),
            str(M121_PROOF_DOC.relative_to(ROOT)),
            str(M121_VERIFIER.relative_to(ROOT)),
            str(M122_PROOF_DOC.relative_to(ROOT)),
            str(M122_VERIFIER.relative_to(ROOT)),
            str(M145_PROOF_DOC.relative_to(ROOT)),
            str(M145_VERIFIER.relative_to(ROOT)),
        ],
        "journeys_passed": journeys_passed,
        "required_markers": REQUIRED_MARKERS,
        "package_receipts": PACKAGE_RECEIPTS,
    }

    existing_generated_at = existing_payload.get("generated_at") if isinstance(existing_payload, dict) else None
    if (
        isinstance(existing_generated_at, str)
        and strip_generated_timestamps(existing_payload) == strip_generated_timestamps(payload)
    ):
        payload["generated_at"] = existing_generated_at
    else:
        payload["generated_at"] = iso_now()

    OUT.parent.mkdir(parents=True, exist_ok=True)
    serialized = json.dumps(payload, indent=2) + "\n"
    OUT.write_text(serialized, encoding="utf-8")
    print(f"wrote mobile local release proof: {OUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
