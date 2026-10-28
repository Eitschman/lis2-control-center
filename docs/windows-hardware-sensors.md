# Windows-Sensoren: CPU-Temperaturen mit PawnIO

LIS2 Control Center liest Windows-Sensoren über LibreHardwareMonitor 0.9.6. CPU-Temperaturen benötigen dessen Zugriff auf den offiziellen signierten PawnIO-Treiber. GPU-Temperaturen und Auslastungswerte können auch ohne diesen Zugriff verfügbar sein. Erkannte CPU-Sensoren mit leerem Messwert werden nicht durch erfundene Werte oder ACPI-Thermalzonen ersetzt.

Die Anwendung kann nach einer ausdrücklich gewählten Benutzerfreigabe ohne Administratorrechte laufen. Ein eigener LIS2-Windows-Dienst ist dafür nicht erforderlich. Administratorrechte werden einmalig für Treiberinstallation und Geräteberechtigungen benötigt.

## Umfang der Benutzerfreigabe

Die Freigabe gilt für ein konkretes Windows-Benutzerkonto. Alle Programme unter diesem Konto können anschließend auf PawnIO zugreifen. Sie ist nicht auf LIS2 oder reine Temperaturabfragen begrenzt. Der Treiberhersteller bezeichnet die Öffnung für normale Benutzer als nicht empfohlen; deshalb wird sie niemals automatisch gesetzt. SYSTEM und Administratoren behalten ihre vorhandenen Rechte. Allgemeine Gruppen wie Jeder erhalten keine zusätzliche Berechtigung.

Das [PowerShell-Werkzeug](../tools/windows/Set-PawnIoUserAccess.ps1) verwendet die Windows-Geräteschnittstelle `SetupDiSetDeviceRegistryPropertyW` mit `SPDRP_SECURITY`. Es ergänzt ausschließlich eine explizite `GENERIC_READ | GENERIC_WRITE`-ACE für die angegebene Benutzer-SID, sichert den Originalzustand und liest die Änderung zur Kontrolle erneut aus. LibreHardwareMonitor öffnet das Gerät mit Lese- und Schreibzugriff; eine reine Lesefreigabe reicht dafür nicht. Danach wird ausschließlich die angegebene PawnIO-Geräteinstanz neu gestartet.

## 1. Offiziellen Treiber installieren

