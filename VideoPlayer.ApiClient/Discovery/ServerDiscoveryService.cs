namespace VideoPlayer.ApiClient.Discovery;

/// <summary>
/// Führt alle registrierten Discovery-Wege (<see cref="IServerDiscovery"/>)
/// parallel aus und merged die Treffer — dedupliziert nach normalisierter
/// Adresse (kleingeschriebene Hosts, einheitlicher Trailing-Slash).
/// </summary>
public sealed class ServerDiscoveryService
{
    private readonly IReadOnlyList<IServerDiscovery> probes;

    public ServerDiscoveryService(IEnumerable<IServerDiscovery> probes)
    {
        ArgumentNullException.ThrowIfNull(probes);
        this.probes = probes.ToList();
    }

    /// <summary>Standardzusammenstellung: mDNS + UDP-Broadcast.</summary>
    public static ServerDiscoveryService CreateDefault()
    {
        return new ServerDiscoveryService(
        [
            new MdnsServerDiscovery(),
            new UdpServerDiscovery()
        ]);
    }

    public async Task<IReadOnlyList<DiscoveredServer>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(
            probes.Select(probe => SafeDiscoverAsync(probe, cancellationToken)));

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var merged = new List<DiscoveredServer>();
        foreach (var server in results.SelectMany(r => r))
        {
            if (seen.Add(server.Address))
            {
                merged.Add(server);
            }
        }

        return merged;
    }

    private static async Task<IReadOnlyList<DiscoveredServer>> SafeDiscoverAsync(
        IServerDiscovery probe, CancellationToken cancellationToken)
    {
        try
        {
            return await probe.DiscoverAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"[ServerDiscoveryService] Probe {probe.GetType().Name}: {ex.Message}");
            return [];
        }
    }
}
