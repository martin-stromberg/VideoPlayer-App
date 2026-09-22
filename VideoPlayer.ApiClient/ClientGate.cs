using System.Reflection;

namespace VideoPlayer.ApiClient;

/// <summary>
/// Löst den API-Schlüssel für das serverseitige Client-Gate auf und stellt den
/// zugehörigen Header-Namen bereit. Quellen in Reihenfolge: Umgebungsvariable,
/// Assembly-Metadata (Build-Eigenschaft), Platzhalter.
/// </summary>
public static class ClientGate
{
    /// <summary>HTTP-Headername, über den der API-Schlüssel gesendet wird.</summary>
    public const string HeaderName = "X-API-Key";

    /// <summary>Umgebungsvariable mit dem API-Schlüssel (höchste Priorität).</summary>
    public const string ApiTokenEnvironmentVariable = "VIDEOWEBPLAYER_MAUI_API_TOKEN";

    /// <summary>Assembly-Metadata-Schlüssel, über den der API-Schlüssel beim Build injiziert wird.</summary>
    public const string ApiTokenAssemblyMetadataKey = "VideoPlayer.ApiClient.ApiToken";

    /// <summary>Platzhalter, wenn weder Umgebungsvariable noch Assembly-Metadata einen Schlüssel liefern.</summary>
    public const string ApiTokenPlaceholder = "<MAUI_CLIENT_API_TOKEN>";

    /// <summary>
    /// Löst den API-Schlüssel auf: Umgebungsvariable vor Assembly-Metadata
    /// vor Platzhalter.
    /// </summary>
    /// <returns>Der aufgelöste API-Schlüssel bzw. der Platzhalter.</returns>
    public static string Resolve()
    {
        return Resolve(
            Environment.GetEnvironmentVariable(ApiTokenEnvironmentVariable),
            typeof(ClientGate).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attribute => attribute.Key == ApiTokenAssemblyMetadataKey)
                ?.Value);
    }

    internal static string Resolve(string? environmentValue, string? metadataValue)
    {
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return environmentValue.Trim();
        }

        return string.IsNullOrWhiteSpace(metadataValue)
            ? ApiTokenPlaceholder
            : metadataValue.Trim();
    }
}
