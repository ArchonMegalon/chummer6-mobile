from __future__ import annotations

import json
import subprocess
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
WORKSPACE_ROOT = REPO_ROOT.parent
SCRIPT = REPO_ROOT / "scripts" / "verify_mobile_pwa_performance_budget.py"
ASSET_PATHS = [
    "mobile.css",
    "mobile-install-shell.js",
    "manifest.webmanifest",
    "manifest.player.webmanifest",
    "manifest.gm.webmanifest",
    "manifest.observer.webmanifest",
    "icons/apple-touch-icon.png",
    "icons/icon-192.png",
    "icons/icon-512.png",
    "icons/icon-192.svg",
    "icons/icon-512.svg",
]
CRITICAL_SHELL_URLS = [
    "/mobile-install-shell.js",
    "/manifest.webmanifest",
    "/manifest.player.webmanifest",
    "/manifest.gm.webmanifest",
    "/manifest.observer.webmanifest",
    "/icons/icon-192.svg",
    "/icons/icon-512.svg",
]


def _write_asset_fixture(root: Path) -> None:
    for relative_path in ASSET_PATHS:
        path = root / relative_path
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(f"fixture:{relative_path}\n".encode("utf-8"))
    quoted_assets = ",\n  ".join(json.dumps(item) for item in CRITICAL_SHELL_URLS)
    (root / "service-worker.js").write_text(
        f"const CRITICAL_SHELL_ASSETS = [\n  {quoted_assets}\n];\n",
        encoding="utf-8",
    )


def _run(
    asset_root: Path,
    output: Path,
    *,
    check_only: bool = False,
) -> subprocess.CompletedProcess[str]:
    command = [
        "python3",
        str(SCRIPT),
        "--asset-root",
        str(asset_root),
        "--output",
        str(output),
    ]
    if check_only:
        command.append("--check-only")
    return subprocess.run(
        command,
        cwd=REPO_ROOT,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
        timeout=30,
    )


class MobilePwaPerformanceBudgetTests(unittest.TestCase):
    def test_current_owned_shell_assets_fit_the_budget(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            output = Path(temp_dir) / "receipt.json"
            completed = _run(REPO_ROOT / "src" / "Chummer.Play.Web" / "wwwroot", output)

            self.assertEqual(completed.returncode, 0, completed.stdout)
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(payload["contract_name"], "chummer_play.mobile_pwa_performance_budget.v1")
            self.assertEqual(payload["status"], "pass")
            self.assertLessEqual(
                payload["aggregate_observed"]["gzip_bytes"],
                payload["aggregate_budget"]["gzip_bytes"],
            )
            self.assertEqual(payload["framework_asset_exceptions"], ["/_framework/blazor.web.js"])
            self.assertEqual(payload["critical_shell_assets"], sorted(CRITICAL_SHELL_URLS))

    def test_check_only_proves_current_assets_without_mutating_a_receipt(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir) / "wwwroot"
            output = Path(temp_dir) / "receipt.json"
            _write_asset_fixture(root)

            completed = _run(root, output, check_only=True)

            self.assertEqual(completed.returncode, 0, completed.stdout)
            self.assertFalse(output.exists())

    def test_oversized_owned_asset_fails_and_materializes_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir) / "wwwroot"
            output = Path(temp_dir) / "receipt.json"
            _write_asset_fixture(root)
            (root / "mobile-install-shell.js").write_bytes(b"x" * (8 * 1024 + 1))

            completed = _run(root, output)

            self.assertEqual(completed.returncode, 1, completed.stdout)
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(payload["status"], "fail")
            self.assertTrue(
                any("/mobile-install-shell.js raw_bytes" in item for item in payload["failures"]),
                payload["failures"],
            )

    def test_unbudgeted_service_worker_asset_fails_closed(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir) / "wwwroot"
            output = Path(temp_dir) / "receipt.json"
            _write_asset_fixture(root)
            service_worker = root / "service-worker.js"
            service_worker.write_text(
                service_worker.read_text(encoding="utf-8").replace(
                    "];", ',\n  "/surprise-heavy-runtime.js"\n];'
                ),
                encoding="utf-8",
            )

            completed = _run(root, output)

            self.assertEqual(completed.returncode, 1, completed.stdout)
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertTrue(
                any("unbudgeted=['/surprise-heavy-runtime.js']" in item for item in payload["failures"]),
                payload["failures"],
            )

    def test_missing_budgeted_asset_fails_closed(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir) / "wwwroot"
            output = Path(temp_dir) / "receipt.json"
            _write_asset_fixture(root)
            (root / "mobile.css").unlink()

            completed = _run(root, output)

            self.assertEqual(completed.returncode, 1, completed.stdout)
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertTrue(
                any("missing shell asset /mobile.css" in item for item in payload["failures"]),
                payload["failures"],
            )

    def test_release_chain_executes_the_budget_fail_closed(self) -> None:
        verify = (REPO_ROOT / "scripts" / "ai" / "verify.sh").read_text(encoding="utf-8")
        mobile_release = (
            REPO_ROOT / "scripts" / "release" / "verify_mobile_release_proof.sh"
        ).read_text(encoding="utf-8")
        root_release = (
            WORKSPACE_ROOT / "scripts" / "release" / "verify_chummer6_release_ready.sh"
        ).read_text(encoding="utf-8")
        release_materializer = (
            WORKSPACE_ROOT
            / "chummer.run-services"
            / "scripts"
            / "materialize_release_ready_receipt.py"
        ).read_text(encoding="utf-8")

        self.assertIn("python3 scripts/verify_mobile_pwa_performance_budget.py", verify)
        self.assertIn(
            'python3 "$repo_root/scripts/verify_mobile_pwa_performance_budget.py" --check-only',
            mobile_release,
        )
        self.assertIn(
            'MATERIALIZER = ROOT / "chummer.run-services/scripts/materialize_release_ready_receipt.py"',
            root_release,
        )
        self.assertIn('"--run-authoritative-controller"', root_release)
        self.assertIn("os.execve(", root_release)
        self.assertIn(
            'spec("verify_mobile_release_proof", f"{bash} {root}/chummer-play/scripts/release/verify_mobile_release_proof.sh"',
            release_materializer,
        )


if __name__ == "__main__":
    unittest.main()
