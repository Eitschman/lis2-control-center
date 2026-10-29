# Anwendungsupdates

## Bedienung

Release v0.1.19 ist der erste Stand mit Updater und wird regulär installiert. Den Updateweg zwischen veröffentlichten Produktversionen prüfen wir mit dem darauffolgenden Release.

Unter Einstellungen zeigt „Updates“ die installierte Version und bietet eine manuelle Suche. Gefunden werden stabile Releases des öffentlichen Repositorys `Eitschman/lis2-control-center`. Vorabversionen und Entwürfe werden nicht angeboten. Versionsvergleich, Versionshinweise, Downloadfortschritt und Fehlerstatus stehen in derselben Karte. Ein verfügbarer neuer Stand erscheint zusätzlich als Schaltfläche unter der Kopfzeile.

„Automatisch nach Updates suchen“ prüft beim nächsten Start und anschließend täglich während der Laufzeit. Die Auswahl wird im Workspace gespeichert und ist zunächst ausgeschaltet. Eine fehlgeschlagene Netzwerkanfrage wird angezeigt und löst keine Installation aus. Die manuelle Suche ist unabhängig davon möglich.

„Update herunterladen“ lädt das passende Anwendungspaket und prüft seine SHA-256-Prüfsumme. Abbrechen beendet Suche oder Download. Der Downloadordner lässt sich nach erfolgreicher Prüfung öffnen. Die Prüfsumme wird vor einer Installation erneut geprüft.

## Windows

„Installieren und neu starten“ gilt für das portable x64-Anwendungspaket. Es bereitet einen neuen Installationsordner neben dem bisherigen Ordner vor. Vorhandene zusätzliche Dateien werden übernommen; Verknüpfungen in der Installation führen zu einem sichtbaren Fehler. Der übergeordnete Ordner muss beschreibbar sein. LIS2 fordert dafür keine Administratorrechte an.

Ein separater Prozess läuft aus einer temporären Anwendungskopie im Updateordner des Benutzerprofils. Erst nach gespeichertem Workspace und geordnetem Beenden von Display-, Audio- und Lüfterlaufzeit bestätigt die bisherige Anwendung den Austausch und beendet sich. Der Updateprozess wartet auf genau diesen Prozess einschließlich seiner Startzeit. Ohne bestätigten Shutdown wird nicht ausgetauscht.

Der bisherige Ordner wird als `.lis2-previous-<Kennung>` erhalten, der vorbereitete Ordner wird zur Installation. Die neue Anwendung muss innerhalb einer Minute ihr geöffnetes Hauptfenster und den abgeschlossenen Transportstart bestätigen. Ein gescheiterter Austausch oder Neustart stellt den bisherigen Ordner wieder her. Die wiederhergestellte Anwendung zeigt den Fehler in der Updatekarte an. Die Regeln zur seriellen Lüfterfreigabe bleiben erhalten; ein Update erteilt keine neue Hardwarefreigabe.

„Automatisch installieren“ ist eine getrennte Windows-Auswahl und zunächst ausgeschaltet. Sie setzt die automatische Suche voraus und erlaubt bei deren nächstem Fund ausdrücklich Download, Austausch und Neustart. Das Ausschalten der automatischen Suche schaltet auch die automatische Installation aus. Eine manuelle Suche allein startet keine automatische Installation.

Die Sicherung bleibt nach erfolgreichem Update erhalten. Bei Stromausfall oder gewaltsam beendetem Updateprozess kann eine manuelle Wiederherstellung nötig sein: Alle LIS2-Prozesse beenden, `install.json` der betroffenen Sitzung lesen und den dort genannten Sicherungsordner an den ursprünglichen Ort zurückverschieben. Bereits vorhandene Ordner vorher erhalten. Das Ergebnisprotokoll heißt `result.txt`; Updatepfade liegen unter `%LOCALAPPDATA%\LIS2ControlCenter\updates\<Kennung>`. Ein fehlgeschlagener vorbereiteter Ordner bleibt ebenfalls erhalten. Diese Dateien können nach Prüfung und beendetem LIS2 entfernt werden. Benutzerkonfiguration und Home-Assistant-Token liegen außerhalb des Installationsordners.

Native Plug-ins in Winamp- oder Legacy-Playerordnern werden nicht automatisch ausgetauscht. Das Update installiert die Anwendung und deren mitgelieferte Komponenten.

## Linux

