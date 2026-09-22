using VideoPlayer.ApiClient;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>Tests für <c>ClientGate</c>.</summary>
public class ClientGateTests
{
    /// <summary>Prüft Resolve: bevorzugt Umgebungsvariable.</summary>
    [Fact]
    public void Resolve_PrefersEnvironmentVariable()
    {
        var resolved = ClientGate.Resolve("  env-key  ", "metadata-key");

        Assert.Equal("env-key", resolved);
    }

    /// <summary>Prüft Resolve: fällt zurück zu Assemblymetadaten.</summary>
    [Fact]
    public void Resolve_FallsBackToAssemblyMetadata()
    {
        var resolved = ClientGate.Resolve(null, " metadata-key ");

        Assert.Equal("metadata-key", resolved);
    }

    /// <summary>Prüft Resolve: fällt zurück zu Platzhalter.</summary>
    /// <param name="environmentValue">Der Wert aus der Umgebungsvariable.</param>
    /// <param name="metadataValue">Der Wert aus den Assembly-Metadaten.</param>
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
