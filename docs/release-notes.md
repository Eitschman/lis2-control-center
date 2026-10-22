# LIS2 Control Center 0.1.0

Erster Avalonia-Release für Windows x64, Linux x64 und Linux ARM64.

- Steuerung des LIS2-Displays mit zwei Zeilen, Helligkeit und acht programmierbaren Glyphen.
- Seitenvorlagen, Scrollen, Sichtbarkeitsbedingungen, Ereignisse und virtuelle Geräteansicht.
- Hardwarewerte, vier Lüfterkanäle, Home Assistant und Medienquellen.
- Deutsch, Englisch, Französisch, Türkisch und Russisch einschließlich kontextbezogener Hilfe.
- Installierbare Debian-Pakete für amd64 und arm64 sowie portable App-Archive.
- Separater Avalonia-Protokolltester für Windows und natives x86-Plug-in für Winamp.

Die App-Pakete und der Protokolltester enthalten .NET 10.0.13. Eine separate .NET-Installation ist nicht nötig. Lizenztexte, Hinweise zu Fremdkomponenten und Quellverweise liegen in den Paketen unter `licenses/` und `THIRD-PARTY-NOTICES.txt`.

Unter Windows das ZIP entpacken und `LIS2.App.Avalonia.exe` starten. Unter Debian das zur Architektur passende Paket mit `sudo apt install ./lis2-control-center_0.1.0_amd64.deb` beziehungsweise `_arm64.deb` installieren. Die portablen Linux-Archive enthalten den Startbefehl `LIS2.App.Avalonia`. Prüfsummen stehen in `SHA256SUMS.txt`.

Die automatisierten Prüfungen verwenden ein virtuelles LIS2. Der Benutzer hat einen Debian-Paketstart auf Raspberry Pi 5 mit Debian 13, KDE Plasma 6.3.6 und Wayland-Sitzung bestätigt. Physische LIS2-Ausgabe, reale Home-Assistant-/Player-Verbindungen sowie weitere Desktop-, Tray- und Autostartprüfungen bleiben gesondert zu prüfen. Serielle Lüfterausgabe benötigt eine ausdrückliche Freigabe in der App.

.NET 10 LTS wird laut [Microsoft](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) bis 14.11.2028 unterstützt. Alle selbstständigen Pakete verwenden 10.0.13. Avalonia bleibt auf 11.3.6; die DBus-Abhängigkeit ist mit dem Sicherheitskorrekturpatch Tmds.DBus.Protocol 0.21.3 festgelegt. Ein neuerer Avalonia-Patchstand bleibt eine gesonderte Wartungsaufgabe.
