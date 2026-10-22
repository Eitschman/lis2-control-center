# LIS2 Control Center 0.1.1

## Avalonia-Patchpflege

App, Protokolltester und Headless-Tests verwenden einheitlich **Avalonia 11.3.22**. Diese Wartungsversion enthält unter anderem Korrekturen für X11/NVIDIA und den X11-Sitzungsabbau. Die Home-Assistant-UI-Tests verwenden die aktuelle Zwischenablage-API; die DBus-Sicherheitskorrektur bleibt eingebunden.

## Downloads und Installation

Enthalten sind portable Pakete für Windows x64, Linux x64 und Linux ARM64, Debian-Pakete für amd64/arm64, der Windows-Protokolltester und das native 32-Bit-Winamp-Plug-in `gen_lis2.dll`.

Unter Windows das ZIP entpacken und `LIS2.App.Avalonia.exe` starten. Unter Debian das passende Paket mit `sudo apt install ./lis2-control-center_0.1.1_amd64.deb` beziehungsweise `_arm64.deb` installieren. Portable Linux-Archive enthalten den Startbefehl `LIS2.App.Avalonia`. Prüfsummen stehen in `SHA256SUMS.txt`.

Die Anwendungspakete liefern **.NET 10.0.13** mit; .NET muss nicht separat installiert werden. Lizenztexte, Fremdkomponentenhinweise und feste Quellverweise liegen unter `licenses/` und `THIRD-PARTY-NOTICES.txt` bei. Avalonia-Lizenztext und Inter-Schrift wurden gegen den tatsächlichen Paketstand geprüft.

## Prüfumfang und offene Praxisprüfungen

Die Release-Pipeline prüft Bibliotheken, Avalonia-Controls, native Windows-/Linux-Oberflächen, GSMTC-Anbieterstart, extrahierte Archive und Debian-Installation auf x64 und nativem ARM64. Die Desktop-Smokes prüfen unter anderem zehn Ansichten, Hilfe/About, Aktivierung aus einem zweiten Prozess und Shutdown. Screenshots dienen der Sichtprüfung.

Der Benutzer bestätigt v0.1.0 mit .NET 10 auf Raspberry Pi 5 unter Debian 13/KDE/Wayland. Diese Rückmeldung ist kein separater Praxisnachweis für v0.1.1. Reale HA-/Playerverbindungen, Anmeldung mit Autostart, weitere Desktopabläufe und physische LIS2-Ausgabe bleiben gesonderte Prüfungen. Serielle Lüfterausgabe benötigt eine ausdrückliche Freigabe in der App.
