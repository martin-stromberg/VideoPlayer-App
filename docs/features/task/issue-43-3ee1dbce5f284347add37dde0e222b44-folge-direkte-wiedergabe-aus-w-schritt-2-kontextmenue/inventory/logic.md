# Bestandsaufnahme: Logik

## `MediaCarousel` (VideoPlayer.Maui)
Datei: `VideoPlayer.Maui/Controls/MediaCarousel.xaml(.cs)`

Horizontale Medienliste; `BindableLayout.ItemsSource` im `HorizontalStackLayout` innerhalb eines horizontalen `ScrollView`; Eintragskarte = `Border` mit `TapGestureRecognizer Tapped="OnItemTapped"`.

| Element | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Title`/`Items`/`ItemWidth`/`ItemHeight` | public (BindableProperty) | Anzeige-/Layout-Parameter der Liste. |
| `ItemTapped` | public event | Tap-Auswahl eines Eintrags (`EventHandler<HomeMediaItem>`). |
| `OnItemsChanged` | private static | Blendet die Liste bei leerem Bestand aus. |
| `OnItemTapped` | private | Liest `sender.BindingContext` (`HomeMediaItem`) und feuert `ItemTapped`. |

Publizierte Events: `ItemTapped`.

## `HomePage` (VideoPlayer.Maui)
Datei: `VideoPlayer.Maui/Pages/HomePage.xaml(.cs)`

| Element | Kurzbeschreibung |
|---------|------------------|
| `controls:MediaCarousel AutomationId="ContinueWatchingCarousel"` (XAML) | Weiterschauen-Liste, `ItemTapped="OnContinueWatchingItemTapped"`, Karten 256×144 (16:9). |
| `OnContinueWatchingItemTapped` | Dispatcht `ContinueWatchingItemSelectedMessage` über `mvuHost.DispatchAsync` — seit Schritt 1 Direktstart/Detail-Fallback. |
| `OnMediaItemTapped` | Dispatcht `MediaItemSelectedMessage` (Favoriten/Neu im Programm). |

## `AppUpdate` (VideoPlayer.Mvu)
Datei: `VideoPlayer.Mvu/AppUpdate.cs` (statische `Update`-Funktion, switch über `AppMessage`)

| Funktion | Kurzbeschreibung |
|----------|------------------|
| `Update` (Z. 85–173) | Statischer Switch Nachricht → Übergangsfunktion; unbekannte Nachrichten → `AppTransition.Unchanged`. |
| `UpdateContinueWatchingItemSelected` (Z. 1218) | Guards `Id > 0`/`MediaType`/`IsSelectable`; `movie`/`tvshowepisode` → `StartPlayback` + ggf. `LoadUpcomingEpisodesEffect`; sonst `NavigateToMediaDetailsEffect`. |
| `UpdateCachedListsChanged` | Löst `LoadHomeListsEffect` aus (Listen aus Cache neu laden). |
| `UpdateFavoriteRemoveRequested`/`UpdateFavoriteRemoveCompleted` (Z. 1818/1841) | Muster einer Listeneintrags-Mutation: Guard → `RemoveFavoriteEffect`; Completed → Liste anpassen/Fehler. |
| `UpdateServerEventReceived` (Z. 1091) | `ServerEventKind.ContinueWatchingUpdated` → `SyncContinueWatchingEffect`. |

## `ShellAppEffectHandler` (VideoPlayer.Maui)
Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` (1521 Z., `IAppEffectHandler`)

| Funktion | Kurzbeschreibung |
|----------|------------------|
| `HandleAsync` (Z. 87) | Switch über `AppEffect`-Typen; ruft private Handler. |
| `case SyncContinueWatchingEffect` (Z. 148) | `RunPartialSyncAsync(s => s.SyncContinueWatchingAsync(ct))` → danach `CachedListsChangedMessage` (Z. 398–412). |
| `RemoveFavoriteAsync` (Z. 733) | Muster: `FavoritesService.RemoveFavoriteAsync` → `FavoriteRemoveCompletedMessage`; bei Erfolg `CachedListsChangedMessage`. |
| `LoadHomeListsAsync` (Z. 414) | Offline → `OfflineContinueWatchingService`, sonst `HomeListService`; Ergebnis `HomeListsLoadedMessage`. |

