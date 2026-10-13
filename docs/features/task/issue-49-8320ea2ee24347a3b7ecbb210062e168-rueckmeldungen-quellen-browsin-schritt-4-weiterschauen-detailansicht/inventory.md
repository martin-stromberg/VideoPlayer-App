# Bestandsaufnahme: Weiterschauen-Eintrag öffnet Titel-Detailansicht mit Wiedergabe

Analysiert wurde der Auswahl- und Navigationspfad der Weiterschauen-Liste der Startseite (`ContinueWatchingItemSelectedMessage` → `UpdateContinueWatchingItemSelected`) einschließlich Detailseiten-Navigation, Wiedergabestart, Serienkontext-Nachladung und Offline-Pfad — bezogen auf die Anforderung in `requirement.md`.

## Zusammenfassung

- `UpdateContinueWatchingItemSelected` (`VideoPlayer.Mvu/AppUpdate.cs:1357-1401`) startet für `movie`/`tvshowepisode` (normalisiert via `MediaTypeNames.Normalize`) direkt über `StartPlayback` (`AppUpdate.cs:3596-3612`) mit `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect` — ohne Detail-Navigation. Episoden mit `ShowId` erhalten zusätzlich `LoadUpcomingEpisodesEffect` (Fortsetzungsliste der Folge-Episoden).
- Nicht direkt abspielbare Typen navigieren bereits über `NavigateToMediaDetailsEffect` mit Kontext-Hints (`ShowId`, `SeasonId`, `EpisodeId`); Playlist-gebundene Einträge (`PlaylistId` + `PlaylistEntryId` > 0) laufen über `StartPlaylistPlaybackEffect`; Guards über `IsSelectableEntry` (`AppUpdate.cs:1456-1457`).
- Die Detailseite existiert vollständig: Shell-Route `mediadetails` → `MediaDetailsPage`, Datenbezug eigenständig über `MediaDetailsRequestedMessage` (OnAppearing) → `LoadMediaDetailsEffect` → `MediaDetailsService.LoadAsync` — online Serverabruf, offline `LoadOfflineAsync`-Snapshot aus Download-Manifest/`ShowDetailsCache`/Favoriten-Kopie.
- „Player schließen" ist als Shell-Pop realisiert (`PlayerPage.OnDisappearing` → `PlayerClosedMessage` → Player-State leeren; die Seite kehrt zur darunterliegenden Route zurück). Liegt `mediadetails` unter `player`, landet der Rücksprung automatisch auf der Detailansicht.
- Effekte werden in `MvuHost.DispatchAsync` (`MvuHost.cs:64-67`) streng sequenziell in Listenreihenfolge ausgeführt — `NavigateToMediaDetailsEffect` vor `NavigateToPlayerEffect` ergibt den gewünschten Stapel.
- Es fehlt: die Detail-Navigation im abspielbaren Zweig der Weiterschauen-Auswahl sowie die Offline-Fallunterscheidung „nutzbarer Kontext" aus der Anforderung.
- Test-Ausgangszustand: `dotnet test VideoWebPlayer.Maui.Tests` — **1326 erfolgreich, 0 fehlgeschlagen, 0 übersprungen** (Nachweis: [tests.md](inventory/tests.md), TRX/Log unter `inventory/test-results/`). E2E-Suite (`VideoPlayer.E2E.Tests`, FlaUI + Windows-Debug-Build der App) im Ausgangslauf nicht ausgeführt — Ausführbarkeit wird beim Testschritt geprüft.

## Details

- [Datenmodell](inventory/models.md) — `HomeMediaItem`, `PlayerState`, `MediaDetailsState`/`MediaDetailsView` (soweit relevant)
- [Logik](inventory/logic.md) — `AppUpdate`-Handler, `MvuHost`, `ShellAppEffectHandler`, `MediaDetailsService`, `OfflineContinueWatchingService`, `MediaDetailsPage`/`PlayerPage`/`HomePage`
- [Enums](inventory/enums.md) — `ConnectionState`, `AppPage`, `MediaTypeNames` (Normalisierung)
- [Tests](inventory/tests.md) — Test-Ausgangszustand und bestehende Testklassen des Bereichs
