# Umsetzungsplan — Schritt 9: tvOS-Downloads (Hintergrund-Session, Ablauf, Reconcile, Quota)

## 1. Plattformneutral (`VideoPlayer.Mvu`)

### 1.1 Neue Message `AppActivatedMessage` (`AppMessage.cs`)

Record ohne Parameter; Dokumentation: App trat in den Vordergrund/wurde aktiviert — löst den Ablauf-/Abgleichslauf des Download-Manifests aus (neben App-Start, Online-Rückkehr, Seitenaufruf) sowie die Neu-Einplanung verwaister Aufträge.

### 1.2 Neuer Effekt `ReconcileDownloadsEffect` (`AppEffect.cs`)

`public sealed record ReconcileDownloadsEffect(AppSettings Settings) : AppEffect;` — Ablauf-/Abgleichslauf ohne Neu-Laden der Liste: überholte Teildateien entfernen, „completed" ohne Datei → `missing_file`, Waisen erkennen, abgelaufene Downloads entfernen. Ergebnis: `ItemChanged`/`ItemRemoved`-Meldungen.

### 1.3 Update-Handler (`AppUpdate.cs`)

- Dispatch-Zeile: `AppActivatedMessage => UpdateAppActivated(model),`
- `UpdateAppActivated`: verbunden (`ConnectionState.Connected`) → `[new ResumeDownloadsEffect(model.Settings)]` (Handler führt Abgleich + `ResumeInterruptedAsync` im selben Lauf); sonst → `[new ReconcileDownloadsEffect(model.Settings)]` (reiner lokaler Abgleich — verhindert `no_connection`-Fehlschläge durch verfrühte Wiederaufnahme).

## 2. tvOS-Backend (`VideoPlayer.TvOS`)

### 2.1 `Services/TvosBackgroundDownloadBackend.cs` (neu)

Port von `IosBackgroundDownloadBackend` (gleiche `NSUrlSession`-API auf tvOS): `NSObject, IDownloadBackend, INSUrlSessionDownloadDelegate`; `SessionIdentifier = "videoplayer.downloads"`, `Shared`, `CompletionSink`, `HandleEventsForBackgroundUrlSession`, zusätzlich `EnsureSession()` (Session-Instanziierung bei Start/Aktivierung, damit ausstehende Transfers Delegate-Rückrufe erhalten). Kein `#if` nötig (nur `net10.0-tvos`-Kompilierung).

### 2.2 `AppComposition.cs`

- `TvosBackgroundDownloadBackend` erzeugen: `Shared` setzen, `CompletionSink = (key, error, tempPath) => Downloads.CompleteExternalTransferAsync(key, error, tempPath, Settings.Load())`, als `backend` an den `DownloadService`-Ctor übergeben; als Property `DownloadBackend` exponieren (Zugriff aus `AppDelegate`). Kommentar Z. 96–99 aktualisieren.

### 2.3 `AppDelegate.cs`

- `[Export("application:handleEventsForBackgroundURLSession:completionHandler:")]` → `TvosBackgroundDownloadBackend.Shared?.HandleEventsForBackgroundUrlSession(...)` sonst `completionHandler()`.
- `OnActivated`: zusätzlich `AppActivatedMessage` dispatchen + `composition.DownloadBackend.EnsureSession()`.

## 3. tvOS-Effektverdrahtung (`TvosAppEffectHandler.cs`)

Neue Fälle (Muster `ShellAppEffectHandler` Z. 1471–1705, jeweils `services.Downloads`):

- `LoadDownloadsEffect` → `LoadDownloadsAsync`: `ReconcileAsync(settings)` (keine Auto-Download-Kandidaten auf tvOS → `null`) + `LoadItemsAsync` + `GetUsedBytes` → `DownloadsLoadedMessage` (inkl. `OrphanedBytes`, `QuotaGb`); Fehler → `Error`-Text.
- `EnqueueDownloadEffect` → `EnqueueDownloadAsync` (ersetzt Platzhalter): Serienkontext → `DownloadEnqueueRequest` (`Origin: DownloadOrigin.Manual`, `PictureId`) → `DownloadEnqueueCompletedMessage`; Exception → generische Fehler-Notice.
- `CancelDownloadEffect` → `CancelAsync`; `DeleteDownloadEffect` → `DeleteAsync` (Rückmeldung über `ItemRemoved`).
- `DeleteAllDownloadsEffect` → `DeleteAllAsync` + `LoadDownloadsAsync`; `PurgeOrphanedDownloadsEffect` → `PurgeOrphansAsync` + `LoadDownloadsAsync`; `DeleteFailedDownloadsEffect` → `DeleteFailedAsync` + `LoadDownloadsAsync`.
- `ResumeDownloadsEffect` → `ReconcileAsync(settings)` + `ResumeInterruptedAsync(settings)`.
- `ReconcileDownloadsEffect` → `ReconcileAsync(settings)` (Fehler nur protokolliert).
- Klassenkommentar + `default`-Kommentar anpassen (Downloads jetzt verdrahtet; weiterhin nicht verdrahtet: `RunAutoDownloadsEffect`, Playlist-/Inhaltsverwaltung, Protokollversand, PiP).

