# Bestandsaufnahme: Frühzeitiger Wechsel zum nächsten Video bei fehlgeschlagenem Fortsetzungs-Seek

Analysiert wurde der Wiedergabepfad der App (`VideoPlayer.Maui`-Seite plus MVU-Kern in `VideoPlayer.Mvu`) bezogen auf die Anforderung, dass ein fehlschlagender Fortsetzungs-Seek (`MediaElement.SeekTo`-Timeout) die laufende Wiedergabe nicht beenden und keinen Episodenwechsel auslösen darf. Beleglage: `videoplayer (5).log` zeigt die Fehlerkette dreimal (Seek-Timeout → `PlaybackFailedMessage` → Rückfall auf die Folge-Episode).

## Zusammenfassung

- **Fehlerkette im Code bestätigt:** `PlayerPage.ApplyResumeSeekAsync` (`VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`, Zeilen 399–415) fängt **jede** Ausnahme des `SeekTo`-Aufrufs — inklusive der `TaskCanceledException` des internen 10-s-Timeouts (`ResumeSeekTimeout`, Zeile 26) — und dispatcht im catch-Zweig `PlaybackFailedMessage`. Dieselbe Message wird auch von den echten Fehlerpfaden `FailPlaybackStart` (Zeile 294) und `OnMediaFailed` (Zeile 426) verwendet; eine Unterscheidung fataler/nicht-fataler Fehler existiert nicht.
- **Rückfall-Mechanik vorhanden:** `AppUpdate.UpdatePlaybackFailed` (Zeile 4033) leitet `PlaybackFailedMessage` ohne weitere Prüfung in den Rückfall — im Serienkontext `AdvanceToNextEpisode` (Zeile 3735, über `PlayerState.UpcomingEpisodes`), im Playlist-Kontext `AdvancePlaylistEntry` (Zeile 2612, über `AdvancePlaylistEntryEffect` → `POST api/playlists/{id}/play/advance`). Derselbe Rückfall wird auch von `UpdatePlaybackSourceResolved` bei nicht auflösbarer Quelle genutzt.
- **Seek-Gating vorhanden:** `pendingResumePosition` + `TryApplyPendingResumeSeek` (Zeilen 359–388) wenden den Fortsetzungs-Seek verzögert an, sobald `CurrentState` `Playing`/`Paused` meldet (iOS-`ReadyToPlay`-Workaround, Toolkit-Issue #3249). Ein Wiederholungsmechanismus oder eine Unterscheidung von Ausnahmetypen existiert nicht.
- **Protokoll-Befund:** Drei Timeout-Fälle, alle `ServerStream` mit Fortsetzungsposition, alle auf iOS (AVPlayer-Pfade): `tvshowepisode/7038`→`7039` (10:28), `7040`→`7041` (12:00), `21599`→`21600` (10:18). Jeweils ~10–15 s nach „Wiedergabe geöffnet" die `TaskCanceledException` aus `MediaElement.SeekTo`, ~0,6 s danach Auflösung und Start der Folge-Episode.
- **Tests vorhanden:** `PlayerContractTests` pinnen den Ist-Zustand — u. a. dass `ApplyResumeSeekAsync` `PlaybackFailedMessage` dispatcht (`PlayerPage_NativeAccessesAreFailureIsolated`, Zeile 370–373) und dass genau ein `SeekTo(`-Aufruf existiert. `AppUpdateTests_Player`/`AppUpdateTests_PlaylistPlayback` pinnen den Episoden-/Playlist-Rückfall. E2E: `PlaybackTests.PlayFromRecent_StartsPlaybackAtResumePosition` (Resume-Seek-Erfolgspfad) und `PlaylistPlaybackTests.StreamFailure_AdvancesToNextPlaylistEntry`; ein Seek-Fehlschlag-Szenario existiert nicht.
- **Keine eigene Message für nicht-fatale Fehler:** `AppMessage` enthält nur `PlaybackFailedMessage` (parameterlos) — keine Meldung, die einen Fehlschlag ohne Rückfall signalisiert.

## Details

- [Datenmodell und MVU-Records](inventory/models.md) — `PlayerState`, `PlaybackSource`, `PlayerEpisodeRef`, `PlaylistPlaybackStartInfo`/`PlaylistNavigationItem`, relevante `AppMessage`-/`AppEffect`-Records, `AppSettings`-Wiedergabefelder
- [Logik](inventory/logic.md) — `PlayerPage` (Seek-Pfad und Fehler-Dispatch), `AppUpdate` (Rückfall-Update-Funktionen), `PlaybackSourceService`, `ShellAppEffectHandler`, `MvuHost`, `PlaylistService`
- [Enums](inventory/enums.md) — `PlaybackSourceKind` (und der externe `MediaElementState` des Toolkits)
- [Interfaces](inventory/interfaces.md) — `IAppEffectHandler`, `IPictureInPictureService`
- [Tests](inventory/tests.md) — Vertrags-/Update-/Dienst-/E2E-Tests des Wiedergabepfads
- [Fehlerprotokoll](inventory/logfindings.md) — Auswertung von `videoplayer (5).log` mit den drei Fehlersequenzen und Zeilenverweisen
