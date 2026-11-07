namespace VideoPlayer.Mvu;

/// <summary>
/// Art einer aufgelösten Wiedergabequelle (<see cref="PlaybackSource"/>):
/// Server-Stream oder lokale Datei (Download — die Befüllung des
/// Download-Manifests folgt in Entwicklungsschritt 11).
/// </summary>
public enum PlaybackSourceKind
{
    /// <summary>Stream-Endpunkt des Servers (<c>api/items/{typ}/{id}/stream</c>).</summary>
    ServerStream,

    /// <summary>Lokale Videodatei aus dem Download-Manifest.</summary>
    LocalFile
}
