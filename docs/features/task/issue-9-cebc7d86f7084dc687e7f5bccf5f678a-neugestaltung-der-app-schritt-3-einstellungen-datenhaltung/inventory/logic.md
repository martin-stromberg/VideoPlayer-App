# Logik und Services

## Neue App

### `AppUpdate` (`VideoPlayer.Mvu/AppUpdate.cs`, statisch)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Update(AppModel, AppMessage)` | public | Reine Update-Funktion: Message → `AppTransition` (Model + Effekte) |
| `UpdateStartupRequested` | private | Startet Erreichbarkeitsprüfung (`CheckServerHealthEffect`) |
| `UpdateHealthCheckCompleted` | private | Setzt Status, navigiert zu `Home` |
| `UpdateNavigateTo` / `UpdateNavigated` | private | Seitenwechsel / Model-Sync |

### `MvuHost` (`VideoPlayer.Mvu/MvuHost.cs`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `DispatchAsync(AppMessage, CancellationToken)` | public | Serialisiert Updates (`SemaphoreSlim`), setzt `Model`, feuert `ModelChanged`/`PropertyChanged`, führt Effekte über `IAppEffectHandler` aus |
| `Model` | public | Aktuelles unveränderliches Model |
| `ModelChanged` / `PropertyChanged` | public | Benachrichtigung nach jedem Update (für UI-Bindings) |

### `ShellAppEffectHandler` (`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `HandleAsync(AppEffect, CancellationToken)` | public | Dispatcht `CheckServerHealthEffect` → `ServerHealthProbe`, `NavigateToPageEffect` → Shell/`GoToAsync` |
| `RunHealthCheckAsync` | private | Fehler → `reachable=false`; dispatcht `ServerHealthCheckCompletedMessage` auf UI-Thread |
| `NavigateAsync` | private | Tauscht `Window.Page` gegen `AppShell`, dann `GoToAsync("//route")` |

### `ServerAddressProvider` (`VideoPlayer.Maui/Services/ServerAddressProvider.cs`)

| Member | Beschreibung |
|--------|--------------|
| `GetServerAddress()` | `Preferences.Default.Get("ServerAddress")` → Env `VIDEOPLAYER_SERVER_URL` → Default `http://localhost/videoplayer/`; ergänzt Schema und abschließenden `/` |
| `PreferencesKey` = `"ServerAddress"`, `EnvironmentVariable` = `"VIDEOPLAYER_SERVER_URL"`, `DefaultAddress` | Konstanten |

**Befund:** Direkter `Preferences`-Zugriff ohne Profil-Präfix — mit Einführung der
Profil-Isolation muss dieser Zugriff über die neue Ablage-Abstraktion laufen.

### `ServerHealthProbe` (`VideoPlayer.Maui/Services/ServerHealthProbe.cs`)
Erzeugt `HttpClient` (10 s Timeout) auf `IServerAddressProvider.GetServerAddress()`
und ruft `VideoPlayerApiClient.CheckHealthAsync`.

### `MauiProgram` (`VideoPlayer.Maui/MauiProgram.cs`)
DI: Singletons `IServerAddressProvider`, `IServerHealthProbe`, `IAppEffectHandler`
(`ShellAppEffectHandler`), `MvuHost`, `AppShell`; Transients der sieben Pages.

### `App` (`VideoPlayer.Maui/App.xaml.cs`)
`CreateWindow` → `Window` mit `LoadingPage`; `RunStartupAsync` dispatcht
`StartupRequestedMessage`.

### `AppShell` (`VideoPlayer.Maui/AppShell.xaml(.cs)`)
Flyout mit sechs Einträgen; `Navigated` → `NavigatedMessage` via `AppRoutes`.

### `SettingsPage` (`VideoPlayer.Maui/Pages/SettingsPage.xaml(.cs)`)
Grundgerüst: Titel + Hinweistext, parameterloser Konstruktor, kein BindingContext,
keine Bedienelemente für Einstellungen.

## Referenz Alt-App (`VideoWebPlayer.Maui` — nur fachliche Referenz)

Details siehe `docs/projects/task/.../inventory/services.md`. Relevante Muster:

- `SettingsService` (`VideoWebPlayer.Maui/Services/SettingsService.cs`):
  `Preferences.Default` für nicht-geheime Werte, `SecureStorage.Default` für
  `Passkey`/`AuthToken`; `GetPrefixedKey` = `ProfileManager.GetPreferencesPrefix() + key`;
  `GetMigratedSecureValue` migriert Legacy-Preferences nach `SecureStorage`.
- `ProfileManager` (`VideoWebPlayer.Maui/Services/ProfileManager.cs`):
  `CurrentProfile` aus Umgebungsvariable `VIDEOWEBPLAYER_PROFILE` (lowercase,
  Default `"default"`); `GetDatabaseFileName()` → `downloads[_profil].db3`;
  `GetPreferencesPrefix()` → `""` bzw. `"{profil}_"`.
- `ClientDatabase` (`VideoWebPlayer.Maui/Services/ClientDatabase.cs`):
  `SQLiteAsyncConnection` (`sqlite-net-pcl`) auf
  `FileSystem.AppDataDirectory/downloads[_profil].db3`; Tabellen `Downloads`,
  `CarouselCache`. Kein Backup-Ausschluss, kein `CacheDirectory`-Einsatz.
- `DownloadManager` (`VideoWebPlayer.Maui/Services/DownloadManager.cs`):
  Videodateien unter `AppDataDirectory/videos[_profil]`, Bilder unter
  `AppDataDirectory/images` — ebenfalls ohne Backup-Klassifikation.

## Nicht vorhanden (für diese Anforderung relevant)

- Kein Einstellungsmodell/-dienst, keine Geheimnisablage in der neuen App.
- Keine Profil-Isolation (Umgebungsvariable) in der neuen App.
- Keine lokale Datenbank und keine strukturierten Dateiablagen in der neuen App.
- Kein Backup-Ausschluss / keine Backup-Klassifikation im gesamten Repo
  (kein `SetSkipBackupAttribute`, kein `FileSystem.CacheDirectory`-Einsatz).
- Keine Einstellungen für automatische Downloads, Auto-Fortsetzung, WLAN-only,
  Zeitfenster, Speicherplatzkontingent, Auto-Löschung (auch in der Alt-App nicht
  vorhanden).
