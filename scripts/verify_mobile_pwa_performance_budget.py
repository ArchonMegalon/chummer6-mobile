#!/usr/bin/env python3
"""Fail-closed transfer-size budget for the owned mobile PWA shell assets."""

from __future__ import annotations

import argparse
import datetime as dt
import gzip
import json
import os
import re
import tempfile
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_ASSET_ROOT = ROOT / "src" / "Chummer.Play.Web" / "wwwroot"
DEFAULT_OUTPUT = ROOT / ".codex-studio" / "published" / "MOBILE_PWA_PERFORMANCE_BUDGET.generated.json"
CONTRACT_NAME = "chummer_play.mobile_pwa_performance_budget.v1"

# These are the source-owned install payloads named by service-worker.js. The
# framework bootstrap is SDK-owned and is intentionally called out separately
# in the receipt instead of being silently treated as measured.
FRAMEWORK_ASSET_EXCEPTIONS = {"/_framework/blazor.web.js"}
ASSET_BUDGETS = {
    "/mobile.css": {"raw_bytes": 14 * 1024, "gzip_bytes": 4 * 1024},
    "/mobile-install-shell.js": {"raw_bytes": 8 * 1024, "gzip_bytes": 3 * 1024},
    "/manifest.webmanifest": {"raw_bytes": 2 * 1024, "gzip_bytes": 768},
    "/manifest.player.webmanifest": {"raw_bytes": 2 * 1024, "gzip_bytes": 768},
    "/manifest.gm.webmanifest": {"raw_bytes": 2 * 1024, "gzip_bytes": 768},
    "/manifest.observer.webmanifest": {"raw_bytes": 2 * 1024, "gzip_bytes": 768},
    "/icons/apple-touch-icon.png": {"raw_bytes": 18 * 1024, "gzip_bytes": 18 * 1024},
    "/icons/icon-192.png": {"raw_bytes": 8 * 1024, "gzip_bytes": 8 * 1024},
    "/icons/icon-512.png": {"raw_bytes": 24 * 1024, "gzip_bytes": 24 * 1024},
    "/icons/icon-192.svg": {"raw_bytes": 1024, "gzip_bytes": 512},
    "/icons/icon-512.svg": {"raw_bytes": 1024, "gzip_bytes": 512},
}
AGGREGATE_BUDGET = {"raw_bytes": 96 * 1024, "gzip_bytes": 56 * 1024}
SERVICE_WORKER_BUDGET = {"raw_bytes": 28 * 1024, "gzip_bytes": 7 * 1024}
EXPECTED_CRITICAL_SHELL_ASSETS = {
    "/mobile-install-shell.js",
    "/manifest.webmanifest",
    "/manifest.player.webmanifest",
    "/manifest.gm.webmanifest",
    "/manifest.observer.webmanifest",
    "/icons/icon-192.svg",
    "/icons/icon-512.svg",
}


def _parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--asset-root", type=Path, default=DEFAULT_ASSET_ROOT)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument(
        "--check-only",
        action="store_true",
        help="evaluate current assets without mutating the materialized receipt",
    )
    return parser.parse_args()


def _gzip_size(data: bytes) -> int:
    return len(gzip.compress(data, compresslevel=9, mtime=0))


def _write_json_atomic(path: Path, payload: dict[str, object]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary_name = tempfile.mkstemp(prefix=f".{path.name}.", dir=path.parent)
    temporary_path = Path(temporary_name)
    try:
        with os.fdopen(descriptor, "w", encoding="utf-8") as stream:
            json.dump(payload, stream, indent=2, sort_keys=True)
            stream.write("\n")
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary_path, path)
    finally:
        temporary_path.unlink(missing_ok=True)


def _read_owned_asset(asset_root: Path, url_path: str) -> tuple[bytes | None, str | None]:
    candidate = asset_root / url_path.removeprefix("/")
    try:
        resolved = candidate.resolve(strict=True)
    except (FileNotFoundError, OSError) as exc:
        return None, f"missing shell asset {url_path}: {exc.__class__.__name__}"
    if candidate.is_symlink() or not resolved.is_relative_to(asset_root):
        return None, f"shell asset escapes the owned asset root: {url_path}"
    if not resolved.is_file():
        return None, f"shell asset is not a regular file: {url_path}"
    return resolved.read_bytes(), None


