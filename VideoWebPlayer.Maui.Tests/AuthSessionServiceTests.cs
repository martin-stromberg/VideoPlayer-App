#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
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
/// Zugangsdaten.
/// </summary>
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
}
