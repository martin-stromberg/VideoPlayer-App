#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VideoPlayer.ApiClient;
using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Tests für <c>DevicePairingClient.BootstrapAsync</c>: der öffentliche
/// Bootstrap-Endpunkt (<c>POST api/pairing/bootstrap</c>) wird ohne
/// Gate-/Bearer-Header und ohne Wiederholungen angesprochen; das
/// verschlüsselte Session-Paket (Geräte-Token, Anmelde-Token, Ablauf,
/// Refresh-Token) wird per ECDH+AES-256-GCM entschlüsselt — derselbe
/// Handshake wie <c>exchange</c>.
/// </summary>
public class DevicePairingClientBootstrapTests
{
    /// <summary>
    /// Responder, der den Server simuliert: importiert den öffentlichen
    /// Geräte-Schlüssel aus dem Request-Body, verschlüsselt das
    /// Session-Paket (JSON) per ECDH+AES-256-GCM und liefert die
    /// Bootstrap-Antwort <c>{serverPublicKey, encryptedPayload}</c>.
    /// </summary>
    /// <param name="payloadJson">Das zu verschlüsselnde Session-Paket (JSON).</param>
    /// <returns>Ein Responder-Delegate für den Stub-Handler.</returns>
    private static Func<HttpRequestMessage, HttpResponseMessage> BootstrapResponder(string payloadJson)
    {
        return request =>
        {
            var body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            var clientPublicKey = JsonDocument.Parse(body).RootElement
                .GetProperty("clientPublicKey").GetString()!;

            using var serverKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using var clientKey = ECDiffieHellman.Create();
            clientKey.ImportSubjectPublicKeyInfo(Convert.FromBase64String(clientPublicKey), out _);
            var sharedKey = serverKey.DeriveKeyFromHmac(
                clientKey.PublicKey, HashAlgorithmName.SHA256, null, null, null);

            var plaintext = Encoding.UTF8.GetBytes(payloadJson);
            var nonce = RandomNumberGenerator.GetBytes(12);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[16];
            using (var aes = new AesGcm(sharedKey, 16))
            {
                aes.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            var payload = Convert.ToBase64String(nonce.Concat(ciphertext).Concat(tag).ToArray());
            var json = JsonSerializer.Serialize(new
            {
                serverPublicKey = Convert.ToBase64String(serverKey.ExportSubjectPublicKeyInfo()),
                encryptedPayload = payload
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        };
    }

    private static string SessionPayload(
        string deviceToken = "device-token-1",
        string token = "jwt-token",
        string expires = "2030-01-01T00:00:00Z",
        string refreshToken = "refresh-1") =>
        JsonSerializer.Serialize(new
        {
            deviceToken,
            token,
            expires,
            refreshToken
        });

    /// <summary>Prüft BootstrapAsync: sendet POST an den Bootstrap-Endpunkt ohne Gate- und Bearer-Header.</summary>
    [Fact]
    public async Task BootstrapAsync_SendsRequest_WithoutGateAndBearerHeaders()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(BootstrapResponder(SessionPayload()));
        var client = new DevicePairingClient(new HttpClient(handler));

        await client.BootstrapAsync(new Uri("http://testserver/"), "ticket-1", "Testgerät");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/pairing/bootstrap", request.PathAndQuery);
        Assert.Null(request.ApiKey);
        Assert.Null(request.Authorization);
    }

    /// <summary>Prüft BootstrapAsync: sendet den Vertrag als camelCase-JSON (ticket, clientPublicKey, deviceName).</summary>
    [Fact]
    public async Task BootstrapAsync_SendsCamelCasePayload()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(BootstrapResponder(SessionPayload()));
        var client = new DevicePairingClient(new HttpClient(handler));

        await client.BootstrapAsync(new Uri("http://testserver/"), "mein-ticket", "Testgerät");

        var body = handler.Requests[0].Body!;
        var json = JsonDocument.Parse(body).RootElement;
        Assert.Equal("mein-ticket", json.GetProperty("ticket").GetString());
        Assert.Equal("Testgerät", json.GetProperty("deviceName").GetString());
        var keyBytes = Convert.FromBase64String(json.GetProperty("clientPublicKey").GetString()!);
        Assert.True(keyBytes.Length > 0);
        Assert.False(json.TryGetProperty("Ticket", out _));
    }

    /// <summary>Prüft BootstrapAsync: lässt den Gerätenamen bei <c>null</c> weg.</summary>
    [Fact]
    public async Task BootstrapAsync_OmitsDeviceName_WhenNull()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(BootstrapResponder(SessionPayload()));
        var client = new DevicePairingClient(new HttpClient(handler));

        await client.BootstrapAsync(new Uri("http://testserver/"), "ticket", null);

        var json = JsonDocument.Parse(handler.Requests[0].Body!).RootElement;
        Assert.False(json.TryGetProperty("deviceName", out _));
    }

