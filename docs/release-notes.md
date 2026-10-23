# LIS2 Control Center 0.1.4

## Kompakte VFD-Vorschau

Die blaue VFD-Fläche in der Übersicht richtet sich nach den tatsächlichen 20 × 2 Zeichen. Schriftgröße und Innenabstand bleiben erhalten; die Fläche wird nicht mehr auf eine größere feste Breite und Höhe ausgedehnt.

Die Vorschaukarte bekommt die dazu passende Breite. Status und Schnellzugriff nutzen den restlichen Platz. Bei ausreichend Platz stehen die Schnellzugriff-Schaltflächen nebeneinander; bei kleineren Fenstern brechen sie um.

Vier Layoutregressionen prüfen beide gefüllten Zeilen und die Kartenbreite auf Deutsch und Englisch bei 950 × 600 und 1305 × 893 Pixeln. Beide deutschen gerenderten Ansichten wurden angesehen. Die Tests erkennen die zu große Vorschaufläche und die unnötig breite Karte; mit der inhaltsabhängigen Größe bestehen sie.

## Windows Media Player Legacy

Die Windows-App enthält die Legacy-Medienkarte, Metadaten und VU/Spektrum über zwei getrennte Plug-ins, `WmpLegacy.*`, drei Presets und fünfsprachige Hilfe. Die Pakete `LIS2-WMP-Legacy-0.1.4-win32.zip` und `LIS2-WMP-Legacy-0.1.4-win64.zip` enthalten jeweils beide nativen DLLs, Registrierungsanleitungen und die MIT-Lizenz. Die Architektur muss zum **Legacy-Playerprozess** passen. Registrierung mit dem passenden administrativen `regsvr32`, danach den Player neu starten. Für Audio **LIS2 Control Center / Telemetry** als Visualisierung wählen. Einzelheiten und manuelle Prüfliste: [Legacy-Leitfaden](https://github.com/Eitschman/lis2-control-center/blob/v0.1.4/docs/wmp-legacy-integration.md). Kein Plug-in greift auf LIS2-Hardware zu.

Die Legacy-DLLs enthalten ihre C/C++-Laufzeit durch statisches Linken. Eine separate Visual-C++-Redistributable-Installation entfällt. Die CI kontrolliert die tatsächlichen DLL-Importe. Sicherheitsaktualisierungen der eingebundenen Laufzeit erfordern einen neuen Plug-in-Build.

## Gemeinsame Windows-/Linux-Anwendung

App, Protokolltester und Headless-Tests verwenden **Avalonia 11.3.22**. Die DBus-Sicherheitskorrektur bleibt eingebunden. About nennt Frameworkversion und MIT-Lizenz; eine lokalisierte Schaltfläche öffnet die Lizenzdateien. Die englische README enthält Installation, Lizenz und die Bibliotheksübersicht.

## Downloads und Installation

Enthalten sind portable Pakete für Windows x64, Linux x64 und Linux ARM64, Debian-Pakete für amd64/arm64, der Windows-Protokolltester, das native 32-Bit-Winamp-Plug-in `gen_lis2.dll` und beide Legacy-Plug-in-Pakete.

Unter Windows das ZIP entpacken und `LIS2.App.Avalonia.exe` starten. Unter Debian das passende Paket mit `sudo apt install ./lis2-control-center_0.1.4_amd64.deb` beziehungsweise `_arm64.deb` installieren. Portable Linux-Archive enthalten den Startbefehl `LIS2.App.Avalonia`. Prüfsummen stehen in `SHA256SUMS.txt`.

Die Anwendungspakete liefern **.NET 10.0.13** mit; .NET muss nicht separat installiert werden. Lizenztexte, Fremdkomponentenhinweise und feste Quellverweise liegen unter `licenses/` und `THIRD-PARTY-NOTICES.txt` bei. Avalonia-Lizenztext und Inter-Schrift wurden gegen den tatsächlichen Paketstand geprüft.

## Prüfumfang und offene Praxisprüfungen

Die Release-Pipeline prüft Bibliotheken, Avalonia-Controls, native Windows-/Linux-Oberflächen, GSMTC-Anbieterstart, extrahierte Archive und Debian-Installation auf x64 und nativem ARM64. Die nativen Legacy-Plug-ins werden für beide Architekturen gebaut und mit Microsoft Defender geprüft. Die Desktop-Smokes prüfen unter anderem zehn Ansichten, Hilfe/About, Aktivierung aus einem zweiten Prozess und Shutdown. Screenshots dienen der Sichtprüfung.

Reale HA-/Playerverbindungen, die COM-Registrierung und Wiedergabe im tatsächlichen Legacy-Player, Anmeldung mit Autostart, weitere Desktopabläufe und physische LIS2-Ausgabe bleiben gesonderte Prüfungen. Pipe-Simulationen belegen keine reale Playerabnahme. Serielle Lüfterausgabe benötigt eine ausdrückliche Freigabe in der App.
