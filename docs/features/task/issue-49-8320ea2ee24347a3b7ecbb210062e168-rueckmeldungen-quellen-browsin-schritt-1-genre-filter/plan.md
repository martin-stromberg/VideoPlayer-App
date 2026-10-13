# Umsetzungsplan: Genre-Filter im Quellen-Browsing (Schritt 1)

## Übersicht

Diagnose und Korrektur des fehlenden Genre-Filters auf der `SourceOverviewPage`: Die Chip-Leiste und der komplette MVU-Ladepfad existieren; der Filter ist nicht nutzbar, weil `SourceBrowser.Genres` im Befundfall nur den synthetischen „Alle"-Eintrag enthält (leere Serverantwort oder Ladefehler-Fallback) und die Leiste keine Sichtbarkeitsregel besitzt. Umgesetzt wird eine abgeleitete Sichtbarkeitsregel (Leiste nur bei realen Server-Genres), dokumentierte Diagnose, Unit-Tests sowie E2E-Abdeckung des Benutzerflusses über die vorhandene FlaUI-/Mock-Server-Infrastruktur.

## Diagnoseergebnis (Teil der Anforderung — dokumentiert)

| Kandidat | Befund |
|----------|--------|
| Leiste fehlt im Auslieferungsstand | **Widerlegt:** `GenresCollection` ist deklariert und gebunden (`SourceOverviewPage.xaml:24-58`); komplette Kette `LoadSourceGenresEffect` → `LoadSourceGenresAsync` → `LoadGenresAsync` → `GET api/SourceGenres/{id}` → `SourceGenresLoadedMessage` → `UpdateSourceGenresLoaded` ist verdrahtet (Effect-Switch `case LoadSourceGenresEffect`, `ShellAppEffectHandler.cs:~190`). |
| Ladefehler | **Möglich, nicht beweisbar ohne Server:** Fehler werden geschluckt (`LogError`, Fallback `[Alle]`). Auswirkung identisch zur leeren Antwort. |
| Leere Serverantwort | **Wahrscheinliche Ursache:** Liefert `api/SourceGenres/{id}` für die Serien-Quelle `genres: []`, enthält `Genres` nur `[Alle]` → die Leiste zeigt eine einzige funktionslose Chip bzw. wirkt wie „kein Genre-Filter". Ohne laufenden Server nicht final beweisbar; der Altbestand zeigt bei derselben Serverantwort dasselbe Bild (`SourceOverviewViewModel.LoadGenresAsync`). |

**Folgerung:** Clientseitig ist die Unterscheidung „leere Antwort" vs. „Ladefehler" ohne Log nicht möglich; beide Pfade münden in `Genres = [Alle]`. Die Korrektur behandelt beide einheitlich: keine funktionslose „Alle"-nur-Zeile (verbindliche Projektklärung). Eine etwaige serverseitige Ursache (Endpunkt liefert für Serien-Quellen leere Genre-Liste) wird dokumentiert — Server/API unverändert.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Sichtbarkeitsregel | Neue abgeleitete Eigenschaft `SourceBrowserState.ShowGenreFilter` (`Genres.Any(g => g.Id > 0)`) + `IsVisible`-Binding auf `GenresCollection` | Konsistent mit den vorhandenen abgeleiteten Flags (`HasItems`, `ShowEmpty`) und den Seiten-Bindings (`IsLoading`); deckt „leere Antwort" und „Ladefehler-Fallback" einheitlich ab — beide ergeben `Genres = [Alle]` (nur `Id = 0`, kein `Id > 0`). Ein `Id > 0`-Test statt `Count > 1` ist robuster, falls die Liste je ohne „Alle" aufgebaut würde. |
| Ladefehler-Verhalten | Fallback `[Alle]` beibehalten, Leiste dadurch ausgeblendet | Anforderung: „keine funktionslose Filterzeile"; kein neuer Fehler-/Retry-Pfad gefordert. Der Eintrags-Retry (`SourceItemsRetryRequestedMessage`) lädt weiterhin nur Einträge neu — Genre-Neuladen ist nicht Teil der Anforderung. |
| Sichtbarkeitssteuerung im View | XAML-Binding `IsVisible="{Binding Model.SourceBrowser.ShowGenreFilter}"` statt Code-Behind in `UpdateUi` | Muster der `InitialLoadingIndicator`/`FooterLoadingIndicator`-Bindungen auf derselben Seite; `UpdateUi` bleibt für die imperativ verwalteten Panels. Kein zusätzlicher Code-Behind nötig. |
| E2E-Nachweis | `MockVideoServer` um Opt-in-Quellen-Routen erweitern + `DevApiModelSnapshot`-Felder + neue Testklasse `SourceBrowsingTests` | UI-Fluss (Menü → Quelle → Genre-Chip → gefiltertes Neuladen) ist der primäre Funktionsnachweis; Infrastruktur (`UiDriver`, `DevApi` `/model`/`/tree`/`/invoke`, `MockVideoServer`) existiert und ist erweiterbar. Ausführbarkeit (maui-windows-Workload vorhanden) wird im Testschritt verifiziert; falls nicht ausführbar → dokumentieren statt erfinden. |
| AutomationIds | `AutomationId` auf `GenresCollection` und `ItemsCollection` | DevApi `/invoke` (itemIndex) und `/tree`-Sichtbarkeitsprüfung brauchen AutomationIds; bestehende Seiten vergeben sie flächendeckend (z. B. `SeasonPicker`, `EpisodesList`). |

