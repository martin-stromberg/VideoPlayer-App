namespace VideoPlayer.Mvu;

/// <summary>
/// Ergebnis des Abgleichs zwischen Video-Ablageort und Download-Manifest
/// (<see cref="DownloadService.ReconcileAsync"/>): automatisch entfernte
/// überholte Teildateien, erkannte Restdaten ohne Manifest-Bezug (werden
/// dem Anwender ausgewiesen, nicht gelöscht) sowie korrigierte
/// „fertig"-Einträge ohne Datei.
/// </summary>
public sealed record DownloadReconcileResult
{
    /// <summary>Durch entfernte überholte Teildateien freigegebene Bytes.</summary>
    /// <value>Die freigegebenen Bytes.</value>
    public long RemovedPartialBytes { get; init; }

    /// <summary>Anzahl entfernter überholter Teildateien.</summary>
    /// <value>Die Anzahl der entfernten Dateien.</value>
    public int RemovedPartialCount { get; init; }

    /// <summary>Bytes erkannter Restdaten ohne Manifest-Eintrag (nicht entfernt).</summary>
    /// <value>Die erkannten Restdaten-Bytes.</value>
    public long OrphanedBytes { get; init; }

    /// <summary>Anzahl erkannter Restdateien ohne Manifest-Eintrag.</summary>
    /// <value>Die Anzahl der erkannten Restdateien.</value>
    public int OrphanedFileCount { get; init; }

    /// <summary>Anzahl korrigierter Manifest-Einträge („fertig" ohne Datei → fehlgeschlagen).</summary>
    /// <value>Die Anzahl der korrigierten Einträge.</value>
    public int CorrectedCompletedCount { get; init; }
}
