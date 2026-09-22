using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoWebPlayer.Maui.Tests;

public class AppUpdateTests_Settings
{
    [Fact]
    public void Update_Startup_EmitsStorageAndSettingsEffects()
    {
        var transition = AppUpdate.Update(new AppModel(), new StartupRequestedMessage());

        Assert.Contains(transition.Effects, e => e is InitializeStorageEffect);
        Assert.Contains(transition.Effects, e => e is LoadSettingsEffect);
        Assert.Contains(transition.Effects, e => e is CheckServerHealthEffect);
    }

    [Fact]
    public void Update_LoadSettingsRequested_EmitsLoadEffect()
    {
        var transition = AppUpdate.Update(new AppModel(), new LoadSettingsRequestedMessage());

        var effect = Assert.Single(transition.Effects);
        Assert.IsType<LoadSettingsEffect>(effect);
    }

    [Fact]
    public void Update_SettingsLoaded_SetsModelSettings()
    {
        var settings = new AppSettings { DownloadQuotaGb = 20, WifiOnlyDownloads = false };

        var transition = AppUpdate.Update(new AppModel(), new SettingsLoadedMessage(settings));

        Assert.Equal(20, transition.Model.Settings.DownloadQuotaGb);
        Assert.False(transition.Model.Settings.WifiOnlyDownloads);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void Update_SettingsLoaded_Normalizes()
    {
        var transition = AppUpdate.Update(
            new AppModel(),
            new SettingsLoadedMessage(new AppSettings { DownloadQuotaGb = 9999 }));

        Assert.Equal(500, transition.Model.Settings.DownloadQuotaGb);
    }

    [Fact]
    public void Update_SaveSettings_NormalizesAndPersists()
    {
        var transition = AppUpdate.Update(
            new AppModel(),
            new SaveSettingsRequestedMessage(new AppSettings { DownloadQuotaGb = 0, AutoDownloadEnabled = true }));

        Assert.Equal(1, transition.Model.Settings.DownloadQuotaGb);
        Assert.True(transition.Model.Settings.AutoDownloadEnabled);
        var effect = Assert.Single(transition.Effects);
        var persist = Assert.IsType<PersistSettingsEffect>(effect);
        Assert.Equal(1, persist.Settings.DownloadQuotaGb);
    }
}
