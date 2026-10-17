using SQLite;

namespace VideoWebPlayer.Maui.Models;

/// <summary>
/// Lokal zwischengespeicherter Eintrag eines Media-Carousels (SQLite-Tabelle "CarouselCache").
/// </summary>
[Table("CarouselCache")]
public class CachedCarouselItem
{
    /// <summary>
    /// Primärschlüssel des Cache-Eintrags (Auto-Increment).
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Name des Carousels, zu dem dieser Eintrag gehört (z. B. "ContinueWatching", "Favorites").
    /// </summary>
    [Indexed]
    public string CarouselName { get; set; } = string.Empty;

    /// <summary>
    /// Sortierposition des Eintrags innerhalb des Carousels.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// ID des Medien-Eintrags auf dem Server (Film-, Serien- oder Episoden-ID).
    /// </summary>
    public long EntryId { get; set; }

    /// <summary>
    /// Medientyp des Eintrags (z. B. "movie", "show", "collection"), siehe <see cref="MediaTypes"/>.
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// Anzeigetitel des Medien-Eintrags.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// URL des Poster-/Banner-Bildes.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// ID des Poster-Bildes auf dem Server (falls vorhanden).
    /// </summary>
    public long? PosterPictureId { get; set; }

    /// <summary>
    /// ID der zugehörigen Staffel (bei Episoden-/Serien-Einträgen).
    /// </summary>
    public long? SeasonId { get; set; }

    /// <summary>
    /// ID der zugehörigen Episode (bei Episoden-Einträgen).
    /// </summary>
    public long? EpisodeId { get; set; }

    /// <summary>
    /// Zeitpunkt (UTC), zu dem der Eintrag im Cache abgelegt wurde.
    /// </summary>
    public DateTime CachedAt { get; set; }
}
