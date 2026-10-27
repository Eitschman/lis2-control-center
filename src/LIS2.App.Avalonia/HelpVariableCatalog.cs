namespace LIS2.App.Avalonia
{
    /// <summary>Gemeinsame Fachübersetzungen ohne Abhängigkeit von WPF oder dessen Sprachdienst.</summary>
    internal static partial class HelpLocalization
    {
        internal static string Translate(string source, string language)
        {
            var dictionary = language switch { "de" => German, "fr" => French, "tr" => Turkish, "ru" => Russian, _ => null };
            if (dictionary is null) return source;
            return dictionary.TryGetValue(source, out var translated) ? translated
                : throw new InvalidOperationException("Missing variable help translation: " + language + ": " + source);
        }
    }
}

namespace LIS2.App.Avalonia
{
    /// <summary>Statische Variablen bleiben auch ohne verbundenen Player in der Hilfe auffindbar.</summary>
    internal static class HelpVariableCatalog
    {
        internal static readonly (string Key, string Description)[] Variables =
        [
            ("Clock.Time", "Current local time, HH:mm:ss."),
            ("Clock.Date", "Current local date, dd.MM.yyyy."),
            ("Clock.Day", "Current local weekday."),
            ("Winamp.State", "Unknown, Stopped, Paused or Playing."),
            ("Winamp.Artist", "Current artist."),
            ("Winamp.Title", "Current title."),
            ("Winamp.Album", "Current album."),
            ("Winamp.PlaylistPosition", "Current playlist position."),
            ("Winamp.PlaylistCount", "Number of playlist entries."),
            ("Winamp.Elapsed", "Elapsed time as mm:ss."),
            ("Winamp.Duration", "Track duration as mm:ss."),
            ("Winamp.BitrateKbps", "Bitrate in kbit/s."),
            ("Winamp.SampleRateHz", "Sample rate in Hz."),
            ("Winamp.VuLeft", "Left VU level."),
            ("Winamp.VuRight", "Right VU level."),
            ("Winamp.Vu", "Preformatted 20-character stereo VU line."),
            ("Winamp.Spectrum", "20-band ASCII spectrum."),
            ("Winamp.SpectrumRaw", "Raw spectrum array."),
            ("Winamp.SpectrumPeak", "Peak spectrum value."),
            ("Winamp.SpectrumGlyphs", "20-band spectrum using the eight custom VFD glyph levels."),
            ("Winamp.Connected", "True while snapshots are fresh."),
            ("Winamp.SnapshotAgeSeconds", "Age of the last snapshot."),
            ("WmpLegacy.State", "Unknown, Stopped, Paused or Playing."),
            ("WmpLegacy.Artist", "Current artist."),
            ("WmpLegacy.Title", "Current title."),
            ("WmpLegacy.Album", "Current album."),
            ("WmpLegacy.TrackNumber", "Current media track number."),
            ("WmpLegacy.PlaylistCount", "Number of playlist entries."),
            ("WmpLegacy.Elapsed", "Elapsed time as mm:ss."),
            ("WmpLegacy.Duration", "Track duration as mm:ss."),
            ("WmpLegacy.VuLeft", "Left VU level from direct Legacy player audio."),
            ("WmpLegacy.VuRight", "Right VU level from direct Legacy player audio."),
            ("WmpLegacy.Vu", "Preformatted 20-character stereo VU line from direct player audio."),
            ("WmpLegacy.Spectrum", "20-band ASCII spectrum from direct player audio."),
            ("WmpLegacy.SpectrumRaw", "Raw 20-band spectrum array from direct player audio."),
            ("WmpLegacy.SpectrumPeak", "Peak spectrum value."),
            ("WmpLegacy.SpectrumGlyphs", "20-band spectrum using the eight custom VFD glyph levels."),
            ("WmpLegacy.Connected", "True while snapshots are fresh."),
            ("WmpLegacy.SnapshotAgeSeconds", "Age of the last snapshot."),
            ("WmpLegacy.VisualizationConnected", "True while direct Legacy player audio capture is supplying snapshots."),
            ("WmpLegacy.VisualizationAgeSeconds", "Age of the last Legacy player audio snapshot."),
            ("WindowsMedia.State", "Unknown, Closed, Opened, Changing, Stopped, Paused or Playing."),
            ("WindowsMedia.Artist", "Current artist."),
            ("WindowsMedia.Title", "Current title."),
            ("WindowsMedia.Album", "Current album."),
            ("WindowsMedia.AlbumArtist", "Current album artist."),
            ("WindowsMedia.TrackNumber", "Current media track number."),
            ("WindowsMedia.AlbumTrackCount", "Total tracks reported for the current album."),
            ("WindowsMedia.Elapsed", "Elapsed time as mm:ss."),
            ("WindowsMedia.Duration", "Track duration as mm:ss."),
            ("WindowsMedia.PlaybackRate", "Playback rate reported by Windows."),
            ("WindowsMedia.SourceAppUserModelId", "Source application user model ID."),
            ("WindowsMedia.Connected", "True while the Windows Media Player session is available."),
            ("WindowsMedia.SnapshotAgeSeconds", "Age of the last snapshot.")
        ];

