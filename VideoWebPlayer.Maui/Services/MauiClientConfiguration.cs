using System.Reflection;

namespace VideoWebPlayer.Maui.Services;

/// <summary>
/// Stellt die clientseitige Konfiguration für den API-Zugang bereit (Client-Gate-Token).
/// </summary>
public static class MauiClientConfiguration
{
    /// <summary>
    /// Name des HTTP-Headers, über den der API-Token gesendet wird.
    /// </summary>
    public const string ApiTokenHeaderName = "X-API-Key";

    /// <summary>
    /// Name der Umgebungsvariable, aus der der API-Token gelesen wird.
    /// </summary>
    public const string ApiTokenEnvironmentVariable = "VIDEOWEBPLAYER_MAUI_API_TOKEN";

    /// <summary>
    /// Schlüssel der Assembly-Metadaten, aus denen der API-Token gelesen wird.
    /// </summary>
    public const string ApiTokenAssemblyMetadataKey = "VideoWebPlayer.Maui.ApiToken";

    /// <summary>
    /// Platzhalter-Token, das verwendet wird, wenn kein API-Token konfiguriert ist.
    /// </summary>
    public const string DefaultApiTokenPlaceholder = "<MAUI_CLIENT_API_TOKEN>";

    /// <summary>
    /// Ruft den konfigurierten API-Token ab (Umgebungsvariable, dann Assembly-Metadaten, sonst Platzhalter).
    /// </summary>
    /// <value>Der aufgelöste API-Token für den Client-Gate-Header.</value>
    public static string ApiToken
    {
        get
        {
            var environmentValue = Environment.GetEnvironmentVariable(ApiTokenEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(environmentValue))
            {
                return environmentValue.Trim();
            }

            var metadataValue = Assembly.GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attribute => attribute.Key == ApiTokenAssemblyMetadataKey)
                ?.Value;

            return string.IsNullOrWhiteSpace(metadataValue)
                ? DefaultApiTokenPlaceholder
                : metadataValue.Trim();
        }
    }

    /// <summary>
    /// Fügt den Client-Gate-Header mit dem API-Token zum <see cref="HttpClient"/> hinzu,
    /// sofern er noch nicht gesetzt ist.
    /// </summary>
    /// <param name="httpClient">Der <see cref="HttpClient"/>, dem der Header hinzugefügt wird.</param>
    public static void ApplyClientGateHeader(HttpClient httpClient)
    {
        if (httpClient.DefaultRequestHeaders.Contains(ApiTokenHeaderName))
        {
            return;
        }

        var apiToken = ApiToken;
        if (!string.IsNullOrWhiteSpace(apiToken))
        {
            httpClient.DefaultRequestHeaders.Add(ApiTokenHeaderName, apiToken);
        }
    }
}
