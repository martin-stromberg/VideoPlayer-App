using System.Text.Json.Serialization;

/// <summary>
/// Basisklasse für Medieneinträge (Filme, Sammlungen, Serien, Staffeln, Episoden),
/// die polymorph über den JSON-Typdiskriminator "$type" serialisiert werden.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DtoMediaEntry), "mediaEntry")]
[JsonDerivedType(typeof(DtoMovie), "movie")]
[JsonDerivedType(typeof(DtoMovieCollection), "movieCollection")]
[JsonDerivedType(typeof(DtoTVShow), "show")]
[JsonDerivedType(typeof(DtoTVShowSeason), "season")]
[JsonDerivedType(typeof(DtoTVShowEpisode), "episode")]
public class DtoMediaEntry
{
    /// <summary>
    /// Eindeutige ID des Medieneintrags.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Anzeigename des Eintrags.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// ID der Medienquelle, zu der der Eintrag gehört.
    /// </summary>
    public long MediaSourceId { get; set; }
    /// <summary>
    /// Veröffentlichungsdatum, falls bekannt.
    /// </summary>
    public DateTime? ReleaseDate { get; set; }
    /// <summary>
    /// Premierendatum (z. B. Serienstart), falls bekannt.
    /// </summary>
    public DateTime? PremieredAt { get; set; }
    /// <summary>
    /// Enddatum (z. B. Serienende), falls bekannt.
    /// </summary>
    public DateTime? EndedAt { get; set; }
    /// <summary>
    /// ID des Poster-Bildes, falls vorhanden.
    /// </summary>
    public long? PosterPictureId { get; set; }
    /// <summary>
    /// ID des Banner-Bildes, falls vorhanden.
    /// </summary>
    public long? BannerPictureId { get; set; }
    /// <summary>
    /// ID des Fanart-Bildes, falls vorhanden.
    /// </summary>
    public long? FanartPictureId { get; set; }
    /// <summary>
    /// Poster-Bild mit vollständigen Bilddaten, falls geladen.
    /// </summary>
    public DtoPicture? PosterPicture { get; set; }
    /// <summary>
    /// Banner-Bild mit vollständigen Bilddaten, falls geladen.
    /// </summary>
    public DtoPicture? BannerPicture { get; set; }
    /// <summary>
    /// Fanart-Bild mit vollständigen Bilddaten, falls geladen.
    /// </summary>
    public DtoPicture? FanartPicture { get; set; }
    /// <summary>
    /// Gibt an, ob der Eintrag vom aktuellen Benutzer als Favorit markiert ist.
    /// </summary>
    public bool IsFavorite { get; set; }
    /// <summary>
    /// Gibt an, ob die Metadaten des Eintrags manuell bearbeitet wurden.
    /// </summary>
    public bool IsManuallyEdited { get; set; }
}