1. Den signierten Installer von [pawnio.eu](https://pawnio.eu/) beziehungsweise den [offiziellen Veröffentlichungen](https://github.com/namazso/PawnIO.Setup/releases) herunterladen. Geprüfter Stand ist PawnIO 2.2.0.
2. In PowerShell die Signatur prüfen:

   ```powershell
   Get-AuthenticodeSignature .\PawnIO_setup.exe |
       Select-Object Status, @{Name='Signer';Expression={$_.SignerCertificate.Subject}}
   ```

   Beim geprüften Installer ist der Status `Valid`, der Herausgeber `namazso`. SHA-256 der geprüften Datei: `1f519a22e47187f70a1379a48ca604981c4fcf694f4e65b734aaa74a9fba3032`.
3. Den Installer mit Administratorrechten ausführen. Die normale signierte Ausgabe verwenden. Die Entwickleroption für die unsignierte Ausgabe ist nicht erforderlich. Der geprüfte unbeaufsichtigte Aufruf lautet `PawnIO_setup.exe -install -silent` aus einer erhöhten Konsole. Exit-Code 0 bedeutet Erfolg; 3010 bedeutet, dass ein Windows-Neustart angefordert wird.
4. Den Treiberdienst prüfen:

   ```powershell
   Get-Service PawnIO
   Get-PnpDevice | Where-Object InstanceId -Like 'ROOT\PAWNIO\*' |
       Select-Object FriendlyName, InstanceId, Status
   ```

   Im geprüften System läuft der Dienst, die Geräteinstanz heißt `ROOT\PAWNIO\0000`, der Gerätestatus ist `OK`. Auf anderen Systemen die tatsächlich angezeigte Instanz verwenden.

Der Installer erlaubt ausdrücklich die unveränderte Weitergabe. Er wird separat installiert; eine neben die Anwendung gelegte DLL ersetzt die Treiberinstallation nicht. Die aktuellen Release-Archive enthalten den Installer nicht.

## 2. Zielkonto und Sicherungspfad bestimmen

In einer **normalen PowerShell des späteren Anwendungsbenutzers** ausführen:

```powershell
[Security.Principal.WindowsIdentity]::GetCurrent().User.Value
Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'LIS2-PawnIO-Access.json'
```

Die SID und den absoluten Sicherungspfad übernehmen. Das ist auch dann wichtig, wenn die UAC-Abfrage mit einem anderen Administratorkonto bestätigt wird. Keine Gruppen-SID einsetzen. Die Sicherungsdatei aufbewahren; sie enthält keine Zugangsdaten.

## 3. Gewählten Benutzer freigeben

Eine PowerShell als Administrator im entpackten Windows-Paket oder im Repository-Verzeichnis öffnen. Das Werkzeug liegt unter `tools/windows/Set-PawnIoUserAccess.ps1`. SID, Sicherungspfad und Geräteinstanz aus den vorigen Schritten eintragen:

```powershell
$userSid = Read-Host 'SID des normalen Anwendungsbenutzers'
$statePath = Read-Host 'Absoluter Sicherungspfad'
$deviceId = Read-Host 'PawnIO-Geräteinstanz'

powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\windows\Set-PawnIoUserAccess.ps1 `
    -UserSid $userSid -StatePath $statePath -DeviceInstanceId $deviceId -WhatIf
```

`-WhatIf` prüft den Zielzustand und meldet die geplante Änderung. Es verändert weder Geräteberechtigungen noch Sicherungsdateien. Für die ausdrücklich gewählte Freigabe denselben Aufruf **ohne `-WhatIf`** ausführen.

Das Werkzeug überschreibt keine vorhandene Sicherung und verweigert bereits vorhandene ACEs für das Zielkonto. Eine bestehende erfolgreiche Einrichtung nicht erneut ausführen. Neben der Sicherung entsteht `<Sicherungspfad>.result.json` mit vorheriger/neuer Berechtigung und dem Ergebnis des Geräteneustarts. Bei einem Fehler den gemeldeten Zustand prüfen; keinen allgemeinen Vollzugriff vergeben. Updates oder Neuinstallation des Treibers können eine erneute Prüfung der Geräteberechtigung erfordern.

## 4. Ohne Administratorrechte prüfen

LIS2 Control Center beenden und **normal** neu starten, auch wenn es vor der Treiberinstallation bereits lief. LibreHardwareMonitor ermittelt Teile des Treiberstatus beim Prozessstart.

Unter Hardware die Sensoren aktualisieren und nach CPU beziehungsweise Temperatur filtern. Für den Intel i7-14700K heißen die Werte unter anderem `CPU Package` und `Core Max`; Schlüssel und Sensornamen hängen von der Hardware ab. Sie lassen sich über den Variablenbrowser für Seiten, Livewerte und Lüfterregeln auswählen. Falls weiterhin Werte fehlen, die sichtbare Hardwarefehlermeldung, Treiberstatus und Geräteberechtigung prüfen. ACPI-Thermalzonen sind kein belegter Ersatz für CPU-Kerntemperaturen.

## 5. Benutzerfreigabe zurücknehmen

In einer erhöhten PowerShell dieselbe SID, dieselbe Geräteinstanz und die gesicherte Originaldatei verwenden:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\windows\Set-PawnIoUserAccess.ps1 `
    -UserSid $userSid -StatePath $statePath -DeviceInstanceId $deviceId -Restore
```

Die Rücknahme stellt die gesicherte Originalberechtigung wieder her und startet nur diese Geräteinstanz neu. Sie verweigert die Änderung, wenn Zielkonto/Gerät nicht zur Sicherung passen oder die Berechtigung seit der Freigabe anderweitig verändert wurde. Dann ist ein manueller Abgleich erforderlich. PawnIO wird dadurch nicht deinstalliert; andere Programme können den Treiber ebenfalls verwenden.

## Prüfnachweis

Am 28.10.2026 geprüft: Windows mit Intel Core i7-14700K, ASUS PRIME Z790-P, LibreHardwareMonitor 0.9.6 und signiertem PawnIO 2.2.0. Ohne Treiber sind die CPU-Temperatursensoren vorhanden, aber leer. Nach Installation bleiben sie ohne Gerätefreigabe im normalen Prozess leer; ein erhöhter Prozess liest Messwerte. Nach Benutzerfreigabe liest auch die reguläre `WindowsHardwareFanSensors`-Quelle im **nicht erhöhten Prozess** Werte, beispielsweise `CPU_Package = 63` und `Core_Max = 63`, und meldet keinen Verfügbarkeitsfehler.

Das Werkzeug besteht die tatsächliche Rücknahme und erneute Freigabe. Gegenproben weisen eine allgemeine SID, eine fremde Geräteinstanz und eine gegenüber der Sicherung veränderte Berechtigung ab. `-WhatIf` lässt Berechtigung und Dateien unverändert. Die persönliche Originalsicherung des geprüften Rechners liegt unter `C:\Users\andreasht\Documents\LIS2-PawnIO-Access.json`; die persönliche SID wird nicht in das Repository aufgenommen. Ein Neustart von Windows und andere CPU-/Mainboardmodelle sind damit nicht geprüft.

Quellen: [PawnIO-Beschreibung](https://pawnio.eu/), [PawnIO 2.2.0](https://github.com/namazso/PawnIO.Setup/releases/tag/2.2.0), [Geräteberechtigungen des Treibers](https://github.com/namazso/PawnIO/blob/master/PawnIO/PawnIO.inf.in), [LibreHardwareMonitor-Gerätezugriff](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/blob/v0.9.6/LibreHardwareMonitorLib/PawnIo/PawnIo.cs).
