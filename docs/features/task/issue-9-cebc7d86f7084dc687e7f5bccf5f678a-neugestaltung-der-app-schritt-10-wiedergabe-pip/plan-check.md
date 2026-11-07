# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Player-Seite + Route `player`, Navigation von MediaDetailsPage („Abspielen") | `PlayerPage` neu, `AppShell`-Route, „Abspielen"-Buttons, `NavigateToPlayerEffect` (Ablauf „Wiedergabe starten", Schritte 5/6/7) | `PlayRequested_ValidMovie_SetsPlayerStateAndEffects`, `PlayerPage_RouteRegistered`, `MediaDetailsPage_HasPlayButton` | Abgedeckt |
| Wiedergabe via `CommunityToolkit.Maui.MediaElement` (stabil, ≥ 7 Tage, `UseMauiCommunityToolkitMediaElement`) | Paket 10.0.0 + Builder-Aufruf (Schritt 1) | `MauiApp_UsesMediaElement` | Abgedeckt |
| Server-Streams über `api/items/{typ}/{id}/stream` mit Token | `PlaybackSourceService` (Stream-URL + Token, Programmablauf/Dienst) | `ResolveAsync_Movie_ReturnsStreamUrlWithToken` | Abgedeckt |
| Lokale Dateien über saubere Quellenabstraktion | `PlaybackSource`/`PlaybackSourceKind.LocalFile` + `download_manifest`-Prüfung | `ResolveAsync_CompletedDownload_ReturnsLocalFile` | Abgedeckt |
| Fortsetzungsposition beim Start | `PlaybackSource.ResumePosition` aus Server-Liste/Cache, `SeekTo` bei `MediaOpened` | `ResolveAsync_Episode_UsesServerResumePosition`, `ResolveAsync_Offline_UsesCachedResumePosition` | Abgedeckt |
| Fortschrittsmeldung online sofort / offline verlustfrei in `pending_progress`, Nachsendung bei Online-Rückkehr | `PlaybackProgressMessage`-Takt → `ReportPlaybackProgressEffect` → `ProgressReportService` (bestehend, Drain via `ServerSyncService`) | `PlaybackProgress_ReportsChangedPosition` + bestehende `ProgressReportServiceTests`/`PendingProgressSyncServiceTests`/`ServerSyncServiceTests` | Abgedeckt |
| „Angesehen" ab `Dauer - WatchedEndWindowSeconds` | `UpdatePlaybackProgress`/`PlaybackEndedMessage` setzen `IsWatched` | `PlaybackProgress_MarksWatchedWithinWindow`, `PlaybackEnded_MarksWatchedAndAdvances` | Abgedeckt |
| Auto-Fortsetzung: Schalter + Restlaufzeit < `AutoResumeRemainingSeconds` → nächste Episode | `UpcomingEpisodes` + `AdvanceToNextEpisode` in Takt und Ende | `PlaybackProgress_AutoResumeAdvancesWhenEnabled` (an/aus/Fenster/keine Episode) | Abgedeckt |
| Quelle nicht verfügbar → Fehlermeldung + Rückfall auf nächste Episode | `PlaybackSourceResolvedMessage(Error)`/`PlaybackFailedMessage` → Error + Folge-Episode; ohne Folge-Episode bleibt Fehlerzustand | `PlaybackSourceResolved_Error_FallsBackToNextEpisode`, `…_ErrorWithoutNext_StaysOnError`, `PlaybackFailed_ShowsErrorAndFallsBack` | Abgedeckt |
| PiP iOS (nativ) | `AVPlayerViewController`-PiP der Transportsteuerung + `UIBackgroundModes=audio` (Machbarkeit in inventory/pip-machbarkeit.md verifiziert) | `IosInfoPlist_DeclaresAudioBackgroundMode`, Contract `ShouldShowPlaybackControls` | Abgedeckt |
| PiP Windows (kompaktes Overlay-/Mini-Player-Fenster) | `IPictureInPictureService`/`PictureInPictureService` mit `AppWindow`-`CompactOverlay`, PiP-Schalter + Overlay-Ausblendung | `PictureInPicture_TogglesFlag`, Contract-Service-Test | Abgedeckt |
| MVU-State `PlayerState` (Position, Dauer, Pufferung, „angesehen", nächste Episode, Fehler) + Messages/Effekte + `AppUpdate`-Übergänge | Schritte 2 + 4, alle Records/Effekte gelistet | Gesamte `AppUpdateTests_Player`-Tabelle | Abgedeckt |
| Einstellungen `AutoResumeEnabled`, `AutoResumeRemainingSeconds`, `WatchedEndWindowSeconds` verwendet, keine neuen | `model.Settings` in der Update-Logik, Konfigurationsabschnitt | Auto-Fortsetzungs- und „Angesehen"-Tests je mit Schalter/Fenster-Variationen | Abgedeckt |
| Kein zweites Warteschlangensystem für Fortschritte | ausschließlich `ReportPlaybackProgressEffect` → `ProgressReportService` | `PlaybackProgress_ReportsChangedPosition` (Effekt statt Direktaufruf) | Abgedeckt |
| UI-Konventionen (klare Titel, Steuerung, Fehler, PiP-Affordanz; keine internen IDs) | `PlayerPage` mit Titel-/Fehler-/Lade-Overlays, `ShouldShowPlaybackControls`; „Abspielen"-Buttons statt ID-Eingabe | Usability-Review (Pflicht, Schritt 8) + `PlayerContractTests` | Abgedeckt |
| Enum-Werte textuell in Tests abgedeckt (Hook) | `EnumCoverageTests` erweitern | `PlaybackSourceKind_AllValuesAreCovered` | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| „Abspielen" → Player öffnet + Quelle | Dispatch-Kette `PlayRequestedMessage` → Model/Effekte (`AppUpdateTests_Player`) + Route-Vertrag (`PlayerContractTests`) | Abgedeckt — keine UI-Testinfrastruktur vorhanden (inventory/tests.md); Fluss-Nachweis über die vollständige MVU-Kette wie in Schritten 7–9 üblich |
| Fortschritt online/offline | `PlaybackProgress_*`-Tests + bestehende `ProgressReportService`/Drain-Tests | Abgedeckt |
| „Angesehen"/Auto-Fortsetzung | `AppUpdateTests_Player` (Fenster/Schalter) | Abgedeckt |
| Fehler → Rückfall nächste Episode | `PlaybackSourceResolved_Error_*`, `PlaybackFailed_*` | Abgedeckt |
| PiP (Windows/iOS) | Toggle-Effekt/Flag-Test + Plattform-Verträge (CompactOverlay-Code, `UIBackgroundModes`) | Abgedeckt mit Begründung: PiP selbst ist Plattform-UI — Gerätetest nicht automatisierbar; Verdrahtung und Voraussetzungen testbar |

## Hinweise

- Keine E2E-/Geräte-Testinfrastruktur im Repo (inventory/tests.md) —
  Benutzerflüsse werden über die komplette MVU-Dispatch-Kette und
  Quelltext-Vertragstests abgesichert; das ist die etablierte
  Nachweisführung der Schritte 7–9.
- PiP auf iOS/Windows ist in `inventory/pip-machbarkeit.md` gegen die
  installierte Toolkit-Version 10.0.0 verifiziert (AVPlayerViewController
  bzw. AppWindow-Presenter) — Plan-Voraussetzung erfüllt.
