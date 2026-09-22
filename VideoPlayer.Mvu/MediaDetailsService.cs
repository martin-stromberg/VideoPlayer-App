using System.Net;
using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Models;

namespace VideoPlayer.Mvu;

/// <summary>
/// Lädt die Detaildaten eines Mediums über den angemeldeten
/// <see cref="AuthSessionService.Client"/> und mappt sie auf das
/// plattformneutrale <see cref="MediaDetailsView"/>. Serien liefern
/// Staffeln und Episoden eingebettet; Episoden- und Staffel-Aufrufe
/// werden — soweit über <c>Season.Show</c> bzw. die Routen-Hints
/// auflösbar — in den Serienkontext mit Vorwahl gebracht (Muster des
/// Altbestands <c>TVShowDetailsPage(tvShowId, seasonId, episodeId)</c>).
/// </summary>
public sealed class MediaDetailsService
{
    private readonly AuthSessionService authSession;

    public MediaDetailsService(AuthSessionService authSession)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
    }

    /// <summary>
    /// Lädt die Detailansicht zum navigierten Medium.
    /// <paramref name="showId"/>/<paramref name="seasonId"/>/
    /// <paramref name="episodeId"/> sind optionale Hints aus der
    /// Listen-Auswahl (eingebetteter Objektgraph).
    /// </summary>
    /// <exception cref="InvalidOperationException">Kein Server verbunden oder Medientyp/Identität nicht auflösbar.</exception>
    public async Task<MediaDetailsView> LoadAsync(
        string mediaType,
        long mediaId,
        string? fallbackTitle,
        long? showId,
        long? seasonId,
        long? episodeId,
        CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        return MediaTypeNames.Normalize(mediaType) switch
        {
            "movie" => MapMovie(await client.GetMovieAsync(mediaId, cancellationToken)),
            "moviecollection" => await LoadCollectionOrMovieAsync(client, mediaId, cancellationToken),
            "tvshow" => MapShow(
                await client.GetTvShowAsync(mediaId, cancellationToken),
                "tvshow", mediaId, favoriteOverride: null, fallbackTitle, seasonId, episodeId),
            "tvshowseason" => await LoadSeasonInShowContextAsync(client, mediaId, showId, episodeId, cancellationToken),
            "tvshowepisode" => await LoadEpisodeAsync(client, mediaId, fallbackTitle, showId, seasonId, cancellationToken),
            var other => throw new InvalidOperationException($"Der Medientyp '{other}' wird nicht unterstützt.")
        };
    }

    /// <summary>
    /// Kollektion laden; antwortet der Server mit 404 (z. B. Einzelfilm
    /// ohne Kollektionshülle), wird die Film-Detailansicht geladen.
    /// </summary>
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
    private static async Task<MediaDetailsView> LoadSeasonInShowContextAsync(
        VideoPlayerApiClient client, long seasonId, long? showId, long? episodeId, CancellationToken cancellationToken)
    {
        if (showId is not > 0)
        {
            throw new InvalidOperationException("Für diese Staffel ist keine Serie hinterlegt.");
        }

        var show = await client.GetTvShowAsync(showId.Value, cancellationToken);
        var season = show.Seasons.FirstOrDefault(s => s.Id == seasonId);
        return MapShow(show, "tvshowseason", seasonId, season?.IsFavorite, null, seasonId, episodeId);
    }

    /// <summary>
    /// Episoden-Aufruf: Serie über den <c>show</c>-Hint oder über den
    /// eingebetteten <c>Season.Show</c>-Bezug der Episoden-Antwort
    /// auflösen → Serienansicht mit Vorwahl; ohne Serie Basisdetails der
    /// Episode.
    /// </summary>
    private static async Task<MediaDetailsView> LoadEpisodeAsync(
        VideoPlayerApiClient client, long episodeId, string? fallbackTitle,
        long? showId, long? seasonId, CancellationToken cancellationToken)
    {
        TvShow? show = null;
        TvShowEpisode? episode = null;

        if (showId is > 0)
        {
            show = await client.GetTvShowAsync(showId.Value, cancellationToken);
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
                    show = await client.GetTvShowAsync(embeddedShowId.Value, cancellationToken);
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
