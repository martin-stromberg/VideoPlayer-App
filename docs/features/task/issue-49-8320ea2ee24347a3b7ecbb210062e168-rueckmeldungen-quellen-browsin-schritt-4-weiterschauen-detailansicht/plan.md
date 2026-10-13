# Umsetzungsplan: Weiterschauen-Eintrag öffnet Titel-Detailansicht mit Wiedergabe

## Übersicht

Ein Tipp auf einen direkt abspielbaren Weiterschauen-Eintrag (`movie`/`tvshowepisode`, ohne Playlist-Bezug) schiebt künftig die Titel-Detailansicht (`mediadetails`) auf den Shell-Stack und legt die Wiedergabe (`player`) darüber — „Player schließen" kehrt zur Detailansicht zurück. Umgesetzt wird dies ausschließlich in `UpdateContinueWatchingItemSelected` durch Voranstellen des bestehenden `NavigateToMediaDetailsEffect` vor den bisherigen `StartPlayback`-Übergang; Guards, Playlist-Pfad, Nicht-abspielbar-Fallback, Kontextmenü und Serienkontext-Nachladung bleiben unverändert. Offline gilt eine Fallunterscheidung: Episoden ohne `ShowId` (kein nutzbarer Detail-Kontext) behalten die Direktwiedergabe.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Navigation Detail+Player | Zwei aufeinanderfolgende Effekte (`NavigateToMediaDetailsEffect`, dann `NavigateToPlayerEffect` aus `StartPlayback`) statt eines neuen kombinierten Effekts | `MvuHost.DispatchAsync` führt Effekte sequenziell in Listenreihenfolge aus; beide Shell-Routen pushen auf denselben Stack — kein neuer Effekt, kein UI-Code nötig |
| Offline-Fall „kein nutzbarer Kontext" | Guard auf Model-Ebene: `model.IsOffline` + `tvshowepisode` ohne `ShowId` → bisherige Direktwiedergabe | Die Update-Funktion ist pur und sieht nur den `HomeMediaItem`; `ShowId` ist die Kennung, mit der `MediaDetailsService.LoadOfflineAsync` den Serienkontext (`ShowDetailsCache`) aufbaut. Filme sind ihr eigener Kontext (Manifest-Eintrag liegt als Download gesichert vor) und öffnen auch offline die Detailansicht |
| Serienkontext/Fortsetzungsliste | `LoadUpcomingEpisodesEffect` bleibt als letzter Effekt erhalten | Zum Startzeitpunkt liegen die Detaildaten noch nicht im Model — `NextEpisodeQueue` wäre leer; `UpcomingEpisodesService` arbeitet offline über `ShowDetailsCache` weiter |
| `SeasonId`/`EpisodeId`-Hints | Beim Detail-Effekt aus dem Eintrag durchreichen (wie im Nicht-abspielbar-Fallback) | Die Detailseite nutzt die Hints zur Staffel-/Episoden-Vorwahl; Offline-Episoden der Weiterschauen-Liste tragen `EpisodeId` (= VideoId) und ggf. `ShowId` |

## Programmabläufe

### Weiterschauen-Tap auf abspielbaren Eintrag (online oder Offline-Film/Episode mit Serien-Kennung)

1. `HomePage.OnContinueWatchingItemTapped` dispatcht `ContinueWatchingItemSelectedMessage`.
2. `UpdateContinueWatchingItemSelected`: `IsSelectableEntry`-Guard; Typ normalisieren; kein Playlist-Bezug.
3. Übergang: Player-State via `StartPlayback` belegen; Effektliste `[NavigateToMediaDetailsEffect(mediaType, id, title, showId, seasonId, episodeId), NavigateToPlayerEffect, ResolvePlaybackSourceEffect]` (+ `LoadUpcomingEpisodesEffect` bei `tvshowepisode` mit `ShowId`).
4. `ShellAppEffectHandler` pusht `mediadetails` → `MediaDetailsPage.OnAppearing` dispatcht `MediaDetailsRequestedMessage` (Detail-Ladung läuft im Hintergrund weiter) → danach `player` → `PlayerPage` liegt oben.
5. „Player schließen": Shell-Pop → `PlayerPage.OnDisappearing` → `PlayerClosedMessage` → zurück auf der Detailansicht mit (inzwischen) geladenen Titelinformationen.

Beteiligte Klassen/Komponenten: `AppUpdate`, `MvuHost`, `ShellAppEffectHandler`, `MediaDetailsPage`, `PlayerPage`, `MediaDetailsService`, `UpcomingEpisodesService`

### Weiterschauen-Tap auf Offline-Episode ohne Serien-Kennung

1. Wie oben bis Schritt 2.
3. Guard: `model.IsOffline && mediaType == "tvshowepisode" && item.ShowId is not > 0` → bisheriger Übergang `[NavigateToPlayerEffect, ResolvePlaybackSourceEffect]` (ohne Detail-Navigation, ohne `LoadUpcomingEpisodesEffect` — kein Serienkontext vorhanden).

