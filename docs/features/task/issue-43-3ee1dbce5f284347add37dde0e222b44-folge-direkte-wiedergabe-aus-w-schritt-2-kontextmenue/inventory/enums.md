# Bestandsaufnahme: Enums

## `ServerEventKind`
Datei: `VideoPlayer.Mvu/ServerEventKind.cs`

| Wert | Bedeutung |
|------|-----------|
| `ContinueWatchingUpdated` | Weiterschauen-Liste wurde aktualisiert → `SyncContinueWatchingEffect`. |
| `FavoritesChanged` | Favoriten geändert → `SyncFavoritesEffect`. |
| `NewVideosScanned` | Neue Inhalte → `SyncMetadataEffect`. |

## `ConnectionState`
Datei: `VideoPlayer.Mvu/ConnectionState.cs`

Zustände `Connecting`, `Connected`, `NeedsLogin`, `NeedsServerSetup`, `Offline` — steuert Online-/Offline-Verzweigung der Mutation (offline: kein `authSession.Client`).

## Hinweis zur Enum-Abdeckung

`EnumCoverageTests` prüft derzeit nur die explizit gelisteten Enums (`RecentEntryKind`, `PlaybackSourceKind`, `HealthCheckResult`, `PairingStatus`, `RecentEntryType`, Legacy-Enums). Ein neuer Enum für die Menüaktion (Hide/Skip) wäre nur dann abdeckungspflichtig, wenn er in die Coverage-Liste aufgenommen würde — alternativ können die beiden Aktionen über getrennte Nachrichten/Effekte (Muster `FavoriteRemoveRequestedMessage` → `RemoveFavoriteEffect`) statt über einen Aktions-Enum modelliert werden.
