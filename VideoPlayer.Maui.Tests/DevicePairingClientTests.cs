#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VideoPlayer.ApiClient;
using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Tests für <c>DevicePairingClient</c>: der öffentliche Pairing-Endpunkt
/// (<c>POST api/pairing/exchange</c>) wird ohne Gate-/Bearer-Header und ohne
/// Wiederholungen angesprochen; HTTP-Fehler werden als <see cref="ApiException"/>
/// mit Statuscode gemeldet.
/// </summary>
public class DevicePairingClientTests
{
    /// <summary>
    /// Responder, der den Server simuliert: importiert den öffentlichen
    /// Geräte-Schlüssel aus dem Request-Body, verschlüsselt das Geräte-Token
    /// per ECDH+AES-256-GCM und liefert die Pairing-Antwort.
    /// </summary>
    /// <param name="token">Das auszuliefernde Geräte-Token (Klartext).</param>
    /// <returns>Ein Responder-Delegate für den Stub-Handler.</returns>
    private static Func<HttpRequestMessage, HttpResponseMessage> PairingResponder(string token)
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

            var plaintext = Encoding.UTF8.GetBytes(token);
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
                encryptedToken = payload
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        };
    }

    /// <summary>Prüft ExchangeAsync: sendet POST ohne Gate- und Bearer-Header.</summary>
    [Fact]
    public async Task ExchangeAsync_SendsRequest_WithoutGateAndBearerHeaders()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(PairingResponder("device-token-1"));
        var client = new DevicePairingClient(new HttpClient(handler));

        await client.ExchangeAsync(new Uri("http://testserver/"), "ABCD2345", "Testgerät");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/pairing/exchange", request.PathAndQuery);
        Assert.Null(request.ApiKey);
        Assert.Null(request.Authorization);
    }

    /// <summary>Prüft ExchangeAsync: sendet den Vertrag als camelCase-JSON.</summary>
    [Fact]
    public async Task ExchangeAsync_SendsCamelCasePayload()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(PairingResponder("device-token-1"));
        var client = new DevicePairingClient(new HttpClient(handler));

        await client.ExchangeAsync(new Uri("http://testserver/"), "ABCD2345", "Testgerät");

        var body = handler.Requests[0].Body!;
        var json = JsonDocument.Parse(body).RootElement;
        Assert.Equal("ABCD2345", json.GetProperty("code").GetString());
        Assert.Equal("Testgerät", json.GetProperty("deviceName").GetString());
        // clientPublicKey: Base64-kodierter SPKI/DER-Schlüssel.
        var keyBytes = Convert.FromBase64String(json.GetProperty("clientPublicKey").GetString()!);
        Assert.True(keyBytes.Length > 0);
        Assert.False(json.TryGetProperty("Code", out _));
    }

    /// <summary>Prüft ExchangeAsync: lässt den Gerätenamen bei <c>null</c> weg (Server-Fallback).</summary>
    [Fact]
    public async Task ExchangeAsync_OmitsDeviceName_WhenNull()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(PairingResponder("device-token-1"));
        var client = new DevicePairingClient(new HttpClient(handler));

        await client.ExchangeAsync(new Uri("http://testserver/"), "ABCD2345", null);

        var json = JsonDocument.Parse(handler.Requests[0].Body!).RootElement;
        Assert.False(json.TryGetProperty("deviceName", out _));
    }

    /// <summary>Prüft ExchangeAsync: entschlüsselt das Geräte-Token aus der Server-Antwort.</summary>
    [Fact]
    public async Task ExchangeAsync_DecryptsToken_Roundtrip()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(PairingResponder("geheimes-geraete-token"));
        var client = new DevicePairingClient(new HttpClient(handler));

        var token = await client.ExchangeAsync(new Uri("http://testserver/"), "ABCD2345", null);

        Assert.Equal("geheimes-geraete-token", token);
    }

    /// <summary>
    /// Prüft ExchangeAsync: HTTP-Fehler werden als <see cref="ApiException"/>
    /// mit Statuscode gemeldet — ohne Wiederholung (auch 429/5xx).
    /// </summary>
    /// <param name="statusCode">Der simulierte HTTP-Statuscode.</param>
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ExchangeAsync_MapsErrorStatus_WithoutRetry(HttpStatusCode statusCode)
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(statusCode, "{}");
        var client = new DevicePairingClient(new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => client.ExchangeAsync(new Uri("http://testserver/"), "ABCD2345", null));

        Assert.Equal(statusCode, exception.StatusCode);
        // Kein Retry: genau ein Request ging raus.
        Assert.Single(handler.Requests);
    }

    /// <summary>Prüft ExchangeAsync: Transportfehler → ApiException ohne Statuscode.</summary>
    [Fact]
    public async Task ExchangeAsync_TransportError_MapsToApiExceptionWithoutStatus()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueException(new HttpRequestException("Verbindung fehlgeschlagen"));
        var client = new DevicePairingClient(new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => client.ExchangeAsync(new Uri("http://testserver/"), "ABCD2345", null));

        Assert.Null(exception.StatusCode);
        Assert.Single(handler.Requests);
    }

    /// <summary>Prüft ExchangeAsync: leerer Code → ArgumentException (kein Request).</summary>
    [Fact]
    public async Task ExchangeAsync_RequiresCode()
    {
        var handler = new StubHttpMessageHandler();
        var client = new DevicePairingClient(new HttpClient(handler));

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => client.ExchangeAsync(new Uri("http://testserver/"), "  ", null));

        Assert.Empty(handler.Requests);
    }
}
