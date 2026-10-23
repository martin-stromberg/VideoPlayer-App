#if DEBUG || DEV_API
namespace msTools.DeveloperApi;

/// <summary>
/// Erweiterungsmethoden der Routen-Tabelle für die kanonischen
/// Endpunkte der Developer-API.
/// </summary>
public static class DevApiRouterExtensions
{
    /// <summary>
    /// Registriert <c>GET /model</c> mit einem Snapshot-Provider: die
    /// Bibliothek kennt kein konkretes Modell — der Provider liefert das
    /// serialisierbare Zustandsobjekt der jeweiligen App.
    /// </summary>
    /// <param name="router">Die Routen-Tabelle.</param>
    /// <param name="snapshotProvider">Liefert den aktuellen Modell-Snapshot; wird je Anfrage auf dem Request-Thread aufgerufen (muss thread-sicher lesbar sein). Keine Zugangsdaten oder Geheimnisse zurückgeben.</param>
    public static void MapModel(this DevApiRouter router, Func<object?> snapshotProvider)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(snapshotProvider);
        router.Map("GET", "/model", (_, _) => Task.FromResult(DevApiResponse.Json(snapshotProvider())));
    }
}
#endif
