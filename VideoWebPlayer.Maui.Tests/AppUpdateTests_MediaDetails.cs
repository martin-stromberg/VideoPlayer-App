using VideoPlayer.Mvu;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// MVU-Übergänge der Medien-Detailseite (Schritt 8): Detail-Ladung mit
/// Serienkontext-Hints, Staffelwahl sowie die Favoriten-Umschaltung
/// (Server führend, lokale Kopie via Sync).
/// </summary>
public class AppUpdateTests_MediaDetails
{
    private static AppModel Connected() => new()
    {
        ConnectionState = ConnectionState.Connected,
        IsStartupRunning = false
    };

    private static MediaDetailsView ShowDetails(long showId = 11, bool isFavorite = false) => new()
    {
        MediaType = "tvshow",
        MediaId = showId,
        Title = "Serie",
        ShowId = showId,
        IsFavorite = isFavorite,
        Seasons =
        [
            new SeasonItem
            {
                Id = 21,
                Number = 1,
                Name = "Staffel 1",
                Episodes = [new EpisodeItem { Id = 31, Number = 1, Title = "Pilot" }]
            },
            new SeasonItem { Id = 22, Number = 2, Name = "Staffel 2", Episodes = [] }
        ],
        SelectedSeasonId = 21
    };

    private static AppModel WithDetails(MediaDetailsView details, bool isFavoriteUpdating = false) =>
        Connected() with
        {
            MediaDetails = new MediaDetailsState
            {
                MediaType = details.MediaType,
                MediaId = details.MediaId,
                Attempted = true,
                IsFavoriteUpdating = isFavoriteUpdating,
                Details = details
            }
        };

    // --- Detail-Ladung ---

    [Fact]
    public void MediaDetailsRequested_StartsLoading_WithHints()
    {
        var transition = AppUpdate.Update(
            Connected(),
            new MediaDetailsRequestedMessage("tvshowepisode", 31, "Pilot", ShowId: 11, SeasonId: 21, EpisodeId: 31));

        var state = transition.Model.MediaDetails;
        Assert.True(state.IsLoading);
        Assert.Equal("tvshowepisode", state.MediaType);
        Assert.Equal(31, state.MediaId);

        var effect = Assert.IsType<LoadMediaDetailsEffect>(Assert.Single(transition.Effects));
        Assert.Equal("tvshowepisode", effect.MediaType);
        Assert.Equal(31, effect.MediaId);
        Assert.Equal(11, effect.ShowId);
        Assert.Equal(21, effect.SeasonId);
        Assert.Equal(31, effect.EpisodeId);
    }

    [Fact]
    public void MediaDetailsRequested_InvalidIdentity_ShowsError()
    {
        var transition = AppUpdate.Update(
            Connected(),
            new MediaDetailsRequestedMessage("movie", 0, null));

        Assert.True(transition.Model.MediaDetails.HasError);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void MediaDetailsRequested_SameTargetAlreadyLoaded_NoSecondRun()
    {
        var model = WithDetails(ShowDetails());

        var transition = AppUpdate.Update(
            model,
            new MediaDetailsRequestedMessage("tvshow", 11, "Serie"));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void MediaDetailsLoaded_SetsDetails()
    {
        var model = Connected() with
        {
            MediaDetails = new MediaDetailsState
            {
                MediaType = "tvshow",
                MediaId = 11,
                IsLoading = true,
                Attempted = true
            }
        };
        var details = ShowDetails();

        var transition = AppUpdate.Update(
            model,
            new MediaDetailsLoadedMessage("tvshow", 11, details, null));

        Assert.False(transition.Model.MediaDetails.IsLoading);
        Assert.Same(details, transition.Model.MediaDetails.Details);
        Assert.False(transition.Model.MediaDetails.HasError);
    }

    [Fact]
    public void MediaDetailsLoaded_StaleResponse_IsDiscarded()
    {
        var model = Connected() with
        {
            MediaDetails = new MediaDetailsState
            {
                MediaType = "movie",
                MediaId = 42,
                IsLoading = true,
                Attempted = true
            }
        };

        var transition = AppUpdate.Update(
            model,
            new MediaDetailsLoadedMessage("tvshow", 11, ShowDetails(), null));

        Assert.Same(model, transition.Model);
    }

    [Fact]
    public void MediaDetailsLoaded_Error_SetsErrorState()
    {
        var model = Connected() with
        {
            MediaDetails = new MediaDetailsState { MediaType = "movie", MediaId = 42, IsLoading = true }
        };

        var transition = AppUpdate.Update(
            model,
            new MediaDetailsLoadedMessage("movie", 42, null, "Die Details konnten nicht geladen werden."));

        Assert.False(transition.Model.MediaDetails.IsLoading);
        Assert.True(transition.Model.MediaDetails.HasError);
        Assert.Null(transition.Model.MediaDetails.Details);
    }

    // --- Staffelwahl ---

    [Fact]
    public void SeasonSelected_SwitchesSelectedSeason()
    {
        var model = WithDetails(ShowDetails());

        var transition = AppUpdate.Update(model, new SeasonSelectedMessage(22));

        Assert.Equal(22, transition.Model.MediaDetails.Details!.SelectedSeasonId);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void SeasonSelected_SameSeason_NoChange()
    {
        var model = WithDetails(ShowDetails());

        var transition = AppUpdate.Update(model, new SeasonSelectedMessage(21));

        Assert.Same(model, transition.Model);
    }

    // --- Favoriten ---

    [Fact]
    public void FavoriteToggleRequested_EmitsToggleEffect()
    {
        var model = WithDetails(ShowDetails());

        var transition = AppUpdate.Update(model, new FavoriteToggleRequestedMessage());

        Assert.True(transition.Model.MediaDetails.IsFavoriteUpdating);
        var effect = Assert.IsType<ToggleFavoriteEffect>(Assert.Single(transition.Effects));
        Assert.Equal("tvshow", effect.MediaType);
        Assert.Equal(11, effect.MediaId);
        Assert.Equal("Serie", effect.Title);
    }

    [Fact]
    public void FavoriteToggleRequested_WithoutDetails_IsIgnored()
    {
        var transition = AppUpdate.Update(Connected(), new FavoriteToggleRequestedMessage());

        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void FavoriteToggleCompleted_AppliesServerState()
    {
        var model = WithDetails(ShowDetails(isFavorite: false), isFavoriteUpdating: true);

        var transition = AppUpdate.Update(
            model,
            new FavoriteToggleCompletedMessage(true, "tvshow", 11));

        var state = transition.Model.MediaDetails;
        Assert.False(state.IsFavoriteUpdating);
        Assert.True(state.Details!.IsFavorite);
        Assert.Null(state.FavoriteError);
    }

    [Fact]
    public void FavoriteToggleCompleted_Failure_KeepsStateAndSetsError()
    {
        var model = WithDetails(ShowDetails(isFavorite: false), isFavoriteUpdating: true);

        var transition = AppUpdate.Update(
            model,
            new FavoriteToggleCompletedMessage(null, "tvshow", 11));

        var state = transition.Model.MediaDetails;
        Assert.False(state.IsFavoriteUpdating);
        Assert.False(state.Details!.IsFavorite);
        Assert.NotNull(state.FavoriteError);
    }

    [Fact]
    public void FavoriteToggleCompleted_OtherMedia_IsDiscarded()
    {
        var model = WithDetails(ShowDetails());

        var transition = AppUpdate.Update(
            model,
            new FavoriteToggleCompletedMessage(true, "movie", 99));

        Assert.Same(model, transition.Model);
    }
}
