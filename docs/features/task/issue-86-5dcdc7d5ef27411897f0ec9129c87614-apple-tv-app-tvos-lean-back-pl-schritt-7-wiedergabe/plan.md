# Umsetzungsplan: tvOS-Wiedergabe mit Fortschritts-Tracking, Auto-Fortsetzung und Spurwahl (Schritt 7)

## Übersicht

Die tvOS-App erhält einen echten nativen Player: `TvosPlayerViewController` (AVPlayerViewController-Unterklasse) als reiner Ereignis-Übersetzer in den bestehenden MVU-Zyklus, dazu die echte Ausführung der Wiedergabe-Effekte im `TvosAppEffectHandler` und die fehlenden Dienst-Singletons in `AppComposition`. Die gesamte Takt-/Schwellen-/Fortsetzungslogik bleibt unverändert in `AppUpdate` bzw. den plattformneutralen Diensten — es entsteht kein neuer Code in `VideoPlayer.Mvu`.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Eine Wiedergabesitzung eines Mediums (`movie`/`tvshowepisode`, ggf. Playlist-Kontext): Quellauflösung → Wiedergabe ab Fortsetzungsposition → 5-s-Fortschrittstakt → plausibilisiertes Ende/Abbruch/Fehler |
| Auslösende Ereignisse | `PlayRequestedMessage` (Detailseite, bereits dispatcht), `ContinueWatchingItemSelectedMessage` (Startseite, bereits dispatcht), Playlist-Start (`StartPlaylistPlaybackEffect`), `PlaybackEndedMessage`/`PlaybackFailedMessage` (Auto-Fortsetzung/Rückfall), `PlaylistNextRequestedMessage`/`PlaylistPreviousRequestedMessage` (manuelle Playlist-Navigation), `PlayerClosedMessage` (Schließen) |
| Erwartete Wirkung | Nativer tvOS-Vollbildplayer spielt Server-Stream/lokale Datei ab Fortsetzungsposition; Fortschritt erreicht Server bzw. `pending_progress`-Queue; „angesehen" nur nach bestätigtem Endfenster; Auto-Fortsetzung über Serien- bzw. Playlist-Kontext; Fehler-Rückfall statt Hängen; Spurwahl wo der Stream Gruppen anbietet |
| Umgang mit fehlenden Objekten | Nicht auflösbare Quelle → Rückfall/Fehlerzustand (MVU-Kern); fehlende Fortsetzungsposition → Start ab Anfang; fehlende Spurgruppen → keine Auswahl (kein Server-Vertrag); offline → nur lokale Dateien (`allowServerStream: false`); 204-Playlist-Navigation → `PlaylistNextExhausted` (MVU-Kern) |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Playlist-Übersicht/-Detail (Schritt 8) | Explizit nicht Teil des Schritts; Play-Endpunkte werden dennoch verdrahtet, damit der Kontext funktioniert | bestätigt (Anforderungstext) |
| 2 | Download-UI/Einreihen (Schritt 9) | Explizit später Schritt; `EnqueueDownloadEffect` bleibt vorbereiteter Hinweis | bestätigt (Anforderungstext) |
| 3 | PiP/Mini-Player | Für tvOS nicht vorgesehen (Grobe Vorgehensentscheidung); `TogglePictureInPictureEffect` bleibt No-Op | bestätigt (Anforderungstext) |
| 4 | Eigene Spurwahl-UI statt System-Spurwahl | tvOS-`AVPlayerViewController` bietet die Auswahl systemseitig, wenn `AVMediaSelectionGroup`s existieren; ohne Gruppen entfällt sie ersatzlos (kein Server-Vertrag) | bestätigt (Anforderungstext) |
| 5 | Weiterschauen-Kontextmenü (hide/skip) | Kein UI-Auslöser auf tvOS; Effekte bleiben unverdrahtete No-Ops | bestätigt (Bestandsaufnahme) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Player-Oberfläche | `AVPlayerViewController`-Unterklasse statt eigener Transport-UI | System-Transport-UI ist Lean-back-Standard auf tvOS und bringt die Spurwahl im Info-/Pull-down-Panel automatisch mit; eigene Bedienung wäre Redesign gegen die Anforderung „systemeigenem Transport-UI" |
| Takt-/Ereignisbrücke | `AddPeriodicTimeObserver` (5 s, `DispatchQueue.MainQueue`) + `AVPlayerItem.Notifications.Observe*` + KVO auf `status`/`timeControlStatus` | Ereignis-Übersetzer-Rolle des Players; deckt Takt, Ende, Fehler und Puffer-Zustandswechsel ohne eigenes Timer-Gerüst ab |
| Fortsetzungs-Seek | Gemerkte `ResumePosition` erst bei `AVPlayerItemStatus.ReadyToPlay` per `Seek` anwenden | MAUI-Muster (iOS-Toolkit-Issue): Seek vor Bereitschaft ist fehleranfällig |
| Playlist next/previous | `TransportBarCustomMenuItems` mit `UIAction`s, Enabled über `CanNavigateNext`/`CanNavigatePrevious` | Nativer Erweiterungspunkt der Transport-Bar (tvOS 11+), fokussierbar per Siri Remote; kein Overlay-Bau nötig |
| Titel-/Playlist-Kontextanzeige | `CustomInfoViewControllers` (tvOS 15 = `SupportedOSPlatformVersion`) mit kleinem `TvosPlayerInfoViewController` (Titel, Playlist-Label, Gesehen-Hinweis) | Systemseitiger Info-Tab im Pull-down-Panel; Positionsanzeige des Playlist-Kontexts ohne Eigenbau-Overlay |
| Fehlerzustand | `UIAlertController` über dem Player, wenn `HasError && !IsResolving`; während `IsResolving` `UIActivityIndicatorView` im `ContentOverlayView` | Entspricht dem MAUI-Fehlerpanel; Rückfall-Fehler (`Error` gesetzt während `IsResolving`) blockiert die laufende Fortsetzung nicht |
| Instanzmodell des Players | Eine frische `TvosPlayerViewController`-Instanz je `NavigateToPlayerEffect`; bei bereits präsentierter Instanz kein erneutes Stapeln | Transiente Seite wie die MAUI-`PlayerPage`; Observer/Lebenszyklus je Wiedergabesitzung sauber gekapselt |

