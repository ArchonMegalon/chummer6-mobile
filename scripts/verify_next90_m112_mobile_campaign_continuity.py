#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[1]
PACKAGE_ID = "next90-m112-mobile-campaign-continuity"
FRONTIER_ID = "3720982159"
ACTIVE_FLAGSHIP_FRONTIER_ID = "1033794907"
MILESTONE_ID = "112"
WORK_TASK_ID = "112.4"
REPO_LABEL = "chummer6-mobile"
CHECKOUT_ROOT = str(ROOT)
REGISTRY = Path("/docker/chummercomplete/chummer-design/products/chummer/NEXT_90_DAY_PRODUCT_ADVANCE_REGISTRY.yaml")
DESIGN_QUEUE = Path("/docker/chummercomplete/chummer-design/products/chummer/NEXT_90_DAY_QUEUE_STAGING.generated.yaml")
FLEET_QUEUE = Path("/docker/fleet/.codex-studio/published/NEXT_90_DAY_QUEUE_STAGING.generated.yaml")

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
    "src/Chummer.Play.Core/Application/PlayEntryRecoveryProjector.cs": (
        "cached, stale, and action-required travel continuity stays explicit",
        'BuildRecoveryContinuityLine("cached state", restorePlan.TravelCampaignCachedState, "Cached state:")',
        'BuildRecoveryContinuityLine("stale state", restorePlan.TravelCampaignStaleState, "Stale state:")',
        'BuildRecoveryContinuityLine("action required", restorePlan.TravelCampaignActionRequired, "Action required:")',
        'return $"Travel continuity {label}: {detail}";',
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
        'setText("workspace-mobile-campaign-current-state", payload.mobileCampaignCurrentState, "No mobile campaign continuity posture is available yet.");',
        'setText("workspace-mobile-campaign-state", payload.mobileCampaignStateSummary, "No mobile campaign-state summary is available yet.");',
        'setText("workspace-action-required", payload.actionRequiredSummary, "No action-required summary is available yet.");',
        'setList("workspace-action-required-list", payload.actionRequiredLabels);',
        'setText("restore-travel-campaign-current-state", payload.travelCampaignCurrentState, "No restore travel campaign continuity posture is available yet.");',
        'setText("restore-travel-campaign-state", payload.travelCampaignStateSummary, "No restore travel campaign-state summary is available yet.");',
        'setText("restore-action-required", payload.actionRequiredSummary, "No restore action-required summary is available yet.");',
        'setList("restore-action-required-labels", payload.actionRequiredLabels);',
        "function inferContinuityTone(text)",
        'if (lowered.includes("action required") || lowered.includes("action-required")) {',
        "function syncContinuityCardTone(cardId, currentStateId, summaryId, currentStateText, actionId)",
        'const actionState = actionId ? (document.getElementById(actionId).textContent || "") : "";',
        "card.dataset.tone = inferContinuityTone(actionState || currentStateText || currentState || summary);",
        'const continuityTone = inferContinuityTone(`${payload.mobileCampaignActionRequired || ""} ${payload.mobileCampaignStaleState || ""}`);',
        'document.getElementById("shell-continuity-status").dataset.tone = continuityTone;',
        '`${payload.mobileCampaignStaleState || ""} ${payload.mobileCampaignCurrentState || ""}`',
        '`${payload.travelCampaignStaleState || ""} ${payload.travelCampaignCurrentState || ""}`',
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
        'Assert(projection.MobileCampaignActionRequired.Contains("Mobile shell owner: player lane. Session: scene-redmond.", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignCurrentState.Contains("Current continuity posture:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignStateSummary.Contains("Cached state:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignStaleState.Contains("Stale state:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignActionRequired.Contains("Action required:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignActionRequired.Contains("Travel lane: play_tablet on install-tablet.", StringComparison.Ordinal)',
        'Assert(postFailureProjection.EntryStateSummary.Contains("cached, stale, and action-required travel continuity", StringComparison.Ordinal)',
        'Assert(postFailureProjection.RecoveryActions.Any(item => item.Contains("Travel continuity cached state:", StringComparison.Ordinal))',
        'Assert(postFailureProjection.RecoveryActions.Any(item => item.Contains("Travel continuity stale state:", StringComparison.Ordinal))',
        'Assert(postFailureProjection.RecoveryActions.Any(item => item.Contains("Travel continuity action required:", StringComparison.Ordinal))',
        'Assert(html.Contains("id=\\"workspace-mobile-campaign-current-state\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"workspace-mobile-campaign-state\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"restore-travel-campaign-current-state\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"restore-travel-campaign-state\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("setText(\\"workspace-mobile-campaign-current-state\\", payload.mobileCampaignCurrentState, \\"No mobile campaign continuity posture is available yet.\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("setText(\\"workspace-mobile-campaign-state\\", payload.mobileCampaignStateSummary, \\"No mobile campaign-state summary is available yet.\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("setText(\\"workspace-action-required\\", payload.actionRequiredSummary, \\"No action-required summary is available yet.\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("setList(\\"workspace-action-required-list\\", payload.actionRequiredLabels);", StringComparison.Ordinal)',
        'Assert(html.Contains("setText(\\"restore-travel-campaign-current-state\\", payload.travelCampaignCurrentState, \\"No restore travel campaign continuity posture is available yet.\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("setText(\\"restore-travel-campaign-state\\", payload.travelCampaignStateSummary, \\"No restore travel campaign-state summary is available yet.\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("setText(\\"restore-action-required\\", payload.actionRequiredSummary, \\"No restore action-required summary is available yet.\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("setList(\\"restore-action-required-labels\\", payload.actionRequiredLabels);", StringComparison.Ordinal)',
        'Assert(html.Contains("function inferContinuityTone(text)", StringComparison.Ordinal)',
        'Assert(html.Contains("if (lowered.includes(\\"action required\\") || lowered.includes(\\"action-required\\")) {", StringComparison.Ordinal)',
        'Assert(html.Contains("function syncContinuityCardTone(cardId, currentStateId, summaryId, currentStateText, actionId)", StringComparison.Ordinal)',
        'Assert(html.Contains("const actionState = actionId ? (document.getElementById(actionId).textContent || \\"\\") : \\"\\";", StringComparison.Ordinal)',
        'Assert(html.Contains("card.dataset.tone = inferContinuityTone(actionState || currentStateText || currentState || summary);", StringComparison.Ordinal)',
        'Assert(html.Contains("syncContinuityCardTone(\\n      \\"workspace-mobile-campaign-card\\",", StringComparison.Ordinal)',
        'Assert(html.Contains("\\"workspace-mobile-campaign-action-required\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("syncContinuityStateBreakdown(\\n      \\"workspace-mobile-campaign-cached-state\\",", StringComparison.Ordinal)',
        'Assert(html.Contains("syncContinuityCardTone(\\n      \\"restore-travel-campaign-card\\",", StringComparison.Ordinal)',
        'Assert(html.Contains("\\"restore-travel-campaign-action-required\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("syncContinuityStateBreakdown(\\n      \\"restore-travel-campaign-cached-state\\",", StringComparison.Ordinal)',
    ),
    "scripts/materialize_mobile_local_release_proof.py": (
        PACKAGE_ID,
        FRONTIER_ID,
        "mobile_campaign_continuity",
        "scripts/verify_next90_m112_mobile_campaign_continuity.py",
        "campaign_memory:travel",
        "campaign_state:mobile",
        'Assert(projection.MobileCampaignCachedState.Contains("Cached state:", StringComparison.Ordinal)',
        'Assert(projection.MobileCampaignStateLabels.Any(item => item.Contains("Action-required lane:", StringComparison.Ordinal))',
        'Assert(plan.TravelCampaignCachedState.Contains("Cached state:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignStateLabels.Any(item => item.Contains("Travel companion lane:", StringComparison.Ordinal))',
        'Assert(projection.MobileCampaignActionRequired.Contains("Mobile shell owner: player lane. Session: scene-redmond.", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignActionRequired.Contains("Travel lane: play_tablet on install-tablet.", StringComparison.Ordinal)',
        'Assert(postFailureProjection.EntryStateSummary.Contains("cached, stale, and action-required travel continuity", StringComparison.Ordinal)',
        'Assert(postFailureProjection.RecoveryActions.Any(item => item.Contains("Travel continuity cached state:", StringComparison.Ordinal))',
        'Assert(postFailureProjection.RecoveryActions.Any(item => item.Contains("Travel continuity stale state:", StringComparison.Ordinal))',
        'Assert(postFailureProjection.RecoveryActions.Any(item => item.Contains("Travel continuity action required:", StringComparison.Ordinal))',
        'setText("workspace-mobile-campaign-cached-state", payload.mobileCampaignCachedState, "No mobile cached campaign state is available yet.");',
        'setText("workspace-mobile-campaign-stale-state", payload.mobileCampaignStaleState, "No mobile stale campaign state is available yet.");',
        'setText("workspace-mobile-campaign-action-required", payload.mobileCampaignActionRequired, "No mobile campaign action-required posture is available yet.");',
        'setText("restore-travel-campaign-cached-state", payload.travelCampaignCachedState, "No restore travel cached campaign state is available yet.");',
        'setText("restore-travel-campaign-stale-state", payload.travelCampaignStaleState, "No restore travel stale campaign state is available yet.");',
        'setText("restore-travel-campaign-action-required", payload.travelCampaignActionRequired, "No restore travel action-required posture is available yet.");',
        'const continuityTone = inferContinuityTone(`${payload.mobileCampaignActionRequired || ""} ${payload.mobileCampaignStaleState || ""}`);',
        'document.getElementById("shell-continuity-status").dataset.tone = continuityTone;',
        '`${payload.mobileCampaignStaleState || ""} ${payload.mobileCampaignCurrentState || ""}`',
        '`${payload.travelCampaignStaleState || ""} ${payload.travelCampaignCurrentState || ""}`',
        'syncContinuityStateBreakdown(\\n      "workspace-mobile-campaign-cached-state",',
        'syncContinuityStateBreakdown(\\n      "restore-travel-campaign-cached-state",',
        '"generated_at": iso_now(),',
        "Fleet freshness gates key off this timestamp",
    ),
    "scripts/ai/verify.sh": (
        "docs/next90-m112-mobile-campaign-continuity.proof.md",
        "mobile_campaign_continuity",
        "python3 scripts/verify_next90_m112_mobile_campaign_continuity.py >/dev/null",
    ),
}

