namespace VideoPlayer.Mvu;

/// <summary>
/// Anforderungsdatensatz einer Download-Einreihe — die fachlich
/// zusammengehörige Datengruppe von <see cref="DownloadService.EnqueueAsync"/>:
/// Medium (Typ, Id, Titel), optionale Kontexte (Serie, Playlist),
/// Vorschaubild und Herkunft des Auftrags. Der Medientyp wird vom
/// Dienst normalisiert (<see cref="MediaTypeNames.Normalize"/>); nur
/// <c>movie</c>/<c>tvshowepisode</c> sind ladbar.
/// </summary>
/// <param name="MediaType">Der Medientyp des Downloads.</param>
/// <param name="MediaId">Die Medien-Id des Downloads.</param>
/// <param name="Title">Der Anzeigetitel des Downloads; <c>null</c> = Fallback.</param>
/// <param name="SeriesContext">Optionaler Serienkontext eines Episoden-Downloads (Grundlage des Offline-„Weiterschauen").</param>
/// <param name="PlaylistContext">Optionaler Playlist-Bezug des Downloads (Herkunft und Reihenfolge playlist-gebundener Einplanungen).</param>
/// <param name="PictureId">Bild-Id des Vorschaubilds (<c>api/pictures/{id}</c> — Episoden-Still bzw. Film-Poster); <c>null</c> = kein Bild bekannt.</param>
/// <param name="Origin">Herkunft des Downloads (<see cref="DownloadOrigin.Manual"/> über die Oberfläche, <see cref="DownloadOrigin.Auto"/> aus dem Auto-Download-Vorlauf).</param>
/// <returns>Anforderungs-Datensatz der Einreihe-Operation.</returns>
public sealed record DownloadEnqueueRequest(
    string MediaType,
    long MediaId,
    string? Title,
    DownloadSeriesContext? SeriesContext = null,
    DownloadPlaylistContext? PlaylistContext = null,
    long? PictureId = null,
    DownloadOrigin Origin = DownloadOrigin.Manual);
