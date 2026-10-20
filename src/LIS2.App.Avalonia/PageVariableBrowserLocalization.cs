namespace LIS2.App.Avalonia;

/// <summary>Translations for the Avalonia-specific variable browser. Canonical filter keys stay English.</summary>
public static class PageVariableBrowserLocalization
{
    private static readonly IReadOnlyDictionary<string, string[]> Texts = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["Invalid condition syntax."] = ["Ungültige Bedingung.", "Syntaxe de condition invalide.", "Geçersiz koşul sözdizimi.", "Неверный синтаксис условия."],
        ["No visibility condition (always visible)."] = ["Keine Sichtbarkeitsbedingung (immer sichtbar).", "Aucune condition (toujours visible).", "Görünürlük koşulu yok (her zaman görünür).", "Нет условия (всегда видно)."],
        ["Condition currently matches."] = ["Bedingung ist aktuell erfüllt.", "La condition est actuellement remplie.", "Koşul şu anda sağlanıyor.", "Условие сейчас выполняется."],
        ["Condition currently does not match."] = ["Bedingung ist aktuell nicht erfüllt.", "La condition n’est actuellement pas remplie.", "Koşul şu anda sağlanmıyor.", "Условие сейчас не выполняется."],
        ["Condition cannot be evaluated with current values."] = ["Bedingung kann mit aktuellen Werten nicht ausgewertet werden.", "Impossible d’évaluer la condition avec les valeurs actuelles.", "Koşul mevcut değerlerle değerlendirilemiyor.", "Невозможно оценить условие по текущим значениям."],
        ["Replace custom glyphs?"] = ["Sonderzeichen ersetzen?", "Remplacer les caractères personnalisés ?", "Özel karakterler değiştirilsin mi?", "Заменить пользовательские символы?"],
        ["The VFD spectrum uses all eight custom character slots. Replace the current glyph set with the eight spectrum bar levels?"] = ["Das VFD-Spektrum benötigt alle acht Sonderzeichenplätze. Den aktuellen Zeichensatz durch die acht Spektrumbalken ersetzen?", "Le spectre VFD utilise les huit caractères personnalisés. Remplacer les caractères actuels par les huit niveaux du spectre ?", "VFD spektrumu sekiz özel karakter yuvasının tamamını kullanır. Mevcut karakterler sekiz spektrum çubuğuyla değiştirilsin mi?", "Спектр VFD использует все восемь пользовательских символов. Заменить текущий набор восемью уровнями спектра?"],
        ["Replace glyphs"] = ["Zeichen ersetzen", "Remplacer", "Karakterleri değiştir", "Заменить символы"],
        ["Cancel"] = ["Abbrechen", "Annuler", "İptal", "Отмена"],
        ["Browse variables…"] = ["Variablen durchsuchen…", "Parcourir les variables…", "Değişkenlere göz at…", "Просмотр переменных…"],
        ["Browse template variables"] = ["Vorlagenvariablen durchsuchen", "Parcourir les variables de modèle", "Şablon değişkenlerine göz at", "Просмотр переменных шаблона"],
        ["Search variable keys (multiple words)…"] = ["Variablen suchen (mehrere Wörter)…", "Rechercher des variables (plusieurs mots)…", "Değişken ara (birden çok sözcük)…", "Поиск переменных (несколько слов)…"],
        ["All"] = ["Alle", "Toutes", "Tümü", "Все"],
        ["Favorites"] = ["Favoriten", "Favoris", "Favoriler", "Избранное"],
        ["Home Assistant"] = ["Home Assistant", "Home Assistant", "Home Assistant", "Home Assistant"],
        ["Hardware"] = ["Hardware", "Matériel", "Donanım", "Оборудование"],
        ["Clock"] = ["Uhrzeit", "Horloge", "Saat", "Часы"],
        ["Media / Other"] = ["Medien / Sonstige", "Médias / Autres", "Medya / Diğer", "Медиа / Прочее"],
        ["All entities"] = ["Alle Entitäten", "Toutes les entités", "Tüm varlıklar", "Все сущности"],
        ["☆ Add favorite"] = ["☆ Favorit hinzufügen", "☆ Ajouter aux favoris", "☆ Favorilere ekle", "☆ В избранное"],
        ["★ Remove favorite"] = ["★ Favorit entfernen", "★ Retirer des favoris", "★ Favorilerden çıkar", "★ Убрать из избранного"],
        ["Insert into line 1"] = ["In Zeile 1 einfügen", "Insérer dans la ligne 1", "1. satıra ekle", "Вставить в строку 1"],
        ["Insert into line 2"] = ["In Zeile 2 einfügen", "Insérer dans la ligne 2", "2. satıra ekle", "Вставить в строку 2"],
        ["Close"] = ["Schließen", "Fermer", "Kapat", "Закрыть"],
        ["Refresh values"] = ["Werte aktualisieren", "Actualiser les valeurs", "Değerleri yenile", "Обновить значения"],
        ["Select a variable to inspect its current value and exact template key."] = ["Variable auswählen, um Wert und Vorlagenschlüssel anzuzeigen.", "Sélectionnez une variable pour afficher sa valeur et sa clé.", "Değeri ve anahtarı görmek için değişken seçin.", "Выберите переменную для просмотра значения и ключа."],
        ["Current value: "] = ["Aktueller Wert: ", "Valeur actuelle : ", "Geçerli değer: ", "Текущее значение: "],
        ["(unavailable)"] = ["(nicht verfügbar)", "(indisponible)", "(kullanılamıyor)", "(недоступно)"],
        ["Template: {"] = ["Vorlage: {", "Modèle : {", "Şablon: {", "Шаблон: {"],
        ["Select a variable, then insert it into the requested line."] = ["Variable auswählen und in die gewünschte Zeile einfügen.", "Sélectionnez une variable puis insérez-la dans la ligne.", "Bir değişken seçip istediğiniz satıra ekleyin.", "Выберите переменную и вставьте в нужную строку."],
    };

    public static string Translate(string english, string? language)
    {
        var index = language switch { "de" => 0, "fr" => 1, "tr" => 2, "ru" => 3, _ => -1 };
        return index >= 0 && Texts.TryGetValue(english, out var values) ? values[index] : english;
    }
}
