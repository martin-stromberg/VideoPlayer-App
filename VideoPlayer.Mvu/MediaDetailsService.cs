using System.Net;
using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Mvu;

/// <summary>
/// Lädt die Detaildaten eines Mediums über den angemeldeten
/// <see cref="AuthSessionService.Client"/> und mappt sie auf das
/// plattformneutrale <see cref="MediaDetailsView"/>. Serien liefern
/// Staffeln und Episoden eingebettet; Episoden- und Staffel-Aufrufe
/// werden — soweit über <c>Season.Show</c> bzw. die Routen-Hints
/// auflösbar — in den Serienkontext mit Vorwahl gebracht (Muster des
/// Altbestands <c>TVShowDetailsPage(tvShowId, seasonId, episodeId)</c>).
/// Ohne Serververbindung baut <see cref="LoadAsync"/> einen
/// Offline-Snapshot aus dem Download-Manifest und der gecachten
/// Serienstruktur (<see cref="ShowDetailsCache"/>) — heruntergeladene
/// Medien bleiben so abspielbar; nicht geladene Episoden werden als
/// Platzhalter markiert.
/// </summary>
public sealed class MediaDetailsService
{
    private readonly AuthSessionService authSession;
    private readonly CacheDatabase cache;
    private readonly Func<string, bool> fileExists;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client.</param>
    /// <param name="cache">Cache-Ablage (Download-Manifest, Serienstruktur, Favoriten-Kopie).</param>
    public MediaDetailsService(AuthSessionService authSession, CacheDatabase cache)
        : this(authSession, cache, null)
    {
    }

