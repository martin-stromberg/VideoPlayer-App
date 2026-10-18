using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// MVU-Übergänge der Wiedergabeseite (Schritt 10): Wiedergabe-Start mit
/// Route und Quellauflösung, Serienkontext/Fortsetzungsliste,
/// Fortschritts-Takt mit „angesehen"-Schwelle und Auto-Fortsetzung,
/// Fehler-Rückfall auf die nächste Episode, PiP-Umschaltung und
/// Seitenende/Abmeldung.
/// </summary>
public class AppUpdateTests_Player
{
    private static AppModel Connected() => new()
    {
        ConnectionState = ConnectionState.Connected,
        IsStartupRunning = false
    };

    /// <summary>Seriendetails mit zwei Staffeln à zwei Episoden (31→32→33→34).</summary>
    /// <returns>Ein Detail-Anzeigemodell mit Serienkontext.</returns>
    private static MediaDetailsView ShowDetails() => new()
    {
        MediaType = "tvshowepisode",
        MediaId = 31,
        Title = "Serie",
        ShowId = 11,
        Seasons =
        [
            new SeasonItem
            {
                Id = 21,
                Number = 1,
                Name = "Staffel 1",
                Episodes =
                [
                    new EpisodeItem { Id = 31, Number = 1, Title = "Pilot" },
                    new EpisodeItem { Id = 32, Number = 2, Title = "Folge 2" }
                ]
            },
            new SeasonItem
            {
                Id = 22,
                Number = 2,
                Name = "Staffel 2",
                Episodes =
                [
                    new EpisodeItem { Id = 33, Number = 1, Title = "Auftakt" },
                    new EpisodeItem { Id = 34, Number = 2, Title = "Fortsetzung" }
                ]
            }
        ]
    };

    private static AppModel WithDetails(MediaDetailsView details) =>
        Connected() with
        {
            MediaDetails = new MediaDetailsState
            {
                MediaType = details.MediaType,
                MediaId = details.MediaId,
                Attempted = true,
                Details = details
            }
        };

    private static PlayerState PlayingState(
        long mediaId = 31,
        IReadOnlyList<PlayerEpisodeRef>? upcoming = null,
        TimeSpan? duration = null,
        TimeSpan? position = null,
        int endWindowStreak = 0) => new()
        {
            MediaType = "tvshowepisode",
            MediaId = mediaId,
            Title = "Pilot",
            Attempted = true,
            IsPlaying = true,
            Duration = duration ?? TimeSpan.FromMinutes(45),
            Position = position ?? TimeSpan.Zero,
            EndWindowStreak = endWindowStreak,
            UpcomingEpisodes = upcoming ?? [],
            Source = new PlaybackSource
            {
                Kind = PlaybackSourceKind.ServerStream,
                Location = "http://testserver/api/items/tvshowepisode/31/stream"
            }
        };

    private static AppModel WithPlayer(PlayerState player) =>
        Connected() with { Player = player };

    // --- Wiedergabe-Start ---

    /// <summary>Prüft PlayRequested: bei einem Film → Player-State + Navigation + Quellauflösung.</summary>
    [Fact]
    public void PlayRequested_ValidMovie_SetsPlayerStateAndEffects()
    {
        var transition = AppUpdate.Update(
            Connected(),
            new PlayRequestedMessage("movie", 42, "Matrix"));

        var player = transition.Model.Player;
        Assert.Equal("movie", player.MediaType);
        Assert.Equal(42, player.MediaId);
        Assert.Equal("Matrix", player.Title);
        Assert.True(player.IsResolving);
        Assert.True(player.Attempted);
        Assert.Empty(player.UpcomingEpisodes);

        Assert.IsType<NavigateToPlayerEffect>(transition.Effects[0]);
        var resolve = Assert.IsType<ResolvePlaybackSourceEffect>(transition.Effects[1]);
        Assert.Equal("movie", resolve.MediaType);
        Assert.Equal(42, resolve.MediaId);
    }

    /// <summary>Prüft PlayRequested: bei einer Episode → Folge-Episoden über Staffeln hinweg.</summary>
    [Fact]
    public void PlayRequested_Episode_BuildsUpcomingEpisodes()
    {
        var transition = AppUpdate.Update(
            WithDetails(ShowDetails()),
            new PlayRequestedMessage("tvshowepisode", 31, "Pilot"));

        var upcoming = transition.Model.Player.UpcomingEpisodes;
        Assert.Equal(new long[] { 32, 33, 34 }, upcoming.Select(e => e.MediaId));
        Assert.Equal("Folge 2", upcoming[0].Title);
    }

    /// <summary>Prüft PlayRequested: bei letzter Episode → leere Fortsetzungsliste.</summary>
    [Fact]
    public void PlayRequested_LastEpisode_HasNoUpcoming()
    {
        var transition = AppUpdate.Update(
            WithDetails(ShowDetails()),
            new PlayRequestedMessage("tvshowepisode", 34, "Fortsetzung"));

        Assert.Empty(transition.Model.Player.UpcomingEpisodes);
    }

