# Umsetzungsplan: Favoriten- und Profil-Seite (Entwicklungsschritt 9)

## Übersicht

Die Platzhalterseiten `FavoritesPage` und `ProfilePage` werden zu
echten Menüseiten ausgebaut. Die Favoriten-Seite zeigt die serverseitig
geführten Favoriten (lokale Kopie `cached_favorites` als Cache-First/
Offline-Basis) als Liste mit Poster, Titel und Typ und erlaubt das
Entfernen direkt aus der Liste (online über `api/favorites/remove`).
Die Profil-Seite zeigt Benutzername/E-Mail (aus den Claims des
gespeicherten JWT — die API besitzt keinen Benutzerinfo-Endpunkt) und
das aktive lokale Profil; „Abmelden" beendet die Sitzung lokal
(Token/Zugangsdaten löschen, Ereignisverbindung trennen, Zustand
zurücksetzen) und führt in den Anmelde-Workflow.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Favoriten-Liste im Model | Eigener `FavoritesPageState` mit `FavoriteListItem`-Einträgen (inkl. `FavoriteId`) statt Wiederverwendung von `AppModel.Favorites` (`HomeMediaItem`) | `api/favorites/remove` braucht die Favoriten-Eintrags-Id, die `HomeMediaItem` nicht trägt; die Seite braucht zudem `IsRemoving`/`Error` pro Kontext — Muster `SourcesOverviewState`/`MediaDetailsState` |
| Laden der Favoriten | Ein Effekt `LoadFavoritesPageEffect`: online erst `SyncFavoritesAsync` (Server führend), dann `cached_favorites` lesen; offline nur Cache lesen | Cache-First-Konzept der App; die lokale Kopie bleibt bei Sync-Fehlschlag anzeigbar; kein zweiter Lauf nötig |
| Lesen der Favoriten-Kopie | `HomeListService.LoadFavoriteItemsAsync()` liefert `FavoriteListItem` (nutzt den vorhandenen `FavoriteEntry`-JSON aus `cached_favorites`) | Derselbe Lesepfad wie die Startseiten-Favoriten, aber mit `FavoriteId` und Typ-Anzeige |
| Entfernen | Dedizierter Endpunkt `api/favorites/remove` über `VideoPlayerApiClient.RemoveFavoriteAsync(favoriteId)` + anschließendes `SyncFavoritesAsync` | Entspricht der Webanwendung (`FavoritesList.razor` → `RemoveFavoriteAsync(favorite.Id)`); Server ist führend, Cache wird autoritativ erneuert |
| Entfernen-Aktion in der Liste | „Entfernen"-`Button` je Eintrag (deaktiviert im Offline-Zustand und während des Laufs) | Web-Referenz nutzt eine Kontextaktion „Entfernen"; ein sichtbarer Button je Zeile ist das MAUI-Äquivalent ohne Kontextmenü-Infrastruktur |
| Benutzerinfo | JWT-Payload des gespeicherten Tokens dekodieren (`unique_name`/`name` → Benutzername, `email` → E-Mail); Fallback: gespeicherter Anmelde-Benutzername | Die API hat keinen Benutzerinfo-Endpunkt (geprüft: `api/auth` kennt nur `login`/`impersonate`); Token-Claims sind die nächstliegende, dokumentierte Quelle — kein Frontend-Workaround gegen die API |
| Abmeldung | Rein lokal: `LogoutEffect` löscht Zugangsdaten/Token (`AppSecretsService.ClearCredentialsAsync`) und gibt den Client frei (`AuthSessionService.LogoutAsync`); dazu `DisconnectServerEventsEffect` + `NavigateToPageEffect(Login)` | Die API besitzt keinen Logout-Endpunkt (JWT zustandslos); die Sitzung endet durch Verwerfen der lokalen Geheimnisse — dokumentiert |
| Zustandsrücksetzung beim Abmelden | Sitzungsbezogener Zustand wird geleert (Listen, Seiten-Zustände, `LoginUsername`), gerätebezogener bleibt (Einstellungen, bekannte Server, aktive Adresse, `HasOfflineData`) | Eine Abmeldung wechselt ggf. den Benutzer — serverbezogene Anzeigedaten dürfen nicht stehen bleiben; Geräte-/Server-Konfiguration bleibt erhalten. Lokale Caches auf der Platte bleiben bestehen (nicht backup-pflichtig, werden beim nächsten Sync autoritativ ersetzt) |

