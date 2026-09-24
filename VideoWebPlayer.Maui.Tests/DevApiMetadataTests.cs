#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Text.Json;
using VideoPlayer.Mvu.DevApi;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Einzeltests der Metadatendatei <c>devapi.json</c>
/// (<see cref="DevApiMetadata"/>): camelCase-Felder port/pid/startedUtc.
/// </summary>
public class DevApiMetadataTests
{
    /// <summary>Die geschriebene Datei enthält port, pid und startedUtc in camelCase.</summary>
    [Fact]
    public async Task WriteAsync_WritesExpectedFields()
    {
        var path = Path.Combine(Path.GetTempPath(), $"devapi-{Guid.NewGuid():N}.json");
        try
        {
            var metadata = DevApiMetadata.Create(47123);
            await metadata.WriteAsync(path);

            var json = await File.ReadAllTextAsync(path);
            var parsed = JsonDocument.Parse(json).RootElement;

            Assert.Equal(47123, parsed.GetProperty("port").GetInt32());
            Assert.Equal(Environment.ProcessId, parsed.GetProperty("pid").GetInt32());
            Assert.True(parsed.GetProperty("startedUtc").GetDateTime() <= DateTime.UtcNow);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
