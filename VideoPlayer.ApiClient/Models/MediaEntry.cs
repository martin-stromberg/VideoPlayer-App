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
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long MediaSourceId { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public DateTime? PremieredAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public long? PosterPictureId { get; set; }
    public long? BannerPictureId { get; set; }
    public long? FanartPictureId { get; set; }
    public Picture? PosterPicture { get; set; }
    public Picture? BannerPicture { get; set; }
    public Picture? FanartPicture { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsManuallyEdited { get; set; }
}

public class Movie : MediaEntry
{
    public string? GenreNames { get; set; }
    public string? Plot { get; set; }
    public MovieCollection? Collection { get; set; }
}

public class MovieCollection : MediaEntry
{
    public Movie[] Movies { get; set; } = [];
}

public class TvShow : MediaEntry
{
    public TvShowSeason[] Seasons { get; set; } = [];
    public string? GenreNames { get; set; }
    public string? Plot { get; set; }
}

public class TvShowSeason : MediaEntry
{
    public int Number { get; set; }
    public TvShow? Show { get; set; }
    public TvShowEpisode[] Episodes { get; set; } = [];
}

public class TvShowEpisode : MediaEntry
{
    public TvShowSeason? Season { get; set; }
    public int Number { get; set; }
    public string? Plot { get; set; }
    public long? GeneratedBackgroundPictureId { get; set; }
}

public class Picture
{
    public long Id { get; set; }
    public long MediaItemId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? Description { get; set; }
    public byte[] Data { get; set; } = [];
    public string ContentType { get; set; } = string.Empty;
}
