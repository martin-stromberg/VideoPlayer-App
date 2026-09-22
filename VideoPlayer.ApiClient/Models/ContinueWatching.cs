namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Eintrag der Weiterschauen-Liste.
/// </summary>
public class ContinueWatchingEntry
{
    public string MediaType { get; set; } = string.Empty;
    public MediaEntry? Entry { get; set; }
    public long PositionSeconds { get; set; }
    public long? DurationSeconds { get; set; }
    public string Title { get; set; } = string.Empty;
    public long? PosterPictureId { get; set; }
}

/// <summary>
/// Ergebnis einer Mutation der Weiterschauen-Liste (Ausblenden/Überspringen).
/// </summary>
public sealed record ContinueWatchingMutationResult(string Status, string Message);

/// <summary>
/// Fortschrittsmeldung für die Wiedergabe eines Mediums.
/// </summary>
public class PlaybackProgressReport
{
    public string MediaType { get; set; } = string.Empty;
    public long MediaId { get; set; }
    public long PositionSeconds { get; set; }
    public long DurationSeconds { get; set; }
}
