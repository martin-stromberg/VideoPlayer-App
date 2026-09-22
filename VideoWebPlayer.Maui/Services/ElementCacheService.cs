using VideoWebPlayer.Maui.Models;
using VideoWebPlayer.Maui.ViewModels;

namespace VideoWebPlayer.Maui.Services;

/// <summary>
/// Provides a persistent cache for MediaCarouselViewModel items.
/// Carousel items are stored in the local database so they can be displayed
/// immediately on startup before the server responds.
/// </summary>
public class ElementCacheService
{
    private static ElementCacheService? _instance;

    /// <summary>
    /// Ruft die gemeinsame Singleton-Instanz des Cache-Dienstes ab.
    /// </summary>
    /// <value>Die Singleton-Instanz des <see cref="ElementCacheService"/>.</value>
    public static ElementCacheService Instance => _instance ??= new ElementCacheService();

    private readonly ClientDatabase _db = ClientDatabase.Instance;

    /// <summary>
    /// Returns all cached items for the given carousel, ordered by their stored position.
    /// </summary>
    /// <param name="carouselName">Der Name des Carousels, dessen Einträge geladen werden.</param>
    /// <returns>Liste der gecachten <see cref="CachedCarouselItem"/>-Einträge.</returns>
    public async Task<List<CachedCarouselItem>> GetCachedItemsAsync(string carouselName)
    {
        await _db.Lock.WaitAsync();
        try
        {
            return await _db.Database.Table<CachedCarouselItem>()
                .Where(c => c.CarouselName == carouselName)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();
        }
        finally
        {
            _db.Lock.Release();
        }
    }

    /// <summary>
    /// Replaces all cached items for the given carousel with the supplied items.
    /// </summary>
    /// <param name="carouselName">Der Name des Carousels, dessen Einträge ersetzt werden.</param>
    /// <param name="items">Die zu speichernden Einträge.</param>
    /// <returns>Ein Task, der das Speichern abschließt.</returns>
    public async Task SaveCachedItemsAsync(string carouselName, IEnumerable<MediaItemViewModel> items)
    {
        await _db.Lock.WaitAsync();
        try
        {
            await _db.Database.ExecuteAsync(
                "DELETE FROM CarouselCache WHERE CarouselName = ?", carouselName);

            int order = 0;
            foreach (var item in items)
            {
                var cached = new CachedCarouselItem
                {
                    CarouselName = carouselName,
                    SortOrder = order++,
                    EntryId = item.EntryId ?? 0,
                    MediaType = item.MediaType,
                    Title = item.Title,
                    ImageUrl = item.ImageUrl,
                    PosterPictureId = item.PosterPictureId,
                    SeasonId = item.SeasonId,
                    EpisodeId = item.EpisodeId,
                    CachedAt = DateTime.Now
                };
                await _db.Database.InsertAsync(cached);
            }

            System.Diagnostics.Debug.WriteLine($"[ElementCacheService] Saved {order} items for '{carouselName}'");
        }
        finally
        {
            _db.Lock.Release();
        }
    }
}
