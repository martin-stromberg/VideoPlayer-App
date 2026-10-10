# Enums

## `ConnectionState`

Datei: `VideoPlayer.Mvu/ConnectionState.cs`

| Wert | Bedeutung |
|------|-----------|
| `Unknown` | Startzustand vor Beginn des Verbindungs-Workflows |
| `Connecting` | Verbindungsaufbau läuft (Erreichbarkeitsprüfung, automatische Anmeldung) |
| `Connected` | Mit dem Server verbunden und angemeldet |
| `NeedsLogin` | Server erreichbar, Anmeldung erforderlich |
| `NeedsServerSetup` | Kein Server hinterlegt bzw. kein Offline-Betrieb möglich |
| `Offline` | Server nicht erreichbar; lokale Offline-Daten vorhanden |

Einziger Übergang in `Connected`: `AppUpdate.EnterConnected` (aus `UpdateAutoLoginCompleted`/`UpdateLoginCompleted`). Der Weg aus `Offline` führt über `NetworkAvailabilityChangedMessage(true)` → `CheckServerHealthEffect` → `ServerHealthCheckCompletedMessage` → `AttemptAutoLoginEffect` → `AutoLoginCompletedMessage(Succeeded)` → `EnterConnected`.

## `AuthAttemptStatus` (Kontext der Reconnect-Nachricht)

Verwendet von `AutoLoginCompletedMessage`/`LoginCompletedMessage`; relevant: `Succeeded` (→ `EnterConnected`), `GateRejected` (→ Re-Pairing), `MissingCredentials`, `ServerUnreachable` (→ Anmeldemaske/Fehlerpfade).
