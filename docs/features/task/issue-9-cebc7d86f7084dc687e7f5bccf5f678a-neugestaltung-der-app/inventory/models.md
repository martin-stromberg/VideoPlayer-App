# Datenmodell

## `AppModel`

Datei: `VideoPlayer.Mvu/AppModel.cs`

Unveränderliches Gesamtmodell der App (MVU-Record). Für die Anforderung relevante Teilmenge:

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `CurrentPage` | `AppPage` | Aktuell angezeigte Seite; Startwert `AppPage.Loading`. Wird über `NavigatedMessage`/`NavigateToMessage` gepflegt; `sourceoverview` erzeugt keinen `CurrentPage`-Wechsel. |
| `IsStartupRunning` | `bool` | Start-Workflow läuft (Startwert `true`). |
| `ServerHealth` | `ServerHealthStatus` | Ergebnis der Server-Erreichbarkeitsprüfung. |
| `ConnectionState` | `ConnectionState` | Verbindungszustand (`Connecting`, `Connected`, `NeedsLogin`, `NeedsServerSetup`, `Offline`); Startwert `Connecting`. Steuert die Navigation. |
| `IsOffline` | `bool` (abgeleitet) | `true`, wenn `ConnectionState == ConnectionState.Offline`. Steuert u. a. das Offline-Verhalten der Quellenliste. |
| `IsOnline` | `bool` | Letzter bekannter Netzwerkstatus (Startwert `true`). |
| `IsSyncRunning` | `bool` | Vollständige Synchronisation läuft (gesetzt in `EnterConnected`, zurückgesetzt in `UpdateSynchronizationCompleted`). |
| `ActiveServerAddress` | `string?` | Kanonische Adresse des aktiven Servers; Basis für `ConnectServerEventsEffect`. |
| `ContinueWatching` | `IReadOnlyList<HomeMediaItem>` | Startseiten-Liste aus der Cache-Ablage (Cache-First-Muster). |
| `Favorites` | `IReadOnlyList<HomeMediaItem>` | Startseiten-Liste aus der Cache-Ablage. |
| `RecentEntries` | `IReadOnlyList<HomeMediaItem>` | Startseiten-Liste aus der Cache-Ablage. |
| `SourcesOverview` | `SourcesOverviewState` | Zustand der Quellen-Übersicht — trägt die Quellenliste (`Sources`). Wird aktuell nur über `SourcesPageOpenedMessage`/`SourcesLoadedMessage` befüllt. |
| `SourceBrowser` | `SourceBrowserState` | Zustand der Quellen-Browsing-Seite (geöffnete Quelle, Filter, Paging) — wird bei `SourceSelectedMessage` belegt. |
| `MediaDetails` / `FavoritesPage` / `Downloads` / `Profile` / `Player` | je eigener State-Record | Zustände der übrigen Seiten (für die Anforderung nur insoweit relevant, als `UpdateLogoutRequested` sie gemeinsam mit den Quellenzuständen zurücksetzt). |
| `StorageNotice` | `string?` | Hinweis der Ablage-Wiederherstellung. |

Es existiert **kein eigener Menü-Quellenbestand** — die Quellen liegen ausschließlich in `SourcesOverview.Sources`.

## `SourcesOverviewState`

Datei: `VideoPlayer.Mvu/SourcesOverviewState.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Initial` | `static SourcesOverviewState` | Leerer Ausgangszustand (nichts geladen, kein Fehler). |
| `IsLoading` | `bool` | Quellenliste wird vom Server geladen. |
| `Attempted` | `bool` | Mindestens ein Ladeversuch abgeschlossen. |
| `Error` | `string?` | Fehlertext des letzten Ladeversuchs; `null` = kein Fehler. |
| `Sources` | `IReadOnlyList<SourceItem>` | Freigegebene Quellen (Stand der Server-API). |
| `HasSources` | `bool` (abgeleitet) | Liste enthält Einträge. |
| `HasError` | `bool` (abgeleitet) | Letzter Ladeversuch fehlgeschlagen. |
| `ShowEmpty` | `bool` (abgeleitet) | Erfolgreich geladen, aber keine Quelle freigegeben. |

## `SourceItem`

Datei: `VideoPlayer.Mvu/SourceItem.cs`

Anzeigefertiger Eintrag der Quellen-Übersicht; trägt bereits alles, was ein Menüeintrag braucht.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Id der Quelle (Navigationsziel der Browsing-Seite, Query-Parameter `id`). |
| `Name` | `string` (required) | Anzeigename der Quelle (Query-Parameter `name`, Seitentitel). |
| `IconPictureId` | `long?` | Bild-Id für `api/sourceicons/{id}`; `null` = kein Icon. |
| `HasIcon` | `bool` (abgeleitet) | `IconPictureId is > 0`. |

## `SourceBrowserState`

Datei: `VideoPlayer.Mvu/SourceBrowserState.cs`

