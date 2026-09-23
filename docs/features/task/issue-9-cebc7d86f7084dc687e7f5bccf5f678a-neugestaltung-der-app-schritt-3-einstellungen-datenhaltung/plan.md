# Umsetzungsplan: Einstellungsverwaltung und lokale Datenhaltung mit Backup-Trennung

## Übersicht

Die neue App erhält eine persistente, profil-isolierte Einstellungsverwaltung und eine
strukturierte lokale Datenablage mit Backup-Trennung. Dafür wird ein neues
plattformneutrales `net10.0`-Projekt `VideoPlayer.Storage` angelegt (Einstellungsmodell,
Ablage-Abstraktionen, Profil-Isolation, Ablageort-Layout, SQLite-Datenbanken), das vom
Testprojekt direkt referenzierbar ist. `VideoPlayer.Maui` liefert die Plattform-Adapter
(`Preferences`, `SecureStorage`, `FileSystem`, iOS-Backup-Ausschluss, Windows-Roaming)
und die ausgebaute `SettingsPage`; `VideoPlayer.Mvu` trägt die Einstellungen im
`AppModel` und erhält Messages/Effekte für Laden und Speichern.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Platzierung der Ablagelogik | Neues `net10.0`-Projekt `VideoPlayer.Storage` (Einstellungsmodell `AppSettings`, `ISettingsStore`, `ISecretStore`, `AppSettingsService`, `AppSecretsService`, `AppProfile`, `AppStorageLayout`, SQLite-Entitäten, `BackupDatabase`/`CacheDatabase`) | Plattformneutral und damit aus `VideoWebPlayer.Maui.Tests` (`net10.0`) laufzeittestbar — gleiches Muster wie `VideoPlayer.Mvu` in Schritt 2; das MAUI-Projekt ist nicht referenzierbar. `sqlite-net-pcl`/`SQLitePCLRaw.bundle_green` sind netstandard-kompatibel und laufen auch im Testprozess. |
| Einstellungsmodell im MVU | `AppSettings` (immutable record in `VideoPlayer.Storage`); `AppModel` erhält `Settings`-Eigenschaft; `VideoPlayer.Mvu` referenziert `VideoPlayer.Storage` | MVU bleibt führend: UI-Änderung → `SaveSettingsRequestedMessage` → `AppUpdate` normalisiert + `PersistSettingsEffect` → `IAppEffectHandler` schreibt über `AppSettingsService`. Laden beim Start über `LoadSettingsRequestedMessage`/`LoadSettingsEffect` → `SettingsLoadedMessage`. |
| Ablage-Abstraktionen | `ISettingsStore` (synchron, `Preferences`-Äquivalent) und `ISecretStore` (asynchron, `SecureStorage`-Äquivalent) als dünne Schlüssel/Wert-Verträge; Profil-Präfixe zentral in `AppProfile`/`AppSettingsService`/`AppSecretsService` | Dünne Adapter in `VideoPlayer.Maui` (`PreferencesSettingsStore`, `SecureStorageSecretStore`); gesamte Schlüssel-/Default-/Normalisierungslogik liegt testbar in `VideoPlayer.Storage`. Verhindert Direktzugriffe (Contract-Test wie `MauiSettingsContractTests`). |
| Profil-Isolation | `AppProfile` liest Umgebungsvariable `VIDEOPLAYER_PROFILE` (injizierbarer Env-Accessor für Tests); `PrefixKey` für Ablage-Schlüssel, Profil-Unterverzeichnis für Dateiablagen (Default-Profil ohne Unterverzeichnis — entspricht Alt-Konvention „kein Suffix bei default") | Anforderung „Isolation über Umgebungsvariable"; Benennung konsistent zu `VIDEOPLAYER_SERVER_URL` (Schritt 2). |
| Geheimnisse | `AppSecretsService` schreibt `AuthToken`/`Passkey` ausschließlich über `ISecretStore` (`SecureStorage` = iOS-Keychain/Credential-Locker), `Username` über `ISettingsStore`; `SetAuthTokenAsync` entfernt Legacy-Preference-Schlüssel | Geheimnisse gehören nicht in die normale Ablage und nicht ins Cloud-Backup — `SecureStorage` erfüllt beides plattformseitig. |
| Backup-Trennung (Ablageorte) | Drei Wurzeln: `BackupDirectory` (backup-pflichtig: lokale Datenbank `videoplayer.db3` mit Serverliste, zuletzt verbundenem Server, Profildaten ohne Geheimnisse, unsynchronisierten Fortschritten), `NoBackupDirectory` (nicht backup-pflichtig: `videoplayer-cache.db3` mit Favoriten-Kopie, Startseiten-Metadaten, Episodenlisten, Download-Manifest sowie Dateiablagen `videos/`, `previews/`), `TempDirectory` unter `FileSystem.CacheDirectory` | Eindeutige, testbare Klassifikation über getrennte Wurzelverzeichnisse; plattformseitiger Ausschluss siehe nächste Zeile. |
| iOS-Backup-Ausschluss | `NoBackupDirectory` unter `FileSystem.AppDataDirectory/nobackup` + `Foundation.NSFileManager.SetSkipBackupAttribute(path, true)` beim Anlegen (`#if IOS || MACCATALYST`); Temp-Cache unter `FileSystem.CacheDirectory` (iOS sichert `Library/Caches` ohnehin nicht) | Plattformüblicher Backup-Ausschluss (`NSURLIsExcludedFromBackupKey`) — vom Requirement explizit erlaubt; große Offline-Videos landen nicht in `Library/Caches` (von iOS löschbar), sondern im attributierten `nobackup`-Ablage. |
| Windows-Roaming | `BackupDirectory`-Wurzel = `%APPDATA%\VideoPlayer` (`Environment.SpecialFolder.ApplicationData` — Roaming-Profil); `NoBackupDirectory`-Wurzel = `FileSystem.AppDataDirectory` (lokal, `LocalAppData`) | Windows hat kein iCloud-Backup; das „geeignete Roaming-Äquivalent" ist das servergespeicherte (Roaming-)Profilverzeichnis — wird genutzt. Unpackaged-App (`WindowsPackageType=None`): `ApplicationData.RoamingFolder` nicht verfügbar → `SpecialFolder.ApplicationData`. |
| SQLite-Bibliothek | `sqlite-net-pcl` + `SQLitePCLRaw.bundle_green` (Versionen wie Alt-App); einmalige Initialisierung über `SqliteBootstrap.EnsureInitialized()` in `VideoPlayer.Storage` | Referenz-Stack der Alt-App (`ClientDatabase`); läuft auf allen Zielen und im Testprozess. |
| Zwei Datenbanken statt einer | `BackupDatabase` (`videoplayer.db3`, backup-pflichtig) und `CacheDatabase` (`videoplayer-cache.db3`, nicht backup-pflichtig) als getrennte Dateien in den jeweiligen Wurzeln | Einfachste eindeutige Backup-Zuordnung auf Dateiebene — keine tabellenweise Klassifikation nötig. |
| Einstellungsseite | `SettingsPage` mit `Switch`- und numerischen `Entry`-Elementen (`Keyboard="Numeric"`), deutschen Klartext-Beschriftungen, Inline-Fehlerhinweis bei ungültiger Zahl; Code-Behind dispatcht `SaveSettingsRequestedMessage`; Werte kommen aus `MvuHost.Model.Settings` | Konsistent zu Schritt-2-Muster (BindingContext = `MvuHost`, Nutzeraktion → Message → Update → Effekt). Keine Auswahl benannter Entitäten nötig — nur Skalare, daher sind Schalter/Zahlenfelder das passende Muster (kein Lookup-Muster im Bestand vorhanden). |
| `ServerAddressProvider` | Umstellung auf `ISettingsStore` mit `AppProfile.PrefixKey("ServerAddress")`; Env-/Default-Fallback bleibt | Beseitigt den letzten direkten `Preferences`-Zugriff und macht die Serveradresse profil-isoliert (Vorgriff auf Schritt 4, bereits vorhandener Schlüssel). |

## Programmabläufe

### App-Start: Ablage initialisieren + Einstellungen laden

1. `App.RunStartupAsync` dispatcht `StartupRequestedMessage` (bestehend).
2. `AppUpdate.Update` → Model `{ IsStartupRunning = true, ServerHealth = Checking }`,
   Effekte `[InitializeStorageEffect, LoadSettingsEffect, CheckServerHealthEffect]`.
3. `MvuHost` übergibt die Effekte an `ShellAppEffectHandler`:
   - `InitializeStorageEffect` → `AppStorageLayoutProvider.CreateLayout` wurde bereits
     bei der DI-Registrierung ausgeführt (Verzeichnisse + iOS-Ausschluss); der Effekt
     ruft `BackupDatabase.InitializeAsync`/`CacheDatabase.InitializeAsync` auf
     (Tabellen anlegen).
   - `LoadSettingsEffect` → `AppSettingsService.Load()` → Rückdispatch
     `SettingsLoadedMessage(settings)` auf dem UI-Thread.
   - `CheckServerHealthEffect` → bestehender Ablauf (unverändert).
4. `AppUpdate.Update` auf `SettingsLoadedMessage` →
   `model with { Settings = message.Settings.Normalize() }`.

Beteiligte Klassen: `App`, `MvuHost`, `AppUpdate`, `AppModel`, `InitializeStorageEffect`,
`LoadSettingsEffect`, `SettingsLoadedMessage`, `ShellAppEffectHandler`,
`BackupDatabase`, `CacheDatabase`, `AppSettingsService`.

### Einstellung über die Einstellungsseite ändern (Benutzerfluss)

1. Nutzer öffnet „Einstellungen" im Seitenmenü → `SettingsPage` (Shell-Navigation,
   bestehend).
