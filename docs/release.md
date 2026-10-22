# Builds, Prüfnachweise und Pakete

Stand: 22.10.2026. Die Workflows unterscheiden Avalonia-Entwicklung, Windows-WPF-Referenz und getaggte Windows-Releases.

## Avalonia-Branch

[linux-portable.yml](../.github/workflows/linux-portable.yml) startet bei Push auf `feature/linux-avalonia`, Pull Requests nach `main` und manueller Auslösung. Neuere Läufe derselben Workflow-/Branchgruppe brechen überholte Läufe ab.

| Jobs | Umfang |
| --- | --- |
| Shared libraries auf Windows/Ubuntu | `LIS2.Core.Tests`, `LIS2.Display.Tests`, `LIS2.Fans.Tests` |
| Avalonia shell auf Windows/Ubuntu | Release-Build und `LIS2.App.Avalonia.Tests`, einschließlich tatsächlicher Geräte-Controls und Skia-Rendering im Headless-Modus |
| Portable app: `win-x64`, `linux-x64`, `linux-arm64` | Abhängig von erfolgreichen Bibliotheks-/Avalonia-Jobs; selbstständiges, ungetrimmtes Multi-file-Publish; ARM64 auf `ubuntu-24.04-arm` |
| Windows Protocol Tester | Selbständiges win-x64-Publish des WPF-Protokolltesters wie auf main, Prüfung der Laufzeitdateien |
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
- `Avalonia-GSMTC-smoke-Windows`: provider.txt und result.txt oder failure.txt.
- `Avalonia-package-smoke-<RID>`: Ergebnis des Starts aus dem extrahierten Archiv, Screenshots, Prozesslog und `environment.json` mit tatsächlicher Architektur, Laufzeitversion und unter Linux Bibliotheksauflösung.

Die UI-Tests exportieren gerenderte Ansichten als Avalonia-device-views-Windows und Avalonia-device-views-Linux. Native Windows-/Linux-X11-Desktop-Smokes prüfen zehn Ansichten, Hilfe/About, fehlende Medienanbieter, Diagnose und Shutdown. Die Screenshots dienen der Sichtprüfung; es gibt keinen automatischen Bildvergleich. Die Paketprüfung kontrolliert Executable, Dateigröße, Managed Assembly, Runtimekonfiguration sowie unter Windows die GSMTC-/WinRT-DLLs und den Anbieterstart. Zusätzlich werden alle drei Archive nach Prüfung ihrer SHA-256-Datei in ein neues temporäres Verzeichnis extrahiert und über ihren Apphost gestartet. Die Prüfung lehnt eine unpassende native Architektur, fehlende Linux-Bibliotheken, fehlende aktuelle Erfolgsmeldung und wiederverwendete Ergebnisverzeichnisse ab. ARM64 verwendet einen nativen GitHub-Runner, keine Emulation. Der tatsächliche CI-Nachweis steht im Status; die Workflowdefinition allein ist kein Laufzeitnachweis.

Die genaue Codebasis und der zugehörige CI-Lauf stehen im [aktuellen Status](prp/linux-port-status.md). Ein neuer Commit muss separat geprüft werden.

## Windows-WPF-Referenz

[build.yml](../.github/workflows/build.yml) läuft bei Push/PR nach `main` sowie manuell. Er baut die Solution, führt deren Tests aus und startet WPF-Smoke-/UI-Regressionstests. Nicht-PR-Läufe publizieren Windows-App, ProtocolTester und nativen x86-Winamp-Plug-in.

Diese WPF-Prüfungen werden durch einen gewöhnlichen Avalonia-Branch-Push nicht ausgelöst. Die erfolgreichen Avalonia-Jobs sind daher kein aktueller Nachweis für den gesamten WPF-/Windows-Workflow.

## Tag-Releases

[release.yml](../.github/workflows/release.yml) reagiert auf `v*`-Tags. Er validiert die Version, baut und testet die Windows-Solution, führt WPF-Smoke aus und publiziert derzeit die WPF-Anwendung, den ProtocolTester und den x86-Winamp-Plug-in.

Assets:

- `LIS2-Control-Center-<version>-win-x64.zip`.
- `LIS2-ProtocolTester-<version>-win-x64.zip`.
- `gen_lis2.dll`.
- `SHA256SUMS.txt`.

Die Version wird aus dem Tag übergeben. Der Tag-Workflow ist kein Avalonia-Releasepfad. Ein Tag wird nur auf ausdrücklichen Releaseauftrag erstellt oder gepusht.

## Offene Distributionsprüfungen

- Archivstarts und native ARM64-Ausführung anhand des zum Commit gehörenden Paket-Smokes beurteilen.
- Debian/KDE und Raspberry Pi 5 mit Systemdaten protokollieren; der ARM64-Runner ersetzt keinen Raspberry-Pi-Desktop.
- Serielle Rechte, DBus-/Keyring-Verfügbarkeit und sichtbare Fehler bei fehlenden Diensten prüfen.
- Avalonia-UI-Smoke und Versionsmetadaten in den künftigen Releasepfad aufnehmen.
- WPF-Paketierung erst nach Windows-/Linux-Paritätsabnahme durch den gemeinsamen Avalonia-Releasepfad ersetzen.

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

