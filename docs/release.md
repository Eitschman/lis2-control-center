# Builds, Prüfnachweise und Pakete

Stand: 20.10.2026. Die Workflows unterscheiden Avalonia-Entwicklung, Windows-WPF-Referenz und getaggte Windows-Releases.

## Avalonia-Branch

[linux-portable.yml](../.github/workflows/linux-portable.yml) startet bei Push auf `feature/linux-avalonia`, Pull Requests nach `main` und manueller Auslösung. Neuere Läufe derselben Workflow-/Branchgruppe brechen überholte Läufe ab.

| Jobs | Umfang |
| --- | --- |
| Shared libraries auf Windows/Ubuntu | `LIS2.Core.Tests`, `LIS2.Display.Tests`, `LIS2.Fans.Tests` |
| Avalonia shell auf Windows/Ubuntu | Release-Build der App und `LIS2.App.Avalonia.Tests` |
| Portable app: `win-x64`, `linux-x64`, `linux-arm64` | Abhängig von erfolgreichen Bibliotheks-/Avalonia-Jobs; selbstständiges, ungetrimmtes Multi-file-Publish |

Für `win-x64` wird `LIS2.WindowsMedia` separat gebaut; Provider-DLL und weitere DLL-Abhängigkeiten werden in das Paket kopiert. Linux-Medien und Tokenstore benötigen zur Laufzeit zusätzlich die jeweils verfügbaren `playerctl`-/`secret-tool`- und Sitzungsdienste.

Artefakte mit 14 Tagen Aufbewahrung:

- `LIS2-Avalonia-win-x64`: ZIP und `.sha256`.
- `LIS2-Avalonia-linux-x64`: `.tar.gz` und `.sha256`.
- `LIS2-Avalonia-linux-arm64`: `.tar.gz` und `.sha256`.

Die Paketprüfung kontrolliert Executable, Dateigröße, Managed Assembly, Runtimekonfiguration und unter Windows die GSMTC-DLL. Linux-Prüfsummen werden im Workflow gegengeprüft. Weder der Start aus dem extrahierten Paket noch echte ARM64-Ausführung oder Avalonia-GUI-Smoke sind implementiert.

Nachgewiesene Codebasis: `71b72a8987a4f9b8a14a3807f94667569e28be43`, [Lauf 39435226116](https://github.com/Eitschman/lis2-control-center/actions/runs/39435226116), sieben erfolgreiche Jobs. Ein neuer Commit muss separat geprüft werden; siehe [aktueller Status](prp/linux-port-status.md).

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
