using VideoPlayer.ApiClient;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>Tests für <c>ClientGate</c>.</summary>
[Collection("ClientGate")]
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

    /// <summary>Prüft Resolve: das gepaarte Geräte-Token schlägt alle statischen Quellen.</summary>
    [Fact]
    public void Resolve_PrefersDeviceTokenOverEnvironment()
    {
        var resolved = ClientGate.Resolve(" device-token ", "env-key", "metadata-key", "file-key");

        Assert.Equal("device-token", resolved);
    }

    /// <summary>
    /// Prüft Resolve: ein leerer oder platzhalterartiger Geräte-Token wird
    /// übersprungen — die statische Quellenkette greift.
    /// </summary>
    /// <param name="deviceToken">Der vom Provider gelieferte Wert.</param>
    /// <param name="expected">Der erwartete aufgelöste Schlüssel.</param>
    [Theory]
    [InlineData(null, "env-key")]
    [InlineData("", "env-key")]
    [InlineData("   ", "env-key")]
    [InlineData(ClientGate.ApiTokenPlaceholder, "env-key")]
    public void Resolve_FallsBack_WhenDeviceTokenProviderReturnsPlaceholderOrNull(
        string? deviceToken,
        string expected)
    {
        var resolved = ClientGate.Resolve(deviceToken, "env-key", "metadata-key", "file-key");

        Assert.Equal(expected, resolved);
    }

    /// <summary>Prüft Resolve: das öffentliche Resolve wertet den DeviceTokenProvider zuerst aus.</summary>
    [Fact]
    public void Resolve_PublicOverload_ConsultsDeviceTokenProvider()
    {
        try
        {
            ClientGate.DeviceTokenProvider = () => "provider-token";

            Assert.Equal("provider-token", ClientGate.Resolve());
        }
        finally
        {
            ClientGate.DeviceTokenProvider = null;
        }
    }

    /// <summary>Prüft ResolveStatic: ignoriert den DeviceTokenProvider (nur statische Quellen).</summary>
    [Fact]
    public void ResolveStatic_IgnoresDeviceTokenProvider()
    {
        try
        {
            ClientGate.DeviceTokenProvider = () => "provider-token";

            // Der Provider-Wert darf in der statischen Auflösung nie auftauchen.
            Assert.NotEqual("provider-token", ClientGate.ResolveStatic());

            // Ohne Provider liefern beide Auflösungen denselben statischen Wert
            // (Umgebungsvariable/Metadata/Datei der aktuellen Maschine).
            ClientGate.DeviceTokenProvider = null;
            Assert.Equal(ClientGate.Resolve(), ClientGate.ResolveStatic());
        }
        finally
        {
            ClientGate.DeviceTokenProvider = null;
        }
    }
}
