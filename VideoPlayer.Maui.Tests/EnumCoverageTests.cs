using VideoPlayer.ApiClient.Models;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Vertragstests der Enum-Testabdeckung (Git-Hook <c>enum-coverage-check</c>):
/// jeder öffentliche/interne Enum-Wert muss in mindestens einer Testdatei
/// vorkommen. Für referenzierte Projekte wird der Wertebestand zur Laufzeit
/// geprüft.
/// </summary>
public class EnumCoverageTests
{
    /// <summary>Prüft RecentEntryKind: alle Werte sind abgedeckt.</summary>
    [Fact]
    public void RecentEntryKind_AllValuesAreCovered()
    {
        // VideoPlayer.ApiClient.Models.RecentEntryKind — direkt referenziert.
        Assert.Equal(
            new[] { "Movie", "MovieCollection", "TvShow", "TvShowSeason", "TvShowEpisode" },
            Enum.GetValues<RecentEntryKind>().Select(value => value.ToString()));
    }

    /// <summary>Prüft PlaybackSourceKind: alle Werte sind abgedeckt.</summary>
    [Fact]
    public void PlaybackSourceKind_AllValuesAreCovered()
    {
        // VideoPlayer.Mvu.PlaybackSourceKind — direkt referenziert.
        Assert.Equal(
            new[] { "ServerStream", "LocalFile" },
            Enum.GetValues<VideoPlayer.Mvu.PlaybackSourceKind>().Select(value => value.ToString()));
    }

    /// <summary>Prüft HealthCheckResult: alle Werte sind abgedeckt.</summary>
    [Fact]
    public void HealthCheckResult_AllValuesAreCovered()
    {
        // VideoPlayer.ApiClient.HealthCheckResult — direkt referenziert.
        Assert.Equal(
            new[] { "Reachable", "GateRejected", "Unreachable" },
            Enum.GetValues<VideoPlayer.ApiClient.HealthCheckResult>().Select(value => value.ToString()));
    }

    /// <summary>Prüft PairingStatus: alle Werte sind abgedeckt.</summary>
    [Fact]
    public void PairingStatus_AllValuesAreCovered()
    {
        // VideoPlayer.Mvu.PairingStatus — direkt referenziert.
        Assert.Equal(
            new[] { "Succeeded", "InvalidCode", "RateLimited", "InvalidRequest", "NotSupported", "ServerUnreachable", "Failed" },
            Enum.GetValues<VideoPlayer.Mvu.PairingStatus>().Select(value => value.ToString()));
    }
}
