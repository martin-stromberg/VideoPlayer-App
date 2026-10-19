using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// MVU-Übergänge der Playlist-Wiedergabe (Issue 43, Schritt 5):
/// Wiedergabestart aus Detailansicht und Weiterschauen-Liste,
/// manuelle Vor-/Rückwärts-Navigation, Auto-Fortsetzung und
/// Fehler-Rückfall über die serverseitigen Play-Endpunkte.
/// </summary>
public class AppUpdateTests_PlaylistPlayback
{
    private static AppModel Connected() => new()
    {
        ConnectionState = ConnectionState.Connected,
        IsStartupRunning = false
    };

    private static PlaylistPlaybackStartInfo StartInfo(
        long mediaId = 10,
        long entryId = 5,
        int position = 2,
        int total = 3,
        string mediaType = "movie",
        long startSeconds = 0) =>
        new()
        {
            PlaylistId = 7,
            PlaylistName = "Liste",
            TotalCount = total,
            CurrentPosition = position,
            CurrentEntryId = entryId,
            MediaType = mediaType,
            MediaId = mediaId,
            Title = "Eintrag",
            StartPositionSeconds = startSeconds
        };

    private static PlaylistNavigationItem NavItem(long mediaId = 11, long entryId = 6, int position = 3) =>
        new()
        {
            EntryId = entryId,
            Position = position,
            MediaType = "movie",
            MediaId = mediaId,
            Title = "Folgeeintrag",
            StartPositionSeconds = 30
        };

    private static AppModel PlayingInPlaylist(
        long playlistId = 7,
        long entryId = 5,
        long mediaId = 10,
        int position = 2,
        int total = 3,
        bool isPlaying = true,
        bool isResolving = false,
        bool nextExhausted = false) =>
        Connected() with
        {
            Player = new PlayerState
            {
                MediaType = "movie",
                MediaId = mediaId,
                Title = "Eintrag",
                Source = new PlaybackSource
                {
                    Kind = PlaybackSourceKind.ServerStream,
                    Location = "http://server/stream"
                },
                IsPlaying = isPlaying,
                IsResolving = isResolving,
                Duration = TimeSpan.FromSeconds(100),
                Position = TimeSpan.FromSeconds(96),
                PlaylistId = playlistId,
                PlaylistName = "Liste",
                PlaylistEntryId = entryId,
                PlaylistPosition = position,
                PlaylistTotalCount = total,
                PlaylistNextExhausted = nextExhausted
            }
        };

    // --- Wiedergabestart ---

    /// <summary>Prüft PlaylistPlaybackStartRequested: leitet den serverseitigen Start ohne Startvorgabe ein.</summary>
    [Fact]
    public void PlaylistPlaybackStartRequested_DispatchesStartEffect()
    {
        var model = Connected();

        var (next, effects) = AppUpdate.Update(
            model, new PlaylistPlaybackStartRequestedMessage(7, null));

        Assert.Same(model, next);
        var effect = Assert.IsType<StartPlaylistPlaybackEffect>(Assert.Single(effects));
        Assert.Equal(7, effect.PlaylistId);
        Assert.Null(effect.EntryId);
    }

    /// <summary>Prüft PlaylistPlaybackStartRequested: ungültige Playlist-Id wird verworfen.</summary>
    [Fact]
    public void PlaylistPlaybackStartRequested_InvalidId_IsIgnored()
    {
        var model = Connected();

        var (next, effects) = AppUpdate.Update(
            model, new PlaylistPlaybackStartRequestedMessage(0, null));

        Assert.Same(model, next);
        Assert.Empty(effects);
    }

