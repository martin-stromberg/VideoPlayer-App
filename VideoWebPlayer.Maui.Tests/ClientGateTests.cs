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

    /// <summary>Prüft Resolve: Token-Datei greift, wenn Umgebungsvariable und Metadaten leer sind.</summary>
    [Fact]
    public void Resolve_FallsBackToTokenFile()
    {
        var resolved = ClientGate.Resolve(null, null, " file-key ");

        Assert.Equal("file-key", resolved);
    }

    /// <summary>Prüft Resolve: Umgebungsvariable und Metadaten haben Vorrang vor der Token-Datei.</summary>
    /// <param name="environmentValue">Der Wert aus der Umgebungsvariable.</param>
    /// <param name="metadataValue">Der Wert aus den Assembly-Metadaten.</param>
    /// <param name="expected">Der erwartete aufgelöste Schlüssel.</param>
    [Theory]
    [InlineData("env-key", "metadata-key", "env-key")]
    [InlineData(null, "metadata-key", "metadata-key")]
    public void Resolve_PrefersEnvironmentAndMetadataOverFile(
        string? environmentValue,
        string? metadataValue,
        string expected)
    {
        var resolved = ClientGate.Resolve(environmentValue, metadataValue, "file-key");

        Assert.Equal(expected, resolved);
    }

    /// <summary>Prüft Resolve: ein Platzhalter-Wert gilt als nicht konfiguriert und wird übersprungen.</summary>
    /// <param name="environmentValue">Der Wert aus der Umgebungsvariable.</param>
    /// <param name="metadataValue">Der Wert aus den Assembly-Metadaten.</param>
    /// <param name="fileValue">Der Wert aus der Token-Datei.</param>
    /// <param name="expected">Der erwartete aufgelöste Schlüssel.</param>
    [Theory]
    [InlineData(ClientGate.ApiTokenPlaceholder, null, "file-key", "file-key")]
    [InlineData(null, ClientGate.ApiTokenPlaceholder, "file-key", "file-key")]
    [InlineData(null, null, ClientGate.ApiTokenPlaceholder, ClientGate.ApiTokenPlaceholder)]
    public void Resolve_SkipsPlaceholderValues(
        string? environmentValue,
        string? metadataValue,
        string? fileValue,
        string expected)
    {
        var resolved = ClientGate.Resolve(environmentValue, metadataValue, fileValue);

        Assert.Equal(expected, resolved);
    }
}
