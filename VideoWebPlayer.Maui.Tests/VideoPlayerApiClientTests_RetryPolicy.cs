#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using VideoPlayer.ApiClient;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>Tests der Retry-Policy des <c>VideoPlayerApiClient</c> (Wiederholungen, Delays, Abbruch).</summary>
public class VideoPlayerApiClientTests_RetryPolicy
{
    /// <summary>Prüft ServerError: bei <c>RetriesUpToThreeTimes</c> → wiederholt mit exponentiellen Delays.</summary>
    [Fact]
    public async Task ServerError_RetriesUpToThreeTimes_WithExponentialDelays()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.InternalServerError);
        handler.Enqueue(HttpStatusCode.InternalServerError);
        handler.Enqueue(HttpStatusCode.InternalServerError);
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<object>());
        var delays = new RecordingDelays();
        var client = ApiClientTestFactory.CreateClientWithDelays(handler, delays);

        var sources = await client.GetSourcesAsync();

        Assert.Empty(sources);
        Assert.Equal(4, handler.Requests.Count);
        Assert.Equal(
            [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4)],
            delays.Delays);
    }

    /// <summary>Prüft ServerError: bei permanentem Fehler → wirft nach Erschöpfung der Retries.</summary>
    [Fact]
    public async Task ServerError_Permanent_ThrowsAfterExhaustion()
    {
        var handler = new StubHttpMessageHandler();
        for (var i = 0; i < 4; i++)
        {
            handler.Enqueue(HttpStatusCode.InternalServerError);
        }
        var delays = new RecordingDelays();
        var client = ApiClientTestFactory.CreateClientWithDelays(handler, delays);

        var exception = await Assert.ThrowsAsync<ApiException>(() => client.GetSourcesAsync());

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Equal(4, handler.Requests.Count);
        Assert.Equal(3, delays.Delays.Count);
    }

    /// <summary>Prüft ClientError: wiederholt nicht.</summary>
    /// <param name="statusCode">Der HTTP-Statuscode.</param>
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task ClientError_NotRetried(HttpStatusCode statusCode)
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(statusCode);
        var delays = new RecordingDelays();
        var client = ApiClientTestFactory.CreateClientWithDelays(handler, delays);

        var exception = await Assert.ThrowsAsync<ApiException>(() => client.GetSourcesAsync());

        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Single(handler.Requests);
        Assert.Empty(delays.Delays);
    }

    /// <summary>Prüft RetriableClientError: wiederholt.</summary>
    /// <param name="statusCode">Der HTTP-Statuscode.</param>
    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task RetriableClientError_Retried(HttpStatusCode statusCode)
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(statusCode);
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<object>());
        var delays = new RecordingDelays();
        var client = ApiClientTestFactory.CreateClientWithDelays(handler, delays);

        await client.GetSourcesAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal([TimeSpan.FromSeconds(1)], delays.Delays);
    }

    /// <summary>Prüft TransportError: wiederholt.</summary>
    [Fact]
    public async Task TransportError_Retried()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueException(new HttpRequestException("connection reset"));
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<object>());
        var delays = new RecordingDelays();
        var client = ApiClientTestFactory.CreateClientWithDelays(handler, delays);

        await client.GetSourcesAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal([TimeSpan.FromSeconds(1)], delays.Delays);
    }

    /// <summary>Prüft TransportError: bei permanentem Fehler → wirft nach Erschöpfung der Retries.</summary>
    [Fact]
    public async Task TransportError_Permanent_ThrowsAfterExhaustion()
    {
        var handler = new StubHttpMessageHandler();
        for (var i = 0; i < 4; i++)
        {
            handler.EnqueueException(new HttpRequestException("connection reset"));
        }
        var delays = new RecordingDelays();
        var client = ApiClientTestFactory.CreateClientWithDelays(handler, delays);

        var exception = await Assert.ThrowsAsync<ApiException>(() => client.GetSourcesAsync());

        Assert.Null(exception.StatusCode);
        Assert.Equal(4, handler.Requests.Count);
    }

    /// <summary>Prüft Timeout: wiederholt.</summary>
    [Fact]
    public async Task Timeout_Retried()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueException(new TaskCanceledException("request timed out"));
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<object>());
        var delays = new RecordingDelays();
        var client = ApiClientTestFactory.CreateClientWithDelays(handler, delays);

        await client.GetSourcesAsync();

        Assert.Equal(2, handler.Requests.Count);
    }

    /// <summary>Prüft CallerCancellation: wiederholt nicht.</summary>
    [Fact]
    public async Task CallerCancellation_NotRetried()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.InternalServerError);
        var delays = new RecordingDelays();
        var client = ApiClientTestFactory.CreateClientWithDelays(handler, delays);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetSourcesAsync(cancellation.Token));

        Assert.Empty(delays.Delays);
        Assert.True(handler.Requests.Count <= 1);
    }
}
