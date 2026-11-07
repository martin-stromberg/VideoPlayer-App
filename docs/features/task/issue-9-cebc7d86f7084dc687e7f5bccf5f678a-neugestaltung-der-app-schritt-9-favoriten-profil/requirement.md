# Übersetzte Anforderung: Favoriten- und Profil-Seite (Entwicklungsschritt 9)

## Fachliche Zusammenfassung

Die MVU-App `VideoPlayer.Maui` erhält zwei ausgebaute Menüseiten:

a) **„Favoriten"** (`AppPage.Favorites`, Route `favorites`): Liste aller als
Favorit markierten Medien des angemeldeten Benutzers mit Posterbild, Titel
und Typ — an der Favoritenliste der Webanwendung orientiert. Die Favoriten
werden primär serverseitig geführt (`GET api/favorites`); die lokale Kopie
in `cached_favorites` (nicht backup-pflichtig) dient als Offline-Cache.
Ein Favorit kann direkt auf der Seite entfernt werden; die Entfernung wird
online zum Server übertragen (`POST api/favorites/remove` mit der
Favoriten-Eintrags-Id — Muster der Webanwendung
`FavoritesList.razor` → `RemoveFavoriteAsync(favorite.Id)`). Im
Offlinebetrieb wird die lokale Kopie mit Hinweis angezeigt; Entfernen ist
dann nicht möglich. Die Auswahl eines Eintrags navigiert zur
Medien-Detailseite (`mediadetails`-Route mit Serienkontext-Hints).

b) **„Profil"** (`AppPage.Profile`, Route `profile`): zeigt den am Server
angemeldeten Benutzer (Benutzername/E-Mail — die Server-API besitzt keinen
Benutzerinfo-Endpunkt; Benutzername und E-Mail werden aus den Claims des
gespeicherten JWT gelesen, als Fallback der gespeicherte
Anmelde-Benutzername) sowie das aktive lokale Profil (`AppProfile.Name`).
Die Schaltfläche „Abmelden" beendet die Sitzung: Token und Zugangsdaten
werden aus den Ablagen gelöscht (`AppSecretsService.ClearCredentialsAsync`),
der gehaltene API-Client wird freigegeben (`AuthSessionService`), die
Ereignisverbindung wird getrennt und der Anwendungszustand wird
zurückgesetzt — die App führt zurück in den Anmelde-Workflow
(`AppPage.Login`, `ConnectionState.NeedsLogin`). Ein serverseitiger
Logout-Endpunkt existiert nicht (JWT ist zustandslos) — die Abmeldung ist
rein lokal.

## Betroffene Klassen und Komponenten

- **MVU-Kern (`VideoPlayer.Mvu`):**
  - `AppModel` — neuer Zustand `FavoritesPage` (Einträge mit
    Favoriten-Eintrags-Id, Lade-/Fehlerzustand, Entfernen-Markierung) und
    `Profile` (Benutzername, E-Mail, Profilname, Ladezustand)
  - `AppMessage` — neue Messages: Favoriten-Seite geöffnet/geladen,
    Favorit entfernen angefordert/abgeschlossen, Profil-Seite
    geöffnet/geladen, Abmeldung angefordert
  - `AppEffect` — neue Effekte: Favoritenliste laden (Sync + Cache lesen),
    Favorit entfernen (Server + Cache-Pflege), Profilinformationen laden,
    Abmeldung ausführen
  - `AppUpdate` — Übergänge der neuen Messages inkl. Offline-Abzweigungen
    und Zustandsrücksetzung beim Abmelden
  - Neue plattformneutrale Records: `FavoriteListItem` (Favoriten-Eintrag
    mit `FavoriteId`, Medien-Identität, Poster, Serienkontext-Hints),
    `FavoritesPageState`, `ProfilePageState`
  - `FavoritesService` — neue Methode `RemoveFavoriteAsync` (Server-Aufruf
    `api/favorites/remove` + `SyncFavoritesAsync` für die lokale Kopie)
  - `AuthSessionService` — neue Methode zum Sitzungsende (Client
    freigeben + Zugangsdaten/Token löschen)
  - Neuer Dienst für Profilinformationen (Benutzername/E-Mail aus
    JWT-Claims des gespeicherten Tokens, Fallback gespeicherter
    Benutzername; Profilname aus `AppProfile`)
  - `HomeListService` — Lesen der `cached_favorites` um die
    Favoriten-Eintrags-Id erweitern bzw. separater Lesepfad für die
    Favoriten-Seite
- **App-Rand (`VideoPlayer.Maui`):**
  - `Pages/FavoritesPage.xaml(.cs)` — Ausbau der Platzhalterseite:
    Liste mit Poster (`CachedPosterImage`), Titel, Typ-Bezeichnung und
    „Entfernen"-Aktion je Eintrag; Offline-Hinweis (Liste aus lokaler
    Kopie, Entfernen deaktiviert); Lade-/Fehler-/Leerzustand
  - `Pages/ProfilePage.xaml(.cs)` — Ausbau der Platzhalterseite:
    Benutzername/E-Mail, aktives lokales Profil, Serveradresse,
    „Abmelden"-Schaltfläche
  - `ShellAppEffectHandler` — Ausführung der neuen Effekte mit
    Rückdispatch der Ergebnis-Messages; nach erfolgreicher Entfernung
    `CachedListsChangedMessage` (Startseiten-Favoriten folgen)
  - `MauiProgram` — DI-Registrierung des Profil-Dienstes
