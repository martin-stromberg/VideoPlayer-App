// Die Developer-API ist auf DEBUG/DEV_API kompiliert — in Release fehlt
// der Namespace und diese Tests haetten kein Compile-Ziel.
#if DEBUG || DEV_API
#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Text;
using VideoPlayer.Mvu.DevApi;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Einzeltests des HTTP-Anfrage-Parsers der Developer-API
/// (<see cref="DevApiRequestReader"/>, plattformneutral in
/// <c>VideoPlayer.Mvu</c>).
/// </summary>
public class DevApiRequestReaderTests
{
    private static MemoryStream StreamOf(string request) =>
        new(Encoding.UTF8.GetBytes(request));

    /// <summary>GET ohne Body wird in Methode, Pfad und Header zerlegt.</summary>
    [Fact]
    public async Task ReadAsync_ParsesGetRequest()
    {
        var request = await DevApiRequestReader.ReadAsync(
            StreamOf("GET /model HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n"));

        Assert.NotNull(request);
        Assert.Equal("GET", request.Method);
        Assert.Equal("/model", request.Path);
        Assert.Equal("127.0.0.1", request.Headers["Host"]);
        Assert.Equal(string.Empty, request.Body);
    }

    /// <summary>POST mit JSON-Body liefert den Body-Inhalt.</summary>
    [Fact]
    public async Task ReadAsync_ParsesPostBody()
    {
        var body = "{\"automationId\":\"ConnectButton\"}";
        var request = await DevApiRequestReader.ReadAsync(StreamOf(
            $"POST /invoke HTTP/1.1\r\nContent-Length: {Encoding.UTF8.GetByteCount(body)}\r\n\r\n{body}"));

        Assert.NotNull(request);
        Assert.Equal("POST", request.Method);
        Assert.Equal("/invoke", request.Path);
        Assert.Equal(body, request.Body);
    }

    /// <summary>Header-Namen sind ohne Beachtung der Groß-/Kleinschreibung lesbar.</summary>
    [Fact]
    public async Task ReadAsync_HeadersAreCaseInsensitive()
    {
        var request = await DevApiRequestReader.ReadAsync(
            StreamOf("GET /tree HTTP/1.1\r\ncontent-length: 0\r\n\r\n"));

        Assert.NotNull(request);
        Assert.True(request.Headers.ContainsKey("Content-Length"));
    }

    /// <summary>Ein Query-Anteil wird aus dem Pfad entfernt.</summary>
    [Fact]
    public async Task ReadAsync_StripsQueryFromPath()
    {
        var request = await DevApiRequestReader.ReadAsync(
            StreamOf("GET /model?verbose=1 HTTP/1.1\r\n\r\n"));

        Assert.NotNull(request);
        Assert.Equal("/model", request.Path);
    }

    /// <summary>Sauberes Verbindungsende ohne Daten liefert null statt eines Fehlers.</summary>
    [Fact]
    public async Task ReadAsync_ReturnsNullOnCleanEnd()
    {
        var request = await DevApiRequestReader.ReadAsync(StreamOf(string.Empty));

        Assert.Null(request);
    }

    /// <summary>Eine kaputte Request-Line löst eine Parse-Ausnahme aus.</summary>
    [Fact]
    public async Task ReadAsync_ThrowsOnMalformedRequestLine()
    {
        await Assert.ThrowsAsync<DevApiRequestException>(() =>
            DevApiRequestReader.ReadAsync(StreamOf("kaputt\r\n\r\n")));
    }

    /// <summary>Ein ungültiger Content-Length-Header löst eine Parse-Ausnahme aus.</summary>
    [Fact]
    public async Task ReadAsync_ThrowsOnInvalidContentLength()
    {
        await Assert.ThrowsAsync<DevApiRequestException>(() =>
            DevApiRequestReader.ReadAsync(StreamOf("POST /invoke HTTP/1.1\r\nContent-Length: abc\r\n\r\n")));
    }

    /// <summary>Ein übergroßer Body wird abgelehnt statt gelesen.</summary>
    [Fact]
    public async Task ReadAsync_ThrowsOnOversizedBody()
    {
        var tooLarge = DevApiRequestReader.MaxBodyBytes + 1;
        await Assert.ThrowsAsync<DevApiRequestException>(() =>
            DevApiRequestReader.ReadAsync(StreamOf($"POST /invoke HTTP/1.1\r\nContent-Length: {tooLarge}\r\n\r\n")));
    }

    /// <summary>Body-Bytes dürfen bereits im selben TCP-Segment wie der Header ankommen.</summary>
    [Fact]
    public async Task ReadAsync_ReadsBodyFromSameSegment()
    {
        var body = "{\"automationId\":\"X\",\"text\":\"abc\"}";
        var request = await DevApiRequestReader.ReadAsync(StreamOf(
            $"POST /set-text HTTP/1.1\r\nContent-Length: {Encoding.UTF8.GetByteCount(body)}\r\n\r\n{body}"));

        Assert.NotNull(request);
        Assert.True(request.TryGetJsonBody<TestBody>(out var parsed, out _));
        Assert.Equal("X", parsed!.AutomationId);
        Assert.Equal("abc", parsed.Text);
    }

    private sealed class TestBody
    {
        public string? AutomationId { get; set; }

        public string? Text { get; set; }
    }
}
#endif
