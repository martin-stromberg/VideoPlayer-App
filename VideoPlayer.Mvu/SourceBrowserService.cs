using VideoPlayer.ApiClient.Models;

namespace VideoPlayer.Mvu;

/// <summary>Ergebnis einer geladenen Eintragsseite des Quellen-Browsers.</summary>
/// <param name="Page">Geladene Seite (0-basiert).</param>
/// <param name="Items">Die Einträge der Seite.</param>
/// <param name="HasMore"><c>true</c>, solange weitere Seiten erwartet werden.</param>
/// <returns>Ergebnis-Datensatz der Eintragsseite.</returns>
public sealed record SourceItemsPage(
    int Page,
    IReadOnlyList<SourceMediaItem> Items,
    bool HasMore);

/// <summary>
/// Lädt die Quellen-Browsing-Daten vom Server über den angemeldeten
/// <see cref="AuthSessionService.Client"/>: freigegebene Quellen
/// (<c>api/Sources</c>), Genres einer Quelle (<c>api/SourceGenres/{id}</c>,
/// mit vorangestelltem „Alle"-Eintrag) und paginierte Medieneinträge
/// (<c>api/items</c> mit Suche/Genre-Filter). Ergebnisse werden auf die
/// plattformneutralen Anzeige-Records gemappt.
/// </summary>
public sealed class SourceBrowserService
{
    /// <summary>Anzeigename des Filter-Eintrags ohne Genre-Einschränkung.</summary>
    public const string AllGenresName = "Alle";

    private readonly AuthSessionService authSession;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client.</param>
    public SourceBrowserService(AuthSessionService authSession)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
    }

    /// <summary>Lädt die freigegebenen Quellen (entspricht den Daten der Server-API).</summary>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Die freigegebenen Quellen.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task<IReadOnlyList<SourceItem>> LoadSourcesAsync(CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        var sources = await client.GetSourcesAsync(cancellationToken);
        var items = new List<SourceItem>(sources.Count);
        foreach (var source in sources)
        {
            if (source.Id <= 0)
            {
                continue;
            }

            items.Add(new SourceItem
            {
                Id = source.Id,
                Name = source.Name,
                IconPictureId = source.IconPictureId
            });
        }

        return items;
    }

    /// <summary>
    /// Lädt die Genres einer Quelle; dem Ergebnis wird der Eintrag
    /// „Alle" (<see cref="SourceGenreItem.Id"/> = 0 = kein Filter)
    /// vorangestellt — Muster des Altbestands.
    /// </summary>
    /// <param name="sourceId">Id der Quelle, deren Genres geladen werden.</param>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Die Genres der Quelle mit vorangestelltem „Alle"-Eintrag.</returns>
    public async Task<IReadOnlyList<SourceGenreItem>> LoadGenresAsync(long sourceId, CancellationToken cancellationToken = default)
    {
        var genres = new List<SourceGenreItem>
        {
            new() { Id = 0, Name = AllGenresName, IsSelected = true }
        };

        var client = authSession.Client;
        if (client is null)
        {
            return genres;
        }

        var sourceGenres = await client.GetSourceGenresAsync(sourceId, cancellationToken);
        foreach (var genre in sourceGenres.Genres)
        {
            if (genre.Id <= 0 || string.IsNullOrWhiteSpace(genre.Name))
            {
                continue;
            }

            genres.Add(new SourceGenreItem { Id = genre.Id, Name = genre.Name });
        }

        return genres;
    }

    /// <summary>
    /// Lädt eine Eintragsseite der Quelle (<c>api/items</c>);
    /// <paramref name="genreId"/> 0/«null» = kein Filter,
    /// <paramref name="searchText"/> leer = keine Suche.
    /// <see cref="SourceItemsPage.HasMore"/> ist wahr, solange die Seite
    /// voll gefüllt zurückkommt.
    /// </summary>
    /// <param name="sourceId">Id der Quelle.</param>
    /// <param name="page">Zu ladende Seite (0-basiert).</param>
    /// <param name="searchText">Suchtext; <c>null</c>/leer = keine Suche.</param>
    /// <param name="genreId">Genre-Filter; <c>null</c>/0 = kein Filter.</param>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Die geladene Eintragsseite.</returns>
    /// <exception cref="InvalidOperationException">Kein Server verbunden.</exception>
    public async Task<SourceItemsPage> LoadItemsAsync(
        long sourceId,
        int page,
        string? searchText,
        long? genreId,
        CancellationToken cancellationToken = default)
    {
        var client = authSession.Client
            ?? throw new InvalidOperationException("Kein Server verbunden.");

        var summaries = await client.GetSourceItemsAsync(
            sourceId, page, SourceBrowserState.PageSize, searchText, genreId, cancellationToken);

        var items = new List<SourceMediaItem>(summaries.Count);
        foreach (var summary in summaries)
        {
            // Einträge ohne identifizierbares Medium werden übersprungen —
            // die Auswahl muss das Medium eindeutig bestimmen können.
            if (summary.Id <= 0 || string.IsNullOrWhiteSpace(summary.Type))
            {
                continue;
            }

            items.Add(new SourceMediaItem
            {
                Id = summary.Id,
                Title = summary.Title,
                MediaType = MapSummaryType(summary.Type),
                PosterPictureId = summary.PictureId
            });
        }

        return new SourceItemsPage(page, items, summaries.Count >= SourceBrowserState.PageSize);
    }

    /// <summary>
    /// Typ-Mapping der Quelleneinträge — wie der Altbestand führen
    /// „Film"-Einträge der Quellenliste zur Kollektionsansicht (der Server
    /// gruppiert Filme in Kollektionen), alles Übrige zur Serienansicht.
    /// </summary>
    /// <param name="type">Typbezeichner der Server-Summary.</param>
    /// <returns>Der normalisierte Medientyp in CLR-Konvention.</returns>
    internal static string MapSummaryType(string type)
    {
        return type.Trim().ToLowerInvariant() switch
        {
            "movie" or "moviecollection" or "collection" => "moviecollection",
            "tvshow" or "show" => "tvshow",
            "tvshowseason" or "season" => "tvshowseason",
            "tvshowepisode" or "episode" => "tvshowepisode",
            _ => "tvshow"
        };
    }
}
