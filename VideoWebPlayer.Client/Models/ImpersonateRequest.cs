namespace VideoWebPlayer.Client.Models
{
    /// <summary>
    /// Anfrage zur Übernahme der Identität (Impersonation) eines anderen Benutzers.
    /// </summary>
    public class ImpersonateRequest
    {
        /// <summary>
        /// ID des Benutzers, dessen Identität übernommen werden soll.
        /// </summary>
        public string UserId { get; set; } = "";
    }
}
