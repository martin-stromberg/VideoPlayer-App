using System.Text.Json;
using VideoPlayer.ApiClient.Models;

namespace VideoWebPlayer.Maui.Tests;

public class MediaEntrySerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

    [Fact]
    public void MediaEntry_SerializesWithTypeDiscriminator()
    {
        var json = JsonSerializer.Serialize<MediaEntry>(new Movie { Id = 1, Name = "Film" });

        Assert.Contains("\"$type\":\"movie\"", json);
    }
}
