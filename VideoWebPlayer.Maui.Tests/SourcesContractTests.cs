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

    /// <summary>Prüft AppShell: baut die Quellen-Menüeinträge dynamisch aus dem Model auf.</summary>
    [Fact]
    public void AppShell_BuildsSourceMenuItems_FromModel()
    {
        var code = Read("VideoPlayer.Maui", "AppShell.xaml.cs");

        // ModelChanged-Abonnement, Quellenstand aus dem Model, Einträge
        // als MenuItem mit eigenem Sektions-Template für die Überschrift
        // „Quellen", positionsabhängiges Einfügen/Entfernen, Dispatch der
        // Auswahl-Message und Icon-Auflösung über den PosterImageService.
        Assert.Contains("ModelChanged", code, StringComparison.Ordinal);
        Assert.Contains("SourcesOverview", code, StringComparison.Ordinal);
        Assert.Contains("MenuItem", code, StringComparison.Ordinal);
        Assert.Contains("SetMenuItemTemplate", code, StringComparison.Ordinal);
        Assert.Contains("Items.Insert", code, StringComparison.Ordinal);
        Assert.Contains("Items.Remove", code, StringComparison.Ordinal);
        Assert.Contains("SourceSelectedMessage", code, StringComparison.Ordinal);
        Assert.Contains("GetSourceIconSourceAsync", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft AppShell: der gerenderte Stand wird erst nach erfolgreichem Aufbau markiert und der UI-Callback ist abgesichert.</summary>
    [Fact]
    public void AppShell_MarksRenderedStateOnlyAfterSuccessfulBuild()
    {
        var code = Read("VideoPlayer.Maui", "AppShell.xaml.cs");

        // Der Main-Thread-Callback liest das Model zum Ausführungszeitpunkt
        // (kein Snapshot-Capture im Event-Handler) und fängt Ausnahmen —
        // renderedSources darf erst nach InsertSourceMenuItems gesetzt
        // werden, sonst pinnt ein halb aufgebautes Menü.
        Assert.Contains("BeginInvokeOnMainThread(UpdateSourceMenuItemsSafe)", code, StringComparison.Ordinal);
        Assert.Contains("UpdateSourceMenuItems(mvuHost.Model)", code, StringComparison.Ordinal);
        var update = code[(code.IndexOf("private void UpdateSourceMenuItems(AppModel model)", StringComparison.Ordinal))..];
        var insert = update.IndexOf("InsertSourceMenuItems(overview", StringComparison.Ordinal);
        var mark = update.IndexOf("renderedSources = overview.Sources", StringComparison.Ordinal);
        Assert.True(insert > 0 && mark > insert);
    }

    /// <summary>Prüft AppShell: die Quellenauswahl schließt das Flyout — MenuItem-Aktivierungen tun das nicht selbst.</summary>
    [Fact]
    public void AppShell_ClosesFlyout_OnSourceSelection()
    {
        var code = Read("VideoPlayer.Maui", "AppShell.xaml.cs");

        // Shell.OnFlyoutItemSelectedAsync returnt für MenuShellItem vor
        // dem FlyoutIsPresented=false — ohne explizites Schließen bliebe
        // das Menü über der geschobenen Übersichtsseite geöffnet. Der
        // Retry-Eintrag schließt bewusst nicht: ohne Seitenwechsel ist
        // die Zeilenaktualisierung die Bestätigung — daher genau ein
        // Vorkommen (Quellen-Eintrag).
        Assert.Contains("FlyoutIsPresented = false", code, StringComparison.Ordinal);
        Assert.Equal(1, code.Split("FlyoutIsPresented").Length - 1);
    }

    /// <summary>Prüft AppShell: Lade- und Leerstand bekommen eine nicht-klickbare Statuszeile.</summary>
    [Fact]
    public void AppShell_ShowsStatusRows_ForLoadingAndEmpty()
    {
        var code = Read("VideoPlayer.Maui", "AppShell.xaml.cs");

        Assert.Contains("Quellen werden geladen", code, StringComparison.Ordinal);
        Assert.Contains("Keine Quellen freigegeben", code, StringComparison.Ordinal);
        Assert.Contains("overview.IsLoading", code, StringComparison.Ordinal);
        Assert.Contains("overview.ShowEmpty", code, StringComparison.Ordinal);
        Assert.Contains("sourceInfoTemplate", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft AppShell: bei Ladefehlschlag steht ein klickbarer Neuversuch-Eintrag im Menü.</summary>
    [Fact]
    public void AppShell_ShowsRetryEntry_OnSourcesError()
    {
        var code = Read("VideoPlayer.Maui", "AppShell.xaml.cs");

        Assert.Contains("overview.HasError", code, StringComparison.Ordinal);
        Assert.Contains("erneut versuchen", code, StringComparison.Ordinal);
        Assert.Contains("SourcesReloadRequestedMessage", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft AppShell: Sektions-, Fehler- und Status-Templates für die Quellengruppe sind definiert.</summary>
    [Fact]
    public void AppShell_DefinesSourceSectionAndErrorTemplates()
    {
        var xaml = Read("VideoPlayer.Maui", "AppShell.xaml");

        Assert.Contains("x:Key=\"SourceSectionTemplate\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"SourceErrorTemplate\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"SourceInfoTemplate\"", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft AppShell: enthält keinen statischen Quellen-Eintrag mehr — nur die sechs Menü-FlyoutItems.</summary>
    [Fact]
    public void AppShell_HasNoStaticSourcesFlyoutItem()
    {
        var xaml = Read("VideoPlayer.Maui", "AppShell.xaml");

        Assert.DoesNotContain("Route=\"sources\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Title=\"Quellen\"", xaml, StringComparison.Ordinal);

        var document = System.Xml.Linq.XDocument.Parse(xaml);
        var ns = System.Xml.Linq.XNamespace.Get("http://schemas.microsoft.com/dotnet/2021/maui");
        Assert.Equal(6, document.Root!.Elements(ns + "FlyoutItem").Count());
    }

    /// <summary>Prüft MauiProgram/Shell: der PosterImageService ist für die Icon-Auflösung des Menüs verdrahtet.</summary>
    [Fact]
    public void MauiProgram_RegistersPosterImageServiceForShell()
    {
        var program = Read("VideoPlayer.Maui", "MauiProgram.cs");
        var shell = Read("VideoPlayer.Maui", "AppShell.xaml.cs");

        Assert.Contains("AddSingleton<PosterImageService>", program, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<AppShell>", program, StringComparison.Ordinal);
        Assert.DoesNotContain("SourcesPage", program, StringComparison.Ordinal);
        Assert.Contains("PosterImageService", shell, StringComparison.Ordinal);
    }

    /// <summary>Prüft SourceOverviewPage: hat Genre-Bar, Suche und ein inkrementelles Raster.</summary>
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

    /// <summary>Prüft SourceOverviewPage: dispatcht Browsernachrichten.</summary>
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

    /// <summary>Prüft MediaDetailsPage: hat Staffel-Picker, Episoden, Collection- und Favoriten-Eintrag.</summary>
    [Fact]
    public void MediaDetailsPage_HasSeasonPicker_Episodes_Collection_AndFavorite()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "MediaDetailsPage.xaml");

        // Serienansicht (Staffel-Picker + Episodenliste), Kollektionsansicht
        // und der Favoriten-Schalter.
        Assert.Contains("SeasonPicker", xaml, StringComparison.Ordinal);
        Assert.Contains("EpisodesList", xaml, StringComparison.Ordinal);
        Assert.Contains("CollectionCollection", xaml, StringComparison.Ordinal);
        Assert.Contains("FavoriteButton", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft MediaDetailsPage: dispatcht Detailsnachrichten.</summary>
    [Fact]
    public void MediaDetailsPage_DispatchesDetailsMessages()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "MediaDetailsPage.xaml.cs");

        Assert.Contains("MediaDetailsRequestedMessage", code, StringComparison.Ordinal);
        Assert.Contains("SeasonSelectedMessage", code, StringComparison.Ordinal);
        Assert.Contains("FavoriteToggleRequestedMessage", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft Shell: bei <c>RegistersSourceOverviewRoute</c> → registriert die Route mit Query-Parameter.</summary>
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

    /// <summary>Prüft EffectHandler: bei <c>HandlesStep8Effects</c> → aktualisiert die Favoriten-Caches.</summary>
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

    /// <summary>Prüft EffectHandler: die Quellen-Navigation schiebt keine identische Seite, wenn die Quelle bereits oben liegt.</summary>
    [Fact]
    public void EffectHandler_SkipsPush_WhenSameSourceOverviewIsTopmost()
    {
        var handler = Read("VideoPlayer.Maui", "Services", "ShellAppEffectHandler.cs");

        // Ein erneuter Tap auf die bereits geöffnete Quelle darf keine
        // identische Seite auf den Stack legen: vor dem GoToAsync wird
        // die oberste Shell-Route (CurrentState.Location) auf
        // „sourceoverview" mit gleicher id geprüft.
        var start = handler.IndexOf("private async Task NavigateToSourceOverviewAsync", StringComparison.Ordinal);
        var end = handler.IndexOf("private async Task LoadSourcesAsync", StringComparison.Ordinal);
        Assert.True(start > 0 && end > start);
        var method = handler[start..end];

        Assert.Contains("sourceoverview", method, StringComparison.Ordinal);
        Assert.Contains("\"id\"", method, StringComparison.Ordinal);
        // Die Dedup-Prüfung liest die oberste Shell-Route und steht vor
        // dem Push — der Helper folgt der Methode, der Aufruf muss vor
        // GoToAsync liegen.
        Assert.Contains("CurrentState", method, StringComparison.Ordinal);
        var guard = method.IndexOf("IsTopmostSourceOverview", StringComparison.Ordinal);
        var push = method.IndexOf("GoToAsync", StringComparison.Ordinal);
        Assert.True(guard > 0 && push > guard);
    }

    /// <summary>Prüft MauiProgram: registriert die Step-8-Services und Seiten.</summary>
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
