using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenarien der Playlist-Verwaltung (Issue #43, Schritt 6):
/// Playlist-Anlage/-Bearbeitung/-Löschung über Formular und
/// Verwaltungsmenü, „Zu Playlist hinzufügen" der Medien-Detailseite,
/// Eintrags-Entfernen inkl. Weiterschauen-Konflikt (HTTP 409 mit
/// Bestätigungs-Retry), Verschieben im manuellen Modus,
/// Sortiermodus-Wechsel mit Verlust-Bestätigung, Genre-Editor
/// (Setzen/Zurücksetzen), Öffentlich-Schalter für Administrator:innen
/// sowie der schreibgeschützte Blick auf fremde öffentliche Playlists.
/// </summary>
public sealed class PlaylistManagementTests : E2ETestBase
{
    private const long OwnPlaylistId = 711;

    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public PlaylistManagementTests(AppFixture fixture) : base(fixture) { }

    private static MockVideoServer NewServer() =>
        new("AAAAAA", "e2e-device-playlistmgmt", "e2e@example.test", "e2e-pass");

    private static MockPlaylistEntry MovieEntry(long id, long mediaId, string title) =>
        new() { Id = id, MediaType = "Movie", MediaId = mediaId, Title = title };

    /// <summary>Führt den Einrichtungsfluss aus und öffnet die Playlists-Übersicht.</summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="server">Der Mock-Server mit den Playlist-Testdaten.</param>
    private static async Task LoginToPlaylistsAsync(AppInstance app, MockVideoServer server)
    {
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);
        app.Ui.OpenFlyoutItem("Playlists");
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistsPageItemCount == server.Playlists.Count,
            description: "Playlists-Übersicht geladen",
            cancellationToken: Ct);
    }

    /// <summary>Öffnet die Detailansicht einer Playlist anhand ihres Namens.</summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="playlistName">Der Anzeigename der Playlist in der Übersicht.</param>
    /// <param name="playlistId">Die erwartete Playlist-Id der Detailansicht.</param>
    private static async Task OpenDetailsAsync(AppInstance app, string playlistName, long playlistId)
    {
        var tree = await app.DevApi.GetTreeAsync(Ct);
        var collection = tree.FindByAutomationId("PlaylistsCollection");
        Assert.NotNull(collection?.Items);
        var index = collection!.Items!.FindIndex(i => i.Text == playlistName);
        Assert.True(index >= 0,
            $"'{playlistName}' fehlt in der Übersicht: " +
            string.Join(", ", collection.Items.Select(i => i.Text)));
        await app.DevApi.InvokeAsync("PlaylistsCollection", index, Ct);
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistDetailsId == playlistId && m.PlaylistDetailsError == null,
            description: "Playlist-Detailansicht geladen",
            cancellationToken: Ct);
    }

    /// <summary>Wählt eine Aktion des Verwaltungsmenüs der Detailseite.</summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="action">Der sichtbare Aktionsname im Menü.</param>
    private static async Task ChooseManageActionAsync(AppInstance app, string action)
    {
        await app.DevApi.InvokeAsync("PlaylistManageButton", cancellationToken: Ct);
        app.Ui.ClickByName(action, TimeSpan.FromSeconds(15), initialWait: TimeSpan.FromMilliseconds(300));
    }

    /// <summary>Wählt eine Aktion des Eintrags-Kontextmenüs.</summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="entryId">Die Eintrags-Id (Teil der Menü-AutomationId).</param>
    /// <param name="action">Der sichtbare Aktionsname im Menü.</param>
    private static async Task ChooseEntryActionAsync(AppInstance app, long entryId, string action)
    {
        await app.DevApi.InvokeAsync($"EntryMenu_{entryId}", cancellationToken: Ct);
        app.Ui.ClickByName(action, TimeSpan.FromSeconds(15));
        // Die nächste Menüöffnung darf erst nach dem Schließen des vorherigen
        // ActionSheets erfolgen; der Modell-Reload kann davor fertig sein.
        app.Ui.WaitForNameAbsent(action, TimeSpan.FromSeconds(15));
    }

    /// <summary>
    /// AK 1: „Neue Playlist" öffnet das Formular; nach dem Speichern
    /// ist die Playlist auf dem Server angelegt und in der Übersicht sichtbar.
    /// </summary>
    [Fact]
    public async Task CreatePlaylist_ViaForm_AppearsInOverviewAndPostsToServer()
    {
        using var server = NewServer();
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);
        app.Ui.OpenFlyoutItem("Playlists");
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistsPageItemCount == 0 && !m.PlaylistFormOpen,
            description: "Playlists-Übersicht geladen (leer)",
            cancellationToken: Ct);

        await app.DevApi.InvokeAsync("CreatePlaylistButton", cancellationToken: Ct);
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistFormOpen,
            description: "Playlist-Formular geöffnet",
            cancellationToken: Ct);
        await app.DevApi.SetTextAsync("PlaylistFormNameEntry", "E2E Neue Liste", Ct);
        await app.DevApi.SetTextAsync("PlaylistFormDescriptionEntry", "Aus dem Test", Ct);
        await app.DevApi.InvokeAsync("PlaylistFormSaveButton", cancellationToken: Ct);

        await app.DevApi.WaitForModelAsync(
            m => !m.PlaylistFormOpen && m.PlaylistsPageItemCount == 1,
            description: "Formular geschlossen, neue Playlist in der Übersicht",
            cancellationToken: Ct);
        Assert.Equal(["E2E Neue Liste"], server.CreatedPlaylistNames);
        Assert.Equal("E2E Neue Liste", server.Playlists.Single().Name);
        Assert.Equal("Aus dem Test", server.Playlists.Single().Description);
    }

    /// <summary>
    /// AK 2 + 3: „Bearbeiten" übernimmt die Werte ins Formular; nach dem
    /// Speichern ist die Umbenennung auf dem Server; „Playlist löschen"
    /// entfernt sie nach Bestätigung und kehrt zur Übersicht zurück.
    /// </summary>
    [Fact]
    public async Task EditAndDeletePlaylist_ViaManageMenu_UpdatesServerAndNavigatesBack()
    {
        using var server = NewServer();
        server.Playlists.Add(new MockPlaylist
        {
            Id = OwnPlaylistId,
            Name = "E2E Umbenennen",
            IsOwner = true,
            Entries = { MovieEntry(1, 501, "Film Eins") }
        });
        using var app = await StartAppAsync();
        await LoginToPlaylistsAsync(app, server);
        await OpenDetailsAsync(app, "E2E Umbenennen", OwnPlaylistId);
        Assert.True((await app.DevApi.GetModelAsync(Ct)).CanManagePlaylist);

        // Bearbeiten: das Formular trägt den bisherigen Namen.
        await ChooseManageActionAsync(app, "Bearbeiten");
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistFormOpen,
            description: "Bearbeiten-Formular geöffnet",
            cancellationToken: Ct);
        await app.DevApi.SetTextAsync("PlaylistFormNameEntry", "E2E Umbenannt", Ct);
        await app.DevApi.InvokeAsync("PlaylistFormSaveButton", cancellationToken: Ct);
        await app.DevApi.WaitForModelAsync(
            m => !m.PlaylistFormOpen,
            description: "Formular geschlossen",
            cancellationToken: Ct);
        Assert.Equal(1, server.UpdatedPlaylistCount);
        Assert.Equal("E2E Umbenannt", server.Playlists.Single().Name);

        // Löschen: erst nach der Bestätigung entfernt der Server die Playlist.
        await ChooseManageActionAsync(app, "Playlist löschen");
        app.Ui.ClickByName("Löschen", TimeSpan.FromSeconds(15));
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistDetailsId == 0 && m.PlaylistsPageItemCount == 0,
            description: "Detailansicht geschlossen, Übersicht leer",
            cancellationToken: Ct);
        Assert.Equal(1, server.DeletedPlaylistCount);
        Assert.Empty(server.Playlists);
    }

    /// <summary>
    /// AK 4: „Zu Playlist" auf der Medien-Detailseite listet die eigenen
    /// Playlists; die Wahl legt den Eintrag auf dem Server an und meldet
    /// den Erfolg.
    /// </summary>
    [Fact]
    public async Task AddMediaToPlaylist_FromDetails_PostsEntry()
    {
        using var server = NewServer();
        server.PlaybackMovieId = 4711;
        server.PlaybackMovieTitle = "E2E-Testfilm";
        server.Playlists.Add(new MockPlaylist
        {
            Id = OwnPlaylistId,
            Name = "E2E Ziel-Liste",
            IsOwner = true
        });
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);
        await app.DevApi.WaitForModelAsync(
            m => m.RecentEntriesCount > 0,
            TimeSpan.FromSeconds(30),
            "Karussell „Neu im Programm\" befüllt", Ct);

        var tree = await app.DevApi.GetTreeAsync(Ct);
        var index = tree.FindByAutomationId("RecentCarousel")!.Items!
            .FindIndex(i => i.Text == "E2E-Testfilm");
        Assert.True(index >= 0);
        await app.DevApi.InvokeAsync("RecentCarousel", index, Ct);
        app.Ui.FindByAutomationId("AddToPlaylistButton", TimeSpan.FromSeconds(30));

        await app.DevApi.InvokeAsync("AddToPlaylistButton", cancellationToken: Ct);
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistTargets is { Count: 1 } && m.PlaylistPickerPending,
            description: "Ziel-Playlist-Auswahl geöffnet",
            cancellationToken: Ct);
        app.Ui.ClickByName("E2E Ziel-Liste", TimeSpan.FromSeconds(15));

        var model = await app.DevApi.WaitForModelAsync(
            m => m.AddToPlaylistNotice != null && !m.AddToPlaylistNoticeIsError,
            description: "Erfolgshinweis der Playlist-Hinzufügung",
            cancellationToken: Ct);
        Assert.False(string.IsNullOrWhiteSpace(model.AddToPlaylistNotice));
        Assert.Equal([$"{OwnPlaylistId}:Movie:4711"], server.AddedPlaylistEntries);
        var added = server.Playlists.Single().Entries.Single();
        Assert.Equal(4711, added.MediaId);
        Assert.Equal("E2E-Testfilm", added.Title);
    }

    /// <summary>
    /// AK 5: „Aus Playlist entfernen" entfernt den Eintrag auf dem
    /// Server; die Detailansicht lädt die verbleibende Liste neu.
    /// </summary>
    [Fact]
    public async Task RemoveEntry_RemovesFromServer()
    {
        using var server = NewServer();
        server.Playlists.Add(new MockPlaylist
        {
            Id = OwnPlaylistId,
            Name = "E2E Entfernen",
            IsOwner = true,
            Entries =
            {
                MovieEntry(11, 511, "Film Alpha"),
                MovieEntry(12, 512, "Film Beta")
            }
        });
        using var app = await StartAppAsync();
        await LoginToPlaylistsAsync(app, server);
        await OpenDetailsAsync(app, "E2E Entfernen", OwnPlaylistId);
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistDetailsEntryCount == 2,
            description: "zwei Einträge geladen",
            cancellationToken: Ct);

        await ChooseEntryActionAsync(app, 11, "Aus Playlist entfernen");

        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistDetailsEntryCount == 1,
            description: "Eintrag entfernt",
            cancellationToken: Ct);
        Assert.Equal([false], server.RemoveEntryConfirmFlags);
        Assert.Equal([12L], server.Playlists.Single().Entries.Select(e => e.Id).ToList());
    }

    /// <summary>
    /// AK 6: ein Eintrag mit Weiterschauen-Bezug liefert die 409-Antwort;
    /// der Dialog („Aus Weiterschauen entfernen?") bestätigt und der
    /// Retry trägt <c>confirmContinueWatchingRemoval=true</c>.
    /// </summary>
    [Fact]
    public async Task RemoveEntryWithConflict_ConfirmationRetriesWithFlag()
    {
        using var server = NewServer();
        server.Playlists.Add(new MockPlaylist
        {
            Id = OwnPlaylistId,
            Name = "E2E Konflikt",
            IsOwner = true,
            Entries =
            {
                new MockPlaylistEntry
                {
                    Id = 11,
                    MediaType = "Movie",
                    MediaId = 511,
                    Title = "Laufender Film",
                    RequiresContinueWatchingRemovalConfirmation = true
                }
            }
        });
        using var app = await StartAppAsync();
        await LoginToPlaylistsAsync(app, server);
        await OpenDetailsAsync(app, "E2E Konflikt", OwnPlaylistId);
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistDetailsEntryCount == 1,
            description: "Eintrag geladen",
            cancellationToken: Ct);

        // Erster Aufruf ohne Flagge → Konflikt-Zustand + Bestätigungsdialog.
        await ChooseEntryActionAsync(app, 11, "Aus Playlist entfernen");
        await app.DevApi.WaitForModelAsync(
            m => m.PendingRemoveConflictEntryId == 11,
            description: "Weiterschauen-Bestätigung ausstehend",
            cancellationToken: Ct);
        app.Ui.ClickByName("Entfernen", TimeSpan.FromSeconds(15));

        // Der Retry trägt die Flagge — der Eintrag verschwindet.
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistDetailsEntryCount == 0,
            description: "Eintrag nach Bestätigung entfernt",
            cancellationToken: Ct);
        Assert.Equal([false, true], server.RemoveEntryConfirmFlags);
    }

    /// <summary>
    /// AK 6 (Abbruch): die Weiterschauen-Bestätigung lässt sich
    /// verwerfen — kein zweiter Serveraufruf, der Eintrag bleibt.
    /// </summary>
    [Fact]
    public async Task RemoveEntryWithConflict_CancelKeepsEntry()
    {
        using var server = NewServer();
        server.Playlists.Add(new MockPlaylist
        {
            Id = OwnPlaylistId,
            Name = "E2E Abbruch",
            IsOwner = true,
            Entries =
            {
                new MockPlaylistEntry
                {
                    Id = 11,
                    MediaType = "Movie",
                    MediaId = 511,
                    Title = "Laufender Film",
                    RequiresContinueWatchingRemovalConfirmation = true
                }
            }
        });
        using var app = await StartAppAsync();
        await LoginToPlaylistsAsync(app, server);
        await OpenDetailsAsync(app, "E2E Abbruch", OwnPlaylistId);
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistDetailsEntryCount == 1,
            description: "Eintrag geladen",
            cancellationToken: Ct);

        await ChooseEntryActionAsync(app, 11, "Aus Playlist entfernen");
        await app.DevApi.WaitForModelAsync(
            m => m.PendingRemoveConflictEntryId == 11,
            description: "Weiterschauen-Bestätigung ausstehend",
            cancellationToken: Ct);
        app.Ui.ClickByName("Abbrechen", TimeSpan.FromSeconds(15));

        await app.DevApi.WaitForModelAsync(
            m => m.PendingRemoveConflictEntryId == null,
            description: "Konflikt verworfen",
            cancellationToken: Ct);
        Assert.Equal([false], server.RemoveEntryConfirmFlags);
        Assert.Single(server.Playlists.Single().Entries);
    }

    /// <summary>
    /// AK 7/8/9: „An den Anfang", „Eins nach vorn/hinten" und
    /// „Ans Ende" ändern die Server-Reihenfolge im manuellen Modus;
    /// die Detailansicht übernimmt sie nach dem Reload.
    /// </summary>
    [Fact]
    public async Task MoveEntries_ReordersOnServer()
    {
        using var server = NewServer();
        server.Playlists.Add(new MockPlaylist
        {
            Id = OwnPlaylistId,
            Name = "E2E Ordnung",
            IsOwner = true,
            Entries =
            {
                MovieEntry(1, 501, "Film A"),
                MovieEntry(2, 502, "Film B"),
                MovieEntry(3, 503, "Film C")
            }
        });
        using var app = await StartAppAsync();
        await LoginToPlaylistsAsync(app, server);
        await OpenDetailsAsync(app, "E2E Ordnung", OwnPlaylistId);
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistDetailsEntryCount == 3,
            description: "drei Einträge geladen",
            cancellationToken: Ct);

        // „Ans Ende": Film A (sortOrder 0) → max + 1 = 3.
        await ChooseEntryActionAsync(app, 1, "Ans Ende");
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistEntryTitles != null
                && m.PlaylistEntryTitles.SequenceEqual(new[] { "Film B", "Film C", "Film A" }),
            description: "Film A ans Ende verschoben",
            cancellationToken: Ct);
        Assert.Equal(1, server.ReorderEntryCount);
        Assert.Equal([2L, 3L, 1L], server.Playlists.Single().Entries.Select(e => e.Id).ToList());

        // „An den Anfang": Film A zurück an den Listenanfang.
        await ChooseEntryActionAsync(app, 1, "An den Anfang");
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistEntryTitles != null
                && m.PlaylistEntryTitles.SequenceEqual(new[] { "Film A", "Film B", "Film C" }),
            description: "Film A wieder vorn",
            cancellationToken: Ct);
        Assert.Equal([1L], server.MoveToBeginningRequests);

        // „Eins nach hinten": Film A auf die Position von Film B.
        await ChooseEntryActionAsync(app, 1, "Eins nach hinten");
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistEntryTitles != null
                && m.PlaylistEntryTitles.SequenceEqual(new[] { "Film B", "Film A", "Film C" }),
            description: "Film A eine Position nach hinten",
            cancellationToken: Ct);
        Assert.Equal([1L], server.MoveBetweenRequests);
    }

    /// <summary>
    /// AK 10: der Sortiermodus-Wechsel einer manuellen Liste löst die
    /// Verlust-Bestätigung aus; „Wechseln" wiederholt den Aufruf mit
    /// der Bestätigungsflagge und der Modus steht auf dem Server.
    /// </summary>
    [Fact]
    public async Task ChangeSortMode_WithConflict_ConfirmsAndPatches()
    {
        using var server = NewServer();
        server.Playlists.Add(new MockPlaylist
        {
            Id = OwnPlaylistId,
            Name = "E2E Sortierung",
            IsOwner = true,
            Entries = { MovieEntry(1, 501, "Film A") }
        });
        using var app = await StartAppAsync();
        await LoginToPlaylistsAsync(app, server);
        await OpenDetailsAsync(app, "E2E Sortierung", OwnPlaylistId);

        await ChooseManageActionAsync(app, "Nach Erscheinungsdatum sortieren");
        await app.DevApi.WaitForModelAsync(
            m => m.PendingSortModeConflict == "ByReleaseDate",
            description: "Verlust-Bestätigung ausstehend",
            cancellationToken: Ct);
        app.Ui.ClickByName("Ändern", TimeSpan.FromSeconds(15));

        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistSortMode == "ByReleaseDate" && m.PendingSortModeConflict == null,
            description: "Sortiermodus gewechselt",
            cancellationToken: Ct);
        Assert.Equal(["ByReleaseDate|unconfirmed", "ByReleaseDate|confirmed"],
            server.SortModeRequests);
        Assert.Equal("ByReleaseDate", server.Playlists.Single().SortMode);
    }

    /// <summary>
    /// AK 11/12: der Genre-Editor zeigt die Server-Optionen mit
    /// Vorauswahl; Speichern schreibt die Ids (markiert manuell) und
    /// „Zurücksetzen" löst die manuelle Festlegung wieder auf.
    /// </summary>
    [Fact]
    public async Task SetAndResetGenres_UpdatesServer()
    {
        using var server = NewServer();
        server.GenreOptions.Add(new MockGenreOption { Id = 1, Name = "Komödie" });
        server.GenreOptions.Add(new MockGenreOption { Id = 2, Name = "Drama" });
        server.Playlists.Add(new MockPlaylist
        {
            Id = OwnPlaylistId,
            Name = "E2E Genres",
            IsOwner = true,
            Genres = ["Komödie"],
            GenreIds = { 1 },
            GenresManuallyOverridden = true,
            Entries = { MovieEntry(1, 501, "Film A") }
        });
        using var app = await StartAppAsync();
        await LoginToPlaylistsAsync(app, server);
        await OpenDetailsAsync(app, "E2E Genres", OwnPlaylistId);
        Assert.True((await app.DevApi.GetModelAsync(Ct)).PlaylistGenresManuallyOverridden);

        // Editor: Vorauswahl folgt den Playlist-Genres.
        await ChooseManageActionAsync(app, "Genres bearbeiten");
        var model = await app.DevApi.WaitForModelAsync(
            m => m.PlaylistGenreEditorOpen && m.PlaylistGenreOptions is { Count: 2 },
            description: "Genre-Editor mit Optionen geöffnet",
            cancellationToken: Ct);
        Assert.True(model.PlaylistGenreOptions![0].IsSelected, "Komödie vorgewählt");
        Assert.False(model.PlaylistGenreOptions[1].IsSelected);

        // Drama dazu wählen und speichern.
        await app.DevApi.InvokeAsync("GenreOption_2", cancellationToken: Ct);
        await app.DevApi.InvokeAsync("GenreEditorSaveButton", cancellationToken: Ct);
        await app.DevApi.WaitForModelAsync(
            m => !m.PlaylistGenreEditorOpen && m.PlaylistGenreText == "Komödie, Drama",
            description: "Genres gespeichert",
            cancellationToken: Ct);
        Assert.Equal([1L, 2L], Assert.Single(server.GenresSetRequests));
        Assert.Equal(["Komödie", "Drama"], server.Playlists.Single().Genres);

        // Zurücksetzen auf die berechneten Genres.
        await ChooseManageActionAsync(app, "Genres bearbeiten");
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistGenreEditorOpen,
            description: "Genre-Editor erneut geöffnet",
            cancellationToken: Ct);
        await app.DevApi.InvokeAsync("GenreEditorResetButton", cancellationToken: Ct);
        await app.DevApi.WaitForModelAsync(
            m => !m.PlaylistGenreEditorOpen && !m.PlaylistGenresManuallyOverridden,
            description: "Genres zurückgesetzt",
            cancellationToken: Ct);
        Assert.Equal(1, server.GenresResetCount);
    }

    /// <summary>
    /// AK 13: ein Admin-Token blendet den Öffentlich-Schalter ein;
    /// „Als öffentlich markieren" stellt die Playlist auf dem Server
    /// öffentlich.
    /// </summary>
    [Fact]
    public async Task SetPublic_AdminUser_TogglesFlag()
    {
        using var server = NewServer();
        server.LoginIsAdmin = true;
        server.Playlists.Add(new MockPlaylist
        {
            Id = OwnPlaylistId,
            Name = "E2E Öffentlich",
            IsOwner = true,
            Entries = { MovieEntry(1, 501, "Film A") }
        });
        using var app = await StartAppAsync();
        await LoginToPlaylistsAsync(app, server);
        await OpenDetailsAsync(app, "E2E Öffentlich", OwnPlaylistId);
        // Der Admin-Claim kommt mit dem Kopf-Nachschlag der Detailseite.
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistIsAdmin,
            description: "Administrator-Claim geladen",
            cancellationToken: Ct);

        await ChooseManageActionAsync(app, "Öffentlich schalten");

        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistIsPublic,
            description: "Playlist öffentlich",
            cancellationToken: Ct);
        Assert.Equal([true], server.PublicFlagRequests);
        Assert.True(server.Playlists.Single().IsPublic);
    }

    /// <summary>
    /// AK 14: eine fremde öffentliche Playlist zeigt keine
    /// Verwaltungsaktionen und keine Eintrags-Kontextmenüs.
    /// </summary>
    [Fact]
    public async Task ForeignPublicPlaylist_ShowsNoManageActions()
    {
        using var server = NewServer();
        server.Playlists.Add(new MockPlaylist
        {
            Id = OwnPlaylistId,
            Name = "E2E Fremde Liste",
            IsOwner = false,
            IsPublic = true,
            Entries = { MovieEntry(1, 501, "Fremder Film") }
        });
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);
        app.Ui.OpenFlyoutItem("Playlists");
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistsPageItemCount == 1,
            description: "Übersicht zeigt die fremde Playlist",
            cancellationToken: Ct);
        await OpenDetailsAsync(app, "E2E Fremde Liste", OwnPlaylistId);
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistDetailsEntryCount == 1,
            description: "fremder Eintrag geladen",
            cancellationToken: Ct);

        var model = await app.DevApi.GetModelAsync(Ct);
        Assert.True(model.PlaylistDetailsId == OwnPlaylistId);
        Assert.False(model.PlaylistIsOwner);
        Assert.False(model.CanManagePlaylist);
        var tree = await app.DevApi.GetTreeAsync(Ct);
        var manageButton = tree.FindByAutomationId("PlaylistManageButton");
        Assert.True(manageButton is null || !manageButton.IsVisible,
            "Verwaltungsmenü darf nicht sichtbar sein.");
        var entryMenu = tree.FindByAutomationId("EntryMenu_1");
        Assert.True(entryMenu is null || !entryMenu.IsVisible,
            "Eintrags-Kontextmenü darf nicht sichtbar sein.");
    }
}
