using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests des Verbindungs-Workflows (Schritt 5): Da das Testprojekt
/// keine MAUI-Targets ausführen kann, werden UI-Verdrahtung der Anmeldemaske,
/// Navigation, DI-Registrierung und Effekt-Handler-Zweige über die
/// Quelldateien geprüft (Muster: <see cref="ServerSetupContractTests"/>).
/// </summary>
public class ConnectionWorkflowContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string MauiFile(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root, "VideoPlayer.Maui" }.Concat(parts).ToArray()));

    /// <summary>Prüft LoginPage: existiert mit Zugangsdaten-Eingabefeldern.</summary>
    [Fact]
    public void LoginPage_ExistsWithCredentialsInput()
    {
        var xamlPath = Path.Combine(Root, "VideoPlayer.Maui", "Pages", "LoginPage.xaml");
        Assert.True(File.Exists(xamlPath));
        var xaml = File.ReadAllText(xamlPath);

        // Zugangsdaten-Eingabe: Benutzername + geschütztes Passwortfeld
        Assert.Contains("UsernameEntry", xaml, StringComparison.Ordinal);
        Assert.Contains("PasswordEntry", xaml, StringComparison.Ordinal);
        Assert.Contains("IsPassword=\"True\"", xaml, StringComparison.Ordinal);
        // Aktionen: Anmelden + Ausstieg zur Servereinrichtung
        Assert.Contains("LoginButton", xaml, StringComparison.Ordinal);
        Assert.Contains("ServerSetupButton", xaml, StringComparison.Ordinal);
        // Laufzustand und Inline-Fehlertext
        Assert.Contains("LoginIndicator", xaml, StringComparison.Ordinal);
        Assert.Contains("ErrorLabel", xaml, StringComparison.Ordinal);
        // Anzeige des aktiven Servers (keine interne Kennung als Eingabe)
        Assert.Contains("ServerLabel", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft LoginPage: die Steuerelemente haben AutomationIds.</summary>
    [Fact]
    public void LoginPage_ControlsHaveAutomationIds()
    {
        var page = System.Xml.Linq.XDocument.Load(
            Path.Combine(Root, "VideoPlayer.Maui", "Pages", "LoginPage.xaml"));

        var automationIds = page.Descendants()
            .Select(e => (string?)e.Attribute("AutomationId"))
            .Where(id => id is not null)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var id in new[]
        {
            "LoginUsernameEntry", "LoginPasswordEntry", "LoginButton",
            "LoginErrorLabel", "LoginServerSetupButton", "LoginServerLabel"
        })
        {
            Assert.Contains(id, automationIds);
        }
    }

    /// <summary>Prüft LoginPage: im Code-Behind werden MVU-Nachrichten dispatcht.</summary>
    [Fact]
    public void LoginPage_CodeBehind_DispatchesMvuMessages()
    {
        var code = MauiFile("Pages", "LoginPage.xaml.cs");

        Assert.Contains("LoginSubmittedMessage", code, StringComparison.Ordinal);
        Assert.Contains("NavigateToMessage", code, StringComparison.Ordinal);
        Assert.Contains("AppPage.ServerSetup", code, StringComparison.Ordinal);
        // Keine internen IDs/GUIDs als Benutzereingabe — nur Benutzername/Passwort.
        Assert.DoesNotContain("Guid", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft AppShell: bei <c>RegistersLoginRoute</c> → registriert die Route ohne Flyout-Eintrag.</summary>
    [Fact]
    public void AppShell_RegistersLoginRoute_WithoutFlyoutItem()
    {
        var codeBehind = MauiFile("AppShell.xaml.cs");
        var xaml = MauiFile("AppShell.xaml");

        Assert.Contains("\"login\"", codeBehind, StringComparison.Ordinal);
        Assert.Contains("LoginPage", codeBehind, StringComparison.Ordinal);
        // Kein Menüeintrag für die Anmeldemaske.
        Assert.DoesNotContain("login", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("LoginPage", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft LoadingPage: bindet den Startup-Statustext.</summary>
    [Fact]
    public void LoadingPage_BindsStartupStatusText()
    {
        var xaml = MauiFile("Pages", "LoadingPage.xaml");

        Assert.Contains("ActivityIndicator", xaml, StringComparison.Ordinal);
        Assert.Contains("Model.StartupStatusText", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft HomePage: zeigt das Offline-Banner.</summary>
    [Fact]
    public void HomePage_ShowsOfflineBanner()
    {
        var xaml = MauiFile("Pages", "HomePage.xaml");
        var code = MauiFile("Pages", "HomePage.xaml.cs");

        Assert.Contains("OfflineBanner", xaml, StringComparison.Ordinal);
        Assert.Contains("Offline-Modus", xaml, StringComparison.Ordinal);
        Assert.Contains("model.IsOffline", code, StringComparison.Ordinal);
        Assert.Contains("ModelChanged", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft MauiProgram: registriert die Auth-Services und die Login-Seite.</summary>
    [Fact]
    public void MauiProgram_RegistersAuthServicesAndLoginPage()
    {
        var program = MauiFile("MauiProgram.cs");

        Assert.Contains("AuthSessionService", program, StringComparison.Ordinal);
        Assert.Contains("LoginPage", program, StringComparison.Ordinal);
        Assert.Contains("AppSecretsService", program, StringComparison.Ordinal);
    }

    /// <summary>Prüft EffectHandler: behandelt alle Connection-Effekte.</summary>
    [Fact]
    public void EffectHandler_HandlesAllConnectionEffects()
    {
        var handler = MauiFile("Services", "ShellAppEffectHandler.cs");

        // Neue Effekt-Zweige des Verbindungs-Workflows
        Assert.Contains("AttemptAutoLoginEffect", handler, StringComparison.Ordinal);
        Assert.Contains("AttemptLoginEffect", handler, StringComparison.Ordinal);
        Assert.Contains("CheckOfflineDataEffect", handler, StringComparison.Ordinal);
        // Rückdispatch der Ergebnis-Messages
        Assert.Contains("AutoLoginCompletedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("LoginCompletedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("OfflineDataCheckCompletedMessage", handler, StringComparison.Ordinal);
        // Sitzungsablauf → Anmeldezustand
        Assert.Contains("SessionExpired", handler, StringComparison.Ordinal);
        Assert.Contains("SessionExpiredMessage", handler, StringComparison.Ordinal);
        // Navigation zur Anmeldemaske
        Assert.Contains("AppPage.Login", handler, StringComparison.Ordinal);
        Assert.Contains("LoginPage", handler, StringComparison.Ordinal);
        // Offline-Daten-Prüfung über die Cache-Ablage
        Assert.Contains("HasOfflineDataAsync", handler, StringComparison.Ordinal);
    }

    /// <summary>Prüft AuthSessionService: verdrahtet TokenRefresher und sicheren Storage.</summary>
    [Fact]
    public void AuthSessionService_WiresTokenRefresherAndSecureStorage()
    {
        var service = File.ReadAllText(
            Path.Combine(Root, "VideoPlayer.Mvu", "AuthSessionService.cs"));

        Assert.Contains("TokenRefresher", service, StringComparison.Ordinal);
        Assert.Contains("AppSecretsService", service, StringComparison.Ordinal);
        Assert.Contains("SetAuthTokenAsync", service, StringComparison.Ordinal);
        Assert.Contains("HasCredentialsAsync", service, StringComparison.Ordinal);
        Assert.Contains("SessionExpired", service, StringComparison.Ordinal);
        Assert.Contains("api/auth/login", File.ReadAllText(
            Path.Combine(Root, "VideoPlayer.ApiClient", "VideoPlayerApiClient.cs")),
            StringComparison.Ordinal);
    }
}
