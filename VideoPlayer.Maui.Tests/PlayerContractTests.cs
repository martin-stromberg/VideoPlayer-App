using System.Xml.Linq;
using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

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
    private static readonly XNamespace MauiNs =
        XNamespace.Get("http://schemas.microsoft.com/dotnet/2021/maui");
    private static readonly XNamespace XNs =
        XNamespace.Get("http://schemas.microsoft.com/winfx/2009/xaml");

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));

    /// <summary>Liest das XAML der PlayerPage als Dokument.</summary>
    /// <returns>Das geparste PlayerPage-XAML.</returns>
    private static XDocument PlayerPageXaml() =>
        XDocument.Parse(Read("VideoPlayer.Maui", "Pages", "PlayerPage.xaml"));

    /// <summary>Liest den Code-Behind der PlayerPage.</summary>
    /// <returns>Quelltext der PlayerPage.xaml.cs.</returns>
    private static string PlayerPageCode() =>
        Read("VideoPlayer.Maui", "Pages", "PlayerPage.xaml.cs");

    /// <summary>Findet das eindeutig benannte XAML-Element mit dem gegebenen <c>x:Name</c>.</summary>
    /// <param name="parent">Elternknoten, unterhalb dessen gesucht wird.</param>
    /// <param name="element">Elementname (inkl. MAUI-Namespace).</param>
    /// <param name="name">Der gesuchte <c>x:Name</c>.</param>
    /// <returns>Das gefundene Element oder <see langword="null"/>.</returns>
    private static XElement? Named(XContainer parent, XName element, string name) =>
        parent.Descendants(element)
            .SingleOrDefault(e => (string?)e.Attribute(XNs + "Name") == name);

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
        var code = PlayerPageCode();

        Assert.Contains("PlayerPageOpenedMessage", code, StringComparison.Ordinal);
        Assert.Contains("PlaybackProgressMessage", code, StringComparison.Ordinal);
        // Der Fortschrittstakt bindet die gemeldeten Werte an die zuletzt
        // angewendete Medienidentität — nachlaufende Takte des
        // Vorgängermediums werden im MVU-Kern verworfen.
        Assert.Contains("appliedMediaId", code, StringComparison.Ordinal);
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
    /// Prüft PlayerPage: die Titelleiste „TitleChrome" liegt nicht mehr als
    /// Overlay auf der Videofläche, sondern in einer eigenen Zeile am oberen
    /// Fensterrand — das PlayerRoot-Grid trägt die Zeilen „Auto,*", die
    /// Leiste steht in Zeile 0 und die Videofläche („VideoSurface" mit dem
    /// MediaElement) in Zeile 1, deren oberer Rand an den unteren Rand der
    /// Leiste grenzt (Issue #75). Mit dem Wegfall der Überlagerung ist die
    /// Leiste ein reguläres Layout — das frühere
    /// VerticalOptions-/InputTransparent-Overlay-Handling (Issue #45) ist
    /// entfernt.
    /// </summary>
    [Fact]
    public void PlayerPage_TitleChromeSitsInOwnRowAbovePlaybackSurface()
    {
        var document = PlayerPageXaml();

        var rootGrid = Named(document, MauiNs + "Grid", "PlayerRoot");
        Assert.NotNull(rootGrid);
        Assert.Equal("Auto,*", (string?)rootGrid.Attribute("RowDefinitions"));

        // Titelleiste als reguläre Zeile 0 — kein Overlay mehr über dem
        // Video: kein VerticalOptions-/InputTransparent-Overlay-Handling
        // und kein Margin-Versatz.
        var title = Named(rootGrid, MauiNs + "VerticalStackLayout", "TitleChrome");
        Assert.NotNull(title);
        Assert.Equal("0", (string?)title.Attribute("Grid.Row"));
        Assert.Null(title.Attribute("VerticalOptions"));
        Assert.Null(title.Attribute("InputTransparent"));
        Assert.Null(title.Attribute("Margin"));

        var band = title.Element(MauiNs + "Border");
        Assert.NotNull(band);
        Assert.Null(band.Attribute("InputTransparent"));

        // Videofläche als eigene Zeile 1 — das MediaElement und die
        // schwebenden Elemente (Mini-Player-Schalter, Fehlerhinweis)
        // liegen in ihr; die Playlist-Navigation sitzt seit Issue #78 in
        // der Titelleiste.
        var surface = Named(rootGrid, MauiNs + "Grid", "VideoSurface");
        Assert.NotNull(surface);
        Assert.Equal("1", (string?)surface.Attribute("Grid.Row"));
        Assert.Contains(surface.Descendants(), e => e.Name.LocalName == "MediaElement");

        var pipButton = Named(surface, MauiNs + "Button", "PipButton");
        Assert.NotNull(pipButton);
        Assert.Equal("OnPipClicked", (string?)pipButton.Attribute("Clicked"));

        // Das MediaElement liegt nicht mehr direkt in PlayerRoot.
        Assert.DoesNotContain(rootGrid.Elements(), e => e.Name.LocalName == "MediaElement");
    }

    /// <summary>
    /// Prüft PlayerPage: die Playlist-Navigation liegt rechtsbündig in der
    /// Titelleiste (Issue #78) — die Bar ist ein Element der
    /// <c>TitleChrome</c>-Zeile in der letzten Spalte des Kopf-Grids und
    /// überlappt die Videofläche nicht mehr; der frühere iOS-Versatz unter
    /// die native Top-Leiste des AVPlayerViewController
    /// (<c>IosNativeControlsClearance</c>) ist damit entfallen. Die
    /// Schaltflächen sind Symbol-Schaltflächen im <c>IconButton</c>-Stil
    /// mit Material-Icons-Glyphen (skip_previous/skip_next); Namen,
    /// AutomationIds und Click-Handler bleiben für die MVU-Verdrahtung
    /// und die E2E-Ansteuerung unverändert.
    /// </summary>
    [Fact]
    public void PlayerPage_PlaylistNavigationSitsRightAlignedInTitleBar()
    {
        var code = PlayerPageCode();
        var document = PlayerPageXaml();

        var rootGrid = Named(document, MauiNs + "Grid", "PlayerRoot");
        Assert.NotNull(rootGrid);
        var title = Named(rootGrid, MauiNs + "VerticalStackLayout", "TitleChrome");
        Assert.NotNull(title);
        var surface = Named(rootGrid, MauiNs + "Grid", "VideoSurface");
        Assert.NotNull(surface);

        // Rechtsbündig in der Titelleiste: letzte (Auto-)Spalte des
        // Kopf-Grids, kein Overlay der Videofläche mehr.
        var navBar = Named(title, MauiNs + "HorizontalStackLayout", "PlaylistNavigationBar");
        Assert.NotNull(navBar);
        Assert.Equal("2", (string?)navBar.Attribute("Grid.Column"));
        Assert.Null(navBar.Attribute("Margin"));
        Assert.Null(navBar.Attribute("InputTransparent"));
        Assert.DoesNotContain(surface.Descendants(),
            e => (string?)e.Attribute(XNs + "Name") == "PlaylistNavigationBar");

        // Symbol-Schaltflächen statt Text: IconButton-Stil,
        // Material-Icons-Glyphen (skip_previous U+E045, skip_next U+E044).
        var previous = Named(navBar, MauiNs + "Button", "PreviousButton");
        var next = Named(navBar, MauiNs + "Button", "NextButton");
        Assert.NotNull(previous);
        Assert.NotNull(next);
        Assert.Equal("{StaticResource IconButton}", (string?)previous.Attribute("Style"));
        Assert.Equal("{StaticResource IconButton}", (string?)next.Attribute("Style"));
        Assert.Equal("MaterialIcons", (string?)previous.Attribute("FontFamily"));
        Assert.Equal("MaterialIcons", (string?)next.Attribute("FontFamily"));
        Assert.Equal("\uE045", (string?)previous.Attribute("Text"));
        Assert.Equal("\uE044", (string?)next.Attribute("Text"));

        // Verdrahtung unverändert: AutomationIds (E2E/DevApi) und
        // Click-Handler auf die Playlist-Navigations-Messages.
        Assert.Equal("PlayerPreviousButton", (string?)previous.Attribute("AutomationId"));
        Assert.Equal("PlayerNextButton", (string?)next.Attribute("AutomationId"));
        Assert.Equal("OnPreviousClicked", (string?)previous.Attribute("Clicked"));
        Assert.Equal("OnNextClicked", (string?)next.Attribute("Clicked"));
        Assert.Contains("PlaylistPreviousRequestedMessage", code, StringComparison.Ordinal);
        Assert.Contains("PlaylistNextRequestedMessage", code, StringComparison.Ordinal);

        // Die Bar steht der nativen iOS-Top-Leiste nicht mehr im Weg —
        // der Versatzblock und seine Konstante sind entfernt.
        var ctor = MethodBody(code, "public PlayerPage(");
        Assert.DoesNotContain("#if IOS", ctor, StringComparison.Ordinal);
        Assert.DoesNotContain("PlaylistNavigationBar.Margin", ctor, StringComparison.Ordinal);
        Assert.DoesNotContain("IosNativeControlsClearance", code, StringComparison.Ordinal);
        Assert.DoesNotContain("new Thickness(0, 96, 16, 0)", code, StringComparison.Ordinal);
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
        var code = PlayerPageCode();
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
        var code = PlayerPageCode();

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
    /// <c>MediaManager.PlatformSeek</c> aus. Der einzige Seek-Aufruf
    /// steht in <c>ApplyResumeSeekAsync</c> und ist ausschließlich über
    /// das UI-Thread-Gate <c>TryApplyPendingResumeSeek</c> erreichbar.
    /// </summary>
    [Fact]
    public void PlayerPage_MarshalsNativeElementAccessOnMainThread()
    {
        var code = PlayerPageCode();

        Assert.Contains("BeginInvokeOnMainThread",
            MethodBody(code, "private void OnMediaOpened"), StringComparison.Ordinal);
        Assert.Contains("BeginInvokeOnMainThread",
            MethodBody(code, "private void OnMediaStateChanged"), StringComparison.Ordinal);

        // Genau ein SeekTo-Aufruf im gesamten Code-Behind — in
        // ApplyResumeSeekAsync, das nur über das UI-Thread-Gate
        // TryApplyPendingResumeSeek angestoßen wird.
        Assert.Equal(1, code.Split("SeekTo(", StringSplitOptions.None).Length - 1);
        Assert.Contains("SeekTo(",
            MethodBody(code, "private async Task ApplyResumeSeekAsync"), StringComparison.Ordinal);
        Assert.Contains("TryApplyPendingResumeSeek",
            MethodBody(code, "private void OnMediaOpened"), StringComparison.Ordinal);
        Assert.Contains("TryApplyPendingResumeSeek",
            MethodBody(code, "private void OnMediaStateChanged"), StringComparison.Ordinal);
        Assert.DoesNotContain("ApplyResumeSeekAsync",
            MethodBody(code, "private void OnMediaOpened"), StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft PlayerPage: der Fortsetzungs-Seek wartet die
    /// Wiedergabebereitschaft ab — <c>OnMediaOpened</c> merkt die
    /// Position nur noch, weil das Toolkit auf iOS <c>MediaOpened</c>
    /// vor <c>AVPlayerItemStatus.ReadyToPlay</c> feuert (Issue #3249)
    /// und ein Seek in diesem Fenster im async-void-Handler des
    /// Toolkits eine unbehandelte Ausnahme auslöst (SIGABRT).
    /// </summary>
    [Fact]
    public void PlayerPage_ResumeSeek_WaitsForReadyState()
    {
        var code = PlayerPageCode();

        // Kein direkter Seek im MediaOpened-Handler — nur das Merken
        // des Seek-Wunsches und der Gate-Aufruf.
        var opened = MethodBody(code, "private void OnMediaOpened");
        Assert.DoesNotContain("SeekTo(", opened, StringComparison.Ordinal);
        Assert.Contains("pendingResumePosition", opened, StringComparison.Ordinal);

        // Das Gate prüft den Bereitschaftszustand und wendet den
        // gemerkten Wunsch genau einmal an.
        var gate = MethodBody(code, "private void TryApplyPendingResumeSeek");
        Assert.Contains("MediaElementState.Playing", gate, StringComparison.Ordinal);
        Assert.Contains("MediaElementState.Paused", gate, StringComparison.Ordinal);
        Assert.Contains("pendingResumePosition = null", gate, StringComparison.Ordinal);
        Assert.Contains("ApplyResumeSeekAsync", gate, StringComparison.Ordinal);

        // Der Seek selbst ist zeitlich begrenzt, damit eine
        // ausbleibende Toolkit-Vollendung nicht ewig hängt.
        var seek = MethodBody(code, "private async Task ApplyResumeSeekAsync");
        Assert.Contains("CancellationTokenSource", seek, StringComparison.Ordinal);
        Assert.Contains("ResumeSeekTimeout", seek, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft PlayerPage: alle nativen Elementzugriffe sind fehlerisoliert
    /// — eine durchschlagende Ausnahme am <c>MediaElement</c> landet als
    /// <c>PlaybackFailedMessage</c> (Fehlerpanel) statt als Prozessabbruch.
    /// Ausnahme: der Fortsetzungs-Seek degradiert nicht-fatal (siehe
    /// <c>PlayerPage_ResumeSeekFailure_IsNonFatal</c>).
    /// </summary>
    [Fact]
    public void PlayerPage_NativeAccessesAreFailureIsolated()
    {
        var code = PlayerPageCode();

        var updateUi = MethodBody(code, "private void UpdateUi");
        Assert.Contains("try", updateUi, StringComparison.Ordinal);
        Assert.Contains("catch", updateUi, StringComparison.Ordinal);
        // Fehlerpfad → PlaybackFailedMessage (FailPlaybackStart meldet
        // den Fehler und setzt sourceApplyFailed für den Neuversuch).
        Assert.Contains("FailPlaybackStart", updateUi, StringComparison.Ordinal);
        Assert.Contains("PlaybackFailedMessage",
            MethodBody(code, "private void FailPlaybackStart"), StringComparison.Ordinal);
        // Quell-Adresse wird validiert, bevor eine MediaSource gebaut wird.
        Assert.Contains("IsValidSourceLocation", updateUi, StringComparison.Ordinal);
        Assert.Contains("Uri.TryCreate", code, StringComparison.Ordinal);

        var seek = MethodBody(code, "private async Task ApplyResumeSeekAsync");
        Assert.Contains("try", seek, StringComparison.Ordinal);
        Assert.Contains("catch", seek, StringComparison.Ordinal);
        // Der Fortsetzungs-Seek-Fehlschlag ist nicht-fatal (stille
        // Degradation) — kein PlaybackFailedMessage-Dispatch mehr;
        // gepinnt in PlayerPage_ResumeSeekFailure_IsNonFatal.
        Assert.DoesNotContain("PlaybackFailedMessage", seek, StringComparison.Ordinal);

        var report = MethodBody(code, "private void ReportPosition");
        Assert.Contains("try", report, StringComparison.Ordinal);
        Assert.Contains("catch", report, StringComparison.Ordinal);

        // Reine Lesezugriffe bleiben ebenfalls isoliert: der Timer-
        // Callback und das Seek-Gate laufen aus Dispatcher-/Marshal-
        // Kontexten ohne eigene Ausnahmebehandlung — ein Wurf des
        // nativen Elements wäre dort sonst unbehandelt (Prozessabbruch).
        var timer = MethodBody(code, "private void StartProgressTimer");
        Assert.Contains("CurrentState", timer, StringComparison.Ordinal);
        Assert.Contains("try", timer, StringComparison.Ordinal);
        Assert.Contains("catch", timer, StringComparison.Ordinal);

        var gate = MethodBody(code, "private void TryApplyPendingResumeSeek");
        Assert.Contains("CurrentState", gate, StringComparison.Ordinal);
        Assert.Contains("try", gate, StringComparison.Ordinal);
        Assert.Contains("catch", gate, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft PlayerPage: ein fehlschlagender oder zeitüberschreitender
    /// Fortsetzungs-Seek ist nicht-fatal — <c>ApplyResumeSeekAsync</c>
    /// protokolliert den Fehlschlag nur noch (Warn-Log) und dispatcht
    /// kein <c>PlaybackFailedMessage</c>: die laufende Wiedergabe läuft
    /// an der vom Player erreichten Position weiter, statt über
    /// <c>UpdatePlaybackFailed</c> den Episoden-/Playlist-Rückfall
    /// auszulösen. Der Rückfall bleibt den fatalen Pfaden
    /// <c>FailPlaybackStart</c> (ungültige/nicht setzbare Quelle) und
    /// <c>OnMediaFailed</c> vorbehalten — beide dispatchen die Meldung
    /// weiterhin.
    /// </summary>
    [Fact]
    public void PlayerPage_ResumeSeekFailure_IsNonFatal()
    {
        var code = PlayerPageCode();

        var seek = MethodBody(code, "private async Task ApplyResumeSeekAsync");
        Assert.Contains("try", seek, StringComparison.Ordinal);
        Assert.Contains("catch", seek, StringComparison.Ordinal);
        Assert.Contains("LogWarning", seek, StringComparison.Ordinal);
        Assert.Contains("Fortsetzungs-Seek", seek, StringComparison.Ordinal);
        Assert.DoesNotContain("PlaybackFailedMessage", seek, StringComparison.Ordinal);

        Assert.Contains("PlaybackFailedMessage",
            MethodBody(code, "private void FailPlaybackStart"), StringComparison.Ordinal);
        Assert.Contains("PlaybackFailedMessage",
            MethodBody(code, "private void OnMediaFailed"), StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft PlayerPage: ein fehlgeschlagener Quellenwechsel darf den
    /// Stop-/Leerpfad des nächsten <c>UpdateUi</c>-Laufs nicht
    /// überspringen. <c>FailPlaybackStart</c> markiert den Fehlschlag
    /// über ein Invalidierungs-Flag statt <c>appliedLocation</c> auf
    /// <c>null</c> zu setzen — sonst träfe ein geleertes
    /// Modell-<c>Source</c> auf <c>null == null</c>, der Frühreturn
    /// griffe und <c>VideoElement.Stop()</c>/<c>Source = null</c>
    /// würden nie laufen (eine zuvor gesetzte Quelle spielt weiter).
    /// </summary>
    [Fact]
    public void PlayerPage_FailedSourceApply_KeepsStopPathReachable()
    {
        var code = PlayerPageCode();

        var fail = MethodBody(code, "private void FailPlaybackStart");
        Assert.DoesNotContain("appliedLocation = null", fail, StringComparison.Ordinal);
        Assert.Contains("sourceApplyFailed = true", fail, StringComparison.Ordinal);

        var updateUi = MethodBody(code, "private void UpdateUi");
        Assert.Contains("sourceApplyFailed", updateUi, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft PlayerPage: <c>appliedMediaId</c> darf erst nach dem
    /// erfolgreichen Quellenwechsel am MediaElement umgeschwenkt werden.
    /// Schlägt <c>VideoElement.Source = …</c> fehl, spielt das Element das
    /// Altmedium weiter — ein dann noch laufender Fortschrittstakt muss
    /// die alte Id melden, damit der Identitäts-Guard des MVU-Kerns die
    /// Alt-Takte verwirft (statt Alt-Positionen der neuen MediaId
    /// anzurechnen). Regression: die Zuweisung stand optimistisch vor
    /// dem Quellenwechsel — Fehlschlag-Pfade meldeten Alt-Positionen
    /// bereits unter der neuen Id.
    /// </summary>
    [Fact]
    public void PlayerPage_AppliedMediaId_AssignedAfterSourceSet()
    {
        var code = PlayerPageCode();
        var updateUi = MethodBody(code, "private void UpdateUi");

        var sourceSet = updateUi.LastIndexOf("VideoElement.Source =", StringComparison.Ordinal);
        var idAssign = updateUi.IndexOf("appliedMediaId = player.MediaId", StringComparison.Ordinal);
        Assert.True(sourceSet >= 0, "Quellenzuweisung am MediaElement nicht gefunden.");
        Assert.True(
            idAssign > sourceSet,
            "appliedMediaId muss erst nach dem erfolgreichen Setzen der Quelle zugewiesen werden.");
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
        var code = PlayerPageCode();

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
