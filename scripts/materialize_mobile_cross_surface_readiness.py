#!/usr/bin/env python3
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import re
import shutil
import subprocess
import tempfile
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / ".codex-studio" / "published" / "MOBILE_CROSS_SURFACE_READINESS.generated.json"
FLEET_MATERIALIZER = Path("/docker/fleet/scripts/materialize_flagship_product_readiness.py")
PUBLIC_EDGE_GATE = Path("/docker/chummercomplete/chummer.run-services/scripts/verify_public_edge_postdeploy_gate.py")
STRICT_PUBLIC_EDGE_PREFLIGHT_RECEIPT = Path("/tmp/chummer-public-edge-deploy-preflight-current.json")
STRICT_PUBLIC_EDGE_POSTDEPLOY_RECEIPT = Path("/tmp/chummer-public-edge-postdeploy-canonical-current.json")
LIVE_BUILD_LOCK_MARKER = "build-chummer6-linux.sh"
WHITESPACE_RE = re.compile(r"\s+")
SURFACE_SOURCE_FILES = [
    "src/Chummer.Play.Web/wwwroot/index.html",
    "src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js",
    "src/Chummer.Play.Web/wwwroot/service-worker.js",
    "src/Chummer.Play.Web/wwwroot/manifest.webmanifest",
    "src/Chummer.Play.Web/wwwroot/manifest.player.webmanifest",
    "src/Chummer.Play.Web/wwwroot/manifest.gm.webmanifest",
    "src/Chummer.Play.Web/Components/App.razor",
    "src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor",
    "src/Chummer.Play.Web/PlayWebApplication.cs",
    "src/Chummer.Play.Web/PlayRouteHandlers.cs",
    "src/Chummer.Play.Web/PlayTurnCompanionService.cs",
]


