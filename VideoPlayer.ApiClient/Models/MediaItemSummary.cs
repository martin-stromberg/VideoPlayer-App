namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Kompakter Medieneintrag einer Quelle (paginierte Liste via <c>api/items</c>).
/// </summary>
public class MediaItemSummary
{
    public string Type { get; set; } = string.Empty;
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Url { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? PictureId { get; set; }
    public int ItemCount { get; set; }
}
