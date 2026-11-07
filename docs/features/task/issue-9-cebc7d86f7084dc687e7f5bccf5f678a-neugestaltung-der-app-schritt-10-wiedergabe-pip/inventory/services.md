# Bestandsaufnahme: Services und Ablage

## `ProgressReportService` (`VideoPlayer.Mvu/ProgressReportService.cs`)

`ReportProgressAsync(videoType, videoId, position, duration, ct)`:
sendet `api/continue-watching/progress` über den angemeldeten
Client; bei Offline, fehlendem Client oder überholbarem Fehler
(`ApiException` ohne Status, `HttpRequestException`, Timeout,
408/429/401) wird die Meldung über `PendingProgressSyncService` in
`pending_progress` eingereiht. Dauerhafte 4xx werden verworfen
(`IsPermanentClientError`). — Direkt aus dem Player verwendbar.

## `PendingProgressSyncService` (`VideoPlayer.Storage/`)

Persistente Warteschlange auf der backup-pflichtigen Tabelle
`pending_progress`: `EnqueueAsync` (pro Medium nur der neueste
Stand), `GetPendingAsync` (chronologisch), `RemoveForMediaAsync`,
`CountAsync`.

## `ServerSyncService` (`VideoPlayer.Mvu/ServerSyncService.cs`)

`SynchronizeAsync` läuft über `RunFullSyncEffect` bei jeder
(Wieder-)Verbindung (`EnterConnected`): `FlushPendingProgressAsync`
sendet wartende Meldungen in Reihenfolge nach, erst danach werden
die Metadaten-Caches erneuert. Der Drain bei Online-Rückkehr
existiert also bereits — der Player muss nur einspeisen.

## `AppSettingsService` (`VideoPlayer.Storage/`)

Schlüssel `AutoResumeEnabledKey`, `AutoResumeRemainingSecondsKey`,
`WatchedEndWindowSecondsKey` existieren; `Load()` liefert
normalisierte `AppSettings`. Das Model hält sie in
`AppModel.Settings`.

## `AuthSessionService` (`VideoPlayer.Mvu/`)

`Client` (angemeldeter `VideoPlayerApiClient` mit `CurrentToken`),
`ClientBaseAddress` — Basis für die Stream-URL-Bildung. Das Token
steht zusätzlich über `AppSecretsService.GetAuthTokenAsync()`
(sichere Ablage) zur Verfügung.

## `VideoPlayerApiClient` (`VideoPlayer.ApiClient/`)

`GetContinueWatchingAsync` liefert `ContinueWatchingEntry` mit
`MediaType`, `Entry.Id`, `PositionSeconds`, `DurationSeconds` —
Quelle der Fortsetzungsposition. `ReportPlaybackProgressAsync`
(POST `api/continue-watching/progress`) wird vom
`ProgressReportService` genutzt. Ein Stream-URL-Helfer existiert
nicht — der Endpunkt `api/items/{typ}/{id}/stream` wird im
Altbestand per String gebaut (s. `legacy.md`).

## `HomeListService` / `CacheDatabase`

`LoadHomeListsAsync` liest die gecachten Listen (inkl.
Weiterschauen-`ContinueWatchingCacheKey` = `"continue_watching"`).
Die gecachten `ContinueWatchingEntry`-Daten enthalten
`PositionSeconds`/`DurationSeconds` — offline aus dem Cache lesbar.

## DI-Registrierung (`VideoPlayer.Maui/MauiProgram.cs`)

`AuthSessionService`, `PendingProgressSyncService`,
`ProgressReportService`, `ServerSyncService`, `MediaDetailsService`
sind Singletons; Seiten sind Transient. `UseMauiCommunityToolkit`
/`UseMauiCommunityToolkitMediaElement` fehlen noch.