/// <summary>
/// Auswahloption für ein Genre (ID und Name), z. B. für Filter-Dropdowns.
/// </summary>
public sealed class DtoGenreOption
{
    /// <summary>
    /// Eindeutige ID des Genres.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Name des Genres.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Anfrage zum Aktualisieren der Metadaten eines Medieneintrags.
/// </summary>
public sealed class MediaMetadataUpdateRequest
{
    /// <summary>
    /// Typ des zu aktualisierenden Objekts (z. B. "movie" oder "show").
    /// </summary>
    public string ObjectType { get; set; } = string.Empty;
    /// <summary>
    /// ID des zu aktualisierenden Eintrags.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Neuer Anzeigename des Eintrags.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Neues Veröffentlichungsdatum, falls gesetzt.
    /// </summary>
    public DateTime? ReleaseDate { get; set; }
    /// <summary>
    /// Neues Premierendatum, falls gesetzt.
    /// </summary>
    public DateTime? PremieredAt { get; set; }
    /// <summary>
    /// Neue Handlungsbeschreibung, falls gesetzt.
    /// </summary>
    public string? Plot { get; set; }
    /// <summary>
    /// Liste der zuzuweisenden Genre-Namen.
    /// </summary>
    public string[] GenreNames { get; set; } = [];
}

/// <summary>
/// DTO für einen Film.
/// </summary>
public class DtoMovie : DtoMediaEntry
{
    /// <summary>
    /// Kommaseparierte Genre-Namen des Films.
    /// </summary>
    public string? GenreNames { get; set; }
    /// <summary>
    /// Handlungsbeschreibung des Films.
    /// </summary>
    public string? Plot { get; set; }
    /// <summary>
    /// Filmsammlung, zu der der Film gehört, falls vorhanden.
    /// </summary>
    public DtoMovieCollection? Collection { get; set; }
}

/// <summary>
/// DTO für eine Filmsammlung (z. B. eine Filmreihe).
/// </summary>
public class DtoMovieCollection : DtoMediaEntry
{
    /// <summary>
    /// Filme, die zu dieser Sammlung gehören; wird bei Property-Zuweisungen übersprungen.
    /// </summary>
    [IgnoreAssignProperty]
    public DtoMovie[] Movies { get; set; } = [];
}

/// <summary>
/// DTO für eine Fernsehserie.
/// </summary>
public class DtoTVShow : DtoMediaEntry
{
    /// <summary>
    /// Staffeln der Serie; wird bei Property-Zuweisungen übersprungen.
    /// </summary>
    [IgnoreAssignProperty]
    public DtoTVShowSeason[] Seasons { get; set; } = [];
    /// <summary>
    /// Kommaseparierte Genre-Namen der Serie.
    /// </summary>
    public string? GenreNames { get; set; }
    /// <summary>
    /// Handlungsbeschreibung der Serie.
    /// </summary>
    public string? Plot { get; set; }
}

/// <summary>
/// DTO für eine Staffel einer Fernsehserie.
/// </summary>
public class DtoTVShowSeason : DtoMediaEntry
{
    /// <summary>
    /// Staffelnummer.
    /// </summary>
    public int Number { get; set; }
    /// <summary>
    /// Die Serie, zu der die Staffel gehört.
    /// </summary>
    public DtoTVShow? Show { get; set; }
    /// <summary>
    /// Episoden der Staffel; wird bei Property-Zuweisungen übersprungen.
    /// </summary>
    [IgnoreAssignProperty]
    public DtoTVShowEpisode[] Episodes { get; set; } = [];
}

/// <summary>
/// DTO für eine Episode einer Fernsehserie.
/// </summary>
public class DtoTVShowEpisode : DtoMediaEntry
{
    /// <summary>
    /// Die Staffel, zu der die Episode gehört.
    /// </summary>
    public DtoTVShowSeason? Season { get; set; }
    /// <summary>
    /// Episodennummer innerhalb der Staffel.
    /// </summary>
    public int Number { get; set; }
    /// <summary>
    /// Handlungsbeschreibung der Episode.
    /// </summary>
    public string? Plot { get; set; }
    /// <summary>
    /// ID des generierten Hintergrundbilds, falls vorhanden.
    /// </summary>
    public long? GeneratedBackgroundPictureId { get; set; }
}

/// <summary>
/// DTO für ein Bild (Poster, Banner, Fanart o. ä.) inklusive Bilddaten.
/// </summary>
public class DtoPicture
{
    /// <summary>
    /// Eindeutige ID des Bildes.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// ID des zugehörigen Medieneintrags (Verweis auf die eigentliche Bilddatei).
    /// </summary>
    public long MediaItemId { get; set; } // Verweis auf die eigentliche Bilddatei
    /// <summary>
    /// Bildtyp, z. B. "poster", "banner", "fanart" oder "thumb".
    /// </summary>
    public string Type { get; set; } = string.Empty; // z.B. "poster", "banner", "fanart", "thumb"
    /// <summary>
    /// Bildbreite in Pixeln, falls bekannt.
    /// </summary>
    public int? Width { get; set; }
    /// <summary>
    /// Bildhöhe in Pixeln, falls bekannt.
    /// </summary>
    public int? Height { get; set; }
    /// <summary>
    /// Optionale Beschreibung des Bildes.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Rohe Bilddaten als Byte-Array.
    /// </summary>
    public byte[] Data { get; set; } = [];
    /// <summary>
    /// MIME-Typ der Bilddaten (z. B. "image/jpeg").
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// Markiert eine Eigenschaft oder ein Feld, das bei der automatischen
/// Property-Zuweisung (z. B. beim Mapping) übersprungen werden soll.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class IgnoreAssignPropertyAttribute : Attribute
{
    // Dieses Attribut kann verwendet werden, um bestimmte Eigenschaften bei der Serialisierung zu ignorieren
}
