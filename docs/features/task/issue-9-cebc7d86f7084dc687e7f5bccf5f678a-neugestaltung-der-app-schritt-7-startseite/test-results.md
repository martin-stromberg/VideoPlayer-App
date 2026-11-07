# Testergebnisse

**Status:** Keine Fehler

## Ausgeführte Verifikationen

| Prüfung | Ergebnis |
|---------|----------|
| Build aller geänderten Projekte (`VideoPlayer.Mvu`, `VideoPlayer.Maui` Windows, `VideoWebPlayer.Maui.Tests`) | erfolgreich, 0 Warnungen, 0 Fehler |
| `dotnet test VideoWebPlayer.Maui.Tests` (Release, mit Coverage-Sammlung) | 350 Tests: 350 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| Code-Coverage (`--collect:"XPlat Code Coverage"`) | `VideoPlayer.Storage` 99,0 %, `VideoPlayer.Mvu` 93,5 %, `VideoPlayer.ApiClient` 86,4 % Zeilen (`VideoWebPlayer.Client` 7,3 %, Altbestand) |
| Windows-Build `VideoPlayer.Maui` (`net10.0-windows10.0.19041.0`, XAML-SourceGen) | erfolgreich, 0 Warnungen, 0 Fehler |
| iOS/MacCatalyst-/Android-Build | lokal auf Windows nicht ausgeführt — plattformneutraler Code (`VideoPlayer.Mvu`) ist vollständig net10.0-getestet; MAUI-spezifische Verdrahtung über Contract-Tests (`HomePageContractTests`) |
| Windows-Smoke-Run / UI-E2E | nicht verfügbar — das Repository enthält kein UI-Automatisierungs-Framework und das Testprojekt kann `VideoPlayer.Maui` nicht referenzieren (siehe `inventory/tests.md`); Ersatz gemäß Plan: vollständige Message-Sequenz-Tests auf MVU-Ebene + Contract-Tests der Oberflächen-Verdrahtung |

Befehl (Arbeitsverzeichnis: Repository-Wurzel, Exit-Code 0):

```
dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"
```

Protokoll: `final-dotnet-test.log` (Coverage-Anlage:
`coverage.cobertura.xml` unter `VideoWebPlayer.Maui.Tests/TestResults/`).

## Baseline vs. Ergebnis

- Baseline (vor Implementierung, `inventory/test-results/baseline-dotnet-test.log`):
  324 Tests, 324 bestanden.
- Nach Implementierung: 350 Tests, 350 bestanden (26 neue Tests).

## Neue Tests für die Akzeptanzkriterien

| Akzeptanzkriterium | Nachweis |
|--------------------|----------|
| Drei Listen mit den korrekten Server-Inhalten | `HomeListServiceTests` (Mapping je Liste über den realen SQLite-Cache), `SynchronizationCompleted_*` lädt neu |
| Gecachte Inhalte bei erneutem Öffnen | `StartupRequested_LoadsHomeListsAfterStorageInit`, `LoadHomeLists_PersistsAcrossReads`, `LoadHomeLists_EmptyCache_ReturnsEmptyLists` |
| Server-Ereignisse ohne manuelle Aktualisierung | `ServerEventReceived_MapsToPartialSync` (alle drei Ereignisse), `ServerEventReceived_FullSequence_RefreshesLists`, `CachedListsChanged_ReloadsHomeLists` |
| Auswahl führt zum Medium/Detailseite | `MediaItemSelected_NavigatesToDetails`, `MediaItemSelected_Without*`, `MediaDetails_RouteRegistered_AndPageReadsQueryParameters` |
| Oberflächen-Verdrahtung | `HomePageContractTests` (Titel, horizontale Liste, Poster-Cache, Tap-Geste, DI) |
| Vorschaubilder außerhalb der Backup-Ablage | `PosterImageService_UsesNonBackupPreviewDirectory` |
