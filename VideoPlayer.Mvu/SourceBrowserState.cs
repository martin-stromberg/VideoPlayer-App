namespace VideoPlayer.Mvu;

/// <summary>
/// Zustand der Quellen-Browsing-Seite: geöffnete Quelle, Genre-Filter,
/// Suchtext und seitenweise nachgeladene Medieneinträge samt Lade-/
/// Fehlerzuständen. <see cref="NextPage"/> zählt die nächste zu ladende
/// Seite (0-basiert); <see cref="HasMore"/> wird falsch, sobald eine
/// Seite weniger als <see cref="PageSize"/> Einträge liefert.
/// </summary>
public sealed record SourceBrowserState
{
    /// <summary>Seitengröße des Nachladens (Wert des Altbestands).</summary>
    public const int PageSize = 20;

    /// <summary>Ausgangszustand (keine Quelle geöffnet).</summary>
    public static SourceBrowserState Empty { get; } = new();

    /// <summary>Id der geöffneten Quelle; 0 = kein Browser aktiv.</summary>
    public long SourceId { get; init; }

    /// <summary>Anzeigename der geöffneten Quelle (Seitentitel).</summary>
    public string SourceName { get; init; } = string.Empty;

    /// <summary>Genre-Leiste inklusive „Alle"-Eintrag (<see cref="SourceGenreItem.Id"/> = 0).</summary>
    public IReadOnlyList<SourceGenreItem> Genres { get; init; } = [];

    /// <summary>Aktuell gewählter Genre-Filter; 0 = „Alle" (kein Filter).</summary>
    public long SelectedGenreId { get; init; }

    /// <summary>Aktiver Suchtext (<c>search</c>-Parameter); leer = keine Suche.</summary>
    public string SearchText { get; init; } = string.Empty;

    /// <summary>Geladene Medieneinträge (kumuliert über alle Seiten).</summary>
    public IReadOnlyList<SourceMediaItem> Items { get; init; } = [];

    /// <summary>Nächste zu ladende Seite (0-basiert).</summary>
    public int NextPage { get; init; }

    /// <summary>Es gibt (vermutlich) weitere Seiten zum Nachladen.</summary>
    public bool HasMore { get; init; }

    /// <summary>Erste Seite / gefilterte Liste lädt (Ersetzen).</summary>
    public bool IsLoading { get; init; }

    /// <summary>Eine Folgeseite lädt (Anhängen).</summary>
    public bool IsLoadingMore { get; init; }

    /// <summary>Mindestens ein Ladeversuch wurde abgeschlossen.</summary>
    public bool Attempted { get; init; }

    /// <summary>Fehlertext des letzten Ersetz-Laufs; <c>null</c> = kein Fehler.</summary>
    public string? Error { get; init; }

    /// <summary>Ein Browser-Kontext ist aktiv (Quelle gewählt).</summary>
    public bool IsActive => SourceId > 0;

    /// <summary>Das Raster enthält Einträge.</summary>
    public bool HasItems => Items.Count > 0;

    /// <summary>Der letzte Ladeversuch ist fehlgeschlagen.</summary>
    public bool HasError => Error is not null;

    /// <summary>Erfolgreich geladen, aber keine Einträge (ggf. durch Filter/Suche).</summary>
    public bool ShowEmpty => Attempted && !IsLoading && !HasError && !HasItems;
}
