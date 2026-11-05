# Bestandsaufnahme: tvOS-Startseite mit fokussierbaren Karussells (Schritt 4)

Inventar des Ist-Stands zur Anforderung `requirement.md` (Schritt 4 des tvOS-Projektplans). Keine Planung, keine Implementierungsvorschläge. Ergänzend zu den Projektdetaildokumenten: [models.md](../../../projects/task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl/inventory/models.md), [logic.md](../../../projects/task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl/inventory/logic.md).

## Zusammenfassung

- **Datenpfad vollständig vorhanden:** `LoadHomeListsEffect` ist im `TvosAppEffectHandler` bereits verdrahtet (`LoadHomeListsAsync`, Zeile 150ff/654ff) — offline via `OfflineContinueWatchingService.LoadHomeListsAsync`, online via `HomeListService.LoadHomeListsAsync` (Cache-First aus `cached_lists`/`cached_favorites`). Ergebnis: `HomeListsLoadedMessage` → `AppUpdate.UpdateHomeListsLoaded` befüllt `AppModel.ContinueWatching`/`Favorites`/`RecentEntries`/`SeasonalGenres`.
- **Auslöser der (Neu-)Ladung vorhanden:** `HomeListsReloadRequestedMessage` → `LoadHomeListsEffect` (AppUpdate.cs:213; dispatcht in `AppDelegate.OnActivated`, Zeile 54); `CachedListsChangedMessage` → `LoadHomeListsEffect` + Quellen-Reload (AppUpdate.cs:1159); `ServerEventReceivedMessage` → `SyncContinueWatchingEffect`/`SyncFavoritesEffect`/`SyncMetadataEffect` (AppUpdate.cs:1260) — alle drei Teil-Syncs im tvOS-Handler verdrahtet (Zeilen 141–149) und melden `CachedListsChangedMessage` zurück; `SynchronizationCompletedMessage` → `LoadHomeListsEffect`.
- **Startseiten-UI fehlt:** `AppPage.Home` wird im `MenuTabBarController` (Zeile 97ff) über `PagePlaceholderViewController` dargestellt; `Render(AppModel)` verteilt das Modell an `IModelRenderable`-Seiten — die Startseite implementiert das Interface noch nicht.
- **Navigations-Effekte der Auswahl unverdrahtet:** `MediaItemSelectedMessage` → `NavigateToMediaDetailsEffect`; `ContinueWatchingItemSelectedMessage` → `NavigateToMediaDetailsEffect` + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect` (+ `LoadUpcomingEpisodesEffect` bei Serienkontext) bzw. `StartPlaylistPlaybackEffect` bei Playlist-Bezug (AppUpdate.cs:1360–1448). Im `TvosAppEffectHandler` laufen alle drei in den protokollierten `default:`-No-Op-Zweig (Zeile 156ff) — erfüllt formal „keinen Fehler", bietet aber kein sichtbares Ziel.
- **Bildpfad fehlt:** `PosterImageService` (MAUI) liefert `ImageSource` — nicht verwendbar; URL-Konvention `api/pictures/{id}` via `VideoPlayerApiClient.GetPictureAsync` (VideoPlayerApiClient.cs:470), Datei-Cache `{id}.poster` im `AppStorageLayout.PreviewsDirectory` (angelegt via `EnsureDirectoriesExist`), Fehlschlag → `null` = Titel-Fallback.
- **Referenz-UI (MAUI):** `HomePage.xaml` nutzt `MediaCarousel`-Steuerelement („Weiterschauen" querformatig 16:9, Rest Poster 2:3), Fortschrittsbalken via `ShowProgress`, Offline-Banner (`IsOffline`), `StorageNotice`-Banner, saisonale Ablauf-Timer (`ScheduleExpiry` → `HomeListsReloadRequestedMessage`), `HomeListsReloadRequestedMessage` in `OnAppearing`.
- **Test-Ausgangszustand:** Quell-Contract-Tests für tvOS vorhanden (`TvOsAppContractTests`, `TvOsConnectionContractTests`, `TvOsCompileContractTests`, `TvOsProjectContractTests`) — diese prüfen Textbestandteile der tvOS-Quelldateien, da `net10.0-tvos` unter Windows nicht kompiliert; `TvOsCompileContractTests` scannt automatisch ALLE tvOS-Dateien (UIView→StackView-Member, `MenuPages.IndexOf`).

## Details

- [MVU-Datenfluss der Startseite](inventory/mvu-datenfluss.md) — Messages/Effekte/Model-Felder und deren Update-Semantik
- [tvOS-App-Gerüst](inventory/tvos-geruest.md) — Navigator, Tab-Rahmen, Effekt-Handler, Komposition, Threading
- [Bild- und Cache-Pfad](inventory/bild-cache.md) — Poster-Konventionen, Ablageorte, Referenz-Implementierung
- [Tests](inventory/tests.md) — Contract-Test-Muster und lokale Validierung

## Feststellungen für die Umsetzung

1. `HomeMediaItem` ist ein `record` — `SequenceEqual`-Vergleich der Listeneinträge reicht für Änderungserkennung.
2. `MenuTabBarController.CreatePageController` erhält heute nur `dispatch`; der Poster-Dienst muss von `AppComposition` über `TvosNavigator` → `MenuTabBarController` → `HomePageViewController` gereicht werden (manuelle Verdrahtung, kein DI-Container).
3. `TvosNavigator.Render` ruft bei jedem Modellwechsel `NavigateTo(model.CurrentPage)` → `main.Show(...)`; ein Prepared Target darf davon nicht sofort wieder verdrängt werden → modale Präsentation (`PresentViewController`) statt `main.Show`.
4. Abgeleitete Sichtbarkeits-Properties existieren bereits: `HomeMediaItem.IsSelectable`, `CardOpacity`, `HasPoster`, `ShowProgress`; `HomeSeasonalSection.AutomationId`.
5. Abnahmerunde-3-Lektion: tvOS-Bindings-Stolpersteine sind Basistyp-vs.-abgeleiteter-Typ (z. B. `DequeueReusableCell` liefert `UICollectionViewCell` → Cast nötig) und nicht-existierende Member — neue tvOS-Quellen müssen sich streng an die .NET-tvOS-Bindings halten (`UICollectionViewFlowLayout`, `UICollectionViewSource`, `RegisterClassForCells`, `DidUpdateFocus`, `CanBecomeFocused`, `UIProgressView`, `UIImage.LoadFromData`, `PressesBegan`/`UIPressType.Menu`).