2. `SettingsPage.OnAppearing` füllt alle Bedienelemente aus `MvuHost.Model.Settings`
   (bereits beim Start geladen; `ModelChanged`-Abo hält die Anzeige synchron).
3. Nutzer kippt einen Schalter (`Toggled`) oder verlässt ein Zahlenfeld
   (`Unfocused`/`Completed`).
4. Code-Behind bildet `host.Model.Settings with { <Feld> = neuerWert }` und dispatcht
   `SaveSettingsRequestedMessage(neueSettings)`. Bei ungültiger Zahleneingabe: kein
   Dispatch, Inline-Hinweis „Bitte eine gültige Zahl eingeben." und Rücksetzen auf den
   Modellwert.
5. `AppUpdate.Update` → `model with { Settings = settings.Normalize() }` (Clamping der
   Schranken) + `PersistSettingsEffect(settings)`.
6. `ShellAppEffectHandler` → `AppSettingsService.Save(settings)` → `ISettingsStore`
   (`Preferences`) mit Profil-Präfix — persistiert über App-Neustarts.
7. `ModelChanged` aktualisiert die Anzeige (inkl. ggf. geklampter Werte).

Beteiligte Klassen: `SettingsPage`, `MvuHost`, `AppUpdate`, `AppModel`,
`SaveSettingsRequestedMessage`, `PersistSettingsEffect`, `ShellAppEffectHandler`,
`AppSettingsService`, `AppSettings`, `ISettingsStore`, `PreferencesSettingsStore`.

