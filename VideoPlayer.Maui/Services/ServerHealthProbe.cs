using VideoPlayer.ApiClient;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// Führt die Erreichbarkeitsprüfung über den plattformneutralen
/// <see cref="VideoPlayerApiClient"/> aus (<c>GET api/health</c>) — für die
/// aufgelöste Serveradresse oder eine konkrete Kandidatenadresse. Der
/// Gate-Schlüssel wird adressspezifisch aufgelöst: das hinterlegte
/// Geräte-Token der geprüften Adresse bzw. die statische Auflösung —
/// niemals das Geräte-Token eines anderen (aktiven) Servers.
/// </summary>
public sealed class ServerHealthProbe : IServerHealthProbe
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    private readonly IServerAddressProvider addressProvider;
    private readonly AppSecretsService secrets;

    /// <summary>Initialisiert eine neue Instanz der <see cref="ServerHealthProbe"/>-Klasse.</summary>
    /// <param name="addressProvider">Liefert die aufgelöste Server-Basisadresse für die parameterlose Prüfung.</param>
    /// <param name="secrets">Sichere Ablage der Geräte-Tokens je Serveradresse.</param>
    public ServerHealthProbe(IServerAddressProvider addressProvider, AppSecretsService secrets)
    {
        this.addressProvider = addressProvider;
        this.secrets = secrets;
    }

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        return await CheckAsync(
            await addressProvider.GetServerAddressAsync(cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckAsync(Uri baseAddress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);

        // Der Gate-Schlüssel gehört zur geprüften Adresse: hinterlegtes
        // Geräte-Token dieses Servers oder die statische Auflösung. Der
        // aktive ClientGate.DeviceTokenProvider bedient ggf. einen anderen
        // Server — dessen Token an eine fremde Kandidatenadresse zu senden
        // wäre eine Credential-Weitergabe.
        var apiKey = await ResolveGateKeyAsync(baseAddress);
        try
        {
            using var httpClient = new HttpClient
            {
                BaseAddress = baseAddress,
                Timeout = RequestTimeout
            };
            var client = new VideoPlayerApiClient(
                httpClient,
                new VideoPlayerApiClientOptions { ApiKey = apiKey });
            return await client.CheckHealthAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or UriFormatException or HttpRequestException)
        {
            System.Diagnostics.Debug.WriteLine($"[ServerHealthProbe] Erreichbarkeitsprüfung fehlgeschlagen: {ex.Message}");
            return HealthCheckResult.Unreachable;
        }
    }

    /// <summary>
    /// Löst den Gate-Schlüssel für eine konkrete Serveradresse auf:
    /// hinterlegtes Geräte-Token der Adresse, sonst die statischen Quellen.
    /// </summary>
    /// <param name="baseAddress">Die zu prüfende Server-Basisadresse.</param>
    /// <returns>Der aufgelöste Gate-Schlüssel bzw. der Platzhalter.</returns>
    private async Task<string> ResolveGateKeyAsync(Uri baseAddress)
    {
        try
        {
            var deviceToken = await secrets.GetDeviceTokenAsync(baseAddress.ToString());
            if (!string.IsNullOrWhiteSpace(deviceToken))
            {
                return deviceToken;
            }
        }
        catch (Exception ex)
        {
            // Ein Lesefehler der sicheren Ablage blockiert die Prüfung
            // nicht — die statische Auflösung greift.
            System.Diagnostics.Debug.WriteLine($"[ServerHealthProbe] Geräte-Token des Kandidaten nicht lesbar: {ex.Message}");
        }

        return ClientGate.ResolveStatic();
    }
}
