from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock


ROOT = Path(__file__).resolve().parents[1]
SCRIPT_PATH = ROOT / "scripts" / "run_mobile_strict_public_edge_follow_through.py"


def load_module(script_path: Path, module_name: str):
    spec = importlib.util.spec_from_file_location(module_name, script_path)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


class MobileStrictPublicEdgeFollowThroughTests(unittest.TestCase):
    def test_main_writes_waiting_receipt_without_rerun_when_locks_remain(self) -> None:
        module = load_module(SCRIPT_PATH, "run_mobile_strict_public_edge_follow_through_waiting")

        waiting_state = {
            "command_result": {
                "command": "python3 scripts/materialize_mobile_release_boundary.py",
                "returncode": 0,
                "duration_seconds": 0.12,
                "stdout_tail": "",
                "stderr_tail": "",
            },
            "boundary_payload": {
                "generated_at_utc": "2026-07-06T21:37:23Z",
                "preflight_snapshot": {
                    "path": "/tmp/chummer-public-edge-deploy-preflight-current.json",
                    "status": "not_found",
                    "blocking_findings": [],
                },
                "postdeploy_snapshot": {
                    "path": "/tmp/chummer-public-edge-postdeploy-canonical-current.json",
                    "status": "not_found",
                    "failures": [],
                },
            },
            "strict_follow_through": {
                "status": "waiting_for_foreign_build_locks",
                "live_build_lock_probe_status": "present",
                "wait_reason": "foreign build-chummer6-linux lanes still present",
                "follow_through_command": "python3 scripts/run_mobile_strict_public_edge_follow_through.py --wait-for-clear --execute-rerun",
                "follow_through_receipt_path": ".codex-studio/published/MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json",
                "rerun_commands": ["cmd-a", "cmd-b"],
            },
            "live_build_lock_probe": {
                "status": "present",
                "process_count": 2,
                "entries": [{"pid": 202947, "command": "bash scripts/build-chummer6-linux.sh --base /work/base"}],
            },
        }

        with tempfile.TemporaryDirectory(prefix="strict-public-edge-follow-through-") as temp_dir:
            output_path = Path(temp_dir) / "receipt.json"
            with (
                mock.patch.object(module, "materialize_boundary_state", return_value=waiting_state),
                mock.patch.object(module, "cross_surface_summary", return_value={"status": "fail", "strict_public_edge_gate_pass": False}),
                mock.patch.object(module, "local_release_proof_summary", return_value={"status": "passed"}),
            ):
                exit_code = module.main(["--output", str(output_path)])

            payload = json.loads(output_path.read_text(encoding="utf-8"))

        self.assertEqual(0, exit_code)
        self.assertEqual("waiting_for_foreign_build_locks", payload["status"])
        self.assertFalse(payload["rerun_executed"])
        self.assertEqual([], payload["command_results"])
        self.assertEqual(1, payload["poll_count"])
        self.assertEqual("present", payload["live_build_lock_probe"]["status"])
        self.assertEqual("not_found", payload["preflight_snapshot"]["status"])
        self.assertEqual("not_found", payload["postdeploy_snapshot"]["status"])

    def test_main_executes_rerun_and_passes_when_strict_gate_turns_green(self) -> None:
        module = load_module(SCRIPT_PATH, "run_mobile_strict_public_edge_follow_through_execute")

        ready_state = {
            "command_result": {
                "command": "python3 scripts/materialize_mobile_release_boundary.py",
                "returncode": 0,
                "duration_seconds": 0.11,
                "stdout_tail": "",
                "stderr_tail": "",
            },
            "boundary_payload": {
                "generated_at_utc": "2026-07-06T23:50:00Z",
                "preflight_snapshot": {
                    "path": "/tmp/chummer-public-edge-deploy-preflight-current.json",
                    "status": "pass",
                    "blocking_findings": [],
                },
                "postdeploy_snapshot": {
                    "path": "/tmp/chummer-public-edge-postdeploy-canonical-current.json",
                    "status": "pass",
                    "preflight_status": "pass",
                    "strict_preflight": True,
                    "strict_invocation": True,
                    "strict_no_allowance_invocation": True,
                    "skip_release_version_match": False,
                    "expected_release_version": "run-20260704-170602",
                    "expected_release_channel": "public_stable",
                    "visible_version_matches_release_channel": True,
                    "status_redirect_version_matches_release_channel": True,
                    "downloads_version_marker_matches_release_channel": True,
                    "status_redirect_version_marker_matches_release_channel": True,
                    "failures": [],
                },
            },
            "strict_follow_through": {
                "status": "ready_to_rerun",
                "live_build_lock_probe_status": "clear",
                "wait_reason": None,
                "follow_through_command": "python3 scripts/run_mobile_strict_public_edge_follow_through.py --wait-for-clear --execute-rerun",
                "follow_through_receipt_path": ".codex-studio/published/MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json",
                "rerun_commands": ["cmd-a", "cmd-b"],
            },
            "live_build_lock_probe": {
                "status": "clear",
                "process_count": 0,
                "entries": [],
            },
        }
        post_rerun_state = {
            **ready_state,
            "boundary_payload": {
                "generated_at_utc": "2026-07-06T23:51:00Z",
                "preflight_snapshot": {
                    "path": "/tmp/chummer-public-edge-deploy-preflight-current.json",
                    "status": "pass",
                    "blocking_findings": [],
                },
                "postdeploy_snapshot": {
                    "path": "/tmp/chummer-public-edge-postdeploy-canonical-current.json",
                    "status": "pass",
                    "preflight_status": "pass",
                    "strict_preflight": True,
                    "strict_invocation": True,
                    "strict_no_allowance_invocation": True,
                    "skip_release_version_match": False,
                    "expected_release_version": "run-20260704-170602",
                    "expected_release_channel": "public_stable",
                    "visible_version_matches_release_channel": True,
                    "status_redirect_version_matches_release_channel": True,
                    "downloads_version_marker_matches_release_channel": True,
                    "status_redirect_version_marker_matches_release_channel": True,
                    "failures": [],
                },
            },
        }

        command_results = [
            {"command": "cmd-a", "returncode": 0, "duration_seconds": 0.3, "stdout_tail": "", "stderr_tail": ""},
            {"command": "cmd-b", "returncode": 0, "duration_seconds": 0.2, "stdout_tail": "", "stderr_tail": ""},
        ]

        with tempfile.TemporaryDirectory(prefix="strict-public-edge-follow-through-pass-") as temp_dir:
            output_path = Path(temp_dir) / "receipt.json"
            with (
                mock.patch.object(module, "materialize_boundary_state", side_effect=[ready_state, post_rerun_state, post_rerun_state]),
                mock.patch.object(module, "run_shell_command", side_effect=command_results),
                mock.patch.object(module, "cross_surface_summary", return_value={"status": "pass", "strict_public_edge_gate_pass": True}),
                mock.patch.object(module, "local_release_proof_summary", return_value={"status": "passed"}),
            ):
                exit_code = module.main(["--output", str(output_path), "--execute-rerun"])

            payload = json.loads(output_path.read_text(encoding="utf-8"))

        self.assertEqual(0, exit_code)
        self.assertEqual("pass", payload["status"])
        self.assertTrue(payload["rerun_executed"])
        self.assertEqual(["cmd-a", "cmd-b"], [item["command"] for item in payload["command_results"]])
        self.assertEqual("clear", payload["live_build_lock_probe"]["status"])
        self.assertEqual("pass", payload["preflight_snapshot"]["status"])
        self.assertEqual("pass", payload["postdeploy_snapshot"]["status"])
        self.assertTrue(payload["postdeploy_snapshot"]["strict_no_allowance_invocation"])
        self.assertEqual("run-20260704-170602", payload["postdeploy_snapshot"]["expected_release_version"])

    def test_main_executes_rerun_when_preflight_is_passed_but_stale_locks_are_still_visible(self) -> None:
        module = load_module(SCRIPT_PATH, "run_mobile_strict_public_edge_follow_through_stale_locks_ready")

        ready_state = {
            "command_result": {
                "command": "python3 scripts/materialize_mobile_release_boundary.py",
                "returncode": 0,
                "duration_seconds": 0.11,
                "stdout_tail": "",
                "stderr_tail": "",
            },
            "boundary_payload": {
                "generated_at_utc": "2026-07-07T09:44:09Z",
                "preflight_snapshot": {
                    "path": "/tmp/chummer-public-edge-deploy-preflight-current.json",
                    "status": "pass",
                    "blocking_findings": [],
                    "ignored_foreign_lock_count": 4,
                },
                "postdeploy_snapshot": {
                    "path": "/tmp/chummer-public-edge-postdeploy-canonical-current.json",
                    "status": "pass",
                    "preflight_status": "pass",
                    "strict_preflight": True,
                    "strict_invocation": True,
                    "strict_no_allowance_invocation": True,
                    "skip_release_version_match": False,
                    "expected_release_version": "run-20260704-170602",
                    "expected_release_channel": "preview",
                    "visible_version_matches_release_channel": True,
                    "status_redirect_version_matches_release_channel": True,
                    "downloads_version_marker_matches_release_channel": True,
                    "status_redirect_version_marker_matches_release_channel": True,
                    "failures": [],
                },
            },
            "strict_follow_through": {
                "status": "ready_to_rerun",
                "live_build_lock_probe_status": "present",
                "preflight_snapshot_status": "pass",
                "wait_reason": None,
                "follow_through_command": "python3 scripts/run_mobile_strict_public_edge_follow_through.py --wait-for-clear --execute-rerun",
                "follow_through_receipt_path": ".codex-studio/published/MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json",
                "rerun_commands": ["cmd-a", "cmd-b"],
            },
            "live_build_lock_probe": {
                "status": "present",
                "process_count": 4,
                "entries": [{"pid": 202947, "command": "bash scripts/build-chummer6-linux.sh --base /work/base"}],
            },
        }
        post_rerun_state = {
            **ready_state,
            "boundary_payload": {
                "generated_at_utc": "2026-07-07T09:45:00Z",
                "preflight_snapshot": ready_state["boundary_payload"]["preflight_snapshot"],
                "postdeploy_snapshot": ready_state["boundary_payload"]["postdeploy_snapshot"],
            },
        }

        command_results = [
            {"command": "cmd-a", "returncode": 0, "duration_seconds": 0.3, "stdout_tail": "", "stderr_tail": ""},
            {"command": "cmd-b", "returncode": 0, "duration_seconds": 0.2, "stdout_tail": "", "stderr_tail": ""},
        ]

        with tempfile.TemporaryDirectory(prefix="strict-public-edge-follow-through-stale-locks-") as temp_dir:
            output_path = Path(temp_dir) / "receipt.json"
            with (
                mock.patch.object(module, "materialize_boundary_state", side_effect=[ready_state, post_rerun_state, post_rerun_state]),
                mock.patch.object(module, "run_shell_command", side_effect=command_results),
                mock.patch.object(module, "cross_surface_summary", return_value={"status": "pass", "strict_public_edge_gate_pass": True}),
                mock.patch.object(module, "local_release_proof_summary", return_value={"status": "passed"}),
            ):
                exit_code = module.main(["--output", str(output_path), "--execute-rerun"])

            payload = json.loads(output_path.read_text(encoding="utf-8"))

        self.assertEqual(0, exit_code)
        self.assertEqual("pass", payload["status"])
        self.assertTrue(payload["rerun_executed"])
        self.assertEqual("present", payload["live_build_lock_probe"]["status"])
        self.assertEqual("pass", payload["preflight_snapshot"]["status"])
        self.assertEqual("pass", payload["postdeploy_snapshot"]["status"])


if __name__ == "__main__":
    unittest.main()
