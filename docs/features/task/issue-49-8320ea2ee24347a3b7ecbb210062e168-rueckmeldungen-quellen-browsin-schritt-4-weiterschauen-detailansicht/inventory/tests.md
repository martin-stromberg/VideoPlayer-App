# Tests – Weiterschauen-Navigation / Detailansicht mit Wiedergabe

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-14, ~00:00 Uhr (lokale Zeit, Windows-Host; `date` liefert keine Zonenangabe — Systemzeit)
- Branch und Commit-ID: `task/issue-49-8320ea2ee24347a3b7ecbb210062e168-rueckmeldungen-quellen-browsin-schritt-4-weiterschauen-detailansicht`, Commit `40c0d14`
- Uncommittete Änderungen im getesteten Stand: nur die Feature-Artefakte unter `docs/features/task/issue-49-…-schritt-4-weiterschauen-detailansicht/` (untracked); kein Produktivcode geändert
- Testumgebung und Runtime-/SDK-Versionen: Windows-Host, .NET SDK 10.0.401, Testprojekt `net10.0` (xUnit v3)
- Ermittelte Testsuiten und Quellen der Testbefehle: Unit-Tests `VideoWebPlayer.Maui.Tests` (Vorgabe aus dem Arbeitsauftrag: `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj`, Erwartung 1326 grün); E2E-Suite `VideoPlayer.E2E.Tests` (FlaUI/Windows-App, `net10.0-windows`, baut die App als Debug-Build `net10.0-windows10.0.19041.0` und startet sie als echten Prozess)

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline Unit | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --logger "trx;LogFileName=baseline-unit-tests.trx" --results-directory docs/features/…/inventory/test-results` | Repo-Root | 0 | 1326 | 0 | 0 | [Log](test-results/baseline-unit-tests.log), [TRX](test-results/baseline-unit-tests.trx) |

### Nachgewiesene bestehende Testfehler

Keine — der Ausgangslauf ist vollständig grün (1326/1326).

### Testlücken und Ausführungsprobleme

- **E2E-Suite `VideoPlayer.E2E.Tests` im Baseline-Lauf nicht ausgeführt.** Sie benötigt einen Debug-Build der MAUI-Windows-App (`dotnet build VideoPlayer.Maui -f net10.0-windows10.0.19041.0 -c Debug`, ~10-Minuten-Budget in `AppFixture`) und startet die App als realen Prozess mit FlaUI-UIA3-Steuerung — abhängig von installiertem MAUI-Workload und interaktiver Fenstersitzung. Ausführbarkeit wird im Testschritt (Schritt 10) geprüft; scheitert die Ausführung, wird dies samt Grund dokumentiert.
- Bestehende Unit- und E2E-Tests des Weiterschauen-Pfads **kodieren das alte Soll-Verhalten** (Direktwiedergabe ohne Detailseite) und werden im Zuge der Umsetzung angepasst — betroffen: `AppUpdateTests_ContinueWatching` (Effekt-Reihenfolge, `DoesNotContain(NavigateToMediaDetailsEffect)`-Assertions), `AppUpdateTests_PlaylistPlayback` (Index-Assertions der Effektliste), E2E `ContinueWatchingPlaybackTests` (Nachweis „keine Detailseite" via `WasPathRequested`-Negativ-Assertions).

## Testklassen

### `AppUpdateTests_ContinueWatching` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_ContinueWatching.cs`)

- `ContinueWatchingItemSelected_Movie_StartsPlayback` — Film-Direktstart: Player-State + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`, **explizit ohne** `NavigateToMediaDetailsEffect` (alte Erwartung, anzupassen)
- `ContinueWatchingItemSelected_Episode_StartsPlaybackWithShowLoad` — Episode mit `ShowId`: plus `LoadUpcomingEpisodesEffect` (Index 2)
- `ContinueWatchingItemSelected_ClrEpisodeType_StartsPlaybackWithShowLoad` — CLR-Typvariante `tvshowepisode`
- `ContinueWatchingItemSelected_EpisodeWithoutShowId_NoShowLoad` — Episode ohne `ShowId`: kein `LoadUpcomingEpisodesEffect`, kein Detail-Effekt (alte Erwartung)
- `ContinueWatchingItemSelected_NonPlayable_NavigatesToDetails` (Theory: `show`/`season`/`collection`) — Detail-Fallback, bleibt gültig
- `ContinueWatchingItemSelected_NonSelectable_Unchanged` (Theory: Id 0, leerer Typ, Platzhalter) — Guards, bleibt gültig
- `UpcomingEpisodesLoaded_MatchingPlayer_SetsQueue` / `…_StaleOrOtherMedia_Discarded` — Übernahme/Verwurf der Fortsetzungsliste, bleibt gültig

### `AppUpdateTests_PlaylistPlayback` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_PlaylistPlayback.cs`)

- `ContinueWatchingSelected_WithPlaylist_StartsPlaylistPlayback` — Playlist-Bezug → `StartPlaylistPlaybackEffect`, kein Player-Wechsel (bleibt gültig)
- `ContinueWatchingSelected_WithoutPlaylist_KeepsSeriesPath` — **Index-Assertions (effects[0..2]) müssen an die neue Reihenfolge angepasst werden**
- `ContinueWatchingSelected_PlaylistWithoutEntry_KeepsSeriesPath` — `PlaylistId` ohne `EntryId` → Serienpfad (Index-Assertion anpassen)

### `ContinueWatchingPlaybackTests` (E2E, `VideoPlayer.E2E.Tests/Tests/ContinueWatchingPlaybackTests.cs`)

- `TapContinueWatchingMovie_StartsPlaybackAtResume_WithoutDetails` — **alte Erwartung** (`Assert.False(WasPathRequested("/api/items/movie/8001"))`) widerspricht dem neuen Soll-Verhalten → anzupassen
- `TapContinueWatchingEpisode_StartsPlaybackAndAdvancesToNextEpisode` — Folgevideo-Funktion über `tvshow`-Abruf; **Negativ-Assertion „keine Detailseite"** anzupassen (Episoden-Detailaufruf ist nun erwartet)
- `TapContinueWatchingShowEntry_OpensDetails_NotPlayback` — nicht abspielbarer Eintrag → Detailansicht, bleibt gültig

### `ContinueWatchingContextMenuTests` (E2E)

- Kontextmenü „Ausblenden"/„Überspringen" — unverändert, bleibt gültig

### `OfflineContinueWatchingServiceTests` / `OfflineRecoveryContractTests`

- Aufbau der Offline-Weiterschauen-Liste (Platzhalter, Kontext-Ids) — unverändert, bleibt gültig

## Hilfsmethoden

### `E2ETestBase` / `AppFixture` (`VideoPlayer.E2E.Tests/`)

- `StartAppAsync(testName, profile)` — baut die App einmalig (Lazy, Debug `net10.0-windows10.0.19041.0`) und startet sie mit isoliertem Profil
- `LoginOnHomeAsync(app, server, …)` — Setup-Fluss bis zur Startseite
- `ContinueWatchingIndexAsync(app, title)` — Index eines Weiterschauen-Eintrags ermitteln
- `MockVideoServer` — lokaler Mock-Server (`PlaybackMovieId`, `PlaybackShowId`, `PlaybackEpisodeId`, `PlaybackNextEpisodeId`, Resume-Positionen, `WasPathRequested`-Nachweis)
- `DevApiClient.InvokeAsync("ContinueWatchingCarousel", index)` — löst `ItemTapped` des Karussells aus (dieselbe Ereigniskette wie Fingertipp)
