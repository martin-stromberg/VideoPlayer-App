namespace VideoPlayer.Mvu;

/// <summary>
/// Staffel-Eintrag der Detailseite (Anzeige im Staffel-`Picker`).
/// </summary>
public sealed record SeasonItem
{
    /// <summary>Id der Staffel.</summary>
    public required long Id { get; init; }

    /// <summary>Staffelnummer.</summary>
    public required int Number { get; init; }

    /// <summary>Anzeigename — bei leerem Servernamen „Staffel {Number}".</summary>
    public required string Name { get; init; }

    /// <summary>Episoden der Staffel (in der Serienantwort eingebettet).</summary>
    public IReadOnlyList<EpisodeItem> Episodes { get; init; } = [];
}

/// <summary>
/// Episoden-Eintrag der Detailseite (Episodenliste der gewählten Staffel).
/// </summary>
public sealed record EpisodeItem
{
    /// <summary>Id der Episode.</summary>
    public required long Id { get; init; }

    /// <summary>Episodennummer innerhalb der Staffel.</summary>
    public required int Number { get; init; }

    /// <summary>Anzeigetitel — bei leerem Servernamen „Episode {Number}".</summary>
    public required string Title { get; init; }

    /// <summary>Handlungstext der Episode.</summary>
    public string? Plot { get; init; }

    /// <summary>
    /// Bild-Id für <c>api/pictures/{id}</c> (generiertes Hintergrundbild
    /// bevorzugt, sonst Poster); <c>null</c> = Titel-Fallback.
    /// </summary>
    public long? PosterPictureId { get; init; }

    /// <summary>
    /// Die Episode ist das navigierte Medium (Aufruf über eine Episode aus
    /// Weiterschauen/Favoriten) — visuell hervorgehoben.
    /// </summary>
    public bool IsHighlighted { get; init; }

    /// <summary>Anzeigezeile „{Number}. {Title}" bzw. nur Titel.</summary>
    public string DisplayTitle => Number > 0 ? $"{Number}. {Title}" : Title;

    /// <summary>Ein Bild kann geladen werden.</summary>
    public bool HasPoster => PosterPictureId is > 0;
}

/// <summary>
/// Film-Eintrag einer Filmkollektion auf der Detailseite.
/// </summary>
public sealed record CollectionMovieItem
{
    /// <summary>Id des Films (Navigationsziel der Film-Detailseite).</summary>
    public required long Id { get; init; }

    /// <summary>Anzeigetitel.</summary>
    public required string Title { get; init; }

    /// <summary>Handlungstext.</summary>
    public string? Plot { get; init; }

    /// <summary>Genres als Anzeigezeile (serverseitig formatiert).</summary>
    public string? GenreNames { get; init; }

    /// <summary>Erscheinungsjahr (aus <c>ReleaseDate</c>).</summary>
    public int? ReleaseYear { get; init; }

    /// <summary>Bild-Id für <c>api/pictures/{id}</c>; <c>null</c> = Titel-Fallback.</summary>
    public long? PosterPictureId { get; init; }

    /// <summary>Der Film ist als Favorit markiert.</summary>
    public bool IsFavorite { get; init; }

    /// <summary>Ein Poster kann geladen werden.</summary>
    public bool HasPoster => PosterPictureId is > 0;
}

/// <summary>
/// Anzeigefertiges Detailmodell eines Mediums. Je nach navigiertem
/// Medientyp enthält es Serienkontext (<see cref="Seasons"/>,
/// <see cref="SelectedSeasonId"/>, <see cref="HighlightedEpisodeId"/>)
/// oder Kollektions-Filme (<see cref="Movies"/>); für Filme und
/// eigenständige Episoden nur die Basisdetails.
/// <see cref="MediaType"/>/<see cref="MediaId"/> bezeichnen das
/// navigierte Medium (Ziel des Favoriten-Sterns), <see cref="ShowId"/>
/// die angezeigte Serie bei Episoden-/Staffel-Kontext.
/// </summary>
public sealed record MediaDetailsView
{
    /// <summary>Normalisierter Medientyp des navigierten Mediums (CLR-Konvention).</summary>
    public required string MediaType { get; init; }

    /// <summary>Id des navigierten Mediums (Ziel der Favoriten-Markierung).</summary>
    public required long MediaId { get; init; }

