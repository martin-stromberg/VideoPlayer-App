namespace VideoPlayer.Storage;

/// <summary>
/// Einmalige Initialisierung des SQLitePCLRaw-Providers (bundle_green) —
/// erforderlich, bevor die erste <c>SQLiteAsyncConnection</c> geöffnet wird.
/// </summary>
public static class SqliteBootstrap
{
    private static int initialized;

    /// <summary>Führt die Initialisierung einmalig aus (threadsicher, idempotent).</summary>
    public static void EnsureInitialized()
    {
        if (Interlocked.Exchange(ref initialized, 1) == 0)
        {
            SQLitePCL.Batteries_V2.Init();
        }
    }
}
