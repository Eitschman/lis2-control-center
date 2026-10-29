# Vorlagenprüfung und Datenaktualität

## Seiteneditor

Die Prüfung steht direkt unter jeder Zeile. Ein Hinweis ist anklickbar und markiert den betroffenen Ausdruck oder die fehlerhafte Operation im Eingabefeld. Tastaturfokus und Aktivierung über die Tastatur werden unterstützt.

Rot kennzeichnet unbekannte statische Variablen, fehlende Glyphen, Klammerfehler sowie ungültige Formatierungsoperationen. `number`, `percent` und `bytes` erlauben null bis sechs Nachkommastellen. `upper` und `lower` haben kein Argument; Textoperationen benötigen den Doppelpunkt. Präfix- und Suffix-Leerzeichen bleiben erhalten.

Orange kennzeichnet gültige, derzeit nicht verfügbare Variablen und noch nicht entdeckte Hardware-/HA-Schlüssel. Dynamische Schlüssel lassen sich ohne verbundene Quelle nicht sicher als Tippfehler einstufen. Bereits entdeckte Schlüssel und konfigurierte Hardware-Schlüssel bleiben für die Prüfung bekannt. Glyphennamen werden dagegen mit dem aktuell geladenen Satz verglichen.

Beispiele:

```text
{Clock.Time|number:no}                       ungültige Nachkommastellen
{Clock.Tiem}                                 unbekannte statische Variable
{Glyph.missing}                              Glyphe fehlt im aktuellen Satz
{HA.sensor.room|number:1|suffix: °C}           gültige Pipeline; Schlüssel im Browser wählen
{Winamp.Artist|fallbackvar:Winamp.StreamName}  beide Schlüssel prüfen
```

Ersatzvariablen werden ebenfalls geprüft. Ein Hinweis kann bestehen bleiben, obwohl eine andere Variable oder ein Ersatztext die Ausgabe liefert. Die Prüfung verändert die Vorlagen nicht, blockiert das Speichern nicht und schaltet keinen anderen Quelltyp ein. Korrekturen aktualisieren die Hinweise und die bestehende Livevorschau.

## Datenaktualität

Hardware-, Medien- und Home-Assistant-Ansichten zeigen Zustand, lokalen Zeitpunkt und Alter der zuletzt gültigen Daten. Die Darstellung verwendet die Regionseinstellungen des Betriebssystems. Die Oberflächensprache verändert diese Einstellungen nicht.

| Quelle | Bedeutung und Grenze |
| --- | --- |
| Hardware gesamt | Letzte erfolgreiche Erfassung mit mindestens einem verfügbaren Sensor; nach zehn Sekunden ohne neue gültige Erfassung veraltet |
| Ausgewählter Sensor | Letzte Erfassung dieses Schlüssels; fehlende Messungen behalten den bisherigen Zeitpunkt und zeigen Nicht verfügbar |
| Winamp / WMP Legacy | Alter des vom Provider empfangenen Metadaten-Snapshots; nach fünf Sekunden ohne neue Daten veraltet, Verbindungsverlust separat sichtbar |
| Linux MPRIS | Zeitpunkt der letzten erfolgreichen Playerabfrage; nach fünf Sekunden ohne neue Daten veraltet |
| Windows Media Player | Ereignisgesteuerter Snapshot; kein künstlicher Timeout bei unveränderter, verbundener Sitzung |
| Home Assistant | Letzter Datenempfang des WebSocket-Quellsatzes; kein Timeout allein wegen unveränderter Werte |

Die Hardwareliste und die ausgewählte Sensordiagnose nutzen tatsächliche Erfassungen. UI-Timer und Ansichtswechsel erzeugen keine Messzeitpunkte. Noch nie empfangene Daten zeigen keinen erfundenen Zeitpunkt. Verbindungsverlust und Wiederverbindung werden unabhängig vom Datenalter dargestellt. Eine neue gültige Messung setzt den Zeitpunkt fort; fehlende Werte verjüngen ihn nicht.

Home Assistant zeigt keinen individuellen Messzeitpunkt je Entität. Ein Sensor kann unverändert bleiben, während andere Entitäten Ereignisse senden. Die Anzeige nennt daher ausdrücklich den Datenempfang. Der letzte Empfang bleibt beim Verbindungsverlust erhalten und wird beim Neukonfigurieren der Quelle zurückgesetzt. Audiostatus und Metadaten bleiben unabhängig; diese Aktualitätszeilen beschreiben die Metadaten, die vorhandenen Audioanzeigen weiterhin den Audiokanal.

## Prüfungen

`SourceQualityTests` prüft gültige und fehlerhafte Vorlagen, Markierungsbereiche, fehlende Quellen, Wiederkehr, Fristen und alle vier Übersetzungen. `SourceQualityUiTests` spielt Markierung, Korrektur, Vorschau und Quellzustände mit tatsächlichen Avalonia-Steuerelementen durch und rendert breite beziehungsweise schmale Ansichten in fünf Sprachen. Der private HA-WebSocket-Test prüft den erhaltenen Empfangszeitpunkt bei Verbindungsverlust und dessen Zurücksetzen bei Neukonfiguration.
