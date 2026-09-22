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

    /// <summary>
    /// Aufgelöste Banner-Bild-Id der Staffel für <c>api/pictures/{id}</c>
    /// (Banner → Fanart → Poster); <c>null</c> = kein eigenes Bild.
    /// </summary>
    public long? BannerPictureId { get; init; }

    /// <summary>Premieren-/Erscheinungsjahr der Staffel; <c>null</c> = unbekannt.</summary>
    public int? Year { get; init; }

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

    /// <summary>Erscheinungsjahr der Episode (aus <c>ReleaseDate</c>).</summary>
    public int? ReleaseYear { get; init; }

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

    /// <summary>
    /// Eine lokale Datei des Downloads liegt vor (nur im Offline-Snapshot
    /// gesetzt, <see cref="MediaDetailsView.IsOfflineSnapshot"/>).
    /// </summary>
    public bool HasLocalFile { get; init; }

    /// <summary>
    /// Platzhalter-Eintrag: die Episode ist im Offline-Snapshot nicht
    /// heruntergeladen und damit nicht abspielbar.
    /// </summary>
    public bool IsPlaceholder { get; init; }

    /// <summary>Hinweiszeile „Nicht heruntergeladen" wird angezeigt.</summary>
    public bool ShowUnavailable => IsPlaceholder;

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

    /// <summary>
    /// Eine lokale Download-Datei des navigierten Mediums liegt vor
    /// (nur im Offline-Snapshot gesetzt) — Grundlage des Abspielen-Schalters
    /// ohne Serververbindung.
    /// </summary>
    public bool HasLocalFile { get; init; }

    /// <summary>
    /// Der Snapshot wurde ohne Serververbindung aus lokalen Daten gebaut —
    /// servergebundene Aktionen (Download, Favoriten-Umschaltung) werden
    /// auf der Detailseite ausgeblendet/deaktiviert.
    /// </summary>
    public bool IsOfflineSnapshot { get; init; }

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

    /// <summary>
    /// Im Kopfbereich ausgewählte Episode: initial die navigierte Episode
    /// (<c>tvshowepisode</c>), nach einem Staffelwechsel die erste Episode
    /// der gewählten Staffel (Web-Parität zu <c>SelectSeason</c>);
    /// <c>null</c> = der Kopfbereich zeigt die Serien-/Staffel-
    /// Informationen statt eines Episoden-Stills.
    /// </summary>
    public long? SelectedEpisodeId { get; init; }

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

    /// <summary>Das Medium ist direkt abspielbar (Film oder Episode).</summary>
    public bool IsPlayable => MediaType is "movie" or "tvshowepisode";

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

    /// <summary>Ausgewählte Kopfbereichs-Episode (Auflösung von <see cref="SelectedEpisodeId"/> in <see cref="Seasons"/>).</summary>
    public EpisodeItem? SelectedEpisode =>
        SelectedEpisodeId is > 0
            ? Seasons.SelectMany(s => s.Episodes).FirstOrDefault(e => e.Id == SelectedEpisodeId)
            : null;

    /// <summary>
    /// Episoden-Id des Kopfbereichs-Stills (<c>api/episodes/{id}/
    /// background-image</c>); <c>null</c> = der Kopfbereich nutzt die
    /// Bildquelle <see cref="HeaderPictureId"/>.
    /// </summary>
    public long? HeaderEpisodeId => SelectedEpisodeId is > 0 ? SelectedEpisodeId : null;

    /// <summary>
    /// Bildquelle des Kopfbereichs für <c>api/pictures/{id}</c>, wenn kein
    /// Episoden-Still gilt: bei Staffel-Kontext das aufgelöste
    /// Staffel-Banner (Fallback: Serien-Banner), sonst der Banner des
    /// Mediums; <c>null</c> = nur Gradienten.
    /// </summary>
    public long? HeaderPictureId =>
        HeaderEpisodeId is > 0
            ? null
            : MediaType == "tvshowseason"
                ? SelectedSeason?.BannerPictureId ?? BannerPictureId
                : BannerPictureId;

    /// <summary>
    /// Titelzeile des Kopfbereichs: bei ausgewählter Episode der
    /// Episodentitel, bei Staffel-Kontext ohne Episode
    /// „{Serie} - {Staffel}", sonst der Medientitel.
    /// </summary>
    public string HeaderTitle =>
        HeaderEpisodeId is > 0
            ? SelectedEpisode?.Title ?? HighlightedEpisodeTitle ?? Title
            : MediaType == "tvshowseason" && SelectedSeason is { } season
                ? $"{Title} - {season.Name}"
                : Title;

    /// <summary>
    /// Metazeile des Kopfbereichs (nur vorhandene Teile): Episode →
    /// „{Jahr} · Episode {Nr.}"; Staffel → „{Jahr} · {N} Episoden";
    /// sonst „{Medientyp} · {Jahr} · {Genres}".
    /// </summary>
    public string? HeaderMeta
    {
        get
        {
            var parts = new List<string>(3);
            if (SelectedEpisode is { } episode)
            {
                if (episode.ReleaseYear is > 0)
                {
                    parts.Add(episode.ReleaseYear.Value.ToString());
                }
                if (episode.Number > 0)
                {
                    parts.Add($"Episode {episode.Number}");
                }
            }
            else if (MediaType == "tvshowseason" && HeaderEpisodeId is not > 0 && SelectedSeason is { } season)
            {
                if (season.Year is > 0)
                {
                    parts.Add(season.Year.Value.ToString());
                }
                if (season.Episodes.Count > 0)
                {
                    parts.Add(season.Episodes.Count == 1 ? "1 Episode" : $"{season.Episodes.Count} Episoden");
                }
            }
            else
            {
                parts.Add(MediaTypeText);
                if (ReleaseYear is > 0)
                {
                    parts.Add(ReleaseYear.Value.ToString());
                }
                if (!string.IsNullOrWhiteSpace(GenreNames))
                {
                    parts.Add(GenreNames);
                }
            }
            return parts.Count > 0 ? string.Join(" · ", parts) : null;
        }
    }

    /// <summary>
    /// Handlungstext des Kopfbereichs: bei ausgewählter Episode deren
    /// Plot, bei Staffel-Kontext ohne Episode keiner (Web-Verhalten),
    /// sonst der Plot des Mediums.
    /// </summary>
    public string? HeaderPlot =>
        SelectedEpisode?.Plot
        ?? (MediaType == "tvshowseason" && HeaderEpisodeId is not > 0 ? null : Plot);

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
