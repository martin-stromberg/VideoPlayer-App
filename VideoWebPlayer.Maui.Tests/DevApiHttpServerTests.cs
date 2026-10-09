// Die Developer-API ist auf DEBUG/DEV_API kompiliert — in Release fehlt
// der Namespace und diese Tests haetten kein Compile-Ziel.
#if DEBUG || DEV_API
#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using VideoPlayer.Mvu.DevApi;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Integraltests des Mini-HTTP-Servers der Developer-API
/// (<see cref="DevApiHttpServer"/>): echter Loopback-Listener auf
/// systemvergebenem Port, JSON-Roundtrip, Statuscodes und mehrere
/// sequentielle Verbindungen.
/// </summary>
public class DevApiHttpServerTests
{
    private static DevApiHttpServer StartServer(out List<DevApiRequest> received)
    {
        var requests = new List<DevApiRequest>();
        var router = new DevApiRouter();
        router.Map("GET", "/model", (_, _) => Task.FromResult(DevApiResponse.Json(new { ok = true })));
        router.Map("POST", "/invoke", (request, _) =>
        {
            requests.Add(request);
            return Task.FromResult(DevApiResponse.Json(new { invoked = true }));
        });
        received = requests;
        var server = new DevApiHttpServer(router);
        server.Start();
        return server;
    }

    /// <summary>Der Server bindet auf einem systemvergebenen Port ungleich 0.</summary>
    [Fact]
    public void Start_AssignsPort()
    {
        using var server = StartServer(out _);

        Assert.True(server.Port > 0);
    }

    /// <summary>GET liefert die vom Handler erzeugte JSON-Antwort.</summary>
    [Fact]
    public async Task Get_ReturnsHandlerResponse()
    {
        using var server = StartServer(out _);
        using var client = new HttpClient();

        var response = await client.GetAsync($"http://127.0.0.1:{server.Port}/model");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"ok\":true", body, StringComparison.Ordinal);
    }

    /// <summary>POST überträgt den JSON-Body an den Handler.</summary>
    [Fact]
    public async Task Post_DeliversJsonBody()
    {
        using var server = StartServer(out var received);
        using var client = new HttpClient();

        var response = await client.PostAsync(
            $"http://127.0.0.1:{server.Port}/invoke",
            new StringContent("{\"automationId\":\"ConnectButton\"}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var request = await WaitForRequestAsync(received);
        Assert.Contains("ConnectButton", request.Body, StringComparison.Ordinal);
    }

    /// <summary>Unbekannte Pfade liefern 404, falsche Methoden 405.</summary>
    [Fact]
    public async Task UnknownPath_Returns404_AndWrongMethod_Returns405()
    {
        using var server = StartServer(out _);
        using var client = new HttpClient();

        var notFound = await client.GetAsync($"http://127.0.0.1:{server.Port}/dispatch");
        var wrongMethod = await client.PostAsync(
            $"http://127.0.0.1:{server.Port}/model",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, wrongMethod.StatusCode);
    }

    /// <summary>Eine syntaktisch kaputte Anfrage wird mit 400 beantwortet.</summary>
    [Fact]
    public async Task MalformedRequest_Returns400()
    {
        using var server = StartServer(out _);
        using var tcp = new System.Net.Sockets.TcpClient();
        await tcp.ConnectAsync(IPAddress.Loopback, server.Port);
        var stream = tcp.GetStream();
        await stream.WriteAsync(Encoding.ASCII.GetBytes("kaputt\r\n\r\n"));

        var buffer = new byte[1024];
        var read = await stream.ReadAsync(buffer);
        var response = Encoding.ASCII.GetString(buffer, 0, read);

        Assert.StartsWith("HTTP/1.1 400", response, StringComparison.Ordinal);
    }

    /// <summary>Mehrere aufeinanderfolgende Verbindungen werden bedient.</summary>
    [Fact]
    public async Task SequentialConnections_AreServed()
    {
        using var server = StartServer(out _);
        using var client = new HttpClient();

        for (var i = 0; i < 3; i++)
        {
            var response = await client.GetAsync($"http://127.0.0.1:{server.Port}/model");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private static async Task<DevApiRequest> WaitForRequestAsync(List<DevApiRequest> received)
    {
        for (var i = 0; i < 50 && received.Count == 0; i++)
        {
            await Task.Delay(20);
        }

        return Assert.Single(received);
    }
}
#endif
