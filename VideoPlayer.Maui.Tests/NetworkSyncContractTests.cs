using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests der Netzwerk-/Sync-Verdrahtung (Schritt 6): Da das
/// Testprojekt keine MAUI-Targets ausführen kann, werden DI-Registrierung,
/// Paketreferenz, Effekt-Handler-Zweige und die SignalR-/Connectivity-
/// Implementierungen über die Quelldateien geprüft (Muster:
/// <see cref="ConnectionWorkflowContractTests"/>).
/// </summary>
public class NetworkSyncContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string MauiFile(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root, "VideoPlayer.Maui" }.Concat(parts).ToArray()));

    /// <summary>Prüft MauiProgram: registriert die Netzwerk- und Sync-Services.</summary>
    [Fact]
    public void MauiProgram_RegistersNetworkAndSyncServices()
    {
        var program = MauiFile("MauiProgram.cs");

        Assert.Contains("INetworkStatusMonitor", program, StringComparison.Ordinal);
        Assert.Contains("ConnectivityNetworkStatusMonitor", program, StringComparison.Ordinal);
        Assert.Contains("IServerEventsConnection", program, StringComparison.Ordinal);
        Assert.Contains("SignalRServerEventsConnection", program, StringComparison.Ordinal);
        Assert.Contains("ServerEventsService", program, StringComparison.Ordinal);
        Assert.Contains("PendingProgressSyncService", program, StringComparison.Ordinal);
        Assert.Contains("ProgressReportService", program, StringComparison.Ordinal);
        Assert.Contains("ServerSyncService", program, StringComparison.Ordinal);
    }

    /// <summary>Prüft Csproj: referenziert den SignalR-Client.</summary>
    [Fact]
    public void Csproj_ReferencesSignalRClient()
    {
        var csproj = MauiFile("VideoPlayer.Maui.csproj");

        Assert.Contains("Microsoft.AspNetCore.SignalR.Client", csproj, StringComparison.Ordinal);
    }

    /// <summary>Prüft EffectHandler: behandelt alle NetworkSync-Effekte.</summary>
    [Fact]
    public void EffectHandler_HandlesAllNetworkSyncEffects()
    {
        var handler = MauiFile("Services", "ShellAppEffectHandler.cs");

        // Effekt-Zweige
        Assert.Contains("StartNetworkMonitoringEffect", handler, StringComparison.Ordinal);
        Assert.Contains("ConnectServerEventsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("DisconnectServerEventsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("RunFullSyncEffect", handler, StringComparison.Ordinal);
        Assert.Contains("SyncContinueWatchingEffect", handler, StringComparison.Ordinal);
        Assert.Contains("SyncFavoritesEffect", handler, StringComparison.Ordinal);
        Assert.Contains("SyncMetadataEffect", handler, StringComparison.Ordinal);

        // Rückdispatch der Ergebnis-/Ereignis-Messages
        Assert.Contains("NetworkAvailabilityChangedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("ServerEventReceivedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("ServerEventsConnectionClosedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("SynchronizationCompletedMessage", handler, StringComparison.Ordinal);

        // Ereignis-Abonnements (Netzwerkmonitor + Ereignisverbindung)
        Assert.Contains("AvailabilityChanged", handler, StringComparison.Ordinal);
        Assert.Contains("EventReceived", handler, StringComparison.Ordinal);
        Assert.Contains("ConnectionClosed", handler, StringComparison.Ordinal);
    }

    /// <summary>Prüft SignalRConnection: bildet die Server-Hub-Events ab.</summary>
    [Fact]
    public void SignalRConnection_MapsServerHubEvents()
    {
        var connection = MauiFile("Services", "SignalRServerEventsConnection.cs");

        // Ereignisnamen des Server-Hubs (VideoWebPlayer/Hubs/MediaUpdateHub)
        Assert.Contains("\"ContinueWatchingUpdated\"", connection, StringComparison.Ordinal);
        Assert.Contains("\"FavoritesChanged\"", connection, StringComparison.Ordinal);
        Assert.Contains("\"NewVideosScanned\"", connection, StringComparison.Ordinal);
        Assert.Contains("\"StatusChanged\"", connection, StringComparison.Ordinal);
        Assert.Contains("WithAutomaticReconnect", connection, StringComparison.Ordinal);
        Assert.Contains("AccessTokenProvider", connection, StringComparison.Ordinal);

        // Hub-Pfad liegt im plattformneutralen Orchestrierungsdienst.
        var service = File.ReadAllText(
            Path.Combine(Root, "VideoPlayer.Mvu", "ServerEventsService.cs"));
        Assert.Contains("hubs/mediaupdate", service, StringComparison.Ordinal);
    }

    /// <summary>Prüft NetworkMonitor: verwendet MAUI Connectivity.</summary>
    [Fact]
    public void NetworkMonitor_UsesMauiConnectivity()
    {
        var monitor = MauiFile("Services", "ConnectivityNetworkStatusMonitor.cs");

        Assert.Contains("Connectivity.Current", monitor, StringComparison.Ordinal);
        Assert.Contains("ConnectivityChanged", monitor, StringComparison.Ordinal);
        Assert.Contains("NetworkAccess.Internet", monitor, StringComparison.Ordinal);
        Assert.Contains("NetworkAccess.ConstrainedInternet", monitor, StringComparison.Ordinal);
    }
}
