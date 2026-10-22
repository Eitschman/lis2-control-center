"""Legt Paketmetadaten, Lizenztexte und Quellverweise dem selbstständigen Publish bei."""

import argparse
import hashlib
import json
from pathlib import Path
import shutil
import xml.etree.ElementTree as ElementTree

VENDORED = Path(__file__).with_name("licenses")


def bundle(publish, assets_files, cache, rid):
    """Bricht bei fehlenden Paketdateien oder ungeklärten Lizenzangaben ausdrücklich ab."""
    publish, cache = Path(publish), Path(cache)
    packages = {}
    for filename in assets_files:
        assets = json.loads(Path(filename).read_text(encoding="utf-8-sig"))
        for identity, package in assets["libraries"].items():
            if package["type"] == "package":
                packages[identity] = package["path"]
        for framework in assets["project"]["frameworks"].values():
            for dependency in framework.get("downloadDependencies", []):
                bounds = dependency["version"].strip("[]").split(",")
                if len(bounds) != 2 or bounds[0].strip() != bounds[1].strip():
                    raise ValueError("Download-Abhängigkeit benötigt eine exakte Version")
                version = bounds[0].strip()
                identity = dependency["name"] + "/" + version
                packages[identity] = identity.lower()
    configuration = next(publish.glob("*.runtimeconfig.json"))
    runtime = json.loads(configuration.read_text())["runtimeOptions"]["includedFrameworks"]
    version = next(item["version"] for item in runtime if item["name"] == "Microsoft.NETCore.App")
    identity = "Microsoft.NETCore.App.Runtime." + rid + "/" + version
    packages[identity] = identity.lower()
    destination = publish / "licenses"
    destination.mkdir(exist_ok=False)
    origins = json.loads((VENDORED / "sources.json").read_text(encoding="utf-8"))
    for name, origin in origins.items():
        content = (VENDORED / name).read_bytes()
        if hashlib.sha256(content).hexdigest() != origin["sha256"]:
            raise ValueError("Lizenztext stimmt nicht mit dem hinterlegten Nachweis überein: " + name)
        shutil.copyfile(VENDORED / name, destination / name)
    entries = []
    for identity, relative in sorted(packages.items()):
        package = cache / relative
        specification = next(package.glob("*.nuspec"), None)
        if specification is None:
            raise ValueError("Fehlende Paketmetadaten: " + identity)
        metadata = next(iter(ElementTree.parse(specification).getroot()))
        elements = {item.tag.split("}")[-1]: item for item in metadata}
        license_node = elements.get("license")
        license_value = license_node.text if license_node is not None else None
        repository = elements.get("repository")
        repository_data = repository.attrib if repository is not None else {}
        documents = [path for path in package.rglob("*") if path.is_file() and
                     path.name.lower().startswith(("license", "licence", "copying", "notice", "third-party", "thirdparty"))]
        if license_node is not None and license_node.get("type") == "file":
            if not (package / license_value).is_file():
                raise ValueError("Fehlender deklarierter Lizenztext: " + identity)
        elif license_value not in ("MIT", "MPL-2.0") and identity.split("/")[0] not in ("Mono.Posix.NETStandard", "Microsoft.Windows.SDK.NET.Ref"):
            raise ValueError("Ungeklärte Lizenzangabe: " + identity + ": " + str(license_value))
        supplemental = []
        name = identity.split("/")[0]
        if name.startswith("Avalonia"):
            supplemental.append("Avalonia.txt")
        elif name == "MicroCom.Runtime":
            supplemental.append("MicroCom.txt")
        elif name == "Tmds.DBus.Protocol":
            supplemental.append("Tmds.DBus.txt")
        elif name == "Mono.Posix.NETStandard":
            supplemental.append("Mono.txt")
        elif name == "Microsoft.Windows.SDK.NET.Ref":
            supplemental.append("WindowsSDK.rtf")
        elif name.startswith(("System.", "runtime.", "Microsoft.")):
            supplemental.append("dotnet.txt")
        if license_value == "MPL-2.0":
            supplemental.append("MPL-2.0.txt")
            if not repository_data.get("url") or not repository_data.get("commit"):
                raise ValueError("Fehlender MPL-Quellverweis: " + identity)
        if name == "HidSharp":
            supplemental.append("Apache-2.0.txt")
        if not documents and not supplemental:
            raise ValueError("Kein Lizenztext für " + identity)
        for filename in supplemental:
            if not (destination / filename).is_file():
                raise ValueError("Fehlender ergänzender Lizenztext: " + filename)
        target = destination / relative
        target.mkdir(parents=True)
        shutil.copyfile(specification, target / "package.nuspec")
        for document in documents:
            output = target / document.relative_to(package)
            output.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(document, output)
        source_url = repository_data.get("url", "")
        if source_url.startswith("https://github.com/") and repository_data.get("commit"):
            source_url = source_url.rstrip("/").removesuffix(".git") + "/archive/" + repository_data["commit"] + ".tar.gz"
        entries.append({"package": identity, "license": license_value, "repository": repository_data,
                        "source": source_url, "documents": [str(path.relative_to(package)) for path in documents],
                        "supplemental": supplemental})
    (destination / "manifest.json").write_text(json.dumps({"packages": entries, "license_sources": origins},
                                                          ensure_ascii=False, indent=2), encoding="utf-8")
    (publish / "THIRD-PARTY-NOTICES.txt").write_text(
        "Die mitgelieferten Bibliotheken behalten ihre eigenen Lizenzen.\n"
        "Lizenztexte, Copyrightangaben und unveränderte NuGet-Metadaten: licenses/\n"
        "Paketversionen und Quellverweise, einschließlich MPL-2.0-Quellen: licenses/manifest.json\n"
        "Die Inter-Schrift verwendet die SIL Open Font License: licenses/Inter.txt\n"
        "Die NuGet-Metadaten umfassen auch transitive und plattformfremde Paketabhängigkeiten.\n",
        encoding="utf-8")
    shutil.copyfile(Path(__file__).resolve().parents[2] / "LICENSE", publish / "LICENSE")
    return entries


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--publish", required=True, type=Path)
    parser.add_argument("--assets", required=True, type=Path, nargs="+")
    parser.add_argument("--cache", type=Path, default=Path.home() / ".nuget/packages")
    parser.add_argument("--rid", required=True, choices=("win-x64", "linux-x64", "linux-arm64"))
    arguments = parser.parse_args()
    print("Lizenzpakete:", len(bundle(arguments.publish, arguments.assets, arguments.cache, arguments.rid)))
