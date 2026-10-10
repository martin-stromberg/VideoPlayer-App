// Die Developer-API ist auf DEBUG/DEV_API kompiliert — in Release fehlt
// der Namespace und diese Tests haetten kein Compile-Ziel.
#if DEBUG || DEV_API
#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Text.Json;
using msTools.DeveloperApi;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Integraltests der Server-Fassade der Developer-API
/// (<see cref="DevApiServer"/>): Port-Vergabe und Ablage der
/// Metadatendatei <c>devapi.json</c> im Primär- bzw. Fallback-Verzeichnis.
/// </summary>
public class DevApiServerTests_Metadata
{
    private static DevApiServer StartServer(string metadataDirectory, string? fallbackDirectory = null)
    {
        var router = new DevApiRouter();
        router.MapModel(() => new { ok = true });
        var server = new DevApiServer(router, metadataDirectory, fallbackDirectory);
        server.Start();
        return server;
    }

    /// <summary>Der Server vergibt nach Start einen Port ungleich 0.</summary>
    [Fact]
    public void Start_AssignsPort()
    {
        using var server = StartServer(Path.GetTempPath());

        Assert.True(server.Port > 0);
    }

    /// <summary>devapi.json wird mit port/pid/startedUtc ins Primärverzeichnis geschrieben.</summary>
    [Fact]
    public async Task Start_WritesMetadataToPrimaryDirectory()
    {
        var directory = Directory.CreateTempSubdirectory("devapi-primary-");
        try
        {
            using var server = StartServer(directory.FullName);
            var path = await WaitForMetadataAsync(server, Path.Combine(directory.FullName, "devapi.json"));

            var parsed = JsonDocument.Parse(await File.ReadAllTextAsync(path)).RootElement;
            Assert.Equal(server.Port, parsed.GetProperty("port").GetInt32());
            Assert.Equal(Environment.ProcessId, parsed.GetProperty("pid").GetInt32());
            Assert.Equal(server.MetadataPath, path);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    /// <summary>Ist das Primärverzeichnis nicht schreibbar, landet devapi.json im Fallback.</summary>
    [Fact]
    public async Task Start_UsesFallbackDirectory_WhenPrimaryFails()
    {
        var fallback = Directory.CreateTempSubdirectory("devapi-fallback-");
        var blockerPath = Path.Combine(Path.GetTempPath(), $"devapi-blocker-{Guid.NewGuid():N}");
        await File.WriteAllTextAsync(blockerPath, "blockiert");
        try
        {
            // Als „Verzeichnis" wird eine existierende Datei übergeben —
            // Path.Combine+Write schlägt fehl, der Fallback greift.
            using var server = StartServer(blockerPath, fallback.FullName);
            var path = await WaitForMetadataAsync(server, Path.Combine(fallback.FullName, "devapi.json"));

            Assert.Equal(server.MetadataPath, path);
        }
        finally
        {
            fallback.Delete(recursive: true);
            File.Delete(blockerPath);
        }
    }

    private static async Task<string> WaitForMetadataAsync(DevApiServer server, string expectedPath)
    {
        // Die Datei wird asynchron geschrieben; sowohl Dateisystem als
        // auch MetadataPath können dem sichtbaren File-Write hinterherhinken.
        for (var i = 0; i < 100 && (!File.Exists(expectedPath) || server.MetadataPath is null); i++)
        {
            await Task.Delay(20);
        }

        Assert.True(File.Exists(expectedPath), $"devapi.json erschien nicht rechtzeitig unter {expectedPath}.");
        return expectedPath;
    }
}
#endif
