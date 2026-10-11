# Testinfrastruktur und Ausgangszustand

## Testprojekte

| Projekt | Art | Runner |
|---------|-----|--------|
| `VideoWebPlayer.Maui.Tests` | Unit-/Vertragstests (xUnit); testet `VideoPlayer.Mvu`, `VideoPlayer.ApiClient`, `VideoPlayer.Storage` sowie Quelltext-Verträge des Legacy-Altbestands | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release` |
| `VideoPlayer.E2E.Tests` | FlaUI/DevApi-E2E (xUnit, `[Collection("E2E")]` sequentiell): `AppFixture` baut `VideoPlayer.Maui` einmalig als **Debug** (`net10.0-windows10.0.19041.0`), startet die App mit isoliertem Profil; `MockVideoServer` (TcpListener-HTTP-Mock) + DevApi (`/model`, `/tree`, `/invoke`, `/set-text`) | `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj -c Debug` (nur Windows) |

CI (`pr-checks.yml`): baut Client, Tests, beide MAUI-Projekte (Windows-Release) und führt die Unit-Tests + `dotnet format whitespace --verify-no-changes` aus. E2E läuft nicht in CI.

## Relevante Testdateien (Muster)

- `VideoWebPlayer.Maui.Tests/AppUpdateTests_Playlists.cs` — Update-Tests der Playlist-Anzeige.
- `VideoWebPlayer.Maui.Tests/PlayerContractTests.cs` — Player-Verhalten (Update-Ebene).
- `VideoWebPlayer.Maui.Tests/EnumCoverageTests.cs` — Enum-Abdeckung; neue Enums brauchen Einträge.
- `VideoPlayer.E2E.Tests/Tests/ContinueWatchingPlaybackTests.cs` — Direktstart aus Weiterschauen inkl. Auto-Fortsetzung (Muster für Playlist-Szenarien).
- `VideoPlayer.E2E.Tests/Tests/PlaylistsTests.cs` — Playlist-Anzeige (Muster für Detailseiten-Flüsse).
- `DevApiClient.InvokeAsync(automationId, itemIndex, ct)` — Button-Klick (`TryInvoke`) oder Elementauswahl (`itemIndex` → `SelectionChanged`; `gesture: "longpress"` für Kontextmenü).
- `DevApiModelSnapshot` (`VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs`) — Player-Felder: `PlayerIsActive`, `PlayerMediaId`, `PlayerUpcomingEpisodeCount`, `PlayerIsPlaying`, `PlayerPositionSeconds`, `PlayerError`; Playlist-Felder: `PlaylistsPageItemCount`, `PlaylistDetailsId`, `PlaylistDetailsEntryCount`, `PlaylistDetailsError`. Für E2E-Nachweise des Playlist-Kontexts ggf. `PlayerPlaylistId`/`PlayerPlaylistEntryId` ergänzen.

## Ausgangszustand (Baseline)

Zeitpunkt: 2026-10-11 17:19 +02:00 · Branch: `task/…-schritt-5-playlist-wiedergabe` · Commit `6e16062` · uncommittete Änderungen: keine (Feature-Verzeichnis untracked) · .NET SDK 10.0.401 · Windows 11 (x64).

| Lauf | Befehl | Arbeitsverzeichnis | Exit-Code | Ergebnis |
|------|--------|--------------------|-----------|----------|
| Unit | `dotnet build VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release` | Repo-Root | 0 | 0 Fehler, 10 Warnungen (Bestand) |
| Unit | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-build -c Release` | Repo-Root | 0 | 1139 bestanden, 0 fehlgeschlagen, 0 übersprungen (14,0 s) |
| E2E | `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj -c Debug` | Repo-Root | _läuft_ | Ergebnis wird nachgetragen |

Maschinenlesbare Nachweise: `inventory/test-results/`.

## Fehlende Infrastruktur für die Anforderung

- `MockVideoServer`: `POST api/playlists/{id}/play` (+`next`/`previous`/`advance`) fehlt (nur GET bedient); Weiterschauen-Einträge ohne Playlist-Felder; Stream-Endpunkte nur für Opt-in-Medien-Ids.
- Keine Tests für Playlist-Wiedergabe, Playlist-Navigation oder playlist-gebundene Weiterschauen-Einträge.