    /// <summary>Anzeigetitel (bei Serienkontext der Serienname).</summary>
    public required string Title { get; init; }

    /// <summary>Handlungstext.</summary>
    public string? Plot { get; init; }

    /// <summary>Genres als Anzeigezeile (serverseitig formatiert).</summary>
    public string? GenreNames { get; init; }

    /// <summary>Erscheinungs-/Premierenjahr.</summary>
    public int? ReleaseYear { get; init; }

    /// <summary>Banner-Bild-Id (<c>api/pictures/{id}</c>); <c>null</c> = kein Banner.</summary>
    public long? BannerPictureId { get; init; }

    /// <summary>Poster-Bild-Id (<c>api/pictures/{id}</c>); <c>null</c> = kein Poster.</summary>
    public long? PosterPictureId { get; init; }

    /// <summary>Das navigierte Medium ist als Favorit markiert.</summary>
    public bool IsFavorite { get; init; }

    /// <summary>Id der angezeigten Serie (Episoden-/Staffel-Kontext); <c>null</c> ohne Serie.</summary>
    public long? ShowId { get; init; }

    /// <summary>Staffeln der angezeigten Serie (leer = kein Serienkontext).</summary>
    public IReadOnlyList<SeasonItem> Seasons { get; init; } = [];

    /// <summary>Vorgewählte Staffel; <c>null</c> = erste Staffel.</summary>
    public long? SelectedSeasonId { get; init; }

    /// <summary>Navigierte Episode im Serienkontext (Markierung + Vorwahl der Staffel).</summary>
    public long? HighlightedEpisodeId { get; init; }

    /// <summary>Titel der navigierten Episode (Untertitel-Zeile).</summary>
    public string? HighlightedEpisodeTitle { get; init; }

    /// <summary>Filme einer Kollektion (leer = keine Kollektion).</summary>
    public IReadOnlyList<CollectionMovieItem> Movies { get; init; } = [];

    /// <summary>Ein Banner-Bild kann geladen werden.</summary>
    public bool HasBanner => BannerPictureId is > 0;

    /// <summary>Ein Poster kann geladen werden.</summary>
    public bool HasPoster => PosterPictureId is > 0;

    /// <summary>Serienkontext vorhanden (Staffel-Picker + Episodenliste anzeigen).</summary>
    public bool HasSeasons => Seasons.Count > 0;

    /// <summary>Kollektions-Filme vorhanden (Filmraster anzeigen).</summary>
    public bool HasMovies => Movies.Count > 0;

    /// <summary>Stern-Beschriftung des Favoriten-Schalters (★ markiert / ☆ unmarkiert).</summary>
    public string FavoriteStarText => IsFavorite ? "★" : "☆";

    /// <summary>Übersetzte Medientyp-Bezeichnung.</summary>
    public string MediaTypeText => MediaType switch
    {
        "movie" => "Film",
        "moviecollection" => "Filmkollektion",
        "tvshow" => "Serie",
        "tvshowseason" => "Staffel",
        "tvshowepisode" => "Episode",
        _ => "Medium"
    };

    /// <summary>Gewählte Staffel (<see cref="SelectedSeasonId"/> oder erste).</summary>
    public SeasonItem? SelectedSeason =>
        Seasons.FirstOrDefault(s => s.Id == SelectedSeasonId) ?? Seasons.FirstOrDefault();

    /// <summary>Episoden der gewählten Staffel.</summary>
    public IReadOnlyList<EpisodeItem> SelectedSeasonEpisodes => SelectedSeason?.Episodes ?? [];

    /// <summary>Anzeigezeile „Jahr | Genres | Episodentitel" (nur vorhandene Teile).</summary>
    public string? YearGenreLine
    {
        get
        {
            var parts = new List<string>(3);
            if (ReleaseYear is > 0)
            {
                parts.Add(ReleaseYear.Value.ToString());
            }
            if (!string.IsNullOrWhiteSpace(GenreNames))
            {
                parts.Add(GenreNames);
            }
            if (!string.IsNullOrWhiteSpace(HighlightedEpisodeTitle))
            {
                parts.Add(HighlightedEpisodeTitle);
            }
            return parts.Count > 0 ? string.Join(" | ", parts) : null;
        }
    }

    /// <summary>Eine Jahr/Genre/Episode-Zeile kann angezeigt werden.</summary>
    public bool HasYearGenreLine => YearGenreLine is not null;
}
