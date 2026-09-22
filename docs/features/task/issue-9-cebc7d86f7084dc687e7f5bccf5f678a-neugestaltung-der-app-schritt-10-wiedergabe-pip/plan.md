# Umsetzungsplan: Videowiedergabe mit Picture-in-Picture und Fortschrittsverfolgung (Entwicklungsschritt 10)

## Übersicht

Die App erhält eine eigene Wiedergabeseite `PlayerPage` (Shell-Route
`player`, geschoben auf den Stack) mit `MediaElement` aus
`CommunityToolkit.Maui.MediaElement` 10.0.0 (stabil, seit ~03.08.2026
verfügbar, bereits vom Altbestand genutzt). PiP läuft auf iOS nativ
über den `AVPlayerViewController` des MediaElement (Transport-
steuerung enthält die PiP-Schaltfläche; `UIBackgroundModes=audio` wird
ergänzt) und auf Windows über den `CompactOverlay`-Presenter des
`AppWindow` (Mini-Player). Fortschritte speisen in den bestehenden
`ProgressReportService` (online sofort, offline über
`pending_progress`, Drain bei Online-Rückkehr bereits vorhanden).
„Angesehen"-Schwelle, Auto-Fortsetzung und Fehler-Rückfall auf die
nächste Episode sind reine MVU-Update-Logik über `AppModel.Settings`
und den Serienkontext aus `MediaDetailsView`.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Quellenabstraktion | `PlaybackSource` (Record) + `PlaybackSourceKind` (`ServerStream`, `LocalFile`) + `PlaybackSourceService` (Service Layer) in `VideoPlayer.Mvu` | Anforderung verlangt saubere Trennung für Schritt 11; der Dienst ist plattformneutral testbar (HTTP-Stub, In-Memory-Cache); `File.Exists` als injizierbare Prüfung |
| Lokale Dateien | `PlaybackSourceService` prüft zuerst `download_manifest` (bestehender `DownloadManifestEntry`, `State == "completed"`, Datei existiert) → `LocalFile`; sonst Server-Stream | Die Tabelle existiert seit Schritt 3/6; Schritt 11 befüllt sie — der Player kann lokale Dateien schon jetzt abspielen, sobald welche vorhanden sind |
| Nächste Episode | `PlayerState.UpcomingEpisodes` (geordnete `PlayerEpisodeRef`-Liste), aus `MediaDetailsView.Seasons` (staffelübergreifend, nach Nummern sortiert, alle Episoden nach der aktuellen) beim `PlayRequestedMessage` ermittelt | Serienkontext liegt bereits geladen vor; die Liste im State macht Rückfall und Auto-Fortsetzung reinen Update-Logik ohne Server-Roundtrip; Kette beliebig fortsetzbar |
| Fortschritts-Takt | Seiten-Timer (5 s, Muster `MediaBannerPlayer` des Altbestands) dispatcht `PlaybackProgressMessage` nur solange `CurrentState == Playing`; Update erzeugt `ReportPlaybackProgressEffect` bei geänderter Position (> 0,5 s) | Wiederverwendbares, bewährtes Muster; Throttling im State (`LastReportedPositionSeconds`) verhindert Redundanz |
| „Angesehen" | `IsWatched` in `PlayerState`: gesetzt, sobald `Position >= Duration - WatchedEndWindowSeconds` (Duration > 0); bei `MediaEnded`/`Close` Abschlussmeldung mit `Position = Duration` | Direkte Abbildung der Anforderung auf das geladene `AppSettings`-Fenster |
| Auto-Fortsetzung | Bei jedem Takt: `AutoResumeEnabled && UpcomingEpisodes.Any() && Duration - Position <= AutoResumeRemainingSeconds` → nächste Episode laden (wie Fehler-Rückfall); bei `MediaEnded` greift dieselbe Bedingung (Restlaufzeit 0) | Entspricht „greift, wenn die Restlaufzeit das Zeitfenster unterschreitet"; Schalter und Fenster aus `AppSettings` |
| Fehler-Rückfall | Quellauflösung fehlgeschlagen oder `MediaFailed` → `Error` setzen (Anzeige als Banner) und — falls `UpcomingEpisodes` nicht leer — sofort die nächste Episode laden | Wortlaut der Anforderung; ohne Folge-Episode bleibt die Fehleranzeige stehen |
| PiP iOS | Nativ über `AVPlayerViewController` (`AllowsPictureInPicturePlayback` ist Standard `true`; Transportsteuerung sichtbar via `ShouldShowPlaybackControls`); zusätzlich `UIBackgroundModes` `audio` in `Info.plist` | Machbarkeit verifiziert (inventory/pip-machbarkeit.md): Toolkit 10.0.0 hostet `AVPlayerViewController` — kein eigener Plattformcode nötig |
| PiP Windows | `IPictureInPictureService` am App-Rand (`#if WINDOWS`): `AppWindow.SetPresenter(CompactOverlay)` ↔ `Default`; die Seite blendet bei aktivem PiP alle Bedienelemente außer dem MediaElement aus | „Kompaktes Overlay-/Mini-Player-Fenster" ist die dokumentierte WinAppSDK-Lösung; `WindowsPackageType=None` ist unkritisch |
| PiP-Schalter | Sichtbar nur auf Windows (`service.IsToggleSupported`); iOS nutzt die PiP-Schaltfläche der nativen Transportsteuerung | iOS bietet PiP bereits plattformseitig in den Controls — ein zweiter Schalter wäre redundant/untrennbar |
| Player-State-Lebenszyklus | `Player = PlayerState.Empty` bei `PlayerClosedMessage` (Seite verlassen) und `LogoutRequestedMessage` | Sitzungs-/Seitenwechsel darf keinen Wiedergaberest zurücklassen; Abschluss-Fortschritt wird vor dem Leeren gemeldet |

