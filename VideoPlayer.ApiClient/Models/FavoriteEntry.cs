namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Serverseitig geführter Favoriteneintrag.
/// </summary>
public class FavoriteEntry
{
    /// <summary>ID des Favoriteneintrags.</summary>
    public long Id { get; set; }

    /// <summary>Zugehöriger Medieneintrag, falls vom Server mitgeliefert.</summary>
    public MediaEntry? Entry { get; set; }

    /// <summary>Zeitpunkt der Favoriten-Markierung.</summary>
    public DateTime CreatedAt { get; set; }
}
