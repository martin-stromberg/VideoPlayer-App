# Übersetzte Anforderung: tvOS-Wiedergabe mit Fortschritts-Tracking, Auto-Fortsetzung und Spurwahl (Projektschritt 7)

Quelle: `docs/projects/task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl/project-plan.md`, `### Schritt 7` (wörtlich verbindlich). Schritt 8 (Playlists-UI) ist nicht Teil dieser Anforderung; der Wiedergabefluss soll den Playlist-Kontext aber nicht verunmöglichen.

## Fachliche Zusammenfassung

Die tvOS-App erhält die vollständige Videowiedergabe auf dem nativen Player-Stack (`AVPlayerViewController`/`AVPlayer` mit systemeigenem Transport-UI), aufbauend auf der bereits vorhandenen plattformneutralen Wiedergabelogik (`PlayerState`, `PlaybackSourceService`, `ProgressReportService`, `UpcomingEpisodesService`, `pending_progress`-Queue, Endfenster/`EndWindowStreak`, Auto-Resume-Einstellung). Der tvOS-Player ist ein reiner Ereignis-Übersetzer: er meldet Takt-, Ende- und Fehlerereignisse als `AppMessage` in den MVU-Zyklus; die Schwellen-/Fortsetzungslogik bleibt in `AppUpdate`. Die bisher platzhalterhaft verdrahteten Effekte `NavigateToPlayerEffect` (zeigt `PreparedTargetViewController`) und `ResolvePlaybackSourceEffect` werden echt ausgeführt.

## Fachlicher Scope

- **Verarbeitungseinheit:** Eine Wiedergabesitzung eines Mediums (`movie`/`tvshowepisode`, ggf. im Playlist-Kontext) von der Quellauflösung über den Fortschrittstakt bis zum plausibilisierten Ende bzw. Abbruch durch den Anwender.
- **Auslösende Ereignisse:** „Abspielen" auf der Detailseite (`PlayRequestedMessage`), Auswahl eines Weiterschauen-Eintrags (`ContinueWatchingItemSelectedMessage`, abspielbare Typen starten direkt), playlist-gebundene Weiterschauen-Einträge (`StartPlaylistPlaybackEffect`), Auto-Fortsetzung und Fehler-Rückfall am Titelende (`PlaybackEndedMessage`/`PlaybackFailedMessage` → `AdvanceToNextEpisode`/`AdvancePlaylistEntry`), manuelle Playlist-Navigation (`PlaylistNextRequestedMessage`/`PlaylistPreviousRequestedMessage`).
- **Erwartete Wirkung:** Das Video spielt im nativen tvOS-Vollbildplayer ab (Server-Stream oder lokale Datei), startet an der Fortsetzungsposition; der Fortschritt erreicht den Server im 5-Sekunden-Takt bzw. die persistente `pending_progress`-Warteschlange; „angesehen" wird erst nach bestätigtem Endfenstereintritt gesetzt; bei aktivierter Auto-Fortsetzung startet am Ende automatisch die nächste Episode bzw. der nächste Playlist-Eintrag; Wiedergabefehler fallen auf den nächsten Eintrag zurück statt zu hängen; Spurwahl (Ton/Untertitel) steht zur Verfügung, sofern der Stream Spuren anbietet.
- **Umgang mit fehlenden Objekten:** Nicht auflösbare Quelle → `PlaybackSourceResolvedMessage` mit Fehler → Rückfall auf Folgeeintrag bzw. Fehlerzustand. Fehlende Fortsetzungsposition → Wiedergabe ab Anfang. Fehlende Spuren im Stream → Spurwahl entfällt ersatzlos (kein Server-Vertrag). Offline → nur lokale Dateien werden aufgelöst (`allowServerStream: false`). Fehlende Playlist-Endpunkte (204) → `PlaylistNextExhausted`-Behandlung des MVU-Kerns.
- **Scope-Ausschlüsse:**
  1. Playlist-Übersicht/-Detail (Schritt 8) — nicht Teil; der Playlist-Kontext wird aber über die vorhandenen Play-Endpunkte (start/next/previous/advance) verdrahtet.
  2. Downloads/Download-UI (Schritt 9) — nicht Teil; der Wiedergabe-Schutz (`ProtectDownloadExpiryEffect`) und die Ablauf-Verkürzung (`ExpireWatchedDownloadEffect`) werden dennoch auf den `DownloadService` verdrahtet (pflichtiger Umfang des Schritts).
  3. Picture-in-Picture/Mini-Player — für tvOS nicht vorgesehen; `TogglePictureInPictureEffect` bleibt wirkungslos.
  4. Kein neuer Server-Vertrag für die Spurwahl — bietet der Stream keine `AVMediaSelectionGroup`s, entfällt die Auswahl.
  5. Keinerlei Schreibzugriff auf Inhalte (unverändert).
  6. Weiterschauen-Kontextmenü (Ausblenden/Überspringen) ist auf tvOS ohne UI-Auslöser — die Effekte bleiben unverdrahtete No-Ops.

