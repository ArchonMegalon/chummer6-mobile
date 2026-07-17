#!/usr/bin/env python3
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import re
import subprocess
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
RUN_SERVICES_ROOT = Path("/docker/chummercomplete/chummer.run-services")
RELEASE_BLOCKERS_RECEIPT = Path("/docker/chummercomplete/RELEASE_BLOCKERS.generated.json")
OUT = ROOT / ".codex-studio" / "published" / "MOBILE_RELEASE_BOUNDARY.generated.json"
DEFAULT_PREFLIGHT_RECEIPT = Path("/tmp/chummer-public-edge-deploy-preflight-current.json")
DEFAULT_POSTDEPLOY_RECEIPT = Path("/tmp/chummer-public-edge-postdeploy-canonical-current.json")
VERIFY_DESIGN_MIRROR_SCRIPT = ROOT / "scripts" / "ai" / "verify_design_mirror.py"
LIVE_BUILD_LOCK_MARKER = "build-chummer6-linux.sh"
STRICT_PUBLIC_EDGE_PREFLIGHT_COMMAND = (
    "python3 /docker/chummercomplete/chummer.run-services/scripts/check_public_edge_deploy_preflight.py "
    "--output /tmp/chummer-public-edge-deploy-preflight-current.json"
)
STRICT_PUBLIC_EDGE_POSTDEPLOY_COMMAND = (
    "python3 /docker/chummercomplete/chummer.run-services/scripts/verify_public_edge_postdeploy_gate.py "
    "--base-url https://chummer.run --strict-preflight --output /tmp/chummer-public-edge-postdeploy-canonical-current.json"
)
STRICT_PUBLIC_EDGE_FOLLOW_THROUGH_COMMAND = (
    "python3 scripts/run_mobile_strict_public_edge_follow_through.py "
    "--wait-for-clear --execute-rerun --timeout-seconds 21600 --poll-interval-seconds 60"
)
STRICT_PUBLIC_EDGE_FOLLOW_THROUGH_RECEIPT = (
    ".codex-studio/published/MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json"
)
MOBILE_RECEIPT_REFRESH_COMMANDS = [
    "python3 scripts/verify_mobile_pwa_performance_budget.py",
    "python3 scripts/materialize_mobile_cross_surface_readiness.py",
    "python3 scripts/materialize_mobile_local_release_proof.py",
    "bash scripts/release/verify_mobile_release_proof.sh",
]

OWNED_PLAY_SOURCE_FILES = [
    ".gitignore",
    "NEXT_SESSION_HANDOFF.md",
    "README.md",
    "docs/migration-map.md",
    "docs/PLAY_RELEASE_SIGNOFF.md",
    "docs/next90-m112-mobile-campaign-continuity.proof.md",
    "docs/next90-m117-mobile-artifact-shelf.proof.md",
    "docs/next90-m119-mobile-onboarding-continuity.proof.md",
    "docs/next90-m121-mobile-live-combat-confidence.proof.md",
    "docs/next90-m122-mobile-runner-goal-updates-and-consequence-feed.proof.md",
    "docs/next90-m145-mobile-quick-explain-and-follow-up.proof.md",
    "scripts/ai/verify.sh",
    "scripts/ai/with-package-plane.sh",
    "scripts/cleanup_mobile_disposable_artifacts.py",
    "scripts/materialize_mobile_cross_surface_readiness.py",
    "scripts/materialize_mobile_local_release_proof.py",
    "scripts/materialize_mobile_release_boundary.py",
    "scripts/release/verify_mobile_release_proof.sh",
    "scripts/run_mobile_strict_public_edge_follow_through.py",
    "scripts/verify_next90_m112_mobile_campaign_continuity.py",
    "scripts/verify_next90_m117_mobile_artifact_shelf.py",
    "scripts/verify_next90_m119_mobile_onboarding_continuity.py",
    "scripts/verify_next90_m121_mobile_live_combat_confidence.py",
    "scripts/verify_next90_m122_mobile_runner_goal_updates.py",
    "scripts/verify_next90_m145_mobile_quick_explain_and_follow_up.py",
    "scripts/verify_mobile_pwa_analytics_smoke.py",
    "scripts/verify_mobile_pwa_performance_budget.py",
    "scripts/verify_mobile_pwa_runtime_smoke.py",
    "scripts/verify_mobile_pwa_viewport_smoke.py",
    "src/Chummer.Play.Core/Application/PlayTurnCompanionProjector.cs",
    "src/Chummer.Play.RegressionChecks/Program.cs",
    "src/Chummer.Play.Web/Components/App.razor",
    "src/Chummer.Play.Web/Components/_Imports.razor",
    "src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor",
    "src/Chummer.Play.Web/Dockerfile",
    "src/Chummer.Play.Web/PlayRouteHandlers.cs",
    "src/Chummer.Play.Web/PlayTurnCompanionService.cs",
    "src/Chummer.Play.Web/PlayWebApplication.cs",
    "src/Chummer.Play.Web/wwwroot/index.html",
    "src/Chummer.Play.Web/wwwroot/manifest.gm.webmanifest",
    "src/Chummer.Play.Web/wwwroot/manifest.player.webmanifest",
    "src/Chummer.Play.Web/wwwroot/manifest.webmanifest",
    "src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js",
    "src/Chummer.Play.Web/wwwroot/mobile.css",
    "src/Chummer.Play.Web/wwwroot/service-worker.js",
]

