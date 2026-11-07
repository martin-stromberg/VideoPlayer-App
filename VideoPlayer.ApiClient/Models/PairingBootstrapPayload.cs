using System.Text.Json.Serialization;

namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Entschlüsseltes Session-Paket der Bootstrap-Antwort: Geräte-Token für
/// das Client-Gate, Anmelde-Token (Bearer) samt Ablaufzeitpunkt und der
/// rotierbare Refresh-Token (<c>api/auth/refresh</c>).
/// </summary>
public class PairingBootstrapPayload
{
    /// <summary>Geräte-Token (Gate-Schlüssel des gepaarten Geräts).</summary>
    [JsonPropertyName("deviceToken")]
    public string DeviceToken { get; set; } = string.Empty;

    /// <summary>Anmelde-Token (Bearer).</summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>Ablaufzeitpunkt des Anmelde-Tokens.</summary>
    [JsonPropertyName("expires")]
    public DateTime Expires { get; set; }

    /// <summary>Refresh-Token für die stille Sitzungserneuerung.</summary>
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
}
