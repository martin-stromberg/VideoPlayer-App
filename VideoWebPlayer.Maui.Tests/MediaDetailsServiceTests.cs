#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Detail-Dienst (Schritt 8): Filme, Filmkollektionen (mit 404-Fallback
/// auf den Einzelfilm), Serien mit eingebetteten Staffeln/Episoden und
/// Episoden-Aufrufe mit Serienkontext-Auflösung — Abbild der
/// Detailseiten des Altbestands.
/// </summary>
public class MediaDetailsServiceTests
{
    private static readonly Uri Server = new("http://testserver/videoplayer/");

    private readonly StubHttpMessageHandler handler = new();
    private readonly AuthSessionService authSession;

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>MediaDetailsServiceTests</c>.</summary>
    public MediaDetailsServiceTests()
    {
        var secrets = new AppSecretsService(
            new InMemorySettingsStore(), new InMemorySecretStore(), AppProfile.Create(null));
        authSession = new AuthSessionService(
            secrets,
            uri => new HttpClient(handler, disposeHandler: false) { BaseAddress = uri },
            new RecordingDelays().Delay);
    }

    private MediaDetailsService CreateService() => new(authSession);

    private async Task LoginAsync()
    {
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "jwt", expires = "2030-01-01T00:00:00Z" });
        await authSession.LoginAsync(Server, "martin", "pw");
        handler.Requests.Clear();
    }

    private static TvShow Show(long id = 11) => new()
    {
        Id = id,
        Name = "Testserie",
        Plot = "Serienhandlung",
        GenreNames = "Drama",
        ReleaseDate = new DateTime(2020, 5, 1),
        IsFavorite = true,
        PosterPictureId = 55,
        Seasons =
        [
            new TvShowSeason
            {
                Id = 21,
                Number = 1,
                Episodes =
                [
                    new TvShowEpisode { Id = 31, Number = 1, Name = "Pilot", Plot = "Folge 1" },
                    new TvShowEpisode { Id = 32, Number = 2, Name = "Folge 2" }
                ]
            },
            new TvShowSeason
            {
                Id = 22,
                Number = 2,
                Episodes = [new TvShowEpisode { Id = 33, Number = 1, Name = "Auftakt" }]
            }
        ]
    };

    /// <summary>Prüft Load: bei einem Film → bildet die Basisdetails ab.</summary>
    [Fact]
    public async Task Load_Movie_MapsBasicDetails()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, new Movie
        {
            Id = 42,
            Name = "Matrix",
            Plot = "Handlung",
            GenreNames = "Sci-Fi",
            ReleaseDate = new DateTime(1999, 3, 31),
            PosterPictureId = 70,
            BannerPictureId = 71,
            IsFavorite = true
        });

        var details = await CreateService().LoadAsync("movie", 42, null, null, null, null);

        Assert.Equal("movie", details.MediaType);
        Assert.Equal(42, details.MediaId);
        Assert.Equal("Matrix", details.Title);
        Assert.Equal("Handlung", details.Plot);
        Assert.Equal("Sci-Fi", details.GenreNames);
        Assert.Equal(1999, details.ReleaseYear);
        Assert.Equal(71, details.BannerPictureId);
        Assert.True(details.IsFavorite);
        Assert.False(details.HasSeasons);
        Assert.False(details.HasMovies);
        Assert.EndsWith("/api/items/movie/42", Assert.Single(handler.Requests).PathAndQuery);
    }

    /// <summary>Prüft Load: bei <c>MovieCollection</c> → bildet die enthaltenen Filme ab.</summary>
    [Fact]
    public async Task Load_MovieCollection_MapsContainedMovies()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, new MovieCollection
        {
            Id = 7,
            Name = "Matrix-Reihe",
            PosterPictureId = 80,
            IsFavorite = false,
            Movies =
            [
                new Movie { Id = 2, Name = "Teil 2", ReleaseDate = new DateTime(2003, 1, 1), GenreNames = "Sci-Fi" },
                new Movie { Id = 1, Name = "Teil 1", ReleaseDate = new DateTime(1999, 1, 1) }
            ]
        });

        var details = await CreateService().LoadAsync("moviecollection", 7, null, null, null, null);

        Assert.Equal("moviecollection", details.MediaType);
        Assert.Equal("Matrix-Reihe", details.Title);
        Assert.True(details.HasMovies);
        // Nach Erscheinungsdatum sortiert.
        Assert.Equal(1, details.Movies[0].Id);
        Assert.Equal(2, details.Movies[1].Id);
        Assert.Equal(1999, details.Movies[0].ReleaseYear);
        Assert.EndsWith("/api/items/moviecollection/7", Assert.Single(handler.Requests).PathAndQuery);
    }

    /// <summary>Prüft Load: bei <c>MovieCollection</c>, wenn nicht gefunden → fällt auf Film zurück.</summary>
    [Fact]
    public async Task Load_MovieCollection_NotFound_FallsBackToMovie()
    {
        await LoginAsync();
        // Einzelfilm ohne Kollektionshülle: der Server antwortet 404 auf
        // den Kollektions-Endpunkt — dann wird die Filmansicht geladen.
        handler.Enqueue(HttpStatusCode.NotFound);
        handler.EnqueueJson(HttpStatusCode.OK, new Movie { Id = 7, Name = "Einzelfilm" });

        var details = await CreateService().LoadAsync("moviecollection", 7, "Einzelfilm", null, null, null);

        Assert.Equal("movie", details.MediaType);
        Assert.Equal("Einzelfilm", details.Title);
    }

    /// <summary>Prüft Load: bei <c>TvShow</c> → bildet Staffeln und Episoden ab.</summary>
    [Fact]
    public async Task Load_TvShow_MapsSeasonsAndEpisodes()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, Show());

        var details = await CreateService().LoadAsync("tvshow", 11, null, null, null, null);

        Assert.Equal("tvshow", details.MediaType);
        Assert.Equal("Testserie", details.Title);
        Assert.True(details.IsFavorite);
        Assert.Equal(11, details.ShowId);
        Assert.True(details.HasSeasons);
        Assert.Equal(2, details.Seasons.Count);
        Assert.Equal(21, details.Seasons[0].Id);
        Assert.Equal(2, details.Seasons[0].Episodes.Count);
        Assert.Equal("Pilot", details.Seasons[0].Episodes[0].Title);
        Assert.Equal("1. Pilot", details.Seasons[0].Episodes[0].DisplayTitle);
        Assert.EndsWith("/api/items/tvshow/11", Assert.Single(handler.Requests).PathAndQuery);
    }

    /// <summary>Prüft Load: bei Episode mit Show-Hint → öffnet den Show-Kontext mit Vorauswahl.</summary>
    [Fact]
    public async Task Load_Episode_WithShowHint_OpensShowContextWithPreselection()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, Show());

        var details = await CreateService().LoadAsync("tvshowepisode", 33, "Auftakt", showId: 11, seasonId: 22, episodeId: 33);

        Assert.Equal("tvshowepisode", details.MediaType);
        Assert.Equal(33, details.MediaId);
        Assert.Equal("Testserie", details.Title);
        Assert.True(details.HasSeasons);
        Assert.Equal(22, details.SelectedSeasonId);
        Assert.Equal(33, details.HighlightedEpisodeId);
        Assert.True(details.SelectedSeasonEpisodes.Single(e => e.Id == 33).IsHighlighted);
        // Nur die Serie wurde geladen — die Episode kam eingebettet mit.
        Assert.Single(handler.Requests);
    }

    /// <summary>Prüft Load: bei Episode ohne Hint → löst die Show über die eingebettete Staffel auf.</summary>
    [Fact]
    public async Task Load_Episode_WithoutHint_ResolvesShowViaEmbeddedSeason()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, new TvShowEpisode
        {
            Id = 31,
            Number = 1,
            Name = "Pilot",
            Season = new TvShowSeason { Id = 21, Show = new TvShow { Id = 11 } }
        });
        handler.EnqueueJson(HttpStatusCode.OK, Show());

        var details = await CreateService().LoadAsync("tvshowepisode", 31, "Pilot", null, null, null);

        Assert.True(details.HasSeasons);
        Assert.Equal(11, details.ShowId);
        Assert.Equal(21, details.SelectedSeasonId);
        Assert.Equal(2, handler.Requests.Count);
    }

    /// <summary>Prüft Load: bei Episode ohne Show → zeigt die Basisdetails.</summary>
    [Fact]
    public async Task Load_Episode_WithoutShow_ShowsBasicDetails()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, new TvShowEpisode
        {
            Id = 31,
            Number = 1,
            Name = "Pilot",
            Plot = "Folgenhandlung",
            IsFavorite = true
        });

        var details = await CreateService().LoadAsync("tvshowepisode", 31, null, null, null, null);

        Assert.Equal("tvshowepisode", details.MediaType);
        Assert.Equal("Pilot", details.Title);
        Assert.Equal("Folgenhandlung", details.Plot);
        Assert.True(details.IsFavorite);
        Assert.False(details.HasSeasons);
    }

    /// <summary>Prüft Load: bei Staffel mit Show-Hint → öffnet den Show-Kontext.</summary>
    [Fact]
    public async Task Load_Season_WithShowHint_OpensShowContext()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, Show());

        var details = await CreateService().LoadAsync("tvshowseason", 22, null, showId: 11, null, null);

        Assert.Equal("tvshowseason", details.MediaType);
        Assert.Equal(22, details.MediaId);
        Assert.Equal(22, details.SelectedSeasonId);
        Assert.True(details.HasSeasons);
    }

    /// <summary>Prüft Load: bei Staffel, bei <c>WithoutShowHint</c> → schlägt fehl.</summary>
    [Fact]
    public async Task Load_Season_WithoutShowHint_Fails()
    {
        await LoginAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().LoadAsync("tvshowseason", 22, null, null, null, null));
        Assert.Empty(handler.Requests);
    }

    /// <summary>Prüft Load: bei unbekanntem Typ → schlägt fehl.</summary>
    [Fact]
    public async Task Load_UnknownType_Fails()
    {
        await LoginAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().LoadAsync("video", 1, null, null, null, null));
    }

    /// <summary>Prüft Load: ohne Client → wirft.</summary>
    [Fact]
    public async Task Load_WithoutClient_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().LoadAsync("movie", 42, null, null, null, null));
        Assert.Empty(handler.Requests);
    }
}
