using VideoPlayer.ApiClient;

namespace VideoWebPlayer.Maui.Tests;

public class ClientGateTests
{
    [Fact]
    public void Resolve_PrefersEnvironmentVariable()
    {
        var resolved = ClientGate.Resolve("  env-key  ", "metadata-key");

        Assert.Equal("env-key", resolved);
    }

    [Fact]
    public void Resolve_FallsBackToAssemblyMetadata()
    {
        var resolved = ClientGate.Resolve(null, " metadata-key ");

        Assert.Equal("metadata-key", resolved);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public void Resolve_FallsBackToPlaceholder(string? environmentValue, string? metadataValue)
    {
        var resolved = ClientGate.Resolve(environmentValue, metadataValue);

        Assert.Equal(ClientGate.ApiTokenPlaceholder, resolved);
    }
}
