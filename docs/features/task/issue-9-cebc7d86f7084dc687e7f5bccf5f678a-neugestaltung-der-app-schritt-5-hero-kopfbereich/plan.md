# Umsetzungsplan: Startseite — Hero-Kopfbereich mit ausblendbarer Einstellung

Branch: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-5-hero-kopfbereich`
Anforderung: `requirement.md` · Bestandsaufnahme: `inventory.md`

## Entwurfsentscheidungen

1. **MVU-konformer Datenfluss.** Das Hero-Bild liegt als `byte[]` in einem
   neuen Sub-State `HomeHeroState` im `AppModel` (plattformneutral). Die
   `HomePage` wandelt die Bytes über `ImageSource.FromStream` um — wie
   `PosterImageService`, aber ohne Dateiablage (flüchtiger Cache im Model).
2. **Sichtbarkeit.** `AppModel.ShowHomeHero => Settings.HomeHeroEnabled`.
   Der Hero-Bereich bleibt auch ohne Bild sichtbar (nur Gradienten + Texte)
   — entspricht dem Web-Verhalten, wo bei fehlendem Bild die Gradienten
   über `--vp-bg-deep` greifen. Ein Lade-Fehlschlag erzeugt damit keinen
   Layout-Bruch; die Bildebene fehlt dann schlicht.
3. **Ladeanstoß** einmalig je Verbindung: in `EnterConnected` (wenn
   `HomeHeroEnabled` und weder Bild vorhanden noch Ladevorgang aktiv)
   sowie in `UpdateSaveSettingsRequested` beim Einschalten der Einstellung
   im verbundenen Zustand (Muster: AutoDownload-Toggle, `WithSourcesReload`).
   Nach Fehlschlag wird beim nächsten Verbindungsaufbau erneut versucht —
   ein `Failed`-Flag mit Dauer-Sperre entfällt (das Web lädt das Bild bei
   jedem Seitenaufruf neu; „einmal je Verbindung + Retry bei Reconnect"
   ist die app-adäquate Umsetzung).
4. **Einstellung** `HomeHeroEnabled` (`bool`, Standard `true`, positiv:
   „Kopfbereich anzeigen") in `AppSettings` + `AppSettingsService`
   (Schlüssel `HomeHeroEnabled`, profilpräfixiert, nicht-geheime Ablage).
   Die Einstellungsseite erhält einen neuen Abschnitt „Darstellung" vor
   „Downloads".
5. **Titel.** Der Web-Hero zeigt den serverseitig konfigurierten
   Anwendungstitel (`applicationTitle`), der über die öffentliche API
   nicht verfügbar ist. Die App zeigt stattdessen den Anzeigenamen des
   aktiven Servers (`KnownServers` → `NameOrAddress` zur
   `ActiveServerAddress`), Fallback „VideoPlayer" — semantisch die
   Bibliotheksbezeichnung.
6. **Endpunkt** `GET api/pictures/hero-background` als neue Methode
   `GetHeroBackgroundAsync` in `#region Pictures` des API-Clients
   (identische Sende-Pipeline: Bearer, Retries, 401-Reauth). Der Server
   liefert selbst ein Placeholder-JPEG — clientseitig wird nichts
   generiert.
