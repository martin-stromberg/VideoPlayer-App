namespace VideoPlayer.Storage;

/// <summary>
/// Einmalige Initialisierung des SQLitePCLRaw-Providers (bundle_green bzw.
/// unter tvOS der System-libsqlite3-Provider) — erforderlich, bevor die erste
/// <c>SQLiteAsyncConnection</c> geöffnet wird.
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
                // tvOS-Sonderweg: Das transitiv via bundle_green gezogene
                // SQLitePCLRaw.lib.e_sqlite3.tvos (bis einschließlich 2.1.14)
                // enthält nur die Slices tvos-arm64 (Gerät) und
                // tvossimulator-x64 — für tvossimulator-arm64 fehlt der
                // Build, sodass der native Link auf Apple-Silicon-Hosts mit
                // "ld: ... built for 'tvOS'" scheitert (das native Archiv
                // wird zusätzlich in VideoPlayer.TvOS.csproj per
                // ExcludeAssets unterdrückt). Statt e_sqlite3 wird daher auf
                // tvOS der Provider der im System vorhandenen
                // libsqlite3.dylib gesetzt — derselbe Mechanismus, den
                // bundle_green auf iOS ohnehin verwendet. Gilt für Gerät
                // und Simulator gleichermaßen.
                SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
            }
            else
            {
                SQLitePCL.Batteries_V2.Init();
            }
        }
    }
}
