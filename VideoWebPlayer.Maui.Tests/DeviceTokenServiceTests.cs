#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using System.Text;
using VideoPlayer.ApiClient;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Tests für <c>DeviceTokenService</c>: Laden/Aktivieren/Verwerfen des
/// gepaarten Geräte-Tokens, Pflege des <c>ClientGate.DeviceTokenProvider</c>
/// und die Gate-Probe (<c>IsGateRejectedAsync</c>). Der statische
/// Provider-Zustand wird nach jedem Test zurückgesetzt; die Collection
/// serialisiert die Klasse gegen andere Provider-Nutzer.
/// </summary>
[Collection("ClientGate")]
public class DeviceTokenServiceTests : IDisposable
{
    private const string Server = "http://nas/videoplayer/";
    private const string OtherServer = "http://anderer/videoplayer/";

    private static (DeviceTokenService Service, AppSecretsService Secrets) CreateService(
        StubHttpMessageHandler? handler = null)
    {
        var secrets = new AppSecretsService(
            new InMemorySettingsStore(), new InMemorySecretStore(), AppProfile.Create(null));
        var service = handler is null
            ? new DeviceTokenService(secrets)
            : new DeviceTokenService(
                secrets,
                uri => new HttpClient(handler, disposeHandler: false) { BaseAddress = uri });
        return (service, secrets);
    }

    /// <summary>Setzt den globalen Provider-Zustand nach dem Test zurück.</summary>
    public void Dispose() => ClientGate.DeviceTokenProvider = null;

    /// <summary>
    /// Test-Vorrichtung: hinterlegt ein Geräte-Token und aktiviert die
    /// Adresse als aktuellen Server-Kontext (wie ein abgeschlossenes
    /// Pairing mit anschließendem Serverwechsel).
    /// </summary>
    private static async Task PairAsync(
        DeviceTokenService service, AppSecretsService secrets, string serverAddress, string token)
    {
        await secrets.SetDeviceTokenAsync(serverAddress, token);
        await service.LoadForServerAsync(serverAddress);
    }

    /// <summary>Prüft LoadForServerAsync: ein hinterlegtes Token wird über den Provider bereitgestellt.</summary>
    [Fact]
    public async Task LoadForServer_SetsClientGateProvider()
    {
        var (service, secrets) = CreateService();
        await secrets.SetDeviceTokenAsync(Server, "dev-token-1");

        await service.LoadForServerAsync(Server);

        Assert.Equal(Server, service.CurrentServerAddress);
        Assert.Equal("dev-token-1", ClientGate.DeviceTokenProvider?.Invoke());
        Assert.Equal("dev-token-1", ClientGate.Resolve());
    }

    /// <summary>Prüft LoadForServerAsync: ohne Token fällt die Auflösung auf die statischen Quellen zurück.</summary>
    [Fact]
    public async Task LoadForServer_WithoutToken_FallsBackToStatic()
    {
        var (service, _) = CreateService();

        await service.LoadForServerAsync(Server);

        Assert.Equal(Server, service.CurrentServerAddress);
        Assert.NotNull(ClientGate.DeviceTokenProvider);
        Assert.Equal(ClientGate.ResolveStatic(), ClientGate.DeviceTokenProvider!());
        Assert.Equal(ClientGate.ResolveStatic(), ClientGate.Resolve());
    }

    /// <summary>Prüft LoadForServerAsync: ohne Serveradresse bleibt die statische Auflösung aktiv.</summary>
    [Fact]
    public async Task LoadForServer_NullAddress_UsesStaticResolution()
    {
        var (service, _) = CreateService();

        await service.LoadForServerAsync(null);

        Assert.Null(service.CurrentServerAddress);
        Assert.Equal(ClientGate.ResolveStatic(), ClientGate.Resolve());
    }

    /// <summary>Prüft DiscardCurrentAsync: entfernt das Token und setzt den Provider zurück.</summary>
    [Fact]
    public async Task DiscardCurrent_ResetsProviderAndClearsStore()
    {
        var (service, secrets) = CreateService();
        await PairAsync(service, secrets, Server, "dev-token-3");

        await service.DiscardCurrentAsync();

        Assert.Null(service.CurrentServerAddress);
        Assert.False(await secrets.HasDeviceTokenAsync(Server));
        Assert.Equal(ClientGate.ResolveStatic(), ClientGate.Resolve());
    }

