# Implementierungsplan — Schritt 6: Playlist-Verwaltung

## Architektur-Überblick

Alle Verwaltungsflüsse folgen dem MVU-Kreislauf: Page → `AppMessage` →
`AppUpdate.Update` → `AppEffect` → `ShellAppEffectHandler` → `PlaylistService`
→ `VideoPlayerApiClient` → Ergebnis-`AppMessage` → Model → `UpdateUi`.
Dialoge (ActionSheet/Alert) bleiben seiten-nah (Muster `HomePage`/
`ServerSetupPage`); wiederholbare serverseitige Konflikte (HTTP 409) laufen
über `Pending*`-Zustände, die `UpdateUi` einmalig in einen
Bestätigungsdialog übersetzt.

## 1. VideoPlayer.ApiClient — DTOs + Endpunkte

### Neue Dateien in `VideoPlayer.ApiClient/Models/`

- `PlaylistAdminModels.cs` — Request/Response-DTOs der Verwaltung
  (eine Datei, wie `MediaSource.cs` mehrere DTOs bündelt):
  - `CreatePlaylistRequest { Name, Description?, SortMode }`
  - `UpdatePlaylistRequest { Name, Description? }`
  - `AddPlaylistEntryRequest { MediaType, MediaId }`
  - `PlaylistAddResult { AddedCount, SkippedCount, Message? }`
  - `PlaylistConflictResponse { IsContinueWatchingConfirmationRequired, IsLossOfDataConfirmationRequired, IsCoverDeletionConfirmationRequired }`
  - `ReorderPlaylistEntryRequest { NewSortOrder }`
  - `ChangeSortModeRequest { NewSortMode, ConfirmLossOfManualOrder }`
  - `SetPlaylistGenresRequest { GenreIds }` (`IReadOnlyList<long>`)
  - `SetPlaylistPublicRequest { IsPublic }`
  - `PlaylistMaxSortOrderResult { MaxSortOrder }` (`long?`)

### `VideoPlayerApiClient` — neue Methoden (Region „Playlists")

| Methode | HTTP | Route |
|---|---|---|
| `CreatePlaylistAsync(CreatePlaylistRequest)` → `Playlist` | POST | `api/playlists` |
| `UpdatePlaylistAsync(id, UpdatePlaylistRequest)` → `Playlist` | PUT | `api/playlists/{id}` |
| `DeletePlaylistAsync(id)` | DELETE | `api/playlists/{id}` |
| `AddPlaylistEntryAsync(id, AddPlaylistEntryRequest)` → `PlaylistAddResult` | POST | `api/playlists/{id}/entries` |
| `RemovePlaylistEntryAsync(id, mediaType, mediaId, confirm)` | DELETE | `api/playlists/{id}/entries/{mediaType}/{mediaId}[?confirmContinueWatchingRemoval=true]` |
| `MovePlaylistEntryToBeginningAsync(id, entryId)` → `PlaylistEntry` | POST | `…/entries/{entryId}/move-to-beginning` |
| `MovePlaylistEntryBetweenAsync(id, entryId, ReorderPlaylistEntryRequest)` | POST | `…/entries/{entryId}/move-between` |
| `ReorderPlaylistEntryAsync(id, entryId, ReorderPlaylistEntryRequest)` | PUT | `…/entries/{entryId}/order` |
| `GetPlaylistMaxSortOrderAsync(id)` → `long?` | GET | `…/entries/max-sort-order` |
| `ChangePlaylistSortModeAsync(id, ChangeSortModeRequest)` → `Playlist` | PATCH | `api/playlists/{id}/sort-mode` |
| `SetPlaylistGenresAsync(id, SetPlaylistGenresRequest)` → `Playlist` | PUT | `api/playlists/{id}/genres` |
| `ResetPlaylistGenresAsync(id)` → `Playlist` | POST | `api/playlists/{id}/genres/reset` |
| `SetPlaylistPublicAsync(id, SetPlaylistPublicRequest)` → `Playlist` | PUT | `api/playlists/{id}/public` |

