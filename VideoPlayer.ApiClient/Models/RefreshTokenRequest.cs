using System.Text.Json.Serialization;

namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Anfrage mit Refresh-Token an <c>POST api/auth/refresh</c> (Rotation)
/// und <c>POST api/auth/logout</c> (Widerruf).
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>Der aktuell gültige Refresh-Token.</summary>
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
}
