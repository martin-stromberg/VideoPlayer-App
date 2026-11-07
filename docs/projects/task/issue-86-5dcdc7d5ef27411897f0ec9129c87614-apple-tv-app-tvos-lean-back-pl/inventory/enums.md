# Enums — Bestandsaufnahme

Dateipfade relativ zum Repository-Root.

## `AppPage`
`VideoPlayer.Mvu/AppPage.cs` — Seiteninventar. Menüseiten sind in `AppRoutes.Routes` verdrahtet; `Player` existiert **nicht** als `AppPage` (Player ist Shell-Route `player`).

| Wert | Bedeutung |
|---|---|
| `Loading` | Start-Ladeanzeige (kein Menüeintrag) |
| `Home` | Startseite (Weiterschauen/Favoriten/Neu im Programm) |
| `Favorites` | Menüseite Favoriten |
| `Playlists` | Menüseite Playlists |
| `Downloads` | Menüseite Downloads |
| `Profile` | Menüseite Profil |
| `Settings` | Menüseite Einstellungen |
| `ServerSetup` | Servereinrichtung (Route `serversetup`, kein Menüeintrag) |
| `Login` | Anmeldemaske (Route `login`, kein Menüeintrag) |

## `DownloadState`
`VideoPlayer.Mvu/DownloadState.cs` — persistiert als Kleinschreib-Strings via `DownloadStateNames.ToPersisted`/`FromPersisted` (unbekannt → `Failed`).

| Wert | Persistiert | Bedeutung |
|---|---|---|
| `Queued` | `queued` | Wartet auf freien Platz / WLAN-Bedingung |
| `Downloading` | `downloading` | Transfer läuft |
| `Completed` | `completed` | Abgeschlossen, Datei lokal |
| `Failed` | `failed` | Fehlgeschlagen (Fehlercode im Eintrag, u. a. `missing_file`) |
| `Cancelled` | `cancelled` | Abgebrochen |

## `DownloadOrigin`
`VideoPlayer.Mvu/DownloadOrigin.cs` — persistiert via `DownloadOriginNames` (`auto`/`manual`; unbekannt/`null` → `Manual`).

| Wert | Bedeutung |
|---|---|
| `Manual` | Über Oberfläche eingeplant (Detailseite, „Erneut versuchen") |
| `Auto` | Automatischer Vorlauf / als Kandidat verlängert (Anzeige „Cache") |

## `PlaybackSourceKind`
`VideoPlayer.Mvu/PlaybackSourceKind.cs`

| Wert | Bedeutung |
|---|---|
| `ServerStream` | Server-Stream (`api/items/{typ}/{id}/stream`) |
| `LocalFile` | Lokale Datei aus dem Download-Manifest |

## `ConnectionState`
`VideoPlayer.Mvu/ConnectionState.cs`

| Wert | Bedeutung |
|---|---|
| `Unknown` | Startzustand |
| `Connecting` | Verbindungsaufbau läuft |
| `Connected` | Verbunden und angemeldet |
| `NeedsLogin` | Server erreichbar, Anmeldung erforderlich |
| `NeedsServerSetup` | Kein Server hinterlegt / kein Offline-Betrieb |
| `Offline` | Server nicht erreichbar, lokale Daten vorhanden |

## `ServerHealthStatus`
`VideoPlayer.Mvu/ServerHealthStatus.cs` — `Unknown`, `Checking`, `Reachable`, `Unreachable`.

## `ServerEventKind`
`VideoPlayer.Mvu/ServerEventKind.cs` — `ContinueWatchingUpdated`, `FavoritesChanged`, `NewVideosScanned` (zugehörig `ServerEventReceivedEventArgs`).

## `PairingStatus`
`VideoPlayer.Mvu/PairingStatus.cs` — Ergebnis `api/pairing/exchange`.

| Wert | Bedeutung |
|---|---|
| `Succeeded` | Pairing erfolgreich |
| `InvalidCode` | Code ungültig/abgelaufen/verwendet (401) |
| `RateLimited` | Versuchssperre (429) |
| `InvalidRequest` | Fachlich ungültig (400) |
| `NotSupported` | Endpunkt fehlt (404) |
| `ServerUnreachable` | Transport/Timeout |
| `Failed` | Sonstiger Fehler |

## `BootstrapStatus`
`VideoPlayer.Mvu/BootstrapStatus.cs` — Ergebnis `api/pairing/bootstrap`: `Succeeded`, `InvalidTicket`, `InvalidRequest`, `RateLimited`, `NotSupported`, `ServerUnreachable`, `Failed`.

## `BootstrapTicketKind`
`VideoPlayer.Mvu/BootstrapTicketParser.cs` — Ticket-Eingabeart (`TicketAddress`/`ShortCode` o. ä.; siehe Parser). Zugehörig `BootstrapTicketInput`.

## `AuthAttemptStatus`
`VideoPlayer.Mvu/AuthAttempt.cs`

| Wert | Bedeutung |
|---|---|
| `Succeeded` | Anmeldung erfolgreich |
| `MissingCredentials` | Keine gespeicherten Zugangsdaten |
| `InvalidCredentials` | Abgelehnt (401/403) |
| `ServerUnreachable` | Transportfehler |
| `GateRejected` | Client-Gate/Geräte-Token abgelehnt (401-Probe) |
| `Failed` | Sonstiger Fehler |

## `DiscoverySource`
`VideoPlayer.ApiClient/Discovery/DiscoverySource.cs` — `Mdns` (`_http._tcp.local.`), `Udp` (Broadcast Port 5001).

## `RecentEntryKind`
`VideoPlayer.ApiClient/Models/RecentEntry.cs` — `Movie`, `MovieCollection`, `TvShow`, `TvShowSeason`, `TvShowEpisode`.

## Keine Enums, aber diskriminierte Record-Hierarchien
- `AppMessage` (`VideoPlayer.Mvu/AppMessage.cs`): abstrakter Basistyp, 135 konkrete `sealed record`-Messages (siehe logic.md → `AppUpdate`).
- `AppEffect` (`VideoPlayer.Mvu/AppEffect.cs`): abstrakter Basistyp, ~75 konkrete `sealed record`-Effekte (siehe logic.md → `ShellAppEffectHandler`).