## Programmabläufe

### Wiedergabe starten (Film oder Episode)

1. `MediaDetailsPage` zeigt „Abspielen" im Kopfbereich bei
   abspielbaren Medientypen (`movie`, `tvshowepisode`; Titel bei
   Episoden: `HighlightedEpisodeTitle ?? Title`) sowie eine
   ▶-Schaltfläche je Episoden-Eintrag der Episodenliste.
   Beide dispatcht `PlayRequestedMessage(mediaType, mediaId, title)`.
2. `AppUpdate`: Guard (`MediaId <= 0` oder Typ nicht `movie`/
   `tvshowepisode` → unverändert). Sonst `Player = new PlayerState
   { …, IsResolving = true, UpcomingEpisodes = <aus
   MediaDetails.Details ermittelt> }`, Effekte
   `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`.
3. `ShellAppEffectHandler`: `GoToAsync("player")`; danach
   `PlaybackSourceService.ResolveAsync` → Ergebnis als
   `PlaybackSourceResolvedMessage` zurück.
4. `PlayerPage.OnAppearing` dispatcht `PlayerPageOpenedMessage`
   (einmalig; setzt `Attempted`), `ModelChanged` setzt
   `MediaElement.Source` (`FromUri` bei `ServerStream`, `FromFile`
   bei `LocalFile`); nach `MediaOpened` einmalig `SeekTo(ResumePosition)`.

Beteiligte Klassen/Komponenten: `MediaDetailsPage`, `AppUpdate`,
`PlayerState`, `ShellAppEffectHandler`, `PlaybackSourceService`,
`PlayerPage`, `MediaElement`.

### Fortschritt melden (online/offline)

1. `PlayerPage` startet einen 5-s-`IDispatcherTimer`; solange
   `MediaElement.CurrentState == Playing` wird
   `PlaybackProgressMessage(position, duration, isPlaying, isBuffering)`
   dispatcht (zusätzlich bei `StateChanged`).
2. `AppUpdate` übernimmt `Position`/`Duration`/`IsPlaying`/
   `IsBuffering` und erzeugt `ReportPlaybackProgressEffect`, wenn
   sich die Position seit der letzten Meldung um > 0,5 s geändert
   hat (`LastReportedPositionSeconds`).
