using System.Net;
using System.Text;
using System.Text.Json;
using VideoPlayer.ApiClient;

namespace VideoWebPlayer.Maui.Tests.Stubs;

/// <summary>
/// Aufgezeichneter HTTP-Request des simulierten Servers.
/// </summary>
public sealed record RecordedRequest(
    HttpMethod Method,
    string PathAndQuery,
    string? Authorization,
    string? ApiKey,
    string? Body);

/// <summary>
/// Simulierter Server: beantwortet Requests der Reihe nach aus einer Queue
/// bzw. über ein Responder-Delegate und zeichnet alle eingehenden Requests auf.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, Task<HttpResponseMessage>>> responses = new();

    public List<RecordedRequest> Requests { get; } = [];

    public void Enqueue(HttpResponseMessage response)
    {
        responses.Enqueue(_ => Task.FromResult(response));
    }

    public void Enqueue(HttpStatusCode statusCode, string? jsonBody = null)
    {
        var response = new HttpResponseMessage(statusCode);
        if (jsonBody is not null)
        {
            response.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }
        Enqueue(response);
    }

    public void EnqueueJson(HttpStatusCode statusCode, object payload)
    {
        Enqueue(statusCode, JsonSerializer.Serialize(payload));
    }

    public void EnqueueException(Exception exception)
    {
        responses.Enqueue(_ => Task.FromException<HttpResponseMessage>(exception));
    }

    public void Enqueue(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        responses.Enqueue(request => Task.FromResult(responder(request)));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new RecordedRequest(
            request.Method,
            request.RequestUri?.PathAndQuery ?? string.Empty,
            request.Headers.Authorization?.ToString(),
            request.Headers.TryGetValues(ClientGate.HeaderName, out var values) ? string.Join(",", values) : null,
            body));

        if (responses.Count == 0)
        {
            throw new InvalidOperationException($"Unerwarteter Request ohne konfigurierte Antwort: {request.Method} {request.RequestUri}");
        }

        return await responses.Dequeue()(request);
    }
}

/// <summary>
/// Delay-Implementierung für Tests: zeichnet angeforderte Warteintervalle auf,
/// ohne tatsächlich zu warten.
/// </summary>
public sealed class RecordingDelays
{
    public List<TimeSpan> Delays { get; } = [];

    public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Delays.Add(delay);
        return Task.CompletedTask;
    }
}

public static class ApiClientTestFactory
{
    public static Uri BaseAddress { get; } = new("http://testserver/");

    public static VideoPlayerApiClient CreateClient(
        StubHttpMessageHandler handler,
        Action<VideoPlayerApiClientOptions>? configure = null)
    {
        var options = new VideoPlayerApiClientOptions { ApiKey = "test-api-key" };
        configure?.Invoke(options);
        return new VideoPlayerApiClient(new HttpClient(handler) { BaseAddress = BaseAddress }, options);
    }

    public static VideoPlayerApiClient CreateClientWithDelays(
        StubHttpMessageHandler handler,
        RecordingDelays delays,
        Action<VideoPlayerApiClientOptions>? configure = null)
    {
        return CreateClient(handler, options =>
        {
            options.DelayAsync = delays.Delay;
            configure?.Invoke(options);
        });
    }
}
