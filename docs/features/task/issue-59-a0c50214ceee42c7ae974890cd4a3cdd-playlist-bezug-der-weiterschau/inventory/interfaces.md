# Interfaces

Betroffene Contracts der Anforderung. Pfade relativ zum Repository-Root.

## `IAppEffectHandler`
Datei: `VideoPlayer.Mvu/IAppEffectHandler.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HandleAsync` | `AppEffect effect`, `CancellationToken cancellationToken = default` | `Task` | Führt einen einzelnen MVU-Effekt am App-Rand aus (Navigation, Server-Aufrufe). Einzige Methode des Contracts — implementiert von `ShellAppEffectHandler` (VideoPlayer.Maui); der MVU-Kern bleibt plattformneutral. Der `ReportPlaybackProgressEffect` läuft als Daten-Record über diesen Contract, die Signatur selbst ist nicht betroffen |

## `INetworkStatusMonitor`
Datei: `VideoPlayer.Mvu/INetworkStatusMonitor.cs`

| Methode / Member | Parameter | Rückgabewert | Zweck |
|------------------|-----------|--------------|-------|
| `IsOnline` | — | `bool` | Letzter bekannter Netzwerkstatus; steuert in `ProgressReportService.ReportProgressAsync` den Direktversand vs. Einreihen (Z. 69) |
| `IsWifi` | — | `bool` | Ungedrosseltes Festnetz-Profil (Download-Einstellung; nicht betroffen) |
| `AvailabilityChanged` | — | `event EventHandler<bool>?` | Wechsel-Meldung des Netzwerkstatus (abonniert vom `ShellAppEffectHandler` → `NetworkAvailabilityChangedMessage`) |
| `Start` | — | `void` | Startet die Überwachung (idempotent) |

## `AuthSessionService` (kein Interface — relevanter Zugriffspunkt)
Datei: `VideoPlayer.Mvu` (Klasse, verwendet von `ProgressReportService`, `ServerSyncService`, `PlaylistService`)

| Member | Zweck |
|--------|-------|
| `Client` | Liefert den angemeldeten `VideoPlayerApiClient` bzw. `null` — Gate zwischen Direktversand und Warteschlange |
| `SessionExpired` | Event: endgültig abgelehnte Neuanmeldung → App wechselt in Anmeldezustand |
| `LoginAsync` / `LogoutAsync` | Anmelde-Workflow |

Es existiert kein eigenes Interface für die Fortschrittsmeldung (`ProgressReportService`, `PendingProgressSyncService`, `ServerSyncService` sind konkrete Klassen ohne Contract-Interface); die Methodensignaturen der konkreten Klassen bilden die zu erweiternden Aufrufpunkte (siehe [Logik](logic.md)).
