# Tasks: Favoriten- und Profil-Seite

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | Anzeige-Records in `VideoPlayer.Mvu` anlegen: `FavoriteListItem`, `FavoritesPageState`, `ProfilePageState`, `ProfileInfo`; `AppModel` um `FavoritesPage`/`Profile` erweitern | Offen | `AppUpdateTests_FavoritesProfile` |
| 2 | MVU | Messages in `AppMessage.cs` ergänzen (`FavoritesPageOpened`, `FavoritesPageLoaded`, `FavoriteRemoveRequested`, `FavoriteRemoveCompleted`, `ProfilePageOpened`, `ProfileInfoLoaded`, `LogoutRequested`) | Offen | `AppUpdateTests_*` |
| 3 | MVU | Effekte in `AppEffect.cs` ergänzen (`LoadFavoritesPageEffect`, `RemoveFavoriteEffect`, `LoadProfileInfoEffect`, `LogoutEffect`) | Offen | `AppUpdateTests_*` |
| 4 | MVU | `AppUpdate`: neue Übergänge (Laden, Entfernen inkl. Offline-Guard, Profil, Abmelden mit Zustandsrücksetzung) | Offen | `AppUpdateTests_FavoritesProfile` |
| 5 | Logik | `HomeListService.LoadFavoriteItemsAsync` (`cached_favorites` → `FavoriteListItem` mit `FavoriteId` und Serienkontext) | Offen | `HomeListServiceTests` |
| 6 | Logik | `FavoritesService.RemoveFavoriteAsync` (`api/favorites/remove` + `SyncFavoritesAsync`) | Offen | `FavoritesServiceTests` |
| 7 | Logik | `AuthSessionService.LogoutAsync` (Client freigeben + `ClearCredentialsAsync`) | Offen | `AuthSessionServiceTests` |
| 8 | Logik | `ProfileInfoService` (`LoadAsync`: JWT-Claims `unique_name`/`email` dekodieren, Fallback gespeicherter Benutzername, `AppProfile.Name`) | Offen | `ProfileInfoServiceTests` |
| 9 | App-Rand | `ShellAppEffectHandler`: Cases für die vier neuen Effekte + `CachedListsChangedMessage` nach erfolgreichem Entfernen | Offen | `FavoritesProfileContractTests` |
| 10 | UI | `FavoritesPage` ausbauen (Poster/Titel/Typ-Liste, Entfernen-Button, Offline-Banner, Lade-/Fehler-/Leerzustand, Detail-Navigation) | Offen | `FavoritesProfileContractTests` |
| 11 | UI | `ProfilePage` ausbauen (Benutzername/E-Mail/lokales Profil/Serveradresse, „Abmelden") | Offen | `FavoritesProfileContractTests` |
| 12 | Verdrahtung | `MauiProgram`: `ProfileInfoService` registrieren | Offen | `FavoritesProfileContractTests` |
| 13 | Tests | `AppUpdateTests_FavoritesProfile` (MVU-Sequenzen inkl. Offline-Guard + Abmelden) | Offen | Testlauf |
| 14 | Tests | Service-Tests: `FavoritesServiceTests.RemoveFavorite_*`, `AuthSessionServiceTests.LogoutAsync_*`, `ProfileInfoServiceTests`, `HomeListServiceTests.LoadFavoriteItems_*` | Offen | Testlauf |
| 15 | Tests | `FavoritesProfileContractTests` (XAML-/DI-/Handler-Verdrahtung) | Offen | Testlauf |
| 16 | Doku | `docs/help/` (Favoriten + Profil), `README.md`, `docs/RELEASE_NOTES.md`, `changes.log` | Offen | — |
