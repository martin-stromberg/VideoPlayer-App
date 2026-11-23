# Abnahmeprüfung – Entwicklungsschritt 7

## Ergebnis

**Status:** Anforderung erfüllt – externe Verifikation ausstehend

## Abweichungen

Keine.

## Ausstehende externe Verifikationen

- **Laufzeitverhalten auf tvOS-Simulator/-Gerät:** Tatsächliches
  Abspielen von Server-Stream und lokaler Datei, das Anspringen der
  Fortsetzungsposition, das Erscheinen der systemeigenen Ton-/
  Untertitel-Spurwahl im Pull-down-Infobereich (nur bei angebotenen
  Spurgruppen), die Transport-Bar-Menüeinträge für Playlist
  next/previous, die Menü-Tasten-Rückkehr sowie die Auto-Fortsetzung
  am Ende sind auf Code-Ebene, über Contract-Tests und den
  macOS-CI-Kompiliernachweis verifiziert — nicht per Laufzeitnachweis.
  Im Verifikationsregister `pending-verification.md` (Zeile
  „2–laufend") erfasst.
- Der macOS-CI-Nachweis selbst ist erbracht: Run `42127417197`
  (`build-tvos` 7m27s + `verify` 6m32s, beide grün) auf `headSha`
  `f49453453fdb6102df79f30cf3874dd9520e29b0` = aktueller HEAD = origin;
  keine ungepushten Commits (`git log origin/<branch>..HEAD` leer),
  Arbeitsbaum sauber.

## Hinweise

Prüfung anhand des Diffs
`task/issue-86-...-apple-tv-app-tvos-lean-back-pl...HEAD` (14
Code-/Dokument-Dateien ohne `.work`-Prozesslogs, +~1200/−180) und
Lesen der Quelldateien samt der plattformneutralen MVU-Gegenseite
(`AppUpdate`, `PlaybackSourceService`, `ProgressReportService`,
`UpcomingEpisodesService`, `PlaylistService`, `DownloadService`).

**(a) Quellauflösung mit lokaler-Datei-Präferenz + Fortsetzungsposition:**
`ResolvePlaybackSourceEffect` → `TvosAppEffectHandler.ResolvePlaybackSourceAsync`
(`TvosAppEffectHandler.cs` Z. 910–947) → `PlaybackSourceService.ResolveAsync`
(`PlaybackSourceService.cs` Z. 99–174): ein fertiger Download
(`download_manifest`, `state=completed`, `fileExists`-Prüfung) hat
Vorrang vor dem Server-Stream (`api/items/{typ}/{id}/stream` mit
`access_token`); die Fortsetzungsposition stammt online aus der
Weiterschauen-Liste, sonst aus `cached_lists`, und wird durch
`resumePositionSecondsOverride` (`StartPositionSeconds` der
Playlist-Antworten) verbindlich überschrieben; `allowServerStream =
!IsOffline` unterbindet unerreichbare Stream-URLs im Offline-Zustand.
Ergebnis als `PlaybackSourceResolvedMessage` →
`UpdatePlaybackSourceResolved` (`AppUpdate.cs` Z. 3707–3742) setzt
`Player.Source` bzw. fällt auf den nächsten Eintrag zurück.

**(b) Seek erst bei ReadyToPlay:** Die Fortsetzungsposition wird beim
Quellenwechsel in `pendingResumePosition` gemerkt
(`TvosPlayerViewController.cs` Z. 266) und ausschließlich im
`AVPlayerItemStatus.ReadyToPlay`-Zweig des Item-`status`-KVO per
`Player.Seek` mit Completion-Callback angewendet (Z. 387–434); der
Seek ist einmalig (`pendingResumePosition = null` vor dem Aufruf),
ein Fehlschlag ist nicht-fatal.

**(c) 5-s-Takt mit appliedMediaId-Bindung:** `ProgressTickInterval =
5 s` (Z. 38) via `AddPeriodicTimeObserver` auf `DispatchQueue.MainQueue`
(Z. 116–119) → `ReportPosition` → `PlaybackProgressMessage(Position,
Duration, IsPlaying, IsBuffering, appliedMediaId)` (Z. 461–466);
`appliedMediaId` wird erst nach erfolgreichem
`ReplaceCurrentItemWithPlayerItem` + `Play()` umgeschwenkt (Z. 292–294)
und beim Leerpfad auf 0 zurückgesetzt; der Takt meldet nur während
`TimeControlStatus == Playing`, Zustandswechsel kommen über den
`timeControlStatus`-KVO bzw. `PlaybackStalled` sofort herein. Der
MVU-Kern verwirft Takte abweichender `MediaId` (`AppUpdate.cs`
Z. 3831–3838) — MAUI-Muster `appliedMediaId`.

**(d) Fortschrittsmeldung Server/Offline-Queue:**
`ReportPlaybackProgressEffect` → `ReportPlaybackProgressAsync` →
`ProgressReportService.ReportProgressAsync` (`ProgressReportService.cs`
Z. 58–139): online direkt an `ReportPlaybackProgressAsync` des
API-Clients (inkl. `playlistId`); offline, ohne Client oder bei
überholbarem Fehler persistente Einreihung in `pending_progress`
(`PendingProgressSyncService`); permanente 4xx-Fehler (außer
401/408/429) werden verworfen. Zusätzlich meldet `UpdatePlayerClosed`
(`AppUpdate.cs` Z. 4174–4197) den Abschluss-Fortschritt beim
Verlassen.

**(e) „Angesehen"-Endfenster im Modell:** Die Schwelle liegt
vollständig im plattformneutralen `EvaluateEndWindow` (`AppUpdate.cs`
Z. 3905–3958): `WatchedEndWindowSeconds`-Fenster plus bestätigte
`EndWindowStreak` (≥ 2 aufeinanderfolgende plausible Takte,
`IsProgressTickPlausible` Z. 4080–4086); der tvOS-Player enthält
keinerlei Schwellenlogik — er ist reiner Ereignis-Übersetzer.

**(f) Plausibilisiertes Ende mit Toleranz:** `IsPlaybackEndPlausible`
(`AppUpdate.cs` Z. 4060–4066) verlangt positive Dauer und Restlaufzeit
≤ `PlaybackEndedToleranceSeconds` (300 s, Z. 89); verfrühte/verspätete
Ende-Ereignisse werden verworfen. Der Player unterscheidet sauber:
`ObserveDidPlayToEndTime` → `PlaybackEndedMessage`,
`ObserveItemFailedToPlayToEndTime` und `status == Failed` →
`PlaybackFailedMessage` (`TvosPlayerViewController.cs` Z. 342–361,
387–401).

**(g) Auto-Fortsetzung Serie + Playlist next/previous gemäß Einstellung:**
`UpdatePlaybackEnded` (`AppUpdate.cs` Z. 3978–4047) wechselt nur bei
`model.Settings.AutoResumeEnabled` — im Playlist-Kontext über
`AdvancePlaylistEntry` → `AdvancePlaylistEntryEffect` →
`PlaylistService.AdvanceAsync` (advance-Endpunkt), sonst über
`AdvanceToNextEpisode` aus der `UpcomingEpisodes`-Liste
(`LoadUpcomingEpisodesEffect` → `UpcomingEpisodesService.LoadAsync` →
`UpcomingEpisodesLoadedMessage`). Manuelle next/previous-Navigation
liegt als `TransportBarCustomMenuItems` („Vorheriger/Nächster Eintrag",
`TvosPlayerViewController.cs` Z. 180–211) nur im Playlist-Kontext vor
und dispatcht `PlaylistPrevious/NextRequestedMessage`; deaktiviert
über `CanNavigateNext`/`CanNavigatePrevious`; Ergebnis-Weg
`PlaylistNavigationCompletedMessage` →
`UpdatePlaylistNavigationCompleted` (Z. 2556–2617) mit
Erschöpfungs-Flag und Identitäts-Guard.

**(h) Fehler-Rückfall nächster Eintrag:** `UpdatePlaybackFailed`
(`AppUpdate.cs` Z. 4140–4157) und der Fehlschlag-Pfad von
`UpdatePlaybackSourceResolved` (Z. 3733–3741) fallen auf
`AdvancePlaylistEntry` (Playlist) bzw. `AdvanceToNextEpisode` (Serie)
zurück — ohne Folge-Eintrag bleibt der Fehlerzustand sichtbar
(`UIAlertController`, `TvosPlayerViewController.cs` Z. 232–252); die
Anzeige wird während laufender Rückfall-Auflösung (`IsResolving`)
unterdrückt.

**(i) Download-Schutz (`ProtectDownloadExpiry`):** Beim ersten Takt
mit bekannter Dauer einer `LocalFile`-Quelle feuert der Kern einmalig
`ProtectDownloadExpiryEffect` (Z. 3861–3878, Flag
`DownloadExpiryProtected`) → `Downloads.ExtendExpiryAsync`
(Ablaufdatum auf ~„jetzt + 2 × Restdauer"); zusätzlich ist
`ExpireWatchedDownloadEffect` verdrahtet (`ExpireAfterAsync`) und die
Manifest-Ereignisse `ItemChanged`/`ItemRemoved` melden Änderungen in
den MVU-Zyklus zurück (`TvosAppEffectHandler.cs` Z. 76–81,
1043–1048).

**(j) Spurwahl nur bei angebotenen Spuren:** Es gibt bewusst keinerlei
manuelle `AVMediaSelectionGroup`-Verdrahtung — einzig ein
Dokumentationskommentar nennt sie (Z. 91–95). Die systemeigene
Ton-/Untertitel-Auswahl des `AVPlayerViewController` erscheint im
Pull-down-Infobereich automatisch, sofern der Stream Spurgruppen
anbietet (z. B. HLS); `CustomInfoViewControllers = [infoPage]` ergänzt
den eigenen Info-Tab *neben* den System-Tabs (tvOS 15-API,
`SupportedOSPlatformVersion` = 15.0). Kein zusätzlicher
Server-Vertrag — sonst entfällt die Auswahl ersatzlos.

**(k) Kein Inhalts-Schreibzugriff:** Der Diff fügt ausschließlich
lesende Effekt-Ausführungen plus lokale Manifest-Ablaufänderungen
hinzu (`ExtendExpiryAsync`/`ExpireAfterAsync` schreiben nur das lokale
Download-Manifest, keinen Server-Inhalt); der Playlist-`advance`-Ruf
ist der bestehende serverseitige Wiedergabe-Cursor des
Wiedergabevertrags, keine Inhaltsverwaltung. Die Schutz-Contract-Tests
(`TvOsEffectHandler_DoesNotWireContentMutations`,
`TvOsEffectHandler_DoesNotWireExcludedAreas`) bleiben grün.

**(l) Tests:** `dotnet test VideoPlayer.Maui.Tests` — **1492/1492
bestanden, 0 Fehler** (54 s). Neue `TvOsPlaybackContractTests`
(5 Tests) prüfen Quellanwendung/Resume-Seek, Ereignis-Übersetzung
(Takt/Ende/Fehler/Lebenszyklus), Playlist-Transport-Menü + Info-Tab,
modale Player-Präsentation und die vollständige Effekt-/Kompositions-
Verdrahtung; `TvOsHomePageContractTests`/`TvOsDetailsContractTests`
wurden von den Platzhalter-Assertionen auf die reale Verdrahtung
umgestellt.

**(m) tvOS-Kompilierfallen:** Die beiden CI-Fix-Commits verifiziert —
`b464b62` (KVO-Observer-Rückgabe korrekt als `IDisposable` statt
`NSObject` typisiert; `UIAction`-Handler als Block; Fehler-Ende
zunächst via `NSNotificationCenter`), `f494534` (finaler Binding-Name
`AVPlayerItem.Notifications.ObserveItemFailedToPlayToEndTime` mit
`args.Error`; `action =>`-Parameter statt Discard). Stichprobe der
übrigen heiklen Signaturen sauber: `AddPeriodicTimeObserver` →
`NSObject` mit `RemoveTimeObserver`, `AddObserver` → `IDisposable`,
`Seek(CMTime, CMTime, CMTime, Action<bool>)`, `UIAlertController`/
`UIAlertAction`-Overloads, `CustomInfoViewControllers`/
`TransportBarCustomMenuItems` (tvOS 15-APIs innerhalb der
Mindestversion 15.0). Endgültiger Nachweis: `build-tvos` auf HEAD
grün (Run `42127417197`).

**(n) CI/Push:** `env -u GH_TOKEN gh run view 42127417197 --repo
martin-stromberg/VideoPlayer-App` → `status: completed`,
`conclusion: success`, `headBranch` = Feature-Branch, `headSha`
`f494534...` = HEAD = `origin/<branch>`; `git status` sauber, keine
ungepushten Commits.

**Weitere Beobachtungen (keine Abweichung):**

- Die Fix-Commits `b464b62`/`f494534` committeten zusätzlich ~90
  `.work/e2e-process-logs/*/process.json`-Einzeiler — Werkzeug-
  Ablage im Diff, fachlich ohne Belang.
- `TvOsEffectHandler_DoesNotWireExcludedAreas` assertiert weiterhin
  `DoesNotContain("DownloadService")` und bleibt grün, weil der
  Handler ausschließlich über die Property `services.Downloads`
  zugreift (Typname taucht nicht im Quelltext auf) — funktional
  korrekt, aber fragile Assertion; kein Handlungsbedarf.
- KVO-Callbacks (`status`, `timeControlStatus`) können prinzipiell
  auf Nicht-Hauptthreads eintreffen: `dispatch` läuft über
  `MainThreadInvoker` (hauptthreadsicher), `Seek` ist thread-sicher;
  `pendingResumePosition` wird faktisch nur aus ApplySource (Hauptthread)
  und dem Item-KVO gelesen/geschrieben — Restrisiko eines
  Laufzeit-Themas, unter Simulator-/Geräte-Verifikation einsortiert.
- `PreparedTargetViewController`/`PresentPreparedTarget` wurden
  entfernt — alle Navigationsziele (Detail, Wiedergabe) sind real;
  ein verbliebener „Muster"-Kommentar in
  `SourceBrowserViewController.cs` Z. 202 ist harmlos.
- `ExpireWatchedDownloadEffect` ist verdrahtet, obwohl „nach dem
  Abspielen automatisch löschen" laut Plan auf tvOS nicht angeboten
  wird — der Effekt ist durch `Settings.AutoDeletePlayedDownloads`
  gegattert und die Einstellung hat auf tvOS keine Oberfläche;
  defensive Vollständigkeit ohne Oberflächenwirkung, keine Abweichung.
- Hilfe-Datei `docs/help/tv-app/beschreibung.md`, `README.md` und
  `changes.log` wurden konsistent auf die reale Wiedergabe
  umgestellt; `test-results.md`/`review.md`/`review-code.md`/
  Tasks-Datei des Schritts sind vorhanden und plausibel.
- Bekannte NU1903-Warnung (`Microsoft.AspNetCore.SignalR.Common`
  10.0.10) ist Altbestand und nicht Gegenstand dieses Schritts.
