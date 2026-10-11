using VideoPlayer.Mvu;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// MVU-Übergänge der Playlists-Anzeige (Issue 43, Schritt 4): Übersicht
/// (eigene + öffentliche Playlists, reines Online-Feature ohne
/// Cache-Stand), Playlist-Auswahl mit Detail-Navigation, Wiederherstellung
/// der Detailseite sowie 1-basiertes Nachladen der Einträge in
/// Server-Reihenfolge.
/// </summary>
public class AppUpdateTests_Playlists
{
    private static AppModel Connected() => new()
    {
        ConnectionState = ConnectionState.Connected,
        IsStartupRunning = false
    };

    private static PlaylistListItem Item(long id, string name = "Liste") =>
        new() { PlaylistId = id, Name = name };

    private static PlaylistEntryItem Entry(long id) =>
        new() { EntryId = id, MediaId = 100 + id, MediaType = "movie", Title = $"Eintrag {id}" };

    // --- Übersichtsseite ---

    /// <summary>Prüft PlaylistsPageOpened: startet den Lauf der Übersicht.</summary>
    [Fact]
    public void PlaylistsPageOpened_StartsLoad()
    {
        var model = Connected();

        var (next, effects) = AppUpdate.Update(model, new PlaylistsPageOpenedMessage());

        Assert.True(next.PlaylistsPage.IsLoading);
        Assert.True(next.PlaylistsPage.Attempted);
        Assert.Null(next.PlaylistsPage.Error);
        var effect = Assert.Single(effects);
        Assert.IsType<LoadPlaylistsEffect>(effect);
    }

    /// <summary>Prüft PlaylistsPageOpened: wiederholtes Öffnen während eines Laufs ändert nichts.</summary>
    [Fact]
    public void PlaylistsPageOpened_WhileLoading_StaysUnchanged()
    {
        var model = Connected() with
        {
            PlaylistsPage = new PlaylistsPageState { IsLoading = true, Attempted = true }
        };

        var (next, effects) = AppUpdate.Update(model, new PlaylistsPageOpenedMessage());

        Assert.Same(model, next);
        Assert.Empty(effects);
    }

    /// <summary>Prüft PlaylistsPageOpened: ohne Serververbindung zeigt die Seite eine Fehleranzeige statt zu laden.</summary>
    [Fact]
    public void PlaylistsPageOpened_Offline_ShowsError()
    {
        var model = new AppModel
        {
            ConnectionState = ConnectionState.Offline,
            IsStartupRunning = false
        };

        var (next, effects) = AppUpdate.Update(model, new PlaylistsPageOpenedMessage());

        Assert.Empty(effects);
        Assert.True(next.PlaylistsPage.Attempted);
        Assert.False(next.PlaylistsPage.IsLoading);
        Assert.Equal(AppUpdate.PlaylistsOfflineError, next.PlaylistsPage.Error);
        Assert.True(next.PlaylistsPage.HasError);
    }

    /// <summary>Prüft PlaylistsLoaded: baut die flache Zeilenliste aus beiden Sektionen.</summary>
    [Fact]
    public void PlaylistsLoaded_FillsItems_WithSectionHeaders()
    {
        var model = Connected() with
        {
            PlaylistsPage = new PlaylistsPageState { IsLoading = true, Attempted = true }
        };

        var (next, effects) = AppUpdate.Update(
            model,
            new PlaylistsLoadedMessage([Item(7, "Eigene")], [Item(8, "Öffentlich")], Error: null));

        Assert.Empty(effects);
        var items = next.PlaylistsPage.Items;
        Assert.Equal(4, items.Count);
        Assert.True(items[0].IsHeader);
        Assert.Equal(PlaylistsPageState.OwnSectionTitle, items[0].Title);
        Assert.Equal(7, items[1].Playlist?.PlaylistId);
        Assert.True(items[2].IsHeader);
        Assert.Equal(PlaylistsPageState.PublicSectionTitle, items[2].Title);
        Assert.Equal(8, items[3].Playlist?.PlaylistId);
        Assert.True(next.PlaylistsPage.HasItems);
        Assert.False(next.PlaylistsPage.IsLoading);
    }

    /// <summary>Prüft PlaylistsLoaded: Fehler bleibt in der Fehleranzeige, bisherige Liste bleibt.</summary>
    [Fact]
    public void PlaylistsLoaded_WithError_KeepsExistingItems()
    {
        var model = Connected() with
        {
            PlaylistsPage = new PlaylistsPageState
            {
                Items = PlaylistsPageState.BuildItems([Item(7)], []),
                IsLoading = true,
                Attempted = true
            }
        };

        var (next, _) = AppUpdate.Update(
            model,
            new PlaylistsLoadedMessage([], [], AppUpdate.PlaylistsLoadError));

        Assert.Equal(2, next.PlaylistsPage.Items.Count);
        Assert.Equal(AppUpdate.PlaylistsLoadError, next.PlaylistsPage.Error);
        Assert.False(next.PlaylistsPage.IsLoading);
    }

