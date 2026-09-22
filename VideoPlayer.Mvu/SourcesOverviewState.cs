namespace VideoPlayer.Mvu;

/// <summary>
/// Zustand der Quellen-Übersicht (Menüseite „Quellen"): die vom Server
/// freigegebenen Medienbereiche samt Lade-/Fehlerzustand. Im
/// Offline-Zustand wird kein Ladevorgang angestoßen — die Seite zeigt
/// einen Hinweis statt einer leeren Liste.
/// </summary>
public sealed record SourcesOverviewState
{
    /// <summary>Ausgangszustand (nichts geladen, kein Fehler).</summary>
    public static SourcesOverviewState Initial { get; } = new();

    /// <summary>Die Quellenliste wird gerade vom Server geladen.</summary>
    public bool IsLoading { get; init; }

    /// <summary>Mindestens ein Ladeversuch wurde abgeschlossen.</summary>
    public bool Attempted { get; init; }

    /// <summary>Fehlertext des letzten Ladeversuchs; <c>null</c> = kein Fehler.</summary>
    public string? Error { get; init; }

    /// <summary>Freigegebene Quellen (Stand der Server-API).</summary>
    public IReadOnlyList<SourceItem> Sources { get; init; } = [];

    /// <summary>Die Liste enthält Einträge.</summary>
    public bool HasSources => Sources.Count > 0;

    /// <summary>Der letzte Ladeversuch ist fehlgeschlagen.</summary>
    public bool HasError => Error is not null;

    /// <summary>Erfolgreich geladen, aber keine Quelle freigegeben.</summary>
    public bool ShowEmpty => Attempted && !IsLoading && !HasError && !HasSources;
}