Neue Send-Helfer: `PutJsonAsync<T>(endpoint, payload, ct)`,
`PatchJsonAsync<T>(endpoint, payload, ct)` (HttpMethod.Patch),
`DeleteAsync(endpoint, ct)` — alle über `SendWithPolicyAsync`
(409 bleibt `ApiException` mit `ResponseBody`).

## 2. VideoPlayer.Mvu — Modelle, Service, Messages/Effekte

### `MediaTypeNames`

- Neuer Helfer `ToServerName(string)` → kanonische Enum-Namen
  (`"movie"→"Movie"`, `"tvshow"→"TVShow"`, `"tvshowseason"→"TVShowSeason"`,
  `"tvshowepisode"→"TVShowEpisode"`, `"moviecollection"→"MovieCollection"`,
  unbekannt → Eingabe durchreichen). Für Add-Body und Delete-Pfadsegment.

### Bestehende Records erweitern

- `PlaylistListItem` += `AllGenreIds` (`IReadOnlyList<long>`),
  `GenresManuallyOverridden` (bool); `MapPlaylist` befüllt beides.
- `PlaylistEntryItem` += `SortOrder` (long?); `LoadEntriesAsync` mappt es.
- `PlaylistDetailsState` += `IsOwner`, `IsPublic`, `IsAdmin`,
  `GenresManuallyOverridden`, `AllGenreIds`, `IsManaging`, `ManageNotice`,
  `ManageNoticeIsError`, `PendingRemoveConflict` (`PlaylistEntryItem?`),
  `PendingSortModeConflict` (string? = Ziel-Sortiermodus);
  Derived: `CanManage => IsOwner && IsActive`,
  `CanReorder => CanManage && SortMode == Manual`,
  `CanSetPublic => IsOwner && IsAdmin`.
- `PlaylistsPageState` += `CanCreate` (immer true bei Connected — Server erlaubt
  jedem Nutzer das Anlegen) — kein Zusatzfeld nötig; Schalter direkt online
  sichtbar.
- `MediaDetailsState` += `PlaylistTargets` (`IReadOnlyList<PlaylistListItem>?`),
  `IsLoadingPlaylistTargets`, `PlaylistPickerPending`,
  `IsAddingToPlaylist`, `AddToPlaylistNotice`, `AddToPlaylistNoticeIsError`.
- `AppModel` += `PlaylistForm` (`PlaylistFormState`),
  `PlaylistGenreEditor` (`PlaylistGenreEditorState`).
- `PlaylistInfo`/`ProfileInfoService`: `internal static bool ReadIsAdminClaim(token)`
  (Payload-Dekodierung wie `ReadTokenClaims`, Claim `"IsAdmin"`, `"True"`→true;
  unlesbar/fehlend → false = Toggle verborgen, Server prüft ohnehin).

### Neue Records (`PlaylistManagementModels.cs` in Mvu)

- `PlaylistFormState { IsOpen, EditingPlaylistId (long?), Name, Description, SortMode, IsSaving, Error }`
  — `EditingPlaylistId = null` ⇒ Anlage (Sortiermodus-Schalter aktiv),
  sonst Bearbeitung (Sortiermodus gesperrt, nur Anzeige).
- `PlaylistGenreEditorState { IsOpen, PlaylistId, Options (IReadOnlyList<PlaylistGenreOptionItem>), IsLoading, IsSaving, Error }`
- `PlaylistGenreOptionItem { Id, Name, IsSelected }` (record)
- `PlaylistMutationOutcome { Succeeded, RequiresConfirmation, Error }` —
  Ergebnis-Träger für 409-fähige Service-Methoden (kein Enum).

### `PlaylistService` — Erweiterung

Konstruktor zusätzlich `AppSecretsService` (beide Singletons registriert;
Testaufbau unverändert machbar). Neue Methoden:

- `CreatePlaylistAsync(name, description, sortMode)` → `PlaylistListItem`
- `UpdatePlaylistAsync(id, name, description)` → `PlaylistListItem`
- `DeletePlaylistAsync(id)`
- `AddMediaToPlaylistAsync(playlistId, mediaType, mediaId)` → `string? Message`
  (MediaType via `ToServerName`)