## Programmabläufe

### Genre-Leiste sichtbar bei Server-Genres (Soll)

1. `SourceSelectedMessage`/`SourceBrowserOpenedMessage` → `NewBrowser` + `LoadSourceGenresEffect` + `LoadSourceItemsEffect`.
2. `ShellAppEffectHandler.LoadSourceGenresAsync` → `SourceBrowserService.LoadGenresAsync` → `api/SourceGenres/{id}` liefert Genres → `SourceGenresLoadedMessage` → `UpdateSourceGenresLoaded` setzt `Genres = [Alle, …reale Genres]`.
3. `ShowGenreFilter` (= `Genres.Any(g => g.Id > 0)`) wird `true` → `GenresCollection.IsVisible = true` (Binding) → Leiste mit „Alle" + Genres sichtbar.
4. Nutzer wählt Genre → `OnGenreSelected` → `SourceGenreSelectedMessage(id)` → `UpdateSourceGenreSelected` → `SelectedGenreId = id`, `IsSelected` auf dem Chip, `ReloadBrowserItems` → `LoadSourceItemsEffect(Seite 0, genreId = id)` → gefilterte Liste ab erster Seite.
5. Nutzer wählt „Alle" (`Id = 0`) → `SelectedGenreId = 0` → `LoadSourceItemsEffect` ohne `genreId` → Filter aufgehoben.

### Keine Server-Genres oder Ladefehler (Korrektur)

1. `api/SourceGenres/{id}` liefert `genres: []` **oder** wirft → `LoadGenresAsync`/`ShellAppEffectHandler`-Fallback liefern `[Alle]` → `Genres` enthält kein `Id > 0`.
2. `ShowGenreFilter` bleibt `false` → `GenresCollection` unsichtbar (keine Höhe im `Auto`-Row) → keine funktionslose „Alle"-Zeile.
3. Vor dem Laden (`Genres = []`) bleibt die Leiste ebenfalls unsichtbar — visuell unverändert zum Ist (leere `CollectionView` renderte bereits ohne Höhe).

Beteiligte Klassen/Komponenten: `SourceBrowserState`, `SourceGenreItem`, `SourceOverviewPage` (XAML), `AppUpdate`, `ShellAppEffectHandler`, `SourceBrowserService`, `VideoPlayerApiClient`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `SourceBrowsingTests` | E2E-Testklasse (`VideoPlayer.E2E.Tests/Tests/`) | Pflichtszenarien Genre-Leiste sichtbar + Filter-Reload + „Alle" setzt zurück; Leiste ausgeblendet bei leerer Serverliste. |
| `MockSource` / `MockSourceGenre` / `MockSourceItem` | E2E-Support-Datentypen (`MockVideoServer.cs`-Datei bzw. `DevApiModels.cs`-Muster) | Konfigurierbare Quellen, deren Genre-Listen und Eintragsseiten (mit `GenreIds` pro Eintrag) des Mock-Servers. |

## Änderungen an bestehenden Klassen

### `SourceBrowserState` (`VideoPlayer.Mvu/SourceBrowserState.cs`)

