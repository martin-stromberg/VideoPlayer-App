#if DEBUG || DEV_API
namespace msTools.DeveloperApi;

/// <summary>
/// Signalisiert eine syntaktisch ungültige HTTP-Anfrage; der Server
/// antwortet darauf mit <c>400 Bad Request</c>.
/// </summary>
public sealed class DevApiRequestException : Exception
{
    /// <summary>Erstellt die Ausnahme mit einem deutschen Fehlertext.</summary>
    /// <param name="message">Beschreibung des Parse-Fehlers.</param>
    public DevApiRequestException(string message)
        : base(message)
    {
    }
}
#endif
