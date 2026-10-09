using System.Xml.Linq;
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

    /// <summary>
    /// Prüft PlayerPage: das Kopf-Overlay „OverlayChrome" blockiert nicht die
    /// Videofläche — es füllt nicht das gesamte Grid (kein Default `Fill`)
    /// und ist außerhalb seines Mini-Player-Schalters eingabedurchlässig.
    /// Pinnt die Korrektur des Hit-Test-Blockers, der die nativen
    /// Transportsteuerungen in der Vollansicht unbedienbar machte.
    /// </summary>
    [Fact]
    public void PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface()
    {
        var document = XDocument.Parse(Read("VideoPlayer.Maui", "Pages", "PlayerPage.xaml"));
        var mauiNs = XNamespace.Get("http://schemas.microsoft.com/dotnet/2021/maui");
        var xNs = XNamespace.Get("http://schemas.microsoft.com/winfx/2009/xaml");

        var overlay = document.Descendants(mauiNs + "VerticalStackLayout")
            .SingleOrDefault(e => (string?)e.Attribute(xNs + "Name") == "OverlayChrome");
        Assert.NotNull(overlay);

        // Ohne VerticalOptions gilt der LayoutOptions-Default Fill:
        // das Layout belegt das gesamte Grid über dem MediaElement.
        Assert.Equal("Start", (string?)overlay.Attribute("VerticalOptions"));

        // Das sichtbare Kopfband bleibt für Eingaben durchlässig.
        var band = overlay.Element(mauiNs + "Border");
        Assert.NotNull(band);
        Assert.Equal("True", (string?)band.Attribute("InputTransparent"));

        // Der Mini-Player-Schalter darf NICHT innerhalb des input-
        // transparenten Bandes liegen: unter Windows deaktiviert
        // IsHitTestVisible=false am Elternelement den gesamten Teilbaum —
        // ein InputTransparent="False" am Kind reaktiviert ihn nicht.
        // Der Schalter ist deshalb eigenes Grid-Element über dem Band.
        Assert.Empty(band.Descendants(mauiNs + "Button"));

        var rootGrid = document.Descendants(mauiNs + "Grid")
            .SingleOrDefault(e => (string?)e.Attribute(xNs + "Name") == "PlayerRoot");
        Assert.NotNull(rootGrid);
        var pipButton = rootGrid.Elements(mauiNs + "Button")
            .SingleOrDefault(e => (string?)e.Attribute(xNs + "Name") == "PipButton");
        Assert.NotNull(pipButton);
        Assert.Equal("OnPipClicked", (string?)pipButton.Attribute("Clicked"));
    }

    /// <summary>
    /// Prüft PlayerPage: Windows-Workaround für die Transportsteuerung.
    /// Das MediaPlayerElement blendet seine Steuerung nach dem Timeout
    /// in dieser Kombination nicht mehr über eigene Pointer-Ereignisse
    /// ein — ein Tip auf die Videofläche ruft deshalb
    /// <c>TransportControls.Show()</c> (nur Windows; iOS/Android regeln
    /// das Einblenden nativ).
    /// </summary>
    [Fact]
    public void PlayerPage_TapShowsTransportControlsOnWindows()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "PlayerPage.xaml.cs");
        var xaml = Read("VideoPlayer.Maui", "Pages", "PlayerPage.xaml");

        Assert.Contains("x:Name=\"PlayerRoot\"", xaml, StringComparison.Ordinal);
        var ctor = MethodBody(code, "public PlayerPage(");
        Assert.Contains("#if WINDOWS", ctor, StringComparison.Ordinal);
        Assert.Contains("TapGestureRecognizer", ctor, StringComparison.Ordinal);
        Assert.Contains("OnPlayerTapped", ctor, StringComparison.Ordinal);
        Assert.Contains("TransportControls?.Show()",
            MethodBody(code, "private void OnPlayerTapped"), StringComparison.Ordinal);
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

    /// <summary>
    /// Prüft PlayerPage: das <c>ModelChanged</c>-Abo wird beim Verlassen
    /// abgemeldet (in <c>OnDisappearing</c>) statt nur im Konstruktor —
    /// verwaiste Wiedergabe-Seiten bleiben sonst am <c>MvuHost</c>
    /// hängen und rendern auf ihrem losgelösten <c>MediaElement</c>
    /// weiter (Absturz-Ursache des wiederholten Abspielens).
    /// </summary>
    [Fact]
    public void PlayerPage_UnsubscribesModelChangedOnDisappearing()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "PlayerPage.xaml.cs");

        Assert.Contains("mvuHost.ModelChanged -= OnModelChanged", code, StringComparison.Ordinal);
        Assert.Contains("ModelChanged -= OnModelChanged",
            MethodBody(code, "protected override void OnDisappearing"), StringComparison.Ordinal);
        // Abo liegt im Sichtbarkeitszyklus (OnAppearing), nicht im
        // Konstruktor — sonst bleibt die verlassene Seite abonniert.
        Assert.Contains("ModelChanged += OnModelChanged",
            MethodBody(code, "protected override void OnAppearing"), StringComparison.Ordinal);
        Assert.DoesNotContain("ModelChanged +=",
            MethodBody(code, "public PlayerPage("), StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft PlayerPage: native Elementzugriffe aus Toolkit-Events laufen
    /// auf dem UI-Thread. <c>MediaOpened</c>/<c>StateChanged</c> können
    /// unter Windows auf Threadpool-Threads feuern; ein direkter
    /// <c>SeekTo</c>/Positionszugriff dort löst die beobachtete
    /// <c>COMException 0x8001010E</c> (<c>RPC_E_WRONG_THREAD</c>) in
    /// <c>MediaManager.PlatformSeek</c> aus.
    /// </summary>
    [Fact]
    public void PlayerPage_MarshalsNativeElementAccessOnMainThread()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "PlayerPage.xaml.cs");

        Assert.Contains("BeginInvokeOnMainThread",
            MethodBody(code, "private void OnMediaOpened"), StringComparison.Ordinal);
        Assert.Contains("BeginInvokeOnMainThread",
            MethodBody(code, "private void OnMediaStateChanged"), StringComparison.Ordinal);

        // Der einzige SeekTo-Aufruf steht innerhalb des MainThread-
        // Marshals in OnMediaOpened — kein direkter Aufruf im
        // Event-Thread.
        var opened = MethodBody(code, "private void OnMediaOpened");
        var marshal = opened.IndexOf("BeginInvokeOnMainThread", StringComparison.Ordinal);
        var seek = opened.IndexOf("SeekTo(", StringComparison.Ordinal);
        Assert.True(seek > marshal, "SeekTo muss innerhalb des MainThread-Marshals stehen.");
        Assert.Equal(1, code.Split("SeekTo(", StringSplitOptions.None).Length - 1);
    }

    /// <summary>
    /// Prüft PlayerPage: eine verlassene Seite reagiert nicht mehr — der
    /// <c>subscribed</c>-Guard sperrt Modell-Updates und Wiedergabe-
    /// Messages verwaister Seiten (ein <c>PlaybackEndedMessage</c> der
    /// alten Seite würde die neue Wiedergabe sonst als gesehen markieren).
    /// </summary>
    [Fact]
    public void PlayerPage_IgnoresModelChangesAfterDisappearing()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "PlayerPage.xaml.cs");

        Assert.Contains("subscribed = false",
            MethodBody(code, "protected override void OnDisappearing"), StringComparison.Ordinal);
        Assert.Contains("subscribed",
            MethodBody(code, "private void OnModelChanged"), StringComparison.Ordinal);
        Assert.Contains("subscribed",
            MethodBody(code, "private void OnMediaOpened"), StringComparison.Ordinal);
        Assert.Contains("subscribed",
            MethodBody(code, "private void OnMediaEnded"), StringComparison.Ordinal);
        Assert.Contains("subscribed",
            MethodBody(code, "private void OnMediaFailed"), StringComparison.Ordinal);
        Assert.Contains("subscribed",
            MethodBody(code, "private void OnMediaStateChanged"), StringComparison.Ordinal);
    }

    /// <summary>Extrahiert den Rumpf einer Methode ab der ersten öffnenden Klammer.</summary>
    /// <param name="code">Quelltext der Datei.</param>
    /// <param name="signature">Eindeutige Signaturmarke der Methode.</param>
    /// <returns>Methodenrumpf inklusive Klammern.</returns>
    private static string MethodBody(string code, string signature)
    {
        var start = code.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Methode '{signature}' nicht gefunden.");
        var open = code.IndexOf('{', start);
        Assert.True(open >= 0, $"Rumpf von '{signature}' nicht gefunden.");
        var depth = 0;
        for (var i = open; i < code.Length; i++)
        {
            if (code[i] == '{')
            {
                depth++;
            }
            else if (code[i] == '}' && --depth == 0)
            {
                return code.Substring(open, i - open + 1);
            }
        }

        return code[open..];
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
