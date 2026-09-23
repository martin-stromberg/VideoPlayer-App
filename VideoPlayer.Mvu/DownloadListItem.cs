namespace VideoPlayer.Mvu;

/// <summary>
/// Anzeigefertiger Eintrag der Downloads-Seite: Identität des Mediums,
/// Titel, Download-Zustand, Fortschritt und Aktionen (Abbrechen,
/// Erneut versuchen, Löschen). Wird von <see cref="DownloadService"/>
/// aus den Zeilen des Download-Manifests gebildet.
/// </summary>
public sealed record DownloadListItem
{
    /// <summary>Id des Manifesteintrags (stabile Sortierung der Liste).</summary>
    public required int EntryId { get; init; }

    /// <summary>Medientyp in CLR-Konvention, kleingeschrieben (<c>movie</c>, <c>tvshowepisode</c>).</summary>
    public required string MediaType { get; init; }

    /// <summary>Id des Mediums.</summary>
    public required long MediaId { get; init; }

    /// <summary>Anzeigetitel des Downloads.</summary>
    public required string Title { get; init; }

    /// <summary>Aktueller Zustand des Downloads.</summary>
    public required DownloadState State { get; init; }

    /// <summary>Bereits übertragene Bytes.</summary>
    public long DownloadedBytes { get; init; }

    /// <summary>Erwartete Gesamtgröße in Bytes; 0 = unbekannt.</summary>
    public long TotalBytes { get; init; }

    /// <summary>Maschinenlesbarer Fehlercode des letzten Fehlschlags; <c>null</c> = kein Fehler.</summary>
    public string? Error { get; init; }

    /// <summary>Der Download wartet oder läuft gerade.</summary>
    public bool IsActive => State is DownloadState.Queued or DownloadState.Downloading;

    /// <summary>Der Abbrechen-Schalter des Eintrags ist wirksam (nur aktive Downloads).</summary>
    public bool CanCancel => IsActive;

    /// <summary>Der Wiederholen-Schalter des Eintrags ist wirksam (fehlgeschlagen oder abgebrochen).</summary>
    public bool CanRetry => State is DownloadState.Failed or DownloadState.Cancelled;

    /// <summary>Der Löschen-Schalter des Eintrags ist wirksam (immer — laufende Downloads werden zuvor abgebrochen).</summary>
    public bool CanDelete => true;

    /// <summary>Der Fortschrittsbalken wird angezeigt (nur laufende Downloads).</summary>
    public bool ShowProgress => State == DownloadState.Downloading;

    /// <summary>Ein Fehlerhinweis wird angezeigt.</summary>
    public bool HasError => ErrorText is not null;

    /// <summary>Fortschritt im Bereich 0–1 (bei unbekannter Gesamtgröße 0).</summary>
    public double Progress =>
        State == DownloadState.Completed
            ? 1d
            : TotalBytes > 0
                ? Math.Clamp((double)DownloadedBytes / TotalBytes, 0d, 1d)
                : 0d;

    /// <summary>Prozentangabe des Fortschritts (z. B. „42 %").</summary>
    /// <value>Anzeigetext wie „42 %“.</value>
    public string ProgressPercentText => $"{(int)Math.Round(Progress * 100d)} %";

    /// <summary>Übersetzter Zustandstext (Wartend, Läuft, Fertig, Fehler, Abgebrochen).</summary>
    public string StateText =>
        State switch
        {
            DownloadState.Queued => "Wartend",
            DownloadState.Downloading => "Läuft",
            DownloadState.Completed => "Fertig",
            DownloadState.Cancelled => "Abgebrochen",
            _ => "Fehler"
        };

    /// <summary>
    /// Dem Anwender anzeigbarer Fehlertext zum <see cref="Error"/>-Code;
    /// <c>null</c>, wenn kein Fehler vorliegt.
    /// </summary>
    public string? ErrorText =>
        Error switch
        {
            null => null,
            DownloadService.ErrorQuotaExceeded => "Das Speicherplatzkontingent ist erreicht.",
            DownloadService.ErrorNoConnection => "Keine Serververbindung.",
            DownloadService.ErrorNetwork => "Der Download wurde unterbrochen (Netzwerkfehler).",
            DownloadService.ErrorIo => "Die Videodatei konnte nicht gespeichert werden.",
            var code when code is not null && code.StartsWith(DownloadService.ErrorHttpPrefix, StringComparison.Ordinal) =>
                "Der Server hat den Download abgelehnt.",
            _ => "Der Download ist fehlgeschlagen."
        };

    /// <summary>Übersetzte Medientyp-Bezeichnung (Anzeige neben dem Zustand).</summary>
    public string MediaTypeText =>
        MediaTypeNames.Normalize(MediaType) switch
        {
            "movie" => "Film",
            "tvshowepisode" => "Episode",
            _ => "Medium"
        };
}
