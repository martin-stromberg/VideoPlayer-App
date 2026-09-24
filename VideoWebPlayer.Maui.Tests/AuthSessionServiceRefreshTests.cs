#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using System.Text;
using System.Text.Json;
using VideoPlayer.ApiClient;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Tests der Refresh-Token-Sitzungserneuerung des <c>AuthSessionService</c>
/// (Schritt 5b): die stille Neuanmeldung nutzt zuerst das gespeicherte
/// Refresh-Token (<c>api/auth/refresh</c>, rotiert atomar) und fällt erst
/// danach auf die Zugangsdaten zurück; die Abmeldung widerruft den
/// Refresh-Token best-effort (<c>api/auth/logout</c>).
/// </summary>
[Collection("ClientGate")]
public class AuthSessionServiceRefreshTests
{
    private static readonly Uri Server = new("http://testserver/videoplayer/");

    private static AuthSessionService CreateService(
        StubHttpMessageHandler handler,
        out AppSecretsService secrets)
    {
        var service = new AppSecretsService(
            new InMemorySettingsStore(),
            new InMemorySecretStore(),
            AppProfile.Create(null));
        secrets = service;
        return new AuthSessionService(
            service,
            uri => new HttpClient(handler, disposeHandler: false) { BaseAddress = uri },
            new RecordingDelays().Delay);
    }

    private static void EnqueueRefreshOk(
        StubHttpMessageHandler handler,
        string token = "jwt-neu",
        string refreshToken = "refresh-neu") =>
        handler.EnqueueJson(HttpStatusCode.OK, new
        {
            token,
            expires = "2030-01-01T00:00:00Z",
            refreshToken
        });

    /// <summary>
    /// Gate-Probe-Antwort „Gate akzeptiert": die Probe sendet ohne
    /// Anmelde-Token → der Server antwortet mit 400 Model-Validation
    /// (das Gate hat den Schlüssel passieren lassen).
    /// </summary>
    private static void EnqueueGateProbeAccepted(StubHttpMessageHandler handler) =>
        handler.Enqueue(HttpStatusCode.BadRequest);

