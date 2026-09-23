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

    /// <summary>Prüft CheckHealthAsync: gibt true auf Erfolg zurück.</summary>
    [Fact]
    public async Task CheckHealthAsync_ReturnsTrueOnSuccess()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK);
        var client = ApiClientTestFactory.CreateClient(handler);

        Assert.True(await client.CheckHealthAsync());
        Assert.Equal("/api/health", handler.Requests[0].PathAndQuery);
    }

    /// <summary>Prüft CheckHealthAsync: gibt false auf Fehlschlag zurück.</summary>
    [Fact]
    public async Task CheckHealthAsync_ReturnsFalseOnFailure()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NotFound);
        var client = ApiClientTestFactory.CreateClient(handler);

        Assert.False(await client.CheckHealthAsync());
    }
}
