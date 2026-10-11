# Bestehender Wiedergabepfad (MVU)

## Ablauf Wiedergabestart

1. Auslöser — zwei Eingänge:
   - `PlayRequestedMessage(MediaType, MediaId, Title)` → `AppUpdate.UpdatePlayRequested` (`AppUpdate.cs:2367`): nur `movie`/`tvshowepisode` → `StartPlayback`.
   - `ContinueWatchingItemSelectedMessage(HomeMediaItem)` → `AppUpdate.UpdateContinueWatchingItemSelected` (`AppUpdate.cs:1268`): abspielbare Typen → `StartPlayback`; bei `tvshowepisode` mit `ShowId` zusätzlich `LoadUpcomingEpisodesEffect` (Serienkontext ohne Detailseite, Schritt 1). Nicht abspielbare Typen → `NavigateToMediaDetailsEffect`.
2. `AppUpdate.StartPlayback` (`AppUpdate.cs:2390`): setzt `model.Player = new PlayerState { MediaType, MediaId, Title, IsResolving = true, Attempted = true, UpcomingEpisodes = NextEpisodeQueue(model, mediaId) }` und gibt `[NavigateToPlayerEffect, ResolvePlaybackSourceEffect(mediaType, mediaId)]` zurück. `NextEpisodeQueue` (`AppUpdate.cs:2417`) liest den Serienkontext aus `model.MediaDetails.Details` (nur befüllt, wenn die Detailseite geladen war).
3. `ShellAppEffectHandler.ResolvePlaybackSourceAsync` (`ShellAppEffectHandler.cs:355`): `PlaybackSourceService.ResolveAsync(mediaType, mediaId, ct, allowServerStream: !IsOffline)` → `PlaybackSourceResolvedMessage` (Quelle oder Fehler).
4. `PlaybackSourceService.ResolveAsync` (`PlaybackSourceService.cs:78`): lokale Datei aus `download_manifest` bevorzugt, sonst `api/items/{typ}/{id}/stream?access_token=…`; Fortsetzungsposition online aus `api/continue-watching`, sonst Cache-Kopie `cached_lists[continue_watching]` (`FindResumePosition`, matcht auf `MediaType` + `Entry.Id`).
5. `AppUpdate.UpdatePlaybackSourceResolved` (`AppUpdate.cs:2449`): identitätsgebunden (MediaId/MediaType müssen zum Player passen); bei `Source == null` → `AdvanceToNextEpisode(model, player, error)` (Fehler-Rückfall).
6. `PlayerPage` (`Pages/PlayerPage.xaml(.cs)`): `MediaElement` mit `ShouldAutoPlay`, seekt auf `Source.ResumePosition` (verzögert bis Playing/Paused, `TryApplyPendingResumeSeek`), 5-s-Fortschrittstakt → `PlaybackProgressMessage`, `MediaEnded` → `PlaybackEndedMessage`, `MediaFailed` → `PlaybackFailedMessage`.

## Folgevideo / Auto-Fortsetzung / Fehler-Rückfall (Serienkontext)

- `PlayerState.UpcomingEpisodes` (`PlayerState.cs:59`): `IReadOnlyList<PlayerEpisodeRef>`; `HasNextEpisode` (`:62`) steuert alles Folgende.
- `AdvanceToNextEpisode` (`AppUpdate.cs:2491`): ohne Folge-Episode → Quelle leeren + Fehler/Endzustand; sonst Medium/Titel der Warteschlangenspitze übernehmen, Wiedergabewerte zurücksetzen, `ResolvePlaybackSourceEffect("tvshowepisode", next.MediaId)`.
- Auto-Fortsetzung:
  - im Takt `UpdatePlaybackProgress` (`AppUpdate.cs:2597`): `AutoResumeEnabled && IsPlaying && HasNextEpisode && Restlaufzeit <= AutoResumeRemainingSeconds` → `AdvanceToNextEpisode`.
  - am Ende `UpdatePlaybackEnded` (`AppUpdate.cs:2651`): `AutoResumeEnabled && HasNextEpisode` → `AdvanceToNextEpisode`.
