// Die Developer-API ist auf DEBUG/DEV_API kompiliert — in Release fehlt
// der Namespace und diese Tests haetten kein Compile-Ziel.
#if DEBUG || DEV_API
using System.Text.Json;
using VideoPlayer.Mvu;
using VideoPlayer.Mvu.DevApi;
using VideoPlayer.Storage;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Einzeltests des kuratierten Modell-Snapshots der Developer-API
/// (<see cref="DevApiModelSnapshot"/>): Feldübernahme, camelCase-JSON und
/// Abwesenheit geheimhaltungsbedürftiger Schlüssel.
/// </summary>
public class DevApiModelSnapshotTests
{
    /// <summary>Der Snapshot übernimmt die Zustandsfelder des Modells.</summary>
    [Fact]
    public void FromModel_CopiesStateFields()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.ServerSetup,
            ConnectionState = ConnectionState.NeedsServerSetup,
            ServerHealth = ServerHealthStatus.Unreachable,
            IsStartupRunning = false,
            IsOnline = false,
            ActiveServerAddress = "http://server/videoplayer/",
            IsPairingRequired = true,
            ServerSetupError = "Server nicht erreichbar",
            LoginUsername = "martin",
            StorageNotice = "Ablage neu initialisiert",
            KnownServers =
            [
                new KnownServer
                {
                    Address = "http://server/videoplayer/",
                    DisplayName = "Wohnzimmer",
                    LastUsedAtUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
                }
            ]
        };

        var snapshot = DevApiModelSnapshot.FromModel(model);

        Assert.Equal("ServerSetup", snapshot.CurrentPage);
        Assert.Equal("NeedsServerSetup", snapshot.ConnectionState);
        Assert.Equal("Unreachable", snapshot.ServerHealth);
        Assert.False(snapshot.IsStartupRunning);
        Assert.False(snapshot.IsOnline);
        Assert.False(snapshot.IsOffline);
        Assert.True(snapshot.IsPairingRequired);
        Assert.Equal("Server nicht erreichbar", snapshot.ServerSetupError);
        Assert.Equal("martin", snapshot.LoginUsername);
        Assert.Equal("Ablage neu initialisiert", snapshot.StorageNotice);
        var known = Assert.Single(snapshot.KnownServers);
        Assert.Equal("Wohnzimmer", known.NameOrAddress);
        Assert.Equal("http://server/videoplayer/", known.Address);
    }

    /// <summary>Der Snapshot mappt den Wiedergabezustand des Modells.</summary>
    [Fact]
    public void DevApiModelSnapshot_MapsPlayerState()
    {
        var model = new AppModel
        {
            Player = new PlayerState
            {
                MediaType = "movie",
                MediaId = 4711,
                IsPlaying = true,
                Position = TimeSpan.FromSeconds(42.5),
                Error = "Die Wiedergabe ist fehlgeschlagen."
            }
        };

        var snapshot = DevApiModelSnapshot.FromModel(model);

        Assert.True(snapshot.PlayerIsActive);
        Assert.True(snapshot.PlayerIsPlaying);
        Assert.Equal(42.5, snapshot.PlayerPositionSeconds);
        Assert.Equal("Die Wiedergabe ist fehlgeschlagen.", snapshot.PlayerError);
    }

    /// <summary>Die JSON-Serialisierung nutzt camelCase-Feldnamen.</summary>
    [Fact]
    public void Snapshot_SerializesCamelCase()
    {
        var json = JsonSerializer.Serialize(
            DevApiModelSnapshot.FromModel(new AppModel()),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("\"currentPage\"", json, StringComparison.Ordinal);
        Assert.Contains("\"connectionState\"", json, StringComparison.Ordinal);
        Assert.Contains("\"isPairingRequired\"", json, StringComparison.Ordinal);
        Assert.Contains("\"serverHealthText\"", json, StringComparison.Ordinal);
    }

    /// <summary>Der Snapshot enthält per Konstruktion keine Geheimnis-Felder.</summary>
    [Fact]
    public void Snapshot_ContainsNoSecretFields()
    {
        var json = JsonSerializer.Serialize(
            DevApiModelSnapshot.FromModel(new AppModel()),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)).ToLowerInvariant();

        Assert.DoesNotContain("token", json, StringComparison.Ordinal);
        Assert.DoesNotContain("password", json, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", json, StringComparison.Ordinal);
    }
}
#endif
