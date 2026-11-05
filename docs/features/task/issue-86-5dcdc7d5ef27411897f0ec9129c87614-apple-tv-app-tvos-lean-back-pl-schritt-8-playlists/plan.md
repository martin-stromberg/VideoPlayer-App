# Umsetzungsplan — tvOS Playlists (lesender Zugriff und Wiedergabe)

## Zielbild

Menüseite „Playlists" mit echter Übersicht (eigene + öffentliche
Playlists), modale Playlist-Detailseite mit seitenweise nachladbaren
Einträgen und „Abspielen"-Einstieg; Wiedergabe läuft über den in
Schritt 7 verdrahteten Playlist-Kontext. Kein Schreibzugriff —
Verwaltung bleibt ohne Oberfläche und ohne Effekt-Verdrahtung.

## Umsetzungsschritte

### 1. `TvosPosterImageService` — Playlist-Cover (erweitern)

`Services/TvosPosterImageService.cs`: `GetPlaylistCoverAsync(long playlistId)`
ergänzen — dritter Schlüsselraum (`coverCache`,
Datei `{playlistId}.playlistcover` im `PreviewsDirectory`), Abruf über
`client.GetPlaylistCoverAsync` (Rückgabe `byte[]?` → leer behandeln).
Die bestehende `LoadImageAsync`-Kaskade wiederverwenden (Fetch-Delegate
um die Null-Faltung ergänzen bzw. die Methode auf `byte[]?`-Rückgabe
lockern — minimaler Eingriff).

### 2. Übersichtsseite (neu) — `Ui/PlaylistsPageViewController.cs`

- `UIViewController, IModelRenderable` — rendert `model.PlaylistsPage`.
- Titel „Playlists" (`TvosControls.Title`).
- Darstellung: `UITableView` mit je einer Sektion pro Modell-Header
  („Meine Playlists"/„Öffentliche Playlists") — Gruppierung der flachen
  `PlaylistOverviewItem`-Liste im Controller; `UITableViewSource` mit
  `NumberOfSections`, `RowsInSection`, `TitleForHeader`,
  `GetHeightForRow`, `GetCell`, `RowSelected`.
- Zeilen: neue `PlaylistTableCell` (`UITableViewCell`-Unterklasse,
  `Ui/PlaylistTableCell.cs`): Cover (240×135, `ScaleAspectFill`,
  abgerundet, `GetPlaylistCoverAsync` über `CoverPlaylistId`,
  Namens-Fallback), Name, Unterzeile `SortModeText` + `GenreText`.
- `ViewDidAppear` → `PlaylistsPageOpenedMessage` (jedes Erscheinen —
  deckt Laden, erneutes Laden bei Tab-Rückkehr und die Rückkehr vom
  modalen Detail ab); Retry-Button dispatcht dieselbe Message.
- Lade- („Playlists werden geladen…"), Leer- („Keine Playlists
  vorhanden.") und Fehlerzustand (`Error`-Text + „Erneut versuchen")
  analog `SourceBrowserViewController.ApplyState`.

### 3. Detailseite (neu) — `Ui/PlaylistDetailsViewController.cs`

- `UIViewController, IModelRenderable` — rendert `model.PlaylistDetails`
  mit Identitäts-Guard (`details.PlaylistId == playlistId`).
- `SetPlaylist(playlistId, name)` vor der Präsentation; `ViewWillAppear`
  → `PlaylistDetailsOpenedMessage(playlistId, name)` unter derselben
  Bedingung wie die MAUI-Seite (`PlaylistId` weicht ab oder
  `!Attempted`).
- Kopfbereich: Cover (links, 280×158 — optionale Anzeige via
  `CoverPlaylistId`), Titel, `SummaryText`, `GenreText`,
  `Description` (max. 3 Zeilen); Aktionszeile „▶ Abspielen" →
  `PlaylistPlaybackStartRequestedMessage(playlistId, null)`.
- Eintragsraster: vertikale `UICollectionView` (`ItemSize` ≈ 300×560)
  mit neuem `PlaylistEntryCell` (`Ui/PlaylistEntryCell.cs`): 2:3-Poster
  über `ResolvedPictureId` (`GetImageAsync`), Titel, Untertitel
  (`item.Subtitle` = übergeordneter Kontext), `IsAccessible == false`
  abgedunkelt; `appliedPictureId`-Guard.
- `EntriesSource : UICollectionViewSource` mit
  `WillDisplayCell`-Lookahead (`NextPageLookahead = 5`) →
  `PlaylistEntriesNextPageRequestedMessage`; `ItemSelected` →
  `PlaylistEntrySelectedMessage(item)`.
- `IsLoadingMore`-Hinweis unten; Fehler- (`Error` + „Erneut versuchen"
  → `PlaylistEntriesRetryRequestedMessage`), Leer- und Ladezustand.
- `PressesBegan` Menü-Taste → `DismissViewController` +
  `onDismissed`-Freigabe (Muster `MediaDetailsViewController`).
