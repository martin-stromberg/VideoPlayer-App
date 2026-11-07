using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// MVU-Übergänge der Weiterschauen-Auswahl (Issue #49, Schritt 4):
/// abspielbare Einträge (<c>movie</c>/<c>episode</c>) öffnen die
/// Titel-Detailansicht und starten darüber die Wiedergabe über den
/// gemeinsamen Startpfad — die Detail-Navigation liegt in der
/// Effektliste vor der Player-Navigation, sodass „Player schließen"
/// zur Detailansicht zurückkehrt. Episoden mit Serienkontext lösen
/// weiterhin die Folge-Episoden-Nachladung aus; offline behalten
/// Episoden ohne Serien-Kennung die bisherige Direktwiedergabe. Alle
/// übrigen Einträge und Guards behalten den Detail-Fallback bzw. die
/// Unverändertheit. Zusätzlich die Übernahme der nachgeladenen
/// Fortsetzungsliste (<see cref="UpcomingEpisodesLoadedMessage"/>).
/// </summary>
public class AppUpdateTests_ContinueWatching
{
    private static AppModel Connected() => new()
    {
        ConnectionState = ConnectionState.Connected,
        IsStartupRunning = false
    };

    private static AppModel Offline() => new()
    {
        ConnectionState = ConnectionState.Offline,
        IsStartupRunning = false,
        HasOfflineData = true
    };

    private static HomeMediaItem Item(
        string mediaType,
        long id,
        string title = "Titel",
        long? showId = null,
        long? seasonId = null,
        long? episodeId = null,
        bool isPlaceholder = false) => new()
        {
            Id = id,
            MediaType = mediaType,
            Title = title,
            ShowId = showId,
            SeasonId = seasonId,
            EpisodeId = episodeId,
            IsPlaceholder = isPlaceholder
        };

    // --- Detailansicht mit Wiedergabe ---

    /// <summary>Prüft den Film-Start: Detailseite unter der Wiedergabe — Effekt-Reihenfolge Details → Player → Quellauflösung.</summary>
    [Fact]
    public void ContinueWatchingItemSelected_Movie_OpensDetailsAndStartsPlayback()
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

