# Favoriten- und Profil-Seite (Entwicklungsschritt 9)

Die Menüseiten **„Favoriten"** und **„Profil"** der neuen
VideoPlayer-App sind ausgebaut — beide laufen vollständig über den
MVU-Kreislauf und ergänzen die bisherigen Platzhalter um echte
Funktion.

## Favoriten

Die Seite zeigt alle Favoriten des am Server angemeldeten Benutzers
als Kartenliste im Stil der Webanwendung — Poster, Titel und
Medientyp („Film", „Filmkollektion", „Serie", „Staffel", „Episode").
Fehlt ein Poster, bleibt der Titel lesbar.

- **Online ist der Server führend:** beim Öffnen wird zuerst die
  lokale Kopie `cached_favorites` über die Synchronisation
  erneuert und danach angezeigt — die Liste spiegelt den
  Serverstand.
- **Offline** wird ausschließlich die gespeicherte Kopie gezeigt;
  ein Banner weist auf den Offline-Stand hin („Entfernen ist ohne
  Serververbindung nicht möglich") und die Entfernen-Schaltflächen
  sind deaktiviert.
- **Entfernen:** die Schaltfläche „Entfernen" je Eintrag löscht den
  Favoriten serverseitig (`api/favorites/remove` mit der
  Favoriten-Eintrags-ID); danach wird die lokale Kopie autoritativ
  erneuert und die Startseiten-Favoritenliste folgt automatisch.
  Scheitert der Aufruf, bleibt der Eintrag stehen und ein Hinweis
  erscheint.
- **Auswahl** eines Eintrags öffnet die Detailseite über die Route
  `mediadetails` — bei Episoden/Staffeln mit den bekannten
  Serienkontext-Hints (`show`/`season`/`episode`), sodass die
  Serienansicht vorgewählt ist.
- Lade-, Fehler- („Erneut laden") und Leerzustand („Keine Favoriten
  vorhanden …") sind getrennt dargestellt.

## Profil

Die Seite zeigt die Identität der laufenden Sitzung und der
Installation:

- **Angemeldeter Benutzer:** Benutzername und — soweit im
  Anmelde-Token enthalten — E-Mail-Adresse. Die Anzeige nutzt die
  Token-Claims (`unique_name`/`name`/`preferred_username`, `email`);
  ohne lesbare Claims greift die gespeicherte Anmelde-Kennung.
- **Lokale Installation:** aktives Profil (`VIDEOPLAYER_PROFILE`,
  Standard `default`) und die Adresse des verbundenen Servers.
- **Abmelden:** die Schaltfläche beendet die Sitzung lokal — ohne
  Server-Endpunkt. Die Ereignisverbindung wird getrennt, der
  authentifizierte Client freigegeben und Benutzername, Passwort
  und Token gelöscht; danach landet die App auf der Anmeldemaske
  mit leeren Eingabefeldern. Serverliste, Einstellungen und
  Offline-Daten bleiben erhalten — eine erneute Anmeldung mit
  denselben Zugangsdaten stellt den vorigen Stand wieder her.

## MVU-Anbindung

Beide Seiten besitzen eigene Zustände (`FavoritesPageState`,
`ProfilePageState`) und wirken ausschließlich über Messages und
Effekte (`FavoritesPageOpened`/`FavoritesPageLoaded`,
`FavoriteRemoveRequested`/`FavoriteRemoveCompleted`,
`ProfilePageOpened`/`ProfileInfoLoaded`, `LogoutRequested` +
`LoadFavoritesPage`/`RemoveFavorite`/`LoadProfileInfo`/`Logout`).
Die Entscheidung „Server-Sync vor Cache-Lesen" trägt der Effekt
`LoadFavoritesPageEffect` im Parameter `RefreshFromServer` — im
Offline-Zustand wird kein Serveraufruf versucht, obwohl der Client
noch gesetzt ist.