    /// <summary>Prüft PlaylistPlaybackStarted: belegt den Player-State mit Playlist-Kontext und startet die Quellauflösung.</summary>
    [Fact]
    public void PlaylistPlaybackStarted_SetsPlayerAndResolves()
    {
        var model = Connected();

        var (next, effects) = AppUpdate.Update(
            model,
            new PlaylistPlaybackStartedMessage(7, StartInfo(mediaId: 10, entryId: 5, position: 2, startSeconds: 42), Error: null));

        var player = next.Player;
        Assert.Equal("movie", player.MediaType);
        Assert.Equal(10, player.MediaId);
        Assert.Equal("Eintrag", player.Title);
        Assert.Equal(7, player.PlaylistId);
        Assert.Equal("Liste", player.PlaylistName);
        Assert.Equal(5, player.PlaylistEntryId);
        Assert.Equal(2, player.PlaylistPosition);
        Assert.Equal(3, player.PlaylistTotalCount);
        Assert.True(player.IsPlaylistPlayback);
        Assert.True(player.IsResolving);
        // Playlist-Kontext hat keine Serien-Fortsetzungsliste.
        Assert.Empty(player.UpcomingEpisodes);
        Assert.False(player.HasNextEpisode);

        Assert.Equal(2, effects.Count);
        Assert.IsType<NavigateToPlayerEffect>(effects[0]);
        var resolve = Assert.IsType<ResolvePlaybackSourceEffect>(effects[1]);
        Assert.Equal("movie", resolve.MediaType);
        Assert.Equal(10, resolve.MediaId);
        // Die serverseitige Fortsetzungsposition wird durchgereicht.
        Assert.Equal(42, resolve.StartPositionSeconds);
    }

    /// <summary>Prüft PlaylistPlaybackStarted: Fehlschlag zeigt den Fehlerzustand auf der Wiedergabeseite.</summary>
    [Fact]
    public void PlaylistPlaybackStarted_Error_ShowsPlayerError()
    {
        var model = Connected();

        var (next, effects) = AppUpdate.Update(
            model,
            new PlaylistPlaybackStartedMessage(7, Start: null, Error: AppUpdate.PlaybackUnavailableError));

        Assert.False(next.Player.IsPlaylistPlayback);
        Assert.Equal(AppUpdate.PlaybackUnavailableError, next.Player.Error);
        var effect = Assert.IsType<NavigateToPlayerEffect>(Assert.Single(effects));
        Assert.NotNull(effect);
    }

    // --- Eintragsauswahl auf der Detailseite ---

    /// <summary>Prüft PlaylistEntrySelected: abspielbarer, zugänglicher Eintrag startet die Wiedergabe ab diesem Eintrag.</summary>
    /// <param name="mediaType">Medientyp des Playlist-Eintrags.</param>
    [Theory]
    [InlineData("movie")]
    [InlineData("tvshowepisode")]
    public void PlaylistEntrySelected_Playable_StartsPlaylistPlayback(string mediaType)
    {
        var model = Connected() with
        {
            PlaylistDetails = new PlaylistDetailsState { PlaylistId = 7 }
        };
        var item = new PlaylistEntryItem
        {
            EntryId = 55,
            MediaId = 12,
            MediaType = mediaType,
            Title = "Folge",
            IsAccessible = true
        };

        var (_, effects) = AppUpdate.Update(model, new PlaylistEntrySelectedMessage(item));

        var effect = Assert.IsType<StartPlaylistPlaybackEffect>(Assert.Single(effects));
        Assert.Equal(7, effect.PlaylistId);
        Assert.Equal(55, effect.EntryId);
    }

    /// <summary>Prüft PlaylistEntrySelected: nicht abspielbare oder nicht zugängliche Einträge öffnen weiterhin die Detailseite.</summary>
    /// <param name="mediaType">Medientyp des Playlist-Eintrags.</param>
    /// <param name="accessible">Zugriffsrecht des Eintrags.</param>
    [Theory]
    [InlineData("tvshow", true)]
    [InlineData("moviecollection", true)]
    [InlineData("movie", false)]
    public void PlaylistEntrySelected_NonPlayableOrInaccessible_OpensDetails(string mediaType, bool accessible)
    {
        var model = Connected() with
        {
            PlaylistDetails = new PlaylistDetailsState { PlaylistId = 7 }
        };
        var item = new PlaylistEntryItem
        {
            EntryId = 55,
            MediaId = 12,
            MediaType = mediaType,
            Title = "Eintrag",
            IsAccessible = accessible
        };

        var (_, effects) = AppUpdate.Update(model, new PlaylistEntrySelectedMessage(item));

        var effect = Assert.IsType<NavigateToMediaDetailsEffect>(Assert.Single(effects));
        Assert.Equal(12, effect.MediaId);
    }

    // --- Weiterschauen mit Playlist-Bezug ---