## Programmabläufe

### Wiedergabestart (Detailseite/Weiterschauen)

1. `PlayRequestedMessage`/`ContinueWatchingItemSelectedMessage` → `AppUpdate` setzt `PlayerState` (`IsResolving`) und emittiert `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect` (ggf. `LoadUpcomingEpisodesEffect`).
2. `TvosAppEffectHandler` führt `NavigateToPlayerEffect` auf dem Hauptthread aus → `TvosNavigator.PresentPlayer()` präsentiert eine neue `TvosPlayerViewController` modal `FullScreen` über `TopmostPresented()`.
3. `ResolvePlaybackSourceEffect` → `PlaybackSourceService.ResolveAsync` (`allowServerStream = !model.IsOffline`, `resumePositionSecondsOverride = StartPositionSeconds`) → `PlaybackSourceResolvedMessage`.
4. `TvosPlayerViewController.Render` wendet `PlayerState.Source` an: `NSUrl` (Stream `FromString`, Datei `FromFilename`) → `AVPlayerItem` → `ReplaceCurrentItemWithPlayerItem`, `appliedMediaId` setzen, `Play()`.
5. Bei `ReadyToPlay` (KVO `status`) wird die gemerkte `ResumePosition` per `Seek` angewendet.

### Fortschrittstakt und „angesehen"-Schwelle