3. `ShellAppEffectHandler` reicht an
   `ProgressReportService.ReportProgressAsync` weiter — online
   sofort, sonst `pending_progress` (Drain bei Online-Rückkehr über
   `RunFullSyncEffect`/`FlushPendingProgressAsync` existiert).
4. `PlayerClosedMessage` (OnDisappearing) und `PlaybackEndedMessage`
   erzeugen eine Abschlussmeldung (bei `MediaEnded` mit
   `Position = Duration`).

Beteiligte Klassen/Komponenten: `PlayerPage`, `AppUpdate`,
`ReportPlaybackProgressEffect`, `ShellAppEffectHandler`,
`ProgressReportService`, `PendingProgressSyncService`,
`ServerSyncService`.

### „Angesehen" und Auto-Fortsetzung

1. In `UpdatePlaybackProgress`: `Duration > 0 && Position >=
   Duration - TimeSpan.FromSeconds(Settings.WatchedEndWindowSeconds)`
   → `IsWatched = true` (Anzeige „Angesehen" im State).
2. Dieselbe Stelle prüft die Auto-Fortsetzung:
   `Settings.AutoResumeEnabled && UpcomingEpisodes.Count > 0 &&
   Duration - Position <= TimeSpan.FromSeconds(Settings.
   AutoResumeRemainingSeconds)` → Wechsel zur nächsten Episode
   (State auf die nächste `PlayerEpisodeRef` umsetzen,
   `ResolvePlaybackSourceEffect`), ohne die gemerkte
   `UpcomingEpisodes`-Restliste zu verlieren.
3. `PlaybackEndedMessage` setzt `IsWatched` und löst — bei
   aktivierter Auto-Fortsetzung und vorhandener nächster Episode —
   denselben Wechsel aus (Restlaufzeit 0 < Fenster).

Beteiligte Klassen/Komponenten: `AppUpdate`, `AppSettings`,
`PlayerState`, `ResolvePlaybackSourceEffect`.

### Quelle nicht verfügbar → Fehler + Rückfall

1. `PlaybackSourceResolvedMessage` mit `Error` bzw.
   `PlaybackFailedMessage` (MediaElement `MediaFailed`):
   `Player.Error = "Das Video kann nicht abgespielt werden."`.
2. Sind `UpcomingEpisodes` vorhanden: State wechselt zur ersten
   wartenden Episode (`MediaType = "tvshowepisode"`, `MediaId`,
   `Title`; `IsResolving = true`, `Source = null`, Position/Duration
   zurückgesetzt, `IsWatched = false`, `Error` bleibt als Hinweis
   sichtbar) + `ResolvePlaybackSourceEffect`. Ohne Folge-Episode
   bleibt der Fehlerzustand stehen (`IsResolving = false`).

Beteiligte Klassen/Komponenten: `AppUpdate`, `PlayerPage`
(Fehlerbanner), `PlaybackSourceService`.

### Picture-in-Picture

1. Windows: PiP-Schalter der `PlayerPage` dispatcht
   `PictureInPictureRequestedMessage` → `TogglePictureInPictureEffect`
   → `IPictureInPictureService.ToggleAsync()` schaltet
   `AppWindow.Presenter` zwischen `CompactOverlay` (Mini-Player) und
   `Default` um und meldet `PictureInPictureChangedMessage(isActive)`.
2. `AppUpdate` setzt `Player.IsPictureInPicture`; die Seite blendet
   bei aktivem PiP Titel-/Fehler-/Schalter-Overlays aus (nur
   MediaElement sichtbar).
3. iOS: PiP-Schaltfläche der nativen Transportsteuerung des
   `AVPlayerViewController`; `UIBackgroundModes=audio` in
   `Info.plist` hält die Wiedergabe im PiP-/Hintergrundmodus aktiv.

Beteiligte Klassen/Komponenten: `PlayerPage`, `AppUpdate`,
`TogglePictureInPictureEffect`, `IPictureInPictureService`/
`PictureInPictureService`, `Platforms/iOS/Info.plist`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `PlaybackSourceKind` | Enum (`VideoPlayer.Mvu`) | `ServerStream`, `LocalFile` — Quellenart der Wiedergabe |
| `PlaybackSource` | Record (`VideoPlayer.Mvu`) | Aufgelöste Quelle: `Kind`, `Location` (URL/Pfad), `ResumePosition` (`TimeSpan?`) |
| `PlayerEpisodeRef` | Record (`VideoPlayer.Mvu`) | Verweis auf eine Episode (`MediaId`, `Title`) für die Fortsetzungsliste |
| `PlayerState` | Record (`VideoPlayer.Mvu`) | Zustand der Wiedergabeseite: Medium, Quelle, Position, Dauer, `IsPlaying`, `IsBuffering`, `IsResolving`, `IsWatched`, `IsPictureInPicture`, `UpcomingEpisodes`, `Error`, `LastReportedPositionSeconds`, `Attempted`, `Empty` |
| `PlaybackSourceService` | Klasse (`VideoPlayer.Mvu`) | Löst Medium → `PlaybackSource` auf: `download_manifest` (LocalFile) → Stream-URL + Token (`AuthSessionService`/`AppSecretsService`); Fortsetzungsposition aus `api/continue-watching` bzw. Cache `cached_lists["continue_watching"]` |
| `IPictureInPictureService` | Interface (`VideoPlayer.Maui/Services`) | `IsToggleSupported`, `ToggleAsync()` — PiP-Umschaltung |
| `PictureInPictureService` | Klasse (`VideoPlayer.Maui/Services`) | Windows-Implementierung über `AppWindow`-Presenter (`#if WINDOWS`); andere Plattformen: `IsToggleSupported = false` |
| `PlayerPage` | ContentPage (`VideoPlayer.Maui/Pages`) | Wiedergabeseite mit `MediaElement`, Titel-/Fehler-Overlays, PiP-Schalter, Positionstakt |
| `PlayRequestedMessage` u. a. | Records (`AppMessage.cs`) | `PlayRequestedMessage`, `PlayerPageOpenedMessage`, `PlaybackSourceResolvedMessage`, `PlaybackProgressMessage`, `PlaybackEndedMessage`, `PlaybackFailedMessage`, `PictureInPictureRequestedMessage`, `PictureInPictureChangedMessage`, `PlayerClosedMessage` |
| `NavigateToPlayerEffect` u. a. | Records (`AppEffect.cs`) | `NavigateToPlayerEffect`, `ResolvePlaybackSourceEffect`, `ReportPlaybackProgressEffect`, `TogglePictureInPictureEffect` |

## Änderungen an bestehenden Klassen

### `AppModel` (Record)

- **Neue Eigenschaft:** `Player` (`PlayerState`, Startwert
  `PlayerState.Empty`) — Zustand der Wiedergabeseite.

### `AppUpdate` (statische Klasse)

- **Neue Übergänge:** siehe Programmabläufe — `UpdatePlayRequested`,
  `UpdatePlayerPageOpened`, `UpdatePlaybackSourceResolved`,
  `UpdatePlaybackProgress`, `UpdatePlaybackEnded`,
  `UpdatePlaybackFailed`, `UpdatePictureInPictureRequested`,
  `UpdatePictureInPictureChanged`, `UpdatePlayerClosed` sowie die
  interne Hilfsfunktion `NextEpisodeQueue` (Serienkontext →
  `UpcomingEpisodes`) und `AdvanceToNextEpisode` (Rückfall/
  Fortsetzung).
- **Geänderte Methoden:** `UpdateLogoutRequested` — zusätzlich
  `Player = PlayerState.Empty` zurücksetzen (sitzungsbezogener
  Zustand).

### `MediaDetailsPage` (ContentPage)

- **Neue Elemente:** „Abspielen"-`Button` im Kopfbereich (sichtbar
  bei `movie`/`tvshowepisode`), ▶-`Button` je Episoden-Eintrag.
