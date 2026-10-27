using System.Text.Json.Serialization;

namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Antwort von <c>POST api/auth/refresh</c>: das neue Anmelde-Token samt
/// Ablaufzeitpunkt und der rotierte Nachfolge-Refresh-Token (der alte ist
/// ab da serverseitig ungültig).
/// </summary>
public class RefreshSessionResult
{
    /// <summary>Neues Anmelde-Token (Bearer).</summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>Ablaufzeitpunkt des Anmelde-Tokens.</summary>
    [JsonPropertyName("expires")]
    public DateTime Expires { get; set; }

    /// <summary>Rotierter Refresh-Token (ersetzt den bisherigen).</summary>
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
}
