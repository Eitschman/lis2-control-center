using System.Globalization;
using System.Windows;
using System.Windows.Controls;
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
                ["Runtime state, data-source health and protocol traffic."]="Laufzeitstatus, Zustand der Datenquellen und Protokollverkehr."
            },
            ["fr"] = new(StringComparer.Ordinal)
            {
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
                ["Runtime state, data-source health and protocol traffic."]="État d'exécution, santé des sources et trafic du protocole."
            },
            ["tr"] = new(StringComparer.Ordinal)
            {
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
                ["Runtime state, data-source health and protocol traffic."]="Çalışma durumu, veri kaynağı sağlığı ve protokol trafiği."
            },
            ["ru"] = new(StringComparer.Ordinal)
            {
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
                ["Runtime state, data-source health and protocol traffic."]="Состояние выполнения, источников данных и трафика протокола."
            }
        };

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

    public static void ApplyTo(DependencyObject root)
    {
        if (root is TextBlock textBlock)
            textBlock.Text = TranslateKnown(textBlock.Text);

        if (root is ContentControl contentControl && contentControl.Content is string content)
            contentControl.Content = TranslateKnown(content);

        if (root is HeaderedContentControl headered && headered.Header is string header)
            headered.Header = TranslateKnown(header);

        if (root is FrameworkElement element && element.ToolTip is string tooltip)
            element.ToolTip = TranslateKnown(tooltip);

        if (root is ItemsControl itemsControl)
        {
            foreach (var item in itemsControl.Items.OfType<DependencyObject>())
                ApplyTo(item);
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
            ApplyTo(VisualTreeHelper.GetChild(root, index));
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
