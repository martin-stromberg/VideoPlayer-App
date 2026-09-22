using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests der Startseite (Schritt 7): Die MAUI-Schicht kann im
/// Testprojekt nicht instanziiert werden — verdrahtete Struktur und
/// Zusammenspiel werden daher über die Quelldateien geprüft
/// (Muster: <see cref="VideoPlayerMauiContractTests"/>).
/// </summary>
public class HomePageContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));

    /// <summary>Prüft HomePage: zeigt drei horizontale Medienlisten.</summary>
    [Fact]
    public void HomePage_ShowsThreeHorizontalMediaLists()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "HomePage.xaml");

        Assert.Contains("Weiterschauen", xaml, StringComparison.Ordinal);
        Assert.Contains("Favoriten", xaml, StringComparison.Ordinal);
        Assert.Contains("Neu im Programm", xaml, StringComparison.Ordinal);
        // Die drei Listen binden an die MVU-Model-Eigenschaften.
        Assert.Contains("{Binding Model.ContinueWatching}", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding Model.Favorites}", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding Model.RecentEntries}", xaml, StringComparison.Ordinal);
        // Bestehende Anzeigen bleiben erhalten.
        Assert.Contains("OfflineBanner", xaml, StringComparison.Ordinal);
        Assert.Contains("ServerStatusLabel", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft MediaCarousel: Einträge sind tippbar und Poster werden über den Cache geladen.</summary>
    [Fact]
    public void MediaCarousel_ItemsAreTappable_PostersViaCache()
    {
        var xaml = Read("VideoPlayer.Maui", "Controls", "MediaCarousel.xaml");

        // Horizontales Scrollen, Poster über das lokale Vorschaubild,
        // Fortschrittsbalken für Weiterschauen und Tap-Auswahl.
        Assert.Contains("Orientation=\"Horizontal\"", xaml, StringComparison.Ordinal);
        Assert.Contains("CachedPosterImage", xaml, StringComparison.Ordinal);
        Assert.Contains("TapGestureRecognizer", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding Progress}", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft HomePage: die Auswahl dispatcht eine MediaItemSelected-Nachricht.</summary>
    [Fact]
    public void HomePage_SelectionDispatchesMediaItemSelected()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "HomePage.xaml.cs");

        Assert.Contains("MediaItemSelectedMessage", code, StringComparison.Ordinal);
        Assert.Contains("DispatchAsync", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft MediaDetails: bei <c>RouteRegistered</c> → die Seite liest die Query-Parameter.</summary>
    [Fact]
    public void MediaDetails_RouteRegistered_AndPageReadsQueryParameters()
    {
        var shell = Read("VideoPlayer.Maui", "AppShell.xaml.cs");
        var page = Read("VideoPlayer.Maui", "Pages", "MediaDetailsPage.xaml.cs");

        Assert.Contains("\"mediadetails\"", shell, StringComparison.Ordinal);
        // Identität des Mediums über die Route — kein interner Schlüssel
        // als Anwendereingabe.
        Assert.Contains("mediaType", page, StringComparison.Ordinal);
        Assert.Contains("\"id\"", page, StringComparison.Ordinal);
        Assert.Contains("\"title\"", page, StringComparison.Ordinal);
        Assert.Contains("QueryProperty", page, StringComparison.Ordinal);
    }

    /// <summary>Prüft ShellAppEffectHandler: bei <c>HandlesNewEffects</c> → benachrichtigt über Listenänderungen.</summary>
    [Fact]
    public void ShellAppEffectHandler_HandlesNewEffects_AndNotifiesListChanges()
    {
        var handler = Read("VideoPlayer.Maui", "Services", "ShellAppEffectHandler.cs");

        Assert.Contains("LoadHomeListsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("NavigateToMediaDetailsEffect", handler, StringComparison.Ordinal);
        // Teil-Synchronisationen melden die Cache-Änderung → Listen neu laden.
        Assert.Contains("CachedListsChangedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("HomeListsLoadedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("MainThread", handler, StringComparison.Ordinal);
    }

    /// <summary>Prüft MauiProgram: registriert die Home-Services und die Details-Seite.</summary>
    [Fact]
    public void MauiProgram_RegistersHomeServicesAndDetailsPage()
    {
        var program = Read("VideoPlayer.Maui", "MauiProgram.cs");

        Assert.Contains("HomeListService", program, StringComparison.Ordinal);
        Assert.Contains("PosterImageService", program, StringComparison.Ordinal);
        Assert.Contains("MediaDetailsPage", program, StringComparison.Ordinal);
    }

    /// <summary>Prüft PosterImageService: verwendet das NoBackup-Vorschauverzeichnis.</summary>
    [Fact]
    public void PosterImageService_UsesNonBackupPreviewDirectory()
    {
        var service = Read("VideoPlayer.Maui", "Services", "PosterImageService.cs");

        // Vorschaubilder gehören nicht in die Backup-Ablage.
        Assert.Contains("PreviewsDirectory", service, StringComparison.Ordinal);
        Assert.Contains("GetPictureAsync", service, StringComparison.Ordinal);
        Assert.DoesNotContain("BackupDirectory", service, StringComparison.Ordinal);
    }
}
