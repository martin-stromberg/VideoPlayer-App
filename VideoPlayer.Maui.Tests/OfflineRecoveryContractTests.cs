using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests des Offlinebetriebs und der Wiederherstellung
/// (Schritt 12): die MAUI-Schicht kann im Testprojekt nicht
/// instanziiert werden — verdrahtete Struktur und Zusammenspiel
/// werden daher über die Quelldateien geprüft (Muster:
/// <see cref="HomePageContractTests"/>).
/// </summary>
public class OfflineRecoveryContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));

    /// <summary>Prüft HomePage: Offline-Banner, Ablage-Hinweis und Leerzustand sind verdrahtet.</summary>
    [Fact]
    public void HomePage_WiresOfflineAndStorageNoticeBanners()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "HomePage.xaml");
        var code = Read("VideoPlayer.Maui", "Pages", "HomePage.xaml.cs");

        Assert.Contains("OfflineBanner", xaml, StringComparison.Ordinal);
        Assert.Contains("Offline-Modus", xaml, StringComparison.Ordinal);
        // Wiederherstellungs-Hinweis der lokalen Ablage.
        Assert.Contains("StorageNoticeBanner", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding Model.StorageNotice}", xaml, StringComparison.Ordinal);
        Assert.Contains("model.StorageNotice is not null", code, StringComparison.Ordinal);
        // Verständlicher Leerzustand ohne zwischengespeicherte Inhalte.
        Assert.Contains("Noch keine Inhalte vorhanden", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft MediaCarousel: Platzhalter-Karten sind abgedunkelt und beschriftet.</summary>
    [Fact]
    public void MediaCarousel_RendersDownloadPlaceholders()
    {
        var xaml = Read("VideoPlayer.Maui", "Controls", "MediaCarousel.xaml");

        Assert.Contains("PlaceholderBadge", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsPlaceholder}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Opacity=\"{Binding CardOpacity}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Nicht heruntergeladen", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft ProfilePage: Offline-Warnung; Server-Benutzerbereich wird offline ausgeblendet.</summary>
    [Fact]
    public void ProfilePage_WiresOfflineWarning()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "ProfilePage.xaml");
        var code = Read("VideoPlayer.Maui", "Pages", "ProfilePage.xaml.cs");

        Assert.Contains("ProfileOfflineBanner", xaml, StringComparison.Ordinal);
        Assert.Contains("UserInfoBorder", xaml, StringComparison.Ordinal);
        Assert.Contains("model.IsOffline", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft MediaDetailsPage: Offline-Platzhalter-Episoden sind markiert und ohne Aktionen.</summary>
    [Fact]
    public void MediaDetailsPage_MarksPlaceholderEpisodes()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "MediaDetailsPage.xaml");
        var code = Read("VideoPlayer.Maui", "Pages", "MediaDetailsPage.xaml.cs");

        Assert.Contains("{Binding IsPlaceholder}", xaml, StringComparison.Ordinal);
        Assert.Contains("Nicht heruntergeladen", xaml, StringComparison.Ordinal);
        // Offline-Snapshot: serverseitige Aktionen werden unterbunden.
        Assert.Contains("IsOfflineSnapshot", code, StringComparison.Ordinal);
        Assert.Contains("HasLocalFile", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft MauiProgram: registriert die Offline-Dienste.</summary>
    [Fact]
    public void MauiProgram_RegistersOfflineServices()
    {
        var program = Read("VideoPlayer.Maui", "MauiProgram.cs");

        Assert.Contains("OfflineContinueWatchingService", program, StringComparison.Ordinal);
        Assert.Contains("PlaybackSourceService", program, StringComparison.Ordinal);
    }

    /// <summary>Prüft ShellAppEffectHandler: Recovery-Initialisierung, Offline-Listen und Benutzerhinweis.</summary>
    [Fact]
    public void ShellAppEffectHandler_WiresRecoveryAndOfflineLists()
    {
        var handler = Read("VideoPlayer.Maui", "Services", "ShellAppEffectHandler.cs");

        Assert.Contains("InitializeWithRecoveryAsync", handler, StringComparison.Ordinal);
        Assert.Contains("StorageRecoveredMessage", handler, StringComparison.Ordinal);
        // Offline werden die Listen aus dem lokalen Bestand gebildet.
        Assert.Contains("OfflineContinueWatchingService", handler, StringComparison.Ordinal);
    }
}