- **Neue Event-Handler:** `OnPlayClicked`,
  `OnEpisodePlayClicked` — dispatchen `PlayRequestedMessage`.

### `AppShell` (Shell)

- **Neue Route:** `Routing.RegisterRoute("player",
  typeof(Pages.PlayerPage))`.

### `ShellAppEffectHandler` (IAppEffectHandler)

- **Neue Fälle:** `NavigateToPlayerEffect` (`GoToAsync("player")`),
  `ResolvePlaybackSourceEffect` (`PlaybackSourceService` →
  `PlaybackSourceResolvedMessage`), `ReportPlaybackProgressEffect`
  (`ProgressReportService`), `TogglePictureInPictureEffect`
  (`IPictureInPictureService` → `PictureInPictureChangedMessage`).

### `MauiProgram`

- `builder.UseMauiCommunityToolkitMediaElement()`;
  `builder.Services.AddSingleton<PlaybackSourceService>()`;
  `builder.Services.AddSingleton<IPictureInPictureService,
  PictureInPictureService>()`;
  `builder.Services.AddTransient<Pages.PlayerPage>()`.

### `VideoPlayer.Maui.csproj`

- `PackageReference CommunityToolkit.Maui.MediaElement` 10.0.0.

### `Platforms/iOS/Info.plist`

