namespace VideoPlayer.Mvu;

/// <summary>
/// Verweis auf eine Episode der Fortsetzungsliste des Players
/// (<see cref="PlayerState.UpcomingEpisodes"/>): Grundlage der
/// Auto-Fortsetzung und des Fehler-Rückfalls auf die nächste Episode.
/// </summary>
public sealed record PlayerEpisodeRef
{
    /// <summary>Id der Episode.</summary>
    public required long MediaId { get; init; }

    /// <summary>Anzeigetitel der Episode.</summary>
    public required string Title { get; init; }
}
