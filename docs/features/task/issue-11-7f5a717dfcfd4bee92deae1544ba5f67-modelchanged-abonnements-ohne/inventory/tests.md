# Bestandsaufnahme Tests

## Testprojekte

- `VideoWebPlayer.Maui.Tests` — xUnit; Unit-Tests der MVU-/Service-Ebene plus **Contract-Tests**, die die MAUI-Schicht quelltextseitig pinnen (die `VideoPlayer.Maui`-Assembly ist im Testprojekt nicht instanziierbar — dokumentierte Konvention).
- `VideoPlayer.E2E.Tests` — xUnit, sequentielle `E2E`-Collection; startet die Windows-App als Debug-Build (`AppFixture`), steuert sie über `UiDriver` und prüft den Modellzustand über die Developer-API (`DevApi.WaitForModelAsync`). Enthält keine Lebenszyklus-/Leck-Tests.

## Testklassen

### `PlayerContractTests` (`VideoWebPlayer.Maui.Tests/PlayerContractTests.cs`)

Referenz für quelltextbasierte Lebenszyklus-Pins:

- `PlayerPage_UnsubscribesModelChangedOnDisappearing` — pinnt `ModelChanged -= OnModelChanged` im Rumpf von `OnDisappearing`, `ModelChanged +=` in `OnAppearing` und das Fehlen von `ModelChanged +=` im Konstruktor.
- `PlayerPage_IgnoresModelChangesAfterDisappearing` — pinnt `subscribed = false` in `OnDisappearing` und den `subscribed`-Guard in `OnModelChanged` sowie allen Media-Event-Handlern.
- `PlayerPage_MarshalsNativeElementAccessOnMainThread` — pinnt `BeginInvokeOnMainThread` um native Zugriffe.

### Weitere Contract-Testklassen der betroffenen Seiten

- `HomePageContractTests` — XAML-Struktur und Code-Behind-Pins der `HomePage` (u. a. `HomePage_CodeBehind_UpdatesHeroImageAndResponsiveHeight`).
- `MediaDetailsPageContractTests` — Header-/Episoden-Pins der `MediaDetailsPage`.
- `DownloadsContractTests` — Downloads-Liste.
- `FavoritesProfileContractTests` — Favoriten- und Profilseite.
- `SourcesContractTests` — Quellenübersicht/AppShell (enthält u. a. `Assert.Contains("ModelChanged", ...)` für `AppShell.xaml.cs`).
- `ServerSetupContractTests`, `ConnectionWorkflowContractTests` — Einrichtungs-/Anmeldefluss (unveränderte Referenzseiten).
- `VideoPlayerMauiContractTests` — übergreifende MAUI-Pins.
- `MvuHostTests` — Unit-Tests des `MvuHost`.

## Hilfsmethoden

### `PlayerContractTests`

- `Read(params string[] parts)` — liest eine Repo-Datei relativ zu `RepositoryRoot.Find()`.
- `MethodBody(code, signature)` — extrahiert einen Methodenrumpf anhand der Signaturmarke (Klammerzählung); Basis der Lebenszyklus-Pins.

### `VideoWebPlayer.Maui.Tests.Stubs`

- `RepositoryRoot.Find()` — lokalisiert den Repo-Stamm.

## Konventionen

- Testnamen: `{Klasse}_{Szenario}` bzw. `MethodName_Szenario_ErwartetesErgebnis`; xUnit `[Fact]`; Assertionen über `Assert.Contains`/`Assert.DoesNotContain` mit `StringComparison.Ordinal` in Contract-Tests; FluentAssertions/Moq in Unit-Tests.
