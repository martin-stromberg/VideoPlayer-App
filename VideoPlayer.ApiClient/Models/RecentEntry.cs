namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Zuletzt hinzugefügter Inhalt („Neu im Programm").
/// </summary>
public class RecentEntry
{
    /// <summary>ID des Eintrags.</summary>
    public long Id { get; set; }

    /// <summary>ID der zugehörigen Medienquelle.</summary>
    public long MediaSourceId { get; set; }

    /// <summary>Veröffentlichungs-/Hinzufügezeitpunkt.</summary>
    public DateTime PublishedAt { get; set; }

    /// <summary>Erfassungszeitpunkt, falls bekannt.</summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>Art des Eintrags.</summary>
    public RecentEntryKind Type { get; set; }

    /// <summary>Zugehöriger Medieneintrag, falls mitgeliefert.</summary>
    public MediaEntry? Entry { get; set; }
}

/// <summary>
/// Art eines zuletzt hinzugefügten Inhalts.
/// </summary>
public enum RecentEntryKind
{
    /// <summary>Film.</summary>
    Movie,

    /// <summary>Filmkollektion.</summary>
    MovieCollection,

    /// <summary>Serie.</summary>
    TvShow,

    /// <summary>Staffel einer Serie.</summary>
    TvShowSeason,

    /// <summary>Episode einer Serie.</summary>
    TvShowEpisode
}
