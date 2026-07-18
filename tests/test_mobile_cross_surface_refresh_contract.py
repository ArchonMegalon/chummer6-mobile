from __future__ import annotations

import importlib.util
import json
import os
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path
from unittest import mock


ROOT = Path(__file__).resolve().parents[1]
CROSS_SURFACE_SCRIPT = ROOT / "scripts" / "materialize_mobile_cross_surface_readiness.py"
LOCAL_RELEASE_SCRIPT = ROOT / "scripts" / "materialize_mobile_local_release_proof.py"


def load_module(script_path: Path, module_name: str):
    spec = importlib.util.spec_from_file_location(module_name, script_path)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


def write_fake_fleet_script(path: Path) -> None:
    path.write_text(
        textwrap.dedent(
            """\
            #!/usr/bin/env python3
            import argparse
            import json
            from pathlib import Path

            parser = argparse.ArgumentParser()
            parser.add_argument("--out", required=True)
            args = parser.parse_args()
            payload = {
                "generated_at": "2026-07-05T10:00:00Z",
                "generated_at_utc": "2026-07-05T10:00:00Z",
                "status": "pass",
                "warning_keys": [],
                "missing_keys": [],
                "readiness_plane_gap_keys": [],
                "coverage": {
                    "mobile_play_shell": "ready",
                    "desktop_client": "warning",
                },
                "coverage_details": {
                    "mobile_play_shell": {
                        "status": "ready",
                        "summary": "Mobile shell is ready.",
                        "evidence": {
                            "mobile_local_release_status": "passed",
                            "campaign_session_recover_recap_effective_state": "passed",
                            "recover_from_sync_conflict_owner_scoped_effective_state": "passed",
                        },
                    },
                },
            }
            Path(args.out).write_text(json.dumps(payload, indent=2) + "\\n", encoding="utf-8")
            """
        ),
        encoding="utf-8",
    )


def write_fake_public_edge_gate_script(path: Path) -> None:
    path.write_text(
        textwrap.dedent(
            """\
            #!/usr/bin/env python3
            import argparse
            import json
            import sys
            from pathlib import Path

            parser = argparse.ArgumentParser()
            parser.add_argument("--frontdoor-navigation-artifact-dir", required=True)
            parser.add_argument("--output", required=True)
            parser.add_argument("--base-url")
            parser.add_argument("--skip-preflight", action="store_true")
            parser.add_argument("--skip-release-version-match", action="store_true")
            parser.add_argument("--require-frontdoor-navigation-playwright", action="store_true")
            args = parser.parse_args()

            artifact_dir = Path(args.frontdoor_navigation_artifact_dir)
            artifact_dir.mkdir(parents=True, exist_ok=True)

            frontdoor_payload = {
                "blazor_shell": "interactive-server",
                "gm_blazor_shell": "interactive-server",
                "live_turn_companion_shell": True,
                "gm_live_turn_companion_shell": True,
                "pwa_manifest_path": "/manifest.player.webmanifest",
                "gm_pwa_manifest_path": "/manifest.gm.webmanifest",
                "player_session_handoff_strips_device": True,
                "gm_session_handoff_strips_device": True,
                "rybbit_configured": True,
                "gm_rybbit_configured": True,
            }
            (artifact_dir / "FRONTDOOR_MOBILE_LAUNCH.generated.json").write_text(
                json.dumps(frontdoor_payload, indent=2) + "\\n",
                encoding="utf-8",
            )

            payload = {
                "status": "fail",
                "frontdoorNavigationStatus": "pass",
                "frontdoorNavigationPlayRoute": "/mobile/player",
                "readyMobileHandoffFrontdoorLaunchRoute": "/mobile/player",
                "frontdoorNavigationPublicTargets": [],
                "frontdoorNavigationGatedTargets": ["Build", "Play"],
                "roleAliasRouteStatus": "pass",
                "pwaStaticStatus": "fail",
                "mobileLedgerStatus": "fail",
                "downloadsStatus": "fail",
                "readyMobileHandoffStatus": "fail",
                "participateIframeShellStatus": "fail",
                "failures": [
                    "downloads version marker proof is not pass",
                    "public PWA static asset proof is not pass",
                ],
            }
            Path(args.output).write_text(json.dumps(payload, indent=2) + "\\n", encoding="utf-8")
            raise SystemExit(1)
            """
        ),
        encoding="utf-8",
    )


