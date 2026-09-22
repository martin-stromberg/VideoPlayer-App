using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoWebPlayer.Maui.Tests;

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
        TimeSpan? position = null) => new()
        {
            MediaType = "tvshowepisode",
            MediaId = mediaId,
            Title = "Pilot",
            Attempted = true,
            IsPlaying = true,
            Duration = duration ?? TimeSpan.FromMinutes(45),
            Position = position ?? TimeSpan.Zero,
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
            new PlaybackProgressMessage(position, duration, true, false));

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
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(45), false, false));

        Assert.Empty(transition.Effects);
        Assert.False(transition.Model.Player.IsPlaying);
    }

    /// <summary>Prüft PlaybackProgress: Ende im „angesehen"-Fenster → IsWatched.</summary>
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
                duration - TimeSpan.FromSeconds(window + 1), duration, true, false));
        Assert.False(outside.Model.Player.IsWatched);

        // Innerhalb des Fensters → „angesehen".
        var inside = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(window), duration, true, false));
        Assert.True(inside.Model.Player.IsWatched);
    }

    // --- Auto-Fortsetzung ---

    /// <summary>Prüft Auto-Fortsetzung: Restlaufzeit im Fenster + Schalter an → nächste Episode.</summary>
    [Fact]
    public void PlaybackProgress_AutoResumeAdvancesWhenEnabled()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(upcoming:
            [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }])) with
        {
            Settings = new AppSettings { AutoResumeEnabled = true, AutoResumeRemainingSeconds = 300 }
        };

        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(299), duration, true, false));

        var player = transition.Model.Player;
        Assert.Equal(32, player.MediaId);
        Assert.True(player.IsResolving);
        Assert.Contains(transition.Effects, e =>
            e is ResolvePlaybackSourceEffect r && r.MediaId == 32);
        // Die Restliste wurde um die gestartete Episode gekürzt.
        Assert.Empty(player.UpcomingEpisodes);
    }

    /// <summary>Prüft Auto-Fortsetzung: Schalter aus → kein Wechsel trotz Fenster-Unterschreitung.</summary>
    [Fact]
    public void PlaybackProgress_AutoResumeDisabled_DoesNotAdvance()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(upcoming:
            [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }])) with
        {
            Settings = new AppSettings { AutoResumeEnabled = false }
        };

        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(10), duration, true, false));

        Assert.Equal(31, transition.Model.Player.MediaId);
        Assert.DoesNotContain(transition.Effects, e => e is ResolvePlaybackSourceEffect);
    }

    /// <summary>Prüft Auto-Fortsetzung: Restlaufzeit oberhalb des Fensters → kein Wechsel.</summary>
    [Fact]
    public void PlaybackProgress_AboveResumeWindow_DoesNotAdvance()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(upcoming:
            [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }])) with
        {
            Settings = new AppSettings { AutoResumeEnabled = true, AutoResumeRemainingSeconds = 300 }
        };

        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(301), duration, true, false));

        Assert.Equal(31, transition.Model.Player.MediaId);
        Assert.DoesNotContain(transition.Effects, e => e is ResolvePlaybackSourceEffect);
    }

    /// <summary>Prüft Auto-Fortsetzung: Takt im Pausenzustand → kein Wechsel trotz Fenster-Unterschreitung.</summary>
    [Fact]
    public void PlaybackProgress_PausedWithinWindow_DoesNotAdvance()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState(upcoming:
            [new PlayerEpisodeRef { MediaId = 32, Title = "Folge 2" }])) with
        {
            Settings = new AppSettings { AutoResumeEnabled = true, AutoResumeRemainingSeconds = 300 }
        };

        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(10), duration, false, false));

        Assert.Equal(31, transition.Model.Player.MediaId);
        Assert.DoesNotContain(transition.Effects, e => e is ResolvePlaybackSourceEffect);
    }

    /// <summary>Prüft Auto-Fortsetzung: keine Folge-Episode → kein Wechsel.</summary>
    [Fact]
    public void PlaybackProgress_NoNextEpisode_DoesNotAdvance()
    {
        var duration = TimeSpan.FromMinutes(45);
        var model = WithPlayer(PlayingState());

        var transition = AppUpdate.Update(
            model,
            new PlaybackProgressMessage(
                duration - TimeSpan.FromSeconds(10), duration, true, false));

        Assert.Equal(31, transition.Model.Player.MediaId);
        Assert.DoesNotContain(transition.Effects, e => e is ResolvePlaybackSourceEffect);
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
