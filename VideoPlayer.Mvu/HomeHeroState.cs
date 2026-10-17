namespace VideoPlayer.Mvu;

/// <summary>
/// Zustand des Hero-Kopfbereichs der Startseite: Ladezustand und die
/// Bilddaten des servergenerierten Hintergrunds
/// (<c>api/pictures/hero-background</c>). Die Bilddaten liegen flüchtig
/// im Model — eine dauerhafte Ablage ist nicht vorgesehen. Die
/// Sichtbarkeit des Bereichs steuert die Einstellung
/// <see cref="VideoPlayer.Storage.AppSettings.HomeHeroEnabled"/> über
/// <see cref="AppModel.ShowHomeHero"/>; ein fehlgeschlagener Abruf lässt
/// <see cref="ImageBytes"/> leer — der Bereich zeigt dann nur die
/// Gradienten und Texte (Web-Verhalten: ohne Bild greifen die Gradienten
/// über dem Seitenhintergrund).
/// </summary>
public sealed record HomeHeroState
{
    /// <summary>Ausgangszustand (nichts geladen, kein Ladevorgang).</summary>
    /// <value>Der leere Ausgangszustand des Hero-Kopfbereichs.</value>
    public static HomeHeroState Empty { get; } = new();

    /// <summary>Der Hero-Hintergrund wird gerade vom Server geladen.</summary>
    public bool IsLoading { get; init; }

    /// <summary>
    /// Bilddaten (JPEG) des Hero-Hintergrunds; <c>null</c>, wenn noch
    /// nichts geladen wurde oder der Abruf fehlgeschlagen ist.
    /// </summary>
    public byte[]? ImageBytes { get; init; }

    /// <summary>Ein Hero-Bild liegt vor.</summary>
    public bool HasImage => ImageBytes is { Length: > 0 };
}