def write_fake_passing_public_edge_gate_script(path: Path) -> None:
    path.write_text(
        textwrap.dedent(
            """\
            #!/usr/bin/env python3
            import argparse
            import json
            from pathlib import Path

            parser = argparse.ArgumentParser()
            parser.add_argument("--frontdoor-navigation-artifact-dir", required=True)
            parser.add_argument("--output", required=True)
            parser.add_argument("--base-url")
            parser.add_argument("--skip-preflight", action="store_true")
            parser.add_argument("--skip-release-version-match", action="store_true")
            parser.add_argument("--require-frontdoor-navigation-playwright", action="store_true")
            args = parser.parse_args()

            artifact_dir = Path(args.frontdoor_navigation_artifact_dir)
            artifact_dir.mkdir(parents=True, exist_ok=True)

            frontdoor_payload = {
                "blazor_shell": "interactive-server",
                "gm_blazor_shell": "interactive-server",
                "live_turn_companion_shell": True,
                "gm_live_turn_companion_shell": True,
                "pwa_manifest_path": "/manifest.player.webmanifest",
                "gm_pwa_manifest_path": "/manifest.gm.webmanifest",
                "player_session_handoff_preserves_session": True,
                "player_session_handoff_preserves_role": True,
                "player_session_handoff_strips_device": True,
                "gm_session_handoff_preserves_session": True,
                "gm_session_handoff_preserves_role": True,
                "gm_session_handoff_strips_device": True,
                "rybbit_configured": True,
                "gm_rybbit_configured": True,
            }
            (artifact_dir / "FRONTDOOR_MOBILE_LAUNCH.generated.json").write_text(
                json.dumps(frontdoor_payload, indent=2) + "\\n",
                encoding="utf-8",
            )

            payload = {
                "status": "pass",
                "frontdoorNavigationStatus": "pass",
                "frontdoorNavigationPlayRoute": "/mobile/player",
                "readyMobileHandoffFrontdoorLaunchRoute": "/mobile/player",
                "frontdoorNavigationPublicTargets": [],
                "frontdoorNavigationGatedTargets": ["Build", "Play"],
                "roleAliasRouteStatus": "pass",
                "pwaStaticStatus": "pass",
                "mobileLedgerStatus": "pass",
                "downloadsStatus": "pass",
                "readyMobileHandoffStatus": "pass",
                "participateIframeShellStatus": "pass",
                "failures": [],
            }
            Path(args.output).write_text(json.dumps(payload, indent=2) + "\\n", encoding="utf-8")
            raise SystemExit(0)
            """
        ),
        encoding="utf-8",
    )


class MobileCrossSurfaceRefreshContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.cross_surface_module = load_module(
            CROSS_SURFACE_SCRIPT,
            "materialize_mobile_cross_surface_readiness_test_module",
        )
        cls.local_release_module = load_module(
            LOCAL_RELEASE_SCRIPT,
            "materialize_mobile_local_release_proof_test_module",
        )

    def test_release_mode_rejects_skip_public_edge_before_materialization(self) -> None:
        with (
            mock.patch.dict(os.environ, {"CHUMMER_VERIFY_MODE": "release"}),
            mock.patch.object(
                sys,
                "argv",
                ["materialize_mobile_cross_surface_readiness.py", "--skip-public-edge"],
            ),
            self.assertRaisesRegex(SystemExit, "release verification cannot skip public-edge proof"),
        ):
            self.cross_surface_module.main()

    def test_local_release_loader_rejects_release_receipt_with_skipped_public_edge(self) -> None:
        payload = {
            "contract_name": "chummer6-mobile.cross_surface_readiness_refresh.v1",
            "verification_mode": "release",
            "generated_at_utc": "2026-07-18T20:00:00Z",
            "status": "pass",
            "checks": {
                "fleet_mobile_play_shell_ready": True,
                "fleet_mobile_local_release_passed": True,
                "fleet_mobile_scope_not_blocking": True,
            },
            "public_edge": {"skipped": True},
            "fleet_readiness": {},
            "surface_source_fingerprint": {"kind": "current_checkout_sha256", "files": []},
        }
        published_dir = ROOT / ".codex-studio" / "published"
        published_dir.mkdir(parents=True, exist_ok=True)
        with tempfile.NamedTemporaryFile(
            prefix="mobile-cross-surface-release-skip-",
            suffix=".json",
            dir=published_dir,
            delete=False,
        ) as handle:
            receipt_path = Path(handle.name)
        try:
            receipt_path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
            with mock.patch.object(
                self.local_release_module,
                "MOBILE_CROSS_SURFACE_REFRESH_RECEIPT",
                receipt_path,
            ):
                _, errors = self.local_release_module.load_cross_surface_refresh()
        finally:
            receipt_path.unlink(missing_ok=True)

        self.assertIn("release cross-surface refresh cannot skip public-edge proof", errors)

    def test_cross_surface_materializer_writes_fail_receipt_for_public_edge_blockers(self) -> None:
        with tempfile.TemporaryDirectory(prefix="mobile-cross-surface-refresh-") as temp_dir:
            temp_root = Path(temp_dir)
            fleet_script = temp_root / "fleet.py"
            gate_script = temp_root / "public_edge_gate.py"
            output_path = temp_root / "MOBILE_CROSS_SURFACE_READINESS.generated.json"
            strict_preflight_receipt = temp_root / "strict-preflight.json"
            strict_postdeploy_receipt = temp_root / "strict-postdeploy.json"
            write_fake_fleet_script(fleet_script)
            write_fake_public_edge_gate_script(gate_script)
            strict_preflight_receipt.write_text(
                json.dumps(
                    {
                        "status": "pass",
                        "allowForeignBuildLocks": False,
                        "allowStaleForeignBuildLocks": False,
                        "findings": [],
                    },
                    indent=2,
                )
                + "\n",
                encoding="utf-8",
            )
            strict_postdeploy_receipt.write_text(
                json.dumps(
                    {
                        "status": "pass",
                        "preflightStatus": "pass",
                        "preflightAllowForeignBuildLocks": False,
                        "preflightAllowStaleForeignBuildLocks": False,
                        "skipPreflight": False,
                        "skipReleaseVersionMatch": False,
                        "strictPreflight": True,
                        "strictInvocation": True,
                        "strictNoAllowanceInvocation": True,
                        "failures": [],
                    },
                    indent=2,
                )
                + "\n",
                encoding="utf-8",
            )

            with (
                mock.patch.object(self.cross_surface_module, "FLEET_MATERIALIZER", fleet_script),
                mock.patch.object(self.cross_surface_module, "PUBLIC_EDGE_GATE", gate_script),
                mock.patch.object(self.cross_surface_module, "STRICT_PUBLIC_EDGE_PREFLIGHT_RECEIPT", strict_preflight_receipt),
                mock.patch.object(self.cross_surface_module, "STRICT_PUBLIC_EDGE_POSTDEPLOY_RECEIPT", strict_postdeploy_receipt),
                mock.patch.object(
                    self.cross_surface_module,
                    "load_live_build_lock_probe",
                    return_value={
                        "status": "present",
                        "command": "ps -eo pid=,args=",
                        "process_count": 2,
                        "entries": [
                            {"pid": 202947, "command": "bash scripts/build-chummer6-linux.sh --base /work/base"},
                            {"pid": 2201485, "command": "bash scripts/build-chummer6-linux.sh --base /work/base"},
                        ],
                    },
                ),
                mock.patch.object(self.cross_surface_module, "OUT", output_path),
                mock.patch.object(
                    sys,
                    "argv",
                    ["materialize_mobile_cross_surface_readiness.py", "--output", str(output_path)],
                ),
            ):
                exit_code = self.cross_surface_module.main()

            self.assertEqual(exit_code, 0)
            payload = json.loads(output_path.read_text(encoding="utf-8"))

        self.assertEqual(payload["status"], "fail")
        self.assertTrue(payload["checks"]["fleet_mobile_play_shell_ready"])
        self.assertTrue(payload["checks"]["frontdoor_player_gm_blazor_shells_live"])
        self.assertFalse(payload["checks"]["public_edge_pwa_static_pass"])
        self.assertFalse(payload["checks"]["public_edge_gate_pass"])
        self.assertEqual(payload["public_edge"]["status"], "fail")
        self.assertEqual(payload["public_edge"]["gate_exit_code"], 1)
        self.assertEqual("present", payload["public_edge"]["live_build_lock_probe"]["status"])
        self.assertEqual(
            payload["public_edge"]["failures"],
            [
                "downloads version marker proof is not pass",
                "public PWA static asset proof is not pass",
            ],
        )

    def test_cross_surface_materializer_fails_when_strict_public_edge_receipt_is_degraded(self) -> None:
        with tempfile.TemporaryDirectory(prefix="mobile-cross-surface-refresh-") as temp_dir:
            temp_root = Path(temp_dir)
            fleet_script = temp_root / "fleet.py"
            gate_script = temp_root / "public_edge_gate.py"
            output_path = temp_root / "MOBILE_CROSS_SURFACE_READINESS.generated.json"
            strict_preflight_receipt = temp_root / "strict-preflight.json"
            strict_postdeploy_receipt = temp_root / "strict-postdeploy.json"
            write_fake_fleet_script(fleet_script)
            write_fake_passing_public_edge_gate_script(gate_script)
            strict_preflight_receipt.write_text(
                json.dumps(
                    {
                        "status": "pass",
                        "allowForeignBuildLocks": False,
                        "allowStaleForeignBuildLocks": False,
                        "findings": [],
                    },
                    indent=2,
                )
                + "\n",
                encoding="utf-8",
            )
            strict_postdeploy_receipt.write_text(
                json.dumps(
                    {
                        "status": "pass",
                        "preflightStatus": "pass",
                        "preflightAllowForeignBuildLocks": False,
                        "preflightAllowStaleForeignBuildLocks": False,
                        "skipPreflight": True,
                        "skipReleaseVersionMatch": True,
                        "strictPreflight": False,
                        "strictInvocation": False,
                        "strictNoAllowanceInvocation": False,
                        "failures": [],
                    },
                    indent=2,
                )
                + "\n",
                encoding="utf-8",
            )

            with (
                mock.patch.object(self.cross_surface_module, "FLEET_MATERIALIZER", fleet_script),
                mock.patch.object(self.cross_surface_module, "PUBLIC_EDGE_GATE", gate_script),
                mock.patch.object(self.cross_surface_module, "STRICT_PUBLIC_EDGE_PREFLIGHT_RECEIPT", strict_preflight_receipt),
                mock.patch.object(self.cross_surface_module, "STRICT_PUBLIC_EDGE_POSTDEPLOY_RECEIPT", strict_postdeploy_receipt),
                mock.patch.object(self.cross_surface_module, "OUT", output_path),
                mock.patch.object(
                    sys,
                    "argv",
                    ["materialize_mobile_cross_surface_readiness.py", "--output", str(output_path)],
                ),
            ):
                exit_code = self.cross_surface_module.main()

            self.assertEqual(exit_code, 0)
            payload = json.loads(output_path.read_text(encoding="utf-8"))

        self.assertEqual(payload["status"], "fail")
        self.assertTrue(payload["checks"]["public_edge_gate_pass"])
        self.assertFalse(payload["checks"]["strict_public_edge_gate_pass"])
        self.assertEqual(payload["public_edge"]["status"], "fail")
        self.assertEqual(payload["public_edge"]["live_gate_status"], "pass")
        self.assertEqual(payload["public_edge"]["strict_status"], "fail")
        self.assertTrue(payload["public_edge"]["strict_postdeploy_skip_preflight"])
        self.assertTrue(payload["public_edge"]["strict_postdeploy_skip_release_version_match"])
        self.assertFalse(payload["public_edge"]["strict_postdeploy_strict_preflight"])
        self.assertFalse(payload["public_edge"]["strict_postdeploy_strict_no_allowance_invocation"])
        self.assertIn(
            "strict public-edge postdeploy receipt was generated with skipped strict checks",
            payload["public_edge"]["failures"],
        )
        self.assertIn(
            "strict public-edge postdeploy receipt was not generated with strict preflight/no-allowance mode",
            payload["public_edge"]["failures"],
        )

    def test_cross_surface_materializer_ignores_stale_strict_postdeploy_failures(self) -> None:
        with tempfile.TemporaryDirectory(prefix="mobile-cross-surface-refresh-") as temp_dir:
            temp_root = Path(temp_dir)
            fleet_script = temp_root / "fleet.py"
            gate_script = temp_root / "public_edge_gate.py"
            output_path = temp_root / "MOBILE_CROSS_SURFACE_READINESS.generated.json"
            strict_preflight_receipt = temp_root / "strict-preflight.json"
            strict_postdeploy_receipt = temp_root / "strict-postdeploy.json"
            write_fake_fleet_script(fleet_script)
            write_fake_passing_public_edge_gate_script(gate_script)
            strict_preflight_receipt.write_text(
                json.dumps(
                    {
                        "generatedAtUtc": "2026-07-06T03:16:40.365067+00:00",
                        "status": "fail",
                        "allowForeignBuildLocks": False,
                        "allowStaleForeignBuildLocks": False,
                        "findings": [
                            {
                                "detail": "bash pid 2201485 matches build-chummer6-linux",
                            }
                        ],
                    },
                    indent=2,
                )
                + "\n",
                encoding="utf-8",
            )
            strict_postdeploy_receipt.write_text(
                json.dumps(
                    {
                        "generatedAtUtc": "2026-07-05T13:34:52.608371+00:00",
                        "status": "fail",
                        "preflightStatus": "fail",
                        "preflightAllowForeignBuildLocks": True,
                        "preflightAllowStaleForeignBuildLocks": True,
                        "skipPreflight": None,
                        "skipReleaseVersionMatch": None,
                        "failures": [
                            "downloads version marker proof is not pass",
                            "/player resolved to /play?role=player instead of /mobile/player",
                        ],
                    },
                    indent=2,
                )
                + "\n",
                encoding="utf-8",
            )

            with (
                mock.patch.object(self.cross_surface_module, "FLEET_MATERIALIZER", fleet_script),
                mock.patch.object(self.cross_surface_module, "PUBLIC_EDGE_GATE", gate_script),
                mock.patch.object(self.cross_surface_module, "STRICT_PUBLIC_EDGE_PREFLIGHT_RECEIPT", strict_preflight_receipt),
                mock.patch.object(self.cross_surface_module, "STRICT_PUBLIC_EDGE_POSTDEPLOY_RECEIPT", strict_postdeploy_receipt),
                mock.patch.object(
                    self.cross_surface_module,
                    "load_live_build_lock_probe",
                    return_value={
                        "status": "present",
                        "command": "ps -eo pid=,args=",
                        "returncode": 0,
                        "process_count": 2,
                        "entries": [
                            {"pid": 2201485, "command": "bash scripts/build-chummer6-linux.sh --base /work/first"},
                            {"pid": 202947, "command": "bash scripts/build-chummer6-linux.sh --base /work/second"},
                        ],
                    },
                ),
                mock.patch.object(self.cross_surface_module, "OUT", output_path),
                mock.patch.object(
                    sys,
                    "argv",
                    ["materialize_mobile_cross_surface_readiness.py", "--output", str(output_path)],
                ),
            ):
                exit_code = self.cross_surface_module.main()

            self.assertEqual(exit_code, 0)
            payload = json.loads(output_path.read_text(encoding="utf-8"))

        self.assertEqual(payload["status"], "fail")
        self.assertTrue(payload["checks"]["public_edge_gate_pass"])
        self.assertFalse(payload["checks"]["strict_public_edge_gate_pass"])
        self.assertTrue(payload["public_edge"]["strict_postdeploy_stale"])
        self.assertIn(
            "strict public-edge postdeploy receipt is older than the current strict preflight receipt",
            payload["public_edge"]["failures"],
        )
        self.assertIn(
            "live build lock probe pid 2201485 matches build-chummer6-linux",
            payload["public_edge"]["failures"],
        )
        self.assertIn(
            "live build lock probe pid 202947 matches build-chummer6-linux",
            payload["public_edge"]["failures"],
        )
        self.assertNotIn(
            "bash pid 2201485 matches build-chummer6-linux",
            payload["public_edge"]["failures"],
        )
        self.assertEqual("present", payload["public_edge"]["live_build_lock_probe"]["status"])
        self.assertNotIn(
            "downloads version marker proof is not pass",
            payload["public_edge"]["failures"],
        )
        self.assertNotIn(
            "/player resolved to /play?role=player instead of /mobile/player",
            payload["public_edge"]["failures"],
        )

    def test_cross_surface_materializer_records_false_strict_postdeploy_flags_when_receipt_missing(self) -> None:
        with tempfile.TemporaryDirectory(prefix="mobile-cross-surface-refresh-") as temp_dir:
            temp_root = Path(temp_dir)
            fleet_script = temp_root / "fleet.py"
            gate_script = temp_root / "public_edge_gate.py"
            output_path = temp_root / "MOBILE_CROSS_SURFACE_READINESS.generated.json"
            strict_preflight_receipt = temp_root / "strict-preflight.json"
            strict_postdeploy_receipt = temp_root / "strict-postdeploy.json"
            write_fake_fleet_script(fleet_script)
            write_fake_passing_public_edge_gate_script(gate_script)
            strict_preflight_receipt.write_text(
                json.dumps(
                    {
                        "generatedAtUtc": "2026-07-06T03:16:40.365067+00:00",
                        "status": "fail",
                        "allowForeignBuildLocks": False,
                        "allowStaleForeignBuildLocks": False,
                        "findings": [],
                    },
                    indent=2,
                )
                + "\n",
                encoding="utf-8",
            )

            with (
                mock.patch.object(self.cross_surface_module, "FLEET_MATERIALIZER", fleet_script),
                mock.patch.object(self.cross_surface_module, "PUBLIC_EDGE_GATE", gate_script),
                mock.patch.object(self.cross_surface_module, "STRICT_PUBLIC_EDGE_PREFLIGHT_RECEIPT", strict_preflight_receipt),
                mock.patch.object(self.cross_surface_module, "STRICT_PUBLIC_EDGE_POSTDEPLOY_RECEIPT", strict_postdeploy_receipt),
                mock.patch.object(
                    self.cross_surface_module,
                    "load_live_build_lock_probe",
                    return_value={
                        "status": "present",
                        "command": "ps -eo pid=,args=",
                        "returncode": 0,
                        "process_count": 1,
                        "entries": [
                            {"pid": 202947, "command": "bash scripts/build-chummer6-linux.sh --base /work/base"},
                        ],
                    },
                ),
                mock.patch.object(self.cross_surface_module, "OUT", output_path),
                mock.patch.object(
                    sys,
                    "argv",
                    ["materialize_mobile_cross_surface_readiness.py", "--output", str(output_path)],
                ),
            ):
                exit_code = self.cross_surface_module.main()

            self.assertEqual(exit_code, 0)
            payload = json.loads(output_path.read_text(encoding="utf-8"))

        self.assertEqual(payload["status"], "fail")
        self.assertIsNone(payload["public_edge"]["strict_postdeploy_status"])
        self.assertFalse(payload["public_edge"]["strict_postdeploy_stale"])
        self.assertFalse(payload["public_edge"]["strict_postdeploy_strict_preflight"])
        self.assertFalse(payload["public_edge"]["strict_postdeploy_strict_invocation"])
        self.assertFalse(payload["public_edge"]["strict_postdeploy_strict_no_allowance_invocation"])
        self.assertIn(
            "strict public-edge postdeploy receipt is missing",
            payload["public_edge"]["failures"],
        )

    def test_local_release_loader_accepts_failed_refresh_when_mobile_live_checks_hold(self) -> None:
        cross_surface_files = []
        for relative_path in self.cross_surface_module.SURFACE_SOURCE_FILES:
            source_path = ROOT / relative_path
            cross_surface_files.append(
                {
                    "path": relative_path,
                    "sha256": self.local_release_module.sha256_file(source_path),
                }
            )

        refresh_payload = {
            "contract_name": "chummer6-mobile.cross_surface_readiness_refresh.v1",
            "generated_at_utc": "2026-07-05T10:30:00Z",
            "status": "fail",
            "base_url": "https://chummer.run",
            "checks": {
                "fleet_mobile_play_shell_ready": True,
                "fleet_mobile_local_release_passed": True,
                "fleet_mobile_scope_not_blocking": True,
                "public_edge_frontdoor_navigation_pass": True,
                "public_edge_frontdoor_route_is_player": True,
                "public_edge_handoff_launch_route_is_player": True,
                "public_edge_role_alias_routes_pass": True,
                "public_edge_pwa_static_pass": False,
                "public_edge_mobile_ledger_pass": False,
                "public_edge_public_targets_keep_play_only": True,
                "frontdoor_player_gm_blazor_shells_live": True,
                "frontdoor_player_gm_role_manifests_live": True,
                "frontdoor_player_gm_handoff_links_preserve_role_and_strip_device": True,
                "frontdoor_rybbit_roles_live": True,
                "public_edge_gate_pass": True,
                "strict_public_edge_gate_pass": False,
            },
            "fleet_readiness": {
                "status": "pass",
                "warning_keys": [],
                "missing_keys": [],
                "mobile_play_shell": {
                    "status": "ready",
                    "summary": "Mobile shell is ready.",
                    "mobile_local_release_status": "passed",
                },
            },
            "public_edge": {
                "skipped": False,
                "status": "fail",
                "live_gate_status": "pass",
                "strict_status": "fail",
                "gate_exit_code": 1,
                "downloads_status": "fail",
                "frontdoor_navigation_status": "pass",
                "frontdoor_route": "/mobile/player",
                "handoff_launch_route": "/mobile/player",
                "role_alias_route_status": "pass",
                "pwa_static_status": "fail",
                "mobile_ledger_status": "fail",
                "ready_mobile_handoff_status": "fail",
                "participate_iframe_shell_status": "fail",
                "player_manifest_path": "/manifest.player.webmanifest",
                "gm_manifest_path": "/manifest.gm.webmanifest",
                "player_handoff_strips_device": True,
                "gm_handoff_strips_device": True,
                "rybbit_player": True,
                "rybbit_gm": True,
                "strict_preflight_status": "fail",
                "strict_postdeploy_status": "fail",
                "strict_postdeploy_stale": False,
                "strict_postdeploy_skip_preflight": None,
                "strict_postdeploy_skip_release_version_match": None,
                "strict_postdeploy_strict_preflight": True,
                "strict_postdeploy_strict_invocation": True,
                "strict_postdeploy_strict_no_allowance_invocation": True,
                "live_build_lock_probe": {
                    "status": "present",
                    "command": "ps -eo pid=,args=",
                    "process_count": 1,
                    "entries": [
                        {"pid": 202947, "command": "bash scripts/build-chummer6-linux.sh --base /work/base"},
                    ],
                },
                "failures": ["downloads version marker proof is not pass"],
            },
            "surface_source_fingerprint": {
                "kind": "current_checkout_sha256",
                "file_count": len(cross_surface_files),
                "files": cross_surface_files,
            },
        }

        published_dir = ROOT / ".codex-studio" / "published"
        published_dir.mkdir(parents=True, exist_ok=True)
        with tempfile.NamedTemporaryFile(
            prefix="mobile-cross-surface-refresh-test-",
            suffix=".json",
            dir=published_dir,
            delete=False,
        ) as handle:
            receipt_path = Path(handle.name)
        try:
            receipt_path.write_text(json.dumps(refresh_payload, indent=2) + "\n", encoding="utf-8")
            with mock.patch.object(self.local_release_module, "MOBILE_CROSS_SURFACE_REFRESH_RECEIPT", receipt_path):
                summary, errors = self.local_release_module.load_cross_surface_refresh()
        finally:
            receipt_path.unlink(missing_ok=True)

        self.assertEqual(errors, [])
        self.assertEqual(summary["status"], "fail")
        self.assertEqual(summary["public_edge"]["status"], "fail")
        self.assertFalse(summary["public_edge"]["strict_postdeploy_stale"])
        self.assertEqual(
            summary["public_edge"]["failures"],
            ["downloads version marker proof is not pass"],
        )
        self.assertEqual("present", summary["public_edge"]["live_build_lock_probe"]["status"])
        self.assertEqual(summary["public_edge"]["frontdoor_route"], "/mobile/player")

    def test_local_release_loader_preserves_stale_strict_postdeploy_signal(self) -> None:
        cross_surface_files = []
        for relative_path in self.cross_surface_module.SURFACE_SOURCE_FILES:
            source_path = ROOT / relative_path
            cross_surface_files.append(
                {
                    "path": relative_path,
                    "sha256": self.local_release_module.sha256_file(source_path),
                }
            )

        refresh_payload = {
            "contract_name": "chummer6-mobile.cross_surface_readiness_refresh.v1",
            "generated_at_utc": "2026-07-06T03:19:11Z",
            "status": "fail",
            "base_url": "https://chummer.run",
            "checks": {
                "fleet_mobile_play_shell_ready": True,
                "fleet_mobile_local_release_passed": True,
                "fleet_mobile_scope_not_blocking": True,
                "public_edge_frontdoor_navigation_pass": True,
                "public_edge_frontdoor_route_is_player": True,
                "public_edge_handoff_launch_route_is_player": True,
                "public_edge_role_alias_routes_pass": True,
                "public_edge_pwa_static_pass": True,
                "public_edge_mobile_ledger_pass": True,
                "public_edge_public_targets_keep_play_only": True,
                "frontdoor_player_gm_blazor_shells_live": True,
                "frontdoor_player_gm_role_manifests_live": True,
                "frontdoor_player_gm_handoff_links_preserve_role_and_strip_device": True,
                "frontdoor_rybbit_roles_live": True,
                "public_edge_gate_pass": True,
                "strict_public_edge_gate_pass": False,
            },
            "fleet_readiness": {
                "status": "pass",
                "warning_keys": [],
                "missing_keys": [],
                "mobile_play_shell": {
                    "status": "ready",
                    "summary": "Mobile shell is ready.",
                    "mobile_local_release_status": "passed",
                },
            },
            "public_edge": {
                "skipped": False,
                "status": "fail",
                "live_gate_status": "pass",
                "strict_status": "fail",
                "gate_exit_code": 0,
                "downloads_status": "pass",
                "frontdoor_navigation_status": "pass",
                "frontdoor_route": "/mobile/player",
                "handoff_launch_route": "/mobile/player",
                "role_alias_route_status": "pass",
                "pwa_static_status": "pass",
                "mobile_ledger_status": "pass",
                "ready_mobile_handoff_status": "pass",
                "participate_iframe_shell_status": "pass",
                "player_manifest_path": "/manifest.player.webmanifest",
                "gm_manifest_path": "/manifest.gm.webmanifest",
                "player_handoff_strips_device": True,
                "gm_handoff_strips_device": True,
                "rybbit_player": True,
                "rybbit_gm": True,
                "strict_preflight_status": "fail",
                "strict_postdeploy_status": "fail",
                "strict_postdeploy_stale": True,
                "strict_postdeploy_skip_preflight": None,
                "strict_postdeploy_skip_release_version_match": None,
                "strict_postdeploy_strict_preflight": True,
                "strict_postdeploy_strict_invocation": True,
                "strict_postdeploy_strict_no_allowance_invocation": True,
                "live_build_lock_probe": {
                    "status": "present",
                    "command": "ps -eo pid=,args=",
                    "process_count": 2,
                    "entries": [
                        {"pid": 202947, "command": "bash scripts/build-chummer6-linux.sh --base /work/base"},
                        {"pid": 2201485, "command": "bash scripts/build-chummer6-linux.sh --base /work/base"},
                    ],
                },
                "failures": [
                    "strict public-edge preflight receipt is not pass",
                    "live build lock probe pid 202947 matches build-chummer6-linux",
                    "live build lock probe pid 2201485 matches build-chummer6-linux",
                    "strict public-edge postdeploy receipt is older than the current strict preflight receipt",
                ],
            },
            "surface_source_fingerprint": {
                "kind": "current_checkout_sha256",
                "file_count": len(cross_surface_files),
                "files": cross_surface_files,
            },
        }

        published_dir = ROOT / ".codex-studio" / "published"
        published_dir.mkdir(parents=True, exist_ok=True)
        with tempfile.NamedTemporaryFile(
            prefix="mobile-cross-surface-refresh-test-",
            suffix=".json",
            dir=published_dir,
            delete=False,
        ) as handle:
            receipt_path = Path(handle.name)
        try:
            receipt_path.write_text(json.dumps(refresh_payload, indent=2) + "\n", encoding="utf-8")
            with mock.patch.object(self.local_release_module, "MOBILE_CROSS_SURFACE_REFRESH_RECEIPT", receipt_path):
                summary, errors = self.local_release_module.load_cross_surface_refresh()
        finally:
            receipt_path.unlink(missing_ok=True)

        self.assertEqual(errors, [])
        self.assertEqual(summary["status"], "fail")
        self.assertTrue(summary["public_edge"]["strict_postdeploy_stale"])
        self.assertTrue(summary["public_edge"]["strict_postdeploy_strict_preflight"])
        self.assertTrue(summary["public_edge"]["strict_postdeploy_strict_invocation"])
        self.assertTrue(summary["public_edge"]["strict_postdeploy_strict_no_allowance_invocation"])
        self.assertEqual(
            summary["public_edge"]["failures"],
            [
                "strict public-edge preflight receipt is not pass",
                "live build lock probe pid 202947 matches build-chummer6-linux",
                "live build lock probe pid 2201485 matches build-chummer6-linux",
                "strict public-edge postdeploy receipt is older than the current strict preflight receipt",
            ],
        )
        self.assertEqual("present", summary["public_edge"]["live_build_lock_probe"]["status"])

    def test_local_release_loader_rejects_pass_refresh_without_strict_public_edge_gate(self) -> None:
        cross_surface_files = []
        for relative_path in self.cross_surface_module.SURFACE_SOURCE_FILES:
            source_path = ROOT / relative_path
            cross_surface_files.append(
                {
                    "path": relative_path,
                    "sha256": self.local_release_module.sha256_file(source_path),
                }
            )

        refresh_payload = {
            "contract_name": "chummer6-mobile.cross_surface_readiness_refresh.v1",
            "generated_at_utc": "2026-07-05T10:30:00Z",
            "status": "pass",
            "base_url": "https://chummer.run",
            "checks": {
                "fleet_mobile_play_shell_ready": True,
                "fleet_mobile_local_release_passed": True,
                "fleet_mobile_scope_not_blocking": True,
                "public_edge_frontdoor_navigation_pass": True,
                "public_edge_frontdoor_route_is_player": True,
                "public_edge_handoff_launch_route_is_player": True,
                "public_edge_role_alias_routes_pass": True,
                "public_edge_pwa_static_pass": True,
                "public_edge_mobile_ledger_pass": True,
                "public_edge_public_targets_keep_play_only": True,
                "frontdoor_player_gm_blazor_shells_live": True,
                "frontdoor_player_gm_role_manifests_live": True,
                "frontdoor_player_gm_handoff_links_preserve_role_and_strip_device": True,
                "frontdoor_rybbit_roles_live": True,
                "public_edge_gate_pass": True,
                "strict_public_edge_gate_pass": False,
            },
            "fleet_readiness": {
                "status": "pass",
                "warning_keys": [],
                "missing_keys": [],
                "mobile_play_shell": {
                    "status": "ready",
                    "summary": "Mobile shell is ready.",
                    "mobile_local_release_status": "passed",
                },
            },
            "public_edge": {
                "skipped": False,
                "status": "fail",
                "live_gate_status": "pass",
                "strict_status": "fail",
                "gate_exit_code": 0,
                "downloads_status": "pass",
                "frontdoor_navigation_status": "pass",
                "frontdoor_route": "/mobile/player",
                "handoff_launch_route": "/mobile/player",
                "role_alias_route_status": "pass",
                "pwa_static_status": "pass",
                "mobile_ledger_status": "pass",
                "ready_mobile_handoff_status": "pass",
                "participate_iframe_shell_status": "pass",
                "player_manifest_path": "/manifest.player.webmanifest",
                "gm_manifest_path": "/manifest.gm.webmanifest",
                "player_handoff_strips_device": True,
                "gm_handoff_strips_device": True,
                "rybbit_player": True,
                "rybbit_gm": True,
                "strict_preflight_status": "pass",
                "strict_postdeploy_status": "pass",
                "strict_postdeploy_skip_preflight": True,
                "strict_postdeploy_skip_release_version_match": True,
                "strict_postdeploy_strict_preflight": False,
                "strict_postdeploy_strict_invocation": False,
                "strict_postdeploy_strict_no_allowance_invocation": False,
                "failures": ["strict public-edge postdeploy receipt was generated with skipped strict checks"],
            },
            "surface_source_fingerprint": {
                "kind": "current_checkout_sha256",
                "file_count": len(cross_surface_files),
                "files": cross_surface_files,
            },
        }

        published_dir = ROOT / ".codex-studio" / "published"
        published_dir.mkdir(parents=True, exist_ok=True)
        with tempfile.NamedTemporaryFile(
            prefix="mobile-cross-surface-refresh-test-",
            suffix=".json",
            dir=published_dir,
            delete=False,
        ) as handle:
            receipt_path = Path(handle.name)
        try:
            receipt_path.write_text(json.dumps(refresh_payload, indent=2) + "\n", encoding="utf-8")
            with mock.patch.object(self.local_release_module, "MOBILE_CROSS_SURFACE_REFRESH_RECEIPT", receipt_path):
                _, errors = self.local_release_module.load_cross_surface_refresh()
        finally:
            receipt_path.unlink(missing_ok=True)

        self.assertIn("cross-surface refresh strict public-edge check failed: strict_public_edge_gate_pass", errors)

    def test_local_release_loader_accepts_failed_refresh_with_explicit_blockers_and_missing_frontdoor_artifacts(self) -> None:
        cross_surface_files = []
        for relative_path in self.cross_surface_module.SURFACE_SOURCE_FILES:
            source_path = ROOT / relative_path
            cross_surface_files.append(
                {
                    "path": relative_path,
                    "sha256": self.local_release_module.sha256_file(source_path),
                }
            )

        refresh_payload = {
            "contract_name": "chummer6-mobile.cross_surface_readiness_refresh.v1",
            "generated_at_utc": "2026-07-05T10:30:00Z",
            "status": "fail",
            "base_url": "https://chummer.run",
            "checks": {
                "fleet_mobile_play_shell_ready": True,
                "fleet_mobile_local_release_passed": True,
                "fleet_mobile_scope_not_blocking": True,
                "public_edge_frontdoor_navigation_pass": False,
                "public_edge_frontdoor_route_is_player": False,
                "public_edge_handoff_launch_route_is_player": False,
                "public_edge_role_alias_routes_pass": False,
                "public_edge_pwa_static_pass": False,
                "public_edge_mobile_ledger_pass": True,
                "public_edge_public_targets_keep_play_only": False,
                "frontdoor_player_gm_blazor_shells_live": False,
                "frontdoor_player_gm_role_manifests_live": False,
                "frontdoor_player_gm_handoff_links_preserve_role_and_strip_device": False,
                "frontdoor_rybbit_roles_live": False,
                "public_edge_gate_pass": False,
                "strict_public_edge_gate_pass": False,
            },
            "fleet_readiness": {
                "status": "pass",
                "warning_keys": [],
                "missing_keys": [],
                "mobile_play_shell": {
                    "status": "ready",
                    "summary": "Mobile shell is ready.",
                    "mobile_local_release_status": "passed",
                },
            },
            "public_edge": {
                "skipped": False,
                "status": "fail",
                "gate_exit_code": 1,
                "downloads_status": "fail",
                "frontdoor_navigation_status": "fail",
                "frontdoor_route": None,
                "handoff_launch_route": None,
                "role_alias_route_status": "fail",
                "pwa_static_status": "fail",
                "mobile_ledger_status": "pass",
                "ready_mobile_handoff_status": "fail",
                "participate_iframe_shell_status": "fail",
                "player_manifest_path": None,
                "gm_manifest_path": None,
                "player_handoff_strips_device": None,
                "gm_handoff_strips_device": None,
                "rybbit_player": None,
                "rybbit_gm": None,
                "strict_postdeploy_strict_preflight": False,
                "strict_postdeploy_strict_invocation": False,
                "strict_postdeploy_strict_no_allowance_invocation": False,
                "live_build_lock_probe": {
                    "status": "clear",
                    "command": "ps -eo pid=,args=",
                    "process_count": 0,
                    "entries": [],
                },
                "failures": [
                    "role alias route redirects drifted",
                    "front-door navigation Playwright proof failed before artifacts were written: Error: Homepage does not disclose current public lane",
                ],
            },
            "surface_source_fingerprint": {
                "kind": "current_checkout_sha256",
                "file_count": len(cross_surface_files),
                "files": cross_surface_files,
            },
        }

        published_dir = ROOT / ".codex-studio" / "published"
        published_dir.mkdir(parents=True, exist_ok=True)
        with tempfile.NamedTemporaryFile(
            prefix="mobile-cross-surface-refresh-test-",
            suffix=".json",
            dir=published_dir,
            delete=False,
        ) as handle:
            receipt_path = Path(handle.name)
        try:
            receipt_path.write_text(json.dumps(refresh_payload, indent=2) + "\n", encoding="utf-8")
            with mock.patch.object(self.local_release_module, "MOBILE_CROSS_SURFACE_REFRESH_RECEIPT", receipt_path):
                summary, errors = self.local_release_module.load_cross_surface_refresh()
        finally:
            receipt_path.unlink(missing_ok=True)

        self.assertEqual(errors, [])
        self.assertEqual(summary["status"], "fail")
        self.assertEqual(summary["public_edge"]["status"], "fail")
        self.assertIsNone(summary["public_edge"]["frontdoor_route"])
        self.assertEqual("clear", summary["public_edge"]["live_build_lock_probe"]["status"])
        self.assertEqual(
            summary["public_edge"]["failures"],
            [
                "role alias route redirects drifted",
                "front-door navigation Playwright proof failed before artifacts were written: Error: Homepage does not disclose current public lane",
            ],
        )


if __name__ == "__main__":
    unittest.main()
