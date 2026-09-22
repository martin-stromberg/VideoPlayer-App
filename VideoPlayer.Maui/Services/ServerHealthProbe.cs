using VideoPlayer.ApiClient;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// Führt die Erreichbarkeitsprüfung über den plattformneutralen
/// <see cref="VideoPlayerApiClient"/> aus (<c>GET api/health</c>) — für die
/// aufgelöste Serveradresse oder eine konkrete Kandidatenadresse.
/// </summary>
public sealed class ServerHealthProbe : IServerHealthProbe
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    private readonly IServerAddressProvider addressProvider;

    public ServerHealthProbe(IServerAddressProvider addressProvider)
    {
        this.addressProvider = addressProvider;
    }

    public async Task<bool> CheckAsync(CancellationToken cancellationToken = default)
    {
        return await CheckAsync(
            await addressProvider.GetServerAddressAsync(cancellationToken),
            cancellationToken);
    }

    public async Task<bool> CheckAsync(Uri baseAddress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);
        try
        {
            using var httpClient = new HttpClient
            {
                BaseAddress = baseAddress,
                Timeout = RequestTimeout
            };
            var client = new VideoPlayerApiClient(httpClient);
            return await client.CheckHealthAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or UriFormatException or HttpRequestException)
        {
            System.Diagnostics.Debug.WriteLine($"[ServerHealthProbe] Erreichbarkeitsprüfung fehlgeschlagen: {ex.Message}");
            return false;
        }
    }
}
