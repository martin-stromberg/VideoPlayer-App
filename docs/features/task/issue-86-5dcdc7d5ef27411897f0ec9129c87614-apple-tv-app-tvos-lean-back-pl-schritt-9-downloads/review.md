# Plan-Review — Schritt 9: tvOS-Downloads

Status: **Vollständig umgesetzt**

Abweichung: Selbst-Review durch den Hauptagenten (keine Unteragenten verfügbar).

## Abgleich Plan ↔ Umsetzung

| Planelement | Umsetzung | Status |
|-------------|-----------|--------|
| 1.1 `AppActivatedMessage` | `VideoPlayer.Mvu/AppMessage.cs` Z. 220–228 | ✅ |
| 1.2 `ReconcileDownloadsEffect` | `VideoPlayer.Mvu/AppEffect.cs` Z. 765–777 | ✅ |
| 1.3 Update-Handler | `VideoPlayer.Mvu/AppUpdate.cs` Z. 217 (Dispatch), Z. 4202–4218 (`UpdateAppActivated`) | ✅ |
| 2.1 `TvosBackgroundDownloadBackend` | `VideoPlayer.TvOS/Services/TvosBackgroundDownloadBackend.cs` (Port iOS-Vorlage + `EnsureSession()`) | ✅ |
| 2.2 `AppComposition` | Backend erzeugt, `Shared`, `CompletionSink` → `CompleteExternalTransferAsync`, als Ctor-Arg an `DownloadService` (Z. 96–110, Property Z. 204–205) | ✅ |
| 2.3 `AppDelegate` | `override HandleEventsForBackgroundUrlSession` (Basistyp-Export), `OnActivated` → `EnsureSession()` + `AppActivatedMessage` | ✅ |
| 3. Effektverdrahtung | `TvosAppEffectHandler`: alle manuellen Download-Effekte + `ReconcileDownloadsEffect`; `ShellAppEffectHandler`: `ReconcileDownloadsEffect`-Fall | ✅ |
| 4.1 `DownloadTableCell` | `VideoPlayer.TvOS/Ui/DownloadTableCell.cs` | ✅ |
| 4.2 `DownloadsPageViewController` | `VideoPlayer.TvOS/Ui/DownloadsPageViewController.cs` (Tabelle, Quota-Zeile, „Fehlerhafte löschen", Aktionsmenü je Eintrag, State-View) | ✅ |
| 4.3 Menüverdrahtung | `MenuTabBarController` Z. 101 | ✅ |
| 5. Tests | `AppUpdateTests_Downloads.cs` (+4 `AppActivated`-Tests), `TvOsAppContractTests.cs` (angepasst + 2 neue) | ✅ |

## Abgleich Akzeptanzkriterien ↔ Umsetzung

- Einplanen von Detailseite, Fortschritt, Abbrechen/Wiederholen/Löschen → `EnqueueDownloadAsync`, `DownloadRequestedMessage` (bestand), Zellen-/Aktions-UI ✅
- Suspensionstaugliche Fortsetzung + Completion-Sink/Neu-Einplanung → `TvosBackgroundDownloadBackend`, `ResumeInterruptedAsync` via `ResumeDownloadsEffect`/`AppActivatedMessage`, `EnsureSession` bei Aktivierung ✅
- Quota-Meldung vor Einplanung → `NoticeQuotaExceeded` aus `DownloadService.EnqueueAsync` → `DownloadNotice` auf Detailseite ✅
- Ablauf-Lauf bei Start/Online-Rückkehr/Seitenaufruf/Aktivierung → Start+Online-Rückkehr bestand; Seitenaufruf via `LoadDownloadsAsync`-Reconcile; Aktivierung via `AppActivatedMessage` ✅
- `missing_file`-Korrektur → `ReconcileAsync` (bestehender Dienst) über alle Trigger ✅
- Plattformneutrale Tests der Aktivierungs-Message → 4 neue Tests ✅
- Lokale Downloads als bevorzugte Wiedergabequelle → `PlaybackSourceService` (Schritt 7, unverändert) ✅

## Offene Aufgaben

Keine.
