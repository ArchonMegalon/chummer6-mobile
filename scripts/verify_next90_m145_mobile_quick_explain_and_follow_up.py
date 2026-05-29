#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PACKAGE_ID = "next90-m145-mobile-quick-explain-and-follow-up"
MILESTONE_ID = "145"
WORK_TASK_ID = "145.3"
QUEUE_FRONTIER_ID = "1453045303"
REPO_LABEL = "chummer6-mobile"
CHECKOUT_ROOT = str(ROOT)
REGISTRY = Path("/docker/chummercomplete/chummer-design/products/chummer/NEXT_90_DAY_PRODUCT_ADVANCE_REGISTRY.yaml")
DESIGN_QUEUE = Path("/docker/chummercomplete/chummer-design/products/chummer/NEXT_90_DAY_QUEUE_STAGING.generated.yaml")
FLEET_QUEUE = Path("/docker/fleet/.codex-studio/published/NEXT_90_DAY_QUEUE_STAGING.generated.yaml")

PROOF_DOC = ROOT / "docs" / "next90-m145-mobile-quick-explain-and-follow-up.proof.md"
PLAY_SIGNOFF = ROOT / "docs" / "PLAY_RELEASE_SIGNOFF.md"
GENERATED_PROOF = ROOT / ".codex-studio" / "published" / "MOBILE_LOCAL_RELEASE_PROOF.generated.json"

IMPLEMENTATION_MARKERS = {
    "src/Chummer.Play.Core/Application/PlayCampaignWorkspaceLiteProjector.cs": (
        "QuickExplainSummary",
        "QuickExplainLabels",
        "SourceAnchorSummary",
        "SourceAnchorLabels",
        "StaleStatePosture",
        "GroundedFollowUpSummary",
        "GroundedFollowUpLabels",
        "BuildQuickExplainSummary",
        "BuildQuickExplainLabels",
        "BuildSourceAnchorSummary",
        "BuildSourceAnchorLabels",
        "BuildStaleStatePosture",
        "BuildGroundedFollowUpSummary",
        "BuildGroundedFollowUpLabels",
    ),
    "src/Chummer.Play.Web/wwwroot/index.html": (
        'id="workspace-quick-explain"',
        'id="workspace-quick-explain-list"',
        'id="workspace-source-anchor"',
        'id="workspace-source-anchor-list"',
        'id="workspace-stale-posture"',
        'id="workspace-grounded-follow-up"',
        'id="workspace-grounded-follow-up-list"',
        'setText("workspace-quick-explain", payload.quickExplainSummary, "No quick explain summary is available yet.");',
        'setList("workspace-quick-explain-list", payload.quickExplainLabels);',
        'setText("workspace-source-anchor", payload.sourceAnchorSummary, "No source-anchor context is available yet.");',
        'setList("workspace-source-anchor-list", payload.sourceAnchorLabels);',
        'setText("workspace-stale-posture", payload.staleStatePosture, "No stale-state posture is available yet.");',
        'setText("workspace-grounded-follow-up", payload.groundedFollowUpSummary, "No grounded follow-up summary is available yet.");',
        'setList("workspace-grounded-follow-up-list", payload.groundedFollowUpLabels);',
    ),
    "src/Chummer.Play.RegressionChecks/Program.cs": (
        'Assert(projection.QuickExplainSummary.Contains("Quick explain:", StringComparison.Ordinal)',
        'Assert(projection.SourceAnchorSummary.Contains("Source anchors:", StringComparison.Ordinal)',
        'Assert(projection.StaleStatePosture.Contains("Stale-state posture: green", StringComparison.Ordinal)',
        'Assert(projection.GroundedFollowUpSummary.Contains("Grounded follow-up:", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"workspace-quick-explain\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"workspace-source-anchor\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"workspace-stale-posture\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"workspace-grounded-follow-up\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("setText(\\"workspace-quick-explain\\", payload.quickExplainSummary, \\"No quick explain summary is available yet.\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("setText(\\"workspace-source-anchor\\", payload.sourceAnchorSummary, \\"No source-anchor context is available yet.\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("setText(\\"workspace-stale-posture\\", payload.staleStatePosture, \\"No stale-state posture is available yet.\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("setText(\\"workspace-grounded-follow-up\\", payload.groundedFollowUpSummary, \\"No grounded follow-up summary is available yet.\\");", StringComparison.Ordinal)',
    ),
    "scripts/materialize_mobile_local_release_proof.py": (
        "quick_explain_follow_up",
        "Successor-wave explain and follow-up criteria (M145)",
        "packet-backed quick explain",
        "source-anchor context",
        "grounded text-first follow-up bounded to the claimed live-play shell",
    ),
    "scripts/ai/verify.sh": (
        "docs/next90-m145-mobile-quick-explain-and-follow-up.proof.md",
        "scripts/verify_next90_m145_mobile_quick_explain_and_follow_up.py",
        'rg -n \'"quick_explain_follow_up"\' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null',
    ),
}

