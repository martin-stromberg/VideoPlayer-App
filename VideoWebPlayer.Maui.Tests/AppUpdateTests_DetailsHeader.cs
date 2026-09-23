using VideoPlayer.Mvu;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// MVU-Übergänge des Detail-Kopfbereichs (Schritt 6): Bildquelle nach
/// Web-Kette (Episoden-Still-Endpunkt bzw. Banner/Fanart/Poster über
/// <c>api/pictures</c>), Ladeeffekt nach der Detail-Ladung, Wechsel der
/// Kopfzeile bei Staffelwahl (erste Episode wie im Web) sowie der
/// Identitäts-Abgleich verspäteter Bildantworten.
/// </summary>
public class AppUpdateTests_DetailsHeader
{
    private static AppModel Connected() => new()
    {
        ConnectionState = ConnectionState.Connected,
        IsStartupRunning = false
    };

    private static AppModel Loading(string mediaType, long mediaId) =>
        Connected() with
        {
            MediaDetails = new MediaDetailsState
            {
                MediaType = mediaType,
                MediaId = mediaId,
                IsLoading = true,
                Attempted = true
            }
        };

    private static AppModel WithDetails(
        MediaDetailsView details,
        byte[]? headerImageBytes = null,
        bool isHeaderImageLoading = false,
        ConnectionState connection = ConnectionState.Connected) =>
        new AppModel
        {
            ConnectionState = connection,
            IsStartupRunning = false
        } with
        {
            MediaDetails = new MediaDetailsState
            {
                MediaType = details.MediaType,
                MediaId = details.MediaId,
                Attempted = true,
                Details = details,
                HeaderImageBytes = headerImageBytes,
                IsHeaderImageLoading = isHeaderImageLoading
            }
        };

