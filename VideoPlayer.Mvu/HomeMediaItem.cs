namespace VideoPlayer.Mvu;

/// <summary>
/// Anzeigefertiger Eintrag einer Startseiten-Liste (Weiterschauen,
/// Favoriten, Neu im Programm). Wird von <see cref="HomeListService"/> aus
/// den gecachten Serverdaten gemappt; plattformneutral — Vorschaubilder
/// werden am App-Rand über <see cref="PosterPictureId"/> aufgelöst.
/// </summary>
public sealed record HomeMediaItem
{
    /// <summary>Id des Mediums (Navigationsziel der Detailseite).</summary>
    public required long Id { get; init; }

    /// <summary>
    /// Medientyp, kleingeschrieben — je nach Herkunft die Server-Konvention
    /// der Weiterschauen-Liste (<c>movie</c>, <c>show</c>, <c>season</c>,
    /// <c>episode</c>, <c>collection</c>) oder die CLR-Konvention der
    /// Favoriten-Kopie (<c>movie</c>, <c>moviecollection</c>, <c>tvshow</c>,
    /// <c>tvshowseason</c>, <c>tvshowepisode</c>).
    /// </summary>
    public required string MediaType { get; init; }

    /// <summary>Anzeigetitel des Eintrags.</summary>
    public required string Title { get; init; }

    /// <summary>
    /// Bild-Id für den Server-Endpunkt <c>api/pictures/{id}</c>;
    /// <c>null</c> = Karte zeigt den Titel als Fallback.
    /// </summary>
    public long? PosterPictureId { get; init; }

    /// <summary>
    /// Wiedergabefortschritt im Bereich 0–1 (nur Weiterschauen-Liste);
    /// <c>null</c> = kein Fortschrittsbalken.
    /// </summary>
    public double? Progress { get; init; }

    /// <summary>
    /// Id der zugehörigen Serie (Episoden-/Staffel-Einträge, aus dem
    /// eingebetteten Objektgraph); <c>null</c> = kein Serienkontext
    /// bekannt.
    /// </summary>
    public long? ShowId { get; init; }

    /// <summary>Id der zugehörigen Staffel (Episoden-Einträge bzw. die Staffel selbst).</summary>
    public long? SeasonId { get; init; }

    /// <summary>Id der Episode selbst bei Episoden-Einträgen; sonst <c>null</c>.</summary>
    public long? EpisodeId { get; init; }

    /// <summary>
    /// Platzhalter-Eintrag der Offline-„Weiterschauen"-Liste: das Medium
    /// ist nicht heruntergeladen und damit nicht abspielbar — die Karte
    /// wird abgedunkelt und mit einem Hinweis markiert.
    /// </summary>
    public bool IsPlaceholder { get; init; }

    /// <summary>Der Eintrag ist auswählbar (Platzhalter sind es nicht).</summary>
    public bool IsSelectable => !IsPlaceholder;

    /// <summary>Deckkraft der Karte (Platzhalter abgedunkelt).</summary>
    public double CardOpacity => IsPlaceholder ? 0.55 : 1.0;

    /// <summary>Ein Poster kann geladen werden.</summary>
    public bool HasPoster => PosterPictureId is > 0;

    /// <summary>Ein Fortschrittsbalken wird auf der Karte angezeigt.</summary>
    public bool ShowProgress => Progress is > 0;
}
