using Zeroconf;

namespace VideoPlayer.ApiClient.Discovery;

/// <summary>
/// mDNS-Dienstsuche über Zeroconf auf <see cref="DiscoveryProtocol.MdnsServiceType"/>.
/// Hinweis: Der Server veröffentlicht derzeit keinen mDNS-Dienst
/// (Server-Issue martin-stromberg/VideoPlayer#223) — die Suche liefert nur
/// Treffer, sobald der Server (oder ein anderer HTTP-Dienst) announced;
/// Fehltreffer werden später per Verbindungsprüfung ausgefiltert.
/// </summary>
public sealed class MdnsServerDiscovery : IServerDiscovery
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

    private readonly TimeSpan timeout;

    /// <summary>Erstellt die Erkennung mit dem Standard-Zeitfenster (3 s).</summary>
    public MdnsServerDiscovery()
        : this(DefaultTimeout)
    {
    }

    internal MdnsServerDiscovery(TimeSpan timeout)
    {
        this.timeout = timeout;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DiscoveredServer>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var found = new List<DiscoveredServer>();
        try
        {
            var responses = await ZeroconfResolver.ResolveAsync(
                DiscoveryProtocol.MdnsServiceType,
                timeout,
                cancellationToken: cancellationToken);

            foreach (var response in responses)
            {
                var host = response.IPAddress;
                if (string.IsNullOrWhiteSpace(host))
                {
                    continue;
                }

                foreach (var service in response.Services.Values)
                {
                    found.Add(new DiscoveredServer
                    {
                        Address = $"http://{host}:{service.Port}/",
                        Source = DiscoverySource.Mdns,
                        Name = service.Name
                    });
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"[MdnsServerDiscovery] {ex.Message}");
        }

        return found;
    }
}
