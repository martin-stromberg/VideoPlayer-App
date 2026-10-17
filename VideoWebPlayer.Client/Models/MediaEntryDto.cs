namespace VideoWebPlayer.Controllers.Models
{
    /// <summary>
    /// Daten-Transfer-Objekt für MediaEntry.
    /// </summary>
    public class MediaEntryDto
    {
        /// <summary>
        /// Medientyp des Eintrags (z. B. Film, Serie oder Sammlung).
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Eindeutige ID des Eintrags.
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Titel des Eintrags.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Optionale Beschreibung des Eintrags.
        /// </summary>
        public string? Description { get; set; }
        /// <summary>
        /// Optionale URL zum Medium.
        /// </summary>
        public string? Url { get; set; }
        /// <summary>
        /// Erstellungszeitpunkt des Eintrags.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>
        /// ID des Vorschaubildes bzw. Posters, falls vorhanden.
        /// </summary>
        public long? PictureId { get; set; } // oder PosterPictureId
        /// <summary>
        /// Anzahl der enthaltenen Elemente (z. B. Filme einer Sammlung).
        /// </summary>
        public int ItemCount { get; set; }

    }
}
