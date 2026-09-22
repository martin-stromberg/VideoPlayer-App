using System.Net;

namespace VideoPlayer.ApiClient;

/// <summary>
/// Fehler einer Server-API-Antwort nach Ausschöpfung der Wiederholungsstrategie.
/// </summary>
public class ApiException : HttpRequestException
{
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
