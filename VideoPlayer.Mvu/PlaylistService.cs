using VideoPlayer.ApiClient;
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
/// Aufgelöster Starteintrag einer Playlist-Wiedergabe
/// (<c>POST api/playlists/{id}/play</c>) — die Daten, die der
/// <see cref="PlayerState"/> für den Playlist-Kontext und die
/// Quellauflösung benötigt.
/// </summary>
public sealed record PlaylistPlaybackStartInfo
{
    /// <summary>Id der Playlist.</summary>
    public required long PlaylistId { get; init; }

    /// <summary>Name der Playlist.</summary>
    public required string PlaylistName { get; init; }

    /// <summary>Gesamtzahl der Einträge der Playlist laut Server.</summary>
    public required int TotalCount { get; init; }

    /// <summary>1-basierte Position des Starteintrags in der Playlist-Reihenfolge.</summary>
    public required int CurrentPosition { get; init; }

    /// <summary>Id des aufgelösten Starteintrags (Playlist-Eintrag).</summary>
    public required long CurrentEntryId { get; init; }

    /// <summary>Medientyp des Mediums (normalisiert, CLR-Konvention).</summary>
    public required string MediaType { get; init; }

    /// <summary>Id des Mediums des Starteintrags.</summary>
    public required long MediaId { get; init; }

    /// <summary>Anzeigetitel des Mediums.</summary>
    public required string Title { get; init; }

    /// <summary>Fortsetzungsposition des Starteintrags in Sekunden (Weiterschauen-Stand; 0 ohne Stand).</summary>
    public required long StartPositionSeconds { get; init; }
}

/// <summary>
/// Aufgelöster Zieleintrag einer Playlist-Navigation
/// (<c>POST api/playlists/{id}/play/next|previous|advance</c>).
/// </summary>
public sealed record PlaylistNavigationItem
{
    /// <summary>Id des Zieleintrags (Playlist-Eintrag).</summary>
    public required long EntryId { get; init; }

    /// <summary>1-basierte Position des Zieleintrags in der Playlist-Reihenfolge.</summary>
    public required int Position { get; init; }

    /// <summary>Medientyp des Mediums (normalisiert, CLR-Konvention).</summary>
    public required string MediaType { get; init; }

    /// <summary>Id des Mediums des Zieleintrags.</summary>
    public required long MediaId { get; init; }

    /// <summary>Anzeigetitel des Mediums.</summary>
    public required string Title { get; init; }

    /// <summary>Fortsetzungsposition des Zieleintrags in Sekunden (Weiterschauen-Stand; 0 ohne Stand).</summary>
    public required long StartPositionSeconds { get; init; }
}

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

    /// <summary>
    /// Startet die Wiedergabe einer Playlist
    /// (<c>POST api/playlists/{id}/play</c>) ab dem angegebenen Eintrag
    /// bzw. ab dem ersten abspielbaren und zugänglichen Eintrag, wenn
    /// <paramref name="entryId"/> nicht gesetzt ist. Online-Feature —
    /// ohne angemeldeten Client schlägt der Aufruf fehl.
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="entryId">Id des Starteintrags; <c>null</c> = erster abspielbarer und zugänglicher Eintrag.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Die Startinformationen des aufgelösten Eintrags.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden oder Antwort ohne Starteintrag.</exception>
    public async Task<PlaylistPlaybackStartInfo> StartPlaybackAsync(
        long playlistId,
        long? entryId = null,
        CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        var start = await client.StartPlaylistPlaybackAsync(playlistId, entryId, cancellationToken);
        var entry = start.CurrentEntry;
        var title = entry?.MediaTitle;
        return new PlaylistPlaybackStartInfo
        {
            PlaylistId = start.PlaylistId,
            PlaylistName = start.PlaylistName,
            TotalCount = start.TotalCount,
            CurrentPosition = start.CurrentPosition,
            CurrentEntryId = start.CurrentEntryId,
            MediaType = MediaTypeNames.Normalize(start.MediaType),
            MediaId = start.MediaId,
            Title = string.IsNullOrWhiteSpace(title) ? start.PlaylistName : title,
            StartPositionSeconds = start.StartPositionSeconds
        };
    }

    /// <summary>
    /// Liefert den nächsten abspielbaren und zugänglichen Eintrag der
    /// Playlist (<c>POST api/playlists/{id}/play/next</c>) — manuelle
    /// Vorwärts-Navigation.
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="currentEntryId">Id des aktuell gespielten Eintrags.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Der Folgeeintrag bzw. <c>null</c>, wenn keiner existiert.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public Task<PlaylistNavigationItem?> GetNextEntryAsync(
        long playlistId,
        long currentEntryId,
        CancellationToken cancellationToken = default)
    {
        return NavigateAsync(playlistId, currentEntryId,
            (c, ct) => c.GetNextPlaylistEntryAsync(playlistId, currentEntryId, ct), cancellationToken);
    }

    /// <summary>
    /// Liefert den vorherigen abspielbaren und zugänglichen Eintrag der
    /// Playlist (<c>POST api/playlists/{id}/play/previous</c>) — manuelle
    /// Rückwärts-Navigation.
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="currentEntryId">Id des aktuell gespielten Eintrags.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Der vorherige Eintrag bzw. <c>null</c>, wenn keiner existiert.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public Task<PlaylistNavigationItem?> GetPreviousEntryAsync(
        long playlistId,
        long currentEntryId,
        CancellationToken cancellationToken = default)
    {
        return NavigateAsync(playlistId, currentEntryId,
            (c, ct) => c.GetPreviousPlaylistEntryAsync(playlistId, currentEntryId, ct), cancellationToken);
    }

    /// <summary>
    /// Liefert den Folgeeintrag für das automatische Weiterschalten am
    /// Titelende (<c>POST api/playlists/{id}/play/advance</c>).
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="currentEntryId">Id des zu Ende gespielten Eintrags.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Der Folgeeintrag bzw. <c>null</c>, wenn keiner existiert.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public Task<PlaylistNavigationItem?> AdvanceAsync(
        long playlistId,
        long currentEntryId,
        CancellationToken cancellationToken = default)
    {
        return NavigateAsync(playlistId, currentEntryId,
            (c, ct) => c.AdvancePlaylistAsync(playlistId, currentEntryId, ct), cancellationToken);
    }

    /// <summary>
    /// Gemeinsamer Kern der drei Navigations-Endpunkte: ruft die
    /// übergebene Client-Operation auf und mappt das Ergebnis auf den
    /// Anzeige-Record; Ergebnisse ohne identifizierbaren Eintrag gelten
    /// als „kein Folgeeintrag".
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="currentEntryId">Id des aktuellen Eintrags.</param>
    /// <param name="call">Die Client-Operation (next/previous/advance).</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Der aufgelöste Eintrag bzw. <c>null</c>, wenn keiner existiert.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    private async Task<PlaylistNavigationItem?> NavigateAsync(
        long playlistId,
        long currentEntryId,
        Func<VideoPlayerApiClient, CancellationToken, Task<PlaylistNavigationResult?>> call,
        CancellationToken cancellationToken)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        var result = await call(client, cancellationToken);
        var entry = result?.Entry;
        if (entry is null || entry.MediaId <= 0 || string.IsNullOrWhiteSpace(entry.MediaType))
        {
            return null;
        }

        return new PlaylistNavigationItem
        {
            EntryId = entry.Id,
            Position = result!.Position,
            MediaType = MediaTypeNames.Normalize(entry.MediaType),
            MediaId = entry.MediaId,
            Title = entry.MediaTitle,
            StartPositionSeconds = result.StartPositionSeconds
        };
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
