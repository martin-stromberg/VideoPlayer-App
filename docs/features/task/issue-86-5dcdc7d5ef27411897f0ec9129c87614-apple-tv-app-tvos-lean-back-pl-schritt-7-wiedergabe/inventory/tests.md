# Tests (Wiedergabe) — Ausgangszustand und bestehende Abdeckung

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-11-05T22:48Z (UTC)
- Branch und Commit-ID: `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl-schritt-7-wiedergabe` @ `2e6a9dba4702719d556437291655ce0e13e90e64`
- Uncommittete Änderungen im getesteten Stand: keine Quelländerungen (nur Feature-Artefakte unter `docs/features/` + untracked `.work/`)
- Testumgebung und Runtime-/SDK-Versionen: Windows 11, .NET SDK 10.0.403; `dotnet test VideoPlayer.Maui.Tests`
- Ermittelte Testsuiten und Quellen der Testbefehle: `VideoPlayer.Maui.Tests` (xUnit) — Testbefehl aus `.github/workflows/pr-checks.yml` (Job `verify`); der tvOS-Simulator-Build läuft ausschließlich im macOS-CI-Job `build-tvos` (Push-Trigger `task/issue-86-**`) und ist unter Windows nicht ausführbar.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 (Baseline) | `dotnet build VideoPlayer.App.sln -v q` + `dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj --no-build --logger "console;verbosity=normal"` | Repo-Wurzel | 0 | 1488 | 0 | 0 | [dotnet-test-baseline.txt](test-results/dotnet-test-baseline.txt) |

### Nachgewiesene bestehende Testfehler

Keine — der Ausgangslauf war vollständig erfolgreich (1488/1488).

### Testlücken und Ausführungsprobleme

- `VideoPlayer.E2E.Tests` wurde nicht ausgeführt (Desktop-E2E-Suite der MAUI-App, nicht Teil des verifizierenden CI-Laufs für diesen Schritt; die Anforderung nennt `dotnet test VideoPlayer.Maui.Tests` als zu haltende Suite).
- tvOS-Kompilier-/Lauffähigkeit ist lokal nicht ausführbar — Nachweis ausschließlich über CI `build-tvos`.

## Testklassen (bestehende Player-Abdeckung)

### `AppUpdateTests_Player`
- Player-Messages/Übergänge des MVU-Kerns: `PlayRequestedMessage`-Guards, Quellauflösung und Rückfall, Fortschrittstakt-Throttling, Endfenster-`EndWindowStreak`, `PlaybackEnded`-Plausibilität, Auto-Fortsetzung, `PlayerClosedMessage`-Abschluss.

### `AppUpdateTests_PlaylistPlayback`
- Playlist-Wiedergabestart/-Navigation: `StartPlaylistPlaybackEffect`-Ergebnis, `PlaylistNavigationCompletedMessage`-Identitätsguards, `PlaylistNextExhausted`, `AdvancePlaylistEntry`-Kontext.

### `PlaybackSourceServiceTests`
- Quellauflösung: lokale Datei vor Stream, Fortsetzungsposition (Server/Cache/Override), Offline-Modus (`allowServerStream: false`), nicht abspielbare Typen.

### `ProgressReportServiceTests` / `ServerSyncServiceTests` (Auszug)
- Fortschrittsmeldung online, `pending_progress`-Einreihung offline, permanente Fehler verworfen, Nachsendung inkl. Playlist-Bezug.

### `UpcomingEpisodesServiceTests`
- Fortsetzungsliste online + Cache-Auffrischung, Offline-Cache-Fallback, leere Liste ohne Kontext.

### `DownloadServiceTests_Expiry`
- `ExtendExpiryAsync` (Wiedergabe-Schutz) und `ExpireAfterAsync` mit injizierter Zeitquelle.

### `TvOs*ContractTests` (Muster für neue tvOS-Contract-Tests)
- `TvOsAppContractTests`, `TvOsBrowsingContractTests`, `TvOsConnectionContractTests`, `TvOsDetailsContractTests`, `TvOsHomePageContractTests`, `TvOsCompileContractTests`, `TvOsProjectContractTests` — lesen tvOS-Quelldateien und prüfen Verdrahtungs-Strings (da tvOS unter Windows nicht kompilierbar).

## Hilfsmethoden

### `Stubs` (`VideoPlayer.Maui.Tests/Stubs`)
- `RepositoryRoot.Find()` — Wurzelpfad-Auflösung für Quell-Contract-Tests.
