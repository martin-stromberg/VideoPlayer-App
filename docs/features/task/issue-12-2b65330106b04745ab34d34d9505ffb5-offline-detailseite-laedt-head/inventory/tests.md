# Tests

Testprojekt: `VideoWebPlayer.Maui.Tests` (xUnit v3, `net10.0`, FluentAssertions-frei — `Assert.*` aus xUnit). UI-E2E-Projekt `VideoPlayer.E2E.Tests` (FlaUI + Dev-API + `MockVideoServer`) existiert, kann aber den OS-seitigen Offline↔Online-Wechsel (`Connectivity`) nicht auslösen — etablierter Funktionsnachweis sind MVU-Übergangstests plus Quell-Vertragstests.

## Testklassen

### `AppUpdateTests_DetailsHeader` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_DetailsHeader.cs`)

- `MediaDetailsLoaded_Show_EmitsHeaderPictureEffect` — `LoadMediaDetailsHeaderEffect` mit `HeaderPictureId` nach der Detail-Ladung
- `MediaDetailsLoaded_Episode_EmitsEpisodeStillEffect` — Effekt mit `HeaderEpisodeId`
- `MediaDetailsLoaded_Season_UsesSeasonBanner` — Staffel-Banner als Bildquelle
- `MediaDetailsLoaded_NoHeaderSource_NoEffect` — ohne Bildquelle kein Effekt
- `MediaDetailsLoaded_Offline_NoHeaderEffect` — **Offline-Gate: kein Header-Effekt** (definiert das Fehlverhalten des Issues)
- `MediaDetailsLoaded_Error_NoHeaderEffect` — Fehler → kein Effekt
- `HeaderImageLoaded_MatchingIdentity_AppliesBytes` — Bild-Übernahme bei passender Identität
- `HeaderImageLoaded_StaleMedia_IsDiscarded` / `HeaderImageLoaded_StaleHeaderSource_IsDiscarded` — verspätete Antworten verworfen
- `HeaderImageLoaded_Failure_StaysStable` — Fehlschlag löscht kein Bild
- `SeasonSelected_SelectsFirstEpisode_ReloadsHeader` / `SeasonSelected_EmptySeason_ShowsSeasonHeader` / `SeasonSelected_SameHeaderSource_KeepsImage` — Staffelwahl und Header-Nachladung
- `HeaderText_*` — Kopfzeilen-Ableitungen des `MediaDetailsView`

### `AppUpdateTests_Offline` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_Offline.cs`)

- `Reconnection_FromOffline_RunsFullSyncAndResumesDownloads` — **Reconnect-Referenzmuster**: `NetworkAvailabilityChangedMessage(true)` → `CheckServerHealthEffect`; `ServerHealthCheckCompletedMessage(true, false)` → `AttemptAutoLoginEffect`; `AutoLoginCompletedMessage(Succeeded)` → `Connected` + `ConnectServerEventsEffect`/`RunFullSyncEffect`/`ResumeDownloadsEffect`
- weitere Offline-Gates (`SourcesPageOpened_Offline_NoLoadEffect`, `ProfilePageOpened_Offline_NoLoadEffect`, `DownloadRequested_Offline_*` u. a.)

### `AppUpdateTests_Hero`/`HomePageContractTests` (Hero-Referenz)

- Hero-Ladepfad über `WithHeroLoad` in `EnterConnected` — Vorbild für den Reconnect-Trigger

## Hilfsmethoden

### `AppUpdateTests_DetailsHeader`

- `Connected()` — `AppModel` mit `ConnectionState.Connected`
- `Loading(mediaType, mediaId)` — Modell mit ladendem `MediaDetailsState`
- `WithDetails(details, headerImageBytes, isHeaderImageLoading, connection)` — Modell mit geladenen Details, parametrierbarem Bild-/Lade-/Verbindungszustand
- `ShowDetails(bannerPictureId)` — `MediaDetailsView`-Fixture (Serie mit zwei Staffeln, `BannerPictureId`, `SelectedSeasonId`)

### `AppUpdateTests_Offline`

- `OfflineModel()` — `AppModel` mit `ConnectionState.Offline` und aktivem Server (Grundlage der Reconnect-Kette)
