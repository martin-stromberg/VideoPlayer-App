using System.Text.RegularExpressions;
using VideoPlayer.ApiClient.Models;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Vertragstests der Enum-Testabdeckung (Git-Hook <c>enum-coverage-check</c>):
/// jeder öffentliche/interne Enum-Wert muss in mindestens einer Testdatei
/// vorkommen. Für referenzierte Projekte wird der Wertebestand zur Laufzeit
/// geprüft; für den Legacy-Altbestand <c>VideoWebPlayer.Maui</c>, den das
/// Testprojekt wegen der MAUI-Zielframeworks nicht referenzieren kann, wird
/// die Enum-Deklaration per Quelltext-Vertrag gelesen und abgeglichen
/// (Muster: <see cref="SourcesContractTests"/>).
/// </summary>
public class EnumCoverageTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));

    /// <summary>
    /// Liest die Wertnamen eines Enums aus einer Quelldatei, damit die
    /// Legacy-Enums des nicht referenzierbaren MAUI-Altbestands trotzdem
    /// einen bruchfesten Vertragstest erhalten.
    /// </summary>
    /// <param name="source">Der Quelltext der Datei, in der das Enum deklariert ist.</param>
    /// <param name="enumName">Der Name des gesuchten Enums.</param>
    /// <returns>Die Liste der im Enum deklarierten Wertnamen.</returns>
    private static IReadOnlyList<string> EnumValuesFromSource(string source, string enumName)
    {
        var match = Regex.Match(
            source,
            @"enum\s+" + Regex.Escape(enumName) + @"\s*(?::\s*[\w.]+)?\s*\{(?<body>[^}]*)\}",
            RegexOptions.Singleline);
        Assert.True(match.Success, $"Enum {enumName} nicht in Quelldatei gefunden.");

        // Kommentare/Attribute erst im Ganzen entfernen: Kommas in XML-Doc-
        // Kommentaren dürfen die Werteliste nicht künstlich zerteilen.
        var body = Regex.Replace(match.Groups["body"].Value, @"//.*|\[.*?\]", string.Empty);

        return body
            .Split(',')
            .Select(part => part.Trim())
            .Select(part => Regex.Match(part, @"^(\w+)").Groups[1].Value)
            .Where(name => name.Length > 0)
            .ToList();
    }

    /// <summary>Prüft RecentEntryKind: alle Werte sind abgedeckt.</summary>
    [Fact]
    public void RecentEntryKind_AllValuesAreCovered()
    {
        // VideoPlayer.ApiClient.Models.RecentEntryKind — direkt referenziert.
        Assert.Equal(
            new[] { "Movie", "MovieCollection", "TvShow", "TvShowSeason", "TvShowEpisode" },
            Enum.GetValues<RecentEntryKind>().Select(value => value.ToString()));
    }

    /// <summary>Prüft RecentEntryType: alle Werte sind abgedeckt.</summary>
    [Fact]
    public void RecentEntryType_AllValuesAreCovered()
    {
        // VideoWebPlayer.Client/Models/DtoRecentEntry.cs — globaler Namespace
        // (Legacy-Datei ohne namespace-Angabe), direkt referenziert.
        Assert.Equal(
            new[] { "Movie", "MovieCollection", "TVShow", "TVShowSeason", "TVShowEpisode" },
            Enum.GetValues<RecentEntryType>().Select(value => value.ToString()));
    }

    /// <summary>Prüft LegacyDownloadRetentionType: alle Werte sind abgedeckt.</summary>
    [Fact]
    public void LegacyDownloadRetentionType_AllValuesAreCovered()
    {
        // VideoWebPlayer.Maui.Models.DownloadRetentionType (Altbestand).
        var source = Read("VideoWebPlayer.Maui", "Models", "DownloadedVideo.cs");

        Assert.Equal(
            new[] { "Cache", "Download", "Watchlist" },
            EnumValuesFromSource(source, "DownloadRetentionType"));
    }

    /// <summary>Prüft LegacyDownloadStatus: alle Werte sind abgedeckt.</summary>
    [Fact]
    public void LegacyDownloadStatus_AllValuesAreCovered()
    {
        // VideoWebPlayer.Maui.Models.DownloadStatus (Altbestand).
        var source = Read("VideoWebPlayer.Maui", "Models", "DownloadedVideo.cs");

        Assert.Equal(
            new[] { "Queued", "Downloading", "Completed", "Failed" },
            EnumValuesFromSource(source, "DownloadStatus"));
    }

    /// <summary>Prüft LegacyVideoSourceType: alle Werte sind abgedeckt.</summary>
    [Fact]
    public void LegacyVideoSourceType_AllValuesAreCovered()
    {
        // VideoWebPlayer.Maui.Models.VideoSourceType (Altbestand).
        var source = Read("VideoWebPlayer.Maui", "Models", "VideoRequest.cs");

        Assert.Equal(
            new[] { "LocalFile", "StreamUrl" },
            EnumValuesFromSource(source, "VideoSourceType"));
    }

    /// <summary>Prüft LegacyCarouselKind: alle Werte sind abgedeckt.</summary>
    [Fact]
    public void LegacyCarouselKind_AllValuesAreCovered()
    {
        // VideoWebPlayer.Maui.ViewModels.CarouselKind (Altbestand).
        var source = Read("VideoWebPlayer.Maui", "ViewModels", "MediaCarouselViewModel.cs");

        Assert.Equal(
            new[] { "ContinueWatching", "Favorites", "RecentEntries", "Downloads", "Other" },
            EnumValuesFromSource(source, "CarouselKind"));
    }
}
