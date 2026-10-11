# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Playlists erstellen/bearbeiten/löschen | `PlaylistFormState` + `PlaylistFormPanel` auf Übersicht + Detailseite; `SavePlaylistEffect` (POST/PUT), `DeletePlaylistEffect` + `NavigateBackEffect`; Bestätigungsdialog seiten-nah | Unit: Form-Fluss, Guards, Ergebnis-Messages; E2E Szenarien 1–3 | Abgedeckt |
| Medium von Detailseite hinzufügen | `AddToPlaylistButton` → `LoadPlaylistTargetsEffect` → ActionSheet mit Playlist-Namen → `AddPlaylistEntryEffect` (POST entries); keine ID-Eingabe | Unit: Targets-/Picker-/Add-Fluss; E2E Szenario 4 | Abgedeckt |
| Eintrag entfernen inkl. 409-Weiterschauen-Konflikt | ⋮-Schalter je Eintrag (`EntryMenu_{EntryId}`) → `RemovePlaylistEntryEffect`; `ApiException.StatusCode=409` + `IsContinueWatchingConfirmationRequired`-Parsing → `PendingRemoveConflict` → DisplayAlert → Retry mit Query-Flagge | Unit: Konflikt-Parsing + Pending/Confirm-Pfad; E2E Szenarien 5–6 | Abgedeckt |
| Einträge an Anfang/zwischen Positionen verschieben | Kontextmenü „An den Anfang", „Ans Ende" (max-sort-order + order), „Eins nach vorn/hinten" (move-between mit Nachbar-SortOrder); nur `CanReorder` (Owner + Manual) | Unit: Ziel-SortOrder-Ermittlung, Guards; E2E Szenario 7 | Abgedeckt |
| Sortiermodus umschalten inkl. Verlust-Bestätigung | `ChangePlaylistSortModeEffect` (PATCH); 409 → `PendingSortModeConflict` → DisplayAlert → Retry mit `ConfirmLossOfManualOrder` | Unit: Completed/Confirmed-Fluss; E2E Szenario 8 | Abgedeckt |
| Genres setzen + zurücksetzen | `PlaylistGenreEditorPanel` (Chips, Vorauswahl `AllGenreIds`) → PUT genres / POST genres/reset; Zurücksetzen nur bei `GenresManuallyOverridden` | Unit: Optionen laden/togglen/speichern; E2E Szenario 9 | Abgedeckt |
| Öffentlich-Flag pflegen | Menüpunkt „Öffentlich schalten"/„Privat schalten" nur bei `CanSetPublic` (IsOwner + `IsAdmin`-Claim); PUT public; 403 → Fehlerhinweis | Unit: Claim-Lesung, Wache; E2E Szenario 10 | Abgedeckt |
| Fremde öffentliche Playlists schreibgeschützt | `CanManage=false` bei `IsOwner=false` → keine „Verwalten"-/⋮-Schalter; Owner-Wache in `AppUpdate` | Unit: Fremd-Wache (Unchanged); E2E Szenario 11 | Abgedeckt |
| Keine Cover-Verwaltung | Plan enthält keine Cover-Endpunkte/ UI; Abgrenzung in `requirement.md` dokumentiert | — | Abgedeckt |
| Keine ID-/Kennungseingabe | Playlist-Auswahl per Name im ActionSheet; Genres per Chip | Usability-Review prüft erneut | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine — alle Akzeptanzkriterien besitzen Unit- und (für UI-Flüsse) E2E-Nachweise.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Playlist erstellen | Szenario 1 (Formular → Übersicht + POST-Assertion) | Abgedeckt |
| Playlist umbenennen | Szenario 2 | Abgedeckt |
| Playlist löschen (Bestätigung) | Szenario 3 | Abgedeckt |
| Medium zu Playlist hinzufügen | Szenario 4 | Abgedeckt |
| Eintrag entfernen | Szenario 5 | Abgedeckt |
| Entfernen mit 409-Bestätigung | Szenario 6 | Abgedeckt |
| Verschieben (Anfang/Position) | Szenario 7 | Abgedeckt |
| Sortiermodus mit 409-Bestätigung | Szenario 8 | Abgedeckt |
| Genres setzen/zurücksetzen | Szenario 9 | Abgedeckt |
| Öffentlich-Flag | Szenario 10 | Abgedeckt |
| Fremde Playlist schreibgeschützt | Szenario 11 | Abgedeckt |

## Fehlende oder unvollständige Planbestandteile

Keine.

## Hinweise

- Neue Enums werden vermieden (EnumCoverage-Constraint); Konflikt-/
  Ergebnistypen sind Records mit Bool-Flags.
- ⋮-Eintragsschalter tragen eindeutige AutomationIds (`EntryMenu_{EntryId}`),
  damit DevApi-Invoke pro Eintrag adressierbar bleibt (UIA-Lücke der
  CollectionView-Items).
- Der Mock erhält `SortOrder`-Verwaltung (Listenindex = SortOrder) und
  ein `LoginIsAdmin`-Opt-in; JWT-Claim `IsAdmin` muss das Mock-Token
  mittragen — im Mock zu verifizieren.
