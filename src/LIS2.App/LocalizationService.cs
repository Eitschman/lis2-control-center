using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace LIS2.App;

public enum AppLanguageMode
{
    System,
    English,
    German,
    French,
    Turkish,
    Russian
}

public static class LocalizationService
{
    private static readonly Dictionary<string, Dictionary<string, string>> Translations =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["de"] = new(StringComparer.Ordinal)
            {
                ["Duplicate"]="Duplizieren",
                ["Move up"]="Nach oben",
                ["Move down"]="Nach unten",
                ["Overflow"]="Überlauf",
                ["Ping-pong"]="Ping-Pong",
                ["Truncate"]="Abschneiden",
                ["Marquee"]="Laufschrift",
                ["Scroll timing"]="Scroll-Timing",
                ["Step (ms)"]="Schritt (ms)",
                ["Edge pause (ms)"]="Randpause (ms)",
                ["Live preview"]="Live-Vorschau",
                ["Dashboard"]="Übersicht", ["Display"]="Anzeige", ["Pages"]="Seiten", ["Events"]="Ereignisse",
                ["Hardware"]="Hardware", ["Fan Control"]="Lüftersteuerung", ["Settings"]="Einstellungen", ["Diagnostics"]="Diagnose",
                ["Transport"]="Transport", ["Connected"]="Verbunden", ["Disconnected"]="Nicht verbunden", ["Change..."]="Ändern...",
                ["Development build"]="Entwicklerversion", ["About"]="Über", ["Overview and quick access to the most important functions."]="Übersicht und Schnellzugriff auf die wichtigsten Funktionen.",
                ["VFD Preview"]="VFD-Vorschau", ["System Status"]="Systemstatus", ["Quick Links"]="Schnellzugriff", ["Open Display Controls"]="Anzeige öffnen",
                ["Open Fan Control"]="Lüftersteuerung öffnen", ["Recent Log"]="Letztes Protokoll", ["Quick Display Test"]="Schneller Anzeigetest",
                ["Line 1"]="Zeile 1", ["Line 2"]="Zeile 2", ["Send"]="Senden", ["Virtual device"]="Virtuelles Gerät", ["Render both lines"]="Beide Zeilen anzeigen",
                ["Clear"]="Leeren", ["Brightness"]="Helligkeit", ["Add"]="Hinzufügen", ["Delete"]="Löschen", ["Name"]="Name", ["Duration (s)"]="Dauer (s)",
                ["Visible when"]="Sichtbar wenn", ["Save page"]="Seite speichern", ["Test event (5 s)"]="Ereignis testen (5 s)",
                ["Nothing playing"]="Keine Wiedergabe", ["Waiting for Winamp..."]="Warte auf Winamp...", ["Waiting for Winamp"]="Warte auf Winamp",
                ["Playback"]="Wiedergabe", ["Unknown"]="Unbekannt", ["Playlist"]="Wiedergabeliste", ["Current position / total tracks"]="Aktuelle Position / Titel gesamt",
                ["Audio"]="Audio", ["Bitrate / sample rate"]="Bitrate / Abtastrate", ["Integration Status"]="Integrationsstatus", ["Available page variables"]="Verfügbare Seitenvariablen",
                ["Priority"]="Priorität", ["Info (10)"]="Info (10)", ["Notice (50)"]="Hinweis (50)", ["Warning (100)"]="Warnung (100)", ["Critical (200)"]="Kritisch (200)",
                ["Show event"]="Ereignis anzeigen", ["Test warning"]="Warnung testen", ["Clear all"]="Alle löschen", ["Active / queued events"]="Aktive / wartende Ereignisse",
                ["0 events"]="0 Ereignisse", ["Select an event to inspect it."]="Ereignis zur Anzeige auswählen.", ["Cancel selected"]="Ausgewähltes abbrechen",
                ["Hardware Monitoring"]="Hardwareüberwachung", ["Waiting for LibreHardwareMonitor values..."]="Warte auf LibreHardwareMonitor-Werte...",
                ["All types"]="Alle Typen", ["Temperature"]="Temperatur", ["Load"]="Auslastung", ["Fan"]="Lüfter", ["Clock"]="Takt", ["Voltage"]="Spannung", ["Power"]="Leistung",
                ["Data"]="Daten", ["Throughput"]="Durchsatz", ["Other"]="Andere", ["Refresh"]="Aktualisieren", ["Sensor"]="Sensor", ["Type"]="Typ", ["Value"]="Wert",
                ["Template key"]="Vorlagen-Schlüssel", ["Selected sensor"]="Ausgewählter Sensor", ["Select a sensor above."]="Oben einen Sensor auswählen.",
                ["Fan Outputs"]="Lüfterausgänge", ["Automatic control"]="Automatische Steuerung", ["Apply"]="Anwenden", ["Channels"]="Kanäle", ["Refresh sensors"]="Sensoren aktualisieren",
                ["Mode"]="Modus", ["Fixed"]="Fest", ["Curve"]="Kurve", ["Follow"]="Folgen", ["External"]="Extern", ["Off"]="Aus", ["Fixed %"]="Fest %", ["Minimum %"]="Minimum %",
                ["Maximum %"]="Maximum %", ["Fail-safe %"]="Notfall %", ["Allow 0% / stop"]="0 % / Stopp erlauben", ["Save fan channel"]="Lüfterkanal speichern",
                ["Device / Transport"]="Gerät / Transport", ["Virtual"]="Virtuell", ["Serial"]="Seriell", ["COM port"]="COM-Port", ["Apply / reconnect"]="Anwenden / neu verbinden",
                ["Appearance"]="Darstellung", ["Theme"]="Design", ["System default"]="Systemstandard", ["Light"]="Hell", ["Dark"]="Dunkel",
                ["System default follows the Windows app theme automatically."]="Systemstandard folgt automatisch dem Windows-App-Design.",
                ["Language"]="Sprache", ["System default uses the Windows display language."]="Systemstandard verwendet die Windows-Anzeigesprache.",
                ["Windows"]="Windows", ["Start LIS2 Control Center with Windows (minimized to tray)"]="LIS2 Control Center mit Windows starten (minimiert im Infobereich)",
                ["Diagnostics Snapshot"]="Diagnoseübersicht", ["Protocol Log"]="Protokoll-Log", ["Open LIS2 Control Center"]="LIS2 Control Center öffnen", ["Exit"]="Beenden",
                ["running in tray"]="läuft im Infobereich", ["No title"]="Kein Titel", ["Winamp connected"]="Winamp verbunden",
                ["Playing"]="Wiedergabe", ["Paused"]="Pausiert", ["Stopped"]="Gestoppt", ["Info"]="Info", ["Notice"]="Hinweis", ["Warning"]="Warnung", ["Critical"]="Kritisch",
                ["1 event"]="1 Ereignis", ["{0} events"]="{0} Ereignisse",
                ["VFD output, brightness and direct display tests."]="VFD-Ausgabe, Helligkeit und direkte Anzeigetests.",
                ["Create and edit the rotating 20x2 display pages."]="Rotierende 20x2-Anzeigeseiten erstellen und bearbeiten.",
                ["Winamp integration, pipe transport and available media variables."]="Winamp-Integration, Pipe-Transport und verfügbare Medienvariablen.",
                ["Priority notifications and temporary VFD overlays."]="Priorisierte Benachrichtigungen und temporäre VFD-Einblendungen.",
                ["LibreHardwareMonitor data sources and sensor availability."]="LibreHardwareMonitor-Datenquellen und Sensorverfügbarkeit.",
                ["Manual output, automatic control, curves and safety limits."]="Manuelle Ausgabe, automatische Steuerung, Kurven und Sicherheitsgrenzen.",
                ["LIS2 transport, COM port and Windows startup behavior."]="LIS2-Transport, COM-Port und Windows-Startverhalten.",
                ["Runtime state, data-source health and protocol traffic."]="Laufzeitstatus, Zustand der Datenquellen und Protokollverkehr.",
                ["Virtual LIS2"]="Virtuelles LIS2",
                ["State available when Virtual transport is active."]="Status verfügbar, wenn der virtuelle Transport aktiv ist.",
                ["Tip: Use the navigation on the left to access display settings, page editor, Winamp integration, hardware and fan control."]="Tipp: Über die Navigation links erreichst du Anzeigeeinstellungen, Seiteneditor, Winamp-Integration, Hardware und Lüftersteuerung.",
                ["Example: Winamp.State=Playing"]="Beispiel: Winamp.State=Playing",
                ["Listening on \\\\.\\pipe\\LIS2ControlCenter.Winamp"]="Lauscht auf \\\\.\\pipe\\LIS2ControlCenter.Winamp",
                ["Install gen_lis2.dll in Winamp's Plugins folder, or run LIS2.WinampSimulator while testing without Winamp."]="Installiere gen_lis2.dll im Plugins-Ordner von Winamp oder starte LIS2.WinampSimulator zum Testen ohne Winamp.",
                ["BUILD FINISHED"]="BUILD ABGESCHLOSSEN",
                ["0 errors"]="0 Fehler",
                ["Search by sensor name or key"]="Nach Sensorname oder Schlüssel suchen",
                ["Use for Fan 1"]="Für Lüfter 1 verwenden",
                ["Fan 1"]="Lüfter 1",
                ["Fan 2"]="Lüfter 2",
                ["Fan 3"]="Lüfter 3",
                ["Fan 4"]="Lüfter 4",
                ["Format: 40:40;60:80;75:100"]="Format: 40:40;60:80;75:100",
                ["Hardware source error: {0}"]="Fehler der Hardwarequelle: {0}",
                ["{0} visible sensor(s) / {1} total values"]="{0} sichtbare Sensor(en) / {1} Werte gesamt",
                ["Select a hardware sensor first."]="Zuerst einen Hardwaresensor auswählen.",
                ["Select a fan channel first."]="Zuerst einen Lüfterkanal auswählen.",
                ["Fixed output"]="Feste Ausgabe",
                ["Minimum output"]="Minimale Ausgabe",
                ["Maximum output"]="Maximale Ausgabe",
                ["Fail-safe output"]="Notfallausgabe",
                ["{0} must be between 0 and 100."]="{0} muss zwischen 0 und 100 liegen.",
                ["Minimum fan output must not be greater than maximum output."]="Die minimale Lüfterleistung darf nicht größer als die maximale sein.",
                ["Unknown fan mode '{0}'."]="Unbekannter Lüftermodus '{0}'.",
                ["Curve mode requires at least one temperature/output point."]="Der Kurvenmodus benötigt mindestens einen Temperatur-/Ausgabepunkt.",
                ["Select a page first."]="Zuerst eine Seite auswählen.",
                ["Page duration must be at least 1 second."]="Die Seitendauer muss mindestens 1 Sekunde betragen.",
                ["New page"]="Neue Seite",
                ["Select a COM port before using the serial transport."]="Vor Verwendung des seriellen Transports einen COM-Port auswählen.",
                ["Virtual state is unavailable while Serial transport is active."]="Der virtuelle Status ist nicht verfügbar, solange der serielle Transport aktiv ist.",
                ["Virtual LIS2 connected"]="Virtuelles LIS2 verbunden",
                ["Virtual LIS2 transport"]="Virtueller LIS2-Transport",
                ["State"]="Status",
                ["Port"]="Port",
                ["Frame line 1"]="Frame Zeile 1",
                ["Frame line 2"]="Frame Zeile 2",
                ["Automatic fan control"]="Automatische Lüftersteuerung",
                ["Data values"]="Datenwerte",
                ["Source"]="Quelle",
                ["OK"]="OK",
                ["ERROR"]="FEHLER",
                ["Time"]="Zeit",
                ["Fans"]="Lüfter",
                ["Event duration must be between 1 and 3600 seconds."]="Die Ereignisdauer muss zwischen 1 und 3600 Sekunden liegen.",
                ["Select a valid event priority."]="Eine gültige Ereignispriorität auswählen.",
                ["expired"]="abgelaufen",
                ["Expires"]="Läuft ab",
                ["ID"]="ID",
                ["Transport: {0}"]="Transport: {0}",
                ["Connected: {0}"]="Verbunden: {0}",
                ["Port: {0}"]="Port: {0}",
                ["Automatic fan control: {0}"]="Automatische Lüftersteuerung: {0}",
                ["Data values: {0}"]="Datenwerte: {0}",
                ["Source {0}: OK ({1} values)"]="Quelle {0}: OK ({1} Werte)",
                ["Source {0}: ERROR - {1}"]="Quelle {0}: FEHLER - {1}",
                ["Brightness: {0}"]="Helligkeit: {0}",
                ["Fans: {0}% / {1}% / {2}% / {3}%"]="Lüfter: {0}% / {1}% / {2}% / {3}%",
                ["Receiving snapshots on {0}"]="Empfängt Snapshots auf {0}",
                ["last update {0}"]="letzte Aktualisierung {0}",
                ["Listening on {0} — no recent plugin/simulator data."]="Lauscht auf {0} — keine aktuellen Plugin-/Simulator-Daten.",
                ["!! WARNING !!"]="!! WARNUNG !!",
                ["Test notification"]="Testbenachrichtigung",
                ["Invalid curve point '{0}'. Use temperature:percent, e.g. 60:80."]="Ungültiger Kurvenpunkt '{0}'. Verwende Temperatur:Prozent, z. B. 60:80.",
                ["LIS2 display writer is not initialized."]="Der LIS2-Display-Writer ist nicht initialisiert.",
                ["LIS2 device is not connected."]="Das LIS2-Gerät ist nicht verbunden.",
                ["Page"]="Seite",
                ["** EVENT TEST **"]="** EREIGNISTEST **",
                ["Overlay for 5 sec"]="Einblendung für 5 Sek.",
                ["Serial: {0}"]="Seriell: {0}",
                ["Time: {0}"]="Zeit: {0}",
                ["Frame line 1: {0}"]="Frame Zeile 1: {0}",
                ["Frame line 2: {0}"]="Frame Zeile 2: {0}",
                ["Fan {0}: {1}, mode={2}, sensor={3}, fixed={4}%, min={5}%, max={6}%, fail-safe={7}%"]="Lüfter {0}: {1}, Modus={2}, Sensor={3}, fest={4}%, min={5}%, max={6}%, Notfall={7}%",
                ["Unknown brightness."]="Unbekannte Helligkeit.",
                ["Yes"]="Ja",
                ["No"]="Nein",
                ["Fan {0}"]="Lüfter {0}",
                ["LIS2 Control Center could not start."]="LIS2 Control Center konnte nicht gestartet werden.",
                ["A diagnostic log was written to:"]="Ein Diagnoseprotokoll wurde geschrieben nach:",
                ["Unable to open the current-user Windows startup registry key."]="Der Windows-Autostart-Registrierungsschlüssel des aktuellen Benutzers konnte nicht geöffnet werden.",
                ["Unable to determine the application executable path."]="Der Pfad zur ausführbaren Anwendung konnte nicht ermittelt werden.",
                ["Windows autostart is available only when running the published LIS2ControlCenter.exe."]="Windows-Autostart ist nur mit der veröffentlichten LIS2ControlCenter.exe verfügbar."
            },
            ["fr"] = new(StringComparer.Ordinal)
            {
                ["Duplicate"]="Dupliquer",
                ["Move up"]="Monter",
                ["Move down"]="Descendre",
                ["Overflow"]="Débordement",
                ["Ping-pong"]="Ping-pong",
                ["Truncate"]="Tronquer",
                ["Marquee"]="Défilement",
                ["Scroll timing"]="Défilement",
                ["Step (ms)"]="Pas (ms)",
                ["Edge pause (ms)"]="Pause au bord (ms)",
                ["Live preview"]="Aperçu en direct",
                ["Dashboard"]="Tableau de bord", ["Display"]="Affichage", ["Pages"]="Pages", ["Events"]="Événements", ["Hardware"]="Matériel",
                ["Fan Control"]="Contrôle des ventilateurs", ["Settings"]="Paramètres", ["Diagnostics"]="Diagnostics", ["Connected"]="Connecté", ["Disconnected"]="Déconnecté",
                ["Change..."]="Modifier...", ["Development build"]="Version de développement", ["Overview and quick access to the most important functions."]="Vue d'ensemble et accès rapide aux fonctions principales.",
                ["VFD Preview"]="Aperçu VFD", ["System Status"]="État du système", ["Quick Links"]="Accès rapides", ["Open Display Controls"]="Ouvrir l'affichage",
                ["Open Fan Control"]="Ouvrir le contrôle des ventilateurs", ["Recent Log"]="Journal récent", ["Quick Display Test"]="Test rapide de l'affichage",
                ["Line 1"]="Ligne 1", ["Line 2"]="Ligne 2", ["Send"]="Envoyer", ["Virtual device"]="Périphérique virtuel", ["Render both lines"]="Afficher les deux lignes",
                ["Clear"]="Effacer", ["Brightness"]="Luminosité", ["Add"]="Ajouter", ["Delete"]="Supprimer", ["Duration (s)"]="Durée (s)", ["Visible when"]="Visible si",
                ["Save page"]="Enregistrer la page", ["Test event (5 s)"]="Tester l'événement (5 s)", ["Nothing playing"]="Aucune lecture", ["Waiting for Winamp..."]="En attente de Winamp...",
                ["Waiting for Winamp"]="En attente de Winamp", ["Playback"]="Lecture", ["Unknown"]="Inconnu", ["Playlist"]="Liste de lecture",
                ["Current position / total tracks"]="Position actuelle / nombre total de pistes", ["Bitrate / sample rate"]="Débit / fréquence d'échantillonnage",
                ["Integration Status"]="État de l'intégration", ["Available page variables"]="Variables de page disponibles", ["Priority"]="Priorité",
                ["Notice (50)"]="Notification (50)", ["Warning (100)"]="Avertissement (100)", ["Critical (200)"]="Critique (200)", ["Show event"]="Afficher l'événement",
                ["Test warning"]="Tester l'avertissement", ["Clear all"]="Tout effacer", ["Active / queued events"]="Événements actifs / en attente",
                ["0 events"]="0 événement", ["Select an event to inspect it."]="Sélectionnez un événement pour l'inspecter.", ["Cancel selected"]="Annuler la sélection",
                ["Hardware Monitoring"]="Surveillance du matériel", ["Waiting for LibreHardwareMonitor values..."]="En attente des valeurs LibreHardwareMonitor...",
                ["All types"]="Tous les types", ["Temperature"]="Température", ["Load"]="Charge", ["Fan"]="Ventilateur", ["Clock"]="Fréquence", ["Voltage"]="Tension", ["Power"]="Puissance",
                ["Data"]="Données", ["Throughput"]="Débit", ["Other"]="Autre", ["Refresh"]="Actualiser", ["Sensor"]="Capteur", ["Type"]="Type", ["Value"]="Valeur",
                ["Template key"]="Clé de modèle", ["Selected sensor"]="Capteur sélectionné", ["Select a sensor above."]="Sélectionnez un capteur ci-dessus.",
                ["Fan Outputs"]="Sorties ventilateurs", ["Automatic control"]="Contrôle automatique", ["Apply"]="Appliquer", ["Channels"]="Canaux", ["Refresh sensors"]="Actualiser les capteurs",
                ["Mode"]="Mode", ["Fixed"]="Fixe", ["Curve"]="Courbe", ["Follow"]="Suivre", ["External"]="Externe", ["Off"]="Arrêt", ["Minimum %"]="Minimum %", ["Maximum %"]="Maximum %",
                ["Fail-safe %"]="Sécurité %", ["Allow 0% / stop"]="Autoriser 0 % / arrêt", ["Save fan channel"]="Enregistrer le canal",
                ["Device / Transport"]="Périphérique / Transport", ["Virtual"]="Virtuel", ["Serial"]="Série", ["COM port"]="Port COM", ["Apply / reconnect"]="Appliquer / reconnecter",
                ["Appearance"]="Apparence", ["Theme"]="Thème", ["System default"]="Valeur système", ["Light"]="Clair", ["Dark"]="Sombre",
                ["System default follows the Windows app theme automatically."]="La valeur système suit automatiquement le thème des applications Windows.",
                ["Language"]="Langue", ["System default uses the Windows display language."]="La valeur système utilise la langue d'affichage de Windows.",
                ["Windows"]="Windows", ["Start LIS2 Control Center with Windows (minimized to tray)"]="Démarrer LIS2 Control Center avec Windows (réduit dans la zone de notification)",
                ["Diagnostics Snapshot"]="Aperçu des diagnostics", ["Protocol Log"]="Journal du protocole", ["Open LIS2 Control Center"]="Ouvrir LIS2 Control Center", ["Exit"]="Quitter",
                ["running in tray"]="actif dans la zone de notification", ["No title"]="Aucun titre", ["Winamp connected"]="Winamp connecté",
                ["Playing"]="Lecture", ["Paused"]="En pause", ["Stopped"]="Arrêté", ["Info"]="Info", ["Notice"]="Notification", ["Warning"]="Avertissement", ["Critical"]="Critique",
                ["1 event"]="1 événement", ["{0} events"]="{0} événements",
                ["VFD output, brightness and direct display tests."]="Sortie VFD, luminosité et tests directs de l'affichage.",
                ["Create and edit the rotating 20x2 display pages."]="Créer et modifier les pages d'affichage 20x2 en rotation.",
                ["Winamp integration, pipe transport and available media variables."]="Intégration Winamp, transport par pipe et variables média disponibles.",
                ["Priority notifications and temporary VFD overlays."]="Notifications prioritaires et affichages VFD temporaires.",
                ["LibreHardwareMonitor data sources and sensor availability."]="Sources LibreHardwareMonitor et disponibilité des capteurs.",
                ["Manual output, automatic control, curves and safety limits."]="Sortie manuelle, contrôle automatique, courbes et limites de sécurité.",
                ["LIS2 transport, COM port and Windows startup behavior."]="Transport LIS2, port COM et démarrage Windows.",
                ["Runtime state, data-source health and protocol traffic."]="État d'exécution, santé des sources et trafic du protocole.",
                ["Transport"]="Transport",
                ["Virtual LIS2"]="LIS2 virtuel",
                ["About"]="À propos",
                ["Name"]="Nom",
                ["Audio"]="Audio",
                ["Info (10)"]="Info (10)",
                ["Fixed %"]="Fixe %",
                ["State available when Virtual transport is active."]="État disponible lorsque le transport virtuel est actif.",
                ["Tip: Use the navigation on the left to access display settings, page editor, Winamp integration, hardware and fan control."]="Astuce : utilisez la navigation à gauche pour accéder aux réglages d'affichage, à l'éditeur de pages, à Winamp, au matériel et aux ventilateurs.",
                ["Example: Winamp.State=Playing"]="Exemple : Winamp.State=Playing",
                ["Listening on \\\\.\\pipe\\LIS2ControlCenter.Winamp"]="Écoute sur \\\\.\\pipe\\LIS2ControlCenter.Winamp",
                ["Install gen_lis2.dll in Winamp's Plugins folder, or run LIS2.WinampSimulator while testing without Winamp."]="Installez gen_lis2.dll dans le dossier Plugins de Winamp ou lancez LIS2.WinampSimulator pour tester sans Winamp.",
                ["BUILD FINISHED"]="BUILD TERMINÉ",
                ["0 errors"]="0 erreur",
                ["Search by sensor name or key"]="Rechercher par nom ou clé de capteur",
                ["Use for Fan 1"]="Utiliser pour ventilateur 1",
                ["Fan 1"]="Ventilateur 1",
                ["Fan 2"]="Ventilateur 2",
                ["Fan 3"]="Ventilateur 3",
                ["Fan 4"]="Ventilateur 4",
                ["Format: 40:40;60:80;75:100"]="Format : 40:40;60:80;75:100",
                ["Hardware source error: {0}"]="Erreur de source matérielle : {0}",
                ["{0} visible sensor(s) / {1} total values"]="{0} capteur(s) visible(s) / {1} valeurs au total",
                ["Select a hardware sensor first."]="Sélectionnez d'abord un capteur matériel.",
                ["Select a fan channel first."]="Sélectionnez d'abord un canal de ventilateur.",
                ["Fixed output"]="Sortie fixe",
                ["Minimum output"]="Sortie minimale",
                ["Maximum output"]="Sortie maximale",
                ["Fail-safe output"]="Sortie de sécurité",
                ["{0} must be between 0 and 100."]="{0} doit être compris entre 0 et 100.",
                ["Minimum fan output must not be greater than maximum output."]="La sortie minimale du ventilateur ne doit pas dépasser la sortie maximale.",
                ["Unknown fan mode '{0}'."]="Mode de ventilateur inconnu « {0} ».",
                ["Curve mode requires at least one temperature/output point."]="Le mode courbe nécessite au moins un point température/sortie.",
                ["Select a page first."]="Sélectionnez d'abord une page.",
                ["Page duration must be at least 1 second."]="La durée de la page doit être d'au moins 1 seconde.",
                ["New page"]="Nouvelle page",
                ["Select a COM port before using the serial transport."]="Sélectionnez un port COM avant d'utiliser le transport série.",
                ["Virtual state is unavailable while Serial transport is active."]="L'état virtuel n'est pas disponible lorsque le transport série est actif.",
                ["Virtual LIS2 connected"]="LIS2 virtuel connecté",
                ["Virtual LIS2 transport"]="Transport LIS2 virtuel",
                ["State"]="État",
                ["Port"]="Port",
                ["Frame line 1"]="Trame ligne 1",
                ["Frame line 2"]="Trame ligne 2",
                ["Automatic fan control"]="Contrôle automatique des ventilateurs",
                ["Data values"]="Valeurs de données",
                ["Source"]="Source",
                ["OK"]="OK",
                ["ERROR"]="ERREUR",
                ["Time"]="Heure",
                ["Fans"]="Ventilateurs",
                ["Event duration must be between 1 and 3600 seconds."]="La durée de l'événement doit être comprise entre 1 et 3600 secondes.",
                ["Select a valid event priority."]="Sélectionnez une priorité d'événement valide.",
                ["expired"]="expiré",
                ["Expires"]="Expire",
                ["ID"]="ID",
                ["Receiving snapshots on {0}"]="Réception des instantanés sur {0}",
                ["last update {0}"]="dernière mise à jour {0}",
                ["Listening on {0} — no recent plugin/simulator data."]="Écoute sur {0} — aucune donnée récente du plugin/simulateur.",
                ["!! WARNING !!"]="!! AVERTISSEMENT !!",
                ["Test notification"]="Notification de test",
                ["Invalid curve point '{0}'. Use temperature:percent, e.g. 60:80."]="Point de courbe invalide « {0} ». Utilisez température:pourcentage, ex. 60:80.",
                ["LIS2 display writer is not initialized."]="Le moteur d'affichage LIS2 n'est pas initialisé.",
                ["LIS2 device is not connected."]="Le périphérique LIS2 n'est pas connecté.",
                ["Page"]="Page",
                ["** EVENT TEST **"]="** TEST D'ÉVÉNEMENT **",
                ["Overlay for 5 sec"]="Affichage pendant 5 s",
                ["Brightness: {0}"]="Luminosité : {0}",
                ["Fans: {0}% / {1}% / {2}% / {3}%"]="Ventilateurs : {0}% / {1}% / {2}% / {3}%",
                ["Connected: {0}"]="Connecté : {0}",
                ["Serial: {0}"]="Série : {0}",
                ["Time: {0}"]="Heure : {0}",
                ["Transport: {0}"]="Transport : {0}",
                ["Port: {0}"]="Port : {0}",
                ["Frame line 1: {0}"]="Trame ligne 1 : {0}",
                ["Frame line 2: {0}"]="Trame ligne 2 : {0}",
                ["Automatic fan control: {0}"]="Contrôle automatique des ventilateurs : {0}",
                ["Data values: {0}"]="Valeurs de données : {0}",
                ["Source {0}: OK ({1} values)"]="Source {0} : OK ({1} valeurs)",
                ["Source {0}: ERROR - {1}"]="Source {0} : ERREUR - {1}",
                ["Fan {0}: {1}, mode={2}, sensor={3}, fixed={4}%, min={5}%, max={6}%, fail-safe={7}%"]="Ventilateur {0} : {1}, mode={2}, capteur={3}, fixe={4}%, min={5}%, max={6}%, sécurité={7}%",
                ["Unknown brightness."]="Luminosité inconnue.",
                ["Yes"]="Oui",
                ["No"]="Non",
                ["Fan {0}"]="Ventilateur {0}",
                ["LIS2 Control Center could not start."]="LIS2 Control Center n'a pas pu démarrer.",
                ["A diagnostic log was written to:"]="Un journal de diagnostic a été écrit dans :",
                ["Unable to open the current-user Windows startup registry key."]="Impossible d'ouvrir la clé de registre de démarrage Windows de l'utilisateur actuel.",
                ["Unable to determine the application executable path."]="Impossible de déterminer le chemin de l'exécutable de l'application.",
                ["Windows autostart is available only when running the published LIS2ControlCenter.exe."]="Le démarrage automatique Windows n'est disponible qu'avec la version publiée de LIS2ControlCenter.exe."
            },
            ["tr"] = new(StringComparer.Ordinal)
            {
                ["Duplicate"]="Çoğalt",
                ["Move up"]="Yukarı taşı",
                ["Move down"]="Aşağı taşı",
                ["Overflow"]="Taşma",
                ["Ping-pong"]="Ping-pong",
                ["Truncate"]="Kes",
                ["Marquee"]="Kayan yazı",
                ["Scroll timing"]="Kaydırma zamanlaması",
                ["Step (ms)"]="Adım (ms)",
                ["Edge pause (ms)"]="Kenar beklemesi (ms)",
                ["Live preview"]="Canlı önizleme",
                ["Dashboard"]="Gösterge Paneli", ["Display"]="Ekran", ["Pages"]="Sayfalar", ["Events"]="Olaylar", ["Hardware"]="Donanım",
                ["Fan Control"]="Fan Kontrolü", ["Settings"]="Ayarlar", ["Diagnostics"]="Tanılama", ["Connected"]="Bağlı", ["Disconnected"]="Bağlı değil",
                ["Change..."]="Değiştir...", ["Development build"]="Geliştirme sürümü", ["VFD Preview"]="VFD Önizleme", ["System Status"]="Sistem Durumu",
                ["Quick Links"]="Hızlı Bağlantılar", ["Recent Log"]="Son Günlük", ["Quick Display Test"]="Hızlı Ekran Testi", ["Line 1"]="Satır 1", ["Line 2"]="Satır 2",
                ["Send"]="Gönder", ["Virtual device"]="Sanal aygıt", ["Render both lines"]="İki satırı göster", ["Clear"]="Temizle", ["Brightness"]="Parlaklık",
                ["Add"]="Ekle", ["Delete"]="Sil", ["Duration (s)"]="Süre (sn)", ["Visible when"]="Şu durumda görünür", ["Save page"]="Sayfayı kaydet",
                ["Nothing playing"]="Oynatılmıyor", ["Waiting for Winamp..."]="Winamp bekleniyor...", ["Waiting for Winamp"]="Winamp bekleniyor", ["Playback"]="Oynatma",
                ["Unknown"]="Bilinmiyor", ["Playlist"]="Çalma Listesi", ["Audio"]="Ses", ["Integration Status"]="Entegrasyon Durumu", ["Available page variables"]="Kullanılabilir sayfa değişkenleri",
                ["Priority"]="Öncelik", ["Warning (100)"]="Uyarı (100)", ["Critical (200)"]="Kritik (200)", ["Show event"]="Olayı göster", ["Test warning"]="Uyarıyı test et",
                ["Clear all"]="Tümünü temizle", ["Active / queued events"]="Etkin / bekleyen olaylar", ["0 events"]="0 olay", ["Cancel selected"]="Seçileni iptal et",
                ["Hardware Monitoring"]="Donanım İzleme", ["All types"]="Tüm türler", ["Temperature"]="Sıcaklık", ["Load"]="Yük", ["Fan"]="Fan", ["Clock"]="Saat",
                ["Voltage"]="Voltaj", ["Power"]="Güç", ["Data"]="Veri", ["Throughput"]="Aktarım", ["Other"]="Diğer", ["Refresh"]="Yenile", ["Sensor"]="Sensör", ["Type"]="Tür", ["Value"]="Değer",
                ["Selected sensor"]="Seçili sensör", ["Fan Outputs"]="Fan Çıkışları", ["Automatic control"]="Otomatik kontrol", ["Apply"]="Uygula", ["Channels"]="Kanallar",
                ["Mode"]="Mod", ["Fixed"]="Sabit", ["Curve"]="Eğri", ["Follow"]="Takip", ["External"]="Harici", ["Off"]="Kapalı", ["Save fan channel"]="Fan kanalını kaydet",
                ["Device / Transport"]="Aygıt / Aktarım", ["Virtual"]="Sanal", ["Serial"]="Seri", ["COM port"]="COM portu", ["Apply / reconnect"]="Uygula / yeniden bağlan",
                ["Appearance"]="Görünüm", ["Theme"]="Tema", ["System default"]="Sistem varsayılanı", ["Light"]="Açık", ["Dark"]="Koyu",
                ["Language"]="Dil", ["System default uses the Windows display language."]="Sistem varsayılanı Windows görüntüleme dilini kullanır.",
                ["Windows"]="Windows", ["Diagnostics Snapshot"]="Tanılama Özeti", ["Protocol Log"]="Protokol Günlüğü",
                ["Open LIS2 Control Center"]="LIS2 Control Center'ı aç", ["Exit"]="Çıkış", ["running in tray"]="sistem tepsisinde çalışıyor",
                ["No title"]="Başlık yok", ["Winamp connected"]="Winamp bağlı", ["Playing"]="Oynatılıyor", ["Paused"]="Duraklatıldı", ["Stopped"]="Durduruldu",
                ["Info"]="Bilgi", ["Notice"]="Bildirim", ["Warning"]="Uyarı", ["Critical"]="Kritik", ["1 event"]="1 olay", ["{0} events"]="{0} olay",
                ["VFD output, brightness and direct display tests."]="VFD çıkışı, parlaklık ve doğrudan ekran testleri.",
                ["Create and edit the rotating 20x2 display pages."]="Dönen 20x2 ekran sayfalarını oluşturun ve düzenleyin.",
                ["Winamp integration, pipe transport and available media variables."]="Winamp entegrasyonu, pipe aktarımı ve kullanılabilir medya değişkenleri.",
                ["Priority notifications and temporary VFD overlays."]="Öncelikli bildirimler ve geçici VFD katmanları.",
                ["LibreHardwareMonitor data sources and sensor availability."]="LibreHardwareMonitor veri kaynakları ve sensör kullanılabilirliği.",
                ["Manual output, automatic control, curves and safety limits."]="Manuel çıkış, otomatik kontrol, eğriler ve güvenlik sınırları.",
                ["LIS2 transport, COM port and Windows startup behavior."]="LIS2 aktarımı, COM portu ve Windows başlangıç davranışı.",
                ["Runtime state, data-source health and protocol traffic."]="Çalışma durumu, veri kaynağı sağlığı ve protokol trafiği.",
                ["Transport"]="Aktarım",
                ["Virtual LIS2"]="Sanal LIS2",
                ["About"]="Hakkında",
                ["Overview and quick access to the most important functions."]="Genel bakış ve en önemli işlevlere hızlı erişim.",
                ["Open Display Controls"]="Ekran kontrollerini aç",
                ["Open Fan Control"]="Fan kontrolünü aç",
                ["Name"]="Ad",
                ["Test event (5 s)"]="Olayı test et (5 sn)",
                ["Current position / total tracks"]="Geçerli konum / toplam parça",
                ["Bitrate / sample rate"]="Bit hızı / örnekleme hızı",
                ["Info (10)"]="Bilgi (10)",
                ["Notice (50)"]="Bildirim (50)",
                ["Select an event to inspect it."]="İncelemek için bir olay seçin.",
                ["Waiting for LibreHardwareMonitor values..."]="LibreHardwareMonitor değerleri bekleniyor...",
                ["Template key"]="Şablon anahtarı",
                ["Select a sensor above."]="Yukarıdan bir sensör seçin.",
                ["Refresh sensors"]="Sensörleri yenile",
                ["Fixed %"]="Sabit %",
                ["Minimum %"]="Minimum %",
                ["Maximum %"]="Maksimum %",
                ["Fail-safe %"]="Güvenli %",
                ["Allow 0% / stop"]="0% / durmaya izin ver",
                ["System default follows the Windows app theme automatically."]="Sistem varsayılanı Windows uygulama temasını otomatik izler.",
                ["Start LIS2 Control Center with Windows (minimized to tray)"]="LIS2 Control Center Windows ile başlatılsın (sistem tepsisine küçültülmüş)",
                ["State available when Virtual transport is active."]="Sanal aktarım etkinken durum kullanılabilir.",
                ["Tip: Use the navigation on the left to access display settings, page editor, Winamp integration, hardware and fan control."]="İpucu: Ekran ayarları, sayfa düzenleyici, Winamp entegrasyonu, donanım ve fan kontrolüne soldaki menüden ulaşabilirsiniz.",
                ["Example: Winamp.State=Playing"]="Örnek: Winamp.State=Playing",
                ["Listening on \\\\.\\pipe\\LIS2ControlCenter.Winamp"]="Dinleniyor: \\\\.\\pipe\\LIS2ControlCenter.Winamp",
                ["Install gen_lis2.dll in Winamp's Plugins folder, or run LIS2.WinampSimulator while testing without Winamp."]="gen_lis2.dll dosyasını Winamp Plugins klasörüne yükleyin veya Winamp olmadan test etmek için LIS2.WinampSimulator'ı çalıştırın.",
                ["BUILD FINISHED"]="BUILD TAMAMLANDI",
                ["0 errors"]="0 hata",
                ["Search by sensor name or key"]="Sensör adı veya anahtara göre ara",
                ["Use for Fan 1"]="Fan 1 için kullan",
                ["Fan 1"]="Fan 1",
                ["Fan 2"]="Fan 2",
                ["Fan 3"]="Fan 3",
                ["Fan 4"]="Fan 4",
                ["Format: 40:40;60:80;75:100"]="Biçim: 40:40;60:80;75:100",
                ["Hardware source error: {0}"]="Donanım kaynağı hatası: {0}",
                ["{0} visible sensor(s) / {1} total values"]="{0} görünür sensör / toplam {1} değer",
                ["Select a hardware sensor first."]="Önce bir donanım sensörü seçin.",
                ["Select a fan channel first."]="Önce bir fan kanalı seçin.",
                ["Fixed output"]="Sabit çıkış",
                ["Minimum output"]="Minimum çıkış",
                ["Maximum output"]="Maksimum çıkış",
                ["Fail-safe output"]="Güvenli çıkış",
                ["{0} must be between 0 and 100."]="{0}, 0 ile 100 arasında olmalıdır.",
                ["Minimum fan output must not be greater than maximum output."]="Minimum fan çıkışı maksimum çıkıştan büyük olamaz.",
                ["Unknown fan mode '{0}'."]="Bilinmeyen fan modu '{0}'.",
                ["Curve mode requires at least one temperature/output point."]="Eğri modu en az bir sıcaklık/çıkış noktası gerektirir.",
                ["Select a page first."]="Önce bir sayfa seçin.",
                ["Page duration must be at least 1 second."]="Sayfa süresi en az 1 saniye olmalıdır.",
                ["New page"]="Yeni sayfa",
                ["Select a COM port before using the serial transport."]="Seri aktarımı kullanmadan önce bir COM portu seçin.",
                ["Virtual state is unavailable while Serial transport is active."]="Seri aktarım etkinken sanal durum kullanılamaz.",
                ["Virtual LIS2 connected"]="Sanal LIS2 bağlı",
                ["Virtual LIS2 transport"]="Sanal LIS2 aktarımı",
                ["State"]="Durum",
                ["Port"]="Port",
                ["Frame line 1"]="Çerçeve satır 1",
                ["Frame line 2"]="Çerçeve satır 2",
                ["Automatic fan control"]="Otomatik fan kontrolü",
                ["Data values"]="Veri değerleri",
                ["Source"]="Kaynak",
                ["OK"]="Tamam",
                ["ERROR"]="HATA",
                ["Time"]="Zaman",
                ["Fans"]="Fanlar",
                ["Event duration must be between 1 and 3600 seconds."]="Olay süresi 1 ile 3600 saniye arasında olmalıdır.",
                ["Select a valid event priority."]="Geçerli bir olay önceliği seçin.",
                ["expired"]="süresi doldu",
                ["Expires"]="Bitiş",
                ["ID"]="ID",
                ["Receiving snapshots on {0}"]="{0} üzerinden snapshot alınıyor",
                ["last update {0}"]="son güncelleme {0}",
                ["Listening on {0} — no recent plugin/simulator data."]="{0} dinleniyor — yakın zamanda plugin/simülatör verisi yok.",
                ["!! WARNING !!"]="!! UYARI !!",
                ["Test notification"]="Test bildirimi",
                ["Invalid curve point '{0}'. Use temperature:percent, e.g. 60:80."]="Geçersiz eğri noktası '{0}'. sıcaklık:yüzde biçimini kullanın, örn. 60:80.",
                ["LIS2 display writer is not initialized."]="LIS2 ekran yazıcısı başlatılmadı.",
                ["LIS2 device is not connected."]="LIS2 aygıtı bağlı değil.",
                ["Page"]="Sayfa",
                ["** EVENT TEST **"]="** OLAY TESTİ **",
                ["Overlay for 5 sec"]="5 sn gösterim",
                ["Brightness: {0}"]="Parlaklık: {0}",
                ["Fans: {0}% / {1}% / {2}% / {3}%"]="Fanlar: {0}% / {1}% / {2}% / {3}%",
                ["Connected: {0}"]="Bağlı: {0}",
                ["Serial: {0}"]="Seri: {0}",
                ["Time: {0}"]="Zaman: {0}",
                ["Transport: {0}"]="Aktarım: {0}",
                ["Port: {0}"]="Port: {0}",
                ["Frame line 1: {0}"]="Çerçeve satır 1: {0}",
                ["Frame line 2: {0}"]="Çerçeve satır 2: {0}",
                ["Automatic fan control: {0}"]="Otomatik fan kontrolü: {0}",
                ["Data values: {0}"]="Veri değerleri: {0}",
                ["Source {0}: OK ({1} values)"]="Kaynak {0}: Tamam ({1} değer)",
                ["Source {0}: ERROR - {1}"]="Kaynak {0}: HATA - {1}",
                ["Fan {0}: {1}, mode={2}, sensor={3}, fixed={4}%, min={5}%, max={6}%, fail-safe={7}%"]="Fan {0}: {1}, mod={2}, sensör={3}, sabit={4}%, min={5}%, maks={6}%, güvenli={7}%",
                ["Unknown brightness."]="Bilinmeyen parlaklık.",
                ["Yes"]="Evet",
                ["No"]="Hayır",
                ["Fan {0}"]="Fan {0}",
                ["LIS2 Control Center could not start."]="LIS2 Control Center başlatılamadı.",
                ["A diagnostic log was written to:"]="Tanılama günlüğü şu konuma yazıldı:",
                ["Unable to open the current-user Windows startup registry key."]="Geçerli kullanıcı için Windows başlangıç kayıt anahtarı açılamadı.",
                ["Unable to determine the application executable path."]="Uygulamanın çalıştırılabilir dosya yolu belirlenemedi.",
                ["Windows autostart is available only when running the published LIS2ControlCenter.exe."]="Windows otomatik başlatma yalnızca yayımlanmış LIS2ControlCenter.exe çalıştırılırken kullanılabilir."
            },
            ["ru"] = new(StringComparer.Ordinal)
            {
                ["Duplicate"]="Дублировать",
                ["Move up"]="Переместить вверх",
                ["Move down"]="Переместить вниз",
                ["Overflow"]="Переполнение",
                ["Ping-pong"]="Пинг-понг",
                ["Truncate"]="Обрезать",
                ["Marquee"]="Бегущая строка",
                ["Scroll timing"]="Параметры прокрутки",
                ["Step (ms)"]="Шаг (мс)",
                ["Edge pause (ms)"]="Пауза у края (мс)",
                ["Live preview"]="Предпросмотр",
                ["Dashboard"]="Панель", ["Display"]="Дисплей", ["Pages"]="Страницы", ["Events"]="События", ["Hardware"]="Оборудование",
                ["Fan Control"]="Управление вентиляторами", ["Settings"]="Настройки", ["Diagnostics"]="Диагностика", ["Connected"]="Подключено", ["Disconnected"]="Отключено",
                ["Change..."]="Изменить...", ["Development build"]="Версия для разработки", ["VFD Preview"]="Предпросмотр VFD", ["System Status"]="Состояние системы",
                ["Quick Links"]="Быстрые ссылки", ["Recent Log"]="Последний журнал", ["Quick Display Test"]="Быстрый тест дисплея", ["Line 1"]="Строка 1", ["Line 2"]="Строка 2",
                ["Send"]="Отправить", ["Virtual device"]="Виртуальное устройство", ["Render both lines"]="Показать обе строки", ["Clear"]="Очистить", ["Brightness"]="Яркость",
                ["Add"]="Добавить", ["Delete"]="Удалить", ["Duration (s)"]="Длительность (с)", ["Visible when"]="Показывать когда", ["Save page"]="Сохранить страницу",
                ["Nothing playing"]="Ничего не воспроизводится", ["Waiting for Winamp..."]="Ожидание Winamp...", ["Waiting for Winamp"]="Ожидание Winamp",
                ["Playback"]="Воспроизведение", ["Unknown"]="Неизвестно", ["Playlist"]="Плейлист", ["Audio"]="Аудио", ["Integration Status"]="Состояние интеграции",
                ["Available page variables"]="Доступные переменные страницы", ["Priority"]="Приоритет", ["Notice (50)"]="Уведомление (50)", ["Warning (100)"]="Предупреждение (100)",
                ["Critical (200)"]="Критическое (200)", ["Show event"]="Показать событие", ["Test warning"]="Тест предупреждения", ["Clear all"]="Очистить всё",
                ["Active / queued events"]="Активные / ожидающие события", ["0 events"]="0 событий", ["Cancel selected"]="Отменить выбранное",
                ["Hardware Monitoring"]="Мониторинг оборудования", ["All types"]="Все типы", ["Temperature"]="Температура", ["Load"]="Нагрузка", ["Fan"]="Вентилятор",
                ["Clock"]="Частота", ["Voltage"]="Напряжение", ["Power"]="Мощность", ["Data"]="Данные", ["Throughput"]="Пропускная способность", ["Other"]="Другое",
                ["Refresh"]="Обновить", ["Sensor"]="Датчик", ["Type"]="Тип", ["Value"]="Значение", ["Selected sensor"]="Выбранный датчик",
                ["Fan Outputs"]="Выходы вентиляторов", ["Automatic control"]="Автоматическое управление", ["Apply"]="Применить", ["Channels"]="Каналы",
                ["Mode"]="Режим", ["Fixed"]="Фиксированный", ["Curve"]="Кривая", ["Follow"]="Следовать", ["External"]="Внешний", ["Off"]="Выкл.",
                ["Save fan channel"]="Сохранить канал", ["Device / Transport"]="Устройство / Транспорт", ["Virtual"]="Виртуальный", ["Serial"]="Последовательный",
                ["COM port"]="COM-порт", ["Apply / reconnect"]="Применить / переподключить", ["Appearance"]="Оформление", ["Theme"]="Тема",
                ["System default"]="Системная", ["Light"]="Светлая", ["Dark"]="Тёмная", ["Language"]="Язык",
                ["System default uses the Windows display language."]="Системная настройка использует язык интерфейса Windows.",
                ["Windows"]="Windows", ["Diagnostics Snapshot"]="Снимок диагностики", ["Protocol Log"]="Журнал протокола",
                ["Open LIS2 Control Center"]="Открыть LIS2 Control Center", ["Exit"]="Выход", ["running in tray"]="работает в области уведомлений",
                ["No title"]="Нет названия", ["Winamp connected"]="Winamp подключён", ["Playing"]="Воспроизведение", ["Paused"]="Пауза", ["Stopped"]="Остановлено",
                ["Info"]="Информация", ["Notice"]="Уведомление", ["Warning"]="Предупреждение", ["Critical"]="Критическое", ["1 event"]="1 событие", ["{0} events"]="{0} событий",
                ["VFD output, brightness and direct display tests."]="Вывод VFD, яркость и прямые тесты дисплея.",
                ["Create and edit the rotating 20x2 display pages."]="Создание и редактирование чередующихся страниц 20x2.",
                ["Winamp integration, pipe transport and available media variables."]="Интеграция Winamp, pipe-транспорт и доступные медиапеременные.",
                ["Priority notifications and temporary VFD overlays."]="Приоритетные уведомления и временные VFD-оверлеи.",
                ["LibreHardwareMonitor data sources and sensor availability."]="Источники LibreHardwareMonitor и доступность датчиков.",
                ["Manual output, automatic control, curves and safety limits."]="Ручной вывод, автоматическое управление, кривые и пределы безопасности.",
                ["LIS2 transport, COM port and Windows startup behavior."]="Транспорт LIS2, COM-порт и запуск с Windows.",
                ["Runtime state, data-source health and protocol traffic."]="Состояние выполнения, источников данных и трафика протокола.",
                ["Transport"]="Транспорт",
                ["Virtual LIS2"]="Виртуальный LIS2",
                ["About"]="О программе",
                ["Overview and quick access to the most important functions."]="Обзор и быстрый доступ к основным функциям.",
                ["Open Display Controls"]="Открыть управление дисплеем",
                ["Open Fan Control"]="Открыть управление вентиляторами",
                ["Name"]="Имя",
                ["Test event (5 s)"]="Тест события (5 с)",
                ["Current position / total tracks"]="Текущая позиция / всего треков",
                ["Bitrate / sample rate"]="Битрейт / частота дискретизации",
                ["Info (10)"]="Информация (10)",
                ["Select an event to inspect it."]="Выберите событие для просмотра.",
                ["Waiting for LibreHardwareMonitor values..."]="Ожидание данных LibreHardwareMonitor...",
                ["Template key"]="Ключ шаблона",
                ["Select a sensor above."]="Выберите датчик выше.",
                ["Refresh sensors"]="Обновить датчики",
                ["Fixed %"]="Фикс. %",
                ["Minimum %"]="Минимум %",
                ["Maximum %"]="Максимум %",
                ["Fail-safe %"]="Аварийный %",
                ["Allow 0% / stop"]="Разрешить 0% / остановку",
                ["System default follows the Windows app theme automatically."]="Системная настройка автоматически следует теме приложений Windows.",
                ["Start LIS2 Control Center with Windows (minimized to tray)"]="Запускать LIS2 Control Center с Windows (свёрнуто в область уведомлений)",
                ["State available when Virtual transport is active."]="Состояние доступно при активном виртуальном транспорте.",
                ["Tip: Use the navigation on the left to access display settings, page editor, Winamp integration, hardware and fan control."]="Подсказка: используйте навигацию слева для доступа к настройкам дисплея, редактору страниц, Winamp, оборудованию и вентиляторам.",
                ["Example: Winamp.State=Playing"]="Пример: Winamp.State=Playing",
                ["Listening on \\\\.\\pipe\\LIS2ControlCenter.Winamp"]="Прослушивание \\\\.\\pipe\\LIS2ControlCenter.Winamp",
                ["Install gen_lis2.dll in Winamp's Plugins folder, or run LIS2.WinampSimulator while testing without Winamp."]="Установите gen_lis2.dll в папку Plugins Winamp или запустите LIS2.WinampSimulator для тестирования без Winamp.",
                ["BUILD FINISHED"]="СБОРКА ЗАВЕРШЕНА",
                ["0 errors"]="0 ошибок",
                ["Search by sensor name or key"]="Поиск по имени или ключу датчика",
                ["Use for Fan 1"]="Использовать для вентилятора 1",
                ["Fan 1"]="Вентилятор 1",
                ["Fan 2"]="Вентилятор 2",
                ["Fan 3"]="Вентилятор 3",
                ["Fan 4"]="Вентилятор 4",
                ["Format: 40:40;60:80;75:100"]="Формат: 40:40;60:80;75:100",
                ["Hardware source error: {0}"]="Ошибка источника оборудования: {0}",
                ["{0} visible sensor(s) / {1} total values"]="{0} видимых датчиков / всего {1} значений",
                ["Select a hardware sensor first."]="Сначала выберите аппаратный датчик.",
                ["Select a fan channel first."]="Сначала выберите канал вентилятора.",
                ["Fixed output"]="Фиксированный выход",
                ["Minimum output"]="Минимальный выход",
                ["Maximum output"]="Максимальный выход",
                ["Fail-safe output"]="Аварийный выход",
                ["{0} must be between 0 and 100."]="{0} должно быть от 0 до 100.",
                ["Minimum fan output must not be greater than maximum output."]="Минимальный выход вентилятора не должен превышать максимальный.",
                ["Unknown fan mode '{0}'."]="Неизвестный режим вентилятора '{0}'.",
                ["Curve mode requires at least one temperature/output point."]="Режим кривой требует хотя бы одной точки температура/выход.",
                ["Select a page first."]="Сначала выберите страницу.",
                ["Page duration must be at least 1 second."]="Длительность страницы должна быть не менее 1 секунды.",
                ["New page"]="Новая страница",
                ["Select a COM port before using the serial transport."]="Перед использованием последовательного транспорта выберите COM-порт.",
                ["Virtual state is unavailable while Serial transport is active."]="Виртуальное состояние недоступно при активном последовательном транспорте.",
                ["Virtual LIS2 connected"]="Виртуальный LIS2 подключён",
                ["Virtual LIS2 transport"]="Виртуальный транспорт LIS2",
                ["State"]="Состояние",
                ["Port"]="Порт",
                ["Frame line 1"]="Кадр строка 1",
                ["Frame line 2"]="Кадр строка 2",
                ["Automatic fan control"]="Автоматическое управление вентиляторами",
                ["Data values"]="Значения данных",
                ["Source"]="Источник",
                ["OK"]="OK",
                ["ERROR"]="ОШИБКА",
                ["Time"]="Время",
                ["Fans"]="Вентиляторы",
                ["Event duration must be between 1 and 3600 seconds."]="Длительность события должна быть от 1 до 3600 секунд.",
                ["Select a valid event priority."]="Выберите допустимый приоритет события.",
                ["expired"]="истекло",
                ["Expires"]="Истекает",
                ["ID"]="ID",
                ["Receiving snapshots on {0}"]="Получение снимков через {0}",
                ["last update {0}"]="последнее обновление {0}",
                ["Listening on {0} — no recent plugin/simulator data."]="Прослушивание {0} — нет свежих данных плагина/симулятора.",
                ["!! WARNING !!"]="!! ПРЕДУПРЕЖДЕНИЕ !!",
                ["Test notification"]="Тестовое уведомление",
                ["Invalid curve point '{0}'. Use temperature:percent, e.g. 60:80."]="Недопустимая точка кривой '{0}'. Используйте температура:процент, например 60:80.",
                ["LIS2 display writer is not initialized."]="Модуль вывода LIS2 не инициализирован.",
                ["LIS2 device is not connected."]="Устройство LIS2 не подключено.",
                ["Page"]="Страница",
                ["** EVENT TEST **"]="** ТЕСТ СОБЫТИЯ **",
                ["Overlay for 5 sec"]="Отображение 5 с",
                ["Brightness: {0}"]="Яркость: {0}",
                ["Fans: {0}% / {1}% / {2}% / {3}%"]="Вентиляторы: {0}% / {1}% / {2}% / {3}%",
                ["Connected: {0}"]="Подключено: {0}",
                ["Serial: {0}"]="Последовательный: {0}",
                ["Time: {0}"]="Время: {0}",
                ["Transport: {0}"]="Транспорт: {0}",
                ["Port: {0}"]="Порт: {0}",
                ["Frame line 1: {0}"]="Кадр строка 1: {0}",
                ["Frame line 2: {0}"]="Кадр строка 2: {0}",
                ["Automatic fan control: {0}"]="Автоматическое управление вентиляторами: {0}",
                ["Data values: {0}"]="Значения данных: {0}",
                ["Source {0}: OK ({1} values)"]="Источник {0}: OK ({1} значений)",
                ["Source {0}: ERROR - {1}"]="Источник {0}: ОШИБКА - {1}",
                ["Fan {0}: {1}, mode={2}, sensor={3}, fixed={4}%, min={5}%, max={6}%, fail-safe={7}%"]="Вентилятор {0}: {1}, режим={2}, датчик={3}, фикс={4}%, мин={5}%, макс={6}%, аварийный={7}%",
                ["Unknown brightness."]="Неизвестная яркость.",
                ["Yes"]="Да",
                ["No"]="Нет",
                ["Fan {0}"]="Вентилятор {0}",
                ["LIS2 Control Center could not start."]="Не удалось запустить LIS2 Control Center.",
                ["A diagnostic log was written to:"]="Диагностический журнал записан в:",
                ["Unable to open the current-user Windows startup registry key."]="Не удалось открыть раздел реестра автозапуска Windows текущего пользователя.",
                ["Unable to determine the application executable path."]="Не удалось определить путь к исполняемому файлу приложения.",
                ["Windows autostart is available only when running the published LIS2ControlCenter.exe."]="Автозапуск Windows доступен только при запуске опубликованного LIS2ControlCenter.exe."
            }
        };

    private sealed class OriginalValues
    {
        public string? Text { get; set; }
        public string? Content { get; set; }
        public string? Header { get; set; }
        public string? ToolTip { get; set; }
        public bool TextCaptured { get; set; }
        public bool ContentCaptured { get; set; }
        public bool HeaderCaptured { get; set; }
        public bool ToolTipCaptured { get; set; }
    }

    private static readonly ConditionalWeakTable<DependencyObject, OriginalValues> Originals = new();

    private static AppLanguageMode _mode = AppLanguageMode.System;
    private static string _languageCode = "en";

    public static AppLanguageMode Mode => _mode;
    public static string LanguageCode => _languageCode;
    public static event EventHandler? LanguageChanged;

    public static void Apply(AppLanguageMode mode)
    {
        _mode = mode;
        _languageCode = ResolveLanguageCode(mode);
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public static string Translate(string english)
    {
        if (string.IsNullOrEmpty(english) || _languageCode == "en")
            return english;

        return Translations.TryGetValue(_languageCode, out var language) &&
               language.TryGetValue(english, out var translated)
            ? translated
            : english;
    }

    public static string Format(string english, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Translate(english), args);

    public static void ApplyTo(DependencyObject root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var visited = new HashSet<DependencyObject>(ReferenceEqualityComparer.Instance);
        ApplyTo(root, visited);
    }

    private static void ApplyTo(
        DependencyObject root,
        HashSet<DependencyObject> visited)
    {
        if (!visited.Add(root))
            return;

        var originals = Originals.GetOrCreateValue(root);

        if (root is TextBlock textBlock &&
            !BindingOperations.IsDataBound(textBlock, TextBlock.TextProperty))
        {
            if (!originals.TextCaptured)
            {
                originals.Text = FindEnglishSource(textBlock.Text);
                originals.TextCaptured = true;
            }

            textBlock.Text = TranslateKnown(originals.Text ?? string.Empty);
        }

        if (root is ContentControl contentControl &&
            !BindingOperations.IsDataBound(contentControl, ContentControl.ContentProperty) &&
            contentControl.Content is string content)
        {
            if (!originals.ContentCaptured)
            {
                originals.Content = FindEnglishSource(content);
                originals.ContentCaptured = true;
            }

            contentControl.Content = TranslateKnown(originals.Content ?? string.Empty);
        }

        if (root is HeaderedContentControl headered &&
            !BindingOperations.IsDataBound(headered, HeaderedContentControl.HeaderProperty) &&
            headered.Header is string header)
        {
            if (!originals.HeaderCaptured)
            {
                originals.Header = FindEnglishSource(header);
                originals.HeaderCaptured = true;
            }

            headered.Header = TranslateKnown(originals.Header ?? string.Empty);
        }

        if (root is FrameworkElement element &&
            !BindingOperations.IsDataBound(element, FrameworkElement.ToolTipProperty) &&
            element.ToolTip is string tooltip)
        {
            if (!originals.ToolTipCaptured)
            {
                originals.ToolTip = FindEnglishSource(tooltip);
                originals.ToolTipCaptured = true;
            }

            element.ToolTip = TranslateKnown(originals.ToolTip ?? string.Empty);
        }

        // WPF only materializes the visual content of the selected TabItem.
        // Walk logical Content explicitly so controls on every page are localized,
        // not only the currently visible page.
        if (root is ContentControl { Content: DependencyObject contentObject })
            ApplyTo(contentObject, visited);

        if (root is HeaderedContentControl { Header: DependencyObject headerObject })
            ApplyTo(headerObject, visited);

        if (root is ItemsControl itemsControl)
        {
            foreach (var item in itemsControl.Items.OfType<DependencyObject>())
                ApplyTo(item, visited);
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
            ApplyTo(VisualTreeHelper.GetChild(root, index), visited);
    }

    private static string TranslateKnown(string value)
    {
        var english = FindEnglishSource(value);

        if (!string.Equals(english, value, StringComparison.Ordinal))
            return Translate(english);

        foreach (var language in Translations.Values)
        {
            foreach (var pair in language)
            {
                if (value.EndsWith(pair.Key, StringComparison.Ordinal))
                {
                    var prefix = value[..^pair.Key.Length];
                    if (prefix.All(ch => !char.IsLetterOrDigit(ch)))
                        return prefix + Translate(pair.Key);
                }
            }
        }

        return Translate(value);
    }

    private static string FindEnglishSource(string value)
    {
        foreach (var language in Translations.Values)
        {
            foreach (var pair in language)
            {
                if (string.Equals(pair.Value, value, StringComparison.Ordinal))
                    return pair.Key;
            }
        }

        return value;
    }

    private static string ResolveLanguageCode(AppLanguageMode mode) =>
        mode switch
        {
            AppLanguageMode.English => "en",
            AppLanguageMode.German => "de",
            AppLanguageMode.French => "fr",
            AppLanguageMode.Turkish => "tr",
            AppLanguageMode.Russian => "ru",
            _ => ResolveSystemLanguage()
        };

    private static string ResolveSystemLanguage()
    {
        var code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        return code is "de" or "fr" or "tr" or "ru" ? code : "en";
    }
}
