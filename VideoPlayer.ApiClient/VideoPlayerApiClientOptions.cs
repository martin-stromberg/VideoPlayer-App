using Microsoft.Extensions.Logging;
using VideoPlayer.ApiClient.Models;

namespace VideoPlayer.ApiClient;

/// <summary>
/// Konfiguration des <see cref="VideoPlayerApiClient"/>.
/// </summary>
public class VideoPlayerApiClientOptions
{
    /// <summary>
    /// Basisadresse des Servers. Wird auf den übergebenen <see cref="HttpClient"/>
    /// angewendet, sofern dort noch keine <see cref="HttpClient.BaseAddress"/> gesetzt ist.
    /// </summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>
    /// API-Schlüssel für das Client-Gate (<see cref="ClientGate.HeaderName"/>).
    /// Standard: <see cref="ClientGate.Resolve()"/>.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Erneuerungslogik bei „nicht autorisiert" (401): liefert ein neues
    /// <see cref="AuthToken"/> (z. B. über eine stille Neuanmeldung) oder
    /// <c>null</c>, wenn keine Erneuerung möglich ist. Wird pro Anfrage
    /// höchstens einmal aufgerufen.
    /// </summary>
    public Func<CancellationToken, Task<AuthToken?>>? TokenRefresher { get; set; }

    /// <summary>
    /// Verzögerungsimplementierung für die Wiederholungsstrategie.
    /// Standard: <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.
    /// </summary>
    public Func<TimeSpan, CancellationToken, Task>? DelayAsync { get; set; }

    /// <summary>Optionaler Logger für Diagnoseausgaben (Retries, Token-Erneuerung).</summary>
    public ILogger? Logger { get; set; }
}
