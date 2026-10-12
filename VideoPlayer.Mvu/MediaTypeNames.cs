namespace VideoPlayer.Mvu;

/// <summary>
/// Vereinheitlicht Medientyp-Bezeichner: Die Server-Listen liefern je nach
/// Endpunkt unterschiedliche Schreibweisen (Weiterschauen: <c>show</c>,
/// <c>season</c>, <c>episode</c>, <c>collection</c>; Favoriten/
/// Detailseiten: CLR-Typnamen kleingeschrieben). <see cref="Normalize"/>
/// bildet beide Konventionen auf die CLR-Konvention ab
/// (<c>movie</c>, <c>moviecollection</c>, <c>tvshow</c>,
/// <c>tvshowseason</c>, <c>tvshowepisode</c>).
/// </summary>
public static class MediaTypeNames
{
    /// <summary>Normalisierte CLR-Konvention des Medientyps; unbekannte/leere Werte werden kleingeschrieben durchgereicht.</summary>
    /// <param name="mediaType">Der zu normalisierende Medientyp; <c>null</c> wird zu <see cref="string.Empty"/>.</param>
    /// <returns>Der normalisierte Medientyp in CLR-Konvention.</returns>
    public static string Normalize(string? mediaType)
    {
        return mediaType?.Trim().ToLowerInvariant() switch
        {
            "movie" => "movie",
            "moviecollection" or "collection" => "moviecollection",
            "tvshow" or "show" => "tvshow",
            "tvshowseason" or "season" => "tvshowseason",
            "tvshowepisode" or "episode" => "tvshowepisode",
            var other => other ?? string.Empty
        };
    }

    /// <summary>
    /// Bildet einen Medientyp auf den kanonischen Server-Enum-Namen ab
    /// (<c>Movie</c>, <c>MovieCollection</c>, <c>TVShow</c>,
    /// <c>TVShowSeason</c>, <c>TVShowEpisode</c>) — die Form, die die
    /// Playlist-Endpunkte als <c>MediaType</c> erwarten (der Server
    /// parst zwar case-insensitiv, die App sendet dennoch die
    /// Referenz-Schreibweise). Unbekannte/leere Werte werden
    /// durchgereicht.
    /// </summary>
    /// <param name="mediaType">Der zu übersetzende Medientyp (beliebige Konvention).</param>
    /// <returns>Der kanonische Server-Medientyp.</returns>
    public static string ToServerName(string? mediaType)
    {
        return Normalize(mediaType) switch
        {
            "movie" => "Movie",
            "moviecollection" => "MovieCollection",
            "tvshow" => "TVShow",
            "tvshowseason" => "TVShowSeason",
            "tvshowepisode" => "TVShowEpisode",
            var other => other
        };
    }
}
