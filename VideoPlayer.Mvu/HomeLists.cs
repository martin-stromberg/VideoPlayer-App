namespace VideoPlayer.Mvu;

/// <summary>
/// Ergebnisbündel des <see cref="HomeListService"/>: die drei
/// Startseiten-Listen aus der lokalen Cache-Ablage.
/// </summary>
public sealed record HomeLists
{
    /// <summary>Leeres Ergebnis (kein gecachter Inhalt).</summary>
    /// <value>Ergebnisbündel mit drei leeren Listen.</value>
    public static readonly HomeLists Empty = new();

    /// <summary>„Weiterschauen" (Cache-Schlüssel <c>continue_watching</c>).</summary>
    public IReadOnlyList<HomeMediaItem> ContinueWatching { get; init; } = [];

    /// <summary>„Favoriten" (Tabelle <c>cached_favorites</c>).</summary>
    public IReadOnlyList<HomeMediaItem> Favorites { get; init; } = [];

    /// <summary>„Neu im Programm" (Cache-Schlüssel <c>recent</c>).</summary>
    public IReadOnlyList<HomeMediaItem> Recent { get; init; } = [];

    /// <summary>Noch nicht abgelaufene, nicht leere saisonale Genre-Sektionen.</summary>
    public IReadOnlyList<HomeSeasonalSection> SeasonalGenres { get; init; } = [];

    /// <summary>
    /// Schlüssel der Cache-Bereiche, deren Einträge beim Lesen als
    /// beschädigt erkannt und verworfen wurden — bei bestehender
    /// Serververbindung löst die App danach eine Neu-Synchronisation aus.
    /// </summary>
    public IReadOnlyList<string> DiscardedCaches { get; init; } = [];
}
