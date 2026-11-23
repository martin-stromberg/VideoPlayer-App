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
            if (OperatingSystem.IsTvOS())
            {
                // Batteries_V2.Init() wuerde die Managed-Assembly
                // SQLitePCLRaw.lib.e_sqlite3.tvos laden, die im tvOS-Projekt
                // wegen fehlender tvossimulator-arm64-Slice ihrer eingebetteten
                // e_sqlite3.a unterdrueckt wird (ExcludeAssets). Der interne
                // Provider bindet direkt gegen __Internal — das tvOS-Projekt
                // linkt e_sqlite3 selbst (retaggte Archive).
                SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_internal());
            }
            else
            {
                SQLitePCL.Batteries_V2.Init();
            }
        }
    }
}