## Programmabläufe

### Favoriten-Seite öffnen und laden

1. `FavoritesPage.OnAppearing` dispatcht `FavoritesPageOpenedMessage`.
2. `AppUpdate`: ist `FavoritesPage.IsLoading`, keine Aktion; sonst
   `FavoritesPage` → `IsLoading = true`, `Attempted = true`,
   `Error = null` und Effekt `LoadFavoritesPageEffect`.
3. `ShellAppEffectHandler`: online (angemeldeter Client vorhanden)
   zuerst `ServerSyncService.SyncFavoritesAsync`; danach
   `HomeListService.LoadFavoriteItemsAsync()` (liest `cached_favorites`);
   Ergebnis als `FavoritesPageLoadedMessage(items, error)` zurück.
4. `AppUpdate` übernimmt die Einträge (`IsLoading = false`).

Beteiligte Klassen/Komponenten: `FavoritesPage`, `AppUpdate`,
`ShellAppEffectHandler`, `ServerSyncService`, `HomeListService`.

### Favorit aus der Liste entfernen (online)

1. „Entfernen"-`Button` dispatcht
   `FavoriteRemoveRequestedMessage(item)`.
2. `AppUpdate`: Guard — `IsOffline`/kein `Connected` oder
   `item.FavoriteId <= 0` → unverändert; sonst Eintrag mit
   `IsRemoving = true` markieren, `Error = null`, Effekt
   `RemoveFavoriteEffect(item.FavoriteId)`.
3. `ShellAppEffectHandler` → `FavoritesService.RemoveFavoriteAsync`:
   `client.RemoveFavoriteAsync(favoriteId)` (POST
   `api/favorites/remove` mit `{ id, userId }`), danach
   `SyncFavoritesAsync` (erneuert `cached_favorites` autoritativ).
   Ergebnis als `FavoriteRemoveCompletedMessage(favoriteId, success)`
   zurück; bei Erfolg zusätzlich `CachedListsChangedMessage` (die
   Startseiten-Favoriten laden den neuen Cache-Stand).
4. `AppUpdate` bei Erfolg: Eintrag aus `Items` entfernen; bei
   Fehlschlag `IsRemoving` zurücksetzen und
   `Error = "Der Favorit konnte nicht entfernt werden."` setzen.

Beteiligte Klassen/Komponenten: `FavoritesPage`, `AppUpdate`,
`FavoritesService`, `ShellAppEffectHandler`, `ServerSyncService`.

### Favoriten-Eintrag auswählen (Detail-Navigation)

1. `SelectionChanged` der `CollectionView` dispatcht
   `MediaItemSelectedMessage` mit einem aus dem `FavoriteListItem`
   gebauten `HomeMediaItem` (inkl. `ShowId`/`SeasonId`/`EpisodeId`).
2. Bestehender Übergang `UpdateMediaItemSelected` erzeugt
   `NavigateToMediaDetailsEffect` → Route `mediadetails?…`.

Beteiligte Klassen/Komponenten: `FavoritesPage`, `AppUpdate`,
`ShellAppEffectHandler`, `MediaDetailsPage`.

### Offline-Anzeige der Favoriten-Seite

1. `FavoritesPageOpenedMessage` im Offline-Zustand → nur
   `LoadFavoritesPageEffect` (der Handler überspringt den Sync ohne
   Client und liest den Cache).
2. Die Seite bindet den Offline-Banner an `Model.IsOffline` und
   deaktiviert die „Entfernen"-Buttons (`IsRemoving` oder offline).

Beteiligte Klassen/Komponenten: `FavoritesPage`, `AppUpdate`,
`ShellAppEffectHandler`.

### Profil-Seite öffnen und anzeigen

1. `ProfilePage.OnAppearing` dispatcht `ProfilePageOpenedMessage`.
2. `AppUpdate`: `Profile` → `IsLoading = true`, Effekt
   `LoadProfileInfoEffect`.
3. `ShellAppEffectHandler` → `ProfileInfoService.LoadAsync`:
   gespeicherten Benutzernamen und Token aus `AppSecretsService`
   lesen, JWT-Claims dekodieren (`unique_name`/`name`, `email`),
   `AppProfile.Name` ergänzen; Ergebnis als
   `ProfileInfoLoadedMessage(info)` zurück.