`ShellAppEffectHandler.cs`: `ReconcileDownloadsEffect`-Fall → `ReconcileWithExpiryPassAsync` (Konsistenz der plattformneutralen Effektmenge).

## 4. tvOS-Download-Liste (`VideoPlayer.TvOS/Ui`)

### 4.1 `DownloadTableCell.cs` (neu)

`UITableViewCell` (Muster `PlaylistTableCell`): Vorschaubild (`GetImageAsync(PictureId)`, 16:9 Still bzw. Poster, Fallback-Rahmen), Titel, Statuszeile (`MediaTypeText` · `StateText` · `SizeText`/`ProgressPercentText` · `ExpiresText` · `ErrorText`), `UIProgressView` für `ShowProgress`. `ReuseId`, `PrepareForReuse`, `Update(item, posters)`.

### 4.2 `DownloadsPageViewController.cs` (neu)

`UIViewController, IModelRenderable` (Muster `PlaylistsPageViewController`):

- Titel „Downloads", Quota-Zeile (`QuotaText` + `OrphanedText`), `UITableView` (Plain) mit `DownloadsSource` (`SetItems`, `RowSelected`).
- `ViewDidAppear` → `DownloadsPageOpenedMessage` (lädt Liste + Abgleich).
- Zeilenauswahl → `UIAlertController` (ActionSheet-Stil) mit den zutreffenden Aktionen des Eintrags: „Abbrechen" (`CanCancel` → `DownloadCancelRequestedMessage`), „Erneut versuchen" (`CanRetry` → `DownloadRetryRequestedMessage`), „Löschen" (`DownloadDeleteRequestedMessage`), „Zurück".
- Kopfaktion „Fehlerhafte löschen" (Button, sichtbar bei `HasFailedItems`) → `DownloadsDeleteFailedRequestedMessage`.
- State-View: Lade-/Leer-/Fehlertext + „Erneut versuchen" (`DownloadsPageOpenedMessage`).
- `Render`: Items-Vergleich → `ReloadData`; Quota-/State-Zeilen aktualisieren.

### 4.3 `MenuTabBarController.cs`

`AppPage.Downloads => new DownloadsPageViewController(dispatch, posters)` statt Platzhalter.

## 5. Tests (`VideoPlayer.Maui.Tests`)

`AppUpdateTests_Downloads.cs` erweitern:

- `AppActivated` verbunden → `ResumeDownloadsEffect` (kein `ReconcileDownloadsEffect`).
- `AppActivated` nicht verbunden (Offline/Connecting) → `ReconcileDownloadsEffect`, kein `ResumeDownloadsEffect`.
- `AppActivated` im Workflow-Zustand (Start) → `ReconcileDownloadsEffect`.

## 6. Dokumentation

- `docs/help/tv-app/beschreibung.md` + `ablauf-anwender.md`: Downloads-Abschnitt (Einplanung, Liste/Aktionen, Ablauf, Quota, Hintergrund-Fortsetzung, Reconcile, fehlende Dateien).
- `README.md`: tvOS-Featureliste ergänzen (falls Downloads dort gelistet ist — prüfen).
- `changes.log`: Eintrag „tvOS-App: Downloads (Schritt 9)".

## 7. Verifikation

- `dotnet test VideoPlayer.Maui.Tests` (Windows) grün.
- Push → CI `build-tvos` grün (Run-ID berichten); bei Fail `gh run view --job <id> --log-failed` → fixen → erneut pushen.

## Offene Punkte

Keine — alle Entscheidungen durch Stakeholder-Vorgaben abgedeckt.
