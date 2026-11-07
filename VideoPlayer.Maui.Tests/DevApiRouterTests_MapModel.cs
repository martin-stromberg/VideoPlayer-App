// Die Developer-API ist auf DEBUG/DEV_API kompiliert — in Release fehlt
// der Namespace und diese Tests haetten kein Compile-Ziel.
#if DEBUG || DEV_API
#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using msTools.DeveloperApi;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Einzeltests der <c>GET /model</c>-Registrierung der Developer-API
/// (<see cref="DevApiRouterExtensions.MapModel"/>): der Provider liefert
/// den Snapshot, die Bibliothek kennt kein konkretes Modell.
/// </summary>
public class DevApiRouterTests_MapModel
{
    private static DevApiRequest Request(string method, string path) =>
        new() { Method = method, Path = path };

    /// <summary>GET /model liefert das camelCase-serialisierte Provider-Ergebnis.</summary>
    [Fact]
    public async Task MapModel_ServesProviderSnapshot()
    {
        var router = new DevApiRouter();
        router.MapModel(() => new { currentPage = "Home", count = 3 });

        var response = await router.RouteAsync(Request("GET", "/model"));

        Assert.Equal(200, response.StatusCode);
        Assert.Contains("\"currentPage\":\"Home\"", response.Body, StringComparison.Ordinal);
        Assert.Contains("\"count\":3", response.Body, StringComparison.Ordinal);
    }

    /// <summary>Der Provider wird je Anfrage erneut aufgerufen.</summary>
    [Fact]
    public async Task MapModel_CallsProviderPerRequest()
    {
        var calls = 0;
        var router = new DevApiRouter();
        router.MapModel(() => new { value = ++calls });

        await router.RouteAsync(Request("GET", "/model"));
        var response = await router.RouteAsync(Request("GET", "/model"));

        Assert.Contains("\"value\":2", response.Body, StringComparison.Ordinal);
    }

    /// <summary>POST auf /model liefert 405.</summary>
    [Fact]
    public async Task MapModel_WrongMethod_Returns405()
    {
        var router = new DevApiRouter();
        router.MapModel(() => new { ok = true });

        var response = await router.RouteAsync(Request("POST", "/model"));

        Assert.Equal(405, response.StatusCode);
    }
}
#endif