        var details = Assert.IsType<NavigateToMediaDetailsEffect>(transition.Effects[0]);
        Assert.Equal("movie", details.MediaType);
        Assert.Equal(42, details.MediaId);
        Assert.Equal("Matrix", details.Title);
        Assert.IsType<NavigateToPlayerEffect>(transition.Effects[1]);
        var resolve = Assert.IsType<ResolvePlaybackSourceEffect>(transition.Effects[2]);
        Assert.Equal("movie", resolve.MediaType);
        Assert.Equal(42, resolve.MediaId);
        Assert.Equal(3, transition.Effects.Count);
    }

    /// <summary>Prüft den Episoden-Start: Detailseite mit Serienkontext-Hints unter der Wiedergabe plus Folge-Episoden-Nachladung.</summary>
    [Fact]
    public void ContinueWatchingItemSelected_Episode_OpensDetailsAndStartsPlaybackWithShowLoad()
    {
        var transition = AppUpdate.Update(
            Connected(),
            new ContinueWatchingItemSelectedMessage(
                Item("episode", 31, "Pilot", showId: 11, seasonId: 7, episodeId: 31)));

        var player = transition.Model.Player;
        Assert.Equal("tvshowepisode", player.MediaType);
        Assert.Equal(31, player.MediaId);
        Assert.Equal("Pilot", player.Title);
        Assert.True(player.IsResolving);
        Assert.True(player.Attempted);

        var details = Assert.IsType<NavigateToMediaDetailsEffect>(transition.Effects[0]);
        Assert.Equal("episode", details.MediaType);
        Assert.Equal(31, details.MediaId);
        Assert.Equal(11, details.ShowId);
        Assert.Equal(7, details.SeasonId);
        Assert.Equal(31, details.EpisodeId);
        Assert.IsType<NavigateToPlayerEffect>(transition.Effects[1]);
        var resolve = Assert.IsType<ResolvePlaybackSourceEffect>(transition.Effects[2]);
        Assert.Equal("tvshowepisode", resolve.MediaType);
        Assert.Equal(31, resolve.MediaId);
        var load = Assert.IsType<LoadUpcomingEpisodesEffect>(transition.Effects[3]);
        Assert.Equal(31, load.MediaId);
        Assert.Equal(11, load.ShowId);
        Assert.Equal(4, transition.Effects.Count);
    }

    /// <summary>Prüft die CLR-Typvariante: ein <c>tvshowepisode</c>-Eintrag (Offline-Liste) öffnet ebenfalls die Detailseite mit Ladeeffekt.</summary>
    [Fact]
    public void ContinueWatchingItemSelected_ClrEpisodeType_OpensDetailsAndStartsPlaybackWithShowLoad()
    {
        var transition = AppUpdate.Update(
            Connected(),
            new ContinueWatchingItemSelectedMessage(Item("tvshowepisode", 31, "Pilot", showId: 11)));

        Assert.Equal("tvshowepisode", transition.Model.Player.MediaType);
        Assert.IsType<NavigateToMediaDetailsEffect>(transition.Effects[0]);
        Assert.IsType<NavigateToPlayerEffect>(transition.Effects[1]);
        Assert.IsType<ResolvePlaybackSourceEffect>(transition.Effects[2]);
        var load = Assert.IsType<LoadUpcomingEpisodesEffect>(transition.Effects[3]);
        Assert.Equal(11, load.ShowId);
    }

    /// <summary>Prüft den Episoden-Start ohne Serienkontext-Hint: online löst die Detailseite die Serie selbst auf — Detailseite und Wiedergabe, aber kein Serien-Ladeeffekt.</summary>
    [Fact]
    public void ContinueWatchingItemSelected_EpisodeWithoutShowId_OpensDetailsAndStartsPlayback()
    {
        var transition = AppUpdate.Update(
            Connected(),
            new ContinueWatchingItemSelectedMessage(Item("episode", 31, "Pilot")));

        Assert.True(transition.Model.Player.IsActive);
        Assert.Equal(31, transition.Model.Player.MediaId);
        Assert.IsType<NavigateToMediaDetailsEffect>(transition.Effects[0]);
        Assert.IsType<NavigateToPlayerEffect>(transition.Effects[1]);
        Assert.IsType<ResolvePlaybackSourceEffect>(transition.Effects[2]);
        Assert.DoesNotContain(transition.Effects, e => e is LoadUpcomingEpisodesEffect);
    }

    // --- Offline-Fallunterscheidung ---

    /// <summary>Prüft den Offline-Film: der Manifesteintrag ist sein eigener Kontext — Detailseite und Wiedergabe.</summary>
    [Fact]
    public void ContinueWatchingItemSelected_OfflineMovie_OpensDetailsAndStartsPlayback()
    {
        var transition = AppUpdate.Update(
            Offline(),
            new ContinueWatchingItemSelectedMessage(Item("movie", 42, "Matrix")));

        Assert.True(transition.Model.Player.IsActive);
        Assert.IsType<NavigateToMediaDetailsEffect>(transition.Effects[0]);
        Assert.IsType<NavigateToPlayerEffect>(transition.Effects[1]);
        Assert.IsType<ResolvePlaybackSourceEffect>(transition.Effects[2]);
    }

    /// <summary>Prüft die Offline-Episode mit Serien-Kennung: der Offline-Snapshot kann den Serienkontext aufbauen — Detailseite, Wiedergabe und Folge-Episoden-Nachladung.</summary>
    [Fact]
    public void ContinueWatchingItemSelected_OfflineEpisodeWithShowId_OpensDetailsAndStartsPlayback()
    {
        var transition = AppUpdate.Update(
            Offline(),
            new ContinueWatchingItemSelectedMessage(Item("tvshowepisode", 31, "Pilot", showId: 11, episodeId: 31)));

        var details = Assert.IsType<NavigateToMediaDetailsEffect>(transition.Effects[0]);
        Assert.Equal(11, details.ShowId);
        Assert.IsType<NavigateToPlayerEffect>(transition.Effects[1]);
        Assert.IsType<ResolvePlaybackSourceEffect>(transition.Effects[2]);
        var load = Assert.IsType<LoadUpcomingEpisodesEffect>(transition.Effects[3]);
        Assert.Equal(11, load.ShowId);
    }

    /// <summary>Prüft die Offline-Episode ohne Serien-Kennung: ohne nutzbaren Kontext bleibt die bisherige Direktwiedergabe — keine Detail-Navigation.</summary>
    [Fact]
    public void ContinueWatchingItemSelected_OfflineEpisodeWithoutShowId_StartsPlaybackWithoutDetails()
    {
        var transition = AppUpdate.Update(
            Offline(),
            new ContinueWatchingItemSelectedMessage(Item("tvshowepisode", 31, "Pilot")));

        Assert.True(transition.Model.Player.IsActive);
        Assert.Equal(31, transition.Model.Player.MediaId);
        Assert.IsType<NavigateToPlayerEffect>(transition.Effects[0]);
        Assert.IsType<ResolvePlaybackSourceEffect>(transition.Effects[1]);
        Assert.Equal(2, transition.Effects.Count);
        Assert.DoesNotContain(transition.Effects, e => e is NavigateToMediaDetailsEffect);
        Assert.DoesNotContain(transition.Effects, e => e is LoadUpcomingEpisodesEffect);
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
