namespace VideoPlayer.Mvu;

/// <summary>
/// Ergebnis eines Plattform-Download-Transfers (<see cref="IDownloadBackend"/>).
/// </summary>
/// <param name="BytesWritten">Tatsächlich geschriebene Bytes (bei Erfolg = Dateigröße).</param>
/// <param name="TotalBytes">Vom Server/System gemeldete Gesamtgröße; 0 = unbekannt.</param>
/// <param name="ErrorCode">Maschinenlesbarer Fehlercode (<c>network</c>, <c>http_*</c>); <c>null</c> bei Erfolg.</param>
/// <returns>Ergebnis-Datensatz des Transfers.</returns>
public sealed record DownloadBackendResult(long BytesWritten, long TotalBytes, string? ErrorCode);

/// <summary>
/// Abstraktion des eigentlichen Download-Transfers, damit auf iOS die
/// Übertragung an eine systemseitige Hintergrund-URL-Session delegiert
/// werden kann, während andere Plattformen den prozessinternen
/// <see cref="HttpClient"/>-Stream nutzen. Auftragsverwaltung (Manifest,
/// Warteschlange, Kontingent) bleibt im <see cref="DownloadService"/>.
/// </summary>
public interface IDownloadBackend
{
    /// <summary>
    /// Führt den Transfer aus: lädt <paramref name="url"/> nach
    /// <paramref name="targetPath"/> (wird überschrieben) und meldet den
    /// Fortschritt über <paramref name="progress"/> (übertragene Bytes,
    /// Gesamtgröße).
    /// </summary>
    /// <param name="url">Die abzurufende Stream-URL.</param>
    /// <param name="token">Zugriffstoken (Authorization-Header); <c>null</c> = kein Token.</param>
    /// <param name="targetPath">Zieldatei im Video-Ablageort.</param>
    /// <param name="wifiOnly"><c>true</c> = nur ungedrosseltes Netz (kein Mobilfunk).</param>
    /// <param name="progress">Fortschritts-Callback (Bytes übertragen, Gesamt).</param>
    /// <param name="cancellationToken">Bricht den Transfer ab (Datei wird entfernt).</param>
    /// <returns>Das Transfer-Ergebnis.</returns>
    Task<DownloadBackendResult> TransferAsync(
        string url,
        string? token,
        string targetPath,
        bool wifiOnly,
        IProgress<(long Downloaded, long Total)> progress,
        CancellationToken cancellationToken);
}
