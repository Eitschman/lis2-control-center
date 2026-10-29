namespace LIS2.App.Avalonia;

/// <summary>Gemeinsame Übersetzungen für Vorlagenprüfung, Datenalter und zugehörige Hilfe.</summary>
internal static class SourceQualityText
{
    private static readonly Dictionary<string, string[]> Texts = new()
    {
        ["Invalid variable"] = ["Ungültige Variable", "Variable non valide", "Geçersiz değişken", "Недопустимая переменная"],
        ["Unknown variable"] = ["Unbekannte Variable", "Variable inconnue", "Bilinmeyen değişken", "Неизвестная переменная"],
        ["Missing glyph"] = ["Fehlende Glyphe", "Glyphe absent", "Eksik glif", "Отсутствующий символ"],
        ["Not discovered"] = ["Noch nicht gefunden", "Pas encore détecté", "Henüz bulunamadı", "Ещё не обнаружено"],
        ["Currently unavailable"] = ["Derzeit nicht verfügbar", "Actuellement indisponible", "Şu anda kullanılamıyor", "Сейчас недоступно"],
        ["Unexpected brace"] = ["Unerwartete Klammer", "Accolade inattendue", "Beklenmeyen parantez", "Неожиданная скобка"],
        ["Unclosed expression"] = ["Nicht geschlossener Ausdruck", "Expression non fermée", "Kapatılmamış ifade", "Незакрытое выражение"],
        ["Decimal places must be 0 to 6"] = ["Nachkommastellen müssen zwischen 0 und 6 liegen", "Décimales entre 0 et 6 requises", "Ondalık basamak 0 ile 6 arasında olmalı", "Число десятичных знаков должно быть от 0 до 6"],
        ["Operation takes no argument"] = ["Operation erlaubt kein Argument", "Opération sans argument", "İşlem bağımsız değişken almaz", "Операция не принимает аргумент"],
        ["Missing argument separator"] = ["Doppelpunkt vor dem Argument fehlt", "Deux-points avant l’argument requis", "Bağımsız değişkenden önce iki nokta eksik", "Перед аргументом отсутствует двоеточие"],
        ["Unknown operation"] = ["Unbekannte Operation", "Opération inconnue", "Bilinmeyen işlem", "Неизвестная операция"],
        ["No data received"] = ["Noch keine Daten empfangen", "Aucune donnée reçue", "Henüz veri alınmadı", "Данные ещё не получены"],
        ["Current"] = ["Aktuell", "À jour", "Güncel", "Актуально"],
        ["Stale"] = ["Veraltet", "Périmé", "Güncelliğini yitirmiş", "Устарело"],
        ["Disconnected"] = ["Nicht verbunden", "Déconnecté", "Bağlı değil", "Нет соединения"],
        ["Last valid data"] = ["Letzte gültige Daten", "Dernières données valides", "Son geçerli veri", "Последние достоверные данные"],
        ["Last data received"] = ["Letzter Datenempfang", "Dernière réception", "Son veri alımı", "Последнее получение данных"],
        ["Template validation"] = ["Vorlagenprüfung", "Validation des modèles", "Şablon doğrulama", "Проверка шаблонов"],
        ["Data freshness"] = ["Datenaktualität", "Actualité des données", "Veri güncelliği", "Актуальность данных"],
        [ValidationHelp] = [
            "Hinweise stehen unter der betroffenen Zeile. Anklicken markiert den Ausdruck im Eingabefeld. Rot kennzeichnet Syntaxfehler, unbekannte statische Variablen und fehlende Glyphen. Orange kennzeichnet derzeit fehlende Werte oder noch nicht gefundene Hardware-/HA-Schlüssel; diese lassen sich ohne verbundene Quelle nicht sicher als falsch einstufen. Hinweise zu Ersatzvariablen bleiben sichtbar, auch wenn eine andere Variable oder ein Ersatztext die Ausgabe liefert. Korrekturen aktualisieren Prüfung und Vorschau; Speichern bleibt möglich.",
            "Les indications sous chaque ligne sélectionnent l’expression au clic. Rouge : syntaxe incorrecte, variable statique inconnue ou glyphe absent. Orange : valeur indisponible ou clé matériel/HA non détectée, impossible à confirmer sans source connectée. Les variables de remplacement restent signalées même si un autre résultat est affiché. Les corrections actualisent validation et aperçu ; l’enregistrement reste possible.",
            "Satırın altındaki bildirime tıklayınca ifade seçilir. Kırmızı: sözdizimi hatası, bilinmeyen sabit değişken veya eksik glif. Turuncu: kullanılamayan değer ya da bulunmamış donanım/HA anahtarı; bağlı kaynak olmadan kesinleştirilemez. Başka bir değer gösterilse bile yedek değişken bildirimleri görünür kalır. Düzeltmeler doğrulamayı ve önizlemeyi yeniler; kayıt engellenmez.",
            "Нажатие на сообщение под строкой выделяет выражение. Красный: ошибка синтаксиса, неизвестная статическая переменная или отсутствующий символ. Оранжевый: недоступное значение или необнаруженный ключ оборудования/HA; без источника нельзя подтвердить ошибку. Сообщения о резервных переменных остаются видны при другом результате. Исправления обновляют проверку и предпросмотр; сохранение не блокируется."],
        [FreshnessHelp] = [
            "Hardware und Medien zeigen Zustand, lokalen Zeitpunkt und Alter der letzten gültigen Daten. Hardware gilt nach 10 Sekunden ohne gültige Messung als veraltet, regelmäßig abgefragte Medien nach 5 Sekunden. Der ereignisgesteuerte Windows Media Player und Home Assistant können bei bestehender Verbindung lange unverändert bleiben; das Alter allein ist dort kein Fehler. HA zeigt den letzten Datenempfang, nicht die letzte Messung jeder Entität. Ein Verbindungsverlust bleibt unabhängig vom Alter sichtbar. Fehlende Daten erhalten keinen erfundenen Zeitstempel. Nach Wiederverbindung aktualisiert erst eine neue gültige Messung den Zeitpunkt.",
            "Matériel et médias affichent état, heure locale et âge des dernières données valides. Délai : 10 secondes pour le matériel, 5 pour les médias interrogés régulièrement. Le lecteur Windows événementiel et Home Assistant peuvent rester inchangés tout en étant connectés ; l’âge seul n’est pas une erreur. HA affiche la dernière réception, pas chaque mesure d’entité. Une déconnexion reste visible. Aucune date n’est inventée ; de nouvelles données valides actualisent la date après reconnexion.",
            "Donanım ve medya durum, yerel zaman ve son geçerli verinin yaşını gösterir. Donanım için 10, düzenli sorgulanan medya için 5 saniye sınırı vardır. Olay tabanlı Windows Media Player ve Home Assistant bağlıyken uzun süre değişmeyebilir; yalnızca veri yaşı hata değildir. HA her varlığın ölçümünü değil son veri alımını gösterir. Bağlantı kaybı ayrıca görünür. Zaman damgası uydurulmaz; yeniden bağlantıdan sonra geçerli veri zamanı yeniler.",
            "Оборудование и медиа показывают состояние, местное время и возраст последних достоверных данных. Порог: 10 секунд для оборудования, 5 для регулярно опрашиваемых медиа. Событийный Windows Media Player и Home Assistant могут долго не изменяться при соединении; возраст сам по себе не ошибка. HA показывает получение данных, а не измерение каждой сущности. Потеря соединения видна отдельно. Время не выдумывается; после подключения его обновляют новые достоверные данные."]
    };
    internal const string ValidationHelp = "Messages below each line select the expression when clicked. Red marks syntax errors, unknown static variables and missing glyphs. Orange marks unavailable values or undiscovered hardware/HA keys, which cannot be confirmed without the source. Fallback-variable messages remain visible even when another value is displayed. Corrections update validation and preview; saving remains possible.";
    internal const string FreshnessHelp = "Hardware and media show state, local timestamp and age of the last valid data. Hardware becomes stale after 10 seconds without valid samples, regularly polled media after 5 seconds. Event-driven Windows Media Player and Home Assistant can remain unchanged while connected; age alone is not an error. HA shows the last data reception, not each entity measurement. Disconnection remains visible independently. Missing data has no invented timestamp. After reconnection, new valid data updates the timestamp.";
    internal static bool Contains(string text) => Texts.ContainsKey(text);
    internal static IEnumerable<string> Sources => Texts.Keys;
    internal static string Get(string text, string language)
    {
        var index = language switch { "de" => 0, "fr" => 1, "tr" => 2, "ru" => 3, _ => -1 };
        return index >= 0 && Texts.TryGetValue(text, out var values) ? values[index] : text;
    }
}
