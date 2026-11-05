# Bestandsaufnahme — tvOS-App (`VideoPlayer.TvOS`)

## Vorhandene Muster (Wiederverwendung)

- `Ui/SourceBrowserViewController.cs` — Muster für die modale
  Vollbild-Seite mit vertikalem `UICollectionView`-Raster,
  `WillDisplayCell`-Lookahead-Paging (`NextPageLookahead = 5`),
  `ItemsSource : UICollectionViewSource`, Lade-/Leer-/Fehlerzustand
  über `TvosControls.Status`/`TvosControls.Button`/`TvosControls.Section`,
  `RemembersLastFocusedIndexPath`, `PressesBegan` Menü-Taste →
  `DismissViewController`, Identitäts-Guard im `Render`
  (`browser.IsActive && browser.SourceId == sourceId`),
  `ViewWillAppear`-Wiederherstellungs-Message.
- `Ui/MediaDetailsViewController.cs` — Muster für modalen
  Detailseiten-Stapel inkl. `onDismissed`-Callback an den Navigator
  (Freigabeliste `detailPages`) und Kopfbereich mit
  „Abspielen"-Aktion (`TvosControls.Button`).
- `Ui/MenuTabBarController.cs` — Tab-Erzeugung je `AppRoutes.MenuPages`;
  `AppPage.Playlists` fällt bisher auf `PagePlaceholderViewController`.
- `Ui/ServerListTableSource.cs` — `UITableViewSource`-Muster
  (Untertitel-Zellen, `DequeueReusableCell` ohne Registrierung).
- `Ui/HomeMediaItemCell.cs`/`Ui/EpisodeItemCell.cs` —
  fokussierbare Poster-/Still-Karten mit asynchronem Bildabruf,
  `appliedPictureId`-Guard gegen Zellen-Recycling, Titel-Fallback.
- `Services/TvosAppEffectHandler.cs` — Switch-Muster; Playlist-
  Wiedergabe-Effekte bereits verdrahtet (`StartPlaylistPlaybackAsync`,
  `NavigatePlaylistEntryAsync`); `services.Playlists` =
  `PlaylistService`-Singleton (mit `Secrets` für den Admin-Claim);
  `DispatchMessageAsync` auf den Hauptthread; Fehler →
  `AppUpdate.*Error`-Konstanten.
- `Ui/TvosNavigator.cs` — `PresentSourceBrowser` (Singleton-Seite,
  Duplikat-Guard), `PresentMediaDetails` (Stapel),
  `TopmostPresented()` als Präsentationsanker,
  `Render(model)`-Verteilung an alle Seiten.
- `Services/TvosPosterImageService.cs` — Cache-Kaskade
  Speicher → Datei (`PreviewsDirectory`) → Server;
  getrennte Schlüsselräume je Endpunkt (`.poster`, `.sourceicon`).

## Fehlende Verdrahtung

- `TvosAppEffectHandler`: `LoadPlaylistsEffect`,
  `NavigateToPlaylistDetailsEffect` (Hauptthread → Navigator),
  `LoadPlaylistDetailsEffect`, `LoadPlaylistEntriesEffect`.
- `TvosNavigator`: `PresentPlaylistDetails(PlaylistId, Name)` +
  Render-Verteilung; Singleton-Seite (ein globaler
  `PlaylistDetails`-Zustand im Modell) mit Duplikat-Guard wie
  `sourceBrowserPage`.
- `MenuTabBarController`: `AppPage.Playlists` → echte Seite.
- `TvosPosterImageService`: Playlist-Cover
  (`client.GetPlaylistCoverAsync(playlistId)` → `byte[]?`), eigener
  Cache-Schlüsselraum (`.playlistcover`, Schlüssel = Playlist-Id).

## tvOS-Kompilierfallen (aus früheren Schritten)

- `IReadOnlyList.IndexOf` fehlt → manuelle Schleife (Muster
  `MenuTabBarController.IndexOfMenuPage`).
- `const nfloat` unzulässig → `static readonly nfloat`.
- `RegisterClassForCell(typeof(Cell), "id")` — string-Reuse-Id
  (`public const string`) ist implizit nach `NSString` konvertierbar.
- `UIAction`-Handler-Parameter nicht als `_` benutzen;
  `presses.ToArray<UIPress>()` für `NSSet<UIPress>`.
- Basistyp-Member/Named-Args-Case gegen Quelltext prüfen.
