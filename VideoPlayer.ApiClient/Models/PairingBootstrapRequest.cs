using System.Text.Json.Serialization;

namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Anfrage an <c>POST api/pairing/bootstrap</c>: löst ein Pairing-Ticket
/// (langes Geheimnis oder Kurzcode-Alias) gegen ein verschlüsseltes
/// Session-Paket aus Geräte-Token, Anmelde-Token und Refresh-Token ein.
/// </summary>
public class PairingBootstrapRequest
{
    /// <summary>Das vom Server erzeugte Pairing-Ticket bzw. der Kurzcode.</summary>
    [JsonPropertyName("ticket")]
    public string Ticket { get; set; } = string.Empty;

    /// <summary>Öffentlicher ECDH-Schlüssel des Geräts (SPKI/DER, Base64).</summary>
    [JsonPropertyName("clientPublicKey")]
    public string ClientPublicKey { get; set; } = string.Empty;

    /// <summary>Optionaler Gerätename; leer/<c>null</c> = Server-Fallback.</summary>
    [JsonPropertyName("deviceName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DeviceName { get; set; }
}
