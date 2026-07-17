#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import shutil
import tempfile
import time
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
BROWSER_STATE_ROOT = ROOT / ".state" / "browser-state"
RUNTIME_TMP_ROOT = ROOT / ".state" / "runtime-tmp"
TMP_ROOT = Path(tempfile.gettempdir())
DEFAULT_MAX_AGE_HOURS = 6.0
OWNED_TMP_ARTIFACT_MAX_AGE_HOURS = 0.0
REPO_LOCAL_ARTIFACT_PATTERNS = [
    "_tmp/mobile-viewport-smoke-*.png",
]
OWNED_TMP_ARTIFACT_PATTERNS = [
    "chummer-play-analytics-smoke-*",
    "chummer-play-runtime-smoke-*",
    "chummer-play-viewport-smoke-*",
    "chummer-play-app-browser-state*",
    "chummer-play-regression-browser-state*",
    "mobile-cross-surface-refresh-*",
    "chummer-character-roster-*",
]
SHARED_TMP_ARTIFACT_PATTERNS = [
    "chummer-mobile-cross-surface-fleet-*",
    "chummer-mobile-cross-surface-public-*",
    "chummer-frontdoor-*",
    "chummer-mobile-frontdoor-*",
    "chummer-public-edge-*",
    "chummer-online-launch*",
    "chummer-run-live-character-roster-*",
    "CHUMMER_*",
]
LIVE_PID_TOKENS = (
    "dotnet-diagnostic-",
    "clr-debug-pipe-",
)


def iso_now() -> str:
    return time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())


def remove_path(path: Path) -> None:
    if path.is_dir() and not path.is_symlink():
        shutil.rmtree(path)
        return
    path.unlink(missing_ok=True)


def is_stale(path: Path, cutoff_epoch: float) -> bool:
    try:
        return path.lstat().st_mtime < cutoff_epoch
    except FileNotFoundError:
        return False


def extract_embedded_pid(name: str) -> int | None:
    for token in LIVE_PID_TOKENS:
        if not name.startswith(token):
            continue
        remainder = name[len(token):]
        pid_text = remainder.split("-", 1)[0]
        if pid_text.isdigit():
            return int(pid_text)
    return None


def pid_is_alive(pid: int) -> bool:
    return Path(f"/proc/{pid}").exists()


def classify_runtime_tmp_path(path: Path, cutoff_epoch: float) -> tuple[bool, str]:
    if not is_stale(path, cutoff_epoch):
        return False, "recent"

    embedded_pid = extract_embedded_pid(path.name)
    if embedded_pid is not None and pid_is_alive(embedded_pid):
        return False, f"live_pid:{embedded_pid}"

    return True, "stale"


def cleanup_runtime_tmp(root: Path, cutoff_epoch: float) -> dict[str, Any]:
    removed: list[dict[str, Any]] = []
    skipped: list[dict[str, Any]] = []

    if not root.exists():
        return {
            "root": str(root),
            "removed_count": 0,
            "skipped_count": 0,
            "removed": removed,
            "skipped": skipped,
        }

    for path in sorted(root.iterdir()):
        should_remove, reason = classify_runtime_tmp_path(path, cutoff_epoch)
        row = {"path": str(path), "reason": reason}
        if should_remove:
            remove_path(path)
            removed.append(row)
        else:
            skipped.append(row)

    if root.exists() and not any(root.iterdir()):
        root.rmdir()

    return {
        "root": str(root),
        "removed_count": len(removed),
        "skipped_count": len(skipped),
        "removed": removed,
        "skipped": skipped,
    }


def cleanup_browser_state(root: Path, cutoff_epoch: float) -> dict[str, Any]:
    removed: list[dict[str, Any]] = []
    skipped: list[dict[str, Any]] = []

    if not root.exists():
        return {
            "root": str(root),
            "removed_count": 0,
            "skipped_count": 0,
            "removed": removed,
            "skipped": skipped,
        }

    for path in sorted(root.iterdir()):
        resolved = str(path.resolve())
        if not is_stale(path, cutoff_epoch):
            skipped.append({"path": resolved, "reason": "recent"})
            continue

        remove_path(path)
        removed.append({"path": resolved, "reason": "stale"})

    if root.exists() and not any(root.iterdir()):
        root.rmdir()

    return {
        "root": str(root),
        "removed_count": len(removed),
        "skipped_count": len(skipped),
        "removed": removed,
        "skipped": skipped,
    }


