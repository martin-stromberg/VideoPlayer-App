#pragma warning disable xUnit1051
using System.Net;
using System.Text.Json;
using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Maui.Tests.Stubs;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Maui.Tests;

/// <summary>Serververtrag, exklusive Ablaufgrenzen und die saisonale Cache-Projektion.</summary>
[Collection("SqliteDatabase")]
public sealed class SeasonalGenreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "seasonal-mobile", Guid.NewGuid().ToString("N"));
    private readonly CacheDatabase cache;
    private readonly TestClock clock = new();

    /// <summary>Erstellt eine isolierte SQLite-Ablage.</summary>
    public SeasonalGenreTests()
    {
        var layout = new AppStorageLayout(Path.Combine(root, "backup"), Path.Combine(root, "nobackup"), Path.Combine(root, "cache"), AppProfile.Create(null));
        layout.EnsureDirectoriesExist();
        cache = new CacheDatabase(layout);
        cache.InitializeAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        SQLite.SQLiteAsyncConnection.ResetPool();
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    /// <summary>Die drei realen Server-Discriminator bleiben bis zur Navigation erhalten.</summary>
    [Fact]
    public async Task ApiContract_DeserializesAllMediaTypesAndUtcExpiry()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            [{"genreId":9,"name":"Halloween","expiresAt":"2026-11-01T00:00:00Z","entries":[
            {"$type":"movie","id":1,"name":"Film","posterPictureId":11},
            {"$type":"movieCollection","id":2,"name":"Sammlung","movies":[]},
            {"$type":"show","id":3,"name":"Serie","seasons":[]}]}]
            """);
        var client = new VideoPlayerApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        client.SetAccessToken(new AuthToken { Token = "jwt" });
        var genre = Assert.Single(await client.GetSeasonalGenresAsync());
        Assert.IsType<Movie>(genre.Entries[0]);
        Assert.IsType<MovieCollection>(genre.Entries[1]);
        Assert.IsType<TvShow>(genre.Entries[2]);
        Assert.Equal(TimeSpan.Zero, genre.ExpiresAt.Offset);
        Assert.Equal("/api/home/seasonal-genres", handler.Requests[0].PathAndQuery);
        Assert.Equal("Bearer jwt", handler.Requests[0].Authorization);
    }

    /// <summary>Ein HTTP-Fehler ist keine erfolgreiche leere Saisonantwort.</summary>
    [Fact]
    public async Task ApiContract_EmptyAndErrorRemainDistinct()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "[]");
        handler.Enqueue(HttpStatusCode.NotFound);
        var client = new VideoPlayerApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        Assert.Empty(await client.GetSeasonalGenresAsync());
        await Assert.ThrowsAsync<ApiException>(() => client.GetSeasonalGenresAsync());
    }

    /// <summary>Genau die Servergrenze gilt, unabhängig von lokalen Saisonkalendern.</summary>
    /// <param name="seconds">Abstand zur exklusiven Ablaufgrenze in Sekunden.</param>
    /// <param name="expected">Erwartete Anzahl sichtbarer saisonaler Sektionen.</param>
    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    public async Task CacheProjection_ExpiresAtIsExclusive(int seconds, int expected)
    {
        var expiry = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        clock.Now = expiry.AddSeconds(seconds);
        await WriteAsync([new SeasonalGenre { GenreId = 7, Name = "Halloween", ExpiresAt = expiry, Entries = [new Movie { Id = 91, Name = "Film" }] }]);
        var service = new HomeListService(cache, timeProvider: clock);
        Assert.Equal(expected, (await service.LoadHomeListsAsync()).SeasonalGenres.Count);
    }

    /// <summary>Unbekannte Medien und fehlende Ablaufdaten werden nicht dauerhaft angezeigt.</summary>
    [Fact]
    public async Task CacheProjection_PreservesOrderAndSupportedTypes_HidesEmptyInvalidAndExpired()
    {
        clock.Now = DateTimeOffset.UtcNow;
        var expiry = clock.Now.AddDays(1);
        await WriteAsync([
            new SeasonalGenre { GenreId = 8, Name = "Zuerst", ExpiresAt = expiry, Entries = [new MovieCollection { Id = 2, Name = "Sammlung" }, new TvShow { Id = 3, Name = "Serie" }] },
            new SeasonalGenre { GenreId = 9, Name = "Danach", ExpiresAt = expiry, Entries = [new Movie { Id = 1, Name = "Film", PosterPictureId = 21 }] },
            new SeasonalGenre { GenreId = 10, Name = "Leer", ExpiresAt = expiry },
            new SeasonalGenre { GenreId = 11, Name = "Fehlende Grenze", Entries = [new Movie { Id = 12 }] },
            new SeasonalGenre { GenreId = 12, Name = "Unbekannt", ExpiresAt = expiry, Entries = [new MediaEntry { Id = 13 }] }
        ]);
        var sections = (await new HomeListService(cache, timeProvider: clock).LoadHomeListsAsync()).SeasonalGenres;
        Assert.Equal(new long[] { 8, 9 }, sections.Select(g => g.GenreId));
        Assert.Equal(new[] { "moviecollection", "tvshow" }, sections[0].Items.Select(i => i.MediaType));
        Assert.Equal(21, sections[1].Items[0].PosterPictureId);
        var transition = AppUpdate.Update(new AppModel(), new MediaItemSelectedMessage(sections[0].Items[0]));
        Assert.Equal(2, Assert.IsType<NavigateToMediaDetailsEffect>(Assert.Single(transition.Effects)).MediaId);
    }

    /// <summary>MVU erhält saisonale Inhalte und kann den Cache ohne Netz neu prüfen.</summary>
    [Fact]
    public void HomeModel_SeasonalContentAndReloadAreWired()
    {
        var section = new HomeSeasonalSection { GenreId = 7, Name = "Halloween", ExpiresAt = clock.Now.AddDays(1), Items = [new HomeMediaItem { Id = 1, Title = "Film", MediaType = "movie" }] };
        var model = AppUpdate.Update(new AppModel(), new HomeListsLoadedMessage(new HomeLists { SeasonalGenres = [section] })).Model;
        Assert.True(model.HasHomeContent);
        Assert.Single(model.SeasonalGenres);
        Assert.IsType<LoadHomeListsEffect>(Assert.Single(AppUpdate.Update(model, new HomeListsReloadRequestedMessage()).Effects));
    }

    private Task<int> WriteAsync(SeasonalGenre[] genres) => cache.Connection.InsertOrReplaceAsync(new CachedListEntry
    {
        Key = ServerSyncService.SeasonalGenresCacheKey,
        Json = JsonSerializer.Serialize(genres),
        UpdatedAtUtc = DateTime.UtcNow
    });

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
