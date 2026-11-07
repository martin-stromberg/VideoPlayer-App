namespace VideoPlayer.Mvu;

/// <summary>
/// Playlist-Bezug eines Downloads: wird beim Einreihen mitgegeben
/// (<see cref="DownloadService.EnqueueAsync"/>) und im Download-Manifest
/// (<c>download_manifest</c>) gespeichert. Grundlage der Einplanung in
/// Playlist-Reihenfolge: Der automatische Vorlauf
/// (<see cref="AutoDownloadService"/>) reiht bei playlist-gebundenen
/// Weiterschauen-Einträgen die Folge-Einträge der Playlist in
/// Server-Reihenfolge ein; die FIFO-Warteschlange übernimmt diese
/// Reihenfolge als Ausführungsreihenfolge.
/// </summary>
/// <param name="PlaylistId">Id der Playlist, aus deren Kontext der Download eingeplant wurde; <c>null</c> = kein Playlist-Bezug.</param>
/// <param name="PlaylistEntryId">Id des Playlist-Eintrags, dem das Medium entspricht; <c>null</c> = unbekannt.</param>
/// <param name="PlaylistSortOrder">Sortierposition des Eintrags in der Playlist (serverseitige Reihenfolge); <c>null</c> = nicht zugewiesen (z. B. Sortiermodus <c>ByReleaseDate</c>).</param>
/// <returns>Kontextdatensatz eines playlist-gebundenen Downloads.</returns>
public sealed record DownloadPlaylistContext(
    long? PlaylistId,
    long? PlaylistEntryId,
    long? PlaylistSortOrder);
