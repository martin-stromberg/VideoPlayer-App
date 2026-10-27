namespace VideoPlayer.Mvu;

/// <summary>
/// Herkunft eines Downloads. Wird im Download-Manifest
/// (<c>download_manifest</c>) als kleingeschriebener String persistiert
/// (<see cref="DownloadOriginNames"/>).
/// </summary>
public enum DownloadOrigin
{
    /// <summary>Der Download wurde über die Oberfläche eingeplant (Detailseite, Episodenliste, „Erneut versuchen").</summary>
    Manual,

    /// <summary>Der Download wurde durch den automatischen Vorlauf eingeplant bzw. als Auto-Download-Kandidat verlängert.</summary>
    Auto
}

/// <summary>
/// Übersetzung zwischen <see cref="DownloadOrigin"/> und den persistierten
/// Herkunfts-Strings des Download-Manifests.
/// </summary>
public static class DownloadOriginNames
{
    /// <summary>Bildet die Herkunft auf den persistierten String ab.</summary>
    /// <param name="origin">Die Herkunft.</param>
    /// <returns>Der Herkunfts-String des Manifests.</returns>
    public static string ToPersisted(DownloadOrigin origin)
    {
        return origin switch
        {
            DownloadOrigin.Auto => "auto",
            _ => "manual"
        };
    }

    /// <summary>Liest den persistierten Herkunfts-String als Herkunft (<c>null</c>/unbekannte Werte — Altbestand — gelten als <see cref="DownloadOrigin.Manual"/>).</summary>
    /// <param name="origin">Der persistierte Herkunfts-String.</param>
    /// <returns>Die gelesene Herkunft.</returns>
    public static DownloadOrigin FromPersisted(string? origin)
    {
        return origin?.Trim().ToLowerInvariant() switch
        {
            "auto" => DownloadOrigin.Auto,
            _ => DownloadOrigin.Manual
        };
    }
}
