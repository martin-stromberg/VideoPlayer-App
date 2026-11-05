using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests des lesenden tvOS-Playlist-Zugriffs (Schritt 8): Da
/// tvOS-Code unter Windows nicht kompiliert werden kann, werden die
/// Übersichtsseite, die modale Detailseite, die Navigator-Präsentation
/// und die Effekt-Verdrahtung über die Quelldateien geprüft (Muster:
/// <see cref="TvOsPlaybackContractTests"/>).
/// </summary>
public class TvOsPlaylistsContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string TvOsFile(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root, "VideoPlayer.TvOS" }.Concat(parts).ToArray()));

    /// <summary>
    /// Prüft: die Übersichtsseite ist modellgetrieben, zeigt eigene und
    /// öffentliche Playlists mit Cover/Sortiermodus/Genres und meldet
    /// Öffnen sowie Auswahl in den MVU-Kreislauf.
    /// </summary>
    [Fact]
    public void Overview_RendersBothSectionsAndDispatchesMessages()
    {
        var page = TvOsFile("Ui", "PlaylistsPageViewController.cs");

        Assert.Contains("IModelRenderable", page, StringComparison.Ordinal);
        Assert.Contains("PlaylistsPage", page, StringComparison.Ordinal);
        Assert.Contains("UITableView", page, StringComparison.Ordinal);
        Assert.Contains("PlaylistsPageOpenedMessage", page, StringComparison.Ordinal);
        Assert.Contains("PlaylistSelectedMessage", page, StringComparison.Ordinal);
        Assert.Contains("NumberOfSections", page, StringComparison.Ordinal);
        Assert.Contains("TitleForHeader", page, StringComparison.Ordinal);

        var cell = TvOsFile("Ui", "PlaylistTableCell.cs");
        Assert.Contains("GetPlaylistCoverAsync", cell, StringComparison.Ordinal);
        Assert.Contains("CoverPlaylistId", cell, StringComparison.Ordinal);
        Assert.Contains("SortModeText", cell, StringComparison.Ordinal);
        Assert.Contains("GenreText", cell, StringComparison.Ordinal);
        Assert.Contains("appliedPlaylistId", cell, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: die Detailseite rendert den Playlist-Zustand
    /// identitätsgeprüft, lädt Einträge seitenweise nach und meldet
    /// Öffnen, Auswahl, Nachladen und Wiederholen in den MVU-Kreislauf.
    /// </summary>
    [Fact]
    public void Details_RendersEntriesAndDispatchesPagingAndSelection()
    {
        var page = TvOsFile("Ui", "PlaylistDetailsViewController.cs");

        Assert.Contains("IModelRenderable", page, StringComparison.Ordinal);
        Assert.Contains("PlaylistDetails", page, StringComparison.Ordinal);
        Assert.Contains("PlaylistDetailsOpenedMessage", page, StringComparison.Ordinal);
        Assert.Contains("PlaylistEntriesNextPageRequestedMessage", page, StringComparison.Ordinal);
        Assert.Contains("PlaylistEntriesRetryRequestedMessage", page, StringComparison.Ordinal);
        Assert.Contains("PlaylistEntrySelectedMessage", page, StringComparison.Ordinal);
        Assert.Contains("PlaylistPlaybackStartRequestedMessage", page, StringComparison.Ordinal);
        Assert.Contains("WillDisplayCell", page, StringComparison.Ordinal);
        Assert.Contains("DismissViewController", page, StringComparison.Ordinal);
        Assert.Contains("UIPressType.Menu", page, StringComparison.Ordinal);
        Assert.Contains("details.PlaylistId != playlistId", page, StringComparison.Ordinal);

        var cell = TvOsFile("Ui", "PlaylistEntryCell.cs");
        Assert.Contains("ResolvedPictureId", cell, StringComparison.Ordinal);
        Assert.Contains("Subtitle", cell, StringComparison.Ordinal);
        Assert.Contains("IsAccessible", cell, StringComparison.Ordinal);
        Assert.Contains("appliedPictureId", cell, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: der Navigator präsentiert die Playlist-Detailseite modal
    /// über dem obersten Controller und verteilt den Modellstand an
    /// sie — ohne erneutes Stapeln bei bereits präsentierter Seite.
    /// </summary>
    [Fact]
    public void Navigator_PresentsPlaylistDetailsModally()
    {
        var navigator = TvOsFile("Ui", "TvosNavigator.cs");

        Assert.Contains("PresentPlaylistDetails", navigator, StringComparison.Ordinal);
        Assert.Contains("PlaylistDetailsViewController", navigator, StringComparison.Ordinal);
        Assert.Contains("UIModalPresentationStyle.FullScreen", navigator, StringComparison.Ordinal);
        Assert.Contains("TopmostPresented", navigator, StringComparison.Ordinal);
        Assert.Contains("playlistDetailsPage?.Render(model)", navigator, StringComparison.Ordinal);
        Assert.Contains("ReleasePlaylistDetailsPage", navigator, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: die Menüseite „Playlists" ist als echter Menübereich
    /// verdrahtet statt als Platzhalter.
    /// </summary>
    [Fact]
    public void Menu_PlaylistsIsRealPage()
    {
        var tabBar = TvOsFile("Ui", "MenuTabBarController.cs");

        Assert.Contains("AppPage.Playlists", tabBar, StringComparison.Ordinal);
        Assert.Contains("PlaylistsPageViewController", tabBar, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: der Effekt-Handler verdrahtet den lesenden
    /// Playlist-Zugriff vollständig — Übersicht, Detail-Navigation,
    /// Kopfdaten und paginierte Einträge samt Ergebnis-Messages.
    /// </summary>
    [Fact]
    public void TvOsPlaylists_WiringIsComplete()
    {
        var handler = TvOsFile("Services", "TvosAppEffectHandler.cs");

        Assert.Contains("case LoadPlaylistsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("PlaylistsLoadedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("AppUpdate.PlaylistsLoadError", handler, StringComparison.Ordinal);
        Assert.Contains("case NavigateToPlaylistDetailsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("PresentPlaylistDetails", handler, StringComparison.Ordinal);
        Assert.Contains("case LoadPlaylistDetailsEffect", handler, StringComparison.Ordinal);
        Assert.Contains("PlaylistDetailsLoadedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("IsCurrentUserAdminAsync", handler, StringComparison.Ordinal);
        Assert.Contains("case LoadPlaylistEntriesEffect", handler, StringComparison.Ordinal);
        Assert.Contains("PlaylistEntriesLoadedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("AppUpdate.PlaylistEntriesLoadError", handler, StringComparison.Ordinal);

        var posters = TvOsFile("Services", "TvosPosterImageService.cs");
        Assert.Contains("GetPlaylistCoverAsync", posters, StringComparison.Ordinal);
        Assert.Contains(".playlistcover", posters, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: die neuen Playlist-Seiten enthalten keinerlei
    /// Verwaltungszugriff — weder Formular noch Lösch-, Verschiebe-,
    /// Genre- oder Öffentlich-Aktionen (rein lesender Zugriff).
    /// </summary>
    [Fact]
    public void Pages_DoNotExposePlaylistManagement()
    {
        foreach (var file in new[]
        {
            TvOsFile("Ui", "PlaylistsPageViewController.cs"),
            TvOsFile("Ui", "PlaylistDetailsViewController.cs"),
            TvOsFile("Ui", "PlaylistTableCell.cs"),
            TvOsFile("Ui", "PlaylistEntryCell.cs")
        })
        {
            Assert.DoesNotContain("PlaylistCreateRequestedMessage", file, StringComparison.Ordinal);
            Assert.DoesNotContain("PlaylistEditRequestedMessage", file, StringComparison.Ordinal);
            Assert.DoesNotContain("PlaylistDeleteConfirmedMessage", file, StringComparison.Ordinal);
            Assert.DoesNotContain("PlaylistEntryRemoveRequestedMessage", file, StringComparison.Ordinal);
            Assert.DoesNotContain("PlaylistEntryMove", file, StringComparison.Ordinal);
            Assert.DoesNotContain("PlaylistSortModeRequestedMessage", file, StringComparison.Ordinal);
            Assert.DoesNotContain("PlaylistGenreEditor", file, StringComparison.Ordinal);
            Assert.DoesNotContain("PlaylistPublicToggledMessage", file, StringComparison.Ordinal);
        }
    }
}
