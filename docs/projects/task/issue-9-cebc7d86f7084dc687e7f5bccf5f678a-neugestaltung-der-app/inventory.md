# Bestandsaufnahme — Design-Angleichung an Webanwendung (Kundenfeedback)

Repo: `VideoPlayer.App.sln` (MAUI-Repo). Server-Repo nur lesend referenziert:
`D:\Repositories\softwareschmiede\fd729906-8801-43ee-8539-fa3e046c88f4`.

Web-Design-Referenz: `web-design-recherche.md` (desselben Verzeichnisses) — enthält
die verifizierten CSS-Tokens, Hero-/Header-Markup und das WatchedIndicator-SVG.

## Projekte und Struktur

- `VideoPlayer.Maui` — neue App (Windows/iOS/MacCatalyst/Android-Targets im csproj)
- `VideoPlayer.Mvu` — MVU-Kern (AppModel/AppUpdate/AppMessage/AppEffect)
- `VideoPlayer.ApiClient` — öffentliche API
- `VideoPlayer.Storage` — sqlite-net Caches, `AppSettings`, Download-Manifest
- `VideoWebPlayer.Client` — DTO-Kopie (Altbestand)
- `VideoWebPlayer.Maui` — Legacy-App (nicht Ziel dieses Projekts)
- `VideoWebPlayer.Maui.Tests` — 388 `public void`-Tests (MVU- + Contract-Tests;
  keine Rendering-/E2E-Tests — etablierte Konvention)

## 1. Styling / Theme (FA-1)

- `VideoPlayer.Maui/Resources/Styles/Colors.xaml:9-29`: unveränderte MAUI-Template-
  Palette (`Primary #512BD4`, `OffBlack #1f1f1f` u. a.) — kein einziges Web-Token.
- `Styles.xaml:26-34`: globaler `Button`-Style `CornerRadius="8"`,
  `Padding 14,10`, `MinimumHeightRequest/WidthRequest 44`.
  Web-Vorgabe: Buttons `radius-sm` 4px; Cards `radius` 8px; Chips/Badges/Icon-
  Buttons pill/kreisrund (`999px`/`50%`).
- `Styles.xaml:404-420`: `ContentPage`/Shell-Hintergrund per `AppThemeBinding` →
  `OffBlack #1f1f1f` statt Web `#131313`/`#0e0e0e`.
