using VideoPlayer.Mvu;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>Tests für <c>AppRoutes</c>.</summary>
public class AppRoutesTests
{
    /// <summary>Prüft MenuPages: enthält alle fünf Menüeinträge in definierter Reihenfolge.</summary>
    [Fact]
    public void MenuPages_ContainsAllFiveMenuEntriesInOrder()
    {
        // Die Quellen sind dynamische Menüeinträge (je freigegebener
        // Quelle) und keine eigene Menüseite mehr.
        Assert.Equal(
            [AppPage.Home, AppPage.Favorites, AppPage.Downloads, AppPage.Profile, AppPage.Settings],
            AppRoutes.MenuPages);
    }

    /// <summary>Prüft ToRoute: bildet alle Menüseiten rundtripfest auf Routen ab.</summary>
    [Fact]
    public void ToRoute_RoundTripsAllMenuPages()
    {
        foreach (var page in AppRoutes.MenuPages)
        {
            var route = AppRoutes.ToRoute(page);
            Assert.False(string.IsNullOrWhiteSpace(route));
            Assert.True(AppRoutes.TryGetPage($"//{route}", out var resolved));
            Assert.Equal(page, resolved);
        }
    }

    /// <summary>Prüft ToRoute: für <c>Loading</c> existiert keine Route.</summary>
    [Fact]
    public void ToRoute_Loading_HasNoRoute()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AppRoutes.ToRoute(AppPage.Loading));
    }

    /// <summary>Prüft TryGetPage: löst Shell-Orte auf die zugehörigen Seiten auf.</summary>
    /// <param name="location">Der Ort.</param>
    /// <param name="expected">Der erwartete Wert.</param>
    [Theory]
    [InlineData("//home", AppPage.Home)]
    [InlineData("//settings?x=1", AppPage.Settings)]
    // Geschobene Routen hängen am Ort des darunterliegenden Menüeintrags —
    // das tiefste Segment bestimmt die angezeigte Seite.
    [InlineData("//settings/serversetup", AppPage.ServerSetup)]
    [InlineData("//home/serversetup", AppPage.ServerSetup)]
    [InlineData("//settings/login", AppPage.Login)]
    [InlineData("//favorites/FavoritesPage", AppPage.Favorites)]
    public void TryGetPage_ResolvesShellLocations(string location, AppPage expected)
    {
        Assert.True(AppRoutes.TryGetPage(location, out var page));
        Assert.Equal(expected, page);
    }

    /// <summary>Prüft TryGetPage: bei unbekanntem Ort → gibt false zurück.</summary>
    /// <param name="location">Der Ort.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("//unknown")]
    // Die ehemalige Menü-Route „sources" existiert nicht mehr — die
    // Quellen sind dynamische Menüeinträge ohne eigene Seite.
    [InlineData("//sources/SourcesPage")]
    public void TryGetPage_UnknownLocation_ReturnsFalse(string? location)
    {
        Assert.False(AppRoutes.TryGetPage(location, out _));
    }
}
