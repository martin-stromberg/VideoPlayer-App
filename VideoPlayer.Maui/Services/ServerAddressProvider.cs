namespace VideoPlayer.Maui.Services;

/// <summary>
/// Löst die Server-Basisadresse auf: gespeicherte Einstellung
/// (Preferences-Key <see cref="PreferencesKey"/>), dann Umgebungsvariable
/// <see cref="EnvironmentVariable"/>, sonst die lokale IIS-Installation
/// (<see cref="DefaultAddress"/>). Die eigentliche Serververwaltung folgt in
/// einem späteren Entwicklungsschritt.
/// </summary>
public sealed class ServerAddressProvider : IServerAddressProvider
{
    public const string PreferencesKey = "ServerAddress";
    public const string EnvironmentVariable = "VIDEOPLAYER_SERVER_URL";

    /// <summary>Lokal im IIS eingerichtetes Webprojekt „VideoPlayer".</summary>
    public static readonly Uri DefaultAddress = new("http://localhost/videoplayer/");

    public Uri GetServerAddress()
    {
        var configured = Preferences.Default.Get(PreferencesKey, string.Empty);
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = Environment.GetEnvironmentVariable(EnvironmentVariable) ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(configured))
        {
            return DefaultAddress;
        }

        configured = configured.Trim();
        if (!configured.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !configured.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            configured = $"http://{configured}";
        }

        // Abschließender Schrägstrich, damit relative API-Pfade unterhalb der
        // Basisadresse aufgelöst werden.
        return new Uri(configured.TrimEnd('/') + "/");
    }
}
