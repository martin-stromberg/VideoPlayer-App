namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Medienquelle des Servers (freigegebener Medienbereich).
/// </summary>
public class MediaSource : MediaEntry
{
    /// <summary>Anlagezeitpunkt der Quelle.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Zeitpunkt der letzten Klassifizierung, falls erfolgt.</summary>
    public DateTime? ClassifiedAt { get; set; }

    /// <summary>Ob seit dem letzten Scan Änderungen vorliegen.</summary>
    public bool Changed { get; set; }

    /// <summary>Zeitpunkt des letzten Scans, falls erfolgt.</summary>
    public DateTime? LastScannedAt { get; set; }

    /// <summary>ID des Quellen-Icons, falls vorhanden.</summary>
    public long? IconPictureId { get; set; }
}

/// <summary>
/// Genres einer Medienquelle.
/// </summary>
public class SourceGenres
{
    /// <summary>ID der Medienquelle.</summary>
    public long SourceId { get; set; }

    /// <summary>Name der Medienquelle.</summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>Genres der Quelle.</summary>
    public List<Genre> Genres { get; set; } = [];
}

/// <summary>
/// Genre eines Medieninhalts.
/// </summary>
public class Genre
{
    /// <summary>ID des Genres.</summary>
    public long Id { get; set; }

    /// <summary>Name des Genres.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL des Genre-Icons, falls vorhanden.</summary>
    public string? IconUrl { get; set; }
}

/// <summary>
/// Wählbare Genre-Option für den Quellenfilter.
/// </summary>
public class GenreOption
{
    /// <summary>ID des Genres.</summary>
    public long Id { get; set; }

    /// <summary>Name des Genres.</summary>
    public string Name { get; set; } = string.Empty;
}
