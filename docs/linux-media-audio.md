# Linux-Spieleraudio

Die Linux-Anwendung liest Metadaten über MPRIS und `playerctl`. Pegel und Spektrum erfasst sie zusätzlich aus dem PipeWire-Wiedergabestream des gewählten Players. Ein eigenes Plug-in und ein sichtbares Playerfenster sind dafür nicht erforderlich. Die Zielsystemprüfung mit Tuner auf dem Raspberry Pi steht aus; dieser Stand wird zunächst als GitHub-Entwicklungsartefakt ausgeliefert.

## Installation und Bedienung

Eine laufende PipeWire-Desktopsitzung mit Sitzungsmanager, beispielsweise WirePlumber, ist erforderlich. In Debian/Ubuntu werden außerdem diese Werkzeuge benötigt:

```sh
sudo apt install playerctl pipewire-bin dbus-bin
```

Die Debian-Pakete empfehlen diese Werkzeuge. Bei Installation ohne Empfehlungen müssen sie separat installiert werden. Eine reine PulseAudio-Sitzung genügt nicht. Die Anwendung richtet keinen Audioserver ein und ändert keine Audiogeräte oder deren Lautstärke.

Auf der Medienseite den MPRIS-Player auswählen und die Wiedergabe starten. Der obere Status betrifft Metadaten, der Bereich „Pegel / Spektrum“ die Audioerfassung. PipeWire-Knoten, Prozesskennung und Knotenseriennummer stehen unter den Balken und im Diagnosebericht. Metadaten werden sekündlich gelesen, Audiodaten und Balken ungefähr alle 100 ms aktualisiert.

Unter „Seiten“ stehen die Linux-Vorlagen Now Playing, VU und Spectrum zur Verfügung. Spectrum benötigt das integrierte Acht-Stufen-Glyphenset; dessen Installation ersetzt die acht benutzerdefinierten Zeichenplätze nach Bestätigung. Freie Vorlagen können beispielsweise diese Zeilen verwenden:

```text
{Media.Title}
{Media.Vu}

{Media.Artist}
{Media.SpectrumGlyphs}
```

`Media.VuLeft` und `Media.VuRight` liefern Zahlen von 0 bis 255. `Media.SpectrumRaw` liefert zwanzig Werte derselben Skala, `Media.SpectrumPeak` den höchsten Wert und `Media.Spectrum` eine Textdarstellung. `Media.AudioConnected` beschreibt ausschließlich die Audioverbindung. Die VU-/Spektrumvorlagen verwenden diese Variable für ihre Sichtbarkeit. Metadaten bleiben unabhängig davon verfügbar.

## Zuordnung und Fehler

Die Zuordnung erfolgt automatisch, ohne Auswahl der Installationsart. Bei nativen Programmen ermittelt die Anwendung die Prozesskennung des gewählten MPRIS-Dienstes über den Sitzungs-D-Bus und sucht dessen PipeWire-Wiedergabeknoten oder einen Knoten seiner Kindprozesse. Dabei werden sowohl direkte PipeWire-Verbindungen als auch die PulseAudio-Schnittstelle von PipeWire berücksichtigt. Normale Paketinstallation und AppImage benötigen keinen gesonderten Zuordnungsmodus.

Bei Flatpak verwendet die Anwendung die Anwendungs- und Instanzkennung des D-Bus-Proxys aus dessen `.flatpak-info`. Über `/proc` werden interne Sandbox-Prozessnummern den Host-Prozessen derselben Instanz zugeordnet. PipeWires Anwendungskennung des Audioclients muss zu dieser Identität passen. Native PipeWire-Clients können über ihre bestätigte Hostkennung direkt derselben Sandbox zugeordnet werden. Bei mehreren Instanzen derselben Flatpak-Anwendung ist ohne eindeutige Instanzkennung im Audioclient keine automatische Auswahl zulässig; die App zeigt einen Fehler. Dafür ist weder das externe `flatpak`-Werkzeug noch eine manuelle Konfiguration erforderlich.