    /// <summary>Prüft ContinueWatchingItemSelected: Playlist-Bezug startet den Playlist-Pfad statt des Serienpfads.</summary>
    [Fact]
    public void ContinueWatchingSelected_WithPlaylist_StartsPlaylistPlayback()
    {
        var model = Connected();
        var item = new HomeMediaItem
        {
            Id = 30,
            MediaType = "episode",
            Title = "Folge 4",
            ShowId = 12,
            SeasonId = 20,
            EpisodeId = 30,
            PlaylistId = 7,
            PlaylistName = "Liste",
            PlaylistEntryId = 55
        };

        var (next, effects) = AppUpdate.Update(
            model, new ContinueWatchingItemSelectedMessage(item));

        // Kein direkter Player-Wechsel — der serverseitige Start löst den
        // Eintrag zuerst auf; der Serienkontext (ShowId-Hint) bleibt ungenutzt.
        Assert.Same(model.Player, next.Player);
        var effect = Assert.IsType<StartPlaylistPlaybackEffect>(Assert.Single(effects));
        Assert.Equal(7, effect.PlaylistId);
        Assert.Equal(55, effect.EntryId);
    }

    /// <summary>Prüft ContinueWatchingItemSelected: ohne Playlist-Bezug bleibt die Serienkontext-Auflösung bestehen.</summary>
    [Fact]
    public void ContinueWatchingSelected_WithoutPlaylist_KeepsSeriesPath()
    {
        var model = Connected();
        var item = new HomeMediaItem
        {
            Id = 30,
            MediaType = "episode",
            Title = "Folge 4",
            ShowId = 12,
            SeasonId = 20,
            EpisodeId = 30
        };

        var (next, effects) = AppUpdate.Update(
            model, new ContinueWatchingItemSelectedMessage(item));

        Assert.Equal(30, next.Player.MediaId);
        Assert.False(next.Player.IsPlaylistPlayback);
        // Detailansicht unter der Wiedergabe, dann Quellauflösung und
        // Serienkontext-Nachladung (Issue #49, Schritt 4).
        Assert.Equal(4, effects.Count);
        Assert.IsType<NavigateToMediaDetailsEffect>(effects[0]);
        Assert.IsType<NavigateToPlayerEffect>(effects[1]);
        Assert.IsType<ResolvePlaybackSourceEffect>(effects[2]);
        var upcoming = Assert.IsType<LoadUpcomingEpisodesEffect>(effects[3]);
        Assert.Equal(12, upcoming.ShowId);
    }

    /// <summary>Prüft ContinueWatchingItemSelected: PlaylistId ohne gültige EntryId (Medium nicht mehr in der Liste) bleibt im Serienpfad.</summary>
    [Fact]
    public void ContinueWatchingSelected_PlaylistWithoutEntry_KeepsSeriesPath()
    {
        var model = Connected();
        var item = new HomeMediaItem
        {
            Id = 30,
            MediaType = "episode",
            Title = "Folge 4",
            ShowId = 12,
            PlaylistId = 7,
            PlaylistName = "Liste",
            PlaylistEntryId = null
        };

        var (_, effects) = AppUpdate.Update(
            model, new ContinueWatchingItemSelectedMessage(item));

        // Detail-Navigation + Wiedergabe + Serienkontext-Nachladung
        // (Issue #49, Schritt 4) — der Ladeeffekt bleibt der letzte.
        Assert.IsType<NavigateToMediaDetailsEffect>(effects[0]);
        Assert.IsType<LoadUpcomingEpisodesEffect>(effects[3]);
    }

    // --- Manuelle Navigation ---

    /// <summary>Prüft PlaylistNextRequested: fordert den nächsten Eintrag an und meldet den Ladezustand.</summary>
    [Fact]
    public void NextRequested_InPlaylist_LoadsNextEntry()
    {
        var model = PlayingInPlaylist();

        var (next, effects) = AppUpdate.Update(model, new PlaylistNextRequestedMessage());

        Assert.True(next.Player.IsResolving);
        var effect = Assert.IsType<LoadNextPlaylistEntryEffect>(Assert.Single(effects));
        Assert.Equal(7, effect.PlaylistId);
        Assert.Equal(5, effect.CurrentEntryId);
    }

