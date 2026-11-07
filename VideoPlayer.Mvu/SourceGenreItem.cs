namespace VideoPlayer.Mvu;

/// <summary>
/// Eintrag der Genre-Leiste der Quellen-Browsing-Seite. Der künstliche
/// Eintrag „Alle" (<see cref="Id"/> = 0) steht für „kein Genre-Filter"
/// (Muster des Altbestands).
/// </summary>
public sealed record SourceGenreItem
{
    /// <summary>Genre-Id; 0 = „Alle" (kein Filter — <c>genreId</c> wird nicht gesendet).</summary>
    public required long Id { get; init; }

    /// <summary>Anzeigename des Genres.</summary>
    public required string Name { get; init; }

    /// <summary>Das Genre ist der aktuell gewählte Filter.</summary>
    public bool IsSelected { get; init; }
}
