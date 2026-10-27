using Microsoft.Extensions.Logging;
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
    private readonly ILogger<SettingsPage> logger;
    private bool suppressEvents;

    /// <summary>Initialisiert eine neue Instanz der <see cref="SettingsPage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; dient als <see cref="BindableObject.BindingContext"/> und Dispatch-Ziel der Speicher-Nachrichten.</param>
    /// <param name="logger">Logger für das Programmprotokoll.</param>
    public SettingsPage(MvuHost mvuHost, ILogger<SettingsPage> logger)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        this.logger = logger;
        BindingContext = mvuHost;

        HomeHeroSwitch.Toggled += OnHomeHeroToggled;
        AutoDownloadSwitch.Toggled += OnAutoDownloadToggled;
        WifiOnlySwitch.Toggled += OnWifiOnlyToggled;
        AutoDeletePlayedSwitch.Toggled += OnAutoDeletePlayedToggled;
        AutoResumeSwitch.Toggled += OnAutoResumeToggled;
        LoggingSwitch.Toggled += OnLoggingToggled;

        WireNumeric(PreloadEpisodeCountEntry, OnPreloadEpisodeCountCompleted);
        WireNumeric(DownloadQuotaGbEntry, OnDownloadQuotaCompleted);
        WireNumeric(WatchedEndWindowEntry, OnWatchedEndWindowCompleted);
        WireNumeric(DownloadRetentionDaysEntry, OnDownloadRetentionDaysCompleted);
        WireNumeric(DeleteAfterPlaybackDelayEntry, OnDeleteAfterPlaybackDelayCompleted);
    }

    private static void WireNumeric(Entry entry, EventHandler handler)
    {
        entry.Completed += handler;
        entry.Unfocused += (sender, _) => handler(sender!, EventArgs.Empty);
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        mvuHost.ModelChanged -= OnModelChanged;
        mvuHost.ModelChanged += OnModelChanged;
        PopulateFromModel();
    }

    /// <inheritdoc/>
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
            HomeHeroSwitch.IsToggled = settings.HomeHeroEnabled;
            AutoDownloadSwitch.IsToggled = settings.AutoDownloadEnabled;
            WifiOnlySwitch.IsToggled = settings.WifiOnlyDownloads;
            AutoDeletePlayedSwitch.IsToggled = settings.AutoDeletePlayedDownloads;
            AutoResumeSwitch.IsToggled = settings.AutoResumeEnabled;
            LoggingSwitch.IsToggled = settings.LoggingEnabled;

            SetTextIfNotFocused(PreloadEpisodeCountEntry, settings.PreloadEpisodeCount);
            SetTextIfNotFocused(DownloadQuotaGbEntry, settings.DownloadQuotaGb);
            SetTextIfNotFocused(WatchedEndWindowEntry, settings.WatchedEndWindowSeconds);
            SetTextIfNotFocused(DownloadRetentionDaysEntry, settings.DownloadRetentionDays);
            SetTextIfNotFocused(DeleteAfterPlaybackDelayEntry, settings.DeleteAfterPlaybackDelayMinutes);
        }
        finally
        {
            suppressEvents = false;
        }

        // Der Versand-Bereich ist nur bei aktivierter Protokollierung
        // sichtbar; der Fehlerhinweis folgt dem Modell-Fehlertext.
        SendLogSection.IsVisible = settings.LoggingEnabled;
        SendLogErrorLabel.Text = mvuHost.Model.LogSendError;
        SendLogErrorLabel.IsVisible = mvuHost.Model.LogSendError is not null;
    }

    private static void SetTextIfNotFocused(Entry entry, int value)
    {
        if (!entry.IsFocused)
        {
            entry.Text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    private void OnHomeHeroToggled(object? sender, ToggledEventArgs e)
    {
        Save(s => s with { HomeHeroEnabled = e.Value });
    }

    private void OnAutoDownloadToggled(object? sender, ToggledEventArgs e)
    {
        Save(s => s with { AutoDownloadEnabled = e.Value });
    }

    private void OnWifiOnlyToggled(object? sender, ToggledEventArgs e)
    {
        Save(s => s with { WifiOnlyDownloads = e.Value });
    }

    private void OnAutoDeletePlayedToggled(object? sender, ToggledEventArgs e)
    {
        Save(s => s with { AutoDeletePlayedDownloads = e.Value });
    }

    private void OnAutoResumeToggled(object? sender, ToggledEventArgs e)
    {
        Save(s => s with { AutoResumeEnabled = e.Value });
    }

    private void OnLoggingToggled(object? sender, ToggledEventArgs e)
    {
        Save(s => s with { LoggingEnabled = e.Value });
    }

    private void OnPreloadEpisodeCountCompleted(object? sender, EventArgs e)
    {
        SaveNumeric(PreloadEpisodeCountEntry, v => s => s with { PreloadEpisodeCount = v });
    }

    private void OnDownloadQuotaCompleted(object? sender, EventArgs e)
    {
        SaveNumeric(DownloadQuotaGbEntry, v => s => s with { DownloadQuotaGb = v });
    }

    private void OnWatchedEndWindowCompleted(object? sender, EventArgs e)
    {
        SaveNumeric(WatchedEndWindowEntry, v => s => s with { WatchedEndWindowSeconds = v });
    }

    private void OnDownloadRetentionDaysCompleted(object? sender, EventArgs e)
    {
        SaveNumeric(DownloadRetentionDaysEntry, v => s => s with { DownloadRetentionDays = v });
    }

    private void OnDeleteAfterPlaybackDelayCompleted(object? sender, EventArgs e)
    {
        SaveNumeric(DeleteAfterPlaybackDelayEntry, v => s => s with { DeleteAfterPlaybackDelayMinutes = v });
    }

    /// <summary>„Server wechseln": Serververwaltung als geschobene Route öffnen.</summary>
    /// <param name="sender">Auslösendes Steuerelement.</param>
    /// <param name="e">Ereignisargumente.</param>
    private void OnServerSwitchClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new NavigateToMessage(AppPage.ServerSetup));
    }

    /// <summary>„Protokoll senden": Versand des Programmprotokolls per Mailentwurf anfordern.</summary>
    /// <param name="sender">Auslösendes Steuerelement.</param>
    /// <param name="e">Ereignisargumente.</param>
    private void OnSendLogClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new SendLogProtocolRequestedMessage());
    }

    /// <summary>
    /// Liest einen Zahlenwert aus einem Eingabefeld und speichert ihn.
    /// Ungültige Eingaben werden nicht übernommen; ein Inline-Hinweis wird
    /// eingeblendet und der Modellwert wiederhergestellt.
    /// </summary>
    /// <param name="entry">Eingabefeld, dessen Text ausgewertet wird.</param>
    /// <param name="apply">Erzeugt aus dem gelesenen Wert die Änderungsfunktion für die Einstellungen.</param>
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
            logger.LogWarning(ex, "Einstellung speichern fehlgeschlagen.");
        }
    }
}