### Geheimnis-Ablage (für spätere Schritte bereitgestellt)

`AppSecretsService` kapselt `AuthToken`/`Passkey` (`ISecretStore` → `SecureStorage`)
und `Username` (`ISettingsStore` → `Preferences`), alle Schlüssel mit Profil-Präfix.
In diesem Schritt gibt es noch keinen Aufrufer aus der UI (Anmeldung folgt in
Schritt 5); der Dienst wird per DI registriert und über Unit-Tests nachgewiesen.

## Neue Klassen

### Projekt `VideoPlayer.Storage` (net10.0, neu)

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `AppSettings` | record | Alle acht Einstellungen + `Normalize()` (Clamping) + Konstante `DefaultDownloadQuotaGb = 5` |
| `ISettingsStore` | Interface | Nicht-geheime Schlüssel/Wert-Ablage (`Contains`, `GetString`/`GetInt`/`GetBool`, `Set`-Overloads, `Remove`) |
| `ISecretStore` | Interface | Sichere Ablage (`GetAsync`, `SetAsync`, `RemoveAsync`) |
| `AppProfile` | Klasse | `VIDEOPLAYER_PROFILE`-Auswertung (injizierbar), `Name`, `PreferencesPrefix`, `PrefixKey`, `DirectoryName` |
| `AppSettingsService` | Klasse | `Load()`/`Save(AppSettings)` über `ISettingsStore` + Profil-Präfix; Schlüsselkonstanten; `Normalize` beim Laden |
| `AppSecretsService` | Klasse | `GetUsername`/`GetPasswordAsync`/`SetCredentialsAsync`/`ClearCredentialsAsync`/`GetAuthTokenAsync`/`SetAuthTokenAsync`/`ClearAuthTokenAsync` |
| `AppStorageLayout` | Klasse | Pfad-Ableitung aus drei Wurzeln + Profil: `BackupDirectory`, `DataDatabasePath`, `NoBackupDirectory`, `CacheDatabasePath`, `VideosDirectory`, `PreviewsDirectory`, `TempDirectory`, `EnsureDirectoriesExist()` |
| `SqliteBootstrap` | statische Klasse | Einmalige `SQLitePCL.Batteries_V2.Init()`-Initialisierung |
| `BackupDatabase` | Klasse | `SQLiteAsyncConnection` auf `DataDatabasePath`; `InitializeAsync` legt Tabellen `KnownServerEntry`, `BackupStateEntry`, `PendingProgressSyncEntry` an |
| `CacheDatabase` | Klasse | `SQLiteAsyncConnection` auf `CacheDatabasePath`; `InitializeAsync` legt Tabellen `CachedListEntry`, `CachedFavoritesEntry`, `DownloadManifestEntry` an |
| `KnownServerEntry` | Entität | `Id`, `Address`, `DisplayName`, `LastUsedAtUtc` (backup-pflichtig: Serverliste) |
| `BackupStateEntry` | Entität | `Key`, `Value` (backup-pflichtig: zuletzt verbundener Server, Profildaten ohne Geheimnisse) |
| `PendingProgressSyncEntry` | Entität | `Id`, `VideoId`, `VideoType`, `PositionTicks`, `UpdatedAtUtc` (backup-pflichtig: unsynchronisierte Fortschritte) |
| `CachedListEntry` | Entität | `Key`, `Json`, `UpdatedAtUtc` (nicht backup-pflichtig: Startseiten-Metadaten, Episodenlisten) |
| `CachedFavoritesEntry` | Entität | `VideoId`, `VideoType`, `Json`, `UpdatedAtUtc` (nicht backup-pflichtig: lokale Favoriten-Kopie) |
| `DownloadManifestEntry` | Entität | `Id`, `VideoId`, `VideoType`, `FilePath`, `SizeBytes`, `State`, `UpdatedAtUtc` (nicht backup-pflichtig: Download-Manifest) |

