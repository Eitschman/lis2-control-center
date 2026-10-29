"""Regressionen für Paketinhalt, Architektur und abgelehnte Publish-Eingaben."""

import hashlib
from pathlib import Path
import shutil
import struct
import subprocess
import tempfile
import unittest
from unittest.mock import patch

from build_debian import ARCHITECTURES, EXECUTABLE, PACKAGE, build_package


class DebianPackageTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="lis2-package-tests-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.publish = self.root / "publish"
        self.publish.mkdir()
        # Ein ausführbares Fixture genügt für Metadatenprüfungen; Laufzeitprüfungen verwenden das echte Publish.
        shutil.copy2("/bin/true", self.publish / EXECUTABLE)
        # Das Fixture bezeichnet immer x64, unabhängig von der Architektur des Testrechners.
        executable = self.publish / EXECUTABLE
        payload = bytearray(executable.read_bytes())
        struct.pack_into("<H", payload, 18, ARCHITECTURES["linux-x64"][1])
        executable.write_bytes(payload)
        for name in (EXECUTABLE + ".dll", EXECUTABLE + ".runtimeconfig.json",
                     "libcoreclr.so", "libhostfxr.so", "libSkiaSharp.so"):
            (self.publish / name).write_text("fixture", encoding="utf-8")
        self.output = self.root / "output"

    def test_both_architectures_and_installed_content(self):
        for rid, (architecture, machine) in ARCHITECTURES.items():
            with self.subTest(rid=rid):
                executable = self.publish / EXECUTABLE
                payload = bytearray(executable.read_bytes())
                struct.pack_into("<H", payload, 18, machine)
                executable.write_bytes(payload)
                package = build_package(self.publish, rid, "0.0~ci1.1", self.output)
                fields = subprocess.check_output(["dpkg-deb", "--field", str(package)], text=True)
                self.assertIn(f"Architecture: {architecture}\n", fields)
                self.assertIn("Recommends: libsecret-tools, playerctl, pipewire-bin, dbus-bin, xdg-utils", fields)
                self.assertIn("libicu76 | libicu74 | libicu72", fields)
                listing = subprocess.check_output(["dpkg-deb", "--contents", str(package)], text=True)
                self.assertIn("root/root", listing)
                self.assertIn(f"./usr/lib/{PACKAGE}/{EXECUTABLE}", listing)
                extracted = self.root / architecture
                subprocess.run(["dpkg-deb", "--extract", str(package), str(extracted)], check=True)
                self.assertEqual(f"/usr/lib/{PACKAGE}/{EXECUTABLE}",
                                 str((extracted / "usr/bin" / PACKAGE).readlink()))
                self.assertTrue((extracted / "usr/lib" / PACKAGE / EXECUTABLE).stat().st_mode & 0o111)
                self.assertTrue((extracted / "usr/share/doc" / PACKAGE / "copyright").is_file())
                desktop = extracted / "usr/share/applications" / f"{PACKAGE}.desktop"
                self.assertIn(f"Exec={PACKAGE}\n", desktop.read_text())
                checksum = package.with_suffix(".deb.sha256").read_text().split()[0]
                self.assertEqual(hashlib.sha256(package.read_bytes()).hexdigest(), checksum)
                with self.assertRaisesRegex(ValueError, "existiert bereits"):
                    build_package(self.publish, rid, "0.0~ci1.1", self.output)

    def test_rejects_wrong_architecture_and_non_elf(self):
        with self.assertRaisesRegex(ValueError, "ELF-Architektur"):
            build_package(self.publish, "linux-arm64", "1.0", self.output)
        (self.publish / EXECUTABLE).write_bytes(b"not an executable")
        with self.assertRaisesRegex(ValueError, "ELF-Architektur"):
            build_package(self.publish, "linux-x64", "1.0", self.output)

    def test_rejects_incomplete_publish(self):
        for name in (EXECUTABLE, EXECUTABLE + ".dll", EXECUTABLE + ".runtimeconfig.json",
                     "libcoreclr.so", "libhostfxr.so", "libSkiaSharp.so"):
            with self.subTest(name=name):
                path = self.publish / name
                saved = self.root / name
                path.rename(saved)
                with self.assertRaisesRegex(ValueError, "Fehlende Publish-Datei"):
                    build_package(self.publish, "linux-x64", "1.0", self.output)
                saved.rename(path)

    def test_rejects_invalid_version_and_rid(self):
        for version in ("", "release", "1\nDepends: bad", "../1", "1-unsafe"):
            with self.subTest(version=version), self.assertRaisesRegex(ValueError, "Paketversion"):
                build_package(self.publish, "linux-x64", version, self.output)
        with self.assertRaisesRegex(ValueError, "Unterstützt"):
            build_package(self.publish, "win-x64", "1.0", self.output)

    def test_rejects_non_executable_publish(self):
        executable = self.publish / EXECUTABLE
        executable.rename(self.root / "saved-executable")
        executable.write_bytes((self.root / "saved-executable").read_bytes())
        with self.assertRaisesRegex(ValueError, "nicht ausführbar"):
            build_package(self.publish, "linux-x64", "1.0", self.output)

    def test_rejects_missing_and_invalid_application_icon(self):
        source = self.root / "src/LIS2.App.Avalonia/App.axaml.cs"
        source.parent.mkdir(parents=True)
        with patch("build_debian.REPOSITORY", self.root):
            source.write_text("no icon", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "nicht gefunden"):
                build_package(self.publish, "linux-x64", "1.0", self.output)
            source.write_text('var iconBytes = Convert.FromBase64String("bm90IGEgcG5n");', encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "32×32-PNG"):
                build_package(self.publish, "linux-x64", "1.0", self.output)


if __name__ == "__main__":
    unittest.main()