- `RemoveEntryAsync(playlistId, mediaType, mediaId, confirm)` →
  `PlaylistMutationOutcome` — 409 + `IsContinueWatchingConfirmationRequired`
  → `RequiresConfirmation`
- `MoveEntryToBeginningAsync(playlistId, entryId)`
- `MoveEntryToEndAsync(playlistId, entryId)` — `GetPlaylistMaxSortOrderAsync`
  + `ReorderPlaylistEntryAsync(max+1)` (Fallback max=0 bei null)
- `MoveEntryAsync(playlistId, entryId, targetSortOrder)` → `move-between`
- `ChangeSortModeAsync(playlistId, newSortMode, confirm)` →
  `(PlaylistListItem? Item, bool RequiresConfirmation)`
- `SetGenresAsync(playlistId, genreIds)` → `PlaylistListItem`
- `ResetGenresAsync(playlistId)` → `PlaylistListItem`
- `SetPublicAsync(playlistId, isPublic)` → `PlaylistListItem`
- `LoadGenreOptionsAsync()` → `IReadOnlyList<PlaylistGenreOptionItem>`
- `LoadOwnPlaylistsAsync()` → gefilterte `Own` aus `LoadPlaylistsAsync`
- `IsCurrentUserAdminAsync()` → `secrets.GetAuthTokenAsync` + `ReadIsAdminClaim`
- Fehlertexte als Konstanten in `AppUpdate` (deutsch, z. B.
  `PlaylistSaveError`, `PlaylistDeleteError`, `PlaylistEntryRemoveError`,
  `PlaylistEntryMoveError`, `PlaylistSortModeError`, `PlaylistGenresError`,
  `PlaylistPublicError`, `PlaylistTargetsLoadError`, `PlaylistNameRequiredError`).

### Neue `AppMessage`-Records (alle deutsch dokumentiert)

Formular:
- `PlaylistCreateRequestedMessage` — „Neue Playlist" (Übersicht)
- `PlaylistEditRequestedMessage` — „Bearbeiten" (Detailseite, befüllt aus State)
- `PlaylistFormNameChangedMessage(string)`
- `PlaylistFormDescriptionChangedMessage(string)`
- `PlaylistFormSortModeSelectedMessage(string)`
- `PlaylistFormCancelMessage`
- `PlaylistFormSaveRequestedMessage` — Validierung Name≠leer →
  `SavePlaylistEffect`
- `PlaylistFormCompletedMessage(PlaylistListItem? Saved, string? Error)` —
  schließt Formular; bei Erfolg `LoadPlaylistsEffect` (Übersicht) bzw.
  `LoadPlaylistDetailsEffect` (Detailseite-Kopf aktualisieren)

Löschen:
- `PlaylistDeleteConfirmedMessage(PlaylistId)` → `DeletePlaylistEffect`
- `PlaylistDeletedMessage(PlaylistId, string? Error)` — bei Erfolg:
  Detail-State leeren + `NavigateBackEffect` (neu, `Shell.GoToAsync("..")`)
  + `LoadPlaylistsEffect` für den Fall der Übersicht

Eintragsverwaltung:
- `PlaylistEntryRemoveRequestedMessage(PlaylistEntryItem)` →
  `RemovePlaylistEntryEffect(confirm:false)`
- `PlaylistEntryRemoveCompletedMessage(PlaylistEntryItem, bool Removed, bool RequiresConfirmation, string? Error)`
  — `RequiresConfirmation` → `PendingRemoveConflict` setzen
- `PlaylistEntryRemoveConfirmedMessage(bool Confirmed)` — Confirmed →
  `RemovePlaylistEntryEffect(confirm:true)`; Abbruch → Pending löschen
