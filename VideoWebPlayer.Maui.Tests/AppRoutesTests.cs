using VideoPlayer.Mvu;

namespace VideoWebPlayer.Maui.Tests;

public class AppRoutesTests
{
    [Fact]
    public void MenuPages_ContainsAllSixMenuEntriesInOrder()
    {
        Assert.Equal(
            [AppPage.Home, AppPage.Sources, AppPage.Favorites, AppPage.Downloads, AppPage.Profile, AppPage.Settings],
            AppRoutes.MenuPages);
    }

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

    [Fact]
    public void ToRoute_Loading_HasNoRoute()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AppRoutes.ToRoute(AppPage.Loading));
    }

    [Theory]
    [InlineData("//home", AppPage.Home)]
    [InlineData("//sources/SourcesPage", AppPage.Sources)]
    [InlineData("//settings?x=1", AppPage.Settings)]
    public void TryGetPage_ResolvesShellLocations(string location, AppPage expected)
    {
        Assert.True(AppRoutes.TryGetPage(location, out var page));
        Assert.Equal(expected, page);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("//unknown")]
    public void TryGetPage_UnknownLocation_ReturnsFalse(string? location)
    {
        Assert.False(AppRoutes.TryGetPage(location, out _));
    }
}