### Projekt `VideoPlayer.Mvu` (erweitert)

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `LoadSettingsRequestedMessage` | record (`AppMessage`) | Einstellungen (erneut) laden anfordern |
| `SettingsLoadedMessage` | record (`AppMessage`) | Geladene `AppSettings` ins Model übernehmen |
| `SaveSettingsRequestedMessage` | record (`AppMessage`) | Geänderte `AppSettings` speichern |
| `InitializeStorageEffect` | record (`AppEffect`) | Ablage-Verzeichnisse/-Tabellen initialisieren |
| `LoadSettingsEffect` | record (`AppEffect`) | Einstellungen aus der Ablage lesen |
| `PersistSettingsEffect` | record (`AppEffect`) | `AppSettings` in die Ablage schreiben |

### Projekt `VideoPlayer.Maui` (neu)

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `PreferencesSettingsStore` | Klasse (`ISettingsStore`) | Adapter auf `IPreferences` (`Preferences.Default`) |
| `SecureStorageSecretStore` | Klasse (`ISecretStore`) | Adapter auf `ISecureStorage` (`SecureStorage.Default`) |
| `AppStorageLayoutProvider` | statische Klasse | Plattform-Wurzeln (`%APPDATA%` auf Windows, `AppDataDirectory`/`nobackup` + `SetSkipBackupAttribute` auf iOS/MacCatalyst, `NoBackupFilesDir` auf Android, `CacheDirectory` für Temp) + `CreateLayout(profile)` inkl. Verzeichnisanlage |

## Änderungen an bestehenden Klassen

### `AppModel` (`VideoPlayer.Mvu`)
- **Neue Eigenschaft:** `Settings` (`AppSettings`, Default `new()`) — geladener
  Einstellungszustand.

### `AppUpdate` (`VideoPlayer.Mvu`)
- **Geändert:** `UpdateStartupRequested` — Effektliste um `InitializeStorageEffect`
  und `LoadSettingsEffect` ergänzen.
