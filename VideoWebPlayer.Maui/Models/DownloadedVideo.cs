using SQLite;

namespace VideoWebPlayer.Maui.Models;

/// <summary>
/// Repräsentiert ein lokal heruntergeladenes Video (SQLite-Tabelle "Downloads")
/// inklusive Metadaten und Download-Status.
/// </summary>
[Table("Downloads")]
public class DownloadedVideo
{
    /// <summary>
    /// Primärschlüssel des Download-Eintrags (Auto-Increment).
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// ID des Videos auf dem Server.
    /// </summary>
    [Indexed]
    public long VideoId { get; set; }

    /// <summary>
    /// Medientyp des Videos ("Movie" oder "Episode"), siehe <see cref="MediaTypes"/>.
    /// </summary>
    [Indexed]
    public string VideoType { get; set; } = string.Empty; // "Movie" or "Episode"

    /// <summary>
    /// Lokaler Dateipfad der heruntergeladenen Videodatei.
    /// </summary>
    public string LocalFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Größe der heruntergeladenen Datei in Bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Aufbewahrungsart des Downloads (bestimmt die Speicherdauer), siehe <see cref="DownloadRetentionType"/>.
    /// </summary>
    public DownloadRetentionType RetentionType { get; set; }

    /// <summary>
    /// Zeitpunkt (UTC), zu dem der Download abgeschlossen wurde.
    /// </summary>
    [Indexed]
    public DateTime DownloadedAt { get; set; }

    /// <summary>
    /// Zeitpunkt (UTC), an dem der Download automatisch abläuft und gelöscht wird.
    /// </summary>
    [Indexed]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Anzeigetitel des Videos.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    // Video-Informationen
    /// <summary>
    /// Handlungsbeschreibung des Videos.
    /// </summary>
    public string? Plot { get; set; }

    /// <summary>
    /// Kommagetrennte Liste der Genrenamen des Videos.
    /// </summary>
    public string? GenreNames { get; set; }

    /// <summary>
    /// Erscheinungsjahr des Videos.
    /// </summary>
    public string? ReleaseYear { get; set; }

    // Episode-spezifische Informationen
    /// <summary>
    /// Episodennummer (nur bei Episoden-Downloads).
    /// </summary>
    public int? EpisodeNumber { get; set; }

    /// <summary>
    /// Staffelnummer (nur bei Episoden-Downloads).
    /// </summary>
    public int? SeasonNumber { get; set; }

    /// <summary>
    /// Name der zugehörigen Serie (nur bei Episoden-Downloads).
    /// </summary>
    public string? TVShowName { get; set; }

    // Lokale Bild-Pfade
    /// <summary>
    /// Lokaler Dateipfad des zwischengespeicherten Poster-Bildes.
    /// </summary>
    public string? LocalPosterImagePath { get; set; }

    /// <summary>
    /// Lokaler Dateipfad des zwischengespeicherten Banner-Bildes.
    /// </summary>
    public string? LocalBannerImagePath { get; set; }

    /// <summary>
    /// ID des zugehörigen Films auf dem Server (falls zutreffend).
    /// </summary>
    public long? MovieId { get; set; }

    /// <summary>
    /// ID der zugehörigen Episode auf dem Server (falls zutreffend).
    /// </summary>
    public long? EpisodeId { get; set; }

    /// <summary>
    /// ID der zugehörigen Staffel auf dem Server (falls zutreffend).
    /// </summary>
    public long? SeasonId { get; set; }

    /// <summary>
    /// ID der zugehörigen Serie auf dem Server (falls zutreffend).
    /// </summary>
    public long? TVShowId { get; set; }

    /// <summary>
    /// Aktueller Status des Downloads, siehe <see cref="DownloadStatus"/>.
    /// </summary>
    public DownloadStatus Status { get; set; }

    /// <summary>
    /// Fortschritt des Downloads in Prozent (0–100).
    /// </summary>
    public double ProgressPercent { get; set; }

    /// <summary>
    /// Zuletzt gespeicherte Abspielposition in Sekunden.
    /// </summary>
    // Abspielposition in Sekunden
    public double PlaybackPositionSeconds { get; set; }

    /// <summary>
    /// Gesamtdauer des Videos in Sekunden.
    /// </summary>
    // Gesamtdauer in Sekunden
    public double DurationSeconds { get; set; }
}

/// <summary>
/// Aufbewahrungsart eines Downloads; bestimmt, wie lange ein Download lokal vorgehalten wird.
/// </summary>
public enum DownloadRetentionType
{
    /// <summary>
    /// Kurzzeitiger Cache – wird nach 1 Tag entfernt.
    /// </summary>
    Cache = 0,      // 1 Tag

    /// <summary>
    /// Normaler Download – wird nach 7 Tagen entfernt.
    /// </summary>
    Download = 1,   // 7 Tage

    /// <summary>
    /// Watchlist-Download (Continue-Watching) – wird nach 3 Tagen entfernt.
    /// </summary>
    Watchlist = 2   // 3 Tage – Continue-Watching
}

/// <summary>
/// Status eines Downloads in der Download-Warteschlange.
/// </summary>
public enum DownloadStatus
{
    /// <summary>
    /// Der Download ist eingereiht und wartet auf die Ausführung.
    /// </summary>
    Queued,

    /// <summary>
    /// Der Download läuft gerade.
    /// </summary>
    Downloading,

    /// <summary>
    /// Der Download wurde erfolgreich abgeschlossen.
    /// </summary>
    Completed,

    /// <summary>
    /// Der Download ist fehlgeschlagen.
    /// </summary>
    Failed
}
