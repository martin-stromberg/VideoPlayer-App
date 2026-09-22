# Bestandsaufnahme: Netzwerkstatus-Erkennung und Synchronisation

Analyse des neuen App-Bestands (`VideoPlayer.Mvu`, `VideoPlayer.Maui`,
`VideoPlayer.Storage`, `VideoPlayer.ApiClient`), des Altbestands
(`VideoWebPlayer.Maui`) sowie des Server-Repositorys
(`martin-stromberg/VideoPlayer`, Branch `main`) bezogen auf die Anforderung
„Netzwerkstatus-Erkennung und Synchronisation".

## Zusammenfassung

- **Netzwerkstatus:** Im neuen App-Bestand wird `Microsoft.Maui.Networking.
  Connectivity` noch nirgends genutzt; es gibt weder eine Abstraktion noch
  eine Laufzeit-Erkennung von Online↔Offline-Wechseln. Der Offline-Zustand
  existiert nur als Ergebnis des Start-Workflows
  (`ConnectionState.Offline` nach fehlgeschlagener Erreichbarkeitsprüfung).
- **Wiederverbindung:** Nicht vorhanden — ein Offline-Wechsel zur Laufzeit
  wird nicht erkannt, eine Online-Rückkehr löst keinerlei Aktion aus. Die
  Start-Kette (`CheckServerHealthEffect` → `AttemptAutoLoginEffect` →
  `Connected`) ist vorhanden und für die Wiederverbindung wiederverwendbar.
- **Echtzeit-Verbindung:** Der Server bietet den SignalR-Hub
  `hubs/mediaupdate` nachweislich an (`VideoWebPlayer/Hubs/
  MediaUpdateHub.cs`, gemappt und JWT-pflichtig in
  `Extensions/WebApplicationExtensions.cs` — `RequireAuthorization` mit
  `JwtBearer`; der JWT-Bearer-Handler liest das Token bei WebSockets aus
  dem `access_token`-Query-Parameter). Hub-Ereignisse:
  `ContinueWatchingUpdated`, `FavoritesChanged`,
  `NewVideosScanned(sourceId, count)`, `StatusChanged(message)`
  (`MediaUpdateNotificationService`). Der Altbestand besitzt dazu den
  `SignalRService` (`Microsoft.AspNetCore.SignalR.Client`,
  `WithAutomaticReconnect()`, manueller Restart 5 s nach `Closed`) und den
  `NotificationEventService` (Pub/Sub, Übersetzung in `NotificationEvent`s
  wie `ContinueWatchingUpdatedEvent`, `FavoritesChangedEvent`,
  `NewVideosScannedEvent`). Im neuen Bestand fehlt beides vollständig —
  kein Server-Issue nötig.
- **Fortschritts-Warteschlange:** Die Tabelle `pending_progress`
  (`PendingProgressSyncEntry`: `Id`, `VideoId`, `VideoType`,
  `PositionTicks`, `UpdatedAtUtc`) ist in der backup-pflichtigen
  `BackupDatabase` angelegt, aber ungenutzt — weder wird eingereiht noch
  nachgesendet. **Lücke:** Der Server-Endpunkt
  `POST api/continue-watching/progress` verlangt `mediaType`, `mediaId`,
  `positionSeconds`, `durationSeconds` — die Entität enthält keine
  Videodauer und muss um `DurationTicks` erweitert werden (Migration in
  `BackupDatabase.InitializeAsync`, `sqlite-net` erweitert bestehende
  Tabellen nicht automatisch). `VideoId` (`int`) passt in `mediaId`
  (`long`).
- **Metadaten-Cache:** `CacheDatabase` besitzt die Tabellen
  `cached_lists` (Key/Json/UpdatedAtUtc — für Startseiten-Metadaten und
  Episodenlisten vorgesehen) und `cached_favorites` (Key/VideoId/
  VideoType/Json), beide bislang unbeschrieben. Es existiert kein
  Synchronisationsdienst.
- **Offline-Anzeige:** `HomePage.xaml` enthält bereits ein
  `OfflineBanner` (AutomationId `OfflineBanner`), das über
  `model.IsOffline` in `HomePage.UpdateUi` gesteuert wird — wird mit dem
  Laufzeit-Zustandswechsel automatisch mitaktualisiert.

Test-Ausgangszustand: 280/280 Tests bestanden (Baseline-Lauf, siehe
[inventory/tests.md](inventory/tests.md)). Kein E2E-/UI-Automatisierungs-
Framework im Repository; Benutzerfluss-Nachweise erfolgen über
MVU-Transitions- und Contract-Tests.

## Details

- [Modelle und Ablage](inventory/models.md)
- [Logik und Dienste](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Interfaces und Abstraktionen](inventory/interfaces.md)
- [Tests und Test-Ausgangszustand](inventory/tests.md)
