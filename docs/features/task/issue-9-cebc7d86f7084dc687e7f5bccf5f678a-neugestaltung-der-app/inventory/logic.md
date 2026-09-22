# Logik

## `AppUpdate`

Datei: `VideoPlayer.Mvu/AppUpdate.cs` (1819 Zeilen)

Reine Update-Funktion des MVU-Zyklus: `Update(AppModel, AppMessage) → AppTransition` bildet jede Nachricht per `switch` auf eine private `Update…`-Methode ab (Zeilen 46–124). Für die Anforderung relevante Übergänge:

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Update` | `public static` | Zentrale Dispatch-Tabelle aller `AppMessage`-Typen (u. a. `SourcesPageOpenedMessage`, `SourcesLoadedMessage`, `SourceSelectedMessage`, `SynchronizationCompletedMessage`, `CachedListsChangedMessage`, `ServerEventReceivedMessage`, `NetworkAvailabilityChangedMessage`). |
| `UpdateStartupRequested` | `private static` | Stößt `InitializeStorageEffect`, `LoadSettingsEffect`, `LoadServersEffect`, `StartNetworkMonitoringEffect` und `LoadHomeListsEffect` an (Cache-First der Startseite) — **kein Quellen-Laden**. |
| `EnterConnected` | `private static` | Übergang in `ConnectionState.Connected` (Zeilen 430–465): setzt `IsSyncRunning = true`, `IsOnline = true`; Effekte `ConnectServerEventsEffect`, `RunFullSyncEffect` (dessen `SyncMetadataAsync` den Quellen-Cache schreibt), `ResumeDownloadsEffect`; navigiert von Workflow-Seiten zu `Home`. **Kein Quellen-Laden für ein Menü.** |
| `UpdateSourcesPageOpened` | `private static` | Zeilen 652–672: Offline → `SourcesOverview` auf `Initial` zurücksetzen, kein Ladevorgang; `Connected` → `IsLoading` + `LoadSourcesEffect`; sonst unverändert. |
| `UpdateSourcesLoaded` | `private static` | Zeilen 674–687: übernimmt `message.Sources`/`message.Error` in `SourcesOverview` (`Attempted = true`). |
| `UpdateSourceSelected` | `private static` | Zeilen 697–711: bei `Source.Id > 0` neuer `SourceBrowser`-Zustand (`NewBrowser`) + Effekte `NavigateToSourceOverviewEffect`, `LoadSourceGenresEffect`, `LoadSourceItemsEffect(Seite 0)` — der komplette „Quelle öffnen"-Pfad, der von Menüeinträgen wiederverwendet werden kann. |
| `UpdateSourceBrowserOpened` | `private static` | Zeilen 713–735: gleiche Quelle → unverändert (Rückkehr behält Listenstand); abweichende Quelle → `NewBrowser` + Genre-/Eintrags-Laden **ohne** Navigation (Wiederherstellungspfad der `SourceOverviewPage`). |
| `NewBrowser` | `private static` | Zeilen 689–695: initialer `SourceBrowserState` (`IsLoading`, `HasMore`). |
| `UpdateSourceGenresLoaded` / `UpdateSourceItemsLoaded` | `private static` | Übernehmen Genres/Eintragsseiten; verwerfen Antworten fremder `SourceId` bzw. veralteter Append-Seiten. |
| `UpdateSourceGenreSelected` / `UpdateSourceSearchSubmitted` / `UpdateSourceItemsNextPageRequested` / `UpdateSourceItemsRetryRequested` | `private static` | Filter-/Such-/Paging-Übergänge der Browsing-Seite (`ReloadBrowserItems` lädt ab Seite 0 neu). |
| `UpdateSourceMediaItemSelected` | `private static` | Auswahl eines Eintrags → `NavigateToMediaDetailsEffect`. |
| `UpdateNetworkAvailabilityChanged` | `private static` | Zeilen 475–514: Netzwerkverlust aus `Connected` → `ConnectionState.Offline` + `DisconnectServerEventsEffect` — **ohne** Rücksetzen von `SourcesOverview`/`SourceBrowser` (das erfolgt erst beim nächsten `SourcesPageOpened` bzw. Logout). Online-Rückkehr aus `Offline` → `CheckServerHealthEffect` (Wiederverbindungs-Kette). |
| `UpdateSynchronizationCompleted` | `private static` | Zeilen 516–528: `IsSyncRunning = false` + `LoadHomeListsEffect` (+ `RunAutoDownloadsEffect` bei aktiviertem Vorlauf) — kein Quellen-Reload. |
| `UpdateCachedListsChanged` | `private static` | Zeilen 530–541: `LoadHomeListsEffect` (+ `RunAutoDownloadsEffect`) — reagiert auf Teil-Syncs; der Quellen-Cache wird dabei **nicht** neu gelesen. |
| `UpdateServerEventReceived` | `private static` | Zeilen 543–560: nur in `Connected` wirksam; `NewVideosScanned` → `SyncMetadataEffect`, `ContinueWatchingUpdated` → `SyncContinueWatchingEffect`, `FavoritesChanged` → `SyncFavoritesEffect`. |
| `UpdateServerEventsConnectionClosed` | `private static` | Neu-Verbinden in `Connected` + online (`ConnectServerEventsEffect`). |
| `UpdateSessionExpired` | `private static` | → `NeedsLogin` + `DisconnectServerEventsEffect` + Navigation zur Anmeldemaske; `SourcesOverview` bleibt unverändert. |
| `UpdateLogoutRequested` | `private static` | Zeilen 1200–1250: nur aus `Connected`/`Offline` wirksam; setzt u. a. `SourcesOverview = SourcesOverviewState.Initial` und `SourceBrowser = SourceBrowserState.Empty` zurück (Zeilen 1231–1232); Effekte `DisconnectServerEventsEffect` + `LogoutEffect` (+ `NavigateToPageEffect(Login)`). |
| `UpdateNavigateTo` | `private static` | Zeilen 1776–1796: programmatische Menü-Navigation; in `NeedsLogin`/`NeedsServerSetup` sind Menüseiten gesperrt. |
| `UpdateNavigated` | `private static` | Zeilen 1798–1818: übernimmt Shell-Navigation ins Model; bei ausstehendem Workflow zurück auf Login/ServerSetup. |
| `IsWorkflowPage` | `private static` | `Loading`, `Login`, `ServerSetup` sind Workflow-Seiten ohne Menüeintrag. |

## `SourceBrowserService`

Datei: `VideoPlayer.Mvu/SourceBrowserService.cs`

Lädt Quellen-/Browsing-Daten über den angemeldeten `AuthSessionService.Client` (`VideoPlayerApiClient`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `AllGenresName` | `public const string` | Anzeigename „Alle" des künstlichen Genre-Eintrags (Id 0). |
| `LoadSourcesAsync` | `public` | `GET api/Sources` → `IReadOnlyList<SourceItem>` (`Id`, `Name`, `IconPictureId`); Einträge mit `Id <= 0` werden übersprungen. Wirft `InvalidOperationException` ohne Client — direkt für das Menü-Laden wiederverwendbar. |
| `LoadGenresAsync` | `public` | `api/SourceGenres/{id}` → Genres mit vorangestelltem „Alle"-Eintrag; ohne Client nur der „Alle"-Eintrag. |
| `LoadItemsAsync` | `public` | `api/items` einer Eintragsseite (Paging, `search`, `genreId`); `HasMore` solange die Seite voll ist (`PageSize` = 20). |
| `MapSummaryType` | `internal static` | Typ-Mapping der Einträge (Filme → `moviecollection` usw.). |

## `ServerSyncService`

Datei: `VideoPlayer.Mvu/ServerSyncService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SourcesCacheKey` | `public const string` | `cached_lists`-Schlüssel `"sources"` (Zeile 33). |
| `RecentCacheKey` / `ContinueWatchingCacheKey` | `public const string` | Schlüssel `"recent"` / `"continue_watching"`. |
| `SynchronizeAsync` | `public` | Voll-Sync: `FlushPendingProgressAsync` → `SyncMetadataAsync` → `SyncContinueWatchingAsync` → `SyncFavoritesAsync`; liefert `SyncResult`. Wird vom `RunFullSyncEffect` ausgelöst. |
| `SyncMetadataAsync` | `public` | Zeilen 142–152: `GET api/Sources` → `cached_lists["sources"]` (JSON der `MediaSource`-Liste) und `GET …/recent` → `cached_lists["recent"]`. |
| `SyncContinueWatchingAsync` | `public` | Erneuert `cached_lists["continue_watching"]`. |
| `SyncFavoritesAsync` | `public` | Ersetzt die Tabelle `cached_favorites`. |
| `TrySyncAsync` / `WriteCachedListAsync` / `MediaTypeOf` | `private` | Fehlerbehandlung (`false` ohne Client/bei Fehler), `InsertOrReplaceAsync` des `CachedListEntry`, Typname-Mapping. |

`SyncResult`-Record (Zeilen 16–21): `SentProgressReports`, `RemainingProgressReports`, `MetadataSynced`, `ContinueWatchingSynced`, `FavoritesSynced`.

## `HomeListService`

Datei: `VideoPlayer.Mvu/HomeListService.cs`

Cache-First-Leser der `CacheDatabase` — **Referenzmuster** für einen Quellen-Cache-Leser. Liest aktuell `continue_watching`, `recent` und `cached_favorites`; den Schlüssel `"sources"` liest **kein** Dienst.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadHomeListsAsync` | `public` | Liest alle drei Startseiten-Listen; beschädigte Einträge werden verworfen und über `HomeLists.DiscardedCaches` gemeldet. |
| `LoadFavoritesAsync` / `LoadRecentAsync` | `public` | Einzelleser der Favoriten-/Recent-Kopie (Offline-Startseite). |
| `LoadFavoriteItemsAsync` | `public` | Liest `cached_favorites` als `FavoriteListItem`s der Favoriten-Seite. |
| `FavoritesCacheName` | `internal const string` | `"cached_favorites"` für `DiscardedCaches`. |
| `ReadListAsync<T>` / `ReadFavoritesAsync` | `private` | Generischer `cached_lists`-Leser (`Key` → `Json` deserialisieren, `PropertyNameCaseInsensitive`) bzw. Favoriten-Tabellenleser; bei Fehlern leere Liste + Discard. |
| `DiscardListEntryAsync` / `DiscardRowAsync` | `private` | Verwerfen beschädigter Cache-Einträge. |
| `MapAll` / `MapContinueWatching` / `MapEntry` / `MapFavorite` / `MediaHints` / `MediaTypeOf` | `private static` | Mapping auf anzeigefertige `HomeMediaItem`/`FavoriteListItem`; Einträge ohne identifizierbares Medium werden übersprungen. |

