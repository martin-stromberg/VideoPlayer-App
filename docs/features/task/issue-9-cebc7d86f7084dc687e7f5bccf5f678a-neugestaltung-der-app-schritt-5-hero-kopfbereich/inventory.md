# Bestandsaufnahme: Startseite — Hero-Kopfbereich (Schritt 5)

Datum: 2026-09-22 (lokale Zeit, UTC+2)
Branch: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-5-hero-kopfbereich`
Commit (Basis der Bestandsaufnahme): `11c38e8`
SDK: .NET SDK 10.0.401
Uncommitted bei Baseline: nur die Feature-Artefakte dieses Schritts
(`docs/features/…-schritt-5-hero-kopfbereich/`) sowie die fremde untracked
Datei `docs/features/task/issue-9-customer-feedback.md` (bleibt unangetastet).

## Test-Ausgangszustand (Baseline)

- Kommando: `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --logger "console;verbosity=normal"`
- Exit-Code: 0
- Ergebnis: **672/672 bestanden**, 0 fehlgeschlagen, ~9,5 s
- Log: `inventory/test-results/baseline-tests.log`
- Hinweis: das im DoD genannte Projekt `VideoWebPlayer.Tests` existiert in
  dieser Lösung nicht; die Testsuite ist `VideoWebPlayer.Maui.Tests`
  (verifiziert).

## Bestehende Struktur (mit Fundstellen)

### Einstellungen (`VideoPlayer.Storage`)

- `AppSettings.cs` — unveränderlicher Record mit 8 Eigenschaften
  (u. a. `AutoDownloadEnabled`, `AutoResumeEnabled = true` als Vorbild
  positiver Defaults). `Normalize()` klemmt Zahlenbereiche.
- `AppSettingsService.cs` — `public const string …Key` je Einstellung,
  `Load()` liest `store.GetBool/GetInt(Key(…), defaults.…)` und
  normalisiert; `Save()` schreibt alle Schlüssel über
  `ISettingsStore.Set(profile.PrefixKey(key), value)`. Ein neuer Schlüssel
  ist rein additiv — fehlende Schlüssel fallen auf Defaults zurück.
- `ISettingsStore.cs` — Bool/Int-Getter/-Setter; Implementierung
  `VideoPlayer.Maui/Services/PreferencesSettingsStore.cs` (MAUI
  `Preferences`, nicht geheim, backup-pflichtig).

### MVU (`VideoPlayer.Mvu`, plattformneutral — keine MAUI-Typen)

- `AppModel.cs` — `Settings` (Z. 61), `KnownServers`/`ActiveServerAddress`
  (Z. 64/70), Home-Listen `ContinueWatching`/`Favorites`/`RecentEntries`
  (Z. 95–113), Sub-States als Records (`SourcesOverviewState.Initial`
  u. a., Z. 121–139). Abgeleitete Properties (`IsOffline`,
  `ServerHealthText`) sind etabliert.
- `AppMessage.cs` — Records je Nachricht; relevant:
  `SettingsLoadedMessage`, `SaveSettingsRequestedMessage(AppSettings)`,
  `HomeListsLoadedMessage`, `AutoLoginCompletedMessage`,
  `LoginCompletedMessage`, `SynchronizationCompletedMessage`.
- `AppEffect.cs` — Records je Effekt; relevant: `LoadSettingsEffect`,
  `PersistSettingsEffect(AppSettings)`, `LoadHomeListsEffect`,
  `RunFullSyncEffect`, `ConnectServerEventsEffect`.
- `AppUpdate.cs` — `Update()`-Switch; `UpdateStartupRequested` emittiert
  `[InitializeStorageEffect, LoadSettingsEffect, LoadServersEffect,
  StartNetworkMonitoringEffect, LoadHomeListsEffect]` (Z. 148);
  `EnterConnected` (Z. 435–474) setzt `ConnectionState.Connected` und
  sammelt Effekte (`LoadSourcesEffect`, `ConnectServerEventsEffect`,
  `RunFullSyncEffect`, `ResumeDownloadsEffect`); `UpdateSaveSettingsRequested`
  (Z. 158–172) normalisiert, emittiert `PersistSettingsEffect` und zeigt
  das Muster „Toggle-on → Sofort-Effekt nur wenn Connected";
  `WithSourcesReload` (Z. 580–592) ist das Muster für bedingtes Laden mit
  `IsLoading`-Sperre; `UpdateSettingsLoaded` (Z. 151–156) übernimmt
  normalisierte Einstellungen ins Model.
- `SourcesOverviewState.cs` — Muster für einen Sub-State-Record
  (`Initial`-Singleton, `IsLoading`, `Attempted`, `Error`, Datenliste).

### API-Client (`VideoPlayer.ApiClient`)

- `VideoPlayerApiClient.cs` — `GetPictureAsync` (Z. 394–403) und
  `GetSourceIconAsync` (Z. 409–418) sind die Vorlage für den Hero-Abruf:
  `SendWithPolicyAsync(endpoint, requestFactory, allowReauth: true, ct)`
  → `response.Content.ReadAsByteArrayAsync`. Die Sende-Pipeline
  (Z. 477–544) enthält Bearer-Header über `httpClient` (Handler),
  Wiederholstrategie (1 s/2 s/4 s) und einmalige Token-Erneuerung bei 401.
  Der Hero-Endpunkt `api/pictures/hero-background` ist ein zusätzlicher
  Fall in der `#region Pictures`.
