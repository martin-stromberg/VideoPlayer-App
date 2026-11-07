# Übersetzte Anforderung: Netzwerkstatus-Erkennung und Synchronisation

## Fachliche Zusammenfassung

Die MVU-App `VideoPlayer.Maui` erkennt Wechsel des Netzwerkstatus (online ↔
offline) zur Laufzeit. Bei Rückkehr ins Netz stellt sie die Verbindung zum
Server automatisch wieder her — inklusive der Echtzeit-Verbindung für
Server-Ereignisse (SignalR-Hub `hubs/mediaupdate` mit den Ereignissen
`ContinueWatchingUpdated`, `FavoritesChanged`, `NewVideosScanned`,
`StatusChanged`) — und führt eine Synchronisation aus: vollständiger
Metadaten-Sync in die `CacheDatabase`, Nachsenden der in der
backup-pflichtigen Warteschlange `pending_progress` (`BackupDatabase`)
gesammelten Fortschrittsmeldungen in sinnvoller Reihenfolge sowie
Aktualisierung der Weiterschauen-Liste. Der Offline-Zustand ist dem Anwender
über den vorhandenen Offline-Hinweis sichtbar.

## Betroffene Klassen und Komponenten

- **MVU-Kern (`VideoPlayer.Mvu`):**
  - `AppModel` — neuer Netzwerkstatus (`IsOnline`), Synchronisationszustand
    (`IsSyncRunning`)
  - `AppMessage` — neue Messages für Netzwerkwechsel, Server-Ereignisse,
    Ereignisverbindung getrennt, Synchronisationsabschluss
  - `AppEffect` — neue Effekte für Netzwerk-Monitoring, Aufbau/Trennung der
    Ereignisverbindung und die Synchronisation (vollständig und teilweise)
  - `AppUpdate` — Verzweigungen Online→Offline (nur aus `Connected`),
    Offline→Wiederverbindung, Ereignis→Teil-Synchronisation
  - Neue Dienste (plattformneutral, testbar): Netzwerkmonitor-Abstraktion
    (`INetworkStatusMonitor`), Ereignisverbindungs-Abstraktion
    (`IServerEventsConnection`) inkl. Orchestrierungsdienst
    (`ServerEventsService`), Synchronisationsdienst (`ServerSyncService`),
    Fortschrittsmelder mit Offline-Warteschlange (`ProgressReportService`)
- **Ablage (`VideoPlayer.Storage`):**
  - `PendingProgressSyncEntry` — Erweiterung um die Videodauer (die
    Server-API verlangt `durationSeconds`); Migration der bestehenden
    Tabelle `pending_progress`
  - `BackupDatabase` — Spaltenmigration bei `InitializeAsync`
  - Neuer `PendingProgressSyncService` — Warteschlangen-Operationen
    (Enqueue, geordnetes/coalesciertes Auslesen, Entfernen nach Senden)
  - `CacheDatabase` — Schreibzugriffe für Metadaten-Cache
    (`cached_lists`-Schlüssel, `cached_favorites`-Ersatz)
- **App-Rand (`VideoPlayer.Maui`):**
  - `ConnectivityNetworkStatusMonitor` — `INetworkStatusMonitor` über
    `Microsoft.Maui.Networking.Connectivity`
  - `SignalRServerEventsConnection` — `IServerEventsConnection` über
    `Microsoft.AspNetCore.SignalR.Client` (Hub `hubs/mediaupdate`,
    Bearer-Token, `WithAutomaticReconnect`)
  - `ShellAppEffectHandler` — Ausführung der neuen Effekte, Verdrahtung
    Monitor-/Ereignis-Events in den MVU-Zyklus
  - `MauiProgram` — DI-Registrierung der neuen Dienste; Paketreferenz
    `Microsoft.AspNetCore.SignalR.Client`
  - `HomePage` — der vorhandene `OfflineBanner` wird nun auch zur Laufzeit
    bei Zustandswechseln ein-/ausgeblendet (bereits modelgetrieben)