    /// <summary>Prüft PlayRequested: bei ungültiger Identität/nicht abspielbarem Typ → unverändert.</summary>
    [Fact]
    public void PlayRequested_InvalidIdentity_Unchanged()
    {
        var model = Connected();

        Assert.Same(model, AppUpdate.Update(model, new PlayRequestedMessage("movie", 0, "x")).Model);
        Assert.Same(model, AppUpdate.Update(model, new PlayRequestedMessage("tvshow", 11, "Serie")).Model);
        Assert.Same(model, AppUpdate.Update(model, new PlayRequestedMessage("moviecollection", 5, "K")).Model);
    }

    /// <summary>Prüft PlaybackSourceResolved: bei Erfolg → Quelle übernommen, Auflösung beendet.</summary>
    [Fact]
    public void PlaybackSourceResolved_Success_SetsSource()
    {
        var source = new PlaybackSource
        {
            Kind = PlaybackSourceKind.ServerStream,
            Location = "http://s/api/items/movie/42/stream?access_token=t",
            ResumePosition = TimeSpan.FromMinutes(10)
        };
        var model = WithPlayer(new PlayerState
        {
            MediaType = "movie",
            MediaId = 42,
            IsResolving = true,
            Attempted = true
        });

        var transition = AppUpdate.Update(
            model,
            new PlaybackSourceResolvedMessage("movie", 42, source, null));

        var player = transition.Model.Player;
        Assert.Same(source, player.Source);
        Assert.False(player.IsResolving);
        Assert.Null(player.Error);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft PlaybackSourceResolved: bei Fehlschlag → Fehler + Rückfall auf die nächste Episode.</summary>
    [Fact]
    public void PlaybackSourceResolved_Error_FallsBackToNextEpisode()
    {
        var model = WithPlayer(PlayingState(upcoming:
        [
            new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" },
            new PlayerEpisodeRef { MediaId = 33, Title = "Auftakt" }
        ]));

        var transition = AppUpdate.Update(
            model,
            new PlaybackSourceResolvedMessage("tvshowepisode", 31, null, "kaputt"));

        var player = transition.Model.Player;
        Assert.Equal(32, player.MediaId);
        Assert.Equal("Folge 2", player.Title);
        Assert.Equal("tvshowepisode", player.MediaType);
        Assert.True(player.IsResolving);
        Assert.Equal("kaputt", player.Error);
        Assert.Single(player.UpcomingEpisodes);
        Assert.Equal(33, player.UpcomingEpisodes[0].MediaId);

        var resolve = Assert.IsType<ResolvePlaybackSourceEffect>(Assert.Single(transition.Effects));
        Assert.Equal(32, resolve.MediaId);
    }

    /// <summary>Prüft PlaybackSourceResolved: Fehlschlag ohne Folge-Episode → Fehlerzustand bleibt.</summary>
    [Fact]
    public void PlaybackSourceResolved_ErrorWithoutNext_StaysOnError()
    {
        var model = WithPlayer(PlayingState());

        var transition = AppUpdate.Update(
            model,
            new PlaybackSourceResolvedMessage("tvshowepisode", 31, null, null));

        var player = transition.Model.Player;
        Assert.Equal(31, player.MediaId);
        Assert.False(player.IsResolving);
        Assert.Equal(AppUpdate.PlaybackUnavailableError, player.Error);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft PlaybackSourceResolved: veraltete Antwort → verworfen.</summary>
    [Fact]
    public void PlaybackSourceResolved_StaleResponse_Discarded()
    {
        var model = WithPlayer(PlayingState(mediaId: 32));
        var transition = AppUpdate.Update(
            model,
            new PlaybackSourceResolvedMessage("tvshowepisode", 31, null, "alt"));

        Assert.Same(model, transition.Model);
    }

    // --- Fortschritts-Takt ---

    /// <summary>Prüft PlaybackProgress: geänderte Position → Fortschrittsmeldung.</summary>
    [Fact]
    public void PlaybackProgress_ReportsChangedPosition()
    {
        var model = WithPlayer(PlayingState());
        var position = TimeSpan.FromMinutes(5);
        var duration = TimeSpan.FromMinutes(45);

        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(position, duration, true, false, 31));

        var player = transition.Model.Player;
        Assert.Equal(position, player.Position);
        Assert.Equal(duration, player.Duration);
        Assert.True(player.IsPlaying);
        Assert.Equal(position.TotalSeconds, player.LastReportedPositionSeconds);

        var report = Assert.IsType<ReportPlaybackProgressEffect>(Assert.Single(transition.Effects));
        Assert.Equal("tvshowepisode", report.MediaType);
        Assert.Equal(31, report.MediaId);
        Assert.Equal(position, report.Position);
        Assert.Equal(duration, report.Duration);
    }

    /// <summary>Prüft PlaybackProgress: kontextlose Wiedergabe meldet PlaylistId = null (Issue 59).</summary>
    [Fact]
    public void PlaybackProgress_WithoutPlaylist_ReportsNullPlaylistId()
    {
        var model = WithPlayer(PlayingState());

        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(45), true, false, 31));