    /// <summary>Prüft PlaylistPreviousRequested: fordert den vorherigen Eintrag an.</summary>
    [Fact]
    public void PreviousRequested_InPlaylist_LoadsPreviousEntry()
    {
        var model = PlayingInPlaylist();

        var (next, effects) = AppUpdate.Update(model, new PlaylistPreviousRequestedMessage());

        Assert.True(next.Player.IsResolving);
        var effect = Assert.IsType<LoadPreviousPlaylistEntryEffect>(Assert.Single(effects));
        Assert.Equal(7, effect.PlaylistId);
        Assert.Equal(5, effect.CurrentEntryId);
    }

    /// <summary>Prüft die Navigations-Anforderungen: ohne Playlist-Kontext oder ausstehenden Lauf bleibt alles unverändert.</summary>
    [Fact]
    public void NavigationRequested_WithoutPlaylistOrWhileResolving_NoEffect()
    {
        var withoutPlaylist = Connected() with
        {
            Player = new PlayerState { MediaType = "movie", MediaId = 10 }
        };

        var (sameNext, noNext) = AppUpdate.Update(withoutPlaylist, new PlaylistNextRequestedMessage());
        Assert.Same(withoutPlaylist, sameNext);
        Assert.Empty(noNext);

        var busy = PlayingInPlaylist(isResolving: true);
        var (sameBusy, noBusy) = AppUpdate.Update(busy, new PlaylistNextRequestedMessage());
        Assert.Same(busy, sameBusy);
        Assert.Empty(noBusy);

        var exhausted = PlayingInPlaylist(nextExhausted: true);
        var (sameExhausted, noExhausted) = AppUpdate.Update(exhausted, new PlaylistNextRequestedMessage());
        Assert.Same(exhausted, sameExhausted);
        Assert.Empty(noExhausted);
    }

    /// <summary>Prüft PlaylistNavigationCompleted: wechselt auf den aufgelösten Eintrag und löst dessen Quelle auf.</summary>
    [Fact]
    public void NavigationCompleted_SwitchesMediaAndResolves()
    {
        var model = PlayingInPlaylist() with
        {
            Player = PlayingInPlaylist().Player with { IsResolving = true }
        };

        var (next, effects) = AppUpdate.Update(
            model,
            new PlaylistNavigationCompletedMessage(
                7, 5, NavItem(mediaId: 11, entryId: 6, position: 3), Forward: true, Error: null));

        var player = next.Player;
        Assert.Equal(11, player.MediaId);
        Assert.Equal("movie", player.MediaType);
        Assert.Equal("Folgeeintrag", player.Title);
        Assert.Equal(6, player.PlaylistEntryId);
        Assert.Equal(3, player.PlaylistPosition);
        Assert.False(player.PlaylistNextExhausted);
        Assert.Null(player.Source);
        Assert.True(player.IsResolving);
        Assert.False(player.IsPlaying);
        Assert.Equal(TimeSpan.Zero, player.Position);

        var resolve = Assert.IsType<ResolvePlaybackSourceEffect>(Assert.Single(effects));
        Assert.Equal(11, resolve.MediaId);
        Assert.Equal(30, resolve.StartPositionSeconds);
    }

    /// <summary>Prüft PlaylistNavigationCompleted: leeres Ergebnis (204) erschöpft die Vorwärts-Versuche, Wiedergabe läuft weiter.</summary>
    [Fact]
    public void NavigationCompleted_NoEntry_ExhaustsForward()
    {
        var model = PlayingInPlaylist() with
        {
            Player = PlayingInPlaylist().Player with { IsResolving = true }
        };

        var (next, effects) = AppUpdate.Update(
            model,
            new PlaylistNavigationCompletedMessage(7, 5, Result: null, Forward: true, Error: null));

        Assert.False(next.Player.IsResolving);
        Assert.True(next.Player.PlaylistNextExhausted);
        Assert.False(next.Player.CanNavigateNext);
        Assert.Equal(10, next.Player.MediaId);
        Assert.True(next.Player.IsPlaying);
        Assert.Empty(effects);
    }

