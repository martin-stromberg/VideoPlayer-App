# Bestandsaufnahme: Favoriten- und Profil-Seite (Entwicklungsschritt 9)

Analysiert wurde der MVU-Bestand der neuen App (`VideoPlayer.Mvu` +
`VideoPlayer.Maui`), die lokale Favoriten-Kopie in `VideoPlayer.Storage`
sowie der Server-API-Vertrag — bezogen auf die Anforderung
„Favoriten- und Profil-Seite" (`requirement.md`).

## Zusammenfassung

- `FavoritesPage` und `ProfilePage` existieren als Platzhalter
  (Schritt 2) und sind als Flyout-Einträge (`favorites`, `profile`) in
  `AppShell.xaml` verdrahtet — jetzt mit echtem Inhalt zu füllen.
- Favoriten-Infrastruktur ist vollständig vorhanden:
  `FavoritesService.ToggleAsync` (Server-Toggle + `SyncFavoritesAsync`),
  `ServerSyncService.SyncFavoritesAsync` (ersetzt `cached_favorites`
  autoritativ), `cached_favorites`-Tabelle (`CachedFavoritesEntry` mit
  serialisiertem `FavoriteEntry`-JSON inkl. Eintrags-Id),
  `VideoPlayerApiClient.GetFavoritesAsync`/`RemoveFavoriteAsync` und
  das SignalR-Ereignis `FavoritesChanged` → `SyncFavoritesEffect`.
- Die Startseiten-Favoriten (`AppModel.Favorites` als
  `IReadOnlyList<HomeMediaItem>`) enthalten **keine** Favoriten-Eintrags-Id
  — für `api/favorites/remove` (Payload `{ id, userId }`, `id` =
  Favoriten-Eintrags-Id laut Server-`FavoritesService.RemoveFavoriteAsync`)
  muss der `FavoriteEntry`-JSON aus `cached_favorites` gelesen werden.
- Profil/Benutzerinfo: Die Server-API besitzt **keinen**
  Benutzerinfo-Endpunkt und **keinen** Logout-Endpunkt
  (`api/auth` kennt nur `login` und das admininterne `impersonate`).
  Das JWT aus `api/auth/login` trägt die Claims `sub`/`nameid` (User-Id),
  `email`, `unique_name` (ClaimTypes.Name → UserName) und `IsAdmin`
  (serverseitige `AuthorizationTokenService.CreateToken`). Damit sind
  Benutzername und E-Mail aus dem gespeicherten Token ermittelbar;
  Fallback ist der gespeicherte Anmelde-Benutzername
  (`AppSecretsService.GetUsername` — die Anmeldung nutzt die E-Mail als
  Benutzerkennung).
- Abmeldung: `AppSecretsService.ClearCredentialsAsync` (Benutzername,
  Passwort, Token) existiert; `AuthSessionService` besitzt noch keine
  Sitzungsende-Methode (`Client`/`ClientBaseAddress` sind
  `private set`). Das Muster für „zurück in den Anmelde-Workflow" liegt
  in `UpdateSessionExpired` vor (`NeedsLogin` + `NavigateToPageEffect(Login)`
  + `DisconnectServerEventsEffect`).
- Lokale Profile: `AppProfile` (Schritt 3) mit `Name` — in DI als
  Singleton registriert.
- UI-Muster vorhanden: `CachedPosterImage`/`PosterImageService`
  (Poster aus `api/pictures/{id}` mit lokaler Ablage), Offline-Banner
  (`#D9A029`-Border), `CollectionView`-Listen mit
  `#23232a`-Karten, `MediaTypeNames.Normalize`, Typ-Bezeichnung
  (`MediaDetailsView.MediaTypeText`-Mapping), Detail-Navigation über
  `MediaItemSelectedMessage` → `NavigateToMediaDetailsEffect`.
- Altbestand-Referenz (Webanwendung): `FavoritesList.razor` zeigt
  Poster + Titel pro Favorit mit Kontextaktion „Entfernen"
  (`RemoveFavoriteAsync(favorite.Id)`); Staffeln zeigen
  „{Show.Name} - {Season.Name}"; Links führen zu Film/Collection/
  Serie mit Season-/Episode-Vorwahl — in der App abgebildet über die
  `mediadetails`-Route mit Serienkontext-Hints.

Test-Ausgangszustand: 431 Tests, alle erfolgreich (0 Fehler,
0 übersprungen) — Nachweis in [inventory/tests.md](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Tests](inventory/tests.md)