Die Bibliotheksabhängigkeiten berücksichtigen Debian 12 und 13 sowie die ICU-/OpenSSL-Paketnamen von Ubuntu 24.04. Grundlage sind die offiziellen [Avalonia-11-Paketierungsangaben](https://v11.docs.avaloniaui.net/docs/deployment/debian-ubuntu/) und [.NET-8-Abhängigkeiten](https://github.com/dotnet/core/blob/main/release-notes/8.0/os-packages.md). `libsecret-tools` und `playerctl` werden empfohlen und von `apt` standardmäßig mitinstalliert. Für sichere Tokenspeicherung muss zusätzlich ein Schlüsselbunddienst in der Desktop-Sitzung laufen. Das Paket richtet weder serielle Gruppenrechte noch Autostart automatisch ein.

Deinstallation: `sudo apt remove lis2-control-center`. Persönliche Einstellungen und Tokens bleiben erhalten. Einen in der App aktivierten Autostart vor der Deinstallation in den Einstellungen deaktivieren.

`tools/packaging/build_debian.py` baut lokal mit `--publish <Verzeichnis> --rid linux-x64 --version <Paketversion> --output <Zielverzeichnis>`; für ARM64 `linux-arm64` verwenden. Der Builder prüft Publish-Dateien und ELF-Architektur. Regressionen prüfen beide Paketarchitekturen und fehlerhafte Eingaben. Die CI installiert das echte Paket auf der jeweiligen nativen Architektur in frischen Debian-12-/Debian-13-Containern, validiert den Desktopeintrag, startet den nativen GUI-Smoke als gewöhnlicher Benutzer und prüft Deinstallation sowie Erhalt von Benutzerdaten. Ein isolierter DBus-/GNOME-Schlüsselbund prüft Speichern, Lesen und Entfernen eines ausschließlich für diesen Lauf erzeugten Testwerts. Containerprotokolle nennen Distribution, Architektur und zentrale Bibliotheksversionen. Dies ersetzt keine KDE-/Wayland-Abnahme und keinen Raspberry-Pi-Test.

## Support und Lizenzen

Prüfdatum: 22.10.2026. Die tatsächlichen Runtimekonfigurationen aus CI `39750384348` nennen .NET **8.0.32** für Linux x64/ARM64 und **8.0.31** für Windows x64. Die [Microsoft-Supportübersicht](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) nennt 8.0.32 als aktuellen Patch und den **10.11.2026** als Supportende von .NET 8. Der Windows-Patchstand muss vor Release angeglichen werden. Für einen länger gepflegten Avalonia-Release ist zudem ein gesondertes Umstellungspaket auf eine unterstützte .NET-Version nötig. Die Übersicht führt .NET 10 LTS mit Support bis 14.11.2028 auf. PORT-07 verändert das Zielframework nicht.

Avalonia ist auf **11.3.6** festgelegt. Das [zu dieser Version gehörende Lizenzdokument](https://github.com/AvaloniaUI/Avalonia/blob/11.3.6/licence.md) und die [NuGet-Metadaten](https://www.nuget.org/packages/Avalonia/11.3.6) weisen MIT aus. Die Paketübersicht führt neuere Versionen, darunter 11.3.22 und 12.1.4; 11.3.6 ist damit kein aktueller Patchstand. Aus der MIT-Lizenz folgt keine Zusage für langfristigen Versionssupport. Ein verifiziertes Support-Enddatum für exakt 11.3.6 liegt nicht vor. Ein Versionswechsel einschließlich API-/Darstellungsregressionen ist gesondert zu prüfen.

Die [.NET-Lizenzhinweise](https://github.com/dotnet/core/blob/main/license-information.md) unterscheiden Projektlizenz und Hinweise zu Fremdkomponenten. Im geprüften Publish fehlen eigenständige Lizenz-/Notice-Dateien. Das Debian-Paket enthält die Projektlizenz, damit sind die Hinweise zu allen mitgelieferten Abhängigkeiten nicht nachgewiesen. Vor Tag-Releases müssen die .NET-/Avalonia-Lizenztexte und die zugehörigen Fremdkomponentenhinweise Bestandteil der Pakete werden. Die aus den tatsächlichen NuGet-Metadaten geprüften Pakete `LibreHardwareMonitorLib/0.9.6`, `BlackSharp.Core/1.0.7`, `DiskInfoToolkit/1.1.2` und `RAMSPDToolkit-NDD/1.4.2` melden MPL-2.0. `HidSharp/2.6.4` verweist auf eine paketinterne Lizenzdatei; `Mono.Posix.NETStandard/1.0.0` enthält keine SPDX-Lizenzangabe. Diese Befunde verlangen einen eigenen Abgleich von Lizenztexten und Quellverweisen; die MIT-Lizenz des Repositories ist keine pauschale Lizenzfreigabe der Abhängigkeiten.

## Zielsystemnachweise

Für Debian/KDE und Raspberry Pi 5 werden zusätzlich zu den automatisierten Paketstarts Distribution/Version, CPU-Architektur, Desktopversion, X11 oder Wayland/XWayland, DPI, Theme und Sprache protokolliert. Zu prüfen sind echte Tray-Klicks, Aktivierung aus einem zweiten Prozess, Autostart nach Anmeldung, Datei-/Ordnerdialoge, Medienquelle und sicherer Tokenstore. Unter Wayland muss die tatsächlich genutzte XWayland-Verbindung festgehalten werden; ein Xvfb-Lauf belegt keine Wayland-Sitzung.

Serielle Rechte werden am konkreten Gerät geprüft: Gerätepfad und Eigentümergruppe mit `ls -l /dev/serial/by-id/` und `ls -l /dev/ttyUSB0` aufnehmen, tatsächlichen Namen entsprechend ersetzen, Benutzergruppen mit `id -nG` vergleichen und Zugriff nach einer etwaigen Gruppenänderung erst nach Neuanmeldung testen. Das Paket ändert keine Rechte und sendet bei der Geräteerkennung keine Protokollkommandos. Ohne physisches Gerät bleiben Zugriff und Hardwareausgabe ungeprüft.
