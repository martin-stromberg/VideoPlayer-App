namespace VideoWebPlayer.Client.Models
{
    /// <summary>
    /// Eintrag der "Weiter ansehen"-Liste des aktuellen Benutzers.
    /// </summary>
    public class ContinueWatchingDto
    {
        /// <summary>
        /// Medientyp des Eintrags (z. B. "movie" oder "episode").
        /// </summary>
        public string MediaType { get; set; } = "";
        /// <summary>
        /// Der zugehörige Medieneintrag.
        /// </summary>
        public DtoMediaEntry Entry { get; set; }
        /// <summary>
        /// Aktuelle Wiedergabeposition in Sekunden.
        /// </summary>
        public long PositionSeconds { get; set; }
        /// <summary>
        /// Gesamtdauer des Mediums in Sekunden, falls bekannt.
        /// </summary>
        public long? DurationSeconds { get; set; }
        /// <summary>
        /// Anzeigetitel des Eintrags.
        /// </summary>
        public string Title { get; set; } = "";
        /// <summary>
        /// ID des Poster-Bildes, falls vorhanden.
        /// </summary>
        public long? PosterPictureId { get; set; }
    }
}
