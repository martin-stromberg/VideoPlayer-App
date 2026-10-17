# Bestandsaufnahme: App-Grundgerüst mit MVU-Struktur und Navigation

Analysiert wurden die Solution-Struktur, der bestehende API-Client (`VideoPlayer.ApiClient`),
die Alt-App `VideoWebPlayer.Maui` (nur als fachliche Referenz), die Build-/CI-Konfiguration
sowie die Test-Infrastruktur — bezogen auf die Anforderung „App-Grundgerüst mit MVU-Struktur
und Navigation" (Entwicklungsschritt 2).

## Zusammenfassung

- **API-Client vorhanden:** `VideoPlayer.ApiClient` (net10.0) stellt `VideoPlayerApiClient`
  mit `CheckHealthAsync`, `LoginAsync`, DTOs, Client-Gate (`ClientGate`, Header `X-API-Key`)
  und Retry-/Token-Refresh-Pipeline bereit. Der Client verlangt eine `HttpClient.BaseAddress`.
- **Alt-App ist MVVM:** `VideoWebPlayer.Maui` nutzt Shell mit `FlyoutBehavior="Flyout"`,
  `ViewModels/` mit `INotifyPropertyChanged` und DI über `MauiProgram`. Kein MVU-Framework
  referenziert. Der Code darf nicht als Laufzeitbasis übernommen werden (nur Referenz).
- **Kein MVU-Grundgerüst vorhanden:** Es existiert weder ein eigener MVU-Zyklus noch eine
  neue App-Projektstruktur. Seiten für „Quellen"-Übersicht (als Menüpunkt), „Favoriten",
  „Downloads" und „Profil" fehlen auch in der Alt-App.
- **Solution `VideoPlayer.App.sln`:** enthält `VideoWebPlayer.Client` (net10.0, Alt-Client),
  `VideoWebPlayer.Maui` (MAUI, Alt-App), `VideoWebPlayer.Maui.Tests` (net10.0, xUnit v3) und
  `VideoPlayer.ApiClient` (net10.0, neu).
- **CI:** `pr-checks.yml` baut Client, Tests und die Alt-App für Android + Windows;
  `staging-merge.yml`/`main-merge.yml` bauen die Alt-App zusätzlich für iOS auf
  `macos-latest` (`net10.0-ios`, `iossimulator-arm64`). `Set-Version.ps1` schreibt
  Versionsnummern fest in `VideoWebPlayer.Maui.csproj`. Für den iOS-Nachweis der neuen App
  müssen die Workflows auf das neue Projekt zeigen.
- **Test-Ausgangszustand:** `dotnet test VideoWebPlayer.Maui.Tests` → **69 Tests, alle
  bestanden** (Details: [Tests](inventory/tests.md)). Das Testprojekt referenziert nur
  `VideoWebPlayer.Client` und `VideoPlayer.ApiClient` — MAUI-Code ist nur über
  Contract-Tests (Quelltext-/XAML-Lesen) prüfbar.
- **Lokale IIS-Instanz:** `W3SVC` läuft; die VideoPlayer-Site antwortet unter
  `http://localhost/videoplayer`, liefert aber derzeit **HTTP 500** für alle Pfade
  (serverseitiges Problem, kein App-Problem). Ein Live-Health-Check kann daher nur den
  Aufruf selbst (Erreichen des Servers) belegen, nicht `200 OK`.

## Details

- [API-Client (VideoPlayer.ApiClient)](inventory/api-client.md)
- [UI / Navigation (Alt-App als Referenz)](inventory/app-shell-navigation.md)
- [Build-, Projekt- und CI-Konfiguration sowie Umgebung](inventory/build-ci.md)
- [Tests und Test-Ausgangszustand](inventory/tests.md)

## Referenzen

- Projektweite Bestandsaufnahme (Ausgangsbasis):
  `docs/projects/task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app/inventory/`
  (insbes. `ui-navigation.md`, `build-environment.md`, `client-api.md`)
- API-Client-Dokumentation: `docs/help/api-client/`
