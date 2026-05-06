#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PACKAGE_ID = "next90-m121-mobile-add-player-table-cards-between-turn-affordances-and-gm-l"
MILESTONE_ID = "121"
WORK_TASK_ID = "121.4"
QUEUE_FRONTIER_ID = "6121780841"
REPO_LABEL = "chummer6-mobile"
CHECKOUT_ROOT = str(ROOT)
REGISTRY = ROOT / ".codex-design" / "product" / "NEXT_90_DAY_PRODUCT_ADVANCE_REGISTRY.yaml"
DESIGN_QUEUE = ROOT / ".codex-design" / "product" / "NEXT_90_DAY_QUEUE_STAGING.generated.yaml"
FLEET_QUEUE = Path("/docker/fleet/.codex-studio/published/NEXT_90_DAY_QUEUE_STAGING.generated.yaml")

PROOF_DOC = ROOT / "docs" / "next90-m121-mobile-live-combat-confidence.proof.md"
PLAY_SIGNOFF = ROOT / "docs" / "PLAY_RELEASE_SIGNOFF.md"
GENERATED_PROOF = ROOT / ".codex-studio" / "published" / "MOBILE_LOCAL_RELEASE_PROOF.generated.json"

IMPLEMENTATION_MARKERS = {
    "src/Chummer.Play.Core/Application/PlayCampaignWorkspaceLiteProjector.cs": (
        "PlayerTableCardsSummary",
        "PlayerTableCardLabels",
        "BetweenTurnAffordancesSummary",
        "BetweenTurnAffordanceLabels",
        "GmLiteContinuitySummary",
        "GmLiteContinuityLabels",
        "BuildPlayerTableCardsSummary",
        "BuildBetweenTurnAffordancesSummary",
        "BuildGmLiteContinuitySummary",
    ),
    "src/Chummer.Play.Web/wwwroot/index.html": (
        'id="workspace-player-table-cards"',
        'id="workspace-player-table-cards-list"',
        'id="workspace-between-turn-affordances"',
        'id="workspace-between-turn-affordances-list"',
        'id="workspace-gm-lite-continuity"',
        'id="workspace-gm-lite-continuity-list"',
        'setText("workspace-player-table-cards", payload.playerTableCardsSummary, "No player table-card summary is available yet.");',
        'setList("workspace-player-table-cards-list", payload.playerTableCardLabels);',
        'setText("workspace-between-turn-affordances", payload.betweenTurnAffordancesSummary, "No between-turn affordance summary is available yet.");',
        'setList("workspace-between-turn-affordances-list", payload.betweenTurnAffordanceLabels);',
        'setText("workspace-gm-lite-continuity", payload.gmLiteContinuitySummary, "No GM-lite continuity summary is available yet.");',
        'setList("workspace-gm-lite-continuity-list", payload.gmLiteContinuityLabels);',
    ),
    "src/Chummer.Play.Player/PlayerShell/PlayerShellModule.cs": (
        "player table cards",
        "between-turn quick actions",
    ),
    "src/Chummer.Play.Gm/TacticalShell/GmTacticalShellModule.cs": (
        "GM-lite continuity",
        "Tactical cards",
    ),
    "src/Chummer.Play.RegressionChecks/Program.cs": (
        'Assert(playerDescriptor.Summary.Contains("player table cards", StringComparison.OrdinalIgnoreCase)',
        'Assert(playerDescriptor.Summary.Contains("between-turn", StringComparison.OrdinalIgnoreCase)',
        'Assert(gmDescriptor.Summary.Contains("GM-lite continuity", StringComparison.Ordinal)',
        'Assert(projection.PlayerTableCardsSummary.Contains("Player table cards:", StringComparison.Ordinal)',
        'Assert(projection.BetweenTurnAffordancesSummary.Contains("Between-turn affordances:", StringComparison.Ordinal)',
        'Assert(projection.GmLiteContinuitySummary.Contains("GM-lite continuity:", StringComparison.Ordinal)',
        'Assert(observerProjection.GmLiteContinuitySummary.Contains("observer lane", StringComparison.OrdinalIgnoreCase)',
        'Assert(gmProjection.PlayerTableCardsSummary.Contains("Advance Initiative", StringComparison.Ordinal)',
        'Assert(gmProjection.GmLiteContinuitySummary.Contains("GM runboard", StringComparison.Ordinal)',
        'Assert(html.Contains("setText(\\"workspace-player-table-cards\\", payload.playerTableCardsSummary, \\"No player table-card summary is available yet.\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("setText(\\"workspace-between-turn-affordances\\", payload.betweenTurnAffordancesSummary, \\"No between-turn affordance summary is available yet.\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("setText(\\"workspace-gm-lite-continuity\\", payload.gmLiteContinuitySummary, \\"No GM-lite continuity summary is available yet.\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"workspace-player-table-cards\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"workspace-between-turn-affordances\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"workspace-gm-lite-continuity\\"", StringComparison.Ordinal)',
    ),
    "scripts/materialize_mobile_local_release_proof.py": (
        "mobile_live_combat_confidence",
        "Mobile live combat confidence criteria (M121)",
        "add_player_table_cards_between:mobile",
        "docs/next90-m121-mobile-live-combat-confidence.proof.md",
        "scripts/verify_next90_m121_mobile_live_combat_confidence.py",
    ),
    "scripts/ai/verify.sh": (
        "docs/next90-m121-mobile-live-combat-confidence.proof.md",
        "scripts/verify_next90_m121_mobile_live_combat_confidence.py",
        'rg -n \'"mobile_live_combat_confidence"\' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null',
    ),
}

