# Übersetzte Anforderung: Direkte Wiedergabe aus der Weiterschauen-Liste

(Entwicklungsschritt 1 von 8 des Projektplans Issue #43 — nur dieser Schritt ist Gegenstand dieser Anforderung.)

## Fachliche Zusammenfassung

Ein Tap auf einen Eintrag der „Weiterschauen"-Liste der `HomePage` soll bei abspielbaren Medientypen (`movie`/`episode`) nicht mehr die Detailseite öffnen, sondern direkt die Wiedergabe starten — über den bestehenden Wiedergabepfad (`PlayerState` + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`), beginnend an der Fortsetzungsposition, die `PlaybackSourceService.ResolveAsync` selbst ermittelt. Einträge aller übrigen Listen und nicht abspielbare Weiterschauen-Typen (`show`/`season`/`collection`) navigieren weiterhin ausschließlich zur Detailansicht. Die Folgevideo-/Auto-Fortsetzungs-Funktion für Serien-Episoden muss erhalten bleiben: Der Serienkontext für `PlayerState.UpcomingEpisodes` ist ohne Detailseiten-Navigation per Datenabruf (`api/items/tvshow/{showId}`, `ShowId` aus den Serienkontext-Hints des `HomeMediaItem`) aufzulösen — die Detailansicht wird nicht im Hintergrund geöffnet.

## Betroffene Klassen und Komponenten

- `VideoPlayer.Maui/Controls/MediaCarousel.xaml(.cs)` — Auslösepunkt der Auswahl; die Herkunftsliste (Weiterschauen) muss unterscheidbar gemacht werden (z. B. eigener `ItemTapped`-Handler am Weiterschauen-Karussell in `HomePage.xaml` statt eines neuen Control-Attributs).
- `VideoPlayer.Maui/Pages/HomePage.xaml(.cs)` — drei `MediaCarousel`-Instanzen teilen derzeit `OnMediaItemTapped` → `MediaItemSelectedMessage`; das Weiterschauen-Karussell benötigt einen eigenen Handler, der eine neue Nachricht dispatcht. Für den E2E-Zugriff fehlt dem Weiterschauen-Karussell außerdem eine `AutomationId` (Muster: `RecentCarousel`).
- `VideoPlayer.Mvu/AppMessage.cs` — neue Nachricht für die Weiterschauen-Auswahl (z. B. `ContinueWatchingItemSelectedMessage(HomeMediaItem)`) sowie eine Ergebnis-Nachricht für die asynchron geladene Fortsetzungsliste (z. B. `UpcomingEpisodesLoadedMessage`).
- `VideoPlayer.Mvu/AppEffect.cs` — neuer Effekt für die detailseiten-unabhängige Serienkontext-Ladung (z. B. `LoadUpcomingEpisodesEffect(MediaId, ShowId)`).
- `VideoPlayer.Mvu/AppUpdate.cs` — neuer `Update…`-Handler: Guard wie `UpdateMediaItemSelected`; bei `movie`/`tvshowepisode` (nach `MediaTypeNames.Normalize`) Wiedergabestart analog `UpdatePlayRequested` plus Folge-Episoden-Ladeeffekt bei bekanntem `ShowId`; sonst `NavigateToMediaDetailsEffect` (Fallback).
- `VideoPlayer.Mvu` — neue Logikklasse für die Serienkontext-Auflösung ohne Detailseite (z. B. `UpcomingEpisodesService`): online `VideoPlayerApiClient.GetTvShowAsync` (+ `ShowDetailsCache.WriteAsync`), offline `ShowDetailsCache.ReadAsync`; staffelübergreifende Episodenfolge wie `AppUpdate.NextEpisodeQueue`/`AutoDownloadService.EnqueueNextEpisodesAsync`.
- `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` — Behandlung des neuen Effekts (Dienstaufruf + Rückdispatch der Ergebnisnachricht, Fehler isoliert).
- `VideoPlayer.Maui/MauiProgram.cs` — DI-Registrierung der neuen Logikklasse (Singleton, Konvention der übrigen Mvu-Services).
- `VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs` — ggf. Erweiterung um ein Feld der Fortsetzungsliste (z. B. `PlayerUpcomingEpisodeCount`), damit der Folgevideo-Beleg E2E prüfbar ist.
- `VideoPlayer.E2E.Tests/Support/MockVideoServer.cs` — Erweiterung um konfigurierbare Weiterschauen-Einträge (inkl. `episode`-Typ mit eingebettetem `season`/`show`-Graph), `GET api/items/tvshow/{id}` und `GET api/items/tvshowepisode/{id}/stream`.
- `VideoPlayer.E2E.Tests/Support/DevApiModels.cs` — ggf. Erweiterung des Modell-Snapshots um das neue Fortsetzungslisten-Feld.
- Tests: `VideoWebPlayer.Maui.Tests` (`AppUpdateTests_Player`/`AppUpdateTests_HomeLists`-nahe neue Themenklasse, `HomePageContractTests`, Service-Tests der neuen Logikklasse), `VideoPlayer.E2E.Tests` (neue Szenarien).

## Implementierungsansatz

- Unterscheidung der Herkunftsliste am Auslösepunkt: Das Weiterschauen-`MediaCarousel` in `HomePage.xaml` erhält einen eigenen `ItemTapped`-Handler (`OnContinueWatchingItemTapped`), der statt `MediaItemSelectedMessage` eine neue `ContinueWatchingItemSelectedMessage` dispatcht. Die übrigen Karussells und alle anderen Auswahlpfade (Favoriten-Seite, Quellen-Übersicht, Episodenliste, Kollektionen) bleiben unverändert bei `MediaItemSelectedMessage`/`SourceMediaItemSelectedMessage` — dadurch ist die Abgrenzung „andere Listen" strukturell sichergestellt.
- Wiedergabestart: Wiederverwendung des bestehenden Wiedergabepfads — `PlayerState` belegen (`IsResolving`, `Attempted`, `UpcomingEpisodes` aus dem ggf. vorhandenen Detailseiten-Serienkontext wie bei `UpdatePlayRequested`), `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`. Die Fortsetzungsposition kommt ohne Zusatz über `PlaybackSourceService.ResolveAsync` mit (`api/continue-watching` bzw. `cached_lists[continue_watching]` + `pending_progress`).
- Abspielbarkeit: `MediaTypeNames.Normalize` → `movie`/`tvshowepisode` starten direkt; `show`/`season`/`collection` (= `tvshow`/`tvshowseason`/`moviecollection`) sowie `IsPlaceholder`-Einträge (`!IsSelectable`) fallen auf `NavigateToMediaDetailsEffect` zurück.
- Folgevideo-Erhalt: Für Episoden mit `ShowId > 0` wird zusätzlich `LoadUpcomingEpisodesEffect` ausgelöst; der Effekt-Handler lädt die Serie über `GetTvShowAsync` (und schreibt `ShowDetailsCache`), offline liest er `ShowDetailsCache`. Die geordnete Folgeliste wird als `UpcomingEpisodesLoadedMessage` zurückgespielt; `AppUpdate` übernimmt sie in `PlayerState.UpcomingEpisodes`, sofern der Player noch dieselbe Episode spielt (Identitäts-Guard analog `UpdatePlaybackSourceResolved`). Auto-Fortsetzung/Fehler-Rückfall (`AdvanceToNextEpisode`) arbeiten danach unverändert.
- Keine Hintergrund-Detailansicht: `model.MediaDetails` wird nicht befüllt; `NavigateToMediaDetailsEffect`/`LoadMediaDetailsEffect` werden im Direktpfad nicht ausgelöst.

## Konfiguration

Keine — das Verhalten ist nicht konfigurierbar (keine neue Einstellung).

## Offene Fragen

- Keine. Die fachlich relevanten Entscheidungen sind in der Anforderung beschlossen: kein Hintergrund-Laden der Detailansicht, Fallback auf Detailansicht bei nicht abspielbaren Typen, Serienkontext-Auflösung per Datenabruf über `api/items/tvshow/{showId}`.

## Akzeptanzkriterien (aus der Kundenanforderung)

1. Ein Tap auf einen abspielbaren Weiterschauen-Eintrag (`movie`/`episode`) startet die Wiedergabe direkt, ohne zuvor die Detailansicht zu öffnen.
2. Die Wiedergabe beginnt an der Fortsetzungsposition, nicht am Anfang.
3. Taps auf Einträge in Favoriten, „Neu im Programm" und Quellen-Übersichten (sowie Favoriten-Seite, Episodenliste, Kollektionen) öffnen weiterhin nur die Detailansicht.
4. Nicht abspielbare Weiterschauen-Einträge (`show`/`season`/`collection`) öffnen weiterhin die Detailansicht.
5. Nach Ende einer direkt aus der Weiterschauen-Liste gestarteten Episode wird die nächste Serienepisode wie bisher als Folgevideo bereitgestellt (Auto-Fortsetzung/Folge-Episoden-Auflösung bleibt erhalten; der Serienkontext wird dafür ohne Detailansicht geladen).
