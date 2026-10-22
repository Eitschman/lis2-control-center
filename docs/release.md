# Builds, Prüfnachweise und Pakete

Stand: 22.10.2026. App und Protokolltester verwenden Avalonia. Hauptbranch, Entwicklungsbranch und Tag-Releases verwenden denselben Paketbau.

[Release v0.1.2](https://github.com/Eitschman/lis2-control-center/releases/tag/v0.1.2) zu `b2331ed` enthält drei portable App-Archive, zwei Debian-Pakete, den Windows-Protokolltester, `gen_lis2.dll` und `SHA256SUMS.txt`. [Release-CI 39770589294](https://github.com/Eitschman/lis2-control-center/actions/runs/39770589294) besteht alle zwölf Jobs. Nach Download stimmen alle sieben Prüfsummen; die sechs Anwendungspakete enthalten Produktversion 0.1.2, Avalonia 11.3.22, Runtime 10.0.13 und Lizenzbeilagen. Alle fünf App-Pakete enthalten die englische README mit Lizenz- und Bibliotheksübersicht. About zeigt Avalonia-Version und MIT-Lizenz und öffnet über eine lokalisierte Schaltfläche den mitgelieferten Lizenzordner. Fehlende Dateien werden ausdrücklich gemeldet. Installation und Paketauswahl stehen in der [README](../README.md#download-and-installation).

## Avalonia-Branch

[linux-portable.yml](../.github/workflows/linux-portable.yml) startet bei Push auf `feature/linux-avalonia` und manueller Auslösung. `build.yml` ruft ihn bei Push/PR nach `main` und manuell auf; `release.yml` verwendet ihn für Tag-Releases. Neuere Läufe derselben Workflow-/Branchgruppe brechen überholte Läufe ab.

| Jobs | Umfang |
| --- | --- |
| Shared libraries auf Windows/Ubuntu | `LIS2.Core.Tests`, `LIS2.Display.Tests`, `LIS2.Fans.Tests` |
| Avalonia shell auf Windows/Ubuntu | Release-Build und `LIS2.App.Avalonia.Tests`, einschließlich tatsächlicher Geräte-Controls und Skia-Rendering im Headless-Modus |
| Portable app: `win-x64`, `linux-x64`, `linux-arm64` | Abhängig von erfolgreichen Bibliotheks-/Avalonia-Jobs; selbstständiges, ungetrimmtes Multi-file-Publish; ARM64 auf `ubuntu-24.04-arm` |
| Windows Protocol Tester | Windows-Solution-Build, Sources-/Winamp-/WindowsMedia-/WmpLegacy-Tests, selbständiges Avalonia-win-x64-Publish, Prüfung der Laufzeitdateien und nativer virtueller UI-Smoke |
| WMP Legacy plug-ins win32/win64 | CMake/MSVC baut Hintergrund- und Visualisierungs-DLL; Defender-Prüfung, gemeinsames ZIP mit beiden Anleitungen und MIT-Lizenz, SHA-256 |
| Winamp plugin x86 | CMake/MSVC mit Win32 und Release, gen_lis2.dll wie auf main |

Für `win-x64` wird `LIS2.WindowsMedia` separat publiziert; Provider-DLL, WinRT.Runtime.dll, Microsoft.Windows.SDK.NET.dll und weitere DLL-Abhängigkeiten werden in das App-Paket übernommen. Der Paketjob startet den echten GSMTC-Anbieter, liest den Verbindungsstatus, beendet ihn und prüft die fehlende Anbieter-DLL. Linux-Medien und Tokenstore benötigen zusätzlich playerctl-/secret-tool- und Sitzungsdienste.

Artefakte mit 14 Tagen Aufbewahrung:

- `LIS2-Avalonia-win-x64`: ZIP und `.sha256`.
- `LIS2-Avalonia-linux-x64`: `.tar.gz` und `.sha256`.
- `LIS2-Avalonia-linux-arm64`: `.tar.gz` und `.sha256`.
- `LIS2-Debian-linux-x64`: Debian-Paket für `amd64` und `.deb.sha256`.
- `LIS2-Debian-linux-arm64`: Debian-Paket für `arm64` und `.deb.sha256`.
- `LIS2-ProtocolTester-win-x64`: selbständiger Windows-Protokolltester einschließlich Laufzeitdateien.
- `gen_lis2-win32`: native gen_lis2.dll für 32-Bit-Winamp.
- `LIS2-WMP-Legacy-win32` und `LIS2-WMP-Legacy-win64`: je ein ZIP mit Hintergrund-/Visualisierungs-DLL, Registrierungshinweisen und MIT-Lizenz sowie SHA-256.
- `Avalonia-GSMTC-smoke-Windows`: provider.txt und result.txt oder failure.txt.
- `Avalonia-package-smoke-<RID>`: Ergebnis des Starts aus dem extrahierten Archiv, Screenshots, Prozesslog und `environment.json` mit tatsächlicher Architektur, Laufzeitversion und unter Linux Bibliotheksauflösung.

Die UI-Tests exportieren gerenderte Ansichten als Avalonia-device-views-Windows und Avalonia-device-views-Linux. Native Windows-/Linux-X11-Desktop-Smokes prüfen zehn Ansichten, Hilfe/About, fehlende Medienanbieter, Diagnose und Shutdown. Die Screenshots dienen der Sichtprüfung; es gibt keinen automatischen Bildvergleich. Die Paketprüfung kontrolliert Executable, Dateigröße, Managed Assembly, Runtimekonfiguration sowie unter Windows die GSMTC-/WinRT-DLLs und den Anbieterstart. Zusätzlich werden alle drei Archive nach Prüfung ihrer SHA-256-Datei in ein neues temporäres Verzeichnis extrahiert und über ihren Apphost gestartet. Die Prüfung lehnt eine unpassende native Architektur, fehlende Linux-Bibliotheken, fehlende aktuelle Erfolgsmeldung und wiederverwendete Ergebnisverzeichnisse ab. ARM64 verwendet einen nativen GitHub-Runner, keine Emulation. Der tatsächliche CI-Nachweis steht im Status; die Workflowdefinition allein ist kein Laufzeitnachweis.

Die genaue Codebasis und der zugehörige CI-Lauf stehen im [aktuellen Status](prp/linux-port-status.md). Ein neuer Commit muss separat geprüft werden.

## Hauptbranch und Tag-Releases

[build.yml](../.github/workflows/build.yml) prüft `main` und Pull Requests mit dem wiederverwendbaren Avalonia-Workflow. Beide nativen Legacy-Plug-ins werden für x86/x64 gebaut und vor dem Verpacken mit Microsoft Defender geprüft. Die Windows-Solution enthält keine WPF-Projekte. Der Paketbau prüft die Windows-App mit Microsoft Defender.

[release.yml](../.github/workflows/release.yml) reagiert auf `v*`-Tags, validiert die Version und führt denselben Avalonia-Workflow aus. Erst nach dessen erfolgreicher Prüfung werden die Assets veröffentlicht:

- `LIS2-Control-Center-<version>-win-x64.zip`.
- `LIS2-WMP-Legacy-<version>-win32.zip` und `-win64.zip` mit beiden Legacy-Plug-ins. Diese Erweiterung gehört zu Entwicklungsständen nach v0.1.2; das veröffentlichte v0.1.2 enthält diese Pakete nicht.
- `LIS2-Control-Center-<version>-linux-x64.tar.gz` und `-linux-arm64.tar.gz`.
- `lis2-control-center_<Debianversion>_amd64.deb` und `_arm64.deb`.
- `LIS2-ProtocolTester-<version>-win-x64.zip`.
- `gen_lis2.dll` und `SHA256SUMS.txt`.

Die Tag-Version wird an App und Protokolltester übergeben. Debian-Pakete verwenden dieselbe Version; ein Bindestrich vor einer Vorabversion wird für die Debian-Sortierung durch `~` ersetzt. App-Archive und Protokolltester enthalten die Projektlizenz, Original-Lizenztexte und Fremdkomponentenhinweise unter `licenses/`, ein Manifest mit Paketversionen und Quellverweisen sowie `THIRD-PARTY-NOTICES.txt`. Die Debian-Pakete übernehmen diese Dateien aus demselben Publish-Verzeichnis. Die Releasebeschreibung kommt aus `docs/release-notes.md`.

Ein Tag wird nur auf ausdrücklichen Releaseauftrag erstellt oder gepusht. Der Workflow kann mit einer optionalen Version manuell als Paketprobe ausgeführt werden, ohne einen Release anzulegen. Eine erfolgreiche Paketprobe ist kein Nachweis des Veröffentlichungsschritts.

## Offene Distributionsprüfungen

- Archivstarts und native ARM64-Ausführung anhand des zum Commit gehörenden Paket-Smokes beurteilen.
- Debian/KDE und Raspberry Pi 5 mit Systemdaten protokollieren; der ARM64-Runner ersetzt keinen Raspberry-Pi-Desktop.
- Serielle Rechte, DBus-/Keyring-Verfügbarkeit und sichtbare Fehler bei fehlenden Diensten prüfen.
- Versionierte Paketprobe und gemeinsamen Hauptbranch-CI-Lauf anhand des jeweiligen Commits nachweisen.
- Reale Tag-Veröffentlichung erst bei ausdrücklich beauftragtem Release prüfen.

Portable Archive und Debian-Pakete werden nebeneinander erzeugt. AppImage und Windows-Installer sind nicht eingerichtet. Hardwareaussagen benötigen getrennte physische Nachweise; offene Protokollfragen stehen in [protocol.md](protocol.md).

## Debian-Pakete

Der Paketbau verwendet dieselben selbstständigen Linux-Publish-Verzeichnisse wie die Archive. Die CI-Paketversion `0.0~ci<Laufnummer>.<Versuch>` kennzeichnet Entwicklungsstände und steigt bei weiteren Workflow-Läufen. Sie ist keine Produktreleaseversion. Paketname ist `lis2-control-center`; .NET muss nicht separat installiert werden.

Das passende Paket aus dem GitHub-Artefakt herunterladen und entpacken. `dpkg --print-architecture` zeigt die benötigte Architektur. Im Downloadverzeichnis:

```sh
sha256sum -c lis2-control-center_*.deb.sha256
sudo apt install ./lis2-control-center_<Paketversion>_amd64.deb
lis2-control-center
```

Für ARM64 den Dateinamen mit `_arm64.deb` verwenden. Ein neueres Paket wird mit demselben `apt install`-Befehl installiert. Die Anwendung erscheint im Anwendungsmenü. Die Dateien liegen unter `/usr/lib/lis2-control-center`, der Startbefehl unter `/usr/bin`.

Die Bibliotheksabhängigkeiten berücksichtigen Debian 12 und 13 sowie die ICU-/OpenSSL-Paketnamen von Ubuntu 24.04. Grundlage sind die offiziellen [Avalonia-11-Paketierungsangaben](https://v11.docs.avaloniaui.net/docs/deployment/debian-ubuntu/) und [.NET-10-Installation für Debian](https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian#dependencies). `libsecret-tools` und `playerctl` werden empfohlen und von `apt` standardmäßig mitinstalliert. Für sichere Tokenspeicherung muss zusätzlich ein Schlüsselbunddienst in der Desktop-Sitzung laufen. Das Paket richtet weder serielle Gruppenrechte noch Autostart automatisch ein.

Deinstallation: `sudo apt remove lis2-control-center`. Persönliche Einstellungen und Tokens bleiben erhalten. Einen in der App aktivierten Autostart vor der Deinstallation in den Einstellungen deaktivieren.

`tools/packaging/build_debian.py` baut lokal mit `--publish <Verzeichnis> --rid linux-x64 --version <Paketversion> --output <Zielverzeichnis>`; für ARM64 `linux-arm64` verwenden. Der Builder prüft Publish-Dateien und ELF-Architektur. Regressionen prüfen beide Paketarchitekturen und fehlerhafte Eingaben. Die CI installiert das echte Paket auf der jeweiligen nativen Architektur in frischen Debian-12-/Debian-13-Containern, validiert den Desktopeintrag, startet den nativen GUI-Smoke als gewöhnlicher Benutzer und prüft Deinstallation sowie Erhalt von Benutzerdaten. Ein isolierter DBus-/GNOME-Schlüsselbund prüft Speichern, Lesen und Entfernen eines ausschließlich für diesen Lauf erzeugten Testwerts. Containerprotokolle nennen Distribution, Architektur und zentrale Bibliotheksversionen. Dies ersetzt keine KDE-/Wayland-Abnahme und keinen Raspberry-Pi-Test.

## Support und Lizenzen

Prüfdatum: 22.10.2026. Alle Projekte verwenden .NET 10; `global.json` verlangt ein stabiles SDK der 10.0-Reihe. `Directory.Build.props` legt die mitgelieferte Laufzeit für selbstständige Pakete auf **10.0.13** fest. Die lokale Windows-Paketprobe enthält diese Version. Windows x64 und Linux x64/ARM64 verwenden dieselbe Vorgabe; der CI-Nachweis steht im [Status](prp/linux-port-status.md). Laut [Microsoft-Supportübersicht](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) endet der Support von .NET 10 LTS am **14.11.2028**. Selbstständige Pakete müssen bei Laufzeitupdates neu gebaut und verteilt werden.

Avalonia ist für App, Protokolltester und Headless-Tests einheitlich auf **11.3.22** festgelegt. Das [versionsbezogene Lizenzdokument](https://github.com/AvaloniaUI/Avalonia/blob/11.3.22/licence.md) weist MIT aus. Der originale Lizenztext und die eingebettete Inter-Schrift sind gegenüber dem bestehenden Paketnachweis unverändert; der Quellverweis nennt den tatsächlichen Paketcommit. [Avalonia 11.3.22](https://github.com/AvaloniaUI/Avalonia/releases/tag/11.3.22) enthält X11-Korrekturen. Aus der MIT-Lizenz folgt keine Zusage für langfristigen Versionssupport. Ein verifiziertes Support-Enddatum für diesen Patchstand liegt nicht vor. API-, Darstellungs- und Paketregressionen werden vor dem Release geprüft.

`tools/packaging/bundle_licenses.py` liest die tatsächlichen NuGet-Assets einschließlich Windows-SDK-Downloadabhängigkeiten und der mitgelieferten Laufzeit. Es kopiert originale Lizenz-/Notice-Dateien und NuGet-Metadaten und ergänzt geprüfte Upstream-Lizenztexte aus `tools/packaging/licenses/`. Deren unveränderte Bytes werden gegen SHA-256-Nachweise geprüft. Fehlende Metadaten, unbekannte Lizenzangaben, fehlende Lizenzdateien und fehlende MPL-Quellverweise brechen den Paketbau ab. Das Manifest enthält feste Quellarchivverweise für die MPL-Pakete LibreHardwareMonitor, BlackSharp, DiskInfoToolkit und RAMSPDToolkit. Skia/HarfBuzz behalten ihre eigenen Fremdkomponentenhinweise; HidSharp erhält den Apache-2.0-Text, Inter die SIL Open Font License und das Windows-SDK seine originale Microsoft-Lizenz. Positive und negative Regressionstests prüfen diese Paketbeilagen. Die Projektlizenz ersetzt die Lizenzen der Abhängigkeiten nicht. Grundlage: [NuGet-Lizenzmetadaten](https://learn.microsoft.com/en-us/nuget/reference/nuspec#license) und [.NET-Lizenzhinweise](https://github.com/dotnet/core/blob/main/license-information.md).

## Zielsystemnachweise

Benutzerbestätigung vom 22.10.2026: Raspberry Pi 5 mit 16 GB RAM, Debian GNU/Linux 13 Trixie, ARM64/aarch64, KDE Plasma 6.3.6 und Wayland-Sitzung. Installiert wurde `lis2-control-center_0.0~ci622.1_arm64.deb`; die Anwendung startet erfolgreich. [Build 622](https://github.com/Eitschman/lis2-control-center/actions/runs/39748001109) gehört laut GitHub-API zu `c1b74ea`. Das ist ein Praxistest der installierten Debian-Paketversion. DPI und tatsächlicher App-Displaybackend sind noch nicht angegeben; die Bestätigung umfasst keine einzeln protokollierten Tray-/Autostart-, Livequellen-, Keyring- oder Hardwareabläufe.

Für Debian/KDE und Raspberry Pi 5 werden zusätzlich zu den automatisierten Paketstarts Distribution/Version, CPU-Architektur, Desktopversion, X11 oder Wayland/XWayland, DPI, Theme und Sprache protokolliert. Zu prüfen sind echte Tray-Klicks, Aktivierung aus einem zweiten Prozess, Autostart nach Anmeldung, Datei-/Ordnerdialoge, Medienquelle und sicherer Tokenstore. Unter Wayland muss die tatsächlich genutzte XWayland-Verbindung festgehalten werden; ein Xvfb-Lauf belegt keine Wayland-Sitzung.

Serielle Rechte werden am konkreten Gerät geprüft: Gerätepfad und Eigentümergruppe mit `ls -l /dev/serial/by-id/` und `ls -l /dev/ttyUSB0` aufnehmen, tatsächlichen Namen entsprechend ersetzen, Benutzergruppen mit `id -nG` vergleichen und Zugriff nach einer etwaigen Gruppenänderung erst nach Neuanmeldung testen. Das Paket ändert keine Rechte und sendet bei der Geräteerkennung keine Protokollkommandos. Ohne physisches Gerät bleiben Zugriff und Hardwareausgabe ungeprüft.
