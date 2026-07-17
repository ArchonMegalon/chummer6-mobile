from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock


REPO_ROOT = Path(__file__).resolve().parents[1]
BOUNDARY_SCRIPT_PATH = REPO_ROOT / "scripts" / "materialize_mobile_release_boundary.py"
PROOF_SCRIPT_PATH = REPO_ROOT / "scripts" / "materialize_mobile_local_release_proof.py"


def load_module(module_name: str, script_path: Path):
    spec = importlib.util.spec_from_file_location(module_name, script_path)
    module = importlib.util.module_from_spec(spec)
    assert spec is not None and spec.loader is not None
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


class MobileReleaseBoundaryTests(unittest.TestCase):
    def test_parse_git_status_lines_preserves_status_and_rename_target(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        rows = module.parse_git_status_lines(
            "MM docs/PLAY_RELEASE_SIGNOFF.md\n"
            "?? tests/test_mobile_release_boundary.py\n"
            "R  old.txt -> new.txt\n"
        )

        self.assertEqual(
            [
                {"status": "MM", "path": "docs/PLAY_RELEASE_SIGNOFF.md", "raw": "MM docs/PLAY_RELEASE_SIGNOFF.md"},
                {"status": "??", "path": "tests/test_mobile_release_boundary.py", "raw": "?? tests/test_mobile_release_boundary.py"},
                {"status": "R ", "path": "new.txt", "raw": "R  old.txt -> new.txt"},
            ],
            rows,
        )

    def test_machine_local_noise_findings_allow_placeholder_and_fixture_device_ids(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        findings = module.machine_local_noise_findings(
            '{"handoff":"http://127.0.0.1:<port>/mobile/player?deviceId=<minted-device>",'
            '"deviceId":"hero-player-shell"}'
        )

        self.assertEqual([], findings)

    def test_machine_local_noise_findings_flag_real_localhost_and_unredacted_device(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        findings = module.machine_local_noise_findings(
            '{"handoff":"http://127.0.0.1:58182/mobile/player?deviceId=tablet-22","deviceId":"tablet-22"}'
        )

        self.assertIn("receipt contains an unredacted localhost origin", findings)
        self.assertIn("receipt contains an unredacted device id: tablet-22", findings)

    def test_worktree_summary_counts_release_receipts_inside_owned_boundary(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        summary = module.worktree_summary(
            [
                {"status": "MM", "path": "docs/PLAY_RELEASE_SIGNOFF.md", "raw": "MM docs/PLAY_RELEASE_SIGNOFF.md"},
                {
                    "status": "AM",
                    "path": ".codex-studio/published/MOBILE_PWA_RUNTIME_SMOKE.generated.json",
                    "raw": "AM .codex-studio/published/MOBILE_PWA_RUNTIME_SMOKE.generated.json",
                },
                {"status": " M", "path": ".codex-design/product/HORIZON_REGISTRY.yaml", "raw": " M .codex-design/product/HORIZON_REGISTRY.yaml"},
            ],
            ["docs/PLAY_RELEASE_SIGNOFF.md"],
            [],
            [".codex-studio/published/MOBILE_PWA_RUNTIME_SMOKE.generated.json"],
        )

        self.assertEqual(3, summary["total_entry_count"])
        self.assertEqual(2, summary["owned_entry_count"])
        self.assertEqual(1, summary["owned_source_test_entry_count"])
        self.assertEqual(1, summary["owned_release_receipt_entry_count"])
        self.assertEqual(1, summary["foreign_entry_count"])
        self.assertEqual(
            [".codex-studio/published/MOBILE_PWA_RUNTIME_SMOKE.generated.json"],
            [entry["path"] for entry in summary["owned_release_receipt_entries"]],
        )
        self.assertEqual(
            [".codex-design/product/HORIZON_REGISTRY.yaml"],
            [entry["path"] for entry in summary["foreign_entry_examples"]],
        )
        self.assertEqual(0, summary["disposable_entry_count"])
        self.assertEqual(0, summary["external_blocker_entry_count"])

    def test_worktree_summary_excludes_disposable_local_state_from_reviewable_foreign_count(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        summary = module.worktree_summary(
            [
                {"status": "??", "path": ".state/browser-state/session.json", "raw": "?? .state/browser-state/session.json"},
                {"status": "??", "path": "NEXT_SESSION_HANDOFF.md", "raw": "?? NEXT_SESSION_HANDOFF.md"},
                {"status": " M", "path": ".codex-design/product/HORIZON_REGISTRY.yaml", "raw": " M .codex-design/product/HORIZON_REGISTRY.yaml"},
            ],
            ["NEXT_SESSION_HANDOFF.md"],
            [],
            [],
            [".state/"],
        )

        self.assertEqual(1, summary["owned_entry_count"])
        self.assertEqual(1, summary["disposable_entry_count"])
        self.assertEqual(1, summary["foreign_entry_count"])
        self.assertEqual(
            [".state/browser-state/session.json"],
            [entry["path"] for entry in summary["disposable_entry_examples"]],
        )
        self.assertEqual(
            [".codex-design/product/HORIZON_REGISTRY.yaml"],
            [entry["path"] for entry in summary["foreign_entry_examples"]],
        )

    def test_worktree_summary_splits_external_blocker_entries_from_reviewable_foreign(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        summary = module.worktree_summary(
            [
                {"status": "??", "path": ".state/browser-state/session.json", "raw": "?? .state/browser-state/session.json"},
                {"status": " M", "path": ".codex-design/product/WEEKLY_PRODUCT_PULSE.generated.json", "raw": " M .codex-design/product/WEEKLY_PRODUCT_PULSE.generated.json"},
                {"status": " M", "path": "README.md", "raw": " M README.md"},
            ],
            [],
            [],
            [],
            [".state/"],
            [".codex-design/"],
        )

        self.assertEqual(1, summary["disposable_entry_count"])
        self.assertEqual(1, summary["external_blocker_entry_count"])
        self.assertEqual(1, summary["foreign_entry_count"])
        self.assertEqual(
            [".codex-design/product/WEEKLY_PRODUCT_PULSE.generated.json"],
            [entry["path"] for entry in summary["external_blocker_entry_examples"]],
        )
        self.assertEqual(
            ["README.md"],
            [entry["path"] for entry in summary["foreign_entry_examples"]],
        )

    def test_worktree_summary_can_treat_all_remaining_foreign_entries_as_ambient(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        summary = module.worktree_summary(
            [
                {"status": " M", "path": "scripts/verify_public_edge_postdeploy_gate.py", "raw": " M scripts/verify_public_edge_postdeploy_gate.py"},
                {"status": " M", "path": "Chummer.Run.Api/Controllers/AccountsController.cs", "raw": " M Chummer.Run.Api/Controllers/AccountsController.cs"},
                {"status": "??", "path": ".codex-studio/published/CHUMMER_PUBLIC_ROUTE_PROOF.generated.json", "raw": "?? .codex-studio/published/CHUMMER_PUBLIC_ROUTE_PROOF.generated.json"},
            ],
            ["scripts/verify_public_edge_postdeploy_gate.py"],
            [],
            [],
            ambient_match_all_foreign=True,
        )

        self.assertEqual(1, summary["owned_entry_count"])
        self.assertEqual(0, summary["foreign_entry_count"])
        self.assertEqual(2, summary["ambient_entry_count"])
        self.assertEqual(
            {
                "Chummer.Run.Api/Controllers/AccountsController.cs",
                ".codex-studio/published/CHUMMER_PUBLIC_ROUTE_PROOF.generated.json",
            },
            {entry["path"] for entry in summary["ambient_entry_examples"]},
        )

    def test_worktree_summary_can_treat_selected_play_tooling_entries_as_ambient(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        summary = module.worktree_summary(
            [
                {"status": " M", "path": "AGENTS.md", "raw": " M AGENTS.md"},
                {"status": "??", "path": ".vexp/index.db", "raw": "?? .vexp/index.db"},
                {"status": "??", "path": ".vexp/manifest.json", "raw": "?? .vexp/manifest.json"},
                {"status": " M", "path": "README.md", "raw": " M README.md"},
            ],
            [],
            [],
            [],
            ambient_path_prefixes=module.AMBIENT_PLAY_WORKTREE_PREFIXES,
        )

        self.assertEqual(0, summary["owned_entry_count"])
        self.assertEqual(1, summary["foreign_entry_count"])
        self.assertEqual(3, summary["ambient_entry_count"])
        self.assertEqual(
            {
                "AGENTS.md",
                ".vexp/index.db",
                ".vexp/manifest.json",
            },
            {entry["path"] for entry in summary["ambient_entry_examples"]},
        )
        self.assertEqual(
            ["README.md"],
            [entry["path"] for entry in summary["foreign_entry_examples"]],
        )

    def test_collect_disposable_artifacts_splits_owned_and_shared_temp_state(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        with tempfile.TemporaryDirectory(prefix="mobile-boundary-owned-artifacts-") as repo_dir:
            repo_root = Path(repo_dir)
            owned_state = repo_root / ".state" / "browser-state" / "session.json"
            owned_state.parent.mkdir(parents=True, exist_ok=True)
            owned_state.write_text("{}", encoding="utf-8")
            owned_viewport = repo_root / "_tmp" / "mobile-viewport-smoke-player-360x740.png"
            owned_viewport.parent.mkdir(parents=True, exist_ok=True)
            owned_viewport.write_bytes(b"png")

            with tempfile.TemporaryDirectory(prefix="chummer-frontdoor-shared-artifacts-") as shared_dir:
                shared_root = Path(shared_dir)
                shared_temp = shared_root / "chummer-frontdoor-proof.json"
                shared_temp.write_text("{}", encoding="utf-8")

                owned = module.collect_disposable_artifacts(
                    [
                        str(repo_root / ".state" / "**" / "*"),
                        str(repo_root / "_tmp" / "mobile-viewport-smoke-*.png"),
                    ],
                    repo_root=repo_root,
                )
                shared = module.collect_disposable_artifacts(
                    [str(shared_root / "chummer-frontdoor-*")],
                    repo_root=repo_root,
                )
                merged = module.merge_disposable_artifacts(owned, shared)

        self.assertEqual(
            {
                str((repo_root / ".state" / "browser-state").resolve()),
                str(owned_state.resolve()),
                str(owned_viewport.resolve()),
            },
            {row["path"] for row in owned},
        )
        self.assertEqual(
            {str(shared_temp.resolve())},
            {row["path"] for row in shared},
        )
        self.assertEqual(4, len(merged))

    def test_receipt_inventory_can_treat_boundary_output_as_pending_owned_receipt(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)
        relative_path = ".codex-studio/published/MOBILE_RELEASE_BOUNDARY.pending.generated.json"

        inventory, findings = module.receipt_inventory(
            REPO_ROOT,
            [relative_path],
            [],
            pending_rows={
                relative_path: {
                    "path": relative_path,
                    "exists": True,
                    "worktree_status": "??",
                    "status": "pass",
                    "contract_name": "chummer6-mobile.release_boundary.v1",
                    "generated_at_utc": "2026-07-06T00:00:00Z",
                    "pending_materialization": True,
                }
            },
        )

        self.assertEqual([], findings)
        self.assertEqual(1, len(inventory))
        self.assertEqual(
            {
                "path": relative_path,
                "exists": True,
                "worktree_status": "??",
                "status": "pass",
                "contract_name": "chummer6-mobile.release_boundary.v1",
                "generated_at_utc": "2026-07-06T00:00:00Z",
                "pending_materialization": True,
            },
            inventory[0],
        )

    def test_owned_release_receipts_include_boundary_receipt(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        self.assertIn(
            ".codex-studio/published/MOBILE_RELEASE_BOUNDARY.generated.json",
            module.OWNED_RELEASE_RECEIPTS,
        )
        self.assertIn(
            ".codex-studio/published/MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json",
            module.OWNED_RELEASE_RECEIPTS,
        )

    def test_owned_play_source_files_include_repo_local_handoff(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        self.assertIn("NEXT_SESSION_HANDOFF.md", module.OWNED_PLAY_SOURCE_FILES)

    def test_summarize_preflight_receipt_surfaces_blocking_findings(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        summary = module.summarize_preflight_receipt(
            {
                "status": "fail",
                "activeLockCount": 3,
                "foreignLockCount": 2,
                "ignoredForeignLockCount": 0,
                "staleLookingLockCount": 1,
                "staleForeignLockCount": 1,
                "overlayRoot": "/docker/chummercomplete/chummer.run-services/.state/public-edge-portal-overlay/app",
                "findings": [
                    {
                        "id": "public_edge_overlay_source_fingerprint_mismatch",
                        "scope": "overlay",
                        "detail": "overlay build info source fingerprint does not match current source",
                    }
                ],
            }
        )

        self.assertEqual("fail", summary["status"])
        self.assertEqual(3, summary["active_lock_count"])
        self.assertEqual(1, summary["finding_count"])
        self.assertEqual(
            [
                {
                    "id": "public_edge_overlay_source_fingerprint_mismatch",
                    "scope": "overlay",
                    "detail": "overlay build info source fingerprint does not match current source",
                }
            ],
            summary["blocking_findings"],
        )

    def test_summarize_postdeploy_receipt_surfaces_failures_and_online_launch(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        summary = module.summarize_postdeploy_receipt(
            {
                "status": "fail",
                "preflightStatus": "fail",
                "strictPreflight": True,
                "strictInvocation": True,
                "strictNoAllowanceInvocation": True,
                "skipReleaseVersionMatch": False,
                "onlineLaunchStatus": "pass",
                "onlineLaunchFinalUrl": "https://chummer.run/app?command=character_roster",
                "expectedReleaseVersion": "run-20260704-170602",
                "expectedReleaseStatus": "published",
                "expectedReleaseChannel": "public_stable",
                "expectedReleaseSupportabilityState": "review_required",
                "expectedReleaseRolloutState": "coverage_incomplete",
                "downloadsVersionMarkerMatchesReleaseChannel": False,
                "statusRedirectVersionMarkerMatchesReleaseChannel": False,
                "visibleVersionMatchesReleaseChannel": False,
                "statusRedirectVersionMatchesReleaseChannel": False,
                "failures": [
                    "public-edge deploy preflight is not pass",
                    "downloads version marker proof is not pass",
                ],
            }
        )

        self.assertEqual("fail", summary["status"])
        self.assertEqual("fail", summary["preflight_status"])
        self.assertTrue(summary["strict_preflight"])
        self.assertTrue(summary["strict_invocation"])
        self.assertTrue(summary["strict_no_allowance_invocation"])
        self.assertFalse(summary["skip_release_version_match"])
        self.assertEqual(2, summary["failure_count"])
        self.assertEqual(
            [
                "public-edge deploy preflight is not pass",
                "downloads version marker proof is not pass",
            ],
            summary["failures"],
        )
        self.assertEqual("pass", summary["online_launch_status"])
        self.assertEqual(
            "https://chummer.run/app?command=character_roster",
            summary["online_launch_final_url"],
        )
        self.assertEqual("run-20260704-170602", summary["expected_release_version"])
        self.assertEqual("published", summary["expected_release_status"])
        self.assertEqual("public_stable", summary["expected_release_channel"])
        self.assertFalse(summary["downloads_version_marker_matches_release_channel"])
        self.assertFalse(summary["status_redirect_version_marker_matches_release_channel"])
        self.assertFalse(summary["visible_version_matches_release_channel"])
        self.assertFalse(summary["status_redirect_version_matches_release_channel"])

    def test_summarize_design_mirror_gate_surfaces_blockers_and_repairs(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        summary = module.summarize_design_mirror_gate(
            1,
            "",
            "design mirror drift detected for chummer6-mobile:\n"
            "  stale .codex-design/product/WEEKLY_PRODUCT_PULSE.generated.json\n"
            "repair with: bash scripts/ai/repair_design_mirror.sh\n"
            "repair with: python3 /docker/chummercomplete/chummer-design/scripts/ai/publish_local_mirrors.py\n",
        )

        self.assertEqual("fail", summary["status"])
        self.assertEqual(
            ["stale .codex-design/product/WEEKLY_PRODUCT_PULSE.generated.json"],
            summary["blocking_findings"],
        )
        self.assertEqual(
            [
                "bash scripts/ai/repair_design_mirror.sh",
                "python3 /docker/chummercomplete/chummer-design/scripts/ai/publish_local_mirrors.py",
            ],
            summary["repair_commands"],
        )

    def test_summarize_live_build_lock_probe_surfaces_matching_processes(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        summary = module.summarize_live_build_lock_probe(
            " 202947 bash scripts/build-chummer6-linux.sh\n"
            " 443627 bash /docker/chummercomplete/Chummer6/scripts/build-chummer6-linux.sh\n"
            " 551000 bash unrelated.sh\n"
        )

        self.assertEqual("present", summary["status"])
        self.assertEqual(2, summary["process_count"])
        self.assertEqual(
            [
                {"pid": 202947, "command": "bash scripts/build-chummer6-linux.sh"},
                {"pid": 443627, "command": "bash /docker/chummercomplete/Chummer6/scripts/build-chummer6-linux.sh"},
            ],
            summary["entries"],
        )

    def test_build_external_follow_through_tracks_design_and_public_edge_next_steps(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        summary = module.build_external_follow_through(
            {
                "status": "fail",
                "blocking_findings": ["stale .codex-design/product/WEEKLY_PRODUCT_PULSE.generated.json"],
                "repair_commands": ["bash scripts/ai/repair_design_mirror.sh"],
            },
            {
                "status": "present",
                "process_count": 2,
                "entries": [
                    {"pid": 202947, "command": "bash scripts/build-chummer6-linux.sh --base /work/base"},
                ],
            },
            {
                "status": "not_found",
                "blocking_findings": [],
            },
        )

        self.assertEqual("blocked", summary["design_mirror"]["status"])
        self.assertEqual(
            ["bash scripts/ai/repair_design_mirror.sh"],
            summary["design_mirror"]["repair_commands"],
        )
        self.assertEqual("waiting_for_foreign_build_locks", summary["strict_public_edge"]["status"])
        self.assertEqual(
            "python3 scripts/run_mobile_strict_public_edge_follow_through.py --wait-for-clear --execute-rerun --timeout-seconds 21600 --poll-interval-seconds 60",
            summary["strict_public_edge"]["follow_through_command"],
        )
        self.assertEqual(
            ".codex-studio/published/MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json",
            summary["strict_public_edge"]["follow_through_receipt_path"],
        )
        self.assertIn(
            "python3 /docker/chummercomplete/chummer.run-services/scripts/check_public_edge_deploy_preflight.py --output /tmp/chummer-public-edge-deploy-preflight-current.json",
            summary["strict_public_edge"]["rerun_commands"],
        )
        self.assertIn(
            "python3 /docker/chummercomplete/chummer.run-services/scripts/verify_public_edge_postdeploy_gate.py --base-url https://chummer.run --strict-preflight --output /tmp/chummer-public-edge-postdeploy-canonical-current.json",
            summary["strict_public_edge"]["rerun_commands"],
        )
        self.assertIn(
            "python3 scripts/materialize_mobile_local_release_proof.py",
            summary["strict_public_edge"]["rerun_commands"],
        )

    def test_build_external_follow_through_is_ready_when_preflight_passed_with_only_stale_locks(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        summary = module.build_external_follow_through(
            {
                "status": "pass",
                "blocking_findings": [],
                "repair_commands": [],
            },
            {
                "status": "present",
                "process_count": 4,
                "entries": [
                    {"pid": 202947, "command": "bash scripts/build-chummer6-linux.sh --base /work/base"},
                ],
            },
            {
                "status": "pass",
                "blocking_findings": [],
                "ignored_foreign_lock_count": 4,
                "stale_foreign_lock_count": 4,
            },
        )

        self.assertEqual("ready_to_rerun", summary["strict_public_edge"]["status"])
        self.assertEqual("present", summary["strict_public_edge"]["live_build_lock_probe_status"])
        self.assertEqual("pass", summary["strict_public_edge"]["preflight_snapshot_status"])
        self.assertEqual([], summary["strict_public_edge"]["preflight_blocking_findings"])
        self.assertIsNone(summary["strict_public_edge"]["wait_reason"])

    def test_build_external_follow_through_surfaces_preflight_blocker_detail(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        summary = module.build_external_follow_through(
            {
                "status": "pass",
                "blocking_findings": [],
                "repair_commands": [],
            },
            {
                "status": "present",
                "process_count": 4,
                "entries": [
                    {"pid": 202947, "command": "bash scripts/build-chummer6-linux.sh --base /work/base"},
                ],
            },
            {
                "status": "fail",
                "blocking_findings": [
                    {
                        "id": "active_build_lane",
                        "scope": "foreign",
                        "detail": "csc pid 189017 matches /Roslyn/bincore/csc",
                    }
                ],
            },
        )

        self.assertEqual("waiting_for_foreign_build_locks", summary["strict_public_edge"]["status"])
        self.assertEqual(
            ["csc pid 189017 matches /Roslyn/bincore/csc"],
            summary["strict_public_edge"]["preflight_blocking_findings"],
        )
        self.assertEqual(
            "strict public-edge preflight blocked: csc pid 189017 matches /Roslyn/bincore/csc",
            summary["strict_public_edge"]["wait_reason"],
        )

    def test_load_canonical_release_blockers_reads_root_blocker_snapshot(self) -> None:
        module = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)

        with tempfile.TemporaryDirectory(prefix="mobile-release-blockers-") as temp_dir:
            receipt_path = Path(temp_dir) / "RELEASE_BLOCKERS.generated.json"
            receipt_path.write_text(
                json.dumps(
                    {
                        "generated_at": "2026-07-06T19:37:33Z",
                        "root_blocker_ids": [
                            "release_posture:non_flagship_channel",
                            "release_truth:windows_installer_visual_audit",
                        ],
                        "root_blockers": [
                            {
                                "id": "release_posture:non_flagship_channel",
                                "blocker_id": "release_posture:non_flagship_channel",
                                "owning_repo": "chummer6-hub-registry",
                                "failing_gate": "published release channel is not in flagship public-stable posture",
                                "stable_promotion_command": "publish stable",
                                "post_promotion_verify_command": "verify stable",
                            },
                            {
                                "id": "release_truth:windows_installer_visual_audit",
                                "blocker_id": "release_truth:windows_installer_visual_audit",
                                "owning_repo": "chummer6-ui",
                                "failing_gate": "windows_installer_visual_audit is not passing for public-stable release truth",
                                "expected_bundle_path": "/tmp/windows-installer-gold-proof.zip",
                                "expected_bundle_path_exists": False,
                            },
                        ],
                    },
                    indent=2,
                )
                + "\n",
                encoding="utf-8",
            )

            summary = module.load_canonical_release_blockers(receipt_path)

        self.assertEqual("present", summary["status"])
        self.assertEqual("2026-07-06T19:37:33Z", summary["generated_at"])
        self.assertEqual(2, summary["root_blocker_count"])
        self.assertEqual(
            [
                "release_posture:non_flagship_channel",
                "release_truth:windows_installer_visual_audit",
            ],
            summary["root_blocker_ids"],
        )
        self.assertEqual(
            "/tmp/windows-installer-gold-proof.zip",
            summary["root_blockers"][1]["expected_bundle_path"],
        )

    def test_local_release_boundary_summary_preserves_design_mirror_snapshot(self) -> None:
        boundary = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)
        proof = load_module("materialize_mobile_local_release_proof", PROOF_SCRIPT_PATH)

        with tempfile.TemporaryDirectory(dir=REPO_ROOT, prefix="boundary-proof-test-") as temp_dir:
            receipt_path = Path(temp_dir) / "MOBILE_RELEASE_BOUNDARY.generated.json"
            receipt_path.write_text(
                json.dumps(
                    {
                        "contract_name": "chummer6-mobile.release_boundary.v1",
                        "status": "pass",
                        "generated_at_utc": "2026-07-06T00:00:00Z",
                        "ownership_checks": {
                            "owned_play_source_files_present": True,
                            "owned_play_test_files_present": True,
                            "owned_run_services_source_files_present": True,
                            "owned_run_services_test_files_present": True,
                            "owned_release_receipts_present": True,
                            "release_receipts_machine_local_noise_free": True,
                        },
                        "owned_boundary": {
                            "release_receipts": [
                                {"path": path, "exists": True}
                                for path in boundary.OWNED_RELEASE_RECEIPTS
                            ],
                        },
                        "worktree": {
                            "play": {
                                "owned_entry_count": 1,
                                "foreign_entry_count": 0,
                                "disposable_entry_count": 6,
                                "external_blocker_entry_count": 3,
                                "ambient_entry_count": 0,
                            },
                            "run_services": {"owned_entry_count": 3, "foreign_entry_count": 0, "ambient_entry_count": 4},
                        },
                        "owned_disposable_local_artifact_count": 1,
                        "shared_external_temp_artifact_count": 1,
                        "disposable_local_artifact_count": 2,
                        "owned_disposable_local_artifacts": [
                            {"path": "/docker/chummercomplete/chummer-play/_tmp/mobile-viewport-smoke-player-360x740.png"}
                        ],
                        "shared_external_temp_artifacts": [
                            {"path": "/tmp/chummer-frontdoor-navigation-proof.json"}
                        ],
                        "disposable_local_artifacts": [
                            {"path": "/docker/chummercomplete/chummer-play/_tmp/mobile-viewport-smoke-player-360x740.png"},
                            {"path": "/tmp/chummer-frontdoor-navigation-proof.json"},
                        ],
                        "preflight_snapshot": {"path": "/tmp/preflight.json", "status": "fail", "blocking_findings": ["lock"]},
                        "postdeploy_snapshot": {"path": "/tmp/postdeploy.json", "status": "pass", "failures": []},
                        "design_mirror_snapshot": {
                            "status": "fail",
                            "script_path": "scripts/ai/verify_design_mirror.py",
                            "command": "python3 scripts/ai/verify_design_mirror.py",
                            "blocking_findings": ["stale .codex-design/product/WEEKLY_PRODUCT_PULSE.generated.json"],
                            "repair_commands": ["bash scripts/ai/repair_design_mirror.sh"],
                            "stdout": "",
                        },
                        "live_build_lock_probe": {
                            "status": "present",
                            "command": "ps -eo pid=,args=",
                            "process_count": 2,
                            "entries": [
                                {"pid": 202947, "command": "bash scripts/build-chummer6-linux.sh"},
                                {"pid": 2201485, "command": "bash scripts/build-chummer6-linux.sh"},
                            ],
                        },
                        "canonical_release_blockers": {
                            "path": "/docker/chummercomplete/RELEASE_BLOCKERS.generated.json",
                            "status": "present",
                            "generated_at": "2026-07-06T19:37:33Z",
                            "root_blocker_ids": [
                                "release_posture:non_flagship_channel",
                                "release_truth:windows_installer_visual_audit",
                            ],
                            "root_blocker_count": 2,
                            "root_blockers": [
                                {
                                    "id": "release_posture:non_flagship_channel",
                                    "blocker_id": "release_posture:non_flagship_channel",
                                    "owning_repo": "chummer6-hub-registry",
                                    "failing_gate": "published release channel is not in flagship public-stable posture",
                                    "stable_promotion_command": "publish stable",
                                    "post_promotion_verify_command": "verify stable",
                                    "expected_bundle_path": None,
                                    "expected_bundle_path_exists": None,
                                },
                                {
                                    "id": "release_truth:windows_installer_visual_audit",
                                    "blocker_id": "release_truth:windows_installer_visual_audit",
                                    "owning_repo": "chummer6-ui",
                                    "failing_gate": "windows_installer_visual_audit is not passing for public-stable release truth",
                                    "stable_promotion_command": None,
                                    "post_promotion_verify_command": None,
                                    "expected_bundle_path": "/tmp/windows-installer-gold-proof.zip",
                                    "expected_bundle_path_exists": False,
                                },
                            ],
                        },
                        "external_follow_through": {
                            "design_mirror": {
                                "status": "blocked",
                                "blocking_findings": ["stale .codex-design/product/WEEKLY_PRODUCT_PULSE.generated.json"],
                                "repair_commands": ["bash scripts/ai/repair_design_mirror.sh"],
                            },
                            "strict_public_edge": {
                                "status": "waiting_for_foreign_build_locks",
                                "live_build_lock_probe_status": "present",
                                "wait_reason": "foreign build-chummer6-linux lanes still present",
                                "follow_through_command": "python3 scripts/run_mobile_strict_public_edge_follow_through.py --wait-for-clear --execute-rerun --timeout-seconds 21600 --poll-interval-seconds 60",
                                "follow_through_receipt_path": ".codex-studio/published/MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json",
                                "rerun_commands": [
                                    "python3 /docker/chummercomplete/chummer.run-services/scripts/check_public_edge_deploy_preflight.py --output /tmp/chummer-public-edge-deploy-preflight-current.json",
                                    "python3 /docker/chummercomplete/chummer.run-services/scripts/verify_public_edge_postdeploy_gate.py --base-url https://chummer.run --strict-preflight --output /tmp/chummer-public-edge-postdeploy-canonical-current.json",
                                    "python3 scripts/materialize_mobile_cross_surface_readiness.py",
                                    "python3 scripts/materialize_mobile_local_release_proof.py",
                                    "bash scripts/release/verify_mobile_release_proof.sh",
                                ],
                            },
                        },
                    }
                ),
                encoding="utf-8",
            )

            with mock.patch.object(proof, "MOBILE_RELEASE_BOUNDARY_RECEIPT", receipt_path):
                summary, errors = proof.load_release_boundary()

        self.assertEqual([], errors)
        self.assertEqual(
            {
                "status": "fail",
                "script_path": "scripts/ai/verify_design_mirror.py",
                "command": "python3 scripts/ai/verify_design_mirror.py",
                "blocking_findings": ["stale .codex-design/product/WEEKLY_PRODUCT_PULSE.generated.json"],
                "repair_commands": ["bash scripts/ai/repair_design_mirror.sh"],
                "stdout": "",
            },
            summary["design_mirror_snapshot"],
        )
        self.assertEqual(0, summary["play_foreign_entry_count"])
        self.assertEqual(6, summary["play_disposable_entry_count"])
        self.assertEqual(3, summary["play_external_blocker_entry_count"])
        self.assertEqual(0, summary["play_ambient_entry_count"])
        self.assertEqual(1, summary["owned_disposable_local_artifact_count"])
        self.assertEqual(1, summary["shared_external_temp_artifact_count"])
        self.assertEqual(2, summary["disposable_local_artifact_count"])
        self.assertEqual(4, summary["run_services_ambient_entry_count"])
        self.assertEqual(
            [
                "release_posture:non_flagship_channel",
                "release_truth:windows_installer_visual_audit",
            ],
            summary["canonical_release_blockers"]["root_blocker_ids"],
        )
        self.assertEqual(
            {
                "status": "present",
                "command": "ps -eo pid=,args=",
                "process_count": 2,
                "entries": [
                    {"pid": 202947, "command": "bash scripts/build-chummer6-linux.sh"},
                    {"pid": 2201485, "command": "bash scripts/build-chummer6-linux.sh"},
                ],
            },
            summary["live_build_lock_probe"],
        )
        self.assertEqual(
            "waiting_for_foreign_build_locks",
            summary["external_follow_through"]["strict_public_edge"]["status"],
        )

    def test_owned_play_source_files_cover_local_release_proof_surface(self) -> None:
        boundary = load_module("materialize_mobile_release_boundary", BOUNDARY_SCRIPT_PATH)
        proof = load_module("materialize_mobile_local_release_proof", PROOF_SCRIPT_PATH)

        owned_boundary = set(boundary.OWNED_PLAY_SOURCE_FILES)
        proof_sources = {
            str(proof.REGRESSION_SOURCE.relative_to(proof.ROOT)),
            str(proof.PLAY_WEB_APPLICATION.relative_to(proof.ROOT)),
            str(proof.PLAY_ROUTE_HANDLERS.relative_to(proof.ROOT)),
            str(proof.TURN_COMPANION_SERVICE.relative_to(proof.ROOT)),
            str(proof.TURN_COMPANION_PROJECTOR.relative_to(proof.ROOT)),
            str(proof.TURN_COMPANION_IMPORTS.relative_to(proof.ROOT)),
            str(proof.TURN_COMPANION_PAGE.relative_to(proof.ROOT)),
            str(proof.TURN_COMPANION_RUNTIME.relative_to(proof.ROOT)),
            str(proof.MOBILE_CSS.relative_to(proof.ROOT)),
            str(proof.WEB_SOURCE.relative_to(proof.ROOT)),
            str(proof.APP_SHELL.relative_to(proof.ROOT)),
            str(proof.PLAY_WEB_DOCKERFILE.relative_to(proof.ROOT)),
            str(proof.SERVICE_WORKER.relative_to(proof.ROOT)),
            str(proof.GENERIC_MANIFEST.relative_to(proof.ROOT)),
            str(proof.PLAYER_MANIFEST.relative_to(proof.ROOT)),
            str(proof.GM_MANIFEST.relative_to(proof.ROOT)),
            str(proof.MIGRATION_MAP.relative_to(proof.ROOT)),
            str(proof.PLAY_SIGNOFF.relative_to(proof.ROOT)),
            str(proof.VERIFY_SCRIPT.relative_to(proof.ROOT)),
            str(proof.PACKAGE_PLANE_HELPER.relative_to(proof.ROOT)),
            str(proof.MOBILE_RELEASE_PROOF_VERIFIER.relative_to(proof.ROOT)),
            str(proof.RUNTIME_SMOKE.relative_to(proof.ROOT)),
            str(proof.VIEWPORT_SMOKE.relative_to(proof.ROOT)),
            str(proof.ANALYTICS_SMOKE.relative_to(proof.ROOT)),
            str(proof.M112_PROOF_DOC.relative_to(proof.ROOT)),
            str(proof.M112_VERIFIER.relative_to(proof.ROOT)),
            str(proof.M119_PROOF_DOC.relative_to(proof.ROOT)),
            str(proof.M119_VERIFIER.relative_to(proof.ROOT)),
            str(proof.M117_PROOF_DOC.relative_to(proof.ROOT)),
            str(proof.M117_VERIFIER.relative_to(proof.ROOT)),
            str(proof.M121_PROOF_DOC.relative_to(proof.ROOT)),
            str(proof.M121_VERIFIER.relative_to(proof.ROOT)),
            str(proof.M122_PROOF_DOC.relative_to(proof.ROOT)),
            str(proof.M122_VERIFIER.relative_to(proof.ROOT)),
            str(proof.M145_PROOF_DOC.relative_to(proof.ROOT)),
            str(proof.M145_VERIFIER.relative_to(proof.ROOT)),
            str(proof.MOBILE_CROSS_SURFACE_REFRESH_SCRIPT.relative_to(proof.ROOT)),
            str(proof.MOBILE_RELEASE_BOUNDARY_SCRIPT.relative_to(proof.ROOT)),
        }

        self.assertEqual(set(), proof_sources - owned_boundary)
        self.assertTrue(
            {
                ".gitignore",
                "src/Chummer.Play.Web/Components/_Imports.razor",
                "src/Chummer.Play.Web/Dockerfile",
            }.issubset(owned_boundary)
        )


if __name__ == "__main__":
    unittest.main()
