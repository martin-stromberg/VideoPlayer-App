# Tasks: Einstellungsverwaltung und lokale Datenhaltung mit Backup-Trennung

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Infrastruktur | `VideoPlayer.Storage`-Projekt (net10.0) anlegen, Pakete `sqlite-net-pcl` + `SQLitePCLRaw.bundle_green`, in `VideoPlayer.App.sln` aufnehmen | Erledigt | — |
| 2 | Ablage | `ISettingsStore`-Interface anlegen | Erledigt | — |
| 3 | Ablage | `ISecretStore`-Interface anlegen | Erledigt | — |
| 4 | Profil | `AppProfile` anlegen (`VIDEOPLAYER_PROFILE`, `PrefixKey`, `DirectoryName`) | Erledigt | — |
| 5 | Einstellungen | `AppSettings`-Record mit acht Einstellungen, `Normalize()`, `DefaultDownloadQuotaGb = 5` | Erledigt | — |
| 6 | Einstellungen | `AppSettingsService` (`Load`/`Save`, Profil-Präfixe, Schlüsselkonstanten) | Erledigt | — |
| 7 | Geheimnisse | `AppSecretsService` (Username/Passkey/AuthToken, Legacy-Bereinigung) | Erledigt | — |
| 8 | Ablage | `AppStorageLayout` (Backup-/NoBackup-/Temp-Wurzeln, `EnsureDirectoriesExist`) | Erledigt | — |
| 9 | Datenbank | `SqliteBootstrap` + Entitäten (`KnownServerEntry`, `BackupStateEntry`, `PendingProgressSyncEntry`, `CachedListEntry`, `CachedFavoritesEntry`, `DownloadManifestEntry`) | Erledigt | — |
| 10 | Datenbank | `BackupDatabase` und `CacheDatabase` mit `InitializeAsync` | Erledigt | — |
| 11 | MVU | `AppModel.Settings`-Eigenschaft ergänzen; `ProjectReference` Mvu → Storage | Erledigt | — |
| 12 | MVU | Messages `LoadSettingsRequestedMessage`, `SettingsLoadedMessage`, `SaveSettingsRequestedMessage` | Erledigt | — |
| 13 | MVU | Effekte `InitializeStorageEffect`, `LoadSettingsEffect`, `PersistSettingsEffect` | Erledigt | — |
| 14 | MVU | `AppUpdate`: neue Zweige + Start-Effekte erweitern | Erledigt | — |
| 15 | MAUI-Adapter | `PreferencesSettingsStore` (`IPreferences`-Adapter) | Erledigt | — |
| 16 | MAUI-Adapter | `SecureStorageSecretStore` (`ISecureStorage`-Adapter) | Erledigt | — |
| 17 | MAUI-Adapter | `AppStorageLayoutProvider` (Plattform-Wurzeln, iOS-Backup-Ausschluss, Windows-Roaming) | Erledigt | — |
| 18 | Verdrahtung | `MauiProgram`: DI-Registrierungen der neuen Dienste | Erledigt | — |
| 19 | Verdrahtung | `ShellAppEffectHandler`: neue Effekt-Zweige | Erledigt | — |
| 20 | Verdrahtung | `ServerAddressProvider` auf `ISettingsStore` + `AppProfile` umstellen | Erledigt | — |
| 21 | UI | `SettingsPage.xaml`: acht Einstellungen als Switch/Entry mit Beschriftung + Fehlerlabel | Erledigt | — |
| 22 | UI | `SettingsPage.xaml.cs`: `MvuHost`-Anbindung, Befüllung, Save-Dispatch, Validierung | Erledigt | — |
| 23 | Tests | Testprojekt: `ProjectReference` auf `VideoPlayer.Storage` | Erledigt | — |
| 24 | Tests | `Stubs/InMemorySettingsStore` + `Stubs/InMemorySecretStore` | Erledigt | — |
| 25 | Tests | `AppSettingsTests` (Defaults, Normalize) | Erledigt | — |
| 26 | Tests | `AppSettingsServiceTests` (Roundtrip, Defaults, Clamp, Profil-Isolation, Präfixe) | Erledigt | — |
| 27 | Tests | `AppSecretsServiceTests` (SecretStore-only, Username, Legacy, Clear, Isolation) | Erledigt | — |
| 28 | Tests | `AppProfileTests` (Env-Auswertung, Default, Lowercase, Präfix/Dir) | Erledigt | — |
| 29 | Tests | `AppStorageLayoutTests` (Trennung, Profildirs, EnsureDirectoriesExist) | Erledigt | — |
| 30 | Tests | `LocalDatabaseTests` (Tabellen + Roundtrips beider DBs, Ablageorte) | Erledigt | — |
| 31 | Tests | `AppUpdateTests_Settings` (Load/Save/Startup-Effekte) | Erledigt | — |
| 32 | Tests | `SettingsStorageContractTests` (XAML-Controls, Verdrahtung, kein Direktzugriff, iOS-Ausschluss, Windows-Roaming) | Erledigt | — |
| 33 | Tests | `AppUpdateTests_Startup` an neue Effektliste anpassen | Erledigt | — |
| 34 | Verifikation | Windows-Build `net10.0-windows10.0.19041.0` + `dotnet test` + `dotnet format`-Check | Erledigt | — |