- `PlaylistEntryMoveRequestedMessage(PlaylistEntryItem, long? TargetSortOrder, PlaylistEntryMoveKind)` —
  statt Enum: drei getrennte Records, klarer:
  - `PlaylistEntryMoveToBeginningRequestedMessage(PlaylistEntryItem)` → `MovePlaylistEntryToBeginningEffect`
  - `PlaylistEntryMoveToEndRequestedMessage(PlaylistEntryItem)` → `MovePlaylistEntryToEndEffect`
  - `PlaylistEntryMoveBetweenRequestedMessage(PlaylistEntryItem, long TargetSortOrder)` → `MovePlaylistEntryBetweenEffect`
  (vor/zurück-Auswahl liegt in der Page, die den Nachbar-SortOrder mitgibt)
- `PlaylistEntryMoveCompletedMessage(PlaylistEntryItem, string? Error)` —
  Erfolg → `LoadPlaylistEntriesEffect(Seite 1, Append:false)` + `IsManaging` frei

Sortiermodus:
- `PlaylistSortModeRequestedMessage(string NewSortMode)` → `ChangePlaylistSortModeEffect(confirm:false)`
- `PlaylistSortModeCompletedMessage(string NewSortMode, PlaylistListItem? Updated, bool RequiresConfirmation, string? Error)` —
  `RequiresConfirmation` → `PendingSortModeConflict`
- `PlaylistSortModeConfirmedMessage(bool Confirmed)` — Confirmed → Effekt mit
  `confirm:true`; sonst Pending löschen

Genres:
- `PlaylistGenreEditorRequestedMessage` → `LoadGenreOptionsEffect` (Owner+
  verbunden) — öffnet Editor mit IsLoading
- `PlaylistGenreOptionsLoadedMessage(IReadOnlyList<PlaylistGenreOptionItem>, string? Error)`
- `PlaylistGenreToggledMessage(long GenreId)` — `IsSelected` flippen
- `PlaylistGenresSaveRequestedMessage` → `SetPlaylistGenresEffect`
- `PlaylistGenresResetRequestedMessage` → `ResetPlaylistGenresEffect`
- `PlaylistGenresCompletedMessage(PlaylistListItem? Updated, string? Error)` —
  Erfolg → Editor schließen + Kopfdaten aktualisieren
- `PlaylistGenreEditorCancelMessage`

Öffentlich-Flag:
- `PlaylistPublicToggledMessage(bool IsPublic)` → `SetPlaylistPublicEffect`
- `PlaylistPublicCompletedMessage(PlaylistListItem? Updated, string? Error)`

„Zu Playlist hinzufügen" (Mediendetails):
- `PlaylistAddTargetsRequestedMessage` → `PlaylistPickerPending=true` +
  `LoadPlaylistTargetsEffect` (nur einmal laden — vorhandene Ziele werden
  wiederverwendet)
- `PlaylistTargetsLoadedMessage(IReadOnlyList<PlaylistListItem>?, string? Error)`
- `PlaylistPickerDismissedMessage(long? PlaylistId)` — Pending löschen;
  PlaylistId>0 → `IsAddingToPlaylist=true` + `AddPlaylistEntryEffect`
- `PlaylistEntryAddedMessage(string PlaylistName, string? ResultMessage, string? Error)`
  → `AddToPlaylistNotice` setzen (Erfolg: Server-`Message` bzw. Standardtext;
  Fehler: `AddToPlaylistNoticeIsError=true`)

### Neue `AppEffect`-Records

- `SavePlaylistEffect(long? EditingPlaylistId, string Name, string? Description, string SortMode)`
- `DeletePlaylistEffect(long PlaylistId)`
- `RemovePlaylistEntryEffect(long PlaylistId, long EntryId, string MediaType, long MediaId, bool ConfirmContinueWatchingRemoval)`
- `MovePlaylistEntryToBeginningEffect(long PlaylistId, long EntryId)`
- `MovePlaylistEntryToEndEffect(long PlaylistId, long EntryId)`
- `MovePlaylistEntryBetweenEffect(long PlaylistId, long EntryId, long NewSortOrder)`
- `ChangePlaylistSortModeEffect(long PlaylistId, string NewSortMode, bool ConfirmLossOfManualOrder)`
- `LoadGenreOptionsEffect(long PlaylistId)`
- `SetPlaylistGenresEffect(long PlaylistId, IReadOnlyList<long> GenreIds)`
- `ResetPlaylistGenresEffect(long PlaylistId)`
- `SetPlaylistPublicEffect(long PlaylistId, bool IsPublic)`
- `LoadPlaylistTargetsEffect`
- `AddPlaylistEntryEffect(long PlaylistId, string MediaType, long MediaId)`
- `NavigateBackEffect` (Shell `..`; generisch, nach Löschen)

