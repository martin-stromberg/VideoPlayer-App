namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Antwort der Playlist-Navigations-Endpunkte
/// (<c>POST api/playlists/{id}/play/next</c>, <c>…/play/previous</c>,
/// <c>…/play/advance</c>): der aufgelöste Eintrag samt seiner
/// tatsächlichen 1-basierten Position in der aktuellen
/// Sortierreihenfolge der Playlist — analog zu
/// <see cref="PlaylistPlaybackStart.CurrentPosition"/>. Die
/// mitgelieferte Position bleibt auch dann korrekt, wenn die
/// Navigation nicht abspielbare oder nicht zugängliche Einträge
/// übersprungen hat. Entspricht dem Referenz-DTO
/// <c>DtoPlaylistNavigationResult</c> des Web-Clients.
/// </summary>
public class PlaylistNavigationResult
{
    /// <summary>Der von der Navigation aufgelöste Eintrag.</summary>
    public PlaylistEntry? Entry { get; set; }

    /// <summary>1-basierte Position von <see cref="Entry"/> in der aktuellen Sortierreihenfolge.</summary>
    public int Position { get; set; }

    /// <summary>
    /// Wiedergabeposition in Sekunden, ab der das Medium von
    /// <see cref="Entry"/> fortgesetzt wird (aus dem passenden
    /// Weiterschauen-Eintrag; <c>0</c>, wenn keiner existiert). Wird
    /// synchron mit der Quelle des Zieleintrags angewendet.
    /// </summary>
    public long StartPositionSeconds { get; set; }
}