QUEUE_TOKENS = (
    f"package_id: {PACKAGE_ID}",
    f"work_task_id: '{WORK_TASK_ID}'",
    f"frontier_id: {QUEUE_FRONTIER_ID}",
    f"milestone_id: {MILESTONE_ID}",
    "status: complete",
    "completion_action: verify_closed_package_only",
    "do_not_reopen_reason: M145 chummer6-mobile quick explain and follow-up is complete",
    f"repo: {REPO_LABEL}",
    "quick_explain:mobile",
    "grounded_follow_up:mobile",
    "/docker/chummercomplete/chummer-play/docs/next90-m145-mobile-quick-explain-and-follow-up.proof.md",
    "/docker/chummercomplete/chummer-play/scripts/verify_next90_m145_mobile_quick_explain_and_follow_up.py",
    "/docker/chummercomplete/chummer-play/scripts/materialize_mobile_local_release_proof.py",
    "/docker/chummercomplete/chummer-play/.codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json",
)

REGISTRY_TOKENS = (
    "id: '145.3'",
    "owner: chummer6-mobile",
    "title: Bring quick explain, source-anchor context, and bounded follow-up to mobile and live-play shells.",
)

PROOF_TOKENS = (
    f"Package: `{PACKAGE_ID}`",
    f"Work task: `{WORK_TASK_ID}`",
    f"Milestone: `{MILESTONE_ID}`",
    f"Concrete checkout root: `{CHECKOUT_ROOT}`",
    f"Canonical queue/registry repo label: `{REPO_LABEL}`",
    "quick_explain:mobile",
    "grounded_follow_up:mobile",
    "packet-backed quick explain, source-anchor context, stale-state posture, and grounded text-first follow-up",
    "QuickExplainSummary",
    "SourceAnchorSummary",
    "GroundedFollowUpSummary",
    "queue-closure guard",
    "Canonical closeout anchors:",
    "NEXT_90_DAY_PRODUCT_ADVANCE_REGISTRY.yaml",
    "NEXT_90_DAY_QUEUE_STAGING.generated.yaml",
    "verify_closed_package_only",
    "Future shards should verify these anchors instead of reopening the package.",
    "scripts/verify_next90_m145_mobile_quick_explain_and_follow_up.py",
    "MOBILE_LOCAL_RELEASE_PROOF.generated.json",
    "package receipt",
    "The package is materially complete for the `chummer6-mobile` slice in this checkout.",
)

SIGNOFF_TOKENS = (
    "Successor-wave explain and follow-up criteria (M145)",
    "packet-backed quick explain copy",
    "source-anchor context explicit",
    "stale-state posture explicit",
    "grounded text-first follow-up bounded to the claimed live-play shell",
)

GENERATED_TOKENS = (
    '"quick_explain_follow_up"',
    '"Successor-wave explain and follow-up criteria (M145)"',
    '"source-anchor context"',
    '"package_receipts"',
    PACKAGE_ID,
)

FORBIDDEN_PROOF_MARKERS = (
    "operator telemetry",
    "active-run helper",
    "supervisor status",
    "TASK_LOCAL_TELEMETRY.generated.json",
    "ACTIVE_RUN_HANDOFF.generated.md",
)


def read_text(path: Path) -> str:
    if not path.is_file():
        raise FileNotFoundError(path)
    return path.read_text(encoding="utf-8")


def require_tokens(label: str, text: str, tokens: tuple[str, ...]) -> list[str]:
    return [f"{label}: {token}" for token in tokens if token not in text]


def queue_block(text: str) -> str:
    position = text.find(f"package_id: {PACKAGE_ID}")
    if position < 0:
        return ""
    start = text.rfind("\n- title:", 0, position)
    end = text.find("\n- title:", position)
    if start < 0:
        start = 0
    if end < 0:
        end = len(text)
    return text[start:end]


def require_single_queue_row(label: str, text: str) -> list[str]:
    count = len(re.findall(rf"(?m)^  package_id: {re.escape(PACKAGE_ID)}$", text))
    if count != 1:
        return [f"{label}: expected exactly one {PACKAGE_ID} row, found {count}"]
    return []


def registry_block(text: str) -> str:
    match = re.search(r"(?ms)^    - id: '145\.3'.*?(?=^    - id: |\Z)", text)
    return match.group(0) if match else ""