- **Neue Eigenschaften:** `ShowGenreFilter` (`bool`, abgeleitet) — `true`, wenn `Genres` mindestens einen Eintrag mit `Id > 0` enthält (reales Server-Genre neben dem synthetischen „Alle"). XML-Doc im Stil der bestehenden Flags.

### `SourceOverviewPage` (`VideoPlayer.Maui/Pages/SourceOverviewPage.xaml`)

- `GenresCollection`: `IsVisible="{Binding Model.SourceBrowser.ShowGenreFilter}"` + `AutomationId="GenresCollection"` (E2E-Griff).
- `ItemsCollection`: `AutomationId="ItemsCollection"` (E2E-Griff für Eintragsassertionen über `/tree`-`Items`).

### `DevApiModelSnapshot` (`VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs`, `#if DEBUG || DEV_API`)

- **Neue Eigenschaften:** `SourceBrowserSourceId` (`long`), `SourceBrowserGenreNames` (`IReadOnlyList<string>`), `SourceBrowserSelectedGenreId` (`long`), `SourceBrowserShowGenreFilter` (`bool`), `SourceBrowserItemTitles` (`IReadOnlyList<string>`) — kuratierter Auszug des `SourceBrowser`-Zustands für `GET /model`, befüllt in `FromModel` (kein Geheimnis, Anzeigedaten).

### `MockVideoServer` (`VideoPlayer.E2E.Tests/Support/MockVideoServer.cs`)

- **Neue Konfiguration:** `Sources` (`List<MockSource>`: Id/Name), `SourceGenres` (Mapping `sourceId → List<MockSourceGenre>`; nicht konfiguriert = leere Liste), `SourceItems` (`List<MockSourceItem>`: Id/Title/Type/MediaSourceId/GenreIds).
- **Neue Routen:** `GET /api/SourceGenres/{id}` (Gate+Bearer wie Listen-Endpunkte; Antwort `{sourceId, sourceName, genres:[{id,name}]}`, Standard `genres: []`) und `GET /api/items` mit `mediaSourceId`-Query (gefiltert nach `genreId`, paginiert via `page`/`size`; jede Anfragepfad+Query in `SourceItemRequests` für Assertions).
- **Geänderte Methode:** `ListOverride` — `api/Sources` liefert bei konfigurierten `Sources` deren JSON statt der Leerliste.
- **Neue Eigenschaft:** `SourceItemRequests` (`List<string>`, rohe Request-Pfade mit Query — Nachweis des `genreId`-Parameters).

### `DevApiModel` (`VideoPlayer.E2E.Tests/Support/DevApiModels.cs`)

- **Neue Eigenschaften:** Spiegel der neuen Snapshot-Felder (`sourceBrowserGenreNames`, `sourceBrowserSelectedGenreId`, `sourceBrowserShowGenreFilter`, `sourceBrowserItemTitles`, `sourceBrowserSourceId`).

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine — keine neue Eingabe.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Sichtbarkeit der Leiste:** `Genres` ist bis zum `SourceGenresLoadedMessage` leer → Leiste unsichtbar während des Ladens (visuell unverändert, die leere `CollectionView` hatte bereits Höhe 0).
- **Ladefehler-Pfad:** Genre-Ladefehler blendet die Leiste nun aus statt eine „Alle"-Zeile zu zeigen — gewünscht (keine funktionslose Zeile); Fehlschlag bleibt im Protokoll.
- **Altbestand `VideoWebPlayer.Maui`:** unberührt (kein gemeinsamer Code).
- **`DevApiModelSnapshot`-Erweiterung:** Debug/DevApi-only (`#if DEBUG || DEV_API`) — Release-Verhalten unverändert.
- **Mock-Server-Routen:** Neue Routen nur Opt-in bzw. für bisher 404-fallende Pfade (`api/SourceGenres`, `api/items` mit `mediaSourceId`); `api/Sources`-Override nur bei konfigurierten `Sources` — bestehende E2E-Tests ohne Quellen-Konfiguration unverändert.

## Umsetzungsreihenfolge

1. **`SourceBrowserState.ShowGenreFilter` + Unit-Tests**
   - Voraussetzungen: keine.
   - Beschreibung: Abgeleitete Eigenschaft mit XML-Doc; Tests in `AppUpdateTests_Sources` (nur „Alle" → ausgeblendet; echte Genres → sichtbar) und `SourceBrowserServiceTests` (leere Serverliste → nur „Alle").

2. **UI-Anbindung `SourceOverviewPage.xaml`**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `IsVisible`-Binding + `AutomationId`s auf `GenresCollection`/`ItemsCollection`.

3. **`DevApiModelSnapshot`-Erweiterung + `DevApiModel`-Spiegel**
   - Voraussetzungen: Schritt 1 (Snapshot nutzt `ShowGenreFilter`).
   - Beschreibung: Felder + Befüllung in `FromModel`; Spiegel-DTO in `DevApiModels.cs`.

4. **`MockVideoServer`-Quellen-Routen**
   - Voraussetzungen: keine.
   - Beschreibung: `Sources`/`SourceGenres`/`SourceItems`-Konfiguration, `ListOverride`-Erweiterung, neue Routen `api/SourceGenres/{id}` und `api/items?mediaSourceId=…` inkl. `SourceItemRequests`-Aufzeichnung, Gate/Bearer-Prüfung wie bei den Listen-Endpunkten.

5. **E2E-Testklasse `SourceBrowsingTests`**
   - Voraussetzungen: Schritte 2–4.
   - Beschreibung: Szenarien „Leiste sichtbar + Genre wählen filtert + Alle hebt auf" und „leere Serverliste → Leiste nicht sichtbar".

6. **Dokumentation**
   - Voraussetzungen: Schritte 1–2.
   - Beschreibung: `docs/help/quellen-browsing-details.md` (Genre-Leiste nur bei Server-Genres sichtbar; Diagnosehinweis), `README.md`-Prüfung, `docs/RELEASE_NOTES.md`-Eintrag (Deutsch + Englisch, Muster der bestehenden Einträge).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `SourceGenresLoaded_OnlyAllEntry_HidesGenreFilter` | `AppUpdateTests_Sources` | `SourceGenresLoadedMessage` mit nur „Alle" → `ShowGenreFilter == false`. |
| `SourceGenresLoaded_ServerGenres_ShowsGenreFilter` | `AppUpdateTests_Sources` | Genres mit echten Einträgen → `ShowGenreFilter == true`, `IsSelected` passend. |
| `LoadGenres_EmptyServerList_ReturnsOnlyAllEntry` | `SourceBrowserServiceTests` | `genres: []` → Ergebnis `[Alle]` (pinnt das Diagnose-Verhalten). |
| `LoadGenres_InvalidServerEntries_AreSkipped` (optional, nur falls trivial) | `SourceBrowserServiceTests` | `Id <= 0`/leere Namen werden übersprungen → ggf. `[Alle]`. |
| `FromModel`/`Snapshot`-Pin für `SourceBrowser*`-Felder | `DevApiModelSnapshotTests` | Neue Felder transportieren `SourceBrowser`-Zustand. |

### Betroffene bestehende Tests

Keine — `ShowGenreFilter` ist eine neue abgeleitete Eigenschaft; bestehende Assertions bleiben unverändert.

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Quelle mit Server-Genres öffnen → Leiste mit „Alle" + Genres sichtbar; Genre-Chip wählen → `api/items`-Aufruf mit `genreId`, Liste ab Seite 0 ersetzt; „Alle" wählen → Aufruf ohne `genreId` | `SourceBrowsingTests.GenreBar_VisibleAndFilters_ResetsOnAll` | Soll: sichtbare Leiste, gefilterter Reload, „Alle" hebt Filter auf | Nutzerfluss über Flyout-Menü, Chip-Auswahl und Raster — nur End-to-End über die echte Seite verifizierbar |
| Pflicht | Quelle mit leerer Server-Genreliste → keine funktionslose Leiste (Element nicht sichtbar), Eintragsliste lädt normal | `SourceBrowsingTests.GenreBar_Hidden_WhenServerHasNoGenres` | Projektklärung: „Alle"-nur-Zeile wird ausgeblendet | Sichtbarkeitsregel wirkt im Renderpfad (`IsVisible`-Binding) — Unit-Test allein beweist nur den Modell-Flag |

Angepasste bestehende E2E-Tests: Keine.

## Offene Punkte

Keine — verbindliche Projektklärung liegt vor (leere Server-Genreliste → Leiste ausblenden; Server/API unverändert). Der Nutzer ist nicht erreichbar; die folgenden Punkte wurden per dokumentierter Annahme entschieden:

- **Ladefehler = ausblenden (nicht Fehler anzeigen):** Der Fehler-Fallback erzeugt dieselbe „Alle"-nur-Liste; eine funktionslose Zeile soll laut Anforderung nicht dargestellt werden. Annahme: einheitliche Regel `ShowGenreFilter`, Ladefehler bleibt im Protokoll (kein UI-Fehlerzustand — war bisher auch keiner).
- **Serverseitige Ursache dokumentieren statt beheben:** Server nicht im Repo; die Ursache „leere Genre-Liste für Serien-Quellen" kann hier nicht live verifiziert werden → Dokumentation in `docs/help/` + Release Notes.
- **E2E-Ausführung:** maui-windows-Workload ist installiert; `dotnet test VideoPlayer.E2E.Tests` wird versucht (gefiltert auf die neuen Tests). Misslingt der App-Build/Start in dieser Umgebung, wird die Suite als nicht ausführbar dokumentiert — der Code bleibt kompilierbar (`dotnet build`).