### `AppUpdate`-Zweige

- Owner-Wache: alle Verwaltungs-Anfragen prüfen
  `PlaylistDetails.CanManage` (bzw. `IsOwner` des Eintrags) bzw. beim
  Hinzufügen `ConnectionState == Connected` — andernfalls `Unchanged`.
- `PlaylistSelected`: zusätzlich `LoadPlaylistDetailsEffect` mitsenden,
  damit `IsAdmin`/`AllGenreIds`/`GenresManuallyOverridden` frisch ankommen.
- `PlaylistDetailsLoadedMessage` erweitert um `IsAdmin` (Handler liefert)
  → befüllt die neuen Header-Felder.
- Identitätswache überall: Ergebnis-Messages nur übernehmen, wenn
  `PlaylistId`/`MediaType+MediaId` zum offenen Zustand passt.
- `IsManaging`-Guard: während einer Mutation keine zweite annehmen.
- Erfolg nach Mutationen → `LoadPlaylistEntriesEffect(1, Append:false)`
  (+ `LoadPlaylistDetailsEffect` für Header-Felder) auslösen.
- Löschen erfolgreich → `PlaylistDetails = PlaylistDetailsState.Empty` +
  `NavigateBackEffect` + `LoadPlaylistsEffect`.
- `PlaylistFormSaveRequested` → leere Name → `Error=PlaylistNameRequiredError`
  ohne Effekt.

### `DevApiModelSnapshot`/`DevApiModel`

Neue Felder: `PlaylistDetailsIsOwner`, `PlaylistDetailsCanManage`,
`PlaylistDetailsCanReorder`, `PlaylistFormIsOpen`, `PlaylistGenreEditorIsOpen`,
`PlaylistDetailsPendingRemoveConflict` (bool), `PlaylistDetailsPendingSortModeConflict`,
`PlaylistDetailsManageNotice`, `MediaDetailsPlaylistPickerPending`,
`MediaDetailsAddToPlaylistNotice`, `PlaylistsPageCanCreate`.

## 3. VideoPlayer.Maui — Seiten und Controls

### `PlaylistDetailsPage`