- **Neue Zweige:** `LoadSettingsRequestedMessage` → `LoadSettingsEffect`;
  `SettingsLoadedMessage` → `model.Settings = Normalize()`;
  `SaveSettingsRequestedMessage` → `model.Settings = Normalize()` + `PersistSettingsEffect`.

### `ShellAppEffectHandler` (`VideoPlayer.Maui`)
- **Neue Effekt-Zweige:** `InitializeStorageEffect` → `BackupDatabase`/`CacheDatabase`
  `InitializeAsync`; `LoadSettingsEffect` → `AppSettingsService.Load()` + Dispatch
  `SettingsLoadedMessage` (UI-Thread); `PersistSettingsEffect` →
  `AppSettingsService.Save`.
- **Konstruktor:** zusätzliche Abhängigkeiten (`AppSettingsService`,
  `BackupDatabase`, `CacheDatabase`) — weiterhin über `IServiceProvider`-Auflösung
  oder direkte Injektion.

### `ServerAddressProvider` (`VideoPlayer.Maui`)
- **Geändert:** `GetServerAddress` liest `ISettingsStore.GetString(profile.PrefixKey("ServerAddress"))`
  statt `Preferences.Default.Get(...)`; Konstruktor nimmt `ISettingsStore` + `AppProfile`.

### `MauiProgram` (`VideoPlayer.Maui`)
- **Neue Registrierungen (Singletons):** `AppProfile` (`FromEnvironment`),
  `ISettingsStore` → `PreferencesSettingsStore`, `ISecretStore` →
  `SecureStorageSecretStore`, `AppSettingsService`, `AppSecretsService`,
  `AppStorageLayout` (via `AppStorageLayoutProvider.CreateLayout`), `BackupDatabase`,
  `CacheDatabase`.
- **`SettingsPage` bleibt Transient**, erhält `MvuHost` per Konstruktor.

### `SettingsPage` (`VideoPlayer.Maui/Pages/SettingsPage.xaml(.cs)`)
- **Vollständiger Ausbau:** acht Einstellungen als beschriftete `Switch`/`Entry`-
  Elemente mit `x:Name`/`AutomationId`, Inline-Fehlerlabel; Konstruktor mit `MvuHost`,
  `OnAppearing` befüllt aus `Model.Settings`, Events dispatcht
  `SaveSettingsRequestedMessage`.

### `VideoPlayer.App.sln` / Projektdateien
- `VideoPlayer.Storage` in die Solution aufnehmen.
- `VideoPlayer.Mvu.csproj`: `ProjectReference` auf `VideoPlayer.Storage`.
- `VideoPlayer.Maui.csproj`: `ProjectReference` auf `VideoPlayer.Storage`.
- `VideoWebPlayer.Maui.Tests.csproj`: `ProjectReference` auf `VideoPlayer.Storage`.
- `VideoPlayer.Storage.csproj`: `sqlite-net-pcl`, `SQLitePCLRaw.bundle_green`.

## Datenbankmigrationen

Keine — die Datenbanken werden neu angelegt (`CreateTableAsync` beim ersten Start).

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `PreloadEpisodeCount` | Ganzzahl, Clamp 1–10 | Ungültige Eingabe: Inline-Hinweis, Wert nicht übernommen; Clamp beim Speichern |
| `AutoResumeRemainingSeconds` | Ganzzahl, Clamp 30–3600 | wie oben |
| `WatchedEndWindowSeconds` | Ganzzahl, Clamp 30–3600 | wie oben |
| `DownloadQuotaGb` | Ganzzahl, Clamp 1–500 | wie oben |
| `AppSettings` (alle Felder) | `Normalize()` beim Laden und Speichern | Werte außerhalb der Schranken werden geklammert |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `VIDEOPLAYER_PROFILE` (Umgebungsvariable) | string | `default` | Profil-Isolation: Präfix für Ablage-Schlüssel, Unterverzeichnis für Dateiablagen |
| `AutoDownloadEnabled` (Ablage-Schlüssel) | bool | `false` | Automatische Downloads an/aus |
| `PreloadEpisodeCount` (Ablage-Schlüssel) | int | `3` | Anzahl vorzuladender Episoden |
| `AutoResumeEnabled` (Ablage-Schlüssel) | bool | `true` | Auto-Fortsetzung an/aus |
| `AutoResumeRemainingSeconds` (Ablage-Schlüssel) | int | `300` | Zeitfenster Auto-Fortsetzung (Sekunden Restlaufzeit) |
| `WifiOnlyDownloads` (Ablage-Schlüssel) | bool | `true` | Downloads nur im WLAN |
| `WatchedEndWindowSeconds` (Ablage-Schlüssel) | int | `300` | „Als angesehen"-Zeitfenster am Videoende |
| `DownloadQuotaGb` (Ablage-Schlüssel) | int | `5` | Speicherplatzkontingent in GB |
| `AutoDeleteWatchedEpisodes` (Ablage-Schlüssel) | bool | `false` | Gesehene Episoden automatisch löschen |
| `Username`/`Passkey`/`AuthToken` (Ablage-Schlüssel) | string | – | Zugangsdaten/Token; `Passkey`+`AuthToken` ausschließlich in `ISecretStore` |