## `MvuHost`

Datei: `VideoPlayer.Mvu/MvuHost.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Model` | `public AppModel` | Aktuelles unveränderliches App-Model. |
| `ModelChanged` | `public event EventHandler` | Wird nach **jedem** angewendeten Update ausgelöst — derzeit abonniert von den Pages (`SourcesPage`, `SourceOverviewPage`, `HomePage` u. a.), **nicht** von `AppShell`. |
| `PropertyChanged` | `public event PropertyChangedEventHandler` | `INotifyPropertyChanged`-Benachrichtigung für `Model`. |
| `DispatchAsync` | `public` | Serialisiert Update + Effektausführung über `SemaphoreSlim`; führt Effekte nacheinander über den `IAppEffectHandler` aus. |

## `AppRoutes`

Datei: `VideoPlayer.Mvu/AppRoutes.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Routes` | `private static` | `AppPage` → Shell-Route: `home`, `sources`, `favorites`, `downloads`, `profile`, `settings`. |
| `MenuPages` | `public static` | Die sechs Menüseiten in Menü-Reihenfolge; gepinnt durch `AppRoutesTests`. |
| `ToRoute` | `public static` | Route einer Menüseite; wirft `ArgumentOutOfRangeException` für Nicht-Menüseiten. |
| `TryGetPage` | `public static` | Löst einen Shell-Ort (Pfadsegmente, Query-agnostisch) auf eine Menüseite auf; `sourceoverview` liefert **keinen** Treffer — `CurrentPage` bleibt beim Push auf die Browsing-Seite unverändert. |

