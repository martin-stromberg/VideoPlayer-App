# Umsetzungsplan: Download-Verwaltung und automatische Downloads (Entwicklungsschritt 11)

## Übersicht

Die Neu-App erhält eine vollwertige Download-Verwaltung im MVU-Muster der
Vorgängerschritte: ein plattformneutraler `DownloadService` im
`VideoPlayer.Mvu`-Projekt führt Downloads als persistente Aufträge aus
(Manifest `download_manifest` in der `CacheDatabase`, Dateien unter
`AppStorageLayout.VideosDirectory`), meldet Fortschritt/Status über ein
Ereignis in den MVU-Zyklus und unterstützt parallele Transfers
(Semaphore, Grad 2), Abbrechen, Löschen, Wiederholen sowie die
automatische Wiederaufnahme nach App-Neustart oder Netzwerkunterbrechung.
Ein `AutoDownloadService` lädt — bei aktivierter Einstellung — die
nächsten Episoden der Weiterschauen-Einträge vor (Serienkontext über
`GetTvShowAsync`), optional nur im WLAN. Das GB-Kontingent
(`DownloadQuotaGb`, Default 5) wird vor und während des Downloads
durchgesetzt; gesehene Episoden werden optional automatisch gelöscht.
Die `DownloadsPage` wird zur echten Verwaltungsseite; die Detailseite
erhält „Herunterladen"-Aktionen. Lokale Wiedergabe ist bereits über
`PlaybackSourceService` verdrahtet (liest `completed`-Manifesteinträge)
— sie funktioniert automatisch, sobald Downloads landen.

