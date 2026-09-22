using System.ComponentModel.DataAnnotations;

namespace VideoWebPlayer.Data
{
    /// <summary>
    /// DTO für einen Favoriten-Eintrag eines Benutzers.
    /// </summary>
    public class DtoFavoriteEntry
    {
        /// <summary>
        /// Eindeutige ID des Favoriten-Eintrags.
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Der favorisierte Medieneintrag.
        /// </summary>
        public DtoMediaEntry Entry { get; set; }
        /// <summary>
        /// Zeitpunkt, an dem der Eintrag als Favorit markiert wurde.
        /// </summary>
        public DateTime CreatedAt { get; set; }

    }
}