        var report = Assert.IsType<ReportPlaybackProgressEffect>(
            Assert.Single(transition.Effects, e => e is ReportPlaybackProgressEffect));
        Assert.Null(report.PlaylistId);
    }

    /// <summary>Prüft PlaybackProgress: unveränderte Position → keine erneute Meldung.</summary>
    [Fact]
    public void PlaybackProgress_UnchangedPosition_NoReport()
    {
        var model = WithPlayer(PlayingState() with
        {
            LastReportedPositionSeconds = TimeSpan.FromMinutes(5).TotalSeconds
        });

        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(45), false, false, 31));

        Assert.Empty(transition.Effects);
        Assert.False(transition.Model.Player.IsPlaying);
    }

    /// <summary>Prüft PlaybackProgress: Ende im „angesehen"-Fenster → IsWatched (bestätigt ab dem zweiten Takt).</summary>
    [Fact]
    public void PlaybackProgress_MarksWatchedWithinWindow()
    {
        var duration = TimeSpan.FromMinutes(45);
        var window = 300; // 5 min (Default)
        var model = WithPlayer(PlayingState());

        // Knapp außerhalb des Fensters → noch nicht „angesehen".
        var outside = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(window + 1), duration, true, false, 31));
        Assert.False(outside.Model.Player.IsWatched);

        // Erster plausibler Takt im Fenster → Streak 1, noch keine Markierung.
        var first = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(window), duration, true, false, 31));
        Assert.Equal(1, first.Model.Player.EndWindowStreak);
        Assert.False(first.Model.Player.IsWatched);

        // Zweiter aufeinanderfolgender Fenstertakt → „angesehen".
        var inside = AppUpdate.Update(
            first.Model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(window - 1), duration, true, false, 31));
        Assert.True(inside.Model.Player.IsWatched);
    }

    // --- Auto-Fortsetzung ---

    /// <summary>Prüft Auto-Fortsetzung: Restlaufzeit im Fenster + Schalter an → nächste Episode (ab dem zweiten Fenstertakt).</summary>
    [Fact]
    public void PlaybackProgress_AutoResumeAdvancesWhenEnabled()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(upcoming:
            [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }])) with
        {
            Settings = new AppSettings { AutoResumeEnabled = true, AutoResumeRemainingSeconds = 300 }
        };

        // Der Fenstereintritt ist bestätigungspflichtig: der erste
        // plausible Takt baut nur die Streak auf.
        var first = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(299), duration, true, false, 31));
        Assert.Equal(31, first.Model.Player.MediaId);

        var transition = AppUpdate.Update(
            first.Model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(298), duration, true, false, 31));

        var player = transition.Model.Player;
        Assert.Equal(32, player.MediaId);
        Assert.True(player.IsResolving);
        Assert.Contains(transition.Effects, e =>
            e is ResolvePlaybackSourceEffect r && r.MediaId == 32);
        // Die Restliste wurde um die gestartete Episode gekürzt.
        Assert.Empty(player.UpcomingEpisodes);
    }

    /// <summary>Prüft Auto-Fortsetzung: Schalter aus → kein Wechsel trotz bestätigtem Fenstereintritt.</summary>
    [Fact]
    public void PlaybackProgress_AutoResumeDisabled_DoesNotAdvance()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(endWindowStreak: 1, upcoming:
            [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }])) with
        {
            Settings = new AppSettings { AutoResumeEnabled = false }
        };

        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(10), duration, true, false, 31));

        Assert.Equal(31, transition.Model.Player.MediaId);
        Assert.DoesNotContain(transition.Effects, e => e is ResolvePlaybackSourceEffect);
    }

    /// <summary>Prüft Auto-Fortsetzung: Restlaufzeit oberhalb des Fensters → kein Wechsel.</summary>
    [Fact]
    public void PlaybackProgress_AboveResumeWindow_DoesNotAdvance()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(endWindowStreak: 1, upcoming:
            [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }])) with
        {
            Settings = new AppSettings { AutoResumeEnabled = true, AutoResumeRemainingSeconds = 300 }
        };

        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(301), duration, true, false, 31));

        // Ein Takt außerhalb des Fensters setzt die Streak zurück.
        Assert.Equal(0, transition.Model.Player.EndWindowStreak);
        Assert.Equal(31, transition.Model.Player.MediaId);
        Assert.DoesNotContain(transition.Effects, e => e is ResolvePlaybackSourceEffect);
    }

    /// <summary>Prüft Auto-Fortsetzung: Takt im Pausenzustand → kein Wechsel trotz bestätigtem Fenstereintritt.</summary>
    [Fact]
    public void PlaybackProgress_PausedWithinWindow_DoesNotAdvance()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(endWindowStreak: 1, upcoming:
            [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }])) with
        {
            Settings = new AppSettings { AutoResumeEnabled = true, AutoResumeRemainingSeconds = 300 }
        };

        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(10), duration, false, false, 31));

        Assert.Equal(31, transition.Model.Player.MediaId);
        Assert.DoesNotContain(transition.Effects, e => e is ResolvePlaybackSourceEffect);
    }

    /// <summary>Prüft Auto-Fortsetzung: keine Folge-Episode → kein Wechsel trotz bestätigtem Fenstereintritt.</summary>
    [Fact]
    public void PlaybackProgress_NoNextEpisode_DoesNotAdvance()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(endWindowStreak: 1));

        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(10), duration, true, false, 31));

        Assert.Equal(31, transition.Model.Player.MediaId);
        Assert.DoesNotContain(transition.Effects, e => e is ResolvePlaybackSourceEffect);
    }

    // --- Endfenster-Plausibilisierung (Takt-Plausibilität, Streak, Identität) ---

    /// <summary>Prüft die Fenster-Plausibilisierung: ein einzelner Fenstertakt löst weder Wechsel noch „angesehen" aus.</summary>
    [Fact]
    public void PlaybackProgress_SingleWindowTick_DoesNotAdvance()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(upcoming:
            [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }])) with
        {
            Settings = new AppSettings { AutoResumeEnabled = true }
        };

        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(10), duration, true, false, 31));

        var player = transition.Model.Player;
        Assert.Equal(1, player.EndWindowStreak);
        Assert.False(player.IsWatched);
        Assert.Equal(31, player.MediaId);
        Assert.DoesNotContain(transition.Effects, e => e is ResolvePlaybackSourceEffect);
    }

    /// <summary>Prüft die Fenster-Plausibilisierung: der zweite plausible Fenstertakt markiert „angesehen" und schaltet weiter.</summary>
    [Fact]
    public void PlaybackProgress_SecondWindowTick_AdvancesAndMarksWatched()
    {
        var duration = TimeSpan.FromMinutes(45);
        // Mit geladenen Details bleibt die „angesehen"-Markierung der
        // beendeten Episode beobachtbar (der neue Player-Stand der
        // Folge-Episode führt IsWatched=false).
        var model = WithDetails(ShowDetails()) with
        {
            Player = PlayingState(upcoming:
                [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }]),
            Settings = new AppSettings { AutoResumeEnabled = true }
        };

        var first = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(11), duration, true, false, 31));
        var transition = AppUpdate.Update(
            first.Model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(10), duration, true, false, 31));

        var player = transition.Model.Player;
        Assert.Equal(32, player.MediaId);
        Assert.True(player.IsResolving);
        Assert.Equal(0, player.EndWindowStreak);
        Assert.Contains(transition.Effects, e =>
            e is ResolvePlaybackSourceEffect r && r.MediaId == 32);
        // Die beendete Episode wurde als gesehen markiert.
        var seasons = transition.Model.MediaDetails.Details!.Seasons;
        Assert.True(seasons[0].Episodes[0].IsWatched);
        Assert.False(seasons[0].Episodes[1].IsWatched);
    }

    /// <summary>Prüft die Fenster-Plausibilisierung: ein eingefrorener Takt bei gemeldetem IsPlaying setzt die Streak zurück.</summary>
    [Fact]
    public void PlaybackProgress_FrozenPosition_ResetsStreak()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(upcoming:
            [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }])) with
        {
            Settings = new AppSettings { AutoResumeEnabled = true }
        };

        var position = duration - TimeSpan.FromSeconds(20);
        var first = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(position, duration, true, false, 31));

        // Eingefrorener Takt: Position unverändert bei IsPlaying → Streak 0.
        var frozen = AppUpdate.Update(
            first.Model,
            new PlaybackProgressMessage(position, duration, true, false, 31));
        Assert.Equal(0, frozen.Model.Player.EndWindowStreak);

        // Der nächste plausible Takt baut die Streak erst wieder bei 1 auf.
        var third = AppUpdate.Update(
            frozen.Model,
            new PlaybackProgressMessage(
                position + TimeSpan.FromSeconds(5), duration, true, false, 31));

        var player = third.Model.Player;
        Assert.Equal(1, player.EndWindowStreak);
        Assert.False(player.IsWatched);
        Assert.Equal(31, player.MediaId);
        Assert.DoesNotContain(third.Effects, e => e is ResolvePlaybackSourceEffect);
    }

    /// <summary>Prüft die Fenster-Plausibilisierung: eine geschrumpfte Dauer setzt die Streak zurück.</summary>
    [Fact]
    public void PlaybackProgress_ShrunkDuration_ResetsStreak()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(upcoming:
            [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }])) with
        {
            Settings = new AppSettings { AutoResumeEnabled = true }
        };

        var first = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(20), duration, true, false, 31));

        // Geschrumpfte Dauer im Fenster → unplausibel, Streak 0.
        var shrunk = AppUpdate.Update(
            first.Model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(15),
                duration - TimeSpan.FromMinutes(5), true, false, 31));
        Assert.Equal(0, shrunk.Model.Player.EndWindowStreak);

        // Erst der nächste plausible Takt mit wieder korrekter Dauer
        // beginnt die Bestätigung von Neuem (Streak 1).
        var third = AppUpdate.Update(
            shrunk.Model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(10), duration, true, false, 31));

        var player = third.Model.Player;
        Assert.Equal(1, player.EndWindowStreak);
        Assert.False(player.IsWatched);
        Assert.Equal(31, player.MediaId);
        Assert.DoesNotContain(third.Effects, e => e is ResolvePlaybackSourceEffect);
    }

    /// <summary>Prüft die Fenster-Plausibilisierung: ein einzelner Sprung direkt ins Fenster löst nicht sofort aus.</summary>
    [Fact]
    public void PlaybackProgress_PositionJump_SingleTick_NoAdvance()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(
            position: TimeSpan.FromMinutes(10),
            upcoming: [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }])) with
        {
            Settings = new AppSettings { AutoResumeEnabled = true }
        };

        // Einzelner Vorwärtssprung direkt ins Fenster → nur Streak 1.
        var jumped = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(10), duration, true, false, 31));

        Assert.Equal(1, jumped.Model.Player.EndWindowStreak);
        Assert.False(jumped.Model.Player.IsWatched);
        Assert.Equal(31, jumped.Model.Player.MediaId);
        Assert.DoesNotContain(jumped.Effects, e => e is ResolvePlaybackSourceEffect);

        // Erst der zweite bestätigende Takt schaltet weiter.
        var transition = AppUpdate.Update(
            jumped.Model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(5), duration, true, false, 31));

        Assert.Equal(32, transition.Model.Player.MediaId);
        Assert.Contains(transition.Effects, e =>
            e is ResolvePlaybackSourceEffect r && r.MediaId == 32);
    }

    /// <summary>Prüft die Fenster-Plausibilisierung: ein Pausentakt ohne Fortschritt setzt die Streak zurück und löst nichts aus.</summary>
    [Fact]
    public void PlaybackProgress_PausedTick_ResetsStreak()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(upcoming:
            [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }])) with
        {
            Settings = new AppSettings { AutoResumeEnabled = true }
        };

        var position = duration - TimeSpan.FromSeconds(20);
        var first = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(position, duration, true, false, 31));

        // Pausentakt ohne Fortschritt → Streak 0, selbst kein Auslöser.
        var paused = AppUpdate.Update(
            first.Model,
            new PlaybackProgressMessage(position, duration, false, false, 31));
        Assert.Equal(0, paused.Model.Player.EndWindowStreak);
        Assert.False(paused.Model.Player.IsWatched);
        Assert.DoesNotContain(paused.Effects, e => e is ResolvePlaybackSourceEffect);

        // Der nächste plausible Takt beginnt die Bestätigung von Neuem.
        var third = AppUpdate.Update(
            paused.Model,
            new PlaybackProgressMessage(
                position + TimeSpan.FromSeconds(5), duration, true, false, 31));

        Assert.Equal(1, third.Model.Player.EndWindowStreak);
        Assert.False(third.Model.Player.IsWatched);
        Assert.Equal(31, third.Model.Player.MediaId);
    }

    /// <summary>Prüft den Identitäts-Guard: ein nachlaufender Takt des Vorgängermediums wird vollständig verworfen.</summary>
    [Fact]
    public void PlaybackProgress_WrongMediaId_IsDiscarded()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(mediaId: 32) with
        {
            // Zustand direkt nach dem Wechsel: Quellauflösung der
            // Folge-Episode läuft — das Element meldet noch den
            // Vorgänger (MediaId 31) mit dessen Endfenster-Werten.
            Position = TimeSpan.Zero,
            Duration = TimeSpan.Zero,
            IsResolving = true,
            UpcomingEpisodes = [new PlayerEpisodeRef { MediaId = 33, Title = "Folge 3" }]
        }) with
        {
            Settings = new AppSettings
            {
                AutoResumeEnabled = true,
                AutoDeleteWatchedEpisodes = true
            }
        };

        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(10), duration, true, false, 31));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
        Assert.False(transition.Model.Player.IsWatched);
        Assert.Equal(32, transition.Model.Player.MediaId);
    }

    /// <summary>Prüft die IsResolving-Sperre: Takte während der Quellauflösung bleiben ohne Fensterwirkung.</summary>
    [Fact]
    public void PlaybackProgress_WhileResolving_NoWindowEffects()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(endWindowStreak: 1, upcoming:
            [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }]) with
        {
            IsResolving = true
        }) with
        {
            Settings = new AppSettings { AutoResumeEnabled = true }
        };

        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(10), duration, true, false, 31));

        var player = transition.Model.Player;
        Assert.Equal(0, player.EndWindowStreak);
        Assert.False(player.IsWatched);
        Assert.Equal(31, player.MediaId);
        Assert.DoesNotContain(transition.Effects, e => e is ResolvePlaybackSourceEffect);
    }

    /// <summary>Prüft den Serienzweig: bei ausstehender Quellauflösung wird nicht kaskadierend weitergeschaltet.</summary>
    [Fact]
    public void PlaybackProgress_SeriesAdvance_RequiresNotResolving()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(endWindowStreak: 1, upcoming:
            [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }]) with
        {
            IsResolving = true
        }) with
        {
            Settings = new AppSettings { AutoResumeEnabled = true }
        };

        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(10), duration, true, false, 31));

        Assert.Equal(31, transition.Model.Player.MediaId);
        Assert.DoesNotContain(transition.Effects, e => e is ResolvePlaybackSourceEffect);
    }

    /// <summary>Prüft die bestätigte Streak: die Markierung der Detail-Episode greift erst mit dem zweiten Fenstertakt.</summary>
    [Fact]
    public void PlaybackProgress_TwoTicks_MarksEpisodeWatchedInDetails()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithDetails(ShowDetails()) with { Player = PlayingState() };

        var first = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(290), duration, true, false, 31));
        var transition = AppUpdate.Update(
            first.Model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(280), duration, true, false, 31));

        Assert.True(transition.Model.Player.IsWatched);
        var seasons = transition.Model.MediaDetails.Details!.Seasons;
        Assert.True(seasons[0].Episodes[0].IsWatched);
        Assert.False(seasons[0].Episodes[1].IsWatched);
    }

    // --- Ende / Fehler / Schließen ---

    /// <summary>Prüft PlaybackEnded: „angesehen", Abschlussmeldung mit Endposition, Auto-Fortsetzung.</summary>
    [Fact]
    public void PlaybackEnded_MarksWatchedAndAdvances()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(
            duration: duration,
            position: duration - TimeSpan.FromSeconds(60),
            upcoming: [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }]));

        var transition = AppUpdate.Update(model, new PlaybackEndedMessage());

        var player = transition.Model.Player;
        // Auto-Fortsetzung (Restlaufzeit 0 < Fenster): nächste Episode ladend.
        Assert.Equal(32, player.MediaId);
        Assert.True(player.IsResolving);

        var report = Assert.IsType<ReportPlaybackProgressEffect>(transition.Effects[0]);
        Assert.Equal(31, report.MediaId);
        Assert.Equal(duration, report.Position);
        Assert.Contains(transition.Effects, e =>
            e is ResolvePlaybackSourceEffect r && r.MediaId == 32);
    }

    /// <summary>Prüft PlaybackEnded: ohne Folge-Episode → „angesehen" + Abschlussmeldung, kein Wechsel.</summary>
    [Fact]
    public void PlaybackEnded_WithoutNext_MarksWatchedOnly()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(duration: duration, position: duration - TimeSpan.FromSeconds(5)));

        var transition = AppUpdate.Update(model, new PlaybackEndedMessage());

        var player = transition.Model.Player;
        Assert.True(player.IsWatched);
        Assert.False(player.IsPlaying);
        Assert.Equal(31, player.MediaId);
        Assert.Single(transition.Effects, e => e is ReportPlaybackProgressEffect);
        Assert.DoesNotContain(transition.Effects, e => e is ResolvePlaybackSourceEffect);
    }

    // --- Ende-Plausibilitätsprüfung (Issue 49, Schritt 5) ---

    /// <summary>Prüft PlaybackEnded: ein Ereignis mitten in der Wiedergabe wird als verfrüht verworfen.</summary>
    [Fact]
    public void PlaybackEnded_MidPlayback_IsIgnored()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(
            duration: duration,
            position: TimeSpan.FromMinutes(10),
            upcoming: [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }]));

        var transition = AppUpdate.Update(model, new PlaybackEndedMessage());

        // Weder „angesehen"/Abschlussmeldung noch Auto-Fortsetzung —
        // der Player-Zustand bleibt unverändert laufend.
        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
        Assert.False(transition.Model.Player.IsWatched);
        Assert.True(transition.Model.Player.IsPlaying);
    }

    /// <summary>Prüft PlaybackEnded: bei unbekannter Gesamtdauer wird das Ereignis nicht als Ende gewertet.</summary>
    [Fact]
    public void PlaybackEnded_UnknownDuration_IsIgnored()
    {
        var model = WithPlayer(PlayingState(
            duration: TimeSpan.Zero,
            position: TimeSpan.FromMinutes(10),
            upcoming: [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }]));

        var transition = AppUpdate.Update(model, new PlaybackEndedMessage());

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
        Assert.Equal(31, transition.Model.Player.MediaId);
    }

    /// <summary>Prüft PlaybackEnded: ein Ereignis am inneren Rand des Toleranzfensters (300 s) gilt als Ende.</summary>
    [Fact]
    public void PlaybackEnded_WithinTolerance_Completes()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(
            duration: duration,
            position: duration - TimeSpan.FromSeconds(300)));

        var transition = AppUpdate.Update(model, new PlaybackEndedMessage());

        Assert.True(transition.Model.Player.IsWatched);
        Assert.Single(transition.Effects, e => e is ReportPlaybackProgressEffect);
    }

    /// <summary>Prüft PlaybackEnded: eine Sekunde außerhalb des Toleranzfensters (301 s) wird verworfen.</summary>
    [Fact]
    public void PlaybackEnded_JustOutsideTolerance_IsIgnored()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(
            duration: duration,
            position: duration - TimeSpan.FromSeconds(301)));

        var transition = AppUpdate.Update(model, new PlaybackEndedMessage());

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>
    /// Prüft PlaybackEnded: ein nachgezogenes Ende-Ereignis des
    /// Vorgängermediums trifft den bereits gewechselten Folge-Zustand
    /// (Position/Dauer zurückgesetzt) und darf die neue Episode nicht
    /// als gesehen markieren oder weiter vorlaufen.
    /// </summary>
    [Fact]
    public void PlaybackEnded_AfterAdvance_IgnoresStaleEvent()
    {
        var model = WithPlayer(PlayingState(mediaId: 32)
            with
        {
            // Zustand direkt nach dem Wechsel: Quellenauflösung der
            // Folge-Episode läuft, Position/Dauer noch unbekannt.
            Position = TimeSpan.Zero,
            Duration = TimeSpan.Zero,
            IsResolving = true,
            UpcomingEpisodes = [new PlayerEpisodeRef { MediaId = 33, Title = "Folge 3" }]
        });

        var transition = AppUpdate.Update(model, new PlaybackEndedMessage());

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
        Assert.False(transition.Model.Player.IsWatched);
        Assert.Equal(32, transition.Model.Player.MediaId);
    }

    // --- Angesehen-Markierung in den geladenen Details (Schritt 7) ---

    /// <summary>Prüft PlaybackEnded: die gesehene Episode wird in den geladenen Details markiert.</summary>
    [Fact]
    public void PlaybackEnded_MarksEpisodeWatchedInDetails()
    {
        var model = WithDetails(ShowDetails()) with
        {
            Player = PlayingState(
                duration: TimeSpan.FromMinutes(45),
                position: TimeSpan.FromMinutes(44))
        };

        var transition = AppUpdate.Update(model, new PlaybackEndedMessage());

        var seasons = transition.Model.MediaDetails.Details!.Seasons;
        var watched = seasons[0].Episodes[0];
        Assert.NotNull(watched.WatchedAt);
        Assert.True(watched.IsWatched);
        // Alle übrigen Episoden bleiben ungesehen.
        Assert.Null(seasons[0].Episodes[1].WatchedAt);
        Assert.All(seasons[1].Episodes, e => Assert.Null(e.WatchedAt));
    }

    /// <summary>Prüft PlaybackEnded: die Markierung greift auch bei Auto-Fortsetzung auf die Folge-Episode.</summary>
    [Fact]
    public void PlaybackEnded_AutoResume_StillMarksFinishedEpisode()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithDetails(ShowDetails()) with
        {
            Player = PlayingState(
                duration: duration,
                position: duration - TimeSpan.FromSeconds(5),
                upcoming: [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }]),
            Settings = new AppSettings { AutoResumeEnabled = true }
        };

        var transition = AppUpdate.Update(model, new PlaybackEndedMessage());

        // Player läuft auf der nächsten Episode weiter …
        Assert.Equal(32, transition.Model.Player.MediaId);
        // … aber die beendete Episode trägt den Gesehen-Zeitpunkt.
        var seasons = transition.Model.MediaDetails.Details!.Seasons;
        Assert.True(seasons[0].Episodes[0].IsWatched);
        Assert.False(seasons[0].Episodes[1].IsWatched);
    }

    /// <summary>Prüft PlaybackProgress: das Watched-Fenster markiert die Episode in den geladenen Details (ab dem zweiten Fenstertakt).</summary>
    [Fact]
    public void PlaybackProgress_WatchedWindow_MarksEpisodeWatchedInDetails()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithDetails(ShowDetails()) with { Player = PlayingState() };

        var first = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(300), duration, true, false, 31));
        var transition = AppUpdate.Update(
            first.Model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(295), duration, true, false, 31));

        Assert.True(transition.Model.Player.IsWatched);
        var seasons = transition.Model.MediaDetails.Details!.Seasons;
        Assert.True(seasons[0].Episodes[0].IsWatched);
        Assert.False(seasons[0].Episodes[1].IsWatched);
    }

    /// <summary>Prüft PlaybackEnded: eine bereits gesehene Episode behält den Server-Zeitpunkt.</summary>
    [Fact]
    public void PlaybackEnded_AlreadyWatchedEpisode_KeepsServerTimestamp()
    {
        var watchedAt = new DateTime(2026, 9, 1, 20, 0, 0, DateTimeKind.Utc);
        var details = ShowDetails();
        details = details with
        {
            Seasons =
            [
                details.Seasons[0] with
                {
                    Episodes =
                    [
                        details.Seasons[0].Episodes[0] with { WatchedAt = watchedAt },
                        details.Seasons[0].Episodes[1]
                    ]
                },
                details.Seasons[1]
            ]
        };
        var model = WithDetails(details) with
        {
            Player = PlayingState(
                duration: TimeSpan.FromMinutes(45),
                position: TimeSpan.FromMinutes(44))
        };

        var transition = AppUpdate.Update(model, new PlaybackEndedMessage());

        Assert.Equal(watchedAt, transition.Model.MediaDetails.Details!.Seasons[0].Episodes[0].WatchedAt);
    }

    /// <summary>Prüft PlaybackEnded: Wiedergabe eines Films berührt die Episodenliste nicht.</summary>
    [Fact]
    public void PlaybackEnded_Movie_DoesNotTouchEpisodeList()
    {
        var model = WithDetails(ShowDetails()) with
        {
            Player = PlayingState(position: TimeSpan.FromMinutes(45))
                with
            { MediaType = "movie", MediaId = 42 }
        };

        var transition = AppUpdate.Update(model, new PlaybackEndedMessage());

        Assert.All(
            transition.Model.MediaDetails.Details!.Seasons.SelectMany(s => s.Episodes),
            e => Assert.Null(e.WatchedAt));
    }

    /// <summary>Prüft PlaybackEnded: eine Episode außerhalb der geladenen Details lässt die Ansicht unverändert.</summary>
    [Fact]
    public void PlaybackEnded_UnknownEpisode_LeavesDetailsUnchanged()
    {
        var details = ShowDetails();
        var model = WithDetails(details) with
        {
            Player = PlayingState(mediaId: 99, position: TimeSpan.FromMinutes(45))
        };

        var transition = AppUpdate.Update(model, new PlaybackEndedMessage());

        Assert.Same(details, transition.Model.MediaDetails.Details);
    }

    /// <summary>Prüft PlaybackFailed: Fehlermeldung + Rückfall auf die nächste Episode.</summary>
    [Fact]
    public void PlaybackFailed_ShowsErrorAndFallsBack()
    {
        var model = WithPlayer(PlayingState(upcoming:
            [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }]));

        var transition = AppUpdate.Update(model, new PlaybackFailedMessage());

        var player = transition.Model.Player;
        Assert.Equal(32, player.MediaId);
        Assert.Equal(AppUpdate.PlaybackUnavailableError, player.Error);
        Assert.Contains(transition.Effects, e =>
            e is ResolvePlaybackSourceEffect r && r.MediaId == 32);
    }

    /// <summary>Prüft PlayerClosed: Abschlussmeldung + Zustandsreset + PiP-Rücksprung.</summary>
    [Fact]
    public void PlayerClosed_ReportsFinalAndResets()
    {
        var model = WithPlayer(PlayingState(
            position: TimeSpan.FromMinutes(10)) with
        {
            IsPictureInPicture = true
        });

        var transition = AppUpdate.Update(model, new PlayerClosedMessage());

        Assert.False(transition.Model.Player.IsActive);
        Assert.Same(PlayerState.Empty, transition.Model.Player);
        Assert.Contains(transition.Effects, e => e is TogglePictureInPictureEffect);
        var report = Assert.Single(transition.Effects, e => e is ReportPlaybackProgressEffect);
        Assert.Equal(TimeSpan.FromMinutes(10), ((ReportPlaybackProgressEffect)report).Position);
    }

    /// <summary>Prüft PlayerClosed: ohne Position → nur Reset, keine Meldung.</summary>
    [Fact]
    public void PlayerClosed_WithoutPosition_ResetsOnly()
    {
        var model = WithPlayer(PlayingState(position: TimeSpan.Zero));

        var transition = AppUpdate.Update(model, new PlayerClosedMessage());

        Assert.Same(PlayerState.Empty, transition.Model.Player);
        Assert.Empty(transition.Effects);
    }

    // --- Picture-in-Picture ---

    /// <summary>Prüft PiP: Anforderung erzeugt Umschalt-Effekt, Ergebnis setzt das Flag.</summary>
    [Fact]
    public void PictureInPicture_TogglesFlag()
    {
        var model = WithPlayer(PlayingState());

        var requested = AppUpdate.Update(model, new PictureInPictureRequestedMessage());
        Assert.IsType<TogglePictureInPictureEffect>(Assert.Single(requested.Effects));

        var changed = AppUpdate.Update(model, new PictureInPictureChangedMessage(true));
        Assert.True(changed.Model.Player.IsPictureInPicture);
    }

    /// <summary>Prüft PiP: ohne aktive Wiedergabe → keine Umschaltung.</summary>
    [Fact]
    public void PictureInPicture_InactivePlayer_Unchanged()
    {
        var model = Connected();
        var transition = AppUpdate.Update(model, new PictureInPictureRequestedMessage());
        Assert.Same(model, transition.Model);
    }

    // --- Lebenszyklus ---

    /// <summary>Prüft Abmeldung: Player-State wird mit dem Sitzungszustand zurückgesetzt.</summary>
    [Fact]
    public void Logout_ResetsPlayer()
    {
        var model = WithPlayer(PlayingState());

        var transition = AppUpdate.Update(model, new LogoutRequestedMessage());

        Assert.Same(PlayerState.Empty, transition.Model.Player);
    }
}
