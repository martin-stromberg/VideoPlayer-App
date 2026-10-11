# Inventar — Ablauflogik und Konventionen

## MVU-Kern

- `AppUpdate.Update(model, message)` → `AppTransition(AppModel, IReadOnlyList<AppEffect>)`;
  zentraler `switch` über alle `AppMessage`-Records; unbekannte Messages → `Unchanged`.
- Effekte: `ShellAppEffectHandler.HandleAsync` → `switch` über `AppEffect`-Records →
  private Methoden → Serverzugriffe über DI-Services → Ergebnis via
  `DispatchMessageAsync` zurück in den Zyklus (immer auf dem MainThread dispatchen).
- Seiten: abonnieren `MvuHost.ModelChanged` in `OnAppearing`/abmelden in
  `OnDisappearing`; `UpdateUi(model)` spiegelt den Zustand; Benutzeraktionen
  dispatchen Messages (kein direkter Servicezugriff aus Pages).
- XML-Dokumentationspflicht (CS1591 = Fehler); alle neuen öffentlichen
  Member deutsch dokumentieren.

## Dialog-/Menümuster (seiten-nah)

- `DisplayActionSheetAsync` auf `Page` — Muster: `HomePage.OnContinueWatchingItemLongPressed`
  (Kontextmenü → Auswahl → Message dispatchen).
- `DisplayAlert`/`DisplayAlertAsync` — Muster: `ServerSetupPage` (Hinweise/Bestätigungen).
- Konflikt-Dialoge aus Zustandsänderung: `Pending*`-Feld im State → `UpdateUi`
  öffnet den Dialog einmal je Konfliktinstanz → Bestätigung/Abbruch dispatchen
  Folge-Messages, die den Pending-Zustand auflösen.

## Vorhandene Playlist-Flüsse

- `PlaylistsPageOpenedMessage` → `LoadPlaylistsEffect` → `PlaylistsLoadedMessage`
  (nur online; Offline → `PlaylistsOfflineError`).
- `PlaylistSelectedMessage` → `NavigateToPlaylistDetailsEffect` +
  `LoadPlaylistEntriesEffect` (Kopfdaten kommen aus dem Listeneintrag!).
- `PlaylistDetailsOpenedMessage` (Wiederherstellungspfad) →
  `LoadPlaylistDetailsEffect` + `LoadPlaylistEntriesEffect`.
- `PlaylistEntriesLoadedMessage` (Page 1 ersetzen / Append nur bei
  `Page == NextPage`), Retry-Pfade vorhanden.
- `PlaylistEntrySelectedMessage` → abspielbar+zugänglich →
  `StartPlaylistPlaybackEffect(entryId)`; sonst `NavigateToMediaDetailsEffect`.
- Wiedergabe: `StartPlaylistPlayback` + `play/next|previous|advance`
  (204 → kein Folgeeintrag).

## Konflikt- und Bestätigungsflüsse (neu)

- **Entfernen mit Weiterschauen-Bezug**: `DELETE …/entries/{mediaType}/{mediaId}`
  → 409 + `IsContinueWatchingConfirmationRequired` → State
  `PendingRemoveConflict` (Eintrag) → `DisplayAlert`-Bestätigung →
  Wiederholung mit `?confirmContinueWatchingRemoval=true` → Erfolg →
  Eintragsliste neu laden.
- **Sortiermodus weg von „Manuell"**: `PATCH …/sort-mode` → 409 +
  `IsLossOfDataConfirmationRequired` → State `PendingSortModeConflict`
  (Ziel-Modus) → `DisplayAlert`-Bestätigung → Wiederholung mit
  `ConfirmLossOfManualOrder=true` → Header/Einträge neu laden.
- **Löschen**: `DisplayAlert` direkt im Klickpfad (kein Pending-State nötig —
  wie Hide/Skip der Weiterschauen-Liste).

## Verschiebe-Semantik (Server)

- `move-to-beginning`: Server setzt `SortOrder=0`-Verschiebung korrekt
  (eigener Endpunkt).
- `move-between`: `NewSortOrder` = `SortOrder` des Ziel-Nachbarn →
  Tausch-Effekt bei benachbarter Verschiebung (vor/zurück um eins).
- „Ans Ende": `GET max-sort-order` + `PUT order` mit `max+1` (Server-UI-Pattern).
- Alle Verschiebe-Endpunkte nur bei `SortMode=Manual` (sonst 400
  `PlaylistNotInManualSortMode`); UI blendet die Optionen sonst aus.

## Berechtigungen

- `IsOwner`: alle Schreibaktionen; `IsAdmin` (JWT-Claim): nur das
  Öffentlich-Flag. UI blendet aus; Server prüft zusätzlich (403).
- Hinzufügen ist nur in **eigene** Playlists erlaubt → Auswahl-Blatt listet
  ausschließlich `Own` (IsOwner).
