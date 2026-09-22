using VideoPlayer.Storage;

namespace VideoWebPlayer.Maui.Tests;

public class AppSettingsTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var settings = new AppSettings();

        Assert.False(settings.AutoDownloadEnabled);
        Assert.Equal(3, settings.PreloadEpisodeCount);
        Assert.True(settings.AutoResumeEnabled);
        Assert.Equal(300, settings.AutoResumeRemainingSeconds);
        Assert.True(settings.WifiOnlyDownloads);
        Assert.Equal(300, settings.WatchedEndWindowSeconds);
        Assert.Equal(5, settings.DownloadQuotaGb);
        Assert.Equal(AppSettings.DefaultDownloadQuotaGb, settings.DownloadQuotaGb);
        Assert.False(settings.AutoDeleteWatchedEpisodes);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(11, 10)]
    [InlineData(5, 5)]
    public void Normalize_ClampsPreloadEpisodeCount(int input, int expected)
    {
        Assert.Equal(expected, new AppSettings { PreloadEpisodeCount = input }.Normalize().PreloadEpisodeCount);
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(4000, 3600)]
    [InlineData(120, 120)]
    public void Normalize_ClampsAutoResumeRemainingSeconds(int input, int expected)
    {
        Assert.Equal(expected, new AppSettings { AutoResumeRemainingSeconds = input }.Normalize().AutoResumeRemainingSeconds);
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(9999, 3600)]
    [InlineData(60, 60)]
    public void Normalize_ClampsWatchedEndWindowSeconds(int input, int expected)
    {
        Assert.Equal(expected, new AppSettings { WatchedEndWindowSeconds = input }.Normalize().WatchedEndWindowSeconds);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(501, 500)]
    [InlineData(50, 50)]
    public void Normalize_ClampsDownloadQuotaGb(int input, int expected)
    {
        Assert.Equal(expected, new AppSettings { DownloadQuotaGb = input }.Normalize().DownloadQuotaGb);
    }

    [Fact]
    public void Normalize_LeavesBooleansUntouched()
    {
        var settings = new AppSettings
        {
            AutoDownloadEnabled = true,
            AutoResumeEnabled = false,
            WifiOnlyDownloads = false,
            AutoDeleteWatchedEpisodes = true
        };

        var normalized = settings.Normalize();

        Assert.True(normalized.AutoDownloadEnabled);
        Assert.False(normalized.AutoResumeEnabled);
        Assert.False(normalized.WifiOnlyDownloads);
        Assert.True(normalized.AutoDeleteWatchedEpisodes);
    }
}