QUEUE_TOKENS = (
    f"package_id: {PACKAGE_ID}",
    "work_task_id: 112.4",
    f"milestone_id: {MILESTONE_ID}",
    "status: complete",
    "wave: W11",
    f"repo: {REPO_LABEL}",
    "completion_action: verify_closed_package_only",
    "do_not_reopen_reason:",
    "allowed_paths:",
    "- src",
    "- tests",
    "- docs",
    "- scripts",
    "campaign_memory:travel",
    "campaign_state:mobile",
)

REGISTRY_TOKENS = (
    "id: 112.4",
    "owner: chummer6-mobile",
)

PROOF_TOKENS = (
    f"Package: `{PACKAGE_ID}`",
    "Milestone: `112`",
    f"Concrete checkout root: `{CHECKOUT_ROOT}`",
    f"Canonical queue/registry repo label: `{REPO_LABEL}`",
    "Allowed package paths: `src`, `tests`, `docs`, `scripts`",
    f"Active flagship frontier: `{ACTIVE_FLAGSHIP_FRONTIER_ID}`",
    "campaign_memory:travel",
    "campaign_state:mobile",
    "current posture plus cached, stale, and action-required campaign continuity",
    "tone-aware continuity state summaries",
    "repeats the travel continuity cached, stale, and action-required breakdown inside the recovery action list",
    "MobileCampaignCurrentState",
    "TravelCampaignCurrentState",
    "cached, stale, and action-required campaign continuity",
    "scripts/verify_next90_m112_mobile_campaign_continuity.py",
    "MOBILE_LOCAL_RELEASE_PROOF.generated.json",
    "closed-package proof posture",
    "repo-local `implemented` receipt",
    "always renews the generated proof timestamp on rerun",
    "Canonical anchors:",
    "NEXT_90_DAY_PRODUCT_ADVANCE_REGISTRY.yaml",
    "NEXT_90_DAY_QUEUE_STAGING.generated.yaml",
    "The implementation and proof remain intentionally scoped to the package-owned paths above",
    "canonical queue and registry rows now record this package as `complete` under `verify_closed_package_only`",
)

