using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Einstellungen: alle benutzerbezogenen Einstellungen werden aus dem
/// MVU-Model angezeigt; Änderungen laufen über
/// <see cref="SaveSettingsRequestedMessage"/> → Update →
/// <see cref="PersistSettingsEffect"/> in die Ablage.
/// </summary>
public partial class SettingsPage : ContentPage
{
    private readonly MvuHost mvuHost;
    private bool suppressEvents;

    public SettingsPage(MvuHost mvuHost)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        BindingContext = mvuHost;

        AutoDownloadSwitch.Toggled += OnAutoDownloadToggled;
        WifiOnlySwitch.Toggled += OnWifiOnlyToggled;
        AutoDeleteWatchedSwitch.Toggled += OnAutoDeleteWatchedToggled;
        AutoResumeSwitch.Toggled += OnAutoResumeToggled;

        WireNumeric(PreloadEpisodeCountEntry, OnPreloadEpisodeCountCompleted);
        WireNumeric(DownloadQuotaGbEntry, OnDownloadQuotaCompleted);
        WireNumeric(AutoResumeRemainingEntry, OnAutoResumeRemainingCompleted);
        WireNumeric(WatchedEndWindowEntry, OnWatchedEndWindowCompleted);
    }

    private static void WireNumeric(Entry entry, EventHandler handler)
    {
        entry.Completed += handler;
        entry.Unfocused += (sender, _) => handler(sender!, EventArgs.Empty);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        mvuHost.ModelChanged -= OnModelChanged;
        mvuHost.ModelChanged += OnModelChanged;
        PopulateFromModel();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        mvuHost.ModelChanged -= OnModelChanged;
    }

    private void OnModelChanged(object? sender, EventArgs e) => PopulateFromModel();

    private void PopulateFromModel()
    {
        var settings = mvuHost.Model.Settings;
        suppressEvents = true;
        try
        {
            AutoDownloadSwitch.IsToggled = settings.AutoDownloadEnabled;
            WifiOnlySwitch.IsToggled = settings.WifiOnlyDownloads;
            AutoDeleteWatchedSwitch.IsToggled = settings.AutoDeleteWatchedEpisodes;
            AutoResumeSwitch.IsToggled = settings.AutoResumeEnabled;

            SetTextIfNotFocused(PreloadEpisodeCountEntry, settings.PreloadEpisodeCount);
            SetTextIfNotFocused(DownloadQuotaGbEntry, settings.DownloadQuotaGb);
            SetTextIfNotFocused(AutoResumeRemainingEntry, settings.AutoResumeRemainingSeconds);
            SetTextIfNotFocused(WatchedEndWindowEntry, settings.WatchedEndWindowSeconds);
        }
        finally
        {
            suppressEvents = false;
        }
    }

    private static void SetTextIfNotFocused(Entry entry, int value)
    {
        if (!entry.IsFocused)
        {
            entry.Text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    private void OnAutoDownloadToggled(object? sender, ToggledEventArgs e)
    {
        Save(s => s with { AutoDownloadEnabled = e.Value });
    }

    private void OnWifiOnlyToggled(object? sender, ToggledEventArgs e)
    {
        Save(s => s with { WifiOnlyDownloads = e.Value });
    }

    private void OnAutoDeleteWatchedToggled(object? sender, ToggledEventArgs e)
    {
        Save(s => s with { AutoDeleteWatchedEpisodes = e.Value });
    }

    private void OnAutoResumeToggled(object? sender, ToggledEventArgs e)
    {
        Save(s => s with { AutoResumeEnabled = e.Value });
    }

    private void OnPreloadEpisodeCountCompleted(object? sender, EventArgs e)
    {
        SaveNumeric(PreloadEpisodeCountEntry, v => s => s with { PreloadEpisodeCount = v });
    }

    private void OnDownloadQuotaCompleted(object? sender, EventArgs e)
    {
        SaveNumeric(DownloadQuotaGbEntry, v => s => s with { DownloadQuotaGb = v });
    }

    private void OnAutoResumeRemainingCompleted(object? sender, EventArgs e)
    {
        SaveNumeric(AutoResumeRemainingEntry, v => s => s with { AutoResumeRemainingSeconds = v });
    }

    private void OnWatchedEndWindowCompleted(object? sender, EventArgs e)
    {
        SaveNumeric(WatchedEndWindowEntry, v => s => s with { WatchedEndWindowSeconds = v });
    }

    /// <summary>
    /// Liest einen Zahlenwert aus einem Eingabefeld und speichert ihn.
    /// Ungültige Eingaben werden nicht übernommen; ein Inline-Hinweis wird
    /// eingeblendet und der Modellwert wiederhergestellt.
    /// </summary>
    private void SaveNumeric(Entry entry, Func<int, Func<AppSettings, AppSettings>> apply)
    {
        if (suppressEvents)
        {
            return;
        }

        if (!int.TryParse(entry.Text, out var value))
        {
            InvalidInputLabel.IsVisible = true;
            PopulateFromModel();
            return;
        }

        InvalidInputLabel.IsVisible = false;
        Save(apply(value));
    }

    private void Save(Func<AppSettings, AppSettings> change)
    {
        if (suppressEvents)
        {
            return;
        }

        var updated = change(mvuHost.Model.Settings);
        _ = DispatchSaveAsync(updated);
    }

    private async Task DispatchSaveAsync(AppSettings settings)
    {
        try
        {
            await mvuHost.DispatchAsync(new SaveSettingsRequestedMessage(settings));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] Einstellung speichern fehlgeschlagen: {ex.Message}");
        }
    }
}
