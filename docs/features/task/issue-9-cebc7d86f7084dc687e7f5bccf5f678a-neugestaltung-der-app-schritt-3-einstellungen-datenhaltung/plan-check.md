# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Alle Einstellungen persistent über Neustarts und über Einstellungsseite änderbar | `AppSettings` + `AppSettingsService` (`Preferences` über `ISettingsStore`), MVU-Strecke `SaveSettingsRequestedMessage` → `PersistSettingsEffect`, `SettingsPage`-Ausbau | `AppSettingsServiceTests.Save_ThenLoad_RoundTrips`, `AppUpdateTests_Settings.*`, `SettingsStorageContractTests.SettingsPage_ContainsAllSettingControls` | Abgedeckt |
| Speicherplatzkontingent in Gigabyte, Standard 5 GB | `AppSettings.DownloadQuotaGb` (int, GB), `DefaultDownloadQuotaGb = 5`, Clamp 1–500, UI als Zahlenfeld „GB" | `AppSettingsTests.Defaults_AreExpected`, `Normalize_ClampsOutOfRangeValues` | Abgedeckt |
| Backup-Klassifikation umgesetzt und nachweisbar | `AppStorageLayout` mit getrennten Backup-/NoBackup-/Temp-Wurzeln, zwei Datenbanken, iOS `SetSkipBackupAttribute`, Windows `%APPDATA%` (Roaming) | `AppStorageLayoutTests.*`, `LocalDatabaseTests.Databases_LandInClassifiedDirectories`, `IosBackupExclusion_IsImplemented`, `WindowsRoaming_IsUsedForBackupRoot` | Abgedeckt |
| Geheimnisse weder in normaler Ablage noch im Cloud-Backup | `ISecretStore`/`SecureStorageSecretStore`, `AppSecretsService` (Token/Passkey nur secret, Legacy-Preference-Entfernung) | `AppSecretsServiceTests.Secrets_GoToSecretStore_Only`, `SetAuthToken_RemovesLegacyPreference`, `NoDirectPlatformStorageAccess` | Abgedeckt |
| Unterschiedliche Profile beeinflussen sich nicht | `AppProfile` (`VIDEOPLAYER_PROFILE`), Schlüssel-Präfixe + Profil-Unterverzeichnisse | `AppSettingsServiceTests.Profiles_DoNotInterfere`, `AppSecretsServiceTests.Secrets_AreProfileIsolated`, `AppStorageLayoutTests.Profiles_GetIsolatedDirectories`, `AppProfileTests.*` | Abgedeckt |

## Abgleich fachlicher Muss-Anforderungen

| Anforderung | Im Plan |
|-------------|---------|
| Automatische Downloads an/aus | `AutoDownloadEnabled` (Switch) |
| Anzahl vorzuladender Episoden | `PreloadEpisodeCount` (Entry, Clamp 1–10) |
| Auto-Fortsetzung an/aus + Zeitfenster | `AutoResumeEnabled` + `AutoResumeRemainingSeconds` |
| WLAN-only-Downloads | `WifiOnlyDownloads` (Switch) |
| Zeitfenster „als angesehen" | `WatchedEndWindowSeconds` |
| Automatische Löschung gesehener Episoden | `AutoDeleteWatchedEpisodes` (Switch) |
| Backup-pflichtig: Serverliste, zuletzt verbundener Server, Profildaten ohne Geheimnisse, App-Einstellungen, unsynchronisierte Fortschritte | `KnownServerEntry`, `BackupStateEntry`, `PendingProgressSyncEntry` in `videoplayer.db3` (Backup-Wurzel); App-Einstellungen in `Preferences` (iOS: gesichert; Windows: Roaming-Ablage) |
| Nicht backup-pflichtig: Offline-Videos, Vorschaubilder, Startseiten-Metadaten, Episodenlisten, Download-Manifest, Temp-Cache, Favoriten-Kopie | `videos/`, `previews/` im NoBackup-Verzeichnis; `CachedListEntry`, `CachedFavoritesEntry`, `DownloadManifestEntry` in `videoplayer-cache.db3`; `TempDirectory` unter `FileSystem.CacheDirectory` |
| iOS: Backup-Ausschluss / Cache-Ablage | `SetSkipBackupAttribute` + `CacheDirectory` |
| Windows: Trennung + Roaming-Äquivalent | `SpecialFolder.ApplicationData` (Roaming) vs. `FileSystem.AppDataDirectory` (lokal) |
| Erneute Anmeldung nach Wiederherstellung | In Doku zu vermerken (Schritt 12) — `SecureStorage` geht nicht ins Cloud-Backup |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Einstellungsseite: Werte anzeigen/ändern | `SettingsPage_ContainsAllSettingControls` + `SettingsPage_WiresMvuHostAndSaveMessage` (Contract-Tests; kein UI-Automatisierungs-Framework im Repo — begründete Ersatzform wie in Schritt 2) | Abgedeckt (Ersatzform) |
| Persistenz über Neustart | `Save_ThenLoad_RoundTrips` + `Update_SaveSettings_NormalizesAndPersists` | Abgedeckt |
| Backup-Klassifikation | Pfad-/Tabellen-Tests + Plattform-Contract-Tests | Abgedeckt |
| Windows-Build-Integrität | `dotnet build -f net10.0-windows10.0.19041.0` dokumentiert in `test-results.md` | Abgedeckt |

## Hinweise

- Kein UI-Automatisierungs-Framework vorhanden; die Ersatz-Nachweiskette entspricht
  der in Schritt 2 akzeptierten Vorgehensweise.
- `ServerAddressProvider`-Umstellung ist enthalten (beseitigt letzten direkten
  `Preferences`-Zugriff der neuen App); kein Migrationsrisiko, da Default-Profil
  ohne Präfix arbeitet.
