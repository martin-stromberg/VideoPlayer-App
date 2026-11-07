using VideoPlayer.ApiClient.Models;

namespace VideoPlayer.Mvu;

/// <summary>
/// Favoriten-Umschaltung aus der Detailseite: sendet die Änderung über
/// <c>api/favorites/toggle</c> zum Server (führendes System) und
/// erneuert danach die lokale Kopie <c>cached_favorites</c> über
/// <see cref="ServerSyncService.SyncFavoritesAsync"/> — derselbe Pfad
/// wie die ereignisgesteuerte Synchronisation, keine eigene
/// Schreiblogik für die Tabelle.
/// </summary>
public sealed class FavoritesService
{
    private readonly AuthSessionService authSession;
    private readonly ServerSyncService syncService;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client.</param>
    /// <param name="syncService">Synchronisationsdienst für die lokale Favoriten-Kopie.</param>
    public FavoritesService(AuthSessionService authSession, ServerSyncService syncService)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
        this.syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
    }

    /// <summary>
    /// Schaltet die Favoriten-Markierung des Mediums um.
    /// </summary>
    /// <param name="mediaType">Medientyp (wird für den Toggle-Request normalisiert).</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="title">Anzeigetitel des Mediums.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>
    /// Neuer Favoriten-Zustand laut Server; <c>null</c>, wenn kein
    /// angemeldeter Client vorhanden ist (offline).
    /// </returns>
    public async Task<bool?> ToggleAsync(string mediaType, long mediaId, string title, CancellationToken cancellationToken = default)
    {
        var client = authSession.Client;
        if (client is null)
        {
            return null;
        }

        var isFavorite = await client.ToggleFavoriteAsync(
            CreateEntry(mediaType, mediaId, title), cancellationToken);

        // Lokale Kopie autoritativ vom Serverstand erneuern — damit sind
        // Favoriten-Liste und Detail-Markierung nach Neustart konsistent.
        await syncService.SyncFavoritesAsync(cancellationToken);

        return isFavorite;
    }

    /// <summary>
    /// Entfernt einen Favoriten-Eintrag über <c>api/favorites/remove</c>
    /// auf dem Server (führendes System — dieselbe Aktion wie die
    /// Entfernen-Kontextaktion der Favoriten-Liste der Webanwendung) und
    /// erneuert danach die lokale Kopie <c>cached_favorites</c> über
    /// <see cref="ServerSyncService.SyncFavoritesAsync"/>.
    /// </summary>
    /// <param name="favoriteId">Id des zu entfernenden Favoriteneintrags.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>
    /// <c>true</c>, wenn der Eintrag entfernt wurde; <c>false</c>, wenn
    /// kein angemeldeter Client vorhanden ist (offline).
    /// </returns>
    public async Task<bool> RemoveFavoriteAsync(long favoriteId, CancellationToken cancellationToken = default)
    {
        var client = authSession.Client;
        if (client is null)
        {
            return false;
        }

        await client.RemoveFavoriteAsync(favoriteId, cancellationToken);

        // Lokale Kopie autoritativ vom Serverstand erneuern — damit sind
        // Favoriten-Seite und Startseiten-Liste nach Neustart konsistent.
        await syncService.SyncFavoritesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Erzeugt den typkorrekten <see cref="MediaEntry"/> für den
    /// Toggle-Request (polymorphes <c>$type</c>); unbekannte Typen fallen
    /// auf die Basisklasse zurück.
    /// </summary>
    /// <param name="mediaType">Medientyp (wird normalisiert).</param>
    /// <param name="id">Id des Mediums.</param>
    /// <param name="name">Anzeigename des Mediums.</param>
    /// <returns>Der typkorrekte <see cref="MediaEntry"/>.</returns>
    internal static MediaEntry CreateEntry(string mediaType, long id, string name)
    {
        return MediaTypeNames.Normalize(mediaType) switch
        {
            "movie" => new Movie { Id = id, Name = name },
            "moviecollection" => new MovieCollection { Id = id, Name = name },
            "tvshow" => new TvShow { Id = id, Name = name },
            "tvshowseason" => new TvShowSeason { Id = id, Name = name },
            "tvshowepisode" => new TvShowEpisode { Id = id, Name = name },
            _ => new MediaEntry { Id = id, Name = name }
        };
    }
}
