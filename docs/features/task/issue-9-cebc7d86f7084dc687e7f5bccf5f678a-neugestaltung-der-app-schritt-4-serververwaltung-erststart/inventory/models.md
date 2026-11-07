# Datenmodell — Bestandsaufnahme

## `KnownServerEntry`

Datei: `VideoPlayer.Storage/Entities/KnownServerEntry.cs`

Backup-pflichtige Entität (Tabelle `known_servers` in `videoplayer.db3`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `int` | `[PrimaryKey, AutoIncrement]` |
| `Address` | `string` | Serveradresse, `[Indexed(Unique = true)]` |
| `DisplayName` | `string` | Anzeigename |
| `LastUsedAtUtc` | `DateTime` | Zeitpunkt der letzten Nutzung |

Wird angelegt von `BackupDatabase.InitializeAsync`. Es existiert **keine**
Zugriffsschicht (kein Service liest/schreibt `KnownServerEntry`).

## `BackupStateEntry`

Datei: `VideoPlayer.Storage/Entities/BackupStateEntry.cs`

Backup-pflichtige Schlüssel/Wert-Tabelle (`backup_state`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Key` | `string` | `[PrimaryKey]` |
| `Value` | `string` | Wert |

Konstante `LastConnectedServerKey = "LastConnectedServer"` ist bereits
definiert — für den zuletzt verbundenen Server vorgesehen, noch ungenutzt.

## `AppModel` (`VideoPlayer.Mvu/AppModel.cs`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `CurrentPage` | `AppPage` | aktuell angezeigte Seite (Startwert `Loading`) |
| `IsStartupRunning` | `bool` | Start-Workflow läuft |
| `ServerHealth` | `ServerHealthStatus` | Ergebnis der Erreichbarkeitsprüfung |
| `Settings` | `AppSettings` | geladene Einstellungen (Defaults bis `SettingsLoadedMessage`) |
| `ServerHealthText` | `string` (berechnet) | anzeigbarer Serverstatus-Text |

Nicht vorhanden: Serverliste, „Server hinterlegt?"-Zustand, Discovery-
Zwischenergebnisse, Einrichtungs-/Wechsel-Zustand.

## `AppSettings` (`VideoPlayer.Storage/AppSettings.cs`)

Unveränderlicher Record der acht benutzerbezogenen Einstellungen mit
`Normalize()`. Enthält **keine** Serverbezüge — Serververwaltung ist
bewusst kein Teil von `AppSettings` (Ablage in `BackupDatabase`), der Wechsel
darf die übrigen Einstellungen nicht berühren (Akzeptanzkriterium 4).
