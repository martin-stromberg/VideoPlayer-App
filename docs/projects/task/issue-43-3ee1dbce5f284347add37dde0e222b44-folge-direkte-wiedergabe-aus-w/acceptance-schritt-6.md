# Abnahmeprüfung – Entwicklungsschritt 6

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

- [ ] Keine.

## Hinweise

Geprüft: `git diff ...folge-direkte-wiedergabe-aus-w...HEAD` (Commit `ad61902`), Quelldateien sowie stichprobenartiger Server-Abgleich gegen `martin-stromberg/VideoPlayer` (`PlaylistsController.cs`, DTO-Modelle, `ApplicationUserClaimsPrincipalFactory`). Keine gelöschten `docs/features/`-Artefakte im Diff (keine `D`-Einträge).

**Akzeptanzkriterium 1 (erstellen/bearbeiten/löschen):** Client-Methoden `CreatePlaylistAsync`/`UpdatePlaylistAsync`/`DeletePlaylistAsync` (`VideoPlayerApiClient.cs`, ~Z. 731-775) decken `POST/PUT/DELETE api/playlists` ab — Routen serverseitig bestätigt. UI: „Neue Playlist"-Schalter auf `PlaylistsPage.xaml` öffnet `PlaylistFormPanel` (Name/Beschreibung/Sortiermodus); „Verwalten" → „Bearbeiten" nutzt dasselbe Panel (`EditingPlaylistId` unterscheidet Anlage/Bearbeitung, `SavePlaylistEffect`, `ShellAppEffectHandler.cs` Z. 1056 ff.); „Playlist löschen" mit Bestätigungsdialog (`PlaylistDetailsPage.xaml.cs` `ConfirmDeleteAsync`).

**Akzeptanzkriterium 2 (Einträge hinzufügen/entfernen, 409-Dialog):** `AddPlaylistEntryAsync` (`POST …/entries`) + „Zu Playlist"-Schalter auf `MediaDetailsPage.xaml` mit Ziel-Action-Sheet über `LoadOwnPlaylistsAsync` (nur eigene Playlists). Entfernen via ⋮-Menü → `DELETE …/entries/{mediaType}/{mediaId}` mit Query-Flagge `confirmContinueWatchingRemoval`. Der 409-Dialog ist vollständig implementiert (nicht nur erkannt): `ReadConflictFlag` liest `IsContinueWatchingConfirmationRequired` aus dem Response-Body (`PlaylistService.cs` Z. ~680-755), `PendingRemoveConflict` triggert `DisplayAlertAsync` („Aus Weiterschauen entfernen?"), `PlaylistEntryRemoveConfirmedMessage` wiederholt den Aufruf mit `ConfirmContinueWatchingRemoval: true` (`AppUpdate.cs` Z. 2898 ff.).

**Akzeptanzkriterium 3 (Reihenfolge/Sortiermodus):** `move-to-beginning`, `move-between` („Eins nach vorn/hinten" über Nachbar-`SortOrder`) sowie „Ans Ende" via `GET …/max-sort-order` + `PUT …/order` (max+1) — analog Server-UI (Server hat keinen move-to-End-Endpunkt). Sortiermodus-Wechsel `PATCH …/sort-mode` mit eigener 409-Behandlung (`IsLossOfDataConfirmationRequired` → Dialog „Sortiermodus ändern?" → Retry `ConfirmLossOfManualOrder: true`, `AppUpdate.cs` Z. 3084 ff.). `batch-reorder` existiert serverseitig, ist aber nicht erforderlich („mind. …").

**Akzeptanzkriterium 4 (Genres/Öffentlich):** `SetPlaylistGenresAsync` (`PUT …/genres`) + `ResetPlaylistGenresAsync` (`POST …/genres/reset`) über `PlaylistGenreEditorPanel` (Optionen via `GET api/items/genres`, serverseitig bestätigt). `SetPlaylistPublicAsync` (`PUT …/public`) hinter `CanSetPublic = IsOwner && IsAdmin`; der `IsAdmin`-Claim wird aus dem JWT gelesen (`ProfileInfoService.ReadIsAdminClaim`, Server schreibt `Claim("IsAdmin", "True")` — Schreibweise stimmt).

**Akzeptanzkriterium 5 (fremde Playlists):** `CanManage = IsActive && IsOwner`, `IsOwner` kommt aus dem Server-DTO (`DtoPlaylist.IsOwner`, führend). „Verwalten"-Schalter und ⋮-Eintragsmenüs sind per `IsVisible`-Bindung an `CanManage` ausgeblendet (`PlaylistDetailsPage.xaml`); E2E `ForeignPublicPlaylist_ShowsNoManageActions` belegt die Unsichtbarkeit. Server erzwingt zusätzlich 403.

**Server-Konsistenz:** Alle verwendeten Routen existieren im `PlaylistsController` (POST/PUT/DELETE `api/playlists`, `…/entries`, `…/entries/{type}/{id}` mit `confirmContinueWatchingRemoval`, `…/order`, `…/max-sort-order`, `…/move-to-beginning`, `…/move-between`, `…/sort-mode`, `…/genres`, `…/genres/reset`, `…/public`). DTO-Feldnamen stimmen (u. a. `IsContinueWatchingConfirmationRequired`, `IsLossOfDataConfirmationRequired`, `ConfirmLossOfManualOrder`, `NewSortOrder`, `GenreIds`, `IsPublic`).

**Scope:** Keine Cover-Verwaltungs-Endpunkte/-UI implementiert (kein `cover/upload|regenerate|preview|DELETE cover` im Client) — verbindlicher Ausschluss eingehalten; Cover-Anzeige aus Schritt 4 (`CachedPlaylistCoverImage`) unverändert.

**Einschränkungen/Kleinigkeiten (keine Kriterien-Abweichungen):**
- `PlaylistAddResult.AddedCount`/`SkippedCount` korrespondieren nicht zu den Server-Feldern `AddedEntries`/`SkippedDuplicateCount` — gegen den echten Server blieben beide Zähler 0. Funktional unerheblich, da nur `Message` (serverseitiger Hinweistext) verwendet wird.
- `UpdatePlaylistRequest` sendet kein `SortMode`; der Server ignoriert das Feld im Update-Endpunkt ohnehin (`PlaylistService.UpdatePlaylistAsync` wertet `sortMode` nicht aus).
- `IsAdmin`-Claim-Lesung ist nur Sichtbarkeitssteuerung; autoritativ ist die Server-Prüfung (403).

**Testnachweise:** 11 E2E-Szenarien in `PlaylistManagementTests.cs` decken alle Kriterien ab (Create/Edit/Delete, Add, Remove, 409 Confirm+Cancel, Reorder, SortMode-409, Genres Set/Reset, Public-Admin, fremde Playlist read-only). Gefilterte Unit-Tests ausgeführt: 72/72 bestanden (`AppUpdateTests_PlaylistManagement`, `PlaylistServiceTests_Management`, `VideoPlayerApiClientTests_PlaylistAdmin`). Behauptete Gesamtzahlen (1278 Unit/50 E2E) plausibel: 1084 Testmethoden inkl. Theorien mit InlineData, E2E exakt 50 `[Fact]`s; Volllauf nicht wiederholt. Regression: Schritt-4/5-Verhalten unberührt — bestehende Handler erhalten, `AppUpdateTests_Playlists` lediglich um den zusätzlichen `LoadPlaylistDetailsEffect` (Kopf-Nachschlag für Verwaltungs-Schalter) angepasst.
