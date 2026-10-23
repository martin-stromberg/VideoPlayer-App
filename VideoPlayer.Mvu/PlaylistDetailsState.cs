namespace VideoPlayer.Mvu;

/// <summary>
/// Anzeigefertiger Eintrag der Playlist-Detailseite — ein Verweis der
/// Playlist auf ein Medium in der serverseitigen Reihenfolge.
/// <see cref="ResolvedPictureId"/> ist bereits serverseitig aufgelöst
/// (Poster → Banner → Fanart) und bindet direkt an
/// <c>api/pictures/{id}</c>.
/// </summary>
public sealed record PlaylistEntryItem
{
    /// <summary>Id des Playlist-Eintrags auf dem Server.</summary>
    public required long EntryId { get; init; }

    /// <summary>Id des referenzierten Mediums (Navigationsziel der Detailseite).</summary>
    public required long MediaId { get; init; }

    /// <summary>Medientyp in CLR-Konvention, kleingeschrieben (<c>movie</c>, <c>moviecollection</c>, <c>tvshow</c>, <c>tvshowseason</c>, <c>tvshowepisode</c>).</summary>
    public required string MediaType { get; init; }

    /// <summary>Anzeigetitel des Mediums.</summary>
    public required string Title { get; init; }

    /// <summary>Medientyp des Elternelements (CLR-Konvention); <c>null</c> = kein Elternkontext.</summary>
    public string? ParentMediaType { get; init; }

    /// <summary>Id des Elternelements (Serien-/Staffelkontext); <c>null</c> = kein Elternkontext.</summary>
    public long? ParentMediaId { get; init; }

    /// <summary>Titel des Elternelements; <c>null</c> = kein Elternkontext.</summary>
    public string? ParentMediaTitle { get; init; }

    /// <summary>
    /// Serverseitig aufgelöste Bild-Id für <c>api/pictures/{id}</c>;
    /// <c>null</c> = der Eintrag zeigt den Titel als Fallback.
    /// </summary>
    public long? ResolvedPictureId { get; init; }

    /// <summary>Episodennummer innerhalb der Staffel; <c>null</c> bei Nicht-Episoden.</summary>
    public int? EpisodeNumber { get; init; }

    /// <summary>Der angemeldete Benutzer hat Zugriff auf das Medium.</summary>
    public bool IsAccessible { get; init; } = true;

    /// <summary>Erscheinungsdatum des Mediums; <c>null</c> = unbekannt.</summary>
    public DateTime? ReleaseDate { get; init; }

    /// <summary>
    /// Serverseitige Sortierreihenfolge des Eintrags (manueller
    /// Sortiermodus) — Zielwert des „Eins nach vorn/hinten"-Verschiebens
    /// über <c>move-between</c>; <c>null</c> = nicht zugewiesen.
    /// </summary>
    public long? SortOrder { get; init; }

    /// <summary>Eindeutige AutomationId des Eintrags-Kontextschalters (DevApi-/UIA-Adressierung je Eintrag).</summary>
    public string EntryMenuAutomationId => $"EntryMenu_{EntryId}";

    /// <summary>Ein Poster kann geladen werden.</summary>
    public bool HasPoster => ResolvedPictureId is > 0;

    /// <summary>Ein Elterntitel liegt vor (z. B. Serientitel bei Episoden/Staffeln).</summary>
    public bool HasParentTitle => ParentMediaTitle is { Length: > 0 };

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
    /// Untertitelzeile des Eintrags: bei Episoden „Serie · Episode N",
    /// sonst Elterntitel bzw. der Medientyp.
    /// </summary>
    public string Subtitle =>
        ParentMediaTitle is { Length: > 0 } parent
            ? EpisodeNumber is > 0
                ? $"{parent} · Episode {EpisodeNumber}"
                : parent
            : MediaTypeText;
}

/// <summary>
/// Zustand der Playlist-Detailseite (Shell-Route „playlistdetails"):
/// Kopfdaten der geöffneten Playlist und ihre Einträge in der
/// serverseitigen Reihenfolge, seitenweise nachgeladen
/// (<c>api/playlists/{id}/entries/paged</c>). <see cref="NextPage"/>
/// zählt die nächste zu ladende Seite in der Server-Konvention
/// (1-basiert); <see cref="HasMore"/> folgt <c>HasNextPage</c> der
/// Paged-Antwort.
/// </summary>
public sealed record PlaylistDetailsState
{
    /// <summary>Seitengröße des Nachladens.</summary>
    public const int PageSize = 20;