## Betroffene Klassen und Komponenten

- `VideoPlayer.TvOS/Ui/TvosPlayerViewController.cs` (neu): `AVPlayerViewController`-Unterklasse — Quellanwendung aus `PlayerState.Source` (`appliedLocation`/`appliedMediaId`/`sourceApplyFailed`-Muster der MAUI-`PlayerPage`), Fortsetzungs-Seek bei `ReadyToPlay`, `AddPeriodicTimeObserver` als 5-s-Takt, `AVPlayerItemDidPlayToEndTime`-/`FailedToPlayToEndTime`-Benachrichtigungen, `PlayerPageOpenedMessage`/`PlayerClosedMessage`, Transport-Bar-Menüeinträge für Playlist next/previous, Fehlerdarstellung.
- `VideoPlayer.TvOS/Ui/TvosNavigator.cs` (erweitert): `PresentPlayer` (modale Präsentation über `TopmostPresented`), Render-Verteilung an den Player-Controller.
- `VideoPlayer.TvOS/Services/TvosAppEffectHandler.cs` (erweitert): echte Ausführung von `NavigateToPlayerEffect`, `ResolvePlaybackSourceEffect`, `LoadUpcomingEpisodesEffect`, `ReportPlaybackProgressEffect`, `StartPlaylistPlaybackEffect`, `LoadNextPlaylistEntryEffect`, `LoadPreviousPlaylistEntryEffect`, `AdvancePlaylistEntryEffect`, `ProtectDownloadExpiryEffect`, `ExpireWatchedDownloadEffect`.
- `VideoPlayer.TvOS/AppComposition.cs` (erweitert): Singletons `PlaybackSourceService`, `UpcomingEpisodesService`, `PlaylistService`, `DownloadService` (ohne Backend).
- `VideoPlayer.Maui.Tests/TvOsPlaybackContractTests.cs` (neu): Quell-Contract-Tests nach dem Muster `TvOsDetailsContractTests`.
- Unverändert (plattformneutral): `AppUpdate`, `PlayerState`, `PlaybackSourceService`, `ProgressReportService`, `UpcomingEpisodesService`, `PlaylistService`, `DownloadService` — die gesamte Takt-/Schwellen-/Fortsetzungslogik.

## Implementierungsansatz

- `IModelRenderable.Render(AppModel)` des Player-Controllers wendet `PlayerState.Source` auf `AVPlayer` an (`ReplaceCurrentItemWithPlayerItem`), springt bei `AVPlayerItemStatus.ReadyToPlay` auf `ResumePosition` und startet die Wiedergabe; ein `PeriodicTimeObserver` (5 s, Haupt-Queue) dispatcht `PlaybackProgressMessage` mit `appliedMediaId`-Bindung; `AVPlayerItem`-Benachrichtigungen dispatcht er als `PlaybackEndedMessage`/`PlaybackFailedMessage` (die Toleranz-Plausibilisierung bleibt im MVU-Kern); `PlayerClosedMessage` beim modale Schließen (Menü-Taste).
- Spurwahl nativ über die `AVMediaSelectionGroup`s des Items — auf tvOS bietet `AVPlayerViewController` die System-Spurauswahl im Info-/Pull-down-Panel automatisch, sofern der Stream Gruppen anbietet (z. B. HLS); ohne Gruppen entfällt sie ersatzlos. Titel-/Playlist-Kontext über `CustomInfoViewControllers` (tvOS 15) bzw. Transport-Bar-Custom-Items (`TransportBarCustomMenuItems`, tvOS 11+) für next/previous.
- Kompiliernachweise: tvOS-Build ausschließlich über macOS-CI (`build-tvos`, Push-Trigger `task/issue-86-**`); plattformneutrale Logik über `VideoPlayer.Maui.Tests` + neue Contract-Tests.

## Konfiguration

Keine neuen Einstellungen — Auto-Fortsetzung folgt `AppSettings.AutoResumeEnabled`, „angesehen"-Fenster `AppSettings.WatchedEndWindowSeconds`, Download-Aufräumen `AppSettings.AutoDeletePlayedDownloads`/`DeleteAfterPlaybackDelayMinutes` (alle bereits vorhanden).

## Offene Fragen

Keine — der Schritt ist in `project-plan.md` vollständig spezifiziert; VORARBEIT und Rahmenbedingungen benennen alle Anschlusspunkte.
