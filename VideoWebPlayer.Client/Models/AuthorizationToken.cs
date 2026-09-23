using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoWebPlayer.Client.Models
{
    /// <summary>
    /// Autorisierungstoken, das der Server nach erfolgreicher Anmeldung zurückgibt.
    /// </summary>
    public class AuthorizationToken
    {
        /// <summary>
        /// Bearer-Zugriffstoken für autorisierte API-Aufrufe.
        /// </summary>
        public string token { get; set; }
        /// <summary>
        /// Zeitpunkt, an dem das Token abläuft.
        /// </summary>
        public DateTime expires { get; set; }
    }
}
