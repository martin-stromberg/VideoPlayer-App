# Code-Review

## Ergebnis

**Status:** Keine Befunde

## Befunde

Keine.

## Geprüfte Dateien

Geändert/neu gegenüber Basisbranch `task/issue-86-…-apple-tv-app-tvos-lean-back-pl`:

- `VideoPlayer.TvOS/Ui/PlaylistsPageViewController.cs` (neu)
- `VideoPlayer.TvOS/Ui/PlaylistDetailsViewController.cs` (neu)
- `VideoPlayer.TvOS/Ui/PlaylistTableCell.cs` (neu)
- `VideoPlayer.TvOS/Ui/PlaylistEntryCell.cs` (neu)
- `VideoPlayer.TvOS/Ui/TvosNavigator.cs` (erweitert)
- `VideoPlayer.TvOS/Ui/MenuTabBarController.cs` (erweitert)
- `VideoPlayer.TvOS/Services/TvosAppEffectHandler.cs` (erweitert)
- `VideoPlayer.TvOS/Services/TvosPosterImageService.cs` (erweitert)
- `VideoPlayer.Maui.Tests/TvOsPlaylistsContractTests.cs` (neu)

## Prüfnotizen

- Alle Seiten folgen den etablierten Mustern (`SourceBrowserViewController`,
  `MediaDetailsViewController`, `ServerListTableSource`,
  `HomeMediaItemCell`/`EpisodeItemCell`): `IModelRenderable`,
  `lastModel`-Vorgriff, Identitäts-Guards, `appliedPictureId`/
  `appliedPlaylistId`/`appliedCoverPlaylistId` gegen Zellen-Recycling,
  Titel-Fallback bei fehlendem Bild, `PressesBegan`-Menü-Taste →
  `DismissViewController`, `RemembersLastFocusedIndexPath`.
- Die Seiten sind reine Ereignis-Übersetzer: Auswahl-/Paging-/
  Retry-Entscheidungen (Wiedergabe ab Eintrag vs. Medien-Detailseite,
  Offline-Fehlerstand, Dedupplizierung) verbleiben im
  plattformneutralen `AppUpdate`.
- Verwaltungsausschluss eingehalten: keine Verwaltungs-Messages in den
  Seiten, keine Verwaltungs-Effekt-Zweige im Handler; die vom
  Contract-Test verbotenen Zeichenfolgen kommen in
  `TvosAppEffectHandler.cs` nicht vor.
- Singleton-Playlist-Detailseite mit Duplikat-Guard und
  Freigabe-Callback — Medien-Detailseiten und Player können darüber
  gestapelt werden (`TopmostPresented`).
- `GetPlaylistCoverAsync`: dritter, sauber getrennter
  Cache-Schlüsselraum (Playlist-Id statt Bild-Id), `byte[]?`-Rückgabe
  des Cover-Endpunkts auf `[]` gefaltet — die Leer-Prüfung in
  `LoadImageAsync` bildet 404 auf `null` ab.
- Fehlerbehandlung im Handler spiegelt `ShellAppEffectHandler`:
  Übersichts- und Eintragsfehler über die `AppUpdate.*Error`-
  Konstanten, Kopf-Nachschlag nicht fatal.
- Bekannte Rest-Risiken (CI-geprüft): `UITableView.
  RemembersLastFocusedIndexPath` und `TitleForHeader`-Signatur —
  beide tvOS-Standard-Bindings; Abweichungen würden `build-tvos`
  melden.
