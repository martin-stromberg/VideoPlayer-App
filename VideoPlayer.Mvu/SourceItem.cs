namespace VideoPlayer.Mvu;

/// <summary>
/// Anzeigefertiger Eintrag der Quellen-Übersicht (freigegebener
/// Medienbereich des Servers). Icons werden am App-Rand über
/// <see cref="IconPictureId"/> (<c>api/sourceicons/{id}</c>) aufgelöst.
/// </summary>
public sealed record SourceItem
{
    /// <summary>Id der Quelle (Navigationsziel der Browsing-Seite).</summary>
    public required long Id { get; init; }

    /// <summary>Anzeigename der Quelle.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// Bild-Id für den Server-Endpunkt <c>api/sourceicons/{id}</c>;
    /// <c>null</c> = kein Icon.
    /// </summary>
    public long? IconPictureId { get; init; }

    /// <summary>Ein Quellen-Icon kann geladen werden.</summary>
    public bool HasIcon => IconPictureId is > 0;
}