1. `AddPeriodicTimeObserver` (5 s) dispatcht `PlaybackProgressMessage(position, duration, isPlaying, isBuffering, appliedMediaId)` — nur solange `TimeControlStatus == Playing`; Zustandswechsel (KVO `timeControlStatus`, `PlaybackStalled`-Notification) melden sofort.
2. `AppUpdate` throttelt (0,5 s), sendet `ReportPlaybackProgressEffect` → `ProgressReportService` (Server oder `pending_progress`), baut `EndWindowStreak`, setzt `IsWatched` ab bestätigtem Fenstereintritt, sendet `ProtectDownloadExpiryEffect` (lokale Datei, einmalig) bzw. `ExpireWatchedDownloadEffect` (Einstellung).
3. `TvosAppEffectHandler` führt `ReportPlaybackProgressEffect` → `ProgressReports.ReportProgressAsync` aus; `ProtectDownloadExpiryEffect`/`ExpireWatchedDownloadEffect` → `DownloadService.ExtendExpiryAsync`/`ExpireAfterAsync`.

### Ende, Fehler und Auto-Fortsetzung

1. `AVPlayerItemDidPlayToEndTime` → `PlaybackEndedMessage`; `AVPlayerItemFailedToPlayToEndTime` bzw. `status == Failed` → `PlaybackFailedMessage`.
2. `AppUpdate` plausibilisiert das Ende (Toleranzfenster), markiert „angesehen", meldet Endposition und schaltet bei `AutoResumeEnabled` über `AdvancePlaylistEntry` (Playlist) oder `AdvanceToNextEpisode` (Serien-Fortsetzungsliste) weiter; Fehler fallen auf denselben Pfad zurück.
3. `AdvancePlaylistEntryEffect`/`LoadNext`/`LoadPreviousPlaylistEntryEffect` → `PlaylistService` → `PlaylistNavigationCompletedMessage`; der Folgeeintrag wird im selben Player-Controller über den geänderten `PlayerState.Source` angewendet (Identitätswechsel über `appliedLocation`/`appliedMediaId`).

### Schließen

1. Menü-Taste der Siri Remote schließt den modalen Player (Systemverhalten von `AVPlayerViewController`).
2. `ViewWillDisappear`/`ViewDidDisappear` mit `IsBeingDismissed`-Guard → `PlayerClosedMessage` → Abschluss-Fortschritt + `PlayerState.Empty`; Navigator gibt die Instanz frei.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `TvosPlayerViewController` | `AVPlayerViewController`-Unterklasse, `IModelRenderable` | Nativer Player: Quellanwendung, Fortsetzungs-Seek, 5-s-Takt, Ende/Fehler-Übersetzung, Playlist-Menüeinträge, Info-Tab, Fehler-/Ladezustand |
| `TvosPlayerInfoViewController` | `UIViewController` | Info-Tab des Pull-down-Panels (`CustomInfoViewControllers`): Titel, `PlaylistLabel`, „angesehen"-Hinweis, Puffer-/Ladestatus |
| `TvOsPlaybackContractTests` | xUnit-Testklasse (`VideoPlayer.Maui.Tests`) | Quell-Contract-Tests der Verdrahtung (Muster `TvOsDetailsContractTests`) |

## Änderungen an bestehenden Klassen

### `TvosNavigator` (`VideoPlayer.TvOS/Ui/TvosNavigator.cs`)

- **Neue Methoden:** `PresentPlayer()` — neue `TvosPlayerViewController`-Instanz modal `FullScreen` über `TopmostPresented()` präsentieren; bereits präsentierte Instanz nicht erneut stapeln; `Render(AppModel)` rendert zusätzlich den Player-Controller.
- **Neue Felder:** `playerPage` (`TvosPlayerViewController?`) — Verweis für Render-Verteilung; Freigabe über Dismissal-Callback.

### `TvosAppEffectHandler` (`VideoPlayer.TvOS/Services/TvosAppEffectHandler.cs`)

