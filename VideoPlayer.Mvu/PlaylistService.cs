using System.Net;
using System.Text.Json;
using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Storage;

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
    private readonly AppSecretsService? secrets;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client.</param>
    /// <param name="secrets">Sichere Ablage des Anmelde-Tokens (Claim-Lesung für den Öffentlich-Schalter); <c>null</c> = Admin-Status nicht ermittelbar.</param>
    public PlaylistService(AuthSessionService authSession, AppSecretsService? secrets = null)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
        this.secrets = secrets;
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
                ReleaseDate = entry.ReleaseDate,
                SortOrder = entry.SortOrder
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

    /// <summary>
    /// Ermittelt, ob der angemeldete Benutzer den <c>IsAdmin</c>-Claim
    /// des gespeicherten Anmelde-Tokens trägt — die serverseitige
    /// Prüfung bleibt führend; die Lesung steuert nur die Sichtbarkeit
    /// des Öffentlich-Schalters.
    /// </summary>
    /// <returns><c>true</c>, wenn der Claim den Administrator-Status meldet.</returns>
    public async Task<bool> IsCurrentUserAdminAsync()
    {
        if (secrets is null)
        {
            return false;
        }

        var token = await secrets.GetAuthTokenAsync();
        return ProfileInfoService.ReadIsAdminClaim(token);
    }

    /// <summary>
    /// Lädt nur die eigenen Playlists des angemeldeten Benutzers
    /// (<c>api/playlists</c>) — Zielauswahl der Aktion
    /// „Zu Playlist hinzufügen" (Hinzufügen ist nur in eigene
    /// Playlists erlaubt).
    /// </summary>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Die eigenen Playlists als Anzeige-Einträge.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task<IReadOnlyList<PlaylistListItem>> LoadOwnPlaylistsAsync(CancellationToken cancellationToken = default)
    {
        var overview = await LoadPlaylistsAsync(cancellationToken);
        return overview.Own;
    }

    /// <summary>
    /// Lädt die wählbaren Genre-Optionen des Servers
    /// (<c>api/items/genres</c>) für den Genre-Editor.
    /// </summary>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Die Genre-Optionen (Id + Klartext-Name).</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task<IReadOnlyList<PlaylistGenreOptionItem>> LoadGenreOptionsAsync(CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        var options = await client.GetGenreOptionsAsync(cancellationToken);
        return options
            .Where(o => o.Id > 0 && !string.IsNullOrWhiteSpace(o.Name))
            .Select(o => new PlaylistGenreOptionItem { Id = o.Id, Name = o.Name })
            .ToList();
    }

    /// <summary>
    /// Legt eine Playlist an (<c>POST api/playlists</c>).
    /// </summary>
    /// <param name="name">Name der neuen Playlist.</param>
    /// <param name="description">Optionale Beschreibung.</param>
    /// <param name="sortMode">Sortiermodus (<see cref="PlaylistSortModeValues"/>).</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Die angelegte Playlist als Anzeige-Eintrag.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task<PlaylistListItem> CreatePlaylistAsync(
        string name,
        string? description,
        string sortMode,
        CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        var playlist = await client.CreatePlaylistAsync(new CreatePlaylistRequest
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            SortMode = sortMode
        }, cancellationToken);
        return MapPlaylist(playlist);
    }

    /// <summary>
    /// Aktualisiert Name und Beschreibung einer Playlist
    /// (<c>PUT api/playlists/{id}</c>).
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="name">Neuer Name.</param>
    /// <param name="description">Neue Beschreibung; <c>null</c>/leer entfernt sie.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Die aktualisierte Playlist als Anzeige-Eintrag.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task<PlaylistListItem> UpdatePlaylistAsync(
        long playlistId,
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        var playlist = await client.UpdatePlaylistAsync(playlistId, new UpdatePlaylistRequest
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description
        }, cancellationToken);
        return MapPlaylist(playlist);
    }

    /// <summary>
    /// Löscht eine Playlist (<c>DELETE api/playlists/{id}</c>).
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Löschen.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task DeletePlaylistAsync(long playlistId, CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        await client.DeletePlaylistAsync(playlistId, cancellationToken);
    }

    /// <summary>
    /// Fügt ein Medium zu einer Playlist hinzu
    /// (<c>POST api/playlists/{id}/entries</c>) und liefert den
    /// serverseitigen Hinweistext (z. B. übersprungene Duplikate).
    /// </summary>
    /// <param name="playlistId">Id der Ziel-Playlist.</param>
    /// <param name="mediaType">Medientyp (beliebige Konvention — wird auf den Server-Enum-Namen abgebildet).</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Der serverseitige Hinweistext; <c>null</c> = Standardtext verwenden.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task<string?> AddMediaToPlaylistAsync(
        long playlistId,
        string mediaType,
        long mediaId,
        CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        var result = await client.AddPlaylistEntryAsync(playlistId, new AddPlaylistEntryRequest
        {
            MediaType = MediaTypeNames.ToServerName(mediaType),
            MediaId = mediaId
        }, cancellationToken);
        return result.Message;
    }

    /// <summary>
    /// Entfernt einen Eintrag aus einer Playlist
    /// (<c>DELETE api/playlists/{id}/entries/{mediaType}/{mediaId}</c>).
    /// Meldet der Server HTTP 409 mit Weiterschauen-Bezug, kommt das
    /// Ergebnis mit gesetztem <c>RequiresConfirmation</c> zurück — die
    /// Anfrage kann nach Bestätigung mit
    /// <paramref name="confirmContinueWatchingRemoval"/> erneut werden.
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="mediaType">Medientyp des Eintrags (beliebige Konvention).</param>
    /// <param name="mediaId">Id des Mediums des Eintrags.</param>
    /// <param name="confirmContinueWatchingRemoval"><c>true</c> = Bestätigung des Weiterschauen-Entfernens mitsenden.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Das Ergebnis (erfolgreich oder Bestätigung erforderlich).</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task<PlaylistMutationOutcome> RemoveEntryAsync(
        long playlistId,
        string mediaType,
        long mediaId,
        bool confirmContinueWatchingRemoval = false,
        CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        try
        {
            await client.RemovePlaylistEntryAsync(
                playlistId,
                MediaTypeNames.ToServerName(mediaType),
                mediaId,
                confirmContinueWatchingRemoval,
                cancellationToken);
            return new PlaylistMutationOutcome(Succeeded: true, RequiresConfirmation: false);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict
            && IsContinueWatchingConflict(ex))
        {
            return new PlaylistMutationOutcome(Succeeded: false, RequiresConfirmation: true);
        }
    }

    /// <summary>
    /// Verschiebt einen Playlist-Eintrag an den Anfang
    /// (<c>POST api/playlists/{id}/entries/{entryId}/move-to-beginning</c>).
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="entryId">Id des Playlist-Eintrags.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Verschieben.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task MoveEntryToBeginningAsync(
        long playlistId,
        long entryId,
        CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        await client.MovePlaylistEntryToBeginningAsync(playlistId, entryId, cancellationToken);
    }

    /// <summary>
    /// Verschiebt einen Playlist-Eintrag ans Ende
    /// (<c>GET api/playlists/{id}/entries/max-sort-order</c> +
    /// <c>PUT …/entries/{entryId}/order</c> mit <c>max + 1</c>,
    /// wie die Server-UI ohne eigenen move-to-end-Endpunkt).
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="entryId">Id des Playlist-Eintrags.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Verschieben.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task MoveEntryToEndAsync(
        long playlistId,
        long entryId,
        CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        var maxSortOrder = await client.GetPlaylistMaxSortOrderAsync(playlistId, cancellationToken) ?? -1;
        await client.ReorderPlaylistEntryAsync(
            playlistId,
            entryId,
            new ReorderPlaylistEntryRequest { NewSortOrder = maxSortOrder + 1 },
            cancellationToken);
    }

    /// <summary>
    /// Verschiebt einen Playlist-Eintrag zwischen Positionen
    /// (<c>POST api/playlists/{id}/entries/{entryId}/move-between</c>).
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="entryId">Id des Playlist-Eintrags.</param>
    /// <param name="newSortOrder">Ziel-Sortierreihenfolge (SortOrder des Ziel-Nachbarn).</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Verschieben.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task MoveEntryBetweenAsync(
        long playlistId,
        long entryId,
        long newSortOrder,
        CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        await client.MovePlaylistEntryBetweenAsync(
            playlistId,
            entryId,
            new ReorderPlaylistEntryRequest { NewSortOrder = newSortOrder },
            cancellationToken);
    }

    /// <summary>
    /// Schaltet den Sortiermodus einer Playlist um
    /// (<c>PATCH api/playlists/{id}/sort-mode</c>). Verlässt die Playlist
    /// den manuellen Modus ohne Bestätigung, kommt das Ergebnis mit
    /// gesetztem <c>RequiresConfirmation</c> zurück — die Anfrage kann
    /// nach Bestätigung mit
    /// <paramref name="confirmLossOfManualOrder"/> erneut werden.
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="newSortMode">Ziel-Sortiermodus (<see cref="PlaylistSortModeValues"/>).</param>
    /// <param name="confirmLossOfManualOrder"><c>true</c> = Verlust der manuellen Reihenfolge bestätigen.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Die aktualisierte Playlist bei Erfolg bzw. das Konflikt-Ergebnis.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task<PlaylistSortModeChangeResult> ChangeSortModeAsync(
        long playlistId,
        string newSortMode,
        bool confirmLossOfManualOrder = false,
        CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        try
        {
            var playlist = await client.ChangePlaylistSortModeAsync(playlistId, new ChangeSortModeRequest
            {
                NewSortMode = newSortMode,
                ConfirmLossOfManualOrder = confirmLossOfManualOrder
            }, cancellationToken);
            return new PlaylistSortModeChangeResult(MapPlaylist(playlist), RequiresConfirmation: false);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict
            && IsLossOfDataConflict(ex))
        {
            return new PlaylistSortModeChangeResult(null, RequiresConfirmation: true);
        }
    }

    /// <summary>
    /// Setzt die Anzeige-Genres einer Playlist
    /// (<c>PUT api/playlists/{id}/genres</c>).
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="genreIds">Ids der gewählten Genres.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Die aktualisierte Playlist als Anzeige-Eintrag.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task<PlaylistListItem> SetGenresAsync(
        long playlistId,
        IReadOnlyList<long> genreIds,
        CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        var playlist = await client.SetPlaylistGenresAsync(
            playlistId,
            new SetPlaylistGenresRequest { GenreIds = genreIds },
            cancellationToken);
        return MapPlaylist(playlist);
    }

    /// <summary>
    /// Setzt die Genres einer Playlist auf die berechneten Werte zurück
    /// (<c>POST api/playlists/{id}/genres/reset</c>).
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Die aktualisierte Playlist als Anzeige-Eintrag.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task<PlaylistListItem> ResetGenresAsync(
        long playlistId,
        CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        var playlist = await client.ResetPlaylistGenresAsync(playlistId, cancellationToken);
        return MapPlaylist(playlist);
    }

    /// <summary>
    /// Setzt das Öffentlich-Flag einer Playlist
    /// (<c>PUT api/playlists/{id}/public</c>) — serverseitig nur
    /// Administrator:innen mit eigener Playlist (sonst HTTP 403).
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="isPublic"><c>true</c> = öffentlich schalten.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Die aktualisierte Playlist als Anzeige-Eintrag.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task<PlaylistListItem> SetPublicAsync(
        long playlistId,
        bool isPublic,
        CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        var playlist = await client.SetPlaylistPublicAsync(
            playlistId,
            new SetPlaylistPublicRequest { IsPublic = isPublic },
            cancellationToken);
        return MapPlaylist(playlist);
    }

    /// <summary>
    /// Prüft, ob eine 409-Antwort den Weiterschauen-Konflikt des
    /// Eintrags-Entfernens meldet
    /// (<c>IsContinueWatchingConfirmationRequired</c> im Antwortkörper —
    /// die Feldform deckt Entfernen- und Sortiermodus-Konflikte ab).
    /// </summary>
    /// <param name="exception">Die gefangene API-Ausnahme.</param>
    /// <returns><c>true</c>, wenn die Bestätigungsflagge gesetzt ist.</returns>
    private static bool IsContinueWatchingConflict(ApiException exception) =>
        ReadConflictFlag(exception, "IsContinueWatchingConfirmationRequired");

    /// <summary>
    /// Prüft, ob eine 409-Antwort den Datenverlust-Konflikt des
    /// Sortiermodus-Wechsels meldet
    /// (<c>IsLossOfDataConfirmationRequired</c> im Antwortkörper).
    /// </summary>
    /// <param name="exception">Die gefangene API-Ausnahme.</param>
    /// <returns><c>true</c>, wenn die Bestätigungsflagge gesetzt ist.</returns>
    private static bool IsLossOfDataConflict(ApiException exception) =>
        ReadConflictFlag(exception, "IsLossOfDataConfirmationRequired");

    /// <summary>
    /// Liest eine Bestätigungsflagge aus dem JSON-Antwortkörper einer
    /// Konflikt-Antwort (HTTP 409); unlesbare/fehlende Körper gelten
    /// als „kein Konflikt" und laufen den normalen Fehlerpfad.
    /// </summary>
    /// <param name="exception">Die gefangene API-Ausnahme.</param>
    /// <param name="property">Der gesuchte Feldname (Groß-/Kleinschreibung tolerant).</param>
    /// <returns><c>true</c>, wenn die Flagge gesetzt ist.</returns>
    private static bool ReadConflictFlag(ApiException exception, string property)
    {
        if (string.IsNullOrWhiteSpace(exception.ResponseBody))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(exception.ResponseBody);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var member in document.RootElement.EnumerateObject())
            {
                if (string.Equals(member.Name, property, StringComparison.OrdinalIgnoreCase)
                    && member.Value.ValueKind == JsonValueKind.True)
                {
                    return true;
                }
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
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
            IsOwner = playlist.IsOwner,
            AllGenreIds = playlist.AllGenreIds,
            GenresManuallyOverridden = playlist.GenresManuallyOverridden
        };
    }
}
