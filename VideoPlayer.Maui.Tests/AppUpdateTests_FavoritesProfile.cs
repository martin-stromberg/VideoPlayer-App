using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// MVU-Übergänge der Favoriten- und Profil-Seite (Schritt 9):
/// Favoritenliste laden (online/offline), direktes Entfernen mit
/// Fehlschlag-Rücknahme, Profilinformationen sowie die Abmeldung mit
/// Zustandsrücksetzung und Rückkehr in den Anmelde-Workflow.
/// </summary>
public class AppUpdateTests_FavoritesProfile
{
    private static AppModel Connected() => new()
    {
        ConnectionState = ConnectionState.Connected,
        IsStartupRunning = false,
        CurrentPage = AppPage.Favorites
    };

    private static FavoriteListItem Item(long favoriteId, long mediaId = 0) =>
        new()
        {
            FavoriteId = favoriteId,
            Id = mediaId > 0 ? mediaId : favoriteId * 10,
            MediaType = "movie",
            Title = $"Favorit {favoriteId}"
        };

    private static FavoritesPageState Loaded(params FavoriteListItem[] items) =>
        new() { Attempted = true, Items = items };

    // --- Favoriten-Seite: Laden ---

    /// <summary>Prüft FavoritesPageOpened: bei <c>Connected</c> → startet das Laden.</summary>
    [Fact]
    public void FavoritesPageOpened_Connected_StartsLoading()
    {
        var transition = AppUpdate.Update(Connected(), new FavoritesPageOpenedMessage());

        Assert.True(transition.Model.FavoritesPage.IsLoading);
        Assert.True(transition.Model.FavoritesPage.Attempted);
        var effect = Assert.Single(transition.Effects);
        Assert.True(Assert.IsType<LoadFavoritesPageEffect>(effect).RefreshFromServer);
    }

    /// <summary>Prüft FavoritesPageOpened: bei <c>Offline</c> → lädt den Cache-Stand ohne Server-Aufruf.</summary>
    [Fact]
    public void FavoritesPageOpened_Offline_LoadsCacheCopy()
    {
        // Im Offline-Zustand wird dieselbe Sequenz angestoßen — der
        // Effekt-Handler überspringt den Server-Sync (RefreshFromServer
        // = false) und liest den Cache direkt.
        var model = Connected() with { ConnectionState = ConnectionState.Offline };

        var transition = AppUpdate.Update(model, new FavoritesPageOpenedMessage());

        Assert.True(transition.Model.FavoritesPage.IsLoading);
        var effect = Assert.Single(transition.Effects);
        Assert.False(Assert.IsType<LoadFavoritesPageEffect>(effect).RefreshFromServer);
    }

