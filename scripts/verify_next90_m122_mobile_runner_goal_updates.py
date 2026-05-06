#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PACKAGE_ID = "next90-m122-mobile-add-mobile-runner-goal-updates-and-player-safe-consequen"
MILESTONE_ID = "122"
WORK_TASK_ID = "122.4"
QUEUE_FRONTIER_ID = "8138838792"
REPO_LABEL = "chummer6-mobile"
CHECKOUT_ROOT = str(ROOT)
REGISTRY = Path("/docker/chummercomplete/chummer-design/products/chummer/NEXT_90_DAY_PRODUCT_ADVANCE_REGISTRY.yaml")
DESIGN_QUEUE = Path("/docker/chummercomplete/chummer-design/products/chummer/NEXT_90_DAY_QUEUE_STAGING.generated.yaml")
FLEET_QUEUE = Path("/docker/fleet/.codex-studio/published/NEXT_90_DAY_QUEUE_STAGING.generated.yaml")

PROOF_DOC = ROOT / "docs" / "next90-m122-mobile-runner-goal-updates-and-consequence-feed.proof.md"
PLAY_SIGNOFF = ROOT / "docs" / "PLAY_RELEASE_SIGNOFF.md"
GENERATED_PROOF = ROOT / ".codex-studio" / "published" / "MOBILE_LOCAL_RELEASE_PROOF.generated.json"

