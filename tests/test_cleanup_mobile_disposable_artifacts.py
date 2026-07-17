from __future__ import annotations

import importlib.util
import os
import sys
import tempfile
import time
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
SCRIPT_PATH = REPO_ROOT / "scripts" / "cleanup_mobile_disposable_artifacts.py"


def load_module(module_name: str, script_path: Path):
    spec = importlib.util.spec_from_file_location(module_name, script_path)
    module = importlib.util.module_from_spec(spec)
    assert spec is not None and spec.loader is not None
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def set_old_mtime(path: Path, *, seconds_ago: float) -> None:
    timestamp = time.time() - seconds_ago
    os.utime(path, (timestamp, timestamp), follow_symlinks=False)


class CleanupMobileDisposableArtifactsTests(unittest.TestCase):
    def test_cleanup_browser_state_removes_only_stale_entries(self) -> None:
        module = load_module("cleanup_mobile_disposable_artifacts_browser_state", SCRIPT_PATH)

        with tempfile.TemporaryDirectory(prefix="cleanup-mobile-browser-state-") as temp_dir:
            browser_state_root = Path(temp_dir)
            stale_file = browser_state_root / "stale.json"
            stale_file.write_text("{}", encoding="utf-8")
            recent_file = browser_state_root / "recent.json"
            recent_file.write_text("{}", encoding="utf-8")

            set_old_mtime(stale_file, seconds_ago=8 * 3600)
            summary = module.cleanup_browser_state(
                browser_state_root,
                time.time() - 6 * 3600,
            )

            self.assertEqual(1, summary["removed_count"])
            self.assertEqual(1, summary["skipped_count"])
            self.assertFalse(stale_file.exists())
            self.assertTrue(recent_file.exists())

    def test_cleanup_tmp_artifacts_removes_only_stale_matches(self) -> None:
        module = load_module("cleanup_mobile_disposable_artifacts", SCRIPT_PATH)

        with tempfile.TemporaryDirectory(prefix="cleanup-mobile-disposable-tmp-") as temp_dir:
            temp_root = Path(temp_dir)
            stale_dir = temp_root / "chummer-play-analytics-smoke-stale"
            stale_dir.mkdir()
            recent_dir = temp_root / "chummer-play-analytics-smoke-recent"
            recent_dir.mkdir()
            ignored_dir = temp_root / "unrelated-temp"
            ignored_dir.mkdir()

            set_old_mtime(stale_dir, seconds_ago=8 * 3600)
            summary = module.cleanup_tmp_artifacts(
                temp_root,
                ["chummer-play-analytics-smoke-*"],
                time.time() - 6 * 3600,
            )

            self.assertEqual(1, summary["removed_count"])
            self.assertEqual(1, summary["skipped_count"])
            self.assertFalse(stale_dir.exists())
            self.assertTrue(recent_dir.exists())
            self.assertTrue(ignored_dir.exists())

    def test_cleanup_tmp_artifacts_removes_repo_local_viewport_receipts(self) -> None:
        module = load_module("cleanup_mobile_disposable_artifacts_repo_local", SCRIPT_PATH)

        with tempfile.TemporaryDirectory(prefix="cleanup-mobile-repo-local-") as temp_dir:
            repo_root = Path(temp_dir)
            screenshot = repo_root / "_tmp" / "mobile-viewport-smoke-player-390x844.png"
            screenshot.parent.mkdir(parents=True, exist_ok=True)
            screenshot.write_bytes(b"png")

            summary = module.cleanup_tmp_artifacts(
                repo_root,
                module.REPO_LOCAL_ARTIFACT_PATTERNS,
                time.time(),
            )

            self.assertEqual(1, summary["removed_count"])
            self.assertEqual(0, summary["skipped_count"])
            self.assertFalse(screenshot.exists())

    def test_owned_tmp_patterns_can_use_tighter_cutoff_than_shared_patterns(self) -> None:
        module = load_module("cleanup_mobile_disposable_artifacts_split", SCRIPT_PATH)

        with tempfile.TemporaryDirectory(prefix="cleanup-mobile-disposable-split-") as temp_dir:
            temp_root = Path(temp_dir)
            owned_dir = temp_root / "chummer-play-analytics-smoke-owned"
            owned_dir.mkdir()
            shared_dir = temp_root / "chummer-frontdoor-shared"
            shared_dir.mkdir()

            set_old_mtime(owned_dir, seconds_ago=30 * 60)
            set_old_mtime(shared_dir, seconds_ago=30 * 60)

            owned_summary = module.cleanup_tmp_artifacts(
                temp_root,
                module.OWNED_TMP_ARTIFACT_PATTERNS,
                time.time() - module.OWNED_TMP_ARTIFACT_MAX_AGE_HOURS * 3600,
            )
            shared_summary = module.cleanup_tmp_artifacts(
                temp_root,
                module.SHARED_TMP_ARTIFACT_PATTERNS,
                time.time() - 6 * 3600,
            )

            self.assertFalse(owned_dir.exists())
            self.assertTrue(shared_dir.exists())
            self.assertEqual(1, owned_summary["removed_count"])
            self.assertEqual(0, shared_summary["removed_count"])

    def test_cleanup_runtime_tmp_keeps_recent_entries_and_live_pid_markers(self) -> None:
        module = load_module("cleanup_mobile_disposable_artifacts_runtime", SCRIPT_PATH)

        with tempfile.TemporaryDirectory(prefix="cleanup-mobile-runtime-tmp-") as temp_dir:
            runtime_root = Path(temp_dir)
            stale_dir = runtime_root / "MSBuildTempStale"
            stale_dir.mkdir()
            recent_dir = runtime_root / "MSBuildTempRecent"
            recent_dir.mkdir()
            live_pid_pipe = runtime_root / f"dotnet-diagnostic-{os.getpid()}-12345-socket"
            live_pid_pipe.write_text("", encoding="utf-8")

            set_old_mtime(stale_dir, seconds_ago=8 * 3600)
            set_old_mtime(live_pid_pipe, seconds_ago=8 * 3600)

            summary = module.cleanup_runtime_tmp(runtime_root, time.time() - 6 * 3600)

            self.assertEqual(1, summary["removed_count"])
            self.assertFalse(stale_dir.exists())
            self.assertTrue(recent_dir.exists())
            self.assertTrue(live_pid_pipe.exists())
            self.assertIn(
                f"live_pid:{os.getpid()}",
                [row["reason"] for row in summary["skipped"]],
            )


if __name__ == "__main__":
    unittest.main()
