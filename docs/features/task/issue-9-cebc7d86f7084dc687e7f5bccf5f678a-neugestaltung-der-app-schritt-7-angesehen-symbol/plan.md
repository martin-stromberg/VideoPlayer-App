# Umsetzungsplan: Episodenlisten — Angesehen-Symbol bei gesehenen Episoden

## Übersicht

Das vom Server bereits gelieferte Feld `WatchedAt` wird in das App-Datenmodell
übernommen (`MediaEntry` → `EpisodeItem` → `CachedShowEpisode`) und in der
Episodenliste der `MediaDetailsPage` als rundes Augen-Badge nach Web-Vorbild
(`WatchedIndicator`, `.watched-indicator-episode -image-left`) dargestellt.
Zusätzlich wird der Anzeigestand der geladenen Detailansicht aktualisiert,
sobald der Player eine Episode als „angesehen" markiert. Keine
Server-Änderung, keine Änderung der Gesehen-Logik selbst.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| `WatchedAt`-Träger im API-Client | Basisklasse `MediaEntry` (nicht nur `TvShowEpisode`) | Server befüllt das Feld auf allen Einträgen (Episoden, Kollektions-Filme); die Basis deckt künftige Badge-Orte (Filmkarten) ohne erneute Modelländerung ab — Empfehlung des Projekt-Plan-Checks |
| Badge-Rendering | `Border` (Pill) + `Path`/`Ellipse`-Shapes direkt im `DataTemplate` | Nachbau des Web-SVG als Vektor-Shapes; kein Bildasset, kein neues Control nötig (einzige Verwendungsstelle). Overlay-Muster existiert bereits: `MediaCarousel.xaml` `PlaceholderBadge` |
| Badge-Farben | Neue Tokens `VpBadgeBg` (#AD000000 ≙ `rgba(0,0,0,.68)`) und `VpBadgeBorder` (#47FFFFFF ≙ `rgba(255,255,255,.28)`) in `Colors.xaml` | `VpOverlay` ist per `DesignTokensContractTests` auf #80000000 gepinnt; Hexfarben/Literale sind in Seiten-XAML verboten → Token in `Colors.xaml` ist der einzig konforme Ort |
| Glanzpunkt-Farbe | `VpText` (#e5e2e1) | App-Weißton; Literal `White` ist per Contract-Test verboten, ein eigenes Token für einen 1-Pixel-Glanzpunkt wäre überzogen |
| In-Session-Aktualisierung | Direkte `with`-Markierung des `EpisodeItem` im geladenen `MediaDetailsView` innerhalb von `UpdatePlaybackProgress`/`UpdatePlaybackEnded` | MVU-rein, ohne neue Message/Effekt; die Identität liegt im `PlayerState`. Nötig, weil die Seite Details nur einmal pro Instanz anfragt und identische Ziele nicht neu lädt |
| Zeitpunkt der lokalen Markierung | `DateTime.UtcNow` | Für die Badge-Sichtbarkeit zählt nur `HasValue`; der exakte Server-Zeitpunkt kommt beim nächsten Laden |
| Offline-Parität | `WatchedAt` auch in `CachedShowEpisode`/`MapOfflineShow` | Gleicher Anzeigestand im Offline-Snapshot; JSON-Schemawechsel ist abwärtskompatibel (fehlendes Feld → `null` = ungesehen) |

## Programmabläufe

### Online-Ladung der Episodenliste

1. `MediaDetailsService.LoadAsync` → `FetchShowAsync` lädt die Serie
   (`api/items/tvshow/{id}`, Episoden tragen `WatchedAt` in der JSON-Antwort)
   und schreibt die Struktur in `ShowDetailsCache` (jetzt inkl. `WatchedAt`).
2. `MapShow` → `MapSeasons` erzeugt `EpisodeItem`s mit `WatchedAt = e.WatchedAt`.
3. `MediaDetailsPage.UpdateUi` → `UpdateSeasonSection` setzt
   `EpisodesCollection.ItemsSource`; das Template blendet das Badge bei
   `IsWatched` (`WatchedAt.HasValue`) ein.

Beteiligte Klassen/Komponenten: `VideoPlayerApiClient`, `MediaEntry`/`TvShowEpisode`,
`MediaDetailsService`, `EpisodeItem`, `MediaDetailsPage`.

### Offline-Snapshot der Episodenliste

1. `LoadOfflineAsync` liest `CachedShowEntry` aus `ShowDetailsCache`.
2. `MapOfflineShow` erzeugt `EpisodeItem`s mit `WatchedAt` aus dem Cache
   (Alteinträge ohne Feld → `null` → kein Badge).

Beteiligte Klassen/Komponenten: `ShowDetailsCache`, `CachedShowEpisode`,
`MediaDetailsService.MapOfflineShow`.

### Markierung beim Gesehen-Werden (In-Session)

1. `UpdatePlaybackProgress`: sobald `remaining <= WatchedEndWindowSeconds`
   und `IsWatched` neu gesetzt wird, wird — bei `MediaType == "tvshowepisode"`
   — das `EpisodeItem` mit der gespielten `MediaId` im geladenen
   `MediaDetailsView` markiert (`WatchedAt = DateTime.UtcNow`, nur wenn noch
   leer). Die Markierung erfolgt vor einem etwaigen Auto-Fortsetzungs-
   Übergang (`AdvanceToNextEpisode` wechselt `MediaId`).
2. `UpdatePlaybackEnded`: dieselbe Markierung vor der Abschlussmeldung/
   Auto-Fortsetzung.
3. `MediaDetailsPage` rendert über `ModelChanged` → `UpdateUi` →
   `ItemsSource`-Neuzuweisung; das Badge erscheint ohne Seitenreload.

Beteiligte Klassen/Komponenten: `AppUpdate` (private Hilfsmethode),
`PlayerState`, `MediaDetailsView`, `EpisodeItem`.

## Neue Klassen

Keine — alle Änderungen erweitern bestehende Records/Seiten.

## Änderungen an bestehenden Klassen

### `MediaEntry` (Datenmodellklasse, `VideoPlayer.ApiClient/Models/MediaEntry.cs`)

- **Neue Eigenschaften:** `WatchedAt` (`DateTime?`) — Gesehen-Zeitpunkt, den
  der Server per `WatchedStatusService.EnrichAsync` mitliefert; `null` =
  ungesehen.

### `EpisodeItem` (Datenmodellklasse, `VideoPlayer.Mvu/MediaDetailsView.cs`)

- **Neue Eigenschaften:** `WatchedAt` (`DateTime?`) — Gesehen-Zeitpunkt der
  Episode; `IsWatched` (`bool`, abgeleitet: `WatchedAt.HasValue`) —
  Bindeziel der Badge-Sichtbarkeit.

### `CachedShowEpisode` (Datenmodellklasse, `VideoPlayer.Mvu/ShowDetailsCache.cs`)

- **Neue Eigenschaften:** `WatchedAt` (`DateTime?`) — gecachter
  Gesehen-Zeitpunkt des Offline-Serienkontexts.

### `ShowDetailsCache` (Logikklasse, `VideoPlayer.Mvu/ShowDetailsCache.cs`)

- **Geänderte Methoden:** `WriteAsync` — schreibt `WatchedAt` der
  `TvShowEpisode` in den Cache-Eintrag mit.

### `MediaDetailsService` (Logikklasse, `VideoPlayer.Mvu/MediaDetailsService.cs`)

- **Geänderte Methoden:** `MapSeasons` — mappt `e.WatchedAt` auf
  `EpisodeItem.WatchedAt`; `MapOfflineShow` — mappt `WatchedAt` aus
  `CachedShowEpisode`.

### `AppUpdate` (Logikklasse, `VideoPlayer.Mvu/AppUpdate.cs`)

- **Geänderte Methoden:** `UpdatePlaybackProgress` — beim Übergang auf
  `IsWatched` zusätzlich Markierung des `EpisodeItem` in den geladenen
  Details; `UpdatePlaybackEnded` — dieselbe Markierung vor Abschlussmeldung/
  Fortsetzung.
- **Neue Methoden:** private Hilfsmethode (z. B. `MarkEpisodeWatchedInDetails`)
  — liefert das Model mit markiertem `EpisodeItem`; greift nur bei
  `tvshowepisode`, vorhandenen `Seasons` und bislang leerem `WatchedAt`.

### `MediaDetailsPage` (UI, `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml`)

- Episoden-`DataTemplate`: `CachedPosterImage` wird in ein `Grid` gehüllt;
  darüber liegt das Watched-Badge (`IsVisible="{Binding IsWatched}"`,
  `HorizontalOptions="Start"`, `VerticalOptions="Start"`, kleiner Margin
  ≙ Web `0.6rem`): `Border` mit `RoundRectangle 999`,
  `BackgroundColor="{StaticResource VpBadgeBg}"`,
  `Stroke="{StaticResource VpBadgeBorder}"`, `StrokeThickness="1"`;
  darin das Auge als `Path` (SVG-Kontur `M2 12C4 7 8 4 12 4C16 4 20 7 22 12C20 17 16 20 12 20C8 20 4 17 2 12Z`,
  `Stroke`/`Fill`-Verhalten wie Web: Kontur gestrichelt in `VpPrimarySoft`)
  plus Pupille (`Ellipse`, `VpPrimarySoft`) und Glanzpunkt (`Ellipse`,
  `VpText`, versetzt) — Größe ≙ Web `2rem` (~28–32 px Badge,
  ~18–24 px Auge, über `Path.Stretch="Uniform"` skaliert).

### `Colors.xaml` (Ressourcen, `VideoPlayer.Maui/Resources/Styles/Colors.xaml`)

- **Neue Tokens:** `VpBadgeBg` = `#AD000000` (Web `rgba(0,0,0,.68)`),
  `VpBadgeBorder` = `#47FFFFFF` (Web `rgba(255,255,255,.28)`).

## Datenbankmigrationen

Keine — `WatchedAt` wird nicht persistiert (nur JSON-Cache-Ablage in
`cached_lists`, schemalos).

## Validierungsregeln

Keine — kein neuer Benutzerinput.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Serialisierung:** `MediaEntry.WatchedAt` wird bei eigenen
  Serialisierungen (z. B. `CachedFavoritesEntry.Json`,
  `StubHttpMessageHandler`-Testpayloads) mitgeschrieben — harmlos
  (Server/Deserializer tolerieren das Feld).
- **`EnumCoverageTests`:** keine neuen Enum-Member — unberührt.
- **Weiterschauen-/Favoriten-Listen:** `MediaEntry`-Ableitungen werden auch
  für Home-/Favoriten-Modelle deserialisiert; dort wird `WatchedAt` nicht
  gelesen — kein Verhaltenseffekt.
- **Badge-Größe:** das Web-Badge (2rem auf ~170 px Poster) wirkt auf dem
  62 px hohen Episoden-Still der App proportioniert größer — die Umsetzung
  wählt eine zum Poster passende Größe (~24–28 px) bei unverändertem
  Web-Motiv (Pill, Position, Farben).
- **Server-Push anderer Geräte:** `ContinueWatchingUpdated` aktualisiert nur
  die Startliste — eine offene Detailseite zeigt fremdgerätige
  Watched-Änderungen erst beim nächsten Laden (Deckung mit Akzeptanzpunkt 3:
  die eigene Wiedergabe aktualisiert sofort).

## Umsetzungsreihenfolge

1. **Datenmodell `MediaEntry.WatchedAt`**
   - Voraussetzungen: Keine.
   - Beschreibung: `DateTime? WatchedAt` an `MediaEntry` ergänzen (deutsche
     XML-Doku, CS1591 = Fehler).

2. **MVU-Modelle: `EpisodeItem.WatchedAt`/`IsWatched` + `CachedShowEpisode.WatchedAt`**
   - Voraussetzungen: Keine (unabhängig von Schritt 1, aber sinnvoll danach).
   - Beschreibung: Eigenschaften mit XML-Doku ergänzen; `IsWatched` als
     abgeleitete Eigenschaft.

3. **Mapping: `ShowDetailsCache.WriteAsync`, `MediaDetailsService.MapSeasons`/`MapOfflineShow`**
   - Voraussetzungen: Schritte 1–2.
   - Beschreibung: `WatchedAt` in Cache-Schreibung, Online- und
     Offline-Mapping durchreichen.

4. **In-Session-Aktualisierung in `AppUpdate`**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: Hilfsmethode + Aufruf in `UpdatePlaybackProgress`
     (IsWatched-Übergang) und `UpdatePlaybackEnded` vor
     `AdvanceToNextEpisode`.

5. **UI: Tokens + Badge im Episoden-Template**
   - Voraussetzungen: Schritt 2 (`IsWatched`-Bindeziel).
   - Beschreibung: `VpBadgeBg`/`VpBadgeBorder` in `Colors.xaml`;
     `CachedPosterImage` in `Grid` hüllen und Badge-`Border` mit
     Augen-`Path`/`Ellipse`s einbauen.

6. **Neue Tests**
   - Voraussetzungen: Schritte 1–5.
   - Beschreibung: Serialisierungs-, Mapping-, Übergangs- und
     Vertragstests (siehe Abschnitt „Tests").

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `MediaEntry_DeserializesWatchedAt` | `MediaEntrySerializationTests` | Episode-JSON mit `watchedAt` → `WatchedAt` gesetzt; ohne Feld → `null` |
| `Load_TvShow_MapsEpisodeWatchedAt` | `MediaDetailsServiceTests` | Serienantwort mit `WatchedAt` auf einer Episode → `EpisodeItem.WatchedAt`; andere Episoden `null` |
| `Load_OfflineShow_MapsEpisodeWatchedAt` | `MediaDetailsServiceTests` | `CachedShowEpisode.WatchedAt` → Offline-`EpisodeItem.WatchedAt` (Alt-Cache ohne Feld → `null`) |
| `PlaybackEnded_MarksEpisodeWatchedInDetails` | `AppUpdateTests_Player` | `PlaybackEndedMessage` → `EpisodeItem.WatchedAt` der gespielten Episode gesetzt, andere unverändert |
| `PlaybackProgress_WatchedWindow_MarksEpisodeWatchedInDetails` | `AppUpdateTests_Player` | Schwelle erreicht → Markierung; Movie-Playback berührt Episodenliste nicht |
| `MediaDetailsPage_EpisodeWatchedBadge_*` | `MediaDetailsPageContractTests` | Badge-Markup: `IsWatched`-Binding, `RoundRectangle 999`, `VpBadgeBg`/`VpBadgeBorder`, `VpPrimarySoft`-Auge, Position oben links über dem Poster (`Grid`-Hülle) |
| `Colors_DefinesBadgeTokens` | `DesignTokensContractTests` | `VpBadgeBg` #AD000000 und `VpBadgeBorder` #47FFFFFF in `Colors.xaml` |

### Betroffene bestehende Tests

Keine — die Änderungen sind additiv (neue optionale Felder, neues Template-
Element, zusätzliche Modellaktualisierung). Der Baseline-Lauf (728 Tests)
muss ohne Anpassung grün bleiben.

### E2E-Tests (primärer Funktionsnachweis)

Das Repository enthält keinerlei UI-E2E-Infrastruktur für die MAUI-App
(kein Playwright/Appium/FlaUI im Testprojekt — nur xUnit auf `net10.0`);
die MAUI-Schicht ist dort nicht instanziierbar. Der etablierte
Funktionsnachweis für UI-Schritte (Schritt 5 Hero, Schritt 6 Detail-Header)
sind MVU-Übergangstests plus Quell-Vertragstests — dieser Plan folgt
demselben Muster. Keine bestehenden E2E-Tests betroffen.

## Offene Punkte

Keine.