def _critical_service_worker_assets(source: str) -> set[str] | None:
    match = re.search(
        r"const\s+CRITICAL_SHELL_ASSETS\s*=\s*\[(.*?)\];",
        source,
        re.DOTALL,
    )
    if match is None:
        return None
    return set(re.findall(r'["\']([^"\']+)["\']', match.group(1)))


def evaluate(asset_root: Path) -> dict[str, object]:
    asset_root = asset_root.resolve()
    try:
        asset_root_label = str(asset_root.relative_to(ROOT))
    except ValueError:
        asset_root_label = "<external-test-fixture>"
    failures: list[str] = []
    observed_assets: dict[str, dict[str, int]] = {}
    aggregate_raw = 0
    aggregate_gzip = 0

    service_worker_data, service_worker_error = _read_owned_asset(asset_root, "/service-worker.js")
    if service_worker_error:
        failures.append(service_worker_error)
        service_worker_assets: set[str] | None = None
        service_worker_observed = None
    else:
        assert service_worker_data is not None
        service_worker_observed = {
            "raw_bytes": len(service_worker_data),
            "gzip_bytes": _gzip_size(service_worker_data),
        }
        service_worker_assets = _critical_service_worker_assets(
            service_worker_data.decode("utf-8")
        )
        if service_worker_assets is None:
            failures.append(
                "service worker CRITICAL_SHELL_ASSETS contract is missing or unparsable"
            )
        for metric, maximum in SERVICE_WORKER_BUDGET.items():
            observed = service_worker_observed[metric]
            if observed > maximum:
                failures.append(
                    f"/service-worker.js {metric} {observed} exceeds budget {maximum}"
                )

    expected_shell_assets = EXPECTED_CRITICAL_SHELL_ASSETS
    if service_worker_assets is not None and service_worker_assets != expected_shell_assets:
        missing = sorted(expected_shell_assets - service_worker_assets)
        unbudgeted = sorted(service_worker_assets - expected_shell_assets)
        failures.append(
            "service worker CRITICAL_SHELL_ASSETS drifted "
            f"(missing={missing}, unbudgeted={unbudgeted})"
        )

    for url_path, budget in sorted(ASSET_BUDGETS.items()):
        data, error = _read_owned_asset(asset_root, url_path)
        if error:
            failures.append(error)
            continue
        assert data is not None
        observed = {"raw_bytes": len(data), "gzip_bytes": _gzip_size(data)}
        observed_assets[url_path] = observed
        aggregate_raw += observed["raw_bytes"]
        aggregate_gzip += observed["gzip_bytes"]
        for metric, maximum in budget.items():
            if observed[metric] > maximum:
                failures.append(
                    f"{url_path} {metric} {observed[metric]} exceeds budget {maximum}"
                )

    aggregate_observed = {"raw_bytes": aggregate_raw, "gzip_bytes": aggregate_gzip}
    for metric, maximum in AGGREGATE_BUDGET.items():
        if aggregate_observed[metric] > maximum:
            failures.append(
                f"owned SHELL_ASSETS aggregate {metric} {aggregate_observed[metric]} "
                f"exceeds budget {maximum}"
            )

    now = dt.datetime.now(dt.timezone.utc).isoformat().replace("+00:00", "Z")
    return {
        "contract_name": CONTRACT_NAME,
        "generated_at_utc": now,
        "status": "pass" if not failures else "fail",
        "measurement": "deterministic gzip-9 with mtime=0 over source-owned service-worker install assets",
        "asset_root": asset_root_label,
        "asset_budgets": ASSET_BUDGETS,
        "assets": observed_assets,
        "aggregate_budget": AGGREGATE_BUDGET,
        "aggregate_observed": aggregate_observed,
        "service_worker_budget": SERVICE_WORKER_BUDGET,
        "service_worker_observed": service_worker_observed,
        "critical_shell_assets": sorted(service_worker_assets or []),
        "framework_asset_exceptions": sorted(FRAMEWORK_ASSET_EXCEPTIONS),
        "failures": failures,
    }


def main() -> int:
    args = _parse_args()
    payload = evaluate(args.asset_root)
    if not args.check_only:
        _write_json_atomic(args.output.resolve(), payload)
    print(
        "mobile_pwa_performance_budget:"
        f"{payload['status']} raw={payload['aggregate_observed']['raw_bytes']} "
        f"gzip={payload['aggregate_observed']['gzip_bytes']}"
    )
    for failure in payload["failures"]:
        print(f"- {failure}")
    return 0 if payload["status"] == "pass" else 1


if __name__ == "__main__":
    raise SystemExit(main())
