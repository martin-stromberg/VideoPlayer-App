namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Anforderung zum Anlegen einer Playlist (<c>POST api/playlists</c>,
/// serverseitig <c>DtoCreatePlaylistRequest</c>).
/// </summary>
public class CreatePlaylistRequest
{
    /// <summary>Name der neuen Playlist (Pflichtfeld serverseitig).</summary>
    public required string Name { get; set; }

    /// <summary>Optionale Beschreibung der Playlist.</summary>
    public string? Description { get; set; }

    /// <summary>Sortiermodus (<see cref="PlaylistSortModeValues.Manual"/> oder <see cref="PlaylistSortModeValues.ByReleaseDate"/>).</summary>
    public required string SortMode { get; set; }
}

/// <summary>
/// Anforderung zum Bearbeiten einer Playlist (<c>PUT api/playlists/{id}</c>,
/// serverseitig <c>DtoUpdatePlaylistRequest</c>) — Name und Beschreibung;
/// der Sortiermodus läuft über den eigenen SortMode-Endpunkt.
/// </summary>
public class UpdatePlaylistRequest
{
    /// <summary>Neuer Name der Playlist (Pflichtfeld serverseitig).</summary>
    public required string Name { get; set; }

    /// <summary>Neue Beschreibung; <c>null</c>/leer entfernt sie.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// Anforderung zum Hinzufügen eines Mediums zu einer Playlist
/// (<c>POST api/playlists/{id}/entries</c>, serverseitig
/// <c>DtoAddMediaToPlaylistRequest</c>). Serien, Staffeln und
/// Filmkollektionen kaskadiert der Server auf ihre Einträge.
/// </summary>
public class AddPlaylistEntryRequest
{
    /// <summary>Medientyp als CLR-Enum-Name (<c>Movie</c>, <c>TVShow</c>, <c>TVShowSeason</c>, <c>TVShowEpisode</c>, <c>MovieCollection</c>).</summary>
    public required string MediaType { get; set; }

    /// <summary>Id des Mediums.</summary>
    public required long MediaId { get; set; }
}

/// <summary>
/// Ergebnis des Hinzufügens eines Mediums zu einer Playlist
/// (<c>POST api/playlists/{id}/entries</c>, serverseitig
/// <c>DtoPlaylistAddResult</c>) — inklusive der vom Server
/// formulierten Hinweismeldung (z. B. übersprungene Duplikate).
/// </summary>
public class PlaylistAddResult
{
    /// <summary>Anzahl der tatsächlich angelegten Einträge (Kaskade eingerechnet).</summary>
    public int AddedCount { get; set; }

    /// <summary>Anzahl der übersprungenen Medien (bereits vorhanden).</summary>
    public int SkippedCount { get; set; }

    /// <summary>Serverseitig formulierter Hinweistext; <c>null</c> = Standardtext verwenden.</summary>
    public string? Message { get; set; }
}

/// <summary>
/// Konflikt-Antwort (HTTP 409) playlistbezogener Verwaltungsaufrufe
/// (serverseitig <c>DtoRemovePlaylistEntryConflictResponse</c> beim
/// Entfernen bzw. <c>DtoChangeSortModeConflictResponse</c> beim
/// Sortiermodus-Wechsel — beide nutzen dieselbe Feldform).
/// </summary>
public class PlaylistConflictResponse
{
    /// <summary>Das Entfernen berührt die Weiterschauen-Liste und benötigt eine Bestätigung.</summary>
    public bool IsContinueWatchingConfirmationRequired { get; set; }

    /// <summary>Der Wechsel des Sortiermodus verwirft die manuelle Reihenfolge und benötigt eine Bestätigung.</summary>
    public bool IsLossOfDataConfirmationRequired { get; set; }

    /// <summary>Das Entfernen eines hochgeladenen Covers benötigt eine Bestätigung (Cover-Verwaltung ist nicht App-Umfang).</summary>
    public bool IsCoverDeletionConfirmationRequired { get; set; }
}

/// <summary>
/// Anforderung zum Verschieben eines Playlist-Eintrags
/// (<c>PUT api/playlists/{id}/entries/{entryId}/order</c> und
/// <c>POST api/playlists/{id}/entries/{entryId}/move-between</c>,
/// serverseitig <c>DtoReorderPlaylistEntryRequest</c>).
/// </summary>
public class ReorderPlaylistEntryRequest
{
    /// <summary>Ziel-Sortierreihenfolge des Eintrags (bei move-between die SortOrder des Ziel-Nachbarn).</summary>
    public required long NewSortOrder { get; set; }
}

/// <summary>
/// Anforderung zum Umschalten des Sortiermodus einer Playlist
/// (<c>PATCH api/playlists/{id}/sort-mode</c>, serverseitig
/// <c>DtoChangeSortModeRequest</c>).
/// </summary>
public class ChangeSortModeRequest
{
    /// <summary>Neuer Sortiermodus (<see cref="PlaylistSortModeValues.Manual"/> oder <see cref="PlaylistSortModeValues.ByReleaseDate"/>).</summary>
    public required string NewSortMode { get; set; }

    /// <summary>Bestätigung, dass die manuelle Reihenfolge verworfen werden darf (nach 409-Konflikt).</summary>
    public bool ConfirmLossOfManualOrder { get; set; }
}

/// <summary>
/// Anforderung zum Setzen der Anzeige-Genres einer Playlist
/// (<c>PUT api/playlists/{id}/genres</c>, serverseitig
/// <c>DtoSetPlaylistGenresRequest</c>).
/// </summary>
public class SetPlaylistGenresRequest
{
    /// <summary>Ids der gewählten Genres.</summary>
    public required IReadOnlyList<long> GenreIds { get; set; }
}

/// <summary>
/// Anforderung zum Setzen des Öffentlich-Flags einer Playlist
/// (<c>PUT api/playlists/{id}/public</c>, serverseitig
/// <c>DtoSetPlaylistPublicRequest</c>) — nur Administrator:innen
/// mit eigener Playlist (sonst HTTP 403).
/// </summary>
public class SetPlaylistPublicRequest
{
    /// <summary><c>true</c> = Playlist öffentlich schalten.</summary>
    public required bool IsPublic { get; set; }
}

/// <summary>
/// Antwort des Max-SortOrder-Nachschlags
/// (<c>GET api/playlists/{id}/entries/max-sort-order</c>,
/// serverseitig <c>DtoMaxSortOrderResult</c>) — Grundlage des
/// „Ans Ende"-Verschiebens (<c>max + 1</c> als Ziel-SortOrder).
/// </summary>
public class PlaylistMaxSortOrderResult
{
    /// <summary>Größte belegte Sortierreihenfolge; <c>null</c> bei leerer Playlist.</summary>
    public long? MaxSortOrder { get; set; }
}
