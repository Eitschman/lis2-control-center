# Sensor- und Lüfterverlauf

In der Lüftersteuerung zeigt der aufklappbare Verlauf einen der vier Kanäle. Zur Auswahl stehen die letzten 5, 15 oder 60 Minuten. Drei getrennte Skalen verwenden dieselbe Zeitachse:

- Regelsensor: der in der gespeicherten Kanalkonfiguration gewählte Sensor, mit dessen Einheit. Bei einer Temperaturquelle ist dies die Temperatur; andere Regelquellen bleiben möglich.
- Berechneter Sollwert: Prozentwert aus der gespeicherten Konfiguration einschließlich Grenzen, Hysterese und Fail-safe. Er bestätigt keine tatsächlich ausgegebene Hardwareleistung. Ungespeicherte Änderungen der Kurvenpunkte fließen erst nach dem Speichern ein.
- Gemessene Drehzahl: ausschließlich der über das Zahnrad der Dashboard-Lüfterkarte zugeordnete Drehzahlsensor. Das LIS2 liefert keine bestätigte Drehzahlrückmeldung. Ohne Zuordnung steht dort ausdrücklich „Kein Drehzahlsensor zugeordnet“.

Die vorhandene Hardwareabfrage liefert alle zwei Sekunden einen gemeinsamen Snapshot. Der Verlauf löst keine zusätzliche Abfrage aus und erteilt keine Hardwarefreigabe. Fehlende, ungültige oder nicht endliche Messwerte bleiben Lücken; eine gemessene Drehzahl von null ist gültig. Liegen zwischen zwei Erfassungen mehr als fünf Sekunden, verbindet das Diagramm sie nicht. Fehlt ein benötigter Regelsensor, bleibt seine Linie unterbrochen, während der berechnete Sollwert den konfigurierten Fail-safe zeigt.

Für die Kurveneinstellung einen Kanal im Verlauf wählen, dessen Messungen beobachten und die gespeicherten Kurvenpunkte im selben Menü bearbeiten. Der Verlaufskanal ist unabhängig vom Bearbeitungskanal. Änderungen der gespeicherten Kanalkonfiguration oder der Drehzahlquelle leeren den Verlauf dieses Kanals, damit unterschiedliche Sensoren und Regelparameter nicht miteinander vermischt werden. Andere Kanäle bleiben erhalten.

Es werden ausschließlich die letzten 60 Minuten im Arbeitsspeicher gehalten, maximal 1801 Messpunkte je Kanal, insgesamt höchstens 7204 Punkte. Häufigere manuelle Erfassungen können das tatsächlich verfügbare Zeitfenster verkürzen. Die Daten werden weder in den Workspace noch in Dateien oder eine Datenbank geschrieben. Beim Beenden gehen sie verloren; nach dem Neustart beginnt ein leerer Verlauf.

Die Hilfe und Bedienung sind auf Deutsch, Englisch, Französisch, Türkisch und Russisch verfügbar. Zahlen folgen weiterhin den Regionseinstellungen des Betriebssystems, unabhängig von der Sprache der Oberfläche.
