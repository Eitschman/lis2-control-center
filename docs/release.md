# Builds, Prüfnachweise und Pakete

Stand: 21.10.2026. Die Workflows unterscheiden Avalonia-Entwicklung, Windows-WPF-Referenz und getaggte Windows-Releases.

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

Portable Archive bleiben das verwendete Format. AppImage/deb oder ein Installer sind keine bereits vorhandene Distribution. Hardwareaussagen benötigen getrennte physische Nachweise; offene Protokollfragen stehen in [protocol.md](protocol.md).
