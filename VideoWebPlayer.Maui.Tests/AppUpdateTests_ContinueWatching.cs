using VideoPlayer.Mvu;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// MVU-Übergänge der Weiterschauen-Auswahl (Issue #43, Schritt 1):
/// abspielbare Einträge (<c>movie</c>/<c>episode</c>) starten direkt die
/// Wiedergabe über den gemeinsamen Startpfad — ohne Detailseite —,
/// Episoden mit Serienkontext lösen die Folge-Episoden-Nachladung aus,
/// alle übrigen Einträge und Guards behalten den Detail-Fallback bzw.
/// die Unverändertheit. Zusätzlich die Übernahme der nachgeladenen
/// Fortsetzungsliste (<see cref="UpcomingEpisodesLoadedMessage"/>).
/// </summary>
public class AppUpdateTests_ContinueWatching
{
    private static AppModel Connected() => new()
    {
        ConnectionState = ConnectionState.Connected,
        IsStartupRunning = false
    };

    private static HomeMediaItem Item(
        string mediaType,
        long id,
        string title = "Titel",
        long? showId = null,
        bool isPlaceholder = false) => new()
        {
            Id = id,
            MediaType = mediaType,
            Title = title,
            ShowId = showId,
            IsPlaceholder = isPlaceholder
        };

    // --- Direkter Wiedergabestart ---

    /// <summary>Prüft den Film-Direktstart: Player-State + Navigation + Quellauflösung ohne Detail-Navigation.</summary>
    [Fact]
    public void ContinueWatchingItemSelected_Movie_StartsPlayback()
    {
        var transition = AppUpdate.Update(
            Connected(),
            new ContinueWatchingItemSelectedMessage(Item("movie", 42, "Matrix")));

        var player = transition.Model.Player;
        Assert.Equal("movie", player.MediaType);
        Assert.Equal(42, player.MediaId);
        Assert.Equal("Matrix", player.Title);
        Assert.True(player.IsResolving);
        Assert.True(player.Attempted);

        Assert.IsType<NavigateToPlayerEffect>(transition.Effects[0]);
        var resolve = Assert.IsType<ResolvePlaybackSourceEffect>(transition.Effects[1]);
        Assert.Equal("movie", resolve.MediaType);
        Assert.Equal(42, resolve.MediaId);
        Assert.DoesNotContain(transition.Effects, e => e is NavigateToMediaDetailsEffect);
        Assert.DoesNotContain(transition.Effects, e => e is LoadUpcomingEpisodesEffect);
    }

    /// <summary>Prüft den Episoden-Direktstart: Wiedergabe-Übergang plus Serienkontext-Ladeeffekt mit den Kontext-Ids.</summary>
    [Fact]
    public void ContinueWatchingItemSelected_Episode_StartsPlaybackWithShowLoad()
    {
        var transition = AppUpdate.Update(
            Connected(),
            new ContinueWatchingItemSelectedMessage(Item("episode", 31, "Pilot", showId: 11)));

        var player = transition.Model.Player;
        Assert.Equal("tvshowepisode", player.MediaType);
        Assert.Equal(31, player.MediaId);
        Assert.Equal("Pilot", player.Title);
        Assert.True(player.IsResolving);
        Assert.True(player.Attempted);

        Assert.IsType<NavigateToPlayerEffect>(transition.Effects[0]);
        var resolve = Assert.IsType<ResolvePlaybackSourceEffect>(transition.Effects[1]);
        Assert.Equal("tvshowepisode", resolve.MediaType);
        Assert.Equal(31, resolve.MediaId);
        var load = Assert.IsType<LoadUpcomingEpisodesEffect>(transition.Effects[2]);
        Assert.Equal(31, load.MediaId);
        Assert.Equal(11, load.ShowId);
        Assert.DoesNotContain(transition.Effects, e => e is NavigateToMediaDetailsEffect);
    }

    /// <summary>Prüft die CLR-Typvariante: ein <c>tvshowepisode</c>-Eintrag (Offline-Liste) startet ebenfalls direkt mit Ladeeffekt.</summary>
    [Fact]
    public void ContinueWatchingItemSelected_ClrEpisodeType_StartsPlaybackWithShowLoad()
    {
        var transition = AppUpdate.Update(
            Connected(),
            new ContinueWatchingItemSelectedMessage(Item("tvshowepisode", 31, "Pilot", showId: 11)));

        Assert.Equal("tvshowepisode", transition.Model.Player.MediaType);
        var load = Assert.IsType<LoadUpcomingEpisodesEffect>(transition.Effects[2]);
        Assert.Equal(11, load.ShowId);
    }

