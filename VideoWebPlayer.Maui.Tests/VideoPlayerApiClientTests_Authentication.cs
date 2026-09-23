#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Models;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>Tests der Authentifizierung des <c>VideoPlayerApiClient</c> (Login, Header, Health-Check).</summary>
public class VideoPlayerApiClientTests_Authentication
{
    /// <summary>Prüft LoginAsync: postet Zugangsdaten und setzt Bearertoken.</summary>
    [Fact]
    public async Task LoginAsync_PostsCredentialsAndSetsBearerToken()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "jwt-token-1", expires = "2030-01-01T00:00:00Z" });
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<object>());
        var client = ApiClientTestFactory.CreateClient(handler);

        var token = await client.LoginAsync("user@example.com", "secret");

        Assert.Equal("jwt-token-1", token.Token);
        Assert.Equal("jwt-token-1", client.CurrentToken);

        var loginRequest = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, loginRequest.Method);
        Assert.Equal("/api/auth/login", loginRequest.PathAndQuery);
        Assert.Contains("\"Email\":\"user@example.com\"", loginRequest.Body);
        Assert.Contains("\"Password\":\"secret\"", loginRequest.Body);

        await client.GetSourcesAsync();
        Assert.Equal("Bearer jwt-token-1", handler.Requests[1].Authorization);
    }

    /// <summary>Prüft AllRequests: enthalten den ClientGate-Header.</summary>
    [Fact]
    public async Task AllRequests_ContainClientGateHeader()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "t", expires = "2030-01-01T00:00:00Z" });
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<object>());
        var client = ApiClientTestFactory.CreateClient(handler);

        await client.LoginAsync("a@b.c", "pw");
        await client.GetSourcesAsync();

        Assert.All(handler.Requests, request => Assert.Equal("test-api-key", request.ApiKey));
    }

    /// <summary>Prüft Constructor: ohne Baseadresse → wirft.</summary>
    [Fact]
    public void Constructor_WithoutBaseAddress_Throws()
    {
        var handler = new StubHttpMessageHandler();

        Assert.Throws<InvalidOperationException>(() => new VideoPlayerApiClient(new HttpClient(handler)));
    }

    /// <summary>Prüft CheckHealthAsync: meldet <c>Reachable</c> auf Erfolg.</summary>
    [Fact]
    public async Task CheckHealthAsync_ReturnsReachableOnSuccess()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK);
        var client = ApiClientTestFactory.CreateClient(handler);

        Assert.Equal(HealthCheckResult.Reachable, await client.CheckHealthAsync());
        Assert.Equal("/api/health", handler.Requests[0].PathAndQuery);
    }

    /// <summary>Prüft CheckHealthAsync: meldet <c>Unreachable</c> auf sonstigem Fehlschlag.</summary>
    /// <param name="statusCode">Der simulierte HTTP-Statuscode.</param>
    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task CheckHealthAsync_ReturnsUnreachableOnFailure(HttpStatusCode statusCode)
    {
        var handler = new StubHttpMessageHandler();
        // 5xx ist wiederholbar — für alle Versuche Antworten einreihen.
        for (var i = 0; i < 4; i++)
        {
            handler.Enqueue(statusCode);
        }
        var client = ApiClientTestFactory.CreateClient(handler, options =>
            options.DelayAsync = (_, _) => Task.CompletedTask);

        Assert.Equal(HealthCheckResult.Unreachable, await client.CheckHealthAsync());
    }

    /// <summary>Prüft CheckHealthAsync: meldet <c>Unreachable</c> bei Transportfehler.</summary>
    [Fact]
    public async Task CheckHealthAsync_ReturnsUnreachableOnTransportError()
    {
        var handler = new StubHttpMessageHandler();
        // Transportfehler sind wiederholbar — für alle Versuche Ausnahmen einreihen.
        for (var i = 0; i < 4; i++)
        {
            handler.EnqueueException(new HttpRequestException("Verbindung fehlgeschlagen"));
        }
        var client = ApiClientTestFactory.CreateClient(handler, options => options.DelayAsync = (_, _) => Task.CompletedTask);

        Assert.Equal(HealthCheckResult.Unreachable, await client.CheckHealthAsync());
    }

    /// <summary>
    /// Prüft CheckHealthAsync: ein 401 ohne Token-Erneuerung wird als
    /// <c>GateRejected</c> gemeldet (Client-Gate lehnt den Schlüssel ab).
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_ReturnsGateRejectedOnUnauthorized()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Unauthorized);
        var client = ApiClientTestFactory.CreateClient(handler);

        Assert.Equal(HealthCheckResult.GateRejected, await client.CheckHealthAsync());
    }

    /// <summary>Prüft SetApiKey: ersetzt den Gate-Header für Folgeanfragen.</summary>
    [Fact]
    public async Task SetApiKey_UpdatesClientGateHeader()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK);
        var client = ApiClientTestFactory.CreateClient(handler);

        client.SetApiKey("device-token-1");
        await client.CheckHealthAsync();

        Assert.Equal("device-token-1", handler.Requests[0].ApiKey);
    }

    /// <summary>Prüft SetApiKey: <c>null</c>/leerer Schlüssel → ArgumentException (kein heimliches Entfernen des Gate-Headers).</summary>
    /// <param name="apiKey">Der abgelehnte Schlüssel.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SetApiKey_RejectsNullOrEmpty(string? apiKey)
    {
        var handler = new StubHttpMessageHandler();
        var client = ApiClientTestFactory.CreateClient(handler);

        Assert.ThrowsAny<ArgumentException>(() => client.SetApiKey(apiKey!));
    }
}
