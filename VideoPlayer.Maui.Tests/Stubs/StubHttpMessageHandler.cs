using System.Net;
using System.Text;
using System.Text.Json;
using VideoPlayer.ApiClient;

namespace VideoPlayer.Maui.Tests.Stubs;

/// <summary>
/// Aufgezeichneter HTTP-Request des simulierten Servers.
/// </summary>
/// <param name="Method">Die HTTP-Methode des Requests.</param>
/// <param name="PathAndQuery">Pfad und Query-String des Requests.</param>
/// <param name="Authorization">Der Authorization-Header, falls vorhanden.</param>
/// <param name="ApiKey">Der API-Key-Header, falls vorhanden.</param>
/// <param name="Body">Der Request-Body, falls vorhanden.</param>
/// <returns>Ein aufgezeichneter Request mit den erfassten Daten.</returns>
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
    private readonly object gate = new();
    private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> responses = new();

    /// <summary>Die in Reihenfolge aufgezeichneten Requests.</summary>
    /// <value>Liste aller eingegangenen Requests.</value>
    public List<RecordedRequest> Requests { get; } = [];

    /// <summary>Reiht eine vorgefertigte Antwort in die Queue ein.</summary>
    /// <param name="response">Die zurückzugebende Antwort.</param>
    public void Enqueue(HttpResponseMessage response)
    {
        lock (gate)
        {
            responses.Enqueue((_, _) => Task.FromResult(response));
        }
    }

    /// <summary>Reiht eine Antwort mit Statuscode und optionalem JSON-Body ein.</summary>
    /// <param name="statusCode">Der HTTP-Statuscode der Antwort.</param>
    /// <param name="jsonBody">Optionaler JSON-Body der Antwort.</param>
    public void Enqueue(HttpStatusCode statusCode, string? jsonBody = null)
    {
        var response = new HttpResponseMessage(statusCode);
        if (jsonBody is not null)
        {
            response.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }
        Enqueue(response);
    }

    /// <summary>Reiht eine Antwort ein, deren Body das serialisierte Objekt ist.</summary>
    /// <param name="statusCode">Der HTTP-Statuscode der Antwort.</param>
    /// <param name="payload">Das zu serialisierende Antwortobjekt.</param>
    public void EnqueueJson(HttpStatusCode statusCode, object payload)
    {
        Enqueue(statusCode, JsonSerializer.Serialize(payload));
    }

    /// <summary>Reiht eine Ausnahme ein, die beim nächsten Request geworfen wird.</summary>
    /// <param name="exception">Die zu werfende Ausnahme.</param>
    public void EnqueueException(Exception exception)
    {
        lock (gate)
        {
            responses.Enqueue((_, _) => Task.FromException<HttpResponseMessage>(exception));
        }
    }

    /// <summary>Reiht ein Responder-Delegate ein, das die Antwort erzeugt.</summary>
    /// <param name="responder">Das Responder-Delegate.</param>
    public void Enqueue(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        lock (gate)
        {
            responses.Enqueue((request, _) => Task.FromResult(responder(request)));
        }
    }

    /// <summary>Asynchrones Responder-Delegate, das das Request-Abbruchtoken beachtet.</summary>
    /// <param name="request">Die abgesendete HTTP-Anfrage.</param>
    /// <param name="cancellationToken">Abbruchtoken der Anfrage.</param>
    /// <returns>Die zu liefernde HTTP-Antwort.</returns>
    public delegate Task<HttpResponseMessage> AsyncResponder(HttpRequestMessage request, CancellationToken cancellationToken);

    /// <summary>
    /// Reiht ein asynchrones Responder-Delegate ein — etwa eine
    /// wartende Aufgabe für Transfers, die auf einen Abbruch warten:
    /// der Handler reicht das Request-<see cref="CancellationToken"/>
    /// durch, sodass ein Abbruch den ausstehenden Send auflöst.
    /// </summary>
    /// <param name="responder">Das asynchrone Responder-Delegate.</param>
    public void EnqueueAsyncResponder(AsyncResponder responder)
    {
        lock (gate)
        {
            responses.Enqueue((request, cancellationToken) => responder(request, cancellationToken));
        }
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder;
        lock (gate)
        {
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

            responder = responses.Dequeue();
        }

        return await responder(request, cancellationToken);
    }
}

/// <summary>
/// Delay-Implementierung für Tests: zeichnet angeforderte Warteintervalle auf,
/// ohne tatsächlich zu warten.
/// </summary>
public sealed class RecordingDelays
{
    /// <summary>Die in Reihenfolge angeforderten Verzögerungen.</summary>
    /// <value>Liste aller aufgezeichneten Warteintervalle.</value>
    public List<TimeSpan> Delays { get; } = [];

    /// <summary>Zeichnet eine angeforderte Verzögerung auf, ohne zu warten.</summary>
    /// <param name="delay">Das angeforderte Warteintervall.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Ein sofort abgeschlossener Task.</returns>
    public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Delays.Add(delay);
        return Task.CompletedTask;
    }
}

/// <summary>Testfabrik für den API-Client.</summary>
public static class ApiClientTestFactory
{
    /// <summary>Die Basisadresse des simulierten Testservers.</summary>
    /// <value>Basis-URI für alle Test-Clients.</value>
    public static Uri BaseAddress { get; } = new("http://testserver/");

    /// <summary>Erstellt einen API-Client, der über den Stub-Handler kommuniziert.</summary>
    /// <param name="handler">Der Stub-Handler mit den eingereihten Antworten.</param>
    /// <param name="configure">Optionale Konfiguration der Client-Optionen.</param>
    /// <returns>Ein konfigurierter <see cref="VideoPlayerApiClient"/>.</returns>
    public static VideoPlayerApiClient CreateClient(
        StubHttpMessageHandler handler,
        Action<VideoPlayerApiClientOptions>? configure = null)
    {
        var options = new VideoPlayerApiClientOptions { ApiKey = "test-api-key" };
        configure?.Invoke(options);
        return new VideoPlayerApiClient(new HttpClient(handler) { BaseAddress = BaseAddress }, options);
    }

    /// <summary>Erstellt einen API-Client, dessen Delays aufgezeichnet werden.</summary>
    /// <param name="handler">Der Stub-Handler mit den eingereihten Antworten.</param>
    /// <param name="delays">Der Delay-Recorder für angeforderte Warteintervalle.</param>
    /// <param name="configure">Optionale Konfiguration der Client-Optionen.</param>
    /// <returns>Ein konfigurierter <see cref="VideoPlayerApiClient"/>.</returns>
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
