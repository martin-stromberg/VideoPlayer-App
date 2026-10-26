# Abschlussprüfungen – Saisonabhängige Genre-Listen

**Status: Fehler vorhanden**

## Mobile Tests

- `dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj`: **1396 bestanden, 0 Fehler, 0 übersprungen** (50 s).
- `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj --filter FullyQualifiedName~SeasonalGenresTests`: **6 bestanden, 0 Fehler, 0 übersprungen** (57 s). Damit sind alle sechs saisonalen Windows-E2E-Flüsse erfolgreich nachgewiesen.

## Windows-E2E-Gesamtsuite

Der volle Befehl `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj` wurde begonnen und nach drei Fehlern mit demselben Setup-Symptom abgebrochen, um die übrigen Tests nicht jeweils in denselben langen Login-Timeout laufen zu lassen. Die betroffenen bestehenden Fälle waren:

- `SourcePagingTests.NextPageLoad_KeepsScrollPosition`
- `PlaylistManagementTests.RemoveEntryWithConflict_CancelKeepsEntry`
- `PlaylistManagementTests.MoveEntries_ReordersOnServer`

Alle drei scheiterten in `E2ETestBase.LoginAsync` mit Timeout „Anmeldung erfolgreich → Startseite“. Der letzte Appzustand war `NeedsLogin` mit `loginError: „Bitte E-Mail-Adresse und Passwort eingeben.“`; der jeweilige Mock meldete `Login-Requests: []`. Der erste Fall wurde anschließend einzeln wiederholt und scheiterte nach 56 s mit demselben Zustand und ohne Login-Request. Das belegt einen reproduzierbaren Fehler im Login-Setup dieser bestehenden E2E-Flüsse. Die Ursache liegt vor der jeweiligen Produktfunktion; es wurde kein Produktcode verändert.

**Offen:** Die vollständige Windows-E2E-Suite wurde nicht zu Ende ausgeführt. Die drei genannten bestehenden E2E-Fehler sind offen; die übrigen nicht-saisonalen UI-Flüsse haben in diesem Lauf keinen Abschlussnachweis. Saisonale UI-Flüsse sind separat alle erfolgreich.

## Windows-Build

`dotnet build VideoPlayer.Maui/VideoPlayer.Maui.csproj -f net10.0-windows10.0.19041.0 -p:IncludeAndroidTarget=false -p:IncludeIosTarget=false -p:IncludeMacCatalystTarget=false`: **erfolgreich, 0 Fehler**. MSBuild benötigte Zugriff auf die lokalen Windows-SDK-Metadaten unter `%LOCALAPPDATA%`; der erste Sandboxlauf wurde deshalb blockiert. Der identische gezielte Build wurde danach mit freigegebenem SDK-Zugriff erfolgreich abgeschlossen. Vorhandene Warnungen: NU1903 zu `Microsoft.AspNetCore.SignalR.Common` 10.0.10 sowie CS0618 für `Page.DisplayAlert`.

## Servernachweise

Die unveränderten Backend-Abschlussläufe sind im [Server-Testbericht](../../../integration/seasonal-genres/server-test-results.md) dokumentiert: Debug-/Release-Build, vollständige Suite mit 2358 bestandenen Tests und gezielte Saison-/Web-E2E. Die unabhängige Abnahme einschließlich Mutationstests und vier realen Web-E2E-Flüssen steht in der [lokalen Abnahmekopie](../../../integration/seasonal-genres/acceptance-schritt-1.md). Diese Server-Suites wurden entsprechend dem Auftrag nicht erneut ausgeführt.
