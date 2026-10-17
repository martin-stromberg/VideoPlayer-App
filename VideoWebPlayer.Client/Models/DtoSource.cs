namespace VideoWebPlayer.Client.Models
{
    /// <summary>
    /// DTO für eine Medienquelle (z. B. ein Verzeichnis mit Medieninhalten).
    /// </summary>
    public class DtoMediaSource : DtoMediaEntry
    {
        /// <summary>
        /// Eindeutige ID des Eintrags.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Anzeigename des Eintrags.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Erstellungszeitpunkt.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>
        /// Zeitpunkt der letzten Klassifizierung
        /// </summary>
        public DateTime? ClassifiedAt { get; set; }
        /// <summary>
        /// Gibt an, ob an der Quelle Änderungen vorgenommen wurden.
        /// </summary>
        public bool Changed { get; set; }
        /// <summary>
        /// Zeitpunkt des letzten Scans der Quelle, falls bereits gescannt.
        /// </summary>
        public DateTime? LastScannedAt { get; set; }

        /// <summary>
        /// ID des optional hochgeladenen Icon-Bildes (gespeichert in der Tabelle
        /// <c>MediaSourceIcons</c>), falls vorhanden.
        /// </summary>
        public long? IconPictureId { get; set; }
    }

    /// <summary>
    /// DTO mit den Genres einer Medienquelle.
    /// </summary>
    public class SourceGenresDto
    {
        /// <summary>
        /// ID der Medienquelle.
        /// </summary>
        public long SourceId { get; set; }
        /// <summary>
        /// Name der Medienquelle.
        /// </summary>
        public string SourceName { get; set; }
        /// <summary>
        /// Liste der in der Quelle verfügbaren Genres.
        /// </summary>
        /// <value>Die Liste der Genres; standardmäßig leer.</value>
        public List<GenreDto> Genres { get; set; } = new();
    }

    /// <summary>
    /// DTO für ein Genre.
    /// </summary>
    public class GenreDto
    {
        /// <summary>
        /// Eindeutige ID des Genres.
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Name des Genres.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// URL eines Genre-Icons, falls vorhanden.
        /// </summary>
        public string? IconUrl { get; set; }
    }
}
