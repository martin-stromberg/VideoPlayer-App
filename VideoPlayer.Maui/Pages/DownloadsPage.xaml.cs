using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Downloads (Menüseite „Downloads"): listet die Aufträge des
/// Download-Manifests mit Titel, Zustand, Fortschritt und den Aktionen
/// Abbrechen, Erneut versuchen und Löschen. Die Daten liegen lokal vor —
/// die Seite ist auch offline anzeigbar; wartende Downloads werden bei
/// Verbindungs-/WLAN-Rückkehr automatisch fortgesetzt. Änderungen
/// laufender Transfers kommen über <c>DownloadItemChangedMessage</c>
/// herein, ohne dass die Seite neu laden muss.
/// </summary>
public partial class DownloadsPage : ContentPage
{
    private readonly MvuHost mvuHost;

    /// <summary>Initialisiert eine neue Instanz der <see cref="DownloadsPage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; dient als <see cref="BindableObject.BindingContext"/> und Dispatch-Ziel der Download-Nachrichten.</param>
    public DownloadsPage(MvuHost mvuHost)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        BindingContext = mvuHost;
        mvuHost.ModelChanged += OnModelChanged;
        UpdateUi(mvuHost.Model);
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Beim Öffnen den Manifest-Stand (neu) laden — lokale Daten,
        // ohne Serverkontakt.
        _ = mvuHost.DispatchAsync(new DownloadsPageOpenedMessage());
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        var model = mvuHost.Model;
        MainThread.BeginInvokeOnMainThread(() => UpdateUi(model));
    }

    private void UpdateUi(AppModel model)
    {
        OfflineBanner.IsVisible = model.IsOffline;
        QuotaLabel.Text = model.Downloads.QuotaText;
        ErrorPanel.IsVisible = model.Downloads.HasError && !model.Downloads.IsLoading;
        ErrorLabel.Text = model.Downloads.Error;
        EmptyLabel.IsVisible = model.Downloads.ShowEmpty;
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        if ((sender as Element)?.BindingContext is DownloadListItem item)
        {
            _ = mvuHost.DispatchAsync(new DownloadCancelRequestedMessage(item));
        }
    }

    private void OnRetryDownloadClicked(object? sender, EventArgs e)
    {
        if ((sender as Element)?.BindingContext is DownloadListItem item)
        {
            _ = mvuHost.DispatchAsync(new DownloadRetryRequestedMessage(item));
        }
    }

    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        if ((sender as Element)?.BindingContext is DownloadListItem item)
        {
            _ = mvuHost.DispatchAsync(new DownloadDeleteRequestedMessage(item));
        }
    }

    private void OnRetryClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new DownloadsPageOpenedMessage());
    }
}