def cleanup_tmp_artifacts(tmp_root: Path, patterns: list[str], cutoff_epoch: float) -> dict[str, Any]:
    removed: list[dict[str, Any]] = []
    skipped: list[dict[str, Any]] = []
    seen: set[str] = set()

    for pattern in patterns:
        for path in sorted(tmp_root.glob(pattern)):
            resolved = str(path.resolve())
            if resolved in seen:
                continue
            seen.add(resolved)

            if not is_stale(path, cutoff_epoch):
                skipped.append({"path": resolved, "reason": "recent"})
                continue

            remove_path(path)
            removed.append({"path": resolved, "reason": "stale"})

    return {
        "root": str(tmp_root),
        "removed_count": len(removed),
        "skipped_count": len(skipped),
        "removed": removed,
        "skipped": skipped,
    }


def merge_tmp_summaries(*summaries: dict[str, Any]) -> dict[str, Any]:
    removed: list[dict[str, Any]] = []
    skipped: list[dict[str, Any]] = []
    for summary in summaries:
        removed.extend(summary.get("removed", []))
        skipped.extend(summary.get("skipped", []))
    return {
        "root": str(TMP_ROOT),
        "removed_count": len(removed),
        "skipped_count": len(skipped),
        "removed": removed,
        "skipped": skipped,
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Remove stale disposable mobile/PWA temp artifacts without touching recent or live runtime state.",
    )
    parser.add_argument(
        "--max-age-hours",
        type=float,
        default=DEFAULT_MAX_AGE_HOURS,
        help=f"Remove only artifacts older than this many hours (default: {DEFAULT_MAX_AGE_HOURS}).",
    )
    parser.add_argument(
        "--output",
        type=Path,
        help="Optional JSON summary output path.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    shared_max_age_hours = max(args.max_age_hours, 0.0)
    owned_max_age_hours = min(shared_max_age_hours, OWNED_TMP_ARTIFACT_MAX_AGE_HOURS)
    shared_cutoff_epoch = time.time() - (shared_max_age_hours * 3600.0)
    owned_cutoff_epoch = time.time() - (owned_max_age_hours * 3600.0)

    browser_state_summary = cleanup_browser_state(BROWSER_STATE_ROOT, shared_cutoff_epoch)
    runtime_summary = cleanup_runtime_tmp(RUNTIME_TMP_ROOT, owned_cutoff_epoch)
    repo_local_summary = cleanup_tmp_artifacts(ROOT, REPO_LOCAL_ARTIFACT_PATTERNS, owned_cutoff_epoch)
    owned_tmp_summary = cleanup_tmp_artifacts(TMP_ROOT, OWNED_TMP_ARTIFACT_PATTERNS, owned_cutoff_epoch)
    shared_tmp_summary = cleanup_tmp_artifacts(TMP_ROOT, SHARED_TMP_ARTIFACT_PATTERNS, shared_cutoff_epoch)
    tmp_summary = merge_tmp_summaries(owned_tmp_summary, shared_tmp_summary)
    payload = {
        "contract_name": "chummer6-mobile.disposable_artifact_cleanup.v1",
        "generated_at_utc": iso_now(),
        "max_age_hours": args.max_age_hours,
        "owned_tmp_artifact_max_age_hours": owned_max_age_hours,
        "browser_state": browser_state_summary,
        "runtime_tmp": runtime_summary,
        "repo_local_artifacts": repo_local_summary,
        "owned_tmp_artifacts": owned_tmp_summary,
        "shared_tmp_artifacts": shared_tmp_summary,
        "tmp_artifacts": tmp_summary,
        "removed_count": browser_state_summary["removed_count"] + runtime_summary["removed_count"] + repo_local_summary["removed_count"] + tmp_summary["removed_count"],
        "skipped_count": browser_state_summary["skipped_count"] + runtime_summary["skipped_count"] + repo_local_summary["skipped_count"] + tmp_summary["skipped_count"],
    }

    if args.output is not None:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")

    print(
        "mobile_disposable_artifact_cleanup ok "
        f"removed={payload['removed_count']} skipped={payload['skipped_count']} "
        f"max_age_hours={args.max_age_hours:g}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
