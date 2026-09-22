# Plan-Review

Geprüft: `plan.md` gegen die Implementierung (Selbstprüfung ohne
Unteragenten — dokumentierte Abweichung, es stehen keine Unteragenten
zur Verfügung). Alle Planelemente wurden im Code gesucht und
vollständig gelesen.

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Projekt `VideoPlayer.Mvu` (net10.0)

- [x] `DownloadState` — `Queued`, `Downloading`, `Completed`, `Failed`,
  `Cancelled` samt `DownloadStateNames` (Persistenz-Mapping auf
  kleingeschriebene Strings).
- [x] `DownloadListItem` — `EntryId`, `MediaType`, `MediaId`, `Title`,
  `State`, `DownloadedBytes`, `TotalBytes`, `Error`, abgeleitet
  `IsActive`/`CanCancel`/`CanRetry`/`CanDelete`/`ShowProgress`/
  `HasError`/`Progress`/`ProgressPercentText`/`StateText`/`ErrorText`/
  `MediaTypeText` (deutsche Anzeigetexte).
- [x] `DownloadsPageState` — `IsLoading`, `Attempted`, `Error`,
  `Items`, `UsedBytes`, `QuotaGb`, abgeleitet `HasItems`/`HasError`/
  `ShowEmpty`/`QuotaText`.
- [x] `DownloadService` — Manifest-CRUD, `EnqueueAsync`
  (Einreihen/Dedup/Rücksetzen), `LoadItemsAsync`, `GetUsedBytes`,
  `CancelAsync`, `DeleteAsync`, `DeleteIfCompletedAsync`,
  `ResumeInterruptedAsync`; Engine-Pump mit
  `MaxParallelDownloads = 2`; Quota-Vorprüfung, Startprüfung und
  laufende Prüfung im Schreibstrom (periodische Neumessung der
  Verzeichnisbelegung gegen parallele Überschreitung);
  `ItemChanged`/`ItemRemoved`-Ereignisse; Fehlercodes
  `quota_exceeded`/`no_connection`/`network`/`io`/`http_*`;
  `NoticeAlready*`/`NoticeEnqueued`-Hinweistexte; injizierbare
  `HttpClient`-Fabrik, `fileExists` und `quotaBytesOverride` für Tests.
- [x] `AutoDownloadService` — `RunAsync(settings, force)` mit Guards
  (`AutoDownloadEnabled`, WLAN-Bedingung, angemeldeter Client,
  60-s-Throttle, Lauf-Sperre); Film-Einträge direkt, Episoden-Einträge
  über `GetTvShowAsync` → geordnete Folge-Episoden bis
  `PreloadEpisodeCount`; Fehler je Eintrag protokolliert/übersprungen;
  injizierbare Zeitquelle.
- [x] `INetworkStatusMonitor.IsWifi` — neu; Semantik: ungedrosseltes
  Festnetz-Profil (WLAN oder Ethernet).
- [x] `MediaDetailsState` — `DownloadNotice`, `DownloadNoticeIsError`.
- [x] `AppModel.Downloads` (`DownloadsPageState.Initial`).
- [x] Messages in `AppMessage.cs`: `DownloadsPageOpenedMessage`,
  `DownloadsLoadedMessage`, `DownloadRequestedMessage`,
  `DownloadEnqueueCompletedMessage`, `DownloadItemChangedMessage`,
  `DownloadDeletedMessage`, `DownloadCancelRequestedMessage`,
  `DownloadRetryRequestedMessage`, `DownloadDeleteRequestedMessage`.
- [x] Effekte in `AppEffect.cs`: `LoadDownloadsEffect`,
  `EnqueueDownloadEffect`, `CancelDownloadEffect`,
  `DeleteDownloadEffect`, `ResumeDownloadsEffect`,
  `RunAutoDownloadsEffect`, `DeleteWatchedDownloadEffect`.
- [x] `AppUpdate`: `UpdateDownloadsPageOpened` (lokaler Lade-Effekt,
  offline möglich), `UpdateDownloadsLoaded`, `UpdateDownloadRequested`
  (Guard + Offline-Hinweis), `UpdateDownloadEnqueueCompleted`
  (Item-Upsert + Detail-Hinweis), `UpdateDownloadItemChanged`
  (Upsert), `UpdateDownloadDeleted` (Entfernen),
  `UpdateDownloadCancelRequested`/`…Retry`/`…Delete` (Guards);
  Trigger: `EnterConnected` + `UpdateNetworkAvailabilityChanged`
  (online, Connected) → `ResumeDownloadsEffect`;
  `UpdateSynchronizationCompleted`/`UpdateCachedListsChanged` →
  `RunAutoDownloadsEffect` (bei `Connected` + Schalter);
  `UpdateSaveSettingsRequested` → `RunAutoDownloadsEffect(force)`
  beim Einschalten; `UpdatePlaybackProgress`/`UpdatePlaybackEnded` →
  `DeleteWatchedDownloadEffect` (Episode + Schalter).

