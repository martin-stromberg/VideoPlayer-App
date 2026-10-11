# Projektplan — Direkte Wiedergabe aus Weiterschauen, Protokollversand und Playlist-Support

## Übersicht

**Issue #43** — Die App (`VideoPlayer.Maui` + `VideoPlayer.Mvu`, MVU-Architektur) soll sechs fachliche Änderungen erhalten:

1. Einträge der Weiterschauen-Liste starten auf Tap direkt die Wiedergabe ab der Fortsetzungsposition (statt nur die Detailansicht zu öffnen); alle anderen Listen behalten das bisherige Verhalten.
2. In den Einstellungen wird die Protokollierung aktivierbar; bei aktivierter Protokollierung öffnet ein „Protokoll senden"-Button einen Mailentwurf mit dem Programmprotokoll als Anhang an `mstromberg84+heimkino@gmail.com`.
3. Die Playlist-Funktionalität des Servers wird in der App unterstützt — im Umfang der Serveranwendung (verbindlich geklärt: „wie Server-UI"): eigene + öffentliche Playlists, Detailansicht mit paginierten Einträgen in Server-Reihenfolge, Wiedergabe inkl. Vor/Zurück/Auto-Fortsetzung sowie Verwaltung (Erstellen/Bearbeiten/Löschen, Einträge hinzufügen/entfernen/sortieren, Sortiermodus, Genres, Öffentlich-Flag). Aufruf über das Menü. **Verbindlich geklärte Ausnahme (Nutzerentscheidung):** Die Cover-Verwaltung (Upload/Regenerieren/Vorschau/Löschen) wird nicht übernommen; die Cover-Anzeige bleibt Teil des Scopes.
4. Bei Weiterschauen-Einträgen mit Playlist-Herkunft wird das richtige Folgevideo bereitgestellt (serverseitig via `play/next|previous|advance`); Downloads mit Playlist-Bezug erfolgen in der richtigen Reihenfolge.
5. iOS-Bugfix: Die Serienansicht zeigt wieder die Episodenliste; die Staffelauswahlbox zeigt die Staffeln in richtiger Reihenfolge und die Auswahl springt nicht mehr zurück.
6. Ein 2-Sekunden-Long-Press auf einen Weiterschauen-Eintrag öffnet ein Kontextmenü mit „Ausblenden" und „Überspringen" (Server-API dafür bereits vorhanden).

**Geklärte Ausgangslage zur Playlist-API:** Die Server-Playlist-API ist bekannt und verifizierbar — Server-Repo `martin-stromberg/VideoPlayer` (public), lokale Installation unter `http://localhost:83/` (`api/playlists` antwortet 401 → Endpunkt existiert). Basisroute `api/playlists` (`PlaylistsController`): Listen (`GET api/playlists`, `GET api/playlists/public`), CRUD (`GET/POST/PUT/DELETE api/playlists[/{id}]`), Einträge (`GET .../{id}/entries`, `GET .../{id}/entries/paged`, `POST .../{id}/entries`, `DELETE .../{id}/entries/{mediaType}/{mediaId}` mit 409-Konflikt bei Weiterschauen-Referenz), Reorder (`PUT .../entries/{entryId}/order`, `POST .../entries/batch-reorder`, `POST .../entries/{entryId}/move-to-beginning`, `POST .../entries/{entryId}/move-between`, `GET .../entries/max-sort-order`), Sortiermodus (`PATCH .../{id}/sort-mode`), Genres (`PUT .../{id}/genres`, `POST .../{id}/genres/reset`), Öffentlich-Flag (`PUT .../{id}/public`), Wiedergabe (`POST .../{id}/play` → `DtoPlaylistPlaybackStart`; `POST .../{id}/play/next|previous|advance` → `DtoPlaylistNavigationResult`), Cover (`GET/DELETE .../{id}/cover`, `POST .../cover/upload|regenerate|preview`). Referenz-Client im Server-Repo: `VideoWebPlayer.Client/IPlaylistApiClient.cs` + `Models/Dto*.cs`; Server-UI (Blazor) unter `VideoWebPlayer/Components/Playlists/`. Weiterschauen-Einträge tragen serverseitig bereits `PlaylistId`, `PlaylistName`, `PlaylistEntryId` (`ContinueWatchingDto`) — die App-seitige `ContinueWatchingEntry`-DTO besitzt diese Felder noch nicht.

**Betroffene Bereiche:** Startseiten-Listen/Weiterschauen (`MediaCarousel`, `HomePage`, `MediaItemSelectedMessage`-Pfad), Wiedergabe (`PlayRequested`-Pfad, `PlaybackSourceService`, `PlayerPage`, `PlayerState.UpcomingEpisodes`), Einstellungen (`SettingsPage`, `AppSettings`), Menü/Navigation (`AppShell`, `AppPage`, `AppRoutes`), API-Client (`VideoPlayerApiClient`, neue Playlist-Endpunkte und DTOs inkl. Playlist-Felder am Weiterschauen-DTO), Downloads (`DownloadService`, `AutoDownloadService`, `download_manifest`), Serienansicht (`MediaDetailsPage`/`SeasonSection`, iOS), Sync/Caches (`cached_lists`).

## Grobe Vorgehensentscheidungen

- **Weiterschauen-Features getrennt, aber vorn angesiedelt:** Direkte Wiedergabe (Anf. 1) und Kontextmenü (Anf. 6) berühren denselben Listenbereich (`MediaCarousel`/Weiterschauen), sind aber fachlich unabhängige, getrennt liefer- und testbare Kundenanforderungen. Zwei eigene, aufeinanderfolgende Schritte — die Reihenfolge vermeidet Überschneidungen am selben Control; Direkte Wiedergabe zuerst als Kernnutzen des Issues.
- **Protokoll-Feature als eigenständiger Schritt:** Anf. 2 hat keinerlei Überschneidung mit den anderen Anforderungen und wird komplett unabhängig geliefert.
- **Playlist-Umsetzung in vier Stufen:** Der verbindlich geklärte Umfang („wie Server-UI") ist zu groß für einen Schritt und wird fachlich geschnitten:
  1. **Anzeige** (API-Anbindung, Menü, Playlist-Übersicht, paginierte Detailansicht in Server-Reihenfolge) — legt Client-Endpunkte, DTOs und Seiten an, auf denen alles Weitere aufbaut.
  2. **Wiedergabe + Folgevideo** (Anf. 3-Wiedergabe inkl. Vor/Zurück/Auto-Fortsetzung und Anf. 4-Folgevideo bei playlist-gebundenen Weiterschauen-Einträgen) — beides nutzt denselben Mechanismus (Playlist-Kontext + `play`-Endpunkte), daher gebündelt.
  3. **Verwaltung** (CRUD, Eintragsverwaltung, Sortieren, Sortiermodus, Genres, Öffentlich-Flag) — unabhängig vom Wiedergabepfad, nur auf der Anzeige-Basis aufbauend.
  4. **Download-Reihenfolge** (Anf. 4-Download-Teil) — eigenes Subsystem (`download_manifest`/`AutoDownloadService`), erst nach dem Playlist-Bezug an Weiterschauen-Einträgen sinnvoll prüf-/korrigierbar.
- **API-Exploration entfällt als Risiko:** Die Server-Playlist-API ist bekannt (siehe Übersicht). Statt Exploration findet in den Playlist-Schritten eine **Verifikation gegen die laufende Instanz `http://localhost:83/`** statt; als DTO-/Verhaltensreferenz dienen `IPlaylistApiClient`/`Dto*.cs` und die Blazor-UI im Server-Repo.
- **iOS-Bugfix als unabhängiger Schritt:** Anf. 5 ist ein reiner Fehler im gemeinsamen UI-Code der Serienansicht ohne Abhängigkeit zu anderen Schritten; Verifikation erfordert einen iOS-Testlauf.
- **Keine Server-Änderungen im Scope:** „Ausblenden"/„Überspringen" (`api/continue-watching/hide|skip` bereits im App-Client vorhanden) und die gesamte Playlist-Funktionalität existieren serverseitig; die App implementiert nur die Client-Seite.
- **Beschlossene Detailentscheidungen (vorab geklärt):** Protokollversand via `Email.ComposeAsync` (Fallback: Fehlerhinweis/Share, wenn kein Mail-Client verfügbar); „Protokoll senden"-Button nur bei aktivierter Protokollierung sichtbar; bei Direktwiedergabe wird die Detailansicht **nicht** im Hintergrund geladen (nur die Folge-Episoden-Daten werden unabhängig von der Seite aufgelöst, siehe Schritt 1); Ist-Zustand der Download-Reihenfolge wird im zuständigen Schritt verifiziert und dokumentiert.
- **Cover-Verwaltung verbindlich ausgeschlossen (Nutzerentscheidung):** Trotz des verbindlichen Umfangs „wie Server-UI" hat der Nutzer ausdrücklich entschieden, dass die Cover-Verwaltung (Upload/Regenerieren/Vorschau/Löschen) **nicht** in die App übernommen wird — bewusste, freigegebene Abweichung vom Server-Funktionsumfang. In Scope bleibt allein die Cover-Anzeige (Schritt 4).

## Entwicklungsschritte

### Schritt 1: Direkte Wiedergabe aus der Weiterschauen-Liste

Ein Tap auf einen Eintrag der Weiterschauen-Liste auf der Startseite startet künftig direkt die Wiedergabe des zugehörigen Videos, beginnend an der Fortsetzungsposition — ohne dass vorher nur die Detailansicht geöffnet wird. Einträge in den anderen Listen (Favoriten, „Neu im Programm", Quellen-Übersichten, Favoriten-Seite, Episodenliste der Serienansicht, Kollektions-Filme) öffnen weiterhin ausschließlich die Detailansicht.

Da `MediaCarousel.ItemTapped` derzeit nur das `HomeMediaItem` liefert, muss am Auslösepunkt die Herkunftsliste (Weiterschauen) unterscheidbar gemacht werden (Hinweis: `HomePage.OnMediaItemTapped` dispatcht aktuell für alle drei Karussells identisch `MediaItemSelectedMessage`). Der Wiedergabestart soll den vorhandenen Wiedergabepfad nutzen (`PlayRequestedMessage` → `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`); die Fortsetzungsposition wird bereits heute von `PlaybackSourceService.ResolveAsync` selbst aus der Weiterschauen-Liste bzw. dem Cache ermittelt und in der `PlayerPage` per verzögertem Seek angesprungen — sie kommt damit automatisch mit. Abspielbar sind nur Medientypen `movie`/`episode`; für Weiterschauen-Einträge der Typen `show`/`season`/`collection` bleibt als Fallback die Detailansicht bestehen (vorgeschlagene Entscheidung, da ein direkter Start technisch nicht möglich ist). **Beschlossen:** Die Detailansicht wird bei der Direktwiedergabe nicht im Hintergrund geöffnet/geladen; sie bleibt über die anderen Listen erreichbar.

**Wichtig — Erhalt der Folgevideo-Funktion:** Die bisherige Folgevideo-/Auto-Fortsetzungs-Funktion für Serien-Episoden aus der Weiterschauen-Liste darf durch den Direktstart nicht verloren gehen. `PlayerState.UpcomingEpisodes` wird derzeit aus dem Serienkontext der geladenen Detailseite befüllt (`AppUpdate.NextEpisodeQueue` aus `model.MediaDetails`) — dieser Kontext fehlt beim Direktstart. In diesem Schritt ist der Serienkontext daher **unabhängig von der Detailansicht aufzulösen**: Die benötigten Episodendaten sind per Datenabruf zu laden (z. B. über den bestehenden Serien-Endpunkt `api/items/tvshow/{showId}`; die Weiterschauen-Einträge liefern `ShowId` über die Serienkontext-Hints), **ohne** die Detailseite zu öffnen — die Entscheidung „keine Hintergrund-Detailansicht" bleibt davon unberührt, es geht nur um die Folge-Episoden-Daten. Den zusätzlichen Playlist-Kontext für playlist-gebundene Einträge ergänzt Schritt 5.

**Akzeptanzkriterien:**
- Ein Tap auf einen abspielbaren Weiterschauen-Eintrag (`movie`/`episode`) startet die Wiedergabe direkt, ohne zuvor die Detailansicht zu öffnen.
- Die Wiedergabe beginnt an der Fortsetzungsposition, nicht am Anfang.
- Taps auf Einträge in Favoriten, „Neu im Programm" und Quellen-Übersichten (sowie Favoriten-Seite, Episodenliste, Kollektionen) öffnen weiterhin nur die Detailansicht.
- Nicht abspielbare Weiterschauen-Einträge (`show`/`season`/`collection`) öffnen weiterhin die Detailansicht.
- Nach Ende einer direkt aus der Weiterschauen-Liste gestarteten Episode wird die nächste Serienepisode wie bisher als Folgevideo bereitgestellt (Auto-Fortsetzung/Folge-Episoden-Auflösung bleibt erhalten; der Serienkontext wird dafür ohne Detailansicht geladen).

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** Weiterschauen-Liste / Startseite, Wiedergabe (Startpfad, Fortsetzungsposition, Folgevideo-/Serienkontext-Auflösung), Detailansicht (Abgrenzung), andere Startseiten-Listen

### Schritt 2: Kontextmenü in der Weiterschauen-Liste (Ausblenden / Überspringen)

Ein 2-Sekunden-Long-Press auf einen Eintrag der Weiterschauen-Liste öffnet ein Kontextmenü mit den Aktionen „Ausblenden" und „Überspringen" — analog zur Serveranwendung. Die Auswahl führt die jeweilige Aktion auf den Eintrag aus; danach wird die Weiterschauen-Liste aktualisiert.

Die Server-Endpunkte existieren bereits im API-Client und werden bislang nicht genutzt: `HideContinueWatchingEntryAsync` → `POST api/continue-watching/hide` und `SkipContinueWatchingEntryAsync` → `POST api/continue-watching/skip` (Ergebnis `ContinueWatchingMutationResult`). Neu zu schaffen sind daher nur: die Long-Press-Erkennung am Weiterschauen-Karussell (Hinweis: `CommunityToolkit.Maui` mit `TouchBehavior LongPressDuration="2000"` ist im Altbestand als 2-s-Muster vorhanden, aber in `VideoPlayer.Maui` noch nicht referenziert), die Menüdarstellung (z. B. `DisplayActionSheet`), die MVU-Nachrichten/-Effekte für Hide/Skip sowie die anschließende Listen-Aktualisierung — online über die bestehende Sync-Strecke (`SyncContinueWatchingEffect` → Cache → `CachedListsChangedMessage`), offline über lokales Entfernen aus dem Cache-Eintrag.

**Akzeptanzkriterien:**
- Ein 2-Sekunden-Long-Press auf einen Weiterschauen-Eintrag öffnet ein Kontextmenü; ein kurzer Tap startet weiterhin das bisherige Verhalten (Wiedergabe/Detailansicht).
- Das Kontextmenü bietet die Aktionen „Ausblenden" und „Überspringen".
- „Ausblenden" ruft die Hide-Funktion der Server-API auf, „Überspringen" die Skip-Funktion; bei Erfolg verschwindet der Eintrag aus der angezeigten Weiterschauen-Liste (bzw. verhält sich gemäß Server-Antwort).
- Das Kontextmenü ist auf die Weiterschauen-Liste beschränkt (keine Ausweitung auf andere Listen).

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** Weiterschauen-Liste / Startseite (Long-Press, Kontextmenü), API-Nutzung (continue-watching hide/skip), Listen-Synchronisation/Cache

### Schritt 3: Protokollierung und Protokollversand per Mail

In den Einstellungen wird die Protokollierung aktivierbar (neuer Schalter). Ist die Protokollierung aktiviert, schreibt die App ein Programmprotokoll (Datei-Logging; als Ablageort bietet sich `AppStorageLayout` an, dessen Verzeichnisse bereits vorhanden sind). Über einen „Protokoll senden"-Button in den Einstellungen wird ein Mailentwurf geöffnet, der das Protokoll als Anhang enthält und an `mstromberg84+heimkino@gmail.com` adressiert ist.

Hinweise aus der Bestandsaufnahme: Es existiert weder ein Datei-Logger noch eine Log-Sammlung (derzeit nur `Debug.WriteLine` bzw. `#if DEBUG`-Debug-Logging). `Microsoft.Maui.Essentials` ist bereits referenziert. **Beschlossen:** Der Versand erfolgt via `Email.ComposeAsync`; ist kein Mail-Client verfügbar, erhält der Nutzer einen Fehlerhinweis (bzw. Fallback über `Share`). Der „Protokoll senden"-Button wird nur bei aktivierter Protokollierung angezeigt. Die neue Einstellung ergänzt `AppSettings`/`AppSettingsService` konventionsgemäß.

**Akzeptanzkriterien:**
- In den Einstellungen lässt sich die Protokollierung aktivieren (und deaktivieren); die Einstellung ist persistent.
- Bei aktivierter Protokollierung wird ein Programmprotokoll erzeugt, das als Datei versendbar ist.
- Der „Protokoll senden"-Button ist nur bei aktivierter Protokollierung sichtbar und öffnet einen Mailentwurf mit dem Programmprotokoll als Anhang.
- Der Mailentwurf ist an `mstromberg84+heimkino@gmail.com` adressiert.
- Ohne aktivierte Protokollierung findet kein Protokollversand statt; ohne verfügbaren Mail-Client erhält der Nutzer einen verständlichen Hinweis.

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** Einstellungen (neuer Schalter + Button), Logging/Protokoll-Infrastruktur, Mailversand

### Schritt 4: Playlist-Anbindung und -Anzeige (Übersicht + Detailansicht)

Die Playlist-Funktionalität des Servers wird in der App angebunden und über das Menü erreichbar: Das Menü enthält einen Eintrag „Playlists", die App zeigt eigene und öffentliche Playlists an und stellt die Einträge einer Playlist paginiert in der Server-Reihenfolge dar.

Die Server-Playlist-API ist bekannt (Basisroute `api/playlists`, siehe Übersicht) — anstelle einer Exploration erfolgt eine **Verifikation der Endpunkte und DTO-Felder gegen die laufende Instanz `http://localhost:83/`** sowie gegen das Server-Repo (`VideoWebPlayer.Client/IPlaylistApiClient.cs` + `Models/Dto*.cs` als Referenz; Blazor-UI unter `VideoWebPlayer/Components/Playlists/` als Verhaltensreferenz). Zu schaffen sind: Playlist-Endpunkte im `VideoPlayerApiClient` für den Anzeige-Teil (`GET api/playlists`, `GET api/playlists/public`, `GET api/playlists/{id}`, `GET api/playlists/{id}/entries` bzw. `entries/paged`, `GET api/playlists/{id}/cover` für die Cover-Anzeige) samt DTOs, MVU-State/Messages/Effects, eine neue Playlists-Übersichtsseite und eine Playlist-Detailansicht, der Menüeintrag (`AppPage` + `AppRoutes` Routes/MenuPages + `FlyoutItem` in `AppShell.xaml` + Seiten/DI-Registrierung; Routen und Menüeinträge sind durch Contract-Tests gepinnt). Der `MockVideoServer` der E2E-Tests ist um die genutzten Playlist-Endpunkte zu ergänzen.

**Annahmen (dokumentiert):** Playlists sind ein Online-Feature — kein Offline-Cache der Playlist-Listen (serverseitig verwalteter Bestand; bei fehlender Verbindung verständliche Fehleranzeige). **Festgelegt:** Die Cover-Anzeige ist Teil des Scopes — Playlist-Cover werden in Übersicht und Detailansicht angezeigt, sofern der Server welche liefert. Die Cover-Verwaltung ist dagegen verbindlich ausgeschlossen (Nutzerentscheidung, siehe Schritt 6).

**Akzeptanzkriterien:**
- Das Menü der App enthält einen Eintrag „Playlists".
- Die eigenen Playlists (`GET api/playlists`) und öffentlichen Playlists (`GET api/playlists/public`) werden aufgelistet.
- Eine Playlist lässt sich öffnen und zeigt ihre Einträge paginiert in der Server-Reihenfolge.
- Die Playlist-Anbindung ist gegen die lokale Server-Instanz verifiziert; bei ausgeschaltetem Server/fehlender Verbindung erscheint eine verständliche Fehleranzeige statt eines Absturzes.

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** Menü/Navigation, API-Client (neue Playlist-Endpunkte/DTOs), neue Playlists-Ansichten (Übersicht + Detail), E2E-Mock-Server

### Schritt 5: Playlist-Wiedergabe und Folgevideo-Auflösung (inkl. Weiterschauen mit Playlist-Bezug)

Einträge einer Playlist lassen sich abspielen — sowohl aus der Playlist-Detailansicht als auch aus der Weiterschauen-Liste, wenn der Weiterschauen-Eintrag aus einer Playlist entstand. Die Wiedergabe nutzt den serverseitigen Playlist-Kontext: Start via `POST api/playlists/{id}/play` (ab entryId oder erstem abspielbarem Eintrag → `DtoPlaylistPlaybackStart`); Folgevideo, Vor/Zurück und Auto-Fortsetzung/Fehler-Rückfall via `POST .../play/next`, `.../play/previous`, `.../play/advance` (→ `DtoPlaylistNavigationResult`).

Damit wird gleichzeitig die Wiedergabe-Hälfte von Anf. 4 erfüllt: Bei einem playlist-gebundenen Weiterschauen-Eintrag kommt das Folgevideo aus der Playlist-Reihenfolge (über die `play`-Endpunkte) statt aus dem Serienkontext. Voraussetzung: Der Playlist-Bezug muss an den Weiterschauen-Daten verfügbar sein — die App-seitige `ContinueWatchingEntry`-DTO ist um `PlaylistId`, `PlaylistName`, `PlaylistEntryId` zu ergänzen (der Server liefert diese Felder bereits in `ContinueWatchingDto`; gegen `localhost:83` verifizieren), und der Bezug ist durch `HomeMediaItem`/Wiedergabepfad bis in den `PlayerState` zu führen.

**Kontextquellen für das Folgevideo:** Die detailseiten-unabhängige Auflösung des Serienkontexts wurde bereits in Schritt 1 geschaffen (Folge-Episoden-Daten werden per Datenabruf geladen, ohne die Detailseite zu öffnen). In diesem Schritt kommt der **Playlist-Kontext als zusätzliche Quelle hinzu**: Hat ein Weiterschauen-Eintrag einen Playlist-Bezug, übernehmen die `play`-Endpunkte (`next`/`previous`/`advance`) die serverseitig korrekte Folge-Ermittlung; ohne Playlist-Bezug gilt unverändert die Serien-Auflösung aus Schritt 1.

**Akzeptanzkriterien:**
- Aus der Playlist-Detailansicht lässt sich die Wiedergabe starten (ab gewähltem Eintrag bzw. erstem abspielbarem Eintrag).
- Innerhalb einer laufenden Playlist-Wiedergabe stehen Vor- und Rückwärts-Navigation zur Verfügung; die Auto-Fortsetzung am Ende bzw. der Fehler-Rückfall setzen auf den nächsten Playlist-Eintrag (Playlist-Reihenfolge).
- Wird ein Weiterschauen-Eintrag mit Playlist-Bezug abgespielt, wird das richtige Folgevideo gemäß Playlist-Reihenfolge bereitgestellt (nicht die nächste Serienepisode).
- Weiterschauen-Einträge ohne Playlist-Bezug verhalten sich wie bisher (Folgevideo aus Serienreihenfolge über die in Schritt 1 geschaffene, von der Detailseite unabhängige Serienkontext-Auflösung).

**Abhängigkeiten:** Schritte 1, 4

**Betroffene Bereiche:** Wiedergabe (Player-State, Folgevideo-Auflösung, Vor/Zurück, Auto-Fortsetzung), API-Client (`play`-Endpunkte, `ContinueWatchingEntry`-DTO mit Playlist-Feldern), Weiterschauen-Liste (Playlist-Kontext an Einträgen), Playlist-Detailansicht

### Schritt 6: Playlist-Verwaltung in der App

Die Playlist-Verwaltung der Serveranwendung wird in der App abgebildet, soweit auf dem Endgerät sinnvoll: Playlists erstellen, bearbeiten und löschen; Einträge hinzufügen und entfernen; Einträge sortieren; Sortiermodus, Genre-Zuordnung und Öffentlich-Flag pflegen.

Konkret (Endpunkte siehe Übersicht): `POST/PUT/DELETE api/playlists[/{id}]` für Anlegen/Bearbeiten/Löschen; `POST .../{id}/entries` zum Hinzufügen eines Mediums; `DELETE .../{id}/entries/{mediaType}/{mediaId}` zum Entfernen — **inkl. Behandlung des 409-Konflikts mit Bestätigungsdialog**, wenn der Eintrag noch von der Weiterschauen-Liste referenziert wird (Server-Verhalten); Reorder via `PUT .../entries/{entryId}/order`, `POST .../entries/batch-reorder`, `POST .../entries/{entryId}/move-to-beginning` bzw. `move-between` (Orientierung an der Server-UI, welche Sortierinteraktionen sie anbietet); `PATCH .../{id}/sort-mode` für den Sortiermodus; `PUT .../{id}/genres` bzw. `POST .../{id}/genres/reset` für Genre-Zuordnung; `PUT .../{id}/public` für das Öffentlich-Flag.

**Annahme (dokumentiert):** Als Einstiegspunkt für „Medium zu Playlist hinzufügen" wird die Detailansicht eines Mediums gewählt (dort liegen bereits Abspielen-/Download-/Favoriten-Aktionen); alternativen Orte (z. B. Quellen-Übersicht) bleiben dem Lifecycle überlassen, solange die Funktion erreichbar ist.

**Verbindlich geklärt — bewusste Abweichung vom Server-Umfang mit Nutzer-Freigabe:** Die Cover-Verwaltung (Upload/Regenerieren/Vorschau/Löschen, `POST .../cover/upload|regenerate|preview`, `DELETE .../{id}/cover`) wird **nicht** in die App übernommen. Obwohl der Playlist-Umfang verbindlich „wie Server-UI" lautet, hat der Nutzer diesen Teilbereich ausdrücklich ausgeschlossen; die Server-UI bleibt für die Cover-Verwaltung zuständig. In Scope bleibt allein die Cover-Anzeige (bereits in Schritt 4 festgelegt).

**Akzeptanzkriterien:**
- Playlists lassen sich in der App erstellen, umbenennen/bearbeiten und löschen.
- Einträge lassen sich zu einer Playlist hinzufügen (aus der Detailansicht eines Mediums) und aus einer Playlist entfernen; beim Entfernen eines noch von der Weiterschauen-Liste referenzierten Eintrags erscheint ein Bestätigungsdialog (409-Konflikt des Servers).
- Die Reihenfolge der Einträge einer Playlist lässt sich ändern (mindestens Verschieben an Anfang/zwischen Positionen analog Server-UI); der Sortiermodus lässt sich umschalten.
- Genre-Zuordnung (setzen/zurücksetzen) und das Öffentlich-Flag lassen sich pflegen.
- Die Bearbeitung öffentlicher fremder Playlists ist nicht möglich bzw. verhält sich wie in der Server-UI (nur eigene Playlists verwaltbar).

**Abhängigkeiten:** Schritt 4

**Betroffene Bereiche:** Playlist-Ansichten (Verwaltungs-UI), API-Client (Verwaltungs-Endpunkte), Detailansicht (Aktion „Zu Playlist hinzufügen"), Dialoge

### Schritt 7: Download-Reihenfolge bei Playlist-Bezug

Videos mit Playlist-Bezug werden beim Download in der richtigen (Playlist-)Reihenfolge heruntergeladen bzw. eingeplant — sofern der Ist-Zustand das nicht bereits abdeckt, was in diesem Schritt zu verifizieren und zu dokumentieren ist.

Ist-Zustand laut Bestandsaufnahme: Die Download-Warteschlange ist FIFO über die Manifest-Id (`download_manifest`, `NextQueuedEntryAsync` → `OrderBy(e => e.Id)`); der automatische Episoden-Vorlauf (`AutoDownloadService`) reiht Folge-Episoden in **Serien**reihenfolge ein — ein Playlist-Bezug ist dem Manifest nicht bekannt (`DownloadManifestEntry` hat kein Playlist-Feld). Nach Schritt 5 tragen Weiterschauen-Einträge den Playlist-Bezug; dieser ist bis in den Download-Kontext durchzureichen (z. B. Playlist-Identität/Sort-Order am Manifest-Eintrag), damit die Einplanung bei playlist-gebundenen Einträgen der Playlist-Reihenfolge statt der Serienreihenfolge folgt. Prüfung: Werden heute Downloads für einen playlist-gebundenen Weiterschauen-Eintrag in falscher Reihenfolge eingeplant (Serienreihenfolge statt Playlist-Reihenfolge)? Falls ja: Korrektur; falls nein: dokumentiert.

**Akzeptanzkriterien:**
- Der Ist-Zustand der Download-Einplanung bei Playlist-Bezug ist verifiziert und das Ergebnis dokumentiert.
- Downloads von Videos mit Playlist-Bezug werden in der Playlist-Reihenfolge eingeplant/ausgeführt (nicht nach Serienreihenfolge).
- Downloads ohne Playlist-Bezug verhalten sich wie bisher.

**Abhängigkeiten:** Schritt 5

**Betroffene Bereiche:** Downloads (`DownloadService`/`AutoDownloadService`, `download_manifest`), API-Modelle/Playlist-Bezug an Download-Daten, Weiterschauen-Kontext

### Schritt 8: iOS-Bugfix — Serienansicht: Episodenliste und Staffelauswahl

Auf iOS zeigt die Serienansicht wieder die Episodenliste an, die Staffelauswahlbox listet die Staffeln in der richtigen (aufsteigenden) Reihenfolge, und die Auswahl einer anderen Staffel als „Staffel 01" bleibt bestehen, statt zurückzuspringen.

Hinweise aus der Bestandsaufnahme (plausibilisierte, auf iOS zu verifizierende Ursachen): Die `CollectionView` `EpisodesCollection` liegt innerhalb des `ScrollView` `DetailsScroll` in `MediaDetailsPage.xaml` (`SeasonSection`) — CollectionView-in-ScrollView ist ein bekanntes iOS-Rendern-Problem und die vermutete Ursache für die fehlende Episodenliste (unter Windows funktioniert die Ansicht). Die Staffeln kommen aus dem Modell korrekt sortiert (`OrderBy(s.Number)` in `MediaDetailsService`); die falsche Anzeigereihenfolge und das Zurückspringen liegen vermutlich am iOS-`Picker`/Binding: `UpdateSeasonSection` setzt bei jedem Model-Tick `ItemsSource`/`SelectedItem` neu, und das Schutzflag `updatingSeasonPicker` deckt nur synchrone `SelectedIndexChanged`-Aufrufe ab — ein verzögertes iOS-Event kann die Auswahl zurücksetzen. Die Ursache ist ohne iOS-Lauf nicht final verifizierbar; der Nachweis der Behebung erfolgt über einen iOS-Testlauf (z. B. `scripts/iOS-Deployment.ps1`).

**Akzeptanzkriterien:**
- Die Serienansicht zeigt auf iOS die Episodenliste an.
- Die Staffelauswahlbox zeigt die Staffeln aufsteigend sortiert („Staffel 01" zuerst).
- Die Auswahl einer anderen Staffel bleibt bestehen und springt nicht auf „Staffel 01" zurück.
- Das Verhalten der Serienansicht auf den anderen Plattformen bleibt unverändert korrekt.

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** Serienansicht / Detailansicht (SeasonSection, Staffel-Picker, Episodenliste), iOS-Plattform

## Übersichtstabelle

| # | Titel | Abhängigkeiten | Betroffene Bereiche |
|---|-------|----------------|---------------------|
| 1 | Direkte Wiedergabe aus der Weiterschauen-Liste | Keine | Weiterschauen-Liste, Wiedergabe (inkl. Folgevideo-Auflösung), Detailansicht, Startseiten-Listen |
| 2 | Kontextmenü in der Weiterschauen-Liste (Ausblenden/Überspringen) | Keine | Weiterschauen-Liste, API (hide/skip), Listen-Sync/Cache |
| 3 | Protokollierung und Protokollversand per Mail | Keine | Einstellungen, Logging, Mailversand |
| 4 | Playlist-Anbindung und -Anzeige (Übersicht + Detailansicht) | Keine | Menü/Navigation, API-Client, Playlists-Ansichten, E2E-Mock-Server |
| 5 | Playlist-Wiedergabe und Folgevideo-Auflösung | 1, 4 | Wiedergabe, API-Client (play-Endpunkte, CW-DTO), Weiterschauen, Playlist-Detail |
| 6 | Playlist-Verwaltung in der App | 4 | Playlist-Ansichten, API-Client (Verwaltung), Detailansicht, Dialoge |
| 7 | Download-Reihenfolge bei Playlist-Bezug | 5 | Downloads, download_manifest, Playlist-Bezug an Download-Daten |
| 8 | iOS-Bugfix: Serienansicht (Episodenliste + Staffelauswahl) | Keine | Serienansicht/Detailansicht, iOS |

## Offene Punkte

- **Einstiegspunkt „Zu Playlist hinzufügen" (Schritt 6):** Die Anforderung (Umfang „wie Server-UI") legt nicht fest, an welcher Stelle der App Medien zu Playlists hinzugefügt werden. *Vorschlag/Annahme:* über die Detailansicht eines Mediums (dort liegen bereits die Medium-Aktionen); weitere Einstiegspunkte (z. B. Quellen-Übersicht, Kontextmenü) bleiben dem Schritt-Lifecycle überlassen — Rückfrage nur, falls ein bestimmter Ort gewünscht ist.
- **Verbindlich entschieden (keine Rückfrage nötig):** Die Cover-Verwaltung wird nicht in die App übernommen — ausdrückliche Nutzerentscheidung und bewusste, freigegebene Abweichung vom Umfang „wie Server-UI" (siehe Schritt 6); die Cover-Anzeige bleibt Teil des Scopes (Schritt 4).
- **Annahmen zur Dokumentation (keine Rückfrage nötig, im Plan festgehalten):** Playlists sind Online-only ohne Offline-Cache (Schritt 4); Einstieg zum Hinzufügen von Medien über die Detailansicht (Schritt 6). Diese Annahmen wurden begründet getroffen und bei Bedarf im jeweiligen Schritt nochmals zu bestätigen.
