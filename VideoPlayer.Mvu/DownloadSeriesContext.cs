namespace VideoPlayer.Mvu;

/// <summary>
/// Serienkontext eines Episoden-Downloads: wird beim Einreihen mitgegeben
/// (<see cref="DownloadRequestedMessage"/>, <see cref="EnqueueDownloadEffect"/>)
/// und im Download-Manifest (<c>download_manifest</c>) gespeichert. Grundlage
/// des Offline-„Weiterschauen" — aus dem Kontext bestimmt der
/// <see cref="OfflineContinueWatchingService"/> ohne Server die Folge-Episode
/// und stellt fehlende Episoden als Platzhalter dar.
/// </summary>
/// <param name="ShowId">Id der zugehörigen Serie; <c>null</c> = unbekannt.</param>
/// <param name="ShowTitle">Anzeigetitel der Serie; <c>null</c> = unbekannt.</param>
/// <param name="SeasonNumber">Staffelnummer der Episode; <c>null</c> = unbekannt.</param>
/// <param name="EpisodeNumber">Episodennummer innerhalb der Staffel; <c>null</c> = unbekannt.</param>
/// <returns>Kontextdatensatz eines Episoden-Downloads.</returns>
public sealed record DownloadSeriesContext(
    long? ShowId,
    string? ShowTitle,
    int? SeasonNumber,
    int? EpisodeNumber);
