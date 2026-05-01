#!/usr/bin/env python3
from __future__ import annotations

import json
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PACKAGE_ID = "next90-m112-mobile-campaign-continuity"
FRONTIER_ID = "3720982159"
MILESTONE_ID = "112"
REPO_LABEL = "chummer6-mobile"
CHECKOUT_ROOT = str(ROOT)

PROOF_DOC = ROOT / "docs" / "next90-m112-mobile-campaign-continuity.proof.md"
PLAY_SIGNOFF = ROOT / "docs" / "PLAY_RELEASE_SIGNOFF.md"
GENERATED_PROOF = ROOT / ".codex-studio" / "published" / "MOBILE_LOCAL_RELEASE_PROOF.generated.json"

IMPLEMENTATION_MARKERS = {
    "src/Chummer.Play.Core/Application/PlayCampaignWorkspaceLiteProjector.cs": (
        "MobileCampaignCurrentState",
        "MobileCampaignStateSummary",
        "MobileCampaignCachedState",
        "MobileCampaignStaleState",
        "MobileCampaignActionRequired",
        "MobileCampaignStateLabels",
        "BuildMobileCampaignCurrentState",
        "BuildMobileCampaignStateSummary",
        "BuildMobileCampaignCachedState",
        "BuildMobileCampaignStaleState",
        "BuildMobileCampaignActionRequired",
        "BuildMobileCampaignStateLabels",
    ),
    "src/Chummer.Play.Core/Roaming/RoamingWorkspaceSyncPlanner.cs": (
        "TravelCampaignCurrentState",
        "TravelCampaignStateSummary",
        "TravelCampaignCachedState",
        "TravelCampaignStaleState",
        "TravelCampaignActionRequired",
        "TravelCampaignStateLabels",
        "BuildTravelCampaignCurrentState",
        "BuildTravelCampaignStateSummary",
        "BuildTravelCampaignCachedState",
        "BuildTravelCampaignStaleState",
        "BuildTravelCampaignActionRequired",
        "BuildTravelCampaignStateLabels",
    ),
    "src/Chummer.Play.Web/wwwroot/index.html": (
        'id="workspace-mobile-campaign-card"',
        'id="restore-travel-campaign-card"',
        'id="workspace-mobile-campaign-current-state"',
        'id="workspace-mobile-campaign-state"',
        'id="workspace-mobile-campaign-cached-state"',
        'id="workspace-mobile-campaign-stale-state"',
        'id="workspace-mobile-campaign-action-required"',
        'id="workspace-mobile-campaign-state-list"',
        'id="restore-travel-campaign-current-state"',
        'id="restore-travel-campaign-state"',
        'id="restore-travel-campaign-cached-state"',
        'id="restore-travel-campaign-stale-state"',
        'id="restore-travel-campaign-action-required"',
        'id="restore-travel-campaign-state-labels"',
        'document.getElementById("workspace-mobile-campaign-current-state").textContent = payload.mobileCampaignCurrentState || "No mobile campaign continuity posture is available yet.";',
        'document.getElementById("workspace-mobile-campaign-state").textContent = payload.mobileCampaignStateSummary || "No mobile campaign-state summary is available yet.";',
        'document.getElementById("restore-travel-campaign-current-state").textContent = payload.travelCampaignCurrentState || "No restore travel campaign continuity posture is available yet.";',
        'document.getElementById("restore-travel-campaign-state").textContent = payload.travelCampaignStateSummary || "No restore travel campaign-state summary is available yet.";',
        "function inferContinuityTone(text)",
        "function syncContinuityCardTone(cardId, currentStateId, summaryId, currentStateText)",
        "function syncContinuityStateBreakdown(cachedId, staleId, staleText, actionId)",
        'setContinuityFieldKind(cachedId, "cached");',
        'setContinuityFieldKind(staleId, "stale");',
        'setContinuityFieldKind(actionId, "action-required");',
    ),
    "src/Chummer.Play.RegressionChecks/Program.cs": (
        'Assert(projection.MobileCampaignCurrentState.Contains("Current continuity posture:", StringComparison.Ordinal)',
        'Assert(projection.MobileCampaignStateSummary.Contains("Cached state:", StringComparison.Ordinal)',
        'Assert(projection.MobileCampaignStaleState.Contains("Stale state:", StringComparison.Ordinal)',
        'Assert(projection.MobileCampaignActionRequired.Contains("Action required:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignCurrentState.Contains("Current continuity posture:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignStateSummary.Contains("Cached state:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignStaleState.Contains("Stale state:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignActionRequired.Contains("Action required:", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"workspace-mobile-campaign-current-state\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"workspace-mobile-campaign-state\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"restore-travel-campaign-current-state\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"restore-travel-campaign-state\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("document.getElementById(\\"workspace-mobile-campaign-current-state\\").textContent = payload.mobileCampaignCurrentState || \\"No mobile campaign continuity posture is available yet.\\";", StringComparison.Ordinal)',
        'Assert(html.Contains("document.getElementById(\\"workspace-mobile-campaign-state\\").textContent = payload.mobileCampaignStateSummary || \\"No mobile campaign-state summary is available yet.\\";", StringComparison.Ordinal)',
        'Assert(html.Contains("document.getElementById(\\"restore-travel-campaign-current-state\\").textContent = payload.travelCampaignCurrentState || \\"No restore travel campaign continuity posture is available yet.\\";", StringComparison.Ordinal)',
        'Assert(html.Contains("document.getElementById(\\"restore-travel-campaign-state\\").textContent = payload.travelCampaignStateSummary || \\"No restore travel campaign-state summary is available yet.\\";", StringComparison.Ordinal)',
        'Assert(html.Contains("function inferContinuityTone(text)", StringComparison.Ordinal)',
        'Assert(html.Contains("syncContinuityCardTone(\\n      \\"workspace-mobile-campaign-card\\",", StringComparison.Ordinal)',
        'Assert(html.Contains("syncContinuityCardTone(\\n      \\"restore-travel-campaign-card\\",", StringComparison.Ordinal)',
    ),
    "scripts/materialize_mobile_local_release_proof.py": (
        PACKAGE_ID,
        FRONTIER_ID,
        "mobile_campaign_continuity",
        "scripts/verify_next90_m112_mobile_campaign_continuity.py",
        "campaign_memory:travel",
        "campaign_state:mobile",
    ),
    "scripts/ai/verify.sh": (
        "docs/next90-m112-mobile-campaign-continuity.proof.md",
        "mobile_campaign_continuity",
        "python3 scripts/verify_next90_m112_mobile_campaign_continuity.py >/dev/null",
    ),
}

