namespace VideoWebPlayer.Maui.Models;

/// <summary>
/// Repräsentiert eine Anfrage zur Wiedergabe eines Videos.
/// Die eigentliche Quelle (lokale Datei oder Stream) wird asynchron über
/// <see cref="SourceAvailable"/> bereitgestellt.
/// </summary>
public class VideoRequest
{
    /// <summary>
    /// ID des Videos auf dem Server.
    /// </summary>
    public long VideoId { get; set; }

    /// <summary>
    /// Medientyp des Videos ("Movie" oder "Episode"), siehe <see cref="MediaTypes"/>.
    /// </summary>
    public string VideoType { get; set; } = string.Empty; // "Movie" or "Episode"

    /// <summary>
    /// Anzeigetitel des Videos.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Informationen zur aufgelösten Videoquelle; <c>null</c>, solange die Quelle noch nicht verfügbar ist.
    /// </summary>
    public VideoSourceInfo? SourceInfo { get; private set; }

    /// <summary>
    /// Gibt an, ob die Videoquelle bereits aufgelöst wurde und die Wiedergabe starten kann.
    /// </summary>
    public bool IsReady => SourceInfo != null;

    /// <summary>
    /// Wird ausgelöst, sobald die Videoquelle verfügbar ist.
    /// </summary>
    public event EventHandler<VideoSourceInfo>? SourceAvailable;

    internal void SetSource(VideoSourceInfo sourceInfo)
    {
        SourceInfo = sourceInfo;
        SourceAvailable?.Invoke(this, sourceInfo);
    }
}

/// <summary>
/// Beschreibt die aufgelöste Quelle eines Videos (Pfad/URL sowie Wiedergabe-Metadaten).
/// </summary>
public class VideoSourceInfo
{
    /// <summary>
    /// Pfad oder URL der Videoquelle (lokale Datei oder Stream-Adresse).
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Art der Quelle, siehe <see cref="VideoSourceType"/>.
    /// </summary>
    public VideoSourceType SourceType { get; set; }

    /// <summary>
    /// Position, ab der die Wiedergabe fortgesetzt werden soll (falls bekannt).
    /// </summary>
    public TimeSpan? ResumePosition { get; set; }

    /// <summary>
    /// Gesamtdauer des Videos (falls bekannt).
    /// </summary>
    public TimeSpan? Duration { get; set; }
}

/// <summary>
/// Art einer Videoquelle.
/// </summary>
public enum VideoSourceType
{
    /// <summary>
    /// Lokale, bereits heruntergeladene Datei.
    /// </summary>
    LocalFile,

    /// <summary>
    /// Stream-URL, die direkt vom Server abgespielt wird.
    /// </summary>
    StreamUrl
}
