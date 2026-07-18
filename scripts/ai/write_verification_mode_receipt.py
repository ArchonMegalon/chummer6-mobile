#!/usr/bin/env python3
from __future__ import annotations

import argparse
import datetime as dt
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUTPUT = (
    ROOT
    / ".codex-studio"
    / "published"
    / "MOBILE_VERIFICATION_MODE.generated.json"
)
ALLOWED_MODES = {"scaffold", "slice", "integration", "release"}
ALLOWED_STATUSES = {"in_progress", "pass", "fail"}


def utc_now() -> str:
    return (
        dt.datetime.now(dt.timezone.utc)
        .replace(microsecond=0)
        .isoformat()
        .replace("+00:00", "Z")
    )


def build_receipt(
    *,
    mode: str,
    status: str,
    stub_packages_allowed: bool,
    skips: list[str],
    verification_run_id: str,
) -> dict[str, object]:
    if mode not in ALLOWED_MODES:
        raise ValueError(f"unsupported verification mode: {mode}")
    if status not in ALLOWED_STATUSES:
        raise ValueError(f"unsupported verification status: {status}")
    normalized_run_id = verification_run_id.strip()
    if not normalized_run_id:
        raise ValueError("verification run id must not be blank")
    normalized_skips = sorted({item.strip() for item in skips if item.strip()})
    return {
        "contractName": "chummer6-mobile.verification-mode/v1",
        "contractVersion": 1,
        "generatedAtUtc": utc_now(),
        "mode": mode,
        "verificationRunId": normalized_run_id,
        "status": status,
        "stubPackagesAllowed": stub_packages_allowed,
        "skipCount": len(normalized_skips),
        "skips": normalized_skips,
        "releaseEvidenceEligible": (
            status == "pass"
            and mode == "release"
            and not stub_packages_allowed
            and not normalized_skips
        ),
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--mode", required=True, choices=sorted(ALLOWED_MODES))
    parser.add_argument("--status", required=True, choices=sorted(ALLOWED_STATUSES))
    parser.add_argument("--stub-packages-allowed", required=True, choices=("0", "1"))
    parser.add_argument("--verification-run-id", required=True)
    parser.add_argument("--skip", action="append", default=[])
    parser.add_argument("--output", default=str(DEFAULT_OUTPUT))
    args = parser.parse_args()

    payload = build_receipt(
        mode=args.mode,
        status=args.status,
        stub_packages_allowed=args.stub_packages_allowed == "1",
        skips=args.skip,
        verification_run_id=args.verification_run_id,
    )
    output = Path(args.output).resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
