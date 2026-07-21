from __future__ import annotations

import json
import os
import shutil
import subprocess
import tempfile
import textwrap
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PACKAGE_PLANE = ROOT / "scripts" / "ai" / "with-package-plane.sh"
RECEIPT_WRITER = ROOT / "scripts" / "ai" / "write_verification_mode_receipt.py"
VERIFY_SCRIPT = ROOT / "scripts" / "ai" / "verify.sh"


def write_fake_dotnet(bin_dir: Path, log_path: Path) -> None:
    dotnet = bin_dir / "dotnet"
    dotnet.write_text(
        textwrap.dedent(
            f"""\
            #!/usr/bin/env bash
            set -euo pipefail
            printf '%s\n' "$*" >> "{log_path}"
            if [[ "${{1:-}}" == "pack" ]]; then
              output=""
              package_id=""
              package_version=""
              previous=""
              for arg in "$@"; do
                if [[ "${{previous}}" == "-o" ]]; then
                  output="${{arg}}"
                fi
                case "${{arg}}" in
                  -p:PackageId=*) package_id="${{arg#-p:PackageId=}}" ;;
                  -p:PackageVersion=*) package_version="${{arg#-p:PackageVersion=}}" ;;
                esac
                previous="${{arg}}"
              done
              mkdir -p "${{output}}"
              printf '%s\n' "${{RANDOM}}-$(date +%s%N)" > "${{output}}/${{package_id}}.${{package_version}}.nupkg"
            fi
            if [[ ("${{1:-}}" == "restore" || "${{1:-}}" == "build") && "${{2:-}}" == *.csproj ]]; then
              target="${{2}}"
              if [[ "${{target}}" != /* ]]; then
                target="$(pwd)/${{target}}"
              fi
              mkdir -p "$(dirname "${{target}}")/obj"
              printf '{{"source":"%s"}}\n' "${{CHUMMER_PUBLISHED_FEED_SOURCES:-local-owner}}" > "$(dirname "${{target}}")/obj/project.assets.json"
            fi
            """
        ),
        encoding="utf-8",
    )
    dotnet.chmod(0o755)


def package_plane_env(temp_root: Path, *, mode: str, allow_stubs: str | None = None) -> tuple[dict[str, str], Path]:
    bin_dir = temp_root / "bin"
    bin_dir.mkdir(parents=True)
    log_path = temp_root / "dotnet.log"
    write_fake_dotnet(bin_dir, log_path)
    missing = temp_root / "missing-owner.csproj"
    env = {
        **os.environ,
        "PATH": f"{bin_dir}:{os.environ['PATH']}",
        "CHUMMER_VERIFY_MODE": mode,
        "CHUMMER_PUBLISHED_FEED_SOURCES": "",
        "CHUMMER_PACKAGE_PLANE_LOCK_FILE": str(temp_root / "package-plane.lock"),
        "CHUMMER_PACKAGE_PLANE_ATTESTATION_FILE": str(temp_root / "package-plane.attestation"),
        "CHUMMER_PACKAGE_PLANE_LOCAL_FEED": str(temp_root / "local-feed"),
        "CHUMMER_VERIFY_RUN_ID": f"test-{mode}-run",
        "NUGET_PACKAGES": str(temp_root / "nuget-packages"),
        "CHUMMER_PACKAGE_PLANE_ENGINE_CONTRACTS_PROJECT": str(missing),
        "CHUMMER_PACKAGE_PLANE_CAMPAIGN_CONTRACTS_PROJECT": str(missing),
        "CHUMMER_PACKAGE_PLANE_CONTROL_CONTRACTS_PROJECT": str(missing),
        "CHUMMER_PACKAGE_PLANE_PLAY_CONTRACTS_PROJECT": str(missing),
        "CHUMMER_PACKAGE_PLANE_UI_KIT_PROJECT": str(missing),
    }
    if allow_stubs is not None:
        env["CHUMMER_ALLOW_STUB_PACKAGES"] = allow_stubs
    else:
        env.pop("CHUMMER_ALLOW_STUB_PACKAGES", None)
    return env, log_path


def run_package_plane(
    env: dict[str, str],
    *extra_args: str,
    command: str = "build",
    project: str | None = "src/Chummer.Play.Core/Chummer.Play.Core.csproj",
) -> subprocess.CompletedProcess[str]:
    command_args = ["bash", str(PACKAGE_PLANE), command]
    if project is not None:
        command_args.append(project)
    command_args.extend(["--nologo", *extra_args])
    return subprocess.run(
        command_args,
        cwd=ROOT,
        env=env,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
        timeout=30,
    )


