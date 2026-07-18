#!/usr/bin/env python3
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import subprocess
import time
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
BOUNDARY_RECEIPT = ROOT / ".codex-studio" / "published" / "MOBILE_RELEASE_BOUNDARY.generated.json"
CROSS_SURFACE_RECEIPT = ROOT / ".codex-studio" / "published" / "MOBILE_CROSS_SURFACE_READINESS.generated.json"
LOCAL_RELEASE_PROOF_RECEIPT = ROOT / ".codex-studio" / "published" / "MOBILE_LOCAL_RELEASE_PROOF.generated.json"
OUT = ROOT / ".codex-studio" / "published" / "MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json"
LEGACY_OUT = ROOT / ".state" / "mobile_strict_public_edge_follow_through.generated.json"
BOUNDARY_MATERIALIZE_COMMAND = "python3 scripts/materialize_mobile_release_boundary.py"


def iso_now() -> str:
    return dt.datetime.now(dt.timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def trim_output(text: str, *, limit: int = 1200) -> str:
    value = str(text or "").strip()
    if len(value) <= limit:
        return value
    return value[: limit - 3].rstrip() + "..."


def load_json(path: Path, label: str) -> dict[str, Any]:
    if not path.is_file():
        raise SystemExit(f"missing {label}: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise SystemExit(f"invalid {label} json at {path}: {exc}") from exc
    if not isinstance(payload, dict):
        raise SystemExit(f"{label} root must be an object: {path}")
    return payload


def load_optional_json(path: Path) -> dict[str, Any]:
    if not path.is_file():
        return {}
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError:
        return {}
    return payload if isinstance(payload, dict) else {}


def run_shell_command(command: str) -> dict[str, Any]:
    started = time.monotonic()
    completed = subprocess.run(
        ["bash", "-lc", command],
        cwd=ROOT,
        check=False,
        text=True,
        capture_output=True,
    )
    duration_seconds = round(time.monotonic() - started, 3)
    return {
        "command": command,
        "returncode": completed.returncode,
        "duration_seconds": duration_seconds,
        "stdout_tail": trim_output(completed.stdout),
        "stderr_tail": trim_output(completed.stderr),
    }


def extract_strict_follow_through(boundary_payload: dict[str, Any]) -> dict[str, Any]:
    external_follow_through = (
        boundary_payload.get("external_follow_through")
        if isinstance(boundary_payload.get("external_follow_through"), dict)
        else {}
    )
    strict_follow_through = (
        external_follow_through.get("strict_public_edge")
        if isinstance(external_follow_through.get("strict_public_edge"), dict)
        else {}
    )
    if not strict_follow_through:
        raise SystemExit("release boundary strict_public_edge follow-through is missing")
    rerun_commands = strict_follow_through.get("rerun_commands")
    if not isinstance(rerun_commands, list) or not rerun_commands:
        raise SystemExit("release boundary strict_public_edge rerun_commands are missing")
    return strict_follow_through


def materialize_boundary_state() -> dict[str, Any]:
    command_result = run_shell_command(BOUNDARY_MATERIALIZE_COMMAND)
    boundary_payload = load_json(BOUNDARY_RECEIPT, "mobile release boundary receipt")
    strict_follow_through = extract_strict_follow_through(boundary_payload)
    live_build_lock_probe = (
        boundary_payload.get("live_build_lock_probe")
        if isinstance(boundary_payload.get("live_build_lock_probe"), dict)
        else {}
    )
    return {
        "command_result": command_result,
        "boundary_payload": boundary_payload,
        "strict_follow_through": strict_follow_through,
        "live_build_lock_probe": live_build_lock_probe,
    }


def cross_surface_summary() -> dict[str, Any]:
    payload = load_optional_json(CROSS_SURFACE_RECEIPT)
    checks = payload.get("checks") if isinstance(payload.get("checks"), dict) else {}
    return {
        "generated_at_utc": payload.get("generated_at_utc"),
        "status": payload.get("status"),
        "public_edge_gate_pass": checks.get("public_edge_gate_pass"),
        "strict_public_edge_gate_pass": checks.get("strict_public_edge_gate_pass"),
    }


def local_release_proof_summary() -> dict[str, Any]:
    payload = load_optional_json(LOCAL_RELEASE_PROOF_RECEIPT)
    cross_surface_refresh = (
        payload.get("cross_surface_refresh")
        if isinstance(payload.get("cross_surface_refresh"), dict)
        else {}
    )
    return {
        "generated_at_utc": payload.get("generated_at_utc"),
        "status": payload.get("status"),
        "cross_surface_refresh_status": cross_surface_refresh.get("status"),
    }


def write_receipt(output_path: Path, payload: dict[str, Any]) -> None:
    payload["verification_mode"] = os.environ.get("CHUMMER_VERIFY_MODE", "slice").strip() or "slice"
    payload["verification_run_id"] = os.environ.get("CHUMMER_VERIFY_RUN_ID", "").strip()
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    if LEGACY_OUT != output_path and LEGACY_OUT.exists():
        LEGACY_OUT.unlink()


def write_bootstrap_receipt(output_path: Path) -> None:
    write_receipt(
        output_path,
        {
            "contract_name": "chummer6-mobile.strict_public_edge_follow_through.v1",
            "generated_at_utc": iso_now(),
            "script_path": "scripts/run_mobile_strict_public_edge_follow_through.py",
            "status": "bootstrapping",
        },
    )


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default=str(OUT))
    parser.add_argument("--wait-for-clear", action="store_true")
    parser.add_argument("--execute-rerun", action="store_true")
    parser.add_argument("--timeout-seconds", type=float, default=0.0)
    parser.add_argument("--poll-interval-seconds", type=float, default=30.0)
    args = parser.parse_args(argv)

    if args.timeout_seconds < 0:
        raise SystemExit("--timeout-seconds must be non-negative")
    if args.poll_interval_seconds <= 0:
        raise SystemExit("--poll-interval-seconds must be greater than zero")

    output_path = Path(args.output).resolve()
    write_bootstrap_receipt(output_path)
    started_at = time.monotonic()
    deadline = started_at + args.timeout_seconds
    poll_count = 0
    poll_history: list[dict[str, Any]] = []
    current_state: dict[str, Any] | None = None

    while True:
        poll_count += 1
        current_state = materialize_boundary_state()
        boundary_payload = current_state["boundary_payload"]
        strict_follow_through = current_state["strict_follow_through"]
        live_build_lock_probe = current_state["live_build_lock_probe"]
        poll_history.append(
            {
                "observed_at_utc": iso_now(),
                "boundary_generated_at_utc": boundary_payload.get("generated_at_utc"),
                "strict_status": strict_follow_through.get("status"),
                "live_build_lock_probe_status": live_build_lock_probe.get("status"),
                "live_build_lock_process_count": live_build_lock_probe.get("process_count"),
            }
        )
        if strict_follow_through.get("status") == "ready_to_rerun":
            break
        if not args.wait_for_clear:
            break
        if time.monotonic() >= deadline:
            break
        time.sleep(args.poll_interval_seconds)

    assert current_state is not None
    boundary_payload = current_state["boundary_payload"]
    strict_follow_through = current_state["strict_follow_through"]
    live_build_lock_probe = current_state["live_build_lock_probe"]
    preflight_snapshot = (
        boundary_payload.get("preflight_snapshot")
        if isinstance(boundary_payload.get("preflight_snapshot"), dict)
        else {}
    )
    postdeploy_snapshot = (
        boundary_payload.get("postdeploy_snapshot")
        if isinstance(boundary_payload.get("postdeploy_snapshot"), dict)
        else {}
    )
    command_results: list[dict[str, Any]] = []
    final_status = str(strict_follow_through.get("status") or "").strip() or "unknown"
    rerun_executed = False
    rerun_failed = False
    post_rerun_boundary_state: dict[str, Any] | None = None

    if strict_follow_through.get("status") == "ready_to_rerun" and args.execute_rerun:
        rerun_executed = True
        rerun_commands = [str(item).strip() for item in strict_follow_through.get("rerun_commands", []) if str(item).strip()]
        for command in rerun_commands:
            result = run_shell_command(command)
            command_results.append(result)
            if result["returncode"] != 0:
                rerun_failed = True
                break
        post_rerun_boundary_state = materialize_boundary_state()
        boundary_payload = post_rerun_boundary_state["boundary_payload"]
        strict_follow_through = post_rerun_boundary_state["strict_follow_through"]
        live_build_lock_probe = post_rerun_boundary_state["live_build_lock_probe"]
        preflight_snapshot = (
            boundary_payload.get("preflight_snapshot")
            if isinstance(boundary_payload.get("preflight_snapshot"), dict)
            else {}
        )
        postdeploy_snapshot = (
            boundary_payload.get("postdeploy_snapshot")
            if isinstance(boundary_payload.get("postdeploy_snapshot"), dict)
            else {}
        )
        cross_summary = cross_surface_summary()
        proof_summary = local_release_proof_summary()
        if not rerun_failed and cross_summary.get("strict_public_edge_gate_pass") is True and proof_summary.get("status") == "passed":
            final_status = "pass"
        else:
            final_status = "rerun_failed"
    elif strict_follow_through.get("status") == "ready_to_rerun":
        final_status = "ready_to_rerun"

    elapsed_seconds = round(time.monotonic() - started_at, 3)
    cross_summary = cross_surface_summary()
    proof_summary = local_release_proof_summary()

    receipt = {
        "contract_name": "chummer6-mobile.strict_public_edge_follow_through.v1",
        "generated_at_utc": iso_now(),
        "status": final_status,
        "script_path": "scripts/run_mobile_strict_public_edge_follow_through.py",
        "boundary_receipt_path": str(BOUNDARY_RECEIPT),
        "output_path": str(output_path),
        "wait_for_clear": bool(args.wait_for_clear),
        "execute_rerun": bool(args.execute_rerun),
        "timeout_seconds": args.timeout_seconds,
        "poll_interval_seconds": args.poll_interval_seconds,
        "poll_count": poll_count,
        "elapsed_seconds": elapsed_seconds,
        "poll_history": poll_history,
        "boundary_refresh": current_state["command_result"],
        "boundary_generated_at_utc": boundary_payload.get("generated_at_utc"),
        "preflight_snapshot": preflight_snapshot,
        "postdeploy_snapshot": postdeploy_snapshot,
        "strict_follow_through": strict_follow_through,
        "live_build_lock_probe": live_build_lock_probe,
        "rerun_executed": rerun_executed,
        "command_results": command_results,
        "cross_surface_summary": cross_summary,
        "local_release_proof_summary": proof_summary,
    }
    if post_rerun_boundary_state is not None:
        receipt["post_rerun_boundary_refresh"] = post_rerun_boundary_state["command_result"]
    write_receipt(output_path, receipt)
    final_boundary_state = materialize_boundary_state()

    if current_state["command_result"]["returncode"] != 0:
        return 1
    if final_boundary_state["command_result"]["returncode"] != 0:
        return 1
    if rerun_failed:
        return 1
    if args.execute_rerun and final_status != "pass":
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