**Dokumentierte Einschränkung (Befund aus der Bestandsaufnahme):** Der
Server-Stream `api/items/{typ}/{id}/stream` ist ein einfacher
GET-Download ohne Range-Support (der Altbestand nutzt keine
Range-Requests; `Accept-Ranges` ist serverseitig nicht nachgewiesen).
„Wiederaufnahme abgebrochener Downloads" bedeutet daher: der Auftrag
bleibt im Manifest und wird automatisch **neu gestartet** (erneuter
Download ab Byte 0); eine fortgesetzte Übertragung (Range-Resume) ist
nicht möglich.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Download-Kern | `DownloadService` (Singleton, `VideoPlayer.Mvu`, plattformneutral): Manifest-CRUD, Einreihen/Dedup, parallele Transfers, Fortschritt, Abbrechen, Löschen, Quota, Resume | Service-Layer-Konvention der bisherigen Schritte; testbar über injizierbare `HttpClient`-Fabrik und reale SQLite-/Temp-Verzeichnisse |
| Parallelität | `SemaphoreSlim`-Begrenzung `MaxParallelDownloads = 2` + Engine-Pump (nach Abschluss/Fehler/Freigabe wird der nächste wartende Manifest-Eintrag gestartet) | Akzeptanzkriterium „parallele Downloads"; 2 ist ein konservativer mobiler Wert |
| Auftrags-Persistenz | Jeder Auftrag ist eine Zeile in `download_manifest` (`State`: `queued`/`downloading`/`completed`/`failed`/`cancelled`, plus `Title`, `DownloadedBytes`, `TotalBytes`, `Error`-Code) | Überlebt App-Neustarts — Grundlage der automatischen Wiederaufnahme und der Listen-Anzeige |
| Manifest-Schema | `DownloadManifestEntry` um `Title`, `DownloadedBytes`, `TotalBytes`, `Error` erweitern; `CacheDatabase.InitializeAsync` legt neue Spalten über `CreateTableAsync` an (sqlite-net migriert fehlende Spalten per `ALTER TABLE`) | Minimaler Eingriff, rückwärtskompatibel |
| Status-Modell | `enum DownloadState { Queued, Downloading, Completed, Failed, Cancelled }` (MVU), Persistenz als kleingeschriebener String in `State` | `Cancelled` trennt Benutzerabbruch von Fehlern — Abgebrochene werden nicht automatisch wiederaufgenommen |
| Wiederaufnahme | `ResumeDownloadsEffect` beim Übergang in `Connected` (Deckt App-Neustart + Offline→Online) sowie bei jeder Online-Meldung im verbundenen Zustand (deckt WLAN↔Mobilfunk-Wechsel); setzt `downloading`→`queued` zurück und nimmt `queued` + nicht-Quota-`failed` wieder auf | „abgebrochene Downloads werden automatisch fortgesetzt, auch über App-Neustarts" |
| Quota | Belegter Speicher = Summe der Dateigrößen im `VideosDirectory`; Prüfung beim Einreihen (Ablehnung mit Hinweis), beim Transfer-Start (Eintrag → `failed`/`quota_exceeded`) und laufend pro Schreibblock (Abbruch + Teildatei-Löschung + `failed`/`quota_exceeded`) | Akzeptanzkriterium „Kontingent wird eingehalten" — auch bei parallelen Downloads |
| WLAN-only | `INetworkStatusMonitor.IsWifi` (neu; App-Rand via `Connectivity.Current.ConnectionProfiles` ⊇ `WiFi`); die Engine startet Transfers nur bei erfüllter Bedingung — betrifft manuelle, automatische und wiederaufgenommene Downloads (Einstellung heißt „Downloads nur im WLAN") | Einheitliche, vorhersehbare Regel; wartende Aufträge zeigen „Wartend" und starten bei WLAN-Rückkehr |
| Auto-Downloads | `AutoDownloadService.RunAsync(settings, force)` — Trigger: `SynchronizationCompletedMessage`, `CachedListsChangedMessage` (nach Teil-Syncs) und `SaveSettingsRequestedMessage` (Schalter aktiviert), jeweils nur wenn `Connected` + `AutoDownloadEnabled`; Throttle 60 s | Angelehnt an `WatchlistDownloadCoordinatorService` des Altbestands (ereignis- + startgetrieben) |
| Auto-Download-Auswahl | Pro Weiterschauen-Episoden-Eintrag die nächsten `PreloadEpisodeCount` Episoden aus dem Serienkontext (`GetTvShowAsync`, staffelübergreifend nach Staffel-/Episodennummer — Muster `NextEpisodeQueue`/`MediaDetailsService`); Film-Einträge laden den Film selbst; Dedup über das Manifest | „nächste Episoden der Continue-Watching-Einträge (Serienkontext-Mechanik aus Schritt 8/10)" |
| Auto-Löschung | Wenn `IsWatched` neu gesetzt wird (`UpdatePlaybackProgress`/`UpdatePlaybackEnded`) und Typ `tvshowepisode` und `AutoDeleteWatchedEpisodes` → `DeleteWatchedDownloadEffect` (löscht nur `completed`-Downloads) | Ankopplung an das „angesehen"-Ereignis aus Schritt 10 |
| Fortschritt → UI | `DownloadService.ItemChanged`-Event (`DownloadListItem`-Snapshot) → `ShellAppEffectHandler` dispatcht `DownloadItemChangedMessage`; Entfernen via `ItemRemoved` → `DownloadDeletedMessage`; Progress-Throttle ~250 ms | Muster `ServerEventsService`/`OnServerEventReceived`; reine Update-Logik bleibt testbar |
| Downloads-Seite | `DownloadsPageState` + `DownloadListItem` (Titel, `StateText`, `Progress`, `ProgressPercentText`, `CanCancel`/`CanRetry`/`CanDelete`, `ErrorText`); `CollectionView` im Stil der `FavoritesPage`; Quota-Zeile „x von y GB belegt"; Aktionen Abbrechen/Erneut versuchen/Löschen | „Liste mit Titel, Status, Fortschritt, Aktionen — an der Webanwendung orientiert" |
| Download-Auslöser | „Herunterladen"-Button im Kopfbereich der `MediaDetailsPage` (bei `IsPlayable`) und ⤓-Button je Episoden-Eintrag → `DownloadRequestedMessage`; Ergebnis-Hinweis als `MediaDetailsState.DownloadNotice` | Minimaler, konsistenter Einstiegspunkt |
| Stream-URL/Auth | Wie `PlaybackSourceService`: `{Base}/api/items/{typ}/{id}/stream?access_token=…`; zusätzlich `Authorization: Bearer`-Header auf dem Request (Altbestand-Muster) | Einheitliche Quelle; funktioniert mit/ohne Header-Auswertung serverseitig |
| Fehlerbehandlung | HTTP-/Transport-/IO-Fehler → `failed` + maschinenlesbarer `Error`-Code (`http_4xx/5xx`, `network`, `io`, `quota_exceeded`); Anzeige als verständlicher deutscher Statustext; `Erneut versuchen` reiht erneut ein | „definierte Fehlerbehandlung" |

## Programmabläufe

### Download anfordern (Detailseite)

1. `MediaDetailsPage`: „Herunterladen" (Kopfbereich, `IsPlayable`) bzw. ⤓
   je Episoden-Eintrag dispatcht
   `DownloadRequestedMessage(mediaType, mediaId, title)`.
2. `AppUpdate`: Guard (`MediaId <= 0` oder Typ nicht `movie`/
   `tvshowepisode` → unverändert; nicht `Connected` →
   `MediaDetails.DownloadNotice` = „Keine Serververbindung …", IsError).
   Sonst `EnqueueDownloadEffect(type, id, title, model.Settings)`.
3. `ShellAppEffectHandler` → `DownloadService.EnqueueAsync`: Dedup gegen
   Manifest (`completed`+Datei → Hinweis „bereits vorhanden";
   `queued`/`downloading` aktiv → „läuft bereits"; `failed`/`cancelled`
   → Rücksetzen auf `queued`), Quota-Vorprüfung (`used >= quota` →
   Eintrag `failed`/`quota_exceeded` + Hinweis), sonst Insert/Update +
   Pump-Start. Ergebnis als `DownloadEnqueueCompletedMessage` → Update
   setzt `DownloadNotice` (bei passender Detail-Identität) und upsertet
   das Item in `Downloads.Items`.
4. Transfer-Task (parallel, Semaphore): `queued`→`downloading`
   (Manifest + ItemChanged), GET Stream, schreibt in
   `VideosDirectory/{typ}_{id}.mp4` (FileMode.Create — kein Range),
   Fortschritt-Throttle → `DownloadItemChangedMessage`; Abschluss →
   `completed` (+`SizeBytes`, `DownloadedBytes=TotalBytes`); danach
   Pump für den nächsten wartenden Auftrag.

Beteiligte: `MediaDetailsPage`, `AppUpdate`, `AppEffect`, `AppMessage`,
`ShellAppEffectHandler`, `DownloadService`, `CacheDatabase`,
`AppStorageLayout`, `INetworkStatusMonitor`.

### Abbrechen / Erneut versuchen / Löschen (Downloads-Seite)

1. `DownloadsPage` dispatcht `DownloadCancelRequestedMessage(item)` /
   `DownloadRetryRequestedMessage(item)` /
   `DownloadDeleteRequestedMessage(item)` (Guards `CanCancel`/`CanRetry`/
   `CanDelete` im Update).
2. Effekte → `DownloadService.CancelAsync` (CTS-Abbruch oder
   `queued`→`cancelled`, Teildatei löschen, Pump weiter) /
   `EnqueueAsync` (Retry-Pfad) / `DeleteAsync` (bricht ggf. ab, löscht
   Datei + Manifest-Zeile → `DownloadDeletedMessage`).

### Automatische Wiederaufnahme

1. `EnterConnected` (Login/Auto-Login/Wiederverbindung) → zusätzlich
   `ResumeDownloadsEffect(settings)`; `NetworkAvailabilityChangedMessage`
   (online, bereits `Connected`) → `ResumeDownloadsEffect` (WLAN-Rückkehr).
2. `DownloadService.ResumeInterruptedAsync`: Manifest-Einträge
   `downloading`→`queued` normalisieren; `queued` sowie `failed` mit
   `Error != "quota_exceeded"` wieder in die Engine geben
   (WLAN-only/Quota gelten beim Start).

### Automatische Downloads (Weiterschauen-Vorlauf)

1. Trigger (s. o.) → `RunAutoDownloadsEffect(settings, force)` →
   `AutoDownloadService.RunAsync`: Guards `AutoDownloadEnabled`,
   WLAN-Bedingung, angemeldeter Client, Throttle (60 s, `force` beim
   Einschalten/Neustart-Trigger umgeht ihn).
2. Weiterschauen-Stand aus `cached_lists[continue_watching]` (über
   `HomeListService.LoadHomeListsAsync`, kein Extra-Serveraufruf);
   je Episoden-Eintrag `GetTvShowAsync(ShowId)` → geordnete Folge-
   Episoden (bis `PreloadEpisodeCount` je Eintrag) →
   `DownloadService.EnqueueAsync`; Film-Einträge → Film selbst.
3. Fehler je Serie/Eintrag werden protokolliert und übersprungen — kein
   Gesamtfehlschlag.

### Auto-Löschung gesehener Episoden

1. `UpdatePlaybackProgress`/`UpdatePlaybackEnded` setzen `IsWatched` neu
   → bei `MediaType == "tvshowepisode"` &&
   `Settings.AutoDeleteWatchedEpisodes` zusätzlich
   `DeleteWatchedDownloadEffect(type, id)`.
2. Handler → `DownloadService.DeleteIfCompletedAsync` → Datei + Manifest
   entfernen → `DownloadDeletedMessage`.

### Downloads-Seite öffnen

1. `DownloadsPage.OnAppearing` → `DownloadsPageOpenedMessage` →
   `LoadDownloadsEffect(settings)` → `LoadItemsAsync` +
   `GetUsedBytesAsync` → `DownloadsLoadedMessage(items, used, quotaGb)`;
   Fortschritte laufen weiter über `DownloadItemChangedMessage`.

## Betroffene Dateien

**Neu (`VideoPlayer.Mvu`):** `DownloadState.cs`, `DownloadListItem.cs`,
`DownloadsPageState.cs`, `DownloadService.cs`, `AutoDownloadService.cs`,
`DownloadItemEventArgs` (im DownloadService-File), ggf.
`DownloadEnqueueResult`-Record.

**Geändert (`VideoPlayer.Mvu`):** `AppModel` (`Downloads`-State),
`MediaDetailsState` (`DownloadNotice`, `DownloadNoticeIsError`),
`AppMessage`, `AppEffect`, `AppUpdate` (neue Handler + Trigger),
`INetworkStatusMonitor` (`IsWifi`).

**Geändert (`VideoPlayer.Storage`):**
`Entities/DownloadManifestEntry` (neue Spalten).

**Geändert (`VideoPlayer.Maui`):** `Pages/DownloadsPage.xaml(.cs)`,
`Pages/MediaDetailsPage.xaml(.cs)`,
`Services/ConnectivityNetworkStatusMonitor.cs` (`IsWifi`),
`Services/ShellAppEffectHandler.cs` (neue Effekte + Event-Subscriptions),
`MauiProgram.cs` (DI).

**Tests (`VideoWebPlayer.Maui.Tests`):**
`AppUpdateTests_Downloads.cs` (neu), `DownloadServiceTests.cs` (neu),
`AutoDownloadServiceTests.cs` (neu), `DownloadsContractTests.cs` (neu),
`EnumCoverageTests.cs` (`DownloadState`), `ProgressReportServiceTests.cs`
(`FakeNetworkMonitor.IsWifi` ergänzen).

## Testnachweise der Benutzerflüsse (verbindlich)

UI-E2E-Automatisierung existiert im Repository nicht (dokumentierte
Einschränkung aller Schritte); die Benutzerflüsse werden daher —
Projektkonvention — über MVU-Übergangstests + XAML-Contract-Tests
abgesichert:

1. **Manueller Download-Fluss**: `DownloadRequestedMessage` → Effekt →
   `DownloadEnqueueCompletedMessage` → Item in `Downloads.Items` +
   `DownloadNotice`; Contract-Test: Button in `MediaDetailsPage.xaml`,
   Dispatch im Code-Behind.
2. **Parallelität**: zwei `EnqueueAsync` blockieren sich nicht —
   Service-Test mit verzögertem Stub-Handler belegt zwei gleichzeitig
   laufende Transfers (`MaxParallelDownloads` > 1).
3. **Wiederaufnahme**: Manifest-Eintrag `downloading` + neuer
   `DownloadService` über derselben DB → `ResumeInterruptedAsync` startet
   den Download erneut → `completed`; MVU-Test: `EnterConnected` erzeugt
   `ResumeDownloadsEffect`.
4. **Quota**: Service-Test (winzige Quota → Ablehnung/`quota_exceeded`;
   Mid-Write-Überschreitung → `failed` + Teildatei weg); MVU-Test:
   Notice bei Ablehnung.
5. **Auto-Downloads**: `AutoDownloadService`-Test (nächste Episoden,
   Anzahl-Limit, WLAN-only-Blockade, deaktivierter Schalter); MVU-Tests
   der Trigger (Sync-Abschluss/Settings/Cache-Änderung nur bei
   `Connected` + Schalter).
6. **Auto-Löschung**: `UpdatePlaybackProgress`/`Ended` erzeugen
   `DeleteWatchedDownloadEffect` nur bei Episode + Schalter +
   neu gesetztem `IsWatched`.
7. **Downloads-Seite**: `DownloadsPageOpened` → `LoadDownloadsEffect` →
   Liste; `ItemChanged` upsertet Fortschritt; Cancel/Retry/Delete-
   Übergänge; Contract-Test: `CollectionView`, Buttons,
   `DownloadsPageOpenedMessage`-Dispatch.

## Offene Punkte

Keine.