    /// <summary>Prüft FavoritesPageOpened: bei ausstehender Anmeldung → kein Ladevorgang.</summary>
    [Fact]
    public void FavoritesPageOpened_NeedsLogin_NoLoad()
    {
        var model = new AppModel { ConnectionState = ConnectionState.NeedsLogin };

        var transition = AppUpdate.Update(model, new FavoritesPageOpenedMessage());

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft FavoritesPageOpened: bei laufendem Ladevorgang → kein zweiter Lauf.</summary>
    [Fact]
    public void FavoritesPageOpened_WhileLoading_NoSecondRun()
    {
        var model = Connected() with
        {
            FavoritesPage = FavoritesPageState.Initial with { IsLoading = true }
        };

        var transition = AppUpdate.Update(model, new FavoritesPageOpenedMessage());

        Assert.Empty(transition.Effects);
        Assert.True(transition.Model.FavoritesPage.IsLoading);
    }

    /// <summary>Prüft FavoritesPageLoaded: übernimmt die Einträge und beendet den Ladezustand.</summary>
    [Fact]
    public void FavoritesPageLoaded_FillsItems()
    {
        var model = Connected() with
        {
            FavoritesPage = FavoritesPageState.Initial with { IsLoading = true, Attempted = true }
        };
        var items = new[] { Item(1), Item(2) };

        var transition = AppUpdate.Update(model, new FavoritesPageLoadedMessage(items, null));

        Assert.False(transition.Model.FavoritesPage.IsLoading);
        Assert.Equal(2, transition.Model.FavoritesPage.Items.Count);
        Assert.True(transition.Model.FavoritesPage.HasItems);
        Assert.False(transition.Model.FavoritesPage.HasError);
    }

    /// <summary>Prüft FavoritesPageLoaded: bei Fehler → Fehlerzustand statt Liste.</summary>
    [Fact]
    public void FavoritesPageLoaded_Error_SetsError()
    {
        var model = Connected() with
        {
            FavoritesPage = FavoritesPageState.Initial with { IsLoading = true, Attempted = true }
        };

        var transition = AppUpdate.Update(
            model, new FavoritesPageLoadedMessage([], "Die Favoriten konnten nicht geladen werden."));

        Assert.True(transition.Model.FavoritesPage.HasError);
        Assert.Empty(transition.Model.FavoritesPage.Items);
    }

    // --- Favoriten-Seite: Entfernen ---

    /// <summary>Prüft FavoriteRemoveRequested: online → markiert den Eintrag und startet den Server-Aufruf.</summary>
    [Fact]
    public void FavoriteRemoveRequested_Online_MarksRemovingAndStartsEffect()
    {
        var model = Connected() with { FavoritesPage = Loaded(Item(5), Item(6)) };

        var transition = AppUpdate.Update(model, new FavoriteRemoveRequestedMessage(Item(5)));

        var removing = Assert.Single(transition.Model.FavoritesPage.Items, i => i.FavoriteId == 5);
        Assert.True(removing.IsRemoving);
        Assert.False(removing.CanRemove);
        Assert.False(Assert.Single(transition.Model.FavoritesPage.Items, i => i.FavoriteId == 6).IsRemoving);
        var effect = Assert.Single(transition.Effects);
        Assert.Equal(5, Assert.IsType<RemoveFavoriteEffect>(effect).FavoriteId);
    }

    /// <summary>Prüft FavoriteRemoveRequested: offline → kein Entfernen (Server ist führend).</summary>
    [Fact]
    public void FavoriteRemoveRequested_Offline_NoOp()
    {
        var model = Connected() with
        {
            ConnectionState = ConnectionState.Offline,
            FavoritesPage = Loaded(Item(5))
        };

        var transition = AppUpdate.Update(model, new FavoriteRemoveRequestedMessage(Item(5)));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft FavoriteRemoveRequested: unbekannter Eintrag → unverändert.</summary>
    [Fact]
    public void FavoriteRemoveRequested_UnknownItem_NoOp()
    {
        var model = Connected() with { FavoritesPage = Loaded(Item(5)) };

        var transition = AppUpdate.Update(model, new FavoriteRemoveRequestedMessage(Item(99)));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft FavoriteRemoveRequested: laufendes Entfernen → kein zweiter Aufruf.</summary>
    [Fact]
    public void FavoriteRemoveRequested_AlreadyRemoving_NoOp()
    {
        var model = Connected() with
        {
            FavoritesPage = Loaded(Item(5) with { IsRemoving = true })
        };

        var transition = AppUpdate.Update(model, new FavoriteRemoveRequestedMessage(Item(5)));

        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft FavoriteRemoveCompleted: bei Erfolg → entfernt den Eintrag aus der Liste.</summary>
    [Fact]
    public void FavoriteRemoveCompleted_Success_RemovesItem()
    {
        var model = Connected() with
        {
            FavoritesPage = Loaded(Item(5) with { IsRemoving = true }, Item(6))
        };

        var transition = AppUpdate.Update(model, new FavoriteRemoveCompletedMessage(5, Success: true));

        var item = Assert.Single(transition.Model.FavoritesPage.Items);
        Assert.Equal(6, item.FavoriteId);
        Assert.False(transition.Model.FavoritesPage.HasError);
    }

    /// <summary>Prüft FavoriteRemoveCompleted: bei Fehlschlag → Eintrag bleibt und Fehler wird angezeigt.</summary>
    [Fact]
    public void FavoriteRemoveCompleted_Failure_RestoresItemAndShowsError()
    {
        var model = Connected() with
        {
            FavoritesPage = Loaded(Item(5) with { IsRemoving = true }, Item(6))
        };

        var transition = AppUpdate.Update(model, new FavoriteRemoveCompletedMessage(5, Success: false));

        Assert.Equal(2, transition.Model.FavoritesPage.Items.Count);
        Assert.False(transition.Model.FavoritesPage.Items.Single(i => i.FavoriteId == 5).IsRemoving);
        Assert.True(transition.Model.FavoritesPage.HasError);
        Assert.Empty(transition.Effects);
    }

    // --- Profil-Seite ---

    /// <summary>Prüft ProfilePageOpened: startet die Ermittlung der Profilinformationen.</summary>
    [Fact]
    public void ProfilePageOpened_StartsLoading()
    {
        var transition = AppUpdate.Update(Connected(), new ProfilePageOpenedMessage());

        Assert.True(transition.Model.Profile.IsLoading);
        var effect = Assert.Single(transition.Effects);
        Assert.IsType<LoadProfileInfoEffect>(effect);
    }

    /// <summary>Prüft ProfilePageOpened: bei laufender Ermittlung → kein zweiter Lauf.</summary>
    [Fact]
    public void ProfilePageOpened_WhileLoading_NoSecondRun()
    {
        var model = Connected() with
        {
            Profile = ProfilePageState.Initial with { IsLoading = true }
        };

        var transition = AppUpdate.Update(model, new ProfilePageOpenedMessage());

        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft ProfileInfoLoaded: übernimmt Benutzername, E-Mail und Profilname.</summary>
    [Fact]
    public void ProfileInfoLoaded_FillsProfile()
    {
        var model = Connected() with
        {
            Profile = ProfilePageState.Initial with { IsLoading = true }
        };

        var transition = AppUpdate.Update(
            model, new ProfileInfoLoadedMessage(new ProfileInfo("martin", "martin@server", "winter")));

        Assert.False(transition.Model.Profile.IsLoading);
        Assert.Equal("martin", transition.Model.Profile.Username);
        Assert.Equal("martin@server", transition.Model.Profile.Email);
        Assert.Equal("winter", transition.Model.Profile.ProfileName);
    }

    // --- Abmeldung ---

    /// <summary>Prüft LogoutRequested: setzt die Sitzung zurück und führt zur Anmeldemaske.</summary>
    [Fact]
    public void LogoutRequested_ResetsSessionAndNavigatesToLogin()
    {
        var model = Connected() with
        {
            LoginUsername = "martin",
            IsSyncRunning = true,
            ContinueWatching = [new HomeMediaItem { Id = 1, MediaType = "movie", Title = "Film" }],
            Favorites = [new HomeMediaItem { Id = 2, MediaType = "movie", Title = "Favorit" }],
            RecentEntries = [new HomeMediaItem { Id = 3, MediaType = "movie", Title = "Neu" }],
            SourcesOverview = new SourcesOverviewState
            {
                Attempted = true,
                Sources = [new SourceItem { Id = 1, Name = "Quelle" }]
            },
            FavoritesPage = Loaded(Item(5)),
            Profile = new ProfilePageState { Attempted = true, Username = "martin" }
        };

        var transition = AppUpdate.Update(model, new LogoutRequestedMessage());

        var updated = transition.Model;
        Assert.Equal(ConnectionState.NeedsLogin, updated.ConnectionState);
        Assert.Equal(AppPage.Login, updated.CurrentPage);
        Assert.Null(updated.LoginUsername);
        Assert.False(updated.IsSyncRunning);
        Assert.Empty(updated.ContinueWatching);
        Assert.Empty(updated.Favorites);
        Assert.Empty(updated.RecentEntries);
        Assert.Equal(SourcesOverviewState.Initial, updated.SourcesOverview);
        Assert.Equal(FavoritesPageState.Initial, updated.FavoritesPage);
        Assert.Equal(ProfilePageState.Initial, updated.Profile);

        Assert.Collection(
            transition.Effects,
            e => Assert.IsType<DisconnectServerEventsEffect>(e),
            e => Assert.IsType<LogoutEffect>(e),
            e => Assert.Equal(AppPage.Login, Assert.IsType<NavigateToPageEffect>(e).Page));
    }

    /// <summary>Prüft LogoutRequested: im Offline-Zustand → beendet die Sitzung ebenfalls.</summary>
    [Fact]
    public void LogoutRequested_Offline_AlsoEndsSession()
    {
        var model = Connected() with { ConnectionState = ConnectionState.Offline };

        var transition = AppUpdate.Update(model, new LogoutRequestedMessage());

        Assert.Equal(ConnectionState.NeedsLogin, transition.Model.ConnectionState);
        Assert.Contains(transition.Effects, e => e is LogoutEffect);
    }

    /// <summary>Prüft LogoutRequested: ohne Sitzung → unverändert.</summary>
    /// <param name="state">Verbindungszustand ohne aktive Sitzung.</param>
    [Theory]
    [InlineData(ConnectionState.NeedsLogin)]
    [InlineData(ConnectionState.NeedsServerSetup)]
    [InlineData(ConnectionState.Connecting)]
    public void LogoutRequested_WithoutSession_NoOp(ConnectionState state)
    {
        var model = new AppModel { ConnectionState = state };

        var transition = AppUpdate.Update(model, new LogoutRequestedMessage());

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }
}
