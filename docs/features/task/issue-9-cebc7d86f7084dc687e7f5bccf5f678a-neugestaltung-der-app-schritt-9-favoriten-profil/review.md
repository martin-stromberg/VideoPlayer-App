# Plan-Review

Geprüft: `plan.md` gegen die Implementierung (Selbstprüfung ohne
Unteragenten — dokumentierte Abweichung, es stehen keine Unteragenten
zur Verfügung). Alle Planelemente wurden im Code gesucht und
vollständig gelesen.

## Ergebnis

**Status:** Vollständig umgesetzt (mit einer verfeinerten Abweichung)

## Umgesetzte Planelemente

### Projekt `VideoPlayer.Mvu` (net10.0)

- [x] `FavoriteListItem` — `FavoriteId`, `Id`, `MediaType`, `Title`,
  `PosterPictureId`, `ShowId`/`SeasonId`/`EpisodeId`, `IsRemoving`,
  abgeleitet `HasPoster`, `CanRemove`, `MediaTypeText` (Film/
  Filmkollektion/Serie/Staffel/Episode/Medium) sowie `ToMediaItem()`
  für die Detail-Navigation.
- [x] `FavoritesPageState` — `Initial`, `IsLoading`, `Attempted`,
  `Error`, `Items`, `HasItems`/`HasError`/`ShowEmpty`.
- [x] `ProfileInfo` — `Username`, `Email`, `ProfileName`.
- [x] `ProfilePageState` — `Initial`, `IsLoading`, `Attempted`,
  `Username`, `Email`, `ProfileName` sowie Anzeige-Texte
  `UsernameText`/`EmailText` („—"-Fallback) und `ProfileNameText`
  (Fallback `default`).
- [x] `ProfileInfoService` — `LoadAsync` liest gespeicherte
  Anmelde-Kennung + Token (`AppSecretsService`), dekodiert die
  JWT-Payload (`unique_name`/`name`/`preferred_username` und
  `email`, jeweils inkl. URI-Claim-Formen), Fallback auf die
  gespeicherte Kennung, ergänzt `AppProfile.Name`.
- [x] Messages in `AppMessage.cs`: `FavoritesPageOpenedMessage`,
  `FavoritesPageLoadedMessage`, `FavoriteRemoveRequestedMessage`,
  `FavoriteRemoveCompletedMessage`, `ProfilePageOpenedMessage`,
  `ProfileInfoLoadedMessage`, `LogoutRequestedMessage`.
- [x] Effekte in `AppEffect.cs`: `LoadFavoritesPageEffect`,
  `RemoveFavoriteEffect`, `LoadProfileInfoEffect`, `LogoutEffect`.
- [x] `AppModel`: `FavoritesPage` + `Profile`.
- [x] `AppUpdate`: alle neuen Übergänge — Laden mit
  Connected/Offline-Guard und Dedup, Entfernen mit Guards (offline,
  unbekannter Eintrag, laufender Lauf) und Fehlschlag-Rücknahme,
  Profil-Laden, Abmeldung mit Zustandsrücksetzung (Sitzungsdaten
  geleert, Gerätedaten behalten) und Effektfolge
  `DisconnectServerEventsEffect` → `LogoutEffect` →
  `NavigateToPageEffect(Login)`.
- [x] `HomeListService.LoadFavoriteItemsAsync` — `cached_favorites`
  → `FavoriteListItem` inkl. `FavoriteId` und Serienkontext-Hints.
- [x] `FavoritesService.RemoveFavoriteAsync` — `api/favorites/remove`
  + autoritative Erneuerung der Kopie via `SyncFavoritesAsync`;
  `false` ohne Client.
- [x] `AuthSessionService.LogoutAsync` — Client/`ClientBaseAddress`
  freigegeben, `ClearCredentialsAsync` (Benutzername, Passwort, Token).

### Projekt `VideoPlayer.Maui`

- [x] `ShellAppEffectHandler`: Cases für alle vier neuen Effekte;
  `RemoveFavoriteEffect` dispatcht bei Erfolg
  `CachedListsChangedMessage` (Startseiten-Favoriten folgen);
  `LoadFavoritesPageEffect` erneuert online zuerst die Kopie.
- [x] `FavoritesPage`: `MvuHost`-Verdrahtung, `CollectionView` mit
  Poster (`CachedPosterImage`), Titel, `MediaTypeText` und
  „Entfernen"-Button je Eintrag (offline per DataTrigger gesperrt,
  während des Laufs über `CanRemove`), Offline-Banner, Lade-/Fehler-/
  Leerzustand, Auswahl → `MediaItemSelectedMessage`.
- [x] `ProfilePage`: Benutzername/E-Mail, lokales Profil und
  Serveradresse, „Abmelden" → `LogoutRequestedMessage`.
- [x] `MauiProgram`: `ProfileInfoService` registriert.

## Abweichungen vom Plan

- `LoadFavoritesPageEffect` trägt den Parameter `RefreshFromServer`
  (Plan aktualisiert): die Entscheidung „erst Server-Sync, dann
  Cache" liegt im Zustand (`ConnectionState.Connected`), nicht am
  Vorhandensein des Clients — im Offline-Zustand ist der Client noch
  gesetzt und ein Sync-Versuch würde vergeblich auf den Server
  warten.
- Plan-Abweichung ist dokumentiert und von den Tests abgedeckt
  (`RefreshFromServer` true/false je Zustand).

## Verbleibende Punkte

Keine — alle Planelemente umgesetzt, keine offenen Punkte im Plan.