QUEUE_TOKENS = (
    f"package_id: {PACKAGE_ID}",
    f"work_task_id: '{WORK_TASK_ID}'",
    f"frontier_id: {QUEUE_FRONTIER_ID}",
    f"milestone_id: {MILESTONE_ID}",
    "status: not_started",
    "repo: chummer6-mobile",
    "owned_surfaces:",
    "add_player_table_cards_between:mobile",
)

REGISTRY_TOKENS = (
    "id: '121.4'",
    "owner: chummer6-mobile",
    "title: Add player table cards, between-turn affordances, and GM-lite continuity views for live combat confidence.",
)

PROOF_TOKENS = (
    f"Package: `{PACKAGE_ID}`",
    f"Work task: `{WORK_TASK_ID}`",
    f"Milestone: `{MILESTONE_ID}`",
    f"Concrete checkout root: `{CHECKOUT_ROOT}`",
    f"Canonical queue/registry repo label: `{REPO_LABEL}`",
    "player table cards",
    "between-turn affordances",
    "GM-lite continuity views",
    "add_player_table_cards_between:mobile",
    "implementation receipt",
    "This proof does not claim queue closure.",
    "The canonical successor queue row remains `not_started`",
    "The repo-local `.codex-design/product/` mirror is the first design anchor for this receipt",
    "scripts/verify_next90_m121_mobile_live_combat_confidence.py",
    "MOBILE_LOCAL_RELEASE_PROOF.generated.json",
    "python3 scripts/verify_next90_m121_mobile_live_combat_confidence.py",
)

SIGNOFF_TOKENS = (
    "Mobile live combat confidence criteria (M121)",
    "player table cards",
    "between-turn affordances",
    "GM-lite continuity views",
    "VerifyBootstrapRoleShellEntryPointsAsync",
)

GENERATED_TOKENS = (
    '"mobile_live_combat_confidence"',
    PACKAGE_ID,
    '"add_player_table_cards_between:mobile"',
    '"docs/next90-m121-mobile-live-combat-confidence.proof.md"',
)

FORBIDDEN_PROOF_MARKERS = (
    "operator telemetry",
    "active-run helper",
    "supervisor status",
    "TASK_LOCAL_TELEMETRY.generated.json",
    "ACTIVE_RUN_HANDOFF.generated.md",
)

FORBIDDEN_IN_PROGRESS_ROW_MARKERS = (
    "completion_action: verify_closed_package_only",
    "do_not_reopen_reason:",
    "status: complete",
    "landed_commit:",
)

EXPECTED_QUEUE_ROW = """- title: Add player table cards, between-turn affordances, and GM-lite continuity views for live combat confidence.
  task: Add player table cards, between-turn affordances, and GM-lite continuity views for live combat confidence.
  package_id: next90-m121-mobile-add-player-table-cards-between-turn-affordances-and-gm-l
  work_task_id: '121.4'
  frontier_id: 6121780841
  milestone_id: 121
  status: not_started
  wave: W15
  repo: chummer6-mobile
  allowed_paths:
  - src
  - tests
  - docs
  - scripts
  owned_surfaces:
  - add_player_table_cards_between:mobile"""

EXPECTED_REGISTRY_ROW = """    - id: '121.4'
      owner: chummer6-mobile
      title: Add player table cards, between-turn affordances, and GM-lite continuity views for live combat confidence."""


