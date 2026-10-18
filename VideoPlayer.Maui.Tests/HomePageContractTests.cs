using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

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

    /// <summary>Prüft HomePage: enthält den Hero-Kopfbereich nach Web-Vorbild oberhalb der Listen.</summary>
    [Fact]
    public void HomePage_ContainsHeroSection_AboveLists()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "HomePage.xaml");

        // Der Hero steht vor den Medienlisten (Reihenfolge im Markup).
        var heroIndex = xaml.IndexOf("HeroSection", StringComparison.Ordinal);
        var carouselIndex = xaml.IndexOf("MediaCarousel", StringComparison.Ordinal);
        Assert.True(heroIndex >= 0);
        Assert.True(carouselIndex > heroIndex);

        // Web-Markup: Kicker-Chip, Titel, Beschreibung; Sichtbarkeit über
        // die positiv formulierte Einstellung im MVU-Model.
        Assert.Contains("PRIVATE MEDIA LIBRARY", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding Model.HeroTitle}", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding Model.ShowHomeHero}\"", xaml, StringComparison.Ordinal);
        // Cover-Darstellung und geschichtete Gradienten.
        Assert.Contains("AspectFill", xaml, StringComparison.Ordinal);
        Assert.Contains("LinearGradientBrush", xaml, StringComparison.Ordinal);
        Assert.Contains("RadialGradientBrush", xaml, StringComparison.Ordinal);
        Assert.Contains("VpHeroSideStrong", xaml, StringComparison.Ordinal);
        // Trennlinie und Bild ohne dauerhafte Ablage.
        Assert.Contains("VpBorder", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("WriteAllBytes", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft HomePage-Code: wandelt die Hero-Bytes in eine ImageSource um und koppelt die Höhe an die Breite.</summary>
    [Fact]
    public void HomePage_CodeBehind_UpdatesHeroImageAndResponsiveHeight()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "HomePage.xaml.cs");

        // Flüchtige Umwandlung byte[] → ImageSource (keine Dateiablage).
        Assert.Contains("ImageSource.FromStream", code, StringComparison.Ordinal);
        Assert.Contains("model.HomeHero.ImageBytes", code, StringComparison.Ordinal);
        Assert.DoesNotContain("WriteAllBytes", code, StringComparison.Ordinal);
        // clamp-Äquivalent der Web-Höhe (260px … 42vw … 520px).
        Assert.Contains("Math.Clamp(Width * 0.42, 260, 520)", code, StringComparison.Ordinal);
        Assert.Contains("SizeChanged", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft ShellAppEffectHandler: führt den Hero-Ladeeffekt über den angemeldeten Client aus.</summary>
    [Fact]
    public void ShellAppEffectHandler_HandlesHeroLoadEffect()
    {
        var handler = Read("VideoPlayer.Maui", "Services", "ShellAppEffectHandler.cs");

        Assert.Contains("LoadHeroBackgroundEffect", handler, StringComparison.Ordinal);
        Assert.Contains("GetHeroBackgroundAsync", handler, StringComparison.Ordinal);
        Assert.Contains("HeroBackgroundLoadedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("authSession.Client", handler, StringComparison.Ordinal);
    }

    /// <summary>Prüft VideoPlayerApiClient: ruft den Hero-Endpunkt mit der gemeinsamen Sende-Pipeline ab.</summary>
    [Fact]
    public void ApiClient_HeroEndpoint_UsesSendPipeline()
    {
        var client = Read("VideoPlayer.ApiClient", "VideoPlayerApiClient.cs");

        Assert.Contains("GetHeroBackgroundAsync", client, StringComparison.Ordinal);
        Assert.Contains("api/pictures/hero-background", client, StringComparison.Ordinal);
    }

    /// <summary>Prüft SettingsPage: enthält den positiv formulierten Schalter für den Kopfbereich.</summary>
    [Fact]
    public void SettingsPage_ContainsHeroToggle_PositiveWording()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "SettingsPage.xaml");
        var code = Read("VideoPlayer.Maui", "Pages", "SettingsPage.xaml.cs");

        // Positiv formuliert: „Kopfbereich anzeigen" (kein „ausblenden").
        Assert.Contains("Kopfbereich anzeigen", xaml, StringComparison.Ordinal);
        Assert.Contains("HomeHeroSwitch", xaml, StringComparison.Ordinal);
        Assert.Contains("HomeHeroEnabled", code, StringComparison.Ordinal);
        Assert.Contains("OnHomeHeroToggled", code, StringComparison.Ordinal);
        Assert.Contains("settings.HomeHeroEnabled", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft Colors: definiert die Hero-Gradienten als transluzente Ableitungen der Design-Tokens.</summary>
    [Fact]
    public void Colors_DefinesHeroGradientTokens()
    {
        var colors = Read("VideoPlayer.Maui", "Resources", "Styles", "Colors.xaml");

        Assert.Contains("VpHeroSideStrong", colors, StringComparison.Ordinal);
        Assert.Contains("VpHeroAccentPrimary", colors, StringComparison.Ordinal);
        Assert.Contains("VpHeroAccentSecondary", colors, StringComparison.Ordinal);
        // Die Tokenfarbe selbst bleibt unverändert die Web-Farbe.
        Assert.Contains("#e50914", colors, StringComparison.OrdinalIgnoreCase);
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

    /// <summary>Prüft HomePage: die Weiterschauen-Karten folgen dem Querformat
    /// der Vorschaubilder (16:9-Stills), Poster-Listen bleiben im Hochformat —
    /// die Kartendimensionen sind Bindable Properties des Carousels.</summary>
    [Fact]
    public void HomePage_ContinueWatching_UsesLandscapeCards()
    {
        var page = Read("VideoPlayer.Maui", "Pages", "HomePage.xaml");
        var code = Read("VideoPlayer.Maui", "Controls", "MediaCarousel.xaml.cs");

        // 256×144 = exakt 16:9.
        Assert.Contains("ItemWidth=\"256\"", page, StringComparison.Ordinal);
        Assert.Contains("ItemHeight=\"144\"", page, StringComparison.Ordinal);
        Assert.Contains("Model.ContinueWatching", page, StringComparison.Ordinal);
        // Dimensionen als bindbare Eigenschaften (Default = Porträt-Poster).
        Assert.Contains("ItemWidthProperty", code, StringComparison.Ordinal);
        Assert.Contains("ItemHeightProperty", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft HomePage: die Auswahl dispatcht eine MediaItemSelected-Nachricht.</summary>
    [Fact]
    public void HomePage_SelectionDispatchesMediaItemSelected()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "HomePage.xaml.cs");

        Assert.Contains("MediaItemSelectedMessage", code, StringComparison.Ordinal);
        Assert.Contains("DispatchAsync", code, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft HomePage: das Weiterschauen-Karussell hat einen eigenen
    /// Tap-Handler mit eigener Nachricht (Direktstart) und eine
    /// AutomationId für den E2E-Zugriff — die übrigen Listen bleiben
    /// beim Detail-Navigationspfad.
    /// </summary>
    [Fact]
    public void HomePage_ContinueWatchingCarousel_DispatchesOwnMessage()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "HomePage.xaml");
        var code = Read("VideoPlayer.Maui", "Pages", "HomePage.xaml.cs");

        Assert.Contains("AutomationId=\"ContinueWatchingCarousel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ItemTapped=\"OnContinueWatchingItemTapped\"", xaml, StringComparison.Ordinal);
        // Favoriten und „Neu im Programm" bleiben beim bestehenden Handler.
        var continueWatchingIndex = xaml.IndexOf("OnContinueWatchingItemTapped", StringComparison.Ordinal);
        var favoritesIndex = xaml.IndexOf("{Binding Model.Favorites}", StringComparison.Ordinal);
        var recentIndex = xaml.IndexOf("{Binding Model.RecentEntries}", StringComparison.Ordinal);
        Assert.True(favoritesIndex > continueWatchingIndex);
        Assert.True(recentIndex > continueWatchingIndex);
        Assert.Equal(2, xaml.Split("OnMediaItemTapped").Length - 1);

        Assert.Contains("OnContinueWatchingItemTapped", code, StringComparison.Ordinal);
        Assert.Contains("ContinueWatchingItemSelectedMessage", code, StringComparison.Ordinal);
        Assert.Contains("MediaItemSelectedMessage", code, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft AppUpdate: die Weiterschauen-Auswahl verzweigt Direktstart
    /// (abspielbare Typen) und Detail-Fallback über denselben
    /// Startpfad wie <c>PlayRequested</c>.
    /// </summary>
    [Fact]
    public void AppUpdate_ContinueWatchingSelection_BranchesPlaybackAndDetails()
    {
        var update = Read("VideoPlayer.Mvu", "AppUpdate.cs");

        Assert.Contains("ContinueWatchingItemSelectedMessage", update, StringComparison.Ordinal);
        Assert.Contains("UpcomingEpisodesLoadedMessage", update, StringComparison.Ordinal);
        Assert.Contains("LoadUpcomingEpisodesEffect", update, StringComparison.Ordinal);
        // Gemeinsamer Wiedergabe-Start beider Aufrufpfade.
        Assert.Contains("StartPlayback(", update, StringComparison.Ordinal);
        Assert.Contains("NavigateToMediaDetailsEffect", update, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft ShellAppEffectHandler + Dienste: der Serienkontext-Effekt
    /// ist verdrahtet und der Folge-Episoden-Dienst registriert.
    /// </summary>
    [Fact]
    public void ShellAppEffectHandler_HandlesLoadUpcomingEpisodesEffect()
    {
        var handler = Read("VideoPlayer.Maui", "Services", "ShellAppEffectHandler.cs");
        var program = Read("VideoPlayer.Maui", "MauiProgram.cs");

        Assert.Contains("LoadUpcomingEpisodesEffect", handler, StringComparison.Ordinal);
        Assert.Contains("UpcomingEpisodesLoadedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("UpcomingEpisodesService", handler, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<UpcomingEpisodesService>", program, StringComparison.Ordinal);
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
