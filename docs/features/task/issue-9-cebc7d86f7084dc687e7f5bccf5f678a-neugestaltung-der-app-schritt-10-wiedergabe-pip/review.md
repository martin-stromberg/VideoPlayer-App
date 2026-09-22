# Plan-Review

Geprüft: `plan.md` gegen die Implementierung (Selbstprüfung ohne
Unteragenten — dokumentierte Abweichung, es stehen keine Unteragenten
zur Verfügung). Alle Planelemente wurden im Code gesucht und
vollständig gelesen.

## Ergebnis

**Status:** Vollständig umgesetzt (mit einer verfeinerten Abweichung)

## Umgesetzte Planelemente

### Projekt `VideoPlayer.Mvu` (net10.0)

- [x] `PlaybackSourceKind` — `ServerStream`, `LocalFile`.
- [x] `PlaybackSource` — `Kind`, `Location`, `ResumePosition`.
- [x] `PlayerEpisodeRef` — `MediaId`, `Title`.
- [x] `PlayerState` — Medium (`MediaType`/`MediaId`/`Title`), `Source`,
  `IsResolving`, `Attempted`, `Position`, `Duration`, `IsPlaying`,
  `IsBuffering`, `IsWatched`, `IsPictureInPicture`,
  `UpcomingEpisodes`/`HasNextEpisode`, `Error`/`HasError`,
  `LastReportedPositionSeconds`, `IsActive`, `Empty`.
- [x] `PlaybackSourceService` — `download_manifest`-Prüfung zuerst
  (`State == "completed"`, Datei existiert → `LocalFile`), sonst
  Stream-URL `{base}/api/items/{typ}/{id}/stream?access_token=…`;
  Fortsetzungsposition aus `api/continue-watching`, Fallback auf die
  Cache-Kopie `cached_lists["continue_watching"]`; injizierbare
  `fileExists`-Prüfung für Tests.
- [x] Messages in `AppMessage.cs`: `PlayRequestedMessage`,
  `PlayerPageOpenedMessage`, `PlaybackSourceResolvedMessage`,
  `PlaybackProgressMessage`, `PlaybackEndedMessage`,
  `PlaybackFailedMessage`, `PictureInPictureRequestedMessage`,
  `PictureInPictureChangedMessage`, `PlayerClosedMessage`.
- [x] Effekte in `AppEffect.cs`: `NavigateToPlayerEffect`,
  `ResolvePlaybackSourceEffect`, `ReportPlaybackProgressEffect`,
  `TogglePictureInPictureEffect`.
- [x] `AppModel.Player` (`PlayerState.Empty` als Ausgangszustand).
- [x] `AppUpdate`: alle neuen Übergänge — `UpdatePlayRequested`
  (Guard + Fortsetzungsliste via `NextEpisodeQueue`),
  `UpdatePlayerPageOpened`, `UpdatePlaybackSourceResolved` (Erfolg/
  Fehlschlag + Rückfall), `UpdatePlaybackProgress` (Positionsübernahme,
  Report-Throttling > 0,5 s, „angesehen"-Schwelle, Auto-Fortsetzung),
  `UpdatePlaybackEnded` (Abschlussmeldung `Position = Duration`,
  Auto-Fortsetzung), `UpdatePlaybackFailed` (Fehler + Rückfall),
  `UpdatePictureInPictureRequested`/`…Changed`,
  `UpdatePlayerClosed` (Abschlussmeldung, PiP-Rücksprung, Reset);
  `AdvanceToNextEpisode` als gemeinsamer Wechsel;
  `UpdateLogoutRequested` leert `Player`.

### Projekt `VideoPlayer.Maui`

- [x] `VideoPlayer.Maui.csproj`: `CommunityToolkit.Maui.MediaElement`
  10.0.0.
- [x] `MauiProgram`: `UseMauiCommunityToolkitMediaElement(true)`
  (Android-Vordergrunddienst-Parameter), `PlaybackSourceService`,
  `IPictureInPictureService`/`PictureInPictureService`,
  `Pages.PlayerPage` registriert.
- [x] `PlayerPage.xaml(.cs)`: `MediaElement` (Autoplay, native
  Transportsteuerung, `MediaOpened`/`MediaEnded`/`MediaFailed`/
  `StateChanged`), Lade-/Puffer-Anzeige, Titel-/„Angesehen"-/
  Fehler-Overlays, Windows-PiP-Schalter (nur bei
  `IsToggleSupported`), 5-s-Positions-Takt (nur `Playing`),
  `StateChanged`-Spiegelung, einmaliges `SeekTo(ResumePosition)`,
  `PlayerClosedMessage` beim Verlassen + `RestoreAsync` des
  PiP-Dienstes.
- [x] `IPictureInPictureService` + `PictureInPictureService`
  (`#if WINDOWS`: `AppWindow.SetPresenter(CompactOverlay)` ↔
  `Default`; andere Plattformen inaktiv).
- [x] `AppShell.xaml.cs`: Route `player` registriert.
- [x] `ShellAppEffectHandler`: vier neue Fälle — Navigation
  (`GoToAsync("player")`), Quellauflösung →
  `PlaybackSourceResolvedMessage`, Fortschrittsmeldung →
  `ProgressReportService.ReportProgressAsync`, PiP-Umschaltung →
  `PictureInPictureChangedMessage`.
- [x] `MediaDetailsPage`: „Abspielen"-Button im Kopf (nur
  `movie`/`tvshowepisode`, Titel via `HighlightedEpisodeTitle ??
  Title`), ▶-Button je Episoden-Eintrag; `MediaDetailsView.IsPlayable`.
- [x] `Platforms/iOS/Info.plist`: `UIBackgroundModes` → `audio`.

## Abweichungen vom Plan

- **Auto-Fortsetzung nur bei laufender Wiedergabe:** Das Update
  verlangt zusätzlich `PlaybackProgressMessage.IsPlaying == true`
  für den Wechsel — ein Takt im Pausenzustand (`StateChanged`
  spiegelt jeden Zustandswechsel) darf nicht zur nächsten Episode
  weiterschalten, wenn der Anwender am Ende pausiert. Ergänzung zum
  Plan, abgedeckt durch
  `PlaybackProgress_PausedWithinWindow_DoesNotAdvance`.
- `UseMauiCommunityToolkitMediaElement(true)`: die Paketversion
  10.0.0 verlangt den Android-Vordergrunddienst-Parameter —
  plan-konforme Aktivierung mit `true`.

## Verbleibende Punkte

Keine — alle Planelemente umgesetzt, keine offenen Punkte im Plan.
