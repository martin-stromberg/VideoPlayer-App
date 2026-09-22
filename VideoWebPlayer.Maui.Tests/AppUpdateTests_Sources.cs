using VideoPlayer.Mvu;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// MVU-Übergänge des Quellen-Browsing (Schritt 8): Quellenliste mit
/// Offline-Sonderbehandlung, Quellenwahl mit Navigation + Erstladung,
/// Genre-Filter, Suche und seitenweises Nachladen.
/// </summary>
public class AppUpdateTests_Sources
{
    private static AppModel Connected() => new()
    {
        ConnectionState = ConnectionState.Connected,
        IsStartupRunning = false
    };

    private static SourceItem Source(long id = 7, string name = "Filme") =>
        new() { Id = id, Name = name };

    private static SourceMediaItem Item(long id) =>
        new() { Id = id, Title = $"Eintrag {id}", MediaType = "moviecollection" };

    // --- Quellenliste (Navigationsmenü) ---

    /// <summary>Prüft EnterConnected: startet das Quellen-Laden fürs Menü und behält bisherige Einträge.</summary>
    [Fact]
    public void EnterConnected_StartsSourcesLoad()
    {
        var model = new AppModel
        {
            ConnectionState = ConnectionState.Connecting,
            ActiveServerAddress = "http://nas/videoplayer/",
            CurrentPage = AppPage.Loading,
            SourcesOverview = new SourcesOverviewState
            {
                Attempted = true,
                Sources = [Source(3, "Archiv")]
            }
        };

        var transition = AppUpdate.Update(
            model,
            new AutoLoginCompletedMessage(AuthAttemptStatus.Succeeded, "martin"));

        Assert.True(transition.Model.SourcesOverview.IsLoading);
        Assert.Null(transition.Model.SourcesOverview.Error);
        // Die bisherigen Einträge bleiben während des Ladens sichtbar.
        var retained = Assert.Single(transition.Model.SourcesOverview.Sources);
        Assert.Equal(3, retained.Id);
        Assert.Contains(transition.Effects, e => e is LoadSourcesEffect);
    }

    /// <summary>Prüft SynchronizationCompleted: bei <c>Connected</c> → lädt die Menü-Quellen neu.</summary>
    [Fact]
    public void SynchronizationCompleted_WhenConnected_ReloadsSources()
    {
        var transition = AppUpdate.Update(
            Connected(),
            new SynchronizationCompletedMessage(new SyncResult(0, 0, true, true, true)));

        Assert.True(transition.Model.SourcesOverview.IsLoading);
        Assert.Contains(transition.Effects, e => e is LoadSourcesEffect);
        Assert.Contains(transition.Effects, e => e is LoadHomeListsEffect);
    }

    /// <summary>Prüft SynchronizationCompleted: bei laufendem Quellen-Lauf → kein zweiter Ladevorgang.</summary>
    [Fact]
    public void SynchronizationCompleted_WhileSourcesLoading_NoDuplicate()
    {
        var model = Connected() with
        {
            SourcesOverview = SourcesOverviewState.Initial with { IsLoading = true }
        };

        var transition = AppUpdate.Update(
            model,
            new SynchronizationCompletedMessage(new SyncResult(0, 0, true, true, true)));

        Assert.DoesNotContain(transition.Effects, e => e is LoadSourcesEffect);
    }

    /// <summary>Prüft CachedListsChanged: bei <c>Connected</c> → lädt die Menü-Quellen neu.</summary>
    [Fact]
    public void CachedListsChanged_WhenConnected_ReloadsSources()
    {
        var transition = AppUpdate.Update(Connected(), new CachedListsChangedMessage());

        Assert.True(transition.Model.SourcesOverview.IsLoading);
        Assert.Contains(transition.Effects, e => e is LoadSourcesEffect);
        Assert.Contains(transition.Effects, e => e is LoadHomeListsEffect);
    }

    /// <summary>Prüft CachedListsChanged: bei laufendem Quellen-Lauf → kein zweiter Ladevorgang.</summary>
    [Fact]
    public void CachedListsChanged_WhileSourcesLoading_NoDuplicate()
    {
        var model = Connected() with
        {
            SourcesOverview = SourcesOverviewState.Initial with { IsLoading = true }
        };

        var transition = AppUpdate.Update(model, new CachedListsChangedMessage());

        Assert.DoesNotContain(transition.Effects, e => e is LoadSourcesEffect);
    }

