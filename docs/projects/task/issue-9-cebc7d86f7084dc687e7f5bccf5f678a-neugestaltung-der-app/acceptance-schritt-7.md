# Abnahmeprüfung – Entwicklungsschritt 7

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

- Keine.

## Hinweise

Geprüft auf Branch `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-7-startseite` (HEAD: `0f4391b`), Diff gegen `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app` (44 Dateien, +3004/−50).

### Drei horizontale Listen mit korrekten Datenquellen

- `HomePage.xaml:37-45` rendert drei `MediaCarousel`-Abschnitte „Weiterschauen", „Favoriten" und „Neu im Programm", gebunden an `Model.ContinueWatching`, `Model.Favorites` und `Model.RecentEntries`. `MediaCarousel.xaml:14-18` ist ein horizontaler `ScrollView` mit `BindableLayout.ItemsSource`; leere Listen blenden den Abschnitt aus (`MediaCarousel.xaml.cs:48-54`).
- Datenquellen: `ServerSyncService.SyncContinueWatchingAsync` → `GetContinueWatchingAsync` (`GET api/continue-watching`, `VideoPlayerApiClient.cs:165`) → `cached_lists`-Schlüssel `continue_watching` (`ServerSyncService.cs:33,140-147`); `SyncFavoritesAsync` → `GetFavoritesAsync` (`GET api/favorites`, `VideoPlayerApiClient.cs:228`) → Tabelle `cached_favorites` als lokale Kopie (`ServerSyncService.cs:150-169`); `SyncMetadataAsync` → `GetRecentEntriesAsync` (`GET api/items/recent`, `VideoPlayerApiClient.cs:155`) → `cached_lists`-Schlüssel `recent` (`ServerSyncService.cs:30,127-137`).
- `HomeListService.LoadHomeListsAsync` (`HomeListService.cs:36-48`) liest alle drei Quellen aus der `CacheDatabase` und mappt auf `HomeMediaItem` (Id, Medientyp, Titel, `PosterPictureId`, Fortschritt nur bei Weiterschauen, `HomeListService.cs:122-156`); beschädigte oder fehlende Cache-Einträge ergeben leere Teillisten statt Fehler (`HomeListService.cs:50-101`).

### Cache-First-Verhalten

- `UpdateStartupRequested` (`AppUpdate.cs:85-97`) emittiert `LoadHomeListsEffect` direkt nach `InitializeStorageEffect` — die gecachten Listen werden ins Model übernommen, bevor Health-Check, Anmeldung oder Synchronisation eine Serverantwort liefern; bei Offline-Start bleiben die Inhalte sichtbar (Offline-Banner zusätzlich, `HomePage.xaml:13-22`).
- Nach dem Voll-Sync lädt `SynchronizationCompletedMessage` die Listen aus dem aktualisierten Cache neu (`AppUpdate.cs:62-66` → `LoadHomeListsEffect` → `HomeListsLoadedMessage` → `UpdateHomeListsLoaded`, `AppUpdate.cs:505-515`).

### Server-Ereignisse → Listenaktualisierung ohne manuelle Aktualisierung

- Vollständige Kette verifiziert: SignalR-Hub `hubs/mediaupdate` (`SignalRServerEventsConnection.cs:42-47`) → `ServerEventsService.EventReceived` (`ServerEventsService.cs:140-147`) → `ServerEventReceivedMessage` über MainThread-Dispatch (`ShellAppEffectHandler.cs:232-233`) → `UpdateServerEventReceived` mappt `ContinueWatchingUpdated`/`FavoritesChanged`/`NewVideosScanned` auf `SyncContinueWatchingEffect`/`SyncFavoritesEffect`/`SyncMetadataEffect` (`AppUpdate.cs:437-454`) → `RunPartialSyncAsync` schreibt den Cache und dispatcht `CachedListsChangedMessage` (`ShellAppEffectHandler.cs:175-189`) → `LoadHomeListsEffect` (`AppUpdate.cs:67`) → `HomeListsLoadedMessage` → Model-Update → UI-Binding via `MvuHost.PropertyChanged` (`MvuHost.cs:54`).
- Nach endgültigem Verbindungsabbruch wird die Ereignisverbindung im Online-Zustand automatisch neu aufgebaut (`AppUpdate.cs:456-464`).

### Auswahl → Navigation zur Detailseite

- Tap auf eine Karte (`TapGestureRecognizer`, `MediaCarousel.xaml:34-36`) → `ItemTapped` → `MediaItemSelectedMessage` (`HomePage.xaml.cs:38-43`) → `NavigateToMediaDetailsEffect` mit Medientyp, Id und Titel (`AppUpdate.cs:517-529`; Einträge ohne identifizierbares Medium navigieren nicht) → `Shell.GoToAsync("mediadetails?mediaType=…&id=…&title=…")` (`ShellAppEffectHandler.cs:209-227`) → `MediaDetailsPage` als Platzhalter mit `QueryProperty`-Parametern (`MediaDetailsPage.xaml.cs:10-12`, Route registriert in `AppShell.xaml.cs:25`). Platzhalter-Seite ist laut Anforderung ausreichend — Befüllung folgt in Schritt 8.

### Vorschaubilder in nicht backup-pflichtiger Ablage

- `PosterImageService` (`PosterImageService.cs:43-68`) lädt Bilder über `api/pictures/{id}` mit dem angemeldeten Client und legt sie als `{id}.poster` in `AppStorageLayout.PreviewsDirectory` = `NoBackupDirectory/previews` (`AppStorageLayout.cs:52`) — nicht backup-pflichtig (iOS-Ausschluss via `SetSkipBackupAttribute` aus Schritt 3); zusätzlich In-Memory-Cache und Titel-Fallback bei fehlendem Bild. `CachedPosterImage` löst `PictureId`-Bindings über den Dienst auf (`CachedPosterImage.cs`).

### Testnachweis

- `dotnet test VideoWebPlayer.Maui.Tests` (net10.0): **350 Tests bestanden, 0 Fehler, 0 übersprungen** (Dauer ~3 s, Exit-Code 0). Neue Suites: `AppUpdateTests_HomeLists` (Cache-First-Effektfolge, Event→Teil-Sync→Reload-Sequenz, Auswahl-Navigation), `HomeListServiceTests` (reales SQLite-Caching/Mapping), `HomePageContractTests` (drei Listen, horizontales Layout, Poster-Cache im NoBackup-Verzeichnis, Detail-Route, DI-Verdrahtung); Bestandstests auf die neue Effektfolge angepasst.

### Einschränkungen

- MAUI-UI ist im Testprojekt nicht instanziierbar — Oberflächen-Verdrahtung über Contract-Tests auf Quelltextebene nachgewiesen (bewährtes Muster der Vor-Schritte); MVU-Kern und Cache-Schicht vollständig testabgedeckt.
- `MediaDetailsPage` zeigt als Platzhalter nur Titel und Medientyp; die Id steht bereits als `QueryProperty` für Schritt 8 bereit.
- `ServerEventReceivedMessage.SourceId` wird transportiert, aber nicht zur selektiven Aktualisierung genutzt — `SyncMetadataEffect` erneuert Quellen und „Neu im Programm" vollständig (fachlich korrekt, nur weniger granular).
