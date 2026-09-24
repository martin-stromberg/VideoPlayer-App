using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests der Server-Einrichtung/-verwaltung: Da das Testprojekt
/// keine MAUI-Targets ausführen kann, werden UI-Verdrahtung, Navigation und
/// Service-Registrierung über die Quelldateien geprüft
/// (Muster: <see cref="VideoPlayerMauiContractTests"/>).
/// </summary>
public class ServerSetupContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string MauiFile(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root, "VideoPlayer.Maui" }.Concat(parts).ToArray()));

    /// <summary>Prüft ServerSetupPage: existiert mit manueller Eingabe und Discovery.</summary>
    [Fact]
    public void ServerSetupPage_ExistsWithManualEntryAndDiscovery()
    {
        var xamlPath = Path.Combine(Root, "VideoPlayer.Maui", "Pages", "ServerSetupPage.xaml");
        Assert.True(File.Exists(xamlPath));
        var xaml = File.ReadAllText(xamlPath);

        // Manuelle Eingabe + Übernahme
        Assert.Contains("AddressEntry", xaml, StringComparison.Ordinal);
        Assert.Contains("ConnectButton", xaml, StringComparison.Ordinal);
        // Netzwerk-Erkennung + Auswahl der gefundenen Server
        Assert.Contains("DiscoverButton", xaml, StringComparison.Ordinal);
        Assert.Contains("DiscoveredServersView", xaml, StringComparison.Ordinal);
        // Bekannte Server + aktiver Server („Server wechseln")
        Assert.Contains("KnownServersView", xaml, StringComparison.Ordinal);
        Assert.Contains("ActiveServerLabel", xaml, StringComparison.Ordinal);
        // Status/Fehler-Anzeige
        Assert.Contains("ErrorLabel", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft ServerSetupPage: die Steuerelemente haben AutomationIds.</summary>
    [Fact]
    public void ServerSetupPage_ControlsHaveAutomationIds()
    {
        var page = System.Xml.Linq.XDocument.Load(
            Path.Combine(Root, "VideoPlayer.Maui", "Pages", "ServerSetupPage.xaml"));

        var automationIds = page.Descendants()
            .Select(e => (string?)e.Attribute("AutomationId"))
            .Where(id => id is not null)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var id in new[]
        {
            "KnownServersView", "ActiveServerLabel", "DiscoverButton",
            "DiscoveredServersView", "DiscoveryEmptyLabel",
            "AddressEntry", "ConnectButton", "ErrorLabel",
            // Modernisierung (Issue 17, Schritt 4): neue Elemente der
            // Kartenstruktur und des Zustandsfeedbacks — bewusste
            // Erweiterung der gepinnten Menge, kein Pin wurde entfernt.
            "ActiveServerCard", "ServerHealthLabel",
            "DiscoveryRunningLabel", "ConnectCard", "CheckStatusLabel",
            "PairingSection", "PairingRunningLabel"
        })
        {
            Assert.Contains(id, automationIds);
        }
    }

    /// <summary>Prüft ServerSetupPage: die Modernisierung nutzt Karten und Vp*-Tokens.</summary>
    [Fact]
    public void ServerSetupPage_UsesCardLayout()
    {
        var xaml = MauiFile("Pages", "ServerSetupPage.xaml");

        // Kartenform: abgerundete Flächen (Radius 8 wie die übrigen Karten
        // der App — Pin aus DesignTokensContractTests).
        Assert.Contains("RoundRectangle 8", xaml, StringComparison.Ordinal);
        // Status-Badge des aktiven Servers (Pill-Form) mit Model-Bindung.
        Assert.Contains("ServerHealthLabel", xaml, StringComparison.Ordinal);
        Assert.Contains("RoundRectangle 999", xaml, StringComparison.Ordinal);
        // Keine harten Farben — nur Vp*-Tokens.
        Assert.DoesNotContain("#", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft ServerSetupPage: im Code-Behind werden MVU-Nachrichten dispatcht.</summary>
    [Fact]
    public void ServerSetupPage_CodeBehind_DispatchesMvuMessages()
    {
        var code = MauiFile("Pages", "ServerSetupPage.xaml.cs");

        Assert.Contains("ServerDiscoveryRequestedMessage", code, StringComparison.Ordinal);
        Assert.Contains("ServerAddressSubmittedMessage", code, StringComparison.Ordinal);
        Assert.Contains("DiscoveredServerSelectedMessage", code, StringComparison.Ordinal);
        Assert.Contains("ServerSwitchRequestedMessage", code, StringComparison.Ordinal);
        // Keine internen IDs/GUIDs als Benutzereingabe — Auswahl läuft über Listen.
        Assert.DoesNotContain("Guid", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft SettingsPage: enthält den Eintrag für den diskreten Serverwechsel.</summary>
    [Fact]
    public void SettingsPage_ContainsDiscreetServerSwitchEntry()
    {
        var xaml = MauiFile("Pages", "SettingsPage.xaml");

        Assert.Contains("Server wechseln", xaml, StringComparison.Ordinal);
        Assert.Contains("ServerSwitchButton", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft AppShell: bei <c>RegistersServerSetupRoute</c> → registriert die Route ohne Flyout-Eintrag.</summary>
    [Fact]
    public void AppShell_RegistersServerSetupRoute_WithoutFlyoutItem()
    {
        var codeBehind = MauiFile("AppShell.xaml.cs");
        var xaml = MauiFile("AppShell.xaml");

        Assert.Contains("serversetup", codeBehind, StringComparison.Ordinal);
        Assert.Contains("ServerSetupPage", codeBehind, StringComparison.Ordinal);
        // Kein Menüeintrag für die Serververwaltung.
        Assert.DoesNotContain("serversetup", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ServerSetupPage", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft MauiProgram: registriert die Servermanagement-Services.</summary>
    [Fact]
    public void MauiProgram_RegistersServerManagementServices()
    {
        var program = MauiFile("MauiProgram.cs");

        Assert.Contains("KnownServersService", program, StringComparison.Ordinal);
        Assert.Contains("ServerDiscoveryService", program, StringComparison.Ordinal);
        Assert.Contains("ServerSetupPage", program, StringComparison.Ordinal);
    }

    /// <summary>Prüft EffectHandler: behandelt alle Server-Effekte.</summary>
    [Fact]
    public void EffectHandler_HandlesAllServerEffects()
    {
        var handler = MauiFile("Services", "ShellAppEffectHandler.cs");

        Assert.Contains("LoadServersEffect", handler, StringComparison.Ordinal);
        Assert.Contains("DiscoverServersEffect", handler, StringComparison.Ordinal);
        Assert.Contains("CheckCandidateServerEffect", handler, StringComparison.Ordinal);
        Assert.Contains("PersistActiveServerEffect", handler, StringComparison.Ordinal);
        Assert.Contains("ServersLoadedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("ServerDiscoveryCompletedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("ServerCheckCompletedMessage", handler, StringComparison.Ordinal);
        // Nur nach erfolgreicher Prüfung wird gespeichert (PersistActiveServerEffect
        // wird ausschließlich aus dem Erfolgszweig des MVU-Updates erzeugt).
        Assert.Contains("CheckHealthAsync", MauiFile("Services", "ServerHealthProbe.cs"), StringComparison.Ordinal);
    }

    /// <summary>Prüft Discovery: verwendet das dokumentierte Protokoll.</summary>
    [Fact]
    public void Discovery_UsesDocumentedProtocol()
    {
        var protocol = File.ReadAllText(
            Path.Combine(Root, "VideoPlayer.ApiClient", "Discovery", "DiscoveryProtocol.cs"));
        var csproj = File.ReadAllText(
            Path.Combine(Root, "VideoPlayer.ApiClient", "VideoPlayer.ApiClient.csproj"));

        Assert.Contains("VIDEOWEBPLAYER_DISCOVERY", protocol, StringComparison.Ordinal);
        Assert.Contains("VIDEOWEBPLAYER_SERVER:", protocol, StringComparison.Ordinal);
        Assert.Contains("_http._tcp.local.", protocol, StringComparison.Ordinal);
        Assert.Contains("5001", protocol, StringComparison.Ordinal);
        Assert.Contains("Zeroconf", csproj, StringComparison.Ordinal);
    }

    /// <summary>Prüft KnownServersService: speichert in der Backup-Datenbank.</summary>
    [Fact]
    public void KnownServersService_StoresInBackupDatabase()
    {
        var service = File.ReadAllText(
            Path.Combine(Root, "VideoPlayer.Storage", "KnownServersService.cs"));

        Assert.Contains("BackupDatabase", service, StringComparison.Ordinal);
        Assert.Contains("KnownServerEntry", service, StringComparison.Ordinal);
        Assert.Contains("BackupStateEntry.LastConnectedServerKey", service, StringComparison.Ordinal);
        Assert.DoesNotContain("CacheDatabase", service, StringComparison.Ordinal);
    }
}
