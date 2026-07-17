from __future__ import annotations

import os
import subprocess
import tempfile
import textwrap
import time
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SCRIPT_PATH = ROOT / "scripts" / "ai" / "with-package-plane.sh"


def write_fake_dotnet(bin_dir: Path, log_path: Path) -> Path:
    dotnet_path = bin_dir / "dotnet"
    dotnet_path.write_text(
        textwrap.dedent(
            f"""\
            #!/usr/bin/env bash
            set -euo pipefail
            printf '%s start\\n' "${{FAKE_DOTNET_LABEL:-unknown}}" >> "{log_path}"
            sleep "${{FAKE_DOTNET_SLEEP_SECONDS:-0}}"
            printf '%s end\\n' "${{FAKE_DOTNET_LABEL:-unknown}}" >> "{log_path}"
            """
        ),
        encoding="utf-8",
    )
    dotnet_path.chmod(0o755)
    return dotnet_path


def run_with_fake_dotnet(
    *,
    fake_bin_dir: Path,
    lock_path: Path,
    label: str,
    sleep_seconds: str,
    timeout_seconds: str = "600",
) -> subprocess.Popen[str]:
    env = os.environ.copy()
    env["PATH"] = f"{fake_bin_dir}:{env['PATH']}"
    env["FAKE_DOTNET_LABEL"] = label
    env["FAKE_DOTNET_SLEEP_SECONDS"] = sleep_seconds
    env["CHUMMER_PACKAGE_PLANE_LOCK_FILE"] = str(lock_path)
    env["CHUMMER_PACKAGE_PLANE_LOCK_TIMEOUT_SECONDS"] = timeout_seconds
    env["CHUMMER_PUBLISHED_FEED_SOURCES"] = "https://packages.example.invalid/v3/index.json"

    return subprocess.Popen(
        [
            "bash",
            str(SCRIPT_PATH),
            "build",
            "src/Chummer.Play.Web/Chummer.Play.Web.csproj",
            "--nologo",
        ],
        cwd=ROOT,
        env=env,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
    )


class WithPackagePlaneLockingTests(unittest.TestCase):
    def test_script_serializes_fake_dotnet_invocations_through_shared_lock(self) -> None:
        with tempfile.TemporaryDirectory(prefix="with-package-plane-lock-") as temp_dir:
            temp_root = Path(temp_dir)
            log_path = temp_root / "dotnet.log"
            fake_bin_dir = temp_root / "bin"
            fake_bin_dir.mkdir(parents=True, exist_ok=True)
            write_fake_dotnet(fake_bin_dir, log_path)
            lock_path = temp_root / "with-package-plane.lock"

            started_at = time.monotonic()
            first = run_with_fake_dotnet(
                fake_bin_dir=fake_bin_dir,
                lock_path=lock_path,
                label="first",
                sleep_seconds="1",
            )
            time.sleep(0.2)
            second = run_with_fake_dotnet(
                fake_bin_dir=fake_bin_dir,
                lock_path=lock_path,
                label="second",
                sleep_seconds="1",
            )

            first_stdout, first_stderr = first.communicate(timeout=30)
            second_stdout, second_stderr = second.communicate(timeout=30)
            elapsed = time.monotonic() - started_at
            log_lines = log_path.read_text(encoding="utf-8").splitlines()

        self.assertEqual(first.returncode, 0, first_stderr or first_stdout)
        self.assertEqual(second.returncode, 0, second_stderr or second_stdout)
        self.assertGreaterEqual(elapsed, 1.8)
        self.assertEqual(log_lines, ["first start", "first end", "second start", "second end"])

    def test_script_fails_closed_when_lock_timeout_expires(self) -> None:
        with tempfile.TemporaryDirectory(prefix="with-package-plane-lock-timeout-") as temp_dir:
            temp_root = Path(temp_dir)
            log_path = temp_root / "dotnet.log"
            fake_bin_dir = temp_root / "bin"
            fake_bin_dir.mkdir(parents=True, exist_ok=True)
            write_fake_dotnet(fake_bin_dir, log_path)
            lock_path = temp_root / "with-package-plane.lock"

            first = run_with_fake_dotnet(
                fake_bin_dir=fake_bin_dir,
                lock_path=lock_path,
                label="first",
                sleep_seconds="2",
            )
            time.sleep(0.2)
            second = run_with_fake_dotnet(
                fake_bin_dir=fake_bin_dir,
                lock_path=lock_path,
                label="second",
                sleep_seconds="0",
                timeout_seconds="1",
            )

            second_stdout, second_stderr = second.communicate(timeout=30)
            first.communicate(timeout=30)
            log_lines = log_path.read_text(encoding="utf-8").splitlines()

        self.assertNotEqual(second.returncode, 0)
        self.assertIn("timed out waiting for package-plane lock", second_stderr or second_stdout)
        self.assertEqual(log_lines, ["first start", "first end"])


if __name__ == "__main__":
    unittest.main()