- **Geänderte Fälle:** `NavigateToPlayerEffect` → `navigator.PresentPlayer()` statt `PreparedTargetViewController`; `StartPlaylistPlaybackEffect` → `PlaylistService.StartPlaybackAsync` → `PlaylistPlaybackStartedMessage` statt Platzhalter.
- **Neue Fälle:** `ResolvePlaybackSourceEffect`, `LoadUpcomingEpisodesEffect`, `ReportPlaybackProgressEffect`, `LoadNextPlaylistEntryEffect`, `LoadPreviousPlaylistEntryEffect`, `AdvancePlaylistEntryEffect`, `ProtectDownloadExpiryEffect`, `ExpireWatchedDownloadEffect` — jeweils mit den Dienstaufrufen und Ergebnis-Messages des MAUI-`ShellAppEffectHandler`-Musters (Fehler → `PlaybackUnavailableError` bzw. protokolliertes Verschlucken).
- **Neue Methoden:** `ResolvePlaybackSourceAsync`, `LoadUpcomingEpisodesAsync`, `ReportPlaybackProgressAsync`, `StartPlaylistPlaybackAsync`, `NavigatePlaylistEntryAsync`, `ProtectDownloadExpiryAsync`, `ExpireWatchedDownloadAsync`; Event-Verdrahtung `downloads.ItemChanged`/`ItemRemoved` → `DownloadItemChangedMessage`/`DownloadDeletedMessage`.

### `AppComposition` (`VideoPlayer.TvOS/AppComposition.cs`)

- **Neue Singletons:** `PlaybackSources` (`PlaybackSourceService`), `UpcomingEpisodes` (`UpcomingEpisodesService`), `Playlists` (`PlaylistService` mit `Secrets` für Claim-Lesung), `Downloads` (`DownloadService` ohne Backend).

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `PlaybackSource.Location` | `ServerStream`: absolute http/https-URL; `LocalFile`: nicht leer (Frühvalidierung wie `IsValidSourceLocation` der MAUI-Seite) | Ungültige Adresse → `PlaybackFailedMessage` (Rückfall/Fehlerzustand) |
| Stream-URL im Protokoll | Nie protokollieren (enthält `access_token`); Fehlertexte über `AppLogService.MaskAccessToken` bereinigen | — |

## Konfigurationsänderungen

Keine — `AutoResumeEnabled`, `WatchedEndWindowSeconds`, `AutoDeletePlayedDownloads`, `DeleteAfterPlaybackDelayMinutes` existieren bereits in `AppSettings`.

## Seiteneffekte und Risiken

- **tvOS-Binding-Fallen** (bekannt): `AddPeriodicTimeObserver`-Signatur, `CMTime`-Fabriken, `AVPlayerItem.Notifications.Observe*`-Helfer, `TransportBarCustomMenuItems`/`CustomInfoViewControllers`-Benennung — nur per `build-tvos`-CI verifizierbar; Iteration über Push-Zyklus eingeplant.
- **Modale Verkettung:** `TopmostPresented()` liefert den Player als Anker; bei geschachtelten Detailseiten bleibt die Rückkehr-Ebene erhalten (Muster `PresentMediaDetails`).
- **Doppelte Präsentation:** `NavigateToPlayerEffect` kann bei noch offenem Player erneut feuern (z. B. Playlist-Start während offenem Player) — Guard verhindert erneutes Stapeln, die neue Identität läuft über `Render`.
- **`EnqueueDownloadEffect`** bleibt der bisherige vorbereitete Hinweis (Schritt 9).
- **Kein Schreibzugriff auf Inhalte** — ausschließlich lesende/fortschrittsmeldende Endpunkte.

## Umsetzungsreihenfolge

