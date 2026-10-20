using Avalonia.Controls;
using System.Globalization;

namespace LIS2.App.Avalonia;

public partial class MainWindow
{
    private static readonly string[] SettingsKeys =
    [
        "Dashboard", "Display", "Pages", "Media", "Events", "Hardware", "Fan Control",
        "Settings", "Diagnostics", "Home Assistant",
        "Device / Transport", "Appearance", "Windows", "Transport", "COM port",
        "Theme", "Language", "Virtual", "Serial", "Refresh", "Apply / reconnect",
        "System default", "Light", "Dark", "English", "German", "French", "Turkish", "Russian",
        "System default follows the operating system theme.",
        "System default uses the operating system display language.",
        "Start LIS2 Control Center with Windows (minimized to tray)",
        "LIS2 transport, COM port and Windows startup behavior."
    ];

    private static readonly Dictionary<string, string[]> SettingsTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["de"] =
        [
            "Übersicht", "Anzeige", "Seiten", "Medien", "Ereignisse", "Hardware", "Lüftersteuerung",
            "Einstellungen", "Diagnose", "Home Assistant",
            "Gerät / Transport", "Darstellung", "Windows", "Transport", "COM-Port",
            "Design", "Sprache", "Virtuell", "Seriell", "Aktualisieren", "Anwenden / neu verbinden",
            "Systemstandard", "Hell", "Dunkel", "Englisch", "Deutsch", "Französisch", "Türkisch", "Russisch",
            "Systemstandard folgt automatisch dem Betriebssystem-Design.",
            "Systemstandard verwendet die Anzeigesprache des Betriebssystems.",
            "LIS2 Control Center mit Windows starten (minimiert im Infobereich)",
            "LIS2-Transport, COM-Port und Windows-Startverhalten."
        ],
        ["fr"] =
        [
            "Tableau de bord", "Affichage", "Pages", "Médias", "Événements", "Matériel", "Ventilateurs",
            "Paramètres", "Diagnostic", "Home Assistant",
            "Appareil / Transport", "Apparence", "Windows", "Transport", "Port COM",
            "Thème", "Langue", "Virtuel", "Série", "Actualiser", "Appliquer / reconnecter",
            "Par défaut du système", "Clair", "Sombre", "Anglais", "Allemand", "Français", "Turc", "Russe",
            "Le thème par défaut suit celui du système.",
            "La langue par défaut suit celle du système.",
            "Démarrer LIS2 Control Center avec Windows (réduit dans la zone de notification)",
            "Transport LIS2, port COM et démarrage Windows."
        ],
        ["tr"] =
        [
            "Genel Bakış", "Ekran", "Sayfalar", "Medya", "Olaylar", "Donanım", "Fan Kontrolü",
            "Ayarlar", "Tanılama", "Home Assistant",
            "Cihaz / Bağlantı", "Görünüm", "Windows", "Bağlantı", "COM bağlantı noktası",
            "Tema", "Dil", "Sanal", "Seri", "Yenile", "Uygula / yeniden bağlan",
            "Sistem varsayılanı", "Açık", "Koyu", "İngilizce", "Almanca", "Fransızca", "Türkçe", "Rusça",
            "Sistem varsayılanı işletim sistemi temasını kullanır.",
            "Sistem varsayılanı işletim sistemi dilini kullanır.",
            "LIS2 Control Center'ı Windows ile başlat (sistem tepsisine küçült)",
            "LIS2 bağlantısı, COM bağlantı noktası ve Windows başlangıcı."
        ],
        ["ru"] =
        [
            "Обзор", "Дисплей", "Страницы", "Медиа", "События", "Оборудование", "Вентиляторы",
            "Настройки", "Диагностика", "Home Assistant",
            "Устройство / Подключение", "Внешний вид", "Windows", "Подключение", "COM-порт",
            "Тема", "Язык", "Виртуальный", "Последовательный", "Обновить", "Применить / переподключить",
            "Системная", "Светлая", "Тёмная", "Английский", "Немецкий", "Французский", "Турецкий", "Русский",
            "Системная тема следует настройкам ОС.",
            "Системный язык следует настройкам ОС.",
            "Запускать LIS2 Control Center вместе с Windows (в области уведомлений)",
            "Подключение LIS2, COM-порт и запуск Windows."
        ]
    };

    private string CurrentUiLanguage()
    {
        var code = _preferredLanguage.ToLowerInvariant() switch
        {
            "german" => "de", "french" => "fr", "turkish" => "tr", "russian" => "ru",
            "english" => "en", _ => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
        };
        return SettingsTranslations.ContainsKey(code) ? code : "en";
    }

    private static readonly Dictionary<string, string[]> SectionDescriptionTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["de"] =
        [
            "Übersicht und Schnellzugriff auf die wichtigsten Funktionen.",
            "Displaytests, Sonderzeichen und Helligkeit.",
            "Displayseiten, Vorlagen, Bedingungen und Vorschau verwalten.",
            "Aktive Medienplayer, Titelinformationen und Audiowerte anzeigen.",
            "Priorisierte, zeitgesteuerte Displayereignisse erstellen und verwalten.",
            "Systemsensoren, Aliase und Lüfterzuordnungen verwalten.",
            "Vier Lüfterkanäle konfigurieren und Regelkurven prüfen.",
            "LIS2-Transport, COM-Port und Windows-Startverhalten.",
            "Virtuelles Gerät, Befehle und Protokoll untersuchen.",
            "Home-Assistant-Entitäten, Favoriten und Vorlagen verbinden."
        ],
        ["fr"] =
        [
            "Vue d'ensemble et accès rapide aux fonctions essentielles.",
            "Tests d'affichage, caractères personnalisés et luminosité.",
            "Gérer les pages, modèles, conditions et aperçu.",
            "Lecteurs multimédias, métadonnées et télémétrie audio.",
            "Créer et gérer les événements d'affichage prioritaires.",
            "Capteurs, alias et affectations des ventilateurs.",
            "Configurer quatre ventilateurs et leurs courbes.",
            "Transport LIS2, port COM et démarrage Windows.",
            "Inspecter l'appareil virtuel, les commandes et le journal.",
            "Associer les entités Home Assistant, favoris et modèles."
        ],
        ["tr"] =
        [
            "Önemli işlevlere genel bakış ve hızlı erişim.",
            "Ekran testleri, özel karakterler ve parlaklık.",
            "Ekran sayfalarını, şablonları ve koşulları yönetin.",
            "Medya oynatıcıları ve ses verilerini görüntüleyin.",
            "Öncelikli ve zamanlanmış ekran olaylarını yönetin.",
            "Sensörleri, takma adları ve fan atamalarını yönetin.",
            "Dört fan kanalını ve kontrol eğrilerini yapılandırın.",
            "LIS2 bağlantısı, COM bağlantı noktası ve Windows başlangıcı.",
            "Sanal cihazı, komutları ve protokol günlüğünü inceleyin.",
            "Home Assistant varlıklarını ve şablonlarını bağlayın."
        ],
        ["ru"] =
        [
            "Обзор и быстрый доступ к основным функциям.",
            "Тесты дисплея, символы и яркость.",
            "Управление страницами, шаблонами и условиями.",
            "Медиаплееры, метаданные и аудиотелеметрия.",
            "Управление приоритетными событиями дисплея.",
            "Датчики, псевдонимы и назначения вентиляторов.",
            "Настройка четырёх вентиляторов и кривых.",
            "Подключение LIS2, COM-порт и запуск Windows.",
            "Диагностика виртуального устройства, команд и журнала.",
            "Home Assistant: сущности, избранное и шаблоны."
        ]
    };

    private string TranslateSectionDescription(string section, string fallback)
    {
        var index = Array.IndexOf(SectionNames, section);
        return index >= 0 && SectionDescriptionTranslations.TryGetValue(CurrentUiLanguage(), out var items)
            ? items[index] : fallback;
    }

    private string SettingsTranslate(string text)
    {
        var index = Array.IndexOf(SettingsKeys, text);
        var code = CurrentUiLanguage();
        return index >= 0 && SettingsTranslations.TryGetValue(code, out var translations)
            ? translations[index] : text;
    }

    private void ApplySettingsLocalization()
    {
        void SetText(string control, string key)
        {
            var block = this.FindControl<TextBlock>(control);
            if (block is not null) block.Text = SettingsTranslate(key);
        }
        SetText("SettingsDeviceHeading", "Device / Transport");
        SetText("SettingsAppearanceHeading", "Appearance");
        SetText("SettingsTransportLabel", "Transport");
        SetText("SettingsPortLabel", "COM port");
        SetText("SettingsThemeLabel", "Theme");
        SetText("SettingsLanguageLabel", "Language");
        SetText("SettingsThemeHint", "System default follows the operating system theme.");
        SetText("SettingsLanguageHint", "System default uses the operating system display language.");

        void SetCombo(string control, params string[] labels)
        {
            if (this.FindControl<ComboBox>(control) is not { } combo) return;
            for (var i = 0; i < labels.Length && i < combo.Items.Count; i++)
                if (combo.Items[i] is ComboBoxItem item) item.Content = SettingsTranslate(labels[i]);
        }
        SetCombo("TransportModeComboBox", "Virtual", "Serial");
        SetCombo("ThemeModeComboBox", "System default", "Light", "Dark");
        SetCombo("LanguageModeComboBox", "System default", "English", "German", "French", "Turkish", "Russian");

        this.FindControl<Button>("SettingsRefreshPorts")!.Content = SettingsTranslate("Refresh");
        this.FindControl<Button>("SettingsApplyTransport")!.Content = SettingsTranslate("Apply / reconnect");
        this.FindControl<CheckBox>("StartWithWindowsCheckBox")!.Content =
            SettingsTranslate("Start LIS2 Control Center with Windows (minimized to tray)");
        for (var i = 0; i < SectionNames.Length; i++)
        {
            var button = this.FindControl<Button>($"NavButton{i}");
            if (button?.Content is Grid grid)
                foreach (var child in grid.Children)
                    if (child is TextBlock block) block.Text = SettingsTranslate(SectionNames[i]);
        }
        ApplyPageLocalization();
        if (this.FindControl<Button>("PageVariableBrowser") is { } browser)
            browser.Content = BrowserText("Browse variables…");
        if (this.FindControl<TextBlock>("PageVariableStatus") is not null)
            RefreshPageVariables();
        UpdateSection();
        UpdateVirtualControls();
        this.FindControl<Button>("OpenHelp")!.Content = HelpReferenceLocalization.Translate("Help & reference", CurrentUiLanguage());
        HelpLanguageChanged?.Invoke();
    }
}
