namespace VideoPlayer.Mvu;

/// <summary>
/// Zustand eines Download-Auftrags. Wird im Download-Manifest
/// (<c>download_manifest</c>) als kleingeschriebener String persistiert
/// (<see cref="DownloadStateNames"/>).
/// </summary>
public enum DownloadState
{
    /// <summary>Der Download wartet auf einen freien Platz bzw. die WLAN-Bedingung.</summary>
    Queued,

    /// <summary>Der Download läuft gerade.</summary>
    Downloading,

    /// <summary>Der Download ist abgeschlossen; die Datei liegt lokal vor.</summary>
    Completed,

    /// <summary>Der Download ist fehlgeschlagen (siehe Fehlercode des Eintrags).</summary>
    Failed,

    /// <summary>Der Download wurde vom Anwender abgebrochen.</summary>
    Cancelled
}

/// <summary>
/// Übersetzung zwischen <see cref="DownloadState"/> und den persistierten
/// Status-Strings des Download-Manifests.
/// </summary>
public static class DownloadStateNames
{
    /// <summary>Bildet den Zustand auf den persistierten Status-String ab.</summary>
    /// <param name="state">Der Zustand.</param>
    /// <returns>Der Status-String des Manifests.</returns>
    public static string ToPersisted(DownloadState state)
    {
        return state switch
        {
            DownloadState.Queued => "queued",
            DownloadState.Downloading => "downloading",
            DownloadState.Completed => "completed",
            DownloadState.Failed => "failed",
            DownloadState.Cancelled => "cancelled",
            _ => "failed"
        };
    }

    /// <summary>Liest den persistierten Status-String als Zustand (unbekannte Werte gelten als <see cref="DownloadState.Failed"/>).</summary>
    /// <param name="state">Der persistierte Status-String.</param>
    /// <returns>Der gelesene Zustand.</returns>
    public static DownloadState FromPersisted(string? state)
    {
        return state?.Trim().ToLowerInvariant() switch
        {
            "queued" => DownloadState.Queued,
            "downloading" => DownloadState.Downloading,
            "completed" => DownloadState.Completed,
            "cancelled" => DownloadState.Cancelled,
            _ => DownloadState.Failed
        };
    }
}
