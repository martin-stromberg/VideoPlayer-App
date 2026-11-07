using System.Text.Json.Serialization;

namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Antwort von <c>POST api/pairing/exchange</c>: enthält den öffentlichen
/// Server-Schlüssel (SPKI/DER, Base64) und das AES-256-GCM-verschlüsselte
/// Geräte-Token (Layout <c>nonce(12) ‖ ciphertext ‖ tag(16)</c>, Base64).
/// </summary>
public class PairingExchangeResponse
{
    /// <summary>Öffentlicher ECDH-Schlüssel des Servers (SPKI/DER, Base64).</summary>
    [JsonPropertyName("serverPublicKey")]
    public string ServerPublicKey { get; set; } = string.Empty;

    /// <summary>Verschlüsseltes Geräte-Token (Base64).</summary>
    [JsonPropertyName("encryptedToken")]
    public string EncryptedToken { get; set; } = string.Empty;
}
