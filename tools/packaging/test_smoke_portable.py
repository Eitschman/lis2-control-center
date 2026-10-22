"""Gegenproben für beschädigte Archive und Ausführung auf falscher Architektur."""

import hashlib
import io
import json
from pathlib import Path
import tarfile
import tempfile
import subprocess
import unittest
from unittest.mock import patch
import zipfile

from smoke_portable import extract_verified, smoke


class PortableArchiveTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="lis2-archive-tests-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)

    def checksum(self, archive):
        checksum = archive.with_suffix(".sha256")
        checksum.write_text(f"{hashlib.sha256(archive.read_bytes()).hexdigest()}  {archive.name}\n")
        return checksum

    def test_extracts_valid_zip_and_tar(self):
        archive = self.root / "app.zip"
        with zipfile.ZipFile(archive, "w") as package:
            package.writestr("folder/value.txt", "valid")
        extract_verified(archive, self.checksum(archive), self.root / "zip")
        self.assertEqual("valid", (self.root / "zip/folder/value.txt").read_text())
        archive = self.root / "app.tar.gz"
        with tarfile.open(archive, "w:gz") as package:
            item = tarfile.TarInfo("value.txt")
            item.size = 5
            package.addfile(item, io.BytesIO(b"valid"))
        extract_verified(archive, self.checksum(archive), self.root / "tar")
        self.assertEqual("valid", (self.root / "tar/value.txt").read_text())

    def test_rejects_corruption_and_wrong_filename(self):
        archive = self.root / "app.zip"
        archive.write_bytes(b"original")
        checksum = self.checksum(archive)
        archive.write_bytes(b"corrupted")
        with self.assertRaisesRegex(ValueError, "Prüfsumme"):
            extract_verified(archive, checksum, self.root / "bad")
        checksum = self.checksum(archive)
        checksum.write_text(checksum.read_text().replace("app.zip", "other.zip"))
        with self.assertRaisesRegex(ValueError, "Archivname"):
            extract_verified(archive, checksum, self.root / "bad")

    def test_rejects_zip_escape(self):
        for name in ("../escape", "/absolute", "C:/absolute", "..\\escape"):
            with self.subTest(name=name):
                archive = self.root / "app.zip"
                with zipfile.ZipFile(archive, "w") as package:
                    package.writestr(name, "invalid")
                with self.assertRaisesRegex(ValueError, "Unsicherer ZIP-Pfad"):
                    extract_verified(archive, self.checksum(archive), self.root / "bad")

    def test_rejects_tar_escape(self):
        archive = self.root / "app.tar.gz"
        with tarfile.open(archive, "w:gz") as package:
            item = tarfile.TarInfo("../escape")
            package.addfile(item)
        with self.assertRaises(tarfile.FilterError):
            extract_verified(archive, self.checksum(archive), self.root / "bad")

    def test_rejects_wrong_native_architecture(self):
        with patch("smoke_portable.platform.system", return_value="Linux"), \
             patch("smoke_portable.platform.machine", return_value="x86_64"):
            with self.assertRaisesRegex(ValueError, "native Laufzeit"):
                smoke("unused", "unused", "linux-arm64", self.root / "bad")

    def windows_fixture(self):
        archive = self.root / "windows.zip"
        with zipfile.ZipFile(archive, "w") as package:
            package.writestr("LIS2.App.Avalonia.exe", "fixture")
            package.writestr("LIS2.App.Avalonia.runtimeconfig.json", "{}")
        return archive, self.checksum(archive)

    def test_requires_current_success_marker_and_records_failures(self):
        archive, checksum = self.windows_fixture()
        with patch("smoke_portable.platform.system", return_value="Windows"), \
             patch("smoke_portable.platform.machine", return_value="AMD64"), \
             patch("smoke_portable.platform.platform", return_value="test platform"), \
             patch("smoke_portable.subprocess.run"):
            with self.assertRaisesRegex(ValueError, "GUI-Nachweis"):
                smoke(archive, checksum, "win-x64", self.root / "missing-result")
            report = json.loads((self.root / "missing-result/environment.json").read_text())
            self.assertEqual("FAIL", report["result"])
            with self.assertRaises(FileExistsError):
                smoke(archive, checksum, "win-x64", self.root / "missing-result")

    def test_accepts_current_success_and_preserves_native_exit_failure(self):
        archive, checksum = self.windows_fixture()
        def successful_process(command, **kwargs):
            result = Path(command[2]) / "result.txt"
            result.parent.mkdir()
            result.write_text("PASS: isolated native fixture")
        with patch("smoke_portable.platform.system", return_value="Windows"), \
             patch("smoke_portable.platform.machine", return_value="AMD64"), \
             patch("smoke_portable.platform.platform", return_value="test platform"):
            with patch("smoke_portable.subprocess.run", side_effect=successful_process):
                report = smoke(archive, checksum, "win-x64", self.root / "success")
                self.assertEqual("PASS", report["result"])
            with patch("smoke_portable.subprocess.run", side_effect=subprocess.CalledProcessError(3, "fixture")):
                with self.assertRaises(subprocess.CalledProcessError):
                    smoke(archive, checksum, "win-x64", self.root / "native-failure")
                report = json.loads((self.root / "native-failure/environment.json").read_text())
                self.assertEqual("FAIL", report["result"])


if __name__ == "__main__":
    unittest.main()
