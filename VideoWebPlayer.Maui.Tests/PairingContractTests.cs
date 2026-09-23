using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests des Geräte-Pairings: Da das Testprojekt die MAUI-Targets
/// nicht referenzieren kann, werden UI-Verdrahtung, DI-Registrierung,
/// Effekt-Behandlung und das Deployment-Skript über die Quelldateien geprüft
/// (Muster: <see cref="ServerSetupContractTests"/>).
/// </summary>
public class PairingContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string MauiFile(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root, "VideoPlayer.Maui" }.Concat(parts).ToArray()));

    /// <summary>Prüft ServerSetupPage: enthält den Pairing-Abschnitt mit allen Steuerelementen.</summary>
    [Fact]
    public void ServerSetupPage_ContainsPairingSection()
    {
        var xaml = MauiFile("Pages", "ServerSetupPage.xaml");

        Assert.Contains("PairingSection", xaml, StringComparison.Ordinal);
        Assert.Contains("PairingCodeEntry", xaml, StringComparison.Ordinal);
        Assert.Contains("PairButton", xaml, StringComparison.Ordinal);
        Assert.Contains("PairingIndicator", xaml, StringComparison.Ordinal);
        Assert.Contains("PairingErrorLabel", xaml, StringComparison.Ordinal);
        // Die Code-Länge ist serverseitig konfigurierbar (Pairing:CodeLength)
        // — das Feld darf keine Längenbegrenzung setzen, die längere
        // Codes still abschneiden würde.
        Assert.DoesNotContain("MaxLength=", xaml, StringComparison.Ordinal);
        // Rechtschreibprüfung/Wortvorschläge dürfen die Code-Eingabe nicht
        // unbemerkt verändern.
        Assert.Contains("IsSpellCheckEnabled=\"False\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsTextPredictionEnabled=\"False\"", xaml, StringComparison.Ordinal);
        // Der Hinweistext benennt die Herkunft (Server-Verwaltung), ohne
        // eine feste Stellenzahl zu behaupten.
        Assert.Contains("Verwaltung", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("-stellig", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft ServerSetupPage: die Pairing-Steuerelemente haben AutomationIds.</summary>
    [Fact]
    public void ServerSetupPage_PairingControlsHaveAutomationIds()
    {
        var page = System.Xml.Linq.XDocument.Load(
            Path.Combine(Root, "VideoPlayer.Maui", "Pages", "ServerSetupPage.xaml"));

        var automationIds = page.Descendants()
            .Select(e => (string?)e.Attribute("AutomationId"))
            .Where(id => id is not null)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var id in new[] { "PairingCodeEntry", "PairButton", "PairingErrorLabel" })
        {
            Assert.Contains(id, automationIds);
        }
    }

    /// <summary>Prüft ServerSetupPage: der Pair-Button dispatcht die MVU-Nachricht.</summary>
    [Fact]
    public void ServerSetupPage_PairButton_DispatchesPairingCodeSubmitted()
    {
        var code = MauiFile("Pages", "ServerSetupPage.xaml.cs");
        var xaml = MauiFile("Pages", "ServerSetupPage.xaml");

        Assert.Contains("OnPairClicked", code, StringComparison.Ordinal);
        Assert.Contains("Clicked=\"OnPairClicked\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PairingCodeSubmittedMessage", code, StringComparison.Ordinal);
        // Der Abschnitt wird ausschließlich über den MVU-Model-Zustand gesteuert.
        Assert.Contains("model.IsPairingRequired", code, StringComparison.Ordinal);
        Assert.Contains("model.IsPairingRunning", code, StringComparison.Ordinal);
        Assert.Contains("model.PairingError", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft EffectHandler: behandelt den Pairing-Austausch und den Widerruf.</summary>
    [Fact]
    public void EffectHandler_HandlesPairingEffects()
    {
        var handler = MauiFile("Services", "ShellAppEffectHandler.cs");

        Assert.Contains("ExchangePairingCodeEffect", handler, StringComparison.Ordinal);
        Assert.Contains("DevicePairingClient", handler, StringComparison.Ordinal);
        Assert.Contains("PairingCompletedMessage", handler, StringComparison.Ordinal);
        // Widerruf-Pfad: Ereignis → MVU-Nachricht.
        Assert.Contains("DeviceTokenRejected", handler, StringComparison.Ordinal);
        Assert.Contains("DeviceTokenRevokedMessage", handler, StringComparison.Ordinal);
        // Persistenz + Provider-Aktivierung nach erfolgreichem Pairing
        // (atomar gegen Serverwechsel, siehe GuardsStaleContext).
        Assert.Contains("SetDeviceTokenAsync", handler, StringComparison.Ordinal);
        Assert.Contains("SetPairedTokenIfCurrentAsync", handler, StringComparison.Ordinal);
        Assert.Contains("SetApiKey", handler, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft EffectHandler: der Pairing-Austausch aktiviert Provider und
    /// API-Client-Schlüssel nur bei noch aktuellem Pairing-Kontext
    /// (Stale-Guard gegen Serverwechsel während des Austauschs).
    /// </summary>
    [Fact]
    public void EffectHandler_PairingExchange_GuardsStaleContext()
    {
        var handler = MauiFile("Services", "ShellAppEffectHandler.cs");

        Assert.Contains("model.IsPairingRunning", handler, StringComparison.Ordinal);
        Assert.Contains("model.ActiveServerAddress", handler, StringComparison.Ordinal);
        // Die eigentliche Aktivierung ist atomar im DeviceTokenService —
        // der Model-Check bleibt nur schneller Filter (kein Check-then-act).
        Assert.Contains("SetPairedTokenIfCurrentAsync", handler, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft EffectHandler: der Health-Check bildet eine Gate-Ablehnung auf
    /// Token-Verwerfen + einmalige Nachprüfung + Re-Pairing-Meldung ab.
    /// </summary>
    [Fact]
    public void EffectHandler_HealthCheck_HandlesGateRejected()
    {
        var handler = MauiFile("Services", "ShellAppEffectHandler.cs");

        Assert.Contains("HealthCheckResult.GateRejected", handler, StringComparison.Ordinal);
        Assert.Contains("DiscardCurrentAsync", handler, StringComparison.Ordinal);
        Assert.Contains("NotifyRejected", handler, StringComparison.Ordinal);
        // Kandidatenprüfung: eine Gate-Ablehnung zählt als Erreichbarkeit.
        Assert.Contains("DiscardIfCurrentAsync", handler, StringComparison.Ordinal);
    }

    /// <summary>Prüft MauiProgram: registriert die Pairing-Dienste.</summary>
    [Fact]
    public void MauiProgram_RegistersPairingServices()
    {
        var program = MauiFile("MauiProgram.cs");

        Assert.Contains("DevicePairingClient", program, StringComparison.Ordinal);
        Assert.Contains("DeviceTokenService", program, StringComparison.Ordinal);
        // Der injizierte HttpClient trägt ein explizites Timeout —
        // konsistent zu den übrigen Clients (kein 100-s-Default).
        Assert.Contains("Timeout", program, StringComparison.Ordinal);
        Assert.Contains("DevicePairingClient.RequestTimeout", program, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft DevicePairingClient: spricht den öffentlichen Endpunkt ohne
    /// Gate-/Bearer-Header und ohne Retry-Pipeline an.
    /// </summary>
    [Fact]
    public void DevicePairingClient_UsesPublicEndpoint_WithoutAuthHeaders()
    {
        var client = File.ReadAllText(
            Path.Combine(Root, "VideoPlayer.ApiClient", "DevicePairingClient.cs"));

        Assert.Contains("api/pairing/exchange", client, StringComparison.Ordinal);
        // Kein Client-Gate-Header am öffentlichen Endpunkt.
        Assert.DoesNotContain("ClientGate.HeaderName", client, StringComparison.Ordinal);
        Assert.DoesNotContain("Authorization", client, StringComparison.Ordinal);
        // Kein Retry-/Reauth-Pipeline-Aufruf.
        Assert.DoesNotContain("SendWithPolicyAsync", client, StringComparison.Ordinal);
    }

    /// <summary>Prüft iOS-Deployment.ps1: propagiert und maskiert den API-Token.</summary>
    [Fact]
    public void IosDeploymentScript_PropagatesAndMasksApiToken()
    {
        var script = File.ReadAllText(
            Path.Combine(Root, "scripts", "iOS-Deployment.ps1"));

        Assert.Contains("VideoPlayerApiToken", script, StringComparison.Ordinal);
        Assert.Contains("VIDEOWEBPLAYER_MAUI_API_TOKEN", script, StringComparison.Ordinal);
        // Der Token wird in der Konsolenanzeige maskiert.
        Assert.Contains("$Name -eq \"VideoPlayerApiToken\"", script, StringComparison.Ordinal);
    }

    /// <summary>Prüft ServerHealthProbe: liefert das dreiwertige Ergebnis.</summary>
    [Fact]
    public void ServerHealthProbe_ReturnsHealthCheckResult()
    {
        var probe = MauiFile("Services", "ServerHealthProbe.cs");
        var contract = MauiFile("Services", "IServerHealthProbe.cs");

        Assert.Contains("Task<HealthCheckResult>", contract, StringComparison.Ordinal);
        Assert.Contains("CheckHealthAsync", probe, StringComparison.Ordinal);
        Assert.Contains("HealthCheckResult.Unreachable", probe, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft DeviceTokenService: die Gate-Probe läuft gegen den einzigen
    /// MauiOnly-gated Endpunkt POST api/auth/login (leerer JSON-Body, nur
    /// X-API-Key) und erkennt die Gate-Ablehnung am Content-Type
    /// application/problem+json der 401-Antwort — der Health-Endpunkt ist
    /// serverseitig nicht gated und kann eine Ablehnung nicht erkennen.
    /// </summary>
    [Fact]
    public void DeviceTokenService_GateProbe_UsesLoginEndpoint()
    {
        var service = File.ReadAllText(
            Path.Combine(Root, "VideoPlayer.Mvu", "DeviceTokenService.cs"));

        Assert.Contains("api/auth/login", service, StringComparison.Ordinal);
        Assert.Contains("HttpMethod.Post", service, StringComparison.Ordinal);
        Assert.Contains("application/problem+json", service, StringComparison.Ordinal);
        Assert.Contains("ClientGate.HeaderName", service, StringComparison.Ordinal);
        // Der Gate-Schlüssel wird adressspezifisch aufgelöst — nicht über
        // den ambienten Provider (Token des aktiven Servers an eine fremde
        // Adresse wäre eine Credential-Weitergabe).
        Assert.Contains("ResolveGateKeyAsync", service, StringComparison.Ordinal);
        Assert.Contains("ClientGate.ResolveStatic()", service, StringComparison.Ordinal);
        Assert.DoesNotContain("ClientGate.Resolve()", service, StringComparison.Ordinal);
        // Der ungeschützte Health-Endpunkt ist kein Probe-Ziel mehr.
        Assert.DoesNotContain("api/health", service, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft ServerHealthProbe: die Kandidatenprüfung löst den Gate-Schlüssel
    /// adressspezifisch auf (hinterlegtes Geräte-Token des Kandidaten bzw.
    /// statische Quelle) — der aktive DeviceTokenProvider darf sein Token
    /// nicht an fremde Adressen senden.
    /// </summary>
    [Fact]
    public void ServerHealthProbe_ResolvesGateKeyPerAddress()
    {
        var probe = MauiFile("Services", "ServerHealthProbe.cs");

        Assert.Contains("GetDeviceTokenAsync", probe, StringComparison.Ordinal);
        Assert.Contains("ClientGate.ResolveStatic()", probe, StringComparison.Ordinal);
        Assert.Contains("ApiKey", probe, StringComparison.Ordinal);
        // Keine ambient-provider-Auflösung: ClientGate.Resolve() würde das
        // Geräte-Token des aktiven Servers an beliebige Adressen senden.
        Assert.DoesNotContain("ClientGate.Resolve()", probe, StringComparison.Ordinal);
    }
}
