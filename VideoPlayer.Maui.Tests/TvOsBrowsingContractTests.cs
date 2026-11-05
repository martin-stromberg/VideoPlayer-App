using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests des tvOS-Quellenbrowsers (Schritt 5): Da tvOS-Code
/// unter Windows nicht kompiliert werden kann, werden die Browsing-Seite,
/// die Quellen-Auswahl der Startseite, die Zellen und die
/// Effekt-Verdrahtung über die Quelldateien geprüft (Muster:
/// <see cref="TvOsHomePageContractTests"/>).
/// </summary>
public class TvOsBrowsingContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string TvOsFile(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root, "VideoPlayer.TvOS" }.Concat(parts).ToArray()));

    /// <summary>
    /// Prüft: die Browsing-Seite ist modellgetrieben und zeigt
    /// Genre-Filterleiste, Suchfeld und Eintrags-Raster — sie dispatcht
    /// die Browser-Messages des MVU-Zyklus.
    /// </summary>
    [Fact]
    public void SourceBrowserPage_RendersGridGenresAndSearchFromModel()
    {
        var page = TvOsFile("Ui", "SourceBrowserViewController.cs");

        Assert.Contains("IModelRenderable", page, StringComparison.Ordinal);
        Assert.Contains("UICollectionView", page, StringComparison.Ordinal);
        Assert.Contains("UITextField", page, StringComparison.Ordinal);
        Assert.Contains("SourceBrowser", page, StringComparison.Ordinal);
        Assert.Contains("ShowGenreFilter", page, StringComparison.Ordinal);
        Assert.Contains("SourceGenreCell", page, StringComparison.Ordinal);
        Assert.Contains("HomeMediaItemCell", page, StringComparison.Ordinal);
        Assert.Contains("RegisterClassForCell", page, StringComparison.Ordinal);

        // Interaktions-Messages des plattformneutralen Suchpfads
        Assert.Contains("SourceBrowserOpenedMessage", page, StringComparison.Ordinal);
        Assert.Contains("SourceGenreSelectedMessage", page, StringComparison.Ordinal);
        Assert.Contains("SourceSearchSubmittedMessage", page, StringComparison.Ordinal);
        Assert.Contains("SourceMediaItemSelectedMessage", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: die Sucheingabe läuft über die tvOS-Systemtastatur
    /// (UITextField — Diktat/Continuity systemseitig), wird bei
    /// EditingDidEnd abgeschickt und der eingegebene Text bleibt
    /// sichtbar (Spiegel-Guard statt Zurückschreiben während der
    /// Bearbeitung).
    /// </summary>
    [Fact]
    public void SourceBrowserPage_SearchTextStaysVisibleWhileEditing()
    {
        var page = TvOsFile("Ui", "SourceBrowserViewController.cs");

        Assert.Contains("EditingDidEnd", page, StringComparison.Ordinal);
        Assert.Contains("SourceSearchSubmittedMessage", page, StringComparison.Ordinal);
        Assert.Contains("mirroredSearchText", page, StringComparison.Ordinal);
        Assert.Contains("IsFirstResponder", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: Lade-, Leer- und Fehlerzustände sind sichtbar, der
    /// Fehlerzustand bietet die Wiederholmöglichkeit und die
    /// Menü-Taste der Siri Remote schließt die Seite.
    /// </summary>
    [Fact]
    public void SourceBrowserPage_StatesAndRetryAreRemoteFriendly()
    {
        var page = TvOsFile("Ui", "SourceBrowserViewController.cs");

        Assert.Contains("IsLoading", page, StringComparison.Ordinal);
        Assert.Contains("IsLoadingMore", page, StringComparison.Ordinal);
        Assert.Contains("ShowEmpty", page, StringComparison.Ordinal);
        Assert.Contains("HasError", page, StringComparison.Ordinal);
        Assert.Contains("Erneut versuchen", page, StringComparison.Ordinal);
        Assert.Contains("SourceItemsRetryRequestedMessage", page, StringComparison.Ordinal);
        Assert.Contains("PressesBegan", page, StringComparison.Ordinal);
        Assert.Contains("UIPressType.Menu", page, StringComparison.Ordinal);
        Assert.Contains("DismissViewController", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: das Raster lädt weitere Seiten bei Annäherung an das
    /// Listenende nach.
    /// </summary>
    [Fact]
    public void SourceBrowserPage_LoadsNextPageAtListEnd()
    {
        var page = TvOsFile("Ui", "SourceBrowserViewController.cs");

        Assert.Contains("WillDisplayCell", page, StringComparison.Ordinal);
        Assert.Contains("SourceItemsNextPageRequestedMessage", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: die Startseite zeigt die Quellen-Sektion mit Auswahl,
    /// Lade-/Leer-/Fehlerzustand und Neuversuch.
    /// </summary>
    [Fact]
    public void HomePage_ShowsSourceSectionWithStates()
    {
        var page = TvOsFile("Ui", "HomePageViewController.cs");

        Assert.Contains("SourceCarouselView", page, StringComparison.Ordinal);
        Assert.Contains("SourcesOverview", page, StringComparison.Ordinal);
        Assert.Contains("Quellen", page, StringComparison.Ordinal);
        Assert.Contains("SourceSelectedMessage", page, StringComparison.Ordinal);
        Assert.Contains("SourcesReloadRequestedMessage", page, StringComparison.Ordinal);
        Assert.Contains("Erneut versuchen", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: Quellen- und Genre-Karten sind fokussierbare
    /// UICollectionView-Zellen mit Wiederverwendungs-Kennung.
    /// </summary>
    [Fact]
    public void SourceCells_AreFocusableCards()
    {
        var sourceCell = TvOsFile("Ui", "SourceItemCell.cs");
        Assert.Contains("UICollectionViewCell", sourceCell, StringComparison.Ordinal);
        Assert.Contains("CanBecomeFocused", sourceCell, StringComparison.Ordinal);
        Assert.Contains("DidUpdateFocus", sourceCell, StringComparison.Ordinal);
        Assert.Contains("PrepareForReuse", sourceCell, StringComparison.Ordinal);
        Assert.Contains("const string ReuseId", sourceCell, StringComparison.Ordinal);
        Assert.Contains("GetSourceIconAsync", sourceCell, StringComparison.Ordinal);

        var genreCell = TvOsFile("Ui", "SourceGenreCell.cs");
        Assert.Contains("UICollectionViewCell", genreCell, StringComparison.Ordinal);
        Assert.Contains("CanBecomeFocused", genreCell, StringComparison.Ordinal);
        Assert.Contains("IsSelected", genreCell, StringComparison.Ordinal);
        Assert.Contains("const string ReuseId", genreCell, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: Komposition, Navigator und Effekt-Handler verdrahten den
    /// Quellenbrowser vollständig (Dienst, Lade-Effekte, modale
    /// Präsentation) — und kein Schreibzugriff entsteht.
    /// </summary>
    [Fact]
    public void TvOsBrowsing_WiringIsComplete()
    {
        var composition = TvOsFile("AppComposition.cs");
        Assert.Contains("SourceBrowserService", composition, StringComparison.Ordinal);
        Assert.Contains("SourceBrowser", composition, StringComparison.Ordinal);

        var navigator = TvOsFile("Ui", "TvosNavigator.cs");
        Assert.Contains("PresentSourceBrowser", navigator, StringComparison.Ordinal);
        Assert.Contains("SourceBrowserViewController", navigator, StringComparison.Ordinal);
        Assert.Contains("UIModalPresentationStyle.FullScreen", navigator, StringComparison.Ordinal);

        var handler = TvOsFile("Services", "TvosAppEffectHandler.cs");
        Assert.Contains("case LoadSourcesEffect", handler, StringComparison.Ordinal);
        Assert.Contains("case LoadSourceGenresEffect", handler, StringComparison.Ordinal);
        Assert.Contains("case LoadSourceItemsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("case NavigateToSourceOverviewEffect", handler, StringComparison.Ordinal);
        Assert.Contains("PresentSourceBrowser", handler, StringComparison.Ordinal);
        Assert.Contains("SourcesLoadedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("SourceGenresLoadedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("SourceItemsLoadedMessage", handler, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: der Poster-Dienst lädt Quellen-Icons über den
    /// Source-Icons-Endpunkt mit eigenem Schlüsselraum.
    /// </summary>
    [Fact]
    public void TvOsPosterImageService_LoadsSourceIconsWithSeparateCache()
    {
        var service = TvOsFile("Services", "TvosPosterImageService.cs");

        Assert.Contains("GetSourceIconAsync", service, StringComparison.Ordinal);
        Assert.Contains(".sourceicon", service, StringComparison.Ordinal);
        Assert.Contains("iconCache", service, StringComparison.Ordinal);
    }
}