    /// <summary>Prüft PlaylistSelected: belegt den Detailzustand und startet Navigation + Erstladung.</summary>
    [Fact]
    public void PlaylistSelected_Navigates_AndStartsEntryLoad()
    {
        var item = new PlaylistListItem
        {
            PlaylistId = 7,
            Name = "Serien-Abend",
            Description = "Beschreibung",
            SortMode = "Manual",
            CoverPictureId = 44
        };
        var model = Connected();

        var (next, effects) = AppUpdate.Update(model, new PlaylistSelectedMessage(item));

        var details = next.PlaylistDetails;
        Assert.True(details.IsActive);
        Assert.Equal(7, details.PlaylistId);
        Assert.Equal("Serien-Abend", details.Name);
        Assert.Equal("Beschreibung", details.Description);
        Assert.Equal(44, details.CoverPictureId);
        Assert.True(details.IsLoading);
        Assert.Equal(2, effects.Count);
        Assert.IsType<NavigateToPlaylistDetailsEffect>(effects[0]);
        var load = Assert.IsType<LoadPlaylistEntriesEffect>(effects[1]);
        Assert.Equal(7, load.PlaylistId);
        Assert.Equal(1, load.Page);
        Assert.False(load.Append);
    }

    /// <summary>Prüft PlaylistSelected: offline bleibt die Auswahl wirkungslos (kein Fehlernavigationssprung).</summary>
    [Fact]
    public void PlaylistSelected_Offline_StaysUnchanged()
    {
        var model = new AppModel
        {
            ConnectionState = ConnectionState.Offline,
            IsStartupRunning = false
        };

        var (next, effects) = AppUpdate.Update(model, new PlaylistSelectedMessage(Item(7)));

        Assert.Same(model, next);
        Assert.Empty(effects);
    }

    // --- Detailseite ---

    /// <summary>Prüft PlaylistDetailsOpened: Wiederherstellung lädt Kopfdaten und erste Eintragsseite.</summary>
    [Fact]
    public void PlaylistDetailsOpened_UnknownPlaylist_LoadsDetailsAndEntries()
    {
        var model = Connected();

        var (next, effects) = AppUpdate.Update(model, new PlaylistDetailsOpenedMessage(9, "Wiederhergestellt"));

        var details = next.PlaylistDetails;
        Assert.Equal(9, details.PlaylistId);
        Assert.Equal("Wiederhergestellt", details.Name);
        Assert.True(details.IsLoading);
        Assert.Equal(2, effects.Count);
        var detailsLoad = Assert.IsType<LoadPlaylistDetailsEffect>(effects[0]);
        Assert.Equal(9, detailsLoad.PlaylistId);
        var entriesLoad = Assert.IsType<LoadPlaylistEntriesEffect>(effects[1]);
        Assert.Equal(9, entriesLoad.PlaylistId);
        Assert.Equal(1, entriesLoad.Page);
        Assert.False(entriesLoad.Append);
    }

    /// <summary>Prüft PlaylistDetailsOpened: Rückkehr auf dieselbe Playlist lässt den Stand erhalten.</summary>
    [Fact]
    public void PlaylistDetailsOpened_SamePlaylist_StaysUnchanged()
    {
        var model = Connected() with
        {
            PlaylistDetails = new PlaylistDetailsState
            {
                PlaylistId = 7,
                Name = "Liste",
                Entries = [Entry(1)],
                Attempted = true
            }
        };

        var (next, effects) = AppUpdate.Update(model, new PlaylistDetailsOpenedMessage(7, "Liste"));

        Assert.Same(model, next);
        Assert.Empty(effects);
    }

    /// <summary>Prüft PlaylistDetailsLoaded: nicht-fataler Nachschlag befüllt nur die Kopfdaten.</summary>
    [Fact]
    public void PlaylistDetailsLoaded_FillsHeader_OnlyForSamePlaylist()
    {
        var model = Connected() with
        {
            PlaylistDetails = new PlaylistDetailsState { PlaylistId = 7, Name = "alt" }
        };
        var info = new PlaylistListItem
        {
            PlaylistId = 7,
            Name = "Neuer Name",
            Description = "Beschreibung",
            SortMode = "Manual",
            CoverPictureId = 44
        };

        var (next, effects) = AppUpdate.Update(model, new PlaylistDetailsLoadedMessage(7, info));

        Assert.Empty(effects);
        Assert.Equal("Neuer Name", next.PlaylistDetails.Name);
        Assert.Equal(44, next.PlaylistDetails.CoverPictureId);

        // Antwort einer anderen Playlist wird verworfen.
        var (same, sameEffects) = AppUpdate.Update(model, new PlaylistDetailsLoadedMessage(8, info));
        Assert.Same(model, same);
        Assert.Empty(sameEffects);
    }

