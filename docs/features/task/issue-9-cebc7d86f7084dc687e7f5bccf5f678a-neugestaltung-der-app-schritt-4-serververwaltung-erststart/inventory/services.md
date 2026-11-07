# Logik / Services — Bestandsaufnahme

Analysiert: neue App (`VideoPlayer.Maui/Services`, `VideoPlayer.Storage`,
`VideoPlayer.Mvu`, `VideoPlayer.ApiClient`) sowie die fachliche Referenz des
Discovery-Protokolls im Altbestand. Vollständige Altbestands-Referenz:
`docs/projects/task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app/inventory/services.md`.

## Neue App — `VideoPlayer.Maui/Services`

### `ServerAddressProvider` (`IServerAddressProvider`)

Datei: `VideoPlayer.Maui/Services/ServerAddressProvider.cs` — Singleton (DI)

| Methode | Beschreibung |
|---------|--------------|
| `GetServerAddress()` | Auflösung in Reihenfolge: `ISettingsStore`-Schlüssel `ServerAddress` (profil-präfixiert) → Umgebungsvariable `VIDEOPLAYER_SERVER_URL` → `DefaultAddress` `http://localhost/videoplayer/`. Ergänzt fehlendes `http://`-Schema und schließenden `/`. |

Wird aufgerufen von `ServerHealthProbe`. **Relevanz für die Anforderung:** Der
implizite IIS-Default verhindert derzeit die Erststart-Erkennung (App wirkt
„konfiguriert", obwohl kein Server hinterlegt ist). Kein Zugriff auf
`known_servers`/`backup_state`.

### `ServerHealthProbe` (`IServerHealthProbe`)

Datei: `VideoPlayer.Maui/Services/ServerHealthProbe.cs` — Singleton (DI)

| Methode | Beschreibung |
|---------|--------------|
| `CheckAsync(ct)` | Erstellt kurzlebigen `HttpClient` (10 s Timeout) mit `BaseAddress` aus `IServerAddressProvider`, ruft `VideoPlayerApiClient.CheckHealthAsync`; alle Fehler → `false`. |

Prüft ausschließlich die aktuell aufgelöste Adresse — keine Prüfung einer
beliebigen Kandidatenadresse (für Einrichtung/Discovery nötig).

### `ShellAppEffectHandler` (`IAppEffectHandler`)

Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` — Singleton (DI)

| Effekt | Ablauf |
|--------|--------|
| `CheckServerHealthEffect` | `healthProbe.CheckAsync` → Dispatch `ServerHealthCheckCompletedMessage` auf dem UI-Thread |
| `NavigateToPageEffect` | `NavigateAsync`: tauscht `window.Page` (Loading → `AppShell`) und navigiert per `Shell.Current.GoToAsync("//{route}")` |
| `InitializeStorageEffect` | `AppStorageLayout` auflösen (legt Verzeichnisse an) + `BackupDatabase.InitializeAsync` + `CacheDatabase.InitializeAsync`; Fehler nur geloggt |
| `LoadSettingsEffect` | `AppSettingsService.Load` → Dispatch `SettingsLoadedMessage` |
| `PersistSettingsEffect` | `AppSettingsService.Save`; Fehler nur geloggt |

`NavigateAsync` geht davon aus, dass jedes Navigationsziel eine Shell-Route
(`AppRoutes.ToRoute`) besitzt — ein Einrichtungsdialog ohne Menü-Route
benötigt eine eigene Behandlung (z. B. modale Page statt Shell-Route).

### `App` / `MauiProgram`

Dateien: `VideoPlayer.Maui/App.xaml.cs`, `VideoPlayer.Maui/MauiProgram.cs`

- `App.CreateWindow`: Root = `LoadingPage` (Transient, DI); dispatcht einmalig
  `StartupRequestedMessage`.
- `MauiProgram` registriert: `IServerAddressProvider`, `IServerHealthProbe`,
  `IAppEffectHandler`, `MvuHost`, `AppProfile.FromEnvironment`,
  `PreferencesSettingsStore` (`Preferences.Default`), `SecureStorageSecretStore`
  (`SecureStorage.Default`), `AppSettingsService`, `AppSecretsService`,
  `AppStorageLayoutProvider.CreateLayout`, `BackupDatabase`, `CacheDatabase`,
  `AppShell` (Singleton) und alle Pages als Transient.
- Kein `ServerSetupPage`-Äquivalent, kein Discovery-Dienst, kein
  Serververwaltungsdienst registriert.

### `SettingsPage`

Dateien: `VideoPlayer.Maui/Pages/SettingsPage.xaml(.cs)`

- Acht Einstellungs-Controls (4 `Switch`, 4 numerische `Entry`) +
  `InvalidInputLabel`; `AutomationId` auf jedem Control (Contract-Test).
- Code-Behind: `BindingContext = MvuHost`, `ModelChanged`-Abo,
  `PopulateFromModel` mit `suppressEvents`; Änderungen dispatcht
  `SaveSettingsRequestedMessage`.
- Kein „Server wechseln"-Eintrag vorhanden.

## Neue App — `VideoPlayer.Storage`

### `BackupDatabase`

Datei: `VideoPlayer.Storage/BackupDatabase.cs`

| Methode | Beschreibung |
|---------|--------------|
| `Connection` | `Lazy<SQLiteAsyncConnection>` auf `AppStorageLayout.DataDatabasePath` |
| `InitializeAsync` | legt `KnownServerEntry`, `BackupStateEntry`, `PendingProgressSyncEntry` an (idempotent) |

Keine fachlichen Zugriffsmethoden — Aufrufer arbeiten direkt auf
`Connection.Table<T>` (bisher nur Tests).

### `AppSettingsService` / `AppSecretsService` / `AppProfile` / `AppStorageLayout`

Siehe Schritt-3-Inventory: Einstellungen bzw. Geheimnisse über
`ISettingsStore`/`ISecretStore` mit Profil-Präfix; `AppProfile` isoliert
Schlüssel und Dateiablagen über `VIDEOPLAYER_PROFILE`. **Hinweis:** Die
`BackupDatabase` liegt profil-isoliert im `BackupDirectory` (Unterverzeichnis
bei Nicht-Default-Profil) — Serverliste und „zuletzt verbunden" sind damit
automatisch profil-isoliert.

## Neue App — `VideoPlayer.Mvu`

| Typ | Inhalt |
|-----|--------|
| `AppUpdate.Update` | behandelt `StartupRequestedMessage`, `ServerHealthCheckCompletedMessage`, `NavigateToMessage`, `NavigatedMessage`, `LoadSettingsRequestedMessage`, `SettingsLoadedMessage`, `SaveSettingsRequestedMessage`; Start emittiert `[InitializeStorageEffect, LoadSettingsEffect, CheckServerHealthEffect]`; Health-Abschluss navigiert stets nach `Home` |
| `MvuHost` | serialisierte `DispatchAsync` (SemaphoreSlim), `ModelChanged`/`PropertyChanged` nach jedem Update |
| `AppRoutes` | Mapping `AppPage` ↔ Shell-Route für die sechs Menüseiten; `ToRoute` wirft für `Loading` |

## Neue App — `VideoPlayer.ApiClient`

`VideoPlayerApiClient.CheckHealthAsync` (`GET api/health`, Retry-Policy,
Client-Gate-Header) liefert `bool`; `BaseAddress` kommt vom injizierten
`HttpClient` — für beliebige Kandidatenadressen wiederverwendbar.

## Altbestand — Discovery-Referenz (nur Protokoll, kein Code-Transfer)

### `SettingsService` (`VideoWebPlayer.Maui/Services/SettingsService.cs`)

| Methode | Protokoll |
|---------|-----------|
| `DiscoverServersAsync(timeoutMs = 3000)` | `ZeroconfResolver.ResolveAsync("_http._tcp.local.")` → `http://{ip}:{port}` je gefundenem Dienst |
| `DiscoverServersUdpAsync(port = 5001, timeoutMs = 2000)` | UDP-Broadcast `VIDEOWEBPLAYER_DISCOVERY` an `255.255.255.255:5001`; Antworten mit Präfix `VIDEOWEBPLAYER_SERVER:` → Adresse übernehmen |

### `ServerSetupPage` (Altbestand, nur fachliche Referenz)

`VideoWebPlayer.Maui/ServerSetupPage.xaml.cs`: reine Code-Behind-Seite —
Adress-`Entry`, „Automatisch suchen" (mDNS + UDP, Ergebnis per
`DisplayActionSheet` auswählbar), „Verbindung herstellen" (speichert ohne
Prüfung), „Abbrechen" (beendet die App). **Unterschied zur Anforderung:** Die
Neuimplementierung muss die Verbindung vor dem Speichern prüfen
(`CheckHealthAsync`) und gefundene Server in einer Auswahl anbieten.

## Serverseitig (Repo `martin-stromberg/VideoPlayer`, nicht Teil dieses Repos)

- `VideoWebPlayer/Services/UdpDiscoveryListener.cs`: UDP-Listener auf
  Port 5001; auf `VIDEOWEBPLAYER_DISCOVERY` antwortet er
  `VIDEOWEBPLAYER_SERVER:{serverAddress}` mit
  `serverAddress = http://{Host:Address ?? localhost}:{Host:Port ?? 5000}`.
- `Program.cs`: Listener wird gestartet, außer in Environment `Testing`.
- **Kein** mDNS-/Zeroconf-Advertisement im Server-Repo vorhanden.
