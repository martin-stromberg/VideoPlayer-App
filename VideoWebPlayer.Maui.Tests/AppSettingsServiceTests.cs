using VideoPlayer.Storage;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

public class AppSettingsServiceTests
{
    private static AppSettingsService CreateService(InMemorySettingsStore store, string? profile = null)
    {
        return new AppSettingsService(store, AppProfile.Create(profile));
    }

    [Fact]
    public void Load_WhenStoreEmpty_ReturnsDefaults()
    {
        var service = CreateService(new InMemorySettingsStore());

        var settings = service.Load();

        Assert.Equal(new AppSettings(), settings);
        Assert.Equal(5, settings.DownloadQuotaGb);
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var store = new InMemorySettingsStore();
        var service = CreateService(store);
        var settings = new AppSettings
        {
            AutoDownloadEnabled = true,
            PreloadEpisodeCount = 7,
            AutoResumeEnabled = false,
            AutoResumeRemainingSeconds = 600,
            WifiOnlyDownloads = false,
            WatchedEndWindowSeconds = 120,
            DownloadQuotaGb = 42,
            AutoDeleteWatchedEpisodes = true
        };

        service.Save(settings);
        var loaded = service.Load();

        Assert.Equal(settings, loaded);
    }

    [Fact]
    public void Load_ClampsOutOfRangeStoredValues()
    {
        var store = new InMemorySettingsStore();
        var service = CreateService(store);
        store.Set(AppSettingsService.DownloadQuotaGbKey, 0);
        store.Set(AppSettingsService.PreloadEpisodeCountKey, 99);
        store.Set(AppSettingsService.WatchedEndWindowSecondsKey, 5);

        var loaded = service.Load();

        Assert.Equal(1, loaded.DownloadQuotaGb);
        Assert.Equal(10, loaded.PreloadEpisodeCount);
        Assert.Equal(30, loaded.WatchedEndWindowSeconds);
    }

    [Fact]
    public void Save_NormalizesBeforeWriting()
    {
        var store = new InMemorySettingsStore();
        var service = CreateService(store);

        service.Save(new AppSettings { DownloadQuotaGb = 10000 });

        Assert.Equal(500, store.GetInt(AppSettingsService.DownloadQuotaGbKey, -1));
    }

    [Fact]
    public void Profiles_DoNotInterfere()
    {
        // Zwei Profile teilen sich denselben physischen Store — die Trennung
        // erfolgt über die Schlüssel-Präfixe.
        var store = new InMemorySettingsStore();
        var defaultService = CreateService(store);
        var otherService = CreateService(store, "test");

        defaultService.Save(new AppSettings { DownloadQuotaGb = 5, AutoDownloadEnabled = true });
        otherService.Save(new AppSettings { DownloadQuotaGb = 20, AutoDownloadEnabled = false });

        Assert.Equal(5, defaultService.Load().DownloadQuotaGb);
        Assert.True(defaultService.Load().AutoDownloadEnabled);
        Assert.Equal(20, otherService.Load().DownloadQuotaGb);
        Assert.False(otherService.Load().AutoDownloadEnabled);
    }

    [Fact]
    public void UsesProfilePrefixedKeys()
    {
        var store = new InMemorySettingsStore();
        var defaultService = CreateService(store);
        var otherService = CreateService(store, "work");

        defaultService.Save(new AppSettings());
        otherService.Save(new AppSettings());

        Assert.Contains(AppSettingsService.DownloadQuotaGbKey, store.Keys);
        Assert.Contains($"work_{AppSettingsService.DownloadQuotaGbKey}", store.Keys);
    }
}
