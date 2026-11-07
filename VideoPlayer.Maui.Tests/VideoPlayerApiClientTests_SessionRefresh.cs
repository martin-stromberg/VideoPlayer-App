#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using System.Text.Json;
using VideoPlayer.ApiClient;
using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Tests der Sitzungserneuerung/-beendigung des <c>VideoPlayerApiClient</c>
/// (Schritt 5b): <c>api/auth/refresh</c> liefert das rotierte Paket,
/// <c>api/auth/logout</c> widerruft den Refresh-Token idempotent; beide
/// Aufrufe laufen hinter dem Client-Gate und ohne 401-Erneuerung.
/// </summary>
public class VideoPlayerApiClientTests_SessionRefresh
{
    /// <summary>Prüft RefreshSessionAsync: sendet POST mit camelCase-Vertrag und Gate-Header.</summary>
    [Fact]
    public async Task RefreshSessionAsync_SendsContract_WithGateHeader()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueJson(HttpStatusCode.OK, new
        {
            token = "jwt-neu",
            expires = "2030-01-01T00:00:00Z",
            refreshToken = "refresh-neu"
        });
        var refresherCalled = false;
        var client = ApiClientTestFactory.CreateClient(handler, options =>
        {
            options.TokenRefresher = _ =>
            {
                refresherCalled = true;
                return Task.FromResult<VideoPlayer.ApiClient.Models.AuthToken?>(null);
            };
        });

        var session = await client.RefreshSessionAsync("refresh-alt");

        Assert.Equal("jwt-neu", session.Token);
        Assert.Equal("refresh-neu", session.RefreshToken);
        Assert.Equal(new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), session.Expires);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/auth/refresh", request.PathAndQuery);
        Assert.Equal("test-api-key", request.ApiKey);
        var body = JsonDocument.Parse(request.Body!).RootElement;
        Assert.Equal("refresh-alt", body.GetProperty("refreshToken").GetString());
        Assert.False(body.TryGetProperty("RefreshToken", out _));
        Assert.False(refresherCalled); // allowReauth: false — kein Rekursionspfad
    }

    /// <summary>
    /// Prüft RefreshSessionAsync: ein 401 (abgelehnter/abgelaufener
    /// Refresh-Token) wird als <see cref="ApiException"/> gemeldet —
    /// ohne Erneuerungsversuch (der Aufruf ist die Erneuerung).
    /// </summary>
    [Fact]
    public async Task RefreshSessionAsync_Unauthorized_Throws_WithoutReauth()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Unauthorized);
        var refresherCalled = false;
        var client = ApiClientTestFactory.CreateClient(handler, options =>
        {
            options.TokenRefresher = _ =>
            {
                refresherCalled = true;
                return Task.FromResult<VideoPlayer.ApiClient.Models.AuthToken?>(null);
            };
        });

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => client.RefreshSessionAsync("refresh-alt"));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Single(handler.Requests);
        Assert.False(refresherCalled);
    }

    /// <summary>Prüft RefreshSessionAsync: ein leeres Token wird lokal abgelehnt (kein Request).</summary>
    [Fact]
    public async Task RefreshSessionAsync_RequiresToken()
    {
        var handler = new StubHttpMessageHandler();
        var client = ApiClientTestFactory.CreateClient(handler);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.RefreshSessionAsync("  "));

        Assert.Empty(handler.Requests);
    }

    /// <summary>Prüft LogoutSessionAsync: sendet den Widerruf als POST mit Refresh-Token.</summary>
    [Fact]
    public async Task LogoutSessionAsync_SendsRevocation()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK);
        var client = ApiClientTestFactory.CreateClient(handler);

        await client.LogoutSessionAsync("refresh-alt");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/auth/logout", request.PathAndQuery);
        Assert.Equal("test-api-key", request.ApiKey);
        var body = JsonDocument.Parse(request.Body!).RootElement;
        Assert.Equal("refresh-alt", body.GetProperty("refreshToken").GetString());
    }

    /// <summary>Prüft LogoutSessionAsync: der Endpunkt ist idempotent — 200 auch bei altem Token.</summary>
    [Fact]
    public async Task LogoutSessionAsync_AlreadyInvalid_StillSucceeds()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK); // idempotent: auch ungültige Token → 200
        var client = ApiClientTestFactory.CreateClient(handler);

        await client.LogoutSessionAsync("bereits-ungueltig");

        Assert.Single(handler.Requests);
    }

    /// <summary>Prüft LogoutSessionAsync: ein leeres Token wird lokal abgelehnt (kein Request).</summary>
    [Fact]
    public async Task LogoutSessionAsync_RequiresToken()
    {
        var handler = new StubHttpMessageHandler();
        var client = ApiClientTestFactory.CreateClient(handler);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.LogoutSessionAsync(""));

        Assert.Empty(handler.Requests);
    }
}