    /// <summary>Prüft den Episoden-Direktstart ohne Serienkontext-Hint: Wiedergabe ja, aber kein Serien-Ladeeffekt.</summary>
    [Fact]
    public void ContinueWatchingItemSelected_EpisodeWithoutShowId_NoShowLoad()
    {
        var transition = AppUpdate.Update(
            Connected(),
            new ContinueWatchingItemSelectedMessage(Item("episode", 31, "Pilot")));

        Assert.True(transition.Model.Player.IsActive);
        Assert.Equal(31, transition.Model.Player.MediaId);
        Assert.DoesNotContain(transition.Effects, e => e is LoadUpcomingEpisodesEffect);
        Assert.DoesNotContain(transition.Effects, e => e is NavigateToMediaDetailsEffect);
    }

    // --- Detail-Fallback nicht abspielbarer Typen ---

    /// <summary>Prüft den Fallback: nicht direkt abspielbare Weiterschauen-Typen navigieren weiterhin zur Detailseite.</summary>
    /// <param name="mediaType">Server-Typ des Eintrags (<c>show</c>/<c>season</c>/<c>collection</c>).</param>
    [Theory]
    [InlineData("show")]
    [InlineData("season")]
    [InlineData("collection")]
    public void ContinueWatchingItemSelected_NonPlayable_NavigatesToDetails(string mediaType)
    {
        var transition = AppUpdate.Update(
            Connected(),
            new ContinueWatchingItemSelectedMessage(Item(mediaType, 12, "Eintrag", showId: 11)));

        var navigate = Assert.IsType<NavigateToMediaDetailsEffect>(Assert.Single(transition.Effects));
        Assert.Equal(mediaType, navigate.MediaType);
        Assert.Equal(12, navigate.MediaId);
        Assert.Equal("Eintrag", navigate.Title);
        Assert.Equal(11, navigate.ShowId);
        Assert.False(transition.Model.Player.IsActive);
        Assert.DoesNotContain(transition.Effects, e => e is NavigateToPlayerEffect);
    }

    // --- Guards ---

    /// <summary>Prüft die Identitäts-Guards: ohne verwertbares Medium bleibt der Übergang wirkungslos.</summary>
    /// <param name="item">Der zu prüfende Eintrag.</param>
    [Theory]
    [MemberData(nameof(NonSelectableItems))]
    public void ContinueWatchingItemSelected_NonSelectable_Unchanged(HomeMediaItem item)
    {
        var model = Connected();

        var transition = AppUpdate.Update(model, new ContinueWatchingItemSelectedMessage(item));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Nicht verwertbare Einträge: Id 0, leerer Medientyp, Offline-Platzhalter.</summary>
    /// <returns>Die Testfälle der Theory.</returns>
    public static IEnumerable<object[]> NonSelectableItems()
    {
        yield return [Item("movie", 0)];
        yield return [Item("", 12)];
        yield return [Item("episode", 31, isPlaceholder: true)];
    }

    // --- Nachgeladene Fortsetzungsliste ---

    /// <summary>Prüft die Übernahme: passende Nachricht befüllt die Fortsetzungsliste des laufenden Players.</summary>
    [Fact]
    public void UpcomingEpisodesLoaded_MatchingPlayer_SetsQueue()
    {
        var model = Connected() with
        {
            Player = new PlayerState
            {
                MediaType = "tvshowepisode",
                MediaId = 31,
                Title = "Pilot",
                IsResolving = true,
                Attempted = true
            }
        };
        IReadOnlyList<PlayerEpisodeRef> upcoming =
        [
            new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" },
            new PlayerEpisodeRef { MediaId = 33, Title = "Folge 3" }
        ];

        var transition = AppUpdate.Update(
            model,
            new UpcomingEpisodesLoadedMessage(31, upcoming));

        Assert.Equal(upcoming, transition.Model.Player.UpcomingEpisodes);
        Assert.True(transition.Model.Player.HasNextEpisode);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft den Verwurf veralteter Antworten: anderes Medium, anderer Typ oder inaktiver Player lassen den Zustand unverändert.</summary>
    /// <param name="player">Der Player-Zustand im Model (<c>null</c> = inaktiv).</param>
    /// <param name="messageMediaId">MediaId der Nachricht.</param>
    [Theory]
    [MemberData(nameof(StaleLoadCases))]
    public void UpcomingEpisodesLoaded_StaleOrOtherMedia_Discarded(PlayerState player, long messageMediaId)
    {
        var model = Connected() with { Player = player };

        var transition = AppUpdate.Update(
            model,
            new UpcomingEpisodesLoadedMessage(messageMediaId,
                [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }]));

        Assert.Same(model.Player, transition.Model.Player);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Verwurfs-Fälle: inaktiver Player, andere MediaId, Film statt Episode.</summary>
    /// <returns>Die Testfälle der Theory.</returns>
    public static IEnumerable<object[]> StaleLoadCases()
    {
        yield return [PlayerState.Empty, 31];
        yield return
        [
            new PlayerState
            {
                MediaType = "tvshowepisode",
                MediaId = 35,
                Attempted = true,
                IsPlaying = true
            },
            31
        ];
        yield return
        [
            new PlayerState
            {
                MediaType = "movie",
                MediaId = 31,
                Attempted = true,
                IsPlaying = true
            },
            31
        ];
    }
}
