namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Serverseitig geführter Favoriteneintrag.
/// </summary>
public class FavoriteEntry
{
    public long Id { get; set; }
    public MediaEntry? Entry { get; set; }
    public DateTime CreatedAt { get; set; }
}