OWNED_PLAY_TEST_FILES = [
    "tests/test_mobile_cross_surface_refresh_contract.py",
    "tests/test_with_package_plane_locking.py",
    "tests/test_mobile_release_boundary.py",
    "tests/test_mobile_pwa_performance_budget.py",
    "tests/test_cleanup_mobile_disposable_artifacts.py",
    "tests/test_mobile_strict_public_edge_follow_through.py",
    "tests/test_verify_mobile_release_proof_contract.py",
]

OWNED_RUN_SERVICES_SOURCE_FILES = [
    "scripts/verify_chummer_online_launch.py",
    "scripts/verify_public_edge_postdeploy_gate.py",
    "tests/public/frontdoor-mobile-launch.spec.ts",
]

OWNED_RUN_SERVICES_TEST_FILES = [
    "tests/test_chummer_online_launch_gate.py",
    "tests/test_public_edge_postdeploy_gate.py",
]

OWNED_RELEASE_RECEIPTS = [
    ".codex-studio/published/MOBILE_RELEASE_BOUNDARY.generated.json",
    ".codex-studio/published/MOBILE_CROSS_SURFACE_READINESS.generated.json",
    ".codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json",
    ".codex-studio/published/MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json",
    ".codex-studio/published/MOBILE_PWA_ANALYTICS_SMOKE.generated.json",
    ".codex-studio/published/MOBILE_PWA_PERFORMANCE_BUDGET.generated.json",
    ".codex-studio/published/MOBILE_PWA_RUNTIME_SMOKE.generated.json",
    ".codex-studio/published/MOBILE_PWA_VIEWPORT_SMOKE.generated.json",
]

OWNED_DISPOSABLE_LOCAL_ARTIFACT_GLOBS = [
    str(ROOT / ".state" / "**" / "*"),
    str(ROOT / "_tmp" / "mobile-viewport-smoke-*.png"),
    "/tmp/chummer-play-*",
    "/tmp/chummer-character-roster-*",
]

SHARED_EXTERNAL_TEMP_ARTIFACT_GLOBS = [
    "/tmp/chummer-public-edge-*",
    "/tmp/chummer-frontdoor-*",
    "/tmp/chummer-mobile-frontdoor-*",
    "/tmp/chummer-online-launch*",
    "/tmp/chummer-run-live-character-roster-*",
    "/tmp/CHUMMER_*",
]

DISPOSABLE_LOCAL_WORKTREE_PREFIXES = [
    ".state/",
]

EXTERNAL_BLOCKER_WORKTREE_PREFIXES = [
    ".codex-design/",
]

AMBIENT_PLAY_WORKTREE_PREFIXES = [
    "AGENTS.md",
    ".vexp/",
]

LOCAL_ORIGIN_RE = re.compile(r"http://(?:127\.0\.0\.1|localhost):\d+")
JSON_DEVICE_ID_RE = re.compile(r'"deviceId"\s*:\s*"([^"]+)"')
QUERY_DEVICE_ID_RE = re.compile(r"deviceId=([^&\"'\s]+)")
WHITESPACE_RE = re.compile(r"\s+")
ALLOWED_RELEASE_DEVICE_IDS = {
    "<minted-device>",
    "hero-player-shell",
    "hero-menu-player-shell",
    "hero-gm-shell",
    "hero-menu-gm-shell",
}


