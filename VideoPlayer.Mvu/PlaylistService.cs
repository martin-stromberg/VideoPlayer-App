using VideoPlayer.ApiClient.Models;

namespace VideoPlayer.Mvu;

/// <summary>Ergebnis einer geladenen Eintragsseite einer Playlist.</summary>
/// <param name="Page">Geladene Seite (1-basiert, Server-Konvention).</param>
/// <param name="Items">Die Einträge der Seite in Server-Reihenfolge.</param>
/// <param name="HasMore"><c>true</c>, solange der Server weitere Seiten meldet.</param>
/// <param name="TotalCount">Gesamtzahl der Einträge der Playlist.</param>
/// <returns>Datensatz der geladenen Eintragsseite.</returns>
public sealed record PlaylistEntriesPage(
    int Page,
    IReadOnlyList<PlaylistEntryItem> Items,
    bool HasMore,
    int TotalCount);

/// <summary>Ergebnis des Übersichts-Laufs: eigene und öffentliche Playlists.</summary>
/// <param name="Own">Die eigenen Playlists des angemeldeten Benutzers.</param>
/// <param name="Public">Die öffentlichen Playlists.</param>
/// <returns>Datensatz des Übersichts-Laufs.</returns>
public sealed record PlaylistsOverview(
    IReadOnlyList<PlaylistListItem> Own,
    IReadOnlyList<PlaylistListItem> Public);

/// <summary>
/// Lädt die Playlist-Daten vom Server über den angemeldeten
/// <see cref="AuthSessionService.Client"/>: eigene und öffentliche
/// Playlists (<c>api/playlists</c>, <c>api/playlists/public</c>),
/// einzelne Playlist-Kopfdaten (<c>api/playlists/{id}</c>) und
/// paginierte Einträge (<c>api/playlists/{id}/entries/paged</c>).
/// Ergebnisse werden auf die plattformneutralen Anzeige-Records
/// gemappt. Playlists sind ein Online-Feature — ohne angemeldeten
/// Client schlagen die Lademethoden fehl.
/// </summary>
public sealed class PlaylistService
{
    private readonly AuthSessionService authSession;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client.</param>
    public PlaylistService(AuthSessionService authSession)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
    }

    /// <summary>
    /// Lädt eigene und öffentliche Playlists in einem Lauf
    /// (<c>api/playlists</c> und <c>api/playlists/public</c>) — beide
    /// Sektionen der Übersichtsseite.
    /// </summary>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Die Playlists beider Sektionen als Anzeige-Einträge.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task<PlaylistsOverview> LoadPlaylistsAsync(CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        var own = await client.GetPlaylistsAsync(cancellationToken: cancellationToken);
        var publicPlaylists = await client.GetPublicPlaylistsAsync(cancellationToken: cancellationToken);
        return new PlaylistsOverview(MapPlaylists(own), MapPlaylists(publicPlaylists));
    }

    /// <summary>
    /// Lädt die Kopfdaten einer einzelnen Playlist
    /// (<c>api/playlists/{id}</c>) — Nachschlag für die Detailseite, wenn
    /// der Zustand ohne vorherige Listen-Auswahl aufgebaut wurde.
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Der Anzeige-Eintrag der Playlist bzw. <c>null</c>, wenn sie nicht existiert.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task<PlaylistListItem?> LoadPlaylistAsync(long playlistId, CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        var playlist = await client.GetPlaylistAsync(playlistId, cancellationToken);
        return playlist is null ? null : MapPlaylist(playlist);
    }

    /// <summary>
    /// Lädt eine Eintragsseite der Playlist
    /// (<c>api/playlists/{id}/entries/paged</c>) — die Einträge kommen in
    /// der serverseitigen Reihenfolge und werden nicht umsortiert.
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="page">Zu ladende Seite (1-basiert, Server-Konvention).</param>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Die geladene Eintragsseite.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task<PlaylistEntriesPage> LoadEntriesAsync(
        long playlistId,
        int page,
        CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        var result = await client.GetPlaylistEntriesPagedAsync(
            playlistId, page, PlaylistDetailsState.PageSize, cancellationToken);

        var items = new List<PlaylistEntryItem>(result.Entries.Length);
        foreach (var entry in result.Entries)
        {
            // Einträge ohne identifizierbares Medium werden übersprungen —
            // die Auswahl muss das Medium eindeutig bestimmen können.
            if (entry.MediaId <= 0 || string.IsNullOrWhiteSpace(entry.MediaType))
            {
                continue;
            }

            items.Add(new PlaylistEntryItem
            {
                EntryId = entry.Id,
                MediaId = entry.MediaId,
                MediaType = MediaTypeNames.Normalize(entry.MediaType),
                Title = entry.MediaTitle,
                ParentMediaType = entry.ParentMediaType is null ? null : MediaTypeNames.Normalize(entry.ParentMediaType),
                ParentMediaId = entry.ParentMediaId,
                ParentMediaTitle = entry.ParentMediaTitle,
                ResolvedPictureId = entry.ResolvedPictureId,
                EpisodeNumber = entry.EpisodeNumber,
                IsAccessible = entry.IsAccessible,
                ReleaseDate = entry.ReleaseDate
            });
        }

        return new PlaylistEntriesPage(result.PageNumber, items, result.HasNextPage, result.TotalCount);
    }

    private static IReadOnlyList<PlaylistListItem> MapPlaylists(IReadOnlyList<Playlist> playlists)
    {
        var items = new List<PlaylistListItem>(playlists.Count);
        foreach (var playlist in playlists)
        {
            if (playlist.Id <= 0)
            {
                continue;
            }

            items.Add(MapPlaylist(playlist));
        }

        return items;
    }

    internal static PlaylistListItem MapPlaylist(Playlist playlist)
    {
        return new PlaylistListItem
        {
            PlaylistId = playlist.Id,
            Name = playlist.Name,
            Description = playlist.Description,
            SortMode = playlist.SortMode,
            CoverPictureId = playlist.CoverPictureId,
            GenreText = string.Join(", ", playlist.Genres.Select(g => g.Name)),
            IsPublic = playlist.IsPublic,
            IsOwner = playlist.IsOwner
        };
    }
}
