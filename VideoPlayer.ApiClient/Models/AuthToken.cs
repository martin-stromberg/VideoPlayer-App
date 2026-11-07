namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Tokenantwort des Servers auf <c>api/auth/login</c>. Die Wire-Feldnamen
/// (<c>token</c>, <c>expires</c>) werden case-insensitiv deserialisiert.
/// </summary>
public class AuthToken
{
    /// <summary>Zugangs-Token (Bearer).</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Ablaufzeitpunkt des Tokens.</summary>
    public DateTime Expires { get; set; }
}