    private static MediaDetailsView ShowDetails(long? bannerPictureId = 88) => new()
    {
        MediaType = "tvshow",
        MediaId = 11,
        Title = "Serie",
        Plot = "Serienhandlung",
        GenreNames = "Drama",
        ReleaseYear = 2020,
        BannerPictureId = bannerPictureId,
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
                    new EpisodeItem { Id = 31, Number = 1, Title = "Pilot", Plot = "Folge 1", ReleaseYear = 2020 },
                    new EpisodeItem { Id = 32, Number = 2, Title = "Folge 2" }
                ]
            },
            new SeasonItem
            {
                Id = 22,
                Number = 2,
                Name = "Staffel 2",
                BannerPictureId = 77,
                Year = 2021,
                Episodes = [new EpisodeItem { Id = 33, Number = 1, Title = "Auftakt" }]
            }
        ],
        SelectedSeasonId = 21
    };

    // --- Header-Textableitung (MediaDetailsView) ---

    /// <summary>Prüft MediaDetailsView: bei einem Film → Titel, Metazeile und Banner-Quelle des Mediums.</summary>
    [Fact]
    public void HeaderText_Movie_ShowsMediumDetails()
    {
        var details = new MediaDetailsView
        {
            MediaType = "movie",
            MediaId = 42,
            Title = "Matrix",
            Plot = "Handlung",
            GenreNames = "Sci-Fi",
            ReleaseYear = 1999,
            BannerPictureId = 71
        };

        Assert.Equal("Matrix", details.HeaderTitle);
        Assert.Equal("Film · 1999 · Sci-Fi", details.HeaderMeta);
        Assert.Equal("Handlung", details.HeaderPlot);
        Assert.Equal(71, details.HeaderPictureId);
        Assert.Null(details.HeaderEpisodeId);
    }

    /// <summary>Prüft MediaDetailsView: bei ausgewählter Episode → Episoden-Titel/-Meta/-Still.</summary>
    [Fact]
    public void HeaderText_SelectedEpisode_ShowsEpisodeDetails()
    {
        var details = ShowDetails() with { MediaType = "tvshowepisode", MediaId = 31, SelectedEpisodeId = 31 };

        Assert.Equal("Pilot", details.HeaderTitle);
        Assert.Equal("2020 · Episode 1", details.HeaderMeta);
        Assert.Equal("Folge 1", details.HeaderPlot);
        Assert.Equal(31, details.HeaderEpisodeId);
        Assert.Null(details.HeaderPictureId);
    }

    /// <summary>Prüft MediaDetailsView: bei Staffel ohne Episode → „Serie - Staffel“ und Staffel-Banner.</summary>
    [Fact]
    public void HeaderText_Season_ShowsSeasonDetails()
    {
        var details = ShowDetails() with
        {
            MediaType = "tvshowseason",
            MediaId = 22,
            SelectedSeasonId = 22
        };

        Assert.Equal("Serie - Staffel 2", details.HeaderTitle);
        Assert.Equal("2021 · 1 Episode", details.HeaderMeta);
        Assert.Null(details.HeaderPlot);
        Assert.Equal(77, details.HeaderPictureId);
        Assert.Null(details.HeaderEpisodeId);
    }

    /// <summary>Prüft MediaDetailsView: bei Staffel ohne eigenes Bild → fällt auf den Serien-Banner zurück.</summary>
    [Fact]
    public void HeaderText_SeasonWithoutBanner_FallsBackToShowBanner()
    {
        var details = ShowDetails() with
        {
            MediaType = "tvshowseason",
            MediaId = 21,
            SelectedSeasonId = 21
        };

        // Staffel 1 trägt keine Bildquelle → Serien-Banner (88).
        Assert.Equal(88, details.HeaderPictureId);
    }

    /// <summary>Prüft MediaDetailsView: bei eigenständiger Episode → Still-Endpunkt über die Medien-Id.</summary>
    [Fact]
    public void HeaderText_StandaloneEpisode_UsesStillEndpoint()
    {
        var details = new MediaDetailsView
        {
            MediaType = "tvshowepisode",
            MediaId = 31,
            Title = "Pilot",
            Plot = "Folgenhandlung",
            ReleaseYear = 2020,
            SelectedEpisodeId = 31
        };

        Assert.Equal(31, details.HeaderEpisodeId);
        Assert.Equal("Pilot", details.HeaderTitle);
        Assert.Equal("Folgenhandlung", details.HeaderPlot);
        Assert.Equal("Episode · 2020", details.HeaderMeta);
    }

    // --- Ladeeffekt nach der Detail-Ladung ---

    /// <summary>Prüft MediaDetailsLoaded: bei Banner-Quelle → stößt den Bild-Ladeeffekt an.</summary>
    [Fact]
    public void MediaDetailsLoaded_Show_EmitsHeaderPictureEffect()
    {
        var transition = AppUpdate.Update(
            Loading("tvshow", 11),
            new MediaDetailsLoadedMessage("tvshow", 11, ShowDetails(), null));

        Assert.True(transition.Model.MediaDetails.IsHeaderImageLoading);
        var effect = Assert.IsType<LoadMediaDetailsHeaderEffect>(Assert.Single(transition.Effects));
        Assert.Equal("tvshow", effect.MediaType);
        Assert.Equal(11, effect.MediaId);
        Assert.Equal(88, effect.HeaderPictureId);
        Assert.Null(effect.HeaderEpisodeId);
    }

    /// <summary>Prüft MediaDetailsLoaded: bei Episode → lädt den Still über den Episoden-Endpunkt.</summary>
    [Fact]
    public void MediaDetailsLoaded_Episode_EmitsEpisodeStillEffect()
    {
        var details = ShowDetails() with
        {
            MediaType = "tvshowepisode",
            MediaId = 31,
            SelectedEpisodeId = 31
        };

        var transition = AppUpdate.Update(
            Loading("tvshowepisode", 31),
            new MediaDetailsLoadedMessage("tvshowepisode", 31, details, null));

        var effect = Assert.IsType<LoadMediaDetailsHeaderEffect>(Assert.Single(transition.Effects));
        Assert.Equal(31, effect.HeaderEpisodeId);
        Assert.Null(effect.HeaderPictureId);
    }

    /// <summary>Prüft MediaDetailsLoaded: bei Staffel → lädt das Staffel-Banner.</summary>
    [Fact]
    public void MediaDetailsLoaded_Season_UsesSeasonBanner()
    {
        var details = ShowDetails() with
        {
            MediaType = "tvshowseason",
            MediaId = 22,
            SelectedSeasonId = 22
        };

        var transition = AppUpdate.Update(
            Loading("tvshowseason", 22),
            new MediaDetailsLoadedMessage("tvshowseason", 22, details, null));

        var effect = Assert.IsType<LoadMediaDetailsHeaderEffect>(Assert.Single(transition.Effects));
        Assert.Equal(77, effect.HeaderPictureId);
        Assert.Null(effect.HeaderEpisodeId);
    }

    /// <summary>Prüft MediaDetailsLoaded: ohne Bildquelle → kein Ladeeffekt.</summary>
    [Fact]
    public void MediaDetailsLoaded_NoHeaderSource_NoEffect()
    {
        var details = ShowDetails(bannerPictureId: null);
        // Ohne Bildquelle auch keine Staffel-Banner in der Sicht.
        var transition = AppUpdate.Update(
            Loading("tvshow", 11),
            new MediaDetailsLoadedMessage("tvshow", 11, details, null));

        Assert.Empty(transition.Effects);
        Assert.False(transition.Model.MediaDetails.IsHeaderImageLoading);
    }

    /// <summary>Prüft MediaDetailsLoaded: offline → wird kein Bild abgerufen.</summary>
    [Fact]
    public void MediaDetailsLoaded_Offline_NoHeaderEffect()
    {
        var model = new AppModel
        {
            ConnectionState = ConnectionState.Offline,
            IsStartupRunning = false
        } with
        {
            MediaDetails = new MediaDetailsState
            {
                MediaType = "tvshow",
                MediaId = 11,
                IsLoading = true,
                Attempted = true
            }
        };

        var transition = AppUpdate.Update(
            model,
            new MediaDetailsLoadedMessage("tvshow", 11, ShowDetails(), null));

        Assert.Empty(transition.Effects);
        Assert.False(transition.Model.MediaDetails.IsHeaderImageLoading);
    }

    /// <summary>Prüft MediaDetailsLoaded: bei Fehler → kein Ladeeffekt.</summary>
    [Fact]
    public void MediaDetailsLoaded_Error_NoHeaderEffect()
    {
        var transition = AppUpdate.Update(
            Loading("movie", 42),
            new MediaDetailsLoadedMessage("movie", 42, null, "Fehler"));

        Assert.Empty(transition.Effects);
        Assert.True(transition.Model.MediaDetails.HasError);
    }

    // --- Ergebnis des Bildlaufs ---

    /// <summary>Prüft MediaDetailsHeaderImageLoaded: bei passender Identität → übernimmt die Bytes.</summary>
    [Fact]
    public void HeaderImageLoaded_MatchingIdentity_AppliesBytes()
    {
        var model = WithDetails(ShowDetails(), isHeaderImageLoading: true);
        byte[] bytes = [1, 2, 3];

        var transition = AppUpdate.Update(
            model,
            new MediaDetailsHeaderImageLoadedMessage("tvshow", 11, HeaderPictureId: 88, HeaderEpisodeId: null, bytes));

        var state = transition.Model.MediaDetails;
        Assert.False(state.IsHeaderImageLoading);
        Assert.Equal(bytes, state.HeaderImageBytes);
        Assert.True(state.HasHeaderImage);
    }

    /// <summary>Prüft MediaDetailsHeaderImageLoaded: bei fremdem Medium → wird verworfen.</summary>
    [Fact]
    public void HeaderImageLoaded_StaleMedia_IsDiscarded()
    {
        var model = WithDetails(ShowDetails(), isHeaderImageLoading: true);

        var transition = AppUpdate.Update(
            model,
            new MediaDetailsHeaderImageLoadedMessage("movie", 99, HeaderPictureId: 88, HeaderEpisodeId: null, [1]));

        Assert.Same(model, transition.Model);
    }

    /// <summary>Prüft MediaDetailsHeaderImageLoaded: bei abgelöster Bildquelle → wird verworfen.</summary>
    [Fact]
    public void HeaderImageLoaded_StaleHeaderSource_IsDiscarded()
    {
        // Staffelwechsel hat die Bildquelle auf Episode 33 gedreht, die
        // Antwort für die Banner-Quelle (88) kommt verspätet an.
        var details = ShowDetails() with { SelectedEpisodeId = 33 };
        var model = WithDetails(details, isHeaderImageLoading: true);

        var transition = AppUpdate.Update(
            model,
            new MediaDetailsHeaderImageLoadedMessage("tvshow", 11, HeaderPictureId: 88, HeaderEpisodeId: null, [1]));

        Assert.Same(model, transition.Model);
    }

    /// <summary>Prüft MediaDetailsHeaderImageLoaded: bei Fehlschlag → bleibt stabil ohne Bild.</summary>
    [Fact]
    public void HeaderImageLoaded_Failure_StaysStable()
    {
        var model = WithDetails(ShowDetails(), isHeaderImageLoading: true);

        var transition = AppUpdate.Update(
            model,
            new MediaDetailsHeaderImageLoadedMessage("tvshow", 11, HeaderPictureId: 88, HeaderEpisodeId: null, null));

        var state = transition.Model.MediaDetails;
        Assert.False(state.IsHeaderImageLoading);
        Assert.False(state.HasHeaderImage);
        Assert.False(state.HasError);
    }

    // --- Staffelwechsel ---

    /// <summary>Prüft SeasonSelected: wählt die erste Episode und lädt deren Still.</summary>
    [Fact]
    public void SeasonSelected_SelectsFirstEpisode_ReloadsHeader()
    {
        var model = WithDetails(ShowDetails(), headerImageBytes: [9]);

        var transition = AppUpdate.Update(model, new SeasonSelectedMessage(22));

        var details = transition.Model.MediaDetails.Details!;
        Assert.Equal(22, details.SelectedSeasonId);
        Assert.Equal(33, details.SelectedEpisodeId);
        // Die Bildquelle wechselte Banner → Episoden-Still: alte Bytes
        // verworfen, neuer Ladevorgang angestoßen.
        Assert.Null(transition.Model.MediaDetails.HeaderImageBytes);
        Assert.True(transition.Model.MediaDetails.IsHeaderImageLoading);
        var effect = Assert.IsType<LoadMediaDetailsHeaderEffect>(Assert.Single(transition.Effects));
        Assert.Equal(33, effect.HeaderEpisodeId);
        Assert.Null(effect.HeaderPictureId);
    }

    /// <summary>Prüft SeasonSelected: bei Staffel ohne Episoden → zeigt das Staffel-Banner.</summary>
    [Fact]
    public void SeasonSelected_EmptySeason_ShowsSeasonHeader()
    {
        var details = ShowDetails() with
        {
            MediaType = "tvshowseason",
            MediaId = 21,
            SelectedSeasonId = 21,
            Seasons =
            [
                new SeasonItem { Id = 21, Number = 1, Name = "Staffel 1", Episodes = [] },
                new SeasonItem { Id = 22, Number = 2, Name = "Staffel 2", BannerPictureId = 77, Episodes = [] }
            ]
        };
        var model = WithDetails(details, headerImageBytes: [9]);

        var transition = AppUpdate.Update(model, new SeasonSelectedMessage(22));

        Assert.Null(transition.Model.MediaDetails.Details!.SelectedEpisodeId);
        var effect = Assert.IsType<LoadMediaDetailsHeaderEffect>(Assert.Single(transition.Effects));
        Assert.Equal(77, effect.HeaderPictureId);
        Assert.Null(effect.HeaderEpisodeId);
    }

    /// <summary>Prüft SeasonSelected: ohne geänderte Bildquelle → bleibt das Bild erhalten.</summary>
    [Fact]
    public void SeasonSelected_SameHeaderSource_KeepsImage()
    {
        // Serienansicht ohne Banner: die Staffelwahl ändert die
        // Bildquelle nicht (beide leer) → kein Reload.
        var details = ShowDetails(bannerPictureId: null) with
        {
            Seasons =
            [
                new SeasonItem { Id = 21, Number = 1, Name = "Staffel 1", Episodes = [] },
                new SeasonItem { Id = 22, Number = 2, Name = "Staffel 2", Episodes = [] }
            ]
        };
        var model = WithDetails(details, headerImageBytes: [9]);

        var transition = AppUpdate.Update(model, new SeasonSelectedMessage(22));

        Assert.Equal([9], transition.Model.MediaDetails.HeaderImageBytes);
        Assert.Empty(transition.Effects);
    }
}
