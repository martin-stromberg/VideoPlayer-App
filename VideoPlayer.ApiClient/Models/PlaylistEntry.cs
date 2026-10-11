namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Ein einzelner Eintrag einer Playlist, der auf ein Medium verweist.
/// Entspricht dem Referenz-DTO <c>DtoPlaylistEntry</c> des Web-Clients —
/// die Felder sind wortgenau übernommen.
/// </summary>
public class PlaylistEntry
{
    /// <summary>Eindeutige ID des Eintrags.</summary>
    public long Id { get; set; }

    /// <summary>ID der Playlist, zu der der Eintrag gehört.</summary>
    public long PlaylistId { get; set; }

    /// <summary>Typ der referenzierten Medienentität (z. B. Movie, TVShow oder Episode).</summary>
    public string MediaType { get; set; } = string.Empty;

    /// <summary>ID der referenzierten Medienentität.</summary>
    public long MediaId { get; set; }

    /// <summary>Titel der referenzierten Medienentität.</summary>
    public string MediaTitle { get; set; } = string.Empty;

    /// <summary>
    /// Typ der Eltern-Medienentität (z. B. die Serie, zu der eine Staffel
    /// gehört); <c>null</c>, wenn die Referenz kein Elternelement hat.
    /// </summary>
    public string? ParentMediaType { get; set; }

    /// <summary>
    /// ID der Eltern-Medienentität; <c>null</c>, wenn die Referenz kein
    /// Elternelement hat.
    /// </summary>
    public long? ParentMediaId { get; set; }

    /// <summary>
    /// Titel der Eltern-Medienentität; <c>null</c>, wenn die Referenz
    /// kein Elternelement hat.
    /// </summary>
    public string? ParentMediaTitle { get; set; }

    /// <summary>Zeitpunkt, zu dem der Eintrag der Playlist hinzugefügt wurde.</summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// ID des für die referenzierte Medienentität anzuzeigenden Bilds,
    /// bereits serverseitig aufgelöst: das Poster, mit Rückfall auf
    /// Banner oder Fanart, wenn kein Poster gesetzt ist; <c>null</c>,
    /// wenn keines davon verfügbar ist. Anders als <c>PosterPictureId</c>
    /// anderer DTOs kann der Wert bereits eine Banner- oder Fanart-Id
    /// sein, da der Rückfall serverseitig erfolgt.
    /// </summary>
    public long? ResolvedPictureId { get; set; }

    /// <summary>Ob der aktuelle Benutzer Zugriff auf die referenzierte Medienentität hat.</summary>
    public bool IsAccessible { get; set; } = true;

    /// <summary>
    /// Manuelle Sortierreihenfolge des Eintrags innerhalb der Playlist —
    /// belegt, wenn der <c>SortMode</c> der Playlist
    /// <c>Manual</c> ist; <c>null</c> bei <c>ByReleaseDate</c>.
    /// </summary>
    public long? SortOrder { get; set; }

    /// <summary>
    /// Erscheinungsdatum der referenzierten Medienentität (mit Rückfall
    /// auf das Premierendatum); <c>null</c>, wenn unbekannt.
    /// </summary>
    public DateTime? ReleaseDate { get; set; }

    /// <summary>Handlungsbeschreibung des referenzierten Films bzw. der Episode; <c>null</c>, wenn keine vorhanden.</summary>
    public string? Plot { get; set; }

    /// <summary>
    /// Episodennummer innerhalb der Staffel bei einem
    /// <c>TVShowEpisode</c>-Eintrag; <c>null</c> bei allen anderen
    /// Medientypen.
    /// </summary>
    public int? EpisodeNumber { get; set; }
}