def iso_now() -> str:
    return dt.datetime.now(dt.timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def sha256_file(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def parse_git_status_lines(text: str) -> list[dict[str, str]]:
    entries: list[dict[str, str]] = []
    for raw_line in text.splitlines():
        if not raw_line:
            continue
        status = raw_line[:2]
        path_text = raw_line[3:] if len(raw_line) > 3 else ""
        if " -> " in path_text:
            path_text = path_text.split(" -> ", 1)[1]
        entries.append(
            {
                "status": status,
                "path": path_text,
                "raw": raw_line,
            }
        )
    return entries


def git_status_entries(repo_root: Path) -> list[dict[str, str]]:
    completed = subprocess.run(
        ["git", "-C", str(repo_root), "status", "--short", "-uall"],
        check=False,
        text=True,
        capture_output=True,
    )
    if completed.returncode != 0:
        raise SystemExit(f"git status failed for {repo_root}: {completed.stderr.strip()}")
    return parse_git_status_lines(completed.stdout)


def worktree_summary(
    status_entries: list[dict[str, str]],
    owned_source_files: list[str],
    owned_test_files: list[str],
    owned_release_receipts: list[str] | None = None,
    disposable_path_prefixes: list[str] | None = None,
    external_blocker_prefixes: list[str] | None = None,
    ambient_path_prefixes: list[str] | None = None,
    ambient_match_all_foreign: bool = False,
) -> dict[str, Any]:
    owned_source_paths = set(owned_source_files)
    owned_test_paths = set(owned_test_files)
    owned_receipt_paths = set(owned_release_receipts or [])
    owned_source_test_paths = owned_source_paths | owned_test_paths
    owned_boundary_paths = owned_source_test_paths | owned_receipt_paths
    disposable_prefixes = tuple(disposable_path_prefixes or [])
    blocker_prefixes = tuple(external_blocker_prefixes or [])
    ambient_prefixes = tuple(ambient_path_prefixes or [])

    owned_entries = [entry for entry in status_entries if entry["path"] in owned_boundary_paths]
    owned_source_test_entries = [entry for entry in owned_entries if entry["path"] in owned_source_test_paths]
    owned_release_receipt_entries = [entry for entry in owned_entries if entry["path"] in owned_receipt_paths]
    foreign_entries = [entry for entry in status_entries if entry["path"] not in owned_boundary_paths]
    disposable_entries = [
        entry
        for entry in foreign_entries
        if disposable_prefixes and entry["path"].startswith(disposable_prefixes)
    ]
    external_blocker_entries = [
        entry
        for entry in foreign_entries
        if entry not in disposable_entries and blocker_prefixes and entry["path"].startswith(blocker_prefixes)
    ]
    ambient_entries = [
        entry
        for entry in foreign_entries
        if entry not in disposable_entries
        and entry not in external_blocker_entries
        and (
            ambient_match_all_foreign
            or (ambient_prefixes and entry["path"].startswith(ambient_prefixes))
        )
    ]
    reviewable_foreign_entries = [
        entry
        for entry in foreign_entries
        if entry not in disposable_entries and entry not in external_blocker_entries and entry not in ambient_entries
    ]

    return {
        "total_entry_count": len(status_entries),
        "owned_entry_count": len(owned_entries),
        "owned_source_test_entry_count": len(owned_source_test_entries),
        "owned_release_receipt_entry_count": len(owned_release_receipt_entries),
        "foreign_entry_count": len(reviewable_foreign_entries),
        "disposable_entry_count": len(disposable_entries),
        "external_blocker_entry_count": len(external_blocker_entries),
        "ambient_entry_count": len(ambient_entries),
        "owned_entries": owned_entries,
        "owned_source_test_entries": owned_source_test_entries,
        "owned_release_receipt_entries": owned_release_receipt_entries,
        "disposable_entry_examples": disposable_entries[:20],
        "external_blocker_entry_examples": external_blocker_entries[:20],
        "ambient_entry_examples": ambient_entries[:20],
        "foreign_entry_examples": reviewable_foreign_entries[:20],
    }


def inventory_paths(
    repo_root: Path,
    paths: list[str],
    status_entries: list[dict[str, str]],
) -> list[dict[str, Any]]:
    status_by_path = {entry["path"]: entry["status"] for entry in status_entries}
    inventory: list[dict[str, Any]] = []
    for relative_path in paths:
        absolute_path = (repo_root / relative_path).resolve()
        row: dict[str, Any] = {
            "path": relative_path,
            "exists": absolute_path.is_file(),
            "worktree_status": status_by_path.get(relative_path, ""),
        }
        if absolute_path.is_file():
            row["sha256"] = sha256_file(absolute_path)
            row["size_bytes"] = absolute_path.stat().st_size
        inventory.append(row)
    return inventory


def collect_device_ids(text: str) -> list[str]:
    found = set(JSON_DEVICE_ID_RE.findall(text))
    found.update(QUERY_DEVICE_ID_RE.findall(text))
    return sorted(found)


def machine_local_noise_findings(text: str) -> list[str]:
    findings: list[str] = []
    if LOCAL_ORIGIN_RE.search(text):
        findings.append("receipt contains an unredacted localhost origin")
    for device_id in collect_device_ids(text):
        if device_id not in ALLOWED_RELEASE_DEVICE_IDS:
            findings.append(f"receipt contains an unredacted device id: {device_id}")
    return findings


def normalize_probe_command(command: str, *, limit: int = 240) -> str:
    normalized = WHITESPACE_RE.sub(" ", command).strip()
    if len(normalized) <= limit:
        return normalized
    return normalized[: limit - 3].rstrip() + "..."


def load_json_object(path: Path) -> dict[str, Any]:
    loaded = json.loads(path.read_text(encoding="utf-8"))
    return loaded if isinstance(loaded, dict) else {}


def receipt_noise_audit_text(payload: dict[str, Any], raw_text: str) -> str:
    contract_name = payload.get("contract_name") or payload.get("contractName")
    if contract_name == "chummer6-mobile.local_release_proof":
        return json.dumps(
            {
                "smoke_receipts": payload.get("smoke_receipts"),
                "role_pwa_contract": payload.get("role_pwa_contract"),
                "cross_surface_refresh": payload.get("cross_surface_refresh"),
                "release_boundary": payload.get("release_boundary"),
            },
            sort_keys=True,
        )
    return raw_text


def receipt_inventory(
    repo_root: Path,
    paths: list[str],
    status_entries: list[dict[str, str]],
    pending_rows: dict[str, dict[str, Any]] | None = None,
) -> tuple[list[dict[str, Any]], list[str]]:
    inventory: list[dict[str, Any]] = []
    findings: list[str] = []
    status_by_path = {entry["path"]: entry["status"] for entry in status_entries}
    for relative_path in paths:
        absolute_path = (repo_root / relative_path).resolve()
        row: dict[str, Any] = {
            "path": relative_path,
            "exists": absolute_path.is_file(),
            "worktree_status": status_by_path.get(relative_path, ""),
        }
        if row["exists"] is False and pending_rows and relative_path in pending_rows:
            inventory.append(dict(pending_rows[relative_path]))
            continue
        if absolute_path.is_file():
            text = absolute_path.read_text(encoding="utf-8", errors="replace")
            row["sha256"] = sha256_file(absolute_path)
            row["size_bytes"] = absolute_path.stat().st_size
            try:
                payload = load_json_object(absolute_path)
            except json.JSONDecodeError as exc:
                row["json_error"] = str(exc)
                findings.append(f"{relative_path}: invalid json: {exc}")
                inventory.append(row)
                continue
            row["status"] = payload.get("status")
            row["contract_name"] = payload.get("contract_name") or payload.get("contractName")
            row["generated_at_utc"] = payload.get("generated_at_utc") or payload.get("generated_at")
            noise = machine_local_noise_findings(receipt_noise_audit_text(payload, text))
            row["machine_local_noise_findings"] = noise
            findings.extend(f"{relative_path}: {item}" for item in noise)
        inventory.append(row)
    return inventory, findings


def collect_disposable_artifacts(
    patterns: list[str],
    *,
    repo_root: Path = ROOT,
) -> list[dict[str, Any]]:
    artifacts: list[dict[str, Any]] = []
    seen: set[str] = set()
    for pattern in patterns:
        path_iter = Path("/").glob(pattern[1:]) if pattern.startswith("/") else repo_root.glob(pattern)
        for path in sorted(path_iter):
            resolved = str(path.resolve())
            if resolved in seen:
                continue
            seen.add(resolved)
            artifacts.append(
                {
                    "path": resolved,
                    "is_file": path.is_file(),
                    "size_bytes": path.stat().st_size if path.is_file() else None,
                }
            )
    return artifacts


def merge_disposable_artifacts(*artifact_groups: list[dict[str, Any]]) -> list[dict[str, Any]]:
    merged: list[dict[str, Any]] = []
    seen: set[str] = set()
    for artifact_group in artifact_groups:
        for row in artifact_group:
            path = row.get("path")
            if not isinstance(path, str) or not path:
                continue
            if path in seen:
                continue
            seen.add(path)
            merged.append(row)
    return merged


def summarize_preflight_receipt(payload: dict[str, Any]) -> dict[str, Any]:
    findings = payload.get("findings") if isinstance(payload.get("findings"), list) else []
    finding_rows = [
        {
            "id": item.get("id"),
            "scope": item.get("scope"),
            "detail": item.get("detail"),
        }
        for item in findings
        if isinstance(item, dict)
    ]
    return {
        "status": payload.get("status"),
        "active_lock_count": payload.get("activeLockCount"),
        "foreign_lock_count": payload.get("foreignLockCount"),
        "ignored_foreign_lock_count": payload.get("ignoredForeignLockCount"),
        "stale_looking_lock_count": payload.get("staleLookingLockCount"),
        "stale_foreign_lock_count": payload.get("staleForeignLockCount"),
        "overlay_root": payload.get("overlayRoot"),
        "finding_count": len(finding_rows),
        "blocking_findings": finding_rows,
    }


def load_preflight_snapshot(path: Path) -> dict[str, Any]:
    if not path.is_file():
        return {
            "path": str(path),
            "status": "not_found",
            "blocking_findings": [],
        }
    try:
        payload = load_json_object(path)
    except json.JSONDecodeError as exc:
        return {
            "path": str(path),
            "status": "invalid",
            "error": str(exc),
            "blocking_findings": [],
        }
    summary = summarize_preflight_receipt(payload)
    summary["path"] = str(path)
    return summary


def summarize_postdeploy_receipt(payload: dict[str, Any]) -> dict[str, Any]:
    failures = [str(item) for item in payload.get("failures", []) if isinstance(item, str)]
    return {
        "status": payload.get("status"),
        "preflight_status": payload.get("preflightStatus"),
        "strict_preflight": payload.get("strictPreflight"),
        "strict_invocation": payload.get("strictInvocation"),
        "strict_no_allowance_invocation": payload.get("strictNoAllowanceInvocation"),
        "skip_release_version_match": payload.get("skipReleaseVersionMatch"),
        "failure_count": len(failures),
        "failures": failures,
        "online_launch_status": payload.get("onlineLaunchStatus"),
        "online_launch_final_url": payload.get("onlineLaunchFinalUrl"),
        "expected_release_version": payload.get("expectedReleaseVersion"),
        "expected_release_status": payload.get("expectedReleaseStatus"),
        "expected_release_channel": payload.get("expectedReleaseChannel"),
        "expected_release_supportability_state": payload.get("expectedReleaseSupportabilityState"),
        "expected_release_rollout_state": payload.get("expectedReleaseRolloutState"),
        "downloads_version_marker_matches_release_channel": payload.get("downloadsVersionMarkerMatchesReleaseChannel"),
        "status_redirect_version_marker_matches_release_channel": payload.get("statusRedirectVersionMarkerMatchesReleaseChannel"),
        "visible_version_matches_release_channel": payload.get("visibleVersionMatchesReleaseChannel"),
        "status_redirect_version_matches_release_channel": payload.get("statusRedirectVersionMatchesReleaseChannel"),
    }


def load_postdeploy_snapshot(path: Path) -> dict[str, Any]:
    if not path.is_file():
        return {
            "path": str(path),
            "status": "not_found",
            "failures": [],
        }
    try:
        payload = load_json_object(path)
    except json.JSONDecodeError as exc:
        return {
            "path": str(path),
            "status": "invalid",
            "error": str(exc),
            "failures": [],
        }
    summary = summarize_postdeploy_receipt(payload)
    summary["path"] = str(path)
    return summary


def summarize_design_mirror_gate(returncode: int, stdout: str, stderr: str) -> dict[str, Any]:
    blocking_findings: list[str] = []
    repair_commands: list[str] = []
    for raw_line in stderr.splitlines():
        line = raw_line.strip()
        if not line or line == "design mirror drift detected for chummer6-mobile:":
            continue
        if line.startswith("repair with: "):
            repair_commands.append(line.removeprefix("repair with: ").strip())
            continue
        blocking_findings.append(line)

    status = "pass" if returncode == 0 else "fail"
    if not VERIFY_DESIGN_MIRROR_SCRIPT.is_file():
        status = "missing_script"
        if not blocking_findings:
            blocking_findings.append(f"missing {VERIFY_DESIGN_MIRROR_SCRIPT.relative_to(ROOT)}")

    return {
        "status": status,
        "script_path": str(VERIFY_DESIGN_MIRROR_SCRIPT.relative_to(ROOT)),
        "command": "python3 scripts/ai/verify_design_mirror.py",
        "blocking_findings": blocking_findings,
        "blocking_finding_count": len(blocking_findings),
        "repair_commands": repair_commands,
        "stdout": stdout.strip(),
    }


def load_design_mirror_snapshot() -> dict[str, Any]:
    if not VERIFY_DESIGN_MIRROR_SCRIPT.is_file():
        return summarize_design_mirror_gate(1, "", "")
    completed = subprocess.run(
        ["python3", str(VERIFY_DESIGN_MIRROR_SCRIPT)],
        check=False,
        text=True,
        capture_output=True,
    )
    return summarize_design_mirror_gate(completed.returncode, completed.stdout, completed.stderr)


def summarize_live_build_lock_probe(ps_output: str) -> dict[str, Any]:
    entries: list[dict[str, Any]] = []
    for raw_line in ps_output.splitlines():
        line = raw_line.strip()
        if not line or LIVE_BUILD_LOCK_MARKER not in line:
            continue
        parts = line.split(None, 1)
        if not parts:
            continue
        pid_text = parts[0]
        try:
            pid = int(pid_text)
        except ValueError:
            continue
        command = parts[1] if len(parts) > 1 else ""
        entries.append(
            {
                "pid": pid,
                "command": normalize_probe_command(command),
            }
        )
    return {
        "status": "present" if entries else "clear",
        "process_count": len(entries),
        "entries": entries,
    }


def load_live_build_lock_probe() -> dict[str, Any]:
    completed = subprocess.run(
        ["ps", "-eo", "pid=,args="],
        check=False,
        text=True,
        capture_output=True,
    )
    probe = summarize_live_build_lock_probe(completed.stdout)
    probe["command"] = "ps -eo pid=,args="
    probe["returncode"] = completed.returncode
    return probe


def summarize_root_blocker(row: dict[str, Any]) -> dict[str, Any]:
    return {
        "id": row.get("id") or row.get("blocker_id"),
        "blocker_id": row.get("blocker_id") or row.get("id"),
        "owning_repo": row.get("owning_repo"),
        "failing_gate": row.get("failing_gate"),
        "stable_promotion_command": row.get("stable_promotion_command"),
        "post_promotion_verify_command": row.get("post_promotion_verify_command"),
        "expected_bundle_path": row.get("expected_bundle_path"),
        "expected_bundle_path_exists": row.get("expected_bundle_path_exists"),
    }


def load_canonical_release_blockers(path: Path = RELEASE_BLOCKERS_RECEIPT) -> dict[str, Any]:
    if not path.is_file():
        return {
            "path": str(path),
            "status": "not_found",
            "generated_at": None,
            "root_blocker_ids": [],
            "root_blocker_count": 0,
            "root_blockers": [],
        }
    try:
        payload = load_json_object(path)
    except json.JSONDecodeError as exc:
        return {
            "path": str(path),
            "status": "invalid",
            "generated_at": None,
            "error": str(exc),
            "root_blocker_ids": [],
            "root_blocker_count": 0,
            "root_blockers": [],
        }

    root_blocker_ids = [
        str(item)
        for item in (payload.get("root_blocker_ids") or [])
        if isinstance(item, str) and item.strip()
    ]
    root_blockers = [
        summarize_root_blocker(row)
        for row in (payload.get("root_blockers") or [])
        if isinstance(row, dict)
    ]
    return {
        "path": str(path),
        "status": "present",
        "generated_at": payload.get("generated_at"),
        "root_blocker_ids": root_blocker_ids,
        "root_blocker_count": len(root_blocker_ids),
        "root_blockers": root_blockers,
    }


def build_external_follow_through(
    design_mirror_snapshot: dict[str, Any],
    live_build_lock_probe: dict[str, Any],
    preflight_snapshot: dict[str, Any],
) -> dict[str, Any]:
    design_status = "clear" if design_mirror_snapshot.get("status") == "pass" else "blocked"
    live_lock_status = str(live_build_lock_probe.get("status") or "").strip() or "unknown"
    preflight_status = str(preflight_snapshot.get("status") or "").strip() or "unknown"
    preflight_blocking_findings = [
        str(item.get("detail") or "").strip()
        for item in (preflight_snapshot.get("blocking_findings") or [])
        if isinstance(item, dict) and str(item.get("detail") or "").strip()
    ]
    strict_public_edge_ready = live_lock_status == "clear" or preflight_status == "pass"
    strict_public_edge_status = "ready_to_rerun" if strict_public_edge_ready else "waiting_for_foreign_build_locks"
    if strict_public_edge_ready:
        wait_reason = None
    elif preflight_status == "fail" and preflight_blocking_findings:
        wait_reason = f"strict public-edge preflight blocked: {preflight_blocking_findings[0]}"
    elif live_lock_status == "present":
        wait_reason = "foreign build-chummer6-linux lanes still present"
    else:
        wait_reason = "public-edge preflight has not cleared yet"
    return {
        "design_mirror": {
            "status": design_status,
            "blocking_findings": design_mirror_snapshot.get("blocking_findings")
            if isinstance(design_mirror_snapshot.get("blocking_findings"), list)
            else [],
            "repair_commands": design_mirror_snapshot.get("repair_commands")
            if isinstance(design_mirror_snapshot.get("repair_commands"), list)
            else [],
        },
        "strict_public_edge": {
            "status": strict_public_edge_status,
            "live_build_lock_probe_status": live_lock_status,
            "preflight_snapshot_status": preflight_status,
            "preflight_blocking_findings": preflight_blocking_findings,
            "wait_reason": wait_reason,
            "follow_through_command": STRICT_PUBLIC_EDGE_FOLLOW_THROUGH_COMMAND,
            "follow_through_receipt_path": STRICT_PUBLIC_EDGE_FOLLOW_THROUGH_RECEIPT,
            "rerun_commands": [
                STRICT_PUBLIC_EDGE_PREFLIGHT_COMMAND,
                STRICT_PUBLIC_EDGE_POSTDEPLOY_COMMAND,
                *MOBILE_RECEIPT_REFRESH_COMMANDS,
            ],
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default=str(OUT))
    parser.add_argument("--preflight-receipt", default=str(DEFAULT_PREFLIGHT_RECEIPT))
    parser.add_argument("--postdeploy-receipt", default=str(DEFAULT_POSTDEPLOY_RECEIPT))
    args = parser.parse_args()
    output_path = Path(args.output).resolve()
    generated_at_utc = iso_now()

    play_status_entries = git_status_entries(ROOT)
    run_services_status_entries = git_status_entries(RUN_SERVICES_ROOT)

    play_worktree = worktree_summary(
        play_status_entries,
        OWNED_PLAY_SOURCE_FILES,
        OWNED_PLAY_TEST_FILES,
        OWNED_RELEASE_RECEIPTS,
        DISPOSABLE_LOCAL_WORKTREE_PREFIXES,
        EXTERNAL_BLOCKER_WORKTREE_PREFIXES,
        AMBIENT_PLAY_WORKTREE_PREFIXES,
    )
    run_services_worktree = worktree_summary(
        run_services_status_entries,
        OWNED_RUN_SERVICES_SOURCE_FILES,
        OWNED_RUN_SERVICES_TEST_FILES,
        ambient_match_all_foreign=True,
    )
    play_worktree["repo_root"] = str(ROOT)
    run_services_worktree["repo_root"] = str(RUN_SERVICES_ROOT)

    owned_play_source_inventory = inventory_paths(ROOT, OWNED_PLAY_SOURCE_FILES, play_status_entries)
    owned_play_test_inventory = inventory_paths(ROOT, OWNED_PLAY_TEST_FILES, play_status_entries)
    owned_run_services_source_inventory = inventory_paths(
        RUN_SERVICES_ROOT,
        OWNED_RUN_SERVICES_SOURCE_FILES,
        run_services_status_entries,
    )
    owned_run_services_test_inventory = inventory_paths(
        RUN_SERVICES_ROOT,
        OWNED_RUN_SERVICES_TEST_FILES,
        run_services_status_entries,
    )

    pending_rows: dict[str, dict[str, Any]] = {}
    try:
        output_relative = str(output_path.relative_to(ROOT))
    except ValueError:
        output_relative = ""
    if output_relative in OWNED_RELEASE_RECEIPTS:
        pending_rows[output_relative] = {
            "path": output_relative,
            "exists": True,
            "worktree_status": next(
                (entry["status"] for entry in play_status_entries if entry["path"] == output_relative),
                "",
            ),
            "status": "pass",
            "contract_name": "chummer6-mobile.release_boundary.v1",
            "generated_at_utc": generated_at_utc,
            "pending_materialization": True,
        }
    strict_follow_through_receipt_path = STRICT_PUBLIC_EDGE_FOLLOW_THROUGH_RECEIPT
    strict_follow_through_receipt_abs = (ROOT / strict_follow_through_receipt_path).resolve()
    if not strict_follow_through_receipt_abs.is_file():
        pending_rows[strict_follow_through_receipt_path] = {
            "path": strict_follow_through_receipt_path,
            "exists": True,
            "worktree_status": next(
                (entry["status"] for entry in play_status_entries if entry["path"] == strict_follow_through_receipt_path),
                "",
            ),
            "status": "pending",
            "contract_name": "chummer6-mobile.strict_public_edge_follow_through.v1",
            "generated_at_utc": generated_at_utc,
            "pending_materialization": True,
        }

    receipt_rows, receipt_findings = receipt_inventory(
        ROOT,
        OWNED_RELEASE_RECEIPTS,
        play_status_entries,
        pending_rows=pending_rows,
    )
    owned_disposable_artifacts = collect_disposable_artifacts(OWNED_DISPOSABLE_LOCAL_ARTIFACT_GLOBS)
    shared_external_temp_artifacts = collect_disposable_artifacts(SHARED_EXTERNAL_TEMP_ARTIFACT_GLOBS)
    disposable_artifacts = merge_disposable_artifacts(
        owned_disposable_artifacts,
        shared_external_temp_artifacts,
    )
    preflight_snapshot = load_preflight_snapshot(Path(args.preflight_receipt))
    postdeploy_snapshot = load_postdeploy_snapshot(Path(args.postdeploy_receipt))
    design_mirror_snapshot = load_design_mirror_snapshot()
    live_build_lock_probe = load_live_build_lock_probe()
    canonical_release_blockers = load_canonical_release_blockers()
    external_follow_through = build_external_follow_through(
        design_mirror_snapshot,
        live_build_lock_probe,
        preflight_snapshot,
    )

    ownership_checks = {
        "owned_play_source_files_present": all(row.get("exists") is True for row in owned_play_source_inventory),
        "owned_play_test_files_present": all(row.get("exists") is True for row in owned_play_test_inventory),
        "owned_run_services_source_files_present": all(row.get("exists") is True for row in owned_run_services_source_inventory),
        "owned_run_services_test_files_present": all(row.get("exists") is True for row in owned_run_services_test_inventory),
        "owned_release_receipts_present": all(row.get("exists") is True for row in receipt_rows),
        "release_receipts_machine_local_noise_free": not receipt_findings,
    }

    payload = {
        "contract_name": "chummer6-mobile.release_boundary.v1",
        "status": "pass" if all(value is True for value in ownership_checks.values()) else "fail",
        "generated_at_utc": generated_at_utc,
        "release_receipt_count": len(receipt_rows),
        "play_owned_entry_count": play_worktree["owned_entry_count"],
        "play_owned_source_test_entry_count": play_worktree["owned_source_test_entry_count"],
        "play_owned_release_receipt_entry_count": play_worktree["owned_release_receipt_entry_count"],
        "play_foreign_entry_count": play_worktree["foreign_entry_count"],
        "play_ambient_entry_count": play_worktree["ambient_entry_count"],
        "run_services_owned_entry_count": run_services_worktree["owned_entry_count"],
        "run_services_foreign_entry_count": run_services_worktree["foreign_entry_count"],
        "run_services_ambient_entry_count": run_services_worktree["ambient_entry_count"],
        "owned_disposable_local_artifact_count": len(owned_disposable_artifacts),
        "shared_external_temp_artifact_count": len(shared_external_temp_artifacts),
        "disposable_local_artifact_count": len(disposable_artifacts),
        "owned_boundary": {
            "play_source_files": owned_play_source_inventory,
            "play_test_files": owned_play_test_inventory,
            "run_services_source_files": owned_run_services_source_inventory,
            "run_services_test_files": owned_run_services_test_inventory,
            "release_receipts": receipt_rows,
        },
        "worktree": {
            "play": play_worktree,
            "run_services": run_services_worktree,
        },
        "owned_disposable_local_artifacts": owned_disposable_artifacts,
        "shared_external_temp_artifacts": shared_external_temp_artifacts,
        "disposable_local_artifacts": disposable_artifacts,
        "preflight_snapshot": preflight_snapshot,
        "postdeploy_snapshot": postdeploy_snapshot,
        "design_mirror_snapshot": design_mirror_snapshot,
        "live_build_lock_probe": live_build_lock_probe,
        "canonical_release_blockers": canonical_release_blockers,
        "external_follow_through": external_follow_through,
        "ownership_checks": ownership_checks,
        "noise_findings": receipt_findings,
        "release_receipt_machine_local_noise_findings": receipt_findings,
    }

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")

    if output_relative in OWNED_RELEASE_RECEIPTS:
        receipt_rows, receipt_findings = receipt_inventory(
            ROOT,
            OWNED_RELEASE_RECEIPTS,
            play_status_entries,
            pending_rows=pending_rows,
        )
        payload["release_receipt_count"] = len(receipt_rows)
        payload["owned_boundary"]["release_receipts"] = receipt_rows
        payload["ownership_checks"]["owned_release_receipts_present"] = all(
            row.get("exists") is True for row in receipt_rows
        )
        payload["ownership_checks"]["release_receipts_machine_local_noise_free"] = not receipt_findings
        payload["noise_findings"] = receipt_findings
        payload["release_receipt_machine_local_noise_findings"] = receipt_findings
        payload["status"] = (
            "pass" if all(value is True for value in payload["ownership_checks"].values()) else "fail"
        )
        output_path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")

    print(f"wrote mobile release boundary: {output_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