## `VideoPlayerApiClient` (VideoPlayer.ApiClient)
Datei: `VideoPlayer.ApiClient/VideoPlayerApiClient.cs`

| Methode | Endpunkt | Bemerkung |
|---------|----------|-----------|
| `GetContinueWatchingAsync` | `GET api/continue-watching` | Liest die Liste. |
| `ReportPlaybackProgressAsync` | `POST api/continue-watching/progress` | Fortschritt (wird bereits genutzt). |
| `HideContinueWatchingEntryAsync` (Z. 296) | `POST api/continue-watching/hide` | **Ungenutzt.** Body `{ MediaType, MediaId }` → `ContinueWatchingMutationResult`. |
| `SkipContinueWatchingEntryAsync` (Z. 315) | `POST api/continue-watching/skip` | **Ungenutzt.** Gleiche Signatur. |

## `ServerSyncService` (VideoPlayer.Mvu)
Datei: `VideoPlayer.Mvu/ServerSyncService.cs`

| Methode | Kurzbeschreibung |
|---------|------------------|
| `SyncContinueWatchingAsync` (Z. 157) | `GetContinueWatchingAsync` → `WriteCachedListAsync(ContinueWatchingCacheKey, entries)`; Cache-Schlüssel `continue_watching` (`public const`). Ohne Client/bei Fehler `false`. |
| `SyncFavoritesAsync` | Ersetzt `cached_favorites` autoritativ. |
| `TrySyncAsync` | Gemeinsame Fehlerkapselung (Client null → false, Exception → Debug.Log + false). |

## `FavoritesService` (VideoPlayer.Mvu) — Muster der Server-Mutation
Datei: `VideoPlayer.Mvu/FavoritesService.cs`

`ToggleAsync`/`RemoveFavoriteAsync`: Serveraufruf über `authSession.Client`, danach `syncService.SyncFavoritesAsync` (führendes System Server). Ohne Client: `null`/`false` — kein Cache-Eingriff.

## `HomeListService` (VideoPlayer.Mvu)
Datei: `VideoPlayer.Mvu/HomeListService.cs`

`LoadHomeListsAsync` liest `cached_lists`-Schlüssel und mappt `ContinueWatchingEntry` → `HomeMediaItem` (`MapContinueWatching`, `MediaTypeOf` bevorzugt den Server-`MediaType` des Eintrags). Nur Lesepfad — kein Lösch-/Entfernpfad vorhanden.

## `OfflineContinueWatchingService` (VideoPlayer.Mvu)
Datei: `VideoPlayer.Mvu/OfflineContinueWatchingService.cs`

Bildet die Offline-Weiterschauen-Liste aus dem Download-Manifest + gecachten Serienstrukturen (Platzhalter). Nutzt `cached_lists`/`continue_watching` nur als Fortschritts-Fallback (`LoadCachedContinueWatchingAsync`).

## Developer-API (msTools.DeveloperApi.Maui)
Datei: `msTools.DeveloperApi.Maui/MauiDevApiService.cs`, `DevApiVisualTree.cs` (je `#if DEBUG || DEV_API`)

| Endpunkt | Funktion |
|----------|----------|
| `GET /model` | Kuratierter `DevApiModelSnapshot` (u. a. `ContinueWatchingCount`). |
| `GET /tree` | Logischer Elementbaum mit `Items` je BindableLayout-Container. |
| `POST /invoke` | `automationId` (+ optional `itemIndex`) → `TryInvoke`/`TrySelectItem`; BindableLayout-Eintrag löst `TapGestureRecognizer.SendTapped` per interner Reflection aus (UIA-Lücke). Long-Press-Auslösung existiert **nicht**. |
| `POST /set-text` | Texteingabe. |

## `MockVideoServer` (VideoPlayer.E2E.Tests)
Datei: `VideoPlayer.E2E.Tests/Support/MockVideoServer.cs` (1196 Z.)

TCP-Loopback-Mock; `POST /api/continue-watching/progress` existiert (204), **hide/skip fehlen** (würden 404 liefern). `GET /api/continue-watching` liefert Opt-in-Einträge aus `PlaybackMovieId`/`PlaybackEpisodeId`/`ContinueWatchingShowId` über `ListOverride`. Diagnose: `RequestedPaths`, `WasPathRequested(path)`.
