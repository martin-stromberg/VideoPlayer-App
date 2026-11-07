using System.Text.Json;
using VideoPlayer.ApiClient.Models;

namespace VideoPlayer.Maui.Tests;

/// <summary>Tests für <c>MediaEntrySerialization</c>.</summary>
public class MediaEntrySerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Prüft MediaEntry: deserialisiert per Typdiscriminator.</summary>
    /// <param name="discriminator">Der Diskriminator.</param>
    /// <param name="expectedType">Der erwartete Typ.</param>
    [Theory]
    [InlineData("mediaEntry", typeof(MediaEntry))]
    [InlineData("movie", typeof(Movie))]
    [InlineData("movieCollection", typeof(MovieCollection))]
    [InlineData("show", typeof(TvShow))]
    [InlineData("season", typeof(TvShowSeason))]
    [InlineData("episode", typeof(TvShowEpisode))]
    public void MediaEntry_DeserializesByTypeDiscriminator(string discriminator, Type expectedType)
    {
        var json = $"{{\"$type\":\"{discriminator}\",\"Id\":1,\"Name\":\"X\",\"MediaSourceId\":2}}";

        var entry = JsonSerializer.Deserialize<MediaEntry>(json, JsonOptions);

        Assert.IsType(expectedType, entry);
    }

    /// <summary>Prüft MediaEntry: serialisiert mit Typdiscriminator.</summary>
    [Fact]
    public void MediaEntry_SerializesWithTypeDiscriminator()
    {
        var json = JsonSerializer.Serialize<MediaEntry>(new Movie { Id = 1, Name = "Film" });

        Assert.Contains("\"$type\":\"movie\"", json);
    }

    /// <summary>Prüft MediaEntry: übernimmt den serverseitigen Gesehen-Zeitpunkt (camelCase-JSON).</summary>
    [Fact]
    public void MediaEntry_DeserializesWatchedAt()
    {
        var json = "{\"$type\":\"episode\",\"Id\":31,\"Name\":\"Pilot\",\"watchedAt\":\"2026-09-20T21:30:00Z\"}";

        var entry = JsonSerializer.Deserialize<MediaEntry>(json, JsonOptions);

        var episode = Assert.IsType<TvShowEpisode>(entry);
        Assert.Equal(new DateTime(2026, 9, 20, 21, 30, 0, DateTimeKind.Utc), episode.WatchedAt);
    }

    /// <summary>Prüft MediaEntry: ohne Gesehen-Feld bleibt der Zeitpunkt leer (ungesehen).</summary>
    [Fact]
    public void MediaEntry_WithoutWatchedAt_StaysNull()
    {
        var json = "{\"$type\":\"episode\",\"Id\":31,\"Name\":\"Pilot\"}";

        var entry = JsonSerializer.Deserialize<MediaEntry>(json, JsonOptions);

        Assert.Null(Assert.IsType<TvShowEpisode>(entry).WatchedAt);
    }
}