### Projekt `VideoPlayer.Storage`

- [x] `DownloadManifestEntry` um `Title`, `DownloadedBytes`,
  `TotalBytes`, `Error` erweitert; `State` dokumentiert
  (`queued`/`downloading`/`completed`/`failed`/`cancelled`);
  `CacheDatabase` legt die Spalten per `CreateTableAsync` an.
- [x] Dateien unter `AppStorageLayout.VideosDirectory` (no-backup).

### Projekt `VideoPlayer.Maui`

- [x] `ConnectivityNetworkStatusMonitor.IsWifi` — `ConnectionProfiles`
  enthält `WiFi` **oder** `Ethernet` (Review-Korrektur: kabelgebundene
  Festnetz-Anbindung zählt als ungedrosselt).
- [x] `ShellAppEffectHandler` — alle sieben Download-Effekte
  verdrahtet; `ItemChanged`/`ItemRemoved`-Subscriptions dispatcht als
  `DownloadItemChangedMessage`/`DownloadDeletedMessage`.
- [x] `MauiProgram` — `DownloadService`, `AutoDownloadService` als
  Singletons.
- [x] `DownloadsPage.xaml(.cs)` — Quota-Zeile, Offline-Banner,
  Lade-/Fehler-/Leerzustand, `CollectionView` mit Titel, Typ, Zustand,
  Fortschrittsbalken, Prozent und Fehler je Eintrag; Aktionen
  Abbrechen/Wiederholen/Löschen.
- [x] `MediaDetailsPage.xaml(.cs)` — „Herunterladen" im Kopfbereich
  (`IsPlayable`), ⤓ je Episoden-Eintrag, `DownloadNoticeLabel`.

### Tests (`VideoWebPlayer.Maui.Tests`)

- [x] `DownloadServiceTests` — Einreihen/Dedup, Parallelität
  (Gate-Test), Quota (Vorab + Mid-Write), Abbrechen (laufend +
  wartend), WLAN-only (blockiert/fortgesetzt), Wiederaufnahme
  (verwaist/abgebrochen/Quota), HTTP-/Transportfehler, Löschen
  (fertig/laufend), `DeleteIfCompleted`, Retry, Fortschritt,
  ohne Verbindung, ungültiger Typ, Listenladen.
- [x] `AutoDownloadServiceTests` — Film/Episode-Kandidaten,
  Preload-Anzahl, Schalter, WLAN-only, nicht angemeldet, Dedup,
  Throttling/Force, fehlender Serienkontext.
- [x] `AppUpdateTests_Downloads` — Seitenfluss, Anforderung (inkl.
  Offline-Hinweis), Enqueue-Abschluss/Notice, Item-Upsert, Löschen,
  Cancel/Retry/Delete-Guards, Watched-Cleanup-Effekt,
  Resume-Trigger beim Verbinden.
- [x] `DownloadsContractTests` — XAML-/Verdrahtungs-Verträge
  (Seitenstruktur, Aktionen, Dispatches, DI, Effekt-Handler,
  WLAN-Monitor, no-backup-Ablage).
- [x] `ProgressReportServiceTests.FakeNetworkMonitor` um `IsWifi`
  ergänzt; `StubHttpMessageHandler` threadsicher gemacht und um
  einen asynchronen Responder (mit Token) erweitert.

## Abweichungen zum Plan

- **`IsWifi` um Ethernet erweitert** — Review-Korrektur (siehe
  `review-code.md`, Befund 2): die WLAN-only-Einstellung soll
  Mobilfunk schonen; kabelgebundenes Festnetz darf nicht blockiert
  werden.
- **Quota-Prüfung im Transfer periodisch statt nur auf Start-Snapshot**
  — Review-Korrektur (Befund 3): parallele Transfers konnten das
  Kontingent gemeinsam überschreiten; die Engine misst die
  Verzeichnisbelegung nun alle 8 Chunks neu.
- **`StubHttpMessageHandler`** — über den Plan hinaus threadsicher
  gemacht (parallele Transfers dequenen gleichzeitig) und um einen
  asynchronen Responder mit CancellationToken erweitert (nötig für
  den Abbruch-Test — Inhalt-Ströme propagieren kein Request-Token).
