#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using VideoPlayer.Mvu.DevApi;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Einzeltests der Routen-Tabelle der Developer-API
/// (<see cref="DevApiRouter"/>): Treffer, 404 bei unbekanntem Pfad und
/// 405 bei bekannter Route mit falscher Methode.
/// </summary>
public class DevApiRouterTests
{
    private static DevApiRequest Request(string method, string path, string body = "") =>
        new() { Method = method, Path = path, Body = body };

    /// <summary>Eine gemappte Route ruft ihren Handler auf.</summary>
    [Fact]
    public async Task RouteAsync_CallsMappedHandler()
    {
        var router = new DevApiRouter();
        router.Map("GET", "/model", (_, _) => Task.FromResult(DevApiResponse.Json(new { ok = true })));

        var response = await router.RouteAsync(Request("GET", "/model"));

        Assert.Equal(200, response.StatusCode);
        Assert.Contains("\"ok\":true", response.Body, StringComparison.Ordinal);
    }

    /// <summary>Ein unbekannter Pfad liefert 404.</summary>
    [Fact]
    public async Task RouteAsync_Returns404ForUnknownPath()
    {
        var router = new DevApiRouter();
        router.Map("GET", "/model", (_, _) => Task.FromResult(DevApiResponse.Json(new { })));

        var response = await router.RouteAsync(Request("GET", "/unbekannt"));

        Assert.Equal(404, response.StatusCode);
    }

    /// <summary>Ein bekannter Pfad mit falscher Methode liefert 405.</summary>
    [Fact]
    public async Task RouteAsync_Returns405ForWrongMethod()
    {
        var router = new DevApiRouter();
        router.Map("GET", "/model", (_, _) => Task.FromResult(DevApiResponse.Json(new { })));

        var response = await router.RouteAsync(Request("POST", "/model"));

        Assert.Equal(405, response.StatusCode);
    }

    /// <summary>Pfade mit abschließendem Schrägstrich werden wie die Route behandelt.</summary>
    [Fact]
    public async Task RouteAsync_ToleratesTrailingSlash()
    {
        var router = new DevApiRouter();
        router.Map("GET", "/tree", (_, _) => Task.FromResult(DevApiResponse.Json(new { })));

        var response = await router.RouteAsync(Request("GET", "/tree/"));

        Assert.Equal(200, response.StatusCode);
    }
}