    /// <summary>Prüft BootstrapAsync: entschlüsselt das vollständige Session-Paket.</summary>
    [Fact]
    public async Task BootstrapAsync_DecryptsSession_Roundtrip()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(BootstrapResponder(SessionPayload(
            deviceToken: "geheimes-geraete-token",
            token: "jwt-abc",
            refreshToken: "refresh-xyz")));
        var client = new DevicePairingClient(new HttpClient(handler));

        var result = await client.BootstrapAsync(new Uri("http://testserver/"), "ticket", null);

        Assert.Equal("geheimes-geraete-token", result.DeviceToken);
        Assert.Equal("jwt-abc", result.Token);
        Assert.Equal("refresh-xyz", result.RefreshToken);
        Assert.Equal(new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.Expires);
    }

    /// <summary>Prüft BootstrapAsync: ein Kurzcode geht unverändert als Ticket durch (Server-Alias).</summary>
    [Fact]
    public async Task BootstrapAsync_ShortCode_PassesTicketThrough()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(BootstrapResponder(SessionPayload()));
        var client = new DevicePairingClient(new HttpClient(handler));

        await client.BootstrapAsync(new Uri("http://testserver/"), "A1B2C3D4", null);

        var json = JsonDocument.Parse(handler.Requests[0].Body!).RootElement;
        Assert.Equal("A1B2C3D4", json.GetProperty("ticket").GetString());
    }

    /// <summary>
    /// Prüft BootstrapAsync: HTTP-Fehler werden als <see cref="ApiException"/>
    /// mit Statuscode gemeldet — ohne Wiederholung (auch 429/5xx).
    /// </summary>
    /// <param name="statusCode">Der simulierte HTTP-Statuscode.</param>
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task BootstrapAsync_MapsErrorStatus_WithoutRetry(HttpStatusCode statusCode)
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(statusCode, "{}");
        var client = new DevicePairingClient(new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => client.BootstrapAsync(new Uri("http://testserver/"), "ticket", null));

        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Single(handler.Requests);
    }

    /// <summary>Prüft BootstrapAsync: Transportfehler → ApiException ohne Statuscode.</summary>
    [Fact]
    public async Task BootstrapAsync_TransportError_MapsToApiExceptionWithoutStatus()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueException(new HttpRequestException("Verbindung fehlgeschlagen"));
        var client = new DevicePairingClient(new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => client.BootstrapAsync(new Uri("http://testserver/"), "ticket", null));

        Assert.Null(exception.StatusCode);
        Assert.Single(handler.Requests);
    }

    /// <summary>Prüft BootstrapAsync: nicht entschlüsselbares Paket → Exception (kein teilweises Ergebnis).</summary>
    [Fact]
    public async Task BootstrapAsync_UndecryptablePayload_Throws()
    {
        var handler = new StubHttpMessageHandler();
        // Gültige Response-Hülle, aber das Paket ist nicht mit dem
        // richtigen Schlüssel verschlüsselt.
        handler.Enqueue(request =>
        {
            using var wrongKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            var json = JsonSerializer.Serialize(new
            {
                serverPublicKey = Convert.ToBase64String(wrongKey.ExportSubjectPublicKeyInfo()),
                encryptedPayload = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });
        var client = new DevicePairingClient(new HttpClient(handler));

        await Assert.ThrowsAnyAsync<Exception>(
            () => client.BootstrapAsync(new Uri("http://testserver/"), "ticket", null));
    }

    /// <summary>Prüft BootstrapAsync: unvollständiges Session-Paket → Exception.</summary>
    [Fact]
    public async Task BootstrapAsync_IncompletePayload_Throws()
    {
        var handler = new StubHttpMessageHandler();
        // refreshToken fehlt — eine teilweise Session darf nicht
        // angenommen werden.
        handler.Enqueue(BootstrapResponder(JsonSerializer.Serialize(new
        {
            deviceToken = "d",
            token = "t",
            expires = "2030-01-01T00:00:00Z"
        })));
        var client = new DevicePairingClient(new HttpClient(handler));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.BootstrapAsync(new Uri("http://testserver/"), "ticket", null));
    }

    /// <summary>Prüft BootstrapAsync: leeres Ticket → ArgumentException (kein Request).</summary>
    [Fact]
    public async Task BootstrapAsync_RequiresTicket()
    {
        var handler = new StubHttpMessageHandler();
        var client = new DevicePairingClient(new HttpClient(handler));

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => client.BootstrapAsync(new Uri("http://testserver/"), "  ", null));

        Assert.Empty(handler.Requests);
    }
}
