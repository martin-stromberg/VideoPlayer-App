using VideoPlayer.Mvu;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests des QR-/Ticket-Onboardings (Schritt 5b): Da das
/// Testprojekt die MAUI-Targets nicht referenzieren kann, werden
/// UI-Verdrahtung, Plattform-Konfiguration, Effekt-Behandlung und die
/// Geheimhaltung der Dev-API über die Quelldateien geprüft
/// (Muster: <see cref="PairingContractTests"/>).
/// </summary>
public class BootstrapContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string MauiFile(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root, "VideoPlayer.Maui" }.Concat(parts).ToArray()));

    /// <summary>Prüft ServerSetupPage: enthält die Kopplungs-Karte mit allen Steuerelementen.</summary>
    [Fact]
    public void ServerSetupPage_ContainsBootstrapSection()
    {
        var xaml = MauiFile("Pages", "ServerSetupPage.xaml");

        Assert.Contains("BootstrapSection", xaml, StringComparison.Ordinal);
        Assert.Contains("QrScanButton", xaml, StringComparison.Ordinal);
        Assert.Contains("BootstrapTicketEntry", xaml, StringComparison.Ordinal);
        Assert.Contains("BootstrapButton", xaml, StringComparison.Ordinal);
        Assert.Contains("BootstrapIndicator", xaml, StringComparison.Ordinal);
        Assert.Contains("BootstrapRunningLabel", xaml, StringComparison.Ordinal);
        Assert.Contains("BootstrapErrorLabel", xaml, StringComparison.Ordinal);
        // Das Ticket ist opak und beliebig lang — kein MaxLength, das es
        // abschneiden würde; keine Rechtschreibkorrektur auf dem Geheimnis.
        Assert.DoesNotContain("MaxLength=", xaml, StringComparison.Ordinal);
        Assert.Contains("IsSpellCheckEnabled=\"False\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsTextPredictionEnabled=\"False\"", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft ServerSetupPage: die Kopplungs-Steuerelemente haben AutomationIds.</summary>
    [Fact]
    public void ServerSetupPage_BootstrapControlsHaveAutomationIds()
    {
        var page = System.Xml.Linq.XDocument.Load(
            Path.Combine(Root, "VideoPlayer.Maui", "Pages", "ServerSetupPage.xaml"));

        var automationIds = page.Descendants()
            .Select(e => (string?)e.Attribute("AutomationId"))
            .Where(id => id is not null)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var id in new[]
        {
            "BootstrapSection", "QrScanButton", "BootstrapTicketEntry",
            "BootstrapButton", "BootstrapRunningLabel", "BootstrapErrorLabel"
        })
        {
            Assert.Contains(id, automationIds);
        }
    }

    /// <summary>Prüft ServerSetupPage: der Einlöse-Button dispatcht die MVU-Nachricht; Scan nur mobil.</summary>
    [Fact]
    public void ServerSetupPage_BootstrapButton_DispatchesTicketSubmitted()
    {
        var code = MauiFile("Pages", "ServerSetupPage.xaml.cs");
        var xaml = MauiFile("Pages", "ServerSetupPage.xaml");

        Assert.Contains("OnBootstrapClicked", code, StringComparison.Ordinal);
        Assert.Contains("Clicked=\"OnBootstrapClicked\"", xaml, StringComparison.Ordinal);
        Assert.Contains("BootstrapTicketSubmittedMessage", code, StringComparison.Ordinal);
        // Der Abschnitt wird ausschließlich über den MVU-Model-Zustand gesteuert.
        Assert.Contains("model.IsBootstrapRunning", code, StringComparison.Ordinal);
        Assert.Contains("model.BootstrapError", code, StringComparison.Ordinal);
        // Der Scan-Button bleibt eine Mobile-Funktion.
        Assert.Contains("#if ANDROID || IOS", code, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"false\"", xaml, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft LoginPage: der Kurzcode-Einstieg liegt auf der Anmeldemaske —
    /// er wird gegen den bereits verbundenen Server eingelöst (der QR-Code
    /// mit der Serveradresse bleibt dagegen bei der Serververbindung).
    /// Pinnt die Workflow-Korrektur: Kopplung ohne Umweg über
    /// „Anderen Server einrichten".
    /// </summary>
    [Fact]
    public void LoginPage_OffersShortCodeBootstrap()
    {
        var xaml = MauiFile("Pages", "LoginPage.xaml");
        var code = MauiFile("Pages", "LoginPage.xaml.cs");

        Assert.Contains("LoginBootstrapCodeEntry", xaml, StringComparison.Ordinal);
        Assert.Contains("LoginBootstrapButton", xaml, StringComparison.Ordinal);
        Assert.Contains("LoginBootstrapErrorLabel", xaml, StringComparison.Ordinal);
        Assert.Contains("OnBootstrapClicked", code, StringComparison.Ordinal);
        Assert.Contains("BootstrapTicketSubmittedMessage", code, StringComparison.Ordinal);
        Assert.Contains("model.IsBootstrapRunning", code, StringComparison.Ordinal);
        Assert.Contains("model.BootstrapError", code, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft ServerSetupPage: ein neuer Kopplungsfehler wird zusätzlich
    /// zum Label als Dialog gezeigt — nach dem QR-Scan kehrt die modale
    /// Scanner-Seite hierher zurück und ein Fehler darf nicht unbemerkt
    /// untergehen.
    /// </summary>
    [Fact]
    public void ServerSetupPage_AlertsOnNewBootstrapError()
    {
        var code = MauiFile("Pages", "ServerSetupPage.xaml.cs");

        Assert.Contains("DisplayAlert", code, StringComparison.Ordinal);
        Assert.Contains("lastBootstrapError", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft EffectHandler: behandelt Serverprüfung und Ticket-Austausch.</summary>
    [Fact]
    public void EffectHandler_HandlesBootstrapEffects()
    {
        var handler = MauiFile("Services", "ShellAppEffectHandler.cs");

        Assert.Contains("CheckBootstrapServerEffect", handler, StringComparison.Ordinal);
        Assert.Contains("ExchangeBootstrapTicketEffect", handler, StringComparison.Ordinal);
        Assert.Contains("BootstrapServerCheckedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("BootstrapCompletedMessage", handler, StringComparison.Ordinal);
        // Persistenz der Bootstrap-Session: Geräte-Token + JWT + Refresh-Token.
        Assert.Contains("SetAuthTokenAsync", handler, StringComparison.Ordinal);
        Assert.Contains("SetRefreshTokenAsync", handler, StringComparison.Ordinal);
        Assert.Contains("SetPairedTokenIfCurrentAsync", handler, StringComparison.Ordinal);
    }

    /// <summary>Prüft MauiProgram/csproj: ZXing-Initialisierung und Paket nur für Mobile-Targets.</summary>
    [Fact]
    public void MauiProgram_ZxingRegistration_IsMobileOnly()
    {
        var program = MauiFile("MauiProgram.cs");
        var csproj = File.ReadAllText(
            Path.Combine(Root, "VideoPlayer.Maui", "VideoPlayer.Maui.csproj"));

        Assert.Contains("UseBarcodeReader", program, StringComparison.Ordinal);
        Assert.Contains("#if ANDROID || IOS", program, StringComparison.Ordinal);
        Assert.Contains("ZXing.Net.Maui.Controls", csproj, StringComparison.Ordinal);
        // Windows trägt keine Kamera-Abhängigkeit — das Paket ist auf
        // die Mobil-Targets konditioniert.
        Assert.Contains("net10.0-android", csproj, StringComparison.Ordinal);
        Assert.Contains("net10.0-ios", csproj, StringComparison.Ordinal);
    }

    /// <summary>Prüft Plattform-Manifeste: Kamera-Permission und Verwendungsbeschreibung.</summary>
    [Fact]
    public void PlatformManifests_DeclareCameraUsage()
    {
        var manifest = MauiFile("Platforms", "Android", "AndroidManifest.xml");
        var infoPlist = MauiFile("Platforms", "iOS", "Info.plist");

        Assert.Contains("android.permission.CAMERA", manifest, StringComparison.Ordinal);
        Assert.Contains("NSCameraUsageDescription", infoPlist, StringComparison.Ordinal);
    }

    /// <summary>Prüft QrScannerPage: existiert, ist mobile-only und dispatcht in den MVU-Fluss.</summary>
    [Fact]
    public void QrScannerPage_IsMobileOnly_AndDispatches()
    {
        var page = MauiFile("Pages", "QrScannerPage.cs");

        Assert.StartsWith("#if ANDROID || IOS", page.TrimStart(), StringComparison.Ordinal);
        Assert.Contains("CameraBarcodeReaderView", page, StringComparison.Ordinal);
        Assert.Contains("BarcodesDetected", page, StringComparison.Ordinal);
        Assert.Contains("BootstrapTicketSubmittedMessage", page, StringComparison.Ordinal);
        // Der Code fließt in die Kopplungs-Eingabe — niemals direkt an
        // einen Server oder in eine Ablage.
        Assert.DoesNotContain("HttpClient", page, StringComparison.Ordinal);
        Assert.DoesNotContain("SetAuthToken", page, StringComparison.Ordinal);
    }

    /// <summary>Prüft BootstrapTicketParser: liegt im plattformneutralen Mvu-Projekt, normalisiert das Ticket nicht.</summary>
    [Fact]
    public void BootstrapTicketParser_LivesInMvu_AndKeepsTicketOpaque()
    {
        var parser = File.ReadAllText(
            Path.Combine(Root, "VideoPlayer.Mvu", "BootstrapTicketParser.cs"));

        Assert.Contains("namespace VideoPlayer.Mvu", parser, StringComparison.Ordinal);
        // Das Ticket darf nicht durch die PairingCode-Normalisierung —
        // Server-Secrets sind case-sensitiv und beliebig geformt.
        Assert.DoesNotContain("PairingCode.TryNormalize", parser, StringComparison.Ordinal);
        Assert.DoesNotContain("ToUpperInvariant", parser, StringComparison.Ordinal);
    }

    /// <summary>Prüft DevApiModelSnapshot: spiegelt den Bootstrap-Zustand ohne Secrets.</summary>
    [Fact]
    public void DevApiModelSnapshot_ExposesBootstrapState_WithoutSecrets()
    {
        var snapshot = File.ReadAllText(
            Path.Combine(Root, "VideoPlayer.Mvu", "DevApi", "DevApiModelSnapshot.cs"));

        Assert.Contains("IsBootstrapRunning", snapshot, StringComparison.Ordinal);
        Assert.Contains("BootstrapError", snapshot, StringComparison.Ordinal);
        // Tickets, Tokens und Schlüssel gehören nicht in den Snapshot —
        // als Property würden sie an den Dev-API-Snapshot leaken.
        Assert.DoesNotContain("public string Ticket", snapshot, StringComparison.Ordinal);
        Assert.DoesNotContain("public string? Ticket", snapshot, StringComparison.Ordinal);
        Assert.DoesNotContain("public string Token", snapshot, StringComparison.Ordinal);
        Assert.DoesNotContain("public string? Token", snapshot, StringComparison.Ordinal);
        Assert.DoesNotContain("public string? RefreshToken", snapshot, StringComparison.Ordinal);
    }
}