    /// <summary>Prüft DiscardIfCurrentAsync: verwirft nur den aktuell bedienten Server.</summary>
    [Fact]
    public async Task DiscardIfCurrent_OnlyDiscardsMatchingServer()
    {
        var (service, secrets) = CreateService();
        await PairAsync(service, secrets, Server, "dev-token-4");

        // Fremde Adresse → no-op.
        Assert.False(await service.DiscardIfCurrentAsync("http://anderer/"));
        Assert.Equal("dev-token-4", await secrets.GetDeviceTokenAsync(Server));
        Assert.Equal("dev-token-4", ClientGate.Resolve());

        // Gleiche Adresse (unnormalisiert) → verworfen.
        Assert.True(await service.DiscardIfCurrentAsync("nas/videoplayer"));
        Assert.False(await secrets.HasDeviceTokenAsync(Server));
        Assert.Equal(ClientGate.ResolveStatic(), ClientGate.Resolve());
    }

    /// <summary>
    /// Prüft SetPairedTokenIfCurrentAsync: bei noch aktuellem Server wird
    /// das Token persistiert und im Provider aktiviert (atomarer
    /// Stale-Guard des Pairing-Austauschs).
    /// </summary>
    [Fact]
    public async Task SetPairedTokenIfCurrent_CurrentServer_StoresAndActivates()
    {
        var (service, secrets) = CreateService();
        await service.LoadForServerAsync(Server);

        Assert.True(await service.SetPairedTokenIfCurrentAsync(Server, "dev-token-new"));

        Assert.Equal("dev-token-new", await secrets.GetDeviceTokenAsync(Server));
        Assert.Equal("dev-token-new", ClientGate.Resolve());
    }

    /// <summary>
    /// Prüft SetPairedTokenIfCurrentAsync: bei zwischenzeitlich
    /// gewechseltem Server bleibt das Token nur unter seiner eigenen
    /// Adresse hinterlegt — der Provider des neuen aktiven Servers wird
    /// nicht zurücküberschrieben.
    /// </summary>
    [Fact]
    public async Task SetPairedTokenIfCurrent_StaleServer_StoresOnlyWithoutActivating()
    {
        var (service, secrets) = CreateService();
        await PairAsync(service, secrets, OtherServer, "dev-token-other");

        // Verspätetes Pairing-Ergebnis des alten Servers.
        Assert.False(await service.SetPairedTokenIfCurrentAsync(Server, "dev-token-old"));

        Assert.Equal("dev-token-old", await secrets.GetDeviceTokenAsync(Server));
        Assert.Equal(OtherServer, service.CurrentServerAddress);
        Assert.Equal("dev-token-other", ClientGate.Resolve());
    }

    /// <summary>Antwort des Client-Gates: 401 mit ProblemDetails-Body (UnauthorizedResult des Action-Filters).</summary>
    private static HttpResponseMessage GateRejection() =>
        new(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(
                "{\"title\":\"Unauthorized\"}", Encoding.UTF8, "application/problem+json"),
        };

    /// <summary>
    /// Prüft IsGateRejectedAsync: die Probe läuft als POST mit leerem
    /// JSON-Body gegen den MauiOnly-gated Login-Endpunkt; 401 mit
    /// application/problem+json → Gate-Ablehnung → true.
    /// </summary>
    [Fact]
    public async Task IsGateRejected_ProblemJsonUnauthorized_ReturnsTrue()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(GateRejection());
        var (service, _) = CreateService(handler);

