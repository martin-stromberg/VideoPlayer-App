# Übersetzte Anforderung — Genre-Filter im Quellen-Browsing (Schritt 1)

Quelle: Kundenanforderung aus dem Projektplan `docs/projects/task/issue-49-…-rueckmeldungen-quellen-browsin/` (Entwicklungsschritt 1), unverändert übernommen. `docs/features.md` existiert nicht — Kontext wurde aus `docs/help/` und dem Code hergestellt.

## Fachliche Zusammenfassung

In der Übersichtsseite einer geöffneten Quelle (`SourceOverviewPage`, Shell-Route `sourceoverview`) steht dem Anwender kein funktionaler Genre-Filter zur Verfügung, obwohl die horizontale Chip-Leiste (`GenresCollection`) und der komplette MVU-Ladepfad bereits implementiert sind. Zu diagnostizieren und zu dokumentieren ist, warum die Leiste im Auslieferungsstand nicht nutzbar ist (Kandidaten: leere Serverantwort von `api/SourceGenres/{id}`, fehlgeschlagener Ladevorgang, fehlende Leiste). Die Korrektur stellt das Soll-Verhalten her: Liefert der Server Genres, ist die Leiste sichtbar („Alle" + Quellen-Genres); Genre-Auswahl lädt die Trefferliste gefiltert ab Seite 0 neu; „Alle" hebt den Filter auf. Bei nachweislich leerer Server-Genreliste (verbindliche Projektklärung) wird die Leiste ausgeblendet — keine funktionslose „Alle"-nur-Zeile. Server und API-Verträge bleiben unverändert; eine serverseitige Ursache wird nur dokumentiert.

## Betroffene Klassen und Komponenten

- **UI:** `VideoPlayer.Maui/Pages/SourceOverviewPage.xaml` (`GenresCollection`, Zeilen 24–58 — derzeit ohne `IsVisible`-Bindung) und ggf. `SourceOverviewPage.xaml.cs` (`UpdateUi`, `OnGenreSelected`).
- **Zustand (MVU):** `VideoPlayer.Mvu/SourceBrowserState.cs` — `Genres` (`IReadOnlyList<SourceGenreItem>`), `SelectedGenreId`; ggf. neue berechnete Eigenschaft für die Sichtbarkeit der Leiste.
- **Ladepfad (bereits vorhanden, Diagnose-/Verifikationsgegenstand):**
  - `SourceSelectedMessage` / `SourceBrowserOpenedMessage` → `AppUpdate.UpdateSourceSelected` / `UpdateSourceBrowserOpened` (`AppUpdate.cs` ~1525–1571) → `LoadSourceGenresEffect` (`AppEffect.cs:221`).
  - `ShellAppEffectHandler.LoadSourceGenresAsync` (`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs:816-833`) → `SourceBrowserService.LoadGenresAsync` (`VideoPlayer.Mvu/SourceBrowserService.cs:74-99`) → `VideoPlayerApiClient.GetSourceGenresAsync` (`GET api/SourceGenres/{id}`, `VideoPlayer.ApiClient/VideoPlayerApiClient.cs:233-237`) → `SourceGenresLoadedMessage` (`AppMessage.cs:326`) → `AppUpdate.UpdateSourceGenresLoaded` (`AppUpdate.cs:1573-1593`).
  - Fallback: Ladefehler → `Genres = [Alle]` (nur geloggt, kein Fehlerzustand); leere Serverliste → `LoadGenresAsync` liefert ebenfalls nur `[Alle]` (`SourceBrowserService.AllGenresName`, `Id = 0`).
- **Filter-Anwendung (bereits vorhanden):** `SourceGenreSelectedMessage` → `AppUpdate.UpdateSourceGenreSelected` (`AppUpdate.cs:1678-1694`) → `ReloadBrowserItems` (`AppUpdate.cs:1654-1676`) → `LoadSourceItemsEffect` mit `genreId` → `api/items?…&genreId=…`.
- **API-Modelle (unverändert):** `SourceGenres` / `Genre` (`VideoPlayer.ApiClient/Models/MediaSource.cs`).
- **Tests:** `VideoWebPlayer.Maui.Tests` — `AppUpdateTests_Sources.cs`, `SourceBrowserServiceTests.cs`; ggf. `VideoPlayer.E2E.Tests` (`MockVideoServer`, FlaUI).

## Implementierungsansatz

- **Diagnose (Dokumentationspflicht):** Codebasiert feststellen, welcher der drei Kandidaten zutrifft. Feststellbar aus dem Code: Die Leiste existiert im Auslieferungsstand (`GenresCollection` vorhanden, `ItemsSource` gebunden). Sie rendert unsichtbar, solange `Genres` leer ist, und funktionslos, wenn `Genres` nur den „Alle"-Eintrag enthält — genau dann, wenn `api/SourceGenres/{id}` eine leere Liste liefert oder der Aufruf fehlschlägt (Fallback `[Alle]`). Ob der Endpunkt für Serien-Quellen serverseitig leere Listen liefert, ist ohne laufenden Server nicht beweisbar → dokumentieren.
- **Korrektur:** Sichtbarkeit der Leiste an „reale Genres vorhanden" koppeln — z. B. berechnete Eigenschaft auf `SourceBrowserState` (`Genres.Count > 1`, da Eintrag 0 stets das synthetische „Alle" ist) und `IsVisible` auf `GenresCollection` binden oder in `UpdateUi` setzen. Damit deckt die Korrektur beide clientseitig nicht unterscheidbaren Ursachen ab (leere Antwort, Ladefehler-Fallback) — und es wird nie eine „Alle"-nur-Zeile gezeigt.
- Keine neue Bedienform: Die vorhandene Chip-Leiste bleibt die Soll-Form; Auswahl- und „Alle"-Logik (`SelectedGenreId` 0 = kein Filter) bleibt unverändert.
- Unit-Tests: `SourceGenresLoaded` mit echten Genres → Leiste sichtbar (`ShowGenreFilter`-Eigenschaft wahr); mit nur „Alle" → Leiste ausgeblendet; Service-Test: leere Serverliste → nur „Alle". E2E: prüfen, ob `MockVideoServer` die Quellen-/Genre-Routen abdeckt (derzeit: `api/Sources` leere Liste, `api/SourceGenres` unbekannt → 404); ggf. Route für ein Source-Browsing-Szenario ergänzen — Ausführbarkeit vor Ort klären, sonst dokumentieren.

## Konfiguration

Keine — Verhalten ist nicht konfigurierbar gefordert.

## Offene Fragen

(Projektklärung bereits verbindlich: Bei leerer Server-Genreliste wird die Leiste ausgeblendet; serverseitige Ursache nur dokumentieren, Server/API unverändert.)

- ~~Sichtbarkeit bei Ladefehler?~~ → Selbst entschieden (Nutzer nicht erreichbar): Der Ladefehler-Fallback erzeugt dieselbe „Alle"-nur-Liste — aus Anwendersicht eine funktionslose Zeile, die laut Anforderung nicht dargestellt werden soll. Die Sichtbarkeitsregel `Genres.Count > 1` blendet sie einheitlich aus.
- E2E-Nachweis: FlaUI-Tests benötigen eine laufende App; falls auf diesem System nicht ausführbar, wird die Lücke dokumentiert statt erfunden (Vorgabe des Auftrags).
