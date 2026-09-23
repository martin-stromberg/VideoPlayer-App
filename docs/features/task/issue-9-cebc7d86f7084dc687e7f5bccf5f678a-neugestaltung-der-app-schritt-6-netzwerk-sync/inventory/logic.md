# Logik und Dienste

## MVU-Kern (`VideoPlayer.Mvu`)

- `AppUpdate.Update(model, message)` — reine Übergangsfunktion; relevante
  Verzweigungen: `StartupRequestedMessage` (Effekte `InitializeStorage`,
  `LoadSettings`, `LoadServers`), `ServersLoadedMessage` (→
  `CheckServerHealthEffect` beim Start), `ServerHealthCheckCompletedMessage`
  (erreichbar → `AttemptAutoLoginEffect`, sonst `CheckOfflineDataEffect`),
  `AutoLoginCompletedMessage`/`LoginCompletedMessage` (Erfolg →
  `Connected` + `NavigateToPageEffect(Home)` — **navigiert aktuell
  immer** auf `Home`, ungeeignet für eine Wiederverbindung zur Laufzeit),
  `OfflineDataCheckCompletedMessage` (Offline → `Home`, sonst
  `ServerSetup` — ebenfalls immer navigierend), `SessionExpiredMessage`
  (→ `NeedsLogin`/`Login`).
- `MvuHost` — serialisierte Dispatch-Verarbeitung, Effekte nacheinander
  über `IAppEffectHandler`.
- `AuthSessionService` — `TryStoredLoginAsync`/`LoginAsync` erzeugen den
  angemeldeten `VideoPlayerApiClient` (`Client`, `ClientBaseAddress`,
  `CurrentToken` für Bearer), verdrahten `TokenRefresher` für die stille
  Neuanmeldung; `SessionExpired`-Event bei endgültiger Ablehnung.
- `AppEffect`-Bestand: Navigation, Health-Check, Storage-Init,
  Settings, Serververwaltung, Auto-/manuelle Anmeldung, Offline-Daten-
  Prüfung. **Fehlt:** Netzwerk-Monitoring, Ereignisverbindung,
  Synchronisation.

## App-Rand (`VideoPlayer.Maui`)

- `ShellAppEffectHandler` — führt alle Effekte aus, dispatcht Ergebnis-
  Messages über `MvuHost` auf dem `MainThread`; abonniert
  `authSession.SessionExpired` im Konstruktor (Muster für weitere
  Event-Verdrahtungen). Löst Dienste lazy über `IServiceProvider` auf.
- `ServerHealthProbe` (`IServerHealthProbe`) — `CheckAsync()` /
  `CheckAsync(Uri)` über kurzlebigen `VideoPlayerApiClient`
  (`GET api/health`, 10 s Timeout).
- `App.xaml.cs` — dispatcht `StartupRequestedMessage` in
  `CreateWindow`; kein Lifecycle-Handling für Konnektivität.
- `MauiProgram` — DI: Singletons für MVU, Storage, Serververwaltung;
  Transients für Pages. **Fehlt:** SignalR-Paket, Netzwerkmonitor,
  Sync-/Ereignis-Dienste.
- `HomePage.xaml` — `OfflineBanner` (vorhanden, `IsVisible` via
  `UpdateUi(model)` ↔ `model.IsOffline`); `ModelChanged`-Subscription
  auf `MvuHost` in `OnAppearing`/`OnDisappearing`.

## Altbestand (`VideoWebPlayer.Maui`) — Referenzimplementierung

- `SignalRService` (`Services/SignalRService.cs`): `HubConnectionBuilder`
  auf `{server}/hubs/mediaupdate` mit `AccessTokenProvider` +
  `WithAutomaticReconnect()`; `.On<...>`-Handler für die vier Hub-
  Methoden; `Closed`-Handler startet nach 5 s manuell neu; `ConnectAsync`
  ist idempotent (`_isConnected`-Flag + `SemaphoreSlim`).
- `NotificationEventService` (`Services/Events/`): Pub/Sub-Übersetzung
  der SignalR-Events in `NotificationEvent`-Subtypen.
- `App.xaml.cs` (Altbestand): initialisiert SignalR nach Login;
  **keine** `Connectivity`-Nutzung im gesamten Altbestand.

## API-Client (`VideoPlayer.ApiClient`)

Vorhanden und direkt nutzbar (`VideoPlayerApiClient`):
`CheckHealthAsync`, `GetSourcesAsync`, `GetRecentEntriesAsync`,
`GetContinueWatchingAsync`, `ReportPlaybackProgressAsync(mediaType,
mediaId, positionSeconds, durationSeconds)`, `GetFavoritesAsync`;
Retry-Strategie (1/2/4 s) und 401-Token-Refresh über `TokenRefresher`.

## Server-API-Vertrag (Server-Repo, `docs/API.md` + Controller)

- `POST api/continue-watching/progress`: `mediaType` (`movie`/`episode`/
  `tvshowepisode`), `mediaId` (long), `positionSeconds`,
  `durationSeconds`; Antwort `204`.
- `GET api/continue-watching`, `GET api/favorites`, `GET api/Sources`,
  `GET api/items/recent` für den Metadaten-Sync.

## Fehlende Bausteine (für diese Anforderung neu)

- Netzwerkmonitor (Abstraktion + `Connectivity`-Implementierung)
- Ereignisverbindung (Abstraktion + SignalR-Implementierung) inkl.
  Orchestrierung (Aufbau nach Login, Abbau bei Offline, Wiederaufbau)
- Synchronisationsdienst (Metadaten-Sync, Weiterschauen-Refresh,
  Pending-Progress-Drain)
- Warteschlangen-Service für `pending_progress` inkl. Einreihen beim
  Senden (Offline/Fehler) und Spaltenmigration `DurationTicks`
