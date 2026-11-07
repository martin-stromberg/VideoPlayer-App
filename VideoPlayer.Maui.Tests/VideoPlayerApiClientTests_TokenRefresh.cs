#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>Tests der Token-Erneuerung des <c>VideoPlayerApiClient</c> bei HTTP 401.</summary>
public class VideoPlayerApiClientTests_TokenRefresh
{
    /// <summary>Prüft Unauthorized: bei <c>RefreshesTokenOnce</c> → erneuert das Token und sendet erneut.</summary>
    [Fact]
    public async Task Unauthorized_RefreshesTokenOnce_AndResends()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Unauthorized);
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<object>());
        var refreshCount = 0;
        var delays = new RecordingDelays();
        var client = ApiClientTestFactory.CreateClientWithDelays(handler, delays, options =>
        {
            options.TokenRefresher = _ =>
            {
                refreshCount++;
                return Task.FromResult<AuthToken?>(new AuthToken { Token = "renewed-token" });
            };
        });
        client.SetAccessToken(new AuthToken { Token = "expired-token" });

        await client.GetSourcesAsync();

        Assert.Equal(1, refreshCount);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("Bearer renewed-token", handler.Requests[1].Authorization);
        Assert.Empty(delays.Delays);
    }

    /// <summary>Prüft Unauthorized: bei <c>RefreshFails</c> → wirft.</summary>
    [Fact]
    public async Task Unauthorized_RefreshFails_Throws()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Unauthorized);
        var client = ApiClientTestFactory.CreateClient(handler, options =>
        {
            options.TokenRefresher = _ => Task.FromResult<AuthToken?>(null);
        });

        var exception = await Assert.ThrowsAsync<ApiException>(() => client.GetSourcesAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Single(handler.Requests);
    }

    /// <summary>Prüft Unauthorized: bei <c>WithoutRefresher</c> → wirft.</summary>
    [Fact]
    public async Task Unauthorized_WithoutRefresher_Throws()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Unauthorized);
        var client = ApiClientTestFactory.CreateClient(handler);

        var exception = await Assert.ThrowsAsync<ApiException>(() => client.GetSourcesAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Single(handler.Requests);
    }

    /// <summary>Prüft Unauthorized: bei <c>AfterSuccessfulRefresh</c> → wirft.</summary>
    [Fact]
    public async Task Unauthorized_AfterSuccessfulRefresh_Throws()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Unauthorized);
        handler.Enqueue(HttpStatusCode.Unauthorized);
        var refreshCount = 0;
        var client = ApiClientTestFactory.CreateClient(handler, options =>
        {
            options.TokenRefresher = _ =>
            {
                refreshCount++;
                return Task.FromResult<AuthToken?>(new AuthToken { Token = "renewed" });
            };
        });

        var exception = await Assert.ThrowsAsync<ApiException>(() => client.GetSourcesAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal(1, refreshCount);
        Assert.Equal(2, handler.Requests.Count);
    }

    /// <summary>Prüft Login: überspringt Re-Authentifizierung.</summary>
    [Fact]
    public async Task Login_SkipsReauthentication()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Unauthorized);
        var refreshCount = 0;
        var client = ApiClientTestFactory.CreateClient(handler, options =>
        {
            options.TokenRefresher = _ =>
            {
                refreshCount++;
                return Task.FromResult<AuthToken?>(new AuthToken { Token = "new" });
            };
        });

        await Assert.ThrowsAsync<ApiException>(() => client.LoginAsync("a@b.c", "pw"));

        Assert.Equal(0, refreshCount);
        Assert.Single(handler.Requests);
    }
}
