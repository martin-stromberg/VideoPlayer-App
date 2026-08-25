using System.Reflection;

namespace VideoWebPlayer.Maui.Services;

public static class MauiClientConfiguration
{
    public const string ApiTokenHeaderName = "X-API-Key";
    public const string ApiTokenEnvironmentVariable = "VIDEOWEBPLAYER_MAUI_API_TOKEN";
    public const string ApiTokenAssemblyMetadataKey = "VideoWebPlayer.Maui.ApiToken";
    public const string DefaultApiTokenPlaceholder = "<MAUI_CLIENT_API_TOKEN>";

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
