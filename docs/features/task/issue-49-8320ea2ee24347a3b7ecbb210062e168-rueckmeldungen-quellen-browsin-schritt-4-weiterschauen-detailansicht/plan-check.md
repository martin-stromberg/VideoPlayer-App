# Plan-Gegenprüfung

Geprüft: `requirement.md`, `inventory.md` inkl. Detaildokumente, `plan.md` (Eigenprüfung — keine Unteragenten verfügbar).

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Tap auf abspielbaren Weiterschauen-Eintrag (Film/Episode) öffnet Detailansicht mit automatisch startender Wiedergabe darüber | `NavigateToMediaDetailsEffect` vor `NavigateToPlayerEffect`+`ResolvePlaybackSourceEffect` in `UpdateContinueWatchingItemSelected`; sequenzielle Effektausführung in `MvuHost` | Unit: neue Effekt-Reihenfolge in `AppUpdateTests_ContinueWatching`; E2E-Pflicht: Film- und Episoden-Szenario mit Detail-Endpunkt-Nachweis | Abgedeckt |
| „Player schließen" kehrt zur Detailansicht zurück (statt Startliste) | `player` liegt über `mediadetails` auf dem Shell-Stack; `PlayerClosedMessage` leert nur den Player-State | E2E-Pflicht: Rückkehr-Nachweis nach Player-Close im Film-/Episoden-Szenario | Abgedeckt |
| Serienkontext-Funktion (Fortsetzungsliste) bleibt erhalten | `LoadUpcomingEpisodesEffect` bei `tvshowepisode` mit `ShowId` unverändert als letzter Effekt | Unit: Episode-mit-`ShowId`-Test; E2E: Folgevideo-Wechsel im Episoden-Szenario bleibt nachgewiesen | Abgedeckt |
| Nicht abspielbare Einträge (Serie/Staffel/Kollektion) → unverändert Detail-Navigation | Zweig bleibt unverändert | Bestands-Unit-Theory `NonPlayable_NavigatesToDetails` + E2E `TapContinueWatchingShowEntry_OpensDetails_NotPlayback` bleiben gültig | Abgedeckt |
| Kontextmenü „Ausblenden"/„Überspringen" unverändert | Keine Änderung an `UpdateContinueWatchingHide/SkipRequested` | Bestands-E2E `ContinueWatchingContextMenuTests` unverändert | Abgedeckt |
| Direktwiedergabe an anderen Stellen nicht zurückgebaut | `PlayRequestedMessage`-Pfad (`UpdatePlayRequested` → `StartPlayback`) unverändert | Bestandstests der Detailseiten-Wiedergabe bleiben gültig | Abgedeckt |
| Playlist-gebundene Einträge → direkter Playlist-Start | `PlaylistId`+`PlaylistEntryId`-Zweig vor der Detail-Entscheidung, unverändert | Unit: `ContinueWatchingSelected_WithPlaylist_StartsPlaylistPlayback` bleibt gültig | Abgedeckt |
| Offline-Einträge mit Kontext → Detail+Wiedergabe; ohne nutzbaren Kontext → bisherige Direktwiedergabe | Guard `IsOffline && tvshowepisode && ShowId nicht > 0` → alter Pfad; Offline-Film und Episode mit `ShowId` → Detail+Wiedergabe (Offline-Snapshot via `LoadOfflineAsync`) | Unit: drei neue Offline-Fälle; E2E entfällt begründet (siehe E2E-Tabelle) | Abgedeckt |
| Nicht wählbare Platzhalter → keine Aktion | `IsSelectableEntry` unverändert | Bestands-Theory `NonSelectable_Unchanged` | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Tap Film → Detailansicht + Wiedergabe, Player-Close → Detailansicht | `ContinueWatchingPlaybackTests` — angepasstes Film-Szenario (Detail-Endpunkt wurde aufgerufen; nach Close liegt die Detailseite vor) | Abgedeckt |
| Tap Episode → Detailansicht + Wiedergabe + Folgevideo-Funktion | `ContinueWatchingPlaybackTests` — angepasstes Episoden-Szenario | Abgedeckt |
| Nicht abspielbarer Eintrag → nur Detailansicht | `TapContinueWatchingShowEntry_OpensDetails_NotPlayback` (bestehend) | Abgedeckt |
| Offline-Eintrag ohne Kontext → Direktwiedergabe / mit Kontext → Detail+Wiedergabe | Kein E2E — Offline-Weiterschauen-Einträge entstehen nur aus abgeschlossenen Downloads; `MockVideoServer` bietet keine Download-Endpunkte und die E2E-Infra keinen Netzverlust-Übergang. Guard-Logik wird auf Unit-Ebene nachgewiesen (`ConnectionState.Offline`-Modell) | Nicht erforderlich mit Begründung |

## Hinweise

- Die E2E-Suite benötigt einen Windows-Debug-Build der App und eine interaktive Fenstersitzung — ist die Ausführung in der Laufumgebung nicht möglich, ist das gemäß Lifecycle im Testschritt zu dokumentieren (kein stiller Ersatz).
- Effektreihenfolge ist der sicherheitskritische Punkt: `NavigateToMediaDetailsEffect` muss strikt vor `NavigateToPlayerEffect` liegen; die Unit-Tests prüfen die Positionen explizit.
- Pre-Commit-Hooks (XML-Doc an public APIs, Stub-/Übersetzungs-Checks) beim Implementierungscommit beachten.
