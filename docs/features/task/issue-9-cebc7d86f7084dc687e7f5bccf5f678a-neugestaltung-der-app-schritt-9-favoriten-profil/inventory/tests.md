# Tests — Bestandsaufnahme (Schritt 9)

## Test-Ausgangszustand

| Datum/Uhrzeit | Branch | Commit | Umgebung |
|---------------|--------|--------|----------|
| 2026-09-21 (lokal, Windows) | `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-9-favoriten-profil` | `3457221` (`chore: Entwicklungsschritt 9 in Arbeit`) | .NET SDK 10.0.401, xUnit v3 |

Uncommittete Änderungen zum Laufzeitpunkt: nur `.gitignore`
(fremde Änderung, wird nicht committed).

| Befehl | Arbeitsverzeichnis | Ergebnis |
|--------|--------------------|----------|
| `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` | Repository-Wurzel | **431 erfolgreich, 0 fehlgeschlagen, 0 übersprungen** (Dauer ~3 s), Exit-Code `0` |

Nachweis: [test-results/baseline-dotnet-test.log](test-results/baseline-dotnet-test.log)

Hinweis: Ein früherer Lauf in derselben Sitzung meldete identisches
Testergebnis bei Exit-Code `1`; der verifizierte Lauf (oben) liefert
Exit-Code `0` — die Abweichung war ein Shell-Wrapper-Artefakt, kein
Testfehler.

Bekannte Build-Warnungen (preexisting, kein Testfehler):
CS8618 im Altbestand `VideoWebPlayer.Client`, CS1570 in
`VideoPlayer.Storage/KnownServersService.cs`, CS1572 in
`VideoPlayer.Mvu/HomeListService.cs` (param-Tags ohne Parameter).

## Bestehende Teststruktur

Testprojekt `VideoWebPlayer.Maui.Tests` (xUnit v3, `net10.0`,
referenziert `VideoPlayer.Mvu`, `VideoPlayer.ApiClient`,
`VideoPlayer.Storage`, `VideoWebPlayer.Client` — das MAUI-Projekt
`VideoPlayer.Maui` kann nicht referenziert werden und wird über
Contract-Tests auf Quelltextebene geprüft).

Relevante vorhandene Testdateien:

| Datei | Zweck |
|-------|-------|
| `AppUpdateTests_*.cs` | MVU-Übergangstests pro Bereich (Navigation, HomeLists, MediaDetails, NetworkSync, Sources, …) |
| `AppUpdateTests_MediaDetails.cs` | Detail-Ladung, Staffelwahl, Favoriten-Toggle |
| `FavoritesServiceTests.cs` | Toggle → Server + `cached_favorites`-Erneuerung (echte SQLite-Datei, `SqliteDatabase`-Collection) |
| `HomeListServiceTests.cs` | Lesen der Cache-Listen inkl. `cached_favorites` |
| `ServerSyncServiceTests.cs` | Teil-/Voll-Synchronisation |
| `AuthSessionServiceTests.cs` | Anmeldung, Token-Erneuerung, SessionExpired |
| `AppSecretsServiceTests.cs` | Geheimnis-Ablage (In-Memory-Stubs) |
| `EnumCoverageTests.cs` | Jeder Enum-Wert muss in einer Testdatei vorkommen (Git-Hook) |
| `VideoPlayerMauiContractTests.cs` | AppShell-Menüstruktur, Projekt-Verdrahtung (Quelltext-Verträge) |
| `ConnectionWorkflowContractTests.cs`, `MauiSettingsContractTests.cs`, `SourcesContractTests.cs`, `NetworkSyncContractTests.cs`, `HomePageContractTests.cs` | Contract-Tests für Seiten/DI-Verdrahtung |

## Test-Stubs

- `Stubs/StubHttpMessageHandler` — Queue-/Responder-basierter
  HTTP-Stub mit Request-Aufzeichnung (`RecordedRequest`)
- `Stubs/InMemorySettingsStore`, `Stubs/InMemorySecretStore` —
  In-Memory-Ablagen für `AppSecretsService`
- `Stubs/RepositoryRoot` — findet die Repo-Wurzel für
  Quelltext-Contract-Tests
- `Stubs/SqliteDatabaseCollection` — serialisiert Tests mit echten
  SQLite-Dateien
- `RecordingDelays` (in Testdateien) — Warte-Strategie ohne echte
  Verzögerung

## Lücken für diese Anforderung

- Keine Tests für Favoriten-Listen-Seite, Favoriten-Entfernung,
  Profil-Anzeige oder Abmeldung vorhanden.
- Neue Contract-Tests nötig für die ausgebauten Seiten
  (`FavoritesPage`, `ProfilePage`) und die DI-Verdrahtung.
