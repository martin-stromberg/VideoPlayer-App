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

    /// <summary>Prüft StartupRequested: lädt die Home-Listen nach der Storage-Initialisierung.</summary>
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

    /// <summary>Prüft HomeListsLoaded: setzt die Listen und Flags.</summary>
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

    /// <summary>Prüft HomeListsLoaded: bei leeren Listen → setzt die Flags zurück.</summary>
    [Fact]
    public void HomeListsLoaded_Empty_ResetsFlags()
    {
        var transition = AppUpdate.Update(ConnectedModel(), new HomeListsLoadedMessage(HomeLists.Empty));

        Assert.False(transition.Model.HasHomeContent);
    }

    /// <summary>Prüft CachedListsChanged: lädt die Home-Listen neu.</summary>
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

    /// <summary>Prüft ServerEventReceived: bei vollständiger Sequenz → aktualisiert die Listen.</summary>
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

    /// <summary>Prüft ServerEventReceived: bildet die Ereignisart auf den passenden Sync-Effekt ab.</summary>
    /// <param name="kind">Die Art.</param>
    /// <param name="expectedEffect">Der erwartete Effekttyp.</param>
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

    /// <summary>Prüft MediaItemSelected: navigiert zur Detailseite.</summary>
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

    /// <summary>Prüft MediaItemSelected: ohne identifizierbare Medien → keine Navigation.</summary>
    /// <param name="id">Die ID.</param>
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

    /// <summary>Prüft MediaItemSelected: ohne Medientyp → keine Navigation.</summary>
    [Fact]
    public void MediaItemSelected_WithoutMediaType_NoNavigation()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new MediaItemSelectedMessage(Item(7, "", "Ohne Typ")));

        Assert.Empty(transition.Effects);
    }
}
