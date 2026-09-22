using System.Xml.Linq;
using VideoPlayer.Mvu;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests für das neue MVU-App-Projekt `VideoPlayer.Maui`. Da das
/// Testprojekt keine MAUI-Targets referenzieren kann, werden Struktur und
/// Verdrahtung über die Quelldateien geprüft (Muster: `MauiSettingsContractTests`).
/// </summary>
public class VideoPlayerMauiContractTests
{
    private static readonly string Root = RepositoryRoot.Find();
    private static readonly XNamespace MauiNs = "http://schemas.microsoft.com/dotnet/2021/maui";

    // Die Quellen stehen als dynamische Einträge je freigegebener Quelle
    // im Menü (aufgebaut in AppShell.xaml.cs) — die statischen
    // FlyoutItem-Einträge sind nur die fünf Menüseiten.
    private static readonly (string Title, AppPage Page)[] ExpectedMenuEntries =
    [
        ("Startseite", AppPage.Home),
        ("Favoriten", AppPage.Favorites),
        ("Downloads", AppPage.Downloads),
        ("Profil", AppPage.Profile),
        ("Einstellungen", AppPage.Settings)
    ];

    private static XDocument LoadAppShell()
    {
        return XDocument.Load(Path.Combine(Root, "VideoPlayer.Maui", "AppShell.xaml"));
    }

    /// <summary>Prüft AppShell: verwendet Flyout-Navigation.</summary>
    [Fact]
    public void AppShell_IsFlyoutNavigation()
    {
        var shell = LoadAppShell().Root!;

        Assert.Equal("Flyout", (string?)shell.Attribute("FlyoutBehavior"));
    }

    /// <summary>Prüft AppShell: enthält alle Menüeinträge.</summary>
    [Fact]
    public void AppShell_ContainsAllMenuEntries()
    {
        var shell = LoadAppShell().Root!;
        var flyoutItems = shell.Elements(MauiNs + "FlyoutItem").ToList();

        Assert.Equal(ExpectedMenuEntries.Length, flyoutItems.Count);
        for (var i = 0; i < ExpectedMenuEntries.Length; i++)
        {
            var (title, page) = ExpectedMenuEntries[i];
            Assert.Equal(title, (string?)flyoutItems[i].Attribute("Title"));
            Assert.Equal(AppRoutes.ToRoute(page), (string?)flyoutItems[i].Attribute("Route"));
        }
    }

    /// <summary>Prüft AppShell: die Menüeinträge binden auf vorhandene Seiten.</summary>
    [Fact]
    public void AppShell_MenuEntries_BindToExistingPages()
    {
        var shell = LoadAppShell().Root!;
        var contentTemplates = shell
            .Elements(MauiNs + "FlyoutItem")
            .Elements(MauiNs + "ShellContent")
            .Select(e => (string?)e.Attribute("ContentTemplate"))
            .ToList();

        Assert.Equal(ExpectedMenuEntries.Length, contentTemplates.Count);
        foreach (var template in contentTemplates)
        {
            Assert.NotNull(template);
            var match = System.Text.RegularExpressions.Regex.Match(
                template!, @"\{DataTemplate\s+pages:(\w+)\}");
            Assert.True(match.Success, $"Unerwartetes ContentTemplate: {template}");
            var pageClass = match.Groups[1].Value;
            Assert.True(
                File.Exists(Path.Combine(Root, "VideoPlayer.Maui", "Pages", $"{pageClass}.xaml")),
                $"Seite {pageClass}.xaml fehlt");
            Assert.NotEqual("LoadingPage", pageClass);
        }
    }

    /// <summary>Prüft NewApp: das Projekt zielt auf die erforderlichen Plattformen.</summary>
    [Fact]
    public void NewApp_ProjectTargetsRequiredPlatforms()
    {
        var csproj = File.ReadAllText(
            Path.Combine(Root, "VideoPlayer.Maui", "VideoPlayer.Maui.csproj"));

        Assert.Contains("net10.0-ios", csproj, StringComparison.Ordinal);
        Assert.Contains("net10.0-windows10.0.19041.0", csproj, StringComparison.Ordinal);
        Assert.Contains("VideoPlayer.ApiClient.csproj", csproj, StringComparison.Ordinal);
        Assert.Contains("VideoPlayer.Mvu.csproj", csproj, StringComparison.Ordinal);
    }

    /// <summary>Prüft NewApp: referenziert keine Legacy- oder externen MVU-Frameworks.</summary>
    [Fact]
    public void NewApp_DoesNotReferenceLegacyOrExternalMvuFrameworks()
    {
        var csproj = File.ReadAllText(
            Path.Combine(Root, "VideoPlayer.Maui", "VideoPlayer.Maui.csproj"));

        Assert.DoesNotContain("VideoWebPlayer", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Fabulous", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Comet", csproj, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Prüft NewApp: der Health-Check ist an den API-Client verdrahtet.</summary>
    [Fact]
    public void NewApp_HealthCheckWiredToApiClient()
    {
        var probe = File.ReadAllText(
            Path.Combine(Root, "VideoPlayer.Maui", "Services", "ServerHealthProbe.cs"));
        var effectHandler = File.ReadAllText(
            Path.Combine(Root, "VideoPlayer.Maui", "Services", "ShellAppEffectHandler.cs"));

        Assert.Contains("VideoPlayerApiClient", probe, StringComparison.Ordinal);
        Assert.Contains("CheckHealthAsync", probe, StringComparison.Ordinal);
        Assert.Contains("CheckServerHealthEffect", effectHandler, StringComparison.Ordinal);
        Assert.Contains("ServerHealthCheckCompletedMessage", effectHandler, StringComparison.Ordinal);
    }
}
