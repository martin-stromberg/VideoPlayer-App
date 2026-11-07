using System.Net;
using System.Text;
using System.Text.Json;
using VideoPlayer.ApiClient.Models;

namespace VideoPlayer.ApiClient;

/// <summary>
/// Öffentlicher Pairing-Endpunkt-Client: tauscht einen Einmal-Code gegen ein
/// Geräte-Token aus (<c>POST api/pairing/exchange</c>) bzw. löst ein
/// Pairing-Ticket gegen ein verschlüsseltes Session-Paket ein
/// (<c>POST api/pairing/bootstrap</c>). Sendet bewusst
/// <b>keinen</b> <c>X-API-Key</c>- und <b>keinen</b> Bearer-Header, verwendet
/// keine Wiederholungen und keine Re-Authentifizierung — die Endpunkte sind
/// öffentlich und ratenbegrenzt.
/// </summary>
public sealed class DevicePairingClient
{
    /// <summary>Relativer Endpunktpfad des Pairing-Austauschs.</summary>
    public const string ExchangeEndpoint = "api/pairing/exchange";

    /// <summary>Relativer Endpunktpfad des Ticket-Bootstraps (QR-/Kurzcode-Onboarding).</summary>
    public const string BootstrapEndpoint = "api/pairing/bootstrap";

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

        var content = await PostJsonAsync(ExchangeEndpoint, baseAddress, request, cancellationToken);
        var payload = JsonSerializer.Deserialize<PairingExchangeResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Die Pairing-Antwort des Servers ist leer.");

        return PairingCrypto.DecryptToken(keyPair, payload.ServerPublicKey, payload.EncryptedToken);
    }

    /// <summary>
    /// Löst <paramref name="ticket"/> (langes Geheimnis oder Kurzcode-Alias)
    /// gegen ein verschlüsseltes Session-Paket ein: erzeugt ein flüchtiges
    /// ECDH-Schlüsselpaar, sendet den öffentlichen Schlüssel mit dem Ticket
    /// an den Server und entschlüsselt die Antwort — Geräte-Token,
    /// Anmelde-Token samt Ablauf und Refresh-Token in einem Schritt.
    /// </summary>
    /// <param name="baseAddress">Basisadresse des Pairing-Servers.</param>
    /// <param name="ticket">Das einzulösende Ticket bzw. der Kurzcode (opak, nicht normalisiert).</param>
    /// <param name="deviceName">Optionaler Gerätename (Server-Fallback bei <c>null</c>).</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Das entschlüsselte Session-Paket.</returns>
    /// <exception cref="ApiException">
    /// HTTP-Fehler des Endpunkts (<see cref="HttpRequestException.StatusCode"/> gesetzt)
    /// oder Transport-/Timeoutfehler (<see cref="HttpRequestException.StatusCode"/> = <c>null</c>).
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Die Antwort ist leer, kein JSON oder enthält keine vollständige Session.
    /// </exception>
    public async Task<BootstrapResult> BootstrapAsync(
        Uri baseAddress,
        string ticket,
        string? deviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(ticket);

        using var keyPair = PairingCrypto.CreateKeyPair();
        var request = new PairingBootstrapRequest
        {
            Ticket = ticket,
            ClientPublicKey = PairingCrypto.ExportPublicKeyBase64(keyPair),
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName.Trim()
        };

        var content = await PostJsonAsync(BootstrapEndpoint, baseAddress, request, cancellationToken);
        var response = JsonSerializer.Deserialize<PairingBootstrapResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Die Bootstrap-Antwort des Servers ist leer.");

        var payloadJson = PairingCrypto.DecryptToken(keyPair, response.ServerPublicKey, response.EncryptedPayload);
        var payload = JsonSerializer.Deserialize<PairingBootstrapPayload>(payloadJson, JsonOptions)
            ?? throw new InvalidOperationException("Das entschlüsselte Bootstrap-Paket ist leer.");

        if (string.IsNullOrWhiteSpace(payload.DeviceToken)
            || string.IsNullOrWhiteSpace(payload.Token)
            || string.IsNullOrWhiteSpace(payload.RefreshToken))
        {
            throw new InvalidOperationException("Das entschlüsselte Bootstrap-Paket ist unvollständig.");
        }

        return new BootstrapResult
        {
            DeviceToken = payload.DeviceToken,
            Token = payload.Token,
            Expires = payload.Expires,
            RefreshToken = payload.RefreshToken
        };
    }

    /// <summary>
    /// Sendet eine JSON-Anfrage an einen öffentlichen Pairing-Endpunkt und
    /// liefert den Antwort-Body; HTTP- und Transportfehler werden als
    /// <see cref="ApiException"/> gemeldet (Transport = <c>StatusCode</c> <c>null</c>).
    /// </summary>
    /// <param name="endpoint">Relativer Endpunktpfad.</param>
    /// <param name="baseAddress">Basisadresse des Pairing-Servers.</param>
    /// <param name="request">Zu serialisierender Anfrage-Body.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Der Antwort-Body als Text.</returns>
    private async Task<string> PostJsonAsync(
        string endpoint,
        Uri baseAddress,
        object request,
        CancellationToken cancellationToken)
    {
        var uri = new Uri(baseAddress, endpoint);
        HttpResponseMessage response;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri)
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
                endpoint,
                statusCode: null,
                responseBody: null,
                $"Pairing-Anfrage an {uri} fehlgeschlagen: {ex.Message}",
                ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new ApiException(
                    endpoint,
                    response.StatusCode,
                    errorBody,
                    $"Pairing-Anfrage an {uri} fehlgeschlagen: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
    }
}
