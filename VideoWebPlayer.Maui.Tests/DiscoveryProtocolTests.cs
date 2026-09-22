using VideoPlayer.ApiClient.Discovery;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>Konstanten und Antwort-Parsing des Server-Discovery-Protokolls.</summary>
public class DiscoveryProtocolTests
{
    [Fact]
    public void ProtocolConstants_MatchServerSpecification()
    {
        Assert.Equal(5001, DiscoveryProtocol.UdpPort);
        Assert.Equal("VIDEOWEBPLAYER_DISCOVERY", DiscoveryProtocol.QueryText);
        Assert.Equal("VIDEOWEBPLAYER_SERVER:", DiscoveryProtocol.ResponsePrefix);
        Assert.Equal("_http._tcp.local.", DiscoveryProtocol.MdnsServiceType);
    }

    [Fact]
    public void TryParseResponse_ValidDatagram_ExtractsAddress()
    {
        var ok = DiscoveryProtocol.TryParseResponse(
            "VIDEOWEBPLAYER_SERVER:http://nas:8080/videoplayer",
            out var address);

        Assert.True(ok);
        Assert.Equal("http://nas:8080/videoplayer", address);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("VIDEOWEBPLAYER_DISCOVERY")]
    [InlineData("VIDEOWEBPLAYER_SERVER:")]
    [InlineData("videowebplayer_server:http://x")] // Präfix ist case-sensitiv
    [InlineData("OTHER:http://x")]
    public void TryParseResponse_InvalidDatagram_ReturnsFalse(string? datagram)
    {
        Assert.False(DiscoveryProtocol.TryParseResponse(datagram, out var address));
        Assert.Equal(string.Empty, address);
    }
}
