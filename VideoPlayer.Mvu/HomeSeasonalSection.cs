namespace VideoPlayer.Mvu;

/// <summary>Anzeigefertige, zeitlich begrenzte Genre-Sektion der Startseite.</summary>
public sealed record HomeSeasonalSection
{
    /// <summary>Stabile Genre-ID.</summary>
    public required long GenreId { get; init; }
    /// <summary>Serverseitige Überschrift.</summary>
    public required string Name { get; init; }
    /// <summary>Exklusive serverseitige UTC-Ablaufgrenze.</summary>
    public required DateTimeOffset ExpiresAt { get; init; }
    /// <summary>Geordnete Medienkarten.</summary>
    public IReadOnlyList<HomeMediaItem> Items { get; init; } = [];
    /// <summary>Stabile Kennung für assistive Technologien und UI-Automatisierung.</summary>
    public string AutomationId => $"SeasonalGenre{GenreId}";
}
