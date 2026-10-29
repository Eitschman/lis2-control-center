namespace LIS2.App.Avalonia;

/// <summary>Gemeinsame Texte für Verlauf und kontextbezogene Hilfe.</summary>
internal static class FanHistoryText
{
    internal const string Help = "The fan history records the configured control sensor, calculated target in percent and assigned speed sensor every two seconds. Select a channel and a 5, 15 or 60 minute window. The three plots share a time axis and use separate scales. Missing, invalid or delayed measurements leave gaps; zero RPM remains a valid measurement. Assign a real speed sensor using the Dashboard fan settings. The target uses saved channel settings and fail-safe rules; it is not confirmation of physical output and does not authorize hardware commands. Changing channel configuration or the speed source clears that channel history. Only the latest 60 minutes are kept in memory, at most 1801 samples per channel. Closing the application discards the history; no files are written.";
    private static readonly Dictionary<string, string[]> Texts = new()
    {
        ["5 min"] = ["5 Min.", "5 min", "5 dk", "5 мин"],
        ["15 min"] = ["15 Min.", "15 min", "15 dk", "15 мин"],
        ["60 min"] = ["60 Min.", "60 min", "60 dk", "60 мин"],
        ["Sensor and fan history"] = ["Sensor- und Lüfterverlauf", "Historique des capteurs et ventilateurs", "Sensör ve fan geçmişi", "История датчиков и вентиляторов"],
        ["Control sensor"] = ["Regelsensor", "Capteur de régulation", "Kontrol sensörü", "Датчик управления"],
        ["Calculated target"] = ["Berechneter Sollwert", "Consigne calculée", "Hesaplanan hedef", "Расчётное задание"],
        ["Measured speed"] = ["Gemessene Drehzahl", "Vitesse mesurée", "Ölçülen hız", "Измеренная скорость"],
        ["No speed sensor assigned"] = ["Kein Drehzahlsensor zugeordnet", "Aucun capteur de vitesse affecté", "Hız sensörü atanmamış", "Датчик скорости не назначен"],
        ["Waiting for measurements"] = ["Warte auf Messwerte", "En attente de mesures", "Ölçümler bekleniyor", "Ожидание измерений"],
        ["Session history · 60 minutes"] = ["Sitzungsverlauf · 60 Minuten", "Historique de session · 60 minutes", "Oturum geçmişi · 60 dakika", "История сеанса · 60 минут"],
        [Help] = [
            "Der Lüfterverlauf erfasst alle zwei Sekunden den konfigurierten Regelsensor, den berechneten Sollwert in Prozent und den zugeordneten Drehzahlsensor. Kanal und Zeitraum von 5, 15 oder 60 Minuten wählen. Die drei Diagramme verwenden dieselbe Zeitachse mit getrennten Skalen. Fehlende, ungültige oder verspätete Messungen bleiben Lücken; null RPM ist ein gültiger Messwert. Einen echten Drehzahlsensor über die Lüftereinstellungen im Dashboard zuordnen. Der Sollwert verwendet gespeicherte Kanaleinstellungen und Fail-safe-Regeln; er bestätigt keine physische Ausgabe und erteilt keine Hardwarefreigabe. Änderungen der Kanalkonfiguration oder Drehzahlquelle leeren den betreffenden Verlauf. Die letzten 60 Minuten bleiben ausschließlich im Arbeitsspeicher, höchstens 1801 Messpunkte pro Kanal. Beim Schließen der Anwendung wird der Verlauf verworfen; es werden keine Dateien geschrieben.",
            "L’historique enregistre toutes les deux secondes le capteur de régulation configuré, la consigne calculée en pourcentage et le capteur de vitesse affecté. Choisissez un canal et 5, 15 ou 60 minutes. Les trois graphiques partagent l’axe temporel avec des échelles séparées. Les mesures absentes, invalides ou retardées laissent des lacunes ; zéro RPM reste valide. Affectez un véritable capteur de vitesse dans les réglages des ventilateurs du tableau de bord. La consigne utilise la configuration enregistrée et les règles de sécurité ; elle ne confirme pas une sortie physique et n’autorise aucune commande matérielle. Changer la configuration ou le capteur de vitesse efface l’historique du canal. Seules les 60 dernières minutes sont conservées en mémoire, avec au plus 1801 points par canal. Fermer l’application efface cet historique ; aucun fichier n’est écrit.",
            "Fan geçmişi iki saniyede bir yapılandırılmış kontrol sensörünü, yüzde olarak hesaplanan hedefi ve atanmış hız sensörünü kaydeder. Kanalı ve 5, 15 veya 60 dakikalık süreyi seçin. Üç grafik ayrı ölçeklerle aynı zaman eksenini kullanır. Eksik, geçersiz veya geciken ölçümler boşluk bırakır; sıfır RPM geçerli bir ölçümdür. Panodaki fan ayarlarından gerçek bir hız sensörü atayın. Hedef, kaydedilmiş kanal ayarlarını ve güvenlik kurallarını kullanır; fiziksel çıkışı doğrulamaz ve donanım komutlarına izin vermez. Kanal yapılandırması veya hız kaynağı değiştiğinde kanal geçmişi temizlenir. Yalnızca son 60 dakika bellekte tutulur, kanal başına en fazla 1801 nokta. Uygulama kapanınca geçmiş silinir; dosya yazılmaz.",
            "История каждые две секунды записывает настроенный датчик управления, расчётное задание в процентах и назначенный датчик скорости. Выберите канал и период 5, 15 или 60 минут. Три графика имеют общую ось времени и отдельные шкалы. Отсутствующие, недостоверные или запоздалые измерения оставляют разрывы; ноль RPM является допустимым значением. Назначьте реальный датчик скорости в настройках вентиляторов панели. Задание использует сохранённые настройки и правила безопасности; оно не подтверждает физический выход и не разрешает аппаратные команды. Изменение настроек канала или датчика скорости очищает его историю. Только последние 60 минут хранятся в памяти, не более 1801 точки на канал. Закрытие приложения удаляет историю; файлы не записываются."]
    };
    internal static bool Contains(string text) => Texts.ContainsKey(text);
    internal static IEnumerable<string> Sources => Texts.Keys;
    internal static string Get(string text, string language)
    {
        var index = language switch { "de" => 0, "fr" => 1, "tr" => 2, "ru" => 3, _ => -1 };
        return index >= 0 && Texts.TryGetValue(text, out var values) ? values[index] : text;
    }
}
