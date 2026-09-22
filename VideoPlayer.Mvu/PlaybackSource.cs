namespace VideoPlayer.Mvu;

/// <summary>
/// Aufgelöste Wiedergabequelle eines Mediums: Ort der Quelle (Stream-URL
/// bzw. lokaler Dateipfad) und die zum Start anzuspringende
/// Fortsetzungsposition (Weiterschauen-Stand), falls bekannt.
/// </summary>
public sealed record PlaybackSource
{
    /// <summary>Art der Quelle (Stream-URL oder lokale Datei).</summary>
    public required PlaybackSourceKind Kind { get; init; }

    /// <summary>
    /// Ort der Quelle: bei <see cref="PlaybackSourceKind.ServerStream"/>
    /// die vollständige Stream-URL (inkl. Zugriffstoken), bei
    /// <see cref="PlaybackSourceKind.LocalFile"/> der Dateipfad.
    /// </summary>
    public required string Location { get; init; }

    /// <summary>
    /// Fortsetzungsposition, auf die beim Start gesprungen wird;
    /// <c>null</c> = Wiedergabe beginnt am Anfang.
    /// </summary>
    public TimeSpan? ResumePosition { get; init; }
}