- Hartcodierte Farben in allen Pages (verifiziert): `HomePage.xaml` (#23232a,
  #9CCC65, #FFB84D, #B06020), `MediaDetailsPage.xaml` (#FFDDAA, #BBBBBB, #FFB84D,
  #23232a, #D9A029), `DownloadsPage.xaml`, `FavoritesPage.xaml`, `ProfilePage.xaml`,
  `SourceOverviewPage.xaml`, `PlayerPage.xaml`, `LoginPage.xaml`,
  `SettingsPage.xaml`, `ServerSetupPage.xaml`, `Controls/MediaCarousel.xaml`
  (#23232a, #FFB84D, #40404a, #AA000000).
- `App.xaml` merged Colors+Styles; `AppShell.xaml` Flyout: Startseite, Favoriten,
  Downloads, Profil, Einstellungen + dynamische Quelleneinträge.

## 2. Startseite / Hero (FA-2)

- `HomePage.xaml:9-67`: Offline-Banner, Storage-Recovery-Banner, Empty-State,
  drei `MediaCarousel`s („Weiterschauen", „Favoriten", „Neu im Programm"),
  Serverstatus-Label. **Kein Hero-Bereich.**
- `Controls/MediaCarousel.xaml`: `ScrollView` + `HorizontalStackLayout` +
  `BindableLayout` (kein CollectionView — alle Karten sofort instanziiert);
  Card 140×210, `CornerRadius 8`, `CachedPosterImage`, ProgressBar #FFB84D.
- `AppModel.cs:95-113`: nur die drei Home-Listen — kein Hero-State.
- Bildpipeline: `PosterImageService` (authentifizierte Bytes via
  `GetPictureAsync` → `api/pictures/{id}`, Preview-Cache) + `CachedPosterImage`.
- **Fehlt:** ApiClient-Methode für `GET api/pictures/hero-background`
  (Server: `PicturesController.cs:90-115`, `[BearerTokenCheck]` — akzeptiert
  Bearer-Header ODER `access_token`-Query; die App setzt bereits den Header).
  Kundenanforderung: Hero nicht persistent speichern → Memory- oder
  Preview-Cache-Lösung.
- **Fehlt:** `AppSettings` (VideoPlayer.Storage/AppSettings.cs:9-52) hat kein
  Hero-Flag; SettingsPage braucht positiven Toggle „Kopfbereich anzeigen",
  Default = an.

## 3. Detailseiten (FA-3/FA-4)

- `MediaDetailsPage.xaml:10-208`: gepaddetes Grid — Poster fester Größe, Titel,
  Meta, Plot, Favoriten-/Play-/Download-Buttons, Staffel-Picker + Episoden-
  CollectionView, Collection-Movie-Grid. **Kein Vollbreiten-Backdrop, kein
  Gradient-Overlay.**
- `MediaDetailsView.cs`: `SeasonItem`, `EpisodeItem`, `CollectionMovieItem` —
  **kein Watched-Feld**; `PosterPictureId`, `BannerPictureId` (Show-Ebene),
  `SelectedSeasonId`, `HighlightedEpisodeId`, `IsFavorite`.
- `MediaDetailsService.cs:562-617` `MapShow`: Bildwahl je Episode
  `GeneratedBackgroundPictureId ?? PosterPictureId`; `:291-313` standalone
  Episode → `GeneratedBackgroundPictureId ?? BannerPictureId ?? PosterPictureId`.
  Kein „Header-Bild der gewählten Episode"-Konzept (Web wechselt auf
  Episoden-Backdrop bei Auswahl; Endpunkt `api/episodes/{id}/background-image`
  existiert, lazy-generierend mit ETag — `EpisodesController.cs:50-83`).
- `WatchedAt`: Server sendet es bereits (`ItemsController.cs:697-718` via
  `WatchedStatusService.EnrichAsync`; Server-DTO `DtoMediaEntry.WatchedAt`).
  MAUI-`MediaEntry` mappt es nicht (STJ ignoriert das JSON-Feld still) →
  **rein clientseitige Ergänzung** (Feld + Mapping + Badge-Rendering).
- Web-Referenz-Markup: `TVShowDetails.razor:46-120` Header mit
  `background-image` + Overlay-Gradienten + Titel/Meta/Plot + Play/Favoriten;
  `:141-167` Episode-Boxen mit `<WatchedIndicator>` bei `WatchedAt.HasValue`.

## 4. Downloads / Flickern (FA-5)

- `DownloadsPage.xaml:10-114`: Quota-Label, Offline-Banner, Loading, Error+Retry,
  Empty, `CollectionView` (Cards 8px).
- **Flicker-Ursache (verifiziert):** `DownloadsPage.xaml.cs` — `OnModelChanged`
  → `UpdateUi` setzt bei jeder Modelländerung `ItemsSource` neu (auch bei
  unveränderter Liste) + weist BindingContexts neu zu → CollectionView-Reset
  pro Fortschritts-Tick. MVU sendet bereits granulare
  `DownloadItemChangedMessage`s (`AppUpdate.cs:1752-1763` upsert;
  `DownloadService.cs:704-724` drosselt auf 250 ms) — die Page nutzt sie nur
  zum Gesamt-Re-Render.
- Relevante Effekte: `LoadDownloadsEffect`/`EnqueueDownloadEffect`/
  `CancelDownloadEffect`/`DeleteDownloadEffect`/`ResumeDownloadsEffect`/
  `RunAutoDownloadsEffect`/`DeleteWatchedDownloadEffect` (AppEffect.cs:296-377).

## 5. Player (FA-6/FA-7)

- `PlayerPage.xaml:10-75`: `toolkit:MediaElement` (Autoplay,
  `ShouldShowPlaybackControls`, `ShouldKeepScreenOn`), `ActivityIndicator`,
  **`OverlayChrome`-`VerticalStackLayout` ohne `VerticalOptions` → füllt das
  gesamte Grid über dem MediaElement** (Default Fill). Sichtbarer Inhalt nur
  oben, aber der Container deckt alles → plausibler Hit-Test-Blocker für die
  nativen Controls (FA-6). Im Mini-Player klickbar, weil
  `OverlayChrome.IsVisible = !IsPictureInPicture` es dort ausblendet.
- `PlayerPage.xaml.cs:153`: `SeekTo` in `OnMediaOpened` — einzige SeekTo-Stelle.
  Kunden-Crash: `COMException 0x8001010E` (RPC_E_WRONG_THREAD) in
  `MediaManager.PlatformSeek` → `MediaPlayerElement.get_MediaPlayer()` auf
  ThreadPool-Thread beim zweiten Video. Toolkit: `CommunityToolkit.Maui.
  MediaElement` 10.0.0 (bekannte Thread-Affinitäts-Probleme: Seek gehört auf
  den UI-Thread).
- Verstärker app-seitig: `ModelChanged +=` ohne Unsubscribe (PlayerPage:38) —
  gepoppte PlayerPages bleiben abonniert und rendern auf detached MediaElement;
  `OnDisappearing` dispatcht `PlayerClosedMessage` (Z.58) → Reset-Race mit der
  `PlayerPageOpenedMessage` der neuen Page. PlayerPage ist transient (neue
  Instanz pro Start, Route „player").
- MVU: `NavigateToPlayerEffect` (AppEffect.cs:250), `ResolvePlaybackSourceEffect`
  (261-263: lokale Datei vor Stream), `ReportPlaybackProgressEffect` (276-280,
  Outbox via `ProgressReportService` → `pending_progress`), Watched-Schwelle
  `WatchedEndWindowSeconds` (AppUpdate.cs:1492-1553), Auto-Delete (1556-1600).
- PiP: `IPictureInPictureService.IsToggleSupported` → PiP-Button nur Windows
  (CompactOverlay); iOS `Info.plist` Audio-Background-Mode vorhanden.

## 6. Querschnitt / Konventionen

- **Event-Leak-Muster:** `ModelChanged +=` ohne `-=` in HomePage:23,
  MediaDetailsPage:37, DownloadsPage:25, PlayerPage:38, FavoritesPage:25,
  SourceOverviewPage:27, ProfilePage:24, AppShell:84 (Singleton → unkritisch).
  Settings/ServerSetup/Login melden korrekt ab.
- Routen `AppRoutes.cs:13-18`: home/favorites/downloads/profile/settings +
  Push-Routen player/mediadetails/sourceoverview/login/serversetup.
- `ProgressReportService.cs:44-86`: online sofort, sonst Outbox; permanente
  4xx (außer 401/408/429) verworfen.
- Storage-Entities: `CachedListEntry`, `CachedFavoritesEntry`,
  `DownloadManifestEntry`, `PendingProgressSyncEntry`, `KnownServerEntry`,
  `BackupStateEntry`.

## 7. Tests

`VideoWebPlayer.Maui.Tests` — 388+ Tests, u. a. `MediaDetailsServiceTests`,
`AppUpdateTests_MediaDetails`, `AppUpdateTests_HomeLists`, `AppUpdateTests_Player`,
`AppUpdateTests_Downloads`, Contract-Tests (`VideoPlayerMauiContractTests`,
`HomePageContractTests`, `DownloadsContractTests`, `PlayerContractTests`,
`MauiSettingsContractTests`, `SettingsStorageContractTests`,
`MediaEntrySerializationTests`, `EnumCoverageTests`, `AppSettingsServiceTests`).
Lücken: kein Rendering-/Visual-Test, keine Device-Verifikation, Hero/Watched/
Backdrop unabgedeckt.

## Größte Lücken (Zusammenfassung)

1. Visuelle Token fehlen komplett (Template-Palette statt Web-Tokens;
   ~10 XAML-Dateien mit Legacy-Hexfarben).
2. Hero fehlt (kein ApiClient-Endpunkt, kein State, kein Setting).
3. Detail-Header fehlt (Poster-Grid statt Vollbreiten-Backdrop+Overlay;
   kein Episoden-Header-Konzept).
4. `WatchedAt` wird nicht gemappt/gerendert (Server liefert es bereits).
5. Button-/Form-Mismatch (8px global statt 4px Buttons/8px Cards/runde
   Icon-Buttons).
6. Laufzeitdefekte: Flicker durch ItemsSource-Reset; OverlayChrome blockiert
   Controls; Seek-COMException + ModelChanged-Leaks auf transienten Pages.

## Offene Fragen

1. Hero-Bild: neue ApiClient-Methode + Preview-Cache vs. nur Memory
   (Server-Endpunkt direkt nutzbar, keine API-Änderung nötig).
2. Detail-Header-Bildquelle: `api/episodes/{id}/background-image` direkt vs.
   `GeneratedBackgroundPictureId`-Fallbackkette via `api/pictures/{id}`.
3. `WatchedAt` an `MediaEntry`-Basis (deckt Movie+Episode) oder nur gezielt.
4. Icon-/Rund-Buttons: `Border`+TapGestureRecognizer vs. `Button` mit
   `RoundRectangle`-StrokeShape — einheitliche Konstruktion definieren.
5. FA-6-Fix: `OverlayChrome` schrumpfen/`InputTransparent` vs.
   Tap-to-toggle-Overlay.
6. FA-7: `SeekTo` UI-Thread-Marshal + Unsubscribe-Muster für transiente Pages.
7. FA-5: ObservableCollection-Inplace-Update vs. ItemsSource-Diff.
8. Tolerierte Design-Abweichungen (App-Bar/Flyout statt Web-Sidebar bleibt?).
