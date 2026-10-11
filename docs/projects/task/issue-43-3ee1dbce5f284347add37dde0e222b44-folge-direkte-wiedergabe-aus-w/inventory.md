# Bestandsaufnahme — Issue #43: Direkte Wiedergabe aus Weiterschauen, Protokollversand, Playlist-Support

**Stand:** Branch `task/issue-43-3ee1dbce5f284347add37dde0e222b44-folge-direkte-wiedergabe-aus-w`
(letzter Commit `9ad7a44` — „fix: Weiterschauen-Karten im Querformat 16:9 (#42)").
**Quelle der Anforderungen:** `requirement.md` (6 Anforderungen).

Detaildokumente in `inventory/`:

- `listen-wiedergabe.md` — Anf. 1: Startseiten-Listen, Tap-Fluss, Wiedergabestart, Fortsetzungsposition
- `einstellungen-protokoll.md` — Anf. 2: Einstellungen, Logging-Stand, Mail-/Share-Fähigkeit
- `api-client-playlists.md` — Anf. 3 + 6 (API): Endpunkt-Bestand, Playlist-Abdeckung, Menü-Einbauort
- `reihenfolge-downloads.md` — Anf. 4: Folgevideo-Ermittlung, Download-Warteschlange, Playlist-Bezug
- `serienansicht-ios.md` — Anf. 5: Serienansicht, Staffel-Picker, iOS-Auffälligkeiten
- `kontextmenue.md` — Anf. 6: Long-Press-/Kontextmenü-Stand (neue App vs. Altbestand)
- `architektur-tests.md` — MVU-Konventionen, Menü/Routing, Testinfrastruktur, Build, Bausteine

## Struktur des Repositories

Solution `VideoPlayer.App.sln`. **Aktive App = `VideoPlayer.Maui` + `VideoPlayer.Mvu`**
(MVU ohne externes Framework); `VideoWebPlayer.Maui`/`VideoWebPlayer.Client` sind der
Altbestand („wird schrittweise abgelöst", README). Weitere Projekte:
`VideoPlayer.ApiClient` (plattformneutraler API-Client), `VideoPlayer.Storage` (SQLite/
Preferences/SecureStorage), `msTools.DeveloperApi(.Maui)` (UI-Automatisierung, nur Debug),
Tests: `VideoWebPlayer.Maui.Tests` (xUnit, Unit + Quelltext-Contract-Tests),
`VideoPlayer.E2E.Tests` (xUnit + FlaUI, Windows-E2E mit `MockVideoServer`).

## Befunde je Anforderung

### 1. Direkte Wiedergabe aus der Weiterschauen-Liste — *nicht vorhanden*

- Alle drei Startseiten-Karussells teilen denselben Tap-Pfad:
  `MediaCarousel.ItemTapped` → `HomePage.OnMediaItemTapped` → `MediaItemSelectedMessage` →
  `AppUpdate.UpdateMediaItemSelected` → `NavigateToMediaDetailsEffect` → Shell-Route
  `mediadetails…` (`AppUpdate.cs` Z. 1188–1203). **Tap = nur Detailansicht**, für
  Weiterschauen wie für Favoriten/„Neu im Programm" identisch.
- Wiedergabestart existiert nur von der Detailseite aus:
  `PlayRequestedMessage` → `UpdatePlayRequested` (`AppUpdate.cs` Z. 1923–1945) →
  `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect` → `PlayerPage` (MediaElement).
- Fortsetzungsposition wird **nicht** über die Nachricht übergeben, sondern von
  `PlaybackSourceService.ResolveAsync` selbst aus der Weiterschauen-Liste ermittelt
  (online `api/continue-watching`, sonst Cache `cached_lists[continue_watching]`) und als
  `PlaybackSource.ResumePosition` in `PlayerPage` per verzögertem `SeekTo` angesprungen
  (`PlayerPage.xaml.cs` Z. 263–351). → Direkter Start aus der Liste hätte die Position
  also automatisch; Voraussetzung: Medientyp `movie`/`episode` (Update-Guard).
- `MediaCarousel.ItemTapped` liefert nur das `HomeMediaItem` — die Herkunftsliste ist am
  Auslösepunkt derzeit nicht unterscheidbar.

### 2. Protokollierung & Protokollversand — *nicht vorhanden*

- `SettingsPage` existiert mit den Abschnitten Darstellung/Downloads/Wiedergabe/Server;
  `AppSettings` (8 Werte) + `AppSettingsService` + `PreferencesSettingsStore` —
  **kein** Schalter für Protokollierung, **kein** „Protokoll senden"-Button.
- Logging besteht nur aus `Debug.WriteLine` (Konvention) und `#if DEBUG` →
  `builder.Logging.AddDebug()` (`MauiProgram.cs` Z. 151–153); `ILogger?` nur als
  Option des ApiClient. **Kein** Datei-Logging, kein Serilog.
- **Keine** Mail-/Share-Nutzung im Repo. `Microsoft.Maui.Essentials` ist in
  `VideoPlayer.Maui` referenziert → `Email.ComposeAsync` o. ä. ohne neue Pakete möglich.

### 3. Playlist-Unterstützung — *komplett neu zu bauen*

- **Kein** Playlist-Code im gesamten Repository: kein ApiClient-Endpunkt/DTO, keine
  Seite, keine Message, kein State — nur Anforderungstexte.
- Ob der Server Playlist-Endpunkte anbietet, ist aus diesem Repo nicht verifizierbar
  (Server liegt separat). Die dokumentierte API-Fläche (`docs/help/api-client/api.md`)
  enthält keine Playlists.
- Menü-Einbauort: `AppShell.xaml` (FlyoutItem) + `AppPage` + `AppRoutes` (gepinnt durch
  `AppRoutesTests`/`VideoPlayerMauiContractTests`) + neue Page + DI + MVU-State/Messages/
  Effects. Muster für dynamische Menügruppen existiert (Quellen in `AppShell.xaml.cs`).

### 4. Wiedergabe-/Download-Reihenfolge bei Playlists — *Playlist-Bezug fehlt überall*

- Folgevideo = **Serienkontext der geladenen Detailseite**: `NextEpisodeQueue`
  (`AppUpdate.cs` Z. 1956–1978) aus `model.MediaDetails.Details.Seasons`, sortiert nach
  Staffel-/Episodennummer; leer ohne geladenen Serienkontext. Auto-Fortsetzung und
  Fehler-Rückfall nutzen `PlayerState.UpcomingEpisodes`.
- Downloads: `DownloadService` = FIFO-Warteschlange über `download_manifest`
  (`NextQueuedEntryAsync` → `OrderBy(e => e.Id)`, `MaxParallelDownloads = 2`).
  `AutoDownloadService` reiht Folge-Episoden ebenfalls in Serienreihenfolge ein
  (`api/items/tvshow/{id}`).
- `ContinueWatchingEntry`, `HomeMediaItem` und `DownloadManifestEntry` besitzen
  **kein** Playlist-Feld — eine Playlist-Reihenfolge ist derzeit nicht abbildbar.

### 5. iOS-Serienansicht — *Fehlerbild plausibel im gemeinsamen Code*

- Serienansicht = `SeasonSection` in `MediaDetailsPage.xaml` (Z. 151–295): `Picker`
  (`SeasonPicker`) + `CollectionView` (`EpisodesCollection`) — **beide innerhalb des
  `ScrollView` `DetailsScroll`**. CollectionView in ScrollView ist ein bekanntes
  iOS-Rendern-Problem (Vermutung als Ursache für fehlende Episodenliste).
- Staffeln kommen aus dem Modell **korrekt sortiert** (`OrderBy(s.Number)` in
  `MediaDetailsService`, Z. 456–457 offline / Z. 608–609 online) — die falsche
  Anzeigereihenfolge liegt damit vermutlich am iOS-`Picker`/Binding (Vermutung).
- Zurückspringen: `UpdateSeasonSection` (`MediaDetailsPage.xaml.cs` Z. 218–237) setzt bei
  **jedem** Model-Tick `ItemsSource`/`SelectedItem` neu; Schutzflag `updatingSeasonPicker`
  deckt nur synchrone `SelectedIndexChanged`-Aufrufe ab — ein verzögertes iOS-Event kann
  als `SeasonSelectedMessage` durchgehen und die Auswahl zurücksetzen (Vermutung als
  Mechanismus).
- Keine iOS-Spezialbehandlung der Serienansicht; `#if IOS` existiert nur für
  Backup-Ausschluss (`AppStorageLayoutProvider`) und QR-Scan (`#if ANDROID || IOS`).

### 6. Kontextmenü in der Weiterschauen-Liste — *API vorhanden, UI fehlt*

- **API-Endpunkte existieren bereits** im neuen Client:
  `HideContinueWatchingEntryAsync` → `POST api/continue-watching/hide`,
  `SkipContinueWatchingEntryAsync` → `POST api/continue-watching/skip`
  (`VideoPlayerApiClient.cs` Z. 296–327; Ergebnis `ContinueWatchingMutationResult`;
  gepinnt in `VideoPlayerApiClientTests_Endpoints.cs` Z. 29–32). In der App bisher
  ungenutzt (keine Message/Effekt).
- **Long-Press/Kontextmenü in der neuen App nicht vorhanden.** Im Altbestand existiert
  ein 2-Sekunden-Long-Press (`mct:TouchBehavior LongPressDuration="2000"`,
  `VideoWebPlayer.Maui/Components/MediaCarousel.xaml` Z. 28), der Weiterschauen-Einträge
  aber nur **lokal** aus der Liste entfernt — ohne Menü, ohne Server-Aufruf
  (`HomePageViewModel.cs` Z. 817–821).
- `CommunityToolkit.Maui` (TouchBehavior) ist in `VideoPlayer.Maui` nicht referenziert —
  nur `CommunityToolkit.Maui.MediaElement`.
- Listen-Aktualisierung nach Hide/Skip kann über die bestehende Sync-Strecke laufen
  (`SyncContinueWatchingEffect` → Cache → `CachedListsChangedMessage`).

## Allgemeine Punkte

- **MVU-Konvention:** `AppMessage`-/`AppEffect`-Records (Deutsch, XML-Doku, CS1591 als
  Fehler), `AppUpdate.Update` als statisches Switch, `ShellAppEffectHandler` am App-Rand;
  Seiten = transient, `BindingContext = MvuHost`, `ModelChanged`-Abo in
  OnAppearing/OnDisappearing, Aktionen als Dispatch.
- **Menü:** statische FlyoutItems + dynamische Quellen-Gruppe (`Items.Insert`);
  geschobene Routen `serversetup`, `login`, `mediadetails`, `sourceoverview`, `player`.
- **Tests:** `VideoWebPlayer.Maui.Tests` = Unit-Tests der neutralen Projekte +
  Quelltext-Contract-Tests der MAUI-Oberfläche (AutomationIds, Routen, Menü gepinnt);
  `VideoPlayer.E2E.Tests` = Windows-E2E (echte App + Mock-Server + Dev-API).
  `EnumCoverageTests` erzwingt Testabdeckung je Enum-Wert (Git-Hook).
- **Build:** `dotnet restore VideoPlayer.App.sln`,
  `dotnet test VideoWebPlayer.Maui.Tests/…`,
  `dotnet build VideoPlayer.Maui/… -f net10.0-windows10.0.19041.0`;
  CI in `.github/workflows/pr-checks.yml` (Windows-Builds + Tests + `dotnet format`).
- **Wiederverwendbar:** `PlaybackSourceService` (Quelle+Resume), `HomeListService`,
  `ServerSyncService` (Caches), `DownloadService`/`AutoDownloadService`,
  `AppSettingsService`/`ISettingsStore`, `AppStorageLayout` (Log-Ablageort),
  `MediaCarousel`, `CachedPosterImage`, `Vp*`-Design-Tokens, Dev-API für UI-Tests.

## Offene Punkte / zu verifizieren (aus der Anforderung heraus)

- Playlist-API des Servers (Endpunkte/DTOs) und Playlist-Felder an Weiterschauen-Einträgen
  — in diesem Repo nicht prüfbar; ggf. Server-Doku/-Code konsultieren.
- Ob „Protokoll senden" via Essentials `Email` oder `Share` umgesetzt wird, ist zu
  entscheiden (kein Vorbild im Code).
- Ursache der iOS-Defekte (CollectionView-in-ScrollView, Picker-Events) ist plausibel
  lokalisiert, aber ohne iOS-Lauf nicht final verifiziert — bei Umsetzung per
  iOS-Deployment (`scripts/iOS-Deployment.ps1`) nachweisen.
