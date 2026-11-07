# Code-Review

Geprüft werden alle neuen/geänderten Quelldateien gegenüber dem Basisbranch
`task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app`
(Selbstprüfung ohne Unteragenten — dokumentierte Abweichung).

## Geänderte/neue Quelldateien

### `VideoPlayer.Storage` (neu, net10.0)

- `AppSettings.cs` — record mit acht Einstellungen; `Normalize()` klammert
  alle numerischen Werte (`PreloadEpisodeCount` 1–10,
  `AutoResumeRemainingSeconds`/`WatchedEndWindowSeconds` 30–3600,
  `DownloadQuotaGb` 1–500, `DefaultDownloadQuotaGb = 5`). Korrekt.
- `ISettingsStore.cs` / `ISecretStore.cs` — dünne Schlüssel/Wert-Verträge;
  Trennung synchron (Preferences-Äquivalent) vs. asynchron
  (SecureStorage-Äquivalent) sachgerecht.
- `AppProfile.cs` — `VIDEOPLAYER_PROFILE`, Kleinschreibung, Fallback
  `default`, leeres Präfix/Unterverzeichnis beim Standardprofil;
  injizierbarer Env-Accessor für Tests.
- `AppSettingsService.cs` — `Load` liefert Defaults + `Normalize`,
  `Save` normalisiert vor dem Schreiben; Schlüssel konstant und
  profil-präfixiert.
- `AppSecretsService.cs` — `Passkey`/`AuthToken` ausschließlich über
  `ISecretStore`; `GetMigratedSecretAsync` migriert etwaige Legacy-Werte
  aus der normalen Ablage in die sichere Ablage und entfernt sie dort —
  erfüllt „keine Geheimnisse in der normalen Ablage" auch rückwirkend.
- `AppStorageLayout.cs` — drei getrennte Wurzeln (Backup/NoBackup/Temp),
  `videoplayer.db3` vs. `videoplayer-cache.db3` als eindeutige
  Datei-Klassifikation; Profil-Scoping per Unterverzeichnis;
  `EnsureDirectoriesExist` legt alle Pfade an.
- `SqliteBootstrap.cs` — `Interlocked`-gesicherte einmalige
  `Batteries_V2.Init()`.
- `BackupDatabase.cs` / `CacheDatabase.cs` — `Lazy<SQLiteAsyncConnection>`
  auf dem jeweils klassifizierten Pfad; `InitializeAsync` legt die
  geplanten Tabellen idempotent an.
- `Entities/` — sechs Entitäten mit passenden `Table`-/`PrimaryKey`-/
  `Indexed`-Attributen; inhaltliche Zuordnung (backup-pflichtig vs.
  cache) entspricht der Anforderungsliste.
- `VideoPlayer.Storage.csproj` — Paket-Pins gegen die NuGet-Advisory-
  Warnung (`SQLitePCLRaw.lib.e_sqlite3` 3.53.3,
  `.android` 2.1.13 — analog Alt-App).

### `VideoPlayer.Mvu` (erweitert)

- `AppModel.cs` — `Settings`-Eigenschaft (Default `new AppSettings()`).
- `AppMessage.cs` / `AppEffect.cs` — drei neue Messages bzw. Effekte als
  `sealed record`, konsistent zum Bestand.
- `AppUpdate.cs` — erschöpfende `switch`-Zweige; Startup emittiert
  `[InitializeStorageEffect, LoadSettingsEffect, CheckServerHealthEffect]`;
  `Save`/`Loaded` normalisieren vor Übernahme ins Model — die UI kann keinen
  ungültigen Zustand ins Model schreiben. Reine Funktion bleibt erhalten.
- `VideoPlayer.Mvu.csproj` — Referenz auf `VideoPlayer.Storage`
  (MVU bleibt führend, Storage ist plattformneutral).

### `VideoPlayer.Maui`

- `MauiProgram.cs` — alle neuen Dienste als Singletons registriert;
  `AppStorageLayout` wird über `AppStorageLayoutProvider.CreateLayout`
  lazy erzeugt (Verzeichnisanlage + iOS-Backup-Ausschluss erst bei
  Auflösung im `InitializeStorageEffect` — nicht im DI-Aufbau).
