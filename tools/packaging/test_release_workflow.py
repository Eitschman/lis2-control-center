"""Prüft die tatsächliche Release-Assemblierung mit isolierten Paket-Fixtures."""

import hashlib
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import textwrap
import unittest
import zipfile


WORKFLOW = Path(__file__).resolve().parents[2] / ".github/workflows/release.yml"
SOURCE = textwrap.dedent(WORKFLOW.read_text().split("          python3 - <<'PYTHON'\n", 1)[1].split("          PYTHON", 1)[0])


class ReleaseWorkflowTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="lis2-release-test-")
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name)
        for rid in ("win-x64", "linux-x64", "linux-arm64"):
            suffix = ".zip" if rid == "win-x64" else ".tar.gz"
            self.write(f"LIS2-Avalonia-{rid}/LIS2-Avalonia-{rid}{suffix}", b"app archive")
        for arch, debian in (("x64", "amd64"), ("arm64", "arm64")):
            self.write(f"LIS2-Debian-linux-{arch}/lis2-control-center_0.2.0_{debian}.deb", b"debian package")
        self.write("LIS2-ProtocolTester-win-x64/LIS2.ProtocolTester.exe", b"protocol tester")
        self.write("gen_lis2-win32/gen_lis2.dll", b"winamp plugin")
        for architecture in ("win32", "win64"):
            self.write(f"LIS2-WMP-Legacy-{architecture}/LIS2-WMP-Legacy-{architecture}.zip", architecture.encode())

    def write(self, name, content):
        path = self.directory / "artifacts" / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(content)

    def assemble(self):
        return subprocess.run([sys.executable, "-c", SOURCE], cwd=self.directory,
                              env={**os.environ, "RELEASE_VERSION": "0.2.0"}, capture_output=True, text=True)

    def test_both_legacy_architectures_and_all_existing_packages_are_hashed(self):
        result = self.assemble()
        self.assertEqual(0, result.returncode, result.stderr)
        output = self.directory / "release"
        entries = (output / "SHA256SUMS.txt").read_text().splitlines()
        self.assertEqual(9, len(entries))
        for entry in entries:
            digest, name = entry.split("  ")
            self.assertEqual(digest, hashlib.sha256((output / name).read_bytes()).hexdigest())
        for architecture in ("win32", "win64"):
            self.assertEqual(architecture.encode(), (output / f"LIS2-WMP-Legacy-0.2.0-{architecture}.zip").read_bytes())
        with zipfile.ZipFile(output / "LIS2-ProtocolTester-0.2.0-win-x64.zip") as archive:
            self.assertEqual(b"protocol tester", archive.read("LIS2.ProtocolTester.exe"))

    def test_missing_legacy_architecture_stops_release_assembly(self):
        (self.directory / "artifacts/LIS2-WMP-Legacy-win64/LIS2-WMP-Legacy-win64.zip").unlink()
        result = self.assemble()
        self.assertNotEqual(0, result.returncode)
        self.assertIn("LIS2-WMP-Legacy-win64.zip", result.stderr)
        self.assertFalse((self.directory / "release/SHA256SUMS.txt").exists())


if __name__ == "__main__":
    unittest.main()
