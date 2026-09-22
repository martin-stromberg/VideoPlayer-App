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

    // --- Quellenliste ---

    [Fact]
    public void SourcesPageOpened_Connected_StartsLoading()
    {
        var transition = AppUpdate.Update(Connected(), new SourcesPageOpenedMessage());

        Assert.True(transition.Model.SourcesOverview.IsLoading);
        var effect = Assert.Single(transition.Effects);
        Assert.IsType<LoadSourcesEffect>(effect);
    }

    [Fact]
    public void SourcesPageOpened_Offline_NoRequestAndStateReset()
    {
        // Offline steht keine Quellenliste zur Verfügung — kein Request,
        // ein alter Stand wird zurückgesetzt (kein veralteter Erfolgszustand).
        var model = new AppModel
        {
            ConnectionState = ConnectionState.Offline,
            SourcesOverview = new SourcesOverviewState
            {
                Attempted = true,
                Sources = [Source()]
            }
        };

        var transition = AppUpdate.Update(model, new SourcesPageOpenedMessage());

        Assert.Empty(transition.Effects);
        Assert.False(transition.Model.SourcesOverview.Attempted);
        Assert.Empty(transition.Model.SourcesOverview.Sources);
    }

    [Fact]
    public void SourcesPageOpened_NotConnected_NoRequest()
    {
        var model = new AppModel { ConnectionState = ConnectionState.Connecting };

        var transition = AppUpdate.Update(model, new SourcesPageOpenedMessage());

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void SourcesPageOpened_AlreadyLoading_NoSecondRun()
    {
        var model = Connected() with
        {
            SourcesOverview = SourcesOverviewState.Initial with { IsLoading = true }
        };

        var transition = AppUpdate.Update(model, new SourcesPageOpenedMessage());

        Assert.Empty(transition.Effects);
    }

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

    [Fact]
    public void SourcesLoaded_Error_SetsErrorState()
    {
        var transition = AppUpdate.Update(
            Connected(),
            new SourcesLoadedMessage([], "Die Quellen konnten nicht geladen werden."));

        Assert.True(transition.Model.SourcesOverview.HasError);
        Assert.False(transition.Model.SourcesOverview.ShowEmpty);
    }

    // --- Quellenwahl / Browser ---

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

    [Fact]
    public void SourceBrowserOpened_Restore_LoadsGenresAndFirstPage()
    {
        var transition = AppUpdate.Update(Connected(), new SourceBrowserOpenedMessage(9, "Serien"));

        Assert.Equal(9, transition.Model.SourceBrowser.SourceId);
        Assert.Contains(transition.Effects, e => e is LoadSourceGenresEffect { SourceId: 9 });
        Assert.Contains(transition.Effects, e => e is LoadSourceItemsEffect { SourceId: 9, Page: 0 });
        Assert.DoesNotContain(transition.Effects, e => e is NavigateToSourceOverviewEffect);
    }

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
