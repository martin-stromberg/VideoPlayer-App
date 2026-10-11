namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Eintrag der Weiterschauen-Liste.
/// </summary>
public class ContinueWatchingEntry
{
    /// <summary>Medientyp des Eintrags (z. B. <c>movie</c>, <c>episode</c>).</summary>
    public string MediaType { get; set; } = string.Empty;

    /// <summary>Zugehöriger Medieneintrag, falls vom Server mitgeliefert.</summary>
    public MediaEntry? Entry { get; set; }

    /// <summary>Gemerkte Wiedergabeposition in Sekunden.</summary>
    public long PositionSeconds { get; set; }

    /// <summary>Gesamtdauer des Mediums in Sekunden, falls bekannt.</summary>
    public long? DurationSeconds { get; set; }

    /// <summary>Anzeigetitel des Eintrags.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>ID des Posterbilds, falls vorhanden.</summary>
    public long? PosterPictureId { get; set; }

    /// <summary>
    /// ID der Playlist, aus deren Wiedergabekontext der Eintrag entstand;
    /// <c>null</c>, wenn der Eintrag außerhalb einer Playlist entstand.
    /// </summary>
    public long? PlaylistId { get; set; }

    /// <summary>Name der zugehörigen Playlist; <c>null</c>, wenn <see cref="PlaylistId"/> nicht gesetzt ist.</summary>
    public string? PlaylistName { get; set; }

    /// <summary>
    /// ID des Playlist-Eintrags, dem das Medium innerhalb der Playlist
    /// aktuell entspricht; <c>null</c>, wenn <see cref="PlaylistId"/> nicht
    /// gesetzt ist oder das Medium nicht mehr Teil der Playlist ist.
    /// Grundlage der Playlist-Wiedergabe ab der exakten Position
    /// (<c>POST api/playlists/{id}/play?entryId=…</c>).
    /// </summary>
    public long? PlaylistEntryId { get; set; }
}

/// <summary>
/// Ergebnis einer Mutation der Weiterschauen-Liste (Ausblenden/Überspringen).
/// </summary>
/// <param name="Status">Status der Mutation (z. B. <c>ok</c>).</param>
/// <param name="Message">Erläuternde Meldung des Servers.</param>
/// <returns>Ein neues Mutationsergebnis.</returns>
public sealed record ContinueWatchingMutationResult(string Status, string Message);

/// <summary>
/// Fortschrittsmeldung für die Wiedergabe eines Mediums.
/// </summary>
public class PlaybackProgressReport
{
    /// <summary>Medientyp (z. B. <c>movie</c>, <c>episode</c>).</summary>
    public string MediaType { get; set; } = string.Empty;

    /// <summary>ID des Mediums.</summary>
    public long MediaId { get; set; }

    /// <summary>Aktuelle Wiedergabeposition in Sekunden.</summary>
    public long PositionSeconds { get; set; }

    /// <summary>Gesamtdauer des Mediums in Sekunden.</summary>
    public long DurationSeconds { get; set; }
}
