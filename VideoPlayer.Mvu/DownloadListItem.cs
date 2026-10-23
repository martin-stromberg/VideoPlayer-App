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

    /// <summary>
    /// Bild-Id des Vorschaubilds (<c>api/pictures/{id}</c>): Episoden-Still
    /// bzw. Film-Poster aus dem Manifest; <c>null</c> = kein Bild bekannt
    /// (der Eintrag zeigt den getönten Platzhalter-Rahmen).
    /// </summary>
    public long? PictureId { get; init; }

    /// <summary>Aktueller Zustand des Downloads.</summary>
    public required DownloadState State { get; init; }

    /// <summary>Bereits übertragene Bytes.</summary>
    public long DownloadedBytes { get; init; }

    /// <summary>Erwartete Gesamtgröße in Bytes; 0 = unbekannt.</summary>
    public long TotalBytes { get; init; }

    /// <summary>Größe der fertigen Datei in Bytes (nur abgeschlossene Downloads).</summary>
    public long SizeBytes { get; init; }

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

    /// <summary>Ein Vorschaubild kann geladen werden.</summary>
    public bool HasPicture => PictureId is > 0;

    /// <summary>
    /// Breite des Vorschaubild-Rahmens in dp — Episode: 16:9-Still
    /// (128), sonst 2:3-Poster (48); die Rahmenhöhe beträgt einheitlich 72.
    /// </summary>
    public double PreviewWidth => MediaTypeNames.Normalize(MediaType) == "tvshowepisode" ? 128 : 48;

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
            DownloadService.ErrorMissingFile => "Die Datei ist nicht mehr vorhanden — der Download kann erneut eingeplant werden.",
            var code when code is not null && code.StartsWith(DownloadService.ErrorHttpPrefix, StringComparison.Ordinal) =>
                "Der Server hat den Download abgelehnt.",
            _ => "Der Download ist fehlgeschlagen."
        };

    /// <summary>
    /// Größenanzeige des Eintrags: bei abgeschlossenen Downloads die Dateigröße,
    /// bei laufenden „x von y", sonst <c>null</c> (kein Label).
    /// </summary>
    public string? SizeText =>
        State switch
        {
            DownloadState.Completed when SizeBytes > 0 => FormatBytes(SizeBytes),
            DownloadState.Downloading when TotalBytes > 0 =>
                $"{FormatBytes(DownloadedBytes)} von {FormatBytes(TotalBytes)}",
            _ => null
        };

    /// <summary>Eine Größenangabe wird angezeigt.</summary>
    public bool HasSizeText => SizeText is not null;

    /// <summary>Formatiert Bytes als lesbare Angabe (GB ab 0,1 GB, sonst MB).</summary>
    /// <param name="bytes">Die Bytezahl.</param>
    /// <returns>Formatierte Größenangabe.</returns>
    public static string FormatBytes(long bytes) =>
        bytes >= 104_857_600L
            ? $"{bytes / 1_073_741_824d:0.0} GB"
            : $"{bytes / 1_048_576d:0} MB";

    /// <summary>Übersetzte Medientyp-Bezeichnung (Anzeige neben dem Zustand).</summary>
    public string MediaTypeText =>
        MediaTypeNames.Normalize(MediaType) switch
        {
            "movie" => "Film",
            "tvshowepisode" => "Episode",
            _ => "Medium"
        };
}
