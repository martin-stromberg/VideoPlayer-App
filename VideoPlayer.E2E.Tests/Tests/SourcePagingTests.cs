using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenario des seitenweisen Nachladens der Quellen-Übersicht
/// (Issue #49, Schritt 3): Erreicht die Trefferliste beim Durchscrollen
/// das Ende, lädt sie die Folgeseite nach — die bisherige
/// Scrollposition bleibt erhalten, die Ansicht springt nicht an den
/// Listenanfang. Nachweis über den vertikalen Scroll-Prozentsatz des
/// ScrollViewers der <c>ItemsCollection</c> (Listeneinträge fehlen
/// unter Windows im UIA-Baum — die Position ist nur dort ablesbar).
/// </summary>
public sealed class SourcePagingTests : E2ETestBase
{
    private const long SourceId = 41;
    private const string SourceName = "E2E-Quelle";
    private const int ItemCount = 45;

    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public SourcePagingTests(AppFixture fixture) : base(fixture) { }

    /// <summary>
    /// Pflichtszenario: Das Durchscrollen bis ans Ende lädt die
    /// Folgeseiten nach (Seite 1 und 2 des Mock-Servers) — die
    /// Scrollposition bleibt danach erhalten (Scroll-Prozentsatz &gt; 0,
    /// kein Sprung an den Listenanfang) und die angehängten Einträge
    /// erscheinen am Ende der Trefferliste.
    /// </summary>
    [Fact]
    public async Task NextPageLoad_KeepsScrollPosition()
    {
        using var server = NewServer();
        using var app = await StartAppAsync();
        await LoginToSourceAsync(app, server);

        // Erste Seite (20 Einträge) ist geladen, Liste steht am Anfang.
        await app.DevApi.WaitForModelAsync(
            m => m.SourceBrowserSourceId == SourceId
                && m.SourceBrowserItemTitles != null
                && m.SourceBrowserItemTitles.Count == 20,
            TimeSpan.FromSeconds(30),
            "Erste Seite der Eintragsliste geladen", Ct);

        // Ans Ende scrollen → RemainingItemsThreshold löst das Nachladen
        // der Folgeseiten aus (echte Scroll-Eingabekette per UIA).
        app.Ui.ScrollToEnd("ItemsCollection");

        // Beide Folgeseiten wurden angehängt (20 + 20 + 5).
        var model = await app.DevApi.WaitForModelAsync(
            m => m.SourceBrowserItemTitles != null
                && m.SourceBrowserItemTitles.Count == ItemCount,
            TimeSpan.FromSeconds(30),
            "Folgeseiten angehängt (45 Einträge)", Ct);
        Assert.Equal("Eintrag 01", model.SourceBrowserItemTitles![0]);
        Assert.Equal("Eintrag 45", model.SourceBrowserItemTitles[ItemCount - 1]);

        // Der Server wurde seitenweise angefragt.
        lock (server.SourceItemRequests)
        {
            Assert.Contains(server.SourceItemRequests,
                r => r.Contains("page=1", StringComparison.Ordinal));
            Assert.Contains(server.SourceItemRequests,
                r => r.Contains("page=2", StringComparison.Ordinal));
        }

        // Die Scrollposition ist nach dem Nachladen erhalten — ein
        // ItemsSource-Reset würde die Ansicht an den Anfang (0 %) setzen.
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        var percent = double.NaN;
        while (DateTime.UtcNow < deadline)
        {
            percent = app.Ui.GetVerticalScrollPercent("ItemsCollection");
            if (!double.IsNaN(percent))
            {
                break;
            }

            await Task.Delay(250, Ct);
        }

        Assert.False(
            double.IsNaN(percent),
            "Kein Scroll-Provider an der ItemsCollection — Scrollposition nicht ablesbar.");
        Assert.True(
            percent > 1,
            $"Die Trefferliste muss nach dem Nachladen gescrollt bleiben (war: {percent} %).");
    }

    /// <summary>Mock-Server mit einer Quelle und <see cref="ItemCount"/> durchnummerierten Einträgen.</summary>
    /// <returns>Der konfigurierte Mock-Server.</returns>
    private static MockVideoServer NewServer()
    {
        var server = new MockVideoServer("AAAAAA", "e2e-device-paging", "e2e@example.test", "e2e-pass");
        server.Sources.Add(new MockSource { Id = SourceId, Name = SourceName });
        for (var i = 1; i <= ItemCount; i++)
        {
            server.SourceItems.Add(new MockSourceItem
            {
                Id = 500 + i,
                Title = $"Eintrag {i:D2}",
                Type = "Movie",
                MediaSourceId = SourceId
            });
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
}
