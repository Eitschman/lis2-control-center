"""Baut ein Debian-Paket aus dem unveränderten selbstständigen Linux-Publish."""

import argparse
import base64
import hashlib
from pathlib import Path
import re
import shutil
import struct
import subprocess
import tempfile


ARCHITECTURES = {"linux-x64": ("amd64", 62), "linux-arm64": ("arm64", 183)}
PACKAGE = "lis2-control-center"
EXECUTABLE = "LIS2.App.Avalonia"
REPOSITORY = Path(__file__).resolve().parents[2]


def build_package(publish, rid, version, output):
    """Prüft die Eingaben und erzeugt Paket sowie separat überprüfbare Prüfsumme."""
    if rid not in ARCHITECTURES:
        raise ValueError("Unterstützt werden linux-x64 und linux-arm64.")
    if not re.fullmatch(r"[0-9][A-Za-z0-9.+~]*", version):
        raise ValueError("Ungültige Debian-Paketversion.")
    subprocess.run(["dpkg", "--validate-version", version], check=True)
    architecture, machine = ARCHITECTURES[rid]
    publish = Path(publish).resolve()
    for name in (EXECUTABLE, EXECUTABLE + ".dll", EXECUTABLE + ".runtimeconfig.json",
                 "libcoreclr.so", "libhostfxr.so", "libSkiaSharp.so"):
        if not (publish / name).is_file():
            raise ValueError(f"Fehlende Publish-Datei: {name}")
    with (publish / EXECUTABLE).open("rb") as executable:
        header = executable.read(20)
    if (len(header) != 20 or header[:6] != b"\x7fELF\x02\x01"
            or struct.unpack_from("<H", header, 18)[0] != machine):
        raise ValueError("ELF-Architektur stimmt nicht mit dem Paket überein.")
    if not (publish / EXECUTABLE).stat().st_mode & 0o111:
        raise ValueError("Die veröffentlichte Anwendung ist nicht ausführbar.")
    icon_source = (REPOSITORY / "src/LIS2.App.Avalonia/App.axaml.cs").read_text(encoding="utf-8")
    match = re.search(r'var iconBytes = Convert.FromBase64String\("([A-Za-z0-9+/=]+)"\)', icon_source)
    if not match:
        raise ValueError("Das Anwendungssymbol wurde nicht gefunden.")
    icon = base64.b64decode(match[1], validate=True)
    if icon[:8] != b"\x89PNG\r\n\x1a\n" or struct.unpack_from(">II", icon, 16) != (32, 32):
        raise ValueError("Das Anwendungssymbol muss ein 32×32-PNG sein.")
    output = Path(output).resolve()
    output.mkdir(parents=True, exist_ok=True)
    destination = output / f"{PACKAGE}_{version}_{architecture}.deb"
    if destination.exists() or destination.with_suffix(".deb.sha256").exists():
        raise ValueError("Paket oder Prüfsumme existiert bereits.")
    with tempfile.TemporaryDirectory(prefix="lis2-debian-") as temporary:
        root = Path(temporary) / "package"
        application = root / "usr/lib" / PACKAGE
        application.parent.mkdir(parents=True)
        shutil.copytree(publish, application, symlinks=True)
        binary = root / "usr/bin"
        binary.mkdir()
        (binary / PACKAGE).symlink_to(f"/usr/lib/{PACKAGE}/{EXECUTABLE}")
        desktop = root / "usr/share/applications"
        desktop.mkdir(parents=True)
        (desktop / f"{PACKAGE}.desktop").write_text(
            "[Desktop Entry]\nType=Application\nName=LIS2 Control Center\n"
            f"Exec={PACKAGE}\nTryExec={PACKAGE}\nIcon={PACKAGE}\n"
            "Terminal=false\nCategories=Utility;\n", encoding="utf-8")
        icons = root / "usr/share/icons/hicolor/32x32/apps"
        icons.mkdir(parents=True)
        (icons / f"{PACKAGE}.png").write_bytes(icon)
        documentation = root / "usr/share/doc" / PACKAGE
        documentation.mkdir(parents=True)
        shutil.copyfile(REPOSITORY / "LICENSE", documentation / "copyright")
        (documentation / "README.Debian").write_text(
            "Start über das Anwendungsmenü oder lis2-control-center.\n"
            "Die .NET-Laufzeit wird mitgeliefert. Einstellungen und Tokens liegen im Benutzerprofil.\n"
            "Ein vorhandener Desktop-Schlüsselbund wird über secret-tool verwendet.\n"
            "Serielle Zugriffsrechte müssen zur Distribution passend eingerichtet werden.\n"
            "Das Paket verändert keine Gruppenmitgliedschaften und aktiviert keinen Autostart.\n",
            encoding="utf-8")
        control = root / "DEBIAN"
        control.mkdir()
        installed_size = (sum(path.stat().st_size for path in root.rglob("*")
                              if path.is_file() and not path.is_symlink()) + 1023) // 1024
        (control / "control").write_text(
            f"Package: {PACKAGE}\nVersion: {version}\nArchitecture: {architecture}\n"
            "Maintainer: Andreas Haarmann-Thiemann <aht@mggm-software.de>\n"
            "Section: utils\nPriority: optional\n"
            f"Installed-Size: {installed_size}\n"
            "Depends: libc6 (>= 2.36), libgcc-s1, libstdc++6, libgssapi-krb5-2, "
            "libicu76 | libicu74 | libicu72, libssl3t64 | libssl3, ca-certificates, tzdata, "
            "libx11-6, libice6, libsm6, libfontconfig1\n"
            "Recommends: libsecret-tools, playerctl, pipewire-bin, dbus-bin, xdg-utils\n"
            "Homepage: https://github.com/Eitschman/lis2-control-center\n"
            "Description: LIS2 hardware and display control center\n"
            " Avalonia desktop application for the VL System L.I.S. 2 display and fans.\n",
            encoding="utf-8")
        subprocess.run(["dpkg-deb", "--root-owner-group", "--build", str(root), str(destination)], check=True)
    checksum = hashlib.sha256(destination.read_bytes()).hexdigest()
    destination.with_suffix(".deb.sha256").write_text(f"{checksum}  {destination.name}\n", encoding="ascii")
    return destination


def main():
    """Stellt denselben Paketbau lokal und im GitHub-Workflow bereit."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--publish", required=True, type=Path)
    parser.add_argument("--rid", required=True, choices=ARCHITECTURES)
    parser.add_argument("--version", required=True)
    parser.add_argument("--output", required=True, type=Path)
    arguments = parser.parse_args()
    print(build_package(arguments.publish, arguments.rid, arguments.version, arguments.output))


if __name__ == "__main__":
    main()