class VerificationModeTests(unittest.TestCase):
    def test_scaffold_mode_may_build_explicit_stub_packages(self) -> None:
        with tempfile.TemporaryDirectory(prefix="mobile-verify-scaffold-") as temp_dir:
            env, log_path = package_plane_env(Path(temp_dir), mode="scaffold")
            completed = run_package_plane(env)
            lines = log_path.read_text(encoding="utf-8").splitlines()

        self.assertEqual(completed.returncode, 0, completed.stdout)
        self.assertEqual(sum("eng/package-stubs/" in line for line in lines), 5)
        self.assertIn("build src/Chummer.Play.Core/Chummer.Play.Core.csproj --nologo", lines[-1])

    def test_integration_mode_fails_when_owner_and_published_packages_are_absent(self) -> None:
        with tempfile.TemporaryDirectory(prefix="mobile-verify-integration-") as temp_dir:
            env, log_path = package_plane_env(Path(temp_dir), mode="integration")
            completed = run_package_plane(env)

        self.assertNotEqual(completed.returncode, 0)
        self.assertIn("integration verification requires owner or published package", completed.stdout)
        self.assertIn("stub fallback is disabled", completed.stdout)
        self.assertFalse(log_path.exists())

    def test_release_and_integration_reject_stub_override(self) -> None:
        for mode in ("integration", "release"):
            with self.subTest(mode=mode), tempfile.TemporaryDirectory(prefix="mobile-verify-override-") as temp_dir:
                env, _ = package_plane_env(Path(temp_dir), mode=mode, allow_stubs="1")
                completed = run_package_plane(env)

            self.assertEqual(completed.returncode, 2)
            self.assertIn(f"{mode} verification forbids stub packages", completed.stdout)

    def test_slice_mode_honors_explicit_stub_prohibition(self) -> None:
        with tempfile.TemporaryDirectory(prefix="mobile-verify-slice-") as temp_dir:
            env, _ = package_plane_env(Path(temp_dir), mode="slice", allow_stubs="0")
            completed = run_package_plane(env)

        self.assertNotEqual(completed.returncode, 0)
        self.assertIn("slice verification requires owner or published package", completed.stdout)

    def test_integration_no_restore_rejects_prior_or_missing_package_provenance(self) -> None:
        with tempfile.TemporaryDirectory(prefix="mobile-verify-no-restore-") as temp_dir:
            env, _ = package_plane_env(Path(temp_dir), mode="integration")
            completed = run_package_plane(env, "--no-restore")

        self.assertNotEqual(completed.returncode, 0)
        self.assertIn("lacks matching same-run no-stub package-plane provenance", completed.stdout)

    def test_strict_no_restore_requires_matching_same_run_feed_attestation(self) -> None:
        with tempfile.TemporaryDirectory(prefix="mobile-verify-attestation-") as temp_dir:
            temp_root = Path(temp_dir)
            env, _ = package_plane_env(temp_root, mode="integration")
            env["CHUMMER_PUBLISHED_FEED_SOURCES"] = "https://packages.example.invalid/v3/index.json"
            project = temp_root / "consumer" / "Consumer.csproj"
            project.parent.mkdir(parents=True)
            project.write_text("<Project />\n", encoding="utf-8")
            refreshed = run_package_plane(env, command="build", project=str(project))
            no_restore = run_package_plane(env, "--no-restore", project=str(project))
            env["CHUMMER_PUBLISHED_FEED_SOURCES"] = "https://different.example.invalid/v3/index.json"
            changed_feed = run_package_plane(env, "--no-restore", project=str(project))
            env["CHUMMER_PUBLISHED_FEED_SOURCES"] = "https://packages.example.invalid/v3/index.json"
            env["CHUMMER_VERIFY_RUN_ID"] = "different-run"
            replay = run_package_plane(env, "--no-restore", project=str(project))

        self.assertEqual(refreshed.returncode, 0, refreshed.stdout)
        self.assertEqual(no_restore.returncode, 0, no_restore.stdout)
        self.assertNotEqual(changed_feed.returncode, 0)
        self.assertIn("lacks matching same-run no-stub package-plane provenance", changed_feed.stdout)
        self.assertNotEqual(replay.returncode, 0)
        self.assertIn("lacks matching same-run no-stub package-plane provenance", replay.stdout)

    def test_non_restore_command_cannot_mint_strict_package_attestation(self) -> None:
        with tempfile.TemporaryDirectory(prefix="mobile-verify-attestation-command-") as temp_dir:
            temp_root = Path(temp_dir)
            env, _ = package_plane_env(temp_root, mode="integration")
            env["CHUMMER_PUBLISHED_FEED_SOURCES"] = "https://packages.example.invalid/v3/index.json"
            project = temp_root / "consumer" / "Consumer.csproj"
            project.parent.mkdir(parents=True)
            project.write_text("<Project />\n", encoding="utf-8")
            info = run_package_plane(env, command="--info", project=None)
            no_restore = run_package_plane(env, "--no-restore", project=str(project))

        self.assertEqual(info.returncode, 0, info.stdout)
        self.assertNotEqual(no_restore.returncode, 0)
        self.assertIn("lacks matching same-run no-stub package-plane provenance", no_restore.stdout)

    def test_repacked_owner_inventory_keeps_prior_target_attestation_valid(self) -> None:
        with tempfile.TemporaryDirectory(prefix="mobile-verify-owner-repack-") as temp_dir:
            temp_root = Path(temp_dir)
            env, _ = package_plane_env(temp_root, mode="integration")
            owner_projects = {}
            for env_name in (
                "CHUMMER_PACKAGE_PLANE_ENGINE_CONTRACTS_PROJECT",
                "CHUMMER_PACKAGE_PLANE_CAMPAIGN_CONTRACTS_PROJECT",
                "CHUMMER_PACKAGE_PLANE_CONTROL_CONTRACTS_PROJECT",
                "CHUMMER_PACKAGE_PLANE_PLAY_CONTRACTS_PROJECT",
                "CHUMMER_PACKAGE_PLANE_UI_KIT_PROJECT",
            ):
                project = temp_root / "owners" / f"{env_name}.csproj"
                project.parent.mkdir(parents=True, exist_ok=True)
                project.write_text("<Project />\n", encoding="utf-8")
                owner_projects[env_name] = str(project)
            env.update(owner_projects)
            first_project = temp_root / "consumer-one" / "ConsumerOne.csproj"
            second_project = temp_root / "consumer-two" / "ConsumerTwo.csproj"
            first_project.parent.mkdir(parents=True)
            second_project.parent.mkdir(parents=True)
            first_project.write_text("<Project />\n", encoding="utf-8")
            second_project.write_text("<Project />\n", encoding="utf-8")

            first_build = run_package_plane(env, command="build", project=str(first_project))
            second_build = run_package_plane(env, command="build", project=str(second_project))
            first_no_restore = run_package_plane(
                env,
                "--no-restore",
                project=str(first_project),
            )

        self.assertEqual(first_build.returncode, 0, first_build.stdout)
        self.assertEqual(second_build.returncode, 0, second_build.stdout)
        self.assertEqual(first_no_restore.returncode, 0, first_no_restore.stdout)

    def test_owner_packages_restore_only_from_the_ephemeral_feed(self) -> None:
        with tempfile.TemporaryDirectory(prefix="mobile-verify-owner-feed-") as temp_dir:
            temp_root = Path(temp_dir)
            env, log_path = package_plane_env(temp_root, mode="integration")
            env["CHUMMER_PUBLISHED_ENGINE_CONTRACTS_VERSION"] = "5.225.1-ci.6f7cc7d8"
            for env_name in (
                "CHUMMER_PACKAGE_PLANE_ENGINE_CONTRACTS_PROJECT",
                "CHUMMER_PACKAGE_PLANE_CAMPAIGN_CONTRACTS_PROJECT",
                "CHUMMER_PACKAGE_PLANE_CONTROL_CONTRACTS_PROJECT",
                "CHUMMER_PACKAGE_PLANE_PLAY_CONTRACTS_PROJECT",
                "CHUMMER_PACKAGE_PLANE_UI_KIT_PROJECT",
            ):
                project = temp_root / "owners" / f"{env_name}.csproj"
                project.parent.mkdir(parents=True, exist_ok=True)
                project.write_text("<Project />\n", encoding="utf-8")
                env[env_name] = str(project)

            completed = run_package_plane(env)
            pack_lines = [
                line
                for line in log_path.read_text(encoding="utf-8").splitlines()
                if line.startswith("pack ")
            ]

        self.assertEqual(completed.returncode, 0, completed.stdout)
        self.assertEqual(len(pack_lines), 5)
        expected_feed = f'-p:RestoreSources={temp_root / "local-feed"}'
        self.assertTrue(all(expected_feed in line for line in pack_lines))
        self.assertTrue(all("-p:RestoreIgnoreFailedSources=false" in line for line in pack_lines))
        self.assertTrue(all("-p:RestorePackagesWithLockFile=false" in line for line in pack_lines))
        self.assertTrue(all("-p:RestoreLockedMode=false" in line for line in pack_lines))
        self.assertTrue(
            all("-p:ChummerEngineContractsPackageVersion=5.225.1-ci.6f7cc7d8" in line for line in pack_lines)
        )
        engine_pack = next(line for line in pack_lines if "-p:PackageId=Chummer.Engine.Contracts" in line)
        campaign_pack = next(line for line in pack_lines if "-p:PackageId=Chummer.Campaign.Contracts" in line)
        self.assertIn(f"{expected_feed};https://api.nuget.org/v3/index.json", engine_pack)
        self.assertNotIn("api.nuget.org", campaign_pack)

    def test_verification_receipt_records_mode_skips_and_release_eligibility(self) -> None:
        with tempfile.TemporaryDirectory(prefix="mobile-verify-receipt-") as temp_dir:
            output = Path(temp_dir) / "receipt.json"
            completed = subprocess.run(
                [
                    "python3",
                    str(RECEIPT_WRITER),
                    "--mode",
                    "slice",
                    "--status",
                    "pass",
                    "--stub-packages-allowed",
                    "1",
                    "--verification-run-id",
                    "test-slice-run",
                    "--skip",
                    "published-feed compatibility",
                    "--skip",
                    "published-feed compatibility",
                    "--output",
                    str(output),
                ],
                cwd=ROOT,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
                timeout=30,
            )
            payload = json.loads(output.read_text(encoding="utf-8"))

        self.assertEqual(completed.returncode, 0, completed.stdout)
        self.assertEqual(payload["contractName"], "chummer6-mobile.verification-mode/v1")
        self.assertEqual(payload["mode"], "slice")
        self.assertEqual(payload["verificationRunId"], "test-slice-run")
        self.assertEqual(payload["skipCount"], 1)
        self.assertEqual(payload["skips"], ["published-feed compatibility"])
        self.assertFalse(payload["releaseEvidenceEligible"])

    def test_release_verify_fails_closed_and_receipts_the_missing_feed(self) -> None:
        with tempfile.TemporaryDirectory(prefix="mobile-verify-release-feed-") as temp_dir:
            temp_root = Path(temp_dir)
            temp_ai_dir = temp_root / "scripts" / "ai"
            temp_ai_dir.mkdir(parents=True)
            copied_verify = temp_ai_dir / "verify.sh"
            copied_writer = temp_ai_dir / "write_verification_mode_receipt.py"
            shutil.copy2(VERIFY_SCRIPT, copied_verify)
            shutil.copy2(RECEIPT_WRITER, copied_writer)
            env = {
                **os.environ,
                "CHUMMER_VERIFY_MODE": "release",
                "CHUMMER_ALLOW_STUB_PACKAGES": "0",
                "CHUMMER_PUBLISHED_FEED_SOURCES": "",
            }
            completed = subprocess.run(
                ["bash", str(copied_verify)],
                cwd=temp_root,
                env=env,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
                timeout=30,
            )
            receipt_path = (
                temp_root
                / ".codex-studio"
                / "published"
                / "MOBILE_VERIFICATION_MODE.generated.json"
            )
            payload = json.loads(receipt_path.read_text(encoding="utf-8"))

        self.assertEqual(completed.returncode, 1, completed.stdout)
        self.assertIn("release verification cannot skip", completed.stdout)
        self.assertEqual(payload["mode"], "release")
        self.assertEqual(payload["status"], "fail")
        self.assertFalse(payload["stubPackagesAllowed"])
        self.assertEqual(payload["skipCount"], 1)
        self.assertFalse(payload["releaseEvidenceEligible"])

    def test_owned_verification_receipt_producers_record_the_mode(self) -> None:
        producers = (
            "scripts/cleanup_mobile_disposable_artifacts.py",
            "scripts/materialize_mobile_cross_surface_readiness.py",
            "scripts/materialize_mobile_local_release_proof.py",
            "scripts/materialize_mobile_release_boundary.py",
            "scripts/run_mobile_strict_public_edge_follow_through.py",
            "scripts/verify_mobile_pwa_analytics_smoke.py",
            "scripts/verify_mobile_pwa_performance_budget.py",
            "scripts/verify_mobile_pwa_runtime_smoke.py",
            "scripts/verify_mobile_pwa_viewport_smoke.py",
        )

        for relative_path in producers:
            with self.subTest(path=relative_path):
                source = (ROOT / relative_path).read_text(encoding="utf-8")
                self.assertIn("CHUMMER_VERIFY_MODE", source)
                self.assertIn('"verification_mode"', source)
                self.assertIn("CHUMMER_VERIFY_RUN_ID", source)
                self.assertIn('"verification_run_id"', source)


if __name__ == "__main__":
    unittest.main()
