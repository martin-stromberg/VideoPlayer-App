using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Tests;

/// <summary>Tests für <c>AppUpdate.Update</c> – Bereich Einstellungen.</summary>
public class AppUpdateTests_Settings
{
    /// <summary>Prüft Update: beim Startup → löst Storage- und Einstellungseffekte aus.</summary>
    [Fact]
    public void Update_Startup_EmitsStorageAndSettingsEffects()
    {
        var transition = AppUpdate.Update(new AppModel(), new StartupRequestedMessage());

        Assert.Contains(transition.Effects, e => e is InitializeStorageEffect);
        Assert.Contains(transition.Effects, e => e is LoadSettingsEffect);
        // Der Health-Check folgt erst nach dem Laden der Serververwaltung
        // (ServersLoadedMessage) — ohne Server verzweigt der Start in die Einrichtung.
        Assert.Contains(transition.Effects, e => e is LoadServersEffect);
    }

    /// <summary>Prüft Update: bei <c>LoadSettingsRequested</c> → löst den Lade-Effekt aus.</summary>
    [Fact]
    public void Update_LoadSettingsRequested_EmitsLoadEffect()
    {
        var transition = AppUpdate.Update(new AppModel(), new LoadSettingsRequestedMessage());

        var effect = Assert.Single(transition.Effects);
        Assert.IsType<LoadSettingsEffect>(effect);
    }

    /// <summary>Prüft Update: bei <c>SettingsLoaded</c> → setzt die Model-Einstellungen.</summary>
    [Fact]
    public void Update_SettingsLoaded_SetsModelSettings()
    {
        var settings = new AppSettings { DownloadQuotaGb = 20, WifiOnlyDownloads = false };

        var transition = AppUpdate.Update(new AppModel(), new SettingsLoadedMessage(settings));

        Assert.Equal(20, transition.Model.Settings.DownloadQuotaGb);
        Assert.False(transition.Model.Settings.WifiOnlyDownloads);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Update: bei <c>SettingsLoaded</c> → normalisiert die Einstellungen.</summary>
    [Fact]
    public void Update_SettingsLoaded_Normalizes()
    {
        var transition = AppUpdate.Update(
            new AppModel(),
            new SettingsLoadedMessage(new AppSettings { DownloadQuotaGb = 9999 }));

        Assert.Equal(500, transition.Model.Settings.DownloadQuotaGb);
    }

    /// <summary>Prüft Update: bei <c>SaveSettings</c> → normalisiert und persistiert.</summary>
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
