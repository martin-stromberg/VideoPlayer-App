using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests der Wiedergabeseite (Schritt 10): die MAUI-Schicht kann
/// im Testprojekt nicht instanziiert werden — verdrahtete Struktur, Route
/// und Plattform-Voraussetzungen (MediaElement, iOS-Hintergrundmodus für
/// PiP, Windows-CompactOverlay) werden daher über die Quelldateien geprüft
/// (Muster: <see cref="SourcesContractTests"/>). Echte UI-End-to-End-
/// Automatisierung ist im Repository derzeit nicht vorhanden
/// (dokumentierte Einschränkung).
/// </summary>
public class PlayerContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));

    /// <summary>Prüft AppShell: die Route „player" ist registriert.</summary>
    [Fact]
    public void PlayerPage_RouteRegistered()
    {
        var shell = Read("VideoPlayer.Maui", "AppShell.xaml.cs");

        Assert.Contains("RegisterRoute(\"player\"", shell, StringComparison.Ordinal);
        Assert.Contains("Pages.PlayerPage", shell, StringComparison.Ordinal);
    }

    /// <summary>Prüft Projekt/MauiProgram: MediaElement-Paket und Toolkit-Aktivierung.</summary>
    [Fact]
    public void MauiApp_UsesMediaElement()
    {
        var csproj = Read("VideoPlayer.Maui", "VideoPlayer.Maui.csproj");
        var program = Read("VideoPlayer.Maui", "MauiProgram.cs");

        Assert.Contains("CommunityToolkit.Maui.MediaElement", csproj, StringComparison.Ordinal);
        Assert.Contains("UseMauiCommunityToolkitMediaElement", program, StringComparison.Ordinal);
        Assert.Contains("PlaybackSourceService", program, StringComparison.Ordinal);
        Assert.Contains("IPictureInPictureService", program, StringComparison.Ordinal);
    }

    /// <summary>Prüft Info.plist (iOS): Hintergrund-Audio für den PiP-Modus.</summary>
    [Fact]
    public void IosInfoPlist_DeclaresAudioBackgroundMode()
    {
        var plist = Read("VideoPlayer.Maui", "Platforms", "iOS", "Info.plist");

        Assert.Contains("UIBackgroundModes", plist, StringComparison.Ordinal);
        Assert.Contains("<string>audio</string>", plist, StringComparison.Ordinal);
    }

    /// <summary>Prüft PlayerPage: MediaElement mit Autoplay und Plattform-Steuerung (iOS-PiP-Voraussetzung).</summary>
    [Fact]
    public void PlayerPage_UsesMediaElementWithControls()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "PlayerPage.xaml");

        Assert.Contains("MediaElement", xaml, StringComparison.Ordinal);
        Assert.Contains("ShouldAutoPlay=\"True\"", xaml, StringComparison.Ordinal);
        // Transportsteuerung einblenden — auf iOS enthält sie die
        // native PiP-Schaltfläche des AVPlayerViewController.
        Assert.Contains("ShouldShowPlaybackControls=\"True\"", xaml, StringComparison.Ordinal);
        Assert.Contains("MediaOpened=", xaml, StringComparison.Ordinal);
        Assert.Contains("MediaEnded=", xaml, StringComparison.Ordinal);
        Assert.Contains("MediaFailed=", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft PlayerPage: dispatcht Öffnen-, Takt-, Ende-/Fehler- und PiP-Nachrichten.</summary>
    [Fact]
    public void PlayerPage_DispatchesPlaybackMessages()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "PlayerPage.xaml.cs");

        Assert.Contains("PlayerPageOpenedMessage", code, StringComparison.Ordinal);
        Assert.Contains("PlaybackProgressMessage", code, StringComparison.Ordinal);
        Assert.Contains("PlaybackEndedMessage", code, StringComparison.Ordinal);
        Assert.Contains("PlaybackFailedMessage", code, StringComparison.Ordinal);
        Assert.Contains("PictureInPictureRequestedMessage", code, StringComparison.Ordinal);
        Assert.Contains("PlayerClosedMessage", code, StringComparison.Ordinal);
        // Fortsetzungsposition per SeekTo beim Öffnen des Mediums.
        Assert.Contains("SeekTo", code, StringComparison.Ordinal);
        // Lokale Dateien über die Quellenabstraktion.
        Assert.Contains("PlaybackSourceKind.LocalFile", code, StringComparison.Ordinal);
        Assert.Contains("MediaSource.FromFile", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft PictureInPictureService: Windows-CompactOverlay (Mini-Player).</summary>
    [Fact]
    public void PictureInPictureService_UsesCompactOverlayOnWindows()
    {
        var code = Read("VideoPlayer.Maui", "Services", "PictureInPictureService.cs");

        Assert.Contains("#if WINDOWS", code, StringComparison.Ordinal);
        Assert.Contains("AppWindowPresenterKind.CompactOverlay", code, StringComparison.Ordinal);
        Assert.Contains("AppWindowPresenterKind.Default", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft MediaDetailsPage: „Abspielen" im Kopf und je Episode verdrahtet.</summary>
    [Fact]
    public void MediaDetailsPage_HasPlayButton()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "MediaDetailsPage.xaml");
        var code = Read("VideoPlayer.Maui", "Pages", "MediaDetailsPage.xaml.cs");

        Assert.Contains("PlayButton", xaml, StringComparison.Ordinal);
        Assert.Contains("Abspielen", xaml, StringComparison.Ordinal);
        Assert.Contains("OnEpisodePlayClicked", xaml, StringComparison.Ordinal);
        Assert.Contains("PlayRequestedMessage", code, StringComparison.Ordinal);
        Assert.Contains("details.IsPlayable", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft ShellAppEffectHandler: alle Player-Effekte sind verdrahtet.</summary>
    [Fact]
    public void EffectHandler_HandlesPlayerEffects()
    {
        var code = Read("VideoPlayer.Maui", "Services", "ShellAppEffectHandler.cs");

        Assert.Contains("NavigateToPlayerEffect", code, StringComparison.Ordinal);
        Assert.Contains("ResolvePlaybackSourceEffect", code, StringComparison.Ordinal);
        Assert.Contains("ReportPlaybackProgressEffect", code, StringComparison.Ordinal);
        Assert.Contains("TogglePictureInPictureEffect", code, StringComparison.Ordinal);
        // Fortschritte gehen ausschließlich über den bestehenden
        // ProgressReportService (kein zweites Warteschlangensystem).
        Assert.Contains("ProgressReportService", code, StringComparison.Ordinal);
    }
}