4. `AppUpdate` übernimmt Benutzername/E-Mail/Profilname.

Beteiligte Klassen/Komponenten: `ProfilePage`, `AppUpdate`,
`ProfileInfoService`, `AppSecretsService`, `AppProfile`,
`ShellAppEffectHandler`.

### Abmelden

1. „Abmelden"-`Button` dispatcht `LogoutRequestedMessage`.
2. `AppUpdate`: Guard — bereits `NeedsLogin`/`NeedsServerSetup` →
   unverändert; sonst Sitzungszustand zurücksetzen
   (`ConnectionState = NeedsLogin`, `CurrentPage = AppPage.Login`,
   `LoginError`/`LoginUsername = null`, Lauf-Flags `false`,
   `ContinueWatching`/`Favorites`/`RecentEntries` = leer,
   `SourcesOverview`/`SourceBrowser`/`MediaDetails`/`FavoritesPage`/
   `Profile` auf Anfangszustand) und Effekte
   `[DisconnectServerEventsEffect, LogoutEffect,
   NavigateToPageEffect(AppPage.Login)]`.
3. `ShellAppEffectHandler`: `LogoutEffect` →
   `AuthSessionService.LogoutAsync()` (Client freigeben,
   `ClientBaseAddress` zurücksetzen,
   `AppSecretsService.ClearCredentialsAsync`).
4. Navigation über die bestehende `login`-Shell-Route; die
   Anmeldemaske zeigt leere Felder (kein gespeicherter Benutzername
   mehr).

Beteiligte Klassen/Komponenten: `ProfilePage`, `AppUpdate`,
`ShellAppEffectHandler`, `AuthSessionService`, `AppSecretsService`,
`LoginPage`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `FavoriteListItem` | Record (`VideoPlayer.Mvu`) | Anzeigeeintrag der Favoriten-Seite: `FavoriteId`, `MediaId`, `MediaType`, `Title`, `PosterPictureId`, `ShowId`/`SeasonId`/`EpisodeId`, `IsRemoving`, abgeleitet `HasPoster`, `MediaTypeText` |
| `FavoritesPageState` | Record (`VideoPlayer.Mvu`) | Zustand der Favoriten-Seite: `Initial`, `IsLoading`, `Attempted`, `Error`, `Items`, abgeleitet `HasItems`/`HasError`/`ShowEmpty` |
| `ProfilePageState` | Record (`VideoPlayer.Mvu`) | Zustand der Profil-Seite: `Initial`, `IsLoading`, `Username`, `Email`, `ProfileName` |
| `ProfileInfo` | Record (`VideoPlayer.Mvu`) | Ergebnis der Profil-Ermittlung: `Username`, `Email`, `ProfileName` |
| `ProfileInfoService` | Klasse (`VideoPlayer.Mvu`) | Liest gespeicherten Benutzernamen + Token (`AppSecretsService`), dekodiert JWT-Claims, ergänzt `AppProfile.Name` |
| `FavoritesPageOpenedMessage`, `FavoritesPageLoadedMessage`, `FavoriteRemoveRequestedMessage`, `FavoriteRemoveCompletedMessage`, `ProfilePageOpenedMessage`, `ProfileInfoLoadedMessage`, `LogoutRequestedMessage` | Records (`VideoPlayer.Mvu`, `AppMessage.cs`) | MVU-Nachrichten der beiden Seiten |
| `LoadFavoritesPageEffect` (`RefreshFromServer`), `RemoveFavoriteEffect`, `LoadProfileInfoEffect`, `LogoutEffect` | Records (`VideoPlayer.Mvu`, `AppEffect.cs`) | MVU-Effekte; `RefreshFromServer` trägt die Entscheidung „Sync vor Cache-Lesen" aus dem Zustand, damit der Handler offline keinen vergeblichen Server-Aufruf macht |

## Änderungen an bestehenden Klassen

### `AppModel` (Datenmodell)

- **Neue Eigenschaften:** `FavoritesPage` (`FavoritesPageState`),
  `Profile` (`ProfilePageState`) — Seitenzustände der neuen Menüseiten.

### `AppMessage` / `AppEffect` (Datenmodelle)

