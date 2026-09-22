using VideoPlayer.ApiClient.Discovery;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Composite-Verhalten der Netzwerk-Erkennung: parallele Probes,
/// Merge + Deduplizierung nach Adresse, Fehlerisolation je Probe.
/// </summary>
public class ServerDiscoveryServiceTests
{
    private sealed class FakeDiscovery(
        DiscoverySource source,
        IReadOnlyList<DiscoveredServer>? results = null,
        Exception? failure = null) : IServerDiscovery
    {
        public Task<IReadOnlyList<DiscoveredServer>> DiscoverAsync(CancellationToken cancellationToken = default)
        {
            if (failure is not null)
            {
                throw failure;
            }

            return Task.FromResult(results ?? (IReadOnlyList<DiscoveredServer>)
            [
                new DiscoveredServer { Address = $"http://{source}-host/", Source = source }
            ]);
        }
    }

    [Fact]
    public async Task DiscoverAsync_MergesAllProbes()
    {
        var service = new ServerDiscoveryService(
        [
            new FakeDiscovery(DiscoverySource.Mdns),
            new FakeDiscovery(DiscoverySource.Udp)
        ]);

        var found = await service.DiscoverAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, found.Count);
        Assert.Contains(found, s => s.Source == DiscoverySource.Mdns);
        Assert.Contains(found, s => s.Source == DiscoverySource.Udp);
    }

    [Fact]
    public async Task DiscoverAsync_DeduplicatesByAddress()
    {
        var address = "http://nas:8080/videoplayer/";
        var service = new ServerDiscoveryService(
        [
            new FakeDiscovery(DiscoverySource.Mdns,
                [new DiscoveredServer { Address = address, Source = DiscoverySource.Mdns }]),
            new FakeDiscovery(DiscoverySource.Udp,
                [new DiscoveredServer { Address = address, Source = DiscoverySource.Udp }])
        ]);

        var found = await service.DiscoverAsync(TestContext.Current.CancellationToken);

        Assert.Single(found);
    }

    [Fact]
    public async Task DiscoverAsync_FailingProbe_DoesNotAffectOthers()
    {
        var service = new ServerDiscoveryService(
        [
            new FakeDiscovery(DiscoverySource.Mdns, failure: new InvalidOperationException("kaputt")),
            new FakeDiscovery(DiscoverySource.Udp)
        ]);

        var found = await service.DiscoverAsync(TestContext.Current.CancellationToken);

        var server = Assert.Single(found);
        Assert.Equal(DiscoverySource.Udp, server.Source);
    }

    [Fact]
    public async Task DiscoverAsync_NoResults_ReturnsEmpty()
    {
        var service = new ServerDiscoveryService(
        [
            new FakeDiscovery(DiscoverySource.Mdns, results: []),
            new FakeDiscovery(DiscoverySource.Udp, results: [])
        ]);

        Assert.Empty(await service.DiscoverAsync(TestContext.Current.CancellationToken));
    }
}