SIGNOFF_TOKENS = (
    "Mobile campaign continuity criteria (M112)",
    "next90-m112-mobile-campaign-continuity",
    "MobileCampaignCurrentState",
    "MobileCampaignStateSummary",
    "TravelCampaignCurrentState",
    "TravelCampaignStateSummary",
    "Recovery proof criteria:",
    "VerifyEntryRecoveryProjectionCoversNoSessionNoCampaignAndPostFailure",
)

GENERATED_TOKENS = (
    PACKAGE_ID,
    FRONTIER_ID,
    ACTIVE_FLAGSHIP_FRONTIER_ID,
    "mobile_campaign_continuity",
    REPO_LABEL,
    CHECKOUT_ROOT,
    "campaign_memory:travel",
    "campaign_state:mobile",
    '"allowed_paths": [',
    '"src"',
    '"tests"',
    '"docs"',
    '"scripts"',
    "Mobile campaign continuity criteria (M112)",
    "docs/next90-m112-mobile-campaign-continuity.proof.md",
    "scripts/verify_next90_m112_mobile_campaign_continuity.py",
)

FORBIDDEN_PROOF_MARKERS = (
    "operator telemetry",
    "active-run helper",
    "supervisor status",
    "TASK_LOCAL_TELEMETRY.generated.json",
    "ACTIVE_RUN_HANDOFF.generated.md",
)