    /// <summary>Ausgangszustand (keine Playlist geöffnet).</summary>
    /// <value>Der leere Ausgangszustand der Detailseite.</value>
    public static PlaylistDetailsState Empty { get; } = new();

    /// <summary>Id der geöffneten Playlist; 0 = keine Detailansicht aktiv.</summary>
    public long PlaylistId { get; init; }

    /// <summary>Name der geöffneten Playlist (Seitentitel).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Beschreibung der Playlist; <c>null</c> = keine vorhanden.</summary>
    public string? Description { get; init; }

    /// <summary>Sortiermodus der Playlist (<see cref="ApiClient.Models.PlaylistSortModeValues"/>).</summary>
    public string SortMode { get; init; } = ApiClient.Models.PlaylistSortModeValues.ByReleaseDate;

    /// <summary>Id des Cover-Bilds; <c>null</c> = kein Cover.</summary>
    public long? CoverPictureId { get; init; }

    /// <summary>Geladene Einträge (kumuliert über alle Seiten, in Server-Reihenfolge).</summary>
    public IReadOnlyList<PlaylistEntryItem> Entries { get; init; } = [];

    /// <summary>Nächste zu ladende Seite (1-basiert, Server-Konvention).</summary>
    public int NextPage { get; init; } = 1;

    /// <summary>Es gibt (laut Server-Antwort) weitere Seiten zum Nachladen.</summary>
    public bool HasMore { get; init; }

    /// <summary>Gesamtzahl der Einträge laut Server (0 = unbekannt/noch nicht geladen).</summary>
    public int TotalCount { get; init; }

    /// <summary>Erste Seite lädt (Ersetzen).</summary>
    public bool IsLoading { get; init; }

    /// <summary>Eine Folgeseite lädt (Anhängen).</summary>
    public bool IsLoadingMore { get; init; }

    /// <summary>Mindestens ein Ladeversuch wurde abgeschlossen.</summary>
    public bool Attempted { get; init; }

    /// <summary>Fehlertext des letzten Ladeversuchs; <c>null</c> = kein Fehler.</summary>
    public string? Error { get; init; }

    /// <summary>Der angemeldete Benutzer ist Besitzer der Playlist (Schreibzugriff).</summary>
    public bool IsOwner { get; init; }

    /// <summary>Die Playlist ist öffentlich geschaltet.</summary>
    public bool IsPublic { get; init; }

    /// <summary>
    /// Der angemeldete Benutzer trägt den <c>IsAdmin</c>-Claim des
    /// Anmelde-Tokens (Sichtbarkeit des Öffentlich-Schalters — die
    /// serverseitige Prüfung bleibt führend).
    /// </summary>
    public bool IsAdmin { get; init; }

    /// <summary>Die Anzeige-Genres wurden manuell gesetzt (Rücksetzen möglich).</summary>
    public bool GenresManuallyOverridden { get; init; }

    /// <summary>Alle serverseitig wirksamen Genre-Ids (Vorauswahl des Genre-Editors).</summary>
    public IReadOnlyList<long> AllGenreIds { get; init; } = [];

    /// <summary>Kommaseparierte Anzeige-Genres; leer = keine.</summary>
    public string GenreText { get; init; } = string.Empty;

    /// <summary>Eine Verwaltungsoperation läuft gerade (Aktionen gesperrt).</summary>
    public bool IsManaging { get; init; }

    /// <summary>Hinweistext der letzten Verwaltungsoperation (Erfolg oder Fehler); <c>null</c> = kein Hinweis.</summary>
    public string? ManageNotice { get; init; }

    /// <summary><c>true</c>, wenn <see cref="ManageNotice"/> einen Fehler beschreibt.</summary>
    public bool ManageNoticeIsError { get; init; }

    /// <summary>
    /// Ausstehende Weiterschauen-Bestätigung eines Entfernens
    /// (HTTP 409): der betroffene Eintrag; <c>null</c> = kein
    /// ausstehender Konflikt. Die Seite öffnet dafür einmalig den
    /// Bestätigungsdialog.
    /// </summary>
    public PlaylistEntryItem? PendingRemoveConflict { get; init; }

    /// <summary>
    /// Ausstehende Sortiermodus-Bestätigung (HTTP 409): der angeforderte
    /// Ziel-Sortiermodus; <c>null</c> = kein ausstehender Konflikt.
    /// </summary>
    public string? PendingSortModeConflict { get; init; }

    /// <summary>Ein Detail-Kontext ist aktiv (Playlist geöffnet).</summary>
    public bool IsActive => PlaylistId > 0;