        Assert.True(await service.IsGateRejectedAsync(Server));

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith("/api/auth/login", request.PathAndQuery);
        Assert.Equal("{}", request.Body);
        // Nur der Gate-Header — kein Bearer-Token.
        Assert.NotNull(request.ApiKey);
        Assert.Null(request.Authorization);
    }

    /// <summary>
    /// Prüft IsGateRejectedAsync: die Probe sendet den Gate-Schlüssel der
    /// geprüften Adresse — nicht den ambienten Provider-Schlüssel des
    /// aktiven Servers (der würde die falsche Identität prüfen und das
    /// Token an eine fremde Adresse weitergeben).
    /// </summary>
    [Fact]
    public async Task IsGateRejected_UsesAddressSpecificToken_NotAmbientProvider()
    {
        var handler = new StubHttpMessageHandler();
        var (service, secrets) = CreateService(handler);
        await secrets.SetDeviceTokenAsync(OtherServer, "dev-token-other");
        // Der ambiente Provider bedient den aktiven Server.
        await PairAsync(service, secrets, Server, "dev-token-active");
        handler.Enqueue(GateRejection());

        Assert.True(await service.IsGateRejectedAsync(OtherServer));

        var request = Assert.Single(handler.Requests);
        Assert.Equal("dev-token-other", request.ApiKey);
    }

    /// <summary>
    /// Prüft IsGateRejectedAsync: ohne hinterlegtes Token der geprüften
    /// Adresse greift die statische Auflösung — auch dann, wenn der
    /// ambiente Provider ein Geräte-Token eines anderen Servers bedient.
    /// </summary>
    [Fact]
    public async Task IsGateRejected_WithoutStoredToken_UsesStaticResolution()
    {
        var handler = new StubHttpMessageHandler();
        var (service, secrets) = CreateService(handler);
        await PairAsync(service, secrets, Server, "dev-token-active");
        handler.Enqueue(GateRejection());

        Assert.True(await service.IsGateRejectedAsync(OtherServer));

        var request = Assert.Single(handler.Requests);
        Assert.Equal(ClientGate.ResolveStatic(), request.ApiKey);
    }

    /// <summary>
    /// Prüft IsGateRejectedAsync: 401 mit text/plain ist ein
    /// Credential-Fehler der Action — der Gate-Schlüssel wurde
    /// akzeptiert → false.
    /// </summary>
    [Fact]
    public async Task IsGateRejected_PlainTextUnauthorized_ReturnsFalse()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("Benutzer nicht gefunden.", Encoding.UTF8, "text/plain"),
        });
        var (service, _) = CreateService(handler);

        Assert.False(await service.IsGateRejectedAsync(Server));
    }

    /// <summary>Prüft IsGateRejectedAsync: 400 (Model-Validation nach passiertem Gate) → false.</summary>
    [Fact]
    public async Task IsGateRejected_BadRequest_ReturnsFalse()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.BadRequest);
        var (service, _) = CreateService(handler);

        Assert.False(await service.IsGateRejectedAsync(Server));
    }

    /// <summary>Prüft IsGateRejectedAsync: 429 → nicht entscheidbar → false (kein falscher Widerruf).</summary>
    [Fact]
    public async Task IsGateRejected_TooManyRequests_ReturnsFalse()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue((HttpStatusCode)429);
        var (service, _) = CreateService(handler);

        Assert.False(await service.IsGateRejectedAsync(Server));
    }

    /// <summary>Prüft IsGateRejectedAsync: erfolgreiche Probe → false.</summary>
    [Fact]
    public async Task IsGateRejected_ReachableProbe_ReturnsFalse()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK);
        var (service, _) = CreateService(handler);

        Assert.False(await service.IsGateRejectedAsync(Server));
    }

    /// <summary>Prüft IsGateRejectedAsync: Transportfehler der Probe → false (kein falscher Widerruf).</summary>
    [Fact]
    public async Task IsGateRejected_TransportError_ReturnsFalse()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueException(new HttpRequestException("Verbindung fehlgeschlagen"));
        var (service, _) = CreateService(handler);

        Assert.False(await service.IsGateRejectedAsync(Server));
    }

    /// <summary>
    /// Prüft IsGateRejectedAsync: ein angeforderter Abbruch wird
    /// durchgereicht — nicht als „nicht abgelehnt" gewertet (der Aufrufer
    /// darf sonst fälschlich den SessionExpired-Pfad beschreiten).
    /// </summary>
    [Fact]
    public async Task IsGateRejected_RequestedCancellation_Propagates()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK);
        var (service, _) = CreateService(handler);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.IsGateRejectedAsync(Server, cts.Token));
    }

    /// <summary>
    /// Prüft IsGateRejectedAsync: eine nicht angeforderte Zeitüberschreitung
    /// (<c>TaskCanceledException</c> ohne Abbruchanforderung) → false.
    /// </summary>
    [Fact]
    public async Task IsGateRejected_UnrequestedTimeout_ReturnsFalse()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueException(new TaskCanceledException("Zeitüberschreitung"));
        var (service, _) = CreateService(handler);

        Assert.False(await service.IsGateRejectedAsync(Server));
    }

    /// <summary>Prüft NotifyRejected: löst das DeviceTokenRejected-Ereignis aus.</summary>
    [Fact]
    public async Task NotifyRejected_RaisesDeviceTokenRejected()
    {
        var (service, secrets) = CreateService();
        var rejected = 0;
        service.DeviceTokenRejected += (_, _) => rejected++;

        // Verwerfen allein löst das Ereignis nicht aus — erst NotifyRejected.
        await PairAsync(service, secrets, Server, "dev-token-5");
        await service.DiscardCurrentAsync();
        Assert.Equal(0, rejected);

        service.NotifyRejected();
        Assert.Equal(1, rejected);
    }
}