## `AppShell` (XAML + Code-Behind)

Dateien: `VideoPlayer.Maui/AppShell.xaml`, `VideoPlayer.Maui/AppShell.xaml.cs`

- XAML (29 Zeilen): `Shell` mit `FlyoutBehavior="Flyout"` und **sechs statischen `FlyoutItem`s** — `Startseite`/`home`, `Quellen`/`sources`, `Favoriten`/`favorites`, `Downloads`/`downloads`, `Profil`/`profile`, `Einstellungen`/`settings`; jedes mit einem `ShellContent` + `ContentTemplate` auf die zugehörige Page. Keine `MenuItem`s, kein `FlyoutContent`, keine dynamischen Einträge.
- Code-Behind: Konstruktor `AppShell(MvuHost)` registriert die geschobenen Routen `serversetup`, `login`, `mediadetails`, `sourceoverview`, `player` via `Routing.RegisterRoute` und abonniert `Navigated`.
- `OnShellNavigated` → `AppRoutes.TryGetPage(e.Current?.Location?.OriginalString)` → `DispatchNavigatedAsync` → `NavigatedMessage`. Für `sourceoverview`/`mediadetails`/`player`/`serversetup`/`login` findet `TryGetPage` keinen Treffer — keine Message.
- Es gibt **kein** `ModelChanged`-Abonnement in der Shell.

