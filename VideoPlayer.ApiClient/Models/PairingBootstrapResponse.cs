using System.Text.Json.Serialization;

namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Antwort von <c>POST api/pairing/bootstrap</c>: enthält den öffentlichen
/// Server-Schlüssel (SPKI/DER, Base64) und das AES-256-GCM-verschlüsselte
/// Session-Paket (Layout <c>nonce(12) ‖ ciphertext ‖ tag(16)</c>, Base64)
/// — entschlüsselt ein JSON mit <c>deviceToken</c>, <c>token</c>,
/// <c>expires</c> und <c>refreshToken</c>.
/// </summary>
public class PairingBootstrapResponse
{
    /// <summary>Öffentlicher ECDH-Schlüssel des Servers (SPKI/DER, Base64).</summary>
    [JsonPropertyName("serverPublicKey")]
    public string ServerPublicKey { get; set; } = string.Empty;

    /// <summary>Verschlüsseltes Session-Paket (Base64).</summary>
    [JsonPropertyName("encryptedPayload")]
    public string EncryptedPayload { get; set; } = string.Empty;
}
