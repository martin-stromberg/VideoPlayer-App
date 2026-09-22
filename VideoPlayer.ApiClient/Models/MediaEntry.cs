using System.Text.Json.Serialization;

namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Basisklasse der Medien-Hierarchie. Der Server diskriminiert über das
/// JSON-Feld <c>$type</c> (mediaEntry, movie, movieCollection, show, season, episode).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(MediaEntry), "mediaEntry")]
[JsonDerivedType(typeof(Movie), "movie")]
[JsonDerivedType(typeof(MovieCollection), "movieCollection")]
[JsonDerivedType(typeof(TvShow), "show")]
[JsonDerivedType(typeof(TvShowSeason), "season")]
[JsonDerivedType(typeof(TvShowEpisode), "episode")]
public class MediaEntry
{
    /// <summary>ID des Eintrags.</summary>
    public long Id { get; set; }

    /// <summary>Anzeigename des Eintrags.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>ID der zugehörigen Medienquelle.</summary>
    public long MediaSourceId { get; set; }

    /// <summary>Veröffentlichungsdatum, falls bekannt.</summary>
    public DateTime? ReleaseDate { get; set; }

    /// <summary>Premieren-/Startdatum (bei Serien), falls bekannt.</summary>
    public DateTime? PremieredAt { get; set; }

    /// <summary>Enddatum (bei Serien), falls bekannt.</summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>ID des Posterbilds, falls vorhanden.</summary>
    public long? PosterPictureId { get; set; }

    /// <summary>ID des Bannerbilds, falls vorhanden.</summary>
    public long? BannerPictureId { get; set; }

    /// <summary>ID des Fanart-Bilds, falls vorhanden.</summary>
    public long? FanartPictureId { get; set; }

    /// <summary>Eingebettetes Posterbild, falls mitgeliefert.</summary>
    public Picture? PosterPicture { get; set; }

    /// <summary>Eingebettetes Bannerbild, falls mitgeliefert.</summary>
    public Picture? BannerPicture { get; set; }

    /// <summary>Eingebettetes Fanart-Bild, falls mitgeliefert.</summary>
    public Picture? FanartPicture { get; set; }

    /// <summary>Ob der Eintrag als Favorit markiert ist.</summary>
    public bool IsFavorite { get; set; }

    /// <summary>Ob die Metadaten des Eintrags manuell bearbeitet wurden.</summary>
    public bool IsManuallyEdited { get; set; }
}

/// <summary>
/// Film.
/// </summary>
public class Movie : MediaEntry
{
    /// <summary>Kommagetrennte Genre-Namen, falls vorhanden.</summary>
    public string? GenreNames { get; set; }

    /// <summary>Handlungsbeschreibung, falls vorhanden.</summary>
    public string? Plot { get; set; }

    /// <summary>Zugehörige Filmkollektion, falls vorhanden.</summary>
    public MovieCollection? Collection { get; set; }
}

/// <summary>
/// Filmkollektion (Sammelbegriff für mehrere Filme).
/// </summary>
public class MovieCollection : MediaEntry
{
    /// <summary>Filme der Kollektion.</summary>
    public Movie[] Movies { get; set; } = [];
}

/// <summary>
/// Serie inklusive Staffeln.
/// </summary>
public class TvShow : MediaEntry
{
    /// <summary>Staffeln der Serie.</summary>
    public TvShowSeason[] Seasons { get; set; } = [];

    /// <summary>Kommagetrennte Genre-Namen, falls vorhanden.</summary>
    public string? GenreNames { get; set; }

    /// <summary>Beschreibung der Serie, falls vorhanden.</summary>
    public string? Plot { get; set; }
}

/// <summary>
/// Staffel einer Serie.
/// </summary>
public class TvShowSeason : MediaEntry
{
    /// <summary>Staffelnummer.</summary>
    public int Number { get; set; }

    /// <summary>Zugehörige Serie, falls mitgeliefert.</summary>
    public TvShow? Show { get; set; }

    /// <summary>Episoden der Staffel.</summary>
    public TvShowEpisode[] Episodes { get; set; } = [];
}

/// <summary>
/// Episode einer Serie.
/// </summary>
public class TvShowEpisode : MediaEntry
{
    /// <summary>Zugehörige Staffel, falls mitgeliefert.</summary>
    public TvShowSeason? Season { get; set; }

    /// <summary>Episodennummer.</summary>
    public int Number { get; set; }

    /// <summary>Handlungsbeschreibung, falls vorhanden.</summary>
    public string? Plot { get; set; }

    /// <summary>ID des generierten Hintergrundbilds, falls vorhanden.</summary>
    public long? GeneratedBackgroundPictureId { get; set; }
}

/// <summary>
/// Bild eines Medieneintrags (Poster, Banner, Fanart).
/// </summary>
public class Picture
{
    /// <summary>ID des Bilds.</summary>
    public long Id { get; set; }

    /// <summary>ID des zugehörigen Medieneintrags.</summary>
    public long MediaItemId { get; set; }

    /// <summary>Bildtyp (z. B. <c>poster</c>, <c>banner</c>, <c>fanart</c>).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Breite in Pixeln, falls bekannt.</summary>
    public int? Width { get; set; }

    /// <summary>Höhe in Pixeln, falls bekannt.</summary>
    public int? Height { get; set; }

    /// <summary>Beschreibung des Bilds, falls vorhanden.</summary>
    public string? Description { get; set; }

    /// <summary>Bilddaten (Rohbytes), falls eingebettet.</summary>
    public byte[] Data { get; set; } = [];

    /// <summary>MIME-Typ der Bilddaten.</summary>
    public string ContentType { get; set; } = string.Empty;
}
