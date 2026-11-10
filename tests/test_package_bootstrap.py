"""Package-source control-flow checks; dotnet is always a non-building fixture."""

from __future__ import annotations

import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[1]
OWNER_INPUTS = {
    "ENGINE_CONTRACTS": ("Chummer.Engine.Contracts", "chummer-core-engine/Chummer.Contracts/Chummer.Contracts.csproj"),
    "CAMPAIGN_CONTRACTS": ("Chummer.Campaign.Contracts", "chummer.run-services/Chummer.Campaign.Contracts/Chummer.Campaign.Contracts.csproj"),
    "CONTROL_CONTRACTS": ("Chummer.Control.Contracts", "chummer.run-services/Chummer.Control.Contracts/Chummer.Control.Contracts.csproj"),
    "PLAY_CONTRACTS": ("Chummer.Play.Contracts", "chummer.run-services/Chummer.Play.Contracts/Chummer.Play.Contracts.csproj"),
    "UI_KIT": ("Chummer.Ui.Kit", "chummer-ui-kit/src/Chummer.Ui.Kit/Chummer.Ui.Kit.csproj"),
}


class PackageBootstrapTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory(prefix="mobile-package-bootstrap-test-")
        self.addCleanup(self.temp.cleanup)
        self.sandbox = Path(self.temp.name)
        self.repo = self.sandbox / "mobile"
        ai = self.repo / "scripts/ai"
        ai.mkdir(parents=True)
        self.script = ai / "with-package-plane.sh"
        shutil.copy2(ROOT / "scripts/ai/with-package-plane.sh", self.script)
        shutil.copy2(ROOT / "Directory.Packages.props", self.repo / "Directory.Packages.props")
        (self.repo / "src").mkdir()
        (self.repo / "eng/package-stubs").mkdir(parents=True)
        bin_dir = self.sandbox / "bin"
        bin_dir.mkdir()
        # This fixture only records argument vectors and optional failures. It
        # neither compiles nor packages anything and cannot prove consumption.
        fake = bin_dir / "dotnet"
        fake.write_text(
            "#!/usr/bin/env python3\n"
            "import json, os, sys\n"
            "with open(os.environ['FAKE_DOTNET_LOG'], 'a') as log:\n"
            "    log.write(json.dumps(sys.argv[1:]) + '\\n')\n"
            "if sys.argv[1] == 'pack' and os.environ.get('FAKE_PACK_FAIL') == '1':\n"
            "    sys.exit(23)\n"
            "sys.exit(int(os.environ.get('FAKE_DOTNET_EXIT', '0')))\n",
            encoding="utf-8",
        )
        fake.chmod(0o755)
        self.log = self.sandbox / "dotnet.jsonl"
        self.env = {
            key: value for key, value in os.environ.items()
            if not key.startswith(("CHUMMER_", "FAKE_DOTNET_", "FAKE_PACK_"))
        }
        self.env.update({
            "PATH": f"{bin_dir}:{os.environ['PATH']}",
            "FAKE_DOTNET_LOG": str(self.log),
            "NUGET_PACKAGES": str(self.sandbox / "cache"),
            "CHUMMER_PACKAGE_PLANE_LOCAL_FEED": str(self.sandbox / "feed"),
            "CHUMMER_PACKAGE_PLANE_LOCK_FILE": str(self.sandbox / "package-plane.lock"),
            "CHUMMER_PACKAGE_PLANE_ATTESTATION_FILE": str(self.sandbox / "restore-attestation.v1"),
        })

    def run_wrapper(self, *args: str) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            ["bash", str(self.script), "build", "src/Consumer/Consumer.csproj", *args],
            cwd=self.repo, env=self.env, text=True, stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT, timeout=10, check=False,
        )

    def invocations(self) -> list[list[str]]:
        return [json.loads(line) for line in self.log.read_text().splitlines()] if self.log.exists() else []

    def test_default_reports_every_missing_pin_without_touching_cached_evidence(self) -> None:
        sentinels = [
            self.sandbox / "feed/Chummer.Engine.Contracts.0.1.0-preview.nupkg",
            self.sandbox / "cache/chummer.engine.contracts/sentinel",
            self.repo / "src/Consumer/obj/project.assets.json",
            self.sandbox / "restore-attestation.v1.source",
        ]
        for sentinel in sentinels:
            sentinel.parent.mkdir(parents=True, exist_ok=True)
            sentinel.write_text("untouched test sentinel\n")
        self.env["CHUMMER_ALLOW_STUB_PACKAGES"] = "1"  # verify.sh's legacy export
        result = self.run_wrapper()
        self.assertNotEqual(result.returncode, 0, result.stdout)
        props = ET.parse(self.repo / "Directory.Packages.props").getroot()
        for package in props.findall("./ItemGroup/PackageVersion"):
            property_name = package.attrib["Version"][2:-1]
            version = props.findtext(f"./PropertyGroup/{property_name}")
            self.assertIn(f"{package.attrib['Include']} {version}", result.stdout)
        self.assertIn("CHUMMER_PUBLISHED_FEED_SOURCES", result.stdout)
        self.assertEqual(self.invocations(), [])
        for sentinel in sentinels:
            self.assertEqual(sentinel.read_text(), "untouched test sentinel\n")

    def test_missing_source_rejected_even_when_restore_or_build_is_skipped(self) -> None:
        for flag in ("--no-restore", "--no-build"):
            with self.subTest(flag=flag):
                result = self.run_wrapper(flag)
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("stub fallback is disabled", result.stdout)
                self.assertEqual(self.invocations(), [])

    def test_siblings_are_not_implicit_package_authority(self) -> None:
        for _, relative_path in OWNER_INPUTS.values():
            project = self.sandbox / relative_path
            project.parent.mkdir(parents=True, exist_ok=True)
            project.write_text("<Project />\n")
        result = self.run_wrapper()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("No complete package source is configured", result.stdout)
        self.assertEqual(self.invocations(), [])

    def test_configured_feed_preserves_product_build_and_version_arguments(self) -> None:
        feed = str(self.sandbox / "approved-feed-fixture")
        self.env["CHUMMER_PUBLISHED_FEED_SOURCES"] = feed
        self.env["CHUMMER_ALLOW_STUB_PACKAGES"] = "1"
        self.env["CHUMMER_PUBLISHED_PLAY_CONTRACTS_VERSION"] = "0.1.0-preview"
        self.env["FAKE_DOTNET_EXIT"] = "37"
        result = self.run_wrapper()
        self.assertEqual(result.returncode, 37, result.stdout)
        self.assertEqual(self.invocations(), [[
            "build", "src/Consumer/Consumer.csproj", f"-p:RestoreSources={feed}",
            "-p:RestoreIgnoreFailedSources=false", "-p:ChummerPlayContractsPackageVersion=0.1.0-preview",
        ]])
        self.assertFalse((self.sandbox / "restore-attestation.v1.source").exists())

    def test_failed_explicit_owner_pack_cannot_fall_back_to_stubs(self) -> None:
        for key, (_, relative_path) in OWNER_INPUTS.items():
            project = self.sandbox / relative_path
            project.parent.mkdir(parents=True, exist_ok=True)
            project.write_text("<Project />\n")
            self.env[f"CHUMMER_PACKAGE_PLANE_{key}_PROJECT"] = str(project)
        self.env["CHUMMER_ALLOW_STUB_PACKAGES"] = "1"
        self.env["FAKE_PACK_FAIL"] = "1"
        result = self.run_wrapper()
        self.assertNotEqual(result.returncode, 0)
        calls = self.invocations()
        self.assertEqual(len(calls), 1, calls)
        self.assertEqual(calls[0][0], "pack")
        self.assertNotIn("package-stubs", calls[0][1])
        self.assertIn("stub fallback is disabled", result.stdout)

    def install_source_bootstrap_fixture(self) -> None:
        # Dispatch fixture only: no downloads, packages or compiler invocations.
        shutil.copy2(ROOT / "eng/package-plane.lock.json", self.repo / "eng/package-plane.lock.json")
        helper = self.repo / "scripts/ai/bootstrap-owner-contracts-feed.py"
        helper.write_text(
            "import json, os, sys\n"
            "from pathlib import Path\n"
            "args=sys.argv[1:]\n"
            "root=Path(args[args.index('--repo-root')+1])\n"
            "lock=json.loads((root/'eng/package-plane.lock.json').read_text())\n"
            "if '--print-version' in args: print(lock['package_version']); sys.exit(0)\n"
            "with open(os.environ['FAKE_SOURCE_LOG'], 'a') as f: f.write(json.dumps(args)+'\\n')\n"
            "if os.environ.get('FAKE_SOURCE_FAIL') == '1': sys.exit(29)\n"
        )
        self.source_log = self.sandbox / "source.jsonl"
        self.env["FAKE_SOURCE_LOG"] = str(self.source_log)

    def test_default_dispatches_locked_source_before_product_build(self) -> None:
        self.install_source_bootstrap_fixture()
        self.env["FAKE_DOTNET_EXIT"] = "37"
        result = self.run_wrapper()
        self.assertEqual(result.returncode, 37, result.stdout)
        source_calls = [json.loads(row) for row in self.source_log.read_text().splitlines()]
        self.assertIn("--ensure", source_calls[0])
        self.assertIn("--check-cache", source_calls[1])
        self.assertIn("--write-restore-config", source_calls[2])
        calls = self.invocations()
        self.assertEqual(len(calls), 1)
        self.assertEqual(calls[0][:2], ["build", "src/Consumer/Consumer.csproj"])
        self.assertIn("-p:RestoreIgnoreFailedSources=false", calls[0])
        self.assertTrue(any(a.startswith("-p:RestoreConfigFile=") for a in calls[0]))
        self.assertTrue(any("source-" in a and "https://api.nuget.org/v3/index.json" in a for a in calls[0]))

    def test_source_failure_cannot_fall_back_to_stub_or_touch_product_evidence(self) -> None:
        self.install_source_bootstrap_fixture()
        self.env["FAKE_SOURCE_FAIL"] = "1"
        self.env["CHUMMER_ALLOW_STUB_PACKAGES"] = "1"
        evidence = self.sandbox / "restore-attestation.v1.source"
        evidence.write_text("preserve")
        result = self.run_wrapper()
        self.assertEqual(result.returncode, 29, result.stdout)
        self.assertEqual(self.invocations(), [])
        self.assertEqual(evidence.read_text(), "preserve")

    def test_locked_source_rejects_version_override_before_any_build(self) -> None:
        self.install_source_bootstrap_fixture()
        self.env["CHUMMER_PUBLISHED_PLAY_CONTRACTS_VERSION"] = "0.1.0-preview"
        result = self.run_wrapper()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("forbids version overrides", result.stdout)
        self.assertEqual(self.invocations(), [])
        self.assertFalse(self.source_log.exists())

    def test_source_skip_restore_validates_without_building_dependencies(self) -> None:
        self.install_source_bootstrap_fixture()
        result = self.run_wrapper("--no-restore")
        self.assertNotEqual(result.returncode, 0)
        calls = [json.loads(row) for row in self.source_log.read_text().splitlines()]
        self.assertIn("--validate-only", calls[0])
        self.assertTrue(all("--ensure" not in call and "--repair-cache" not in call for call in calls))
        self.assertEqual(self.invocations(), [])

    def test_only_explicit_scaffold_mode_retains_legacy_stub_path(self) -> None:
        self.env["CHUMMER_VERIFY_MODE"] = "scaffold"
        result = self.run_wrapper()
        self.assertEqual(result.returncode, 0, result.stdout)
        calls = self.invocations()
        self.assertEqual(len(calls), 6)
        self.assertTrue(all(call[0] == "pack" and "package-stubs" in call[1] for call in calls[:5]))
        self.assertEqual(calls[-1][0], "build")


if __name__ == "__main__":
    unittest.main()
