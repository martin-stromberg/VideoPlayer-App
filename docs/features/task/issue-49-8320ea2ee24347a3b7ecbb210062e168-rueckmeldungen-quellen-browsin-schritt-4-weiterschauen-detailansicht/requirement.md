# Übersetzte Anforderung – Weiterschauen-Eintrag öffnet Titel-Detailansicht mit Wiedergabe

Quelle: Projektplan `docs/projects/task/issue-49-8320ea2ee24347a3b7ecbb210062e168-rueckmeldungen-quellen-browsin/project-plan.md`, Entwicklungsschritt 4 (Issue #49, Punkt 4), inklusive der dort als verbindlich dokumentierten Projektklärungen (Offene Punkte #2 und #3).

## Fachliche Zusammenfassung

Ein Tipp auf einen direkt abspielbaren Eintrag (`movie`/`tvshowepisode`) der Weiterschauen-Liste der Startseite soll nicht mehr nur die Wiedergabe starten, sondern zuerst die Titel-Detailansicht (`mediadetails`-Route, `MediaDetailsPage`) des Titels auf den Navigationsstack schieben und darüber die Wiedergabe (`player`-Route, `PlayerPage`) öffnen — Parität zur Serveranwendung („Detailansicht des Titels mit Player"). „Player schließen" kehrt damit zur Detailansicht zurück statt zur Startliste. Playlist-gebundene Einträge, nicht abspielbare Typen und der bestehende Offline-Sonderfall behalten bzw. erhalten ihr in der Anforderung festgelegtes Verhalten.

## Betroffene Klassen und Komponenten

- `VideoPlayer.Mvu/AppUpdate.cs` — `UpdateContinueWatchingItemSelected` (Kern der Änderung): für abspielbare, nicht Playlist-gebundene Einträge zusätzlich `NavigateToMediaDetailsEffect` vor dem bisherigen `StartPlayback`-Übergang (`NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`); `LoadUpcomingEpisodesEffect` bei Episoden mit `ShowId` bleibt erhalten.
- `VideoPlayer.Mvu/AppEffect.cs` — bestehende Effekte werden wiederverwendet: `NavigateToMediaDetailsEffect`, `NavigateToPlayerEffect`, `ResolvePlaybackSourceEffect`, `LoadUpcomingEpisodesEffect`, `StartPlaylistPlaybackEffect`. Kein neuer Effekt nötig.
- `VideoPlayer.Mvu/HomeMediaItem.cs` — unverändert; liefert die Kontext-Ids (`ShowId`, `SeasonId`, `EpisodeId`, `PlaylistId`, `PlaylistEntryId`, `IsPlaceholder`/`IsSelectable`).
- `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` — unverändert; `NavigateToMediaDetailsAsync` und `NavigateToPlayerAsync` pushen nacheinander auf den Shell-Stack (Effekte laufen sequenziell in `MvuHost.DispatchAsync`).
- `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml.cs` — unverändert; lädt ihre Daten selbst über `MediaDetailsRequestedMessage` in `OnAppearing` (inkl. Offline-Snapshot über `MediaDetailsService.LoadOfflineAsync`).
- `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs` — unverändert; „Player schließen"/`NavigateBackEffect` poppt zur darunterliegenden Detailseite.
- Tests: `VideoWebPlayer.Maui.Tests/AppUpdateTests_ContinueWatching.cs` (anzupassen/erweitern), `AppUpdateTests_PlaylistPlayback.cs` (Erwartungen prüfen), E2E `VideoPlayer.E2E.Tests/Tests/ContinueWatchingPlaybackTests.cs` (Szenarien an das neue Soll-Verhalten anpassen).

## Implementierungsansatz

- In `UpdateContinueWatchingItemSelected` bleiben die Guards (`IsSelectableEntry`), der Nicht-abspielbar-Fallback (`NavigateToMediaDetailsEffect` allein) und der Playlist-Pfad (`StartPlaylistPlaybackEffect` bei `PlaylistId` + `PlaylistEntryId` > 0) unverändert.
- Für `movie`/`tvshowepisode` ohne Playlist-Bezug wird dem `StartPlayback`-Übergang ein `NavigateToMediaDetailsEffect` mit denselben Kontext-Hints (`ShowId`, `SeasonId`, `EpisodeId`) vorangestellt — die Effektreihenfolge stellt sicher, dass die Detailseite unter dem Player liegt.
- Sonderfall Offline (`model.IsOffline`): Einträge ohne nutzbaren Detail-Kontext behalten die bisherige Direktwiedergabe. Entscheidbar ist das rein über den Eintrag: eine `tvshowepisode` ohne `ShowId` kann die Offline-Detailansicht nicht im Serienkontext aufbauen → Direktstart. `movie`-Einträge sowie Episoden mit `ShowId` öffnen auch offline die Detailansicht mit Wiedergabe (der `MediaDetailsService`-Offline-Snapshot baut aus Download-Manifest/gecacheter Serienstruktur; die Offline-Weiterschauen-Liste enthält ohnehin nur abgeschlossene Downloads mit Datei). Online bleibt der Start unverändert — der Server löst den Serienkontext über den eingebetteten `Season.Show`-Bezug auch ohne `ShowId`-Hint auf.
- `LoadUpcomingEpisodesEffect` (Fortsetzungsliste der Folge-Episoden für Episoden mit `ShowId`) bleibt als letzter Effekt erhalten — die Funktion wird weiterhin gebraucht, da die Detaildaten zum Startzeitpunkt noch nicht im Model liegen.
- Die Direktwiedergabe an anderen Stellen (`PlayRequestedMessage` der Detailseite, Playlist-Pfade) wird nicht zurückgebaut.

## Konfiguration

Keine — das Verhalten ist nicht konfigurierbar.

## Offene Fragen / Entscheidungen

- **Auto-Start vs. nur Detailansicht:** Verbindlich geklärt (Projektplan, Offener Punkt 2) — die Wiedergabe startet beim Tipp automatisch über der geöffneten Detailansicht.
- **Offline-Film ohne jede Kennung:** Annahme (dokumentiert): Ein `movie`-Eintrag ist selbst sein nutzbarer Kontext — die Offline-Detailansicht (`LoadOfflineAsync`, Fall `movie`) benötigt nur `MediaId`/Manifesteintrag, der für Weiterschauen-Download-Einträge gesichert vorliegt. Daher öffnet ein Offline-Film die Detailansicht mit Wiedergabe. Eine Offline-Episode ohne `ShowId` (fehlende Serien-Kennung) behält dagegen die Direktwiedergabe.
- **„Nicht wählbare Platzhalter":** unverändert durch `IsSelectableEntry` abgedeckt (`IsPlaceholder` → `IsSelectable == false` → keine Aktion).
