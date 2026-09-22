# Abnahmeprüfung – Entwicklungsschritt 3

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

- Keine.

## Hinweise

Geprüft auf Branch `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-3-einstellungen-datenhaltung` (HEAD: `d64aa9b`), Diff gegen `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app`. Neues plattformneutrales Projekt `VideoPlayer.Storage` (net10.0, sqlite-net-pcl), erweitert um `VideoPlayer.Mvu` (Settings im Model, neue Messages/Effekte) und `VideoPlayer.Maui` (Einstellungsseite, Plattform-Adapter, DI-Verdrahtung).

### Alle 8 Einstellungen vorhanden, persistent und pflegbar

- `VideoPlayer.Storage/AppSettings.cs:14-42` enthält alle acht geforderten Einstellungen: `AutoDownloadEnabled`, `PreloadEpisodeCount`, `AutoResumeEnabled`, `AutoResumeRemainingSeconds` (Zeitfenster Auto-Fortsetzung, greift bei Restlaufzeit unterhalb des Fensters), `WifiOnlyDownloads`, `WatchedEndWindowSeconds` (Zeitfenster „als angesehen" am Videoende), `DownloadQuotaGb`, `AutoDeleteWatchedEpisodes`.
- Persistenz über `AppSettingsService` (`AppSettingsService.cs:29-59`) → `ISettingsStore` → `PreferencesSettingsStore` (Adapter auf `Preferences`, `VideoPlayer.Maui/Services/PreferencesSettingsStore.cs`) — damit über App-Neustarts erhalten. `Load()` liefert Defaults für fehlende Schlüssel und normalisiert; `Save()` normalisiert vor dem Schreiben.
- Pflegbarkeit über die Einstellungsseite: `SettingsPage.xaml` enthält 4 Switches + 4 numerische Eingabefelder mit `AutomationId`s (Zeilen 21-74); `SettingsPage.xaml.cs` dispatcht Änderungen als `SaveSettingsRequestedMessage` → `AppUpdate.UpdateSaveSettingsRequested` (`AppUpdate.cs:46-52`) → `PersistSettingsEffect` → `ShellAppEffectHandler.SaveSettings` (`ShellAppEffectHandler.cs:80-90`) → `AppSettingsService.Save`. Beim Start lädt `LoadSettingsEffect` die Werte ins Model (`AppUpdate.cs:34-37`, `ShellAppEffectHandler.cs:62-78`); `OnAppearing` befüllt die Controls aus `Model.Settings`.
- Ungültige Zahleneingaben werden abgelehnt (Inline-Hinweis `InvalidInputLabel`, Wert-Rücksetzung) — `SettingsPage.xaml.cs:131-147`.

### Speicherplatzkontingent in GB mit Standard 5

- `AppSettings.DownloadQuotaGb` (Gigabyte, Schranke 1–500) mit `DefaultDownloadQuotaGb = 5` (`AppSettings.cs:12,38-39`); UI-Feld „Speicherplatzkontingent für Downloads (GB)", Placeholder „Standard: 5" (`SettingsPage.xaml:41-45`). Testnachweis: `AppSettingsTests.Defaults_AreExpected`, `Normalize_ClampsDownloadQuotaGb`, `AppSettingsServiceTests.Load_WhenStoreEmpty_ReturnsDefaults`.

### Sichere Ablage vs. normale Einstellungsablage, Profil-Isolation

- Zwei Verträge: `ISettingsStore` (synchron, Preferences-Äquivalent) und `ISecretStore` (asynchron, SecureStorage-Äquivalent: iOS-Keychain, Windows-Credential-Ablage) — `VideoPlayer.Storage/ISettingsStore.cs`, `ISecretStore.cs`.
- `AppSecretsService` (`AppSecretsService.cs`) hält `Passkey` und `AuthToken` ausschließlich über `ISecretStore`; `GetMigratedSecretAsync` (Zeilen 83-101) migriert etwaige Legacy-Werte aus der normalen Ablage in die sichere und entfernt sie dort. `SetAuthTokenAsync` entfernt Reste in der normalen Ablage (Zeile 69).
- Profil-Isolation über Umgebungsvariable `VIDEOPLAYER_PROFILE` (`AppProfile.cs:12`): Schlüssel-Präfix (`PrefixKey`, Zeile 35) für Einstellungen/Geheimnisse und Unterverzeichnis (`DirectoryName`, Zeile 32) für Dateiablagen; Standardprofil `default` arbeitet ohne Präfix/Unterverzeichnis. Injizierbarer Env-Accessor `FromEnvironment` für Tests; Registrierung via `AppProfile.FromEnvironment()` in `MauiProgram.cs:24`.
- `ServerAddressProvider` wurde auf `ISettingsStore` mit Profil-Präfix umgestellt (`ServerAddressProvider.cs:31`) — kein direkter `Preferences`-Zugriff mehr (abgesichert durch Contract-Test `NoDirectPlatformStorageAccess`).

### Backup-Klassifikation der Ablageorte

- Drei Wurzeln in `AppStorageLayout` (`AppStorageLayout.cs`): `BackupDirectory`/`DataDatabasePath` (`videoplayer.db3`), `NoBackupDirectory`/`CacheDatabasePath` (`videoplayer-cache.db3`) + `VideosDirectory` (`videos/`) + `PreviewsDirectory` (`previews/`), `TempDirectory` unter `FileSystem.CacheDirectory`.
- Backup-pflichtig (`BackupDatabase`, `BackupDatabase.cs:30-35`): `KnownServerEntry` (Serverliste), `BackupStateEntry` mit `LastConnectedServerKey` (zuletzt verbundener Server, Profildaten ohne Geheimnisse), `PendingProgressSyncEntry` (unsynchronisierte Fortschritte). App-Einstellungen liegen in `Preferences` (auf iOS Bestandteil des Geräte-Backups).
- Nicht backup-pflichtig (`CacheDatabase`, `CacheDatabase.cs:30-35`): `CachedListEntry` (Startseiten-Metadaten, Episodenlisten), `CachedFavoritesEntry` (lokale Kopie der serverseitigen Favoriten), `DownloadManifestEntry` (Download-Manifest); plus Offline-Videos und Vorschaubilder als Dateiablagen sowie Temp-Cache im Cache-Verzeichnis.
- iOS-Ausschluss umgesetzt: `AppStorageLayoutProvider.ApplyBackupExclusion` ruft unter `#if IOS || MACCATALYST` `Foundation.NSFileManager.SetSkipBackupAttribute(layout.NoBackupDirectory, true)` auf (`AppStorageLayoutProvider.cs:72-80`) — plattformüblicher Backup-Ausschluss; Temp-Cache liegt im plattformseitig nie gesicherten `CacheDirectory`.
- Windows-Roaming-Äquivalent geprüft und genutzt: `GetBackupRoot` liefert unter `#if WINDOWS` `%APPDATA%\VideoPlayer` (`SpecialFolder.ApplicationData`, Zeilen 45-55); die App ist unpackaged, `ApplicationData.RoamingFolder` steht daher nicht zur Verfügung — dokumentiert im Code (Zeilen 10-12). `nobackup` bleibt unter `AppDataDirectory` lokal (kein Roaming).
- Die Ablagetrennung wird zusätzlich auf Android (`NoBackupFilesDir`) und MacCatalyst vorbereitet — über die geforderten Zielplattformen hinaus, ohne Konflikt.

### Geheimnisse weder in normaler Ablage noch im Backup

- Passwort/Token werden nur über `SecureStorage` geschrieben (sichere Plattformablage, nicht Teil des Cloud-Backups); die normale Ablage wird aktiv bereinigt (Migration, `Remove`-Aufrufe). Testnachweis: `AppSecretsServiceTests.Secrets_GoToSecretStore_Only`, `SetAuthToken_RemovesLegacyPreference`, `GetAuthToken_MigratesLegacyPreference`.
- Der Benutzername liegt bewusst in der normalen Ablage (`AppSecretsService.cs:27-28`, kommentiert „kein Geheimnis") und zählt zu den backup-pflichtigen „Profildaten ohne Geheimnisse" — Passwort und Token (die eigentlichen Zugangsgeheimnisse) bleiben in der sicheren Ablage; nach einer Wiederherstellung ist eine erneute Anmeldung erforderlich (dokumentiert in `docs/help/einstellungen-datenhaltung.md:64-70`).

### Testnachweis

- `dotnet test VideoWebPlayer.Maui.Tests` lokal ausgeführt: **160/160 Tests bestanden** (0 Fehler, 0 übersprungen, ~350 ms). Warnungen nur aus dem Altprojekt `VideoWebPlayer.Client` (CS0108/CS8618), keine im neuen Code.
- Abdeckung der Akzeptanzkriterien:
  - Persistenz/Defaults/Clamping: `AppSettingsTests`, `AppSettingsServiceTests` (Roundtrip, Schranken, Profil-Isolation der Schlüssel).
  - Einstellungsseite: `SettingsStorageContractTests.SettingsPage_ContainsAllSettingControls` (alle 8 Controls + Fehlerlabel, Numeric-Keyboard) und `SettingsPage_WiresMvuHostAndSaveMessage`.
  - MVU-Strecke: `AppUpdateTests_Settings`, `AppUpdateTests_Startup`, `MvuHostTests` (Startup emittiert `InitializeStorageEffect`, `LoadSettingsEffect`, `CheckServerHealthEffect`).
  - Backup-Klassifikation: `AppStorageLayoutTests` (getrennte Wurzeln, Profilverzeichnisse), `LocalDatabaseTests` (reale SQLite-Tabellen, Roundtrips, Dateiablage in klassifizierten Verzeichnissen), Contract-Tests `IosBackupExclusion_IsImplemented` und `WindowsRoaming_IsUsedForBackupRoot`.
  - Geheimnisse/Isolation: `AppSecretsServiceTests` (Secret-Only-Ablage, Legacy-Migration, `Secrets_AreProfileIsolated`), `AppProfileTests` (`VIDEOPLAYER_PROFILE`, Präfixe).

### Einschränkungen

- Der iOS-Backup-Ausschluss (`SetSkipBackupAttribute`) und die Windows-Roaming-Wurzel sind über Quellcode-Contract-Tests nachgewiesen; ein iOS-Laufzeitnachweis ist unter Windows nicht möglich (Kompilierung des iOS-Targets erfolgt in der CI, siehe Schritt-2-Abnahme).
- Der Windows-Build wurde in diesem Prüfschritt nicht erneut ausgeführt (nur `dotnet test` auf net10.0); die Windows-spezifischen `#if WINDOWS`-Pfade sind rein deklarativ geprüft.
- Ein interaktiver UI-Laufzeittest der Einstellungsseite wurde nicht durchgeführt; die Verdrahtung ist über Contract-Tests und die MVU-Update-Tests abgesichert.
