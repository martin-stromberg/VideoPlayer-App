# Bestandsaufnahme — Schritt 9: tvOS-Downloads

## Vorhandene Bausteine (plattformneutral, `VideoPlayer.Mvu`)

| Baustein | Ort | Stand |
|----------|-----|-------|
| `IDownloadBackend` + `DownloadBackendResult` | `VideoPlayer.Mvu/IDownloadBackend.cs` | fertig — `TransferAsync(url, token, targetPath, wifiOnly, progress, ct)` |
| `HttpDownloadBackend` | `VideoPlayer.Mvu/HttpDownloadBackend.cs` | Fallback/Tests |
| `DownloadService` | `VideoPlayer.Mvu/DownloadService.cs` | Manifest, Queue (`MaxParallelDownloads=2`), Quota (`quota_exceeded`, `NoticeQuotaExceeded`), `EnqueueAsync` (Dedup/Notices, `DownloadOrigin`), `CancelAsync`, `DeleteAsync`, `DeleteAllAsync`, `DeleteFailedAsync`, `PurgeOrphansAsync`, `ResumeInterruptedAsync` (verwaiste `downloading`/`queued`/nicht-Quota-Fehler → requeue), `ReconcileAsync` (Teildateien, Waisen, `missing_file`-Korrektur, Ablauf-Pass mit `autoDownloadCandidateKeys`-Verlängerung), `CompleteExternalTransferAsync` (Completion-Sink der Hintergrund-Session), `ExpireAfterAsync`/`ExtendExpiryAsync`, `ItemChanged`/`ItemRemoved`-Events; Backend als optionaler Ctor-Parameter |
| `DownloadListItem` | `VideoPlayer.Mvu/DownloadListItem.cs` | anzeigefertig: `StateText`, `ErrorText` (inkl. `missing_file`-Text), `Progress`, `ProgressPercentText`, `SizeText`, `ExpiresText`, `CanCancel`/`CanRetry`/`CanDelete`, `ShowProgress` |
| `DownloadsPageState` | `VideoPlayer.Mvu/DownloadsPageState.cs` | `Items`, `UsedBytes`, `QuotaGb`, `OrphanedBytes`, `HasFailedItems`, `QuotaText`, `OrphanedText`, Lade-/Fehler-/Leerzustand |
| Download-Messages | `VideoPlayer.Mvu/AppMessage.cs` (Z. 1046–1159) | `DownloadsPageOpenedMessage`, `DownloadsLoadedMessage`, `DownloadRequestedMessage` (inkl. `ShowId`/`ShowTitle`/`SeasonNumber`/`EpisodeNumber`/`PictureId`), `DownloadEnqueueCompletedMessage`, `DownloadItemChangedMessage`, `DownloadDeletedMessage`, `DownloadCancelRequestedMessage`, `DownloadRetryRequestedMessage`, `DownloadDeleteRequestedMessage`, `DownloadsDeleteAllRequestedMessage`, `DownloadsPurgeOrphansRequestedMessage`, `DownloadsDeleteFailedRequestedMessage` |
| Download-Effekte | `VideoPlayer.Mvu/AppEffect.cs` (Z. 666–798) | `LoadDownloadsEffect`, `EnqueueDownloadEffect` (inkl. Serienkontext-Properties), `CancelDownloadEffect`, `DeleteDownloadEffect`, `DeleteAllDownloadsEffect`, `PurgeOrphanedDownloadsEffect`, `DeleteFailedDownloadsEffect`, `ResumeDownloadsEffect`, `RunAutoDownloadsEffect` (auf tvOS nicht verdrahtet), `ExpireWatchedDownloadEffect`, `ProtectDownloadExpiryEffect` |
| Update-Handler | `VideoPlayer.Mvu/AppUpdate.cs` (Z. 4199–4398, Trigger Z. 1059–1136) | alle Download-Messages fertig; `ResumeDownloadsEffect` wird bei Verbindungsaufbau (Z. 1062) und Online-Rückkehr (Z. 1135) ausgelöst; `LoadDownloadsEffect` bei `DownloadsPageOpenedMessage` |

## iOS-Vorlage (`VideoPlayer.Maui`)

