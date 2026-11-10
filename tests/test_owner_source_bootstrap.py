"""Non-building fixtures for the public source bootstrap; no real package evidence."""

from __future__ import annotations

from dataclasses import replace
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import sys
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET
import zipfile


ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts/ai/bootstrap-owner-contracts-feed.py"
SPEC = importlib.util.spec_from_file_location("mobile_owner_bootstrap", SCRIPT)
bootstrap = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = bootstrap
SPEC.loader.exec_module(bootstrap)
FIXTURE_LICENSE = b"Non-building unit-test license fixture\n"


class OwnerSourceBootstrapTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory(prefix="mobile-owner-source-test-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.feed = self.root / "feed"
        self.feed.mkdir()
        self.lock_path = ROOT / "eng/package-plane.lock.json"
        self.lock = bootstrap.load_lock(self.lock_path)
        self.lock_sha = bootstrap._sha256(self.lock_path)
        license_patch = patch.object(bootstrap, "HUB_LICENSE_SHA256", hashlib.sha256(FIXTURE_LICENSE).hexdigest())
        license_patch.start()
        self.addCleanup(license_patch.stop)

    def package(self, spec, *, feed=None, commit=None, dependencies=None, assembly=True):
        """A deliberately non-executable ZIP fixture, never a usable assembly."""
        feed = feed or self.feed
        path = feed / f"{spec.package_id}.{self.lock.package_version}.nupkg"
        metadata = ET.Element("package")
        values = ET.SubElement(metadata, "metadata")
        ET.SubElement(values, "id").text = spec.package_id
        ET.SubElement(values, "version").text = self.lock.package_version
        ET.SubElement(values, "repository", type="git", url=spec.repository, commit=commit or spec.commit)
        license_value = bootstrap.EXPECTED_LICENSE_EXPRESSIONS.get(spec.package_id)
        ET.SubElement(values, "license", type="expression" if license_value else "file").text = license_value or "LICENSE"
        deps = ET.SubElement(values, "dependencies")
        for dependency in dependencies if dependencies is not None else bootstrap.EXPECTED_INTERNAL_DEPENDENCIES[spec.package_id]:
            ET.SubElement(deps, "dependency", id=dependency, version=self.lock.package_version)
        with zipfile.ZipFile(path, "w") as archive:
            archive.writestr(f"{spec.package_id}.nuspec", ET.tostring(metadata))
            archive.writestr("LICENSE", FIXTURE_LICENSE)
            if assembly:
                archive.writestr(f"lib/net10.0/{spec.package_id}.dll", b"unit-test fixture, NOT a managed assembly")
        return path

    def inventory(self):
        for spec in self.lock.packages:
            self.package(spec)
        payload = bootstrap._inventory_payload(self.lock, feed=self.feed, lock_sha256=self.lock_sha)
        bootstrap._atomic_write_json(self.feed / bootstrap.INVENTORY_FILE_NAME, payload)
        return payload

    def test_real_lock_and_consumer_pins_are_coherent(self):
        bootstrap.validate_consumer_pins(ROOT / "Directory.Packages.props", self.lock)
        self.assertEqual(len(self.lock.packages), 5)
        self.assertEqual(len({p.repository for p in self.lock.packages}), 3)
        self.assertEqual(self.lock.dotnet_sdk, "10.0.103")

    def test_floating_or_wrong_owner_identity_is_rejected(self):
        original = json.loads(self.lock_path.read_text())
        for key, value in (("commit", "main"), ("repository", "https://github.com/ArchonMegalon/other.git"),
                           ("project", "../../fake.csproj"), ("project_sha256", "unknown")):
            with self.subTest(key=key):
                payload = json.loads(json.dumps(original))
                payload["packages"][0][key] = value
                with self.assertRaises(bootstrap.PackagePlaneError):
                    bootstrap.validate_lock_payload(payload)

    def test_control_contracts_are_feed_only_and_cannot_be_pinned_by_mobile(self):
        self.assertIn("Chummer.Control.Contracts", bootstrap.EXPECTED_PACKAGE_IDS)
        self.assertNotIn("Chummer.Control.Contracts", bootstrap.EXPECTED_CONSUMER_PACKAGE_IDS)
        props = self.root / "Directory.Packages.props"
        tree = ET.parse(ROOT / "Directory.Packages.props")
        ET.SubElement(tree.getroot().find("ItemGroup"), "PackageVersion",
                      Include="Chummer.Control.Contracts", Version=self.lock.package_version)
        tree.write(props)
        with self.assertRaisesRegex(bootstrap.PackagePlaneError, "must not pin Control"):
            bootstrap.validate_consumer_pins(props, self.lock)

    def test_pin_drift_is_rejected(self):
        props = self.root / "Directory.Packages.props"
        props.write_text((ROOT / "Directory.Packages.props").read_text().replace(self.lock.package_version, "0.1.0-preview", 1))
        with self.assertRaisesRegex(bootstrap.PackagePlaneError, "consumer pin"):
            bootstrap.validate_consumer_pins(props, self.lock)

    def test_sparse_checkout_retains_all_owner_projects_and_build_metadata(self):
        origin = self.root / "public-owner-fixture"
        origin.mkdir()
        projects = tuple(p.project for p in self.lock.packages
                         if p.checkout_directory == "chummer6-hub")
        for project in projects:
            path = origin / project
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("<Project />\n")
        (origin / "Directory.Build.props").write_text("<Project />\n")
        (origin / ".gitattributes").write_text("*.csproj text eol=crlf\n")
        (origin / "media").mkdir()
        (origin / "media/unrelated.bin").write_bytes(b"not needed by contracts" * 100)
        bootstrap._run(("git", "init", str(origin)))
        bootstrap._run(("git", "config", "uploadpack.allowFilter", "true"), cwd=origin)
        bootstrap._run(("git", "add", "."), cwd=origin)
        bootstrap._run(("git", "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
                        "commit", "-m", "Local sparse-source fixture"), cwd=origin)
        commit = bootstrap._run(("git", "rev-parse", "HEAD"), cwd=origin).strip()
        spec = replace(self.lock.packages[1], repository=origin.as_uri(), commit=commit)
        checkout = bootstrap.ensure_exact_checkout(self.root / "source-cache", spec, projects=projects)
        for project in projects:
            self.assertEqual((checkout / project).read_bytes(), (origin / project).read_bytes())
            self.assertNotIn(b"\r\n", (checkout / project).read_bytes())
        self.assertTrue((checkout / "Directory.Build.props").is_file())
        self.assertFalse((checkout / "media").exists())
        self.assertEqual(bootstrap._run(("git", "config", "remote.origin.partialclonefilter"), cwd=checkout).strip(), "blob:none")
        self.assertEqual(bootstrap._run(("git", "rev-parse", "HEAD"), cwd=checkout).strip(), commit)

    def test_sparse_project_paths_cannot_escape_or_select_whole_repository(self):
        for projects in (("../Other.csproj",), ("Root.csproj",)):
            with self.subTest(projects=projects), patch.object(bootstrap, "_run") as run:
                with self.assertRaises(bootstrap.PackagePlaneError):
                    bootstrap.ensure_exact_checkout(self.root / "source-cache", self.lock.packages[0], projects=projects)
                run.assert_not_called()

    def test_inventory_binds_bytes_source_lock_and_recipe(self):
        original = self.inventory()
        bootstrap.validate_feed_inventory(self.feed, self.lock, self.lock_sha)
        path = self.feed / bootstrap.INVENTORY_FILE_NAME
        for key in ("package_plane_lock_sha256", "bootstrap_sha256"):
            changed = dict(original, **{key: "0" * 64})
            path.write_text(json.dumps(changed))
            with self.assertRaises(bootstrap.PackagePlaneError):
                bootstrap.validate_feed_inventory(self.feed, self.lock, self.lock_sha)
        path.write_text(json.dumps(original))
        package = bootstrap.package_path(self.feed, self.lock.packages[0].package_id, self.lock.package_version)
        with package.open("ab") as stream:
            stream.write(b"tampered")
        with self.assertRaises(bootstrap.PackagePlaneError):
            bootstrap.validate_feed_inventory(self.feed, self.lock, self.lock_sha)

    def test_incompatible_package_shape_is_rejected(self):
        for options in ({"commit": "0" * 40}, {"dependencies": ["Unexpected.Library"]}, {"assembly": False}):
            with self.subTest(options=options):
                spec = self.lock.packages[0]
                self.package(spec, **options)
                with self.assertRaises(bootstrap.PackagePlaneError):
                    bootstrap.validate_package(self.feed, spec, self.lock.package_version)
        campaign = self.lock.packages[1]
        self.package(campaign, dependencies=[])
        with self.assertRaisesRegex(bootstrap.PackagePlaneError, "dependency drift"):
            bootstrap.validate_package(self.feed, campaign, self.lock.package_version)

    def test_extra_package_and_linked_package_are_rejected(self):
        self.inventory()
        extra = self.feed / "extra.nupkg"
        extra.write_bytes(b"unexpected")
        with self.assertRaises(bootstrap.PackagePlaneError):
            bootstrap.validate_feed_inventory(self.feed, self.lock, self.lock_sha)
        extra.unlink()
        target = bootstrap.package_path(self.feed, self.lock.packages[0].package_id, self.lock.package_version)
        elsewhere = self.root / "elsewhere.nupkg"
        target.rename(elsewhere)
        target.symlink_to(elsewhere)
        with self.assertRaises(bootstrap.PackagePlaneError):
            bootstrap.validate_feed_inventory(self.feed, self.lock, self.lock_sha)

    def test_only_mismatched_exact_cache_version_is_repaired(self):
        self.inventory()
        spec = self.lock.packages[0]
        cache = self.root / "nuget-cache"
        cached = cache / spec.package_id.lower() / self.lock.package_version.lower()
        cached.mkdir(parents=True)
        (cached / "sentinel").write_text("old scaffold")
        other = cached.parent / "unrelated-version"
        other.mkdir()
        with self.assertRaisesRegex(bootstrap.PackagePlaneError, "consumer cache"):
            bootstrap.check_consumer_cache(cache, self.feed, self.lock, repair=False)
        self.assertTrue(cached.exists())
        bootstrap.check_consumer_cache(cache, self.feed, self.lock, repair=True)
        self.assertFalse(cached.exists())
        self.assertTrue(other.exists())

    def test_restore_mapping_reserves_chummer_namespace_for_local_owners(self):
        config = self.root / "NuGet.Config"
        bootstrap.write_restore_config(config, self.feed, self.lock.approved_remote_source)
        root = ET.parse(config).getroot()
        self.assertEqual(root.find("./packageSourceMapping/packageSource[@key='owners']/package").get("pattern"), "Chummer.*")
        self.assertEqual(root.find("./packageSourceMapping/packageSource[@key='nuget']/package").get("pattern"), "*")
        args = bootstrap._owner_pack_restore_args(self.lock, staged_feed=self.feed)
        self.assertIn("-p:ChummerUseLocalCompatibilityTree=false", args)
        self.assertIn("-p:RestoreIgnoreFailedSources=false", args)
        self.assertIn("-p:RuntimeIdentifiers=", args)
        self.assertIn("-p:ImportDirectoryPackagesProps=false", args)

    def test_intact_archive_does_not_hide_altered_expanded_assembly(self):
        self.inventory()
        spec = self.lock.packages[0]
        cache = self.root / "nuget-cache"
        directory = cache / spec.package_id.lower() / self.lock.package_version.lower()
        directory.mkdir(parents=True)
        package = bootstrap.package_path(self.feed, spec.package_id, self.lock.package_version)
        shutil.copy2(package, directory / package.name.lower())
        with zipfile.ZipFile(package) as archive:
            archive.extractall(directory)  # Test-created fixture members only.
        bootstrap.check_consumer_cache(cache, self.feed, self.lock, repair=False)
        (directory / f"lib/net10.0/{spec.package_id}.dll").write_bytes(b"changed fixture")
        with self.assertRaisesRegex(bootstrap.PackagePlaneError, "consumer cache"):
            bootstrap.check_consumer_cache(cache, self.feed, self.lock, repair=False)

    def test_project_digest_failure_precedes_any_pack_or_feed_promotion(self):
        workspace = self.root / "sources"
        def checkout(workspace, spec, *, projects):
            project = workspace / spec.checkout_directory / spec.project
            project.parent.mkdir(parents=True, exist_ok=True)
            project.write_text("altered project")
        with patch.object(bootstrap, "ensure_exact_checkout", side_effect=checkout), patch.object(bootstrap, "_run") as run:
            with self.assertRaisesRegex(bootstrap.PackagePlaneError, "digest mismatch"):
                bootstrap.build_feed(self.lock, lock_sha256=self.lock_sha, feed=self.feed,
                                     workspace=workspace, package_root=self.root / "packages", dotnet="must-not-run")
            run.assert_not_called()
        self.assertFalse((self.feed / bootstrap.INVENTORY_FILE_NAME).exists())

    def test_mock_build_packs_all_actual_owner_project_identities_in_order(self):
        project_bytes = b"<Project />\n"  # Mock checkout only; no compiler runs.
        lock = replace(self.lock, packages=tuple(replace(p, project_sha256=hashlib.sha256(project_bytes).hexdigest()) for p in self.lock.packages))
        calls = []
        def checkout(workspace, spec, *, projects):
            for row in lock.packages:
                if row.repository == spec.repository:
                    p = workspace / row.checkout_directory / row.project
                    p.parent.mkdir(parents=True, exist_ok=True)
                    p.write_bytes(project_bytes)
        def run(command, **kwargs):
            if command[1] == "--version":
                return lock.dotnet_sdk
            calls.append(command)
            spec = next(p for p in lock.packages if str(command[2]).endswith(p.project))
            self.package(spec, feed=Path(command[command.index("--output") + 1]))
            return "non-building fixture\n"
        with patch.object(bootstrap, "ensure_exact_checkout", side_effect=checkout), patch.object(bootstrap, "_run", side_effect=run), patch.object(bootstrap, "canonicalize_nupkg"):
            bootstrap.build_feed(lock, lock_sha256=self.lock_sha, feed=self.feed,
                                 workspace=self.root / "sources", package_root=self.root / "packages", dotnet="fixture-only")
        self.assertEqual([Path(c[2]).name for c in calls], [Path(p.project).name for p in lock.packages])
        for call in calls:
            self.assertIn("-m:1", call)
            self.assertIn("-p:BuildInParallel=false", call)
            self.assertIn(f"-p:PackageVersion={lock.package_version}", call)
            self.assertTrue(any(a.startswith("-p:RestoreConfigFile=") for a in call))


if __name__ == "__main__":
    unittest.main()
