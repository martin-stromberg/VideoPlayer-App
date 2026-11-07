using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests der tvOS-Startseite (Schritt 4): Da tvOS-Code unter
/// Windows nicht kompiliert werden kann, werden die Karussell-Seite,
/// die Medienkarte, der Poster-Cache und die Verdrahtung der
/// vorbereiteten Navigationsziele über die Quelldateien geprüft
/// (Muster: <see cref="TvOsConnectionContractTests"/>).
/// </summary>
public class TvOsHomePageContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string TvOsFile(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root, "VideoPlayer.TvOS" }.Concat(parts).ToArray()));

    /// <summary>
    /// Prüft: die Startseite ist modellgetrieben, baut Karussells aus
    /// den drei Pflicht-Listen plus saisonalen Sektionen und dispatcht
    /// die Auswahl-/Nachlade-Messages.
    /// </summary>
    [Fact]
    public void HomePage_RendersAllCarouselsFromModel()
    {
        var page = TvOsFile("Ui", "HomePageViewController.cs");

        Assert.Contains("IModelRenderable", page, StringComparison.Ordinal);
        Assert.Contains("UIScrollView", page, StringComparison.Ordinal);
        Assert.Contains("HomeCarouselView", page, StringComparison.Ordinal);

        // Drei Pflicht-Karussells plus saisonale Sektionen
        Assert.Contains("ContinueWatching", page, StringComparison.Ordinal);
        Assert.Contains("Weiterschauen", page, StringComparison.Ordinal);
        Assert.Contains("Favorites", page, StringComparison.Ordinal);
        Assert.Contains("RecentEntries", page, StringComparison.Ordinal);
        Assert.Contains("Neu im Programm", page, StringComparison.Ordinal);
        Assert.Contains("SeasonalGenres", page, StringComparison.Ordinal);

        // Auswahl- und Nachlade-Messages
        Assert.Contains("ContinueWatchingItemSelectedMessage", page, StringComparison.Ordinal);
        Assert.Contains("MediaItemSelectedMessage", page, StringComparison.Ordinal);
        Assert.Contains("HomeListsReloadRequestedMessage", page, StringComparison.Ordinal);

        // Offline-Hinweis und Ablage-Hinweis; Platzhalter-Schutz
        Assert.Contains("IsOffline", page, StringComparison.Ordinal);
        Assert.Contains("StorageNotice", page, StringComparison.Ordinal);
        Assert.Contains("IsSelectable", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: saisonale Sektionen planen ihren Ablauf und lösen das
    /// Nachladen aus — wie die Ablauf-Timer-Logik der MAUI-Startseite.
    /// </summary>
    [Fact]
    public void HomePage_SchedulesSeasonalExpiry()
    {
        var page = TvOsFile("Ui", "HomePageViewController.cs");

        Assert.Contains("ExpiresAt", page, StringComparison.Ordinal);
        Assert.Contains("TimeProvider", page, StringComparison.Ordinal);
        Assert.Contains("CreateTimer", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: die Medienkarte ist fokussierbar, hebt den Fokus
    /// hervor, zeigt Fortschritt und Poster und setzt ihren Zustand
    /// beim Recyceln zurück.
    /// </summary>
    [Fact]
    public void HomeMediaItemCell_IsFocusableCardWithPosterAndProgress()
    {
        var cell = TvOsFile("Ui", "HomeMediaItemCell.cs");

        Assert.Contains("UICollectionViewCell", cell, StringComparison.Ordinal);
        Assert.Contains("UIImageView", cell, StringComparison.Ordinal);
        Assert.Contains("UIProgressView", cell, StringComparison.Ordinal);
        Assert.Contains("CanBecomeFocused", cell, StringComparison.Ordinal);
        Assert.Contains("DidUpdateFocus", cell, StringComparison.Ordinal);
        Assert.Contains("PrepareForReuse", cell, StringComparison.Ordinal);
        Assert.Contains("CardOpacity", cell, StringComparison.Ordinal);
        Assert.Contains("ShowProgress", cell, StringComparison.Ordinal);
        Assert.Contains("PosterPictureId", cell, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: die Karussell-Reihe nutzt horizontales Flow-Layout, eine
    /// UICollectionViewSource, Zell-Registrierung und Fokus-Gedächtnis.
    /// </summary>
    [Fact]
    public void HomeCarouselView_UsesFocusableCollectionViewSource()
    {
        var carousel = TvOsFile("Ui", "HomeCarouselView.cs");

        Assert.Contains("UICollectionView", carousel, StringComparison.Ordinal);
        Assert.Contains("UICollectionViewFlowLayout", carousel, StringComparison.Ordinal);
        Assert.Contains("UICollectionViewScrollDirection.Horizontal", carousel, StringComparison.Ordinal);
        Assert.Contains("UICollectionViewSource", carousel, StringComparison.Ordinal);
        Assert.Contains("RegisterClassForCell", carousel, StringComparison.Ordinal);
        Assert.Contains("DequeueReusableCell", carousel, StringComparison.Ordinal);
        Assert.Contains("RemembersLastFocusedIndexPath", carousel, StringComparison.Ordinal);
        Assert.Contains("ItemSelected", carousel, StringComparison.Ordinal);
        Assert.Contains("ReloadData", carousel, StringComparison.Ordinal);
        Assert.Contains("SequenceEqual", carousel, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: der Poster-Dienst nutzt die etablierte Cache-Konvention
    /// (Arbeitsspeicher, Datei im Previews-Verzeichnis, Serverabruf
    /// über api/pictures) und liefert tvOS-Bilder.
    /// </summary>
    [Fact]
    public void TvOsPosterImageService_UsesEstablishedCacheConvention()
    {
        var service = TvOsFile("Services", "TvosPosterImageService.cs");

        Assert.Contains("GetPictureAsync", service, StringComparison.Ordinal);
        Assert.Contains("PreviewsDirectory", service, StringComparison.Ordinal);
        Assert.Contains("UIImage", service, StringComparison.Ordinal);
        Assert.Contains("ConcurrentDictionary", service, StringComparison.Ordinal);
        Assert.Contains("AuthSessionService", service, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: Home-Tab, Navigator, Komposition und Tab-Leiste sind
    /// verdrahtet (Seite, Poster-Dienst, vorbereitetes Ziel).
    /// </summary>
    [Fact]
    public void HomePage_WiringIsComplete()
    {
        var tabBar = TvOsFile("Ui", "MenuTabBarController.cs");
        Assert.Contains("HomePageViewController", tabBar, StringComparison.Ordinal);
        Assert.Contains("TvosPosterImageService", tabBar, StringComparison.Ordinal);

        var composition = TvOsFile("AppComposition.cs");
        Assert.Contains("TvosPosterImageService", composition, StringComparison.Ordinal);
        Assert.Contains("PosterImages", composition, StringComparison.Ordinal);

        var navigator = TvOsFile("Ui", "TvosNavigator.cs");
        Assert.Contains("PresentPlayer", navigator, StringComparison.Ordinal);
        Assert.Contains("TvosPlayerViewController", navigator, StringComparison.Ordinal);
        Assert.Contains("PresentViewController", navigator, StringComparison.Ordinal);
        Assert.Contains("UIModalPresentationStyle.FullScreen", navigator, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: der tvOS-Effekt-Handler bildet die Navigations-Effekte der
    /// Eintrags-Auswahl ab — die Medien-Detailseite (`PresentMediaDetails`)
    /// und die Wiedergabe (`PresentPlayer`) sind real verdrahtet.
    /// </summary>
    [Fact]
    public void TvOsEffectHandler_MapsSelectionEffectsToRealTargets()
    {
        var handler = TvOsFile("Services", "TvosAppEffectHandler.cs");

        Assert.Contains("case NavigateToMediaDetailsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("case NavigateToPlayerEffect", handler, StringComparison.Ordinal);
        Assert.Contains("case StartPlaylistPlaybackEffect", handler, StringComparison.Ordinal);
        Assert.Contains("PresentPlayer", handler, StringComparison.Ordinal);
        Assert.Contains("PresentMediaDetails", handler, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: keinerlei Schreibzugriff auf verwaltete Inhalte —
    /// Weiterschauen-Kontextmenü- und Favoriten-Entfernen-Effekte bleiben
    /// No-Op. Die Favoriten-Umschaltung selbst ist seit dem
    /// Detailseiten-Schritt verdrahtet (erlaubtes Nutzungsdatum), das
    /// Download-Einreihen ist nur als vorbereitete Hinweis-Aktion
    /// verdrahtet — beides ohne Inhalts-Schreibzugriff.
    /// </summary>
    [Fact]
    public void TvOsEffectHandler_DoesNotWireContentMutations()
    {
        var handler = TvOsFile("Services", "TvosAppEffectHandler.cs");

        Assert.DoesNotContain("ContinueWatchingHide", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("ContinueWatchingSkip", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("RemoveFavoriteEffect", handler, StringComparison.Ordinal);
    }
}
