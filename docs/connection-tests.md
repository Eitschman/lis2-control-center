# Gezielte Verbindungstests

Unter **Diagnose → Verbindungstest** die Quelle auswählen und **Verbindung prüfen** starten. Die Auswahl enthält nur die zur Plattform passenden Quellen. Jeder Schritt meldet ausstehend, läuft, bestanden, fehlgeschlagen, übersprungen oder abgebrochen. Nach einem Fehler werden abhängige Schritte übersprungen. Eine konkrete Handlungsmöglichkeit steht am fehlgeschlagenen Schritt. **Quelle öffnen** führt zu Einstellungen, Hardware, Home Assistant oder Medien.

Die Ergebnisse sind eine Momentaufnahme mit lokalem Zeitpunkt. Ein erneuter Test ersetzt sie; ein Quellenwechsel leert sie. Änderungen der Oberflächensprache erhalten die Ergebnisse. **Bericht exportieren** übernimmt die Ergebnisse und ihren Zeitpunkt in den Diagnosebericht. Der Test speichert keine Einstellungen und erteilt keine Gerätefreigabe.

| Quelle | Prüfschritte | Aussagegrenze |
| --- | --- | --- |
| Virtuelles LIS2 | Bestehende virtuelle Verbindung | Kein Befehl wird gesendet. |
| Serielles LIS2 | Angewendete Portauswahl, Erkennung im Betriebssystem, bereits geöffnete Verbindung | Der Test öffnet keinen Port. Ein erkannter Port bestätigt weder LIS2-Identität noch Zugriffsrechte. Eine geschlossene Verbindung muss über Einstellungen angewendet werden. Es gibt keinen seriellen Schreib-, Lüfter- oder Geräteidentifikationstest. |
| Hardware | Vorhandene endliche Sensormessungen und Aktualität unter zehn Sekunden | Bestehende Sensorabfrage verwenden; fehlende Sensoren und Berechtigungen bleiben sichtbar. |
| Home Assistant | Adresse, vorhandenes Token, WebSocket-Verbindung, Begrüßung, Anmeldung | Separate Testverbindung mit den aktuell eingegebenen Werten. Keine Tokenpersistenz, keine Änderung der laufenden Quelle, keine Entitätsabfrage und kein Dienstaufruf. |
| Winamp | Bestehende Metadaten-Pipe, Aktualität unter fünf Sekunden, aktuelle VU-Telemetrie bei Wiedergabe | Der Test ersetzt oder startet kein Plug-in und verändert keine Wiedergabe. Nullpegel sind gültig. |
| Windows Media Player Legacy | Metadaten-Pipe, Aktualität, vorhandene Prozess-Audioerfassung bei Wiedergabe | Kein Registrieren von DLLs und kein Starten des Players. |
| Windows Media Player | GSMTC-Anbieter und vorhandene Playersitzung | Kein eigener VU-Test, da diese Quelle keine Audiotelemetrie liefert. Unveränderte Metadaten werden nicht allein wegen ihres Alters abgelehnt. |
| Linux-Medien | playerctl vorhanden, ausgewählte bestehende MPRIS-Sitzung, Aktualität; bei Wiedergabe zusätzlich pw-dump, pw-record, dbus-send und vorhandene Player-Audioerfassung | Native und Flatpak-Player verwenden dieselben Prüfschritte. Der Test ändert die Playerauswahl nicht und startet keine zusätzliche Aufnahme. |

Ohne laufende Wiedergabe werden Audioabhängigkeiten und Audioprüfung ausdrücklich übersprungen. Erfolgreiche Metadaten bedeuten daher keine bestätigte Audioübertragung. Der aktuelle PipeWire-/WASAPI-Audiostatus berücksichtigt die Fristen der jeweiligen Quelle.

**Test abbrechen** fordert den kooperativen Abbruch an. Der Runner setzt nach zwölf Sekunden ein Abbruchsignal; die Home-Assistant-Testverbindung hat zusätzlich zehn Sekunden Zeitlimit. Bereits laufende Schritte und ihre Bereinigung werden vor dem Ende des Testtasks abgewartet. Das geordnete Beenden der Anwendung bricht einen laufenden Test ab und wartet ihn ebenfalls ab.

Netzwerkfehler enthalten ausschließlich eine Fehlerkategorie und gegebenenfalls einen technischen Socket- oder Betriebssystemcode. Ausnahme-Nachrichten, fremde Servertexte, URLs und Zugangsdaten werden nicht in die Verbindungstestergebnisse übernommen. Bei Home Assistant wird ein abgelehntes Token am Anmeldeschritt sichtbar, ohne die Antwort des Servers auszugeben.

Die vorhandenen Suiten prüfen Erfolg und Fehlschlag, fehlende Ports und Werkzeuge, veraltete oder ungültige Daten, Nullpegel, Audio ohne Wiedergabe, Abbruch, Zeitüberschreitung, Wiederholung und Shutdown. Lokale WebSocket-Fixtures liefern erfolgreiche Anmeldung, Ablehnung, unerwartete Begrüßung und ausbleibende Antwort. Sie ersetzen keine reale Verbindung zu Benutzergeräten. Hilfe und Bedienung sind auf Deutsch, Englisch, Französisch, Türkisch und Russisch verfügbar.