## Seiteneffekte und Risiken

- **Neuer MVU-Effekt beim Start:** `InitializeStorageEffect` legt Verzeichnisse und
  DB-Tabellen an; Fehler dürfen den Start nicht blockieren (Handler fängt
  Exceptions wie beim Health-Check ab; bei Ausfall bleibt `Settings` auf Defaults).
- **`ServerAddressProvider`-Umstellung:** Der bisherige `ServerAddress`-Schlüssel wird
  künftig profil-präfixiert gelesen. Für das Default-Profil bleibt der Schlüssel
  unverändert (`""`-Präfix) — kein Migrationsbedarf. Nicht-Default-Profile lesen
  eigene Schlüssel (gewünschte Isolation).
- **iOS-Simulator-Nachweis:** `SetSkipBackupAttribute` ist nur im iOS/MacCatalyst-
  Build aktiv (`#if`); lokaler Nachweis per Contract-Test auf den Quelltext +
  CI-iOS-Build.
- **Neue Paketabhängigkeiten:** `sqlite-net-pcl`/`SQLitePCLRaw.bundle_green` in
  `VideoPlayer.Storage` (laufen auf allen Zielen und in `net10.0`-Tests).
- **Datenbanktabellen ohne aktive Schreiber:** Serverliste, Fortschritts-Outbox,
  Manifest usw. werden in diesem Schritt strukturell angelegt; befüllt werden sie
  in späteren Schritten (4, 6, 10, 11). Nachweis über Unit-Tests.

## Umsetzungsreihenfolge

1. **`VideoPlayer.Storage`-Projekt anlegen** (csproj `net10.0`, `ImplicitUsings`,
   `Nullable`, Lizenz-Metadaten wie `VideoPlayer.Mvu`; Pakete `sqlite-net-pcl` +
   `SQLitePCLRaw.bundle_green`; in `VideoPlayer.App.sln` aufnehmen)
   - Voraussetzungen: keine.
2. **Ablage-Abstraktionen und Profil** (`ISettingsStore`, `ISecretStore`, `AppProfile`)
   - Voraussetzungen: Schritt 1.
3. **Einstellungsmodell und -dienst** (`AppSettings`, `AppSettingsService`,
   `AppSecretsService`)
   - Voraussetzungen: Schritt 2.
4. **Ablageort-Layout** (`AppStorageLayout`)
   - Voraussetzungen: Schritt 2 (`AppProfile`).
5. **Datenbanken** (`SqliteBootstrap`, Entitäten, `BackupDatabase`, `CacheDatabase`)
   - Voraussetzungen: Schritte 1, 4.
6. **MVU-Erweiterung** (`AppModel.Settings`, neue Messages/Effekte, `AppUpdate`;
   `ProjectReference` `VideoPlayer.Mvu` → `VideoPlayer.Storage`)
   - Voraussetzungen: Schritt 3.
7. **MAUI-Adapter und Verdrahtung** (`PreferencesSettingsStore`,
   `SecureStorageSecretStore`, `AppStorageLayoutProvider`, `MauiProgram`,
   `ShellAppEffectHandler`, `ServerAddressProvider`-Umstellung;
   `ProjectReference` `VideoPlayer.Maui` → `VideoPlayer.Storage`)
   - Voraussetzungen: Schritte 3–6.
8. **Einstellungsseite ausbauen** (`SettingsPage.xaml(.cs)`)
   - Voraussetzungen: Schritte 6–7.
9. **Tests schreiben** (siehe Abschnitt Tests; `ProjectReference` Testprojekt →
   `VideoPlayer.Storage`)
   - Voraussetzungen: Schritte 3–8.
