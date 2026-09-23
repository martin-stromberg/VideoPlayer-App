using VideoPlayer.Mvu;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>Tests für <c>AppUpdate.Update</c> – Bereich Navigation.</summary>
public class AppUpdateTests_Navigation
{
    /// <summary>Alle Menüseiten als Theory-Datenquelle.</summary>
    /// <value>Je ein Argumentensatz pro Menüseite.</value>
    public static IEnumerable<object[]> MenuPages =>
        AppRoutes.MenuPages.Select(page => new object[] { page });

    /// <summary>Prüft Update: bei <c>NavigateTo</c> → setzt Seite und löst Effekt aus.</summary>
    /// <param name="page">Die Seite.</param>
    [Theory]
    [MemberData(nameof(MenuPages))]
    public void Update_NavigateTo_SetsPageAndEmitsEffect(AppPage page)
    {
        var model = new AppModel { CurrentPage = AppPage.Home, IsStartupRunning = false };

        var transition = AppUpdate.Update(model, new NavigateToMessage(page));

        Assert.Equal(page, transition.Model.CurrentPage);
        if (page == AppPage.Home)
        {
            Assert.Empty(transition.Effects);
        }
        else
        {
            var effect = Assert.Single(transition.Effects);
            Assert.Equal(page, Assert.IsType<NavigateToPageEffect>(effect).Page);
        }
    }

    /// <summary>Prüft Update: bei <c>NavigateTo</c> auf gleicher Seite → kein Effekt.</summary>
    [Fact]
    public void Update_NavigateTo_SamePage_NoEffect()
    {
        var model = new AppModel { CurrentPage = AppPage.Settings, IsStartupRunning = false };

        var transition = AppUpdate.Update(model, new NavigateToMessage(AppPage.Settings));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Update: bei <c>NavigateTo</c> nach <c>Loading</c> → wird ignoriert.</summary>
    [Fact]
    public void Update_NavigateTo_Loading_IsIgnored()
    {
        var model = new AppModel { CurrentPage = AppPage.Home, IsStartupRunning = false };

        var transition = AppUpdate.Update(model, new NavigateToMessage(AppPage.Loading));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Update: bei einer Navigated-Nachricht → aktualisiert nur das Model.</summary>
    [Fact]
    public void Update_NavigatedMessage_OnlyUpdatesModel()
    {
        var model = new AppModel { CurrentPage = AppPage.Home, IsStartupRunning = false };

        var transition = AppUpdate.Update(model, new NavigatedMessage(AppPage.Downloads));

        Assert.Equal(AppPage.Downloads, transition.Model.CurrentPage);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Update: bei einer Navigated-Nachricht auf gleicher Seite → keine Änderung.</summary>
    [Fact]
    public void Update_NavigatedMessage_SamePage_NoChange()
    {
        var model = new AppModel { CurrentPage = AppPage.Profile, IsStartupRunning = false };

        var transition = AppUpdate.Update(model, new NavigatedMessage(AppPage.Profile));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }
}
