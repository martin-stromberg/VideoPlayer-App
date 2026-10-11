using System.Net;
using System.Text;
using System.Text.Json;

namespace VideoPlayer.E2E.Tests.Support;

/// <summary>
/// Client der Developer-API der laufenden App (Loopback, Port aus
/// <c>devapi.json</c>). Der Server beantwortet je Verbindung genau eine
/// Anfrage sequentiell — alle Aufrufe laufen über einen gemeinsamen
/// <see cref="HttpClient"/> ohne Pipelining.
/// </summary>
public sealed class DevApiClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient http = new();
    private readonly Uri baseUri;

    /// <summary>Erstellt den Client für den angegebenen Loopback-Port.</summary>
    /// <param name="port">Der von der App vergebene Port.</param>
    public DevApiClient(int port)
    {
        baseUri = new Uri($"http://127.0.0.1:{port}/");
    }

    /// <summary>Liest den Modell-Snapshot (<c>GET /model</c>).</summary>
    /// <returns>Der aktuelle Modellzustand.</returns>
    public async Task<DevApiModel> GetModelAsync(CancellationToken cancellationToken = default)
    {
        var json = await GetStringAsync("model", cancellationToken);
        return JsonSerializer.Deserialize<DevApiModel>(json, JsonOptions)
            ?? throw new InvalidOperationException("GET /model lieferte keine Antwort.");
    }

    /// <summary>Liest den Elementbaum der aktuellen Seite (<c>GET /tree</c>).</summary>
    /// <returns>Der Wurzelknoten des Baums.</returns>
    public async Task<DevApiTreeNode> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var json = await GetStringAsync("tree", cancellationToken);
        return JsonSerializer.Deserialize<DevApiTreeNode>(json, JsonOptions)
            ?? throw new InvalidOperationException("GET /tree lieferte keine Antwort.");
    }

    /// <summary>
    /// Löst das Clicked-Äquivalent eines Elements aus — oder wählt bei
    /// angegebenem <paramref name="itemIndex"/> einen Listeneintrag
    /// (CollectionView oder BindableLayout-Container) aus
    /// (<c>POST /invoke</c>).
    /// </summary>
    /// <param name="automationId">AutomationId des Elements.</param>
    /// <param name="itemIndex">Optionaler Index des auszuwählenden Eintrags.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    public async Task InvokeAsync(string automationId, int? itemIndex = null, CancellationToken cancellationToken = default)
    {
        var payload = itemIndex is null
            ? new Dictionary<string, object> { ["automationId"] = automationId }
            : new Dictionary<string, object> { ["automationId"] = automationId, ["itemIndex"] = itemIndex.Value };
        await PostAsync("invoke", payload, cancellationToken);
    }

    /// <summary>Setzt den Text eines Eingabefelds (<c>POST /set-text</c>).</summary>
    /// <param name="automationId">AutomationId des Eingabefelds.</param>
    /// <param name="text">Der zu setzende Text.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    public async Task SetTextAsync(string automationId, string text, CancellationToken cancellationToken = default)
    {
        await PostAsync("set-text", new { automationId, text }, cancellationToken);
    }

    /// <summary>
    /// Wartet, bis der Modell-Snapshot die Bedingung erfüllt (Poll alle
    /// 250 ms). Bei Zeitüberschreitung schlägt die Assertion mit dem
    /// zuletzt gelesenen Zustand fehl — so bleibt der Fehler diagnostizierbar.
    /// </summary>
    /// <param name="condition">Bedingung an den Snapshot.</param>
    /// <param name="timeout">Maximale Wartezeit (Standard 20 s).</param>
    /// <param name="description">Beschreibung der erwarteten Bedingung für die Fehlermeldung.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Der erste Snapshot, der die Bedingung erfüllt.</returns>
    public async Task<DevApiModel> WaitForModelAsync(
        Func<DevApiModel, bool> condition,
        TimeSpan? timeout = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(20));
        DevApiModel? last = null;
        Exception? lastError = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                last = await GetModelAsync(cancellationToken);
                lastError = null;
                if (condition(last))
                {
                    return last;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                && !cancellationToken.IsCancellationRequested)
            {
                // Die API kann während des Starts/Killens kurz nicht
                // antworten — weiter pollen bis zum Timeout.
                lastError = ex;
            }

            await Task.Delay(250, cancellationToken);
        }

        var lastState = last is null
            ? $"(kein Snapshot lesbar: {lastError?.Message})"
            : JsonSerializer.Serialize(last, JsonOptions);
        throw new TimeoutException(
            $"Bedingung nicht innerhalb des Zeitlimits erreicht ({description ?? "Modellbedingung"}). Letzter Zustand: {lastState}");
    }

    /// <inheritdoc/>
    public void Dispose() => http.Dispose();

    private async Task<string> GetStringAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(new Uri(baseUri, path), cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        return body;
    }

    private async Task PostAsync(string path, object payload, CancellationToken cancellationToken)
    {
        using var content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await http.PostAsync(new Uri(baseUri, path), content, cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"POST /{path} fehlgeschlagen: {(int)response.StatusCode} {body}");
        }
    }
}