    /// <summary>Prüft PlaylistEntriesLoaded: ersetzt bei Seite 1, hängt Folgeseiten an und zählt NextPage weiter.</summary>
    [Fact]
    public void PlaylistEntriesLoaded_Replaces_ThenAppends()
    {
        var model = Connected() with
        {
            PlaylistDetails = new PlaylistDetailsState
            {
                PlaylistId = 7,
                IsLoading = true,
                HasMore = true
            }
        };

        var (loaded, _) = AppUpdate.Update(
            model,
            new PlaylistEntriesLoadedMessage(7, 1, [Entry(1), Entry(2)], HasMore: true, TotalCount: 4, Append: false, Error: null));

        Assert.Equal(2, loaded.PlaylistDetails.Entries.Count);
        Assert.Equal(2, loaded.PlaylistDetails.NextPage);
        Assert.True(loaded.PlaylistDetails.HasMore);
        Assert.Equal(4, loaded.PlaylistDetails.TotalCount);
        Assert.False(loaded.PlaylistDetails.IsLoading);

        var appended = loaded with
        {
            PlaylistDetails = loaded.PlaylistDetails with { IsLoadingMore = true }
        };
        var (next, _) = AppUpdate.Update(
            appended,
            new PlaylistEntriesLoadedMessage(7, 2, [Entry(3), Entry(4)], HasMore: false, TotalCount: 4, Append: true, Error: null));

        Assert.Equal([1L, 2L, 3L, 4L], next.PlaylistDetails.Entries.Select(e => e.EntryId));
        Assert.Equal(3, next.PlaylistDetails.NextPage);
        Assert.False(next.PlaylistDetails.HasMore);
        Assert.False(next.PlaylistDetails.IsLoadingMore);
    }

    /// <summary>Prüft PlaylistEntriesLoaded: veraltete Anhänge-Antworten (falsche Seite) werden verworfen.</summary>
    [Fact]
    public void PlaylistEntriesLoaded_StaleAppend_IsDiscarded()
    {
        var model = Connected() with
        {
            PlaylistDetails = new PlaylistDetailsState
            {
                PlaylistId = 7,
                Entries = [Entry(1)],
                NextPage = 2,
                HasMore = true,
                Attempted = true
            }
        };

        var (next, effects) = AppUpdate.Update(
            model,
            new PlaylistEntriesLoadedMessage(7, 3, [Entry(3)], HasMore: true, TotalCount: 3, Append: true, Error: null));

        Assert.Same(model, next);
        Assert.Empty(effects);
    }

    /// <summary>Prüft PlaylistEntriesLoaded: Antworten einer verlassenen Playlist werden verworfen.</summary>
    [Fact]
    public void PlaylistEntriesLoaded_WrongPlaylist_IsDiscarded()
    {
        var model = Connected() with
        {
            PlaylistDetails = new PlaylistDetailsState { PlaylistId = 7, IsLoading = true }
        };

        var (next, effects) = AppUpdate.Update(
            model,
            new PlaylistEntriesLoadedMessage(8, 1, [Entry(1)], HasMore: false, TotalCount: 1, Append: false, Error: null));

        Assert.Same(model, next);
        Assert.Empty(effects);
    }

    /// <summary>Prüft PlaylistEntriesLoaded: Fehler beim Anhängen lässt die Liste stehen, Fehler des Erstladens → Fehleranzeige.</summary>
    [Fact]
    public void PlaylistEntriesLoaded_WithError_BehavesByAppend()
    {
        var withItems = Connected() with
        {
            PlaylistDetails = new PlaylistDetailsState
            {
                PlaylistId = 7,
                Entries = [Entry(1)],
                NextPage = 2,
                HasMore = true,
                Attempted = true,
                IsLoadingMore = true
            }
        };

        var (appendFailed, _) = AppUpdate.Update(
            withItems,
            new PlaylistEntriesLoadedMessage(7, 2, [], HasMore: false, TotalCount: 0, Append: true, Error: "fehlgeschlagen"));

        Assert.Single(appendFailed.PlaylistDetails.Entries);
        Assert.False(appendFailed.PlaylistDetails.IsLoadingMore);
        // Anhänge-Fehler ist kein Seiten-Fehlerzustand — Liste bleibt, nächster Scroll versucht erneut.
        Assert.Null(appendFailed.PlaylistDetails.Error);
        Assert.True(appendFailed.PlaylistDetails.HasMore);

        var empty = Connected() with
        {
            PlaylistDetails = new PlaylistDetailsState { PlaylistId = 7, IsLoading = true }
        };
        var (initialFailed, _) = AppUpdate.Update(
            empty,
            new PlaylistEntriesLoadedMessage(7, 1, [], HasMore: false, TotalCount: 0, Append: false, Error: AppUpdate.PlaylistEntriesLoadError));

        Assert.Equal(AppUpdate.PlaylistEntriesLoadError, initialFailed.PlaylistDetails.Error);
        Assert.True(initialFailed.PlaylistDetails.HasError);
    }