    /// <summary>Erstellt den Dienst mit injizierbarer Dateiprüfung (für Tests).</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client.</param>
    /// <param name="cache">Cache-Ablage (Download-Manifest, Serienstruktur, Favoriten-Kopie).</param>
    /// <param name="fileExists">Dateiprüfung; <c>null</c> = <see cref="File.Exists(string?)"/>.</param>
    internal MediaDetailsService(AuthSessionService authSession, CacheDatabase cache, Func<string, bool>? fileExists)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.fileExists = fileExists ?? File.Exists;
    }

    /// <summary>
    /// Lädt die Detailansicht zum navigierten Medium.
    /// <paramref name="showId"/>/<paramref name="seasonId"/>/
    /// <paramref name="episodeId"/> sind optionale Hints aus der
    /// Listen-Auswahl (eingebetteter Objektgraph). Ohne angemeldeten
    /// Client wird ein Offline-Snapshot aus lokalen Daten gebaut.
    /// </summary>
    /// <param name="mediaType">Medientyp (wird normalisiert).</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="fallbackTitle">Fallback-Titel, falls der Server keinen Namen liefert.</param>
    /// <param name="showId">Optionaler Hint: Id der zugehörigen Serie.</param>
    /// <param name="seasonId">Optionaler Hint: Id der vorzuwählenden Staffel.</param>
    /// <param name="episodeId">Optionaler Hint: Id der hervorzuhebenden Episode.</param>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Das Anzeigemodell der Detailseite.</returns>
    /// <exception cref="InvalidOperationException">Medientyp/Identität nicht auflösbar — online wie offline.</exception>
    public async Task<MediaDetailsView> LoadAsync(
        string mediaType,
        long mediaId,
        string? fallbackTitle,
        long? showId,
        long? seasonId,
        long? episodeId,
        CancellationToken cancellationToken = default)
    {
        var client = authSession.Client;
        if (client is null)
        {
            return await LoadOfflineAsync(
                mediaType, mediaId, fallbackTitle, showId, seasonId, episodeId, cancellationToken);
        }

        return MediaTypeNames.Normalize(mediaType) switch
        {
            "movie" => MapMovie(await client.GetMovieAsync(mediaId, cancellationToken)),
            "moviecollection" => await LoadCollectionOrMovieAsync(client, mediaId, cancellationToken),
            "tvshow" => MapShow(
                await FetchShowAsync(client, mediaId, cancellationToken),
                "tvshow", mediaId, favoriteOverride: null, fallbackTitle, seasonId, episodeId),
            "tvshowseason" => await LoadSeasonInShowContextAsync(client, mediaId, showId, episodeId, cancellationToken),
            "tvshowepisode" => await LoadEpisodeAsync(client, mediaId, fallbackTitle, showId, seasonId, cancellationToken),
            var other => throw new InvalidOperationException($"Der Medientyp '{other}' wird nicht unterstützt.")
        };
    }

    /// <summary>
    /// Baut die Detailansicht ohne Server aus dem Download-Manifest, der
    /// gecachten Serienstruktur und der Favoriten-Kopie. Filme und
    /// Episoden bleiben abspielbar (<see cref="MediaDetailsView.HasLocalFile"/>),
    /// fehlende Episoden erscheinen als Platzhalter
    /// (<see cref="EpisodeItem.IsPlaceholder"/>). Ohne lokale Daten wird
    /// eine <see cref="InvalidOperationException"/> geworfen — die
    /// Detailseite zeigt dann ihren regulären Fehlertext.
    /// </summary>
    /// <param name="mediaType">Medientyp (wird normalisiert).</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="fallbackTitle">Fallback-Titel der Listen-Auswahl.</param>
    /// <param name="showId">Optionaler Hint: Id der zugehörigen Serie.</param>
    /// <param name="seasonId">Optionaler Hint: Id der vorzuwählenden Staffel.</param>
    /// <param name="episodeId">Optionaler Hint: Id der hervorzuhebenden Episode.</param>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Der Offline-Snapshot der Detailansicht.</returns>
    /// <exception cref="InvalidOperationException">Kein lokaler Kontext für das Medium vorhanden.</exception>
    private async Task<MediaDetailsView> LoadOfflineAsync(
        string mediaType,
        long mediaId,
        string? fallbackTitle,
        long? showId,
        long? seasonId,
        long? episodeId,
        CancellationToken cancellationToken)
    {
        var type = MediaTypeNames.Normalize(mediaType);
        var manifest = await LoadManifestEntriesAsync();
        var entry = manifest.FirstOrDefault(e =>
            e.VideoId == (int)mediaId && MediaTypeNames.Normalize(e.VideoType) == type);

        switch (type)
        {
            case "movie":
                {
                    var title = entry?.Title is { Length: > 0 } manifestTitle
                        ? manifestTitle
                        : fallbackTitle ?? await FindCachedTitleAsync(mediaId) ?? "Medium";
                    return new MediaDetailsView
                    {
                        MediaType = "movie",
                        MediaId = mediaId,
                        Title = title,
                        IsFavorite = await IsCachedFavoriteAsync(type, mediaId),
                        HasLocalFile = HasLocalFile(entry),
                        IsOfflineSnapshot = true
                    };
                }

            case "tvshowepisode":
                {
                    var resolvedShowId = showId is > 0 ? showId : entry?.ShowId;
                    var show = resolvedShowId is > 0
                        ? await ShowDetailsCache.ReadAsync(cache, resolvedShowId.Value, cancellationToken)
                        : null;
                    if (show is not null)
                    {
                        return MapOfflineShow(
                            show, "tvshowepisode", mediaId,
                            manifest, seasonId ?? FindSeasonId(show, mediaId), mediaId,
                            await IsCachedFavoriteAsync(type, mediaId),
                            FindEpisodeTitle(show, mediaId));
                    }

                    // Ohne Serienkontext: Basisdetails aus Manifest/Auswahl.
                    var title = entry?.Title is { Length: > 0 } manifestTitle
                        ? manifestTitle
                        : fallbackTitle ?? await FindCachedTitleAsync(mediaId) ?? "Episode";
                    return new MediaDetailsView
                    {
                        MediaType = "tvshowepisode",
                        MediaId = mediaId,
                        Title = title,
                        IsFavorite = await IsCachedFavoriteAsync(type, mediaId),
                        HasLocalFile = HasLocalFile(entry),
                        IsOfflineSnapshot = true
                    };
                }

            case "tvshow":
                {
                    var show = await ShowDetailsCache.ReadAsync(cache, mediaId, cancellationToken)
                        ?? throw new InvalidOperationException("Für diese Serie liegen keine Offline-Daten vor.");
                    return MapOfflineShow(
                        show, "tvshow", mediaId,
                        manifest, seasonId, episodeId,
                        await IsCachedFavoriteAsync(type, mediaId));
                }

            case "tvshowseason":
                {
                    var resolvedShowId = showId is > 0
                        ? showId.Value
                        : throw new InvalidOperationException("Für diese Staffel ist keine Serie hinterlegt.");
                    var show = await ShowDetailsCache.ReadAsync(cache, resolvedShowId, cancellationToken)
                        ?? throw new InvalidOperationException("Für diese Staffel liegen keine Offline-Daten vor.");
                    return MapOfflineShow(
                        show, "tvshowseason", mediaId,
                        manifest, mediaId, episodeId,
                        await IsCachedFavoriteAsync(type, mediaId));
                }

            default:
                throw new InvalidOperationException($"Der Medientyp '{type}' ist offline nicht verfügbar.");
        }
    }

    /// <summary>
    /// Kollektion laden; antwortet der Server mit 404 (z. B. Einzelfilm
    /// ohne Kollektionshülle), wird die Film-Detailansicht geladen.
    /// </summary>
    /// <param name="client">Der angemeldete API-Client.</param>
    /// <param name="mediaId">Id der Kollektion bzw. des Films.</param>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Das Anzeigemodell der Kollektion bzw. des Films.</returns>
    private static async Task<MediaDetailsView> LoadCollectionOrMovieAsync(
        VideoPlayerApiClient client, long mediaId, CancellationToken cancellationToken)
    {
        try
        {
            return MapCollection(await client.GetMovieCollectionAsync(mediaId, cancellationToken));
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return MapMovie(await client.GetMovieAsync(mediaId, cancellationToken));
        }
    }

    /// <summary>
    /// Staffel-Aufruf: ohne Serien-Hint ist die Staffel nicht auflösbar
    /// (es existiert kein Staffel-Endpunkt) → Fehlerzustand.
    /// </summary>
    /// <param name="client">Der angemeldete API-Client.</param>
    /// <param name="seasonId">Id der angefragten Staffel.</param>
    /// <param name="showId">Hint: Id der zugehörigen Serie (erforderlich).</param>
    /// <param name="episodeId">Optionaler Hint: Id der hervorzuhebenden Episode.</param>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Serienansicht mit Vorwahl der Staffel.</returns>
    private async Task<MediaDetailsView> LoadSeasonInShowContextAsync(
        VideoPlayerApiClient client, long seasonId, long? showId, long? episodeId, CancellationToken cancellationToken)
    {
        if (showId is not > 0)
        {
            throw new InvalidOperationException("Für diese Staffel ist keine Serie hinterlegt.");
        }

        var show = await FetchShowAsync(client, showId.Value, cancellationToken);
        var season = show.Seasons.FirstOrDefault(s => s.Id == seasonId);
        return MapShow(show, "tvshowseason", seasonId, season?.IsFavorite, null, seasonId, episodeId);
    }

    /// <summary>
    /// Episoden-Aufruf: Serie über den <c>show</c>-Hint oder über den
    /// eingebetteten <c>Season.Show</c>-Bezug der Episoden-Antwort
    /// auflösen → Serienansicht mit Vorwahl; ohne Serie Basisdetails der
    /// Episode.
    /// </summary>
    /// <param name="client">Der angemeldete API-Client.</param>
    /// <param name="episodeId">Id der angefragten Episode.</param>
    /// <param name="fallbackTitle">Fallback-Titel, falls die Episode keinen Namen liefert.</param>
    /// <param name="showId">Optionaler Hint: Id der zugehörigen Serie.</param>
    /// <param name="seasonId">Optionaler Hint: Id der vorzuwählenden Staffel.</param>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Serienansicht mit Vorwahl bzw. Basisdetails der Episode.</returns>
    private async Task<MediaDetailsView> LoadEpisodeAsync(
        VideoPlayerApiClient client, long episodeId, string? fallbackTitle,
        long? showId, long? seasonId, CancellationToken cancellationToken)
    {
        TvShow? show = null;
        TvShowEpisode? episode = null;

        if (showId is > 0)
        {
            show = await FetchShowAsync(client, showId.Value, cancellationToken);
            episode = show.Seasons
                .SelectMany(s => s.Episodes)
                .FirstOrDefault(e => e.Id == episodeId);
        }

        if (episode is null)
        {
            episode = await client.GetTvShowEpisodeAsync(episodeId, cancellationToken);
            var embeddedShowId = episode.Season?.Show?.Id;
            if (show is null && embeddedShowId is > 0)
            {
                try
                {
                    show = await FetchShowAsync(client, embeddedShowId.Value, cancellationToken);
                }
                catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
                {
                    // Serie nicht ladbar → Basisdetails der Episode zeigen.
                    System.Diagnostics.Debug.WriteLine($"[MediaDetailsService] Serie {embeddedShowId} nicht ladbar: {ex.Message}");
                }
            }
        }

        if (show is not null)
        {
            var resolvedSeasonId = seasonId
                ?? show.Seasons.FirstOrDefault(s => s.Episodes.Any(e => e.Id == episodeId))?.Id
                ?? episode.Season?.Id;
            return MapShow(
                show, "tvshowepisode", episodeId, episode.IsFavorite,
                fallbackTitle, resolvedSeasonId, episodeId,
                string.IsNullOrWhiteSpace(episode.Name) ? null : episode.Name);
        }

        // Ohne Serienkontext: Basisdetails der Episode.
        return new MediaDetailsView
        {
            MediaType = "tvshowepisode",
            MediaId = episode.Id,
            Title = string.IsNullOrWhiteSpace(episode.Name) ? (fallbackTitle ?? string.Empty) : episode.Name,
            Plot = episode.Plot,
            ReleaseYear = episode.ReleaseDate?.Year,
            BannerPictureId = episode.GeneratedBackgroundPictureId ?? episode.BannerPictureId ?? episode.PosterPictureId,
            PosterPictureId = episode.PosterPictureId,
            IsFavorite = episode.IsFavorite
        };
    }

    /// <summary>
    /// Lädt eine Serie vom Server und legt ihre Struktur im
    /// <see cref="ShowDetailsCache"/> ab (Offline-Serienkontext für
    /// Platzhalter und Offline-Detailansicht).
    /// </summary>
    /// <param name="client">Der angemeldete API-Client.</param>
    /// <param name="showId">Id der Serie.</param>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Die geladene Serie.</returns>
    private async Task<TvShow> FetchShowAsync(VideoPlayerApiClient client, long showId, CancellationToken cancellationToken)
    {
        var show = await client.GetTvShowAsync(showId, cancellationToken);
        await ShowDetailsCache.WriteAsync(cache, show, cancellationToken);
        return show;
    }

    /// <summary>
    /// Liest das Download-Manifest; Fehler liefern eine leere Liste
    /// (beschädigte lokale Daten führen nicht zum Absturz).
    /// </summary>
    /// <returns>Alle Manifesteinträge.</returns>
    private async Task<IReadOnlyList<DownloadManifestEntry>> LoadManifestEntriesAsync()
    {
        try
        {
            return await cache.Connection.Table<DownloadManifestEntry>().ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MediaDetailsService] Manifest nicht lesbar: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Prüft, ob das Medium in der lokalen Favoriten-Kopie
    /// (<c>cached_favorites</c>) geführt wird.
    /// </summary>
    /// <param name="mediaType">Normalisierter Medientyp.</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <returns><c>true</c>, wenn das Medium als Favorit markiert ist.</returns>
    private async Task<bool> IsCachedFavoriteAsync(string mediaType, long mediaId)
    {
        try
        {
            var id = (int)mediaId;
            var rows = await cache.Connection.Table<CachedFavoritesEntry>()
                .Where(e => e.VideoId == id)
                .ToListAsync();
            return rows.Any(r => string.Equals(
                MediaTypeNames.Normalize(r.VideoType), mediaType, StringComparison.Ordinal));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MediaDetailsService] Favoriten-Kopie nicht lesbar: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sucht den Anzeigetitel eines Mediums in der Favoriten-Kopie als
    /// letzte Titelquelle ohne Manifest.
    /// </summary>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <returns>Der gefundene Titel; <c>null</c>, wenn keiner bekannt ist.</returns>
    private async Task<string?> FindCachedTitleAsync(long mediaId)
    {
        try
        {
            var id = (int)mediaId;
            var rows = await cache.Connection.Table<CachedFavoritesEntry>()
                .Where(e => e.VideoId == id)
                .ToListAsync();
            foreach (var row in rows)
            {
                try
                {
                    var favorite = System.Text.Json.JsonSerializer.Deserialize<FavoriteEntry>(row.Json);
                    if (favorite?.Entry?.Name is { Length: > 0 } name)
                    {
                        return name;
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    // Beschädigte Zeile → nächste prüfen.
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MediaDetailsService] Titelsuche fehlgeschlagen: {ex.Message}");
        }

        return null;
    }

    /// <summary>Ob zum Manifesteintrag eine abgeschlossene lokale Datei vorliegt.</summary>
    /// <param name="entry">Der Manifesteintrag; <c>null</c> = kein Download.</param>
    /// <returns><c>true</c>, wenn der Download abgeschlossen ist und die Datei existiert.</returns>
    private bool HasLocalFile(DownloadManifestEntry? entry)
    {
        return entry is not null
            && DownloadStateNames.FromPersisted(entry.State) == DownloadState.Completed
            && entry.FilePath is { Length: > 0 }
            && fileExists(entry.FilePath);
    }

    /// <summary>
    /// Baut die Serienansicht aus der gecachten Struktur: jede Episode
    /// trägt ihren lokalen Datei-Stand (<see cref="EpisodeItem.HasLocalFile"/>)
    /// bzw. die Platzhalter-Markierung (<see cref="EpisodeItem.IsPlaceholder"/>).
    /// </summary>
    /// <param name="show">Die gecachte Serienstruktur.</param>
    /// <param name="mediaType">Medientyp der navigierten Identität.</param>
    /// <param name="mediaId">Id der navigierten Identität.</param>
    /// <param name="manifest">Das Download-Manifest (für Datei-Stände).</param>
    /// <param name="selectedSeasonId">Vorzuwählende Staffel; <c>null</c> = erste.</param>
    /// <param name="highlightedEpisodeId">Hervorzuhebende Episode; <c>null</c> = keine.</param>
    /// <param name="isFavorite">Favoriten-Stand aus der lokalen Kopie.</param>
    /// <param name="highlightedEpisodeTitle">Titel der navigierten Episode; <c>null</c> = kein Untertitel.</param>
    /// <returns>Der Offline-Snapshot der Serienansicht.</returns>
    private MediaDetailsView MapOfflineShow(
        CachedShowEntry show,
        string mediaType,
        long mediaId,
        IReadOnlyList<DownloadManifestEntry> manifest,
        long? selectedSeasonId,
        long? highlightedEpisodeId,
        bool isFavorite,
        string? highlightedEpisodeTitle = null)
    {
        var completedIds = manifest
            .Where(e => MediaTypeNames.Normalize(e.VideoType) == "tvshowepisode" && HasLocalFile(e))
            .Select(e => (long)e.VideoId)
            .ToHashSet();

        var seasons = show.Seasons
            .OrderBy(s => s.Number)
            .Select(s => new SeasonItem
            {
                Id = s.Id,
                Number = s.Number,
                Name = $"Staffel {s.Number}",
                Episodes = s.Episodes
                    .OrderBy(e => e.Number)
                    .Select(e => new EpisodeItem
                    {
                        Id = e.Id,
                        Number = e.Number,
                        Title = string.IsNullOrWhiteSpace(e.Title) ? $"Episode {e.Number}" : e.Title,
                        IsHighlighted = e.Id == highlightedEpisodeId,
                        HasLocalFile = completedIds.Contains(e.Id),
                        IsPlaceholder = !completedIds.Contains(e.Id)
                    })
                    .ToList()
            })
            .ToList();

        var selected = selectedSeasonId
            ?? seasons.FirstOrDefault(s => s.Episodes.Any(e => e.Id == highlightedEpisodeId))?.Id;

        var mediumEntry = manifest.FirstOrDefault(e =>
            e.VideoId == (int)mediaId && MediaTypeNames.Normalize(e.VideoType) == MediaTypeNames.Normalize(mediaType));

        return new MediaDetailsView
        {
            MediaType = mediaType,
            MediaId = mediaId,
            Title = show.Title,
            IsFavorite = isFavorite,
            ShowId = show.ShowId,
            Seasons = seasons,
            SelectedSeasonId = selected,
            HighlightedEpisodeId = highlightedEpisodeId,
            HighlightedEpisodeTitle = highlightedEpisodeTitle,
            HasLocalFile = HasLocalFile(mediumEntry),
            IsOfflineSnapshot = true
        };
    }

    /// <summary>Ermittelt die Staffel-Id einer Episode in der gecachten Struktur.</summary>
    /// <param name="show">Die gecachte Serie.</param>
    /// <param name="episodeId">Id der Episode.</param>
    /// <returns>Die Staffel-Id; <c>null</c>, wenn die Episode unbekannt ist.</returns>
    private static long? FindSeasonId(CachedShowEntry show, long episodeId)
    {
        return show.Seasons.FirstOrDefault(s => s.Episodes.Any(e => e.Id == episodeId))?.Id;
    }

    /// <summary>Ermittelt den Titel einer Episode in der gecachten Struktur.</summary>
    /// <param name="show">Die gecachte Serie.</param>
    /// <param name="episodeId">Id der Episode.</param>
    /// <returns>Der Episodentitel; <c>null</c>, wenn die Episode unbekannt ist.</returns>
    private static string? FindEpisodeTitle(CachedShowEntry show, long episodeId)
    {
        var episode = show.Seasons.SelectMany(s => s.Episodes).FirstOrDefault(e => e.Id == episodeId);
        return episode is null ? null
            : string.IsNullOrWhiteSpace(episode.Title) ? $"Episode {episode.Number}" : episode.Title;
    }

    private static MediaDetailsView MapMovie(Movie movie)
    {
        return new MediaDetailsView
        {
            MediaType = "movie",
            MediaId = movie.Id,
            Title = movie.Name,
            Plot = movie.Plot,
            GenreNames = movie.GenreNames,
            ReleaseYear = movie.ReleaseDate?.Year,
            BannerPictureId = movie.BannerPictureId ?? movie.FanartPictureId ?? movie.PosterPictureId,
            PosterPictureId = movie.PosterPictureId,
            IsFavorite = movie.IsFavorite
        };
    }

    private static MediaDetailsView MapCollection(MovieCollection collection)
    {
        var movies = collection.Movies
            .OrderBy(m => m.ReleaseDate)
            .Select(m => new CollectionMovieItem
            {
                Id = m.Id,
                Title = m.Name,
                Plot = m.Plot,
                GenreNames = m.GenreNames,
                ReleaseYear = m.ReleaseDate?.Year,
                PosterPictureId = m.PosterPictureId,
                IsFavorite = m.IsFavorite
            })
            .ToList();

        return new MediaDetailsView
        {
            MediaType = "moviecollection",
            MediaId = collection.Id,
            Title = collection.Name,
            ReleaseYear = collection.ReleaseDate?.Year,
            BannerPictureId = collection.BannerPictureId ?? collection.FanartPictureId ?? collection.PosterPictureId,
            PosterPictureId = collection.PosterPictureId,
            IsFavorite = collection.IsFavorite,
            Movies = movies
        };
    }

    private static MediaDetailsView MapShow(
        TvShow show,
        string mediaType,
        long mediaId,
        bool? favoriteOverride,
        string? fallbackTitle,
        long? selectedSeasonId,
        long? highlightedEpisodeId,
        string? highlightedEpisodeTitle = null)
    {
        var seasons = MapSeasons(show.Seasons, highlightedEpisodeId);
        var selected = selectedSeasonId
            ?? seasons.FirstOrDefault(s => s.Episodes.Any(e => e.Id == highlightedEpisodeId))?.Id;

        return new MediaDetailsView
        {
            MediaType = mediaType,
            MediaId = mediaId,
            Title = string.IsNullOrWhiteSpace(show.Name) ? (fallbackTitle ?? string.Empty) : show.Name,
            Plot = show.Plot,
            GenreNames = show.GenreNames,
            ReleaseYear = show.ReleaseDate?.Year ?? show.PremieredAt?.Year,
            BannerPictureId = show.BannerPictureId ?? show.FanartPictureId ?? show.PosterPictureId,
            PosterPictureId = show.PosterPictureId,
            IsFavorite = favoriteOverride ?? show.IsFavorite,
            ShowId = show.Id,
            Seasons = seasons,
            SelectedSeasonId = selected,
            HighlightedEpisodeId = highlightedEpisodeId,
            HighlightedEpisodeTitle = highlightedEpisodeTitle
        };
    }

    private static IReadOnlyList<SeasonItem> MapSeasons(TvShowSeason[] seasons, long? highlightedEpisodeId)
    {
        return seasons
            .OrderBy(s => s.Number)
            .Select(s => new SeasonItem
            {
                Id = s.Id,
                Number = s.Number,
                Name = string.IsNullOrWhiteSpace(s.Name) ? $"Staffel {s.Number}" : s.Name,
                Episodes = s.Episodes
                    .OrderBy(e => e.Number)
                    .Select(e => new EpisodeItem
                    {
                        Id = e.Id,
                        Number = e.Number,
                        Title = string.IsNullOrWhiteSpace(e.Name) ? $"Episode {e.Number}" : e.Name,
                        Plot = e.Plot,
                        PosterPictureId = e.GeneratedBackgroundPictureId ?? e.PosterPictureId,
                        IsHighlighted = e.Id == highlightedEpisodeId
                    })
                    .ToList()
            })
            .ToList();
    }
}
