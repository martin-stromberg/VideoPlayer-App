#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using System.Text;
using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Tests des Anmelde-Dienstes (Schritt 5): automatische und manuelle
/// Anmeldung mit Ergebnis-Mapping sowie stille Neuanmeldung (TokenRefresher)
/// inklusive <c>SessionExpired</c>-Ereignis bei fehlenden oder abgelehnten
/// Zugangsdaten. Die Collection serialisiert die Klasse gegen andere
/// Nutzer des statischen <c>ClientGate.DeviceTokenProvider</c>.
/// </summary>
[Collection("ClientGate")]
public class AuthSessionServiceTests
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
        // disposeHandler: false — Sitzungs- und Refresh-Client teilen sich den
        // Stub-Handler; der Refresh-Client wird pro Aufruf verworfen.
        // RecordingDelays überspringt die echten Wartezeiten der Retry-Strategie.
        return new AuthSessionService(
            service,
            uri => new HttpClient(handler, disposeHandler: false) { BaseAddress = uri },
            new RecordingDelays().Delay);
    }

    private static void EnqueueLoginOk(StubHttpMessageHandler handler, string token = "jwt-token") =>
        handler.EnqueueJson(HttpStatusCode.OK, new { token, expires = "2030-01-01T00:00:00Z" });

    /// <summary>
    /// Reiht eine Gate-Ablehnung der Probe ein: 401 mit
    /// ProblemDetails-Body (<c>application/problem+json</c>) — das
    /// UnauthorizedResult des Action-Filters am gated Login-Endpunkt.
    /// </summary>
    private static void EnqueueGateProbeRejected(StubHttpMessageHandler handler) =>
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(
                "{\"title\":\"Unauthorized\"}", Encoding.UTF8, "application/problem+json"),
        });

    /// <summary>Prüft TryStoredLogin: ohne Zugangsdaten → gibt MissingCredentials zurück.</summary>
    [Fact]
    public async Task TryStoredLogin_WithoutCredentials_ReturnsMissingCredentials()
    {
        var service = CreateService(new StubHttpMessageHandler(), out _);

        var attempt = await service.TryStoredLoginAsync(Server);

        Assert.Equal(AuthAttemptStatus.MissingCredentials, attempt.Status);
        Assert.Null(service.Client);
    }

    /// <summary>Prüft TryStoredLogin: bei nur gespeichertem Benutzernamen → gibt MissingCredentials mit Benutzername zurück.</summary>
    [Fact]
    public async Task TryStoredLogin_UsernameOnly_ReturnsMissingCredentialsWithUsername()
    {
        // Backup-Wiederherstellung: der nicht geheime Benutzername bleibt
        // erhalten (normale Ablage), das Passwort fehlt (sichere Ablage) —
        // die Anmeldemaske wird mit dem bekannten Namen vorbefüllt.
        var settings = new InMemorySettingsStore();
        settings.Set(AppSecretsService.UsernameKey, "martin");
        var secretService = new AppSecretsService(
            settings, new InMemorySecretStore(), AppProfile.Create(null));
        var service = new AuthSessionService(
            secretService,
            uri => new HttpClient(new StubHttpMessageHandler(), disposeHandler: false) { BaseAddress = uri });

        var attempt = await service.TryStoredLoginAsync(Server);

        Assert.Equal(AuthAttemptStatus.MissingCredentials, attempt.Status);
        Assert.Equal("martin", attempt.Username);
    }

    /// <summary>Prüft TryStoredLogin: bei Erfolg → speichert das Token und stellt den Client bereit.</summary>
    [Fact]
    public async Task TryStoredLogin_Success_StoresTokenAndProvidesClient()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateService(handler, out var secrets);
        await secrets.SetCredentialsAsync("martin", "pw");
        EnqueueLoginOk(handler, "jwt-stored");

        var attempt = await service.TryStoredLoginAsync(Server);

        Assert.Equal(AuthAttemptStatus.Succeeded, attempt.Status);
        Assert.Equal("martin", attempt.Username);
        Assert.NotNull(service.Client);
        Assert.Equal(Server.ToString(), service.ClientBaseAddress);
        Assert.Equal("jwt-stored", await secrets.GetAuthTokenAsync());
    }

    /// <summary>Prüft TryStoredLogin: ein gültiges gespeichertes Token wird direkt wiederverwendet (kein Login-Request).</summary>
    [Fact]
    public async Task TryStoredLogin_ValidStoredToken_ReusesTokenWithoutLogin()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateService(handler, out var secrets);
        await secrets.SetCredentialsAsync("martin", "pw");
        await secrets.SetAuthTokenAsync("jwt-vorhanden");
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<object>()); // api/favorites

        var attempt = await service.TryStoredLoginAsync(Server);

        Assert.Equal(AuthAttemptStatus.Succeeded, attempt.Status);
        Assert.Equal("martin", attempt.Username);
        Assert.NotNull(service.Client);
        // Genau eine Anfrage: die Token-Validierung — kein Login-Request.
        var request = Assert.Single(handler.Requests);
        Assert.EndsWith("/api/favorites", request.PathAndQuery);
        Assert.Equal("Bearer jwt-vorhanden", request.Authorization);
    }

    /// <summary>Prüft TryStoredLogin: ein abgelehntes Token fällt über die stille Neuanmeldung auf die Zugangsdaten zurück.</summary>
    [Fact]
    public async Task TryStoredLogin_RejectedStoredToken_FallsBackToCredentials()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateService(handler, out var secrets);
        await secrets.SetCredentialsAsync("martin", "pw");
        await secrets.SetAuthTokenAsync("jwt-abgelaufen");
        handler.Enqueue(HttpStatusCode.Unauthorized);          // api/favorites mit altem Token
        EnqueueLoginOk(handler, "jwt-neu");                    // stille Neuanmeldung
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<object>()); // Wiederholung api/favorites

        var attempt = await service.TryStoredLoginAsync(Server);

        Assert.Equal(AuthAttemptStatus.Succeeded, attempt.Status);
        Assert.NotNull(service.Client);
        Assert.Equal(3, handler.Requests.Count);
        Assert.EndsWith("/api/favorites", handler.Requests[0].PathAndQuery);
        Assert.Equal("Bearer jwt-abgelaufen", handler.Requests[0].Authorization);
        Assert.EndsWith("/api/auth/login", handler.Requests[1].PathAndQuery);
        Assert.Equal("Bearer jwt-neu", handler.Requests[2].Authorization);
        Assert.Equal("jwt-neu", await secrets.GetAuthTokenAsync());
    }

    /// <summary>Prüft TryStoredLogin: Token vorhanden, aber keine Zugangsdaten (Backup-Wiederherstellung) → MissingCredentials.</summary>
    [Fact]
    public async Task TryStoredLogin_TokenWithoutCredentials_ReturnsMissingCredentials()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateService(handler, out var secrets);
        await secrets.SetAuthTokenAsync("jwt-waise");
        handler.Enqueue(HttpStatusCode.Unauthorized); // api/favorites lehnt ab

        var attempt = await service.TryStoredLoginAsync(Server);

        Assert.Equal(AuthAttemptStatus.MissingCredentials, attempt.Status);
        Assert.Null(service.Client);
        Assert.Null(await secrets.GetAuthTokenAsync());
        // [0] api/favorites (Token abgelehnt), [1] Gate-Probe
        // POST api/auth/login — sie trennt eine Gate-Ablehnung von einem
        // abgelaufenen Token.
        Assert.Equal(2, handler.Requests.Count);
        Assert.EndsWith("/api/favorites", handler.Requests[0].PathAndQuery);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.EndsWith("/api/auth/login", handler.Requests[1].PathAndQuery);
        Assert.Equal("{}", handler.Requests[1].Body);
        Assert.Null(handler.Requests[1].Authorization);
    }

    /// <summary>Prüft TryStoredLogin: gespeicherter Token, Server nicht erreichbar → ServerUnreachable (Offline-Fallback).</summary>
    [Fact]
    public async Task TryStoredLogin_TokenValidationUnreachable_ReturnsServerUnreachable()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateService(handler, out var secrets);
        await secrets.SetAuthTokenAsync("jwt-vorhanden");
        // Retry-Politik des API-Clients: 3 Wiederholungen nach dem Erstversuch.
        for (var i = 0; i < 4; i++)
        {
            handler.EnqueueException(new HttpRequestException("Verbindung fehlgeschlagen"));
        }

        var attempt = await service.TryStoredLoginAsync(Server);

        Assert.Equal(AuthAttemptStatus.ServerUnreachable, attempt.Status);
        Assert.Null(service.Client);
    }

    /// <summary>Prüft TryStoredLogin: bei ungültigen Zugangsdaten → gibt InvalidCredentials zurück.</summary>
    [Fact]
    public async Task TryStoredLogin_InvalidCredentials_ReturnsInvalidCredentials()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateService(handler, out var secrets);
        await secrets.SetCredentialsAsync("martin", "falsch");
        handler.Enqueue(HttpStatusCode.Unauthorized);

        var attempt = await service.TryStoredLoginAsync(Server);

        Assert.Equal(AuthAttemptStatus.InvalidCredentials, attempt.Status);
        Assert.Equal("martin", attempt.Username);
        Assert.Null(service.Client);
    }

    /// <summary>Prüft TryStoredLogin: bei Transportfehler → gibt Server nicht erreichbar zurück.</summary>
    [Fact]
    public async Task TryStoredLogin_TransportError_ReturnsServerUnreachable()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateService(handler, out var secrets);
        await secrets.SetCredentialsAsync("martin", "pw");
        // Retry-Politik des API-Clients: 3 Wiederholungen nach dem Erstversuch.
        for (var i = 0; i < 4; i++)
        {
            handler.EnqueueException(new HttpRequestException("Verbindung fehlgeschlagen"));
        }

        var attempt = await service.TryStoredLoginAsync(Server);

        Assert.Equal(AuthAttemptStatus.ServerUnreachable, attempt.Status);
        Assert.Equal(4, handler.Requests.Count);
    }

    /// <summary>Prüft LoginAsync: bei Erfolg → speichert Zugangsdaten und Token.</summary>
    [Fact]
    public async Task LoginAsync_Success_SavesCredentialsAndToken()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateService(handler, out var secrets);
        EnqueueLoginOk(handler, "jwt-manual");

        var attempt = await service.LoginAsync(Server, "neu@server", "pw");

        Assert.True(attempt.Succeeded);
        Assert.Equal("neu@server", secrets.GetUsername());
        Assert.Equal("pw", await secrets.GetPasswordAsync());
        Assert.Equal("jwt-manual", await secrets.GetAuthTokenAsync());
        Assert.NotNull(service.Client);
    }

    /// <summary>Prüft LoginAsync: bei Serverfehler → gibt einen Fehlschlag zurück.</summary>
    [Fact]
    public async Task LoginAsync_ServerError_ReturnsFailed()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateService(handler, out _);
        // 5xx wird vom API-Client dreimal wiederholt → 4 Requests.
        for (var i = 0; i < 4; i++)
        {
            handler.Enqueue(HttpStatusCode.InternalServerError);
        }

        var attempt = await service.LoginAsync(Server, "martin", "pw");

        Assert.Equal(AuthAttemptStatus.Failed, attempt.Status);
    }

    /// <summary>Prüft LogoutAsync: gibt den Client frei und entfernt Zugangsdaten und Token.</summary>
    [Fact]
    public async Task LogoutAsync_ClearsClientAndSecrets()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateService(handler, out var secrets);
        await secrets.SetCredentialsAsync("martin", "pw");
        EnqueueLoginOk(handler, "jwt-logout");
        await service.TryStoredLoginAsync(Server);
        Assert.NotNull(service.Client);

        await service.LogoutAsync();

        // Die API besitzt keinen Logout-Endpunkt — die Sitzung endet
        // lokal: Client freigegeben, Geheimnisse entfernt, keine
        // automatische Anmeldung mehr möglich.
        Assert.Null(service.Client);
        Assert.Null(service.ClientBaseAddress);
        Assert.Equal(string.Empty, secrets.GetUsername());
        Assert.Equal(string.Empty, await secrets.GetPasswordAsync());
        Assert.Null(await secrets.GetAuthTokenAsync());
        Assert.False(await secrets.HasCredentialsAsync());
    }

    /// <summary>Prüft LogoutAsync: ohne Sitzung → bleibt ohne Fehler ein No-Op.</summary>
    [Fact]
    public async Task LogoutAsync_WithoutSession_IsNoOp()
    {
        var service = CreateService(new StubHttpMessageHandler(), out _);

        await service.LogoutAsync();

        Assert.Null(service.Client);
        Assert.Null(service.ClientBaseAddress);
    }

    /// <summary>Prüft TokenRefresh: meldet still erneut an und wiederholt den Request.</summary>
    [Fact]
    public async Task TokenRefresh_SilentlyReloginsAndRetriesRequest()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateService(handler, out var secrets);
        await secrets.SetCredentialsAsync("martin", "pw");
        EnqueueLoginOk(handler, "jwt-alt");
        await service.TryStoredLoginAsync(Server);
        var expired = 0;
        service.SessionExpired += (_, _) => expired++;

        // Folgeanfrage läuft auf 401 → stille Neuanmeldung → erneuter Request.
        handler.Enqueue(HttpStatusCode.Unauthorized);
        EnqueueLoginOk(handler, "jwt-neu");
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<object>());

        await service.Client!.GetSourcesAsync();

        // [0] Initial-Login, [1] 401-Folgeanfrage, [2] stille Neuanmeldung,
        // [3] wiederholte Anfrage mit neuem Token.
        Assert.Equal(4, handler.Requests.Count);
        Assert.EndsWith("/api/auth/login", handler.Requests[2].PathAndQuery);
        Assert.Equal("Bearer jwt-neu", handler.Requests[3].Authorization);
        Assert.Equal("jwt-neu", await secrets.GetAuthTokenAsync());
        Assert.Equal(0, expired);
    }

    /// <summary>Prüft TokenRefresh: ohne Zugangsdaten → löst das SessionExpired-Ereignis aus.</summary>
    [Fact]
    public async Task TokenRefresh_WithoutCredentials_RaisesSessionExpired()
    {
        // Simuliert den Zustand nach einer Backup-Wiederherstellung: Token und
        // aktiver Server sind vorhanden, die Geheimnisse fehlen.
        var handler = new StubHttpMessageHandler();
        var service = CreateService(handler, out var secrets);
        await secrets.SetCredentialsAsync("martin", "pw");
        EnqueueLoginOk(handler);
        await service.TryStoredLoginAsync(Server);
        await secrets.ClearCredentialsAsync();
        var expired = 0;
        service.SessionExpired += (_, _) => expired++;

        handler.Enqueue(HttpStatusCode.Unauthorized);
        var exception = await Assert.ThrowsAsync<ApiException>(() => service.Client!.GetSourcesAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal(1, expired);
        Assert.Null(await secrets.GetAuthTokenAsync());
    }

    /// <summary>Prüft TokenRefresh: bei <c>RejectedCredentials</c> → löst das SessionExpired-Ereignis aus.</summary>
    [Fact]
    public async Task TokenRefresh_RejectedCredentials_RaisesSessionExpired()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateService(handler, out var secrets);
        await secrets.SetCredentialsAsync("martin", "alt-pw");
        EnqueueLoginOk(handler);
        await service.TryStoredLoginAsync(Server);
        var expired = 0;
        service.SessionExpired += (_, _) => expired++;

        handler.Enqueue(HttpStatusCode.Unauthorized); // Folgeanfrage
        handler.Enqueue(HttpStatusCode.Unauthorized); // stille Neuanmeldung abgelehnt
        var exception = await Assert.ThrowsAsync<ApiException>(() => service.Client!.GetSourcesAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal(1, expired);
        Assert.Null(await secrets.GetAuthTokenAsync());
    }

    /// <summary>Prüft TokenRefresh: bei transientem Fehler → löst das SessionExpired-Ereignis nicht aus.</summary>
    [Fact]
    public async Task TokenRefresh_TransientError_DoesNotRaiseSessionExpired()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateService(handler, out var secrets);
        await secrets.SetCredentialsAsync("martin", "pw");
        EnqueueLoginOk(handler);
        await service.TryStoredLoginAsync(Server);
        var expired = 0;
        service.SessionExpired += (_, _) => expired++;

        handler.Enqueue(HttpStatusCode.Unauthorized);       // Folgeanfrage
        handler.Enqueue(HttpStatusCode.InternalServerError); // Refresh: 4× 500
        handler.Enqueue(HttpStatusCode.InternalServerError);
        handler.Enqueue(HttpStatusCode.InternalServerError);
        handler.Enqueue(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<ApiException>(() => service.Client!.GetSourcesAsync());

        Assert.Equal(0, expired);
    }

    /// <summary>
    /// Erstellt den Dienst samt geteilter <see cref="DeviceTokenService"/>-
    /// Instanz (beide nutzen dieselbe Stub-Handler-Fabrik).
    /// </summary>
    /// <param name="handler">Der Stub-Handler mit den eingereihten Antworten.</param>
    /// <param name="secrets">Die Ablage der Zugangsdaten.</param>
    /// <param name="deviceTokens">Die Geräte-Token-Verwaltung.</param>
    /// <returns>Der konfigurierte Dienst.</returns>
    private static AuthSessionService CreateServiceWithDeviceTokens(
        StubHttpMessageHandler handler,
        out AppSecretsService secrets,
        out DeviceTokenService deviceTokens)
    {
        secrets = new AppSecretsService(
            new InMemorySettingsStore(), new InMemorySecretStore(), AppProfile.Create(null));
        HttpClient Factory(Uri uri) => new(handler, disposeHandler: false) { BaseAddress = uri };
        deviceTokens = new DeviceTokenService(secrets, Factory);
        return new AuthSessionService(secrets, deviceTokens, Factory, new RecordingDelays().Delay);
    }

    /// <summary>
    /// Test-Vorrichtung: hinterlegt ein Geräte-Token für den Testserver und
    /// aktiviert ihn als aktuellen Server-Kontext.
    /// </summary>
    private static async Task PairDeviceAsync(DeviceTokenService deviceTokens, AppSecretsService secrets)
    {
        await secrets.SetDeviceTokenAsync(Server.ToString(), "dev-token");
        await deviceTokens.LoadForServerAsync(Server.ToString());
    }

    /// <summary>
    /// Prüft TokenRefresh: lehnt das Gate den Schlüssel ab (Probe-401), wird das
    /// Geräte-Token verworfen und <c>DeviceTokenRejected</c> ausgelöst — nicht
    /// <c>SessionExpired</c> (die Zugangsdaten sind nicht das Problem).
    /// </summary>
    [Fact]
    public async Task TokenRefresh_GateRejected_RaisesDeviceTokenRejected_NotSessionExpired()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateServiceWithDeviceTokens(handler, out var secrets, out var deviceTokens);
        try
        {
            await secrets.SetCredentialsAsync("martin", "pw");
            await PairDeviceAsync(deviceTokens, secrets);
            EnqueueLoginOk(handler);
            await service.TryStoredLoginAsync(Server);
            var expired = 0;
            var rejected = 0;
            service.SessionExpired += (_, _) => expired++;
            deviceTokens.DeviceTokenRejected += (_, _) => rejected++;

            handler.Enqueue(HttpStatusCode.Unauthorized); // Folgeanfrage
            handler.Enqueue(HttpStatusCode.Unauthorized); // stille Neuanmeldung abgelehnt
            EnqueueGateProbeRejected(handler);            // Gate-Probe → Gate lehnt ab
            await Assert.ThrowsAsync<ApiException>(() => service.Client!.GetSourcesAsync());

            Assert.Equal(1, rejected);
            Assert.Equal(0, expired);
            // Das Geräte-Token wurde verworfen (Re-Pairing folgt über den
            // MVU-Rand); das Anmelde-Token bleibt unberührt.
            Assert.False(await secrets.HasDeviceTokenAsync(Server.ToString()));
            Assert.NotNull(await secrets.GetAuthTokenAsync());
            // [0] Initial-Login, [1] 401-Folgeanfrage, [2] stille
            // Neuanmeldung, [3] Gate-Probe POST api/auth/login.
            Assert.Equal(4, handler.Requests.Count);
            Assert.Equal(HttpMethod.Post, handler.Requests[3].Method);
            Assert.EndsWith("/api/auth/login", handler.Requests[3].PathAndQuery);
            Assert.Equal("{}", handler.Requests[3].Body);
            Assert.Null(handler.Requests[3].Authorization);
        }
        finally
        {
            ClientGate.DeviceTokenProvider = null;
        }
    }

    /// <summary>
    /// Prüft TokenRefresh: akzeptiert die Gate-Probe den Schlüssel (400
    /// Model-Validation nach passiertem Gate), ist die 401-Ablehnung der
    /// Neuanmeldung ein Zugangsdaten-Problem → <c>SessionExpired</c> wie
    /// bisher.
    /// </summary>
    [Fact]
    public async Task TokenRefresh_GateProbeAccepted_RaisesSessionExpired()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateServiceWithDeviceTokens(handler, out var secrets, out var deviceTokens);
        try
        {
            await secrets.SetCredentialsAsync("martin", "alt-pw");
            await PairDeviceAsync(deviceTokens, secrets);
            EnqueueLoginOk(handler);
            await service.TryStoredLoginAsync(Server);
            var expired = 0;
            var rejected = 0;
            service.SessionExpired += (_, _) => expired++;
            deviceTokens.DeviceTokenRejected += (_, _) => rejected++;

            handler.Enqueue(HttpStatusCode.Unauthorized); // Folgeanfrage
            handler.Enqueue(HttpStatusCode.Unauthorized); // stille Neuanmeldung abgelehnt
            handler.Enqueue(HttpStatusCode.BadRequest);   // Gate-Probe → Gate akzeptiert (Model-Validation)
            await Assert.ThrowsAsync<ApiException>(() => service.Client!.GetSourcesAsync());

            Assert.Equal(0, rejected);
            Assert.Equal(1, expired);
            Assert.Null(await secrets.GetAuthTokenAsync());
        }
        finally
        {
            ClientGate.DeviceTokenProvider = null;
        }
    }

    /// <summary>
    /// Prüft TryStoredLogin: lehnt das Gate den Schlüssel beim
    /// Zugangsdaten-Login ab (widerrufenes Geräte-Token, kein gespeicherter
    /// Anmelde-Token), liefert der Versuch <c>GateRejected</c> — nicht
    /// <c>InvalidCredentials</c> (die Anmeldemaske wäre eine Sackgasse).
    /// </summary>
    [Fact]
    public async Task TryStoredLogin_GateRejectedLogin_ReturnsGateRejected()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateServiceWithDeviceTokens(handler, out var secrets, out var deviceTokens);
        try
        {
            await secrets.SetCredentialsAsync("martin", "pw");
            await PairDeviceAsync(deviceTokens, secrets);
            var rejected = 0;
            deviceTokens.DeviceTokenRejected += (_, _) => rejected++;

            handler.Enqueue(HttpStatusCode.Unauthorized); // api/auth/login → Gate-401
            EnqueueGateProbeRejected(handler);            // Gate-Probe → 401 problem+json

            var attempt = await service.TryStoredLoginAsync(Server);

            Assert.Equal(AuthAttemptStatus.GateRejected, attempt.Status);
            Assert.Equal(1, rejected);
            // Das Geräte-Token wurde verworfen; das Anmelde-Token bleibt
            // unberührt (es ist nicht das Problem).
            Assert.False(await secrets.HasDeviceTokenAsync(Server.ToString()));
            Assert.Equal(2, handler.Requests.Count);
            Assert.EndsWith("/api/auth/login", handler.Requests[0].PathAndQuery);
            // Die Probe geht an denselben gated Endpunkt — leerer Body,
            // nur der Gate-Header, kein Bearer.
            Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
            Assert.EndsWith("/api/auth/login", handler.Requests[1].PathAndQuery);
            Assert.Equal("{}", handler.Requests[1].Body);
            Assert.Null(handler.Requests[1].Authorization);
            Assert.Null(service.Client);
        }
        finally
        {
            ClientGate.DeviceTokenProvider = null;
        }
    }

    /// <summary>
    /// Prüft TryStoredLogin: wird der gespeicherte Anmelde-Token hinter einem
    /// widerrufenen Geräte-Token abgelehnt (401-Kaskade über die stille
    /// Neuanmeldung), liefert der Versuch <c>GateRejected</c> statt
    /// <c>InvalidCredentials</c>.
    /// </summary>
    [Fact]
    public async Task TryStoredLogin_StoredTokenGateRejected_ReturnsGateRejected()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateServiceWithDeviceTokens(handler, out var secrets, out var deviceTokens);
        try
        {
            await secrets.SetCredentialsAsync("martin", "pw");
            await secrets.SetAuthTokenAsync("jwt-alt");
            await PairDeviceAsync(deviceTokens, secrets);
            var expired = 0;
            var rejected = 0;
            service.SessionExpired += (_, _) => expired++;
            deviceTokens.DeviceTokenRejected += (_, _) => rejected++;

            handler.Enqueue(HttpStatusCode.Unauthorized); // api/favorites → Gate-401
            handler.Enqueue(HttpStatusCode.Unauthorized); // stille Neuanmeldung abgelehnt
            EnqueueGateProbeRejected(handler);            // Gate-Probe → 401 problem+json
            EnqueueGateProbeRejected(handler);            // Gate-Probe im Token-Catch → 401 problem+json

            var attempt = await service.TryStoredLoginAsync(Server);

            Assert.Equal(AuthAttemptStatus.GateRejected, attempt.Status);
            Assert.True(rejected >= 1);
            Assert.Equal(0, expired);
            Assert.False(await secrets.HasDeviceTokenAsync(Server.ToString()));
            // [0] api/favorites, [1] stille Neuanmeldung,
            // [2]+[3] Gate-Probes POST api/auth/login.
            Assert.Equal(4, handler.Requests.Count);
            Assert.EndsWith("/api/favorites", handler.Requests[0].PathAndQuery);
            Assert.EndsWith("/api/auth/login", handler.Requests[1].PathAndQuery);
            Assert.EndsWith("/api/auth/login", handler.Requests[2].PathAndQuery);
            Assert.Equal("{}", handler.Requests[2].Body);
            Assert.EndsWith("/api/auth/login", handler.Requests[3].PathAndQuery);
            Assert.Equal("{}", handler.Requests[3].Body);
        }
        finally
        {
            ClientGate.DeviceTokenProvider = null;
        }
    }

    /// <summary>
    /// Prüft TryStoredLogin: akzeptiert die Gate-Probe den Schlüssel (400
    /// Model-Validation nach passiertem Gate), ist die 401-Ablehnung ein
    /// Zugangsdaten-Problem → <c>InvalidCredentials</c> wie bisher, ohne
    /// Widerrufs-Meldung.
    /// </summary>
    [Fact]
    public async Task TryStoredLogin_GateProbeAccepted_ReturnsInvalidCredentials()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateServiceWithDeviceTokens(handler, out var secrets, out var deviceTokens);
        try
        {
            await secrets.SetCredentialsAsync("martin", "falsch");
            await PairDeviceAsync(deviceTokens, secrets);
            var rejected = 0;
            deviceTokens.DeviceTokenRejected += (_, _) => rejected++;

            handler.Enqueue(HttpStatusCode.Unauthorized); // api/auth/login abgelehnt
            handler.Enqueue(HttpStatusCode.BadRequest);   // Gate-Probe → Gate akzeptiert (Model-Validation)

            var attempt = await service.TryStoredLoginAsync(Server);

            Assert.Equal(AuthAttemptStatus.InvalidCredentials, attempt.Status);
            Assert.Equal(0, rejected);
            // Das Geräte-Token wurde nicht verworfen — das Gate akzeptiert es.
            Assert.True(await secrets.HasDeviceTokenAsync(Server.ToString()));
            Assert.Null(service.Client);
        }
        finally
        {
            ClientGate.DeviceTokenProvider = null;
        }
    }

    /// <summary>
    /// Prüft LoginAsync: lehnt das Gate den Schlüssel bei der manuellen
    /// Anmeldung ab, liefert der Versuch <c>GateRejected</c> — der Aufrufer
    /// steuert auf die Re-Pairing-Eingabe statt einen Anmeldefehler zu zeigen.
    /// </summary>
    [Fact]
    public async Task LoginAsync_GateRejected_ReturnsGateRejected()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateServiceWithDeviceTokens(handler, out var secrets, out var deviceTokens);
        try
        {
            await PairDeviceAsync(deviceTokens, secrets);
            var rejected = 0;
            deviceTokens.DeviceTokenRejected += (_, _) => rejected++;

            handler.Enqueue(HttpStatusCode.Unauthorized); // api/auth/login → Gate-401
            EnqueueGateProbeRejected(handler);            // Gate-Probe → 401 problem+json

            var attempt = await service.LoginAsync(Server, "martin", "pw");

            Assert.Equal(AuthAttemptStatus.GateRejected, attempt.Status);
            Assert.Equal(1, rejected);
            Assert.False(await secrets.HasDeviceTokenAsync(Server.ToString()));
            Assert.Equal("martin", attempt.Username);
        }
        finally
        {
            ClientGate.DeviceTokenProvider = null;
        }
    }

    /// <summary>
    /// Prüft LoginAsync: lehnt das Gate den Schlüssel eines Servers ab, der
    /// zwischenzeitlich nicht mehr der aktive ist, wird nur dessen
    /// Geräte-Token gezielt gelöscht — Provider und Token des aktiven
    /// Servers bleiben unberührt und es wird kein Widerruf gemeldet (keine
    /// Re-Pairing-Eingabe für den falschen Server).
    /// </summary>
    [Fact]
    public async Task LoginAsync_GateRejected_ForNonCurrentServer_KeepsActiveTokenAndStaysSilent()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateServiceWithDeviceTokens(handler, out var secrets, out var deviceTokens);
        try
        {
            var otherServer = new Uri("http://otherserver/videoplayer/");
            await secrets.SetDeviceTokenAsync(otherServer.ToString(), "dev-token-other");
            // Aktiver Server: der ambiente Provider bedient "dev-token".
            await PairDeviceAsync(deviceTokens, secrets);
            var rejected = 0;
            deviceTokens.DeviceTokenRejected += (_, _) => rejected++;

            handler.Enqueue(HttpStatusCode.Unauthorized); // api/auth/login → Gate-401
            EnqueueGateProbeRejected(handler);            // Gate-Probe → 401 problem+json

            var attempt = await service.LoginAsync(otherServer, "martin", "pw");

            Assert.Equal(AuthAttemptStatus.GateRejected, attempt.Status);
            Assert.Equal(2, handler.Requests.Count);
            // Die Probe sendet den adressspezifischen Schlüssel — nicht den
            // ambienten des aktiven Servers.
            Assert.Equal("dev-token-other", handler.Requests[1].ApiKey);
            // Das abgelehnte Token der geprüften Adresse wurde gelöscht …
            Assert.False(await secrets.HasDeviceTokenAsync(otherServer.ToString()));
            // … aber Provider und Token des aktiven Servers blieben
            // unberührt …
            Assert.Equal("dev-token", await secrets.GetDeviceTokenAsync(Server.ToString()));
            Assert.Equal("dev-token", ClientGate.Resolve());
            // … und der Widerruf wurde nicht gemeldet.
            Assert.Equal(0, rejected);
        }
        finally
        {
            ClientGate.DeviceTokenProvider = null;
        }
    }
}
