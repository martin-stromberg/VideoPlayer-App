# Protokoll-Befund: `videoplayer (6).log`

Datei: `videoplayer (6).log` (Repository-Root, 4095 Zeilen, iOS-Gerät). Vorfall: Episode `tvshowepisode/21600` („Normale Helden", ServerStream) wird um 13:42:48 durch `tvshowepisode/21603` („Die Hochzeit des Jahrhunderts") ersetzt — ohne `MediaEnded`/`MediaFailed`-Eintrag.

## Timeline des Vorfalls (ab Log-Zeile 3863)

| Zeit (2026-10-18) | Eintrag | Bewertung |
|--------------------|---------|-----------|
| 13:24:04.138/.143 | Download-Aufträge 21600 + 21603 eingeplant („2 aktuelle und folgende Episoden für Episode 21600") | Auto-Download beider Episoden. |
| 13:24:24.379 | „Wiedergabequelle für tvshowepisode/21600 aufgelöst: ServerStream, Fortsetzung (null)." | Wiedergabestart 21600 von vorn. |
| 13:24:24.413 | „Wiedergabe geöffnet: Danger Force Staffel 02 23: Normale Helden (Fortsetzung (null))." | `PlayerPage.OnMediaOpened`. |
| 13:31:17–13:36:16 | Downloads 21600/21603: `Downloading` → `Completed` (21600: 13:36:09.966, 21603: 13:36:16.822) | Beide Downloads abgeschlossen — Voraussetzung für `DeleteIfCompletedAsync`. |
| 13:36:23.894 | `AVPlayerItemPlaybackStalledNotification` (Stream 21600) | Stream-Stall Nr. 1. |
| 13:36:26.511 | `AVPlayerItemPlaybackStalledNotification` (Stream 21600) | Stall Nr. 2. |
| 13:39:22.722 | `AVPlayerItemPlaybackStalledNotification` (Stream 21600) | Stall Nr. 3. |
| 13:39:43.161 | `AVPlayerItemPlaybackStalledNotification` (Stream 21600) | Stall Nr. 4 — letzter relevanter Eintrag vor dem Wechsel. |
| 13:42:48.278 | „Download tvshowepisode/21603 (… Jahrhunderts) entfernt." | Löschung des **Folge**-Downloads — **vor** Abschluss dessen Quellauflösung. |
| 13:42:48.341 | „Download tvshowepisode/21600 (… Helden) entfernt." | Löschung des laufenden Downloads (`AutoDeleteWatchedEpisodes` aktiv). |
| 13:42:48.462 | „Wiedergabequelle für tvshowepisode/21603 aufgelöst: ServerStream, Fortsetzung 00:17:03." | Wechsel abgeschlossen; serverseitige Fortsetzungsposition 17:03. |
| 13:42:48.480 | „Wiedergabe geöffnet: Die Hochzeit des Jahrhunderts (Fortsetzung 00:17:03)." | `PlayerPage.OnMediaOpened` des Folgetitels. |
| 13:42:58.509 | „Fortsetzungs-Seek auf 00:17:03 fehlgeschlagen." (`TaskCanceledException`, `MediaElement.SeekTo`) | Seek-Timeout — nicht-fatal (Fix `e465907`), kein Rückfall. Ende des Protokolls. |

## Negative Befunde

- **Kein** „Wiedergabe beendet"-Eintrag (`OnMediaEnded` → `PlaybackEndedMessage`) im gesamten Abschnitt — der plausibilisierte Endpfad (`UpdatePlaybackEnded`/`IsPlaybackEndPlausible`) scheidet als Auslöser aus.
- **Kein** „Wiedergabe fehlgeschlagen"- oder „Wiedergabestart fehlgeschlagen"-Eintrag (`OnMediaFailed`/`FailPlaybackStart` → `PlaybackFailedMessage`) — `UpdatePlaybackFailed` scheidet aus.
- **Kein** Seek-Fehlschlag an 21600 vor dem Wechsel (der erste Fortsetzungs-Seek betrifft 21600 mit „(null)" — nichts zu seeken).
- **Keine** Positions-/Dauer-Werte im Protokoll — der Fortschrittstakt wird nicht protokolliert; die Auslöser-Konstellation (Position ≈ Duration vs. geschrumpfte `Duration` vs. legitimer Fenstereintritt) ist aus dem Protokoll nicht rekonstruierbar.

## Ableitung des Wechselpfads

- Einzig verbleibender Wechselmechanismus ist der positionsgetriebene Fenstereintritt in `UpdatePlaybackProgress` (`AppUpdate.cs` Z. 3846–3870): `remaining <= AutoResumeRemainingSeconds` bei `IsPlaying` → `AdvanceToNextEpisode` (Serienkontext, kein Playlist-Kontext im Protokoll erkennbar).
- Der Wechsel erfolgte ~18:24 min nach Wiedergabestart mit „Fortsetzung (null)". Ob die reale Restlaufzeit von 21600 zu diesem Zeitpunkt das 300-s-Fenster legitim unterschritten hatte (Abspann-Vorlauf) oder ein inkonsistenter Takt ein künstliches Ende erzeugte, ist ohne Positionsdaten offen.
- **Löschung von 21603:** `DeleteIfCompletedAsync` wird automatisch nur über `DeleteWatchedDownloadEffect` erreicht — der nur für die **aktuelle** `MediaId` emittiert wird. Da die Löschung (13:42:48.278) der Quellauflösung von 21603 (13:42:48.462) vorausgeht, ist der einzige codekompatible Erzeuger ein `PlaybackProgressMessage`-Takt, der **nach** `AdvanceToNextEpisode` (State: `MediaId` = 21603, `IsWatched` = false, `Position`/`Duration` = 0) verarbeitet wurde, während das `MediaElement` noch die alte Position/Dauer meldete → Fenstertest trifft → `IsWatched`/`DeleteWatchedDownloadEffect` für 21603. Dieselbe Konstellation kann im Serienzweig (`HasNextEpisode` ohne `IsResolving`-Guard) weitere Vorläufe kaskadieren.
- Vier dokumentierte Stalls (`AVPlayerItemPlaybackStalledNotification`) in den ~6 min vor dem Wechsel stützen die Verdachtsrichtung gestörter Positions-/Dauer-Meldungen; ob `MediaElementState` dabei `Playing` blieb, ist am Protokoll nicht ablesbar (die Meldung trüge dann `IsPlaying = true`, `IsBuffering = false`).