    /// <summary>
    /// Baut eine aktive Sitzung mit gespeichertem Anmelde- und Refresh-Token
    /// auf (Token-Validierung über <c>api/favorites</c>).
    /// </summary>
    private static async Task<(AuthSessionService Service, AppSecretsService Secrets)> CreateActiveSessionAsync(
        StubHttpMessageHandler handler,
        string refreshToken = "refresh-alt")
    {
        var service = CreateService(handler, out var secrets);
        await secrets.SetAuthTokenAsync("jwt-alt");
        await secrets.SetRefreshTokenAsync(refreshToken);
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<object>()); // api/favorites
        var attempt = await service.TryStoredLoginAsync(Server);
        Assert.Equal(AuthAttemptStatus.Succeeded, attempt.Status);
        Assert.NotNull(service.Client);
        return (service, secrets);
    }

    /// <summary>
    /// Prüft TokenRefresh: ein 401 der Folgeanfrage löst die Erneuerung per
    /// Refresh-Token aus — Anfrage, Persistenz und Wiederholung.
    /// </summary>
    [Fact]
    public async Task TokenRefresh_StoredRefreshToken_RotatesAndPersists()
    {
        var handler = new StubHttpMessageHandler();
        var (service, secrets) = await CreateActiveSessionAsync(handler);
        var expired = 0;
        service.SessionExpired += (_, _) => expired++;

        handler.Enqueue(HttpStatusCode.Unauthorized);        // Folgeanfrage 401
        EnqueueRefreshOk(handler);                            // api/auth/refresh
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<object>()); // Wiederholung

        await service.Client!.GetSourcesAsync();

        // [0] Favorites-Validierung, [1] 401-Folgeanfrage,
        // [2] Refresh-Token-Erneuerung, [3] wiederholte Anfrage.
        Assert.Equal(4, handler.Requests.Count);
        Assert.Equal(HttpMethod.Post, handler.Requests[2].Method);
        Assert.EndsWith("/api/auth/refresh", handler.Requests[2].PathAndQuery);
        var body = JsonDocument.Parse(handler.Requests[2].Body!).RootElement;
        Assert.Equal("refresh-alt", body.GetProperty("refreshToken").GetString());
        Assert.Equal("Bearer jwt-neu", handler.Requests[3].Authorization);
        Assert.Equal("jwt-neu", await secrets.GetAuthTokenAsync());
        Assert.Equal("refresh-neu", await secrets.GetRefreshTokenAsync());
        Assert.Equal(0, expired);
    }

    /// <summary>
    /// Prüft TokenRefresh: ein abgelaufenes gespeichertes Token wird beim
    /// Start direkt über das Refresh-Token erneuert — ohne Zugangsdaten
    /// (Bootstrap-Sitzung) und ohne Anmeldemaske.
    /// </summary>
    [Fact]
    public async Task TryStoredLogin_ExpiredToken_RefreshesViaRefreshToken()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateService(handler, out var secrets);
        await secrets.SetAuthTokenAsync("jwt-alt");
        await secrets.SetRefreshTokenAsync("refresh-alt");

        handler.Enqueue(HttpStatusCode.Unauthorized);        // Token-Validierung 401
        EnqueueRefreshOk(handler);                            // api/auth/refresh
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<object>()); // Favorites-Wiederholung

        var attempt = await service.TryStoredLoginAsync(Server);

        Assert.Equal(AuthAttemptStatus.Succeeded, attempt.Status);
        Assert.Equal("jwt-neu", await secrets.GetAuthTokenAsync());
        Assert.Equal("refresh-neu", await secrets.GetRefreshTokenAsync());
        Assert.Equal(3, handler.Requests.Count);
        Assert.EndsWith("/api/auth/refresh", handler.Requests[1].PathAndQuery);
    }

    /// <summary>
    /// Prüft TokenRefresh: ein vom Server abgelehnter Refresh-Token (Gate
    /// akzeptiert den Geräte-Schlüssel) wird verworfen — danach greifen
    /// die gespeicherten Zugangsdaten.
    /// </summary>
    [Fact]
    public async Task TokenRefresh_RefreshTokenRejected_FallsBackToCredentials()
    {
        var handler = new StubHttpMessageHandler();
        var (service, secrets) = await CreateActiveSessionAsync(handler);
        await secrets.SetCredentialsAsync("martin", "pw");
        var expired = 0;
        service.SessionExpired += (_, _) => expired++;

        handler.Enqueue(HttpStatusCode.Unauthorized);            // Folgeanfrage 401
        handler.Enqueue(HttpStatusCode.Unauthorized);            // api/auth/refresh abgelehnt
        EnqueueGateProbeAccepted(handler);                        // Gate-Probe → Gate ok
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "jwt-login", expires = "2030-01-01T00:00:00Z" });
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<object>()); // Wiederholung

        await service.Client!.GetSourcesAsync();

        // [0] Favorites, [1] 401, [2] refresh-401, [3] Gate-Probe,
        // [4] Zugangsdaten-Login, [5] Wiederholung.
        Assert.Equal(6, handler.Requests.Count);
        Assert.EndsWith("/api/auth/refresh", handler.Requests[2].PathAndQuery);
        Assert.EndsWith("/api/auth/login", handler.Requests[3].PathAndQuery);
        Assert.Equal("{}", handler.Requests[3].Body); // Gate-Probe ohne Body-Felder
        Assert.EndsWith("/api/auth/login", handler.Requests[4].PathAndQuery);
        Assert.Equal("Bearer jwt-login", handler.Requests[5].Authorization);
        // Der abgelehnte Refresh-Token ist verworfen, das Ereignis blieb aus.
        Assert.Null(await secrets.GetRefreshTokenAsync());
        Assert.Equal(0, expired);
    }

    /// <summary>
    /// Prüft TokenRefresh: abgelehnter Refresh-Token ohne Zugangsdaten →
    /// <c>SessionExpired</c> — die Sitzung ist endgültig zu Ende.
    /// </summary>
    [Fact]
    public async Task TokenRefresh_RefreshTokenRejected_WithoutCredentials_RaisesSessionExpired()
    {
        var handler = new StubHttpMessageHandler();
        var (service, secrets) = await CreateActiveSessionAsync(handler);
        var expired = 0;
        service.SessionExpired += (_, _) => expired++;

        handler.Enqueue(HttpStatusCode.Unauthorized);            // Folgeanfrage 401
        handler.Enqueue(HttpStatusCode.Unauthorized);            // refresh abgelehnt
        EnqueueGateProbeAccepted(handler);                        // Gate ok → Token-Problem

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.Client!.GetSourcesAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal(1, expired);
        Assert.Null(await secrets.GetRefreshTokenAsync());
        Assert.Null(await secrets.GetAuthTokenAsync());
    }

    /// <summary>
    /// Prüft TokenRefresh: eine Gate-Ablehnung beim Refresh (widerrufenes
    /// Geräte-Token) bleibt Gerätewiderruf — kein Zugangsdaten-Fallback.
    /// </summary>
    [Fact]
    public async Task TokenRefresh_RefreshRejectedByGate_DoesNotFallback()
    {
        var handler = new StubHttpMessageHandler();
        var (service, secrets) = await CreateActiveSessionAsync(handler);
        await secrets.SetCredentialsAsync("martin", "pw");
        var expired = 0;
        service.SessionExpired += (_, _) => expired++;

        handler.Enqueue(HttpStatusCode.Unauthorized); // Folgeanfrage 401
        handler.Enqueue(HttpStatusCode.Unauthorized); // refresh → Gate-401
        // Gate-Probe → 401: das Gate lehnt den Schlüssel selbst ab.
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(
                "{\"title\":\"Unauthorized\"}", Encoding.UTF8, "application/problem+json"),
        });
        // Auch der Zugangsdaten-Fallback scheitert am selben Gate:
        // Login → Gate-401 → erneute Probe → Gate-401.
        handler.Enqueue(HttpStatusCode.Unauthorized);
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(
                "{\"title\":\"Unauthorized\"}", Encoding.UTF8, "application/problem+json"),
        });

        await Assert.ThrowsAsync<ApiException>(() => service.Client!.GetSourcesAsync());

        // [0] Favorites, [1] 401, [2] refresh-401, [3] Gate-Probe,
        // [4] Login-401, [5] Gate-Probe — kein SessionExpired: der
        // Widerruf meldet sich über das DeviceTokenRejected-Ereignis.
        Assert.Equal(6, handler.Requests.Count);
        Assert.Equal(0, expired);
    }

    /// <summary>
    /// Prüft LogoutAsync: ein vorhandener Refresh-Token wird best-effort
    /// am Server widerrufen (<c>api/auth/logout</c>), bevor die lokalen
    /// Geheimnisse entfernt werden.
    /// </summary>
    [Fact]
    public async Task LogoutAsync_WithRefreshToken_RevokesAtServer()
    {
        var handler = new StubHttpMessageHandler();
        var (service, secrets) = await CreateActiveSessionAsync(handler);
        handler.Enqueue(HttpStatusCode.OK); // api/auth/logout

        await service.LogoutAsync();

        // [0] Favorites-Validierung, [1] Logout.
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.EndsWith("/api/auth/logout", handler.Requests[1].PathAndQuery);
        var body = JsonDocument.Parse(handler.Requests[1].Body!).RootElement;
        Assert.Equal("refresh-alt", body.GetProperty("refreshToken").GetString());
        Assert.Null(service.Client);
        Assert.Null(await secrets.GetAuthTokenAsync());
        Assert.Null(await secrets.GetRefreshTokenAsync());
    }

    /// <summary>
    /// Prüft LogoutAsync: ein nicht erreichbarer Server verhindert die
    /// lokale Abmeldung nicht (best-effort Widerruf).
    /// </summary>
    [Fact]
    public async Task LogoutAsync_ServerUnreachable_StillClearsSecrets()
    {
        var handler = new StubHttpMessageHandler();
        var (service, secrets) = await CreateActiveSessionAsync(handler);
        // Logout schlägt fehl (Retry-Strategie: 4 Versuche).
        for (var i = 0; i < 4; i++)
        {
            handler.Enqueue(HttpStatusCode.InternalServerError);
        }

        await service.LogoutAsync();

        Assert.Null(service.Client);
        Assert.Null(await secrets.GetAuthTokenAsync());
        Assert.Null(await secrets.GetRefreshTokenAsync());
        Assert.False(await secrets.HasCredentialsAsync());
    }
}