10. **Verifizieren** (`dotnet restore` Solution; `dotnet build` `VideoPlayer.Maui`
    für `net10.0-windows10.0.19041.0`; `dotnet test`; `dotnet format whitespace
    --verify-no-changes` auf geänderte Projekte)
    - Voraussetzungen: Schritte 6–9.

## Tests

### Neue Tests (in `VideoWebPlayer.Maui.Tests`, xUnit)

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Defaults_AreExpected` (u. a. `DownloadQuotaGb == 5`) | `AppSettingsTests` | Standardwerte aller acht Einstellungen |
| `Normalize_ClampsOutOfRangeValues` | `AppSettingsTests` | Clamping aller numerischen Schranken |
| `Load_WhenStoreEmpty_ReturnsDefaults` | `AppSettingsServiceTests` | Defaults bei leerem Store |
| `Save_ThenLoad_RoundTrips` | `AppSettingsServiceTests` | Persistenz-Roundtrip über `InMemorySettingsStore` |
| `Load_ClampsOutOfRangeStoredValues` | `AppSettingsServiceTests` | `Normalize` beim Laden |
| `Profiles_DoNotInterfere` | `AppSettingsServiceTests` | Zwei `AppProfile`-Instanzen, getrennte Schlüssel/ Werte — Profil-Isolation |
| `UsesProfilePrefixedKeys` | `AppSettingsServiceTests` | Schlüsselnamen tragen Profil-Präfix (default: kein Präfix) |
| `Secrets_GoToSecretStore_Only` | `AppSecretsServiceTests` | Token/Passwort nur in `ISecretStore`; `ISettingsStore` enthält keine Geheimnis-Schlüssel |
| `Username_GoesToSettingsStore` | `AppSecretsServiceTests` | Username in normaler Ablage |
| `SetAuthToken_RemovesLegacyPreference` | `AppSecretsServiceTests` | Kein Token-Rest in `ISettingsStore` |
| `ClearCredentials_RemovesAll` | `AppSecretsServiceTests` | Username+Passwort+Token entfernt |
| `Secrets_AreProfileIsolated` | `AppSecretsServiceTests` | Profil-Isolation auch für Geheimnisse |
| `FromEnvironment_ReadsProfileVariable` / `DefaultsToDefault` / `Lowercases` | `AppProfileTests` | `VIDEOPLAYER_PROFILE`-Auswertung über injizierten Accessor |
| `PrefixKey_And_DirectoryName` | `AppProfileTests` | Präfix-/Verzeichnisregeln default vs. benannt |
| `SeparatesBackupAndNoBackupRoots` | `AppStorageLayoutTests` | `DataDatabasePath` unter Backup-Wurzel; `CacheDatabasePath`/`Videos`/`Previews` unter NoBackup-Wurzel; `Temp` unter Cache-Wurzel |
| `Profiles_GetIsolatedDirectories` | `AppStorageLayoutTests` | Profil-Unterverzeichnisse greifen nicht ineinander |
| `EnsureDirectoriesExist_CreatesAll` | `AppStorageLayoutTests` | Reale Verzeichnisanlage im Temp-Verzeichnis |
| `BackupDatabase_CreatesTables` / `RoundTrips` | `LocalDatabaseTests` | Reale SQLite-Datei im Temp-Pfad: Tabellen + Insert/Select für `KnownServerEntry`, `BackupStateEntry`, `PendingProgressSyncEntry` |
| `CacheDatabase_CreatesTables` / `RoundTrips` | `LocalDatabaseTests` | `CachedListEntry`, `CachedFavoritesEntry`, `DownloadManifestEntry` |
| `Databases_LandInClassifiedDirectories` | `LocalDatabaseTests` | `videoplayer.db3` im Backup-Verzeichnis, `videoplayer-cache.db3` im NoBackup-Verzeichnis |
| `Update_LoadSettingsRequested_EmitsLoadEffect` | `AppUpdateTests_Settings` | `LoadSettingsRequestedMessage` → `LoadSettingsEffect` |
| `Update_SettingsLoaded_SetsModelSettings` | `AppUpdateTests_Settings` | `SettingsLoadedMessage` → `Model.Settings` (normalisiert) |
| `Update_SaveSettings_NormalizesAndPersists` | `AppUpdateTests_Settings` | `SaveSettingsRequestedMessage` → Model + `PersistSettingsEffect` mit geklammerten Werten |
| `Update_Startup_EmitsStorageAndSettingsEffects` | `AppUpdateTests_Settings` | `StartupRequestedMessage` → `InitializeStorageEffect` + `LoadSettingsEffect` + `CheckServerHealthEffect` |
| `SettingsPage_ContainsAllSettingControls` | `SettingsStorageContractTests` | `SettingsPage.xaml` (XML): alle acht Einstellungen mit beschriftetem Bedienelement (AutomationIds) |
| `SettingsPage_WiresMvuHostAndSaveMessage` | `SettingsStorageContractTests` | Code-Behind nutzt `MvuHost`, dispatcht `SaveSettingsRequestedMessage` |
| `MauiProgram_RegistersStorageServices` | `SettingsStorageContractTests` | DI-Registrierung aller neuen Dienste |
| `ServerAddressProvider_UsesSettingsStore` | `SettingsStorageContractTests` | kein `Preferences.Default` mehr in `ServerAddressProvider` |
| `NoDirectPlatformStorageAccess` | `SettingsStorageContractTests` | `Preferences.Default`/`SecureStorage.Default` nur in den Adapter-Klassen |
| `IosBackupExclusion_IsImplemented` | `SettingsStorageContractTests` | `SetSkipBackupAttribute` auf dem NoBackup-Verzeichnis vorhanden (`#if IOS`) |
| `WindowsRoaming_IsUsedForBackupRoot` | `SettingsStorageContractTests` | `SpecialFolder.ApplicationData` (Roaming) als Backup-Wurzel unter Windows |
| `InMemorySettingsStore` / `InMemorySecretStore` | `Stubs/` | Test-Fakes der Ablage-Abstraktionen |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `AppUpdateTests_Startup.Update_StartupRequested_SetsCheckingAndEmitsHealthCheck` | Effektliste enthält nun drei Effekte statt einem — Assertion auf „enthält `CheckServerHealthEffect`" umstellen |

