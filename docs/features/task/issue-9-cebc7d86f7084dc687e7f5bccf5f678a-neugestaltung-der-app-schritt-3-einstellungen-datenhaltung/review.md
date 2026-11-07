# Plan-Review

Geprüft: `plan.md` gegen die Implementierung (Selbstprüfung ohne Unteragenten —
dokumentierte Abweichung, es stehen keine Unteragenten zur Verfügung).
Alle Planelemente wurden im Code gesucht und vollständig gelesen.

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Projekt `VideoPlayer.Storage` (net10.0, neu)

- [x] `VideoPlayer.Storage.csproj` — `net10.0`, `ImplicitUsings`, `Nullable`,
  Lizenz-Metadaten wie `VideoPlayer.Mvu`; Pakete `sqlite-net-pcl` 1.11.285,
  `SQLitePCLRaw.bundle_green` 2.1.11, `SQLitePCLRaw.lib.e_sqlite3` 3.53.3 und
  `SQLitePCLRaw.lib.e_sqlite3.android` 2.1.13 (Pins gegen bekannte
  SQLite-Advisory-Warnungen, Versionen analog Alt-App).
- [x] `ISettingsStore` — `Contains`, `GetString`/`GetInt`/`GetBool`,
  `Set`-Overloads, `Remove`.
- [x] `ISecretStore` — `GetAsync`, `SetAsync`, `RemoveAsync`.
- [x] `AppProfile` — `VIDEOPLAYER_PROFILE` (injizierbarer Env-Accessor),
  `DefaultName`, `PreferencesPrefix`, `DirectoryName`, `PrefixKey`,
  `FromEnvironment`, `Create` (Kleinschreibung, Fallback `default`).
- [x] `AppSettings` — record mit allen acht Einstellungen, Defaults inkl.
  `DefaultDownloadQuotaGb = 5`, `Normalize()` mit Schranken
  (1–10 / 30–3600 / 30–3600 / 1–500).
- [x] `AppSettingsService` — `Load()` (Defaults + `Normalize`),
  `Save()` (`Normalize` vor dem Schreiben), Schlüsselkonstanten,
  Profil-Präfix über `AppProfile.PrefixKey`.
- [x] `AppSecretsService` — `GetUsername`, `GetPasswordAsync`,
  `HasCredentialsAsync`, `SetCredentialsAsync`, `ClearCredentialsAsync`,
  `GetAuthTokenAsync`, `SetAuthTokenAsync`, `ClearAuthTokenAsync`;
  `Passkey`/`AuthToken` ausschließlich über `ISecretStore` inkl.
  Legacy-Migration aus der normalen Ablage.
- [x] `AppStorageLayout` — `BackupDirectory`, `DataDatabasePath`,
  `NoBackupDirectory`, `CacheDatabasePath`, `VideosDirectory`,
  `PreviewsDirectory`, `TempDirectory`, `EnsureDirectoriesExist()`,
  Profil-Scoping.
- [x] `SqliteBootstrap` — einmalige `SQLitePCL.Batteries_V2.Init()`.
- [x] `BackupDatabase` (`videoplayer.db3`) — Tabellen `KnownServerEntry`,
  `BackupStateEntry`, `PendingProgressSyncEntry`.
- [x] `CacheDatabase` (`videoplayer-cache.db3`) — Tabellen `CachedListEntry`,
  `CachedFavoritesEntry`, `DownloadManifestEntry`.
- [x] Entitäten `Entities/`: `KnownServerEntry`, `BackupStateEntry`,
  `PendingProgressSyncEntry`, `CachedListEntry`, `CachedFavoritesEntry`,
  `DownloadManifestEntry`.

### Projekt `VideoPlayer.Mvu` (erweitert)

- [x] `AppModel.Settings` (`AppSettings`, Default `new()`).
- [x] Messages: `LoadSettingsRequestedMessage`, `SettingsLoadedMessage`,
  `SaveSettingsRequestedMessage`.
- [x] Effekte: `InitializeStorageEffect`, `LoadSettingsEffect`,
  `PersistSettingsEffect(AppSettings)`.
- [x] `AppUpdate`: Startup-Effekte `[InitializeStorage, LoadSettings,
  CheckServerHealth]`; `LoadSettingsRequested` → `LoadSettingsEffect`;
  `SettingsLoaded` → `Model.Settings` normalisiert; `SaveSettingsRequested`
  → `Model.Settings` normalisiert + `PersistSettingsEffect`.