    /// <summary>Prüft CachedListsChanged: bei <c>NotConnected</c> → kein Quellen-Ladevorgang.</summary>
    [Fact]
    public void CachedListsChanged_WhenNotConnected_NoSourcesLoad()
    {
        var model = Connected() with
        {
            ConnectionState = ConnectionState.Offline,
            IsOnline = false
        };

        var transition = AppUpdate.Update(model, new CachedListsChangedMessage());

        Assert.DoesNotContain(transition.Effects, e => e is LoadSourcesEffect);
    }

    /// <summary>Prüft SourcesLoaded: außerhalb von <c>Connected</c> → wird verworfen.</summary>
    /// <param name="state">Der Verbindungszustand.</param>
    [Theory]
    [InlineData(ConnectionState.Offline)]
    [InlineData(ConnectionState.NeedsLogin)]
    public void SourcesLoaded_WhenNotConnected_IsIgnored(ConnectionState state)
    {
        // Verspätete Antwort eines abgelösten Ladevorgangs — das
        // Offline-/Abmelde-Menü darf nicht wieder befüllt werden.
        var model = Connected() with { ConnectionState = state };

        var transition = AppUpdate.Update(
            model,
            new SourcesLoadedMessage([Source(1, "Filme")], null));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft SourcesLoaded: bei <c>SetsList</c> → eine leere Liste ist unterscheidbar.</summary>
    [Fact]
    public void SourcesLoaded_SetsList_AndEmptyIsDistinguishable()
    {
        var transition = AppUpdate.Update(
            Connected(),
            new SourcesLoadedMessage([Source(1, "Filme"), Source(2, "Serien")], null));

        Assert.Equal(2, transition.Model.SourcesOverview.Sources.Count);
        Assert.True(transition.Model.SourcesOverview.Attempted);
        Assert.False(transition.Model.SourcesOverview.IsLoading);
        Assert.False(transition.Model.SourcesOverview.HasError);
        Assert.False(transition.Model.SourcesOverview.ShowEmpty);

        var empty = AppUpdate.Update(Connected(), new SourcesLoadedMessage([], null));
        Assert.True(empty.Model.SourcesOverview.ShowEmpty);
    }

    /// <summary>Prüft SourcesLoaded: bei Fehler → setzt Fehlerzustand.</summary>
    [Fact]
    public void SourcesLoaded_Error_SetsErrorState()
    {
        var transition = AppUpdate.Update(
            Connected(),
            new SourcesLoadedMessage([], "Die Quellen konnten nicht geladen werden."));

        Assert.True(transition.Model.SourcesOverview.HasError);
        Assert.False(transition.Model.SourcesOverview.ShowEmpty);
    }

    /// <summary>Prüft SourcesLoaded: bei Fehler mit Bestand → bleiben die bisherigen Einträge erhalten.</summary>
    [Fact]
    public void SourcesLoaded_Error_KeepsExistingSources()
    {
        // Der Effekt-Handler dispatcht im Fehlerfall eine leere Liste —
        // ein transienter Ladefehlschlag (z. B. Neuversuch aus dem Menü)
        // darf die bisherigen Menüeinträge nicht abräumen: das Menü
        // zeigt die Retry-Zeile über den weiterhin antippbaren
        // Einträgen.
        var model = Connected() with
        {
            SourcesOverview = new SourcesOverviewState
            {
                Attempted = true,
                IsLoading = true,
                Sources = [Source(3, "Archiv"), Source(4, "Filme")]
            }
        };

        var transition = AppUpdate.Update(
            model,
            new SourcesLoadedMessage([], "Die Quellen konnten nicht geladen werden."));

        var overview = transition.Model.SourcesOverview;
        Assert.True(overview.Attempted);
        Assert.True(overview.HasError);
        Assert.False(overview.IsLoading);
        Assert.Equal(2, overview.Sources.Count);
        Assert.Equal(3, overview.Sources[0].Id);
        Assert.Equal(4, overview.Sources[1].Id);
    }

    /// <summary>Prüft SourcesLoaded: ohne Fehler ersetzt die Antwort den bisherigen Bestand.</summary>
    [Fact]
    public void SourcesLoaded_Success_ReplacesExistingSources()
    {
        var model = Connected() with
        {
            SourcesOverview = new SourcesOverviewState
            {
                Attempted = true,
                IsLoading = true,
                Sources = [Source(3, "Archiv")]
            }
        };

        var transition = AppUpdate.Update(
            model,
            new SourcesLoadedMessage([Source(9, "Neu")], null));

        var overview = transition.Model.SourcesOverview;
        Assert.False(overview.HasError);
        var retained = Assert.Single(overview.Sources);
        Assert.Equal(9, retained.Id);
    }

    // --- Manueller Neuversuch aus dem Menü (Fehler-Eintrag) ---

    /// <summary>Prüft SourcesReloadRequested: bei <c>Connected</c> → startet den Ladevorgang erneut und löscht den Fehler.</summary>
    [Fact]
    public void SourcesReloadRequested_WhenConnected_ReloadsAndClearsError()
    {
        var model = Connected() with
        {
            SourcesOverview = new SourcesOverviewState
            {
                Attempted = true,
                Error = "Die Quellen konnten nicht geladen werden."
            }
        };

        var transition = AppUpdate.Update(model, new SourcesReloadRequestedMessage());

        Assert.True(transition.Model.SourcesOverview.IsLoading);
        Assert.Null(transition.Model.SourcesOverview.Error);
        Assert.IsType<LoadSourcesEffect>(Assert.Single(transition.Effects));
    }

    /// <summary>Prüft SourcesReloadRequested: außerhalb von <c>Connected</c> → wird ignoriert.</summary>
    /// <param name="state">Der Verbindungszustand.</param>
    [Theory]
    [InlineData(ConnectionState.Offline)]
    [InlineData(ConnectionState.NeedsLogin)]
    public void SourcesReloadRequested_WhenNotConnected_IsIgnored(ConnectionState state)
    {
        // Der Neuversuch-Eintrag existiert nur im verbundenen Menü —
        // verspätete Dispatches dürfen offline keinen Lauf auslösen.
        var model = Connected() with
        {
            ConnectionState = state,
            SourcesOverview = new SourcesOverviewState
            {
                Attempted = true,
                Error = "Fehler"
            }
        };

        var transition = AppUpdate.Update(model, new SourcesReloadRequestedMessage());

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft SourcesReloadRequested: bei laufendem Ladevorgang → kein zweiter Lauf.</summary>
    [Fact]
    public void SourcesReloadRequested_WhileLoading_NoDuplicate()
    {
        var model = Connected() with
        {
            SourcesOverview = SourcesOverviewState.Initial with { IsLoading = true }
        };

        var transition = AppUpdate.Update(model, new SourcesReloadRequestedMessage());

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    // --- Quellenwahl / Browser ---

    /// <summary>Prüft SourceSelected: navigiert und startet das initiale Laden.</summary>
    [Fact]
    public void SourceSelected_NavigatesAndStartsInitialLoad()
    {
        var transition = AppUpdate.Update(Connected(), new SourceSelectedMessage(Source(5, "Filme")));

        var browser = transition.Model.SourceBrowser;
        Assert.True(browser.IsActive);
        Assert.Equal(5, browser.SourceId);
        Assert.Equal("Filme", browser.SourceName);
        Assert.True(browser.IsLoading);
        Assert.True(browser.HasMore);

        Assert.Equal(3, transition.Effects.Count);
        var navigation = Assert.IsType<NavigateToSourceOverviewEffect>(transition.Effects[0]);
        Assert.Equal(5, navigation.SourceId);
        var genres = Assert.IsType<LoadSourceGenresEffect>(transition.Effects[1]);
        Assert.Equal(5, genres.SourceId);
        var items = Assert.IsType<LoadSourceItemsEffect>(transition.Effects[2]);
        Assert.Equal(5, items.SourceId);
        Assert.Equal(0, items.Page);
        Assert.False(items.Append);
        Assert.Null(items.GenreId);
        Assert.Null(items.SearchText);
    }

    /// <summary>Prüft SourceBrowserOpened: bei gleicher Quelle → behält den Zustand.</summary>
    [Fact]
    public void SourceBrowserOpened_SameSource_KeepsState()
    {
        var model = Connected() with
        {
            SourceBrowser = new SourceBrowserState { SourceId = 5, Attempted = true }
        };

        var transition = AppUpdate.Update(model, new SourceBrowserOpenedMessage(5, "Filme"));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft SourceBrowserOpened: bei <c>Restore</c> → lädt Genres und die erste Seite.</summary>
    [Fact]
    public void SourceBrowserOpened_Restore_LoadsGenresAndFirstPage()
    {
        var transition = AppUpdate.Update(Connected(), new SourceBrowserOpenedMessage(9, "Serien"));

        Assert.Equal(9, transition.Model.SourceBrowser.SourceId);
        Assert.Contains(transition.Effects, e => e is LoadSourceGenresEffect { SourceId: 9 });
        Assert.Contains(transition.Effects, e => e is LoadSourceItemsEffect { SourceId: 9, Page: 0 });
        Assert.DoesNotContain(transition.Effects, e => e is NavigateToSourceOverviewEffect);
    }

    /// <summary>Prüft SourceGenresLoaded: bei falscher Quelle → wird verworfen.</summary>
    [Fact]
    public void SourceGenresLoaded_WrongSource_IsDiscarded()
    {
        var model = Connected() with
        {
            SourceBrowser = new SourceBrowserState { SourceId = 5 }
        };

        var transition = AppUpdate.Update(
            model,
            new SourceGenresLoadedMessage(6, [new SourceGenreItem { Id = 0, Name = "Alle" }]));

        Assert.Same(model, transition.Model);
    }

    // --- Filter / Suche ---

    /// <summary>Prüft GenreSelected: bei <c>ReloadsFromFirstPage</c> → lädt mit Genrefilter neu.</summary>
    [Fact]
    public void GenreSelected_ReloadsFromFirstPage_WithGenreFilter()
    {
        var model = Connected() with
        {
            SourceBrowser = new SourceBrowserState
            {
                SourceId = 5,
                NextPage = 3,
                Items = [Item(1)],
                Genres =
                [
                    new SourceGenreItem { Id = 0, Name = "Alle", IsSelected = true },
                    new SourceGenreItem { Id = 4, Name = "Komödie" }
                ]
            }
        };

        var transition = AppUpdate.Update(model, new SourceGenreSelectedMessage(4));

        var browser = transition.Model.SourceBrowser;
        Assert.Equal(4, browser.SelectedGenreId);
        Assert.Empty(browser.Items);
        Assert.Equal(0, browser.NextPage);
        Assert.True(browser.IsLoading);
        Assert.True(browser.Genres.Single(g => g.Id == 4).IsSelected);

        var effect = Assert.IsType<LoadSourceItemsEffect>(Assert.Single(transition.Effects));
        Assert.Equal(4, effect.GenreId);
        Assert.Equal(0, effect.Page);
        Assert.False(effect.Append);
    }

    /// <summary>Prüft SearchSubmitted: bei <c>ReloadsFromFirstPage</c> → lädt mit dem Suchtext neu.</summary>
    [Fact]
    public void SearchSubmitted_ReloadsFromFirstPage_WithSearchText()
    {
        var model = Connected() with
        {
            SourceBrowser = new SourceBrowserState { SourceId = 5, NextPage = 2, Items = [Item(1)] }
        };

        var transition = AppUpdate.Update(model, new SourceSearchSubmittedMessage("  Matrix  "));

        Assert.Equal("Matrix", transition.Model.SourceBrowser.SearchText);
        var effect = Assert.IsType<LoadSourceItemsEffect>(Assert.Single(transition.Effects));
        Assert.Equal("Matrix", effect.SearchText);
        Assert.Equal(0, effect.Page);
    }

    /// <summary>Prüft SearchSubmitted: bei gleichem Text → lädt nicht neu.</summary>
    [Fact]
    public void SearchSubmitted_SameText_NoReload()
    {
        var model = Connected() with
        {
            SourceBrowser = new SourceBrowserState { SourceId = 5, SearchText = "Matrix" }
        };

        var transition = AppUpdate.Update(model, new SourceSearchSubmittedMessage("Matrix"));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    // --- Nachladen ---

    /// <summary>Prüft NextPageRequested: lädt die nächste Seite und hängt sie an.</summary>
    [Fact]
    public void NextPageRequested_LoadsAppendedPage()
    {
        var model = Connected() with
        {
            SourceBrowser = new SourceBrowserState
            {
                SourceId = 5,
                NextPage = 1,
                HasMore = true,
                Items = [Item(1)],
                Attempted = true
            }
        };

        var transition = AppUpdate.Update(model, new SourceItemsNextPageRequestedMessage());

        Assert.True(transition.Model.SourceBrowser.IsLoadingMore);
        var effect = Assert.IsType<LoadSourceItemsEffect>(Assert.Single(transition.Effects));
        Assert.Equal(1, effect.Page);
        Assert.True(effect.Append);
    }

    /// <summary>Prüft NextPageRequested: ohne weitere Seite oder bei Busy → wird ignoriert.</summary>
    [Fact]
    public void NextPageRequested_NoMoreOrBusy_IsIgnored()
    {
        var model = Connected() with
        {
            SourceBrowser = new SourceBrowserState { SourceId = 5, HasMore = false, Attempted = true }
        };

        var transition = AppUpdate.Update(model, new SourceItemsNextPageRequestedMessage());

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft ItemsLoaded: bei <c>Append</c> → hängt Einträge an und rückt die Seite vor.</summary>
    [Fact]
    public void ItemsLoaded_Append_ConcatenatesAndAdvancesPage()
    {
        var model = Connected() with
        {
            SourceBrowser = new SourceBrowserState
            {
                SourceId = 5,
                NextPage = 1,
                IsLoadingMore = true,
                Items = [Item(1)]
            }
        };

        var transition = AppUpdate.Update(
            model,
            new SourceItemsLoadedMessage(5, 1, [Item(2), Item(3)], HasMore: true, Append: true, Error: null));

        var browser = transition.Model.SourceBrowser;
        Assert.Equal(3, browser.Items.Count);
        Assert.Equal(2, browser.NextPage);
        Assert.True(browser.HasMore);
        Assert.False(browser.IsLoadingMore);
    }

    /// <summary>Prüft ItemsLoaded: bei <c>StaleAppend</c> → wird verworfen.</summary>
    [Fact]
    public void ItemsLoaded_StaleAppend_IsDiscarded()
    {
        // Anhänge-Antwort einer anderen Seitennummer (Filter wurde
        // zwischenzeitlich geändert) darf nicht angehängt werden.
        var model = Connected() with
        {
            SourceBrowser = new SourceBrowserState
            {
                SourceId = 5,
                NextPage = 0,
                IsLoadingMore = true,
                Items = []
            }
        };

        var transition = AppUpdate.Update(
            model,
            new SourceItemsLoadedMessage(5, 3, [Item(9)], HasMore: false, Append: true, Error: null));

        Assert.Same(model, transition.Model);
    }

    /// <summary>Prüft ItemsLoaded: bei <c>AppendError</c> → behält vorhandene Einträge.</summary>
    [Fact]
    public void ItemsLoaded_AppendError_KeepsExistingItems()
    {
        var model = Connected() with
        {
            SourceBrowser = new SourceBrowserState
            {
                SourceId = 5,
                NextPage = 1,
                HasMore = true,
                IsLoadingMore = true,
                Items = [Item(1)]
            }
        };

        var transition = AppUpdate.Update(
            model,
            new SourceItemsLoadedMessage(5, 1, [], HasMore: false, Append: true, Error: "Fehler"));

        var browser = transition.Model.SourceBrowser;
        Assert.Single(browser.Items);
        Assert.False(browser.IsLoadingMore);
        // Fehler beim Anhängen setzt keinen Vollfehler — die Liste bleibt nutzbar.
        Assert.False(browser.HasError);
    }

    /// <summary>Prüft Retry: nach leerem Fehler → lädt von Grund auf neu.</summary>
    [Fact]
    public void Retry_OnEmptyError_ReloadsFromScratch()
    {
        var model = Connected() with
        {
            SourceBrowser = new SourceBrowserState
            {
                SourceId = 5,
                Attempted = true,
                Error = "Fehler"
            }
        };

        var transition = AppUpdate.Update(model, new SourceItemsRetryRequestedMessage());

        Assert.True(transition.Model.SourceBrowser.IsLoading);
        Assert.Null(transition.Model.SourceBrowser.Error);
        Assert.IsType<LoadSourceItemsEffect>(Assert.Single(transition.Effects));
    }

    /// <summary>Prüft Retry: mit Einträgen → wiederholt die ausstehende nächste Seite.</summary>
    [Fact]
    public void Retry_WithItems_RetriesPendingNextPage()
    {
        var model = Connected() with
        {
            SourceBrowser = new SourceBrowserState
            {
                SourceId = 5,
                NextPage = 2,
                HasMore = true,
                Items = [Item(1)]
            }
        };

        var transition = AppUpdate.Update(model, new SourceItemsRetryRequestedMessage());

        var effect = Assert.IsType<LoadSourceItemsEffect>(Assert.Single(transition.Effects));
        Assert.Equal(2, effect.Page);
        Assert.True(effect.Append);
    }

    // --- Auswahl → Details ---

    /// <summary>Prüft MediaItemSelected: navigiert zur Detailseite.</summary>
    [Fact]
    public void MediaItemSelected_NavigatesToDetails()
    {
        var item = new SourceMediaItem { Id = 42, Title = "Matrix", MediaType = "moviecollection" };

        var transition = AppUpdate.Update(
            Connected(),
            new SourceMediaItemSelectedMessage(item));

        var effect = Assert.IsType<NavigateToMediaDetailsEffect>(Assert.Single(transition.Effects));
        Assert.Equal("moviecollection", effect.MediaType);
        Assert.Equal(42, effect.MediaId);
        Assert.Equal("Matrix", effect.Title);
    }
}
