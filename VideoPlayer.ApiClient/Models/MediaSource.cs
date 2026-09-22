namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Medienquelle des Servers (freigegebener Medienbereich).
/// </summary>
public class MediaSource : MediaEntry
{
    public DateTime CreatedAt { get; set; }
    public DateTime? ClassifiedAt { get; set; }
    public bool Changed { get; set; }
    public DateTime? LastScannedAt { get; set; }
    public long? IconPictureId { get; set; }
}

public class SourceGenres
{
    public long SourceId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public List<Genre> Genres { get; set; } = [];
}

public class Genre
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
}

public class GenreOption
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
