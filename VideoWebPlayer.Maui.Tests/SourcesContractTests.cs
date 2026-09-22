using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests des Quellen-Browsing und der Detailseiten (Schritt 8):
/// die MAUI-Schicht kann im Testprojekt nicht instanziiert werden —
/// verdrahtete Struktur und Zusammenspiel werden daher über die
/// Quelldateien geprüft (Muster: <see cref="HomePageContractTests"/>).
/// Echte UI-End-to-End-Automatisierung ist im Repository derzeit nicht
/// vorhanden ( dokumentierte Einschränkung ).
/// </summary>
public class SourcesContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));

    [Fact]
    public void SourcesPage_ShowsSources_WithIcons_AndOfflineHint()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "SourcesPage.xaml");

        // Quellenliste mit Icon + Name, Offline-Banner und Fehler-Retry.
        Assert.Contains("{Binding Model.SourcesOverview.Sources}", xaml, StringComparison.Ordinal);
        Assert.Contains("CachedSourceIconImage", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding IconPictureId}", xaml, StringComparison.Ordinal);
        Assert.Contains("OfflineBanner", xaml, StringComparison.Ordinal);
        Assert.Contains("Offline", xaml, StringComparison.Ordinal);
        Assert.Contains("RetryButton", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void SourcesPage_DispatchesOpenAndSelection()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "SourcesPage.xaml.cs");

        Assert.Contains("SourcesPageOpenedMessage", code, StringComparison.Ordinal);
        Assert.Contains("SourceSelectedMessage", code, StringComparison.Ordinal);
        Assert.Contains("DispatchAsync", code, StringComparison.Ordinal);
    }

    [Fact]
    public void SourceOverviewPage_HasGenreBar_Search_AndIncrementalGrid()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "SourceOverviewPage.xaml");

        Assert.Contains("{Binding Model.SourceBrowser.Genres}", xaml, StringComparison.Ordinal);
        Assert.Contains("SearchBar", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding Model.SourceBrowser.Items}", xaml, StringComparison.Ordinal);
        // Seitenweises Nachladen am Listenende.
        Assert.Contains("RemainingItemsThresholdReached", xaml, StringComparison.Ordinal);
        Assert.Contains("CachedPosterImage", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void SourceOverviewPage_DispatchesBrowserMessages()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "SourceOverviewPage.xaml.cs");

        Assert.Contains("SourceBrowserOpenedMessage", code, StringComparison.Ordinal);
        Assert.Contains("SourceGenreSelectedMessage", code, StringComparison.Ordinal);
        Assert.Contains("SourceSearchSubmittedMessage", code, StringComparison.Ordinal);
        Assert.Contains("SourceItemsNextPageRequestedMessage", code, StringComparison.Ordinal);
        Assert.Contains("SourceItemsRetryRequestedMessage", code, StringComparison.Ordinal);
        Assert.Contains("SourceMediaItemSelectedMessage", code, StringComparison.Ordinal);
    }

    [Fact]
    public void MediaDetailsPage_HasSeasonPicker_Episodes_Collection_AndFavorite()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "MediaDetailsPage.xaml");

        // Serienansicht (Staffel-Picker + Episodenliste), Kollektionsansicht
        // und der Favoriten-Schalter.
        Assert.Contains("SeasonPicker", xaml, StringComparison.Ordinal);
        Assert.Contains("EpisodesCollection", xaml, StringComparison.Ordinal);
        Assert.Contains("CollectionCollection", xaml, StringComparison.Ordinal);
        Assert.Contains("FavoriteButton", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MediaDetailsPage_DispatchesDetailsMessages()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "MediaDetailsPage.xaml.cs");

        Assert.Contains("MediaDetailsRequestedMessage", code, StringComparison.Ordinal);
        Assert.Contains("SeasonSelectedMessage", code, StringComparison.Ordinal);
        Assert.Contains("FavoriteToggleRequestedMessage", code, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_RegistersSourceOverviewRoute_WithQueryParameters()
    {
        var shell = Read("VideoPlayer.Maui", "AppShell.xaml.cs");
        var page = Read("VideoPlayer.Maui", "Pages", "SourceOverviewPage.xaml.cs");

        Assert.Contains("\"sourceoverview\"", shell, StringComparison.Ordinal);
        Assert.Contains("\"id\"", page, StringComparison.Ordinal);
        Assert.Contains("\"name\"", page, StringComparison.Ordinal);
        Assert.Contains("QueryProperty", page, StringComparison.Ordinal);
    }

    [Fact]
    public void EffectHandler_HandlesStep8Effects_AndRefreshesFavoriteCaches()
    {
        var handler = Read("VideoPlayer.Maui", "Services", "ShellAppEffectHandler.cs");

        Assert.Contains("LoadSourcesEffect", handler, StringComparison.Ordinal);
        Assert.Contains("NavigateToSourceOverviewEffect", handler, StringComparison.Ordinal);
        Assert.Contains("LoadSourceGenresEffect", handler, StringComparison.Ordinal);
        Assert.Contains("LoadSourceItemsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("LoadMediaDetailsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("ToggleFavoriteEffect", handler, StringComparison.Ordinal);
        // Nach der Favoriten-Umschaltung wird die lokale Kopie gemeldet —
        // die Startseiten-Favoritenliste lädt ohne manuelle Aktualisierung nach.
        Assert.Contains("CachedListsChangedMessage", handler, StringComparison.Ordinal);
    }

    [Fact]
    public void MauiProgram_RegistersStep8ServicesAndPages()
    {
        var program = Read("VideoPlayer.Maui", "MauiProgram.cs");

        Assert.Contains("SourceBrowserService", program, StringComparison.Ordinal);
        Assert.Contains("MediaDetailsService", program, StringComparison.Ordinal);
        Assert.Contains("FavoritesService", program, StringComparison.Ordinal);
        Assert.Contains("SourceOverviewPage", program, StringComparison.Ordinal);
    }
}
