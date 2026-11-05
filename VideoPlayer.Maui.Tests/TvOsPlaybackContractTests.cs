using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests der tvOS-Wiedergabe (Schritt 7): Da tvOS-Code unter
/// Windows nicht kompiliert werden kann, werden der native Player-
/// Controller, die Navigator-Präsentation und die Effekt-Verdrahtung
/// über die Quelldateien geprüft (Muster:
/// <see cref="TvOsDetailsContractTests"/>).
/// </summary>
public class TvOsPlaybackContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string TvOsFile(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root, "VideoPlayer.TvOS" }.Concat(parts).ToArray()));

    /// <summary>
    /// Prüft: der Player-Controller ist modellgetrieben und wendet die
    /// aufgelöste Quelle identitätsgebunden auf den nativen Player an —
    /// Stream-URL und lokale Datei werden unterschieden, die
    /// Fortsetzungsposition wird erst bei Wiedergabebereitschaft
    /// angesprungen.
    /// </summary>
    [Fact]
    public void Player_AppliesSourceAndResumeFromModel()
    {
        var page = TvOsFile("Ui", "TvosPlayerViewController.cs");

        Assert.Contains("AVPlayerViewController", page, StringComparison.Ordinal);
        Assert.Contains("IModelRenderable", page, StringComparison.Ordinal);
        Assert.Contains("ReplaceCurrentItemWithPlayerItem", page, StringComparison.Ordinal);
        Assert.Contains("appliedMediaId", page, StringComparison.Ordinal);
        Assert.Contains("sourceApplyFailed", page, StringComparison.Ordinal);
        Assert.Contains("pendingResumePosition", page, StringComparison.Ordinal);
        Assert.Contains("ResumePosition", page, StringComparison.Ordinal);
        Assert.Contains("PlaybackSourceKind.ServerStream", page, StringComparison.Ordinal);
        Assert.Contains("PlaybackSourceKind.LocalFile", page, StringComparison.Ordinal);
        Assert.Contains("AVPlayerItemStatus.ReadyToPlay", page, StringComparison.Ordinal);
        Assert.Contains("AppLogService.MaskAccessToken", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: der Player übersetzt native Ereignisse in die MVU-Messages —
    /// periodischer 5-s-Takt, Ende- und Fehlerereignis sowie Seiten-
    /// Lebenszyklus bleiben reine Ereignis-Übersetzung.
    /// </summary>
    [Fact]
    public void Player_TranslatesTickEndAndFailureToMessages()
    {
        var page = TvOsFile("Ui", "TvosPlayerViewController.cs");

        Assert.Contains("AddPeriodicTimeObserver", page, StringComparison.Ordinal);
        Assert.Contains("PlaybackProgressMessage", page, StringComparison.Ordinal);
        Assert.Contains("ObserveDidPlayToEndTime", page, StringComparison.Ordinal);
        Assert.Contains("PlaybackEndedMessage", page, StringComparison.Ordinal);
        Assert.Contains("ObserveFailedToPlayToEndTime", page, StringComparison.Ordinal);
        Assert.Contains("PlaybackFailedMessage", page, StringComparison.Ordinal);
        Assert.Contains("PlayerPageOpenedMessage", page, StringComparison.Ordinal);
        Assert.Contains("PlayerClosedMessage", page, StringComparison.Ordinal);
        Assert.Contains("AVPlayerTimeControlStatus.Playing", page, StringComparison.Ordinal);
        Assert.Contains("AVPlayerTimeControlStatus.WaitingToPlayAtSpecifiedRate", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: der Playlist-Kontext nutzt die Transport-Bar-Menüeinträge
    /// für next/previous und den Info-Tab für die Kontextanzeige — der
    /// Player verunmöglicht den Playlist-Kontext nicht.
    /// </summary>
    [Fact]
    public void Player_PlaylistContextUsesTransportMenuAndInfoPanel()
    {
        var page = TvOsFile("Ui", "TvosPlayerViewController.cs");

        Assert.Contains("TransportBarCustomMenuItems", page, StringComparison.Ordinal);
        Assert.Contains("PlaylistNextRequestedMessage", page, StringComparison.Ordinal);
        Assert.Contains("PlaylistPreviousRequestedMessage", page, StringComparison.Ordinal);
        Assert.Contains("IsPlaylistPlayback", page, StringComparison.Ordinal);
        Assert.Contains("CanNavigateNext", page, StringComparison.Ordinal);
        Assert.Contains("CanNavigatePrevious", page, StringComparison.Ordinal);
        Assert.Contains("CustomInfoViewControllers", page, StringComparison.Ordinal);

        var info = TvOsFile("Ui", "TvosPlayerInfoViewController.cs");
        Assert.Contains("PlaylistLabel", info, StringComparison.Ordinal);
        Assert.Contains("IsWatched", info, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: der Navigator präsentiert den Player modal über dem
    /// obersten Controller und verteilt den Modellstand an ihn —
    /// ohne erneutes Stapeln bei bereits präsentiertem Player.
    /// </summary>
    [Fact]
    public void Navigator_PresentsPlayerModally()
    {
        var navigator = TvOsFile("Ui", "TvosNavigator.cs");

        Assert.Contains("PresentPlayer", navigator, StringComparison.Ordinal);
        Assert.Contains("TvosPlayerViewController", navigator, StringComparison.Ordinal);
        Assert.Contains("UIModalPresentationStyle.FullScreen", navigator, StringComparison.Ordinal);
        Assert.Contains("TopmostPresented", navigator, StringComparison.Ordinal);
        Assert.Contains("playerPage?.Render(model)", navigator, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: Komposition und Effekt-Handler verdrahten die Wiedergabe
    /// vollständig — Dienst-Singletons, alle Player-Effekte samt
    /// Ergebnis-Messages, Playlist-Navigation und Download-Ablauf.
    /// </summary>
    [Fact]
    public void TvOsPlayback_WiringIsComplete()
    {
        var composition = TvOsFile("AppComposition.cs");
        Assert.Contains("PlaybackSourceService", composition, StringComparison.Ordinal);
        Assert.Contains("UpcomingEpisodesService", composition, StringComparison.Ordinal);
        Assert.Contains("PlaylistService", composition, StringComparison.Ordinal);
        Assert.Contains("DownloadService", composition, StringComparison.Ordinal);

        var handler = TvOsFile("Services", "TvosAppEffectHandler.cs");
        Assert.Contains("case NavigateToPlayerEffect", handler, StringComparison.Ordinal);
        Assert.Contains("PresentPlayer", handler, StringComparison.Ordinal);
        Assert.Contains("case ResolvePlaybackSourceEffect", handler, StringComparison.Ordinal);
        Assert.Contains("PlaybackSourceResolvedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("case LoadUpcomingEpisodesEffect", handler, StringComparison.Ordinal);
        Assert.Contains("UpcomingEpisodesLoadedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("case ReportPlaybackProgressEffect", handler, StringComparison.Ordinal);
        Assert.Contains("case StartPlaylistPlaybackEffect", handler, StringComparison.Ordinal);
        Assert.Contains("PlaylistPlaybackStartedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("case LoadNextPlaylistEntryEffect", handler, StringComparison.Ordinal);
        Assert.Contains("case LoadPreviousPlaylistEntryEffect", handler, StringComparison.Ordinal);
        Assert.Contains("case AdvancePlaylistEntryEffect", handler, StringComparison.Ordinal);
        Assert.Contains("PlaylistNavigationCompletedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("case ProtectDownloadExpiryEffect", handler, StringComparison.Ordinal);
        Assert.Contains("case ExpireWatchedDownloadEffect", handler, StringComparison.Ordinal);
        Assert.Contains("allowServerStream", handler, StringComparison.Ordinal);
    }
}