Zustand der Zielseite der Menüeinträge (`SourceOverviewPage`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `PageSize` | `const int = 20` | Seitengröße des Nachladens. |
| `Empty` | `static SourceBrowserState` | Ausgangszustand (keine Quelle geöffnet). |
| `SourceId` | `long` | Id der geöffneten Quelle; 0 = kein Browser aktiv. |
| `SourceName` | `string` | Anzeigename der geöffneten Quelle. |
| `Genres` | `IReadOnlyList<SourceGenreItem>` | Genre-Leiste inkl. „Alle"-Eintrag. |
| `SelectedGenreId` | `long` | Gewählter Genre-Filter; 0 = „Alle". |
| `SearchText` | `string` | Aktiver Suchtext. |
| `Items` | `IReadOnlyList<SourceMediaItem>` | Geladene Einträge (kumuliert). |
| `NextPage` / `HasMore` | `int` / `bool` | Paging-Zustand. |
| `IsLoading` / `IsLoadingMore` / `Attempted` / `Error` | `bool` / `bool` / `bool` / `string?` | Lade-/Fehlerzustände. |
| `IsActive` / `HasItems` / `HasError` / `ShowEmpty` | `bool` (abgeleitet) | Komfort-Abfragen für die Anzeige. |

## `SourceGenreItem`

Datei: `VideoPlayer.Mvu/SourceGenreItem.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Genre-Id; 0 = „Alle" (kein Filter). |
| `Name` | `string` (required) | Anzeigename. |
| `IsSelected` | `bool` | Aktuell gewählter Filter. |

## `SourceMediaItem`

Datei: `VideoPlayer.Mvu/SourceMediaItem.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Id des Mediums (Navigationsziel der Detailseite). |
| `Title` | `string` (required) | Anzeigetitel. |
| `MediaType` | `string` (required) | Normalisierter Medientyp (`moviecollection`, `tvshow`, …). |
| `PosterPictureId` | `long?` | Bild-Id für `api/pictures/{id}`. |
| `HasPoster` | `bool` (abgeleitet) | `PosterPictureId is > 0`. |

## `MediaSource` (API-Client-Modell)

Datei: `VideoPlayer.ApiClient/Models/MediaSource.cs`

Server-Modell der Quelle (`GET api/Sources`); erbt `Id`, `Name`, `PosterPictureId` u. a. von `MediaEntry` (`VideoPlayer.ApiClient/Models/MediaEntry.cs`, Zeilen 16–43).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` (geerbt) | `long` | Id der Quelle. |
| `Name` (geerbt) | `string` | Name der Quelle. |
| `IconPictureId` | `long?` | Id des Quellen-Icons. |
| `CreatedAt` / `ClassifiedAt` / `Changed` / `LastScannedAt` | `DateTime`/`DateTime?`/`bool` | Scan-/Klassifizierungs-Metadaten (für das Menü irrelevant). |

Wird von `SourceBrowserService.LoadSourcesAsync` auf `SourceItem` gemappt und von `ServerSyncService.SyncMetadataAsync` als JSON in `cached_lists["sources"]` geschrieben.

## `CachedListEntry`

Datei: `VideoPlayer.Storage/Entities/CachedListEntry.cs`

Tabelle `cached_lists` der `CacheDatabase` (`VideoPlayer.Storage/CacheDatabase.cs`) — generischer Schlüssel-Wert-Cache; der Schlüssel `"sources"` wird bereits von `ServerSyncService.SyncMetadataAsync` geschrieben. **Keine Schemaänderung erforderlich.**

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Key` | `string` (PK) | Schlüssel des Cache-Eintrags (z. B. `"sources"`, `"recent"`, `"continue_watching"`). |
| `Json` | `string` | Serialisierte Listendaten (JSON). |
| `UpdatedAtUtc` | `DateTime` | Zeitpunkt der letzten Aktualisierung. |

## `HomeLists`

Datei: `VideoPlayer.Mvu/HomeLists.cs`

Ergebnisbündel des `HomeListService` — Referenzmuster für Cache-First-Listen inkl. `DiscardedCaches` (beschädigte Cache-Bereiche, die eine Neu-Synchronisation auslösen).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Empty` | `static HomeLists` | Leeres Ergebnis. |
| `ContinueWatching` | `IReadOnlyList<HomeMediaItem>` | Cache-Schlüssel `continue_watching`. |
| `Favorites` | `IReadOnlyList<HomeMediaItem>` | Tabelle `cached_favorites`. |
| `Recent` | `IReadOnlyList<HomeMediaItem>` | Cache-Schlüssel `recent`. |
| `DiscardedCaches` | `IReadOnlyList<string>` | Verworfene (beschädigte) Cache-Bereiche — löst bei Verbindung `RunFullSyncEffect` aus. |

## `AppTransition`

Datei: `VideoPlayer.Mvu/AppTransition.cs`

| Element | Typ | Beschreibung / Zweck |
|---------|-----|----------------------|
| `Model` | `AppModel` | Neues, unveränderliches Model des Update-Schritts. |
| `Effects` | `IReadOnlyList<AppEffect>` | Auszuführende Effekte (in Reihenfolge). |
| `Unchanged(model)` | `static` | Übergang ohne Zustandsänderung und Effekte. |
