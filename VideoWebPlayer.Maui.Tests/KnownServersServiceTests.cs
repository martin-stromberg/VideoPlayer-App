using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Serververwaltung auf der realen SQLite-Ablage: Liste bekannter Server,
/// zuletzt verbundener Server und Migration des Legacy-Ablageschlüssels.
/// Läuft in der gemeinsamen SQLite-Collection, damit <c>ResetPool()</c> im
/// Dispose nicht parallel zu anderen Datenbank-Tests ausgeführt wird.
/// </summary>
[Collection("SqliteDatabase")]
public class KnownServersServiceTests : IDisposable
{
    private readonly string root;
    private readonly InMemorySettingsStore settingsStore = new();
    private readonly AppProfile profile = AppProfile.Create(null);

    public KnownServersServiceTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-server-tests", Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        SQLite.SQLiteAsyncConnection.ResetPool();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private KnownServersService CreateService()
    {
        var layout = new AppStorageLayout(
            backupRoot: Path.Combine(root, "backup"),
            noBackupRoot: Path.Combine(root, "nobackup"),
            cacheRoot: Path.Combine(root, "cache"),
            profile: profile);
        layout.EnsureDirectoriesExist();

        var database = new BackupDatabase(layout);
        database.InitializeAsync().GetAwaiter().GetResult();
        return new KnownServersService(database, settingsStore, profile);
    }

    [Fact]
    public async Task Initially_NoServersNoLastConnected()
    {
        var service = CreateService();

        Assert.Empty(await service.GetAllAsync());
        Assert.Null(await service.GetLastConnectedAddressAsync());
    }

    [Fact]
    public async Task SetActiveServer_InsertsEntryAndLastConnected()
    {
        var service = CreateService();

        var server = await service.SetActiveServerAsync("http://nas/videoplayer", "NAS");

        Assert.Equal("http://nas/videoplayer/", server.Address);
        Assert.Equal("NAS", server.DisplayName);
        Assert.Equal("http://nas/videoplayer/", await service.GetLastConnectedAddressAsync());

        var all = await service.GetAllAsync();
        var entry = Assert.Single(all);
        Assert.Equal("http://nas/videoplayer/", entry.Address);
    }

    [Fact]
    public async Task SetActiveServer_SameAddress_UpsertsWithoutDuplicate()
    {
        var service = CreateService();

        await service.SetActiveServerAsync("http://nas/videoplayer", "NAS");
        await Task.Delay(5, TestContext.Current.CancellationToken); // LastUsedAtUtc unterscheidbar machen
        var second = await service.SetActiveServerAsync("http://NAS/videoplayer/", "NAS neu");

        var all = await service.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("NAS neu", second.DisplayName);
    }

    [Fact]
    public async Task SetActiveServer_OrdersByLastUsedDescending()
    {
        var service = CreateService();

        await service.SetActiveServerAsync("http://eins/", null);
        await Task.Delay(5, TestContext.Current.CancellationToken);
        await service.SetActiveServerAsync("http://zwei/", null);

        var all = await service.GetAllAsync();
        Assert.Equal(2, all.Count);
        Assert.Equal("http://zwei/", all[0].Address);
        Assert.Equal("http://zwei/", await service.GetLastConnectedAddressAsync());
    }

    [Fact]
    public async Task SetActiveServer_InvalidAddress_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SetActiveServerAsync("ftp://server/"));
    }

    [Fact]
    public async Task MigrateLegacy_MovesAddressIntoDatabase()
    {
        var legacyKey = profile.PrefixKey(KnownServersService.LegacyServerAddressKey);
        settingsStore.Set(legacyKey, "http://altserver/videoplayer");
        var service = CreateService();

        await service.MigrateLegacyServerAddressAsync();

        Assert.Equal("http://altserver/videoplayer/", await service.GetLastConnectedAddressAsync());
        Assert.Single(await service.GetAllAsync());
        Assert.False(settingsStore.Contains(legacyKey));
    }

    [Fact]
    public async Task MigrateLegacy_InvalidAddress_IsDroppedWithoutEntry()
    {
        var legacyKey = profile.PrefixKey(KnownServersService.LegacyServerAddressKey);
        settingsStore.Set(legacyKey, ":::kaputt");
        var service = CreateService();

        await service.MigrateLegacyServerAddressAsync();

        Assert.Null(await service.GetLastConnectedAddressAsync());
        Assert.Empty(await service.GetAllAsync());
        Assert.False(settingsStore.Contains(legacyKey));
    }

    [Fact]
    public async Task MigrateLegacy_WithoutLegacyValue_IsNoOp()
    {
        var service = CreateService();

        await service.MigrateLegacyServerAddressAsync();

        Assert.Empty(await service.GetAllAsync());
        Assert.Null(await service.GetLastConnectedAddressAsync());
    }

    [Fact]
    public async Task ServersLandInBackupDatabase_NotInCache()
    {
        // Die Serverliste gehört in die backup-pflichtige Datenbank —
        // cache_database darf keine known_servers-Tabelle besitzen.
        var layout = new AppStorageLayout(
            backupRoot: Path.Combine(root, "backup"),
            noBackupRoot: Path.Combine(root, "nobackup"),
            cacheRoot: Path.Combine(root, "cache"),
            profile: profile);
        layout.EnsureDirectoriesExist();

        var cache = new CacheDatabase(layout);
        await cache.InitializeAsync();
        var tables = await cache.Connection
            .QueryAsync<TableNameRow>("SELECT name AS Name FROM sqlite_master WHERE type = 'table'");
        Assert.DoesNotContain(tables, t => t.Name == "known_servers");
        Assert.DoesNotContain(tables, t => t.Name == "backup_state");
    }

    private sealed class TableNameRow
    {
        public string Name { get; set; } = string.Empty;
    }
}
