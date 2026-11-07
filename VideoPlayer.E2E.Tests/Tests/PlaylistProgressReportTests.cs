using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Nachweis des Playlist-Bezugs in der Fortschrittsmeldung
/// (Issue #59): die Senke <c>POST /api/continue-watching/progress</c>
/// des Mock-Servers zeichnet die Request-Bodies auf
/// (<see cref="MockVideoServer.ProgressReportBodies"/>); eine
/// Playlist-Wiedergabe muss die <c>playlistId</c> der aktiven Playlist
/// melden, eine kontextlose Wiedergabe keine bzw. <c>null</c>. Nur
/// dieser Weg belegt die komplette Kette inklusive App-Rand
/// (<c>ShellAppEffectHandler</c>) und realem HTTP-Versand.
/// </summary>
public sealed class PlaylistProgressReportTests : E2ETestBase
{
    private const long PlaylistId = 701;

    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public PlaylistProgressReportTests(AppFixture fixture) : base(fixture) { }

    private static string TestVideo => Path.Combine(AppContext.BaseDirectory, "Assets", "testvideo.mp4");

    private static MockVideoServer NewServer() =>
        new("AAAAAA", "e2e-device-playlist-progress", "e2e@example.test", "e2e-pass")
        {
            PlaybackVideoPath = TestVideo
        };

    private static MockPlaylistEntry MovieEntry(long id, long mediaId, string title) =>
        new() { Id = id, MediaType = "Movie", MediaId = mediaId, Title = title };

    /// <summary>
    /// Gemeinsamer Einstieg: anmelden, Playlists-Übersicht öffnen und
    /// die Detailansicht der Playlist erreichen (Einträge geladen).
    /// </summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="server">Der Mock-Server.</param>
    /// <param name="playlistId">Die erwartete Playlist-Id.</param>
    /// <param name="entryCount">Die erwartete Eintragszahl.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach Erreichen der Detailansicht.</returns>
    private static async Task OpenPlaylistDetailsAsync(
        AppInstance app, MockVideoServer server, long playlistId, int entryCount)
    {
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);

        app.Ui.OpenFlyoutItem("Playlists");
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistsPageItemCount == 1,
            description: "Playlists-Übersicht zeigt die Playlist",
            cancellationToken: Ct);

        // Sektionsheader steht vor dem Eintrag → Index 1.
        await app.DevApi.InvokeAsync("PlaylistsCollection", 1, Ct);

        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistDetailsId == playlistId && m.PlaylistDetailsEntryCount == entryCount,
            description: "Playlist-Detailansicht zeigt alle Einträge",
            cancellationToken: Ct);
    }

    /// <summary>
    /// Wartet per Polling auf die erste am Mock-Server aufgezeichnete
    /// Fortschrittsmeldung (5-s-Takt bzw. Abschlussmeldung eines
    /// Eintrags) und liefert ihre ausgewerteten Playlist-Bezüge.
    /// </summary>
    /// <param name="server">Der Mock-Server.</param>
    /// <returns>Die Playlist-Bezüge der eingegangenen Meldungen.</returns>
    private static async Task<List<long?>> WaitForProgressReportsAsync(MockVideoServer server)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
        while (true)
        {
            var playlistIds = server.GetProgressReportPlaylistIds();
            if (playlistIds.Count > 0)
            {
                return playlistIds;
            }

            Assert.True(DateTime.UtcNow < deadline,
                "Keine Fortschrittsmeldung am Mock-Server eingegangen.");
            await Task.Delay(250, Ct);
        }
    }

    /// <summary>
    /// Pflichtszenario: eine laufende Playlist-Wiedergabe meldet den
    /// Fortschritt mit der <c>playlistId</c> der aktiven Playlist — der
    /// Server kann so den playlist-gebundenen Weiterschauen-Eintrag
    /// fortschreiben statt einen zweiten, kontextlosen anzulegen.
    /// </summary>
    [Fact]
    public async Task PlaylistPlayback_ProgressReport_IncludesPlaylistId()
    {
        Assert.True(File.Exists(TestVideo), $"Testvideo fehlt im Output-Verzeichnis: {TestVideo}");
        using var server = NewServer();
        server.Playlists.Add(new MockPlaylist
        {
            Id = PlaylistId,
            Name = "E2E Fortschritt",
            IsOwner = true,
            Entries =
            {
                MovieEntry(5, 501, "Erster Film"),
                MovieEntry(6, 502, "Zweiter Film")
            }
        });
        using var app = await StartAppAsync();
        await OpenPlaylistDetailsAsync(app, server, PlaylistId, 2);
        await app.DevApi.InvokeAsync("PlaylistDetailsPlayButton", cancellationToken: Ct);

        await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerPlaylistId == PlaylistId,
            TimeSpan.FromSeconds(60),
            "Playlist-Wiedergabe läuft", Ct);

        var playlistIds = await WaitForProgressReportsAsync(server);

        Assert.False(app.Process.HasExited, "Die App wurde während der Wiedergabe beendet.");
        Assert.Contains(playlistIds, id => id == PlaylistId);
        // Kein Meldungs-Body trägt einen fremden Playlist-Bezug.
        Assert.DoesNotContain(playlistIds, id => id.HasValue && id != PlaylistId);
    }

    /// <summary>
    /// Soll-Szenario (Gegenprobe): eine kontextlose Wiedergabe (Film
    /// direkt aus „Neu im Programm") meldet den Fortschritt ohne
    /// Playlist-Bezug — <c>playlistId</c> fehlt oder ist <c>null</c>.
    /// </summary>
    [Fact]
    public async Task Playback_ProgressReport_WithoutPlaylist_HasNullPlaylistId()
    {
        Assert.True(File.Exists(TestVideo), $"Testvideo fehlt im Output-Verzeichnis: {TestVideo}");
        using var server = new MockVideoServer(
            "AAAAAA", "e2e-device-progress", "e2e@example.test", "e2e-pass")
        {
            PlaybackMovieId = 4711,
            PlaybackMovieTitle = "E2E-Testfilm",
            PlaybackVideoPath = TestVideo,
            PlaybackResumePositionSeconds = 1
        };
        using var app = await StartAppAsync();

        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);

        // „Neu im Programm" wird aus dem synchronisierten Cache befüllt.
        await app.DevApi.WaitForModelAsync(
            m => m.RecentEntriesCount > 0,
            TimeSpan.FromSeconds(30),
            "Karussell „Neu im Programm\" befüllt", Ct);

        var tree = await app.DevApi.GetTreeAsync(Ct);
        var carousel = tree.FindByAutomationId("RecentCarousel");
        Assert.NotNull(carousel);
        Assert.NotNull(carousel.Items);
        var index = carousel.Items.FindIndex(i => i.Text == server.PlaybackMovieTitle);
        Assert.True(index >= 0,
            $"'{server.PlaybackMovieTitle}' fehlt im Karussell: " +
            string.Join(", ", carousel.Items.Select(i => i.Text)));

        await app.DevApi.InvokeAsync("RecentCarousel", index, Ct);

        // „Abspielen" erscheint erst nach dem Laden der Detaildaten.
        app.Ui.FindByAutomationId("PlayButton", TimeSpan.FromSeconds(30));
        app.Ui.ClickButton("PlayButton");

        await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerPlaylistId == 0,
            TimeSpan.FromSeconds(60),
            "Kontextlose Wiedergabe läuft", Ct);

        var playlistIds = await WaitForProgressReportsAsync(server);

        Assert.False(app.Process.HasExited, "Die App wurde während der Wiedergabe beendet.");
        Assert.All(playlistIds, id => Assert.Null(id));
    }
}
