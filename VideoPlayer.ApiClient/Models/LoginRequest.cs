namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Anmeldedaten für <c>POST api/auth/login</c>.
/// </summary>
public class LoginRequest
{
    /// <summary>E-Mail-Adresse des Benutzers.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Passwort des Benutzers.</summary>
    public string Password { get; set; } = string.Empty;
}
