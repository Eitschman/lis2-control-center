"""Prüft Lizenzbeilagen und die ausdrückliche Ablehnung fehlender Nachweise."""

import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import bundle_licenses


class BundleLicenseTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="lis2-license-test-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.publish = self.root / "publish"
        self.publish.mkdir()
        self.cache = self.root / "cache"
        self.assets = self.root / "assets.json"
        self.assets.write_text(json.dumps({"libraries": {"Example/1.0": {"type": "package", "path": "example/1.0"}},
                                          "project": {"frameworks": {"net10.0": {}}}}))
        (self.publish / "app.runtimeconfig.json").write_text(json.dumps({"runtimeOptions": {"includedFrameworks": [
            {"name": "Microsoft.NETCore.App", "version": "10.0.13"}]}}))
        self.package = self.make_package("example/1.0")
        self.make_package("microsoft.netcore.app.runtime.win-x64/10.0.13")
        self.vendor = self.root / "vendor"
        self.vendor.mkdir()
        text = b"Original license text"
        (self.vendor / "dotnet.txt").write_bytes(text)
        (self.vendor / "sources.json").write_text(json.dumps({"dotnet.txt": {
            "url": "https://example.test/license", "sha256": hashlib.sha256(text).hexdigest()}}))
        self.mock_vendor = patch.object(bundle_licenses, "VENDORED", self.vendor)
        self.mock_vendor.start()
        self.addCleanup(self.mock_vendor.stop)

    def make_package(self, relative):
        directory = self.cache / relative
        directory.mkdir(parents=True)
        (directory / "package.nuspec").write_text('<package><metadata><license type="expression">MIT</license></metadata></package>')
        (directory / "LICENSE.txt").write_text("Original package license")
        (directory / "THIRD-PARTY-NOTICES.txt").write_text("Original dependency notice")
        return directory

    def run_bundle(self):
        return bundle_licenses.bundle(self.publish, [self.assets], self.cache, "win-x64")

    def test_valid_package_preserves_original_notices_and_metadata(self):
        entries = self.run_bundle()
        self.assertEqual(2, len(entries))
        self.assertEqual("Original dependency notice", (self.publish / "licenses/example/1.0/THIRD-PARTY-NOTICES.txt").read_text())
        self.assertTrue((self.publish / "LICENSE").is_file())
        self.assertTrue((self.publish / "licenses/manifest.json").is_file())

    def test_missing_metadata_is_rejected(self):
        (self.package / "package.nuspec").unlink()
        with self.assertRaisesRegex(ValueError, "Fehlende Paketmetadaten"):
            self.run_bundle()

    def test_unknown_license_is_rejected(self):
        (self.package / "package.nuspec").write_text('<package><metadata><license type="expression">Unknown</license></metadata></package>')
        with self.assertRaisesRegex(ValueError, "Ungeklärte Lizenzangabe"):
            self.run_bundle()

    def test_missing_declared_license_file_is_rejected(self):
        (self.package / "package.nuspec").write_text('<package><metadata><license type="file">missing.txt</license></metadata></package>')
        with self.assertRaisesRegex(ValueError, "Fehlender deklarierter Lizenztext"):
            self.run_bundle()

    def test_missing_mpl_source_reference_is_rejected(self):
        (self.package / "package.nuspec").write_text('<package><metadata><license type="expression">MPL-2.0</license></metadata></package>')
        with self.assertRaisesRegex(ValueError, "Fehlender MPL-Quellverweis"):
            self.run_bundle()

    def test_modified_vendored_license_is_rejected(self):
        (self.vendor / "dotnet.txt").write_text("Changed")
        with self.assertRaisesRegex(ValueError, "Nachweis"):
            self.run_bundle()

    def test_missing_runtime_package_is_rejected(self):
        (self.cache / "microsoft.netcore.app.runtime.win-x64/10.0.13/package.nuspec").unlink()
        with self.assertRaisesRegex(ValueError, "Fehlende Paketmetadaten"):
            self.run_bundle()

    def test_mpl_package_keeps_immutable_source_archive(self):
        (self.package / "package.nuspec").write_text(
            '<package><metadata><license type="expression">MPL-2.0</license>'
            '<repository url="https://github.com/example/library.git" commit="abcdef" />'
            '</metadata></package>')
        (self.vendor / "MPL-2.0.txt").write_bytes(b"MPL license")
        origins = json.loads((self.vendor / "sources.json").read_text())
        origins["MPL-2.0.txt"] = {"url": "https://example.test/mpl", "sha256": hashlib.sha256(b"MPL license").hexdigest()}
        (self.vendor / "sources.json").write_text(json.dumps(origins))
        entry = next(item for item in self.run_bundle() if item["package"] == "Example/1.0")
        self.assertEqual("https://github.com/example/library/archive/abcdef.tar.gz", entry["source"])

    def test_download_dependency_is_included(self):
        data = json.loads(self.assets.read_text())
        data["project"]["frameworks"]["net10.0"]["downloadDependencies"] = [
            {"name": "Downloaded.Package", "version": "[1.2, 1.2]"}]
        self.assets.write_text(json.dumps(data))
        self.make_package("downloaded.package/1.2")
        self.assertIn("Downloaded.Package/1.2", [item["package"] for item in self.run_bundle()])

    def test_inexact_download_dependency_is_rejected(self):
        data = json.loads(self.assets.read_text())
        data["project"]["frameworks"]["net10.0"]["downloadDependencies"] = [
            {"name": "Downloaded.Package", "version": "[1.2, 2.0]"}]
        self.assets.write_text(json.dumps(data))
        with self.assertRaisesRegex(ValueError, "exakte Version"):
            self.run_bundle()

    def test_missing_supplemental_license_is_rejected(self):
        (self.package / "package.nuspec").write_text(
            '<package><metadata><license type="expression">MPL-2.0</license>'
            '<repository url="https://example.test/source" commit="abcdef" /></metadata></package>')
        with self.assertRaisesRegex(ValueError, "Fehlender ergänzender Lizenztext"):
            self.run_bundle()