    /// <summary>Prüft PlaylistNavigationCompleted: leeres Ergebnis rückwärts erschöpft die Vorwärts-Versuche nicht.</summary>
    [Fact]
    public void NavigationCompleted_NoEntryBackward_KeepsForward()
    {
        var model = PlayingInPlaylist() with
        {
            Player = PlayingInPlaylist().Player with { IsResolving = true }
        };

        var (next, _) = AppUpdate.Update(
            model,
            new PlaylistNavigationCompletedMessage(7, 5, Result: null, Forward: false, Error: null));

        Assert.False(next.Player.IsResolving);
        Assert.False(next.Player.PlaylistNextExhausted);
        Assert.True(next.Player.CanNavigateNext);
    }

    /// <summary>Prüft PlaylistNavigationCompleted: Fehlschlag zeigt den Fehler und lässt das Medium stehen.</summary>
    [Fact]
    public void NavigationCompleted_Error_ShowsError()
    {
        var model = PlayingInPlaylist() with
        {
            Player = PlayingInPlaylist().Player with { IsResolving = true }
        };

        var (next, effects) = AppUpdate.Update(
            model,
            new PlaylistNavigationCompletedMessage(
                7, 5, Result: null, Forward: true, Error: AppUpdate.PlaybackUnavailableError));

        Assert.False(next.Player.IsResolving);
        Assert.Equal(AppUpdate.PlaybackUnavailableError, next.Player.Error);
        Assert.Equal(10, next.Player.MediaId);
        Assert.Empty(effects);
    }

    /// <summary>Prüft PlaylistNavigationCompleted: Antworten abgelösender Identität werden verworfen.</summary>
    [Fact]
    public void NavigationCompleted_StaleIdentity_IsDiscarded()
    {
        var model = PlayingInPlaylist(entryId: 6, mediaId: 11);

        // Die Antwort gehört zu einem früheren Eintrag (CurrentEntryId 5).
        var (same, effects) = AppUpdate.Update(
            model,
            new PlaylistNavigationCompletedMessage(
                7, 5, NavItem(mediaId: 99, entryId: 8, position: 4), Forward: true, Error: null));

        Assert.Same(model, same);
        Assert.Empty(effects);
    }

    // --- Auto-Fortsetzung und Fehler-Rückfall ---

    /// <summary>Prüft PlaybackEnded: bei aktivierter Auto-Fortsetzung schaltet der Server zum Folgeeintrag weiter.</summary>
    [Fact]
    public void PlaybackEnded_InPlaylist_AdvancesPlaylist()
    {
        var model = PlayingInPlaylist();

        var (next, effects) = AppUpdate.Update(model, new PlaybackEndedMessage());

        Assert.True(next.Player.IsResolving);
        // Neben dem Advance läuft die Abschluss-Fortschrittsmeldung des
        // beendeten Mediums mit (ReportPlaybackProgressEffect).
        var effect = Assert.IsType<AdvancePlaylistEntryEffect>(
            Assert.Single(effects, e => e is AdvancePlaylistEntryEffect));
        Assert.Equal(7, effect.PlaylistId);
        Assert.Equal(5, effect.CurrentEntryId);
    }

    /// <summary>Prüft PlaybackEnded: ohne Auto-Fortsetzung bleibt der Endzustand stehen.</summary>
    [Fact]
    public void PlaybackEnded_InPlaylist_AutoResumeOff_NoAdvance()
    {
        var model = PlayingInPlaylist() with
        {
            Settings = new AppSettings { AutoResumeEnabled = false }
        };

        var (next, effects) = AppUpdate.Update(model, new PlaybackEndedMessage());

        Assert.Empty(effects.OfType<AdvancePlaylistEntryEffect>());
        Assert.False(next.Player.IsResolving);
    }

    /// <summary>
    /// Prüft PlaybackEnded: ein verfrühtes Ende-Ereignis mitten in der
    /// Wiedergabe wird auch im Playlist-Kontext verworfen — kein
    /// serverseitiger Vorlauf, keine „angesehen"-Markierung.
    /// </summary>
    [Fact]
    public void PlaybackEnded_InPlaylist_MidPlayback_IsIgnored()
    {
        var playing = PlayingInPlaylist();
        var model = playing with
        {
            // Langes Medium, Position weit vor dem Ende — das Ereignis
            // liegt außerhalb des Toleranzfensters und muss verworfen
            // werden (die Fixture-Dauer von 100 s liegt selbst komplett
            // im Fenster).
            Player = playing.Player with
            {
                Duration = TimeSpan.FromMinutes(45),
                Position = TimeSpan.FromMinutes(10)
            }
        };

        var (next, effects) = AppUpdate.Update(model, new PlaybackEndedMessage());

        Assert.Same(model, next);
        Assert.Empty(effects);
        Assert.False(next.Player.IsWatched);
    }