Auf Debian-/Ubuntu-Systemen ist „Debian-Paket“ vorausgewählt. Nach dem Download öffnet „System-Paketinstallation öffnen“ das geprüfte `.deb` über `xdg-open`. Dafür werden `xdg-utils` und eine konfigurierte grafische Paketinstallation für `.deb` benötigt, beispielsweise Discover oder GNOME Software mit passender Paketunterstützung. Installation, Bestätigung und gegebenenfalls Rechteabfrage erfolgen dort. LIS2 wird anschließend vom Benutzer neu gestartet. Ein erfolgreicher Aufruf des Systemprogramms beweist noch keine erfolgreiche Paketinstallation.

Fehlt eine passende Dateizuordnung oder meldet das Systemprogramm einen Fehler, zeigt die Anwendung den Fehler. Das geprüfte Paket bleibt über „Downloadordner öffnen“ erreichbar. Es wird kein anderer Installer oder eine Shell mit erhöhten Rechten automatisch gestartet.

„Portables Archiv“ lädt das x64- oder ARM64-Archiv zur manuellen Installation. Auf Systemen ohne `dpkg` wird ausschließlich das Archiv angeboten. Portable Archive werden nicht automatisch über eine bestehende Installation entpackt. Das Paketformat kann vor der Suche gewählt werden; ein Wechsel verwirft die aktuelle Updateauswahl.

## Vertrauensmodell

Die Release-Metadaten kommen über HTTPS aus der offiziellen GitHub-API. Paket- und Prüfsummen-URLs müssen exakt zum gefundenen Tag und zum festgelegten Repository gehören. Architektur, Dateiname, stabile Versionsnummer und eindeutige Assetzuordnung werden geprüft. Downloads und Metadaten haben Größenlimits. Fehlende oder doppelte Prüfsummeneinträge sowie eine abweichende Prüfsumme verhindern die Übergabe.

GitHub, das Repository und dessen Veröffentlichungsberechtigungen sind die Vertrauensquelle. Die Prüfsummen liegen im selben Release; sie sind keine unabhängige digitale Signatur gegen eine kompromittierte Veröffentlichung. Es werden keine GitHub-Zugangsdaten benötigt. Öffentliche API-Limits und Netzwerkfehler werden angezeigt. ZIP-Dateien dürfen keine Pfadüberschreitungen, doppelten Zielpfade oder Verknüpfungen enthalten; erforderliche Anwendungsdateien und die Produktversion werden geprüft.

API-Vertrag: [GitHub Releases](https://docs.github.com/en/rest/releases/releases#get-the-latest-release). Die Desktopübergabe verwendet [xdg-open](https://manpages.debian.org/trixie/xdg-utils/xdg-open.1.en.html).

## Prüfnachweise

[Release-CI 40890232699](https://github.com/Eitschman/lis2-control-center/actions/runs/40890232699) für v0.1.19 besteht alle 14 Jobs. Linux besteht 600 Anwendungstests, Windows 599 mit einem übersprungenen Linux-Schlüsselbundtest. Die 40 Updatefälle und drei Update-Oberflächenfälle sind enthalten. Die veröffentlichten Pakete und alle neun Prüfsummen sind geprüft. Der echte Windows-Austausch und die Wiederherstellung bestehen im Paket-Smoke.

Die Anwendungstests prüfen Versionsvergleich, Plattformpakete, stabile Metadaten und offizielle URLs, Prüfsummen samt beschädigter Datei nach dem Download, HTTP-Fehler, Abbruch, ZIP-Pfade, Paketversion, Austausch, unterbrochenen Austausch und Wiederherstellung bei gescheitertem Neustart. Linux prüft die wörtliche Pfadübergabe an einen isolierten Desktopaufruf sowie dessen Fehlerstatus. Die tatsächliche Installation durch Discover oder GNOME Software ist eine eigene Desktopprüfung.

Die Oberflächentests prüfen Suche und Download mit isolierten Release-Fixtures, gespeicherte Einstellungen und deutsche/russische Darstellung bei 1305×893 und 950×600. Hilfe und statische Beschriftungen werden in allen fünf Anwendungssprachen geprüft. Test-Release 9.9.9 und Testpakete sind ausschließlich Fixtures.

`tools/packaging/smoke_windows_update.ps1` prüft mit einer selbstständigen Windows-Anwendung den echten externen Updateprozess und den gestarteten Desktop. Der Erfolgsfall verlangt den neuen Installationsmarker und einen geordnet beendeten Desktop-Smoke. Eine absichtlich fehlende Programmdatei erzwingt die Gegenprobe: alter Marker wiederhergestellt, alte Anwendung erneut geöffnet und geordnet beendet. Der Paketworkflow führt beide Fälle aus und lädt die Nachweise mit den Paket-Smokes hoch.