- Der Endpunkt akzeptiert serverseitig auch `?access_token=` — entfällt,
  da der Client Bearer setzt.

### Effekt-Ausführung (`VideoPlayer.Maui/Services`)

- `ShellAppEffectHandler.cs` — `HandleAsync`-Switch (Z. 68–199); neuer
  `case LoadHeroBackgroundEffect:` wird dort ergänzt. `authSession.Client`
  liefert den angemeldeten `VideoPlayerApiClient` (`null`, wenn keine
  Sitzung). Fehlermuster: `try/catch` mit `Debug.WriteLine`, Ergebnis über
  `MainThread.InvokeOnMainThreadAsync(() => host.DispatchAsync(…Message))`
  (vgl. `LoadSettingsAsync`, Z. 896–912, und `LoadSourcesAsync`).
- `PosterImageService.cs` — `ImageSource.FromStream(() => new
  MemoryStream(bytes))` (Z. 73) ist das Umwandlungsmuster Byte[]→ImageSource;
  flüchtiger `memoryCache` + Dateiablage `previews/`. Für den Hero genügt
  per Vorgabe der flüchtige Anteil — keine Dateiablage.

### UI

- `Pages/HomePage.xaml` (69 Zeilen) — `ScrollView` > `VerticalStackLayout`
  mit `OfflineBanner`, `StorageNoticeBanner`, Leerzustands-Label
  (`DataTrigger` auf `Model.HasHomeContent`), drei `MediaCarousel`
  (`Items="{Binding Model.…}"`, `ItemTapped="OnMediaItemTapped"`),
  `ServerStatusLabel`. Der Hero wird als erstes Element des
  `VerticalStackLayout` eingefügt (vollbreit, kein Margin).
- `Pages/HomePage.xaml.cs` — `MvuHost` als BindingContext,
  `ModelChanged` → `UpdateBanners(model)` auf dem MainThread. Hier kommen
  die Hero-Aktualisierung (Sichtbarkeit, Bildquelle aus Bytes, Titel) und
  die Höhenberechnung (`SizeChanged`) hin.
- `Pages/SettingsPage.xaml` — Abschnitte Downloads/Wiedergabe/Server;
  Switches mit `x:Name` + `AutomationId`, `Toggled`-Handler im Code-Behind.
  Neuer Schalter „Kopfbereich anzeigen" erhält eine eigene Abschnittsgruppe
  (Darstellung) vor „Downloads".
- `Pages/SettingsPage.xaml.cs` — `PopulateFromModel()` setzt Switch-Stände
  unter `suppressEvents`-Schutz; `On…Toggled` → `Save(s => s with { … })`
  → `SaveSettingsRequestedMessage`.

### Design-Tokens (`VideoPlayer.Maui/Resources/Styles/Colors.xaml`)

- Vorhanden: `VpBg` `#131313`, `VpBgDeep` `#0e0e0e`, `VpSurface*` (4 Stufen),
  `VpText`, `VpTextMuted`, `VpTextSoft`, `VpBorder` `#1AFFFFFF`,
  `VpBorderStrong`, `VpPrimary` `#e50914`, `VpPrimarySoft`,
  `VpSecondary` `#00a8e8`, `VpSecondarySoft`, `VpDanger`, `VpOverlay`.
