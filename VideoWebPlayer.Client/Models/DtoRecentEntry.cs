/// <summary>
/// DTO für einen kürzlich hinzugefügten Medieneintrag ("Neu"-Liste).
/// </summary>
public class DtoRecentEntry
{
    /// <summary>
    /// Eindeutige ID des Eintrags.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// ID der Medienquelle, zu der der Eintrag gehört.
    /// </summary>
    public long MediaSourceId { get; set; } // NEU: Quelle
    /// <summary>
    /// Zeitpunkt der Veröffentlichung.
    /// </summary>
    public DateTime PublishedAt { get; set; }
    /// <summary>
    /// Zeitpunkt der Erfassung des Eintrags.
    /// </summary>
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Art des Eintrags (Film, Filmsammlung, Serie, Staffel oder Episode).
    /// </summary>
    public RecentEntryType Type { get; set; } // Movie, MovieCollection, TVShow, TVShowSeason, TVShowEpisode
    /// <summary>
    /// Der zugehörige Medieneintrag.
    /// </summary>
    public DtoMediaEntry Entry { get; set; }
}

/// <summary>
/// Art eines Eintrags in der Liste der kürzlich hinzugefügten Medien.
/// </summary>
public enum RecentEntryType
{
    /// <summary>
    /// Film.
    /// </summary>
    Movie,
    /// <summary>
    /// Filmsammlung.
    /// </summary>
    MovieCollection,
    /// <summary>
    /// Fernsehserie.
    /// </summary>
    TVShow,
    /// <summary>
    /// Staffel einer Fernsehserie.
    /// </summary>
    TVShowSeason,
    /// <summary>
    /// Episode einer Fernsehserie.
    /// </summary>
    TVShowEpisode
}
