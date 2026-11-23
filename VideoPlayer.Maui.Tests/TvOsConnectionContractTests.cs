using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests der tvOS-Serververbindung (Schritt 3): Da tvOS-Code
/// unter Windows nicht kompiliert werden kann, werden die
/// Verbindungs-Oberflächen (Servereinrichtung, Anmeldemaske,
/// Menü-Einstiege) und die Effekt-Verdrahtung über die Quelldateien
/// geprüft (Muster: <see cref="TvOsAppContractTests"/>).
/// </summary>
public class TvOsConnectionContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string TvOsFile(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root, "VideoPlayer.TvOS" }.Concat(parts).ToArray()));

    /// <summary>
    /// Prüft: die Einrichtungsseite dispatcht sämtliche Messages des
    /// Verbindungsflusses und nutzt fernbedienungstaugliche Eingaben.
    /// </summary>
    [Fact]
    public void ServerSetupPage_DispatchesAllConnectionMessages()
    {
        var page = TvOsFile("Ui", "ServerSetupViewController.cs");

        // Serverwahl und -wechsel
        Assert.Contains("ServerDiscoveryRequestedMessage", page, StringComparison.Ordinal);
        Assert.Contains("DiscoveredServerSelectedMessage", page, StringComparison.Ordinal);
        Assert.Contains("ServerAddressSubmittedMessage", page, StringComparison.Ordinal);
        Assert.Contains("ServerSwitchRequestedMessage", page, StringComparison.Ordinal);

        // Kopplung und Anmeldung
        Assert.Contains("PairingCodeSubmittedMessage", page, StringComparison.Ordinal);
        Assert.Contains("BootstrapTicketSubmittedMessage", page, StringComparison.Ordinal);
        Assert.Contains("LoginSubmittedMessage", page, StringComparison.Ordinal);

        // Nachladen und Wiederholung
        Assert.Contains("LoadServersRequestedMessage", page, StringComparison.Ordinal);
        Assert.Contains("StartupRequestedMessage", page, StringComparison.Ordinal);

        // Fernbedienungstaugliche Eingabe und Listen; die gesicherte
        // Passworteingabe wird über die Fabrik konfiguriert.
        Assert.Contains("UITextField", page, StringComparison.Ordinal);
        Assert.Contains("UITableView", page, StringComparison.Ordinal);
        Assert.Contains("secure: true", page, StringComparison.Ordinal);

        var controls = TvOsFile("Ui", "TvosControls.cs");
        Assert.Contains("SecureTextEntry", controls, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: die Einrichtungsseite rendert sämtliche Modellzustände des
    /// Verbindungsflusses (Listen, aktiver Server, Laufzustände, Stufen,
    /// Fehlertexte).
    /// </summary>
    [Fact]
    public void ServerSetupPage_RendersAllModelStates()
    {
        var page = TvOsFile("Ui", "ServerSetupViewController.cs");

        // Serverbestand und -status
        Assert.Contains("KnownServers", page, StringComparison.Ordinal);
        Assert.Contains("DiscoveredServers", page, StringComparison.Ordinal);
        Assert.Contains("ActiveServerAddress", page, StringComparison.Ordinal);
        Assert.Contains("ServerHealthText", page, StringComparison.Ordinal);

        // Laufzustände
        Assert.Contains("IsDiscoveryRunning", page, StringComparison.Ordinal);
        Assert.Contains("DiscoveryAttempted", page, StringComparison.Ordinal);
        Assert.Contains("IsServerCheckRunning", page, StringComparison.Ordinal);
        Assert.Contains("IsPairingRunning", page, StringComparison.Ordinal);
        Assert.Contains("IsBootstrapRunning", page, StringComparison.Ordinal);
        Assert.Contains("IsLoginRunning", page, StringComparison.Ordinal);

        // Stufenlogik
        Assert.Contains("IsPairingRequired", page, StringComparison.Ordinal);
        Assert.Contains("ConnectionState.NeedsLogin", page, StringComparison.Ordinal);

        // Fehlertexte und Vorbefüllung
        Assert.Contains("ServerSetupError", page, StringComparison.Ordinal);
        Assert.Contains("PairingError", page, StringComparison.Ordinal);
        Assert.Contains("BootstrapError", page, StringComparison.Ordinal);
        Assert.Contains("LoginError", page, StringComparison.Ordinal);
        Assert.Contains("LoginUsername", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: die Einrichtungsseite bietet bei bestehender Verbindung die
    /// Rückkehr zum Menürahmen — per Menü-Taste der Siri Remote und per
    /// sichtbarem Rückweg.
    /// </summary>
    [Fact]
    public void ServerSetupPage_AllowsMenuBackNavigationWhenConnected()
    {
        var page = TvOsFile("Ui", "ServerSetupViewController.cs");

        Assert.Contains("PressesBegan", page, StringComparison.Ordinal);
        Assert.Contains("UIPressType.Menu", page, StringComparison.Ordinal);
        Assert.Contains("ConnectionState.Connected", page, StringComparison.Ordinal);
        Assert.Contains("Zurück zum Menü", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: die Anmeldemaske bietet Zugangsdaten- und Kurzcode-Eingabe
    /// samt Ausstieg zur Servereinrichtung.
    /// </summary>
    [Fact]
    public void LoginPage_ProvidesCredentialAndShortCodeEntry()
    {
        var page = TvOsFile("Ui", "LoginViewController.cs");

        Assert.Contains("LoginSubmittedMessage", page, StringComparison.Ordinal);
        Assert.Contains("BootstrapTicketSubmittedMessage", page, StringComparison.Ordinal);
        Assert.Contains("NavigateToMessage", page, StringComparison.Ordinal);
        Assert.Contains("AppPage.ServerSetup", page, StringComparison.Ordinal);

        // Sichere Passworteingabe (über die Fabrik) und Modellanbindung
        Assert.Contains("secure: true", page, StringComparison.Ordinal);
        Assert.Contains("LoginError", page, StringComparison.Ordinal);
        Assert.Contains("BootstrapError", page, StringComparison.Ordinal);
        Assert.Contains("IsLoginRunning", page, StringComparison.Ordinal);
        Assert.Contains("IsBootstrapRunning", page, StringComparison.Ordinal);
        Assert.Contains("LoginUsername", page, StringComparison.Ordinal);
        Assert.Contains("ActiveServerAddress", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: die Menüseiten bieten „Server wechseln" (Einstellungen) und
    /// „Abmelden" (Profil) und werden vom Tab-Controller instanziiert.
    /// </summary>
    [Fact]
    public void MenuPages_ExposeServerSwitchAndLogout()
    {
        var settings = TvOsFile("Ui", "SettingsPageViewController.cs");
        Assert.Contains("NavigateToMessage", settings, StringComparison.Ordinal);
        Assert.Contains("AppPage.ServerSetup", settings, StringComparison.Ordinal);

        var profile = TvOsFile("Ui", "ProfilePageViewController.cs");
        Assert.Contains("LogoutRequestedMessage", profile, StringComparison.Ordinal);

        var tabBar = TvOsFile("Ui", "MenuTabBarController.cs");
        Assert.Contains("SettingsPageViewController", tabBar, StringComparison.Ordinal);
        Assert.Contains("ProfilePageViewController", tabBar, StringComparison.Ordinal);
        Assert.Contains("IModelRenderable", tabBar, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: der tvOS-Effekt-Handler führt den Abmelde-Effekt über den
    /// Sitzungsdienst aus.
    /// </summary>
    [Fact]
    public void TvOsEffectHandler_HandlesLogoutEffect()
    {
        var handler = TvOsFile("Services", "TvosAppEffectHandler.cs");

        Assert.Contains("case LogoutEffect:", handler, StringComparison.Ordinal);
        Assert.Contains("authSession.LogoutAsync", handler, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: die Kamera-gebundene QR-Scan-Variante des Pairings ist auf
    /// tvOS nicht verdrahtet — nur Code-/Ticket-Texteingabe.
    /// </summary>
    [Fact]
    public void TvOsPages_DoNotWireQrScan()
    {
        foreach (var file in new[]
        {
            "Ui/ServerSetupViewController.cs",
            "Ui/LoginViewController.cs",
            "Services/TvosAppEffectHandler.cs",
            "AppComposition.cs"
        })
        {
            var content = TvOsFile(file.Split('/'));
            Assert.DoesNotContain("QrScan", content, StringComparison.Ordinal);
            Assert.DoesNotContain("ZXing", content, StringComparison.Ordinal);
            Assert.DoesNotContain("AVCapture", content, StringComparison.Ordinal);
        }
    }
}