- `UIBackgroundModes` → `audio` (PiP-/Hintergrund-Audio).

## Datenbankmigrationen

Keine — `download_manifest` und `pending_progress` existieren.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `PlayRequestedMessage` | `MediaId > 0` und `MediaType` ∈ {`movie`, `tvshowepisode`} | Nachricht wird ignoriert (unverändertes Model) |
| `PlaybackSource` | `Location` nicht leer | Quellauflösung meldet Fehler → Rückfall |
| Fortschrittsmeldung | `Duration > 0`, `Position >= 0` | Kein Report-Effekt |
| Stream-URL | `ClientBaseAddress`/Token vorhanden | Quelle „nicht verfügbar" → Fehler + Rückfall |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `CommunityToolkit.Maui.MediaElement` | NuGet | 10.0.0 | Wiedergabekomponente |
| `UIBackgroundModes` (`Info.plist`, iOS) | Plist-Array | `audio` | PiP-/Hintergrundwiedergabe |

Bestehende Einstellungen `AutoResumeEnabled`,
`AutoResumeRemainingSeconds`, `WatchedEndWindowSeconds` werden
verwendet — keine neuen Benutzereinstellungen.

## Seiteneffekte und Risiken

- **Windows-AppWindow:** `CompactOverlay` schaltet das ganze Fenster
  um — beim Verlassen der Player-Seite wird `Default`
  wiederhergestellt (Page `OnDisappearing` → Service-Reset; zudem
  Rücksprung via `PlayerClosedMessage`).
- **MediaElement unter `MauiXamlInflator=SourceGen`:** Toolkit-
  Controls sind quellgenerierungs-kompatibel (Altbestand nutzt sie
  bereits mit derselben Paketversion).
- **Stream-Token als Query-Parameter:** folgt dem Altbestand; das
  Token erscheint nicht in Logausgaben (nur Debug.WriteLine ohne
  URL).
- **Logout während der Wiedergabe:** `Player` wird zurückgesetzt;
  die Player-Seite wird beim Wechsel zur Anmeldemaske verworfen.
- **Kein Serienkontext** (z. B. direkter Film-Start oder veralteter
  `MediaDetails`-State): `UpcomingEpisodes` leer — Fehler zeigen nur
  die Meldung, kein Rückfall.

## Umsetzungsreihenfolge

1. **NuGet + Toolkit-Aktivierung**
   - Voraussetzungen: keine (Paket 10.0.0 im lokalen Cache vorhanden)
   - `VideoPlayer.Maui.csproj`: `CommunityToolkit.Maui.MediaElement`
     10.0.0; `MauiProgram`: `UseMauiCommunityToolkitMediaElement()`;
     `Info.plist` (iOS): `UIBackgroundModes` `audio`.

2. **MVU-Datentypen und Player-State**
   - Voraussetzungen: Schritt 1 nicht erforderlich
   - `PlaybackSourceKind`, `PlaybackSource`, `PlayerEpisodeRef`,
     `PlayerState`; `AppModel.Player`; Messages und Effekte in
     `AppMessage.cs`/`AppEffect.cs`.