## `ShellAppEffectHandler`

Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` (1033 Zeilen)

`IAppEffectHandler`-Implementierung am App-Rand; `HandleAsync` (Zeilen 68–199) dispatcht jeden `AppEffect`-Typ per `switch`. Für die Anforderung relevant:

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `HandleAsync` | `public` | Effekt-Dispatch; u. a. `LoadSourcesEffect` → `LoadSourcesAsync`, `NavigateToSourceOverviewEffect` → `NavigateToSourceOverviewAsync`, `RunFullSyncEffect` → `RunFullSyncAsync`, `SyncMetadataEffect`/`SyncFavoritesEffect`/`SyncContinueWatchingEffect` → `RunPartialSyncAsync`, `LoadHomeListsEffect` → `LoadHomeListsAsync`. |
| `LoadSourcesAsync` | `private` | Zeilen 428–444: `SourceBrowserService.LoadSourcesAsync`; bei Fehler `Error = "Die Quellen konnten nicht geladen werden."`; dispatcht `SourcesLoadedMessage(sources, error)` — direkt für ein Menü-Laden wiederverwendbar. |
| `NavigateToSourceOverviewAsync` | `private` | Zeilen 409–426: `Shell.Current.GoToAsync("sourceoverview?id=…&name=…")` (URI-escaped, Push auf den Stack). |
| `RunFullSyncAsync` | `private` | `ServerSyncService.SynchronizeAsync` → `SynchronizationCompletedMessage`. |
| `RunPartialSyncAsync` | `private` | Zeilen 336–350: führt eine Teil-Synchronisation aus und dispatcht **immer** `CachedListsChangedMessage` — auch nach `SyncMetadataEffect` (Quellen-Cache erneuert). |
| `LoadHomeListsAsync` | `private` | Zeilen 352–373: offline `OfflineContinueWatchingService`, sonst `HomeListService` → `HomeListsLoadedMessage`. |
| `LoadSourceGenresAsync` / `LoadSourceItemsAsync` | `private` | Genre-/Eintrags-Laden → `SourceGenresLoadedMessage`/`SourceItemsLoadedMessage` (Fehler → „Alle"-Fallback bzw. Fehlertext). |
| `NavigateAsync` | `private` | Zeilen 973–1033: Menü-Navigation `//{route}`; `ServerSetup`/`Login` als Window-Root bzw. geschobene Route. |
| `DispatchMessageAsync` | `private` | Zeilen 715–726: marshal´d jede Ergebnis-Message über `MainThread` in den `MvuHost`. |
| Event-Handler `OnNetworkAvailabilityChanged`, `OnServerEventReceived`, `OnServerEventsConnectionClosed`, `OnSessionExpired`, `OnDownloadItemChanged`, `OnDownloadItemRemoved` | `private` | Übersetzen Dienst-Events in MVU-Messages (Netzwerk, Server-Ereignisse, Sitzungsablauf, Downloads). |

## `PosterImageService`

Datei: `VideoPlayer.Maui/Services/PosterImageService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetImageSourceAsync` | `public` | Poster `api/pictures/{id}` → `ImageSource?` (Memory-Cache + Datei `{id}.poster` im nicht backup-pflichtigen `PreviewsDirectory`). |
| `GetSourceIconSourceAsync` | `public` | Zeilen 96–142: Quellen-Icon `api/sourceicons/{id}` → `ImageSource?` (eigener `iconMemoryCache` + Datei `{id}.sourceicon`); `null` ohne Client/Icon/Fehler — liefert ein `ImageSource`, das `FlyoutItem.Icon`/`MenuItem.IconImageSource` direkt akzeptiert. |

## `CachedSourceIconImage`

Datei: `VideoPlayer.Maui/Controls/CachedSourceIconImage.cs`

`Image`-Control mit Bindable Property `PictureId` (`long?`); löst die Icon-Id über `PosterImageService.GetSourceIconSourceAsync` auf und setzt `Source`. Wird in `SourcesPage.xaml` verwendet; für Shell-Menüeinträge nicht direkt nutzbar (dort wird ein `ImageSource` benötigt).

## `SourcesPage`

Dateien: `VideoPlayer.Maui/Pages/SourcesPage.xaml(.cs)`

- Inhalt der Route `sources` (FlyoutItem „Quellen"): `CollectionView` an `Model.SourcesOverview.Sources`, Karten mit `CachedSourceIconImage` + `Name`; Offline-Banner („Offline – die Quellenliste steht ohne Verbindung zum Server nicht zur Verfügung."), Ladeindikator, Fehlerpanel mit „Erneut laden", Leer-Label.
- Code-Behind: `OnAppearing` → `SourcesPageOpenedMessage`; Auswahl → `SourceSelectedMessage`; `ModelChanged` → `UpdateUi` (Banner/Fehler/Leer-Zustände); Retry → `SourcesPageOpenedMessage`.

