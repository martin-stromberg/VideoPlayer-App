# Bestandsaufnahme: Genre-Filter im Quellen-Browsing (Schritt 1)

Analysiert wurde der Bereich Quellen-Browsing (`SourceOverviewPage` + MVU-Kreislauf) der neuen App (`VideoPlayer.Maui`/`VideoPlayer.Mvu`/`VideoPlayer.ApiClient`) bezogen auf die Anforderung „fehlender Genre-Filter in geöffneter Serien-Quelle". Grundlage zusätzlich: Projekt-Bestandsaufnahme unter `docs/projects/task/issue-49-…/inventory/` (Befund FA-1), die hier verifiziert und vertieft wurde.

## Zusammenfassung

- **Die Bedienleiste existiert im Auslieferungsstand:** `GenresCollection` ist in `SourceOverviewPage.xaml:24-58` deklariert, an `Model.SourceBrowser.Genres` gebunden, mit horizontalem `LinearItemsLayout`, Chip-Template („Alle"-Eintrag mit `Id = 0` zuerst) und `SelectionChanged="OnGenreSelected"`. **Kein `IsVisible`-Binding, kein Code-Behind-Schutz** — die Leiste rendert unsichtbar (Höhe 0), solange `Genres` leer ist, und als funktionslose Ein-Eintrag-Zeile, wenn `Genres` nur „Alle" enthält.
- **Der komplette Ladepfad ist vorhanden und verdrahtet:** `SourceSelectedMessage`/`SourceBrowserOpenedMessage` → `LoadSourceGenresEffect` → `ShellAppEffectHandler` (Effect-Switch `case LoadSourceGenresEffect`, Zeile ~190 → `LoadSourceGenresAsync` Zeilen 816–833) → `SourceBrowserService.LoadGenresAsync` → `VideoPlayerApiClient.GetSourceGenresAsync` (`GET api/SourceGenres/{id}`) → `SourceGenresLoadedMessage` → `UpdateSourceGenresLoaded` → `SourceBrowser.Genres`.
- **Filterpfad ist vorhanden:** `OnGenreSelected` → `SourceGenreSelectedMessage` → `UpdateSourceGenreSelected` → `ReloadBrowserItems` (Liste zurücksetzen, ab Seite 0 mit `genreId` laden; `genreId = 0` hebt den Filter auf — „Alle").
- **Diagnoseergebnis (codebasiert):** Die Leiste fehlt nicht im Auslieferungsstand (Kandidat 3 entfällt). Der beobachtete Zustand „kein Genre-Filter" entsteht, sobald `Genres` nie über den „Alle"-Eintrag hinaus befüllt wird — d. h. `api/SourceGenres/{id}` liefert für die Quelle eine leere Genre-Liste **oder** der Aufruf schlägt fehl (`ShellAppEffectHandler`-Fallback liefert ebenfalls nur „Alle", Fehler nur im Log). Ob der Endpunkt für Serien-Quellen serverseitig leere Listen liefert, ist ohne laufenden Server nicht beweisbar — zu dokumentieren; der Altbestand (`VideoWebPlayer.Maui/ViewModels/SourceOverviewViewModel.cs:110-156`) nutzt denselben Endpunkt und zeigt dasselbe „Alle"-nur-Verhalten.
- **Lücke:** Es gibt keine Sichtbarkeitsregel — weder `IsVisible`-Bindung noch berechnete Zustandseigenschaft, die eine „Alle"-nur-Leiste ausblendet.
- **Altbestand-Parallele:** Der alte `SourceOverviewViewModel.LoadGenresAsync` stellt „Alle" (`Id = 0`) voran und wählt es aus; kein Sichtbarkeits-Schutz auch dort.

**Test-Ausgangszustand:** `dotnet test VideoWebPlayer.Maui.Tests` — **1312 erfolgreich, 0 fehlgeschlagen, 0 übersprungen** (Commit `9a7387d`, Details: [Tests](inventory/tests.md), [Log](inventory/test-results/dotnet-test-baseline.log)). Abweichung zur Auftragsangabe (Baseline 1281): Der tatsächliche Stand enthält 1312 Tests — als Ausgangszustand gilt der gemessene Wert. E2E-Suite `VideoPlayer.E2E.Tests` wurde für die Baseline nicht ausgeführt (FlaUI, benötigt gebaute App); Umfang/Ausführbarkeit siehe `inventory/tests.md`.

## Details

- [Datenmodell](inventory/models.md) — `SourceBrowserState`, `SourceGenreItem`, API-Modelle `SourceGenres`/`Genre`
- [Logik](inventory/logic.md) — `AppUpdate`-Übergänge, `ShellAppEffectHandler`, `SourceBrowserService`, `SourceOverviewPage`, `VideoPlayerApiClient`, Altbestand-`SourceOverviewViewModel`
- [Interfaces](inventory/interfaces.md) — `IAppEffectHandler`
- [Tests](inventory/tests.md) — Test-Ausgangszustand, bestehende Abdeckung, Lücken (u. a. kein Test für leere Serverliste, kein E2E für Quellen-Browsing)
