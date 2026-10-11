# Detailbefund — Anforderung 6: Kontextmenü / Long-Press in der Weiterschauen-Liste

## Neue App (VideoPlayer.Maui + VideoPlayer.Mvu)

- **Kein** Long-Press und **kein** Kontextmenü implementiert. `MediaCarousel`
  (`VideoPlayer.Maui/Controls/MediaCarousel.xaml` Z. 39–41) registriert ausschließlich
  `TapGestureRecognizer` — kein `TouchBehavior`, kein `ContextFlyout`, kein
  `MenuFlyout`, kein `SwipeView`, kein `DisplayActionSheet` im Projekt
  (Volltextsuche über `VideoPlayer.Maui`/`VideoPlayer.Mvu`).
- `CommunityToolkit.Maui` (enthält `TouchBehavior` mit `LongPressDuration`) ist in
  `VideoPlayer.Maui` **nicht** referenziert — nur `CommunityToolkit.Maui.MediaElement`
  (`VideoPlayer.Maui.csproj` Z. 68; `MauiProgram.cs` Z. 33 `UseMauiCommunityToolkitMediaElement`).
- Für Hide/Skip existieren **keine** MVU-Nachrichten/-Effekte — `AppMessage.cs` und
  `AppEffect.cs` enthalten nichts dazu; `HideContinueWatchingEntryAsync`/
  `SkipContinueWatchingEntryAsync` des Clients werden in der App nicht aufgerufen.

## Altbestand (VideoWebPlayer.Maui) — vorhandenes Long-Press-Muster

- `VideoWebPlayer.Maui/Components/MediaCarousel.xaml` Z. 27–29:
  `mct:TouchBehavior LongPressDuration="2000" LongPressCompleted="TouchBehavior_LongPressCompleted"`
  — also bereits ein **2-Sekunden-Long-Press** (CommunityToolkit.Maui, Paket 15.0.0 in
  `VideoWebPlayer.Maui.csproj` Z. 76).
- `MediaCarousel.xaml.cs` Z. 83–98: `TouchBehavior_LongPressCompleted` delegiert an
  `MediaCarouselViewModel.ExecuteLongPress` bzw. `item.DeleteDownloadCommand`.
- `ViewModels/MediaCarouselViewModel.cs` Z. 127–139: `ExecuteLongPress` erlaubt das Ereignis
  nur für `CarouselKind.Favorites`, `ContinueWatching`, `Downloads` → `ItemPressed`.
- `ViewModels/HomePageViewModel.cs` Z. 786–827 `OnCarousel_ItemPressed`:
  - `Downloads` → `DownloadManager.DeleteDownloadAsync` + aus Liste entfernen
  - `Favorites` → `_client.ToggleFavorite` → ggf. aus Liste entfernen
  - `ContinueWatching` → **nur lokales Entfernen aus der Carousel-Liste** (Z. 817–821) —
    **kein** Server-Aufruf, kein „Ausblenden"/„Überspringen", kein Menü.
- Es existiert also im Altbestand ein Long-Press-Gerüst (2 s), aber **keine** Auswahl
  („Ausblenden"/„Überspringen") und **kein** API-Aufruf für Weiterschauen-Einträge.

## API-Seite (für die Aktionen „Ausblenden"/„Überspringen")

Bereits vorhanden (siehe `api-client-playlists.md`):

- `VideoPlayerApiClient.HideContinueWatchingEntryAsync` → `POST api/continue-watching/hide`
  (`VideoPlayerApiClient.cs` Z. 296–308)
- `VideoPlayerApiClient.SkipContinueWatchingEntryAsync` → `POST api/continue-watching/skip`
  (Z. 315–327), Ergebnis `ContinueWatchingMutationResult`.
- Nach einem erfolgreichen Hide/Skip müsste die Liste aktualisiert werden — vorhandener
  Mechanismus: `SyncContinueWatchingEffect` → `ServerSyncService.SyncContinueWatchingAsync`
  (schreibt `cached_lists[continue_watching]` neu) → `CachedListsChangedMessage` →
  `LoadHomeListsEffect` (Verdrahtung `AppUpdate.cs` Z. 1101, `ShellAppEffectHandler.cs`
  Z. 148–150, 370–384). Offline wäre ein lokales Entfernen aus dem Cache-Eintrag nötig
  (Altbestand-Muster) — zu entscheiden in der Umsetzung.

## Verortung der Umsetzung (Konvention)

Ein Kontextmenü in der neuen App müsste neu gebaut werden — naheliegende Bausteine:
Long-Press über `CommunityToolkit.Maui` `TouchBehavior` (Paket hinzuzufügen) oder eigener
Gesture; Menü z. B. `DisplayActionSheet`/`DisplayAlert` (im Altbestand genutzt) oder
Flyout; MVU-seitig neue Messages (z. B. `ContinueWatchingHideRequestedMessage` /
`...Skip...`) + Effekte auf die vorhandenen Client-Methoden, Ergebnis-Messages und
Cache-/Listen-Aktualisierung über die bestehende Sync-Strecke.
