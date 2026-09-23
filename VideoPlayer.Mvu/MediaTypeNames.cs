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
}
