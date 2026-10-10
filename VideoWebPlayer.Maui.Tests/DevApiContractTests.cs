using System.Text.RegularExpressions;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests der Developer-API: Da das Testprojekt die MAUI-Targets
/// nicht referenzieren kann, werden der Compile-Time-Ausschluss
/// (<c>#if DEBUG || DEV_API</c>), die genau vier Endpunkte (kein
/// <c>dispatch</c>), die Loopback-Bindung, die Metadaten-Ablage und die
/// Bibliotheks-Grenzen (Kern ohne MAUI-Abhängigkeit) über die
/// Quelldateien geprüft (Muster: <see cref="PairingContractTests"/>).
/// </summary>
public class DevApiContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static IEnumerable<string> DevApiFiles()
    {
        foreach (var dir in new[]
        {
            "msTools.DeveloperApi",
            "msTools.DeveloperApi.Maui",
            Path.Combine("VideoPlayer.Mvu", "DevApi"),
            Path.Combine("VideoPlayer.Maui", "DevApi")
        })
        {
            var path = Path.Combine(Root, dir);
            foreach (var file in Directory.GetFiles(path, "*.cs"))
            {
                yield return file;
            }
        }
    }

    /// <summary>Es existieren Dev-Api-Quelldateien in Kern, MAUI-Bibliothek und App.</summary>
    [Fact]
    public void DevApiFiles_ExistInLibrariesAndApp()
    {
        Assert.Contains(DevApiFiles(), f => f.Contains("msTools.DeveloperApi.Maui", StringComparison.Ordinal));
        Assert.Contains(DevApiFiles(), f => f.Contains(Path.Combine("msTools.DeveloperApi", "DevApi"), StringComparison.Ordinal));
        Assert.Contains(DevApiFiles(), f => f.Contains(Path.Combine("VideoPlayer.Maui", "DevApi"), StringComparison.Ordinal));
    }

    /// <summary>
    /// Jede Dev-Api-Datei ist vollständig in <c>#if DEBUG || DEV_API</c>
    /// eingeschlossen – der Release-Build enthält die API nicht.
    /// </summary>
    [Fact]
    public void DevApiFiles_AreFullyWrappedInDebugOrDevApiIfdef()
    {
        foreach (var file in DevApiFiles())
        {
            var lines = File.ReadAllLines(file);
            Assert.Equal("#if DEBUG || DEV_API", lines[0]);
            Assert.Equal("#endif", lines[^1].TrimEnd());
        }
    }

    /// <summary>
    /// Die Verdrahtung in <c>MauiProgram.cs</c> und <c>App.xaml.cs</c>
    /// liegt innerhalb von <c>#if</c>-Blöcken (kein Release-Codepfad).
    /// </summary>
    [Fact]
    public void DevApiWiring_IsInsideIfdefBlocks()
    {
        foreach (var file in new[] { "MauiProgram.cs", Path.Combine("App.xaml.cs") })
        {
            var lines = File.ReadAllLines(Path.Combine(Root, "VideoPlayer.Maui", file));
            var depth = 0;
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("#if", StringComparison.Ordinal))
                {
                    depth++;
                }
                else if (trimmed.StartsWith("#endif", StringComparison.Ordinal))
                {
                    depth--;
                }

                if (trimmed.Contains("DevApiService", StringComparison.Ordinal))
                {
                    Assert.True(depth > 0, $"{file}: DevApiService-Verweis außerhalb eines #if-Blocks: {trimmed}");
                }
            }
        }
    }

    /// <summary>
    /// Die API umfasst genau die vier festgelegten Endpunkte; ein
    /// <c>dispatch</c>-Endpunkt existiert nicht (Projekt-Entscheid OP-5).
    /// </summary>
    [Fact]
    public void MauiDevApiService_MapsExactlyTheFourEndpoints()
    {
        var source = File.ReadAllText(Path.Combine(Root, "msTools.DeveloperApi.Maui", "MauiDevApiService.cs"));

        Assert.Equal(4, CountOccurrences(source, "Map(") + CountOccurrences(source, "MapModel("));
        Assert.Contains("MapModel(", source, StringComparison.Ordinal);
        Assert.Contains("\"GET\", \"/tree\"", source, StringComparison.Ordinal);
        Assert.Contains("\"POST\", \"/invoke\"", source, StringComparison.Ordinal);
        Assert.Contains("\"POST\", \"/set-text\"", source, StringComparison.Ordinal);
        foreach (var file in DevApiFiles())
        {
            Assert.DoesNotContain("dispatch", File.ReadAllText(file), StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Der HTTP-Server bindet ausschließlich an den Loopback.</summary>
    [Fact]
    public void DevApiHttpServer_BindsLoopbackOnly()
    {
        var source = File.ReadAllText(Path.Combine(Root, "msTools.DeveloperApi", "DevApiHttpServer.cs"));

        Assert.Contains("IPAddress.Loopback", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IPAddress.Any", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Die Metadatendatei <c>devapi.json</c> wird im Programmverzeichnis
    /// abgelegt und alle UI-Endpunkte marshallen auf den UI-Thread.
    /// </summary>
    [Fact]
    public void MauiDevApiService_WritesMetadataAndMarshallsToUiThread()
    {
        var source = File.ReadAllText(Path.Combine(Root, "msTools.DeveloperApi.Maui", "MauiDevApiService.cs"));

        Assert.Contains("AppContext.BaseDirectory", source, StringComparison.Ordinal);
        Assert.Contains("FileSystem.AppDataDirectory", source, StringComparison.Ordinal);
        Assert.True(CountOccurrences(source, "MainThread.InvokeOnMainThreadAsync") >= 3);

        var serverSource = File.ReadAllText(Path.Combine(Root, "msTools.DeveloperApi", "DevApiServer.cs"));
        Assert.Contains("\"devapi.json\"", serverSource, StringComparison.Ordinal);
    }

    /// <summary>
    /// Invoke nutzt die Clicked-Äquivalente der Steuerelemente,
    /// set-text den <c>InputView</c>-Setter (feuert <c>TextChanged</c>).
    /// </summary>
    [Fact]
    public void DevApiVisualTree_UsesPlatformInvokeEquivalents()
    {
        var source = File.ReadAllText(Path.Combine(Root, "msTools.DeveloperApi.Maui", "DevApiVisualTree.cs"));

        Assert.Contains("SendClicked()", source, StringComparison.Ordinal);
        Assert.Contains("InputView", source, StringComparison.Ordinal);
        Assert.Contains("ItemsSource", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Der Kern <c>msTools.DeveloperApi</c> bleibt frei von MAUI-
    /// Abhängigkeiten und öffnet keine Internals für Fremdassemblies.
    /// </summary>
    [Fact]
    public void MsToolsDeveloperApi_HasNoMauiDependency()
    {
        var csproj = File.ReadAllText(Path.Combine(Root, "msTools.DeveloperApi", "msTools.DeveloperApi.csproj"));

        Assert.DoesNotContain("UseMaui", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Maui", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("InternalsVisibleTo", csproj, StringComparison.Ordinal);
    }

    /// <summary>
    /// In den Bibliotheksquellen existiert kein generischer
    /// <c>dispatch</c>-Endpunkt und keine Dispatch-Benennung.
    /// </summary>
    [Fact]
    public void MsToolsDeveloperApi_HasNoDispatchEndpoint()
    {
        foreach (var dir in new[] { "msTools.DeveloperApi", "msTools.DeveloperApi.Maui" })
        {
            foreach (var file in Directory.GetFiles(Path.Combine(Root, dir), "*.cs"))
            {
                Assert.DoesNotContain("dispatch", File.ReadAllText(file), StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>Die MAUI-Bibliothek baut auf dem Kern auf.</summary>
    [Fact]
    public void MsToolsDeveloperApiMaui_ReferencesCore()
    {
        var csproj = File.ReadAllText(Path.Combine(Root, "msTools.DeveloperApi.Maui", "msTools.DeveloperApi.Maui.csproj"));

        Assert.Contains("msTools.DeveloperApi.csproj", csproj, StringComparison.Ordinal);
    }

    /// <summary>
    /// Die Bibliotheken enthalten keine geheimhaltungsbedürftigen
    /// Bezeichner (prüfbare Namenskonvention: kein token/password/secret
    /// als eigenes Wort — <c>CancellationToken</c>/<c>.Token</c> zählen
    /// nicht).
    /// </summary>
    [Fact]
    public void MsToolsDeveloperApi_HasNoSecretSurface()
    {
        foreach (var dir in new[] { "msTools.DeveloperApi", "msTools.DeveloperApi.Maui" })
        {
            foreach (var file in Directory.GetFiles(Path.Combine(Root, dir), "*.cs"))
            {
                var source = File.ReadAllText(file);
                Assert.DoesNotMatch(
                    new Regex(@"(?<!\.)\b(token|password|secret)\b", RegexOptions.IgnoreCase),
                    source);
            }
        }
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
