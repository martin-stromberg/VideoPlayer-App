using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests der tvOS-MVU-Verdrahtung (Schritt 2): Da tvOS-Code unter
/// Windows nicht kompiliert werden kann, werden Plattform-Adapter,
/// Effekt-Abdeckung, Komposition, Lebenszyklus-Verdrahtung und der
/// Navigationsrahmen über die Quelldateien geprüft (Muster:
/// <see cref="TvOsProjectContractTests"/>).
/// </summary>
public class TvOsAppContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string TvOsFile(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root, "VideoPlayer.TvOS" }.Concat(parts).ToArray()));

    private static string RepoFile(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));

    /// <summary>Prüft: die tvOS-Plattform-Adapter existieren und nutzen die tvOS-eigenen Mechanismen.</summary>
    [Fact]
    public void TvOsProject_ProvidesPlatformAdapters()
    {
        var settings = TvOsFile("Services", "NSUserDefaultsSettingsStore.cs");
        Assert.Contains("ISettingsStore", settings, StringComparison.Ordinal);
        Assert.Contains("NSUserDefaults", settings, StringComparison.Ordinal);
        Assert.DoesNotContain("SecKeyChain", settings, StringComparison.Ordinal);

        var secrets = TvOsFile("Services", "KeychainSecretStore.cs");
        Assert.Contains("ISecretStore", secrets, StringComparison.Ordinal);
        Assert.Contains("SecKeyChain", secrets, StringComparison.Ordinal);
        Assert.Contains("SecKind.GenericPassword", secrets, StringComparison.Ordinal);
        Assert.DoesNotContain("NSUserDefaults", secrets, StringComparison.Ordinal);

        var layout = TvOsFile("Services", "TvosStorageLayoutProvider.cs");
        Assert.Contains("CachesDirectory", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("SetSkipBackupAttribute", layout, StringComparison.Ordinal);

        var monitor = TvOsFile("Services", "TvosNetworkStatusMonitor.cs");
        Assert.Contains("INetworkStatusMonitor", monitor, StringComparison.Ordinal);
        Assert.Contains("NWPathMonitor", monitor, StringComparison.Ordinal);
        Assert.Contains("NWPathStatus.Satisfied", monitor, StringComparison.Ordinal);
        Assert.Contains("NWInterfaceType.Wifi", monitor, StringComparison.Ordinal);
        Assert.Contains("NWInterfaceType.Wired", monitor, StringComparison.Ordinal);
    }

    /// <summary>Prüft: der tvOS-Effekt-Handler deckt die Effekte des Start- und Verbindungs-Workflows ab.</summary>
    [Fact]
    public void TvOsEffectHandler_CoversStartupAndConnectionEffects()
    {
        var handler = TvOsFile("Services", "TvosAppEffectHandler.cs");

        // Start-/Ablage-Effekte
        Assert.Contains("InitializeStorageEffect", handler, StringComparison.Ordinal);
        Assert.Contains("LoadSettingsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("PersistSettingsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("LoadServersEffect", handler, StringComparison.Ordinal);
        Assert.Contains("CheckServerHealthEffect", handler, StringComparison.Ordinal);
        Assert.Contains("CheckOfflineDataEffect", handler, StringComparison.Ordinal);
        Assert.Contains("StartNetworkMonitoringEffect", handler, StringComparison.Ordinal);
        Assert.Contains("LoadHomeListsEffect", handler, StringComparison.Ordinal);

        // Verbindungs-/Anmelde-Effekte
        Assert.Contains("AttemptAutoLoginEffect", handler, StringComparison.Ordinal);
        Assert.Contains("AttemptLoginEffect", handler, StringComparison.Ordinal);
        Assert.Contains("ConnectServerEventsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("DisconnectServerEventsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("RunFullSyncEffect", handler, StringComparison.Ordinal);

        // Server-Einrichtung (Einrichtungsfluss der späteren UI-Schritte)
        Assert.Contains("DiscoverServersEffect", handler, StringComparison.Ordinal);
        Assert.Contains("CheckCandidateServerEffect", handler, StringComparison.Ordinal);
        Assert.Contains("PersistActiveServerEffect", handler, StringComparison.Ordinal);
        Assert.Contains("ExchangePairingCodeEffect", handler, StringComparison.Ordinal);
        Assert.Contains("CheckBootstrapServerEffect", handler, StringComparison.Ordinal);
        Assert.Contains("ExchangeBootstrapTicketEffect", handler, StringComparison.Ordinal);

        // Navigation
        Assert.Contains("NavigateToPageEffect", handler, StringComparison.Ordinal);

        // Ergebnis-/Ereignis-Messages zurück in den MVU-Zyklus
        Assert.Contains("SettingsLoadedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("ServersLoadedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("ServerHealthCheckCompletedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("AutoLoginCompletedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("LoginCompletedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("OfflineDataCheckCompletedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("NetworkAvailabilityChangedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("ServerEventReceivedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("ServerEventsConnectionClosedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("SynchronizationCompletedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("HomeListsLoadedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("SessionExpiredMessage", handler, StringComparison.Ordinal);
        Assert.Contains("DeviceTokenRevokedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("PairingCompletedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("BootstrapServerCheckedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("BootstrapCompletedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("ServerDiscoveryCompletedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("ServerCheckCompletedMessage", handler, StringComparison.Ordinal);

        // Nicht verdrahtete Effekte erzeugen keine Fehler: default-Zweig
        // als protokolliertes No-Op.
        Assert.Contains("default:", handler, StringComparison.Ordinal);

        // Ereignis-Abonnements des Dienst-Rands
        Assert.Contains("SessionExpired", handler, StringComparison.Ordinal);
        Assert.Contains("DeviceTokenRejected", handler, StringComparison.Ordinal);
        Assert.Contains("AvailabilityChanged", handler, StringComparison.Ordinal);
        Assert.Contains("EventReceived", handler, StringComparison.Ordinal);
        Assert.Contains("ConnectionClosed", handler, StringComparison.Ordinal);
    }

    /// <summary>Prüft: der Handler verdrahtet die für tvOS nicht vorgesehenen Bereiche nicht.</summary>
    [Fact]
    public void TvOsEffectHandler_DoesNotWireExcludedAreas()
    {
        var handler = TvOsFile("Services", "TvosAppEffectHandler.cs");

        Assert.DoesNotContain("SavePlaylistEffect", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("DeletePlaylistEffect", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("RemovePlaylistEntryEffect", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("AddPlaylistEntryEffect", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("EnqueueDownloadEffect", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("RunAutoDownloadsEffect", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("TogglePictureInPictureEffect", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("SendLogProtocolEffect", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("MediaElement", handler, StringComparison.Ordinal);
    }

    /// <summary>Prüft: die Komposition verdrahtet die Kerndienste und den MVU-Host.</summary>
    [Fact]
    public void TvOsApp_CompositionWiresCoreServices()
    {
        var composition = TvOsFile("AppComposition.cs");

        Assert.Contains("MvuHost", composition, StringComparison.Ordinal);
        Assert.Contains("TvosAppEffectHandler", composition, StringComparison.Ordinal);
        Assert.Contains("NSUserDefaultsSettingsStore", composition, StringComparison.Ordinal);
        Assert.Contains("KeychainSecretStore", composition, StringComparison.Ordinal);
        Assert.Contains("TvosStorageLayoutProvider", composition, StringComparison.Ordinal);
        Assert.Contains("TvosNetworkStatusMonitor", composition, StringComparison.Ordinal);
        Assert.Contains("AppProfile.FromEnvironment", composition, StringComparison.Ordinal);
        Assert.Contains("AppStorageLayout", composition, StringComparison.Ordinal);
        Assert.Contains("ServerAddressProvider", composition, StringComparison.Ordinal);
        Assert.Contains("ServerHealthProbe", composition, StringComparison.Ordinal);
        Assert.Contains("AuthSessionService", composition, StringComparison.Ordinal);
        Assert.Contains("DeviceTokenService", composition, StringComparison.Ordinal);
        Assert.Contains("ServerEventsService", composition, StringComparison.Ordinal);
        Assert.Contains("SignalRServerEventsConnection", composition, StringComparison.Ordinal);
        Assert.Contains("ServerSyncService", composition, StringComparison.Ordinal);
        Assert.Contains("HomeListService", composition, StringComparison.Ordinal);
        Assert.Contains("ServerDiscoveryService.CreateDefault", composition, StringComparison.Ordinal);

        // Kein DI-Container und keine MAUI-Abhängigkeit.
        Assert.DoesNotContain("Microsoft.Maui", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("ServiceCollection", composition, StringComparison.Ordinal);
    }

    /// <summary>Prüft: der App-Delegate baut den MVU-Kreislauf auf und stößt den Start-Workflow an.</summary>
    [Fact]
    public void TvOsApp_DelegateStartsMvuLoop()
    {
        var appDelegate = TvOsFile("AppDelegate.cs");

        Assert.Contains("AppComposition", appDelegate, StringComparison.Ordinal);
        Assert.Contains("MainViewController", appDelegate, StringComparison.Ordinal);
        Assert.Contains("MakeKeyAndVisible", appDelegate, StringComparison.Ordinal);
        Assert.Contains("StartupRequestedMessage", appDelegate, StringComparison.Ordinal);
        Assert.Contains("OnActivated", appDelegate, StringComparison.Ordinal);
        Assert.Contains("HomeListsReloadRequestedMessage", appDelegate, StringComparison.Ordinal);
    }

    /// <summary>Prüft: der Seitenrahmen nutzt UITabBarController und die modellgetriebene Navigation.</summary>
    [Fact]
    public void TvOsUi_UsesTabBarAndModelDrivenNavigation()
    {
        var tabBar = TvOsFile("Ui", "MenuTabBarController.cs");
        Assert.Contains("UITabBarController", tabBar, StringComparison.Ordinal);
        Assert.Contains("AppRoutes.MenuPages", tabBar, StringComparison.Ordinal);
        Assert.Contains("NavigatedMessage", tabBar, StringComparison.Ordinal);

        var navigator = TvOsFile("Ui", "TvosNavigator.cs");
        Assert.Contains("CurrentPage", navigator, StringComparison.Ordinal);
        Assert.Contains("AppPage.ServerSetup", navigator, StringComparison.Ordinal);
        Assert.Contains("AppPage.Login", navigator, StringComparison.Ordinal);

        var loading = TvOsFile("Ui", "LoadingViewController.cs");
        Assert.Contains("UIActivityIndicatorView", loading, StringComparison.Ordinal);

        var statePage = TvOsFile("Ui", "ConnectionStatePageViewController.cs");
        Assert.Contains("UIButton", statePage, StringComparison.Ordinal);

        var main = TvOsFile("MainViewController.cs");
        Assert.Contains("AddChildViewController", main, StringComparison.Ordinal);
    }

    /// <summary>Prüft: die plattformneutralen Dienste liegen in VideoPlayer.Mvu und das SignalR-Paket dorthin verschoben wurde.</summary>
    [Fact]
    public void PlatformNeutralServices_LiveInMvu()
    {
        foreach (var file in new[]
        {
            "IServerAddressProvider.cs",
            "ServerAddressProvider.cs",
            "IServerHealthProbe.cs",
            "ServerHealthProbe.cs",
            "SignalRServerEventsConnection.cs"
        })
        {
            var content = RepoFile("VideoPlayer.Mvu", file);
            Assert.Contains("namespace VideoPlayer.Mvu;", content, StringComparison.Ordinal);
        }

        var mvuCsproj = RepoFile("VideoPlayer.Mvu", "VideoPlayer.Mvu.csproj");
        Assert.Contains("Microsoft.AspNetCore.SignalR.Client", mvuCsproj, StringComparison.Ordinal);

        var mauiCsproj = RepoFile("VideoPlayer.Maui", "VideoPlayer.Maui.csproj");
        Assert.DoesNotContain("Microsoft.AspNetCore.SignalR.Client", mauiCsproj, StringComparison.Ordinal);
    }
}
