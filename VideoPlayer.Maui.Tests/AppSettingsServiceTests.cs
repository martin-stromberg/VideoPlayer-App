using VideoPlayer.Maui.Tests.Stubs;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Tests;

/// <summary>Tests für <c>AppSettingsService</c>.</summary>
public class AppSettingsServiceTests
{
    private static AppSettingsService CreateService(InMemorySettingsStore store, string? profile = null)
    {
        return new AppSettingsService(store, AppProfile.Create(profile));
    }

    /// <summary>Prüft Load: bei leerem Store → gibt die Standardwerte zurück.</summary>
    [Fact]
    public void Load_WhenStoreEmpty_ReturnsDefaults()
    {
        var service = CreateService(new InMemorySettingsStore());

        var settings = service.Load();

        Assert.Equal(new AppSettings(), settings);
        Assert.Equal(5, settings.DownloadQuotaGb);
    }

    /// <summary>Prüft Save: gespeicherte Werte überstehen ein erneutes Laden (Roundtrip).</summary>
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
            WifiOnlyDownloads = false,
            WatchedEndWindowSeconds = 120,
            DownloadQuotaGb = 42,
            AutoDeleteWatchedEpisodes = true,
            HomeHeroEnabled = false,
            LoggingEnabled = true
        };

        service.Save(settings);
        var loaded = service.Load();

        Assert.Equal(settings, loaded);
    }

    /// <summary>Prüft Save/Load: eine aktivierte Protokollierung übersteht das erneute Laden (Neustart).</summary>
    [Fact]
    public void Save_LoggingEnabled_PersistsAcrossLoad()
    {
        var store = new InMemorySettingsStore();
        var service = CreateService(store);

        service.Save(new AppSettings { LoggingEnabled = true });
        var loaded = service.Load();

        Assert.True(loaded.LoggingEnabled);
        // Der Schlüssel wurde tatsächlich in der Ablage geschrieben.
        Assert.True(store.GetBool(AppSettingsService.LoggingEnabledKey, false));
    }

    /// <summary>Prüft Load: ohne gespeicherten Protokoll-Schlüssel → die Protokollierung bleibt deaktiviert.</summary>
    [Fact]
    public void Load_WithoutLoggingKey_KeepsLoggingDisabled()
    {
        var service = CreateService(new InMemorySettingsStore());

        var loaded = service.Load();

        Assert.False(loaded.LoggingEnabled);
    }

    /// <summary>Prüft Load: ohne gespeicherten Hero-Schlüssel → der Kopfbereich bleibt aktiviert (positiver Standard).</summary>
    [Fact]
    public void Load_WithoutHomeHeroKey_KeepsHeroEnabled()
    {
        var service = CreateService(new InMemorySettingsStore());

        var loaded = service.Load();

        Assert.True(loaded.HomeHeroEnabled);
    }

    /// <summary>Prüft Save/Load: ein ausgeschalteter Kopfbereich übersteht das erneute Laden (Neustart).</summary>
    [Fact]
    public void Save_HomeHeroDisabled_PersistsAcrossLoad()
    {
        var store = new InMemorySettingsStore();
        var service = CreateService(store);

        service.Save(new AppSettings { HomeHeroEnabled = false });
        var loaded = service.Load();

        Assert.False(loaded.HomeHeroEnabled);
        // Der Schlüssel wurde tatsächlich in der Ablage geschrieben.
        Assert.False(store.GetBool(AppSettingsService.HomeHeroEnabledKey, true));
    }

    /// <summary>Prüft Load: begrenzt gespeicherte Werte außerhalb des gültigen Bereichs.</summary>
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

    /// <summary>Prüft Save: normalisiert die Werte vor dem Schreiben.</summary>
    [Fact]
    public void Save_NormalizesBeforeWriting()
    {
        var store = new InMemorySettingsStore();
        var service = CreateService(store);

        service.Save(new AppSettings { DownloadQuotaGb = 10000 });

        Assert.Equal(500, store.GetInt(AppSettingsService.DownloadQuotaGbKey, -1));
    }

    /// <summary>Prüft Profiles: Profile beeinflussen sich gegenseitig nicht.</summary>
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

    /// <summary>Prüft UsesProfilePrefixedKeys: die Schlüssel sind profilpräfixiert.</summary>
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
