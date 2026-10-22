"""Prüft Prüfsumme, sichere Extraktion und nativen Start eines portablen Pakets."""

import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import platform
import subprocess
import tarfile
import tempfile
import zipfile


def extract_verified(archive, checksum, destination):
    """Extrahiert nur das zur Prüfsumme gehörende Archiv ohne externe Schreibziele."""
    archive, checksum, destination = Path(archive), Path(checksum), Path(destination)
    fields = checksum.read_text(encoding="utf-8").strip().split()
    digest = hashlib.sha256(archive.read_bytes()).hexdigest()
    if len(fields) != 2 or fields[0].lower() != digest or fields[1].lstrip("*") != archive.name:
        raise ValueError("Prüfsumme oder Archivname stimmt nicht überein.")
    if archive.name.endswith(".zip"):
        with zipfile.ZipFile(archive) as package:
            for member in package.infolist():
                path = PurePosixPath(member.filename.replace("\\", "/"))
                if path.is_absolute() or ".." in path.parts or ":" in member.filename:
                    raise ValueError("Unsicherer ZIP-Pfad.")
            package.extractall(destination)
    elif archive.name.endswith(".tar.gz"):
        with tarfile.open(archive) as package:
            package.extractall(destination, filter="data")
    else:
        raise ValueError("Unbekanntes Archivformat.")
    return digest


def smoke(archive, checksum, rid, output):
    """Startet ausschließlich den isolierten GUI-Smoke mit gesperrtem seriellen Zugriff."""
    expected = {"linux-x64": ("Linux", ("x86_64", "amd64")),
                "linux-arm64": ("Linux", ("aarch64", "arm64")),
                "win-x64": ("Windows", ("amd64", "x86_64"))}
    operating_system, architectures = expected[rid]
    if platform.system() != operating_system or platform.machine().lower() not in architectures:
        raise ValueError(f"Paket {rid} benötigt eine passende native Laufzeit.")
    output = Path(output).resolve()
    output.mkdir(parents=True, exist_ok=False)
    report = {"rid": rid, "system": platform.system(), "machine": platform.machine(),
              "platform": platform.platform(), "session_type": os.environ.get("XDG_SESSION_TYPE"),
              "display": bool(os.environ.get("DISPLAY")),
              "wayland_display": bool(os.environ.get("WAYLAND_DISPLAY")),
              "dbus_session": bool(os.environ.get("DBUS_SESSION_BUS_ADDRESS"))}
    try:
        with tempfile.TemporaryDirectory(prefix="lis2-portable-") as temporary:
            unpacked = Path(temporary)
            report["sha256"] = extract_verified(archive, checksum, unpacked)
            executable = unpacked / ("LIS2.App.Avalonia.exe" if rid == "win-x64" else "LIS2.App.Avalonia")
            report["runtime"] = json.loads((unpacked / "LIS2.App.Avalonia.runtimeconfig.json").read_text())
            if operating_system == "Linux":
                report["os_release"] = Path("/etc/os-release").read_text()
                dependencies = {}
                for name in (executable.name, "libcoreclr.so", "libSkiaSharp.so"):
                    result = subprocess.run(["ldd", str(unpacked / name)], capture_output=True, text=True, check=True)
                    dependencies[name] = result.stdout
                    if "not found" in result.stdout:
                        raise ValueError(f"Fehlende native Bibliothek für {name}.")
                report["native_dependencies"] = dependencies
            with (output / "process.log").open("w", encoding="utf-8") as log:
                subprocess.run([str(executable), "--desktop-smoke", str(output / "desktop"), "--minimized"],
                               cwd=unpacked, stdout=log, stderr=subprocess.STDOUT, timeout=90, check=True)
            result = output / "desktop/result.txt"
            if not result.is_file() or not result.read_text().startswith("PASS:"):
                raise ValueError("Der Paketstart hat keinen erfolgreichen GUI-Nachweis erzeugt.")
            report["result"] = "PASS"
    except Exception as error:
        report["result"] = "FAIL"
        report["error"] = str(error)
        raise
    finally:
        (output / "environment.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    return report


def main():
    """Verwendet lokal und in CI dieselbe Prüfung des veröffentlichten Pakets."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--archive", required=True, type=Path)
    parser.add_argument("--checksum", required=True, type=Path)
    parser.add_argument("--rid", required=True, choices=("linux-x64", "linux-arm64", "win-x64"))
    parser.add_argument("--output", required=True, type=Path)
    arguments = parser.parse_args()
    report = smoke(arguments.archive, arguments.checksum, arguments.rid, arguments.output)
    print(f"{report['result']}: {report['rid']} on {report['system']}/{report['machine']}; {arguments.output}")


if __name__ == "__main__":
    main()