- `Platforms/iOS/IosBackgroundDownloadBackend.cs` (`#if IOS`): `SessionIdentifier = "videoplayer.downloads"`, `Shared`-Singleton, `CompletionSink` (`Func<string,string?,string?,Task>`), `HandleEventsForBackgroundUrlSession`, `INSUrlSessionDownloadDelegate`-Exports, `TaskDescription = Key(url, targetPath)` (`{dateiname-ohne-endung}|{urlhash}`), `pending`-Transfers per `TaskIdentifier`, Temp→Ziel per `File.Copy`, `DidFinishEventsForBackgroundSession` → `systemCompletionHandlers`
- `Platforms/iOS/AppDelegate.cs`: `[Export("application:handleEventsForBackgroundURLSession:completionHandler:")]` → `Shared.HandleEventsForBackgroundUrlSession` (Fallback `completionHandler()`)
- `MauiProgram.cs` Z. 150–164: Backend-Singleton mit `CompletionSink = key,error,tempPath → DownloadService.CompleteExternalTransferAsync(key, error, tempPath, settings.Load())`
- `Services/ShellAppEffectHandler.cs` Z. 1471–1705: `LoadDownloadsAsync` (Reconcile-mit-Ablauf + Liste + `DownloadsLoadedMessage`), `EnqueueDownloadAsync` (Serienkontext → `DownloadEnqueueRequest` mit `DownloadOrigin.Manual`), `CancelDownloadAsync`, `DeleteDownloadAsync`, `DeleteAllDownloadsAsync`, `PurgeOrphanedDownloadsAsync`, `DeleteFailedDownloadsAsync`, `ResumeDownloadsAsync` (`ReconcileWithExpiryPassAsync` + `ResumeInterruptedAsync`), `ExpireWatchedDownloadAsync`, `ProtectDownloadExpiryAsync`, `OnDownloadItemChanged`/`OnDownloadItemRemoved` → MVU-Messages

## tvOS-Bestand (`VideoPlayer.TvOS`)

- `AppComposition.cs` Z. 96–102: `Downloads = new DownloadService(...)` **ohne Backend** (Kommentar verweist auf diesen Schritt)
- `AppDelegate.cs`: `FinishedLaunching` → `StartupRequestedMessage`; `OnActivated` → `HomeListsReloadRequestedMessage`; **kein** `handleEventsForBackgroundURLSession`
- `Services/TvosAppEffectHandler.cs` Z. 199–211: `EnqueueDownloadEffect` → Platzhalter-Notice „folgt in einem späteren Entwicklungsschritt"; alle übrigen Download-Effekte → `default`-No-Op; `ItemChanged`/`ItemRemoved`-Events bereits an `DownloadItemChangedMessage`/`DownloadDeletedMessage` verdrahtet (Z. 82–83); `DispatchMessageAsync` über `MainThreadInvoker` + `services.Host`
- `Ui/MenuTabBarController.cs` Z. 97–106: `AppPage.Downloads` → `PagePlaceholderViewController`
- `Ui/MediaDetailsViewController.cs` Z. 612–645: „Herunterladen"-Button dispatcht `DownloadRequestedMessage` **mit Serienkontext** — UI-Seite fertig; Notice-Anzeige über `DownloadNotice`/`DownloadNoticeIsError` (Z. 471)
- `Ui/PlaylistsPageViewController.cs` + `PlaylistTableCell.cs`: Muster für `UITableView`-Menüseite (Source, `RowSelected`, `RemembersLastFocusedIndexPath`, State-View mit Retry)
- `TvosPosterImageService.GetImageAsync(pictureId)`: Bild-Abruf für `DownloadListItem.PictureId`
- `PlaybackSourceService` (Schritt 7): bevorzugt lokale Download-Datei — Zusammenspiel gegeben

## Lücken / zu schaffen

1. `TvosBackgroundDownloadBackend` (Port des iOS-Backends; tvOS-Projekt kompiliert nur unter `net10.0-tvos`, kein `#if` nötig — degradierte Hülle hat `EnableDefaultCompileItems=false`)
2. `AppDelegate`: `handleEventsForBackgroundURLSession`-Export + Session-Sicherstellung bei Aktivierung + Dispatch der neuen Aktivierungs-Message
3. Neue plattformneutrale `AppActivatedMessage` + `ReconcileDownloadsEffect` (Ablauf-/Abgleichslauf) + Update-Handler (verbunden → `ResumeDownloadsEffect` [enthält Abgleich]; nicht verbunden → `ReconcileDownloadsEffect`, damit kein `no_connection`-Fehlschlag entsteht)
4. `TvosAppEffectHandler`: reale Verdrahtung aller Download-Effekte (analog Shell-Handler, ohne Auto-Download-Kandidaten → leere Menge)
5. `DownloadsPageViewController` (schlank) + `DownloadTableCell` + Menüverdrahtung
6. `AppComposition`: Backend erzeugen/verdrahten (`Shared`, `CompletionSink`), in `DownloadService` injizieren
7. `ShellAppEffectHandler`: `ReconcileDownloadsEffect`-Fall ergänzen (Konsistenz — Effekt ist plattformneutral)
8. Tests: `AppActivatedMessage` → Effekte je Verbindungszustand (`VideoPlayer.Maui.Tests`)

## Rahmen

- tvOS lokal nicht kompilierbar → CI `build-tvos` (Push auf `task/issue-86-**` autorisiert)
- Bekannte tvOS-Fallen: Basistyp-Member, Named-Args-Case, `IReadOnlyList.IndexOf`, `const nfloat`, `RegisterClassForCell`, `SeparatorStyle` (existiert nicht), `UIAction`-Parameter nicht `_` nennen
