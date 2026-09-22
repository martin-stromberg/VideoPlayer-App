using System.Reflection;

namespace VideoPlayer.ApiClient;

/// <summary>
/// Löst den API-Schlüssel für das serverseitige Client-Gate auf und stellt den
/// zugehörigen Header-Namen bereit. Quellen in Reihenfolge: Umgebungsvariable,
/// Assembly-Metadata (Build-Eigenschaft), Platzhalter.
/// </summary>
public static class ClientGate
{
    public const string HeaderName = "X-API-Key";
    public const string ApiTokenEnvironmentVariable = "VIDEOWEBPLAYER_MAUI_API_TOKEN";
    public const string ApiTokenAssemblyMetadataKey = "VideoPlayer.ApiClient.ApiToken";
    public const string ApiTokenPlaceholder = "<MAUI_CLIENT_API_TOKEN>";

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
