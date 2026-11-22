namespace VideoPlayer.E2E.Tests.Support;

/// <summary>
/// Rekursives Verzeichnis-Löschen für das Testprofil-Cleanup mit
/// Retry/Backoff: gesperrte Dateihandles (z. B. noch offene
/// SQLite-Dateien) lösen sich oft innerhalb weniger hundert
/// Millisekunden, daher werden <see cref="IOException"/> und
/// <see cref="UnauthorizedAccessException"/> erneut versucht. Jeder
/// Versuch und der verbleibende Restfehler werden über den
/// Log-Callback protokolliert — das Cleanup bleibt best-effort, aber
/// nicht mehr kommentarlos.
/// </summary>
internal static class TempDirectoryCleanup
{
    private static readonly TimeSpan[] Backoff =
    [
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromMilliseconds(1000)
    ];

    /// <summary>
    /// Löscht <paramref name="path"/> rekursiv; nicht existierende
    /// Verzeichnisse sind ein No-op. Fehlschläge werden bis zu
    /// <paramref name="maxAttempts"/>-mal wiederholt; die Pause vor
    /// einem erneuten Versuch folgt den Backoff-Stufen 200/500/1000 ms
    /// (mit dem Standard <paramref name="maxAttempts"/>=3 wirken die
    /// Stufen 200 und 500 ms zwischen den Versuchen). Danach bleibt
    /// der Rest best-effort liegen.
    /// </summary>
    /// <param name="path">Zu löschendes Verzeichnis.</param>
    /// <param name="maxAttempts">Maximale Versuchszahl (Standard 3).</param>
    /// <param name="log">Protokoll-Callback für Versuche und Restfehler.</param>
    public static void DeleteWithRetry(string path, int maxAttempts = 3, Action<string>? log = null)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (Exception error)
            {
                var retryable = error is IOException or UnauthorizedAccessException;
                log?.Invoke($"[cleanup] {path}: Versuch {attempt}/{maxAttempts} fehlgeschlagen "
                    + $"({error.GetType().Name}: {error.Message})");
                if (!retryable || attempt == maxAttempts)
                {
                    return;
                }

                Thread.Sleep(Backoff[Math.Min(attempt - 1, Backoff.Length - 1)]);
            }
        }
    }
}
