#if DEBUG || DEV_API
using System.Text.Json;

namespace msTools.DeveloperApi;

/// <summary>
/// Eine eingehende Anfrage an die Developer-API (bereits zerlegt in
/// Methode, Pfad, Header und Body).
/// </summary>
public sealed record DevApiRequest
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>HTTP-Methode (z. B. <c>GET</c>, <c>POST</c>).</summary>
    public required string Method { get; init; }

    /// <summary>Angefragter Pfad ohne Query-Anteil (z. B. <c>/model</c>).</summary>
    public required string Path { get; init; }

    /// <summary>Header-Felder (schlüssel-seitig ohne Beachtung der Groß-/Kleinschreibung).</summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Request-Body als Text (leer bei GET-Anfragen).</summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>
    /// Versucht, den Body als JSON in den angegebenen Typ zu deserialisieren.
    /// </summary>
    /// <typeparam name="T">Zieltyp des Bodys.</typeparam>
    /// <param name="value">Der deserialisierte Wert bei Erfolg.</param>
    /// <param name="error">Deutscher Fehlertext bei ungültigem JSON.</param>
    /// <returns><c>true</c>, wenn der Body gelesen werden konnte.</returns>
    public bool TryGetJsonBody<T>(out T? value, out string? error)
    {
        value = default;
        error = null;
        if (string.IsNullOrWhiteSpace(Body))
        {
            error = "Der Request-Body ist leer.";
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<T>(Body, JsonOptions);
            return true;
        }
        catch (JsonException)
        {
            error = "Der Request-Body ist kein gültiges JSON.";
            return false;
        }
    }
}
#endif