def read_text(path: Path) -> str:
    if not path.is_file():
        raise FileNotFoundError(path)
    return path.read_text(encoding="utf-8")


def require_tokens(label: str, text: str, tokens: tuple[str, ...]) -> list[str]:
    return [f"{label}: {token}" for token in tokens if token not in text]


def require_clean_markers(label: str, text: str, markers: tuple[str, ...]) -> list[str]:
    lowered = text.lower()
    return [f"{label}: forbidden marker present: {marker}" for marker in markers if marker.lower() in lowered]


def normalize_block(text: str) -> str:
    return text.strip().replace("\r\n", "\n")


def require_equal_block(label: str, actual: str, expected: str) -> list[str]:
    if normalize_block(actual) != normalize_block(expected):
        return [f"{label}: block drifted from the canonical M121 implementation-only shape"]
    return []


def require_exact_mapping_keys(
    label: str,
    payload: dict[str, object],
    expected_keys: tuple[str, ...]) -> list[str]:
    actual_keys = tuple(sorted(payload.keys()))
    if actual_keys != tuple(sorted(expected_keys)):
        return [f"{label}: keys drifted from the canonical M121 implementation-only receipt shape"]
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
    match = re.search(r"(?ms)^    - id: '121\.4'.*?(?=^    - id: |\Z)", text)
    return match.group(0) if match else ""


