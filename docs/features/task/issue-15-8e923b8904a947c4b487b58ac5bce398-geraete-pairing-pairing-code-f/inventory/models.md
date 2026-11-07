# Datenmodell — Bestandsaufnahme

## `AppModel`
Datei: `VideoPlayer.Mvu/AppModel.cs`

Unveränderliches Gesamtmodell (MVU-Record). Für den Pairing-Flow relevante Eigenschaften:

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `CurrentPage` | `AppPage` | Aktuell angezeigte Seite; Startwert `Loading`. |
| `IsStartupRunning` | `bool` | Start-Workflow läuft (Startwert `true`). |
| `ServerHealth` | `ServerHealthStatus` | Ergebnis der Erreichbarkeitsprüfung. |
| `ConnectionState` | `ConnectionState` | Verbindungszustand, steuert Navigation; Startwert `Connecting`. Kein Pairing-spezifischer Zustand vorhanden. |
| `IsLoginRunning` | `bool` | Anmeldeversuch läuft. |
| `LoginError` | `string?` | Inline-Fehlertext der Anmeldemaske. |
| `LoginUsername` | `string?` | Bekannter Benutzername zur Vorbefüllung. |
| `HasOfflineData` | `bool` | Lokale Offline-Daten vorhanden. |
| `IsOffline` | `bool` | Abgeleitet: `ConnectionState == Offline`. |
| `IsOnline` | `bool` | Letzter bekannter Netzwerkstatus. |
| `IsSyncRunning` | `bool` | Vollständige Synchronisation läuft. |
| `Settings` | `AppSettings` | Geladene App-Einstellungen. |
| `KnownServers` | `IReadOnlyList<KnownServer>` | Bekannte Server (backup-pflichtig). |
| `ActiveServerAddress` | `string?` | Kanonische Adresse des aktiven Servers; `null` = Erststart-Einrichtung. |
| `DiscoveredServers` | `IReadOnlyList<DiscoveredServer>` | Gefundene Server-Kandidaten (mDNS/UDP). |
| `IsDiscoveryRunning` | `bool` | Netzwerk-Erkennung läuft. |
| `DiscoveryAttempted` | `bool` | Erkennung wurde mindestens einmal ausgeführt. |
| `IsServerCheckRunning` | `bool` | Verbindungsprüfung eines Kandidaten läuft (treibt `CheckIndicator`/`ConnectButton` der `ServerSetupPage`). |
| `ServerSetupError` | `string?` | Inline-Fehlertext der Server-Einrichtung (`InvalidAddressError`/`UnreachableServerError`); `null` = kein Fehler. |
| `ServerHealthText` / `StartupStatusText` | `string` | Abgeleitete Anzeigetexte. |
| `StorageNotice` | `string?` | Hinweis nach Ablage-Wiederherstellung. |

**Keine** Pairing-Felder vorhanden (kein `IsPairingRunning`, `PairingError`, kein „gekoppelt"-Status).

## `KnownServer`
Datei: `VideoPlayer.Storage/KnownServer.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Address` | `string` (required) | Kanonische Serveradresse (`ServerAddress.Normalize`); Vergleichs-/Speicherschlüssel in `known_servers`. |
| `DisplayName` | `string` | Anzeigename; leer, wenn keiner vergeben. |
| `LastUsedAtUtc` | `DateTime` | Letzte Nutzung (UTC). |
| `NameOrAddress` | `string` | Abgeleitet: Anzeigename oder Adresse. |

## `AuthToken`
Datei: `VideoPlayer.ApiClient/Models/AuthToken.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Token` | `string` | Bearer-Zugangstoken (Benutzer-JWT), Wire-Feld `token`. |
| `Expires` | `DateTime` | Ablaufzeitpunkt, Wire-Feld `expires`. |

Hinweis: Das **Geräte-Token** der Anforderung ist davon getrennt — es wandert in den `X-API-Key`-Header (Gate), nicht in den Bearer-Header.

## `LoginRequest`
Datei: `VideoPlayer.ApiClient/Models/LoginRequest.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Email` | `string` | E-Mail des Benutzers (`POST api/auth/login`). |
| `Password` | `string` | Passwort. |

Serialisierung erfolgt über `System.Text.Json` mit `PropertyNameCaseInsensitive` (`VideoPlayerApiClient.JsonOptions`); Request-Payloads werden mit CLR-Eigenschaftsnamen (PascalCase) serialisiert — der verbindlich camelCase-kodierte Pairing-Vertrag benötigt entsprechende `JsonPropertyName`-Attribute oder eigene DTOs.

## `AuthAttempt` (Ergebnis-Record)
Datei: `VideoPlayer.Mvu/AuthAttempt.cs`

| Member | Typ | Beschreibung / Zweck |
|--------|-----|----------------------|
| `Status` | `AuthAttemptStatus` | Ergebnis des Anmeldeversuchs (Enum, siehe `enums.md`). |
| `Username` | `string?` | Bekannter/eingegebener Benutzername zur Vorbefüllung. |
| `Succeeded` | `bool` | Abgeleitet: `Status == Succeeded`. |

Analoges Muster für ein Pairing-Ergebnis existiert noch nicht.

## `DiscoveredServer`
Datei: `VideoPlayer.ApiClient/Discovery/DiscoveredServer.cs`

Wird von der `ServerSetupPage` (Kandidatenliste) und `ServerDiscoveryCompletedMessage` genutzt (`Address`, `Name`, `NameOrAddress`).

## Nicht vorhandene Modelle (Fehlstellen)

- **`PairingExchange*`-DTOs:** In `VideoWebPlayer.Client/Models/` dieses Repos existieren keine Pairing-Modelle — vorhanden sind nur `AuthenticationRequest`, `AuthorizationToken`, `ContinueWatchingDto`, `ContinueWatchingMutationResult`, `DtoMovie`, `DtoRecentEntry`, `DtoSource`, `FavoriteEntry`, `ImpersonateRequest`, `MediaEntryDto`. Die serverseitigen DTOs liegen laut Anforderung im Server-Repo; eine Übernahme/Synchronisation ist dort nicht geschehen (offene Frage der Anforderung bestätigt sich im Code).
- **Pairing-Request (`code`, `clientPublicKey`, `deviceName`) / -Response (`serverPublicKey`, `encryptedToken`):** nicht vorhanden — weder in `VideoPlayer.ApiClient/Models/` noch in `VideoWebPlayer.Client/Models/`.
