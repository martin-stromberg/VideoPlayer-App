using System.Text.Json;
using Microsoft.Extensions.Logging;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Mvu;

/// <summary>
/// Gecachter Episoden-Eintrag einer Serie (Offline-Serienkontext).
/// </summary>
public sealed record CachedShowEpisode
{
    /// <summary>Id der Episode.</summary>
    public required long Id { get; init; }

    /// <summary>Episodennummer innerhalb der Staffel.</summary>
    public required int Number { get; init; }

    /// <summary>Anzeigetitel der Episode.</summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gesehen-Zeitpunkt der Episode beim Schreiben des Cache-Eintrags;
    /// <c>null</c> = ungesehen bzw. aus einem älteren Cache-Stand nicht
    /// bekannt (abwärtskompatibel).
    /// </summary>
    public DateTime? WatchedAt { get; init; }
}

/// <summary>
/// Gecachte Staffel einer Serie (Offline-Serienkontext).
/// </summary>
public sealed record CachedShowSeason
{
    /// <summary>Id der Staffel.</summary>
    public required long Id { get; init; }

    /// <summary>Staffelnummer.</summary>
    public required int Number { get; init; }

    /// <summary>Episoden der Staffel.</summary>
    public IReadOnlyList<CachedShowEpisode> Episodes { get; init; } = [];
}

/// <summary>
/// Verweis auf eine Episode in der geordneten Serienfolge
/// (Staffelnummer + gecachter Episoden-Eintrag).
/// </summary>
/// <param name="SeasonNumber">Staffelnummer der Episode.</param>
/// <param name="Episode">Der gecachte Episoden-Eintrag.</param>
/// <returns>Positionsverweis innerhalb der Serienfolge.</returns>
public sealed record ShowEpisodeRef(int SeasonNumber, CachedShowEpisode Episode);

/// <summary>
/// Gecachte Serienstruktur (Offline-Serienkontext): Staffeln und
/// Episoden einer Serie, geschrieben beim Online-Laden der Detailseite
/// bzw. des Auto-Download-Vorlaufs. Grundlage der Offline-Platzhalter
/// fehlender Folge-Episoden und der Offline-Detailseite.
/// </summary>
public sealed record CachedShowEntry
{
    /// <summary>Id der Serie.</summary>
    public required long ShowId { get; init; }

    /// <summary>Anzeigetitel der Serie.</summary>
    public required string Title { get; init; }

    /// <summary>Staffeln der Serie.</summary>
    public IReadOnlyList<CachedShowSeason> Seasons { get; init; } = [];
}