Gibt es mehrere laufende passende Streams, wird die Erfassung mit einer Fehlermeldung angehalten. Ohne laufende Kandidaten wird nur ein einzelner passender Knoten akzeptiert. Es gibt keine automatische Aufnahme des Standardgeräts, Mikrofons oder gesamten Systemtons. Der Diagnosebericht enthält die D-Bus-Hostkennung und gegebenenfalls die erkannte Flatpak-Identität.

Fehlende Werkzeuge, ein nicht erreichbarer PipeWire-Server und fehlende oder mehrdeutige Streams bleiben als Audiostatus sichtbar. Gesperrte Prozessinformationen oder fehlende Sandbox-Identitäten verhindern eine bestätigte Zuordnung und führen ebenfalls zum Audiofehler. Tuner als Flatpak auf dem Pi ist weiterhin am Zielsystem zu prüfen. Bei Pause, Quellenwechsel und veralteten Audiodaten werden Pegel und Spektrum geleert. Neue Wiedergabeknoten werden sekündlich gesucht, etwa nach einem Titelwechsel. Die Audiodaten bleiben im Arbeitsspeicher und werden nicht gespeichert.

## Prüfung auf dem Raspberry Pi

Teststand: [Build 40853093985](https://github.com/Eitschman/lis2-control-center/actions/runs/40853093985), Code `a29e12f`, Paketversion `0.0~ci603.1`. Das [ARM64-Debian-Artefakt](https://github.com/Eitschman/lis2-control-center/actions/runs/40853093985/artifacts/12888402125) enthält `.deb` und SHA-256-Datei. Die GitHub-Anmeldung kann für den Download erforderlich sein; Entwicklungsartefakte werden 14 Tage aufbewahrt. Es handelt sich um einen Testbuild, kein neues Release.

Nach dem Entpacken auf dem Pi:

```sh
sha256sum -c lis2-control-center_0.0~ci603.1_arm64.deb.sha256
sudo apt install ./lis2-control-center_0.0~ci603.1_arm64.deb
```

APT kann dies wegen der niedrigeren Entwicklungsversionsnummer als Downgrade gegenüber `0.1.17` anzeigen. Die Einstellungen werden weiter aus dem vorhandenen Benutzerprofil gelesen.

1. ARM64-Debian-Artefakt des GitHub-Builds herunterladen, ZIP entpacken und das enthaltene `.deb` mit `sudo apt install ./<Paketname>.deb` installieren.
2. Tuner mit derselben Radio-URL starten und auf der Medienseite ausdrücklich auswählen. Titel, Laufzeit und beide Audiobalken prüfen.
3. Linux-VU- und Spektrumvorlage anlegen; auf Dashboard und Seitenansicht prüfen. Spektrum-Glyphen nur nach Zustimmung ersetzen.
4. Automatischen Titelwechsel, Pause/Fortsetzen, Player minimieren, Stream stoppen/starten und Player neu starten prüfen. Bei fehlendem Stream müssen Balken verschwinden, anschließend wieder erscheinen.
5. Wenn möglich gleichzeitig einen zweiten Player abspielen. Dessen Ton darf den gewählten Stream nicht ersetzen.
6. Bei Fehlern Diagnosebericht exportieren und Betriebssystem, Desktop, Player-/PipeWire-Version sowie Zeitpunkt und Aktion nennen. Token und private Metadaten vor Weitergabe prüfen.

Die lokalen Integrationstests verwenden einen privaten PipeWire-Server, D-Bus und synthetische Stereo-Testtöne. Sie prüfen direkte PipeWire-Wiedergabe, Pulse-Wiedergabe sowie eine Sandbox mit echtem D-Bus-Proxy, `.flatpak-info` und getrenntem PID-Namensraum. Alle Varianten prüfen Pegel, Frequenzbänder, Streamersetzung und Pause. Die Gegenprobe ohne Sandbox-Zuordnung reproduziert den Fehler „Kein Audiostream für den gewählten Player“ ausschließlich für die Sandbox. Diese Tests ersetzen den Test mit dem tatsächlichen Pi-Desktop und Tuner nicht.
