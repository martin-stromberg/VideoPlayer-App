# Windows-End-to-End-Tests (FlaUI)

Das Projekt `VideoPlayer.E2E.Tests` führt echte End-to-End-Tests gegen
die Windows-App aus (Issue #17, Schritt 3): Jeder Test startet einen
eigenen `VideoPlayer.Maui.exe`-Prozess, steuert die Oberfläche über
FlaUI/UI-Automation und prüft den App-Zustand über die
[Developer-API](developer-api.md). Als Backend dient ein in den Tests
mitlaufender lokaler Mock-Server — kein reales Server-Deployment nötig.

## Voraussetzungen

- **Windows** mit Desktop-Session (die Tests brauchen sichtbare Fenster;
  auf Windows lassen sie sich lokal oder per RDP ausführen).
- Die Tests führen den **Debug-Build** selbst aus
  (`dotnet build VideoPlayer.Maui -f net10.0-windows10.0.19041.0 -c Debug`)
  und starten anschließend
  `VideoPlayer.Maui/bin/Debug/net10.0-windows10.0.19041.0/win-x64/VideoPlayer.Maui.exe`.
- Die Tests laufen **sequentiel** (`[Collection("E2E")]` ohne
  Parallelisierung): Fensterfokus und `devapi.json` sind exklusiv.

## Ausführen

```bash
dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj
```

Erwartetes Ergebnis: 74 Testfälle in 19 Testklassen, alle bestanden
(Laufzeit ca. 20–25 min, geprägt von App-Starts und echten
Wartezeiten, u. a. beim nicht-erreichbaren Server und dem
Wiedergabestart).

Die gesamte Suite ist umgebungssensitiv (Prozesse, UI-Automation,
Sockets, Dateisystem) und trägt deshalb klassenweise das Trait
`[Trait("Category", "OsInterface")]` — sie läuft bewusst **nicht** in
der CI (gehostete Runner haben keine interaktive Desktop-Session).
Gefilterte Läufe und die Lane-Trennung der Unit-Tests beschreibt
[OsInterface-Tests und Flake-Quarantäne](stabilitaet/os-interface-tests.md):

```bash
dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj --filter "Category=OsInterface"
```

## Architektur

- **`AppFixture`** — einmaliger Debug-Build der App für alle Tests.
- **`AppInstance`** — startet die Exe mit isoliertem Environment:
  `VIDEOPLAYER_PROFILE=e2e-<test>-<guid>` (eigenes App-Daten- und
  Token-Verzeichnis), entfernt geerbtes `VIDEOWEBPLAYER_MAUI_API_TOKEN`,
  wartet auf `devapi.json` mit passender PID, attacht FlaUI.UIA3 auf
  das Hauptfenster und räumt in `Dispose` Prozess (Kill-Baum nach
  `CloseMainWindow`), Metadatei und Profilverzeichnisse auf. Das
  Löschen der Profilverzeichnisse läuft über
  `TempDirectoryCleanup.DeleteWithRetry` — bis zu drei Versuche mit
  gestaffeltem Backoff (200/500/1000 ms) bei gesperrten Handles
  (`IOException`/`UnauthorizedAccessException`); jeder Versuch und
  ein verbleibender Restfehler werden protokolliert statt
  kommentarlos geschluckt.
- **`DevApiClient`** — `GET /model`-Polling mit Timeout und Letztstand
  im Fehlertext, `GET /tree`, `POST /invoke` (inkl. `itemIndex` für
  `CollectionView` und `BindableLayout`-Container), `POST /set-text`.
- **`MockVideoServer`** — `TcpListener` auf `127.0.0.1` mit OS-Port;
  implementiert `api/health`, `api/pairing/exchange` (ECDH P-256 +
  AES-256-GCM, deckungsgleich mit `PairingCrypto`), `api/pairing/bootstrap`
  (Ticket → verschlüsseltes Session-Paket inkl. Refresh-Token,
  Kurzcode-Alias), `api/auth/login`, `api/auth/refresh` (atomare
  Refresh-Rotation) und `api/auth/logout` (idempotenter Widerruf)
  sowie die Listen-Endpunkte, die Home/Synchronisation benötigen.
  Unterscheidet Gate-Ablehnung (`application/problem+json`) von
  Credential-Ablehnung (`text/plain`) wie der echte Server.
  Opt-in pro Test (über `PlaybackMovieId` u. a. aktiviert, Standard
  bleiben die Leerlisten): `api/items/recent` mit Movie-Eintrag,
  `api/items/movie/{id}` (Details), `api/continue-watching` mit
  Fortsetzungsposition, `api/items/movie/{id}/stream` (`video/mp4`
  aus `Assets/testvideo.mp4`, `Accept-Ranges`/`206 Partial Content`
  auf `Range`-Requests, per `access_token`-Query authentifiziert,
  optionaler 404-Fehlschlag), die Fortschritts-Senke
  `POST api/continue-watching/progress` (204) sowie die
  Kontextmenü-Mutationen `POST api/continue-watching/hide` und
  `…/skip` — gemeldete `mediaType`/`mediaId`-Paare werden aus
  Folgeantworten der Liste entfernt (`RemovedContinueWatchingKeys`,
  `HideContinueWatchingCount`, `SkipContinueWatchingCount` als
  Diagnose-Zähler).
- **`UiDriver`** — AutomationId-Suche, Texteingabe per Value-Pattern,
  Button-Invoke, Flyout-Navigation per `SelectionItem`, Auslösung per
  Klartext-Name (`ClickByName`: Fenster + prozessbezogene Popups, für
  Dialog-Schaltflächen wie das `DisplayActionSheet`) — bewusst ohne
  Koordinatenklicks.

## Szenarien (74 Tests in 19 Klassen)

- **Server-Einrichtung** (5): Erststart zeigt die Einrichtungsseite
  (inkl. `ConnectCard` im Elementbaum); ungültige Adresse →
  Validierungsfehler; nicht erreichbarer Server → sichtbarer Fehler;
  erreichbarer Server → „Pairing erforderlich"; erreichbarer Server →
  Aktiver-Server-Karte mit Status-Badge „Server: erreichbar" und
  geführter Pairing-Karte.
- **Pairing** (3): ungültiges Format → lokaler Formatfehler; falscher
  Code → Pairing-Fehler vom Server; gültiger Code → Anmeldemaske.
- **Anmeldung** (2): falsches Passwort → sichtbarer Login-Fehler;
  gültige Anmeldung → Startseite.
- **Serverwechsel** (1): Server A einrichten (Pairing + Anmeldung),
  über Einstellungen auf Server B wechseln, dann Server A aus der
  Liste bekannter Server per `/invoke` `itemIndex` auswählen →
  Rückkehr zur Startseite mit Server A (Regression aus Issue #15).
- **Bootstrap** (6): Ticket-Adresse → direkt auf die Startseite ohne
  Anmeldung (Server wird vorher übernommen); Kurzcode auf dem
  verbundenen Server → Startseite; ungültiges Ticket →
  Kopplungs-Fehler vom Server; nicht erreichbarer Ticket-Server →
  Fehler ohne Speicherung; fehlerhafte Eingabe → lokaler
  Validierungsfehler ohne Serverkontakt; Bootstrap-Sitzung
  übersteht einen App-Neustart (Refresh-/Geräte-Token bleiben
  wirksam, keine erneute Eingabe).
- **Wiedergabe** (2, `PlaybackTests`): Wiedergabestart mit
  Fortsetzungsposition — Eintrag des „Neu im Programm"-Karussells
  (`AutomationId="RecentCarousel"`, BindableLayout-Container) per
  `/invoke`+`itemIndex` wählen → Detailseite → „Abspielen"
  (`AutomationId="PlayButton"`) → `playerIsActive`/`playerIsPlaying`
  und `playerPositionSeconds` ≥ Weiterschauen-Position, Prozess lebt
  (Regressionsschutz für den iOS-Absturz beim Resume-Start); sowie
  Stream-Fehlschlag (Endpunkt → 404) → `playerError` im Modell
  sichtbar statt Prozessabbruch.
- **Weiterschauen-Direktstart** (3, `ContinueWatchingPlaybackTests`):
  Tap auf Film-Eintrag → Wiedergabe ab Fortsetzungsposition ohne
  Detailaufruf; Tap auf Episoden-Eintrag → Wiedergabe mit
  Serienkontext-Auflösung und Wechsel zur Folge-Episode; Tap auf
  Serien-Eintrag → Detailansicht statt Wiedergabe.
- **Weiterschauen-Kontextmenü** (3, `ContinueWatchingContextMenuTests`):
  Long-Press (`/invoke` mit `gesture:"longpress"`) öffnet das Menü mit
  „Ausblenden"/„Überspringen" — „Ausblenden" ruft den hide-Endpunkt
  und der Eintrag verschwindet, „Überspringen" entsprechend den
  skip-Endpunkt, „Abbrechen" löst keinen Request aus und lässt den
  Eintrag bestehen.
- **Playlists-Übersicht** (4, `PlaylistsTests`): Menüeintrag zeigt
  eigene und öffentliche Playlists; Auswahl zeigt die Einträge in
  Server-Reihenfolge; Cover werden über den Cover-Endpunkt
  angefordert; Serverfehler → verständliche Fehleranzeige mit
  „Erneut versuchen".
- **Playlist-Verwaltung** (11, `PlaylistManagementTests`): Anlage per
  Formular erscheint in der Übersicht und wird an den Server gemeldet;
  Bearbeiten und Löschen über das Verwaltungsmenü; Medium zur Playlist
  hinzufügen; Eintrag entfernen inklusive Konflikt-Dialog
  (Bestätigen wiederholt mit Flag, Abbrechen behält den Eintrag);
  Einträge umsortieren; Sortiermodus-Änderung mit Konflikt-
  Bestätigung; Genres setzen/zurücksetzen; Öffentlich-Schalter für
  Admin-Benutzer; fremde öffentliche Playlists zeigen keine
  Verwaltungsaktionen.
- **Playlist-Wiedergabe** (6, `PlaylistPlaybackTests`): „Abspielen"
  startet beim ersten abspielbaren Eintrag; Eintragsauswahl startet
  gezielt; Next-/Previous-Navigation; Wiedergabeende und
  Stream-Fehlschlag springen zum nächsten Eintrag;
  Weiterschauen-Einträge folgen der Playlist-Reihenfolge, nicht der
  Serienfolge.
- **Playlist-Fortschrittsmeldung** (2, `PlaylistProgressReportTests`):
  Fortschritt aus Playlist-Wiedergabe trägt die Playlist-ID, ohne
  Playlist-Kontext `null`.
- **Serien-Detailseite** (3, `SeriesDetailsTests`): Episoden und
  Staffeln in aufsteigender Reihenfolge; Staffelwechsel mit erhaltener
  Auswahl; Rückkehr aus Episoden-Details zeigt die gewählte Staffel.
- **Quellen-Browsing, -Paging und -Suche** (7, `SourceBrowsingTests`/
  `SourcePagingTests`/`SourceSearchTests`): Genre-Filterleiste sichtbar
  und filtert, „Alle" setzt zurück; Leiste ausgeblendet ohne
  Server-Genres und höhenbegrenzt, damit das Poster-Raster sichtbar
  bleibt; Nachladen der nächsten Seite hält die Scrollposition;
  uneingereichte Sucheingabe übersteht Modelltakte, Enter filtert ab
  der ersten Seite, der eingereichte Text bleibt nach Rückkehr
  sichtbar.
- **Saisonale Genres** (6, `SeasonalGenresTests`): Filmkarte öffnet
  den exakten Film; mehrere Genre-Sektionen halten Reihenfolge und
  Kollektions-Navigation; eine erfolgreiche leere Antwort entfernt die
  Sektion; Fehlschlag + Neustart bewahrt den noch gültigen Cache;
  keine saisonalen Genres → kein leeres Karussell; Ablauf bei
  geöffneter Seite plus Neustart blendet den abgelaufenen Cache aus.
- **Downloads** (3, `DownloadsTests`): „Fehlerhafte löschen" entfernt
  den fehlgeschlagenen Eintrag; ein Auto-Download trägt die
  „Cache"-Kennzeichnung; ein angesehener Download wird nach Ablauf der
  Löschverzögerung bereinigt.
- **Programmprotokoll** (7, `LogProtocolTests`): deaktiviertes
  Protokoll blendet die Versand-Schaltfläche aus und schreibt keine
  Datei; Aktivierung zeigt den Versand und legt die Logdatei an;
  die Einstellung übersteht einen App-Neustart; „Protokoll senden"
  ruft Teilen/E-Mail ohne Absturz auf; echte Abläufe (Anmeldung,
  Abmeldung, PiP, Wiedergabefehler) schreiben Diagnose-Einträge.

## Grenzen und Hinweise

- Nur Windows (UIA3); Android/iOS sind nicht abgedeckt.
- Der Mock bildet den real getesteten API-Ausschnitt ab; ein optionaler
  Lauf gegen einen echten Server bleibt ein manueller Nachweis
  (Backend-Abwägung in `inventory/backend-optionen.md` der Feature-Doku).
- `devapi.json` liegt neben der Exe — parallel gestartete App-Instanzen
  aus demselben Verzeichnis würden sich die Metadatei überbieten;
  die sequentielle Collection verhindert das innerhalb der Suite.
- CI-Ausführung ist nicht vorgesehen: gehostete Runner haben keine
  interaktive Desktop-Session (Entscheidung in Issue #97). Die
  Trait-Markierung dient lokalen gefilterten Läufen — Details in
  [OsInterface-Tests und Flake-Quarantäne](stabilitaet/os-interface-tests.md).