- `Services/PreferencesSettingsStore.cs` — dünner `IPreferences`-Adapter,
  keine Logik.
- `Services/SecureStorageSecretStore.cs` — `ISecureStorage`-Adapter;
  Leseausnahmen werden protokolliert und als „leer" behandelt
  (Plattformverfügbarkeit), leere Werte entfernen den Schlüssel.
- `Services/AppStorageLayoutProvider.cs` — plattformspezifische Wurzeln:
  Windows `%APPDATA%\VideoPlayer` (Roaming-Äquivalent, da unpackaged),
  iOS/MacCatalyst `AppDataDirectory/nobackup` +
  `NSFileManager.SetSkipBackupAttribute` unter `#if IOS || MACCATALYST`,
  Android `NoBackupFilesDir` mit Fallback, Temp = `CacheDirectory`.
- `Services/ShellAppEffectHandler.cs` — neue Effekt-Zweige; Fehler bei
  Ablage-Initialisierung, Laden und Speichern werden abgefangen und
  protokolliert (Start blockiert nicht, Defaults bleiben aktiv);
  `SettingsLoadedMessage` wird auf dem UI-Thread dispatched.
- `Services/ServerAddressProvider.cs` — liest `ISettingsStore` mit
  Profil-Präfix; `Preferences.Default`-Zugriff entfernt; Default-Profil
  unverändert kompatibel.
- `Pages/SettingsPage.xaml` — `ScrollView`, Abschnitte „Downloads"/
  „Wiedergabe", acht beschriftete Controls mit `AutomationId`,
  `Keyboard="Numeric"`, rotes Inline-Fehlerlabel.
- `Pages/SettingsPage.xaml.cs` — `ModelChanged`-Abo in `OnAppearing`/
  Abmeldung in `OnDisappearing` (korrekte Lebensdauer bei wiederholtem
  Öffnen); `suppressEvents` verhindert Rückkopplung beim Befüllen;
  `SetTextIfNotFocused` überschreibt keine aktive Eingabe; ungültige
  Zahlen zeigen den Hinweis und werden nicht gespeichert; Dispatch
  fire-and-forget mit Fehlerprotokollierung.

### Solution / Tests

- `VideoPlayer.App.sln` — `VideoPlayer.Storage` mit allen Konfigurationen.
- `VideoWebPlayer.Maui.Tests.csproj` — Referenz auf `VideoPlayer.Storage`.
- `Stubs/InMemorySettingsStore.cs`, `Stubs/InMemorySecretStore.cs` —
  einfache Fakes der Abstraktionen.
- Neue Testdateien (siehe `test-results.md`) decken Defaults/Clamping,
  Persistenz-Roundtrips, Profil-Isolation (Schlüssel + Verzeichnisse),
  Secret-Only-Ablage inkl. Legacy-Bereinigung, Layout-Klassifikation,
  reale SQLite-Tabellen/Roundtrips, MVU-Update-Fälle und Quelltext-
  Verträge (Controls, DI, kein Direktzugriff, iOS-Ausschluss,
  Windows-Roaming) ab.
- `AppUpdateTests_Startup.cs`, `MvuHostTests.cs` — an die erweiterte
  Startup-Effektliste angepasst.

## Befunde

— (keine offenen Befunde)

### Behobene Befunde im Verlauf

- `SettingsPage.xaml.cs`: `Entry.Unfocused` erwartet
  `EventHandler<FocusEventArgs>` — gemeinsame Verdrahtung über
  Adapter-Delegate gelöst (CS0029 beim Windows-Build).
- `ModelChanged`-Abo von Konstruktor nach `OnAppearing` verlegt —
  sonst keine Modellaktualisierung nach erneutem Öffnen der Seite.
- NuGet-Warnung `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (Advisory) — explizite
  Pins auf 3.53.3 bzw. `.android` 2.1.13 gesetzt.
