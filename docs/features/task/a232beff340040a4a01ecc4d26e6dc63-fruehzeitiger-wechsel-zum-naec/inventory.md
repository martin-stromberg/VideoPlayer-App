# Bestandsaufnahme: Auto-Fortsetzung nur am tatsächlichen Medienende — konfigurierbarer Vorlauf entfernen

Bestandsaufnahme des Wiedergabe-/Fortsetzungsbereichs (MVU-Kern, Einstellungsmodell und -persistenz, Settings-UI, Player-Rand) bezogen auf die Anforderung in `requirement.md`, ergänzt um den Befund des Vorfallprotokolls `videoplayer.log` (2026-10-19 ~21:03).

## Zusammenfassung

- **Zwei unabhängige Auto-Advance-Auslöser existieren heute:** der positionsgetriebene Vorlauf in `AppUpdate.EvaluateEndWindow` (Playlist-Zweig Z. 3924–3937, Serienzweig Z. 3939–3951, jeweils `remaining <= AutoResumeRemainingSeconds` + `EndWindowStreak >= 2`) und der Ende-Pfad `AppUpdate.UpdatePlaybackEnded` (Z. 3981–4045) mit Plausibilitätsgate `IsPlaybackEndPlausible`/`PlaybackEndedToleranceSeconds` (300 s, interne Konstante).
- **Vorfall im Protokoll belegt:** `videoplayer.log` Z. 5381–5386 (Start 21614 um 20:48:26 über `ServerStream`, Fortsetzung 00:04:49) und Z. 5577–5579 (21:03:40 Download von 21614 entfernt, Quelle für 21615 aufgelöst, Wiedergabe gestartet). Zwischen beiden Zeitpunkten liegt **kein** `Wiedergabe beendet`-Eintrag und kein `MediaFailed` — der Wechsel erfolgte allein über das Restlaufzeitfenster (~4–5 min vor dem tatsächlichen Ende, Fenster 300 s). Zwei `AVPlayerItemPlaybackStalledNotification`-Warnungen (Z. 5384, 5546) trafen den Streak-/Fenster-Mechanismus nicht — die Plausibilisierung aus RC.29 arbeitete korrekt, die Schaltung war fachlich „legal".
- **`AutoResumeRemainingSeconds`** ist vollständig verdrahtet: `AppSettings` (Property + `Normalize()`-Clamp), `AppSettingsService` (Key-Konstante, `Load`, `Save`), `SettingsPage.xaml` (Entry `AutoResumeRemainingEntry`) + `.xaml.cs` (`WireNumeric`, `SetTextIfNotFocused`, Handler `OnAutoResumeRemainingCompleted`) sowie zahlreiche Test-Referenzen.
- **`AppSettings.EndWindow`** (Z. 47–48) liefert `max(WatchedEndWindowSeconds, AutoResumeRemainingSeconds)` und hat genau zwei Konsumenten: `AppUpdate.EvaluateEndWindow` (Z. 3883, Streak-Fenster) und `PlayerPage.ReportPosition` (Z. 189–190, Diagnose-`LogDebug`).
- **`WatchedEndWindowSeconds`**-Markierung samt Streak-Plausibilisierung (`PlayerState.EndWindowStreak`, `IsProgressTickPlausible`, `EndWindowRequiredTicks = 2`) und optionalem `DeleteWatchedDownloadEffect` existiert und ist von der Anforderung nicht betroffen.
- **Identitäts-Guard** (`PlaybackProgressMessage.MediaId` in `AppMessage.cs` Z. 1009–1014, `appliedMediaId` in `PlayerPage`) und `OnMediaEnded` → `PlaybackEndedMessage` (Z. 455–462) sind vorhanden.
- **Tests:** Breite Abdeckung in `AppUpdateTests_Player`, `AppUpdateTests_PlaylistPlayback`, `AppUpdateTests_Downloads` — viele `PlaybackProgress_*`-Tests pinnen heute den positionsgetriebenen Wechsel; die `PlaybackEnded_*`-Tests decken den Ende-Pfad bereits ab. Settings-Tests (`AppSettingsTests`, `AppSettingsServiceTests`, `SettingsStorageContractTests`) pinnen Property, Persistenz und UI-Elemente. E2E-Tests in `PlaylistPlaybackTests`/`ContinueWatchingPlaybackTests` stützen sich auf den sofortigen Fenster-Vorlauf des kurzen Testvideos.
- **Fehlend bzw. zu entfernen:** nichts Neues ist zu bauen — die Anforderung ist eine reine Entfernungs-/Vereinfachungsaufgabe: Vorlauf-Zweige, Einstellung, UI-Feld und zugehörige Test-/Doc-Referenzen entfallen.

## Vorfallprotokoll (Befund `videoplayer.log`)

| Zeit (2026-10-19) | Zeile | Eintrag |
|-------------------|-------|---------|
| 20:48:26.768 | Z. 5381 | `PlaybackSourceService`: Quelle für `tvshowepisode/21614` aufgelöst: `ServerStream`, Fortsetzung 00:04:49 |
| 20:48:26.771 | Z. 5382 | `ShellAppEffectHandler`: Wiedergabe gestartet 21614 über ServerStream |
| 20:48:41.292 | Z. 5384 | `MediaManager`-WARN: `AVPlayerItemPlaybackStalledNotification` (Stream 21614) |
| 20:53:37.090 | Z. 5446 | Download 21614 abgeschlossen |
| 21:01:21.420 | Z. 5546 | Erneute `AVPlayerItemPlaybackStalledNotification` (Stream 21614) |
| 21:03:40.559 | Z. 5577 | `DownloadService`: Download 21614 entfernt (`DeleteWatchedDownloadEffect` nach `IsWatched`) |
| 21:03:40.801 | Z. 5578 | Quelle für `tvshowepisode/21615` aufgelöst: `LocalFile` |
| 21:03:40.802 | Z. 5579 | Wiedergabe gestartet 21615 über LocalFile |

Kein `PlayerPage: Wiedergabe beendet`-Eintrag und kein `PlaybackFailedMessage` für 21614 im gesamten Protokoll (vgl. `grep „beendet"` — einzige Ende-Meldung Z. 3279, anderer Titel). Der Wechsel 21614→21615 um 21:03:40 ist damit eindeutig dem positionsgetriebenen Fenster-Vorlauf zuzuordnen.

## Details

- [Datenmodell und Persistenz](inventory/models.md) — `AppSettings`, `PlayerState` (Wiedergabe-Zustand), `PlaybackProgressMessage`/`PlaybackEndedMessage`
- [Logik](inventory/logic.md) — `AppUpdate` (MVU-Kern), `AppSettingsService`, `PlayerPage`, `SettingsPage`
- [Interfaces](inventory/interfaces.md) — `ISettingsStore`
- [Tests](inventory/tests.md) — Unit-/Contract-Tests (`VideoPlayer.Maui.Tests`) und E2E-Tests (`VideoPlayer.E2E.Tests`)
