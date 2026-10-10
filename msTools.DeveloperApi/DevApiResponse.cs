#if DEBUG || DEV_API
using System.Text.Json;

namespace msTools.DeveloperApi;

/// <summary>
/// Eine ausgehende Antwort der Developer-API (Statuscode plus JSON-Body).
/// </summary>
public sealed record DevApiResponse
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>HTTP-Statuscode der Antwort.</summary>
    public required int StatusCode { get; init; }

    /// <summary>Antwort-Body (UTF-8-kodiert verschickt).</summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>Content-Type der Antwort.</summary>
    public string ContentType { get; init; } = "application/json; charset=utf-8";

    /// <summary>Erzeugt eine 200er-Antwort mit camelCase-serialisiertem JSON-Body.</summary>
    /// <param name="payload">Das zu serialisierende Objekt (kann <c>null</c> sein).</param>
    /// <returns>Die Antwort.</returns>
    public static DevApiResponse Json(object? payload) =>
        new() { StatusCode = 200, Body = JsonSerializer.Serialize(payload, JsonOptions) };

    /// <summary>Erzeugt eine Fehlerantwort im Format <c>{"error":"…"}</c>.</summary>
    /// <param name="statusCode">HTTP-Statuscode.</param>
    /// <param name="message">Deutscher Fehlertext.</param>
    /// <returns>Die Antwort.</returns>
    public static DevApiResponse Error(int statusCode, string message) =>
        new() { StatusCode = statusCode, Body = JsonSerializer.Serialize(new { error = message }, JsonOptions) };
}
#endif
