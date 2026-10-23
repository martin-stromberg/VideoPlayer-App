using System.Text.Json.Serialization;

namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Anfrage an <c>POST api/pairing/exchange</c>: tauscht einen Einmal-Pairing-Code
/// gegen ein verschlüsseltes Geräte-Token aus.
/// </summary>
public class PairingExchangeRequest
{
    /// <summary>Der vom Server erzeugte Einmal-Code.</summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>Öffentlicher ECDH-Schlüssel des Geräts (SPKI/DER, Base64).</summary>
    [JsonPropertyName("clientPublicKey")]
    public string ClientPublicKey { get; set; } = string.Empty;

    /// <summary>Optionaler Gerätename; leer/<c>null</c> = Server-Fallback.</summary>
    [JsonPropertyName("deviceName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DeviceName { get; set; }
}
