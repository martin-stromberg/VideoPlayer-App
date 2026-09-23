# Übersetzte Anforderung — Schritt 12: Offlinebetrieb und Wiederherstellung

Quelle: `docs/projects/task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app/project-plan.md`, Abschnitt „### Schritt 12: Offlinebetrieb und Wiederherstellung". Abhängigkeiten: Schritte 7 (Startseite/Caches), 9 (Favoriten/Profil), 11 (Downloads).

## Fachliche Zusammenfassung

Die neu entwickelte .NET-MAUI-App „VideoPlayer" (MVU-Architektur, `VideoPlayer.Mvu` + `VideoPlayer.Maui`, Zielplattformen iOS und Windows) erhält den vollständigen Offlinebetrieb und einen definierten Wiederherstellungsablauf nach einem Cloud-Backup-Restore. Ohne Serververbindung arbeitet die App auf lokalen Daten weiter; eingeschränkte Bereiche werden sauber gekennzeichnet statt Fehler zu werfen.

## Funktionale Anforderungen (technisch übersetzt)

### 1. Offline-Startseite

- Ohne Serververbindung wird die Startseite (`AppPage.Home`, `HomePage`) aus dem Cache befüllt.
- „Weiterschauen" (`HomeLists.ContinueWatching`) wird **ausschließlich** aus den lokalen Downloads gebildet — nur `download_manifest`-Einträge mit `State == DownloadStateNames.Completed` und vorhandener Datei (`AppStorageLayout.VideosDirectory`).
- Serienkontext offline: fehlende/nicht heruntergeladene Folge-Episoden werden als **Platzhalter-Einträge** dargestellt (nicht abspielbar, entsprechend markiert — z. B. reduzierte Deckkraft + Hinweistext).
- Favoriten sind über die lokale Kopie (`cached_favorites`) verfügbar.
- Einzelne beschädigte Cache-Einträge (JSON/SQLite-Fehler) führen zu leeren Teillisten, nicht zum Absturz.

### 2. Offline-Einschränkungen (sauber gekennzeichnet)

- Keine neuen Inhalte (keine Server-Live-Abfragen; die „Neu im Programm"-Liste zeigt nur den Cache-Stand).
- Keine Quellenliste (`SourcesPage` bleibt live-only; bereits vorhandener Offline-Hinweis).
- Kein Profilabruf (`ProfilePage` zeigt offline eine klare Meldung statt serverabhängiger Daten).
- Der Offline-Zustand ist in der UI erkennbar (Offline-Banner über `AppModel.IsOffline`).

### 3. Offline-Wiedergabe

- Heruntergeladene Videos sind offline abspielbar; `PlaybackSourceService` bevorzugt bereits die lokale Datei (nur `completed`-Einträge mit vorhandener Datei).
- Der Weg zur Wiedergabe funktioniert ohne Server: Detailseite baut sich aus lokalem Kontext (Download-Manifest, gecachter Serienstruktur) auf oder die Wiedergabe startet direkt.

### 4. Wiederherstellung nach Cloud-Backup

- Einstellungen + bekannte Server + zuletzt genutzter Server überleben den Restore (`BackupDatabase`/`Preferences`); Geheimnisse (Token, Passwort via `SecureStorage`) **nicht**.
- Start nach Restore: automatischer Verbindungsversuch zum zuletzt genutzten Server (`KnownServersService.LastConnectedServer` → `CheckServerHealthEffect`).
- Server erreichbar:
  - noch gültiges Token vorhanden → direkter Einstieg (Token validieren, ohne Login-Maske);
  - Token ungültig/fehlend oder Zugangsdaten fehlen → `NeedsLogin` + Anmeldemaske (erneute Anmeldung, Benutzername vorbefüllt).
- Server nicht erreichbar:
  - nutzbare lokale Daten vorhanden → `ConnectionState.Offline` + Startseite;
  - keine lokalen Daten → `NeedsServerSetup` + Serverauswahl.
- Nach erfolgreicher (Wieder-)Verbindung: vollständiger Metadaten-Sync (`ServerSyncService.SynchronizeAsync`), Nachsenden unsynchronisierter Fortschritte (`pending_progress` via `PendingProgressSyncService`), Aktualisierung der Weiterschauen-Liste (`LoadHomeListsEffect`).

### 5. Beschädigte lokale Daten (definiertes Verhalten)

- Beschädigte Cache-Daten werden verworfen und neu synchronisiert (bei bestehender Verbindung; sonst beim nächsten Sync).
- Eine nicht initialisierbare lokale Datenbank wird neu angelegt; die alte Datei wird zuvor als `*.corrupt-*` beiseitegelegt (Backup der alten Datei).
- Der Nutzer wird über die Neuinitialisierung informiert (Startmeldung/Banner).
- Kein Fall darf die App terminieren lassen.

## Akzeptanzkriterien

1. Alle genannten Offline-Fähigkeiten und -Einschränkungen greifen.
2. Der beschriebene Wiederherstellungsablauf funktioniert inklusive erneuter Anmeldung bei fehlenden Geheimnissen.
3. Beschädigte lokale Daten führen nicht zum Absturz, sondern zum definierten Verhalten (Cache-Neuaufbau bzw. Datenbank-Neuinitialisierung mit Nutzerinformation).

## Architekturvorgaben

- Bestehende Mechanismen orchestrieren statt neu erfinden: `INetworkStatusMonitor`, `HomeListService`/`cached_lists`, `cached_favorites`, `DownloadService`/`download_manifest`, `PlaybackSourceService`, `PendingProgressSyncService`, `AuthSessionService`/`KnownServersService`, `ServerSyncService`, `AppStorageLayout`.
- MVU-Muster einhalten: `AppModel` (Zustand), `AppMessage` (Ereignisse), `AppEffect` (Nebenwirkungen), `AppUpdate` (Transitionen), `ShellAppEffectHandler` (Effekt-Ausführung).
- Alle neuen/veränderten öffentlichen Member mit vollständiger deutscher XML-Doku.
- Jeder `RaiseUiActionRequested`-Mechanismus benötigt einen Handler auf der betroffenen Seite.
