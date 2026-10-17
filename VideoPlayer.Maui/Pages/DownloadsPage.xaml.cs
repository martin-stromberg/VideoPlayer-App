using System.Collections.ObjectModel;
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

    // Stabile ItemsSource: Fortschritts-Ticks aktualisieren die Einträge
    // in-place (PropertyChanged am Eintrag), statt den CollectionView
    // pro Tick über eine neue Listeninstanz zurückzusetzen (Flackern).
    private readonly ObservableCollection<DownloadItemViewModel> downloadItems = [];
    private bool subscribed;

    /// <summary>Initialisiert eine neue Instanz der <see cref="DownloadsPage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; dient als <see cref="BindableObject.BindingContext"/> und Dispatch-Ziel der Download-Nachrichten.</param>
    public DownloadsPage(MvuHost mvuHost)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        BindingContext = mvuHost;
        DownloadsCollection.ItemsSource = downloadItems;
        UpdateUi(mvuHost.Model);
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Das ModelChanged-Abo gilt nur, solange die Seite sichtbar ist
        // (Muster: SettingsPage) — verlassene Instanzen dürfen nicht
        // weiter am MvuHost hängen.
        subscribed = true;
        mvuHost.ModelChanged -= OnModelChanged;
        mvuHost.ModelChanged += OnModelChanged;
        UpdateUi(mvuHost.Model);
        // Beim Öffnen den Manifest-Stand (neu) laden — lokale Daten,
        // ohne Serverkontakt.
        _ = mvuHost.DispatchAsync(new DownloadsPageOpenedMessage());
    }

    /// <inheritdoc/>
    protected override void OnDisappearing()
    {
        subscribed = false;
        mvuHost.ModelChanged -= OnModelChanged;
        base.OnDisappearing();
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        var model = mvuHost.Model;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (subscribed)
            {
                UpdateUi(model);
            }
        });
    }

    private void UpdateUi(AppModel model)
    {
        // Granularer Abgleich: nur neue/entfallene Einträge lösen
        // CollectionChanged aus; Fortschritte laufen über das
        // Eintrags-ViewModel (kein Neuaufbau der Liste).
        DownloadListSync.Sync(downloadItems, model.Downloads.Items);
        OfflineBanner.IsVisible = model.IsOffline;
        QuotaLabel.Text = model.Downloads.QuotaText;
        OrphanedLabel.Text = model.Downloads.OrphanedText;
        var showOrphans = model.Downloads.OrphanedText is not null;
        OrphanedLabel.IsVisible = showOrphans;
        PurgeOrphansButton.IsVisible = showOrphans;
        DeleteAllButton.IsVisible = model.Downloads.HasItems || showOrphans;
        ErrorPanel.IsVisible = model.Downloads.HasError && !model.Downloads.IsLoading;
        ErrorLabel.Text = model.Downloads.Error;
        EmptyLabel.IsVisible = model.Downloads.ShowEmpty;
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        if ((sender as Element)?.BindingContext is DownloadItemViewModel viewItem)
        {
            _ = mvuHost.DispatchAsync(new DownloadCancelRequestedMessage(viewItem.Item));
        }
    }

    private void OnRetryDownloadClicked(object? sender, EventArgs e)
    {
        if ((sender as Element)?.BindingContext is DownloadItemViewModel viewItem)
        {
            _ = mvuHost.DispatchAsync(new DownloadRetryRequestedMessage(viewItem.Item));
        }
    }

    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        if ((sender as Element)?.BindingContext is DownloadItemViewModel viewItem)
        {
            _ = mvuHost.DispatchAsync(new DownloadDeleteRequestedMessage(viewItem.Item));
        }
    }

    private void OnRetryClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new DownloadsPageOpenedMessage());
    }

    /// <summary>
    /// „Alle löschen" mit Rückfrage — Muster: PlaylistDetailsPage.ConfirmDeleteAsync.
    /// </summary>
    /// <param name="sender">Der auslösende Schalter.</param>
    /// <param name="e">Die Ereignisdaten.</param>
    private async void OnDeleteAllClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Alle Downloads löschen?",
            "Alle heruntergeladenen Videos und erkannten Restdaten werden dauerhaft entfernt.",
            "Löschen",
            "Abbrechen");
        if (confirmed)
        {
            _ = mvuHost.DispatchAsync(new DownloadsDeleteAllRequestedMessage());
        }
    }

    /// <summary>Bereinigung erkannter Restdaten ohne Manifest-Eintrag.</summary>
    /// <param name="sender">Der auslösende Schalter.</param>
    /// <param name="e">Die Ereignisdaten.</param>
    private async void OnPurgeOrphansClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Speicher bereinigen?",
            $"Nicht zugeordnete Restdaten ({mvuHost.Model.Downloads.OrphanedText}) werden dauerhaft entfernt.",
            "Bereinigen",
            "Abbrechen");
        if (confirmed)
        {
            _ = mvuHost.DispatchAsync(new DownloadsPurgeOrphansRequestedMessage());
        }
    }
}
