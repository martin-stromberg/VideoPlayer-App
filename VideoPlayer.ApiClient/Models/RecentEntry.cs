namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Zuletzt hinzugefügter Inhalt („Neu im Programm").
/// </summary>
public class RecentEntry
{
    public long Id { get; set; }
    public long MediaSourceId { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public RecentEntryKind Type { get; set; }
    public MediaEntry? Entry { get; set; }
}

public enum RecentEntryKind
{
    Movie,
    MovieCollection,
    TvShow,
    TvShowSeason,
    TvShowEpisode
}