- Neue Message-/Effekt-Records wie unter „Neue Klassen" aufgeführt.

### `AppUpdate` (Logik)

- **Neue Übergänge:** `FavoritesPageOpenedMessage`,
  `FavoritesPageLoadedMessage`, `FavoriteRemoveRequestedMessage`,
  `FavoriteRemoveCompletedMessage`, `ProfilePageOpenedMessage`,
  `ProfileInfoLoadedMessage`, `LogoutRequestedMessage` (inkl.
  Sitzungs-Rücksetzung und Offline-/Guard-Regeln).

### `HomeListService` (Logik)

- **Neue Methode:** `LoadFavoriteItemsAsync` — liest
  `cached_favorites` wie `ReadFavoritesAsync` und mappt auf
  `FavoriteListItem` (inkl. `FavoriteId`, `MediaType` normalisiert,
  Serienkontext-Hints). `MediaHints` wird dafür intern genutzt;
  `MapEntry` bleibt für die Startseiten-Liste unverändert.

### `FavoritesService` (Logik)

- **Neue Methode:** `RemoveFavoriteAsync(long favoriteId,
  CancellationToken)` — `client.RemoveFavoriteAsync` +
  `SyncFavoritesAsync`; `false` ohne angemeldeten Client.

### `AuthSessionService` (Logik)

- **Neue Methode:** `LogoutAsync()` — setzt `Client`/
  `ClientBaseAddress` auf `null` und ruft
  `AppSecretsService.ClearCredentialsAsync` auf (Benutzername,
  Passwort, Token entfernt).

### `FavoritesPage` (UI)

- Vollständiger Ausbau: `MvuHost`-Konstruktor + `BindingContext`,
  `OnAppearing` dispatcht `FavoritesPageOpenedMessage`,
  `CollectionView` mit Poster/Titel/Typ + „Entfernen"-Button,
  Offline-Banner, Lade-/Fehler-/Leerzustand, Auswahl →
  `MediaItemSelectedMessage`.

### `ProfilePage` (UI)

- Vollständiger Ausbau: `MvuHost`-Konstruktor + `BindingContext`,
  `OnAppearing` dispatcht `ProfilePageOpenedMessage`, Anzeige
  Benutzername/E-Mail/lokales Profil/Serveradresse,
  „Abmelden"-`Button` → `LogoutRequestedMessage`.

### `ShellAppEffectHandler` (App-Rand)

- **Neue Effekt-Cases:** `LoadFavoritesPageEffect`,
  `RemoveFavoriteEffect` (inkl. `CachedListsChangedMessage` bei
  Erfolg), `LoadProfileInfoEffect`, `LogoutEffect`.

### `MauiProgram` (DI)

- `ProfileInfoService` als Singleton registrieren.

## Datenbankmigrationen

Keine — `cached_favorites` ist seit Schritt 8 vorhanden und enthält
den vollständigen `FavoriteEntry`-JSON inklusive Eintrags-Id.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `FavoriteListItem.FavoriteId` | Entfernen nur bei `FavoriteId > 0` und verbundenem Zustand | Update-Guard: unverändert; Button offline deaktiviert |
| `FavoriteRemoveCompletedMessage` | Fehlschlag (`Success = false`) → Eintrag bleibt, `Error` gesetzt | Fehlerlabel auf der Seite |
| `LogoutRequestedMessage` | Nur aus `Connected`/`Offline` wirksam | Bereits `NeedsLogin`/`NeedsServerSetup` → unverändert |

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Startseiten-Favoriten:** Folgen dem Entfernen über
  `CachedListsChangedMessage` automatisch (bestehende Verdrahtung).
- **Sitzungsablauf vs. Abmeldung:** Beide führen zu `NeedsLogin`;
  die Abmeldung löscht zusätzlich die gespeicherten Zugangsdaten —
  danach ist keine automatische Anmeldung mehr möglich
  (`MissingCredentials` → Anmeldemaske), was der geforderten
  Abmelde-Semantik entspricht.
- **Offline-Daten nach Abmeldung:** Die lokalen Caches
  (`cached_favorites`, `cached_lists`, Downloads) bleiben auf dem
  Gerät — sie sind nicht backup-pflichtig und werden beim nächsten
  Login/Sync autoritativ ersetzt. Im reinen Offline-Szenario bleibt
  der bisherige Offline-Einstieg (ohne Anmeldung) unverändert — das
  ist Bestandsverhalten aus Schritt 6, keine neue Funktion.
