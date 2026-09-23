# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Trennen und Wiederherstellen der Netzwerkverbindung wird zuverlässig erkannt | `INetworkStatusMonitor`/`ConnectivityNetworkStatusMonitor` → `NetworkAvailabilityChangedMessage` → `AppUpdate`-Zweige (Offline-Übergang aus `Connected`, Reconnect aus `Offline`) | `AppUpdateTests_NetworkSync` (`NetworkLost_*`, `NetworkRestored_*`) | Abgedeckt |
| Nach Online-Rückkehr laufen Wiederverbindung und Synchronisation automatisch ab | `Offline`→`CheckServerHealthEffect`→`AttemptAutoLoginEffect`→`ConnectServerEventsEffect`+`RunFullSyncEffect`; Ereignisverbindung inklusive | `AutoLoginSucceeded_*`, `LoginSucceeded_*`, `ServerSyncServiceTests`, `ServerEventsServiceTests` | Abgedeckt |
| Zuvor offline erfasste Fortschritte erreichen den Server ohne Verlust | `pending_progress`-Warteschlange (`PendingProgressSyncService`, backup-pflichtig, `DurationTicks`-Migration), `ProgressReportService`-Einreihung, Drain in `ServerSyncService` (chronologisch, coalesciert, Abbruch bei Transportfehler, Verwurf bei 4xx) | `PendingProgressSyncServiceTests` (inkl. Neustart-Persistenz), `ProgressReportServiceTests`, `ServerSyncServiceTests`, Migrationstest | Abgedeckt |
| Vom Server empfangene Ereignisse aktualisieren die betroffenen Anzeigen ohne manuelle Aktualisierung | `IServerEventsConnection`/`SignalRServerEventsConnection` (Hub `hubs/mediaupdate`, `WithAutomaticReconnect`) → `ServerEventReceivedMessage` → Teil-Sync-Effekte aktualisieren die Caches, die die Anzeigen speisen; `Closed` → Reconnect-Effekt | `ServerEventReceived_*`, `ServerEventsClosed_*`, `ServerEventsServiceTests` | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Alle vier Akzeptanzkriterien | Kein E2E-Framework im Repository (kein FlaUI/Appium/Playwright); Netzwerkwechsel sind keine per UI auslösbare Aktion — Funktionsnachweise über Message-Sequenz-Tests (`AppUpdate`/`MvuHost`), Service-Tests mit `StubHttpMessageHandler`/realer SQLite-Ablage, Contract-Tests der MAUI-Verdrahtung und Windows-Build | Nicht erforderlich mit Begründung (gleiche Feststellung wie Schritte 4/5) |

## Hinweise

- Der Plan nutzt den nachgewiesenen Server-Hub (`/hubs/mediaupdate`,
  JWT-authentifiziert) — kein Server-Issue erforderlich.
- Betroffene Bestandstests (`AppUpdateTests_Startup`,
  `AppUpdateTests_Connection`) sind explizit benannt.
