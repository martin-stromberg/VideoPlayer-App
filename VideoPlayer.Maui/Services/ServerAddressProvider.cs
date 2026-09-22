using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// Löst die Server-Basisadresse auf: zuletzt verbundener Server aus der
/// backup-pflichtigen Serververwaltung (<see cref="KnownServersService"/>,
/// inkl. Migration des bisherigen Ablage-Schlüssels), dann Umgebungsvariable
/// <see cref="EnvironmentVariable"/>, sonst die lokale IIS-Installation
/// (<see cref="DefaultAddress"/> — nur als Auflösungs-Fallback, nicht als
/// „hinterlegter Server" für die Erststart-Erkennung).
/// </summary>
public sealed class ServerAddressProvider : IServerAddressProvider
{
    public const string EnvironmentVariable = "VIDEOPLAYER_SERVER_URL";

    /// <summary>Lokal im IIS eingerichtetes Webprojekt „VideoPlayer".</summary>
    public static readonly Uri DefaultAddress = new("http://localhost/videoplayer/");

    private readonly KnownServersService knownServers;

    public ServerAddressProvider(KnownServersService knownServers)
    {
        this.knownServers = knownServers ?? throw new ArgumentNullException(nameof(knownServers));
    }

    public async Task<Uri?> GetConfiguredServerAddressAsync(CancellationToken cancellationToken = default)
    {
        var configured = await knownServers.GetLastConnectedAddressAsync();
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = Environment.GetEnvironmentVariable(EnvironmentVariable) ?? string.Empty;
        }

        return ServerAddress.TryNormalize(configured, out var normalized)
            ? new Uri(normalized)
            : null;
    }

    public async Task<Uri> GetServerAddressAsync(CancellationToken cancellationToken = default)
    {
        return await GetConfiguredServerAddressAsync(cancellationToken) ?? DefaultAddress;
    }
}
