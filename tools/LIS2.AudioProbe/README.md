# Direkte Legacy-Audioerfassung prüfen

Dieses Windows-x64-Konsolenwerkzeug verwendet dieselbe WASAPI-Erfassung und Signalberechnung wie die Anwendung. Es erfasst ausschließlich den angegebenen `wmplayer`-Prozess und seine Unterprozesse. Es speichert keine Audiodatei; der JSONL-Bericht enthält Zeitstempel, PID, empfangene Framezahl, Stereo-Spitzenpegel, zwanzig Frequenzbänder und Datenunterbrechungen.

Voraussetzungen: .NET-10-SDK, Windows-Build 20348 oder neuer und ein laufender Windows Media Player Legacy. Die Erfassung benötigt keine ausgewählte Visualisierung. Die bestehende Berichtsdatei wird nicht überschrieben.

```powershell
dotnet build tools/LIS2.AudioProbe/LIS2.AudioProbe.csproj -c Release
$player = Get-Process wmplayer
dotnet tools/LIS2.AudioProbe/bin/Release/net10.0-windows10.0.20348.0/LIS2.AudioProbe.dll $player.Id 600 .\audio-report.jsonl
dotnet test tests/LIS2.WmpLegacy.Tests/LIS2.WmpLegacy.Tests.csproj -c Release
```

Der Lauf endet nach der angegebenen Dauer oder mit Strg+C. Fremde Prozessnamen und ungültige Parameter werden zurückgewiesen; ein beendeter Zielprozess beendet die Prüfung mit einem Fehler. Es gibt keinen Wechsel zum Systemmix oder zur Visualisierungs-Pipe.

Für die Praxisprüfung mehrere MP3-Titel automatisch wechseln lassen, anschließend Pause/Fortsetzen, Seek und Minimieren prüfen. Der Bericht allein beweist keinen Titelwechsel: Playerzustand und Titel müssen mit Zeitstempeln separat beobachtet werden. Zur Quellenabgrenzung den Player pausieren und nur in einem anderen Prozess Audio wiedergeben; die Playerpegel müssen dabei still bleiben. Stille Musikpassagen sind kein Verbindungsverlust.

Der dokumentierte Referenzlauf und die GUI-Nachweise stehen im [Portierungsstatus](../../docs/prp/linux-port-status.md). Die API-Grundlage ist das [Microsoft-Beispiel für prozessbezogene Audioerfassung](https://learn.microsoft.com/en-us/samples/microsoft/windows-classic-samples/applicationloopbackaudio-sample/).
