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

    /// <summary>
    /// Id der Playlist, in deren Kontext die Wiedergabe läuft;
    /// <c>null</c> = keine Playlist-Wiedergabe (Serien-/Einzelkontext).
    /// </summary>
    public long? PlaylistId { get; init; }

    /// <summary>Name der wiedergegebenen Playlist; <c>null</c> ohne Playlist-Kontext.</summary>
    public string? PlaylistName { get; init; }

    /// <summary>Id des aktuell gespielten Playlist-Eintrags; 0 ohne Playlist-Kontext.</summary>
    public long PlaylistEntryId { get; init; }

    /// <summary>1-basierte Position des aktuellen Eintrags in der Playlist-Reihenfolge; 0 ohne Playlist-Kontext.</summary>
    public int PlaylistPosition { get; init; }

    /// <summary>Gesamtzahl der Playlist-Einträge laut Server; 0 ohne Playlist-Kontext.</summary>
    public int PlaylistTotalCount { get; init; }

    /// <summary>
    /// Der Server meldete keinen Folgeeintrag (204 auf Vorwärts-Navigation/
    /// Advance) — unterdrückt weitere Vorwärts-Versuche bis zum nächsten
    /// Medienwechsel.
    /// </summary>
    public bool PlaylistNextExhausted { get; init; }

    /// <summary>Die Wiedergabe läuft im Playlist-Kontext (Folgevideo aus der Playlist-Reihenfolge).</summary>
    public bool IsPlaylistPlayback => PlaylistId is > 0 && PlaylistEntryId > 0;

    /// <summary>Manuelle Vorwärts-Navigation ist sinnvoll möglich (Heuristik aus Position/Gesamtzahl).</summary>
    public bool CanNavigateNext =>
        IsPlaylistPlayback && !PlaylistNextExhausted && PlaylistPosition < PlaylistTotalCount;

    /// <summary>Manuelle Rückwärts-Navigation ist sinnvoll möglich.</summary>
    public bool CanNavigatePrevious => IsPlaylistPlayback && PlaylistPosition > 1;

    /// <summary>
    /// Kontextzeile der Playlist-Wiedergabe („„Name" · Eintrag n/m" —
    /// ohne Positionsdaten nur der Name).
    /// </summary>
    public string PlaylistLabel =>
        PlaylistPosition > 0 && PlaylistTotalCount > 0
            ? $"„{PlaylistName}“ · Eintrag {PlaylistPosition}/{PlaylistTotalCount}"
            : $"„{PlaylistName}“";

    /// <summary>
    /// In der Fortschrittsmeldung zu übermittelnder Playlist-Bezug:
    /// <see cref="PlaylistId"/> bei gültiger Playlist-Wiedergabe, sonst
    /// <c>null</c> (kontextlose Wiedergabe — der Server legt den
    /// Weiterschauen-Eintrag dann ohne Playlist-Bezug an).
    /// </summary>
    public long? ProgressReportPlaylistId => IsPlaylistPlayback ? PlaylistId : null;

    /// <summary>Fehlertext der letzten Quell-/Wiedergabe-Störung; <c>null</c> = kein Fehler.</summary>
    public string? Error { get; init; }

    /// <summary>Ein Fehler liegt vor (Fehlerbanner anzeigen).</summary>
    public bool HasError => Error is not null;

    /// <summary>
    /// Zuletzt an den Server gemeldete Position in Sekunden — Throttling
    /// der Fortschrittsmeldungen (keine Meldung ohne Positionsänderung).
    /// </summary>
    public double LastReportedPositionSeconds { get; init; } = -1;

    /// <summary>
    /// Anzahl aufeinanderfolgender plausibler Fortschrittstakte innerhalb
    /// des Endfensters — Bestätigungsgrundlage der „angesehen"-Schwelle
    /// (ein einzelner Ausreißer-Takt darf nicht auslösen).
    /// </summary>
    public int EndWindowStreak { get; init; }

    /// <summary>Eine Wiedergabe ist angefordert (State belegt).</summary>
    public bool IsActive => MediaId > 0 && MediaType is not null;
}
