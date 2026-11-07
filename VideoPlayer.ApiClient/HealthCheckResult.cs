namespace VideoPlayer.ApiClient;

/// <summary>
/// Ergebnis der Server-Erreichbarkeitsprüfung (<c>GET api/health</c>).
/// Unterscheidet eine Ablehnung durch das Client-Gate (ungültiger bzw.
/// widerrufener Schlüssel) von einer echten Nichterreichbarkeit.
/// </summary>
public enum HealthCheckResult
{
    /// <summary>Server erreichbar und Client-Gate-Schlüssel akzeptiert.</summary>
    Reachable,

    /// <summary>Server erreichbar, aber das Client-Gate lehnt den Schlüssel ab (HTTP 401).</summary>
    GateRejected,

    /// <summary>Server nicht erreichbar oder anderer Fehler.</summary>
    Unreachable
}
