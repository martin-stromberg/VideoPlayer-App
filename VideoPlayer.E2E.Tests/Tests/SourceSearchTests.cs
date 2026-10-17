using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenarien der Suche im Quellen-Browsing (Issue #49, Schritt 2):
/// eine unbestätigte Sucheingabe im <c>MediaSearchBar</c> übersteht
/// beliebige Modell-Aktualisierungen (kein Live-Suchen), die Return-Taste
/// schickt die Suche mit dem eingegebenen Text ab und lädt die
/// Trefferliste ab der ersten Seite, und der abgeschickte Suchtext bleibt
/// bei Rückkehr auf die Seite als aktiver Filter sichtbar.
/// </summary>
public sealed class SourceSearchTests : E2ETestBase
{
    private const long SourceId = 41;
    private const string SourceName = "E2E-Quelle";
    private const string SearchTerm = "Komöd";

    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public SourceSearchTests(AppFixture fixture) : base(fixture) { }

    /// <summary>
    /// Pflichtszenario: Eine getippte, aber nicht abgeschickte Eingabe
    /// bleibt über Modell-Aktualisierungen (hier: gefilterter Reload per
    /// Genre-Auswahl) erhalten; es wird keine Suche ohne Abschicken
    /// ausgelöst.
    /// </summary>
    [Fact]
    public async Task UnsubmittedInput_SurvivesModelTicks()
    {
        using var server = NewServer();
        using var app = await StartAppAsync();
        await LoginToSourceAsync(app, server);
        await WaitForLoadedAsync(app);

        // Unbestätigte Eingabe ins Suchfeld (setzt TextChanged ab wie echtes Tippen).
        await app.DevApi.SetTextAsync("MediaSearchBar", SearchTerm, Ct);

        // Modell-Ticks auslösen: die Genre-Auswahl lädt die Eintragsliste
        // gefiltert neu (Ladezustands- und Ergebnis-Ticks).
        await app.DevApi.InvokeAsync("GenresCollection", 1, Ct);
        await app.DevApi.WaitForModelAsync(
            m => m.SourceBrowserSelectedGenreId == 4
                && m.SourceBrowserItemTitles != null
                && m.SourceBrowserItemTitles.SequenceEqual(["Komödien-Film"]),
            TimeSpan.FromSeconds(30),
            "Gefilterte Eintragsliste für Genre „Komödie“ (Id 4)", Ct);

        // Die unbestätigte Eingabe hat die Modell-Ticks überstanden.
        Assert.Equal(SearchTerm, await SearchFieldTextAsync(app, SearchTerm));

        // Kein Live-Suchen: ohne Abschicken läuft kein search-Request.
        var model = await app.DevApi.GetModelAsync(Ct);
        Assert.Equal(string.Empty, model.SourceBrowserSearchText);
        lock (server.SourceItemRequests)
        {
            Assert.DoesNotContain(server.SourceItemRequests,
                r => r.Contains("search=", StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Pflichtszenario: Die Return-Taste auf dem Suchfeld schickt die
    /// eingegebene Suche ab — der Server wird mit dem
    /// <c>search</c>-Parameter ab Seite 0 abgefragt und die Trefferliste
    /// zeigt nur die Treffer; der abgeschickte Text bleibt im Feld
    /// sichtbar.
    /// </summary>
    [Fact]
    public async Task SubmitViaEnter_FiltersFromFirstPage()
    {
        using var server = NewServer();
        using var app = await StartAppAsync();
        await LoginToSourceAsync(app, server);
        await WaitForLoadedAsync(app);

        await app.DevApi.SetTextAsync("MediaSearchBar", SearchTerm, Ct);
        app.Ui.PressEnter("MediaSearchBar");

        await app.DevApi.WaitForModelAsync(
            m => m.SourceBrowserSearchText == SearchTerm
                && m.SourceBrowserItemTitles != null
                && m.SourceBrowserItemTitles.SequenceEqual(["Komödien-Film"]),
            TimeSpan.FromSeconds(30),
            "Suche „Komöd“ abgeschickt → gefilterte Trefferliste ab Seite 0", Ct);

        var escaped = Uri.EscapeDataString(SearchTerm);
        lock (server.SourceItemRequests)
        {
            Assert.Contains(server.SourceItemRequests,
                r => r.Contains($"search={escaped}", StringComparison.Ordinal)
                    && r.Contains("page=0", StringComparison.Ordinal));
        }

        // Der aktive Filter bleibt im Suchfeld sichtbar.
        Assert.Equal(SearchTerm, await SearchFieldTextAsync(app, SearchTerm));
    }

    /// <summary>
    /// Der zuletzt abgeschickte Suchtext bleibt bei Rückkehr auf die
    /// Quellen-Übersicht im Feld sichtbar (aktiver Filter erkennbar).
    /// </summary>
    [Fact]
    public async Task SubmittedText_RemainsVisibleAfterReturn()
    {
        using var server = NewServer();
        using var app = await StartAppAsync();
        await LoginToSourceAsync(app, server);
        await WaitForLoadedAsync(app);

        await app.DevApi.SetTextAsync("MediaSearchBar", SearchTerm, Ct);
        app.Ui.PressEnter("MediaSearchBar");
        await app.DevApi.WaitForModelAsync(
            m => m.SourceBrowserSearchText == SearchTerm
                && m.SourceBrowserItemTitles != null
                && m.SourceBrowserItemTitles.SequenceEqual(["Komödien-Film"]),
            TimeSpan.FromSeconds(30),
            "Suche „Komöd“ abgeschickt", Ct);

        // Treffer öffnen → Detailseite lädt den Film vom Server.
        await app.DevApi.InvokeAsync("ItemsCollection", 0, Ct);
        await WaitForRequestAsync(server, "/api/items/movie/501", "Film-Detailseite geladen");

        app.Ui.GoBack();

        // Rückkehr: das Suchfeld zeigt den abgeschickten Filtertext.
        Assert.Equal(SearchTerm, await SearchFieldTextAsync(app, SearchTerm));
        await app.DevApi.WaitForModelAsync(
            m => m.SourceBrowserSourceId == SourceId
                && m.SourceBrowserSearchText == SearchTerm,
            TimeSpan.FromSeconds(30),
            "Rückkehr auf die Quellen-Übersicht mit aktivem Suchfilter", Ct);
    }

    /// <summary>Wartet auf die geladene Eintragsliste der Quelle.</summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Laden der Einträge.</returns>
    private async Task WaitForLoadedAsync(AppInstance app)
    {
        await app.DevApi.WaitForModelAsync(
            m => m.SourceBrowserSourceId == SourceId
                && m.SourceBrowserItemTitles != null
                && m.SourceBrowserItemTitles.Count == 2,
            TimeSpan.FromSeconds(30),
            "Quellen-Einträge geladen", Ct);
    }

    /// <summary>
    /// Pollt den Elementbaum, bis das Suchfeld den erwarteten Text meldet
    /// (Binding-Laufzeit nach dem Modell-Tick).
    /// </summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="expected">Der erwartete Feldinhalt.</param>
    /// <returns>Der zuletzt gelesene Feldinhalt.</returns>
    private async Task<string?> SearchFieldTextAsync(AppInstance app, string expected)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        string? text = null;
        while (DateTime.UtcNow < deadline)
        {
            var tree = await app.DevApi.GetTreeAsync(Ct);
            text = tree.FindByAutomationId("MediaSearchBar")?.Text;
            if (string.Equals(text, expected, StringComparison.Ordinal))
            {
                return text;
            }

            await Task.Delay(250, Ct);
        }

        return text;
    }

    /// <summary>Wartet, bis der Mock-Server den angegebenen Pfad abgerufen hat.</summary>
    /// <param name="server">Der Mock-Server.</param>
    /// <param name="path">Der erwartete Pfad (ohne Query).</param>
    /// <param name="description">Beschreibung der Wartebedingung für Fehlermeldungen.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Request-Eingang.</returns>
    private static async Task WaitForRequestAsync(
        MockVideoServer server, string path, string description)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!server.WasPathRequested(path))
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"Wartebedingung nicht erreicht: {description} (Pfad '{path}').");
            }

            await Task.Delay(250, Ct);
        }
    }

    /// <summary>Mock-Server mit einer Quelle und zwei Einträgen; „Komödien-Film" trägt Genre 4.</summary>
    /// <returns>Der konfigurierte Mock-Server.</returns>
    private static MockVideoServer NewServer()
    {
        var server = new MockVideoServer("AAAAAA", "e2e-device-search", "e2e@example.test", "e2e-pass");
        server.Sources.Add(new MockSource { Id = SourceId, Name = SourceName });
        server.SourceGenres[SourceId] =
        [
            new MockSourceGenre { Id = 4, Name = "Komödie" },
            new MockSourceGenre { Id = 8, Name = "Drama" }
        ];
        server.SourceItems.AddRange(
        [
            new MockSourceItem
            {
                Id = 501,
                Title = "Komödien-Film",
                Type = "Movie",
                MediaSourceId = SourceId,
                GenreIds = { 4 }
            },
            new MockSourceItem
            {
                Id = 502,
                Title = "Drama-Serie",
                Type = "TvShow",
                MediaSourceId = SourceId,
                GenreIds = { 8 }
            }
        ]);
        // Der Filmtreffer ist detailseitig aufrufbar (Rückkehr-Szenario).
        server.PlaybackMovieId = 501;
        server.PlaybackMovieTitle = "Komödien-Film";

        return server;
    }

    /// <summary>
    /// Führt den Einrichtungsfluss bis zur Anmeldung aus und öffnet die
    /// Quelle über ihren Menüeintrag (die Quelleneinträge erscheinen erst
    /// nach dem <c>api/Sources</c>-Laden — <see cref="UiDriver.OpenFlyoutItem"/>
    /// wartet auf den Eintrag).
    /// </summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="server">Der Mock-Server.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Öffnen der Quelle.</returns>
    private static async Task LoginToSourceAsync(AppInstance app, MockVideoServer server)
    {
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);
        app.Ui.OpenFlyoutItem(SourceName);
    }
}