3. **`PlaybackSourceService`**
   - Voraussetzungen: `AuthSessionService`, `AppSecretsService`,
     `CacheDatabase` (vorhanden), Schritt 2
   - Manifest-Prüfung (LocalFile), Stream-URL-Bildung
     (`{base}/api/items/{typ}/{id}/stream?access_token=…`),
     Fortsetzungsposition (Server → Cache-Fallback),
     injizierbare `fileExists`-Prüfung für Tests.

4. **Update-Übergänge**
   - Voraussetzungen: Schritt 2
   - Alle neuen Übergänge in `AppUpdate` inkl. `NextEpisodeQueue`/
     `AdvanceToNextEpisode`; `UpdateLogoutRequested` erweitert.

5. **`PlayerPage` + PiP-Dienst**
   - Voraussetzungen: Schritte 1–4
   - `PlayerPage.xaml(.cs)` mit `MediaElement`, Titel-/Fehler-/
     Lade-Overlays, Windows-PiP-Schalter, 5-s-Takt, `SeekTo`-
     Fortsetzung; `IPictureInPictureService`/`PictureInPictureService`
     (`#if WINDOWS` CompactOverlay); Route `player` in
     `AppShell.xaml.cs`; DI-Registrierungen.

6. **„Abspielen" auf der Detailseite**
   - Voraussetzungen: Schritt 2
   - `MediaDetailsPage`: Kopf-Button (`movie`/`tvshowepisode`) +
     ▶ je Episoden-Eintrag → `PlayRequestedMessage`.

7. **Effekt-Handler-Verdrahtung**
   - Voraussetzungen: Schritte 3–5
   - `ShellAppEffectHandler`: vier neue Fälle.

8. **Tests**
   - Voraussetzungen: Schritte 2–7
   - Neue Testdateien (s. u.); `EnumCoverageTests` erweitern;
     Contract-Tests für Route/Paket/Info.plist.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `PlayRequested_ValidMovie_SetsPlayerStateAndEffects` | `AppUpdateTests_Player` | State (Medium, Resolving, leere Upcoming), `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect` |
| `PlayRequested_Episode_BuildsUpcomingEpisodes` | `AppUpdateTests_Player` | Episoden nach der aktuellen über Staffeln hinweg sortiert |
| `PlayRequested_InvalidIdentity_Unchanged` | `AppUpdateTests_Player` | Guard: `MediaId <= 0` / nicht abspielbarer Typ |
| `PlaybackSourceResolved_Success_SetsSource` | `AppUpdateTests_Player` | Quelle übernommen, `IsResolving = false` |
| `PlaybackSourceResolved_Error_FallsBackToNextEpisode` | `AppUpdateTests_Player` | Fehler gesetzt + nächste Episode ladend (`ResolvePlaybackSourceEffect`) |
| `PlaybackSourceResolved_ErrorWithoutNext_StaysOnError` | `AppUpdateTests_Player` | Fehlerzustand ohne Folge-Episode |
| `PlaybackProgress_ReportsChangedPosition` | `AppUpdateTests_Player` | `ReportPlaybackProgressEffect` bei > 0,5 s Differenz; keine erneute Meldung unveränderter Position |
| `PlaybackProgress_MarksWatchedWithinWindow` | `AppUpdateTests_Player` | `IsWatched` ab `Duration - WatchedEndWindowSeconds`; nicht früher |
| `PlaybackProgress_AutoResumeAdvancesWhenEnabled` | `AppUpdateTests_Player` | Restlaufzeit < `AutoResumeRemainingSeconds` → nächste Episode (Schalter an/aus) |
| `PlaybackEnded_MarksWatchedAndAdvances` | `AppUpdateTests_Player` | `IsWatched`, Abschluss-Report (`Position = Duration`), Auto-Fortsetzung |
| `PlaybackFailed_ShowsErrorAndFallsBack` | `AppUpdateTests_Player` | Fehler + nächste Episode |
| `PlayerClosed_ReportsFinalAndResets` | `AppUpdateTests_Player` | Abschluss-Report + `PlayerState.Empty` |
| `PictureInPicture_TogglesFlag` | `AppUpdateTests_Player` | Effekt + `IsPictureInPicture` |
| `Logout_ResetsPlayer` | `AppUpdateTests_Player` | `Player` wird bei Abmeldung geleert |
| `ResolveAsync_Movie_ReturnsStreamUrlWithToken` | `PlaybackSourceServiceTests` | URL `{base}/api/items/movie/{id}/stream?access_token=…` |
| `ResolveAsync_Episode_UsesServerResumePosition` | `PlaybackSourceServiceTests` | `PositionSeconds` der Weiterschauen-Liste |
| `ResolveAsync_Offline_UsesCachedResumePosition` | `PlaybackSourceServiceTests` | Fortsetzung aus `cached_lists` ohne Client |
| `ResolveAsync_CompletedDownload_ReturnsLocalFile` | `PlaybackSourceServiceTests` | `download_manifest`-Treffer → `LocalFile` |
| `ResolveAsync_NoSource_ReturnsNull/Error` | `PlaybackSourceServiceTests` | weder Datei noch Server → nicht verfügbar |
| `PlaybackSourceKind_AllValuesAreCovered` | `EnumCoverageTests` | `ServerStream`, `LocalFile` textuell abgedeckt |
| `PlayerPage_RouteRegistered` | `PlayerContractTests` | `Routing.RegisterRoute("player", …)` in `AppShell.xaml.cs` |
| `MauiApp_UsesMediaElement` | `PlayerContractTests` | Paket-Referenz + `UseMauiCommunityToolkitMediaElement()` |
| `IosInfoPlist_DeclaresAudioBackgroundMode` | `PlayerContractTests` | `UIBackgroundModes`/`audio` für PiP |
| `MediaDetailsPage_HasPlayButton` | `PlayerContractTests` | „Abspielen"-Handler verdrahtet |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `EnumCoverageTests` | Neuer Enum `PlaybackSourceKind` — Erweiterung um Werte-Test |

