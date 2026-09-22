namespace VideoPlayer.Mvu;

/// <summary>
/// Ergebnisbündel des <see cref="HomeListService"/>: die drei
/// Startseiten-Listen aus der lokalen Cache-Ablage.
/// </summary>
public sealed record HomeLists
{
    /// <summary>Leeres Ergebnis (kein gecachter Inhalt).</summary>
    public static readonly HomeLists Empty = new();

    /// <summary>„Weiterschauen" (Cache-Schlüssel <c>continue_watching</c>).</summary>
    public IReadOnlyList<HomeMediaItem> ContinueWatching { get; init; } = [];

    /// <summary>„Favoriten" (Tabelle <c>cached_favorites</c>).</summary>
    public IReadOnlyList<HomeMediaItem> Favorites { get; init; } = [];

    /// <summary>„Neu im Programm" (Cache-Schlüssel <c>recent</c>).</summary>
    public IReadOnlyList<HomeMediaItem> Recent { get; init; } = [];
}
