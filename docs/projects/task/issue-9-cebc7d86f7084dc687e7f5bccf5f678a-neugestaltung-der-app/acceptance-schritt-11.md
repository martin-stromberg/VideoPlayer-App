# Abnahmeprüfung Entwicklungsschritt 11 – Download-Verwaltung und automatische Downloads

Datum: 2025 (Schritt-Branch `task/issue-9-…-schritt-11-downloads`, Implementierungscommit `f995573`)
Prüfung: unabhängige fachliche Abnahme gegen die Anforderung (nicht durch den Implementierungsagenten).

## Akzeptanzkriterien

| Kriterium | Nachweis | Ergebnis |
|---|---|---|
| Mehrere Downloads laufen parallel | `VideoPlayer.Mvu/DownloadService.cs` — `MaxParallelDownloads = 2`, `SemaphoreSlim`-Pump (`PumpQueue`), `ConcurrentDictionary runningTransfers`; Tests `DownloadServiceTests` | Erfüllt |
| Abgebrochene Downloads werden automatisch fortgesetzt (auch über Neustarts) | `DownloadService.ResumeInterruptedAsync` — reiht `queued`, verwaiste `downloading` und nicht-Quota-`failed`-Einträge aus dem persistenten Manifest erneut ein; aufgerufen aus `ShellAppEffectHandler` (Start/Sync). Server-Stream ohne Range-Support → Wiederaufnahme ab Byte 0, dokumentiert in der Typsummary. | Erfüllt |
| Speicherplatzkontingent wird eingehalten (GB, Default 5 GB) | `AppSettings.DownloadQuotaGb` (1–500, Default 5); `DownloadService.GetUsedBytes` + Vorab-Prüfung in `EnqueueAsync` (`ErrorQuotaExceeded`) + Quota-Scan während des Transfers (`QuotaScanIntervalChunks`); Quota-Anzeige `QuotaLabel` auf der Downloads-Seite | Erfüllt |
| Automatische Downloads respektieren Anzahl- und WLAN-only-Einstellungen | `AutoDownloadService.RunAsync` — Guards auf `AutoDownloadEnabled`, `WifiOnlyDownloads && !IsWifi`, `PreloadEpisodeCount` via `Take(...)`; Weiterschauen-Episoden über Serienkontext staffelübergreifend, Filme direkt; Throttle `MinRunInterval` | Erfüllt |
| Auto-Löschung gesehener Episoden bei Aktivierung | `AppSettings.AutoDeleteWatchedEpisodes` (Settings-Switch `AutoDeleteWatchedSwitch`); `AppUpdate` emittiert `DeleteWatchedDownloadEffect` nur bei `tvshowepisode` + IsWatched-Übergang; Handler → `DeleteIfCompletedAsync` (nur `completed`) | Erfüllt |
| Downloads-Seite zeigt Status und Fortschritt aller Downloads | `DownloadsPage.xaml` — CollectionView aller Manifest-Einträge mit Titel, Typ, Zustand, Fortschritt (Prozent + ProgressBar), Fehlertext, Aktionen Abbrechen/Erneut/Löschen, Quota-Label, Offline-Banner | Erfüllt |
| Download-Manifest nicht backup-pflichtig, Bestand rekonstruierbar | `download_manifest`-Tabelle (`VideoPlayer.Storage/Entities/DownloadManifestEntry.cs`) im Cache-/Nicht-Backup-Bereich; Dateien unter `AppStorageLayout.VideosDirectory` (Nicht-Backup) | Erfüllt |
| Heruntergeladene Videos abspielbar | `PlaybackSourceService` aus Schritt 10 bevorzugt `completed`-Manifest-Einträge mit vorhandener Datei vor dem Server-Stream | Erfüllt |
| Definierte Fehlerbehandlung | Fehlercodes `quota_exceeded`, `no_connection`, `network`, `io`, `http_<code>`; Fehler im Eintrag persistiert, Retry-Aktion auf der Seite; Quota-Fehler werden nicht automatisch wiederholt | Erfüllt |

## Prüfung

- Code-Stichproben in `DownloadService.cs`, `AutoDownloadService.cs`, `AppUpdate.cs` (Auto-Lösch-Verdrahtung), `ShellAppEffectHandler.cs` (Effekt-Handling), `DownloadsPage.xaml`, `SettingsPage` (Schalter).
- Abnahme-Testlauf: `dotnet test VideoWebPlayer.Maui.Tests` → **580 bestanden, 0 fehlgeschlagen** (Basis 510 + 70 neue Tests: `DownloadServiceTests`, `AutoDownloadServiceTests`, `AppUpdateTests_Downloads`, `DownloadsContractTests`).
- Windows-MAUI-Build laut Lifecycle-Nachweis: 0 Warnungen, 0 Fehler.
- Pre-Commit-Hooks (XML-Doku, Stub-Check, Enum-Abdeckung, Razor/Translation) liefen bei allen Commits des Schritts fehlerfrei.

## Ergebnis

**Abgenommen.** Alle Akzeptanzkriterien erfüllt; Schritt 11 kann in den Basisbranch gemergt werden.
