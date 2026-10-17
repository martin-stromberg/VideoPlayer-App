namespace VideoWebPlayer.Client.Models
{
    /// <summary>
    /// Anmeldeanfrage für den Login-Endpunkt mit E-Mail-Adresse und Passwort.
    /// </summary>
    public class AuthenticationRequest
    {
        /// <summary>
        /// E-Mail-Adresse des Benutzers.
        /// </summary>
        public string Email { get; set; } = "";
        /// <summary>
        /// Passwort des Benutzers.
        /// </summary>
        public string Password { get; set; } = "";
    }
}
