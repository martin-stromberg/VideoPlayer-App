using System.Net;
using System.Text;
using System.Text.Json;
using VideoPlayer.ApiClient.Models;

namespace VideoPlayer.ApiClient;

/// <summary>
/// Öffentlicher Pairing-Endpunkt-Client: tauscht einen Einmal-Code gegen ein
/// Geräte-Token aus (<c>POST api/pairing/exchange</c>). Sendet bewusst
/// <b>keinen</b> <c>X-API-Key</c>- und <b>keinen</b> Bearer-Header, verwendet
/// keine Wiederholungen und keine Re-Authentifizierung — der Endpunkt ist
/// öffentlich und ratenbegrenzt.
/// </summary>
public sealed class DevicePairingClient
{
    /// <summary>Relativer Endpunktpfad des Pairing-Austauschs.</summary>
    public const string ExchangeEndpoint = "api/pairing/exchange";

    /// <summary>
    /// Empfohlenes Timeout des injizierten <see cref="HttpClient"/> für
    /// den Pairing-Austausch — konsistent zu den übrigen HTTP-Clients
    /// (10–30 s); der <see cref="HttpClient"/>-Default von 100 s ließe
    /// einen hängenden Endpunkt den Pairing-Indikator zu lange blockieren.
    /// </summary>
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient httpClient;

    /// <summary>Erstellt den Pairing-Client.</summary>
    /// <param name="httpClient">
    /// Injizierter HTTP-Client. Er darf keine vorkonfigurierten
    /// Auth-Header tragen; <see cref="HttpClient.BaseAddress"/> wird nicht
    /// benötigt, da die Anfrage gegen eine absolute Adresse geht.
    /// </param>
    public DevicePairingClient(HttpClient httpClient)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Tauscht <paramref name="code"/> gegen ein Geräte-Token: erzeugt ein
    /// flüchtiges ECDH-Schlüsselpaar, sendet den öffentlichen Schlüssel mit
    /// dem Code an den Server und entschlüsselt das zurückgegebene Token.
    /// </summary>
    /// <param name="baseAddress">Basisadresse des Pairing-Servers.</param>
    /// <param name="code">Der einzulösende Einmal-Code.</param>
    /// <param name="deviceName">Optionaler Gerätename (Server-Fallback bei <c>null</c>).</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Das entschlüsselte Geräte-Token.</returns>
    /// <exception cref="ApiException">
    /// HTTP-Fehler des Endpunkts (<see cref="HttpRequestException.StatusCode"/> gesetzt)
    /// oder Transport-/Timeoutfehler (<see cref="HttpRequestException.StatusCode"/> = <c>null</c>).
    /// </exception>
    public async Task<string> ExchangeAsync(
        Uri baseAddress,
        string code,
        string? deviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        using var keyPair = PairingCrypto.CreateKeyPair();
        var request = new PairingExchangeRequest
        {
            Code = code,
            ClientPublicKey = PairingCrypto.ExportPublicKeyBase64(keyPair),
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName.Trim()
        };

        var endpoint = new Uri(baseAddress, ExchangeEndpoint);
        HttpResponseMessage response;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(request, JsonOptions),
                    Encoding.UTF8,
                    "application/json")
            };
            response = await httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ApiException(
                ExchangeEndpoint,
                statusCode: null,
                responseBody: null,
                $"Pairing-Anfrage an {endpoint} fehlgeschlagen: {ex.Message}",
                ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new ApiException(
                    ExchangeEndpoint,
                    response.StatusCode,
                    errorBody,
                    $"Pairing-Anfrage an {endpoint} fehlgeschlagen: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize<PairingExchangeResponse>(content, JsonOptions)
                ?? throw new InvalidOperationException("Die Pairing-Antwort des Servers ist leer.");

            return PairingCrypto.DecryptToken(keyPair, payload.ServerPublicKey, payload.EncryptedToken);
        }
    }
}