FORBIDDEN_IN_PROGRESS_ROW_MARKERS = (
    "status: in_progress",
)

EXPECTED_QUEUE_ROW = {
    "title": "Make mobile travel campaign continuity explicit for stale campaign state",
    "task": "Make travel and mobile campaign continuity explicit for stale, cached, and action-required campaign state.",
    "package_id": PACKAGE_ID,
    "work_task_id": 112.4,
    "milestone_id": 112,
    "status": "complete",
    "wave": "W11",
    "repo": REPO_LABEL,
    "completion_action": "verify_closed_package_only",
    "do_not_reopen_reason": "M112 chummer6-mobile travel and mobile campaign continuity is complete; future shards must verify the closed-package guard, package proof, regression coverage, and canonical queue plus registry rows instead of reopening this slice.",
    "allowed_paths": ["src", "tests", "docs", "scripts"],
    "proof": [
        "/docker/chummercomplete/chummer-play/src/Chummer.Play.Core/Application/PlayCampaignWorkspaceLiteProjector.cs",
        "/docker/chummercomplete/chummer-play/src/Chummer.Play.Core/Roaming/RoamingWorkspaceSyncPlanner.cs",
        "/docker/chummercomplete/chummer-play/src/Chummer.Play.Core/Application/PlayEntryRecoveryProjector.cs",
        "/docker/chummercomplete/chummer-play/src/Chummer.Play.Web/wwwroot/index.html",
        "/docker/chummercomplete/chummer-play/src/Chummer.Play.RegressionChecks/Program.cs",
        "/docker/chummercomplete/chummer-play/docs/next90-m112-mobile-campaign-continuity.proof.md",
        "/docker/chummercomplete/chummer-play/scripts/verify_next90_m112_mobile_campaign_continuity.py",
        "python3 scripts/verify_next90_m112_mobile_campaign_continuity.py",
        "scripts/ai/with-package-plane.sh dotnet run --project src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj",
    ],
    "owned_surfaces": ["campaign_memory:travel", "campaign_state:mobile"],
}

EXPECTED_REGISTRY_ROW = {
    "id": 112.4,
    "owner": REPO_LABEL,
    "title": "Make travel and mobile campaign continuity explicit for stale, cached, and action-required campaign state.",
}