IMPLEMENTATION_MARKERS = {
    "src/Chummer.Play.Core/Application/PlayCampaignWorkspaceLiteProjector.cs": (
        "RunnerGoalUpdatesSummary",
        "RunnerGoalUpdateLabels",
        "PlayerSafeConsequenceFeedSummary",
        "PlayerSafeConsequenceFeedLabels",
        "BuildRunnerGoalUpdatesSummary",
        "BuildRunnerGoalUpdateLabels",
        "BuildPlayerSafeConsequenceFeedSummary",
        "BuildPlayerSafeConsequenceFeedLabels",
        "Return moments stay player-safe",
        "BLACK LEDGER world truth",
    ),
    "src/Chummer.Play.Web/wwwroot/index.html": (
        'id="workspace-runner-goal-updates"',
        'id="workspace-runner-goal-update-list"',
        'id="workspace-player-safe-consequence-feed"',
        'id="workspace-player-safe-consequence-feed-list"',
        'setText("workspace-runner-goal-updates", payload.runnerGoalUpdatesSummary, "No runner-goal update summary is available yet.");',
        'setList("workspace-runner-goal-update-list", payload.runnerGoalUpdateLabels);',
        'setText("workspace-player-safe-consequence-feed", payload.playerSafeConsequenceFeedSummary, "No player-safe consequence feed summary is available yet.");',
        'setList("workspace-player-safe-consequence-feed-list", payload.playerSafeConsequenceFeedLabels);',
    ),
    "src/Chummer.Play.RegressionChecks/Program.cs": (
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
        'Assert(html.Contains("id=\\"workspace-runner-goal-updates\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"workspace-runner-goal-update-list\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"workspace-player-safe-consequence-feed\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"workspace-player-safe-consequence-feed-list\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("setText(\\"workspace-runner-goal-updates\\", payload.runnerGoalUpdatesSummary, \\"No runner-goal update summary is available yet.\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("setList(\\"workspace-runner-goal-update-list\\", payload.runnerGoalUpdateLabels);", StringComparison.Ordinal)',
        'Assert(html.Contains("setText(\\"workspace-player-safe-consequence-feed\\", payload.playerSafeConsequenceFeedSummary, \\"No player-safe consequence feed summary is available yet.\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("setList(\\"workspace-player-safe-consequence-feed-list\\", payload.playerSafeConsequenceFeedLabels);", StringComparison.Ordinal)',
    ),
    "scripts/materialize_mobile_local_release_proof.py": (
        "mobile_runner_goal_updates",
        "Mobile runner-goal updates and consequence-feed criteria (M122)",
        "add_mobile_runner_goal_updates:mobile",
        "docs/next90-m122-mobile-runner-goal-updates-and-consequence-feed.proof.md",
        "scripts/verify_next90_m122_mobile_runner_goal_updates.py",
    ),
    "scripts/ai/verify.sh": (
        "docs/next90-m122-mobile-runner-goal-updates-and-consequence-feed.proof.md",
        "scripts/verify_next90_m122_mobile_runner_goal_updates.py",
        'rg -n \'"mobile_runner_goal_updates"\' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null',
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
    "add_mobile_runner_goal_updates:mobile",
)

REGISTRY_TOKENS = (
    "id: '122.4'",
    "owner: chummer6-mobile",
    "title: Add mobile runner-goal updates and player-safe consequence feed views for campaign return moments.",
)

PROOF_TOKENS = (
    f"Package: `{PACKAGE_ID}`",
    f"Work task: `{WORK_TASK_ID}`",
    f"Milestone: `{MILESTONE_ID}`",
    f"Concrete checkout root: `{CHECKOUT_ROOT}`",
    f"Canonical queue/registry repo label: `{REPO_LABEL}`",
    "runner-goal return updates",
    "player-safe consequence feed views",
    "add_mobile_runner_goal_updates:mobile",
    "implementation receipt",
    "This proof does not claim queue closure.",
    "The canonical successor queue row remains `not_started`",
    "scripts/verify_next90_m122_mobile_runner_goal_updates.py",
    "MOBILE_LOCAL_RELEASE_PROOF.generated.json",
)

SIGNOFF_TOKENS = (
    "Mobile runner-goal updates and consequence-feed criteria (M122)",
    "RunnerGoalUpdatesSummary",
    "PlayerSafeConsequenceFeedSummary",
    "add_mobile_runner_goal_updates:mobile",
)

GENERATED_TOKENS = (
    '"mobile_runner_goal_updates"',
    PACKAGE_ID,
    '"add_mobile_runner_goal_updates:mobile"',
    '"docs/next90-m122-mobile-runner-goal-updates-and-consequence-feed.proof.md"',
)

FORBIDDEN_PROOF_MARKERS = (
    "operator telemetry",
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

EXPECTED_QUEUE_ROW = """- title: Add mobile runner-goal updates and player-safe consequence feed views for campaign return moments.
  task: Add mobile runner-goal updates and player-safe consequence feed views for campaign return moments.
  package_id: next90-m122-mobile-add-mobile-runner-goal-updates-and-player-safe-consequen
  work_task_id: '122.4'
  frontier_id: 8138838792
  milestone_id: 122
  status: not_started
  wave: W15
  repo: chummer6-mobile
  allowed_paths:
  - src
  - tests
  - docs
  - scripts
  owned_surfaces:
  - add_mobile_runner_goal_updates:mobile"""

EXPECTED_REGISTRY_ROW = """    - id: '122.4'
      owner: chummer6-mobile
      title: Add mobile runner-goal updates and player-safe consequence feed views for campaign return moments."""


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
        return [f"{label}: block drifted from the canonical M122 implementation-only shape"]
    return []


def require_exact_mapping_keys(
    label: str,
    payload: dict[str, object],
    expected_keys: tuple[str, ...]) -> list[str]:
    actual_keys = tuple(sorted(payload.keys()))
    if actual_keys != tuple(sorted(expected_keys)):
        return [f"{label}: keys drifted from the canonical M122 implementation-only receipt shape"]
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
    matches = [item for item in receipts if surface in item.get("owned_surfaces", [])]
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
    match = re.search(r"(?ms)^    - id: '122\.4'.*?(?=^    - id: |\Z)", text)
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
        print(f"m122_mobile_runner_goal_updates_verify_failed: missing file: {exc}", file=sys.stderr)
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
    missing.extend(require_clean_markers("proof_doc", proof_text, FORBIDDEN_PROOF_MARKERS))

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
            "docs/next90-m122-mobile-runner-goal-updates-and-consequence-feed.proof.md",
            "scripts/verify_next90_m122_mobile_runner_goal_updates.py",
        ):
            if required_source not in source_files:
                missing.append(f"generated_proof_payload: source_files missing {required_source}")

    journeys = payload.get("journeys_passed")
    if not isinstance(journeys, list) or "mobile_runner_goal_updates" not in journeys:
        missing.append("generated_proof_payload: mobile_runner_goal_updates journey missing")

    required_markers = payload.get("required_markers")
    if not isinstance(required_markers, dict) or "mobile_runner_goal_updates" not in required_markers:
        missing.append("generated_proof_payload: mobile_runner_goal_updates required markers missing")
    else:
        mobile_markers = required_markers.get("mobile_runner_goal_updates")
        if not isinstance(mobile_markers, list):
            missing.append("generated_proof_payload: mobile_runner_goal_updates required markers are not a list")
        else:
            for required_marker in (
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
                'setText("workspace-runner-goal-updates", payload.runnerGoalUpdatesSummary, "No runner-goal update summary is available yet.");',
                'setList("workspace-runner-goal-update-list", payload.runnerGoalUpdateLabels);',
                'setText("workspace-player-safe-consequence-feed", payload.playerSafeConsequenceFeedSummary, "No player-safe consequence feed summary is available yet.");',
                'setList("workspace-player-safe-consequence-feed-list", payload.playerSafeConsequenceFeedLabels);',
                "add_mobile_runner_goal_updates:mobile",
            ):
                if required_marker not in mobile_markers:
                    missing.append(f"generated_proof_payload: mobile_runner_goal_updates markers missing {required_marker}")

    receipts = payload.get("package_receipts")
    if not isinstance(receipts, list):
        missing.append("generated_proof_payload: package_receipts missing")
    else:
        receipt_dicts = [item for item in receipts if isinstance(item, dict)]
        if len(receipt_dicts) != len(receipts):
            missing.append("generated_proof_payload: package_receipts must contain only mappings")

        missing.extend(require_unique_receipt_field(receipt_dicts, field="package_id", value=PACKAGE_ID, label="generated_proof_payload"))
        missing.extend(require_unique_receipt_field(receipt_dicts, field="proof_marker_set", value="mobile_runner_goal_updates", label="generated_proof_payload"))
        missing.extend(require_unique_receipt_field(receipt_dicts, field="proof_receipt", value="docs/next90-m122-mobile-runner-goal-updates-and-consequence-feed.proof.md", label="generated_proof_payload"))
        missing.extend(require_unique_surface_membership(receipt_dicts, surface="add_mobile_runner_goal_updates:mobile", label="generated_proof_payload"))

        matches = [item for item in receipt_dicts if item.get("package_id") == PACKAGE_ID]
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
                missing.append("generated_proof_payload: M122 receipt status must stay implemented")
            if receipt.get("milestone_id") != MILESTONE_ID:
                missing.append("generated_proof_payload: wrong milestone_id for M122 receipt")
            if receipt.get("work_task_id") != WORK_TASK_ID:
                missing.append("generated_proof_payload: wrong work_task_id for M122 receipt")
            if receipt.get("frontier_id") != QUEUE_FRONTIER_ID:
                missing.append("generated_proof_payload: wrong frontier_id for M122 receipt")
            if receipt.get("proof_marker_set") != "mobile_runner_goal_updates":
                missing.append("generated_proof_payload: wrong proof marker set for M122 receipt")
            if receipt.get("repo") != REPO_LABEL:
                missing.append("generated_proof_payload: wrong repo label for M122 receipt")
            if receipt.get("checkout_root") != CHECKOUT_ROOT:
                missing.append("generated_proof_payload: wrong checkout_root for M122 receipt")
            if receipt.get("proof_receipt") != "docs/next90-m122-mobile-runner-goal-updates-and-consequence-feed.proof.md":
                missing.append("generated_proof_payload: wrong proof receipt for M122 receipt")
            if receipt.get("owned_surfaces") != ["add_mobile_runner_goal_updates:mobile"]:
                missing.append("generated_proof_payload: wrong owned surfaces for M122 receipt")
            if receipt.get("allowed_paths") != ["src", "tests", "docs", "scripts"]:
                missing.append("generated_proof_payload: wrong allowed paths for M122 receipt")

    if missing:
        for item in missing:
            print(f"m122_mobile_runner_goal_updates_verify_failed: {item}", file=sys.stderr)
        return 1

    print("m122_mobile_runner_goal_updates_verify_ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
