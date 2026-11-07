namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Playlist des Servers (<c>api/playlists</c>). Entspricht dem
/// Referenz-DTO <c>DtoPlaylist</c> des Web-Clients — die Felder sind
/// wortgenau übernommen.
/// </summary>
public class Playlist
{
    /// <summary>ID der Playlist.</summary>
    public long Id { get; set; }

    /// <summary>Name der Playlist.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Beschreibung der Playlist.</summary>
    public string? Description { get; set; }

    /// <summary>Sortiermodus der Playlist (siehe <see cref="PlaylistSortModeValues"/>).</summary>
    public string SortMode { get; set; } = PlaylistSortModeValues.ByReleaseDate;

    /// <summary>Anlagezeitpunkt der Playlist.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Zeitpunkt der letzten Änderung der Playlist.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Anzeige-Genres der Playlist (gekappte Auswahl, nach Häufigkeit
    /// absteigend geordnet; bei manuellem Override die Auswahl des
    /// Besitzers). Die vollständige Id-Menge steht in
    /// <see cref="AllGenreIds"/>.
    /// </summary>
    public GenreOption[] Genres { get; set; } = [];

    /// <summary>
    /// Alle der Playlist zugeordneten Genre-Ids (abgeleitet oder manuell
    /// überschrieben), nicht nur die gekappte Teilmenge in
    /// <see cref="Genres"/>.
    /// </summary>
    public long[] AllGenreIds { get; set; } = [];

    /// <summary>
    /// Ob der Besitzer die Genres manuell überschrieben hat. Solange
    /// <see langword="true"/>, spiegeln <see cref="Genres"/>/
    /// <see cref="AllGenreIds"/> die manuelle Auswahl.
    /// </summary>
    public bool GenresManuallyOverridden { get; set; }

    /// <summary>
    /// ID des als Cover genutzten Bilds (vom Besitzer hochgeladen oder
    /// als Collage generiert); <c>null</c>, wenn noch keins gesetzt ist.
    /// Steuert, ob die Anzeige das Cover-Bild
    /// (<c>api/playlists/{id}/cover</c>) oder einen Platzhalter rendert.
    /// </summary>
    public long? CoverPictureId { get; set; }

    /// <summary>
    /// Ob <see cref="CoverPictureId"/> vom Besitzer hochgeladen wurde
    /// (<see langword="true"/>) oder automatisch als Collage generiert
    /// wurde (<see langword="false"/>).
    /// </summary>
    public bool CoverPictureIsUserUploaded { get; set; }

    /// <summary>
    /// Ob die Playlist als öffentlich markiert ist: für jeden
    /// angemeldeten Benutzer sichtbar (schreibgeschützt).
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Ob der anfragende Benutzer Besitzer der Playlist ist. Standard
    /// <see langword="false"/> (fail-safe: Bearbeitung nur bei
    /// bestätigtem Besitz).
    /// </summary>
    public bool IsOwner { get; set; }
}

/// <summary>
/// Eine sortierte Seite von Einträgen einer Playlist
/// (<c>api/playlists/{id}/entries/paged</c>). Entspricht dem
/// Referenz-DTO <c>DtoPlaylistEntriesPagedResult</c>.
/// </summary>
public class PlaylistEntriesPagedResult
{
    /// <summary>Die Einträge der angeforderten Seite.</summary>
    public PlaylistEntry[] Entries { get; set; } = [];

    /// <summary>Gesamtzahl der Einträge der Playlist über alle Seiten.</summary>
    public int TotalCount { get; set; }

    /// <summary>Ob eine weitere Seite folgt.</summary>
    public bool HasNextPage { get; set; }

    /// <summary>Die 1-basierte Nummer der gelieferten Seite.</summary>
    public int PageNumber { get; set; }

    /// <summary>Maximale Einträge pro Seite.</summary>
    public int PageSize { get; set; }
}