Beteiligte Klassen/Komponenten: `AppUpdate`, `ShellAppEffectHandler`, `PlayerPage`

### Unveränderte Pfade

- Playlist-gebundene Einträge → `StartPlaylistPlaybackEffect` (vor der Detail-Entscheidung).
- Nicht abspielbare Typen (`tvshow`, `tvshowseason`, `moviecollection`) → `NavigateToMediaDetailsEffect` allein.
- Nicht wählbare Einträge/Platzhalter → keine Aktion.
- Kontextmenü „Ausblenden"/„Überspringen" → `Hide`/`SkipContinueWatchingEntryEffect`.
- Direktwiedergabe von der Detailseite (`PlayRequestedMessage`), aus Playlists und sonstigen Stellen.

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen

### `AppUpdate` (Logikklasse, `VideoPlayer.Mvu/AppUpdate.cs`)

- **Geänderte Methode:** `UpdateContinueWatchingItemSelected` — im abspielbaren Nicht-Playlist-Zweig wird dem `StartPlayback`-Übergang ein `NavigateToMediaDetailsEffect` (mit `item.MediaType`, `item.Id`, `item.Title`, `item.ShowId`, `item.SeasonId`, `item.EpisodeId`) vorangestellt; Ausnahme: `model.IsOffline` und `tvshowepisode` ohne `ShowId` → bisheriger Übergang ohne Detail-Effekt. `LoadUpcomingEpisodesEffect`-Anhängung bleibt. XML-Doc der Methode wird an das neue Verhalten angepasst.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine neuen — die bestehenden Guards (`IsSelectableEntry`, Typ-Normalisierung, Playlist-Bezug) bleiben maßgeblich.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **`MediaDetails`-Slot:** Der Detail-Ladevorgang belegt `model.MediaDetails` mit der Identität des Weiterschauen-Eintrags — identisch zum bisherigen Verhalten bei Navigation von anderen Stellen; die `MediaDetailsPage` prüft die Identität in `OnAppearing`/`UpdateUi`.
- **`NextEpisodeQueue`:** liest `model.MediaDetails.Details` zum Startzeitpunkt — zuvor geladene Detaildaten einer anderen Serie könnten theoretisch eine Queue erzeugen, die `LoadUpcomingEpisodesEffect` später korrekt überschreibt (Verwurf-Guard `UpcomingEpisodesLoaded_StaleOrOtherMedia_Discarded` greift nur bei Identitätswechsel). Bestehendes Verhalten, keine Verschlechterung.
- **Doppelte Detailseiten-Instanzen:** Mehrfaches Antippen desselben Eintrags schiebt jeweils eine neue `mediadetails`-Seite (kein Duplikat-Guard wie bei `sourceoverview`) — entspricht dem Verhalten der übrigen Detail-Navigationen (`UpdateMediaItemSelected`), kein neues Risiko.
- **Tests:** bestehende Unit-/E2E-Tests kodieren die alte Direktwiedergabe — siehe Abschnitt Tests.

## Umsetzungsreihenfolge

1. **`UpdateContinueWatchingItemSelected` anpassen** (`VideoPlayer.Mvu/AppUpdate.cs`)
   - Voraussetzungen: keine (alle Effekte/Modelle vorhanden)
   - Beschreibung: Detail-Effekt voranstellen, Offline-Guard ergänzen, XML-Doc aktualisieren.
2. **Unit-Tests anpassen und ergänzen** (`VideoWebPlayer.Maui.Tests/AppUpdateTests_ContinueWatching.cs`, `AppUpdateTests_PlaylistPlayback.cs`)
   - Voraussetzungen: Schritt 1
   - Beschreibung: Erwartete Effekt-Reihenfolge aktualisieren (Detail-Effekt an Position 0), neue Fälle Offline-Episode ohne/mit `ShowId`, Offline-Film, Hints-Durchreichung.
3. **E2E-Tests anpassen** (`VideoPlayer.E2E.Tests/Tests/ContinueWatchingPlaybackTests.cs`)
   - Voraussetzungen: Schritt 1
   - Beschreibung: Negativ-Nachweise „keine Detailseite" in positive Nachweise umkehren (Detail-Endpunkt wurde aufgerufen); ggf. Rückkehr-Nachweis nach Player-Close.
