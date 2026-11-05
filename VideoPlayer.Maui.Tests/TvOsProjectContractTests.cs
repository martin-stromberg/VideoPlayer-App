using System.Xml.Linq;
using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests für das tvOS-App-Grundgerüst `VideoPlayer.TvOS`. Da das
/// Projekt unter Windows nicht kompiliert werden kann, werden die
/// Akzeptanzkriterien (Solution-Mitgliedschaft, Referenzmenge,
/// TFM-Schalter, macOS-CI-Job) über die Quelldateien geprüft
/// (Muster: `VideoPlayerMauiContractTests`).
/// </summary>
public class TvOsProjectContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string ProjectDirectory => Path.Combine(Root, "VideoPlayer.TvOS");

    private static XDocument LoadProject()
    {
        return XDocument.Load(Path.Combine(ProjectDirectory, "VideoPlayer.TvOS.csproj"));
    }

    /// <summary>Prüft: das tvOS-Projekt ist Mitglied der Solution.</summary>
    [Fact]
    public void TvOsProject_IsPartOfSolution()
    {
        var solution = File.ReadAllText(Path.Combine(Root, "VideoPlayer.App.sln"));

        Assert.Contains("VideoPlayer.TvOS\\VideoPlayer.TvOS.csproj", solution);
    }

    /// <summary>
    /// Prüft: das tvOS-Projekt referenziert ausschließlich die vier
    /// plattformneutralen net10.0-Bibliotheken — keine MAUI-Projekte.
    /// </summary>
    [Fact]
    public void TvOsProject_ReferencesOnlyPlatformNeutralLibraries()
    {
        var references = LoadProject().Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .OrderBy(value => value)
            .ToArray();

        Assert.Equal(
            [
                @"..\msTools.DeveloperApi\msTools.DeveloperApi.csproj",
                @"..\VideoPlayer.ApiClient\VideoPlayer.ApiClient.csproj",
                @"..\VideoPlayer.Mvu\VideoPlayer.Mvu.csproj",
                @"..\VideoPlayer.Storage\VideoPlayer.Storage.csproj"
            ],
            references);
    }

    /// <summary>
    /// Prüft: das tvOS-Projekt referenziert keine NuGet-Pakete und kein
    /// MAUI (UseMaui) — die leere Programmhülle kommt ohne externe
    /// Abhängigkeiten aus.
    /// </summary>
    [Fact]
    public void TvOsProject_HasNoPackageReferencesOrMaui()
    {
        var project = LoadProject();

        Assert.Empty(project.Descendants("PackageReference"));
        Assert.Empty(project.Descendants("UseMaui"));
    }

    /// <summary>
    /// Prüft: `net10.0-tvos` steht hinter dem `IncludeTvOsTarget`-Schalter
    /// mit Nicht-macOS-Fallback — sonst bräche der Windows-Restore der
    /// Solution (tvOS-Workload ist dort nicht installierbar).
    /// </summary>
    [Fact]
    public void TvOsProject_TargetsTvOsOnlyConditionally()
    {
        var content = File.ReadAllText(
            Path.Combine(ProjectDirectory, "VideoPlayer.TvOS.csproj"));

        Assert.Contains("IncludeTvOsTarget", content);
        Assert.Contains("net10.0-tvos", content);
        Assert.Contains(
            "<TargetFramework Condition=\"'$(IncludeTvOsTarget)' != 'true'\">net10.0</TargetFramework>",
            content);
    }

    /// <summary>
    /// Prüft: der tvOS-Einstieg verdrahtet `UIApplication.Main` mit dem
    /// `AppDelegate`, und der Delegate aktiviert die Startansicht.
    /// </summary>
    [Fact]
    public void TvOsApp_StartsWithWindowAndStartView()
    {
        var program = File.ReadAllText(Path.Combine(ProjectDirectory, "Program.cs"));
        var appDelegate = File.ReadAllText(Path.Combine(ProjectDirectory, "AppDelegate.cs"));

        Assert.Contains("UIApplication.Main(args, null, typeof(AppDelegate))", program);
        Assert.Contains("MakeKeyAndVisible", appDelegate);
        Assert.Contains("MainViewController", appDelegate);
    }

    /// <summary>
    /// Prüft: `pr-checks.yml` enthält den macOS-Job `build-tvos` mit
    /// tvOS-Workload-Rollback und Simulator-Build ohne Codesignierung.
    /// </summary>
    [Fact]
    public void PrChecksWorkflow_ContainsTvOsSimulatorBuildJob()
    {
        var workflow = File.ReadAllText(
            Path.Combine(Root, ".github", "workflows", "pr-checks.yml"));

        Assert.Contains("build-tvos:", workflow);
        Assert.Contains("runs-on: macos-latest", workflow);
        Assert.Contains("microsoft.net.sdk.tvos", workflow);
        Assert.Contains("workload-rollback.json", workflow);
        Assert.Contains("dotnet workload install tvos --skip-manifest-update", workflow);
        Assert.Contains("tvossimulator-arm64", workflow);
        Assert.Contains("EnableCodeSigning=false", workflow);
    }
}
