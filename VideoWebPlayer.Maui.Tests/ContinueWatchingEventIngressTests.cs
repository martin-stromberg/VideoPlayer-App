using System.Text.Json;
using VideoWebPlayer.Client.Models;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>Tests für <c>ContinueWatchingEventIngress</c>.</summary>
public class ContinueWatchingEventIngressTests
{
    /// <summary>Prüft ContinueWatchingDto: übersteht den JSON-Vertrag im Roundtrip.</summary>
    [Fact]
    public void ContinueWatchingDto_RoundTripsThroughJsonContract()
    {
        var dto = new ContinueWatchingDto
        {
            MediaType = "movie",
            Entry = new DtoMovie { Id = 10, Name = "Example Movie", MediaSourceId = 7 },
            PositionSeconds = 15,
            DurationSeconds = 5400,
            Title = "Example Movie",
            PosterPictureId = 22
        };

        var json = JsonSerializer.Serialize(dto);
        var roundTrip = JsonSerializer.Deserialize<ContinueWatchingDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(roundTrip);
        Assert.Equal("movie", roundTrip.MediaType);
        Assert.Equal(15, roundTrip.PositionSeconds);
        Assert.Equal(5400, roundTrip.DurationSeconds);
        Assert.Equal("Example Movie", roundTrip.Title);
        Assert.Equal(22, roundTrip.PosterPictureId);
    }

    /// <summary>Prüft MutationResult: verwendet stabile Status- und Nachrichtenfelder.</summary>
    [Fact]
    public void MutationResult_UsesStableStatusAndMessageFields()
    {
        var result = new ContinueWatchingMutationResult("hidden", "Entry hidden.");

        Assert.Equal("hidden", result.Status);
        Assert.Equal("Entry hidden.", result.Message);
    }
}
