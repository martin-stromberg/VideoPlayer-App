using System.Net;

namespace VideoPlayer.ApiClient;

/// <summary>
/// Fehler einer Server-API-Antwort nach Ausschöpfung der Wiederholungsstrategie.
/// </summary>
public class ApiException : HttpRequestException
{
    /// <summary>
    /// Erstellt die Ausnahme für eine fehlgeschlagene Serveranfrage.
    /// </summary>
    /// <param name="endpoint">Relativer Endpunktpfad der Anfrage.</param>
    /// <param name="statusCode">HTTP-Statuscode der Antwort, falls eine eingegangen ist.</param>
    /// <param name="responseBody">Rohantwort des Servers, falls vorhanden.</param>
    /// <param name="message">Fehlermeldung der Ausnahme.</param>
    /// <param name="innerException">Ursprüngliche Ausnahme, falls vorhanden.</param>
    public ApiException(string endpoint, HttpStatusCode? statusCode, string? responseBody, string message, Exception? innerException = null)
        : base(message, innerException, statusCode)
    {
        Endpoint = endpoint;
        ResponseBody = responseBody;
    }

    /// <summary>Relativer Endpunktpfad der fehlgeschlagenen Anfrage.</summary>
    public string Endpoint { get; }

    /// <summary>Rohantwort des Servers, falls vorhanden.</summary>
    public string? ResponseBody { get; }
}
