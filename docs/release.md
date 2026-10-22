# Builds, Prüfnachweise und Pakete

Stand: 22.10.2026. Die Workflows unterscheiden Avalonia-Entwicklung, Windows-WPF-Referenz und getaggte Windows-Releases.

## Avalonia-Branch

[linux-portable.yml](../.github/workflows/linux-portable.yml) startet bei Push auf `feature/linux-avalonia`, Pull Requests nach `main` und manueller Auslösung. Neuere Läufe derselben Workflow-/Branchgruppe brechen überholte Läufe ab.

| Jobs | Umfang |
| --- | --- |
| Shared libraries auf Windows/Ubuntu | `LIS2.Core.Tests`, `LIS2.Display.Tests`, `LIS2.Fans.Tests` |
| Avalonia shell auf Windows/Ubuntu | Release-Build und `LIS2.App.Avalonia.Tests`, einschließlich tatsächlicher Geräte-Controls und Skia-Rendering im Headless-Modus |
| Portable app: `win-x64`, `linux-x64`, `linux-arm64` | Abhängig von erfolgreichen Bibliotheks-/Avalonia-Jobs; selbstständiges, ungetrimmtes Multi-file-Publish |
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

Die UI-Tests exportieren gerenderte Ansichten als Avalonia-device-views-Windows und Avalonia-device-views-Linux. Native Windows-/Linux-X11-Desktop-Smokes prüfen zehn Ansichten, Hilfe/About, fehlende Medienanbieter, Diagnose und Shutdown. Die Screenshots dienen der Sichtprüfung; es gibt keinen automatischen Bildvergleich. Die Paketprüfung kontrolliert Executable, Dateigröße, Managed Assembly, Runtimekonfiguration sowie unter Windows die GSMTC-/WinRT-DLLs und den Anbieterstart. Linux-Prüfsummen werden gegengeprüft. Start aus einem extrahierten Archiv und echte ARM64-Ausführung bleiben offen.

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

- Extrahierte Windows-/Linux-Pakete starten und benötigte native Bibliotheken auf den Zielsystemen ermitteln.
- ARM64 auf einem ARM64-System prüfen; Debian/KDE und Raspberry Pi 5 mit Systemdaten protokollieren.
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

`tools/packaging/build_debian.py` baut lokal mit `--publish <Verzeichnis> --rid linux-x64 --version <Paketversion> --output <Zielverzeichnis>`; für ARM64 `linux-arm64` verwenden. Der Builder prüft Publish-Dateien und ELF-Architektur. Regressionen prüfen beide Paketarchitekturen und fehlerhafte Eingaben. Die CI installiert das echte amd64-Paket in frischen Debian-12-/Debian-13-Containern, validiert den Desktopeintrag, startet den nativen GUI-Smoke als gewöhnlicher Benutzer und prüft Deinstallation sowie Erhalt von Benutzerdaten. Dies ersetzt weder KDE-/Wayland-Abnahme noch echte ARM64-Ausführung.