- **JWT-Dekodierung:** Reines Lesen der Payload (Base64Url + JSON),
  keine Signaturprüfung nötig — die Anzeige ist informativ, nicht
  sicherheitsrelevant. Beschädigte/unkonventionelle Tokens fallen auf
  den gespeicherten Benutzernamen zurück.

## Umsetzungsreihenfolge

1. **MVU-Records und Zustände**
   - Voraussetzungen: Keine.
   - Beschreibung: `FavoriteListItem`, `FavoritesPageState`,
     `ProfilePageState`, `ProfileInfo` anlegen; `AppModel` um
     `FavoritesPage`/`Profile` erweitern; Messages und Effekte in
     `AppMessage.cs`/`AppEffect.cs` ergänzen.

2. **Update-Übergänge**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `AppUpdate` um die neuen Übergänge erweitern
     (Laden, Entfernen, Profil, Abmelden mit Zustandsrücksetzung).

3. **Dienste**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `HomeListService.LoadFavoriteItemsAsync`,
     `FavoritesService.RemoveFavoriteAsync`,
     `AuthSessionService.LogoutAsync`, `ProfileInfoService` (inkl.
     JWT-Claim-Dekodierung) implementieren.

4. **Effekt-Handler und Seiten**
   - Voraussetzungen: Schritte 1–3.
   - Beschreibung: `ShellAppEffectHandler`-Cases,
     `FavoritesPage`/`ProfilePage` ausbauen, `MauiProgram`-Registrierung.

5. **Tests**
   - Voraussetzungen: Schritte 1–4.
   - Beschreibung: Neue Testdatei(en) für MVU-Übergänge, Service-Tests
     (RemoveFavorite, Profil-Info, Logout) und Contract-Tests.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `FavoritesPageOpened_StartsLoading` | `AppUpdateTests_FavoritesProfile` | `IsLoading` + `LoadFavoritesPageEffect` |
| `FavoritesPageOpened_WhileLoading_NoOp` | `AppUpdateTests_FavoritesProfile` | kein zweiter Lauf |
| `FavoritesPageLoaded_FillsItems` | `AppUpdateTests_FavoritesProfile` | Einträge/Fehler im Zustand |
| `FavoriteRemoveRequested_Online_MarksRemoving` | `AppUpdateTests_FavoritesProfile` | `IsRemoving` + `RemoveFavoriteEffect` |
| `FavoriteRemoveRequested_Offline_NoOp` | `AppUpdateTests_FavoritesProfile` | Guard im Offline-Zustand |
| `FavoriteRemoveCompleted_Success_RemovesItem` | `AppUpdateTests_FavoritesProfile` | Eintrag weg + `CachedListsChangedMessage`-Verdrahtung (Handler) |
| `FavoriteRemoveCompleted_Failure_ShowsError` | `AppUpdateTests_FavoritesProfile` | `IsRemoving` zurück, `Error` gesetzt |
| `ProfilePageOpened_StartsLoading` | `AppUpdateTests_FavoritesProfile` | `IsLoading` + `LoadProfileInfoEffect` |
| `ProfileInfoLoaded_FillsProfile` | `AppUpdateTests_FavoritesProfile` | Benutzername/E-Mail/Profilname |
| `LogoutRequested_ResetsSessionAndNavigates` | `AppUpdateTests_FavoritesProfile` | `NeedsLogin`, `CurrentPage` Login, Listen/Seitenzustände leer, Effekte `DisconnectServerEventsEffect` + `LogoutEffect` + `NavigateToPageEffect(Login)` |
| `LogoutRequested_AlreadyLoggedOut_NoOp` | `AppUpdateTests_FavoritesProfile` | Guard |
| `LoadFavoriteItems_MapsFavoriteIdAndHints` | `HomeListServiceTests` | `FavoriteId`, `MediaType`, Show/Season/Episode-Hints aus `cached_favorites` |
| `RemoveFavorite_CallsApiAndRefreshesCache` | `FavoritesServiceTests` | POST `api/favorites/remove` + `cached_favorites` neu (echte SQLite) |
| `RemoveFavorite_WithoutClient_ReturnsFalse` | `FavoritesServiceTests` | kein Client → `false` |
| `LogoutAsync_ClearsClientAndSecrets` | `AuthSessionServiceTests` | `Client`/`ClientBaseAddress` `null`, Zugangsdaten/Token gelöscht |
| `LoadAsync_ReadsTokenClaims` | `ProfileInfoServiceTests` | `unique_name`/`email` aus JWT-Payload |
| `LoadAsync_FallsBackToStoredUsername` | `ProfileInfoServiceTests` | ungültiges/fehlendes Token → gespeicherter Benutzername, `ProfileName` aus `AppProfile` |
| `FavoritesPage_HasListOfflineHintAndRemove` | `FavoritesProfileContractTests` | XAML: `CollectionView` an `Model.FavoritesPage.Items`, Offline-Border, Entfernen-Button; Code-behind dispatchet `FavoritesPageOpenedMessage`/`FavoriteRemoveRequestedMessage`/`MediaItemSelectedMessage` |
| `ProfilePage_HasInfoAndLogout` | `FavoritesProfileContractTests` | XAML: Profil-Felder + „Abmelden"; Code-behind dispatchet `ProfilePageOpenedMessage`/`LogoutRequestedMessage` |
| `ShellAppEffectHandler_HandlesNewEffects` | `FavoritesProfileContractTests` | Cases + `CachedListsChangedMessage` nach erfolgreichem Entfernen |
| `MauiProgram_RegistersProfileInfoService` | `FavoritesProfileContractTests` | DI-Registrierung |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| Keine | `HomeMediaItem`/`AppModel` bleiben kompatibel erweitert (neue Member mit Defaults); `FavoritesPage`/`ProfilePage` bekommen einen `MvuHost`-Konstruktor — sie werden nur über DI instanziiert (ContentTemplate), Contract-Tests prüfen nur Existenz der XAML. |

