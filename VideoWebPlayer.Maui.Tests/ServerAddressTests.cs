using VideoPlayer.Storage;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>Normalisierung von Serveradressen in die kanonische Speicherform.</summary>
public class ServerAddressTests
{
    [Theory]
    [InlineData("http://server/videoplayer", "http://server/videoplayer/")]
    [InlineData("http://server/videoplayer/", "http://server/videoplayer/")]
    [InlineData("server", "http://server/")]
    [InlineData("nas:8080", "http://nas:8080/")]
    [InlineData("  http://Server/Videoplayer  ", "http://server/Videoplayer/")]
    [InlineData("https://server:8443/app/", "https://server:8443/app/")]
    public void TryNormalize_ValidInput_ProducesCanonicalForm(string input, string expected)
    {
        Assert.True(ServerAddress.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("keine gültige adresse")]
    [InlineData("ftp://server/")]
    public void TryNormalize_InvalidInput_ReturnsFalse(string? input)
    {
        Assert.False(ServerAddress.TryNormalize(input, out var normalized));
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void TryNormalize_HostIsLowerCased()
    {
        Assert.True(ServerAddress.TryNormalize("HTTP://NAS.local:8080/App", out var normalized));
        Assert.Equal("http://nas.local:8080/App/", normalized);
    }

    [Fact]
    public void Normalize_InvalidInput_Throws()
    {
        Assert.Throws<ArgumentException>(() => ServerAddress.Normalize("ftp://x/"));
    }
}
