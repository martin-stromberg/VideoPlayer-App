namespace VideoPlayer.Mvu;

/// <summary>
/// Anzeigefertiger Eintrag des Quellen-Medienrasters (aus
/// <c>api/items</c>-Summaries gemappt). <see cref="MediaType"/> ist bereits
/// normalisiert (CLR-Konvention, siehe <see cref="MediaTypeNames"/>) und
/// bestimmt die Detailansicht bei Auswahl.
/// </summary>
public sealed record SourceMediaItem
{
    /// <summary>Id des Mediums (Navigationsziel der Detailseite).</summary>
    public required long Id { get; init; }

    /// <summary>Anzeigetitel des Eintrags.</summary>
    public required string Title { get; init; }

    /// <summary>
    /// Normalisierter Medientyp (<c>moviecollection</c>, <c>tvshow</c>, …);
    /// Einträge vom Typ „Film" führen wie im Altbestand zur
    /// Kollektionsansicht.
    /// </summary>
    public required string MediaType { get; init; }

    /// <summary>Bild-Id für <c>api/pictures/{id}</c>; <c>null</c> = Titel-Fallback.</summary>
    public long? PosterPictureId { get; init; }

    /// <summary>Ein Poster kann geladen werden.</summary>
    public bool HasPoster => PosterPictureId is > 0;
}