    /// <summary>Prüft die Fenster-Bestätigung im Playlist-Kontext: zwei plausible Fenstertakte markieren „angesehen", lösen aber keinen Wechsel aus — der erfolgt ausschließlich am Wiedergabeende.</summary>
    [Fact]
    public void PlaybackProgress_NearEnd_TwoTicks_MarksWatchedOnly()
    {
        var model = PlayingInPlaylist();

        // Erster Fenstertakt: Streak 1 — noch keine Markierung.
        var first = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                Position: TimeSpan.FromSeconds(97),
                Duration: TimeSpan.FromSeconds(100),
                IsPlaying: true,
                IsBuffering: false,
                MediaId: 10));

        Assert.Equal(1, first.Model.Player.EndWindowStreak);
        Assert.False(first.Model.Player.IsWatched);
        Assert.False(first.Model.Player.IsResolving);
        Assert.DoesNotContain(first.Effects, e => e is AdvancePlaylistEntryEffect);

        // Zweiter plausibler Fenstertakt: bestätigt → „angesehen",
        // aber kein Advance im Takt (Wechsel nur über PlaybackEnded).
        var (next, effects) = AppUpdate.Update(
            first.Model,
            new PlaybackProgressMessage(
                Position: TimeSpan.FromSeconds(98),
                Duration: TimeSpan.FromSeconds(100),
                IsPlaying: true,
                IsBuffering: false,
                MediaId: 10));

        Assert.True(next.Player.IsWatched);
        Assert.Equal(2, next.Player.EndWindowStreak);
        Assert.False(next.Player.IsResolving);
        Assert.Equal(10, next.Player.MediaId);
        Assert.DoesNotContain(effects, e => e is AdvancePlaylistEntryEffect);
    }

    /// <summary>Prüft PlaybackProgress: während eines ausstehenden Navigationslaufs bleibt der Takt ohne Fensterwirkung.</summary>
    [Fact]
    public void PlaybackProgress_NearEnd_WhileResolving_NoWindowEffects()
    {
        var model = PlayingInPlaylist(isResolving: true);

        var (next, effects) = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                Position: TimeSpan.FromSeconds(97),
                Duration: TimeSpan.FromSeconds(100),
                IsPlaying: true,
                IsBuffering: false,
                MediaId: 10));

        Assert.Equal(0, next.Player.EndWindowStreak);
        Assert.DoesNotContain(effects, e => e is AdvancePlaylistEntryEffect);
    }

    /// <summary>Prüft PlaybackFailed: der Fehler-Rückfall schaltet über den Server zum Folgeeintrag weiter.</summary>
    [Fact]
    public void PlaybackFailed_InPlaylist_AdvancesPlaylist()
    {
        var model = PlayingInPlaylist();

        var (next, effects) = AppUpdate.Update(model, new PlaybackFailedMessage());

        Assert.True(next.Player.IsResolving);
        Assert.Equal(AppUpdate.PlaybackUnavailableError, next.Player.Error);
        var effect = Assert.IsType<AdvancePlaylistEntryEffect>(Assert.Single(effects));
        Assert.Equal(5, effect.CurrentEntryId);
    }

    /// <summary>Prüft PlaybackSourceResolved: nicht auflösbare Quelle schaltet im Playlist-Kontext serverseitig weiter.</summary>
    [Fact]
    public void SourceResolveFailed_InPlaylist_AdvancesPlaylist()
    {
        var model = PlayingInPlaylist(isResolving: true);

        var (next, effects) = AppUpdate.Update(
            model,
            new PlaybackSourceResolvedMessage(
                "movie", 10, Source: null, Error: "Quelle nicht verfügbar."));

        Assert.True(next.Player.IsResolving);
        var effect = Assert.IsType<AdvancePlaylistEntryEffect>(Assert.Single(effects));
        Assert.Equal(5, effect.CurrentEntryId);
    }

    /// <summary>Prüft die Fehler-Rückfälle: mit erschöpftem Listenende (204) bleibt der Fehlerzustand stehen.</summary>
    [Fact]
    public void PlaybackFailed_InPlaylist_Exhausted_KeepsErrorState()
    {
        var model = PlayingInPlaylist(nextExhausted: true);

        var (next, effects) = AppUpdate.Update(model, new PlaybackFailedMessage());

        Assert.Empty(effects.OfType<AdvancePlaylistEntryEffect>());
        Assert.False(next.Player.IsResolving);
        Assert.Equal(AppUpdate.PlaybackUnavailableError, next.Player.Error);
    }

    /// <summary>Prüft die Serien-Fortsetzung: ohne Playlist-Kontext bleibt der UpcomingEpisodes-Pfad bestehen.</summary>
    [Fact]
    public void PlaybackEnded_WithoutPlaylist_KeepsSeriesAdvance()
    {
        var model = Connected() with
        {
            Player = new PlayerState
            {
                MediaType = "tvshowepisode",
                MediaId = 31,
                Title = "Folge 1",
                IsPlaying = true,
                // Position am Medienende — das Ende-Ereignis gilt nur
                // innerhalb des Toleranzfensters als Wiedergabeende.
                Duration = TimeSpan.FromMinutes(45),
                Position = TimeSpan.FromMinutes(45),
                UpcomingEpisodes = [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }]
            }
        };

        var (next, effects) = AppUpdate.Update(model, new PlaybackEndedMessage());

        Assert.Equal(32, next.Player.MediaId);
        var resolve = Assert.IsType<ResolvePlaybackSourceEffect>(
            Assert.Single(effects, e => e is ResolvePlaybackSourceEffect));
        Assert.Equal(32, resolve.MediaId);
        Assert.Null(resolve.StartPositionSeconds);
    }

    // --- Playlist-Bezug in der Fortschrittsmeldung (Issue 59) ---

    /// <summary>Prüft PlaybackProgress: der Takt-Effekt trägt die PlaylistId des Wiedergabekontexts.</summary>
    [Fact]
    public void PlaybackProgress_InPlaylist_ReportsPlaylistId()
    {
        var model = PlayingInPlaylist();
        // Einzelner Fenstertakt — die Fortschrittsmeldung bleibt der
        // einzige Effekt des Takts (kein Wechsel im Takt mehr).

        var (_, effects) = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                Position: TimeSpan.FromSeconds(50),
                Duration: TimeSpan.FromSeconds(100),
                IsPlaying: true,
                IsBuffering: false,
                MediaId: 10));

        var report = Assert.IsType<ReportPlaybackProgressEffect>(Assert.Single(effects));
        Assert.Equal(7, report.PlaylistId);
        Assert.Equal(10, report.MediaId);
    }

    /// <summary>
    /// Prüft PlaybackEnded: die Abschlussmeldung trägt die PlaylistId des
    /// endenden Stands — sie läuft dem Advance-Effekt voraus
    /// (Snapshot-Semantik: die Meldung gilt dem alten Eintrag).
    /// </summary>
    [Fact]
    public void PlaybackEnded_InPlaylist_ReportsPlaylistId()
    {
        var model = PlayingInPlaylist();

        var (_, effects) = AppUpdate.Update(model, new PlaybackEndedMessage());

        var report = Assert.IsType<ReportPlaybackProgressEffect>(
            Assert.Single(effects, e => e is ReportPlaybackProgressEffect));
        Assert.Equal(7, report.PlaylistId);
        Assert.Equal(10, report.MediaId);
        Assert.Contains(effects, e => e is AdvancePlaylistEntryEffect);
    }

    /// <summary>Prüft PlayerClosed: die Abschlussmeldung beim Schließen trägt die PlaylistId.</summary>
    [Fact]
    public void PlayerClosed_InPlaylist_ReportsPlaylistId()
    {
        var model = PlayingInPlaylist();

        var (next, effects) = AppUpdate.Update(model, new PlayerClosedMessage());

        var report = Assert.IsType<ReportPlaybackProgressEffect>(
            Assert.Single(effects, e => e is ReportPlaybackProgressEffect));
        Assert.Equal(7, report.PlaylistId);
        Assert.False(next.Player.IsActive);
    }
}
