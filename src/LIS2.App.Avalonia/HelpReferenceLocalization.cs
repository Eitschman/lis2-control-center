namespace LIS2.App.Avalonia;

/// <summary>Hilfetexte für Deutsch, Französisch, Türkisch und Russisch; Englisch ist die Quellsprache.</summary>
internal static class HelpReferenceLocalization
{
    private static readonly IReadOnlyDictionary<string, string[]> Texts = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["Token storage"] = ["Tokenspeicherung", "Stockage du jeton", "Belirteç depolama", "Хранение токена"],
        ["Windows uses DPAPI. Linux uses the desktop keyring through secret-tool. Local AES storage requires explicit selection; the key is stored beside the encrypted token. Keyring errors are displayed without switching storage automatically. Disabling Home Assistant stops the source and removes the token from the selected store. Switching back to the keyring migrates local files only after verifying the saved token."] = [
            "Windows verwendet DPAPI. Linux verwendet den Desktop-Schlüsselbund über secret-tool. Lokale AES-Speicherung muss ausdrücklich ausgewählt werden; der Schlüssel liegt neben dem verschlüsselten Token. Schlüsselbundfehler werden angezeigt, ohne die Speicherart automatisch zu wechseln. Deaktivieren stoppt die Quelle und entfernt das Token aus dem gewählten Speicher. Beim Wechsel zurück zum Schlüsselbund werden lokale Dateien erst nach Prüfung des gespeicherten Tokens entfernt.",
            "Windows utilise DPAPI. Linux utilise le trousseau du bureau via secret-tool. Le stockage AES local doit être choisi explicitement ; la clé se trouve à côté du jeton chiffré. Les erreurs du trousseau sont affichées sans changement automatique de stockage. Désactiver Home Assistant arrête la source et supprime le jeton du stockage sélectionné. Le retour au trousseau ne supprime les fichiers locaux qu’après vérification du jeton enregistré.",
            "Windows DPAPI kullanır. Linux, secret-tool üzerinden masaüstü anahtarlığını kullanır. Yerel AES depolama açıkça seçilmelidir; anahtar şifreli belirtecin yanında saklanır. Anahtarlık hataları otomatik depolama değişikliği olmadan gösterilir. Home Assistant devre dışı bırakıldığında kaynak durdurulur ve seçili depodaki belirteç silinir. Anahtarlığa dönüşte yerel dosyalar yalnızca kaydedilen belirteç doğrulandıktan sonra silinir.",
            "Windows использует DPAPI. Linux использует хранилище ключей через secret-tool. Локальное хранение AES требует явного выбора; ключ находится рядом с зашифрованным токеном. Ошибки хранилища показываются без автоматического переключения. Отключение Home Assistant останавливает источник и удаляет токен из выбранного хранилища. При возврате к системному хранилищу локальные файлы удаляются только после проверки сохранённого токена."],
        ["Help"] = ["Hilfe", "Aide", "Yardım", "Справка"],
        ["Help & reference"] = ["Hilfe und Referenz", "Aide et référence", "Yardım ve başvuru", "Справка и документация"],
        ["Close"] = ["Schließen", "Fermer", "Kapat", "Закрыть"],
        ["Dashboard"] = ["Übersicht", "Tableau de bord", "Gösterge Paneli", "Панель"],
        ["Display"] = ["Anzeige", "Affichage", "Ekran", "Дисплей"],
        ["Pages"] = ["Seiten", "Pages", "Sayfalar", "Страницы"],
        ["Media"] = ["Medien", "Médias", "Medya", "Медиа"],
        ["Events"] = ["Ereignisse", "Événements", "Olaylar", "События"],
        ["Hardware"] = ["Hardware", "Matériel", "Donanım", "Оборудование"],
        ["Fan Control"] = ["Lüftersteuerung", "Contrôle des ventilateurs", "Fan Kontrolü", "Управление вентиляторами"],
        ["Settings"] = ["Einstellungen", "Paramètres", "Ayarlar", "Настройки"],
        ["Diagnostics"] = ["Diagnose", "Diagnostic", "Tanılama", "Диагностика"],
        ["Home Assistant"] = ["Home Assistant", "Home Assistant", "Home Assistant", "Home Assistant"],
        ["VFD preview"] = ["VFD-Vorschau", "Aperçu VFD", "VFD önizlemesi", "Предпросмотр VFD"],
        ["Fan outputs"] = ["Lüfterausgänge", "Sorties des ventilateurs", "Fan çıkışları", "Выходы вентиляторов"],
        ["Quick links"] = ["Direktzugriff", "Accès rapide", "Hızlı erişim", "Быстрый доступ"],
        ["Lines and brightness"] = ["Zeilen und Helligkeit", "Lignes et luminosité", "Satırlar ve parlaklık", "Строки и яркость"],
        ["Custom glyphs"] = ["Sonderzeichen", "Caractères personnalisés", "Özel karakterler", "Пользовательские символы"],
        ["Named glyph"] = ["Benanntes Zeichen", "Caractère nommé", "Adlandırılmış karakter", "Именованный символ"],
        ["Character ROM"] = ["Zeichen-ROM", "ROM de caractères", "Karakter ROM'u", "ПЗУ символов"],
        ["Timing"] = ["Zeitsteuerung", "Temporisation", "Zamanlama", "Временные параметры"],
        ["Template syntax"] = ["Vorlagensyntax", "Syntaxe des modèles", "Şablon sözdizimi", "Синтаксис шаблонов"],
        ["Formatting"] = ["Formatierung", "Formatage", "Biçimlendirme", "Форматирование"],
        ["Formatting details"] = ["Formatierungsdetails", "Détails du formatage", "Biçimlendirme ayrıntıları", "Подробности форматирования"],
        ["Visibility"] = ["Sichtbarkeit", "Visibilité", "Görünürlük", "Видимость"],
        ["Clock"] = ["Uhrzeit", "Horloge", "Saat", "Часы"],
        ["Available runtime variables"] = ["Verfügbare Laufzeitvariablen", "Variables disponibles à l’exécution", "Kullanılabilir çalışma zamanı değişkenleri", "Доступные переменные времени выполнения"],
        ["Home Assistant variables"] = ["Home-Assistant-Variablen", "Variables Home Assistant", "Home Assistant değişkenleri", "Переменные Home Assistant"],
        ["Winamp"] = ["Winamp", "Winamp", "Winamp", "Winamp"],
        ["Spectrum"] = ["Spektrum", "Spectre", "Spektrum", "Спектр"],
        ["Windows media"] = ["Windows-Medien", "Médias Windows", "Windows medyası", "Медиа Windows"],
        ["Example"] = ["Beispiel", "Exemple", "Örnek", "Пример"],
        ["Priorities"] = ["Prioritäten", "Priorités", "Öncelikler", "Приоритеты"],
        ["Lifetime"] = ["Anzeigedauer", "Durée d’affichage", "Gösterim süresi", "Время отображения"],
        ["Sensor variables"] = ["Sensorvariablen", "Variables des capteurs", "Sensör değişkenleri", "Переменные датчиков"],
        ["Linux and Windows"] = ["Linux und Windows", "Linux et Windows", "Linux ve Windows", "Linux и Windows"],
        ["Regulation"] = ["Regelung", "Régulation", "Düzenleme", "Регулирование"],
        ["Fail-safe"] = ["Sicherheitsausgabe", "Sortie de sécurité", "Güvenli çıkış", "Аварийный выход"],
        ["Transport"] = ["Transport", "Transport", "Aktarım", "Транспорт"],
        ["Windows import"] = ["Windows-Import", "Import Windows", "Windows içe aktarımı", "Импорт Windows"],
        ["Appearance"] = ["Darstellung", "Apparence", "Görünüm", "Оформление"],
        ["Windows startup"] = ["Windows-Autostart", "Démarrage Windows", "Windows başlangıcı", "Автозапуск Windows"],
        ["Export"] = ["Exportieren", "Exporter", "Dışa aktar", "Экспорт"],
        ["Virtual LIS2"] = ["Virtueller LIS2", "LIS2 virtuel", "Sanal LIS2", "Виртуальное устройство LIS2"],
        ["Connection"] = ["Verbindung", "Connexion", "Bağlantı", "Подключение"],
        ["Entity state"] = ["Entitätszustand", "État de l’entité", "Varlık durumu", "Состояние сущности"],
        ["Unit and friendly name"] = ["Einheit und Anzeigename", "Unité et nom convivial", "Birim ve görünen ad", "Единица измерения и отображаемое имя"],
        ["Attributes"] = ["Attribute", "Attributs", "Öznitelikler", "Атрибуты"],
        ["Aliases"] = ["Aliase", "Alias", "Takma adlar", "Псевдонимы"],
        ["Unavailable values"] = ["Nicht verfügbare Werte", "Valeurs indisponibles", "Kullanılamayan değerler", "Недоступные значения"],
        ["Finding keys"] = ["Schlüssel finden", "Rechercher des clés", "Anahtarları bulma", "Поиск ключей"],
        ["The Dashboard shows the current 20x2 VFD preview, connection status, recent commands and direct fan outputs."] = [
            "Die Übersicht zeigt die aktuelle 20x2-VFD-Vorschau, den Verbindungsstatus, die letzten Befehle und die direkten Lüfterausgänge.",
            "Le tableau de bord affiche l’aperçu VFD 20x2, l’état de connexion, les dernières commandes et les sorties directes des ventilateurs.",
            "Gösterge paneli geçerli 20x2 VFD önizlemesini, bağlantı durumunu, son komutları ve doğrudan fan çıkışlarını gösterir.",
            "Панель показывает текущий предпросмотр VFD 20x2, состояние подключения, последние команды и прямые выходы вентиляторов."],
        ["The preview displays the normalized 20-character lines. Long text may scroll during page playback."] = [
            "Die Vorschau zeigt die auf 20 Zeichen angepassten Zeilen. Lange Texte können während der Seitenwiedergabe scrollen.",
            "L’aperçu affiche les lignes normalisées à 20 caractères. Les textes longs peuvent défiler pendant l’affichage des pages.",
            "Önizleme, 20 karaktere uyarlanmış satırları gösterir. Uzun metinler sayfa gösterimi sırasında kaydırılabilir.",
            "Предпросмотр показывает строки, приведённые к 20 символам. Длинный текст может прокручиваться при показе страниц."],
        ["Manual output respects the minimum, maximum and AllowStop of all four channels. Each serial action needs fresh authorization. Authorized serial fan output sends 100% first; stop or orderly disconnect sends 100%. Manual serial output is blocked during automatic serial control."] = [
            "Manuelle Ausgaben beachten Minimum, Maximum und AllowStop der vier Kanäle. Jede serielle Aktion benötigt eine neue Freigabe. Freigegebene serielle Lüfterausgabe sendet zuerst 100%; Stopp oder geordnetes Trennen sendet 100%. Während automatischer serieller Regelung ist die manuelle serielle Ausgabe gesperrt.",
            "La sortie manuelle respecte le minimum, le maximum et AllowStop des quatre canaux. Chaque action série exige une nouvelle autorisation. La sortie série autorisée envoie d’abord 100 % ; l’arrêt ou la déconnexion normale envoie 100 %. La sortie série manuelle est bloquée pendant la régulation série automatique.",
            "Manuel çıkış dört kanalın minimum, maksimum ve AllowStop ayarlarını uygular. Her seri işlem yeni izin gerektirir. İzin verilen seri fan çıkışı önce %100 gönderir; durdurma veya düzenli bağlantı kesme %100 gönderir. Otomatik seri kontrol sırasında manuel seri çıkış engellenir.",
            "Ручной вывод учитывает минимум, максимум и AllowStop всех четырёх каналов. Каждое последовательное действие требует нового разрешения. Разрешённый последовательный вывод сначала отправляет 100 %; остановка или штатное отключение отправляет 100 %. При автоматическом последовательном регулировании ручной последовательный вывод заблокирован."],
        ["Open Display for manual output or Fan Control for regulation and sensor assignments."] = [
            "Öffne Anzeige für manuelle Ausgaben oder Lüftersteuerung für Regelung und Sensorzuordnungen.",
            "Ouvrez Affichage pour une sortie manuelle ou Contrôle des ventilateurs pour la régulation et l’affectation des capteurs.",
            "Manuel çıkış için Ekran bölümünü, düzenleme ve sensör atamaları için Fan Kontrolü bölümünü açın.",
            "Откройте Дисплей для ручного вывода или Управление вентиляторами для регулирования и назначения датчиков."],
        ["Use the Display section for direct physical or virtual LIS2 output. Manual commands interrupt page scheduling."] = [
            "Verwende Anzeige für die direkte Ausgabe am physischen oder virtuellen LIS2. Manuelle Befehle unterbrechen die Seitenwiedergabe.",
            "Utilisez Affichage pour envoyer directement des données au LIS2 physique ou virtuel. Les commandes manuelles interrompent la rotation des pages.",
            "Fiziksel veya sanal LIS2’ye doğrudan çıkış için Ekran bölümünü kullanın. Manuel komutlar sayfa zamanlamasını keser.",
            "Используйте раздел Дисплей для прямого вывода на физическое или виртуальное устройство LIS2. Ручные команды прерывают смену страниц."],
        ["Lines, characters and brightness use the applied transport. Brightness supports 100%, 75%, 50% and 25%, is saved and reapplied on connection. The serial preview shows sent content, without feedback from the physical display."] = [
            "Zeilen, Zeichen und Helligkeit werden an den angewendeten Transport gesendet. Helligkeit unterstützt 100%, 75%, 50% und 25%, wird gespeichert und beim Verbinden erneut angewendet. Die serielle Vorschau zeigt gesendete Inhalte, keine Display-Rückmeldung.",
            "Les lignes, caractères et réglages de luminosité utilisent le transport appliqué. La luminosité accepte 100 %, 75 %, 50 % et 25 %, est enregistrée et réappliquée à la connexion. L’aperçu série montre le contenu envoyé, sans retour de l’écran physique.",
            "Satırlar, karakterler ve parlaklık uygulanan aktarımı kullanır. Parlaklık %100, %75, %50 ve %25’i destekler; kaydedilir ve bağlantıda yeniden uygulanır. Seri önizleme, fiziksel ekrandan geri bildirim almadan gönderilen içeriği gösterir.",
            "Строки, символы и яркость передаются через выбранный транспорт. Поддерживается яркость 100 %, 75 %, 50 % и 25 %; она сохраняется и применяется при подключении. Предпросмотр последовательного порта показывает отправленное содержимое без обратной связи от физического дисплея."],
        ["Eight programmable 5x8 slots are available. Use {Glyph.1} through {Glyph.8} in templates."] = [
            "Acht programmierbare 5x8-Zeichenplätze stehen zur Verfügung. Verwende {Glyph.1} bis {Glyph.8} in Vorlagen.",
            "Huit emplacements programmables de 5x8 pixels sont disponibles. Utilisez {Glyph.1} à {Glyph.8} dans les modèles.",
            "Sekiz programlanabilir 5x8 karakter yuvası vardır. Şablonlarda {Glyph.1} ile {Glyph.8} arasındaki anahtarları kullanın.",
            "Доступны восемь программируемых ячеек 5x8. Используйте в шаблонах ключи от {Glyph.1} до {Glyph.8}."],
        ["The raw ROM and special-character tests are intended for protocol validation on real hardware."] = [
            "Die ROM-Rohdaten- und Sonderzeichentests dienen der Protokollprüfung an echter Hardware.",
            "Les tests de ROM brute et de caractères spéciaux servent à valider le protocole sur du matériel réel.",
            "Ham ROM ve özel karakter testleri, gerçek donanımda protokol doğrulaması içindir.",
            "Тесты сырых данных ПЗУ и специальных символов предназначены для проверки протокола на реальном оборудовании."],
        ["Each enabled page contains a name, two templates, duration, refresh interval, overflow modes and optional visibility condition."] = [
            "Jede aktivierte Seite enthält einen Namen, zwei Vorlagen, Anzeigedauer, Aktualisierungsintervall, Überlaufmodi und eine optionale Sichtbarkeitsbedingung.",
            "Chaque page activée contient un nom, deux modèles, une durée, un intervalle d’actualisation, des modes de débordement et une condition de visibilité facultative.",
            "Her etkin sayfa bir ad, iki şablon, gösterim süresi, yenileme aralığı, taşma modları ve isteğe bağlı görünürlük koşulu içerir.",
            "Каждая включённая страница содержит имя, два шаблона, длительность показа, интервал обновления, режимы переполнения и необязательное условие видимости."],
        ["Duration is in seconds; refresh and scrolling use milliseconds. Overflow can truncate, marquee or ping-pong."] = [
            "Die Anzeigedauer wird in Sekunden angegeben, Aktualisierung und Scrolling in Millisekunden. Überlange Texte können abgeschnitten werden, durchlaufen oder hin und her scrollen.",
            "La durée est en secondes ; l’actualisation et le défilement sont en millisecondes. Les textes trop longs peuvent être tronqués, défiler en boucle ou en aller-retour.",
            "Gösterim süresi saniye, yenileme ve kaydırma milisaniye cinsindendir. Uzun metinler kesilebilir, sürekli veya ileri geri kaydırılabilir.",
            "Длительность задаётся в секундах, обновление и прокрутка в миллисекундах. Длинный текст можно обрезать, прокручивать по кругу или вперёд и назад."],
        ["Operations may be chained: prefix, suffix, upper, lower, fallback, number, percent and bytes."] = [
            "Operationen können verkettet werden: prefix, suffix, upper, lower, fallback, number, percent und bytes.",
            "Les opérations peuvent être enchaînées : prefix, suffix, upper, lower, fallback, number, percent et bytes.",
            "İşlemler zincirlenebilir: prefix, suffix, upper, lower, fallback, number, percent ve bytes.",
            "Операции можно объединять в цепочки: prefix, suffix, upper, lower, fallback, number, percent и bytes."],
        ["Use =, !=, >, >=, < or <=. Example: HA.sensor.temperature>=25"] = [
            "Verwende =, !=, >, >=, < oder <=. Beispiel: HA.sensor.temperature>=25",
            "Utilisez =, !=, >, >=, < ou <=. Exemple : HA.sensor.temperature>=25",
            "=, !=, >, >=, < veya <= kullanın. Örnek: HA.sensor.temperature>=25",
            "Используйте =, !=, >, >=, < или <=. Пример: HA.sensor.temperature>=25"],
        ["No runtime variables currently available."] = [
            "Derzeit sind keine Laufzeitvariablen verfügbar.", "Aucune variable n’est actuellement disponible.",
            "Şu anda kullanılabilir çalışma zamanı değişkeni yok.", "Сейчас нет доступных переменных времени выполнения."],
        ["Use the Home Assistant browser for the full live entity and attribute catalogue rather than listing thousands of entries here."] = [
            "Der Home-Assistant-Browser enthält den aktuellen Katalog aller Entitäten und Attribute, statt hier Tausende Einträge aufzulisten.",
            "Utilisez le navigateur Home Assistant pour consulter toutes les entités et tous les attributs actuels, plutôt que d’en afficher des milliers ici.",
            "Burada binlerce kayıt listelemek yerine tüm canlı varlıklar ve öznitelikler için Home Assistant tarayıcısını kullanın.",
            "Используйте обозреватель Home Assistant для полного актуального каталога сущностей и атрибутов, вместо вывода тысяч записей здесь."],
        ["Connected players supply read-only playback status, track metadata, elapsed time, duration and audio telemetry where supported."] = [
            "Verbundene Player liefern schreibgeschützt Wiedergabestatus, Titelinformationen, Spielzeit, Dauer und, sofern unterstützt, Audiodaten.",
            "Les lecteurs connectés fournissent en lecture seule l’état de lecture, les métadonnées, le temps écoulé, la durée et les données audio disponibles.",
            "Bağlı oynatıcılar salt okunur oynatma durumu, parça bilgileri, geçen süre, toplam süre ve destekleniyorsa ses verileri sağlar.",
            "Подключённые проигрыватели предоставляют для чтения состояние воспроизведения, данные трека, прошедшее время, длительность и поддерживаемые аудиоданные."],
        ["{Winamp.Vu}  {Winamp.Spectrum}  {Winamp.SpectrumGlyphs} require a supported source of live audio telemetry."] = [
            "{Winamp.Vu}  {Winamp.Spectrum}  {Winamp.SpectrumGlyphs} benötigen eine unterstützte Quelle für aktuelle Audiodaten.",
            "{Winamp.Vu}  {Winamp.Spectrum}  {Winamp.SpectrumGlyphs} nécessitent une source compatible de données audio en direct.",
            "{Winamp.Vu}  {Winamp.Spectrum}  {Winamp.SpectrumGlyphs} desteklenen bir canlı ses verisi kaynağı gerektirir.",
            "{Winamp.Vu}  {Winamp.Spectrum}  {Winamp.SpectrumGlyphs} требуют поддерживаемый источник аудиоданных в реальном времени."],
        ["Timed events temporarily override ordinary page output on connected displays."] = [
            "Zeitlich begrenzte Ereignisse ersetzen vorübergehend die normale Seitenausgabe auf verbundenen Displays.",
            "Les événements temporisés remplacent temporairement les pages normales sur les écrans connectés.",
            "Süreli olaylar bağlı ekranlarda normal sayfa çıkışının yerini geçici olarak alır.",
            "События с ограниченным временем действия временно заменяют обычные страницы на подключённых дисплеях."],
        ["Critical events preempt warnings, notices and information messages. The highest priority active event is displayed."] = [
            "Kritische Ereignisse haben Vorrang vor Warnungen, Hinweisen und Informationen. Angezeigt wird das aktive Ereignis mit der höchsten Priorität.",
            "Les événements critiques passent avant les avertissements, notifications et informations. L’événement actif de priorité maximale est affiché.",
            "Kritik olaylar uyarı, bildirim ve bilgi mesajlarından önceliklidir. En yüksek öncelikli etkin olay gösterilir.",
            "Критические события имеют приоритет перед предупреждениями, уведомлениями и информацией. Показывается активное событие с наивысшим приоритетом."],
        ["An event expires automatically. Cancel or clear it in the Events view; ordinary page playback resumes afterward."] = [
            "Ein Ereignis läuft automatisch ab. Es kann in der Ereignisansicht abgebrochen oder gelöscht werden. Danach wird die normale Seitenwiedergabe fortgesetzt.",
            "Un événement expire automatiquement. Annulez-le ou effacez-le dans la vue Événements ; la lecture normale des pages reprend ensuite.",
            "Olayın süresi otomatik olarak dolar. Olaylar görünümünde iptal edin veya temizleyin; ardından normal sayfa oynatımı sürer.",
            "Событие истекает автоматически. Отмените или удалите его в разделе События; затем возобновится обычная смена страниц."],
        ["The hardware browser exposes read-only sensors from the current operating system."] = [
            "Der Hardwarebrowser zeigt die Sensoren des aktuellen Betriebssystems schreibgeschützt an.",
            "Le navigateur matériel expose en lecture seule les capteurs du système d’exploitation actuel.",
            "Donanım tarayıcısı mevcut işletim sisteminin sensörlerini salt okunur olarak gösterir.",
            "Обозреватель оборудования показывает датчики текущей операционной системы в режиме чтения."],
        ["Use the exact Hardware.* key from the sensor browser in a page template."] = [
            "Verwende den exakten Hardware.*-Schlüssel aus dem Sensorbrowser in einer Seitenvorlage.",
            "Utilisez la clé Hardware.* exacte du navigateur de capteurs dans un modèle de page.",
            "Sayfa şablonunda sensör tarayıcısındaki Hardware.* anahtarını aynen kullanın.",
            "Используйте в шаблоне страницы точный ключ Hardware.* из обозревателя датчиков."],
        ["Available temperature, utilization, fan and storage sensors depend on the host and installed drivers."] = [
            "Verfügbare Sensoren für Temperatur, Auslastung, Lüfter und Speicher hängen vom Rechner und den installierten Treibern ab.",
            "Les capteurs disponibles de température, charge, ventilateurs et stockage dépendent de l’ordinateur et des pilotes installés.",
            "Kullanılabilir sıcaklık, kullanım, fan ve depolama sensörleri bilgisayara ve yüklü sürücülere bağlıdır.",
            "Доступность датчиков температуры, загрузки, вентиляторов и накопителей зависит от компьютера и установленных драйверов."],
        ["Configure four channels, each with sensor assignment, mode and safe minimum/maximum output."] = [
            "Konfiguriere vier Kanäle mit jeweils einer Sensorzuordnung, einem Modus und sicheren minimalen und maximalen Ausgangswerten.",
            "Configurez quatre canaux, chacun avec un capteur, un mode et des limites de sortie sûres.",
            "Her biri sensör ataması, mod ve güvenli minimum/maksimum çıkış içeren dört kanalı yapılandırın.",
            "Настройте четыре канала, назначив каждому датчик, режим и безопасные минимальный и максимальный уровни выхода."],
        ["Fixed uses the configured percentage. Curve interpolates temperature=percent points and applies hysteresis. Follow rounds the sensor value to a percentage. External uses the fail-safe value because this application supplies no external control percentage. Off outputs zero only with AllowStop; otherwise it uses the minimum. Minimum and maximum limit normal outputs."] = [
            "Fixed verwendet den eingestellten Prozentwert. Curve interpoliert Punkte im Format Temperatur=Prozent und berücksichtigt die Hysterese. Follow rundet den Sensorwert auf einen Prozentwert. External verwendet den Sicherheitswert, da diese Anwendung keinen externen Stellwert liefert. Off gibt nur mit AllowStop null aus, sonst den Mindestwert. Minimum und Maximum begrenzen normale Ausgaben.",
            "Fixed utilise le pourcentage configuré. Curve interpole les points température=pourcentage avec hystérésis. Follow arrondit la valeur du capteur en pourcentage. External utilise la valeur de sécurité car cette application ne fournit aucun pourcentage externe. Off produit zéro uniquement avec AllowStop, sinon le minimum. Minimum et maximum limitent les sorties normales.",
            "Fixed ayarlanan yüzdeyi kullanır. Curve sıcaklık=yüzde noktaları arasında ara değer hesaplar ve histerezis uygular. Follow sensör değerini yüzdeye yuvarlar. Uygulama harici kontrol yüzdesi sağlamadığından External güvenli değeri kullanır. Off yalnızca AllowStop ile sıfır, aksi halde minimum değeri verir. Minimum ve maksimum normal çıkışları sınırlar.",
            "Fixed использует заданный процент. Curve интерполирует точки температура=процент с гистерезисом. Follow округляет показание датчика до процента. External использует аварийное значение, поскольку приложение не передаёт внешний процент управления. Off выдаёт ноль только с AllowStop, иначе минимум. Минимум и максимум ограничивают обычный выход."],
        ["Missing or invalid sensors in Curve and Follow use the configured fail-safe value within the channel limits. Preview and simulation do not control physical fans. Physical output requires explicit authorization for the current serial connection. Start sends 100% first; stop and orderly disconnect send 100%. A broken physical connection cannot guarantee delivery of a safety frame."] = [
            "Fehlende oder ungültige Sensoren führen bei Curve und Follow zum eingestellten Sicherheitswert innerhalb der Kanalgrenzen. Vorschau und Simulation steuern keine physischen Lüfter. Physische Ausgaben erfordern eine ausdrückliche Freigabe für die aktuelle serielle Verbindung. Beim Start werden zuerst 100% gesendet; Stopp und geordnetes Trennen senden 100%. Bei unterbrochener physischer Verbindung kann die Zustellung des Sicherheitsbefehls nicht garantiert werden.",
            "Un capteur absent ou invalide en mode Curve ou Follow entraîne la valeur de sécurité dans les limites du canal. Aperçu et simulation ne contrôlent pas les ventilateurs physiques. La sortie physique exige une autorisation pour la connexion série actuelle. Le démarrage envoie d’abord 100 % ; l’arrêt et la déconnexion normale envoient 100 %. Une connexion physique rompue ne garantit pas la réception de la commande de sécurité.",
            "Curve ve Follow modlarında eksik veya geçersiz sensörler, kanal sınırları içindeki güvenli değeri kullanır. Önizleme ve simülasyon fiziksel fanları kontrol etmez. Fiziksel çıkış mevcut seri bağlantı için açık izin gerektirir. Başlangıçta önce %100 gönderilir; durdurma ve düzenli bağlantı kesme %100 gönderir. Fiziksel bağlantı kopmuşsa güvenlik komutunun ulaşması garanti edilemez.",
            "При отсутствии или ошибке датчика в Curve и Follow применяется аварийное значение в пределах канала. Предпросмотр и моделирование не управляют физическими вентиляторами. Физический вывод требует разрешения для текущего последовательного соединения. При запуске сначала отправляется 100 %; при остановке и штатном отключении также 100 %. При обрыве физического соединения доставка команды безопасности не гарантируется."],
        ["Save glyphs in the workspace and send them to the connected device. Enable programming on connect to restore them after reconnecting. Names label the editor slots; templates currently require numeric references such as {Glyph.1}."] = [
            "Speichere Zeichen im Arbeitsbereich und sende sie an das verbundene Gerät. Aktiviere das Programmieren beim Verbinden, um sie nach einer erneuten Verbindung wiederherzustellen. Namen beschriften die Plätze im Editor; Vorlagen benötigen derzeit numerische Referenzen wie {Glyph.1}.",
            "Enregistrez les caractères dans l’espace de travail et envoyez-les à l’appareil connecté. Activez la programmation à la connexion pour les restaurer. Les noms étiquettent les emplacements de l’éditeur ; les modèles nécessitent actuellement des références numériques comme {Glyph.1}.",
            "Karakterleri çalışma alanına kaydedip bağlı aygıta gönderin. Yeniden bağlanınca geri yüklemek için bağlantıda programlamayı etkinleştirin. Adlar düzenleyicideki yuvaları etiketler; şablonlar şu anda {Glyph.1} gibi sayısal başvurular gerektirir.",
            "Сохраните символы в рабочей области и отправьте на устройство. Включите программирование при подключении для их восстановления. Имена обозначают ячейки редактора; шаблонам сейчас нужны числовые ссылки, например {Glyph.1}."],
        ["number and percent accept 0 to 6 decimal places. percent appends % without multiplying by 100; bytes uses powers of 1024. Numbers follow the operating system culture. Missing values return fallback directly, without further formatting. Visibility uses one condition without braces; numeric comparisons use a decimal point. Missing keys hide the page."] = [
            "number und percent akzeptieren 0 bis 6 Nachkommastellen. percent hängt % an, ohne mit 100 zu multiplizieren; bytes verwendet Potenzen von 1024. Zahlen folgen dem Zahlenformat des Betriebssystems. Fehlende Werte geben fallback direkt aus, ohne weitere Formatierung. Sichtbarkeit verwendet eine Bedingung ohne geschweifte Klammern; Zahlenvergleiche verwenden den Dezimalpunkt. Fehlende Schlüssel blenden die Seite aus.",
            "number et percent acceptent 0 à 6 décimales. percent ajoute % sans multiplier par 100 ; bytes utilise les puissances de 1024. Les nombres suivent le format du système. Une valeur absente renvoie directement fallback sans autre formatage. La visibilité utilise une condition sans accolades ; les comparaisons numériques utilisent un point décimal. Une clé absente masque la page.",
            "number ve percent 0 ile 6 ondalık basamak kabul eder. percent, 100 ile çarpmadan % ekler; bytes 1024’ün kuvvetlerini kullanır. Sayılar işletim sisteminin biçimini izler. Eksik değerler başka biçimlendirme olmadan doğrudan fallback döndürür. Görünürlük süslü parantezsiz tek koşul kullanır; sayısal karşılaştırmalarda ondalık nokta kullanılır. Eksik anahtarlar sayfayı gizler.",
            "number и percent принимают от 0 до 6 десятичных знаков. percent добавляет % без умножения на 100; bytes использует степени 1024. Числа следуют формату системы. Отсутствующие значения сразу возвращают fallback без дальнейшего форматирования. Видимость задаётся одним условием без фигурных скобок; числовые сравнения используют десятичную точку. Отсутствующий ключ скрывает страницу."],
        ["This list shows up to 200 current non-HA keys. Browse variables searches the current snapshot and shows at most 50 matches; narrow the search or refresh it after connecting a source. Copy a key or insert it into a line. Save page changes before starting playback. Only enabled pages with a matching visibility condition participate in rotation."] = [
            "Diese Liste zeigt bis zu 200 aktuelle Schlüssel ohne HA. Variablen durchsuchen sucht im aktuellen Datenstand und zeigt höchstens 50 Treffer. Grenze die Suche ein oder aktualisiere sie nach dem Verbinden einer Quelle. Kopiere einen Schlüssel oder füge ihn in eine Zeile ein. Speichere Seitenänderungen vor dem Start der Wiedergabe. Nur aktivierte Seiten mit erfüllter Sichtbarkeitsbedingung nehmen an der Rotation teil.",
            "Cette liste affiche au plus 200 clés actuelles hors HA. Parcourir les variables recherche dans l’état actuel et affiche au plus 50 résultats ; précisez la recherche ou actualisez-la après connexion d’une source. Copiez ou insérez une clé dans une ligne. Enregistrez les modifications avant la lecture. Seules les pages activées satisfaisant leur condition de visibilité participent à la rotation.",
            "Bu liste HA dışındaki en fazla 200 güncel anahtarı gösterir. Değişkenlere göz at mevcut veride arama yapıp en fazla 50 eşleşme gösterir; aramayı daraltın veya kaynak bağlandıktan sonra yenileyin. Anahtarı kopyalayın veya satıra ekleyin. Oynatmadan önce değişiklikleri kaydedin. Yalnızca etkin ve görünürlük koşulunu sağlayan sayfalar dönüşüme katılır.",
            "Список показывает до 200 текущих ключей без HA. Обзор переменных ищет в текущих данных и показывает до 50 результатов; уточните поиск или обновите его после подключения источника. Скопируйте ключ или вставьте в строку. Сохраните изменения перед воспроизведением. В смене участвуют только включённые страницы с выполненным условием видимости."],
        ["Linux uses MPRIS through playerctl, which must be installed. Select an automatic or specific player; the selection is saved. An unavailable selected player stays disconnected. Media.* provides Player, State, Title, Artist, Album, Elapsed, Duration and Connected, but no VU or spectrum. Values are refreshed every second. Windows uses WindowsMedia.* for GSMTC and Winamp.* for the Winamp plug-in. WMP Legacy is deferred and is not started. Create a page from the relevant media card. No plug-in is needed for GSMTC."] = [
            "Linux verwendet MPRIS über playerctl, das installiert sein muss. Wähle die automatische Auswahl oder einen bestimmten Player; die Auswahl wird gespeichert. Ein nicht verfügbarer ausgewählter Player bleibt getrennt. Media.* liefert Player, State, Title, Artist, Album, Elapsed, Duration und Connected, aber keine VU- oder Spektrumsdaten. Die Werte werden jede Sekunde aktualisiert. Windows verwendet WindowsMedia.* für GSMTC und Winamp.* für das Winamp-Plug-in. WMP Legacy ist zurückgestellt und wird nicht gestartet. Erstelle eine Seite aus der jeweiligen Medienkarte. GSMTC benötigt kein Plug-in.",
            "Linux utilise MPRIS via playerctl, qui doit être installé. Choisissez la sélection automatique ou un lecteur précis ; le choix est enregistré. Un lecteur sélectionné absent reste déconnecté. Media.* fournit Player, State, Title, Artist, Album, Elapsed, Duration et Connected, mais ni VU ni spectre. Les valeurs sont actualisées chaque seconde. Windows utilise WindowsMedia.* pour GSMTC et Winamp.* pour le module Winamp. WMP Legacy est reporté et ne démarre pas. Créez une page depuis la carte du lecteur. GSMTC ne nécessite aucun module.",
            "Linux, kurulması gereken playerctl üzerinden MPRIS kullanır. Otomatik seçimi veya belirli bir oynatıcıyı seçin; seçim kaydedilir. Seçilen oynatıcı yoksa bağlantı kesik kalır. Media.* Player, State, Title, Artist, Album, Elapsed, Duration ve Connected sağlar; VU veya spektrum sağlamaz. Değerler her saniye yenilenir. Windows, GSMTC için WindowsMedia.* ve Winamp eklentisi için Winamp.* kullanır. WMP Legacy ertelenmiştir ve başlatılmaz. İlgili medya kartından sayfa oluşturun. GSMTC eklenti gerektirmez.",
            "Linux использует MPRIS через playerctl, который необходимо установить. Выберите автоматический выбор или конкретный проигрыватель; выбор сохраняется. Недоступный выбранный проигрыватель остаётся отключённым. Media.* предоставляет Player, State, Title, Artist, Album, Elapsed, Duration и Connected, но не VU или спектр. Значения обновляются каждую секунду. Windows использует WindowsMedia.* для GSMTC и Winamp.* для плагина Winamp. WMP Legacy отложен и не запускается. Создайте страницу из карточки проигрывателя. Для GSMTC плагин не нужен."],
        ["Select Virtual LIS2 or a serial port. Configure language, appearance and device startup preferences."] = [
            "Wähle Virtuelles LIS2 oder einen seriellen Anschluss. Konfiguriere Sprache, Darstellung und Startverhalten des Geräts.",
            "Sélectionnez LIS2 virtuel ou un port série. Configurez la langue, l’apparence et les préférences de démarrage de l’appareil.",
            "Sanal LIS2 veya bir seri bağlantı noktası seçin. Dil, görünüm ve aygıt başlangıç tercihlerini yapılandırın.",
            "Выберите виртуальное устройство LIS2 или последовательный порт. Настройте язык, оформление и запуск устройства."],
        ["Applying saves the transport and port, disconnects the current device and connects the selected transport. A missing saved port remains selected and is never replaced automatically. Serial display control does not enable fans."] = [
            "Anwenden speichert Transport und Anschluss, trennt das aktuelle Gerät und verbindet den ausgewählten Transport. Ein fehlender gespeicherter Anschluss bleibt ausgewählt und wird niemals automatisch ersetzt. Serielle Displaysteuerung aktiviert keine Lüfter.",
            "Appliquer enregistre le transport et le port, déconnecte l’appareil actuel et connecte le transport choisi. Un port enregistré absent reste sélectionné et n’est jamais remplacé automatiquement. Le contrôle série de l’écran n’active pas les ventilateurs.",
            "Uygula, aktarımı ve bağlantı noktasını kaydeder, mevcut aygıtı ayırır ve seçili aktarımı bağlar. Kayıtlı bağlantı noktası bulunamazsa seçili kalır ve otomatik değiştirilmez. Seri ekran kontrolü fanları etkinleştirmez.",
            "Применение сохраняет транспорт и порт, отключает текущее устройство и подключает выбранный транспорт. Отсутствующий сохранённый порт остаётся выбранным и не заменяется автоматически. Управление дисплеем через последовательный порт не включает вентиляторы."],
        ["Import in Settings reads settings.json without changing the source file. It imports transport, port, brightness, glyphs, pages, fans and sensor preferences, including Home Assistant activation and URL. The token stays separate. Reconnect Home Assistant afterward if required."] = [
            "Der Import in den Einstellungen liest settings.json, ohne die Quelldatei zu ändern. Importiert werden Transport, Anschluss, Helligkeit, Zeichen, Seiten, Lüfter und Sensoreinstellungen einschließlich Home-Assistant-Aktivierung und URL. Das Token bleibt getrennt. Verbinde Home Assistant anschließend bei Bedarf erneut.",
            "L’importation dans les paramètres lit settings.json sans modifier le fichier source. Elle importe transport, port, luminosité, caractères, pages, ventilateurs et préférences des capteurs, ainsi que l’activation et l’URL Home Assistant. Le jeton reste séparé. Reconnectez Home Assistant ensuite si nécessaire.",
            "Ayarlardaki içe aktarma, kaynak dosyayı değiştirmeden settings.json dosyasını okur. Aktarım, bağlantı noktası, parlaklık, karakterler, sayfalar, fanlar, sensör tercihleri ile Home Assistant etkinleştirmesi ve URL’si alınır. Belirteç ayrı tutulur. Gerekirse ardından Home Assistant’a yeniden bağlanın.",
            "Импорт в настройках читает settings.json без изменения исходного файла. Импортируются транспорт, порт, яркость, символы, страницы, вентиляторы, настройки датчиков, включение и URL Home Assistant. Токен хранится отдельно. При необходимости подключите Home Assistant повторно."],
        ["The system, light and dark themes are supported. Language changes apply to the application interface."] = [
            "Systemdarstellung, helles und dunkles Design werden unterstützt. Sprachänderungen gelten für die Anwendungsoberfläche.",
            "Les thèmes système, clair et sombre sont pris en charge. Les changements de langue s’appliquent à l’interface.",
            "Sistem, açık ve koyu temalar desteklenir. Dil değişiklikleri uygulama arayüzüne uygulanır.",
            "Поддерживаются системная, светлая и тёмная темы. Изменение языка применяется к интерфейсу приложения."],
        ["Automatic startup is only available on Windows."] = [
            "Automatischer Start ist nur unter Windows verfügbar.", "Le démarrage automatique est disponible uniquement sous Windows.",
            "Otomatik başlatma yalnızca Windows’ta kullanılabilir.", "Автоматический запуск доступен только в Windows."],
        ["Inspect the current device snapshot, decoded command history and protocol log."] = [
            "Prüfe den aktuellen Gerätezustand, den dekodierten Befehlsverlauf und das Protokoll.",
            "Consultez l’état actuel de l’appareil, l’historique des commandes décodées et le journal du protocole.",
            "Mevcut aygıt durumunu, çözümlenmiş komut geçmişini ve protokol günlüğünü inceleyin.",
            "Просмотрите текущее состояние устройства, историю расшифрованных команд и журнал протокола."],
        ["Export diagnostic information for troubleshooting or hardware-validation sessions."] = [
            "Exportiere Diagnoseinformationen zur Fehlersuche oder Hardwareprüfung.",
            "Exportez les informations de diagnostic pour le dépannage ou la validation du matériel.",
            "Sorun giderme veya donanım doğrulaması için tanılama bilgilerini dışa aktarın.",
            "Экспортируйте диагностические данные для поиска неисправностей или проверки оборудования."],
        ["The virtual device permits protocol tests without physical hardware."] = [
            "Das virtuelle Gerät ermöglicht Protokolltests ohne physische Hardware.",
            "L’appareil virtuel permet de tester le protocole sans matériel physique.",
            "Sanal aygıt, fiziksel donanım olmadan protokol testlerine olanak tanır.",
            "Виртуальное устройство позволяет проверять протокол без физического оборудования."],
        ["The read-only WebSocket integration loads states and receives live state_changed updates."] = [
            "Die schreibgeschützte WebSocket-Integration lädt Zustände und empfängt state_changed-Aktualisierungen.",
            "L’intégration WebSocket en lecture seule charge les états et reçoit les mises à jour state_changed en direct.",
            "Salt okunur WebSocket entegrasyonu durumları yükler ve canlı state_changed güncellemelerini alır.",
            "Интеграция WebSocket в режиме чтения загружает состояния и получает обновления state_changed в реальном времени."],
        ["Enter the Home Assistant URL and a long-lived access token, then test and save the connection."] = [
            "Gib die Home-Assistant-URL und ein langlebiges Zugriffstoken ein. Teste und speichere anschließend die Verbindung.",
            "Saisissez l’URL Home Assistant et un jeton d’accès longue durée, puis testez et enregistrez la connexion.",
            "Home Assistant URL’sini ve uzun ömürlü erişim belirtecini girin, ardından bağlantıyı test edip kaydedin.",
            "Введите URL Home Assistant и долгосрочный токен доступа, затем проверьте и сохраните соединение."],
        ["Unknown and unavailable states can be handled with fallback. Complex attributes are represented as compact JSON."] = [
            "Unbekannte und nicht verfügbare Zustände können mit fallback behandelt werden. Komplexe Attribute werden als kompaktes JSON dargestellt.",
            "Les états inconnus ou indisponibles peuvent être traités avec fallback. Les attributs complexes sont représentés en JSON compact.",
            "Bilinmeyen ve kullanılamayan durumlar fallback ile işlenebilir. Karmaşık öznitelikler kompakt JSON olarak gösterilir.",
            "Неизвестные и недоступные состояния можно обработать с помощью fallback. Сложные атрибуты представлены компактным JSON."],
        ["Search the Home Assistant browser by entity ID, friendly name or alias, then inspect attributes and copy the exact template key."] = [
            "Suche im Home-Assistant-Browser nach Entitäts-ID, Anzeigename oder Alias. Prüfe anschließend die Attribute und kopiere den exakten Vorlagenschlüssel.",
            "Recherchez dans le navigateur Home Assistant par identifiant, nom ou alias, puis consultez les attributs et copiez la clé exacte du modèle.",
            "Home Assistant tarayıcısında varlık kimliği, görünen ad veya takma ad ile arayın; ardından öznitelikleri inceleyip tam şablon anahtarını kopyalayın.",
            "Ищите в обозревателе Home Assistant по идентификатору, имени или псевдониму сущности, затем просмотрите атрибуты и скопируйте точный ключ шаблона."],
    };

    internal static string Translate(string source, string language)
    {
        var index = language switch { "de" => 0, "fr" => 1, "tr" => 2, "ru" => 3, _ => -1 };
        if (index < 0) return source;
        if (!Texts.TryGetValue(source, out var values) || values.Length != 4 || string.IsNullOrWhiteSpace(values[index]))
            throw new InvalidOperationException("Missing help translation for " + language + ": " + source);
        return values[index];
    }
}