- [x] `VideoPlayer.Mvu.csproj` — `ProjectReference` auf `VideoPlayer.Storage`.

### Projekt `VideoPlayer.Maui`

- [x] `PreferencesSettingsStore` — `ISettingsStore`-Adapter auf `IPreferences`.
- [x] `SecureStorageSecretStore` — `ISecretStore`-Adapter auf `ISecureStorage`;
  Lese-Fehler werden als „leer" behandelt, leere Werte entfernen den Schlüssel.
- [x] `AppStorageLayoutProvider` — Windows: `%APPDATA%\VideoPlayer` (Roaming)
  als Backup-Wurzel; iOS/MacCatalyst: `AppDataDirectory/nobackup` +
  `NSFileManager.SetSkipBackupAttribute`; Android: `NoBackupFilesDir`;
  Temp über `FileSystem.CacheDirectory`.
- [x] `MauiProgram` — DI-Registrierungen: `AppProfile.FromEnvironment`,
  `ISettingsStore`, `ISecretStore`, `AppSettingsService`, `AppSecretsService`,
  `AppStorageLayout` (via Provider), `BackupDatabase`, `CacheDatabase`.
- [x] `ShellAppEffectHandler` — neue Zweige für `InitializeStorageEffect`
  (Layout-Auflösung + beide `InitializeAsync`), `LoadSettingsEffect`
  (Load + `SettingsLoadedMessage`-Dispatch auf dem UI-Thread) und
  `PersistSettingsEffect` (`Save`); Fehler werden abgefangen und
  protokolliert, der Start blockiert nicht.
- [x] `ServerAddressProvider` — liest über `ISettingsStore` mit
  `profile.PrefixKey("ServerAddress")`; Env-/Default-Fallback unverändert;
  kein direkter `Preferences`-Zugriff mehr.
- [x] `SettingsPage.xaml` — acht Einstellungen mit deutschen
  Klartext-Beschriftungen, `x:Name`/`AutomationId` für alle Controls,
  `Keyboard="Numeric"`, Inline-Fehlerlabel `InvalidInputLabel`.
- [x] `SettingsPage.xaml.cs` — `MvuHost`-Anbindung, `OnAppearing` befüllt aus
  `Model.Settings` und abonniert `ModelChanged` (Abmeldung in
  `OnDisappearing`), `suppressEvents`-Guard, Switch-`Toggled`- und
  Entry-`Completed`/`Unfocused`-Verdrahtung, ungültige Eingaben zeigen den
  Inline-Hinweis und werden nicht gespeichert.
- [x] `VideoPlayer.Maui.csproj` — `ProjectReference` auf `VideoPlayer.Storage`.

### Solution / Tests

- [x] `VideoPlayer.App.sln` — `VideoPlayer.Storage` mit allen
  Konfigurationseinträgen aufgenommen.
- [x] `VideoWebPlayer.Maui.Tests.csproj` — `ProjectReference` auf
  `VideoPlayer.Storage`.
- [x] `Stubs/InMemorySettingsStore.cs`, `Stubs/InMemorySecretStore.cs`.
- [x] `AppSettingsTests`, `AppSettingsServiceTests`, `AppSecretsServiceTests`,
  `AppProfileTests`, `AppStorageLayoutTests`, `LocalDatabaseTests`,
  `AppUpdateTests_Settings`, `SettingsStorageContractTests` — alle geplanten
  Tests vorhanden und grün.
- [x] `AppUpdateTests_Startup` — an die neue Startup-Effektliste angepasst;
  zusätzlich `MvuHostTests` an die vier Startup-Effekte angepasst.

## Planabweichungen

- `Entry.Unfocused` verwendet `EventHandler<FocusEventArgs>` — die
  gemeinsame Verdrahtung erfolgt über einen Adapter-Delegate
  (`WireNumeric` in `SettingsPage.xaml.cs`), funktional identisch zum Plan.
- `SQLitePCLRaw.lib.e_sqlite3(.android)` wurde zusätzlich gepinnt, um die
  NuGet-Sicherheitswarnung der transitiven 2.1.11-Variante zu beseitigen.
- `ModelChanged`-Abo der `SettingsPage` wurde von Konstruktor nach
  `OnAppearing`/`OnDisappearing` verlegt (korrekte Lebensdauer bei
  wiederholtem Öffnen der Seite).

## Offene Aufgaben

—
