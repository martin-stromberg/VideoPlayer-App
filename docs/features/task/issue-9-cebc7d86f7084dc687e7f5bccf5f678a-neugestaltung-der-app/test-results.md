# Testergebnisse: Dynamische Quellen-Menüeinträge

**Status:** Keine Fehler

Branch: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app`
(uncommitted Änderungen sind Teil des Features)

## Ergebnis je Schritt

| Schritt | Befehl | Ergebnis |
|---------|--------|----------|
| 1. Solution-Build (Debug) | `dotnet build VideoPlayer.App.sln` | Erfolgreich — 0 Fehler, 0 Warnungen |
| 2. Volle Testsuite | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` | Bestanden — 644 erfolgreich, 0 fehlgeschlagen, 0 übersprungen (Dauer ~11 s) |
| 3. Plan-Testnachweis | grep gegen `VideoWebPlayer.Maui.Tests` | Alle geplanten Tests vorhanden und bestanden (Details unten) |
| 4. Format-Check | `dotnet format whitespace VideoPlayer.App.sln --verify-no-changes` | Erfolgreich — keine Verletzungen (Exit 0) |
| 5. Windows-Build | `dotnet build VideoPlayer.Maui -f net10.0-windows10.0.19041.0` | Erfolgreich — 0 Fehler, 0 Warnungen |

## Fehlgeschlagene Tests

Keine.

## Plan-Testnachweis (Schritt 3)

### Neue Tests aus `plan.md` — alle vorhanden und im Lauf enthalten

| Test | Klasse / Datei |
|------|----------------|
| `EnterConnected_StartsSourcesLoad` | `AppUpdateTests_Sources.cs` (Z. 28) |
| `SynchronizationCompleted_WhenConnected_ReloadsSources` | `AppUpdateTests_Sources.cs` (Z. 56) |
| `SynchronizationCompleted_WhileSourcesLoading_NoDuplicate` | `AppUpdateTests_Sources.cs` (Z. 69) |
| `CachedListsChanged_WhenConnected_ReloadsSources` | `AppUpdateTests_Sources.cs` (Z. 85) |
| `CachedListsChanged_WhileSourcesLoading_NoDuplicate` | `AppUpdateTests_Sources.cs` (Z. 96) |
| `CachedListsChanged_WhenNotConnected_NoSourcesLoad` | `AppUpdateTests_Sources.cs` (Z. 110) |
| `SourcesLoaded_WhenNotConnected_IsIgnored` | `AppUpdateTests_Sources.cs` (Z. 128, Theory über Offline/NeedsLogin) |
| `NetworkLost_FromConnected_ResetsSourcesOverview` | `AppUpdateTests_Offline.cs` (Z. 64) |
| `SessionExpired_ResetsSourcesOverviewAndBrowser` | `AppUpdateTests_Connection.cs` (Z. 295) |
| `ServerCheckCompleted_Success_ResetsSourcesOverviewAndBrowser` | `AppUpdateTests_Connection.cs` (Z. 445) |
| `AppShell_BuildsSourceMenuItems_FromModel` | `SourcesContractTests.cs` (Z. 22) |
| `AppShell_HasNoStaticSourcesFlyoutItem` | `SourcesContractTests.cs` (Z. 111) |
| `MauiProgram_RegistersPosterImageServiceForShell` | `SourcesContractTests.cs` (Z. 125) |

### Angepasste/entfernte Bestandstests — wie geplant umgesetzt

- Vorhanden und bestanden: `AppShell_ContainsAllMenuEntries`,
  `AutoLoginCompleted_Succeeded_ConnectsAndNavigatesHome`,
  `LoginCompleted_Succeeded_ConnectsAndNavigatesHome`,
  `DispatchAsync_UpdatesModelAndExecutesEffects`, `CachedListsChanged_ReloadsHomeLists`,
  `ServerEventReceived_FullSequence_RefreshesLists`, `SynchronizationCompleted_ClearsSyncFlag`.
- `MenuPages_ContainsAllSixMenuEntriesInOrder` wurde planmäßig umbenannt zu
  `MenuPages_ContainsAllFiveMenuEntriesInOrder` (`AppRoutesTests.cs` Z. 10).
- Entfernte Tests (`SourcesPageOpened_*` in `AppUpdateTests_Sources`/`_Offline`,
  `SourcesPage_ShowsSources_WithIcons_AndOfflineHint`, `SourcesPage_DispatchesOpenAndSelection`)
  sind im Testprojekt nicht mehr vorhanden — Verifizierung per grep ohne Treffer.

## Hinweise

- **E2E/UI-Laufzeittests existieren in diesem Repository nicht.** Die etablierte
  Konvention sind MVU-Unit-Tests plus Quelltext-Contract-Tests (dokumentiert in
  `inventory/tests.md` und dem `SourcesContractTests`-Klassenkommentar). Das ist
  keine fehlgeschlagene Abdeckung, sondern eine dokumentierte Einschränkung.
- **Manueller Sichtcheck des Flyouts steht noch aus:** Die Darstellung der zur
  Laufzeit eingefügten `MenuShellItem`s (Position zwischen den `FlyoutItem`s,
  deaktivierte „Quellen"-Überschrift, Icon-Auflösung) ist per Contract-Test nur
  quelltextseitig gepinnt und muss manuell in der laufenden App verifiziert werden
  (in `plan.md` unter „Seiteneffekte und Risiken" als Restrisiko geführt).
- `dotnet format` wurde mit der Solution als Argument ausgeführt (CI-Äquivalent);
  Exit 0 ohne Meldungen.
