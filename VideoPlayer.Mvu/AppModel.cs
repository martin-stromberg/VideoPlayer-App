using VideoPlayer.ApiClient.Discovery;
using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>
/// Unveränderliches Gesamtmodell der App (MVU). Änderungen entstehen
/// ausschließlich über <see cref="AppUpdate.Update"/> als neue Instanz.
/// </summary>
public sealed record AppModel
{
    /// <summary>Aktuell angezeigte Seite. Startwert ist <see cref="AppPage.Loading"/>.</summary>
    public AppPage CurrentPage { get; init; } = AppPage.Loading;

    /// <summary>Start-Workflow (Erreichbarkeitsprüfung) läuft.</summary>
    public bool IsStartupRunning { get; init; } = true;

    /// <summary>Ergebnis der Server-Erreichbarkeitsprüfung.</summary>
    public ServerHealthStatus ServerHealth { get; init; } = ServerHealthStatus.Unknown;

    /// <summary>
    /// Geladene App-Einstellungen (beim Start über <see cref="LoadSettingsEffect"/>
    /// befüllt; bis dahin Defaults).
    /// </summary>
    public AppSettings Settings { get; init; } = new();

    /// <summary>Bekannte Server (backup-pflichtige Liste, zuletzt verwendet zuerst).</summary>
    public IReadOnlyList<KnownServer> KnownServers { get; init; } = [];

    /// <summary>
    /// Kanonische Adresse des aktiven (zuletzt verbundenen) Servers;
    /// <c>null</c>, wenn noch kein Server hinterlegt ist (→ Erststart-Einrichtung).
    /// </summary>
    public string? ActiveServerAddress { get; init; }

    /// <summary>Zuletzt per Netzwerk-Erkennung gefundene Server-Kandidaten.</summary>
    public IReadOnlyList<DiscoveredServer> DiscoveredServers { get; init; } = [];

    /// <summary>Netzwerk-Erkennung läuft.</summary>
    public bool IsDiscoveryRunning { get; init; }

    /// <summary>Netzwerk-Erkennung wurde mindestens einmal durchgeführt.</summary>
    public bool DiscoveryAttempted { get; init; }

    /// <summary>Verbindungsprüfung eines Server-Kandidaten läuft.</summary>
    public bool IsServerCheckRunning { get; init; }

    /// <summary>
    /// Fehlertext der Server-Einrichtung/-verwaltung (ungültige Eingabe oder
    /// nicht erreichbarer Server); <c>null</c> = kein Fehler.
    /// </summary>
    public string? ServerSetupError { get; init; }

    /// <summary>Dem Anwender anzeigbarer Text zum Serverstatus.</summary>
    public string ServerHealthText => ServerHealth switch
    {
        ServerHealthStatus.Checking => "Serververbindung wird geprüft …",
        ServerHealthStatus.Reachable => "Server: erreichbar",
        ServerHealthStatus.Unreachable => "Server: nicht erreichbar",
        _ => "Server: unbekannt"
    };
}
