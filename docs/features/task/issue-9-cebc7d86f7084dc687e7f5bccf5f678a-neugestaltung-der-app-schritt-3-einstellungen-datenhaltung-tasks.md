# Tasks: Einstellungsverwaltung und lokale Datenhaltung mit Backup-Trennung

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Infrastruktur | `VideoPlayer.Storage`-Projekt (net10.0) anlegen, Pakete `sqlite-net-pcl` + `SQLitePCLRaw.bundle_green`, in `VideoPlayer.App.sln` aufnehmen | Offen | — |
| 2 | Ablage | `ISettingsStore`-Interface anlegen | Offen | — |
| 3 | Ablage | `ISecretStore`-Interface anlegen | Offen | — |
| 4 | Profil | `AppProfile` anlegen (`VIDEOPLAYER_PROFILE`, `PrefixKey`, `DirectoryName`) | Offen | — |
| 5 | Einstellungen | `AppSettings`-Record mit acht Einstellungen, `Normalize()`, `DefaultDownloadQuotaGb = 5` | Offen | — |
| 6 | Einstellungen | `AppSettingsService` (`Load`/`Save`, Profil-Präfixe, Schlüsselkonstanten) | Offen | — |
| 7 | Geheimnisse | `AppSecretsService` (Username/Passkey/AuthToken, Legacy-Bereinigung) | Offen | — |
| 8 | Ablage | `AppStorageLayout` (Backup-/NoBackup-/Temp-Wurzeln, `EnsureDirectoriesExist`) | Offen | — |
| 9 | Datenbank | `SqliteBootstrap` + Entitäten (`KnownServerEntry`, `BackupStateEntry`, `PendingProgressSyncEntry`, `CachedListEntry`, `CachedFavoritesEntry`, `DownloadManifestEntry`) | Offen | — |
| 10 | Datenbank | `BackupDatabase` und `CacheDatabase` mit `InitializeAsync` | Offen | — |
| 11 | MVU | `AppModel.Settings`-Eigenschaft ergänzen; `ProjectReference` Mvu → Storage | Offen | — |
| 12 | MVU | Messages `LoadSettingsRequestedMessage`, `SettingsLoadedMessage`, `SaveSettingsRequestedMessage` | Offen | — |
| 13 | MVU | Effekte `InitializeStorageEffect`, `LoadSettingsEffect`, `PersistSettingsEffect` | Offen | — |
| 14 | MVU | `AppUpdate`: neue Zweige + Start-Effekte erweitern | Offen | — |
| 15 | MAUI-Adapter | `PreferencesSettingsStore` (`IPreferences`-Adapter) | Offen | — |
| 16 | MAUI-Adapter | `SecureStorageSecretStore` (`ISecureStorage`-Adapter) | Offen | — |
| 17 | MAUI-Adapter | `AppStorageLayoutProvider` (Plattform-Wurzeln, iOS-Backup-Ausschluss, Windows-Roaming) | Offen | — |
| 18 | Verdrahtung | `MauiProgram`: DI-Registrierungen der neuen Dienste | Offen | — |
| 19 | Verdrahtung | `ShellAppEffectHandler`: neue Effekt-Zweige | Offen | — |
| 20 | Verdrahtung | `ServerAddressProvider` auf `ISettingsStore` + `AppProfile` umstellen | Offen | — |
| 21 | UI | `SettingsPage.xaml`: acht Einstellungen als Switch/Entry mit Beschriftung + Fehlerlabel | Offen | — |
| 22 | UI | `SettingsPage.xaml.cs`: `MvuHost`-Anbindung, Befüllung, Save-Dispatch, Validierung | Offen | — |
| 23 | Tests | Testprojekt: `ProjectReference` auf `VideoPlayer.Storage` | Offen | — |
| 24 | Tests | `Stubs/InMemorySettingsStore` + `Stubs/InMemorySecretStore` | Offen | — |
| 25 | Tests | `AppSettingsTests` (Defaults, Normalize) | Offen | — |
| 26 | Tests | `AppSettingsServiceTests` (Roundtrip, Defaults, Clamp, Profil-Isolation, Präfixe) | Offen | — |
| 27 | Tests | `AppSecretsServiceTests` (SecretStore-only, Username, Legacy, Clear, Isolation) | Offen | — |
| 28 | Tests | `AppProfileTests` (Env-Auswertung, Default, Lowercase, Präfix/Dir) | Offen | — |
| 29 | Tests | `AppStorageLayoutTests` (Trennung, Profildirs, EnsureDirectoriesExist) | Offen | — |
| 30 | Tests | `LocalDatabaseTests` (Tabellen + Roundtrips beider DBs, Ablageorte) | Offen | — |
| 31 | Tests | `AppUpdateTests_Settings` (Load/Save/Startup-Effekte) | Offen | — |
| 32 | Tests | `SettingsStorageContractTests` (XAML-Controls, Verdrahtung, kein Direktzugriff, iOS-Ausschluss, Windows-Roaming) | Offen | — |
| 33 | Tests | `AppUpdateTests_Startup` an neue Effektliste anpassen | Offen | — |
| 34 | Verifikation | Windows-Build `net10.0-windows10.0.19041.0` + `dotnet test` + `dotnet format`-Check | Offen | — |