/// <summary>
/// Lese-/Schreibzugang der gecachten Serienstruktur in
/// <c>cached_lists</c> (Schlüssel <c>show:{id}</c>). Die Ablage ist ein
/// nicht backup-pflichtiger Cache — bei einer Wiederherstellung wird sie
/// beim nächsten Online-Laden erneut geschrieben. Lesefehler liefern
/// <c>null</c> statt eine Ausnahme (beschädigte Einträge führen nicht
/// zum Absturz).
/// </summary>
public static class ShowDetailsCache
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Cache-Schlüssel einer Serie.</summary>
    /// <param name="showId">Id der Serie.</param>
    /// <returns>Der <c>cached_lists</c>-Schlüssel im Format <c>show:{id}</c>.</returns>
    public static string KeyFor(long showId) => $"show:{showId}";

    /// <summary>
    /// Schreibt die Serienstruktur (Staffeln + Episoden, jeweils nach
    /// Nummer geordnet) in den Cache. Schreibfehler werden protokolliert
    /// und übergangen — der Cache ist eine Komfortfunktion, kein
    /// Pflichtbestand.
    /// </summary>
    /// <param name="cache">Die Cache-Ablage.</param>
    /// <param name="show">Die geladene Serie (Serverantwort).</param>
    /// <param name="logger">Optionaler Logger für das Programmprotokoll (Fehlerpfad).</param>
    /// <param name="cancellationToken">Bricht das Schreiben ab.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Schreiben.</returns>
    public static async Task WriteAsync(
        CacheDatabase cache,
        TvShow show,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(show);
        if (show.Id <= 0)
        {
            return;
        }

        var entry = new CachedShowEntry
        {
            ShowId = show.Id,
            Title = show.Name ?? string.Empty,
            Seasons = show.Seasons
                .OrderBy(s => s.Number)
                .Select(s => new CachedShowSeason
                {
                    Id = s.Id,
                    Number = s.Number,
                    Episodes = s.Episodes
                        .OrderBy(e => e.Number)
                        .Select(e => new CachedShowEpisode
                        {
                            Id = e.Id,
                            Number = e.Number,
                            Title = e.Name ?? string.Empty,
                            WatchedAt = e.WatchedAt
                        })
                        .ToList()
                })
                .ToList()
        };

        try
        {
            var row = new CachedListEntry
            {
                Key = KeyFor(show.Id),
                Json = JsonSerializer.Serialize(entry),
                UpdatedAtUtc = DateTime.UtcNow
            };
            await cache.Connection.InsertOrReplaceAsync(row);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Schreiben des Serien-Caches '{CacheKey}' fehlgeschlagen.", KeyFor(show.Id));
        }
    }

    /// <summary>
    /// Liest die gecachte Serienstruktur. Fehlende oder beschädigte
    /// Einträge liefern <c>null</c>.
    /// </summary>
    /// <param name="cache">Die Cache-Ablage.</param>
    /// <param name="showId">Id der Serie.</param>
    /// <param name="logger">Optionaler Logger für das Programmprotokoll (Fehlerpfad).</param>
    /// <param name="cancellationToken">Bricht das Lesen ab.</param>
    /// <returns>Die gecachte Serienstruktur; <c>null</c> bei Fehlen oder Lesefehler.</returns>
    public static async Task<CachedShowEntry?> ReadAsync(
        CacheDatabase cache,
        long showId,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);
        // Den Schlüssel vorab berechnen: sqlite-net kann den Methodenaufruf
        // nicht in SQL übersetzen („no such function: keyfor").
        var key = KeyFor(showId);
        try
        {
            var row = await cache.Connection.Table<CachedListEntry>()
                .Where(e => e.Key == key)
                .FirstOrDefaultAsync();
            if (row?.Json is not { Length: > 0 } json)
            {
                return null;
            }

            var entry = JsonSerializer.Deserialize<CachedShowEntry>(json, JsonOptions);
            return entry?.ShowId > 0 ? entry : null;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Lesen des Serien-Caches '{CacheKey}' fehlgeschlagen.", KeyFor(showId));
            return null;
        }
    }

    /// <summary>
    /// Alle Episoden der Serie staffelübergreifend geordnet (Staffel-,
    /// dann Episodennummer) — dieselbe Reihenfolge wie die
    /// Fortsetzungslogik der Detailseite und des Auto-Downloads.
    /// </summary>
    /// <param name="show">Die gecachte Serie.</param>
    /// <returns>Die geordnete Episodenfolge mit Staffelnummer.</returns>
    public static IReadOnlyList<ShowEpisodeRef> OrderedEpisodes(CachedShowEntry show)
    {
        ArgumentNullException.ThrowIfNull(show);
        return show.Seasons
            .OrderBy(s => s.Number)
            .SelectMany(s => s.Episodes.OrderBy(e => e.Number).Select(e => new ShowEpisodeRef(s.Number, e)))
            .ToList();
    }

    /// <summary>
    /// Bestimmt die Folge-Episode einer Position in der Serienfolge.
    /// </summary>
    /// <param name="show">Die gecachte Serie.</param>
    /// <param name="seasonNumber">Staffelnummer der Ausgangsepisode.</param>
    /// <param name="episodeNumber">Episodennummer der Ausgangsepisode.</param>
    /// <returns>Die nächste Episode samt Staffelnummer; <c>null</c>, wenn die Serie an der Position endet oder die Ausgangsepisode unbekannt ist.</returns>
    public static ShowEpisodeRef? NextEpisodeAfter(
        CachedShowEntry show, int seasonNumber, int episodeNumber)
    {
        var episodes = OrderedEpisodes(show);
        var index = IndexOfEpisode(episodes, e => e.SeasonNumber == seasonNumber && e.Episode.Number == episodeNumber);
        return index >= 0 && index + 1 < episodes.Count ? episodes[index + 1] : null;
    }

    /// <summary>
    /// Bestimmt die Folge-Episode einer Episoden-Id in der Serienfolge.
    /// </summary>
    /// <param name="show">Die gecachte Serie.</param>
    /// <param name="episodeId">Id der Ausgangsepisode.</param>
    /// <returns>Die nächste Episode samt Staffelnummer; <c>null</c>, wenn die Serie an der Position endet oder die Ausgangsepisode unbekannt ist.</returns>
    public static ShowEpisodeRef? NextEpisodeAfterId(
        CachedShowEntry show, long episodeId)
    {
        var episodes = OrderedEpisodes(show);
        var index = IndexOfEpisode(episodes, e => e.Episode.Id == episodeId);
        return index >= 0 && index + 1 < episodes.Count ? episodes[index + 1] : null;
    }

    /// <summary>Index des ersten geordneten Episoden-Eintrags, der das Prädikat erfüllt; -1 = nicht gefunden.</summary>
    /// <param name="episodes">Die geordnete Episodenfolge.</param>
    /// <param name="predicate">Das Suchprädikat.</param>
    /// <returns>Der 0-basierte Index; -1, wenn kein Eintrag passt.</returns>
    private static int IndexOfEpisode(
        IReadOnlyList<ShowEpisodeRef> episodes,
        Func<ShowEpisodeRef, bool> predicate)
    {
        for (var i = 0; i < episodes.Count; i++)
        {
            if (predicate(episodes[i]))
            {
                return i;
            }
        }

        return -1;
    }
}