### E2E-Tests (primärer Funktionsnachweis)

Es existiert kein UI-Automatisierungs-Framework im Repo (siehe `inventory/tests.md`);
gerenderte E2E-Tests sind nicht möglich. Nachweiskette für den Benutzerfluss
„Einstellungen pflegen" auf allen verfügbaren Ebenen:

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist / Ersatzform |
|-----------|----------|------------------------|-------------------------------|----------------------------------|
| Pflicht | Einstellungsseite öffnen → alle acht Einstellungen sichtbar und bedienbar | `SettingsStorageContractTests.SettingsPage_ContainsAllSettingControls` + `SettingsPage_WiresMvuHostAndSaveMessage` | Einstellungen über die Einstellungsseite änderbar | Struktureller Nachweis der Bedienelemente und der MVU-Verdrahtung — einziges automatisierbares Äquivalent ohne UI-Framework |
| Pflicht | Wert ändern → wird normalisiert und persistiert; übersteht Neustart | `AppUpdateTests_Settings.Update_SaveSettings_NormalizesAndPersists` + `AppSettingsServiceTests.Save_ThenLoad_RoundTrips` (Roundtrip über die Ablage-Abstraktion) + `AppSecretsServiceTests`/`Profiles_DoNotInterfere` | Persistenz über App-Neustarts; Kontingent in GB mit 5-GB-Default | Unit-Nachweis der kompletten Speicherstrecke Message → Update → Effekt → Service → Store |
| Pflicht | Backup-Klassifikation der Ablageorte | `AppStorageLayoutTests.SeparatesBackupAndNoBackupRoots` + `LocalDatabaseTests.Databases_LandInClassifiedDirectories` + `SettingsStorageContractTests.IosBackupExclusion_IsImplemented`/`WindowsRoaming_IsUsedForBackupRoot` | Backup-Klassifikation umgesetzt und nachweisbar | Pfad-Tests + Plattformattribut-Contract-Tests |
| Pflicht | App baut weiterhin unter Windows | Windows-Build `net10.0-windows10.0.19041.0` der `VideoPlayer.Maui`-Projektdatei, dokumentiert in `test-results.md` | Gesamtintegrität der App | Build-Nachweis auf Prozessebene |

Welche bestehenden E2E-Tests müssen angepasst werden? Keine — es existieren keine.

## Offene Punkte

Keine.
