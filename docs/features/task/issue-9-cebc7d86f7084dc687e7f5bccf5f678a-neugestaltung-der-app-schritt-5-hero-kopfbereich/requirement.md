# Anforderung: Startseite — Hero-Kopfbereich mit ausblendbarer Einstellung (Entwicklungsschritt 5)

Quelle: `docs/projects/task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app/project-plan.md`,
Schritt 5 (Kundenfeedback Issue #9, Projekt „Design-Angleichung an Webanwendung").
Design-Referenz: `docs/projects/task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app/web-design-recherche.md`
(Startseiten-Hero der Webanwendung: `.home-hero`/`.home-hero-content`/`.hero-kicker`).

Kundenvorgabe (wortlaut): „Der Kopfbereich mit dem generierten Bild soll auch in der
App gezeigt werden. Er soll in den Einstellungen ausblendbar sein. Die Einstellung muss
positiv gestaltet sein, also so, dass der Kopfbereich aktivierbar ist und standardmäßig
aktiviert ist."

## Fachliche Zusammenfassung

Die Startseite der neuen App (`VideoPlayer.Maui`, `Pages/HomePage.xaml` — die
Legacy-App `VideoWebPlayer.Maui` bleibt unangetastet) erhält oberhalb der drei
Medienlisten einen vollbreiten Hero-Kopfbereich nach Web-Vorbild: das
servergenerierte Hintergrundbild vom Endpunkt `api/pictures/hero-background`
(Bearer-Authentifizierung über den bestehenden API-Client), darüber geschichtete
Dunkelgradienten und unten links Kicker-Chip, Titel und Beschreibungstext. Über
eine neue, positiv formulierte App-Einstellung „Kopfbereich anzeigen"
(`AppSettings.HomeHeroEnabled`, Standardwert **aktiviert**) lässt sich der
Bereich ein- und ausblenden; die Einstellung wird wie alle übrigen
App-Einstellungen über `AppSettingsService`/`ISettingsStore` profilpräfixiert
persistiert und überlebt einen Neustart (app-lokal, keine Server-Synchronisation).

## Akzeptanzkriterien (aus der Kundenanforderung abgeleitet)

1. Die Startseite zeigt — sofern aktiviert — oberhalb der Inhaltslisten einen
   vollbreiten Hero-Kopfbereich mit dem servergenerierten Bild von
   `api/pictures/hero-background` (JPEG bzw. serverseitiger Placeholder).
2. Darstellung entsprechend dem Web-Markup: Cover-Bild (`AspectFill`,
   Gegenstück zu `background-size: cover`), responsiv begrenzte Höhe
   (Web-Referenz `clamp(260px, 42vw, 520px)`), geschichtete Dunkelgradienten
   (seitlicher Verlauf, Bottom-Fade in den Seitenhintergrund, dezente
   Primär-/Sekundär-Akzente), Kicker-Chip „Private Media Library", Titel,
   Beschreibungstext; Trennlinie am unteren Rand (`VpBorder`).
3. Der Bildabruf nutzt den Bearer-Mechanismus des angemeldeten
   `VideoPlayerApiClient` (stille Token-Erneuerung eingeschlossen).
4. Das Hero-Bild wird nicht dauerhaft gespeichert — ein flüchtiger
   (Arbeitsspeicher-)Zustand genügt.
5. Offline- und Fehlerfall: die Startseite bleibt stabil — das Bild wird
   sauber ausgelassen (Hero zeigt nur Gradienten/Texte) bzw. der Abruf
   scheitert ohne Layout-Bruch oder Absturz.
6. Einstellung „Kopfbereich anzeigen" auf der Einstellungsseite: positiv
   formulierter Schalter, Standardwert aktiviert, wirksames Ein-/Ausblenden,
   Persistenz wie die übrigen App-Einstellungen (überlebt Neustart,
   profilpräfixiert, app-lokal).
7. Definition of Done: `dotnet build VideoPlayer.App.sln` in Debug **und**
   Release fehlerfrei, volle Testsuite
   `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` grün.

## Betroffene Klassen und Komponenten

### Datenmodell / Einstellungen (`VideoPlayer.Storage`)

- `AppSettings` — neue Eigenschaft `HomeHeroEnabled` (`bool`, Standard `true`).
- `AppSettingsService` — neuer Ablageschlüssel `HomeHeroEnabled` in `Load()`/`Save()`.

### API-Client (`VideoPlayer.ApiClient`)

- `VideoPlayerApiClient` — neue Methode zum Abruf des Hero-Bilds
  (`GET api/pictures/hero-background` → `byte[]`), analog `GetPictureAsync`/
  `GetSourceIconAsync` (Retry-Strategie, Bearer, stille Neuanmeldung).

### MVU-Kern (`VideoPlayer.Mvu`, plattformneutral — keine MAUI-Typen)

- `AppModel` — neuer Hero-Zustand (Ladezustand + Bilddaten als `byte[]`,
  flüchtig) sowie eine Sichtbarkeits-Ableitung aus `Settings.HomeHeroEnabled`.
- Neue Zustands-Record `HomeHeroState` (analog `SourcesOverviewState` u. a.).
- `AppMessage` — neue Nachricht für das Ladeergebnis des Hero-Bilds.
- `AppEffect` — neuer Effekt zum Laden des Hero-Bilds.
- `AppUpdate` — Auslösen des Lade-Effekts beim Verbindungsaufbau
  (`EnterConnected`, nur wenn aktiviert und noch nicht geladen/ladend) sowie
  beim Aktivieren der Einstellung im verbundenen Zustand
  (`UpdateSaveSettingsRequested`); Übernahme des Ladeergebnisses ins Model.

### UI (`VideoPlayer.Maui`)

- `Pages/HomePage.xaml` — neuer Hero-Abschnitt oberhalb der Listen
  (Bildebene + Gradienten-Überlagerungen + Inhalt: Kicker-Chip, Titel,
  Beschreibung), Höhe sinngemäß responsiv (`clamp(260px, 42vw, 520px)`).
- `Pages/HomePage.xaml.cs` — Sichtbarkeit aus dem Model ableiten, Bilddaten in
  `ImageSource` umwandeln, responsive Höhe an die Seitenbreite koppeln.
- `Pages/SettingsPage.xaml` + `SettingsPage.xaml.cs` — neuer Schalter
  „Kopfbereich anzeigen" (positiv formuliert), Verdrahtung wie die übrigen
  Switches über `SaveSettingsRequestedMessage`.
- `Services/ShellAppEffectHandler.cs` — Ausführung des neuen Lade-Effekts über
  den angemeldeten API-Client (`AuthSessionService.Client`), Ergebnis-Rückmeldung
  in den MVU-Zyklus; Fehlschlag = sauberes Auslassen (Debug-Log-Muster).
- `Resources/Styles/Colors.xaml` — ausschließlich vorhandene `Vp*`-Tokens
  verwenden (keine neuen Hexfarben in Seiten; transluzente Varianten der
  Tokens für die Gradienten).

### Tests (`VideoWebPlayer.Maui.Tests`)

- `AppSettingsTests` — Standardwert `HomeHeroEnabled = true`.
- `AppSettingsServiceTests` — Schlüssel/Persistenz-Roundtrip der neuen Einstellung.
- `AppUpdateTests_*` (neues Thema HomeHero bzw. Settings/HomeLists) —
  Lade-Effekt beim Verbindungsaufbau, Toggle-Wirkung im Model, kein erneutes
  Laden bei vorhandenem Bild, Fehlschlag → Bildlos bei stabilem Zustand.
- `VideoPlayerApiClientTests_Endpoints` — Endpunkt-Abruf `api/pictures/hero-background`.
- `SettingsStorageContractTests` — neuer Schalter auf der Einstellungsseite
  (AutomationId, Switch-Anzahl).
- `HomePageContractTests` — Hero-Markup (Kicker, Titel, Beschreibung,
  Gradienten, Bildbindung) und Effekt-Verdrahtung als Quell-Vertragstests
  (MAUI-Schicht ist im Testprojekt nicht instanziierbar — etabliertes Muster).

## Implementierungsansatz

- MVU-konform: der Hero-Zustand lebt im `AppModel` (Bilddaten als `byte[]` —
  `ImageSource` ist ein MAUI-Typ und darf nicht in den plattformneutralen
  MVU-Kern); die `HomePage` wandelt die Bytes in `ImageSource.FromStream` um
  (Muster: `PosterImageService`/`CachedPosterImage`, aber ohne Dateiablage).
- Der Ladevorgang ist ein einmaliger Effekt je verbundenem Zustand
  (erneut nur nach Fehlschlag oder wenn noch kein Bild vorliegt); er wird bei
  `EnterConnected` sowie beim Einschalten der Einstellung im verbundenen
  Zustand ausgelöst. Es gibt keine Aktualisierungs-Pflicht bei Listenänderung.
- Fehlerfall: `HeroBackgroundLoadedMessage` ohne Bilddaten → `IsLoading`
  entfällt, der Hero bleibt mit Gradienten/Text stehen (Web-Verhalten: bei
  fehlendem Bild greifen die Gradienten über `--vp-bg-deep`), keine
  Exception an die Oberfläche.
- Titel: im Web steht der konfigurierte Anwendungstitel (`applicationTitle`,
  Server-Programmeinstellung — über die öffentliche API nicht verfügbar).
  Annahme/Entscheidung: die App zeigt als Titel den Anzeigenamen des aktiven
  Servers (`KnownServers`-Eintrag zur `ActiveServerAddress`,
  `NameOrAddress`-Konvention), Fallback „VideoPlayer".

## Konfiguration

- Neue App-Einstellung `HomeHeroEnabled` (Ablageschlüssel `HomeHeroEnabled`),
  positiv formuliert („Kopfbereich anzeigen"), Standardwert `true`,
  profilpräfixiert über `AppProfile.PrefixKey`, nicht-geheime Ablage —
  konsistent zu den bestehenden acht Einstellungen.

## Offene Fragen

Keine — die Vorgaben (Endpunkt, Darstellung, positive Einstellung mit
Standard „an", flüchtiger Cache, Offline-Stabilität) sind eindeutig. Die
Titel-Annahme ist oben als Entscheidung markiert (kein API-Endpunkt für den
serverseitigen Anwendungstitel vorhanden).
