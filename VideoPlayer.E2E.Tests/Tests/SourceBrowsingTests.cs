using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenarien des Quellen-Browsing (Genre-Filter, Schritt 1): Die
/// horizontale Genre-Leiste ist sichtbar, wenn der Server Genres für
/// die Quelle liefert; die Auswahl eines Genre-Chips lädt die
/// Trefferliste gefiltert ab der ersten Seite neu und „Alle" hebt den
/// Filter wieder auf. Liefert der Server keine Genres (leere Liste),
/// bleibt die Leiste ausgeblendet — keine funktionslose
/// „Alle"-nur-Zeile.
/// </summary>
public sealed class SourceBrowsingTests : E2ETestBase
{
    private const long SourceId = 41;
    private const string SourceName = "E2E-Quelle";

    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public SourceBrowsingTests(AppFixture fixture) : base(fixture) { }

    /// <summary>
    /// Pflichtszenario: Bei Server-Genres ist die Genre-Leiste sichtbar
    /// („Alle" + Quellen-Genres); die Genre-Auswahl lädt die Einträge
    /// gefiltert neu, „Alle" stellt die ungefilterte Liste wieder her.
    /// </summary>
    [Fact]
    public async Task GenreBar_VisibleAndFilters_ResetsOnAll()
    {
        using var server = NewServer(withGenres: true);
        using var app = await StartAppAsync();
        await LoginToSourceAsync(app, server);

        // Leiste mit „Alle" + Server-Genres sichtbar.
        await app.DevApi.WaitForModelAsync(
            m => m.SourceBrowserSourceId == SourceId
                && m.SourceBrowserGenreNames != null
                && m.SourceBrowserGenreNames.SequenceEqual(["Alle", "Komödie", "Drama"])
                && m.SourceBrowserShowGenreFilter
                && m.SourceBrowserItemTitles != null
                && m.SourceBrowserItemTitles.Count == 2,
            TimeSpan.FromSeconds(30),
            "Genre-Leiste befüllt („Alle“ + Komödie + Drama), Einträge geladen", Ct);
        Assert.True(
            await WaitForBarVisibilityAsync(app, expected: true),
            "Die Genre-Leiste muss bei Server-Genres sichtbar sein.");

        // Genre-Chip „Komödie" (Index 1) wählen → gefilterter Reload.
        await app.DevApi.InvokeAsync("GenresCollection", 1, Ct);
        await app.DevApi.WaitForModelAsync(
            m => m.SourceBrowserSelectedGenreId == 4
                && m.SourceBrowserItemTitles != null
                && m.SourceBrowserItemTitles.SequenceEqual(["Komödien-Film"]),
            TimeSpan.FromSeconds(30),
            "Gefilterte Eintragsliste für Genre „Komödie“ (Id 4)", Ct);
        lock (server.SourceItemRequests)
        {
            Assert.Contains(server.SourceItemRequests,
                r => r.Contains("genreId=4", StringComparison.Ordinal));
        }

        // „Alle" (Index 0) hebt den Filter wieder auf.
        int requestsBeforeAll;
        lock (server.SourceItemRequests)
        {
            requestsBeforeAll = server.SourceItemRequests.Count;
        }
        await app.DevApi.InvokeAsync("GenresCollection", 0, Ct);
        await app.DevApi.WaitForModelAsync(
            m => m.SourceBrowserSelectedGenreId == 0
                && m.SourceBrowserItemTitles != null
                && m.SourceBrowserItemTitles.Count == 2,
            TimeSpan.FromSeconds(30),
            "Ungefilterte Eintragsliste nach „Alle“", Ct);
        lock (server.SourceItemRequests)
        {
            Assert.Contains(
                server.SourceItemRequests.Skip(requestsBeforeAll).ToList(),
                r => !r.Contains("genreId=", StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Pflichtszenario (Projektklärung): Liefert der Server eine leere
    /// Genre-Liste, wird keine funktionslose „Alle"-nur-Zeile gezeigt —
    /// die Leiste bleibt ausgeblendet, die Eintragsliste lädt normal.
    /// </summary>
    [Fact]
    public async Task GenreBar_Hidden_WhenServerHasNoGenres()
    {
        using var server = NewServer(withGenres: false);
        using var app = await StartAppAsync();
        await LoginToSourceAsync(app, server);

        var model = await app.DevApi.WaitForModelAsync(
            m => m.SourceBrowserSourceId == SourceId
                && m.SourceBrowserGenreNames != null
                && m.SourceBrowserGenreNames.Count == 1
                && m.SourceBrowserItemTitles != null
                && m.SourceBrowserItemTitles.Count == 2,
            TimeSpan.FromSeconds(30),
            "Browser geladen, Genre-Liste enthält nur „Alle“", Ct);
        Assert.Equal("Alle", model.SourceBrowserGenreNames![0]);
        Assert.False(model.SourceBrowserShowGenreFilter);
        Assert.True(
            await WaitForBarVisibilityAsync(app, expected: false),
            "Bei leerer Server-Genreliste darf keine „Alle“-nur-Zeile erscheinen.");
    }

    /// <summary>
    /// Regressionstest (TestFlight-Feedback, iPad): Die Genre-Leiste darf
    /// nur eine schmale Zeile belegen — das Medien-Raster muss darunter
    /// sichtbar sein und die Titel müssen erreichbar bleiben. Unter iOS
    /// dehnte die CollectionView die Leiste auf die volle Resthöhe aus
    /// und verdeckte alle Einträge.
    /// </summary>
    [Fact]
    public async Task GenreBar_HeightBounded_ItemsGridVisible()
    {
        using var server = NewServer(withGenres: true);
        using var app = await StartAppAsync();
        await LoginToSourceAsync(app, server);

        await app.DevApi.WaitForModelAsync(
            m => m.SourceBrowserSourceId == SourceId
                && m.SourceBrowserShowGenreFilter
                && m.SourceBrowserItemTitles != null
                && m.SourceBrowserItemTitles.Count == 2,
            TimeSpan.FromSeconds(30),
            "Genre-Leiste und Einträge geladen", Ct);

        var tree = await WaitForLayoutAsync(app);
        var bar = tree.FindByAutomationId("GenresCollection");
        var grid = tree.FindByAutomationId("ItemsCollection");
        Assert.NotNull(bar);
        Assert.NotNull(grid);
        Assert.NotNull(bar!.Bounds);
        Assert.NotNull(grid!.Bounds);
        Assert.True(
            bar.Bounds!.Height is > 0 and <= 120,
            $"Die Genre-Leiste darf nur eine Zeile belegen (Ist: {bar.Bounds.Height}).");
        Assert.True(grid.IsVisible, "Das Medien-Raster muss unter der Genre-Leiste sichtbar sein.");
        Assert.True(
            grid.Bounds!.Height >= 200,
            $"Das Medien-Raster muss nennenswerte Höhe belegen (Ist: {grid.Bounds.Height}).");
    }

    /// <summary>Mock-Server mit einer Quelle und zwei Einträgen; Genres nur bei <paramref name="withGenres"/>.</summary>
    /// <param name="withGenres"><c>true</c> = die Quelle liefert „Komödie"/„Drama"; <c>false</c> = leere Genre-Liste.</param>
    /// <returns>Der konfigurierte Mock-Server.</returns>
    private static MockVideoServer NewServer(bool withGenres)
    {
        var server = new MockVideoServer("AAAAAA", "e2e-device-sources", "e2e@example.test", "e2e-pass");
        server.Sources.Add(new MockSource { Id = SourceId, Name = SourceName });
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
        if (withGenres)
        {
            server.SourceGenres[SourceId] =
            [
                new MockSourceGenre { Id = 4, Name = "Komödie" },
                new MockSourceGenre { Id = 8, Name = "Drama" }
            ];
        }

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

    /// <summary>
    /// Pollt den Elementbaum, bis die <c>GenresCollection</c> die
    /// erwartete Sichtbarkeit meldet (Binding-Laufzeit nach dem
    /// Modell-Tick).
    /// </summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="expected">Die erwartete Sichtbarkeit der Leiste.</param>
    /// <returns><c>true</c>, wenn die erwartete Sichtbarkeit erreicht wurde.</returns>
    private static async Task<bool> WaitForBarVisibilityAsync(AppInstance app, bool expected)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            var tree = await app.DevApi.GetTreeAsync(Ct);
            var bar = tree.FindByAutomationId("GenresCollection");
            if (bar is not null && bar.IsVisible == expected)
            {
                return true;
            }

            await Task.Delay(250, Ct);
        }

        return false;
    }

    /// <summary>
    /// Pollt den Elementbaum, bis <c>GenresCollection</c> und
    /// <c>ItemsCollection</c> gemessene Bounds melden (Layout nach dem
    /// Laden abgewartet).
    /// </summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <returns>Der letzte gelesene Elementbaum.</returns>
    private static async Task<DevApiTreeNode> WaitForLayoutAsync(AppInstance app)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        DevApiTreeNode? tree = null;
        while (DateTime.UtcNow < deadline)
        {
            tree = await app.DevApi.GetTreeAsync(Ct);
            var bar = tree.FindByAutomationId("GenresCollection");
            var grid = tree.FindByAutomationId("ItemsCollection");
            if (bar?.Bounds is { Height: > 0 } && grid?.Bounds is { Height: > 0 })
            {
                return tree;
            }

            await Task.Delay(250, Ct);
        }

        return tree!;
    }
}