## `SourceOverviewPage`

Dateien: `VideoPlayer.Maui/Pages/SourceOverviewPage.xaml(.cs)`

- Navigationsziel `sourceoverview?id=…&name=…` (`[QueryProperty]` `SourceId`/`SourceName`; `Title` = Quellenname).
- `OnAppearing` dispatcht `SourceBrowserOpenedMessage(sourceId, sourceName)`, wenn `SourceBrowser.SourceId` abweicht oder noch nichts geladen wurde — die Seite kann daher auch ohne vorheriges `SourceSelectedMessage` ihren Zustand selbst initialisieren.
- UI: `SearchBar` (→ `SourceSearchSubmittedMessage`), Genre-`CollectionView` (→ `SourceGenreSelectedMessage`), Medien-Raster mit `RemainingItemsThresholdReached` (→ `SourceItemsNextPageRequestedMessage`), Fehler-/Leer-Zustände, Retry (→ `SourceItemsRetryRequestedMessage`), Auswahl (→ `SourceMediaItemSelectedMessage`).

## `MauiProgram`

Datei: `VideoPlayer.Maui/MauiProgram.cs`

DI-Registrierungen (alle Singleton, Seiten Transient): u. a. `AuthSessionService`, `IAppEffectHandler` → `ShellAppEffectHandler`, `MvuHost`, `CacheDatabase`, `ServerSyncService`, `HomeListService`, `OfflineContinueWatchingService`, `PosterImageService`, `SourceBrowserService`, `MediaDetailsService`, `FavoritesService`, `AppShell`, alle Pages inkl. `SourcesPage` und `SourceOverviewPage`. Ein Cache-Leser für Quellen ist **nicht** registriert (müsste ggf. ergänzt werden — Muster `HomeListService`).

## `AuthSessionService`

Datei: `VideoPlayer.Mvu/AuthSessionService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Client` | `public VideoPlayerApiClient?` | Zuletzt erfolgreich angemeldeter API-Client (Bearer-Token, stille Neuanmeldung verdrahtet); `null` ohne Sitzung — Quelle aller Server-Aufrufe (`LoadSourcesAsync`, Icon-Abrufe, Sync). |
| `ClientBaseAddress` | `public string?` | Basisadresse des angemeldeten Servers. |
| `SessionExpired` | `public event` | Endgültig abgelehnte stille Neuanmeldung → `SessionExpiredMessage`. |
| `TryStoredLoginAsync` / `LoginAsync` / `LogoutAsync` | `public` | Anmelde-Workflow; `LogoutAsync` setzt `Client` auf `null` und löscht Geheimnisse. |

## `VideoPlayerApiClient`

Datei: `VideoPlayer.ApiClient/VideoPlayerApiClient.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetSourcesAsync` | `public` | `GET api/Sources` → `IReadOnlyList<MediaSource>` (nur freigeschaltete Quellen). |
| `GetSourceAsync` | `public` | `GET api/Sources/{id}` → einzelne Quelle. |
| `GetSourceIconAsync` | `public` | `GET api/sourceicons/{id}` → `byte[]` (Zeilen 405–414). |
| `GetSourceGenresAsync` / `GetSourceItemsAsync` / `GetRecentEntriesAsync` / `GetContinueWatchingAsync` / `GetFavoritesAsync` | `public` | Übrige Endpunkte des Browsing-/Sync-Strangs. |

## `ServerEventsService`

Datei: `VideoPlayer.Mvu/ServerEventsService.cs`

Orchestriert die SignalR-Verbindung (`hubs/mediaupdate`); übersetzt Verbindungs-Events in `EventReceived` (`ServerEventReceivedEventArgs` mit `Kind`, `SourceId`, `Count`) und `ConnectionClosed`. `NewVideosScanned` trägt die betroffene `SourceId` — führt über `UpdateServerEventReceived` zu `SyncMetadataEffect` (erneuert u. a. den Quellen-Cache).

## `OfflineContinueWatchingService`

Datei: `VideoPlayer.Mvu/OfflineContinueWatchingService.cs`

Liefert `LoadHomeListsAsync` für den Offline-Modus (Weiterschauen aus lokalen Downloads) — wird im `LoadHomeListsEffect`-Pfad des `ShellAppEffectHandler` statt `HomeListService` verwendet. Kein Quellen-Bezug.
