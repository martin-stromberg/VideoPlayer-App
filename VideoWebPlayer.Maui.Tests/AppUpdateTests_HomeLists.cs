using VideoPlayer.Mvu;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Startseite mit gecachten Medienlisten (Schritt 7): Cache-First beim
/// Start, Übernahme der Listen ins Model, Neu-Laden nach Synchronisation
/// und Server-Ereignissen sowie Navigation bei Auswahl eines Eintrags.
/// </summary>
public class AppUpdateTests_HomeLists
{
    private static HomeMediaItem Item(long id = 5, string mediaType = "movie", string title = "Film") =>
        new() { Id = id, MediaType = mediaType, Title = title };

    private static AppModel ConnectedModel() => new()
    {
        ConnectionState = ConnectionState.Connected,
        ActiveServerAddress = "http://nas/videoplayer/",
        CurrentPage = AppPage.Home,
        IsOnline = true,
        IsStartupRunning = false
    };

    [Fact]
    public void StartupRequested_LoadsHomeListsAfterStorageInit()
    {
        var transition = AppUpdate.Update(new AppModel(), new StartupRequestedMessage());

        // Cache-First: die Listen werden unmittelbar nach der
        // Ablage-Initialisierung gelesen — ohne Serverantwort abzuwarten.
        var initIndex = transition.Effects.ToList().FindIndex(e => e is InitializeStorageEffect);
        var loadIndex = transition.Effects.ToList().FindIndex(e => e is LoadHomeListsEffect);
        Assert.True(initIndex >= 0);
        Assert.True(loadIndex > initIndex);
    }

    [Fact]
    public void HomeListsLoaded_SetsListsAndFlags()
    {
        var lists = new HomeLists
        {
            ContinueWatching = [Item(1, "episode", "Weiter")],
            Favorites = [Item(2, "tvshow", "Serie"), Item(3, "movie", "Lieblingsfilm")],
            Recent = [Item(4, "movie", "Neu")]
        };

        var transition = AppUpdate.Update(ConnectedModel(), new HomeListsLoadedMessage(lists));

        var item = Assert.Single(transition.Model.ContinueWatching);
        Assert.Equal(1, item.Id);
        Assert.Equal(2, transition.Model.Favorites.Count);
        Assert.Single(transition.Model.RecentEntries);
        Assert.True(transition.Model.HasContinueWatching);
        Assert.True(transition.Model.HasFavorites);
        Assert.True(transition.Model.HasRecentEntries);
        Assert.True(transition.Model.HasHomeContent);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void HomeListsLoaded_Empty_ResetsFlags()
    {
        var transition = AppUpdate.Update(ConnectedModel(), new HomeListsLoadedMessage(HomeLists.Empty));

        Assert.False(transition.Model.HasHomeContent);
    }

    [Fact]
    public void CachedListsChanged_ReloadsHomeLists()
    {
        // Der Effekt-Handler meldet nach jeder Teil-Synchronisation
        // (Server-Ereignis) diese Nachricht — die Anzeige lädt den
        // aktualisierten Cache-Stand.
        var model = ConnectedModel();
        var transition = AppUpdate.Update(model, new CachedListsChangedMessage());

        var effect = Assert.Single(transition.Effects);
        Assert.IsType<LoadHomeListsEffect>(effect);
        // Das Model bleibt unverändert — nur ein Neu-Laden wird ausgelöst.
        Assert.Same(model, transition.Model);
    }

    [Fact]
    public void ServerEventReceived_FullSequence_RefreshesLists()
    {
        // Favoriten-Ereignis → Teil-Synchronisation; der Handler meldet
        // danach CachedListsChanged → die Listen werden aus dem
        // aktualisierten Cache neu geladen.
        var syncTransition = AppUpdate.Update(
            ConnectedModel(),
            new ServerEventReceivedMessage(ServerEventKind.FavoritesChanged));

        Assert.Contains(syncTransition.Effects, e => e is SyncFavoritesEffect);

        var reloadTransition = AppUpdate.Update(
            syncTransition.Model,
            new CachedListsChangedMessage());

        var effect = Assert.Single(reloadTransition.Effects);
        Assert.IsType<LoadHomeListsEffect>(effect);
    }

    [Theory]
    [InlineData(ServerEventKind.ContinueWatchingUpdated, typeof(SyncContinueWatchingEffect))]
    [InlineData(ServerEventKind.FavoritesChanged, typeof(SyncFavoritesEffect))]
    [InlineData(ServerEventKind.NewVideosScanned, typeof(SyncMetadataEffect))]
    public void ServerEventReceived_MapsToPartialSync(ServerEventKind kind, Type expectedEffect)
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new ServerEventReceivedMessage(kind));

        Assert.Contains(transition.Effects, e => e.GetType() == expectedEffect);
    }

    [Fact]
    public void MediaItemSelected_NavigatesToDetails()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new MediaItemSelectedMessage(Item(42, "tvshow", "Meine Serie")));

        var effect = Assert.Single(transition.Effects);
        var navigation = Assert.IsType<NavigateToMediaDetailsEffect>(effect);
        Assert.Equal("tvshow", navigation.MediaType);
        Assert.Equal(42, navigation.MediaId);
        Assert.Equal("Meine Serie", navigation.Title);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MediaItemSelected_WithoutIdentifiableMedia_NoNavigation(long id)
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new MediaItemSelectedMessage(Item(id, "movie", "Unbekannt")));

        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void MediaItemSelected_WithoutMediaType_NoNavigation()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new MediaItemSelectedMessage(Item(7, "", "Ohne Typ")));

        Assert.Empty(transition.Effects);
    }
}
