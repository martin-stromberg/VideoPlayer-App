# Abschlussprüfungen – Saisonabhängige Genre-Listen (Iteration 2)

**Status: Fehler vorhanden**

## Mobile UnitSuite und Windows-Build

Die Mobile-Produktdateien blieben seit dem vorherigen erfolgreichen Lauf unverändert. Daher wurde die UnitSuite nicht erneut ausgeführt: `dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj` hatte **1396 bestanden, 0 Fehler, 0 übersprungen** (50 s) ergeben. Der Windows-MAUI-Build war ebenfalls im vorherigen Schritt erfolgreich (0 Fehler; bestehende NU1903- und CS0618-Warnungen). Die E2E-Tests liefen anschließend erneut unter Windows.

## Vollständige Windows-E2E

`dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj`: **70 bestanden, 2 fehlgeschlagen, 0 übersprungen, 72 gesamt** (9 m 15 s). Alle sechs saisonalen Flüsse bestanden; ebenso die zuvor auffälligen Login-/Playlist-/Source-Paging-Flüsse.

Die beiden Fehler traten in `SourceSearchTests` auf:

- `SubmittedText_RemainsVisibleAfterReturn`: Timeout beim Warten auf „Suche ‚Komöd‘ abgeschickt“. Letzter Dev-API-Zustand: Seite `Home`, `sourceBrowserSearchText` leer; Genres und Quelleneinträge waren vorhanden.
- `SubmitViaEnter_FiltersFromFirstPage`: Timeout beim Warten auf „Suche ‚Komöd‘ abgeschickt → gefilterte Trefferliste ab Seite 0“. Letzter Dev-API-Zustand ebenfalls `Home`, `sourceBrowserSearchText` leer; ursprüngliche Einträge blieben sichtbar.

Beide Tests wurden gezielt wiederholt: `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj --filter "FullyQualifiedName~SubmittedText_RemainsVisibleAfterReturn|FullyQualifiedName~SubmitViaEnter_FiltersFromFirstPage"`. Die Wiederholung bestätigte beide Timeouts (2/2 fehlgeschlagen, 0 übersprungen). Das Verhalten tritt nach erfolgreichem Login auf und unterscheidet sich vom zuvor korrigierten Login-Setup. Es wurde kein Produkt- oder Testcode im Rahmen dieses Prüfschritts verändert.

**Offen:** Die zwei SourceSearch-E2E-Fehler verhindern einen vollständig grünen Windows-E2E-Abschluss. Ihre Ursache ist hier nicht abschließend isoliert; der letzte beobachtete Zustand zeigt, dass der Suchtext nicht im Modell ankam. Alle übrigen 70 UI-E2E einschließlich der sechs saisonalen Abläufe bestanden.

## Servernachweise

Die Server-Suites wurden nicht erneut ausgeführt. Der [Server-Testbericht](../../../integration/seasonal-genres/server-test-results.md) und die [unabhängige Abnahme](../../../integration/seasonal-genres/acceptance-schritt-1.md) dokumentieren Debug-/Release-Build, die vollständige Server-Suite (2358 bestanden, 2 bestehende Skips) und die unabhängige Saisonabnahme einschließlich 12/12 gezielter Tests und Mutationstests.