- Header zusätzlich: „Verwalten"-Schalter (`AutomationId="ManagePlaylistButton"`,
  nur `CanManage`) → `DisplayActionSheet` mit:
  - „Bearbeiten" → `PlaylistEditRequestedMessage`
  - „Sortiermodus ändern" → `PlaylistSortModeRequestedMessage(other)`
    (Caption „„Manuell" → „Nach Erscheinungsdatum"" umgekehrt wie Server)
  - „Genres bearbeiten" → `PlaylistGenreEditorRequestedMessage`
  - „Genres zurücksetzen" (nur `GenresManuallyOverridden`)
  - „Öffentlich schalten" / „Privat schalten" (nur `CanSetPublic`)
  - „Playlist löschen" (zerstörerisch, rot) → `DisplayAlert`-Bestätigung →
    `PlaylistDeleteConfirmedMessage`
- Eintragskarte += „⋮"-Schalter (`AutomationId` via `AutomationProperties` —
  pro Karte `EntryMenuButton` nicht eindeutig; stattdessen `Clicked` mit
  `BindingContext`-Eintrag, DevApi-Invoke über Tree-Item nicht möglich →
  daher: **Karte per Tap öffnet weiterhin Wiedergabe/Details; zusätzlicher
  sichtbarer Icon-Schalter** `EntryActionsButton` mit `x:Name` —
  DevApi-AutomationId in DataTemplate muss eindeutig sein! → Lösung:
  **Auswahl-Kontextmenü über zweiten Tap?** Nein — einfachste testbare
  Lösung: Eintragsauswahl (SelectionChanged) öffnet bei `CanManage`
  **nicht** sofort Wiedergabe, sondern … das bricht Schritt 5.
  
  **Bessere Lösung**: Long-Press ist in CollectionView-Items nicht
  DevApi-fähig; ein `⋮`-Button pro Karte mit `AutomationId` pro Eintrag
  (`AutomationId` per Bindung: `AutomationId="{Binding EntryMenuAutomationId}"`
  → `EntryItem.AutomationId = $"EntryMenu_{EntryId}"`) — eindeutig und
  DevApi-invokable. `PlaylistEntryItem` += `EntryMenuAutomationId`.
- „⋮"-Klick → `DisplayActionSheet(entry.Title, "Abbrechen", …)`:
  - „Aus der Playlist entfernen" (immer)
  - „An den Anfang" (CanReorder && nicht erster)
  - „Ans Ende" (CanReorder && nicht letzter)
  - „Eins nach vorn" (CanReorder && nicht erster) → TargetSortOrder =
    SortOrder des Vorgängers
  - „Eins nach hinten" (CanReorder && nicht letzter) → SortOrder des Nachfolgers
  - Position aus `Entries.IndexOf(entry)`.
- `UpdateUi`: `PendingRemoveConflict` → einmalig `DisplayAlert`
  („Eintrag wird in der Weiterschauen-Liste verwendet …") →
  `PlaylistEntryRemoveConfirmedMessage(choice)`; `PendingSortModeConflict`
  → `DisplayAlert` (Verlust der manuellen Reihenfolge) →
  `PlaylistSortModeConfirmedMessage(choice)`; `ManageNotice`-Label
  (Erfolg/Fehler, Muster `DownloadNoticeLabel`).
- Overlay-Panels (Playlist-Formular + Genre-Editor) im Root-Grid einbinden.

### `PlaylistsPage`

- „Neue Playlist"-Schalter (`AutomationId="PlaylistCreateButton"`,
  sichtbar bei `!IsLoading && !HasError`) → `PlaylistCreateRequestedMessage`.
- Overlay-Panel Playlist-Formular einbinden.

### Neues Control `Controls/PlaylistFormPanel.xaml(+cs)`

Overlay-Karte (Border, zentriert, max. Breite):
- `NameEntry` (`AutomationId="PlaylistFormNameEntry"`), `DescriptionEntry`,
  Sortiermodus als zwei Toggle-Schalter `SortModeManualButton` /
  `SortModeReleaseDateButton` (nur aktiv bei `EditingPlaylistId==null`,
  sonst Anzeige-Label des aktuellen Modus)
- `SaveButton`, `CancelButton`, Fehler-Label, ActivityIndicator bei `IsSaving`
- Events `SaveRequested/CancelRequested/NameChanged/DescriptionChanged/
  SortModeSelected` → Page dispatchet Messages.
- Sichtbarkeit `IsVisible={Binding Model.PlaylistForm.IsOpen}`.

### Neues Control `Controls/PlaylistGenreEditorPanel.xaml(+cs)`

- Scrollbare Genre-Chipliste: je Option ein `CheckBox`+Label bzw. Toggle-
  Button (`AutomationId="GenreOption_{Id}"`) mit `IsSelected`-Styling
- `GenreEditorSaveButton`, `GenreEditorResetButton` (nur
  `GenresManuallyOverridden`), `GenreEditorCancelButton`, Fehler-Label,
  ActivityIndicator `IsLoading`/`IsSaving`
- Events analog.

### `MediaDetailsPage`

- Schalter „Zu Playlist hinzufügen" (`AutomationId="AddToPlaylistButton"`)
  im Aktions-Stack — sichtbar bei `Details != null && !IsOfflineSnapshot`
  (alle Medientypen: Serie/Staffel/Kollektion kaskadiert der Server).
- `UpdateUi`: `PlaylistPickerPending && !IsLoadingPlaylistTargets` →
  `DisplayActionSheet` mit Playlist-Namen (Klartext); leer → `DisplayAlert`
  „Keine eigene Playlist vorhanden."; Auswahl →
  `PlaylistPickerDismissedMessage(id)`; Abbrechen →
  `PlaylistPickerDismissedMessage(null)`; `AddToPlaylistNotice` als
  Hinweis-Label (Muster DownloadNoticeLabel).

## 4. MockVideoServer (E2E)

`HandlePlaylistRequest` erweitern:

- `POST /api/playlists` → Body lesen → neue `MockPlaylist` (Id = max+1,
  `IsOwner=true`) → `PlaylistJson`
- `PUT /api/playlists/{id}` → Name/Description übernehmen → `PlaylistJson`
- `DELETE /api/playlists/{id}` → entfernen → 204
- `POST /{id}/entries` → `{mediaType, mediaId}` lesen → neuen
  `MockPlaylistEntry` anhängen (Id=max+1, Titel aus `MediaTitle`-Tabelle
  oder generiert; Duplikat → `skippedCount`) → `PlaylistAddResult`
- `DELETE /{id}/entries/{mediaType}/{mediaId}` → Eintrag suchen; wenn
  `MockPlaylistEntry.ReferencesContinueWatching` (neue Flagge/Test-Opt-in)
  und `confirmContinueWatchingRemoval` fehlt → 409
  `{isContinueWatchingConfirmationRequired:true}`; mit Flagge → entfernen
  (und `RemovedContinueWatchingKeys` ergänzen, damit die Liste stimmt) → 204
- `POST /{id}/entries/{entryId}/move-to-beginning` → Eintrag an Index 0,
  SortOrders = Index neu vergeben
- `POST /{id}/entries/{entryId}/move-between` → `NewSortOrder` lesen →
  Eintrag an Position `NewSortOrder` einsortieren, Indizes neu vergeben
- `PUT /{id}/entries/{entryId}/order` → `NewSortOrder` → Eintrag an
  Position min(NewSortOrder, count-1)
- `GET /{id}/entries/max-sort-order` → `{maxSortOrder: count-1}`
- `PATCH /{id}/sort-mode` → `NewSortMode`; `Manual`→andere ohne
  `ConfirmLossOfManualOrder` → 409 `{isLossOfDataConfirmationRequired:true}`;
  mit Flagge → `SortMode` setzen → `PlaylistJson`
- `PUT /{id}/genres` → `GenreIds` → `Genres`=Id-Namen + `Overridden=true`
- `POST /{id}/genres/reset` → `Overridden=false`, Genres = berechnet
- `PUT /{id}/public` → `IsPublic` übernehmen (IsAdmin aus Auth-Token-
  Prüfung simulieren: `MockVideoServer.LoginIsAdmin`-Flagge, Standard true;
  ohne Admin → 403)
- `EntryJson` += `sortOrder = entry.SortOrder` (Mock-Feld `SortOrder`,
  Standard = Listindex beim Serialisieren)
- Request-Tracking der Admin-Aufrufe für Assertions
  (`WasPathRequested` reicht für Pfade; Body-Erfassung für Confirm-Query
  via `WasPathRequested("…confirmContinueWatchingRemoval=true")` auf
  `rawPath`-Basis — Query wird derzeit für Pfadchecks ignoriert → eigenes
  Zählfeld `LastPlaylistRemovePath`/`RequestPaths` erweitern, Query inkl.
- `api/items/genres` — existiert? → prüfen; sonst GenreOptions-Liste
  aus `GenreOptions`-Sammlung (neu) liefern.

`PlaylistJson`: `genresManuallyOverridden` aus Mock-Feld; `allGenreIds`
für manuell gesetzte Genres verwenden.

## 5. Tests

### Unit (`VideoWebPlayer.Maui.Tests`)

- `VideoPlayerApiClientTests_Playlists` += ~13 Tests: Routen, Methoden,
  Query-Parameter (`confirmContinueWatchingRemoval`), Bodies, 404/409-
  Verhalten, Guards.
- `PlaylistServiceTests` += ~15 Tests: Mapping, 409→RequiresConfirmation
  (ResponseBody-Parsing!), move-to-end (max+1), Admin-Claim-Lesung
  (JWT mit/ohne IsAdmin), Fehlerpfade ohne Client.
- `AppUpdateTests_Playlists` (neue Datei `AppUpdateTests_PlaylistManagement.cs`)
  += ~30 Tests: Formular öffnen/validieren/speichern/abschließen; Löschen;
  Entfernen inkl. Pending→Confirm→Retry-Pfad; Move-To-Beginning/End/Between
  (Ziel-SortOrder-Ermittlung); Sortiermodus inkl. Pending-Confirm; Genres
  (Laden/Togglen/Speichern/Reset); Öffentlich-Toggle; Owner-/Admin-Wachen
  (fremde Playlist → Unchanged); Add-To-Playlist-Fluss auf MediaDetails
  (Targets laden → Picker → Auswahl → Notice); Identitätswachen.
- `EnumCoverageTests`: keine neuen Enums → nichts nötig.

### E2E (`VideoPlayer.E2E.Tests`, neue `PlaylistManagementTests.cs`)

Pflichtszenarien (jeder echte UI-Fluss):
1. Playlist erstellen über „Neue Playlist"-Formular → erscheint in Übersicht
   (`POST /api/playlists` bedient).
2. Playlist umbenennen auf Detailseite → PUT bedient, Titel aktualisiert.
3. Playlist löschen mit Bestätigung → DELETE bedient, zurück zur Übersicht.
4. Medium von Detailseite zu Playlist hinzufügen → POST entries bedient,
   Hinweistext sichtbar.
5. Eintrag entfernen (ohne Konflikt) → DELETE bedient, Liste neu geladen.
6. Eintrag mit Weiterschauen-Bezug entfernen → 409 → Bestätigungsdialog →
   Retry mit `confirmContinueWatchingRemoval=true`.
7. Eintrag „An den Anfang" + „Eins nach hinten" verschieben → move-to-
   beginning/move-between bedient, Reihenfolge aktualisiert.
8. Sortiermodus wechseln inkl. 409-Bestätigung → PATCH mit Confirm-Flag.
9. Genres setzen + zurücksetzen → PUT/POST bedient.
10. Öffentlich schalten (Admin-JWT) → PUT public bedient.
11. Fremde öffentliche Playlist: keine Verwaltungs-Schalter,
    keine ⋮-Schalter an Einträgen.

Mock-JWT mit `IsAdmin:True` Claim — `MockVideoServer.AuthToken`-Erzeugung
prüfen (JWT wird dort vermutlich einfach gebaut; Claim einbetten).

## 6. Doku

- `docs/help/playlists.md` um Abschnitt „Verwaltung" erweitern
  (endanwendergerecht, ohne technische Kennungen).
- `docs/RELEASE_NOTES.md` neue Ausgabe bilingual (EN/DE).
- `changes.log` Eintrag.
- README: Playlist-Verwaltung in der Feature-Liste ergänzen.

## Umsetzungsreihenfolge

1. ApiClient-DTOs + Methoden (+ Unit-Tests dazu)
2. `MediaTypeNames.ToServerName`, `ProfileInfoService.ReadIsAdminClaim`
3. Mvu-Records/State-Erweiterungen, `PlaylistService`-Erweiterungen
4. Messages/Effekte/`AppUpdate`-Zweige (+ `AppUpdateTests`)
5. `DevApiModelSnapshot`-Felder
6. `PlaylistDetailsPage`/`PlaylistsPage`/`MediaDetailsPage` + Controls
7. `MockVideoServer`-Erweiterung
8. E2E-Tests
9. Reviews (Plan/Usability/Code) → ggf. Iterationen (max. 3)
10. Doku + Finaler Testlauf + Commit
