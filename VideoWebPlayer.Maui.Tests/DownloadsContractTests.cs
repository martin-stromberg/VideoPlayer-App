using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests der Download-Verwaltung (Schritt 11): die MAUI-Schicht
/// kann im Testprojekt nicht instanziiert werden — verdrahtete Struktur
/// und Zusammenspiel werden daher über die Quelldateien geprüft
/// (Muster: <see cref="FavoritesProfileContractTests"/>).
/// </summary>
public class DownloadsContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));

    /// <summary>Prüft DownloadsPage: Liste mit Titel, Typ, Status, Fehler und Fortschrittsbalken.</summary>
    [Fact]
    public void DownloadsPage_ShowsItems_WithStatusAndProgress()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "DownloadsPage.xaml");

        // Stabile ItemsSource (Code-Behind): kein Listen-Binding, das bei
        // jedem Fortschritts-Tick eine neue Instanz setzt (Flackern).
        Assert.Contains("x:Name=\"DownloadsCollection\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ItemsSource=\"{Binding", xaml, StringComparison.Ordinal);
        Assert.Contains("x:DataType=\"mvu:DownloadItemViewModel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding Title}", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding MediaTypeText}", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding StateText}", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding ErrorText}", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding Progress}", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding ProgressPercentText}", xaml, StringComparison.Ordinal);
        // Quota-Anzeige, Offline-Banner, Lade-/Fehler-/Leerzustand.
        Assert.Contains("QuotaLabel", xaml, StringComparison.Ordinal);
        Assert.Contains("OfflineBanner", xaml, StringComparison.Ordinal);
        Assert.Contains("LoadingIndicator", xaml, StringComparison.Ordinal);
        Assert.Contains("RetryButton", xaml, StringComparison.Ordinal);
        Assert.Contains("EmptyLabel", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft DownloadsPage: Aktionen Abbrechen, Wiederholen und Löschen je Eintrag.</summary>
    [Fact]
    public void DownloadsPage_HasCancelRetryDeleteActions()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "DownloadsPage.xaml");

        Assert.Contains("OnCancelClicked", xaml, StringComparison.Ordinal);
        Assert.Contains("OnRetryDownloadClicked", xaml, StringComparison.Ordinal);
        Assert.Contains("OnDeleteClicked", xaml, StringComparison.Ordinal);
        // Aktions-Sichtbarkeit je Eintragszustand.
        Assert.Contains("{Binding CanCancel}", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding CanRetry}", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft DownloadsPage: dispatcht Öffnen-, Abbrechen-, Wiederholen- und Löschen-Nachrichten.</summary>
    [Fact]
    public void DownloadsPage_DispatchesMessages()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "DownloadsPage.xaml.cs");

        Assert.Contains("DownloadsPageOpenedMessage", code, StringComparison.Ordinal);
        Assert.Contains("DownloadCancelRequestedMessage", code, StringComparison.Ordinal);
        Assert.Contains("DownloadRetryRequestedMessage", code, StringComparison.Ordinal);
        Assert.Contains("DownloadDeleteRequestedMessage", code, StringComparison.Ordinal);
        Assert.Contains("DispatchAsync", code, StringComparison.Ordinal);
        // Quota- und Offline-Anzeige werden aus dem Modell aktualisiert.
        Assert.Contains("model.Downloads.QuotaText", code, StringComparison.Ordinal);
        Assert.Contains("model.IsOffline", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft DownloadsPage: granulare Listen-Aktualisierung ohne CollectionView-Reset (Anti-Flackern).</summary>
    [Fact]
    public void DownloadsPage_SyncsItems_WithoutItemsSourceReset()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "DownloadsPage.xaml.cs");

        // Stabile ObservableCollection, einmalig als ItemsSource gesetzt.
        Assert.Contains("ObservableCollection<DownloadItemViewModel>", code, StringComparison.Ordinal);
        Assert.Contains("ItemsSource = downloadItems", code, StringComparison.Ordinal);
        // Granularer Abgleich pro Modelländerung (in-place statt Reset).
        Assert.Contains("DownloadListSync.Sync(downloadItems, model.Downloads.Items)", code, StringComparison.Ordinal);
        // Eintrags-Kontext ist das ViewModel; die Messages erhalten den Record.
        Assert.Contains("is DownloadItemViewModel viewItem", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft MediaDetailsPage: Download-Schalter für Film und je Episode.</summary>
    [Fact]
    public void MediaDetailsPage_HasDownloadButtons()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "MediaDetailsPage.xaml");
        var code = Read("VideoPlayer.Maui", "Pages", "MediaDetailsPage.xaml.cs");

        Assert.Contains("DownloadButton", xaml, StringComparison.Ordinal);
        Assert.Contains("OnDownloadClicked", xaml, StringComparison.Ordinal);
        Assert.Contains("OnEpisodeDownloadClicked", xaml, StringComparison.Ordinal);
        Assert.Contains("DownloadNoticeLabel", xaml, StringComparison.Ordinal);
        Assert.Contains("DownloadRequestedMessage", code, StringComparison.Ordinal);
        Assert.Contains("DownloadNotice", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft MauiProgram: Download- und Auto-Download-Dienst werden registriert.</summary>
    [Fact]
    public void MauiProgram_RegistersDownloadServices()
    {
        var code = Read("VideoPlayer.Maui", "MauiProgram.cs");

        Assert.Contains("AddSingleton<DownloadService>", code, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<AutoDownloadService>", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft ShellAppEffectHandler: alle Download-Effekte sind verdrahtet, Ereignisse abonniert.</summary>
    [Fact]
    public void ShellAppEffectHandler_WiresDownloadEffects()
    {
        var code = Read("VideoPlayer.Maui", "Services", "ShellAppEffectHandler.cs");

        foreach (var effect in new[]
        {
            "LoadDownloadsEffect",
            "EnqueueDownloadEffect",
            "CancelDownloadEffect",
            "DeleteDownloadEffect",
            "ResumeDownloadsEffect",
            "RunAutoDownloadsEffect",
            "DeleteWatchedDownloadEffect"
        })
        {
            Assert.Contains(effect, code, StringComparison.Ordinal);
        }

        Assert.Contains("downloads.ItemChanged", code, StringComparison.Ordinal);
        Assert.Contains("downloads.ItemRemoved", code, StringComparison.Ordinal);
        Assert.Contains("DownloadItemChangedMessage", code, StringComparison.Ordinal);
        Assert.Contains("DownloadDeletedMessage", code, StringComparison.Ordinal);
        Assert.Contains("DownloadsLoadedMessage", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft ConnectivityMonitor: WLAN-Profil wird erkannt (Wi-Fi-only-Downloads).</summary>
    [Fact]
    public void ConnectivityMonitor_ReportsWifi()
    {
        var code = Read("VideoPlayer.Maui", "Services", "ConnectivityNetworkStatusMonitor.cs");

        Assert.Contains("ConnectionProfile.WiFi", code, StringComparison.Ordinal);
        Assert.Contains("IsWifi", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft Manifest-Ablage: no-backup-Dateien (aus System-Backup ausgeschlossen).</summary>
    [Fact]
    public void StorageLayout_DownloadsUnderNoBackup()
    {
        var code = Read("VideoPlayer.Storage", "AppStorageLayout.cs");

        Assert.Contains("NoBackupDirectory", code, StringComparison.Ordinal);
        Assert.Contains("videos", code, StringComparison.Ordinal);
    }
}