- Die Web-Gradienten (`rgba(19,19,19,…)`, `rgba(229,9,20,.3)`,
  `rgba(0,168,232,.22)`) werden als `LinearGradientBrush`/
  `RadialGradientBrush` aus denselben RGB-Werten der Tokens abgebildet
  (transluzente Ableitungen der Tokenfarben — keine neuen abweichenden
  Hexfarben).

### Web-Referenz (Quell-Repo `fd729906-…`, nur gelesen)

- `VideoWebPlayer/wwwroot/app.css` — `.home-hero` (Höhe
  `clamp(260px, 42vw, 520px)`, `background-size: cover`, Gradienten-Stack),
  `.home-hero-content`, `.hero-kicker` (Uppercase-Chip).
- `Components/Pages/Home/Home.razor` — Markup: `section.home-hero` mit
  `--home-hero-image: url('/api/pictures/hero-background?…')`, Kicker
  „Private Media Library", `h1` = `applicationTitle`, Beschreibung.
- `Controllers/PicturesController.cs` — `GET api/pictures/hero-background`,
  `CheckLogedIn`, liefert generiertes JPEG bzw. serverseitigen Placeholder.
- `Services/HomeBackgroundImage/HomeBackgroundImageGenerator.cs` —
  Bildgenerierung aus Weiterschauen-Postern; Placeholder-Fallback
  serverseitig (die App muss kein eigenes Placeholder-Bild erzeugen).

### Tests (`VideoWebPlayer.Maui.Tests`)

- 672 Tests grün (Baseline). Relevante Dateien:
  `AppSettingsTests.cs`, `AppSettingsServiceTests.cs` (In-Memory-Store),
  `AppUpdateTests_*.cs` (Update-Zyklen, Effekt-Assertions),
  `SettingsStorageContractTests.cs` (XAML-Quell-Vertrag: Switches,
  AutomationIds), `HomePageContractTests.cs` (XAML/CS-Quell-Vertrag),
  `VideoPlayerApiClientTests_Endpoints.cs` + `Stubs/StubHttpMessageHandler.cs`
  + `ApiClientTestFactory` (aufgezeichnete Requests mit Authorization-Header).
- Es existiert **keine** MAUI-UI-/E2E-Testinfrastruktur (kein Appium/
  Device-Test-Projekt). UI-Verhalten wird per Konvention über
  Quell-Vertragstests abgesichert; die Sichtbarkeitslogik liegt in
  `AppModel` (unit-testbar). Einschränkung wird in Plan und
  Testergebnissen dokumentiert.

## Lücken / zu ergänzende Teile

1. `AppSettings.HomeHeroEnabled` + Ablageschlüssel (fehlt).
2. `VideoPlayerApiClient`-Methode für `api/pictures/hero-background` (fehlt).
3. Hero-Zustand im MVU-Model + Nachricht + Effekt + Update-Verdrahtung (fehlt).
4. Effekt-Ausführung in `ShellAppEffectHandler` (fehlt).
5. Hero-Markup in `HomePage.xaml` + Code-Behind (Sichtbarkeit, ImageSource,
   Höhe) (fehlt).
6. Schalter in `SettingsPage` (fehlt).
7. Tests zu allen oben genannten Punkten (fehlen).
8. Dokumentation `docs/help/` (Startseiten-/Einstellungs-Artikel),
   `changes.log`, `README.md`, `docs/RELEASE_NOTES.md` (stehen aus).

## Risiken

- `ImageSource.FromStream` benötigt den MainThread — Umwandlung im
  Code-Behind innerhalb `MainThread.BeginInvokeOnMainThread`.
- Höhe `clamp(260px, 42vw, 520px)`: MAUI kennt kein `clamp` — Umsetzung
  über `SizeChanged`/`Width` im Code-Behind (`Math.Clamp(width * 0.42,
  260, 520)`).
- Das hero-Image ändert sich serverseitig mit der Weiterschauen-Liste;
  die App lädt einmalig je Verbindung (Anforderung verlangt kein Refresh —
  flüchtiger Cache genügt).
- Kompilierbare Trennung: `byte[]` im MVU-Model ist erlaubt
  (plattformneutral); `ImageSource` nur in `VideoPlayer.Maui`.