        private static readonly (string Key, string[] Descriptions)[] PortableVariables =
        [
            ("WindowsMedia.Subtitle", ["Subtitle supplied by GSMTC; not a guaranteed station name.", "Von GSMTC gelieferter Untertitel; kein zugesicherter Sendername.", "Sous-titre fourni par GSMTC ; pas nécessairement un nom de station.", "GSMTC alt başlığı; istasyon adı olması garanti değildir.", "Подзаголовок GSMTC; не обязательно название станции."]),
            ("WmpLegacy.StreamName", ["WM/RadioStationName from network media; empty if the player supplies none.", "WM/RadioStationName des Netzwerkmediums; leer, wenn der Player keinen Sender liefert.", "WM/RadioStationName du média réseau ; vide si le lecteur ne le fournit pas.", "Ağ ortamının WM/RadioStationName değeri; oynatıcı sağlamazsa boştur.", "WM/RadioStationName сетевого медиа; пусто, если проигрыватель не передаёт станцию."]),
            ("WmpLegacy.StreamUrl", ["Current network media playback URL; empty for local files.", "Wiedergabeadresse des aktuellen Netzwerkmediums; bei lokalen Dateien leer.", "URL de lecture du média réseau ; vide pour les fichiers locaux.", "Geçerli ağ ortamının oynatma adresi; yerel dosyalarda boştur.", "Адрес воспроизведения сетевого медиа; пусто для локальных файлов."]),
            ("WmpLegacy.IsStream", ["True for a non-file URL, false for a local file; unavailable with older plug-ins.", "Wahr bei einer Nicht-Datei-URL, falsch bei lokaler Datei; bei älteren Plug-ins nicht verfügbar.", "Vrai pour une URL réseau, faux pour un fichier local ; absent avec les anciens plug-ins.", "Ağ adresinde doğru, yerel dosyada yanlış; eski eklentilerde kullanılamaz.", "Истина для сетевого URL, ложь для локального файла; отсутствует в старых плагинах."]),
            ("Winamp.StreamName", ["Station name supplied by the decoder.", "Vom Decoder gelieferter Sendername.", "Nom de station fourni par le décodeur.", "Çözücünün sağladığı istasyon adı.", "Название станции от декодера."]),
            ("Winamp.StreamTitle", ["Unmodified stream title; it may contain artist and title together.", "Unveränderter Streamtitel; kann Interpret und Titel zusammen enthalten.", "Titre brut du flux ; peut contenir artiste et titre ensemble.", "Değiştirilmemiş yayın başlığı; sanatçı ve başlığı birlikte içerebilir.", "Исходное название потока; может содержать исполнителя и название вместе."]),
            ("Winamp.StreamGenre", ["Genre supplied by the stream.", "Vom Stream geliefertes Genre.", "Genre fourni par le flux.", "Yayının sağladığı tür.", "Жанр, переданный потоком."]),
            ("Winamp.StreamUrl", ["URL supplied by the decoder; not necessarily the playback URL.", "Vom Decoder gelieferte URL; nicht zwingend die Wiedergabeadresse.", "URL fournie par le décodeur ; pas nécessairement celle de lecture.", "Çözücünün sağladığı URL; oynatma adresi olmayabilir.", "URL от декодера; может отличаться от адреса воспроизведения."]),
            ("Winamp.IsStream", ["True for a stream, false for a local file; unavailable with older plug-ins.", "Wahr für einen Stream, falsch für eine lokale Datei; bei älteren Plug-ins nicht verfügbar.", "Vrai pour un flux, faux pour un fichier local ; absent avec les anciens plug-ins.", "Yayında doğru, yerel dosyada yanlış; eski eklentilerde kullanılamaz.", "Истина для потока, ложь для локального файла; отсутствует в старых плагинах."]),
            ("Winamp.TitleSource", ["Technical title origin: FileTags, StreamMetadata, StreamTitle, Playlist or None.", "Technische Titelherkunft: FileTags, StreamMetadata, StreamTitle, Playlist oder None.", "Origine technique du titre : FileTags, StreamMetadata, StreamTitle, Playlist ou None.", "Başlığın teknik kaynağı: FileTags, StreamMetadata, StreamTitle, Playlist veya None.", "Технический источник названия: FileTags, StreamMetadata, StreamTitle, Playlist или None."]),
            ("Clock.Hour", ["Local hour, 0 to 23.", "Lokale Stunde, 0 bis 23.", "Heure locale, 0 à 23.", "Yerel saat, 0 ile 23.", "Местный час, от 0 до 23."]),
            ("Clock.Minute", ["Local minute, 0 to 59.", "Lokale Minute, 0 bis 59.", "Minute locale, 0 à 59.", "Yerel dakika, 0 ile 59.", "Местная минута, от 0 до 59."]),
            ("Clock.Second", ["Local second, 0 to 59.", "Lokale Sekunde, 0 bis 59.", "Seconde locale, 0 à 59.", "Yerel saniye, 0 ile 59.", "Местная секунда, от 0 до 59."]),
            ("Clock.Month", ["Local month, 1 to 12.", "Lokaler Monat, 1 bis 12.", "Mois local, 1 à 12.", "Yerel ay, 1 ile 12.", "Местный месяц, от 1 до 12."]),
            ("Clock.Year", ["Local year.", "Lokales Jahr.", "Année locale.", "Yerel yıl.", "Местный год."]),
            ("Clock.DayOfWeek", ["English weekday name.", "Englischer Wochentagsname.", "Jour de la semaine en anglais.", "İngilizce haftanın günü adı.", "Название дня недели на английском."]),
            ("Media.Player", ["Selected MPRIS player.", "Ausgewählter MPRIS-Player.", "Lecteur MPRIS sélectionné.", "Seçilen MPRIS oynatıcısı.", "Выбранный проигрыватель MPRIS."]),
            ("Media.State", ["MPRIS playback state.", "MPRIS-Wiedergabestatus.", "État de lecture MPRIS.", "MPRIS oynatma durumu.", "Состояние воспроизведения MPRIS."]),
            ("Media.Title", ["Current title.", "Aktueller Titel.", "Titre actuel.", "Geçerli başlık.", "Текущее название."]),
            ("Media.Artist", ["Current artist.", "Aktueller Interpret.", "Artiste actuel.", "Geçerli sanatçı.", "Текущий исполнитель."]),
            ("Media.Album", ["Current album.", "Aktuelles Album.", "Album actuel.", "Geçerli albüm.", "Текущий альбом."]),
            ("Media.Elapsed", ["Elapsed time, mm:ss or h:mm:ss.", "Verstrichene Zeit, mm:ss oder h:mm:ss.", "Temps écoulé, mm:ss ou h:mm:ss.", "Geçen süre, mm:ss veya h:mm:ss.", "Прошедшее время, mm:ss или h:mm:ss."]),
            ("Media.Duration", ["Duration, mm:ss or h:mm:ss.", "Dauer, mm:ss oder h:mm:ss.", "Durée, mm:ss ou h:mm:ss.", "Süre, mm:ss veya h:mm:ss.", "Длительность, mm:ss или h:mm:ss."]),
            ("Media.Connected", ["True while the selected player is available.", "Wahr, solange der ausgewählte Player verfügbar ist.", "Vrai lorsque le lecteur sélectionné est disponible.", "Seçilen oynatıcı kullanılabilirken doğru.", "Истина, пока выбранный проигрыватель доступен."])
        ];

        internal static IEnumerable<(string Heading, string Body)> Get(string prefix, string language)
        {
            var index = language switch { "de" => 1, "fr" => 2, "tr" => 3, "ru" => 4, _ => 0 };
            return Variables.Where(variable => variable.Key.StartsWith(prefix, StringComparison.Ordinal))
                .Select(variable => ("{" + variable.Key + "}", HelpLocalization.Translate(variable.Description, language)))
                .Concat(PortableVariables.Where(variable => variable.Key.StartsWith(prefix, StringComparison.Ordinal))
                    .Select(variable => ("{" + variable.Key + "}", variable.Descriptions[index])));
        }
    }
}