def iso_now() -> str:
    return dt.datetime.now(dt.timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def parse_timestamp(value: object) -> dt.datetime | None:
    if not isinstance(value, str):
        return None
    text = value.strip()
    if not text:
        return None
    try:
        parsed = dt.datetime.fromisoformat(text.replace("Z", "+00:00"))
    except ValueError:
        return None
    if parsed.tzinfo is None:
        return parsed.replace(tzinfo=dt.timezone.utc)
    return parsed.astimezone(dt.timezone.utc)


def sha256_file(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def build_surface_source_fingerprint() -> dict[str, object]:
    files: list[dict[str, str]] = []
    for relative_path in SURFACE_SOURCE_FILES:
        path = ROOT / relative_path
        if not path.is_file():
            raise SystemExit(f"missing cross-surface source file: {path}")
        files.append(
            {
                "path": relative_path,
                "sha256": sha256_file(path),
            }
        )
    return {
        "kind": "current_checkout_sha256",
        "file_count": len(files),
        "files": files,
    }


def load_json(path: Path, label: str) -> dict[str, object]:
    if not path.is_file():
        raise SystemExit(f"missing {label}: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise SystemExit(f"invalid {label} json at {path}: {exc}") from exc
    if not isinstance(payload, dict):
        raise SystemExit(f"{label} root must be an object: {path}")
    return payload


def load_optional_json(path: Path) -> dict[str, object]:
    if not path.is_file():
        return {}
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError:
        return {}
    return payload if isinstance(payload, dict) else {}


def run_recorded(command: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(command, check=False, text=True, capture_output=True)


def normalize_probe_command(command: str, *, limit: int = 240) -> str:
    normalized = WHITESPACE_RE.sub(" ", command).strip()
    if len(normalized) <= limit:
        return normalized
    return normalized[: limit - 3].rstrip() + "..."


def summarize_live_build_lock_probe(ps_output: str) -> dict[str, object]:
    entries: list[dict[str, object]] = []
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


def load_live_build_lock_probe() -> dict[str, object]:
    completed = subprocess.run(
        ["ps", "-eo", "pid=,args="],
        check=False,
        text=True,
        capture_output=True,
    )
    payload = summarize_live_build_lock_probe(completed.stdout)
    payload["command"] = "ps -eo pid=,args="
    payload["returncode"] = completed.returncode
    return payload


def live_build_lock_failure_messages(live_build_lock_probe: dict[str, object]) -> list[str]:
    entries = live_build_lock_probe.get("entries")
    if not isinstance(entries, list):
        return []
    messages: list[str] = []
    for entry in entries:
        if not isinstance(entry, dict) or not isinstance(entry.get("pid"), int):
            continue
        messages.append(f"live build lock probe pid {entry.get('pid')} matches build-chummer6-linux")
    return messages


def frontdoor_targets_keep_mobile_play_intent(payload: dict[str, object]) -> bool:
    public_targets = [
        str(item)
        for item in (payload.get("frontdoorNavigationPublicTargets") or [])
        if isinstance(item, str) and str(item).strip()
    ]
    gated_targets = {
        str(item)
        for item in (payload.get("frontdoorNavigationGatedTargets") or [])
        if isinstance(item, str) and str(item).strip()
    }

    return (
        public_targets == ["Play"] and gated_targets == {"Build"}
    ) or (
        public_targets == [] and gated_targets == {"Build", "Play"}
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base-url", default="https://chummer.run")
    parser.add_argument("--output", default=str(OUT))
    parser.add_argument("--skip-public-edge", action="store_true")
    args = parser.parse_args()

    output_path = Path(args.output).resolve()
    generated_at = iso_now()

    fleet_tmp_dir = Path(tempfile.mkdtemp(prefix="chummer-mobile-cross-surface-fleet-"))
    public_tmp_dir = Path(tempfile.mkdtemp(prefix="chummer-mobile-cross-surface-public-"))
    public_artifact_dir = public_tmp_dir / "frontdoor-artifacts"
    fleet_receipt_path = fleet_tmp_dir / "FLAGSHIP_PRODUCT_READINESS.audit.json"
    public_gate_receipt_path = public_tmp_dir / "PUBLIC_EDGE_POSTDEPLOY.audit.json"

    try:
        fleet_result = run_recorded(
            [
                "python3",
                str(FLEET_MATERIALIZER),
                "--out",
                str(fleet_receipt_path),
            ]
        )
        fleet_payload = load_optional_json(fleet_receipt_path)
        if not fleet_payload and fleet_result.returncode == 0:
            raise SystemExit(f"missing fleet readiness audit receipt: {fleet_receipt_path}")
        if not fleet_payload and fleet_result.returncode != 0:
            fleet_payload = {
                "status": "fail",
                "warning_keys": [],
                "missing_keys": ["mobile_play_shell"],
                "readiness_plane_gap_keys": [],
                "coverage": {},
                "coverage_details": {},
            }
        fleet_coverage = fleet_payload.get("coverage") if isinstance(fleet_payload.get("coverage"), dict) else {}
        fleet_details = fleet_payload.get("coverage_details") if isinstance(fleet_payload.get("coverage_details"), dict) else {}
        mobile_detail = fleet_details.get("mobile_play_shell") if isinstance(fleet_details.get("mobile_play_shell"), dict) else {}
        mobile_evidence = mobile_detail.get("evidence") if isinstance(mobile_detail.get("evidence"), dict) else {}

        public_gate_payload: dict[str, object] = {}
        frontdoor_payload: dict[str, object] = {}
        public_edge_failures: list[str] = []
        strict_public_edge_failures: list[str] = []
        live_build_lock_probe: dict[str, object] = {}
        public_edge_gate_pass = False
        strict_public_edge_gate_pass = False
        public_gate_result = subprocess.CompletedProcess(args=[], returncode=0, stdout="", stderr="")
        strict_preflight_payload: dict[str, object] = {}
        strict_postdeploy_payload: dict[str, object] = {}
        if not args.skip_public_edge:
            public_gate_result = run_recorded(
                [
                    "python3",
                    str(PUBLIC_EDGE_GATE),
                    "--base-url",
                    args.base_url,
                    "--skip-preflight",
                    "--skip-release-version-match",
                    "--require-frontdoor-navigation-playwright",
                    "--frontdoor-navigation-artifact-dir",
                    str(public_artifact_dir),
                    "--output",
                    str(public_gate_receipt_path),
                ]
            )
            public_gate_payload = load_optional_json(public_gate_receipt_path)
            frontdoor_receipt_path = public_artifact_dir / "FRONTDOOR_MOBILE_LAUNCH.generated.json"
            frontdoor_payload = load_optional_json(frontdoor_receipt_path)
            public_edge_failures = [
                str(item)
                for item in public_gate_payload.get("failures", [])
                if isinstance(item, str) and item.strip()
            ]
            public_edge_gate_pass = (
                public_gate_result.returncode == 0
                and public_gate_payload.get("status") == "pass"
            )
            live_build_lock_probe = load_live_build_lock_probe()
            live_build_lock_messages = live_build_lock_failure_messages(live_build_lock_probe)
            strict_preflight_payload = load_optional_json(STRICT_PUBLIC_EDGE_PREFLIGHT_RECEIPT)
            strict_postdeploy_payload = load_optional_json(STRICT_PUBLIC_EDGE_POSTDEPLOY_RECEIPT)

            strict_preflight_receipt_present = bool(strict_preflight_payload)
            strict_preflight_pass = strict_preflight_payload.get("status") == "pass"
            strict_preflight_no_allowances = (
                strict_preflight_payload.get("allowForeignBuildLocks") is False
                and strict_preflight_payload.get("allowStaleForeignBuildLocks") is False
            )
            strict_preflight_clears_live_build_locks = strict_preflight_receipt_present and strict_preflight_pass
            strict_postdeploy_receipt_present = bool(strict_postdeploy_payload)
            strict_preflight_generated_at = parse_timestamp(strict_preflight_payload.get("generatedAtUtc"))
            strict_postdeploy_generated_at = parse_timestamp(strict_postdeploy_payload.get("generatedAtUtc"))
            strict_postdeploy_stale = (
                strict_preflight_receipt_present
                and strict_postdeploy_receipt_present
                and strict_preflight_generated_at is not None
                and strict_postdeploy_generated_at is not None
                and strict_postdeploy_generated_at < strict_preflight_generated_at
            )
            strict_postdeploy_pass = strict_postdeploy_payload.get("status") == "pass"
            strict_postdeploy_preflight_pass = strict_postdeploy_payload.get("preflightStatus") == "pass"
            strict_postdeploy_no_preflight_allowances = (
                strict_postdeploy_payload.get("preflightAllowForeignBuildLocks") is False
                and strict_postdeploy_payload.get("preflightAllowStaleForeignBuildLocks") is False
            )
            strict_postdeploy_declares_invocation = (
                isinstance(strict_postdeploy_payload.get("skipPreflight"), bool)
                and isinstance(strict_postdeploy_payload.get("skipReleaseVersionMatch"), bool)
            )
            strict_postdeploy_invocation_strict = (
                strict_postdeploy_declares_invocation
                and strict_postdeploy_payload.get("skipPreflight") is False
                and strict_postdeploy_payload.get("skipReleaseVersionMatch") is False
            )
            strict_postdeploy_declares_no_allowance_invocation = (
                isinstance(strict_postdeploy_payload.get("strictPreflight"), bool)
                and isinstance(strict_postdeploy_payload.get("strictNoAllowanceInvocation"), bool)
            )
            strict_postdeploy_no_allowance_invocation = (
                strict_postdeploy_declares_no_allowance_invocation
                and strict_postdeploy_payload.get("strictPreflight") is True
                and strict_postdeploy_payload.get("strictNoAllowanceInvocation") is True
            )
            strict_public_edge_gate_pass = all(
                [
                    strict_preflight_receipt_present,
                    strict_preflight_pass,
                    strict_preflight_no_allowances,
                    strict_postdeploy_receipt_present,
                    not strict_postdeploy_stale,
                    strict_postdeploy_pass,
                    strict_postdeploy_preflight_pass,
                    strict_postdeploy_no_preflight_allowances,
                    strict_postdeploy_invocation_strict,
                    strict_postdeploy_no_allowance_invocation,
                ]
            )

            if not strict_preflight_receipt_present:
                strict_public_edge_failures.append("strict public-edge preflight receipt is missing")
            else:
                if not strict_preflight_pass:
                    strict_public_edge_failures.append("strict public-edge preflight receipt is not pass")
                if not strict_preflight_no_allowances:
                    strict_public_edge_failures.append("strict public-edge preflight receipt still allows foreign build locks")
                preflight_findings = strict_preflight_payload.get("findings")
                if isinstance(preflight_findings, list):
                    strict_public_edge_failures.extend(
                        str(finding.get("detail"))
                        for finding in preflight_findings
                        if (
                            isinstance(finding, dict)
                            and str(finding.get("detail") or "").strip()
                            and not (
                                live_build_lock_messages
                                and "build-chummer6-linux" in str(finding.get("detail") or "")
                            )
                        )
                    )

            if not strict_postdeploy_receipt_present:
                strict_public_edge_failures.append("strict public-edge postdeploy receipt is missing")
            else:
                if strict_postdeploy_stale:
                    strict_public_edge_failures.append(
                        "strict public-edge postdeploy receipt is older than the current strict preflight receipt"
                    )
                else:
                    if not strict_postdeploy_pass:
                        strict_public_edge_failures.append("strict public-edge postdeploy receipt is not pass")
                    if not strict_postdeploy_preflight_pass:
                        strict_public_edge_failures.append("strict public-edge postdeploy receipt does not prove preflight pass")
                    if not strict_postdeploy_no_preflight_allowances:
                        strict_public_edge_failures.append("strict public-edge postdeploy receipt still allows foreign build locks")
                    if not strict_postdeploy_declares_invocation:
                        strict_public_edge_failures.append("strict public-edge postdeploy receipt does not declare invocation strictness")
                    elif not strict_postdeploy_invocation_strict:
                        strict_public_edge_failures.append("strict public-edge postdeploy receipt was generated with skipped strict checks")
                    if not strict_postdeploy_declares_no_allowance_invocation:
                        strict_public_edge_failures.append("strict public-edge postdeploy receipt does not declare no-allowance strictness")
                    elif not strict_postdeploy_no_allowance_invocation:
                        strict_public_edge_failures.append("strict public-edge postdeploy receipt was not generated with strict preflight/no-allowance mode")
                    postdeploy_failures = strict_postdeploy_payload.get("failures")
                    if isinstance(postdeploy_failures, list):
                        strict_public_edge_failures.extend(
                            str(item)
                            for item in postdeploy_failures
                            if isinstance(item, str) and item.strip()
                        )

            if not strict_preflight_clears_live_build_locks:
                strict_public_edge_failures.extend(live_build_lock_messages)

            public_edge_failures = list(dict.fromkeys([*public_edge_failures, *strict_public_edge_failures]))

        checks = {
            "fleet_mobile_play_shell_ready": fleet_coverage.get("mobile_play_shell") == "ready",
            "fleet_mobile_local_release_passed": mobile_evidence.get("mobile_local_release_status") == "passed",
            "fleet_mobile_scope_not_blocking": "mobile_play_shell"
            not in {
                *(fleet_payload.get("warning_keys") or []),
                *(fleet_payload.get("missing_keys") or []),
                *(fleet_payload.get("readiness_plane_gap_keys") or []),
            },
        }

        if args.skip_public_edge:
            checks["public_edge_skipped"] = True
        else:
            live_public_edge_checks = {
                "public_edge_frontdoor_navigation_pass": public_gate_payload.get("frontdoorNavigationStatus") == "pass",
                "public_edge_frontdoor_route_is_player": public_gate_payload.get("frontdoorNavigationPlayRoute") == "/mobile/player",
                "public_edge_handoff_launch_route_is_player": public_gate_payload.get("readyMobileHandoffFrontdoorLaunchRoute") == "/mobile/player",
                "public_edge_role_alias_routes_pass": public_gate_payload.get("roleAliasRouteStatus") == "pass",
                "public_edge_pwa_static_pass": public_gate_payload.get("pwaStaticStatus") == "pass",
                "public_edge_mobile_ledger_pass": public_gate_payload.get("mobileLedgerStatus") == "pass",
                "public_edge_public_targets_keep_play_only": frontdoor_targets_keep_mobile_play_intent(public_gate_payload),
                "frontdoor_player_gm_blazor_shells_live": frontdoor_payload.get("blazor_shell") == "interactive-server"
                and frontdoor_payload.get("gm_blazor_shell") == "interactive-server"
                and frontdoor_payload.get("live_turn_companion_shell") is True
                and frontdoor_payload.get("gm_live_turn_companion_shell") is True,
                "frontdoor_player_gm_role_manifests_live": frontdoor_payload.get("pwa_manifest_path") == "/manifest.player.webmanifest"
                and frontdoor_payload.get("gm_pwa_manifest_path") == "/manifest.gm.webmanifest",
                "frontdoor_player_gm_handoff_links_preserve_role_and_strip_device": frontdoor_payload.get("player_session_handoff_preserves_session") is True
                and frontdoor_payload.get("player_session_handoff_preserves_role") is True
                and frontdoor_payload.get("player_session_handoff_strips_device") is True
                and frontdoor_payload.get("gm_session_handoff_preserves_session") is True
                and frontdoor_payload.get("gm_session_handoff_preserves_role") is True
                and frontdoor_payload.get("gm_session_handoff_strips_device") is True,
                "frontdoor_rybbit_roles_live": frontdoor_payload.get("rybbit_configured") is True
                and frontdoor_payload.get("gm_rybbit_configured") is True,
            }
            checks.update(live_public_edge_checks)
            checks["public_edge_gate_pass"] = public_edge_gate_pass
            checks["strict_public_edge_gate_pass"] = strict_public_edge_gate_pass

        required_check_keys = [
            "fleet_mobile_play_shell_ready",
            "fleet_mobile_local_release_passed",
            "fleet_mobile_scope_not_blocking",
        ]
        if args.skip_public_edge:
            required_check_keys.append("public_edge_skipped")
        else:
            required_check_keys.extend(
                [
                    "public_edge_frontdoor_navigation_pass",
                    "public_edge_frontdoor_route_is_player",
                    "public_edge_handoff_launch_route_is_player",
                    "public_edge_role_alias_routes_pass",
                    "public_edge_pwa_static_pass",
                    "public_edge_mobile_ledger_pass",
                    "public_edge_public_targets_keep_play_only",
                    "frontdoor_player_gm_blazor_shells_live",
                    "frontdoor_player_gm_role_manifests_live",
                    "frontdoor_player_gm_handoff_links_preserve_role_and_strip_device",
                    "frontdoor_rybbit_roles_live",
                    "public_edge_gate_pass",
                    "strict_public_edge_gate_pass",
                ]
            )

        status = "pass" if all(checks.get(key) is True for key in required_check_keys) else "fail"
        payload = {
            "contract_name": "chummer6-mobile.cross_surface_readiness_refresh.v1",
            "generated_at_utc": generated_at,
            "status": status,
            "base_url": args.base_url,
            "commands": {
                "fleet_readiness_audit": f"python3 {FLEET_MATERIALIZER} --out <tempfile>",
                "public_edge_gate": None
                if args.skip_public_edge
                else f"python3 {PUBLIC_EDGE_GATE} --base-url {args.base_url} --skip-preflight --skip-release-version-match --require-frontdoor-navigation-playwright --frontdoor-navigation-artifact-dir <tempdir> --output <tempfile>",
            },
            "surface_source_fingerprint": build_surface_source_fingerprint(),
            "checks": checks,
            "fleet_readiness": {
                "source_receipt_mode": "ephemeral_tempfile_materialization",
                "published_script_path": str(FLEET_MATERIALIZER),
                "generated_at": fleet_payload.get("generated_at"),
                "generated_at_utc": fleet_payload.get("generated_at_utc"),
                "status": fleet_payload.get("status"),
                "warning_keys": fleet_payload.get("warning_keys") if isinstance(fleet_payload.get("warning_keys"), list) else [],
                "missing_keys": fleet_payload.get("missing_keys") if isinstance(fleet_payload.get("missing_keys"), list) else [],
                "readiness_plane_gap_keys": fleet_payload.get("readiness_plane_gap_keys")
                if isinstance(fleet_payload.get("readiness_plane_gap_keys"), list)
                else [],
                "coverage": {
                    "mobile_play_shell": fleet_coverage.get("mobile_play_shell"),
                    "desktop_client": fleet_coverage.get("desktop_client"),
                },
                "mobile_play_shell": {
                    "status": mobile_detail.get("status"),
                    "summary": mobile_detail.get("summary"),
                    "mobile_local_release_status": mobile_evidence.get("mobile_local_release_status"),
                    "campaign_session_recover_recap_effective_state": mobile_evidence.get("campaign_session_recover_recap_effective_state"),
                    "recover_from_sync_conflict_owner_scoped_effective_state": mobile_evidence.get("recover_from_sync_conflict_owner_scoped_effective_state"),
                },
            },
            "public_edge": {
                "skipped": args.skip_public_edge,
                "receipt_mode": "ephemeral_tempfile_materialization" if not args.skip_public_edge else None,
                "status": None if args.skip_public_edge else ("pass" if public_edge_gate_pass and strict_public_edge_gate_pass else "fail"),
                "live_gate_status": None if args.skip_public_edge else public_gate_payload.get("status"),
                "gate_exit_code": None if args.skip_public_edge else public_gate_result.returncode,
                "gate_receipt_present": None if args.skip_public_edge else public_gate_receipt_path.is_file(),
                "frontdoor_receipt_present": None if args.skip_public_edge else (public_artifact_dir / "FRONTDOOR_MOBILE_LAUNCH.generated.json").is_file(),
                "strict_status": None if args.skip_public_edge else ("pass" if strict_public_edge_gate_pass else "fail"),
                "strict_preflight_receipt_path": None if args.skip_public_edge else str(STRICT_PUBLIC_EDGE_PREFLIGHT_RECEIPT),
                "strict_preflight_generated_at_utc": None if args.skip_public_edge else strict_preflight_payload.get("generatedAtUtc"),
                "strict_preflight_status": None if args.skip_public_edge else strict_preflight_payload.get("status"),
                "strict_preflight_allow_foreign_build_locks": None if args.skip_public_edge else strict_preflight_payload.get("allowForeignBuildLocks"),
                "strict_preflight_allow_stale_foreign_build_locks": None if args.skip_public_edge else strict_preflight_payload.get("allowStaleForeignBuildLocks"),
                "strict_postdeploy_receipt_path": None if args.skip_public_edge else str(STRICT_PUBLIC_EDGE_POSTDEPLOY_RECEIPT),
                "strict_postdeploy_generated_at_utc": None if args.skip_public_edge else strict_postdeploy_payload.get("generatedAtUtc"),
                "strict_postdeploy_status": None if args.skip_public_edge else strict_postdeploy_payload.get("status"),
                "strict_postdeploy_stale": None if args.skip_public_edge else strict_postdeploy_stale,
                "strict_postdeploy_preflight_status": None if args.skip_public_edge else strict_postdeploy_payload.get("preflightStatus"),
                "strict_postdeploy_allow_foreign_build_locks": None if args.skip_public_edge else strict_postdeploy_payload.get("preflightAllowForeignBuildLocks"),
                "strict_postdeploy_allow_stale_foreign_build_locks": None if args.skip_public_edge else strict_postdeploy_payload.get("preflightAllowStaleForeignBuildLocks"),
                "strict_postdeploy_skip_preflight": None if args.skip_public_edge else strict_postdeploy_payload.get("skipPreflight"),
                "strict_postdeploy_skip_release_version_match": None if args.skip_public_edge else strict_postdeploy_payload.get("skipReleaseVersionMatch"),
                "strict_postdeploy_strict_preflight": None if args.skip_public_edge else strict_postdeploy_payload.get("strictPreflight") is True,
                "strict_postdeploy_strict_invocation": None if args.skip_public_edge else strict_postdeploy_invocation_strict,
                "strict_postdeploy_strict_no_allowance_invocation": None if args.skip_public_edge else strict_postdeploy_no_allowance_invocation,
                "live_build_lock_probe": None if args.skip_public_edge else live_build_lock_probe,
                "failures": [] if args.skip_public_edge else public_edge_failures,
                "downloads_status": None if args.skip_public_edge else public_gate_payload.get("downloadsStatus"),
                "frontdoor_navigation_status": None if args.skip_public_edge else public_gate_payload.get("frontdoorNavigationStatus"),
                "frontdoor_route": None if args.skip_public_edge else public_gate_payload.get("frontdoorNavigationPlayRoute"),
                "handoff_launch_route": None if args.skip_public_edge else public_gate_payload.get("readyMobileHandoffFrontdoorLaunchRoute"),
                "public_targets": None if args.skip_public_edge else public_gate_payload.get("frontdoorNavigationPublicTargets"),
                "gated_targets": None if args.skip_public_edge else public_gate_payload.get("frontdoorNavigationGatedTargets"),
                "role_alias_route_status": None if args.skip_public_edge else public_gate_payload.get("roleAliasRouteStatus"),
                "pwa_static_status": None if args.skip_public_edge else public_gate_payload.get("pwaStaticStatus"),
                "mobile_ledger_status": None if args.skip_public_edge else public_gate_payload.get("mobileLedgerStatus"),
                "ready_mobile_handoff_status": None if args.skip_public_edge else public_gate_payload.get("readyMobileHandoffStatus"),
                "participate_iframe_shell_status": None if args.skip_public_edge else public_gate_payload.get("participateIframeShellStatus"),
                "blazor_shell": None if args.skip_public_edge else frontdoor_payload.get("blazor_shell"),
                "gm_blazor_shell": None if args.skip_public_edge else frontdoor_payload.get("gm_blazor_shell"),
                "player_manifest_path": None if args.skip_public_edge else frontdoor_payload.get("pwa_manifest_path"),
                "gm_manifest_path": None if args.skip_public_edge else frontdoor_payload.get("gm_pwa_manifest_path"),
                "player_handoff_url": None if args.skip_public_edge else frontdoor_payload.get("player_session_handoff_url"),
                "gm_handoff_url": None if args.skip_public_edge else frontdoor_payload.get("gm_session_handoff_url"),
                "player_handoff_strips_device": None if args.skip_public_edge else frontdoor_payload.get("player_session_handoff_strips_device"),
                "gm_handoff_strips_device": None if args.skip_public_edge else frontdoor_payload.get("gm_session_handoff_strips_device"),
                "rybbit_player": None if args.skip_public_edge else frontdoor_payload.get("rybbit_configured"),
                "rybbit_gm": None if args.skip_public_edge else frontdoor_payload.get("gm_rybbit_configured"),
            },
            "external_blockers": [] if args.skip_public_edge else public_edge_failures,
        }
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
        print(f"wrote mobile cross-surface readiness: {output_path}")
        return 0
    finally:
        shutil.rmtree(fleet_tmp_dir, ignore_errors=True)
        shutil.rmtree(public_tmp_dir, ignore_errors=True)


if __name__ == "__main__":
    raise SystemExit(main())