def main() -> int:
    missing: list[str] = []

    try:
        queue_text = read_text(FLEET_QUEUE)
        design_queue_text = read_text(DESIGN_QUEUE)
        registry_text = read_text(REGISTRY)
        proof_text = read_text(PROOF_DOC)
        signoff_text = read_text(PLAY_SIGNOFF)
        generated_text = read_text(GENERATED_PROOF)
    except FileNotFoundError as exc:
        print(f"m145_mobile_quick_explain_and_follow_up_verify_failed: missing file: {exc}", file=sys.stderr)
        return 1

    queue_row = queue_block(queue_text)
    design_queue_row = queue_block(design_queue_text)
    registry_row = registry_block(registry_text)

    for relative_path, markers in IMPLEMENTATION_MARKERS.items():
        source_text = read_text(ROOT / relative_path)
        missing.extend(require_tokens(relative_path, source_text, markers))

    missing.extend(require_single_queue_row("fleet queue", queue_text))
    missing.extend(require_single_queue_row("design queue", design_queue_text))
    missing.extend(require_tokens("fleet queue", queue_row, QUEUE_TOKENS))
    missing.extend(require_tokens("design queue", design_queue_row, QUEUE_TOKENS))
    missing.extend(require_tokens("registry row", registry_row, REGISTRY_TOKENS))
    missing.extend(require_tokens("proof_doc", proof_text, PROOF_TOKENS))
    missing.extend(require_tokens("play_signoff", signoff_text, SIGNOFF_TOKENS))
    missing.extend(require_tokens("generated_proof_text", generated_text, GENERATED_TOKENS))

    payload = json.loads(generated_text)
    if payload.get("contract_name") != "chummer6-mobile.local_release_proof":
        missing.append("generated_proof_payload: wrong contract_name")
    if payload.get("status") != "passed":
        missing.append("generated_proof_payload: status is not passed")

    journeys = payload.get("journeys_passed")
    if not isinstance(journeys, list) or "quick_explain_follow_up" not in journeys:
        missing.append("generated_proof_payload: quick_explain_follow_up journey missing")

    required_markers = payload.get("required_markers")
    if not isinstance(required_markers, dict) or "quick_explain_follow_up" not in required_markers:
        missing.append("generated_proof_payload: quick_explain_follow_up required markers missing")

    receipts = payload.get("package_receipts")
    if not isinstance(receipts, list):
        missing.append("generated_proof_payload: package_receipts missing")
    else:
        receipt = next((item for item in receipts if item.get("package_id") == PACKAGE_ID), None)
        if receipt is None:
            missing.append(f"generated_proof_payload: missing receipt for {PACKAGE_ID}")
        else:
            if receipt.get("milestone_id") != MILESTONE_ID:
                missing.append("generated_proof_payload: wrong milestone_id for M145 receipt")
            if receipt.get("work_task_id") != WORK_TASK_ID:
                missing.append("generated_proof_payload: wrong work_task_id for M145 receipt")
            if receipt.get("frontier_id") != QUEUE_FRONTIER_ID:
                missing.append("generated_proof_payload: wrong frontier_id for M145 receipt")
            if receipt.get("status") != "closed":
                missing.append("generated_proof_payload: wrong status for M145 receipt")
            if receipt.get("proof_marker_set") != "quick_explain_follow_up":
                missing.append("generated_proof_payload: wrong proof_marker_set for M145 receipt")
            if tuple(receipt.get("owned_surfaces", [])) != ("quick_explain:mobile", "grounded_follow_up:mobile"):
                missing.append("generated_proof_payload: wrong owned_surfaces for M145 receipt")
            if receipt.get("proof_receipt") != "docs/next90-m145-mobile-quick-explain-and-follow-up.proof.md":
                missing.append("generated_proof_payload: wrong proof_receipt for M145 receipt")

    for marker in FORBIDDEN_PROOF_MARKERS:
        if marker in queue_row:
            missing.append(f"fleet_queue: forbidden marker present: {marker}")
        if marker in design_queue_row:
            missing.append(f"design_queue: forbidden marker present: {marker}")
        if marker in registry_row:
            missing.append(f"registry_row: forbidden marker present: {marker}")
        if marker in proof_text:
            missing.append(f"proof_doc: forbidden marker present: {marker}")
        if marker in signoff_text:
            missing.append(f"play_signoff: forbidden marker present: {marker}")
        if marker in generated_text:
            missing.append(f"generated_proof_text: forbidden marker present: {marker}")

    if missing:
        for item in missing:
            print(f"m145_mobile_quick_explain_and_follow_up_verify_failed: {item}", file=sys.stderr)
        return 1

    print("m145_mobile_quick_explain_and_follow_up_verify_ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
