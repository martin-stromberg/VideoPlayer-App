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

    /// <summary>Dem Anwender anzeigbarer Text zum Serverstatus.</summary>
    public string ServerHealthText => ServerHealth switch
    {
        ServerHealthStatus.Checking => "Serververbindung wird geprüft …",
        ServerHealthStatus.Reachable => "Server: erreichbar",
        ServerHealthStatus.Unreachable => "Server: nicht erreichbar",
        _ => "Server: unbekannt"
    };
}