    /// <summary>Ein Cover kann geladen werden.</summary>
    public bool HasCover => CoverPictureId is > 0;

    /// <summary>Playlist-Id für den Cover-Abruf; <c>null</c>, wenn kein Cover existiert (verhindert 404-Abrufe).</summary>
    public long? CoverPlaylistId => HasCover ? PlaylistId : null;

    /// <summary>Die Eintragsliste enthält Einträge.</summary>
    public bool HasItems => Entries.Count > 0;

    /// <summary>Der letzte Ladeversuch ist fehlgeschlagen.</summary>
    public bool HasError => Error is not null;

    /// <summary>Erfolgreich geladen, aber die Playlist enthält keine Einträge.</summary>
    public bool ShowEmpty => Attempted && !IsLoading && !HasError && !HasItems;

    /// <summary>Eine Beschreibung liegt vor.</summary>
    public bool HasDescription =>
        !string.IsNullOrWhiteSpace(Description);

    /// <summary>Deutsche Anzeigebezeichnung des Sortiermodus.</summary>
    public string SortModeText =>
        ApiClient.Models.PlaylistSortModeValues.GetDisplayLabel(SortMode);

    /// <summary>
    /// Zusammenfassungszeile des Kopfbereichs: Eintragszahl (sobald
    /// bekannt) und Sortiermodus.
    /// </summary>
    public string SummaryText =>
        TotalCount > 0
            ? $"{TotalCount} {(TotalCount == 1 ? "Eintrag" : "Einträge")} · {SortModeText}"
            : SortModeText;

    /// <summary>Verwaltungsaktionen sind erlaubt (eigene Playlist, aktiver Kontext).</summary>
    public bool CanManage => IsActive && IsOwner;

    /// <summary>Verschiebe-Aktionen sind erlaubt (eigene Playlist im manuellen Sortiermodus).</summary>
    public bool CanReorder =>
        CanManage && SortMode == ApiClient.Models.PlaylistSortModeValues.Manual;

    /// <summary>Das Öffentlich-Flag darf gepflegt werden (eigene Playlist + Administrator-Claim).</summary>
    public bool CanSetPublic => IsOwner && IsAdmin;

    /// <summary>Anzeige-Genres liegen vor.</summary>
    public bool HasGenreText => GenreText.Length > 0;

    /// <summary>
    /// Ermittelt die Ziel-SortOrder für das „Eins nach vorn/hinten"-
    /// Verschieben (<c>move-between</c>): die SortOrder des direkten
    /// Listen-Nachbarn in der gewählten Richtung.
    /// </summary>
    /// <param name="entry">Der zu verschiebende Eintrag.</param>
    /// <param name="forward"><c>true</c> = nach hinten (höhere Position); <c>false</c> = nach vorn.</param>
    /// <returns>Die Ziel-SortOrder des Nachbarn bzw. <c>null</c>, wenn in der Richtung kein Nachbar existiert.</returns>
    public long? NeighborSortOrder(PlaylistEntryItem entry, bool forward)
    {
        var index = IndexOfEntry(entry);
        var neighbor = forward
            ? (index >= 0 && index < Entries.Count - 1 ? Entries[index + 1] : null)
            : (index > 0 ? Entries[index - 1] : null);
        return neighbor?.SortOrder;
    }

    /// <summary>Der Eintrag ist der erste der geladenen Liste.</summary>
    /// <param name="entry">Der zu prüfende Eintrag.</param>
    /// <returns><c>true</c>, wenn kein Vorgänger existiert.</returns>
    public bool IsFirstEntry(PlaylistEntryItem entry) => IndexOfEntry(entry) == 0;

    /// <summary>Der Eintrag ist der letzte der geladenen Liste.</summary>
    /// <param name="entry">Der zu prüfende Eintrag.</param>
    /// <returns><c>true</c>, wenn kein Nachfolger existiert.</returns>
    public bool IsLastEntry(PlaylistEntryItem entry) =>
        Entries.Count > 0 && IndexOfEntry(entry) == Entries.Count - 1;

    /// <summary>Position eines Eintrags anhand seiner Eintrags-Id; -1 = nicht in der Liste.</summary>
    /// <param name="entry">Der gesuchte Eintrag.</param>
    /// <returns>Der nullbasierte Listenindex.</returns>
    public int IndexOfEntry(PlaylistEntryItem entry)
    {
        for (var i = 0; i < Entries.Count; i++)
        {
            if (Entries[i].EntryId == entry.EntryId)
            {
                return i;
            }
        }

        return -1;
    }
}
