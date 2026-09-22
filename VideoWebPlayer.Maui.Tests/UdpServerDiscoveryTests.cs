using System.Net;
using System.Net.Sockets;
using System.Text;
using VideoPlayer.ApiClient.Discovery;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// UDP-Broadcast-Erkennung: Protokoll-Austausch gegen einen lokalen
/// Loopback-Responder sowie die Adress-Auflösung der gemeldeten Antworten.
/// </summary>
public class UdpServerDiscoveryTests
{
    /// <summary>Prüft ResolveAnnouncedAddress: normalisiert den abschließenden Schrägstrich.</summary>
    [Fact]
    public void ResolveAnnouncedAddress_NormalizesTrailingSlash()
    {
        var address = UdpServerDiscovery.ResolveAnnouncedAddress(
            "http://nas:8080/videoplayer", IPAddress.Parse("192.168.1.10"));

        Assert.Equal("http://nas:8080/videoplayer/", address);
    }

    /// <summary>Prüft ResolveAnnouncedAddress: fügt ein fehlendes Schema hinzu.</summary>
    [Fact]
    public void ResolveAnnouncedAddress_AddsMissingScheme()
    {
        var address = UdpServerDiscovery.ResolveAnnouncedAddress(
            "nas:8080/videoplayer/", IPAddress.Parse("192.168.1.10"));

        Assert.Equal("http://nas:8080/videoplayer/", address);
    }

    /// <summary>Prüft ResolveAnnouncedAddress: bei Remote-Loopback → verwendet die Responder-Adresse.</summary>
    [Fact]
    public void ResolveAnnouncedAddress_RemoteLoopback_UsesResponderAddress()
    {
        // Meldet ein entfernter Server „localhost", meint er sich selbst.
        var address = UdpServerDiscovery.ResolveAnnouncedAddress(
            "http://localhost:8080/videoplayer", IPAddress.Parse("192.168.1.10"));

        Assert.Equal("http://192.168.1.10:8080/videoplayer/", address);
    }

    /// <summary>Prüft ResolveAnnouncedAddress: bei lokalem Loopback → bleibt Loopback.</summary>
    [Fact]
    public void ResolveAnnouncedAddress_LocalLoopback_StaysLoopback()
    {
        var address = UdpServerDiscovery.ResolveAnnouncedAddress(
            "http://localhost/videoplayer", IPAddress.Loopback);

        Assert.Equal("http://localhost/videoplayer/", address);
    }

    /// <summary>Prüft ResolveAnnouncedAddress: wenn ungültig → gibt null zurück.</summary>
    [Fact]
    public void ResolveAnnouncedAddress_Invalid_ReturnsNull()
    {
        Assert.Null(UdpServerDiscovery.ResolveAnnouncedAddress(":::", IPAddress.Loopback));
    }

    /// <summary>Prüft DiscoverAsync: bei <c>ResponderReplies</c> → gibt Server zurück.</summary>
    [Fact]
    public async Task DiscoverAsync_ResponderReplies_ReturnsServer()
    {
        // Lokaler Responder, der das Protokoll beantwortet.
        using var responder = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)responder.Client.LocalEndPoint!).Port;

        var ct = TestContext.Current.CancellationToken;
        var responderTask = Task.Run(async () =>
        {
            var request = await responder.ReceiveAsync(ct);
            var payload = Encoding.UTF8.GetBytes(
                $"{DiscoveryProtocol.ResponsePrefix}http://localhost:4711/videoplayer");
            await responder.SendAsync(payload, request.RemoteEndPoint, ct);
        }, ct);

        var discovery = new UdpServerDiscovery(
            new IPEndPoint(IPAddress.Loopback, port), TimeSpan.FromSeconds(3));

        var found = await discovery.DiscoverAsync(ct);
        await responderTask.WaitAsync(TimeSpan.FromSeconds(5), ct);

        var server = Assert.Single(found);
        Assert.Equal(DiscoverySource.Udp, server.Source);
        Assert.Equal("http://localhost:4711/videoplayer/", server.Address);
    }

    /// <summary>Prüft DiscoverAsync: der Responder ignoriert fremde Datagramme.</summary>
    [Fact]
    public async Task DiscoverAsync_ResponderIgnoresForeignDatagrams()
    {
        using var responder = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)responder.Client.LocalEndPoint!).Port;

        var ct = TestContext.Current.CancellationToken;
        var responderTask = Task.Run(async () =>
        {
            var request = await responder.ReceiveAsync(ct);
            // Antwort ohne das geforderte Präfix → keine Übernahme.
            var payload = Encoding.UTF8.GetBytes("UNRELATED_RESPONSE");
            await responder.SendAsync(payload, request.RemoteEndPoint, ct);
        }, ct);

        var discovery = new UdpServerDiscovery(
            new IPEndPoint(IPAddress.Loopback, port), TimeSpan.FromMilliseconds(400));

        var found = await discovery.DiscoverAsync(ct);
        await responderTask.WaitAsync(TimeSpan.FromSeconds(5), ct);

        Assert.Empty(found);
    }

    /// <summary>Prüft DiscoverAsync: ohne Responder → gibt eine leere Liste zurück.</summary>
    [Fact]
    public async Task DiscoverAsync_NoResponder_ReturnsEmpty()
    {
        var discovery = new UdpServerDiscovery(
            new IPEndPoint(IPAddress.Loopback, 1), TimeSpan.FromMilliseconds(300));

        var found = await discovery.DiscoverAsync(TestContext.Current.CancellationToken);

        Assert.Empty(found);
    }
}
