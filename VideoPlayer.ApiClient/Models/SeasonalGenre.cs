namespace VideoPlayer.ApiClient.Models;

/// <summary>Eine serverseitig ausgewählte saisonale Startseiten-Sektion.</summary>
public sealed class SeasonalGenre
{
    /// <summary>Stabile Genre-ID.</summary>
    public long GenreId { get; set; }
    /// <summary>Vom Server gelieferte Überschrift.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Exklusive UTC-Ablaufgrenze; ein fehlender Wert ist bereits abgelaufen.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
    /// <summary>Geordnete Filme, Sammlungen und Serien.</summary>
    public MediaEntry[] Entries { get; set; } = [];
}
