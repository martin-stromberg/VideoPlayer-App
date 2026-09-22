namespace VideoPlayer.Mvu;

/// <summary>
/// Zustand der Wiedergabeseite: abzuspielendes Medium, aufgelöste Quelle,
/// Wiedergabeposition/-dauer, Pufferung, „angesehen"-Markierung,
/// Fortsetzungsliste (nächste Episoden), Picture-in-Picture-Zustand und
/// Fehlertext. Der Ausgangszustand ist <see cref="Empty"/>.
/// </summary>
public sealed record PlayerState
{
    /// <summary>Ausgangszustand (keine Wiedergabe angefordert).</summary>
    /// <value>Der leere Ausgangszustand der Wiedergabeseite.</value>
    public static PlayerState Empty { get; } = new();

    /// <summary>Medientyp des abzuspielenden Mediums (normalisiert, CLR-Konvention).</summary>
    public string? MediaType { get; init; }

    /// <summary>Id des abzuspielenden Mediums.</summary>
    public long MediaId { get; init; }

    /// <summary>Anzeigetitel des abzuspielenden Mediums.</summary>
    public string? Title { get; init; }

    /// <summary>Aufgelöste Wiedergabequelle; <c>null</c> während der Auflösung oder bei Fehlschlag.</summary>
    public PlaybackSource? Source { get; init; }

    /// <summary>Die Wiedergabequelle wird gerade aufgelöst.</summary>
    public bool IsResolving { get; init; }

    /// <summary>Die Seite wurde geöffnet und der Wiedergabeversuch gestartet.</summary>
    public bool Attempted { get; init; }

    /// <summary>Aktuelle Wiedergabeposition (letzter gemeldeter Takt).</summary>
    public TimeSpan Position { get; init; }

    /// <summary>Gesamtdauer des Mediums (soweit vom Player bekannt).</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Die Wiedergabe läuft gerade.</summary>
    public bool IsPlaying { get; init; }

    /// <summary>Der Player puffert (Zwischenladen).</summary>
    public bool IsBuffering { get; init; }

    /// <summary>
    /// Das Medium gilt als „angesehen" (Position hat das Ende abzüglich
    /// des eingestellten Zeitfensters erreicht bzw. das Ende wurde
    /// erreicht).
    /// </summary>
    public bool IsWatched { get; init; }

    /// <summary>Die Wiedergabe läuft im Picture-in-Picture-/Mini-Player-Modus.</summary>
    public bool IsPictureInPicture { get; init; }

    /// <summary>
    /// Nächste Episoden im Serienkontext (sortiert über Staffeln hinweg)
    /// — Grundlage der Auto-Fortsetzung und des Fehler-Rückfalls.
    /// </summary>
    public IReadOnlyList<PlayerEpisodeRef> UpcomingEpisodes { get; init; } = [];

    /// <summary>Eine Folge-Episode steht für Fortsetzung/Rückfall bereit.</summary>
    public bool HasNextEpisode => UpcomingEpisodes.Count > 0;

    /// <summary>Fehlertext der letzten Quell-/Wiedergabe-Störung; <c>null</c> = kein Fehler.</summary>
    public string? Error { get; init; }

    /// <summary>Ein Fehler liegt vor (Fehlerbanner anzeigen).</summary>
    public bool HasError => Error is not null;

    /// <summary>
    /// Zuletzt an den Server gemeldete Position in Sekunden — Throttling
    /// der Fortschrittsmeldungen (keine Meldung ohne Positionsänderung).
    /// </summary>
    public double LastReportedPositionSeconds { get; init; } = -1;

    /// <summary>Eine Wiedergabe ist angefordert (State belegt).</summary>
    public bool IsActive => MediaId > 0 && MediaType is not null;
}