def read_text(path: Path) -> str:
    if not path.is_file():
        raise FileNotFoundError(path)
    return path.read_text(encoding="utf-8")


def require_tokens(label: str, text: str, tokens: tuple[str, ...]) -> list[str]:
    return [f"{label}: {token}" for token in tokens if token not in text]


def require_clean_markers(label: str, text: object, markers: tuple[str, ...]) -> list[str]:
    if isinstance(text, str):
        rendered = text
    else:
        rendered = render_yaml_block(text)
    lowered = rendered.lower()
    return [f"{label}: forbidden marker present: {marker}" for marker in markers if marker.lower() in lowered]


def render_yaml_block(payload: object) -> str:
    return yaml.safe_dump(payload, sort_keys=False).strip()


def normalize_block(text: object) -> str:
    if isinstance(text, str):
        rendered = text
    else:
        rendered = render_yaml_block(text)
    return rendered.strip().replace("\r\n", "\n")


def require_equal_block(label: str, actual: str, expected: str) -> list[str]:
    if not isinstance(actual, str) or not isinstance(expected, str):
        if actual != expected:
            return [f"{label}: block drifted from the canonical M112 closed-package shape"]
        return []
    if normalize_block(actual) != normalize_block(expected):
        return [f"{label}: block drifted from the canonical M112 closed-package shape"]
    return []


def require_exact_mapping_keys(
    label: str,
    payload: dict[str, object],
    expected_keys: tuple[str, ...]) -> list[str]:
    actual_keys = tuple(sorted(payload.keys()))
    if actual_keys != tuple(sorted(expected_keys)):
        return [f"{label}: keys drifted from the canonical M112 local proof receipt shape"]
    return []


def require_unique_receipt_field(
    receipts: list[dict[str, object]],
    *,
    field: str,
    value: object,
    label: str) -> list[str]:
    matches = [item for item in receipts if item.get(field) == value]
    if len(matches) != 1:
        return [f"{label}: expected exactly one package receipt with {field}={value!r}, found {len(matches)}"]
    return []


def require_unique_surface_membership(
    receipts: list[dict[str, object]],
    *,
    surface: str,
    label: str) -> list[str]:
    matches = [
        item for item in receipts
        if surface in item.get("owned_surfaces", [])
    ]
    if len(matches) != 1:
        return [f"{label}: expected surface {surface!r} on exactly one package receipt, found {len(matches)}"]
    return []


def read_yaml(path: Path) -> object:
    if not path.is_file():
        raise FileNotFoundError(path)
    return yaml.safe_load(path.read_text(encoding="utf-8"))


def queue_rows(payload: object) -> list[dict[str, object]]:
    if not isinstance(payload, dict):
        return []
    items = payload.get("items")
    if not isinstance(items, list):
        return []
    return [row for row in items if isinstance(row, dict) and row.get("package_id") == PACKAGE_ID]


def registry_rows(payload: object) -> list[dict[str, object]]:
    if not isinstance(payload, dict):
        return []
    milestones = payload.get("milestones")
    if not isinstance(milestones, list):
        return []
    matches: list[dict[str, object]] = []
    for milestone in milestones:
        if not isinstance(milestone, dict):
            continue
        work_tasks = milestone.get("work_tasks")
        if not isinstance(work_tasks, list):
            continue
        for task in work_tasks:
            if isinstance(task, dict) and str(task.get("id")) == str(WORK_TASK_ID):
                matches.append(task)
    return matches


def require_single_queue_row(label: str, rows: list[dict[str, object]]) -> list[str]:
    if len(rows) != 1:
        return [f"{label}: expected exactly one {PACKAGE_ID} row, found {len(rows)}"]
    return []


def require_single_registry_row(rows: list[dict[str, object]]) -> list[str]:
    if len(rows) != 1:
        return [f"registry row: expected exactly one {WORK_TASK_ID} row, found {len(rows)}"]
    return []


