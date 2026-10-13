# Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-13 ~19:50 UTC
- Branch und Commit-ID: `task/issue-49-8320ea2ee24347a3b7ecbb210062e168-rueckmeldungen-quellen-browsin-schritt-1-genre-filter` @ `9a7387df4db4e7db6e073f0cb3fc82a2fbda77f0` („chore: Schritt 1 (Genre-Filter) in Arbeit")
- Uncommittete Änderungen im getesteten Stand: nur die Lifecycle-Arbeitsartefakte unter `docs/features/task/issue-49-…-schritt-1-genre-filter/` (Markdown); kein Produktivcode verändert.
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK `10.0.401`, Testprojekt `net10.0`, xUnit v3.
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `VideoWebPlayer.Maui.Tests` (Unit-/Vertragstests über `VideoPlayer.Mvu`, `VideoPlayer.ApiClient`, `VideoPlayer.Storage`, Altbestand-Client) — Befehl aus dem Auftrag: `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj`.
  - `VideoPlayer.E2E.Tests` (FlaUI, Windows; baut die App als Debug-Build über `AppFixture` und startet sie gegen `MockVideoServer`) — benötigt laufende App-Instanz; für die Baseline nicht ausgeführt.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --nologo -v q` | Repo-Root | 0 | 1312 | 0 | 0 | [dotnet-test-baseline.log](test-results/dotnet-test-baseline.log) |

Hinweis: Der Auftrag nannte eine Baseline von 1281 Tests; gemessen wurden 1312 (Stand des Basisbranch hat sich offenbar erweitert). Als Referenz gilt der dokumentierte Messwert.

### Nachgewiesene bestehende Testfehler

Keine — der Baseline-Lauf ist vollständig grün.

### Testlücken und Ausführungsprobleme

- `VideoPlayer.E2E.Tests` nicht ausgeführt (Baseline): FlaUI-Tests starten einen realen Debug-Build der App (`AppFixture.BuildAppAsync`, TFM `net10.0-windows10.0.19041.0`) und fahren FlaUI-Automation — Ausführbarkeit in dieser Umgebung wird im Verlauf geprüft; falls nicht ausführbar, wird dies dokumentiert (Auftragsvorgabe: dokumentieren statt erfinden).
- `MockVideoServer` (E2E-Infrastruktur) deckt das Quellen-Browsing derzeit **nicht** ab: `api/Sources` ist in `IsListEndpoint` enthalten und liefert die leere Standardliste (keine Quelleneinträge im Menü), `api/SourceGenres/{id}` und `api/items?mediaSourceId=…` fallen auf die 404-Standardroute (`MockVideoServer.cs:583-606`, `1823-1827`). Es existiert **kein E2E-Test** für die `SourceOverviewPage` (keine Treffer in `VideoPlayer.E2E.Tests/Tests/`).
- Projekt-Inventar-Notiz (verifiziert): `SourceBrowserServiceTests` enthält keinen Test für eine leere Server-Genreliste oder einen Ladefehler des Diensts selbst.

## Testklassen

### `AppUpdateTests_Sources` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_Sources.cs`)

MVU-Übergänge des Quellen-Browsing. Relevante bestehende Tests:

- `SourceSelected_NavigatesAndStartsInitialLoad` — Quellenwahl erzeugt `NavigateToSourceOverviewEffect` + `LoadSourceGenresEffect` + `LoadSourceItemsEffect` (Seite 0).
- `SourceBrowserOpened_SameSource_KeepsState` / `SourceBrowserOpened_Restore_LoadsGenresAndFirstPage` — Öffnen/Wiederherstellen des Browsers.
- `SourceGenresLoaded_WrongSource_IsDiscarded` — Genres-Antwort verlassener Quelle wird verworfen.
- `GenreSelected_ReloadsFromFirstPage_WithGenreFilter` — Genre-Auswahl setzt `SelectedGenreId`/`IsSelected` und lädt ab Seite 0 mit `GenreId`.
- `SearchSubmitted_*`, `NextPageRequested_*`, `ItemsLoaded_*` — Suche, Nachladen, Fehlerpfade der Eintragsliste.

### `SourceBrowserServiceTests` (`VideoWebPlayer.Maui.Tests/SourceBrowserServiceTests.cs`)

- `LoadGenres_PrependsAllEntry_AndMapsServerGenres` — „Alle" wird vorangestellt, Server-Genres gemappt; Endpunkt `/api/SourceGenres/1` verifiziert (Fake-`StubHttpMessageHandler`).
- `LoadItems_*` — Paging, `HasMore`, Query-Parameter (`search`, `genreId`), `MapSummaryType`-Theory.

### `SourcesContractTests` / `VideoPlayerApiClientTests_Endpoints` (`VideoWebPlayer.Maui.Tests/`)

- Endpunkt-Pins: u. a. `SourceGenres` (`GET /api/SourceGenres/5`), `SourceItems`/`SourceItemsSearchGenre` (`GET /api/items?…&genreId=…`).

## Hilfsmethoden

### `StubHttpMessageHandler` (`VideoWebPlayer.Maui.Tests/Stubs/`)

- `EnqueueJson(status, payload)` — legt HTTP-Antworten in eine Warteschlange; `Requests` protokolliert die aufgerufenen URIs (Basis der Service-Tests).

### `AuthSessionService`-Testsetup (`SourceBrowserServiceTests`)

- `LoginAsync()` — fädelt Login-Antwort ein (`token`, `expires`), erzeugt den angemeldeten `Client` für `SourceBrowserService`.
