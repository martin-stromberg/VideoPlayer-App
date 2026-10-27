using SQLite;

namespace VideoPlayer.Storage.Entities;

/// <summary>
/// Download-Manifest (nicht backup-pflichtig): Zuordnung heruntergeladener
/// Videos zu lokalen Dateien — nach einer Wiederherstellung ohnehin neu
/// aufzubauen.
/// </summary>
[Table("download_manifest")]
public sealed class DownloadManifestEntry
{
    /// <summary>ID des Manifesteintrags (Primärschlüssel, automatisch).</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>ID des heruntergeladenen Videos (indiziert).</summary>
    [Indexed]
    public int VideoId { get; set; }

    /// <summary>Medientyp des Videos (z. B. <c>movie</c>, <c>episode</c>).</summary>
    public string VideoType { get; set; } = string.Empty;

    /// <summary>Pfad der lokalen Videodatei.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Größe der Datei in Bytes (bei abgeschlossenen Downloads).</summary>
    public long SizeBytes { get; set; }

    /// <summary>Anzeigetitel des Downloads (für die Downloads-Seite ohne Server-Roundtrip).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Bereits übertragene Bytes des laufenden/letzten Laufs.</summary>
    public long DownloadedBytes { get; set; }

    /// <summary>Erwartete Gesamtgröße in Bytes (<c>Content-Length</c>); 0 = unbekannt.</summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Maschinenlesbarer Fehlercode des letzten Fehlschlags
    /// (z. B. <c>quota_exceeded</c>, <c>network</c>, <c>http_404</c>);
    /// leer/<c>null</c> = kein Fehler.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Status des Downloads (<c>queued</c>, <c>downloading</c>,
    /// <c>completed</c>, <c>failed</c>, <c>cancelled</c>).
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Id der zugehörigen Serie (nur Episoden-Downloads, beim Einreihen
    /// mitgegeben); <c>null</c> = kein Serienkontext bekannt. Grundlage
    /// des Offline-Serienkontexts (Platzhalter fehlender Folge-Episoden).
    /// </summary>
    public long? ShowId { get; set; }

    /// <summary>Anzeigetitel der Serie (nur Episoden-Downloads); <c>null</c> = unbekannt.</summary>
    public string? ShowTitle { get; set; }

    /// <summary>Staffelnummer der Episode (nur Episoden-Downloads); <c>null</c> = unbekannt.</summary>
    public int? SeasonNumber { get; set; }

    /// <summary>Episodennummer innerhalb der Staffel (nur Episoden-Downloads); <c>null</c> = unbekannt.</summary>
    public int? EpisodeNumber { get; set; }

    /// <summary>
    /// Id der Playlist, aus deren Kontext der Download eingeplant wurde
    /// (nur playlist-gebundene Downloads); <c>null</c> = kein Playlist-Bezug.
    /// </summary>
    public long? PlaylistId { get; set; }

    /// <summary>
    /// Id des Playlist-Eintrags, dem das Medium innerhalb der Playlist
    /// entspricht; <c>null</c> = kein Playlist-Bezug oder unbekannt.
    /// </summary>
    public long? PlaylistEntryId { get; set; }

    /// <summary>
    /// Sortierposition des Eintrags in der Playlist (serverseitige
    /// Reihenfolge); <c>null</c> = kein Playlist-Bezug oder nicht zugewiesen.
    /// </summary>
    public long? PlaylistSortOrder { get; set; }

    /// <summary>
    /// Bild-Id des Vorschaubilds der Downloads-Liste
    /// (<c>api/pictures/{id}</c>): Episoden-Still bzw. Film-Poster, beim
    /// Einreihen mitgegeben; <c>null</c> = kein Bild bekannt (Platzhalter).
    /// </summary>
    public long? PictureId { get; set; }

    /// <summary>Zeitpunkt der letzten Aktualisierung (UTC).</summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    /// Ablaufdatum des abgeschlossenen Downloads (UTC): Fertigstellung
    /// plus konfigurierter Aufbewahrungszeit; <c>null</c> = nicht gesetzt
    /// bzw. Altbestand (gilt im Bereinigungslauf als abgelaufen).
    /// </summary>
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>
    /// Persistierte Herkunft des Downloads (<c>manual</c>/<c>auto</c>);
    /// <c>null</c> = Altbestand = <c>manual</c>.
    /// </summary>
    public string? Origin { get; set; }
}