def main() -> int:
    missing: list[str] = []

    try:
        queue_text = read_text(FLEET_QUEUE)
        design_queue_text = read_text(DESIGN_QUEUE)
        registry_text = read_text(REGISTRY)
        queue_payload = read_yaml(FLEET_QUEUE)
        design_queue_payload = read_yaml(DESIGN_QUEUE)
        registry_payload = read_yaml(REGISTRY)
        proof_text = read_text(PROOF_DOC)
        signoff_text = read_text(PLAY_SIGNOFF)
        generated_text = read_text(GENERATED_PROOF)
    except FileNotFoundError as exc:
        print(f"m112_mobile_campaign_continuity_verify_failed: missing file: {exc}", file=sys.stderr)
        return 1

    queue_rows_found = queue_rows(queue_payload)
    design_queue_rows_found = queue_rows(design_queue_payload)
    registry_rows_found = registry_rows(registry_payload)
    queue_row = queue_rows_found[0] if queue_rows_found else {}
    design_queue_row = design_queue_rows_found[0] if design_queue_rows_found else {}
    registry_row = registry_rows_found[0] if registry_rows_found else {}

    for relative_path, markers in IMPLEMENTATION_MARKERS.items():
        source_text = read_text(ROOT / relative_path)
        missing.extend(require_tokens(relative_path, source_text, markers))

    missing.extend(require_single_queue_row("fleet queue", queue_rows_found))
    missing.extend(require_single_queue_row("design queue", design_queue_rows_found))
    missing.extend(require_single_registry_row(registry_rows_found))
    missing.extend(require_tokens("fleet queue", render_yaml_block(queue_row), QUEUE_TOKENS))
    missing.extend(require_tokens("design queue", render_yaml_block(design_queue_row), QUEUE_TOKENS))
    missing.extend(require_tokens("registry row", render_yaml_block(registry_row), REGISTRY_TOKENS))
    missing.extend(require_equal_block("fleet queue", queue_row, EXPECTED_QUEUE_ROW))
    missing.extend(require_equal_block("design queue", design_queue_row, EXPECTED_QUEUE_ROW))
    missing.extend(require_equal_block("queue mirror parity", queue_row, design_queue_row))
    missing.extend(require_equal_block("registry row", registry_row, EXPECTED_REGISTRY_ROW))
    missing.extend(require_tokens("proof_doc", proof_text, PROOF_TOKENS))
    missing.extend(require_tokens("play_signoff", signoff_text, SIGNOFF_TOKENS))
    missing.extend(require_tokens("generated_proof_text", generated_text, GENERATED_TOKENS))

    payload = json.loads(generated_text)
    if payload.get("contract_name") != "chummer6-mobile.local_release_proof":
        missing.append("generated_proof_payload: wrong contract_name")
    if payload.get("status") != "passed":
        missing.append("generated_proof_payload: status is not passed")

    source_files = payload.get("source_files")
    if not isinstance(source_files, list):
        missing.append("generated_proof_payload: source_files missing")
    else:
        for required_source in (
            "docs/next90-m112-mobile-campaign-continuity.proof.md",
            "scripts/verify_next90_m112_mobile_campaign_continuity.py",
        ):
            if required_source not in source_files:
                missing.append(f"generated_proof_payload: source_files missing {required_source}")

    journeys = payload.get("journeys_passed")
    if not isinstance(journeys, list) or "mobile_campaign_continuity" not in journeys:
        missing.append("generated_proof_payload: mobile_campaign_continuity journey missing")

    required_markers = payload.get("required_markers")
    if not isinstance(required_markers, dict) or "mobile_campaign_continuity" not in required_markers:
        missing.append("generated_proof_payload: mobile_campaign_continuity required markers missing")
    else:
        mobile_markers = required_markers.get("mobile_campaign_continuity")
        if not isinstance(mobile_markers, list):
            missing.append("generated_proof_payload: mobile_campaign_continuity required markers are not a list")
        else:
            for required_marker in (
                'Assert(projection.MobileCampaignCurrentState.Contains("Current continuity posture:", StringComparison.Ordinal)',
                'Assert(plan.TravelCampaignCurrentState.Contains("Current continuity posture:", StringComparison.Ordinal)',
                'setText("workspace-mobile-campaign-current-state", payload.mobileCampaignCurrentState, "No mobile campaign continuity posture is available yet.");',
                'setText("restore-travel-campaign-current-state", payload.travelCampaignCurrentState, "No restore travel campaign continuity posture is available yet.");',
                'setList("workspace-mobile-campaign-state-list", payload.mobileCampaignStateLabels);',
                'setList("restore-travel-campaign-state-labels", payload.travelCampaignStateLabels);',
                'const continuityTone = inferContinuityTone(`${payload.mobileCampaignActionRequired || ""} ${payload.mobileCampaignStaleState || ""}`);',
                'document.getElementById("shell-continuity-status").dataset.tone = continuityTone;',
                'function syncContinuityCardTone(cardId, currentStateId, summaryId, currentStateText, actionId)',
                'card.dataset.tone = inferContinuityTone(actionState || currentStateText || currentState || summary);',
                '`${payload.mobileCampaignStaleState || ""} ${payload.mobileCampaignCurrentState || ""}`',
                '`${payload.travelCampaignStaleState || ""} ${payload.travelCampaignCurrentState || ""}`',
                'syncContinuityStateBreakdown(\n      "workspace-mobile-campaign-cached-state",',
                'syncContinuityStateBreakdown(\n      "restore-travel-campaign-cached-state",',
                'id="workspace-mobile-campaign-current-state"',
                'id="workspace-mobile-campaign-state"',
                'id="workspace-mobile-campaign-state-list"',
                'id="restore-travel-campaign-current-state"',
                'id="restore-travel-campaign-state"',
                'id="restore-travel-campaign-state-labels"',
            ):
                if required_marker not in mobile_markers:
                    missing.append(f"generated_proof_payload: mobile_campaign_continuity markers missing {required_marker}")

    receipts = payload.get("package_receipts")
    if not isinstance(receipts, list):
        missing.append("generated_proof_payload: package_receipts missing")
    else:
        receipt_dicts = [item for item in receipts if isinstance(item, dict)]
        if len(receipt_dicts) != len(receipts):
            missing.append("generated_proof_payload: package_receipts must contain only mappings")

        matching_marker_receipts = [item for item in receipts if item.get("proof_marker_set") == "mobile_campaign_continuity"]
        if len(matching_marker_receipts) != 1:
            missing.append(f"generated_proof_payload: expected exactly one mobile_campaign_continuity proof-marker receipt, found {len(matching_marker_receipts)}")

        matching_frontier_receipts = [item for item in receipts if str(item.get("frontier_id")) == str(FRONTIER_ID)]
        if len(matching_frontier_receipts) != 1:
            missing.append(f"generated_proof_payload: expected exactly one receipt for frontier {FRONTIER_ID}, found {len(matching_frontier_receipts)}")

        matching_proof_receipts = [item for item in receipts if item.get("proof_receipt") == "docs/next90-m112-mobile-campaign-continuity.proof.md"]
        if len(matching_proof_receipts) != 1:
            missing.append(f"generated_proof_payload: expected exactly one receipt for docs/next90-m112-mobile-campaign-continuity.proof.md, found {len(matching_proof_receipts)}")

        missing.extend(require_unique_receipt_field(
            receipt_dicts,
            field="package_id",
            value=PACKAGE_ID,
            label="generated_proof_payload"))
        missing.extend(require_unique_receipt_field(
            receipt_dicts,
            field="work_task_id",
            value=WORK_TASK_ID,
            label="generated_proof_payload"))
        missing.extend(require_unique_receipt_field(
            receipt_dicts,
            field="title",
            value="Make travel and mobile campaign continuity explicit for stale, cached, and action-required campaign state.",
            label="generated_proof_payload"))
        missing.extend(require_unique_surface_membership(
            receipt_dicts,
            surface="campaign_memory:travel",
            label="generated_proof_payload"))
        missing.extend(require_unique_surface_membership(
            receipt_dicts,
            surface="campaign_state:mobile",
            label="generated_proof_payload"))

        matched_receipts = [item for item in receipts if item.get("package_id") == PACKAGE_ID]
        if len(matched_receipts) != 1:
            missing.append(f"generated_proof_payload: expected exactly one receipt for {PACKAGE_ID}, found {len(matched_receipts)}")
        elif matched_receipts[0] is None:
            missing.append(f"generated_proof_payload: missing receipt for {PACKAGE_ID}")
        else:
            receipt = matched_receipts[0]
            missing.extend(require_exact_mapping_keys(
                "generated_proof_payload: M112 receipt",
                receipt,
                (
                    "allowed_paths",
                    "active_flagship_frontier_id",
                    "checkout_root",
                    "frontier_id",
                    "milestone_id",
                    "owned_surfaces",
                    "package_id",
                    "proof_marker_set",
                    "proof_receipt",
                    "repo",
                    "status",
                    "title",
                    "work_task_id",
                )))
            if str(receipt.get("milestone_id")) != str(MILESTONE_ID):
                missing.append("generated_proof_payload: wrong milestone_id for M112 receipt")
            if str(receipt.get("work_task_id")) != str(WORK_TASK_ID):
                missing.append("generated_proof_payload: wrong work_task_id for M112 receipt")
            if str(receipt.get("frontier_id")) != str(FRONTIER_ID):
                missing.append("generated_proof_payload: wrong frontier_id for M112 receipt")
            if str(receipt.get("active_flagship_frontier_id")) != str(ACTIVE_FLAGSHIP_FRONTIER_ID):
                missing.append("generated_proof_payload: wrong active_flagship_frontier_id for M112 receipt")
            if receipt.get("title") != "Make travel and mobile campaign continuity explicit for stale, cached, and action-required campaign state.":
                missing.append("generated_proof_payload: wrong title for M112 receipt")
            if receipt.get("repo") != REPO_LABEL:
                missing.append("generated_proof_payload: wrong repo for M112 receipt")
            if receipt.get("checkout_root") != CHECKOUT_ROOT:
                missing.append("generated_proof_payload: wrong checkout_root for M112 receipt")
            if tuple(receipt.get("allowed_paths", [])) != ("src", "tests", "docs", "scripts"):
                missing.append("generated_proof_payload: wrong allowed_paths for M112 receipt")
            if receipt.get("status") != "implemented":
                missing.append("generated_proof_payload: wrong status for M112 receipt")
            if receipt.get("proof_marker_set") != "mobile_campaign_continuity":
                missing.append("generated_proof_payload: wrong proof_marker_set for M112 receipt")
            if tuple(receipt.get("owned_surfaces", [])) != ("campaign_memory:travel", "campaign_state:mobile"):
                missing.append("generated_proof_payload: wrong owned_surfaces for M112 receipt")
            if receipt.get("proof_receipt") != "docs/next90-m112-mobile-campaign-continuity.proof.md":
                missing.append("generated_proof_payload: wrong proof_receipt for M112 receipt")

    missing.extend(require_clean_markers("fleet_queue", queue_row, FORBIDDEN_PROOF_MARKERS))
    missing.extend(require_clean_markers("design_queue", design_queue_row, FORBIDDEN_PROOF_MARKERS))
    missing.extend(require_clean_markers("registry_row", registry_row, FORBIDDEN_PROOF_MARKERS))
    missing.extend(require_clean_markers("proof_doc", proof_text, FORBIDDEN_PROOF_MARKERS))
    missing.extend(require_clean_markers("play_signoff", signoff_text, FORBIDDEN_PROOF_MARKERS))
    missing.extend(require_clean_markers("generated_proof_text", generated_text, FORBIDDEN_PROOF_MARKERS))

    for marker in FORBIDDEN_IN_PROGRESS_ROW_MARKERS:
        if marker in queue_row:
            missing.append(f"fleet_queue: closed-package row drifted back to in-progress posture: {marker}")
        if marker in design_queue_row:
            missing.append(f"design_queue: closed-package row drifted back to in-progress posture: {marker}")
        if marker in registry_row:
            missing.append(f"registry_row: closed-package row drifted back to in-progress posture: {marker}")

    if missing:
        for item in missing:
            print(f"m112_mobile_campaign_continuity_verify_failed: {item}", file=sys.stderr)
        return 1

    print("m112_mobile_campaign_continuity_verify_ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
