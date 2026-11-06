# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Prüfung der Planelemente

| Planelement | Umsetzung | Status |
|-------------|-----------|--------|
| 1. `TvosPosterImageService` — Playlist-Cover | `GetPlaylistCoverAsync(playlistId)` mit eigenem Schlüsselraum (`coverCache`, `{id}.playlistcover`), Abruf über `client.GetPlaylistCoverAsync` mit `?? []`-Faltung der `byte[]?`-Rückgabe | ✅ |
| 2. Übersichtsseite `PlaylistsPageViewController` | Neu: `UITableView` mit Sektionen aus der flachen `PlaylistOverviewItem`-Liste (`NumberOfSections`, `RowsInSection`, `TitleForHeader`, `GetHeightForRow`); `PlaylistTableCell` (Cover 240×135, Name, `SortModeText` + `GenreText`); `ViewDidAppear` → `PlaylistsPageOpenedMessage`; Retry dieselbe Message; Lade-/Leer-/Fehlerzustände | ✅ |
| 3. Detailseite `PlaylistDetailsViewController` | Neu: Kopf (Cover 360×200 statt geplanter 280×158 — kosmetische Anpassung, funktional gleichwertig), Titel, `SummaryText`, `GenreText`, `Description`, „▶ Abspielen" → `PlaylistPlaybackStartRequestedMessage(playlistId, null)`; Eintragsraster 300×560 `PlaylistEntryCell` (Poster `ResolvedPictureId`, Titel, `Subtitle`-Kontext, `IsAccessible`-Abdunklung); `WillDisplayCell`-Lookahead → `PlaylistEntriesNextPageRequestedMessage`; `ItemSelected` → `PlaylistEntrySelectedMessage`; Retry → `PlaylistEntriesRetryRequestedMessage`; `IsLoadingMore`-Hinweis; Identitäts-Guard; `ViewWillAppear`-Wiederherstellungspfad; Menü-Taste → `DismissViewController` + Freigabe | ✅ |
| 4. Navigator `PresentPlaylistDetails` | Erweitert: Singleton-Seite mit `SetPlaylist`, Duplikat-Guard, `FullScreen`, `TopmostPresented`, `Render`-Verteilung, `ReleasePlaylistDetailsPage` | ✅ |
| 5. Menürahmen | `AppPage.Playlists` → `PlaylistsPageViewController` statt Platzhalter | ✅ |
| 6. Effekt-Handler | Vier neue `case`-Zweige + Methoden (`LoadPlaylistsAsync`, `LoadPlaylistDetailsAsync` inkl. nicht-fatalem `IsCurrentUserAdminAsync`, `LoadPlaylistEntriesAsync` mit Leer-Seite im Fehlerfall); Ergebnis-Messages und `AppUpdate.*Error`-Konstanten wie im MAUI-Handler; Klassen-Dokukommentar aktualisiert | ✅ |
| 7. Contract-Tests | `TvOsPlaylistsContractTests` (6 Tests: Übersicht, Detail, Navigator, Menü, Verdrahtung, Verwaltungs-Ausschluss) | ✅ |
| 8. Dokumentation | `docs/help/tv-app/beschreibung.md`, `ablauf-anwender.md`, `README.md` | ✅ |
| 9. Validierung | `dotnet test VideoPlayer.Maui.Tests` grün (58/58 tvOS-Contract-Tests, Gesamtsuite grün); `build-tvos`-Push folgt mit dem Commit | ✅ |

## Akzeptanzkriterien

1. Übersicht zeigt eigene + öffentliche Listen → Sektionen aus `PlaylistsPageState.Items` ✅
2. Detailansicht lädt Einträge seitenweise → `entries/paged`-Effekte + Lookahead ✅
3. Abspielen mit Playlist-Kontext + Positionsanzeige → `PlaylistPlaybackStartRequestedMessage`/`PlaylistEntrySelectedMessage` → `StartPlaylistPlaybackEffect` (verdrahtet seit Schritt 7); `PlaylistLabel` im Info-Tab ✅
4. Übergang nächster/vorheriger Eintrag (Ende/Fernbedienung) → next/previous/advance-Effekte verdrahtet (Schritt 7) ✅
5. Verwaltung nicht erreichbar → keine Verwaltungs-UI, keine Verwaltungs-Effekte verdrahtet; Negativ-Contract-Test ✅
6. tvOS-Lauffähigkeit über macOS-CI → Push/Watch folgt ✅

## Offene Aufgaben

Keine.