def require_single_registry_row(text: str) -> list[str]:
    count = len(re.findall(rf"(?m)^    - id: '{re.escape(WORK_TASK_ID)}'$", text))
    if count != 1:
        return [f"registry row: expected exactly one {WORK_TASK_ID} row, found {count}"]
    return []


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
        print(f"m121_mobile_live_combat_confidence_verify_failed: missing file: {exc}", file=sys.stderr)
        return 1

    queue_row = queue_block(queue_text)
    design_queue_row = queue_block(design_queue_text)
    registry_row = registry_block(registry_text)

    for relative_path, markers in IMPLEMENTATION_MARKERS.items():
        source_text = read_text(ROOT / relative_path)
        missing.extend(require_tokens(relative_path, source_text, markers))

    missing.extend(require_single_queue_row("fleet queue", queue_text))
    missing.extend(require_single_queue_row("design queue", design_queue_text))
    missing.extend(require_single_registry_row(registry_text))
    missing.extend(require_tokens("fleet queue", queue_row, QUEUE_TOKENS))
    missing.extend(require_tokens("design queue", design_queue_row, QUEUE_TOKENS))
    missing.extend(require_tokens("registry row", registry_row, REGISTRY_TOKENS))
    missing.extend(require_equal_block("fleet queue", queue_row, EXPECTED_QUEUE_ROW))
    missing.extend(require_equal_block("design queue", design_queue_row, EXPECTED_QUEUE_ROW))
    missing.extend(require_equal_block("queue mirror parity", queue_row, design_queue_row))
    missing.extend(require_equal_block("registry row", registry_row, EXPECTED_REGISTRY_ROW))
    missing.extend(require_clean_markers("fleet queue", queue_row, FORBIDDEN_IN_PROGRESS_ROW_MARKERS))
    missing.extend(require_clean_markers("design queue", design_queue_row, FORBIDDEN_IN_PROGRESS_ROW_MARKERS))
    missing.extend(require_tokens("proof_doc", proof_text, PROOF_TOKENS))
    missing.extend(require_tokens("play_signoff", signoff_text, SIGNOFF_TOKENS))
    missing.extend(require_tokens("generated_proof_text", generated_text, GENERATED_TOKENS))
    missing.extend(require_clean_markers("play_signoff", signoff_text, FORBIDDEN_PROOF_MARKERS))
    missing.extend(require_clean_markers("generated_proof_text", generated_text, FORBIDDEN_PROOF_MARKERS))

    for marker in FORBIDDEN_PROOF_MARKERS:
        if marker.lower() in proof_text.lower():
            missing.append(f"proof_doc: forbidden marker present: {marker}")

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
            "docs/next90-m121-mobile-live-combat-confidence.proof.md",
            "scripts/verify_next90_m121_mobile_live_combat_confidence.py",
        ):
            if required_source not in source_files:
                missing.append(f"generated_proof_payload: source_files missing {required_source}")

    journeys = payload.get("journeys_passed")
    if not isinstance(journeys, list) or "mobile_live_combat_confidence" not in journeys:
        missing.append("generated_proof_payload: mobile_live_combat_confidence journey missing")

    required_markers = payload.get("required_markers")
    if not isinstance(required_markers, dict) or "mobile_live_combat_confidence" not in required_markers:
        missing.append("generated_proof_payload: mobile_live_combat_confidence required markers missing")
    else:
        mobile_markers = required_markers.get("mobile_live_combat_confidence")
        if not isinstance(mobile_markers, list):
            missing.append("generated_proof_payload: mobile_live_combat_confidence required markers are not a list")
        else:
            for required_marker in (
                'Assert(playerDescriptor.Summary.Contains("player table cards", StringComparison.OrdinalIgnoreCase)',
                'Assert(playerDescriptor.Summary.Contains("between-turn", StringComparison.OrdinalIgnoreCase)',
                'Assert(gmDescriptor.Summary.Contains("GM-lite continuity", StringComparison.Ordinal)',
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
                'id="workspace-player-table-cards-list"',
                'id="workspace-between-turn-affordances-list"',
                'id="workspace-gm-lite-continuity-list"',
                "add_player_table_cards_between:mobile",
            ):
                if required_marker not in mobile_markers:
                    missing.append(f"generated_proof_payload: mobile_live_combat_confidence markers missing {required_marker}")

    receipts = payload.get("package_receipts")
    if not isinstance(receipts, list):
        missing.append("generated_proof_payload: package_receipts missing")
    else:
        receipt_dicts = [item for item in receipts if isinstance(item, dict)]
        if len(receipt_dicts) != len(receipts):
            missing.append("generated_proof_payload: package_receipts must contain only mappings")

        missing.extend(require_unique_receipt_field(receipt_dicts, field="package_id", value=PACKAGE_ID, label="generated_proof_payload"))
        missing.extend(require_unique_receipt_field(receipt_dicts, field="proof_marker_set", value="mobile_live_combat_confidence", label="generated_proof_payload"))
        missing.extend(require_unique_receipt_field(receipt_dicts, field="proof_receipt", value="docs/next90-m121-mobile-live-combat-confidence.proof.md", label="generated_proof_payload"))
        missing.extend(require_unique_surface_membership(receipt_dicts, surface="add_player_table_cards_between:mobile", label="generated_proof_payload"))

        matches = [item for item in receipts if item.get("package_id") == PACKAGE_ID]
        if len(matches) != 1:
            missing.append(f"generated_proof_payload: expected exactly one receipt for {PACKAGE_ID}, found {len(matches)}")
        else:
            receipt = matches[0]
            missing.extend(require_exact_mapping_keys(
                "generated_proof_payload",
                receipt,
                (
                    "allowed_paths",
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
            if receipt.get("status") != "implemented":
                missing.append("generated_proof_payload: M121 receipt status must stay implemented")
            if receipt.get("milestone_id") != MILESTONE_ID:
                missing.append("generated_proof_payload: wrong milestone_id for M121 receipt")
            if receipt.get("work_task_id") != WORK_TASK_ID:
                missing.append("generated_proof_payload: wrong work_task_id for M121 receipt")
            if receipt.get("frontier_id") != QUEUE_FRONTIER_ID:
                missing.append("generated_proof_payload: wrong frontier_id for M121 receipt")
            if receipt.get("proof_marker_set") != "mobile_live_combat_confidence":
                missing.append("generated_proof_payload: wrong proof marker set for M121 receipt")
            if receipt.get("repo") != REPO_LABEL:
                missing.append("generated_proof_payload: wrong repo label for M121 receipt")
            if receipt.get("checkout_root") != CHECKOUT_ROOT:
                missing.append("generated_proof_payload: wrong checkout_root for M121 receipt")
            if receipt.get("proof_receipt") != "docs/next90-m121-mobile-live-combat-confidence.proof.md":
                missing.append("generated_proof_payload: wrong proof receipt for M121 receipt")
            if receipt.get("owned_surfaces") != ["add_player_table_cards_between:mobile"]:
                missing.append("generated_proof_payload: wrong owned surfaces for M121 receipt")
            if receipt.get("allowed_paths") != ["src", "tests", "docs", "scripts"]:
                missing.append("generated_proof_payload: wrong allowed paths for M121 receipt")

    if missing:
        for item in missing:
            print(f"m121_mobile_live_combat_confidence_verify_failed: {item}", file=sys.stderr)
        return 1

    print("m121_mobile_live_combat_confidence_verify_ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
