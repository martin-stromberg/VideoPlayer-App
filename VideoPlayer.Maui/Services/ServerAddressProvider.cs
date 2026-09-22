using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// Löst die Server-Basisadresse auf: gespeicherte Einstellung
/// (Ablage-Schlüssel <see cref="SettingsKey"/>, profil-präfixiert), dann
/// Umgebungsvariable <see cref="EnvironmentVariable"/>, sonst die lokale
/// IIS-Installation (<see cref="DefaultAddress"/>). Die eigentliche
/// Serververwaltung folgt in einem späteren Entwicklungsschritt.
/// </summary>
public sealed class ServerAddressProvider : IServerAddressProvider
{
    public const string SettingsKey = "ServerAddress";
    public const string EnvironmentVariable = "VIDEOPLAYER_SERVER_URL";

    /// <summary>Lokal im IIS eingerichtetes Webprojekt „VideoPlayer".</summary>
    public static readonly Uri DefaultAddress = new("http://localhost/videoplayer/");

    private readonly ISettingsStore settingsStore;
    private readonly AppProfile profile;

    public ServerAddressProvider(ISettingsStore settingsStore, AppProfile profile)
    {
        this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public Uri GetServerAddress()
    {
        var configured = settingsStore.GetString(profile.PrefixKey(SettingsKey), string.Empty);
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