7. **Darstellung** als `Grid`-Stapel in `HomePage.xaml` (erste Position
   im `VerticalStackLayout`, volle Breite ohne Margin):
   `Image` (`AspectFill`) als unterste Ebene, drei Gradient-`Border`
   (linear 0°/`StartPoint 0,0 → 1,0` dunkel; linearer Bottom-Fade in
   `VpBg`; zwei `RadialGradientBrush` mit `VpPrimary`/`VpSecondary`-Akzent),
   `BoxView`-/`Border`-Trennlinie `VpBorder` unten, Inhalts-`VerticalStackLayout`
   (Kicker-Chip „PRIVATE MEDIA LIBRARY", Titel, Beschreibung). Höhe per
   `SizeChanged`-Handler: `Math.Clamp(Width * 0.42, 260, 520)`.
8. **Tests** auf drei Ebenen: Model/Update-Ebene (Default, Toggle,
   Effekt-Auslösung, Fehlschlag-Stabilität), Persistenz-Ebene
   (`AppSettingsService`-Roundtrip), Client-Ebene (Endpunkt + Bearer über
   `StubHttpMessageHandler`), plus Quell-Vertragstests für XAML/CS
   (Hero-Markup, Schalter, Effekt-Case). **E2E:** nicht möglich — es
   existiert keine MAUI-UI-Testinfrastruktur; dokumentierte Einschränkung.

## Aufgaben

1. `VideoPlayer.Storage/AppSettings.cs`: `HomeHeroEnabled { get; init; } = true` hinzufügen (dokumentiert, positiv formuliert).
2. `VideoPlayer.Storage/AppSettingsService.cs`: Schlüssel `HomeHeroEnabledKey = "HomeHeroEnabled"`, `Load()`/`Save()` ergänzen.
3. `VideoPlayer.ApiClient/VideoPlayerApiClient.cs`: `GetHeroBackgroundAsync(CancellationToken)` → `GET api/pictures/hero-background` → `byte[]`.
4. `VideoPlayer.Mvu/HomeHeroState.cs` (neu): `record` mit `IsLoading`, `ImageBytes (byte[]?)`, `static Empty`; XML-Doku deutsch.
5. `VideoPlayer.Mvu/AppModel.cs`: `HomeHero` (init `HomeHeroState.Empty`), `ShowHomeHero => Settings.HomeHeroEnabled`, `HeroTitle` (aktiver Server → `NameOrAddress`, Fallback „VideoPlayer").
6. `VideoPlayer.Mvu/AppMessage.cs`: `HeroBackgroundLoadedMessage(byte[]? ImageBytes)`.
7. `VideoPlayer.Mvu/AppEffect.cs`: `LoadHeroBackgroundEffect`.
8. `VideoPlayer.Mvu/AppUpdate.cs`: Switch-Case für `HeroBackgroundLoadedMessage`; `EnterConnected` und `UpdateSaveSettingsRequested` um bedingten `LoadHeroBackgroundEffect` + `IsLoading`-Setzung erweitern (Hilfsfunktion `WithHeroLoad` analog `WithSourcesReload`).
9. `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`: `case LoadHeroBackgroundEffect` → `LoadHeroBackgroundAsync()` (Client aus `authSession`, Exceptions → `HeroBackgroundLoadedMessage(null)`, Dispatch via MainThread).
10. `VideoPlayer.Maui/Pages/HomePage.xaml`: Hero-`Grid` als erstes Element (Bildebene, 4 Overlay-`Border` mit Gradienten aus `Vp*`-Tokenfarben, `VpBorder`-Trennlinie, Content-Stack mit Kicker/Titel/Beschreibung), `x:Name`-Referenzen, `IsVisible` via Binding auf `Model.ShowHomeHero`.
11. `VideoPlayer.Maui/Pages/HomePage.xaml.cs`: `UpdateHero(model)` in `OnModelChanged` (Sichtbarkeit, `ImageSource` aus Bytes — gecachte Umwandlung nur bei Bytes-Wechsel, Titel aus `Model.HeroTitle`); `SizeChanged` → `Math.Clamp(Width*0.42, 260, 520)`.
12. `VideoPlayer.Maui/Pages/SettingsPage.xaml(.cs)`: Abschnitt „Darstellung" + Switch `HomeHeroSwitch` (`AutomationId`), Handler → `Save(s => s with { HomeHeroEnabled = e.Value })`, `PopulateFromModel` ergänzen.
13. Tests: `AppSettingsTests` (Default `true`), `AppSettingsServiceTests` (Roundtrip + Fehlender-Schlüssel→`true`), `AppUpdateTests_HomeHero.cs` (neu: EnterConnected löst Effekt nur bei aktivierter Einstellung/fehlendem Bild aus; Toggle-on connected → Effekt; `HeroBackgroundLoadedMessage` setzt Bytes/`IsLoading=false`; `null`-Ergebnis bleibt stabil; `ShowHomeHero` folgt der Einstellung), `VideoPlayerApiClientTests_Endpoints` (Hero-Endpunkt liefert Bytes, Authorization-Header), `SettingsStorageContractTests` (Schalter in XAML + Handler-Verdrahtung), `HomePageContractTests` (Hero-Markup/-Verdrahtung).
14. Doku: `docs/help/startseite-medienlisten.md` (Hero-Abschnitt), `docs/help/einstellungen-datenhaltung.md` (neuer Schalter), `changes.log`, `README.md`, `docs/RELEASE_NOTES.md`.

## Offene Punkte

Keine. Die Titel-Entscheidung (5) ist getroffen; die E2E-Lücke (8) ist eine
bewusste, dokumentierte Einschränkung — die Sichtbarkeitslogik ist vollständig
im unit-testbaren `AppModel` verankert.
