namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Antwort des Wiedergabestart-Endpunkts einer Playlist
/// (<c>POST api/playlists/{id}/play</c>): der aufgelöste Starteintrag
/// samt Stream-Information und seiner Position in der aktuellen
/// Sortierreihenfolge der Playlist. Entspricht dem Referenz-DTO
/// <c>DtoPlaylistPlaybackStart</c> des Web-Clients.
/// </summary>
public class PlaylistPlaybackStart
{
    /// <summary>ID der Playlist.</summary>
    public long PlaylistId { get; set; }

    /// <summary>Name der Playlist.</summary>
    public string PlaylistName { get; set; } = string.Empty;

    /// <summary>Gesamtzahl der (nicht verwaisten) Einträge der Playlist.</summary>
    public int TotalCount { get; set; }

    /// <summary>1-basierte Position des Starteintrags in der aktuellen Sortierreihenfolge.</summary>
    public int CurrentPosition { get; set; }

    /// <summary>ID des aufgelösten Starteintrags.</summary>
    public long CurrentEntryId { get; set; }

    /// <summary>Der aufgelöste Starteintrag.</summary>
    public PlaylistEntry? CurrentEntry { get; set; }

    /// <summary>
    /// Relative Stream-URL des Mediums des Starteintrags, ohne
    /// <c>access_token</c>-Parameter (der Client hängt sein eigenes Token an).
    /// </summary>
    public string StreamUrl { get; set; } = string.Empty;

    /// <summary>
    /// Medientyp des Starteintrags in VideoPlayer-Konvention
    /// (z. B. <c>movie</c> oder <c>episode</c>).
    /// </summary>
    public string MediaType { get; set; } = string.Empty;

    /// <summary>ID des Mediums des Starteintrags.</summary>
    public long MediaId { get; set; }

    /// <summary>
    /// Wiedergabeposition in Sekunden, ab der das Medium des
    /// Starteintrags fortgesetzt wird (aus dem passenden
    /// Weiterschauen-Eintrag; <c>0</c>, wenn keiner existiert).
    /// </summary>
    public long StartPositionSeconds { get; set; }
}
