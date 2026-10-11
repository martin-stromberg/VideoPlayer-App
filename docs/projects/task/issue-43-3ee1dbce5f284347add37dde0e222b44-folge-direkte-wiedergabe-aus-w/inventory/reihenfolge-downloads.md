# Detailbefund — Anforderung 4: Wiedergabe-/Download-Reihenfolge

## Folgevideo beim Abspielen (Ist-Zustand)

- `PlayerState.UpcomingEpisodes` (`VideoPlayer.Mvu/PlayerState.cs` Z. 59): Liste der
  `PlayerEpisodeRef` (Id + Titel) — Basis der Auto-Fortsetzung und des Fehler-Rückfalls.
- Befüllung ausschließlich in `AppUpdate.UpdatePlayRequested` (Z. 1941) über
  `NextEpisodeQueue(model, mediaId)` (`AppUpdate.cs` Z. 1956–1978): **aus dem
  Serienkontext der geladenen Detailseite** (`model.MediaDetails.Details.Seasons`),
  staffelübergreifend nach `Season.Number`/`Episode.Number` sortiert, alle Episoden
  nach der gestarteten. Ohne geladenen Serienkontext (`details?.HasSeasons != true`)
  bleibt die Liste **leer** — kein Rückfall, keine Playlist-Logik.
- Verbrauch: `AdvanceToNextEpisode` (`AppUpdate.cs` Z. 2030–2072) schaltet auf
  `UpcomingEpisodes[0]` um und löst `ResolvePlaybackSourceEffect("tvshowepisode", …)` neu aus.
  Auslöser: fehlgeschlagene Quellauflösung (`UpdatePlaybackSourceResolved` Z. 2016),
  Auto-Fortsetzung im Fortschrittstakt (`UpdatePlaybackProgress` Z. 2136–2146,
  Restlaufzeit < `Settings.AutoResumeRemainingSeconds` bei laufender Wiedergabe) und
  Ende der Wiedergabe (`UpdatePlaybackEnded` Z. 2190–2197).
- `DeleteWatchedDownloadEffect` räumt bei `AutoDeleteWatchedEpisodes` den Download
  gesehener Episoden ab (Z. 2126–2129, 2183–2186).

**Befund Anf. 4 (Wiedergabe):** Das „richtige Folgevideo" wird aktuell ausschließlich aus
der **Serien-Reihenfolge** der geladenen Detailansicht bestimmt. Ein Playlist-Bezug existiert
nirgends — weder im Weiterschauen-DTO (`ContinueWatchingEntry` hat kein Playlist-Feld) noch
im Player-State. Ein direkter Wiedergabestart aus der Weiterschauen-Liste (Anf. 1) ohne
vorher geladene Detailseite hätte außerdem eine **leere** `UpcomingEpisodes`-Liste, wenn
`model.MediaDetails` nicht zufällig den passenden Serienkontext enthält (Vermutung für die
Umsetzung: Serienkontext müsste unabhängig von der Detailseite aufgelöst werden, bzw. der
Playlist-Kontext müsste vom Server kommen — serverseitig nicht in diesem Repo prüfbar).

## Download-Reihenfolge (Ist-Zustand)

- `DownloadService` (`VideoPlayer.Mvu/DownloadService.cs`): persistente Warteschlange
  `download_manifest` (Tabelle in `videoplayer-cache.db3`, siehe
  `VideoPlayer.Storage/Entities/DownloadManifestEntry.cs` — Felder ohne Playlist-Bezug).
- `MaxParallelDownloads = 2` (Z. 61); `PumpQueueAsync` (Z. 484–538) startet wartende
  Aufträge, `NextQueuedEntryAsync` (Z. 545–553) wählt den **ältesten** `queued`-Eintrag
  (`OrderBy(e => e.Id)` = Manifest-Id = Einreihe-Reihenfolge, FIFO).
- Einreihen: `EnqueueAsync` (Z. 224–316) — Dedup pro Medium, Quota-Vorabprüfung,
  `NoticeEnqueued`; Serienkontext (`DownloadSeriesContext`: ShowId/ShowTitle/
  SeasonNumber/EpisodeNumber) wird ins Manifest geschrieben (`ApplySeriesContext`
  Z. 325–336).
- Auslöser des Einreihens:
  - Detailseite: `OnDownloadClicked`/`OnEpisodeDownloadClicked`
    (`MediaDetailsPage.xaml.cs` Z. 346–397) → `DownloadRequestedMessage` →
    `UpdateDownloadRequested` → `EnqueueDownloadEffect` (mit Serien-Hints).
  - Automatischer Vorlauf: `AutoDownloadService.RunAsync`
    (`VideoPlayer.Mvu/AutoDownloadService.cs`) — liest die Weiterschauen-Liste aus dem
    Cache und reiht je Episoden-Eintrag die nächsten `PreloadEpisodeCount` Episoden ein
    (`EnqueueNextEpisodesAsync` Z. 154–192: **`api/items/tvshow/{showId}`**, sortiert nach
    Staffel-/Episodennummer). Filme werden als Film selbst eingeplant (Z. 119–122).
    Trigger: `RunAutoDownloadsEffect` aus `AppUpdate` (Z. 215 beim Einschalten der
    Einstellung; Z. 982/999 nach Sync-/Cache-Ereignissen), gedrosselt auf 60 s.
- Downloads-Seite: `LoadItemsAsync` (Z. 177–184) sortiert nach `Id` (Auftragsreihenfolge);
  `DownloadsPage` zeigt Manifest-Einträge mit Abbrechen/Wiederholen/Löschen
  (`DownloadsPage.xaml.cs`, `DownloadListSync.cs`).

**Befund Anf. 4 (Downloads):** Reihenfolge = Einreihe-Reihenfolge (FIFO über Manifest-Id).
Bei Episoden-Vorlauf entspricht das der **Serienreihenfolge** — eine Playlist-Reihenfolge
ist dem Manifest nicht bekannt und kann derzeit nicht abgebildet werden (kein Feld, kein
API-Bezug).

## Playlist-Bezug in Weiterschauen-Einträgen

- `ContinueWatchingEntry` (`VideoPlayer.ApiClient/Models/ContinueWatching.cs` Z. 6–25):
  `MediaType`, `Entry`, `PositionSeconds`, `DurationSeconds`, `Title`, `PosterPictureId` —
  **kein** Playlist-Feld.
- `HomeMediaItem` (`VideoPlayer.Mvu/HomeMediaItem.cs`): `Id`, `MediaType`, `Title`,
  `PosterPictureId`, `Progress`, `ShowId`, `SeasonId`, `EpisodeId`, `IsPlaceholder` —
  ebenfalls **kein** Playlist-Feld.
- Ob die Server-API Playlist-Metadaten an Weiterschauen-Einträgen liefert, ist aus diesem
  Repository nicht verifizierbar (Server-Repo separat). *Vermutung:* Wenn der Server einen
  Playlist-Bezug im DTO liefert, müsste er in `ContinueWatchingEntry`, `HomeMediaItem` und
  ggf. `DownloadManifestEntry`/`DownloadSeriesContext` durchgereicht werden.