### E2E-Tests (primärer Funktionsnachweis)

Das Projekt besitzt keine UI-/E2E-Testinfrastruktur (das Testprojekt
kann die MAUI-Zielframeworks nicht referenzieren; es existieren keine
Geräte-/UI-Tests — siehe inventory/tests.md). Der Benutzerfluss wird
daher über die vollständige MVU-Dispatch-Kette (Message → Model →
Effekte → Handler-Aufrufe) plus Quelltext-Contract-Tests der
Seiten/Routen abgesichert — dieselbe bewährte Nachweisführung wie in
den Schritten 7–9:

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | „Abspielen" auf Detailseite → Player öffnet, Quelle wird aufgelöst | `AppUpdateTests_Player` + `PlayerContractTests` | Navigation `player`, Quellauflösung, Fortsetzungsposition | Benutzerfluss: Tap → Wiedergabe; abgesichert über Dispatch-Kette + Route-Vertrag |
| Pflicht | Fortschritt online/offline | `AppUpdateTests_Player` + `ProgressReportServiceTests` (bestehend) | Meldung online sofort / offline verzögert verlustfrei | Positionstakt → `ReportPlaybackProgressEffect` → `ProgressReportService` (bestehender Drain) |
| Pflicht | „Angesehen" + Auto-Fortsetzung | `AppUpdateTests_Player` | Zeitfenster und Schalter greifen | Reine Update-Logik, vollständig testbar |
| Pflicht | Quelle nicht verfügbar → Fehler + nächste Episode | `AppUpdateTests_Player` | Fehlermeldung + Rückfall | Reine Update-Logik, vollständig testbar |
| Pflicht | PiP-Umschaltung (Windows) / native PiP (iOS) | `AppUpdateTests_Player` + `PlayerContractTests` | Toggle-Effekt + Flag; iOS-Voraussetzungen (`UIBackgroundModes`, `ShouldShowPlaybackControls`) | PiP selbst ist plattform-UI — verifiziert über Contract + Verdrahtung; manuelle Verifikation auf Gerät dokumentiert |

Bestehende E2E-Tests: keine vorhanden — keine Anpassung nötig.

## Offene Punkte

Keine.
