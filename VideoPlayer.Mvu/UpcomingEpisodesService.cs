using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>
/// Ermittelt die Folge-Episoden eines Mediums aus dem Serienkontext —
/// unabhängig von der Detailseite. Grundlage der Folgevideo-/
/// Auto-Fortsetzungs-Funktion beim Direktstart aus der
/// Weiterschauen-Liste: online wird die Serie über
/// <c>api/items/tvshow/{showId}</c> geladen und die Struktur im
/// <see cref="ShowDetailsCache"/> abgelegt; ohne angemeldeten Client oder
/// nach einem Ladefehlschlag greift der Dienst auf die gecachte
/// Serienstruktur zurück. Die geordnete Episodenfolge entspricht der
/// Fortsetzungslogik der Detailseite (<see cref="AppUpdate"/>) und des
/// Auto-Download-Vorlaufs (<see cref="AutoDownloadService"/>).
/// </summary>
public sealed class UpcomingEpisodesService
{
    private readonly AuthSessionService authSession;
    private readonly CacheDatabase cache;
    private readonly ILogger<UpcomingEpisodesService> logger;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client.</param>
    /// <param name="cache">Cache-Ablage (gecachte Serienstruktur).</param>
    /// <param name="logger">Optionaler Logger für das Programmprotokoll.</param>
    public UpcomingEpisodesService(
        AuthSessionService authSession,
        CacheDatabase cache,
        ILogger<UpcomingEpisodesService>? logger = null)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.logger = logger ?? NullLogger<UpcomingEpisodesService>.Instance;
    }

    /// <summary>
    /// Lädt die Folge-Episoden der gestarteten Episode: alle Episoden
    /// nach <paramref name="mediaId"/>, staffelübergreifend nach
    /// Staffel-/Episodennummer sortiert. Ohne auffindbaren Serienkontext
    /// oder nicht enthaltener Episode bleibt die Liste leer.
    /// </summary>
    /// <param name="mediaId">Id der gestarteten Episode.</param>
    /// <param name="showId">Id der zugehörigen Serie.</param>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Die geordnete Fortsetzungsliste (leer ohne Folge-Episoden).</returns>
    public async Task<IReadOnlyList<PlayerEpisodeRef>> LoadAsync(
        long mediaId,
        long showId,
        CancellationToken cancellationToken = default)
    {
        var episodes = await LoadOrderedEpisodesAsync(showId, cancellationToken);
        var index = episodes.FindIndex(e => e.MediaId == mediaId);
        return index < 0 ? [] : episodes.Skip(index + 1).ToList();
    }

    /// <summary>
    /// Liefert die vollständige, staffelübergreifend geordnete
    /// Episodenfolge der Serie — online vom Server (mit
    /// Cache-Auffrischung), sonst aus dem <see cref="ShowDetailsCache"/>.
    /// </summary>
    /// <param name="showId">Id der Serie.</param>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Die geordnete Episodenfolge (leer ohne Serienkontext).</returns>
    private async Task<List<PlayerEpisodeRef>> LoadOrderedEpisodesAsync(
        long showId,
        CancellationToken cancellationToken)
    {
        var client = authSession.Client;
        if (client is not null)
        {
            try
            {
                var show = await client.GetTvShowAsync(showId, cancellationToken);
                await ShowDetailsCache.WriteAsync(cache, show, logger, cancellationToken);
                return show.Seasons
                    .OrderBy(s => s.Number)
                    .SelectMany(s => s.Episodes.OrderBy(e => e.Number))
                    .Select(e => new PlayerEpisodeRef { MediaId = e.Id, Title = e.Name })
                    .ToList();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Online-Ladung fehlgeschlagen → auf die gecachte
                // Serienstruktur zurückfallen (Offline-/Fehler-Fallback).
                logger.LogWarning(ex, "Serie {ShowId} nicht ladbar.", showId);
            }
        }

        var cached = await ShowDetailsCache.ReadAsync(cache, showId, logger, cancellationToken);
        return cached is null
            ? []
            : ShowDetailsCache.OrderedEpisodes(cached)
                .Select(e => new PlayerEpisodeRef { MediaId = e.Episode.Id, Title = e.Episode.Title })
                .ToList();
    }
}
