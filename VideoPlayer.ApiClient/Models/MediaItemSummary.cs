namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Kompakter Medieneintrag einer Quelle (paginierte Liste via <c>api/items</c>).
/// </summary>
public class MediaItemSummary
{
    /// <summary>Medientyp des Eintrags (z. B. <c>movie</c>, <c>show</c>).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>ID des Eintrags.</summary>
    public long Id { get; set; }

    /// <summary>Titel des Eintrags.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Kurzbeschreibung, falls vorhanden.</summary>
    public string? Description { get; set; }

    /// <summary>URL des Eintrags, falls vorhanden.</summary>
    public string? Url { get; set; }

    /// <summary>Zeitpunkt der Erfassung.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>ID des Vorschaubilds, falls vorhanden.</summary>
    public long? PictureId { get; set; }

    /// <summary>Anzahl enthaltener Elemente (z. B. Episoden einer Serie).</summary>
    public int ItemCount { get; set; }
}
