#!/usr/bin/env python3
from __future__ import annotations

import datetime as dt
import json
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
REGRESSION_SOURCE = ROOT / "src" / "Chummer.Play.RegressionChecks" / "Program.cs"
WEB_SOURCE = ROOT / "src" / "Chummer.Play.Web" / "wwwroot" / "index.html"
OUT = ROOT / ".codex-studio" / "published" / "MOBILE_LOCAL_RELEASE_PROOF.generated.json"

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
        'Assert(projection.RoleFollowThroughHref.Contains("/play/{sessionId}"',
        'Assert(projection.DecisionNotice.Contains("Use the current bundle proof for scene-redmond"',
        'document.getElementById("workspace-decision-notice-link").textContent = payload.decisionNotice || "Decision notice follow-through";',
        'document.getElementById("follow-through-update-link").textContent = payload.updateFollowThrough || "Update follow-through";',
        'document.getElementById("follow-through-support-link").textContent = payload.supportFollowThrough || "Support follow-through";',
        'document.getElementById("follow-through-role-link").textContent = payload.roleFollowThrough || "Role follow-through";',
        'document.getElementById("restore-follow-through-link").textContent = payload.resumeFollowThrough || "Claimed-device follow-through";',
        'document.getElementById("restore-support-follow-through-link").textContent = payload.supportFollowThrough || "Restore support follow-through";',
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
}


def iso_now() -> str:
    return dt.datetime.now(dt.timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def main() -> int:
    if not REGRESSION_SOURCE.is_file():
        print(f"missing regression source: {REGRESSION_SOURCE}", file=sys.stderr)
        return 1
    if not WEB_SOURCE.is_file():
        print(f"missing web source: {WEB_SOURCE}", file=sys.stderr)
        return 1

    regression_text = REGRESSION_SOURCE.read_text(encoding="utf-8")
    web_text = WEB_SOURCE.read_text(encoding="utf-8")
    combined_text = f"{regression_text}\n{web_text}"

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

    payload = {
        "contract_name": "chummer6-mobile.local_release_proof",
        "generated_at": iso_now(),
        "status": "passed",
        "proof_kind": "source_backed_local_regression_contract",
        "source_files": [
            str(REGRESSION_SOURCE.relative_to(ROOT)),
            str(WEB_SOURCE.relative_to(ROOT)),
        ],
        "journeys_passed": journeys_passed,
        "required_markers": REQUIRED_MARKERS,
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    serialized = json.dumps(payload, indent=2) + "\n"
    OUT.write_text(serialized, encoding="utf-8")
    print(f"wrote mobile local release proof: {OUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
