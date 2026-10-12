# Enums

## `AuthAttemptStatus`
Datei: `VideoPlayer.Mvu/AuthAttempt.cs`

Ergebnis-Status der Anmeldeversuche (`AuthSessionService`).

| Wert | Bedeutung |
|------|-----------|
| `Succeeded` | Anmeldung erfolgreich |
| `MissingCredentials` | Weder Token noch Zugangsdaten gespeichert |
| `InvalidCredentials` | Zugangsdaten vom Server abgelehnt |
| `ServerUnreachable` | Transport-/Serverfehler |
| `GateRejected` | Client-Gate (Geräte-Token) abgelehnt → Re-Pairing |
| `Failed` | Sonstiger API-Fehlschlag |

## `PlaybackSourceKind`
Datei: `VideoPlayer.Mvu/PlaybackSourceKind.cs`

| Wert | Bedeutung |
|------|-----------|
| `ServerStream` | Server-Stream-URL `api/items/{typ}/{id}/stream` (mit `access_token`-Query — Geheimnis) |
| `LocalFile` | Lokale Datei aus dem Download-Manifest (Offline-Wiedergabe) |

## `DownloadState`
Datei: `VideoPlayer.Mvu/DownloadState.cs`

| Wert | Bedeutung |
|------|-----------|
| `Queued` | Wartet auf einen Transfer-Platz |
| `Downloading` | Transfer läuft |
| `Completed` | Abgeschlossen (Datei vorhanden) |
| `Failed` | Fehlgeschlagen (Fehlercode in `Error*`-Konstanten) |
| `Cancelled` | Abgebrochen |

Hilfsklasse `DownloadStateNames` übersetzt `ToPersisted`/`FromPersisted` (Kleinbuchstaben-Strings im Manifest).

## `ServerEventKind`
Datei: `VideoPlayer.Mvu/ServerEventKind.cs`

| Wert | Bedeutung |
|------|-----------|
| `ContinueWatchingUpdated` | Weiterschauen-Liste serverseitig geändert |
| `FavoritesChanged` | Favoriten serverseitig geändert |
| `NewVideosScanned` | Neue Inhalte eingescannt (mit `SourceId`/`Count` in `ServerEventReceivedEventArgs`) |

## `ConnectionState`
Datei: `VideoPlayer.Mvu/ConnectionState.cs`

| Wert | Bedeutung |
|------|-----------|
| `Unknown` | Startzustand |
| `Connecting` | Erreichbarkeitsprüfung läuft |
| `Connected` | Verbunden/angemeldet |
| `NeedsLogin` | Anmeldung erforderlich |
| `NeedsServerSetup` | Kein/kein erreichbarer Server konfiguriert |
| `Offline` | Kein Netz — Offline-Betrieb |
