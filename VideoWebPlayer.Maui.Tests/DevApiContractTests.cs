using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests der Developer-API: Da das Testprojekt die MAUI-Targets
/// nicht referenzieren kann, werden der Compile-Time-Ausschluss
/// (<c>#if DEBUG || DEV_API</c>), die genau vier Endpunkte (kein
/// <c>dispatch</c>), die Loopback-Bindung und die Metadaten-Ablage über
/// die Quelldateien geprüft (Muster: <see cref="PairingContractTests"/>).
/// </summary>
public class DevApiContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static IEnumerable<string> DevApiFiles()
    {
        foreach (var dir in new[] { "VideoPlayer.Mvu", "VideoPlayer.Maui" })
        {
            var path = Path.Combine(Root, dir, "DevApi");
            foreach (var file in Directory.GetFiles(path, "*.cs"))
            {
                yield return file;
            }
        }
    }

    /// <summary>Es existieren Dev-Api-Quelldateien in beiden Projekten.</summary>
    [Fact]
    public void DevApiFiles_ExistInBothProjects()
    {
        Assert.Contains(DevApiFiles(), f => f.Contains("VideoPlayer.Mvu", StringComparison.Ordinal));
        Assert.Contains(DevApiFiles(), f => f.Contains("VideoPlayer.Maui", StringComparison.Ordinal));
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
    public void DevApiService_MapsExactlyTheFourEndpoints()
    {
        var source = File.ReadAllText(Path.Combine(Root, "VideoPlayer.Maui", "DevApi", "DevApiService.cs"));

        Assert.Equal(4, CountOccurrences(source, "router.Map("));
        Assert.Contains("\"GET\", \"/model\"", source, StringComparison.Ordinal);
        Assert.Contains("\"GET\", \"/tree\"", source, StringComparison.Ordinal);
        Assert.Contains("\"POST\", \"/invoke\"", source, StringComparison.Ordinal);
        Assert.Contains("\"POST\", \"/set-text\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("dispatch", source, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Der HTTP-Server bindet ausschließlich an den Loopback.</summary>
    [Fact]
    public void DevApiHttpServer_BindsLoopbackOnly()
    {
        var source = File.ReadAllText(Path.Combine(Root, "VideoPlayer.Mvu", "DevApi", "DevApiHttpServer.cs"));

        Assert.Contains("IPAddress.Loopback", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IPAddress.Any", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Die Metadatendatei <c>devapi.json</c> wird im Programmverzeichnis
    /// abgelegt und alle UI-Endpunkte marshallen auf den UI-Thread.
    /// </summary>
    [Fact]
    public void DevApiService_WritesMetadataAndMarshallsToUiThread()
    {
        var source = File.ReadAllText(Path.Combine(Root, "VideoPlayer.Maui", "DevApi", "DevApiService.cs"));

        Assert.Contains("AppContext.BaseDirectory", source, StringComparison.Ordinal);
        Assert.Contains("\"devapi.json\"", source, StringComparison.Ordinal);
        Assert.True(CountOccurrences(source, "MainThread.InvokeOnMainThreadAsync") >= 3);
    }

    /// <summary>
    /// Invoke nutzt die Clicked-Äquivalente der Steuerelemente,
    /// set-text den <c>InputView</c>-Setter (feuert <c>TextChanged</c>).
    /// </summary>
    [Fact]
    public void DevApiVisualTree_UsesPlatformInvokeEquivalents()
    {
        var source = File.ReadAllText(Path.Combine(Root, "VideoPlayer.Maui", "DevApi", "DevApiVisualTree.cs"));

        Assert.Contains("SendClicked()", source, StringComparison.Ordinal);
        Assert.Contains("InputView", source, StringComparison.Ordinal);
        Assert.Contains("ItemsSource", source, StringComparison.Ordinal);
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