- Keine Verwaltungselemente.

### 4. Navigation — `Ui/TvosNavigator.cs` (erweitern)

- Feld `playlistDetailsPage` + Freigabeliste
  (`List<PlaylistDetailsViewController>` oder Singleton — Entscheidung:
  Singleton wie `sourceBrowserPage`, da `AppModel.PlaylistDetails`
  global und eindeutig ist; Medien-Details können darüber gestapelt
  werden).
- `PresentPlaylistDetails(NavigateToPlaylistDetailsEffect)` bzw.
  `(long playlistId, string name)`: `SetPlaylist`, Duplikat-Guard
  (bereits präsentiert → nicht erneut stapeln), `FullScreen`,
  `TopmostPresented().PresentViewController`.
- `Render`: `playlistDetailsPage?.Render(model)`.

### 5. Menürahmen — `Ui/MenuTabBarController.cs` (erweitern)

`AppPage.Playlists` → `new PlaylistsPageViewController(dispatch, posters)`
im `CreatePageController`-Switch.

### 6. Effekt-Handler — `Services/TvosAppEffectHandler.cs` (erweitern)

Neue `case`-Zweige + Methoden (Muster `ShellAppEffectHandler`):

- `LoadPlaylistsEffect` → `LoadPlaylistsAsync` →
  `PlaylistsLoadedMessage(own, public, error)`,
  Fehler `AppUpdate.PlaylistsLoadError`.
- `NavigateToPlaylistDetailsEffect` →
  `MainThreadInvoker.InvokeOnMainThreadAsync` →
  `navigator.PresentPlaylistDetails(...)`.
- `LoadPlaylistDetailsEffect` → `LoadPlaylistDetailsAsync` →
  `PlaylistDetailsLoadedMessage(id, info, isAdmin)` —
  `IsCurrentUserAdminAsync` nicht fatal.
- `LoadPlaylistEntriesEffect` → `LoadPlaylistEntriesAsync` →
  `PlaylistEntriesLoadedMessage(id, page, items, hasMore, totalCount,
  append, error)`, Fehler `AppUpdate.PlaylistEntriesLoadError`,
  Leer-Seite als Fehlerwert (`PlaylistEntriesPage(page, [], false, 0)`).

Klassen-Dokukommentar anpassen (Playlist-Lesezugriff mit aufnehmen).
Achtung: die Zeichenfolgen `SavePlaylistEffect`,
`DeletePlaylistEffect`, `RemovePlaylistEntryEffect`,
`AddPlaylistEntryEffect`, `DownloadService`, `EnqueueAsync` dürfen in
der Datei nicht vorkommen (Contract-Test) — auch nicht in Kommentaren.

### 7. Contract-Tests (neu) — `VideoPlayer.Maui.Tests/TvOsPlaylistsContractTests.cs`

Quell-Prüfungen analog `TvOsPlaybackContractTests`:

- Übersichtsseite: `PlaylistsPageOpenedMessage`,
  `PlaylistSelectedMessage`, `UITableView`, `PlaylistsPage`,
  `IModelRenderable`.
- Detailseite: `PlaylistDetailsOpenedMessage`,
  `PlaylistEntrySelectedMessage`,
  `PlaylistEntriesNextPageRequestedMessage`,
  `PlaylistEntriesRetryRequestedMessage`,
  `PlaylistPlaybackStartRequestedMessage`, Identitäts-Guard,
  `DismissViewController`.
- Navigator: `PresentPlaylistDetails`, `FullScreen`, Render-Verteilung.
- Handler: die vier `case`-Zweige + Ergebnis-Messages.
- `MenuTabBarController`: `PlaylistsPageViewController`.
- `TvosPosterImageService`: `GetPlaylistCoverAsync`.
- Negativ-Prüfung: keine Verwaltungs-Messages in den neuen Seiten
  (`PlaylistCreateRequestedMessage`, `PlaylistEditRequestedMessage`,
  `PlaylistDeleteConfirmedMessage`, `PlaylistEntryRemoveRequestedMessage`).

### 8. Dokumentation

- `docs/help/tv-app/beschreibung.md`: Playlist-Abschnitt (Übersicht,
  Detailansicht, Wiedergabekontext, Ausschluss der Verwaltung);
  Einschränkungsliste aktualisieren (Playlist-Menüseite kein
  Platzhalter mehr).
- `docs/help/tv-app/ablauf-anwender.md`: Schritt „Playlists nutzen".
- `README.md`: tvOS-Zeilen um den Playlist-Lesezugriff ergänzen.

### 9. Validierung

- `dotnet test VideoPlayer.Maui.Tests` (Windows, plattformneutral).
- Push → `build-tvos` (macOS-CI) als Kompiliernachweis; Iteration bis
  grün (`env -u GH_TOKEN gh run watch <id> --repo
  martin-stromberg/VideoPlayer-App --exit-status`).

## Offene Punkte

Keine.
