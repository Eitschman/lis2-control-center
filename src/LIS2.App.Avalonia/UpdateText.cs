namespace LIS2.App.Avalonia;

/// <summary>Gemeinsame Texte für Updatebedienung und kontextbezogene Hilfe.</summary>
internal static class UpdateText
{
    private static readonly Dictionary<string, string[]> Texts = new()
    {
        ["Updates"] = ["Updates", "Mises à jour", "Güncellemeler", "Обновления"],
        ["Check for updates"] = ["Nach Updates suchen", "Rechercher des mises à jour", "Güncellemeleri denetle", "Проверить обновления"],
        ["Check automatically"] = ["Automatisch nach Updates suchen", "Rechercher automatiquement", "Otomatik denetle", "Проверять автоматически"],
        ["Install automatically (Windows; restarts the application)"] = ["Automatisch installieren (Windows; startet die Anwendung neu)", "Installer automatiquement (Windows ; redémarre l’application)", "Otomatik kur (Windows; uygulamayı yeniden başlatır)", "Устанавливать автоматически (Windows; перезапуск приложения)"],
        ["Download update"] = ["Update herunterladen", "Télécharger la mise à jour", "Güncellemeyi indir", "Скачать обновление"],
        ["Install and restart"] = ["Installieren und neu starten", "Installer et redémarrer", "Kur ve yeniden başlat", "Установить и перезапустить"],
        ["Open system installer"] = ["System-Paketinstallation öffnen", "Ouvrir l’installateur système", "Sistem paket kurucusunu aç", "Открыть системный установщик"],
        ["Open download folder"] = ["Downloadordner öffnen", "Ouvrir le dossier de téléchargement", "İndirme klasörünü aç", "Открыть папку загрузки"],
        ["Cancel"] = ["Abbrechen", "Annuler", "İptal", "Отмена"],
        ["Release notes"] = ["Versionshinweise", "Notes de version", "Sürüm notları", "Примечания к выпуску"],
        ["Debian package"] = ["Debian-Paket", "Paquet Debian", "Debian paketi", "Пакет Debian"],
        ["Portable archive"] = ["Portables Archiv", "Archive portable", "Taşınabilir arşiv", "Переносимый архив"],
        ["Installed: {0}"] = ["Installiert: {0}", "Version installée : {0}", "Kurulu: {0}", "Установлено: {0}"],
        ["Installed: {0} · Available: {1}"] = ["Installiert: {0} · Verfügbar: {1}", "Installée : {0} · Disponible : {1}", "Kurulu: {0} · Kullanılabilir: {1}", "Установлено: {0} · Доступно: {1}"],
        ["Not checked."] = ["Noch nicht geprüft.", "Pas encore vérifié.", "Henüz denetlenmedi.", "Ещё не проверено."],
        ["Checking…"] = ["Suche nach Updates…", "Recherche…", "Denetleniyor…", "Проверка…"],
        ["Update available."] = ["Update verfügbar.", "Mise à jour disponible.", "Güncelleme var.", "Доступно обновление."],
        ["This version is up to date."] = ["Diese Version ist aktuell.", "Cette version est à jour.", "Bu sürüm güncel.", "Установлена актуальная версия."],
        ["Downloading and verifying…"] = ["Herunterladen und Prüfen…", "Téléchargement et vérification…", "İndiriliyor ve doğrulanıyor…", "Загрузка и проверка…"],
        ["Download verified."] = ["Download geprüft.", "Téléchargement vérifié.", "İndirme doğrulandı.", "Загрузка проверена."],
        ["Preparing installation…"] = ["Installation wird vorbereitet…", "Préparation de l’installation…", "Kurulum hazırlanıyor…", "Подготовка установки…"],
        ["System installer opened; confirm installation there, then restart LIS2."] = ["System-Paketinstallation geöffnet; dort bestätigen und LIS2 anschließend neu starten.", "Installateur système ouvert ; confirmez puis redémarrez LIS2.", "Sistem kurucusu açıldı; orada onaylayıp LIS2’yi yeniden başlatın.", "Системный установщик открыт; подтвердите установку и перезапустите LIS2."],
        ["Cancelled."] = ["Abgebrochen.", "Annulé.", "İptal edildi.", "Отменено."],
        ["Update failed: {0}"] = ["Update fehlgeschlagen: {0}", "Échec de la mise à jour : {0}", "Güncelleme başarısız: {0}", "Ошибка обновления: {0}"],
        ["Update restored the previous version: {0}"] = ["Update hat die vorherige Version wiederhergestellt: {0}", "La version précédente a été restaurée : {0}", "Önceki sürüm geri yüklendi: {0}", "Восстановлена предыдущая версия: {0}"],
        ["Update available"] = ["Update verfügbar", "Mise à jour disponible", "Güncelleme var", "Доступно обновление"],
        [Help] = [
            "Unter Einstellungen nach stabilen GitHub-Releases suchen. Automatische Prüfung und automatische Windows-Installation sind getrennt und zunächst ausgeschaltet. Die Prüfung läuft beim Start und danach täglich. Versionen und Versionshinweise werden angezeigt. Heruntergeladene Pakete werden anhand SHA256SUMS.txt desselben Releases geprüft; GitHub und das Repository sind die Vertrauensquelle, eine unabhängige digitale Signatur gibt es nicht. Windows sichert den Installationsordner, beendet Display und Lüfter geordnet und startet die neue Version. Bestätigt diese ihren Start nicht, wird die vorherige Version wiederhergestellt. Schreibrechte im übergeordneten Ordner sind erforderlich. Debian-Pakete werden an die System-Paketinstallation übergeben; dort bestätigen und LIS2 anschließend neu starten. Ohne Paketzuordnung bleibt die Datei im Downloadordner. Portable Linux-Archive werden heruntergeladen und manuell entpackt. Native Player-Plug-ins werden nicht automatisch in fremden Installationsordnern ersetzt.",
            "Dans Paramètres, recherchez les versions stables GitHub. La recherche automatique et l’installation automatique Windows sont séparées et désactivées initialement. La recherche a lieu au démarrage puis chaque jour. Versions et notes sont affichées. Les paquets sont vérifiés avec SHA256SUMS.txt de la même version ; GitHub et le dépôt sont la source de confiance, sans signature numérique indépendante. Windows sauvegarde le dossier, arrête proprement affichage et ventilateurs puis redémarre. Sans confirmation du démarrage, la version précédente est restaurée. Le dossier parent doit être accessible en écriture. Les paquets Debian sont transmis à l’installateur système ; confirmez puis redémarrez LIS2. Sans association de paquet, le fichier reste dans le dossier de téléchargement. Les archives Linux portables sont à extraire manuellement. Les plug-ins ne sont pas remplacés dans les autres installations.",
            "Ayarlar’da kararlı GitHub sürümlerini denetleyin. Otomatik denetim ve otomatik Windows kurulumu ayrıdır ve başlangıçta kapalıdır. Denetim başlangıçta ve her gün yapılır. Sürümler ve notlar gösterilir. Paketler aynı sürümün SHA256SUMS.txt dosyasıyla doğrulanır; güven kaynağı GitHub ve depodur, bağımsız dijital imza yoktur. Windows klasörü yedekler, ekran ve fan işlemlerini düzenli kapatır ve yeni sürümü başlatır. Başlatma doğrulanmazsa önceki sürüm geri yüklenir. Üst klasörde yazma izni gerekir. Debian paketleri sistem kurucusuna verilir; orada onaylayıp LIS2’yi yeniden başlatın. Paket ilişkilendirmesi yoksa dosya indirme klasöründe kalır. Taşınabilir Linux arşivleri elle açılır. Eklentiler diğer kurulumlarda otomatik değiştirilmez.",
            "В настройках проверяйте стабильные выпуски GitHub. Автопроверка и автоустановка Windows независимы и изначально выключены. Проверка выполняется при запуске и затем ежедневно. Отображаются версии и примечания. Пакеты проверяются по SHA256SUMS.txt того же выпуска; доверенными источниками являются GitHub и репозиторий, независимой цифровой подписи нет. Windows сохраняет папку, корректно завершает работу дисплея и вентиляторов и запускает новую версию. Без подтверждения запуска восстанавливается предыдущая версия. Нужна запись в родительскую папку. Пакеты Debian передаются системному установщику; подтвердите установку и перезапустите LIS2. Без обработчика пакет остаётся в папке загрузки. Переносимые архивы Linux распаковываются вручную. Плагины в сторонних папках автоматически не заменяются."]
    };

    internal const string Help = "In Settings, check for stable GitHub releases. Automatic checks and automatic Windows installation are separate and initially disabled. Checks run at startup and daily. Versions and release notes are displayed. Downloads are verified against SHA256SUMS.txt from the same release; GitHub and the repository are the trust source, without an independent digital signature. Windows backs up the installation directory, drains display and fan output and restarts the new version. If startup is not confirmed, it restores the previous version. The parent directory must be writable. Debian packages are handed to the system installer; confirm there and restart LIS2 afterward. Without a package association, the file remains in the download folder. Portable Linux archives are downloaded for manual extraction. Native player plug-ins are not automatically replaced in other installation directories.";

    internal static bool Contains(string text) => Texts.ContainsKey(text);
    internal static IEnumerable<string> Sources => Texts.Keys;

    internal static string Get(string text, string language)
    {
        var index = language switch { "de" => 0, "fr" => 1, "tr" => 2, "ru" => 3, _ => -1 };
        return index >= 0 && Texts.TryGetValue(text, out var values) ? values[index] : text;
    }
}
