# Code-Review

Geprüfte Änderungen: alle auf diesem Zweig geänderten Quell- und
Testdateien des Schritts 9 (siehe `git diff` gegen den Planungscommit
`bc95955`).

## Ergebnis

**Status:** Ein Befund behoben; keine offenen Befunde

## Findings

### Befund 1 (behoben): Offline-Zustand mit gesetztem Client

- **Beschreibung:** `LoadFavoritesPageAsync` rief den Server-Sync
  anhand `AuthSessionService.Client != null` auf. Im Offline-Zustand
  bleibt der authentifizierte Client jedoch gesetzt — die Seite hätte
  trotz `ConnectionState.Offline` vergeblich auf den Server gewartet.
- **Korrektur:** `LoadFavoritesPageEffect` trägt den Parameter
  `RefreshFromServer`; `AppUpdate` setzt ihn aus dem Zustand
  (`Connected` → `true`, `Offline` → `false`), der Effekt-Handler
  synchronisiert nur noch bei `true`. Abgedeckt durch
  `FavoritesPageOpened_Offline_UsesCacheOnly` (prüft
  `RefreshFromServer == false`).

## Geprüfte Aspekte

### MVU-Reinheit

- Alle neuen Übergänge in `AppUpdate` sind reine Zustandsfunktionen;
  Seiteneffekte laufen ausschließlich über `AppEffect`-Records —
  `LoadFavoritesPageEffect` (`RefreshFromServer`),
  `RemoveFavoriteEffect` (`FavoriteId`), `LoadProfileInfoEffect`,
  `LogoutEffect`. Die Seiten dispatchten nur Messages, kein
  Geschäftslogik im Code-Behind.

### Guards und Veraltungs-Schutz

- `UpdateFavoritesPageOpened`: ignoriert unbekannte Verbindungszustände
  (nur `Connected`/`Offline` starten den Lauf) und laufende Läufe.
- `UpdateFavoritesPageLoaded`: verwirft Antworten, die keiner
  ausstehenden Ladung entsprechen (`Attempted`-Prüfung analog zu den
  übrigen Listen-Ladern).
- `UpdateFavoriteRemoveRequested`: sperrt Offline, unbekannte
  `FavoriteId` und bereits laufende Entfernungen desselben Eintrags.
- `UpdateFavoriteRemoveCompleted`: Entfernung nur bei Erfolg;
  Fehlschlag löscht nur `IsRemoving`, die Liste bleibt intakt und
  `Error` zeigt den Fehlversuch.
- `UpdateLogoutRequested`: Guard auf `ConnectionState.Connected`;
  die Rücksetzung leert Sitzungsdaten (Favoriten, Listen, Quellen,
  Detail-/Browser-Zustand, `LoginUsername`, Favoriten-/Profil-Seiten)
  und behält Gerätedaten (Serveradresse, letzte Quellenwahl,
  Netzverfügbarkeit).

### Server vs. Cache — Zuständigkeit

- Online ist der Server führend: `LoadFavoritesPageAsync` erneuert
  `cached_favorites` zuerst über `ServerSyncService.SyncFavoritesAsync`
  und liest danach die Kopie; `FavoritesService.RemoveFavoriteAsync`
  ruft `api/favorites/remove` mit der **Favoriten-Eintrags-ID**
  (nicht der Medien-ID) und erneuert die Kopie autoritativ.
- Offline wird ausschließlich der Cache gelesen — kein Serveraufruf
  (siehe Befund 1); Entfernen ist bereits im Update blockiert
  (doppelte Absicherung zur deaktivierten Schaltfläche).

### Effekt-Handler

- `RemoveFavoriteEffect` dispatcht bei Erfolg
  `CachedListsChangedMessage` — die Startseiten-Favoritenliste folgt
  derselben Aktualisierungs-Strecke wie alle Cache-Änderungen.
- `LogoutEffect` → `AuthSessionService.LogoutAsync` löscht Client,
  Benutzername, Passwort und Token; die Abmeldung braucht keinen
  Server-Endpunkt. `DisconnectServerEventsEffect` trennt die
  Ereignisverbindung vor dem Effekt.

### Fehlerbehandlung

- `LoadFavoritesPageAsync`/`RemoveFavoriteAsync` fangen Ausnahmen
  und dispatchten fehlertragende Messages statt Ausnahmen aus dem
  Handler zu werfen; `LoadProfileInfoAsync` liefert bei Fehlschlag
  ein `ProfileInfo` aus den gespeicherten Daten (Fallback statt
  Fehlerseite).

### DI und Infrastruktur

- `ProfileInfoService` in `MauiProgram` registriert; bestehende
  Registrierungen (`AuthSessionService`, `FavoritesService`,
  `HomeListService`, `ServerSyncService`) unverändert verwendet.
  `ProfilePage`/`FavoritesPage` waren bereits registriert.

### Tests

- 44 neue Tests: MVU-Übergänge inkl. Guards/Offline-Flags,
  `ProfileInfoService` (Token-Claims, Fallbacks), `FavoritesService`
  (Remove-Payload, Cache-Erneuerung), `AuthSessionService` (Logout),
  `HomeListService` (Favoriten-IDs, Serienkontext, Poster-Mapping)
  und XAML-/Code-Behind-Verträge — Gesamtstand 469 Tests, alle grün.