- Fehler-Rückfall: `UpdatePlaybackSourceResolved` (Quelle null), `UpdatePlaybackFailed` (`AppUpdate.cs:2717`, `MediaFailed`/`Seek-Fehler` → `PlaybackFailedMessage`) → `AdvanceToNextEpisode`.
- `UpcomingEpisodesLoadedMessage` → `UpdateUpcomingEpisodesLoaded` (`AppUpdate.cs:1369`): befüllt `UpcomingEpisodes` nur, wenn der Player noch dieselbe Episode spielt (Identitätswache).
- Vor-/Zurück-Navigation im Player: **existiert nicht** — `PlayerPage.xaml` kennt nur MediaElement-Transportsteuerung, Mini-Player-Schalter (`PipButton`) und Fehlerbanner.

## MVU-Konventionen

- `AppMessage`/`AppEffect`: `public sealed record ... : AppMessage`/`AppEffect`, deutsche XML-Doku (CS1591 als Fehler). Effekte sind reine Datensätze; Ausführung ausschließlich im `ShellAppEffectHandler` (`IAppEffectHandler.HandleAsync`, `AppEffect`-Switch, `case`-Blöcke + private Methoden, Ergebnis via `DispatchMessageAsync` → `MvuHost.Dispatch`).
- `AppUpdate.Update` (`AppUpdate.cs:142 ff.`): statischer `switch` über alle `AppMessage`-Subtypen → private `Update*`-Methoden liefern `AppTransition(model, effects)` / `AppTransition.Unchanged(model)`.
- `PlayerState` (`PlayerState.cs`): unveränderlicher Record; `IsActive => MediaId > 0 && MediaType is not null`; `PlayerState.Empty` als Ausgang.
- DI: Dienste als Singleton in `MauiProgram.cs` (u. a. `PlaylistService`, `PlaybackSourceService`, `UpcomingEpisodesService` registriert); der Effekt-Handler löst weitere Dienste über `services.GetRequiredService<T>()` auf.
- Fehlertext-Konstanten in `AppUpdate` (z. B. `PlaybackUnavailableError`, `AppUpdate.cs` → öffentliche consts, vom Handler referenziert).

## Beteiligte Dateien

| Datei | Rolle |
|-------|-------|
| `VideoPlayer.Mvu/AppMessage.cs` | Nachrichten (`PlayRequestedMessage` :581, `ContinueWatchingItemSelectedMessage` :258, `UpcomingEpisodesLoadedMessage` :289, `PlaybackSourceResolvedMessage` :599, `PlaybackProgressMessage` :616, `PlaybackEndedMessage` :623, `PlaybackFailedMessage` :626, `PlayerClosedMessage` :643) |
| `VideoPlayer.Mvu/AppEffect.cs` | Effekte (`NavigateToPlayerEffect` :377, `ResolvePlaybackSourceEffect` :388, `LoadUpcomingEpisodesEffect` :403, `ReportPlaybackProgressEffect` :418) |
| `VideoPlayer.Mvu/AppUpdate.cs` | Update-Logik (siehe Ablauf oben) |
| `VideoPlayer.Mvu/PlayerState.cs` | Player-Zustand inkl. `UpcomingEpisodes`/`HasNextEpisode` |
| `VideoPlayer.Mvu/PlaybackSource.cs` / `PlaybackSourceKind.cs` / `PlaybackSourceService.cs` | Quellauflösung (lokale Datei → Server-Stream mit Token + Weiterschauen-Position) |
| `VideoPlayer.Mvu/UpcomingEpisodesService.cs` | Detailseiten-unabhängige Serienkontext-Auflösung (Schritt 1) |
| `VideoPlayer.Maui/Pages/PlayerPage.xaml(.cs)` | MediaElement-Wiedergabe, Resume-Seek, Takt, Fehleranzeige |
| `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` | Effekt-Ausführung (`ResolvePlaybackSourceAsync` :355, `LoadUpcomingEpisodesAsync` :392) |
