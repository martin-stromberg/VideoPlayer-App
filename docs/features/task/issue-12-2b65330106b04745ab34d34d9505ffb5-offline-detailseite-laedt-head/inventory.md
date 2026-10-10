# Bestandsaufnahme: Offline-Detailseite lädt Header-Bild nach Reconnect nicht nach

Analyse des MVU-Ladepfads für das Kopfbereichs-Bild der Medien-Detailseite (`VideoPlayer.Mvu`, App-Rand `VideoPlayer.Maui`) bezogen auf das Nachladen nach Wiederherstellung der Verbindung.

## Zusammenfassung

- `WithDetailsHeaderLoad` in `AppUpdate` ist der einzige Auslöser des `LoadMediaDetailsHeaderEffect`. Es wird nur aus `UpdateMediaDetailsLoaded` (nach der Detail-Ladung) und `UpdateSeasonSelected` (bei geänderter Bildquelle) aufgerufen — niemals beim Übergang in `ConnectionState.Connected`.
- `EnterConnected` ist der **einzige** Übergang in `Connected` (aufgerufen aus `UpdateAutoLoginCompleted` und `UpdateLoginCompleted`; die Offline-Rückkehr läuft über `NetworkAvailabilityChangedMessage` → `CheckServerHealthEffect` → `AutoLoginCompletedMessage`). Dort wird der Startseiten-Hero über `WithHeroLoad` nachgeladen — für den Detail-Kopfbereich fehlt das Pendant.
- `WithDetailsHeaderLoad` gated auf `ConnectionState.Connected`, `Details != null`, vorhandener Bildquelle (`HeaderEpisodeId`/`HeaderPictureId`) und `!IsHeaderImageLoading` — es prüft **nicht** `HasHeaderImage` (anders als `WithHeroLoad` mit `HomeHero.HasImage`).
- Der Offline-Snapshot (`MediaDetailsService.LoadOfflineAsync`/`MapOfflineShow`) setzt für `tvshowepisode` die `SelectedEpisodeId` — damit ist `HeaderEpisodeId` offline ableitbar und der Episoden-Still nach Reconnect ladbar. Für Film/Serie/Staffel fehlt im Offline-Snapshot `BannerPictureId` — dort gibt es ohnehin keine Bildquelle.
- `ShellAppEffectHandler.LoadMediaDetailsHeaderAsync` lädt das Bild über den angemeldeten API-Client (`GetEpisodeBackgroundImageAsync`/`GetPictureAsync`) und meldet `MediaDetailsHeaderImageLoadedMessage`; `UpdateMediaDetailsHeaderImageLoaded` prüft die Identität gegen den aktuellen Zustand und verwirft verspätete Antworten.
- `MediaDetails` wird beim Offline-Übergang **nicht** zurückgesetzt (die Seite bleibt geöffnet); bei Abmeldung/Sitzungsablauf/Gate-Ablehnung wird `MediaDetailsState.Empty` gesetzt — ein Reconnect nach Sitzungsende betrifft den Detail-Zustand damit nicht.
- Testmuster ist vorhanden: `AppUpdateTests_DetailsHeader` (Header-Ladepfad) und `AppUpdateTests_Offline.Reconnection_FromOffline_RunsFullSyncAndResumesDownloads` (Reconnect-Kette über drei Nachrichten).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