1. **`AppComposition`: Dienst-Singletons ergänzen** — Voraussetzungen: keine (alle Abhängigkeiten vorhanden). `PlaybackSourceService`, `UpcomingEpisodesService`, `PlaylistService`, `DownloadService` registrieren.
2. **`TvosPlayerInfoViewController` + `TvosPlayerViewController` anlegen** — Voraussetzungen: `IModelRenderable`, `TvosControls`, `MainThreadInvoker` (alle vorhanden). Player als Ereignis-Übersetzer ohne eigene Fachlogik.
3. **`TvosNavigator`: `PresentPlayer` + Render-Verteilung** — Voraussetzungen: Schritt 2.
4. **`TvosAppEffectHandler`: Player-Effekte echt verdrahten** — Voraussetzungen: Schritte 1–3; MAUI-`ShellAppEffectHandler` als Muster.
5. **Contract-Tests `TvOsPlaybackContractTests`** — Voraussetzungen: Schritte 1–4.
6. **Push → `build-tvos` grün** — Voraussetzungen: Schritte 1–5; bei Compilerfehlern Binding-Signaturen korrigieren und erneut pushen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Player_AppliesSourceAndResumeFromModel` | `TvOsPlaybackContractTests` | `TvosPlayerViewController` enthält `IModelRenderable`, `ReplaceCurrentItemWithPlayerItem`, `ResumePosition`-Seek, `appliedMediaId`, `PlaybackSourceKind`-Unterscheidung, `NSUrl`-Behandlung |
| `Player_TranslatesTickEndAndFailureToMessages` | `TvOsPlaybackContractTests` | `AddPeriodicTimeObserver`, `PlaybackProgressMessage`, `ObserveDidPlayToEndTime`/`PlaybackEndedMessage`, `PlaybackFailedMessage`, `PlayerPageOpenedMessage`, `PlayerClosedMessage` |
| `Player_PlaylistContextUsesTransportMenuAndInfoPanel` | `TvOsPlaybackContractTests` | `TransportBarCustomMenuItems`, `PlaylistNextRequestedMessage`, `PlaylistPreviousRequestedMessage`, `PlaylistLabel`, `CustomInfoViewControllers` |
| `Navigator_PresentsPlayerModally` | `TvOsPlaybackContractTests` | `PresentPlayer`, `TvosPlayerViewController`, `UIModalPresentationStyle.FullScreen`, `TopmostPresented`, Render-Verteilung |
| `TvOsPlayback_WiringIsComplete` | `TvOsPlaybackContractTests` | `AppComposition` hält `PlaybackSourceService`/`UpcomingEpisodesService`/`PlaylistService`/`DownloadService`; Handler-Cases für alle Player-Effekte + `PlaybackSourceResolvedMessage`/`PlaylistPlaybackStartedMessage`/`PlaylistNavigationCompletedMessage`; `TvosAppEffectHandler` enthält kein `PresentPreparedTarget` mehr für `NavigateToPlayerEffect`/`StartPlaylistPlaybackEffect` |

### Betroffene bestehende Tests

Keine — `VideoPlayer.Mvu` bleibt unverändert; `TvOsDetailsContractTests.TvOsDetails_WiringIsComplete` verlangt lediglich das Vorhandensein von `NavigateToPlayerEffect` im Handler (weiterhin erfüllt).

### E2E-Tests (primärer Funktionsnachweis)

Der Benutzerfluss läuft auf tvOS — die Plattform ist auf dem Entwicklungsrechner (Windows) weder kompilier- noch ausführbar; `VideoPlayer.E2E.Tests` deckt ausschließlich die MAUI-Windows-App ab und bietet keinen tvOS-Pfad. Als Funktionsnachweis bleiben: die plattformneutrale Suite (MVU-Übergänge sind dort bereits testabgedeckt, vgl. `AppUpdateTests_Player`/`AppUpdateTests_PlaylistPlayback`), die neuen Contract-Tests und der macOS-CI-Build `build-tvos` als Kompiliernachweis; die reale Abnahme auf Simulator/Gerät ist dokumentiert als manueller Schritt außerhalb des lokalen Laufs (identisch zur Vorgehensweise der Schritte 4–6, siehe `changes.log`-Einträge).

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Nicht planbar | Wiedergabe Server-Stream ab Fortsetzungsposition | — (kein tvOS-E2E-Pfad) | Wiedergabe funktioniert auf Simulator/Gerät | Plattform lokal nicht ausführbar — Nachweis über CI-Build + Contract-Tests + MVU-Übergangstests |

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path Stream-Wiedergabe | „Abspielen" auf Detailseite | Angemeldet, online, kein Fortsetzungsstand | `PlayRequestedMessage` → Player-State → `PresentPlayer` → `ResolvePlaybackSourceEffect` → Stream-URL → `AVPlayerItem` → `Play()` | Video läuft im nativen Vollbildplayer; Fortschritt geht 5-s-getaktet an den Server | `build-tvos` + `TvOsPlaybackContractTests` + `AppUpdateTests_Player` |
| 2 | Fortsetzung | Weiterschauen-Eintrag wählen | Fortsetzungsposition > 0 (Server oder Cache) | `ContinueWatchingItemSelectedMessage` → Start + Quellauflösung mit `ResumePosition` → `ReadyToPlay` → `Seek` | Wiedergabe springt zur gespeicherten Position | `build-tvos` + Contract-Tests |
| 3 | Offline/Queue | Wiedergabe ohne Server | `IsOffline`, lokale Datei im Manifest | `allowServerStream: false` → lokale Datei; Fortschritt → `pending_progress` | Lokale Datei spielt; Meldungen verbleiben in der Warteschlange bis zum Sync | `PlaybackSourceServiceTests` (bereits) + `build-tvos` |
| 4 | Angesehen-Schwelle | Wiedergabe ins Endfenster | `WatchedEndWindowSeconds` aktiv | Takte im Fenster → `EndWindowStreak` ≥ 2 → `IsWatched` | „Gesehen" wird erst nach bestätigtem Eintritt gesetzt | `AppUpdateTests_Player` (bereits) |
| 5 | Auto-Fortsetzung Serie | `DidPlayToEndTime` plausibel | `AutoResumeEnabled`, `HasNextEpisode` | Ende-Meldung → `AdvanceToNextEpisode` → Folgequelle im selben Player | Nächste Episode startet automatisch | `AppUpdateTests_Player` (bereits) + `build-tvos` |
| 6 | Auto-Fortsetzung Playlist | Ende im Playlist-Kontext | `IsPlaylistPlayback`, `AutoResumeEnabled` | `AdvancePlaylistEntryEffect` → `play/advance` → Folgeeintrag | Nächster Playlist-Eintrag startet; `PlaylistLabel` zeigt Position | `AppUpdateTests_PlaylistPlayback` (bereits) + `build-tvos` |
| 7 | Fehler-Rückfall | `PlaybackFailedMessage`/nicht auflösbare Quelle | Folgeeintrag vorhanden | Fehler → `AdvanceToNextEpisode`/`AdvancePlaylistEntry`; sonst Fehler-Alert | Kein Hängen; Folge oder verständliche Fehleranzeige mit Schließen | `AppUpdateTests_Player` (bereits) + `build-tvos` |
| 8 | Spurwahl | Stream mit Ton-/Untertitelgruppen | HLS-Stream mit `AVMediaSelectionGroup`s | System-Info-Panel des `AVPlayerViewController` | Spurwahl verfügbar; ohne Gruppen entfällt sie | `build-tvos` (Kompiliernachweis) |
| 9 | Schließen | Menü-Taste | Laufende Wiedergabe | `ViewDidDisappear` (dismissed) → `PlayerClosedMessage` → Abschluss-Fortschritt | Player schließt; Endposition wird gemeldet; `PlayerState` leer | `build-tvos` + `AppUpdateTests_Player` (bereits) |

## Offene Punkte

Keine.
