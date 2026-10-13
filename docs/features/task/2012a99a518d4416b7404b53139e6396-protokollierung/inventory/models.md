# Detail: Datenmodell / Einstellungen (Protokollierung)

## `AppSettings`
Datei: `VideoPlayer.Storage/AppSettings.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `LoggingEnabled` | `bool` | Schalter „Protokollierung" (Standard `false`); persistent über `AppSettingsService` (`LoggingEnabledKey` = `"LoggingEnabled"`, Profil-Präfix über `ISettingsStore`) |

## `AppStorageLayout`
Datei: `VideoPlayer.Storage/AppStorageLayout.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `LogsDirectory` | `string` | Nicht backup-pflichtiges Protokollverzeichnis (`logs` unterhalb von `NoBackupDirectory`, profil-isoliert) — Ziel von `videoplayer.log` / `videoplayer.previous.log` |

Erzeugt über `AppStorageLayoutProvider.CreateLayout(AppProfile)` (`VideoPlayer.Maui/Services/AppStorageLayoutProvider.cs`), registriert als Singleton in `MauiProgram`.

## `DevApiModelSnapshot` (nur Debug/DEV_API)
Datei: `VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `SettingsLoggingEnabled` | `bool` | Spiegelt `model.Settings.LoggingEnabled` für E2E-Beobachtbarkeit |