### E2E-Tests (primärer Funktionsnachweis)

Das Projekt besitzt kein UI-E2E-Harness (das MAUI-Projekt ist aus dem
Testprojekt nicht referenzierbar) — der etablierte Nachweis ist die
Kette „Seiten-Dispatch (Contract-Test auf Code-Behind) →
Update-Übergang (MVU-Test) → Effekt-Ausführung (Contract-Test +
Service-Test)". Dieselbe Konvention wie Schritt 7/8.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Favoriten öffnen (online) → Server-Favoriten sichtbar | `FavoritesPageOpened`→`LoadFavoritesPageEffect`→`FavoritesPageLoaded` (`AppUpdateTests_FavoritesProfile`) + Handler-Contract (Sync vor Cache-Lesen) + XAML-Contract | AK 1 (online) | Benutzerfluss |
| Pflicht | Favoriten öffnen (offline) → lokale Kopie + Hinweis, Entfernen gesperrt | `FavoriteRemoveRequested_Offline_NoOp` + XAML-Contract (Offline-Banner an `IsOffline`, deaktivierter Button) | AK 1 (offline) | Benutzerfluss + Sichtbarkeitsregel |
| Pflicht | Favorit entfernen → Server-Call → Liste + Startseite aktualisiert | `FavoriteRemove*` (`AppUpdateTests_FavoritesProfile`) + `FavoritesServiceTests.RemoveFavorite_*` (echte SQLite) + Handler-Contract (`CachedListsChangedMessage`) | AK 2 | Benutzerfluss |
| Pflicht | Eintrag antippen → Detailseite | `MediaItemSelectedMessage`-Übergang (vorhanden) + XAML-Contract (Dispatch aus `SelectionChanged`) | AK 3 | Benutzerfluss |
| Pflicht | Profil öffnen → Benutzer + lokales Profil sichtbar | `ProfilePageOpened`/`ProfileInfoLoaded` + `ProfileInfoServiceTests` + XAML-Contract | AK 4 (Anzeige) | Benutzerfluss |
| Pflicht | Abmelden → Sitzung beendet, Anmeldemaske | `LogoutRequested_*` + `AuthSessionServiceTests.LogoutAsync_*` + XAML-Contract | AK 4 (Abmeldung) | Benutzerfluss |

Angepasste bestehende E2E-Tests: Keine.

## Offene Punkte

Keine.
