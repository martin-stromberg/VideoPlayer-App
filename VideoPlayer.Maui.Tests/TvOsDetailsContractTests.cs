using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests der tvOS-Detailseiten (Schritt 6): Da tvOS-Code unter
/// Windows nicht kompiliert werden kann, werden die Detailseite, die
/// Episodenkarte, die modale Stapel-Navigation und die Effekt-Verdrahtung
/// über die Quelldateien geprüft (Muster:
/// <see cref="TvOsBrowsingContractTests"/>).
/// </summary>
public class TvOsDetailsContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string TvOsFile(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root, "VideoPlayer.TvOS" }.Concat(parts).ToArray()));

    private static string MvuFile(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root, "VideoPlayer.Mvu" }.Concat(parts).ToArray()));

    /// <summary>
    /// Prüft: die Detailseite ist modellgetrieben, identitätsgeprüft und
    /// meldet ihre Identität beim Erscheinen — sie zeigt Kopfbereich,
    /// Metazeile, Handlung und die Badges Gesehen / lokale Datei /
    /// Offline-Stand.
    /// </summary>
    [Fact]
    public void DetailsPage_RendersHeaderAndBadgesFromModel()
    {
        var page = TvOsFile("Ui", "MediaDetailsViewController.cs");

        Assert.Contains("IModelRenderable", page, StringComparison.Ordinal);
        Assert.Contains("MediaDetailsRequestedMessage", page, StringComparison.Ordinal);
        Assert.Contains("ViewWillAppear", page, StringComparison.Ordinal);
        Assert.Contains("HeaderTitle", page, StringComparison.Ordinal);
        Assert.Contains("HeaderMeta", page, StringComparison.Ordinal);
        Assert.Contains("HeaderPlot", page, StringComparison.Ordinal);
        Assert.Contains("HeaderImageBytes", page, StringComparison.Ordinal);
        Assert.Contains("IsWatched", page, StringComparison.Ordinal);
        Assert.Contains("HasLocalFile", page, StringComparison.Ordinal);
        Assert.Contains("IsOfflineSnapshot", page, StringComparison.Ordinal);
        Assert.Contains("MediaTypeNames.Normalize", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: die Aktionszeile dispatcht die MVU-Messages für Wiedergabe,
    /// Download und Favoriten — der Favoriten-Schalter folgt dem
    /// Umschaltzustand und dem Offline-Snapshot.
    /// </summary>
    [Fact]
    public void DetailsPage_ActionsDispatchMvuMessages()
    {
        var page = TvOsFile("Ui", "MediaDetailsViewController.cs");

        Assert.Contains("PlayRequestedMessage", page, StringComparison.Ordinal);
        Assert.Contains("DownloadRequestedMessage", page, StringComparison.Ordinal);
        Assert.Contains("FavoriteToggleRequestedMessage", page, StringComparison.Ordinal);
        Assert.Contains("FavoriteStarText", page, StringComparison.Ordinal);
        Assert.Contains("IsFavoriteUpdating", page, StringComparison.Ordinal);
        Assert.Contains("IsPlayable", page, StringComparison.Ordinal);
        Assert.Contains("DownloadNotice", page, StringComparison.Ordinal);
        Assert.Contains("FavoriteError", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: Serienkontext zeigt fokussierbare Staffel-Chips und eine
    /// Episodenreihe; die Auswahl dispatcht Staffel- und
    /// Eintrags-Messages in den MVU-Zyklus.
    /// </summary>
    [Fact]
    public void DetailsPage_SeasonsAndEpisodesAreFocusable()
    {
        var page = TvOsFile("Ui", "MediaDetailsViewController.cs");

        Assert.Contains("UICollectionView", page, StringComparison.Ordinal);
        Assert.Contains("SourceGenreCell", page, StringComparison.Ordinal);
        Assert.Contains("EpisodeItemCell", page, StringComparison.Ordinal);
        Assert.Contains("RegisterClassForCell", page, StringComparison.Ordinal);
        Assert.Contains("SeasonSelectedMessage", page, StringComparison.Ordinal);
        Assert.Contains("SelectedSeasonEpisodes", page, StringComparison.Ordinal);
        Assert.Contains("MediaItemSelectedMessage", page, StringComparison.Ordinal);
        Assert.Contains("RemembersLastFocusedIndexPath", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: Kollektionen zeigen die enthaltenen Filme im Karussell,
    /// Lade- und Fehlerzustände sind sichtbar, die Menü-Taste der
    /// Siri Remote schließt die Seite.
    /// </summary>
    [Fact]
    public void DetailsPage_StatesMoviesAndMenuKeyAreCovered()
    {
        var page = TvOsFile("Ui", "MediaDetailsViewController.cs");

        Assert.Contains("HomeCarouselView", page, StringComparison.Ordinal);
        Assert.Contains("HasMovies", page, StringComparison.Ordinal);
        Assert.Contains("IsLoading", page, StringComparison.Ordinal);
        Assert.Contains("HasError", page, StringComparison.Ordinal);
        Assert.Contains("Erneut versuchen", page, StringComparison.Ordinal);
        Assert.Contains("PressesBegan", page, StringComparison.Ordinal);
        Assert.Contains("UIPressType.Menu", page, StringComparison.Ordinal);
        Assert.Contains("DismissViewController", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: die Episodenkarte zeigt Still, Gesehen-Badge und den
    /// lokale-Datei-Hinweis; Platzhalter sind abgedunkelt und nicht
    /// auswählbar.
    /// </summary>
    [Fact]
    public void EpisodeCell_ShowsWatchedLocalAndPlaceholderStates()
    {
        var cell = TvOsFile("Ui", "EpisodeItemCell.cs");

        Assert.Contains("UICollectionViewCell", cell, StringComparison.Ordinal);
        Assert.Contains("CanBecomeFocused", cell, StringComparison.Ordinal);
        Assert.Contains("PrepareForReuse", cell, StringComparison.Ordinal);
        Assert.Contains("const string ReuseId", cell, StringComparison.Ordinal);
        Assert.Contains("GetImageAsync", cell, StringComparison.Ordinal);
        Assert.Contains("IsWatched", cell, StringComparison.Ordinal);
        Assert.Contains("HasLocalFile", cell, StringComparison.Ordinal);
        Assert.Contains("IsPlaceholder", cell, StringComparison.Ordinal);
        Assert.Contains("IsHighlighted", cell, StringComparison.Ordinal);
        Assert.Contains("Auf diesem Gerät", cell, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: der Navigator stapelt Detailseiten modal über dem obersten
    /// Controller und rendert sie modellgetrieben — der Platzhalter
    /// bleibt den vorbereiteten Wiedergabezielen vorbehalten.
    /// </summary>
    [Fact]
    public void Navigator_StacksModalDetailPages()
    {
        var navigator = TvOsFile("Ui", "TvosNavigator.cs");

        Assert.Contains("PresentMediaDetails", navigator, StringComparison.Ordinal);
        Assert.Contains("MediaDetailsViewController", navigator, StringComparison.Ordinal);
        Assert.Contains("UIModalPresentationStyle.FullScreen", navigator, StringComparison.Ordinal);
        Assert.Contains("TopmostPresented", navigator, StringComparison.Ordinal);
        Assert.Contains("PresentPreparedTarget", navigator, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: Komposition und Effekt-Handler verdrahten die Detailseite
    /// vollständig — Detaildienst, Favoriten-Dienst, Lade-, Kopfbild-,
    /// Favoriten- und Download-Effekte.
    /// </summary>
    [Fact]
    public void TvOsDetails_WiringIsComplete()
    {
        var composition = TvOsFile("AppComposition.cs");
        Assert.Contains("MediaDetailsService", composition, StringComparison.Ordinal);
        Assert.Contains("FavoritesService", composition, StringComparison.Ordinal);

        var handler = TvOsFile("Services", "TvosAppEffectHandler.cs");
        Assert.Contains("case NavigateToMediaDetailsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("PresentMediaDetails", handler, StringComparison.Ordinal);
        Assert.Contains("case LoadMediaDetailsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("MediaDetailsLoadedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("case LoadMediaDetailsHeaderEffect", handler, StringComparison.Ordinal);
        Assert.Contains("MediaDetailsHeaderImageLoadedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("case ToggleFavoriteEffect", handler, StringComparison.Ordinal);
        Assert.Contains("FavoriteToggleCompletedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("case EnqueueDownloadEffect", handler, StringComparison.Ordinal);
        Assert.Contains("DownloadEnqueueCompletedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("NavigateToPlayerEffect", handler, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: das Detail-Anzeigemodell trägt den Gesehen-Zeitpunkt des
    /// Mediums und der Dienst befüllt ihn aus dem Server-Modell.
    /// </summary>
    [Fact]
    public void MediaDetailsView_CarriesWatchedState()
    {
        var view = MvuFile("MediaDetailsView.cs");
        Assert.Contains("WatchedAt", view, StringComparison.Ordinal);
        Assert.Contains("IsWatched", view, StringComparison.Ordinal);

        var service = MvuFile("MediaDetailsService.cs");
        Assert.Contains("WatchedAt = movie.WatchedAt", service, StringComparison.Ordinal);
        Assert.Contains("WatchedAt = episode.WatchedAt", service, StringComparison.Ordinal);
    }
}