    /// <summary>Prüft PlaylistEntriesNextPageRequested: fordert die Folgeseite an, solange HasMore und kein Lauf aktiv.</summary>
    [Fact]
    public void PlaylistEntriesNextPageRequested_RequestsNextPage_WithGuards()
    {
        var model = Connected() with
        {
            PlaylistDetails = new PlaylistDetailsState
            {
                PlaylistId = 7,
                Entries = [Entry(1)],
                NextPage = 2,
                HasMore = true,
                Attempted = true
            }
        };

        var (next, effects) = AppUpdate.Update(model, new PlaylistEntriesNextPageRequestedMessage());

        Assert.True(next.PlaylistDetails.IsLoadingMore);
        var effect = Assert.Single(effects);
        var load = Assert.IsType<LoadPlaylistEntriesEffect>(effect);
        Assert.Equal(2, load.Page);
        Assert.True(load.Append);

        // Ende erreicht oder Lauf aktiv → keine weitere Anfrage.
        var done = model with { PlaylistDetails = model.PlaylistDetails with { HasMore = false } };
        var (same, none) = AppUpdate.Update(done, new PlaylistEntriesNextPageRequestedMessage());
        Assert.Same(done, same);
        Assert.Empty(none);

        var busy = model with { PlaylistDetails = model.PlaylistDetails with { IsLoading = true } };
        var (sameBusy, noneBusy) = AppUpdate.Update(busy, new PlaylistEntriesNextPageRequestedMessage());
        Assert.Same(busy, sameBusy);
        Assert.Empty(noneBusy);
    }

    /// <summary>Prüft PlaylistEntriesRetryRequested: leere Liste → kompletter Neu-Lauf, belegte Liste → Folgeseiten-Neuversuch.</summary>
    [Fact]
    public void PlaylistEntriesRetryRequested_ReloadsOrRetriesAppend()
    {
        var failed = Connected() with
        {
            PlaylistDetails = new PlaylistDetailsState
            {
                PlaylistId = 7,
                Attempted = true,
                Error = AppUpdate.PlaylistEntriesLoadError
            }
        };

        var (next, effects) = AppUpdate.Update(failed, new PlaylistEntriesRetryRequestedMessage());

        Assert.True(next.PlaylistDetails.IsLoading);
        Assert.Null(next.PlaylistDetails.Error);
        Assert.Equal(1, Assert.IsType<LoadPlaylistEntriesEffect>(Assert.Single(effects)).Page);

        var appendFailed = failed with
        {
            PlaylistDetails = failed.PlaylistDetails with
            {
                Entries = [Entry(1)],
                NextPage = 2,
                HasMore = true
            }
        };
        var (retry, retryEffects) = AppUpdate.Update(appendFailed, new PlaylistEntriesRetryRequestedMessage());
        var load = Assert.IsType<LoadPlaylistEntriesEffect>(Assert.Single(retryEffects));
        Assert.Equal(2, load.Page);
        Assert.True(load.Append);
        Assert.True(retry.PlaylistDetails.IsLoadingMore);
    }

    /// <summary>Prüft PlaylistEntrySelected: navigiert zur Medien-Detailseite mit dem Elternkontext als Hint.</summary>
    [Fact]
    public void PlaylistEntrySelected_NavigatesToMediaDetails()
    {
        var item = new PlaylistEntryItem
        {
            EntryId = 1,
            MediaId = 55,
            MediaType = "tvshowepisode",
            Title = "Folge 4",
            ParentMediaType = "tvshow",
            ParentMediaId = 12
        };
        var model = Connected();

        var (_, effects) = AppUpdate.Update(model, new PlaylistEntrySelectedMessage(item));

        var effect = Assert.Single(effects);
        var navigate = Assert.IsType<NavigateToMediaDetailsEffect>(effect);
        Assert.Equal("tvshowepisode", navigate.MediaType);
        Assert.Equal(55, navigate.MediaId);
        Assert.Equal(12, navigate.ShowId);
        Assert.Null(navigate.SeasonId);
    }
}