PROOF_TOKENS = (
    f"Package: `{PACKAGE_ID}`",
    "Milestone: `112`",
    f"Concrete checkout root: `{CHECKOUT_ROOT}`",
    f"Canonical queue/registry repo label: `{REPO_LABEL}`",
    "campaign_memory:travel",
    "campaign_state:mobile",
    "current posture plus cached, stale, and action-required campaign continuity",
    "tone-aware continuity state summaries",
    "MobileCampaignCurrentState",
    "TravelCampaignCurrentState",
    "cached, stale, and action-required campaign continuity",
    "scripts/verify_next90_m112_mobile_campaign_continuity.py",
    "MOBILE_LOCAL_RELEASE_PROOF.generated.json",
    "implementation-only receipt",
    "repo-local `implemented` receipt",
    "does not claim queue closure outside the current mobile repo",
)

SIGNOFF_TOKENS = (
    "Mobile campaign continuity criteria (M112)",
    "next90-m112-mobile-campaign-continuity",
    "MobileCampaignCurrentState",
    "MobileCampaignStateSummary",
    "TravelCampaignCurrentState",
    "TravelCampaignStateSummary",
)

GENERATED_TOKENS = (
    PACKAGE_ID,
    FRONTIER_ID,
    "mobile_campaign_continuity",
    "campaign_memory:travel",
    "campaign_state:mobile",
    "Mobile campaign continuity criteria (M112)",
)

FORBIDDEN_PROOF_MARKERS = (
    "operator telemetry",
    "active-run helper",
    "supervisor status",
    "TASK_LOCAL_TELEMETRY.generated.json",
    "ACTIVE_RUN_HANDOFF.generated.md",
    "verify_closed_package_only",
    "do_not_reopen_reason",
)


def read_text(path: Path) -> str:
    if not path.is_file():
        raise FileNotFoundError(path)
    return path.read_text(encoding="utf-8")


def require_tokens(label: str, text: str, tokens: tuple[str, ...]) -> list[str]:
    return [f"{label}: {token}" for token in tokens if token not in text]


def main() -> int:
    missing: list[str] = []

    try:
        proof_text = read_text(PROOF_DOC)
        signoff_text = read_text(PLAY_SIGNOFF)
        generated_text = read_text(GENERATED_PROOF)
    except FileNotFoundError as exc:
        print(f"m112_mobile_campaign_continuity_verify_failed: missing file: {exc}", file=sys.stderr)
        return 1

    for relative_path, markers in IMPLEMENTATION_MARKERS.items():
        source_text = read_text(ROOT / relative_path)
        missing.extend(require_tokens(relative_path, source_text, markers))

    missing.extend(require_tokens("proof_doc", proof_text, PROOF_TOKENS))
    missing.extend(require_tokens("play_signoff", signoff_text, SIGNOFF_TOKENS))
    missing.extend(require_tokens("generated_proof_text", generated_text, GENERATED_TOKENS))

    payload = json.loads(generated_text)
    if payload.get("contract_name") != "chummer6-mobile.local_release_proof":
        missing.append("generated_proof_payload: wrong contract_name")
    if payload.get("status") != "passed":
        missing.append("generated_proof_payload: status is not passed")

    receipts = payload.get("package_receipts")
    if not isinstance(receipts, list):
        missing.append("generated_proof_payload: package_receipts missing")
    else:
        receipt = next((item for item in receipts if item.get("package_id") == PACKAGE_ID), None)
        if receipt is None:
            missing.append(f"generated_proof_payload: missing receipt for {PACKAGE_ID}")
        else:
            if receipt.get("frontier_id") != FRONTIER_ID:
                missing.append("generated_proof_payload: wrong frontier_id for M112 receipt")
            if receipt.get("status") != "implemented":
                missing.append("generated_proof_payload: wrong status for M112 receipt")
            if receipt.get("proof_marker_set") != "mobile_campaign_continuity":
                missing.append("generated_proof_payload: wrong proof_marker_set for M112 receipt")
            if tuple(receipt.get("owned_surfaces", [])) != ("campaign_memory:travel", "campaign_state:mobile"):
                missing.append("generated_proof_payload: wrong owned_surfaces for M112 receipt")

    for marker in FORBIDDEN_PROOF_MARKERS:
        if marker in proof_text:
            missing.append(f"proof_doc: forbidden marker present: {marker}")
        if marker in signoff_text:
            missing.append(f"play_signoff: forbidden marker present: {marker}")
        if marker in generated_text:
            missing.append(f"generated_proof_text: forbidden marker present: {marker}")

    if missing:
        for item in missing:
            print(f"m112_mobile_campaign_continuity_verify_failed: {item}", file=sys.stderr)
        return 1

    print("m112_mobile_campaign_continuity_verify_ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