- **Ablage (`VideoPlayer.Storage`):**
  - `cached_favorites` unverändert (bereits aus Schritt 8 vorhanden);
    kein Schema-Change
- **Tests (`VideoWebPlayer.Maui.Tests`):**
  - MVU-Übergangstests: Favoriten-Seite laden (online/offline),
    Entfernen (Erfolg/Fehlschlag/Offline-Block), Detail-Navigation,
    Profil laden, Abmelden (Zustandsrücksetzung + Navigation)
  - Service-Tests: `FavoritesService.RemoveFavoriteAsync` (Server +
    Cache-Pflege über reale SQLite-Datei), Profil-Info aus JWT-Claims +
    Fallback, `AuthSessionService`-Sitzungsende
  - Contract-Tests: Seitenstruktur (Liste, Entfernen-Aktion,
    Offline-Hinweis, Abmelden-Schaltfläche), DI-Verdrahtung

## Implementierungsansatz

- Favoriten- und Profil-Zustand werden als unveränderliche Records im
  `AppModel` geführt (MVU, plattformneutral); die Seiten dispatchen
  Benutzeraktionen als Messages und binden an `Model.*` — Muster
  `SourcesPage`/`SourcesOverviewState`.
- Favoritenliste: Beim Öffnen der Seite wird `LoadFavoritesPageEffect`
  angestoßen; der Effekt-Handler synchronisiert online zuerst
  `cached_favorites` vom Server (`SyncFavoritesAsync` — Server ist
  führend) und liest danach die lokale Kopie; offline wird nur die lokale
  Kopie gelesen. So bleibt die Anzeige auch bei Sync-Fehlschlag
  funktionsfähig (Cache-First).
- Entfernen: `FavoriteRemoveRequestedMessage` markiert den Eintrag als
  „wird entfernt"; der Effekt ruft `RemoveFavoriteAsync(favoriteId)` mit
  der Favoriten-Eintrags-Id auf und erneuert danach `cached_favorites`;
  Erfolg entfernt den Eintrag aus der Liste und löst
  `CachedListsChangedMessage` aus (Startseiten-Favoriten). Im
  Offline-Zustand ist die Aktion blockiert (Schaltfläche deaktiviert +
  Update-Guard).
- Detail-Navigation: Die Auswahl eines Favoriten dispatcht die bestehende
  `MediaItemSelectedMessage` (über ein `HomeMediaItem` mit
  Show/Season/Episode-Hints aus dem eingebetteten Objektgraph — Muster
  `HomeListService.MediaHints`).
- Profil: `ProfilePageOpenedMessage` stößt `LoadProfileInfoEffect` an;
  der Handler liest den gespeicherten Benutzernamen und das
  Anmelde-Token, dekodiert die JWT-Claims (`unique_name`/`name`,
  `email`) und meldet die Profilinformationen zurück.
- Abmelden: `LogoutRequestedMessage` setzt den sitzungsbezogenen
  Zustand zurück (`ConnectionState.NeedsLogin`, `CurrentPage` =
  `AppPage.Login`, gecachte Listen und Seiten-Zustände geleert) und
  erzeugt `LogoutEffect` (Geheimnisse löschen, Client freigeben,
  Ereignisverbindung trennen) plus `NavigateToPageEffect(AppPage.Login)`.
- **Auswahl-/Identifikationsanforderungen:** Favoriten werden
  ausschließlich über sichtbare Listeneinträge ausgewählt/entfernt —
  keine Eingabe interner Kennungen. Die Anzeige zeigt Titel, Typ in
  Klartext und Poster — keine Datenbank-Ids.

## Konfiguration

Keine neuen Konfigurationseinträge. Serveradresse, Anmeldung und Profil
kommen aus dem bestehenden Verbindungs-Workflow (`AuthSessionService`,
`AppSecretsService`, `AppProfile`).

## Akzeptanzkriterien (aus der Anforderung)

1. Die Favoritenliste zeigt online die serverseitigen Favoriten und
   offline die lokale Kopie mit Hinweis.
2. Entfernen eines Favoriten wirkt serverseitig und aktualisiert die
   lokale Kopie sowie die Startseiten-Favoritenliste.
3. Die Auswahl eines Eintrags navigiert zur Medien-Detailseite.
4. Die Profilseite zeigt den Serverbenutzer (Benutzername/E-Mail aus
   Token-Claims bzw. gespeichertem Benutzernamen) und das aktive lokale
   Profil; „Abmelden" führt in den Login-Workflow.

## Offene Fragen

Keine — die API-Lage ist geklärt (kein Benutzerinfo-/Logout-Endpunkt;
JWT trägt `unique_name`/`email`-Claims; Favoriten-Entfernung über
`api/favorites/remove` mit der Eintrags-Id wie in der Webanwendung), und
alle UI-/MVU-Muster liegen aus den Schritten 5–8 vor.