- **API-Client (`VideoPlayer.ApiClient`):**
  - `VideoPlayerApiClient` — vorhandene Methoden werden verwendet:
    `ReportPlaybackProgressAsync`, `GetContinueWatchingAsync`,
    `GetSourcesAsync`, `GetRecentEntriesAsync`, `GetFavoritesAsync`
- **Tests (`VideoWebPlayer.Maui.Tests`):**
  - MVU-Übergangstests für Netzwerkwechsel, Wiederverbindung,
    Server-Ereignisse und Sync-Abschluss
  - Service-Tests: Warteschlange (persistente `BackupDatabase` im
    Temp-Pfad, Neustart-Simulation), Synchronisation mit
    `StubHttpMessageHandler`, Ereignisverbindung mit Fake-`IServerEventsConnection`
  - Contract-/Migrationstest für die neue `DurationTicks`-Spalte

## Implementierungsansatz

- Der Netzwerkstatus wird über `Connectivity.Current.NetworkAccess` /
  `ConnectivityChanged` ermittelt, hinter `INetworkStatusMonitor`
  gekapselt (plattformneutral testbar) und als `AppModel.IsOnline` im
  MVU-Model geführt; Wechsel gelangen als `AppMessage` in den
  `AppUpdate`-Zyklus.
- Wiederverbindung nutzt die bestehende Start-Kette
  (`CheckServerHealthEffect` → `AttemptAutoLoginEffect` → `Connected`);
  erfolgreiche Anmeldung stößt zusätzlich den Aufbau der
  Ereignisverbindung und die vollständige Synchronisation an.
- Die Ereignisverbindung ist als `IServerEventsConnection` abstrahiert
  (SignalR-Implementierung am App-Rand); eingehende Server-Ereignisse
  werden als Messages in den MVU-Zyklus dispatcht und lösen gezielte
  Teil-Synchronisationen aus — die betroffenen Anzeigen werden über die
  aktualisierten Caches ohne manuelle Aktualisierung frisch.
- Fortschrittsmeldungen werden bei Offline oder Sendefehler persistent in
  `pending_progress` eingereiht (`PendingProgressSyncService`) und bei der
  Synchronisation chronologisch — pro Medium nur die jeweils neueste
  Meldung — nachgesendet; erfolgreich gesendete Einträge werden entfernt.
- Auswahl-/Identifikationsanforderungen: keine — die Anforderung enthält
  keine Anwender-Auswahl; alle Abläufe sind automatisch.

## Konfiguration

Keine neuen Konfigurationseinträge. Die Serveradresse kommt aus der
vorhandenen Serververwaltung (`ActiveServerAddress`), das Bearer-Token für
die Ereignisverbindung aus dem angemeldeten `AuthSessionService.Client`.

## Akzeptanzkriterien (aus der Anforderung)

1. Trennen und Wiederherstellen der Netzwerkverbindung wird zuverlässig
   erkannt (Online↔Offline-Wechsel aktualisiert den Verbindungszustand).
2. Nach Online-Rückkehr laufen Wiederverbindung und Synchronisation
   automatisch ab (inkl. erneuter Aufbau der Ereignisverbindung).
3. Zuvor offline erfasste Fortschritte erreichen den Server ohne Verlust
   (persistente Warteschlange, geordnetes Nachsenden, Aufbewahrung über
   App-Neustarts).
4. Vom Server empfangene Ereignisse aktualisieren die betroffenen Anzeigen
   ohne manuelle Aktualisierung (Weiterschauen, Favoriten, neue Inhalte —
   über die gezielte Aktualisierung der zugrundeliegenden Caches).

## Offene Fragen

Keine — der Server bietet den SignalR-Hub `hubs/mediaupdate`
(JWT-authentifiziert) nachweislich an (`VideoWebPlayer/Hubs/
MediaUpdateHub.cs`, `WebApplicationExtensions.cs` im Server-Repo), die
Ablageorte und der API-Vertrag sind bekannt, und die technische
Entscheidung (Connectivity-API, SignalR-Client) ist durch den Kontext
vorgegeben.
