namespace VideoPlayer.Mvu;

/// <summary>
/// Anzeigefertiger Eintrag der Favoriten-Seite (Menüseite „Favoriten").
/// Gegenüber <see cref="HomeMediaItem"/> trägt er die serverseitige
/// Favoriten-Eintrags-Id (<see cref="FavoriteId"/>) — Grundlage der
/// Entfernen-Aktion über <c>api/favorites/remove</c> — sowie den
/// Entfernen-Laufzustand.
/// </summary>
public sealed record FavoriteListItem
{
    /// <summary>Id des Favoriteneintrags auf dem Server (Navigationsziel der Entfernen-Aktion).</summary>
    public required long FavoriteId { get; init; }

    /// <summary>Id des Mediums (Navigationsziel der Detailseite).</summary>
    public required long Id { get; init; }

    /// <summary>Medientyp in CLR-Konvention, kleingeschrieben (<c>movie</c>, <c>moviecollection</c>, <c>tvshow</c>, <c>tvshowseason</c>, <c>tvshowepisode</c>).</summary>
    public required string MediaType { get; init; }

    /// <summary>Anzeigetitel des Eintrags.</summary>
    public required string Title { get; init; }

    /// <summary>
    /// Bild-Id für den Server-Endpunkt <c>api/pictures/{id}</c>;
    /// <c>null</c> = der Eintrag zeigt den Titel als Fallback.
    /// </summary>
    public long? PosterPictureId { get; init; }

    /// <summary>Id der zugehörigen Serie (Episoden-/Staffel-Einträge); <c>null</c> = kein Serienkontext bekannt.</summary>
    public long? ShowId { get; init; }

    /// <summary>Id der zugehörigen Staffel (Episoden-Einträge bzw. die Staffel selbst).</summary>
    public long? SeasonId { get; init; }

    /// <summary>Id der Episode selbst bei Episoden-Einträgen; sonst <c>null</c>.</summary>
    public long? EpisodeId { get; init; }

    /// <summary>Das Entfernen des Eintrags läuft gerade (Server-Aufruf ausstehend).</summary>
    public bool IsRemoving { get; init; }

    /// <summary>Ein Poster kann geladen werden.</summary>
    public bool HasPoster => PosterPictureId is > 0;

    /// <summary>Der Entfernen-Schalter des Eintrags ist bedienbar (kein laufender Lauf).</summary>
    public bool CanRemove => !IsRemoving;

    /// <summary>Übersetzte Medientyp-Bezeichnung (Anzeige unter dem Titel).</summary>
    public string MediaTypeText =>
        MediaTypeNames.Normalize(MediaType) switch
        {
            "movie" => "Film",
            "moviecollection" => "Filmkollektion",
            "tvshow" => "Serie",
            "tvshowseason" => "Staffel",
            "tvshowepisode" => "Episode",
            _ => "Medium"
        };

    /// <summary>
    /// Bildet den Eintrag auf das gemeinsame <see cref="HomeMediaItem"/>
    /// der Detail-Navigation ab (<see cref="MediaItemSelectedMessage"/>).
    /// </summary>
    /// <returns>Der Eintrag als <see cref="HomeMediaItem"/>.</returns>
    public HomeMediaItem ToMediaItem()
    {
        return new HomeMediaItem
        {
            Id = Id,
            MediaType = MediaType,
            Title = Title,
            PosterPictureId = PosterPictureId,
            ShowId = ShowId,
            SeasonId = SeasonId,
            EpisodeId = EpisodeId
        };
    }
}

/// <summary>
/// Zustand der Favoriten-Seite (Menüseite „Favoriten"): die Einträge der
/// lokalen Kopie <c>cached_favorites</c> (online zuvor vom Serverstand
/// erneuert) samt Lade-/Fehlerzustand. Im Offline-Zustand wird derselbe
/// Cache-Stand angezeigt — die Seite kennzeichnet dies über
/// <see cref="AppModel.IsOffline"/>.
/// </summary>
public sealed record FavoritesPageState
{
    /// <summary>Ausgangszustand (nichts geladen, kein Fehler).</summary>
    /// <value>Der leere Ausgangszustand der Favoriten-Seite.</value>
    public static FavoritesPageState Initial { get; } = new();

    /// <summary>Die Favoritenliste wird gerade (neu) geladen.</summary>
    public bool IsLoading { get; init; }

    /// <summary>Mindestens ein Ladeversuch wurde abgeschlossen.</summary>
    public bool Attempted { get; init; }

    /// <summary>Fehlertext des letzten Lade- oder Entfernen-Laufs; <c>null</c> = kein Fehler.</summary>
    public string? Error { get; init; }

    /// <summary>Favoriten-Einträge (Stand der lokalen Kopie).</summary>
    public IReadOnlyList<FavoriteListItem> Items { get; init; } = [];

    /// <summary>Die Liste enthält Einträge.</summary>
    public bool HasItems => Items.Count > 0;

    /// <summary>Der letzte Lade- oder Entfernen-Lauf ist fehlgeschlagen.</summary>
    public bool HasError => Error is not null;

    /// <summary>Erfolgreich geladen, aber keine Favoriten vorhanden.</summary>
    public bool ShowEmpty => Attempted && !IsLoading && !HasError && !HasItems;
}