4. **Tests ausführen** — `dotnet test VideoWebPlayer.Maui.Tests`; E2E-Suite sofern ausführbar.
   - Voraussetzungen: Schritte 1–3

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `ContinueWatchingItemSelected_Movie_OpensDetailsAndStartsPlayback` (umbenannt/angepasst) | `AppUpdateTests_ContinueWatching` | Film: Effekte `[NavigateToMediaDetailsEffect, NavigateToPlayerEffect, ResolvePlaybackSourceEffect]`, Player-State belegt, Detail-Effekt mit Titel/Hints |
| Episode mit `ShowId` — Detail+Wiedergabe+`LoadUpcomingEpisodesEffect` | `AppUpdateTests_ContinueWatching` | Effekte `[NavigateToMediaDetailsEffect(mit show/episode-Hints), NavigateToPlayerEffect, ResolvePlaybackSourceEffect, LoadUpcomingEpisodesEffect]` |
| Episode ohne `ShowId` (online) — Detail+Wiedergabe, kein Upcoming-Load | `AppUpdateTests_ContinueWatching` | Server löst Kontext über `Season.Show` selbst auf |
| Offline-Episode ohne `ShowId` — Direktwiedergabe ohne Detail-Effekt | `AppUpdateTests_ContinueWatching` | `ConnectionState.Offline`-Model, `[NavigateToPlayerEffect, ResolvePlaybackSourceEffect]` |
| Offline-Episode mit `ShowId` — Detail+Wiedergabe+Upcoming-Load | `AppUpdateTests_ContinueWatching` | Offline-Snapshot der Detailseite kann Serienkontext laden |
| Offline-Film — Detail+Wiedergabe | `AppUpdateTests_ContinueWatching` | Film ist eigener Kontext (Manifest) |
| Offline-Modell-Hilfsmethode (`Offline()`/`Connected()`-Variante) | `AppUpdateTests_ContinueWatching` | `ConnectionState = ConnectionState.Offline` |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `AppUpdateTests_ContinueWatching` — alle `*StartsPlayback*`-Tests und `EpisodeWithoutShowId_NoShowLoad` | Neue Effekt-Reihenfolge; `DoesNotContain(NavigateToMediaDetailsEffect)` wird zum positiven Nachweis |
| `AppUpdateTests_PlaylistPlayback.ContinueWatchingSelected_WithoutPlaylist_KeepsSeriesPath` | Index-Assertions `effects[0..2]` → `effects[1..3]` |
| `AppUpdateTests_PlaylistPlayback.ContinueWatchingSelected_PlaylistWithoutEntry_KeepsSeriesPath` | Index-Assertion `effects[2]` → `effects[3]` |
| `ContinueWatchingSelected_WithPlaylist_StartsPlaylistPlayback` | bleibt gültig (Playlist-Pfad vor Detail-Entscheidung) — Assertions nur prüfen |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Tap auf Weiterschauen-Film → Wiedergabe startet UND Detail-Endpunkt geladen; nach Player-Close liegt die Detailansicht vor | `ContinueWatchingPlaybackTests` | Detailansicht mit Wiedergabe, Rückkehr zur Detailseite | Kern des Benutzerflusses (Navigationsstapel) — nur E2E nachweisbar |
| Pflicht | Tap auf Weiterschauen-Episode → Wiedergabe + Serienkontext (Fortsetzungsliste) bleibt; Player-Close → Detailansicht | `ContinueWatchingPlaybackTests` | Serienkontext-Funktion erhalten | Fortsetzungsliste + Stapel nur E2E-nachweisbar |
| Pflicht (bestehend) | Nicht abspielbarer Eintrag (`show`) → Detailansicht ohne Wiedergabe | `ContinueWatchingPlaybackTests` (`TapContinueWatchingShowEntry_OpensDetails_NotPlayback`) | unverändertes Verhalten | bereits vorhanden |
| Entfällt mit Begründung | Offline-Weiterschauen-Eintrag ohne Kontext → Direktwiedergabe | — (Unit-Tests auf Update-Ebene) | Offline-Fallunterscheidung | Die Offline-Weiterschauen-Liste enthält nur abgeschlossene Downloads — ein E2E-Szenario müsste Download-Endpunkte am `MockVideoServer` und einen Netzverlust-Übergang nachbilden; beides existiert in der E2E-Infrastruktur nicht. Die Unterscheidung ist eine reine Guard-Entscheidung der Update-Funktion und wird auf Unit-Ebene (Offline-Modell mit/ohne `ShowId`) nachgewiesen |

Anzupassende bestehende E2E-Tests:

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `TapContinueWatchingMovie_StartsPlaybackAtResume_WithoutDetails` | `Assert.False(WasPathRequested("/api/items/movie/8001"))` → Detail-Abruf ist nun erwartet; Test umbenennen (ohne „WithoutDetails") und Rückkehr zur Detailansicht nachweisen (z. B. nach Schließen des Players liegt `mediadetails` oben / Detailzustand geladen) |
| `TapContinueWatchingEpisode_StartsPlaybackAndAdvancesToNextEpisode` | Negativ-Assertions zu Episoden-Detailaufrufen entfernen/umkehren — der Episoden-Detailaufruf bzw. `tvshow`-Abruf der Detailseite ist nun erwartet; Folgevideo-Verhalten unverändert |

## Offene Punkte

Keine.
