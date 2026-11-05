# Bestandsaufnahme — tvOS Playlists (lesend)

Detaildokumente:

- `inventory/mvu.md` — plattformneutrale Playlist-Bausteine (bereits vollständig vorhanden)
- `inventory/tvos.md` — tvOS-App: vorhandene Muster und fehlende Verdrahtung
- `inventory/maui-referenz.md` — Referenzverhalten der MAUI-App

## Ergebnis in Kürze

Die komplette Fachlogik des lesenden Playlist-Zugriffs existiert bereits
plattformneutral (`VideoPlayer.Mvu`): Zustände, Messages, Update-Übergänge,
`PlaylistService` mit den Endpunkten Übersicht/Kopfdaten/`entries/paged`/`play`
sowie next/previous/advance. Ebenso ist der Playlist-Wiedergabekontext
(`StartPlaylistPlaybackEffect`, next/previous/advance-Effekte) im
`TvosAppEffectHandler` seit Schritt 7 verdrahtet und `PlaylistService` ist in
`AppComposition` als `Playlists`-Singleton verfügbar.

Was fehlt, ist ausschließlich die tvOS-Oberfläche und die Verdrahtung der
vier Lese-Effekte:

| Effekt | Status im `TvosAppEffectHandler` |
|--------|----------------------------------|
| `StartPlaylistPlaybackEffect` | verdrahtet (`StartPlaylistPlaybackAsync`) |
| `LoadNextPlaylistEntryEffect` / `LoadPreviousPlaylistEntryEffect` / `AdvancePlaylistEntryEffect` | verdrahtet (`NavigatePlaylistEntryAsync`) |
| `LoadPlaylistsEffect` | **fehlt** (No-Op-Default) |
| `NavigateToPlaylistDetailsEffect` | **fehlt** (No-Op-Default) |
| `LoadPlaylistDetailsEffect` | **fehlt** (No-Op-Default) |
| `LoadPlaylistEntriesEffect` | **fehlt** (No-Op-Default) |

UI-Seitig fehlen: die Menüseite `AppPage.Playlists` (bisher
`PagePlaceholderViewController`), die modale Playlist-Detailseite
(`NavigateToPlaylistDetailsEffect` hat kein Pendant im `TvosNavigator`)
sowie der Cover-Abpfad `api/playlists/{id}/cover` im
`TvosPosterImageService` (Client-Methode `GetPlaylistCoverAsync`
existiert bereits im `VideoPlayerApiClient`, Rückgabe `byte[]?` —
`null` bei 404).

Kein neuer Schreibpfad: die Verwaltungs-Effekte bleiben bewusst im
No-Op-Default; der Contract-Test
`TvOsAppContractTests.TvOsEffectHandler_DoesNotWireExcludedAreas`
verbietet u. a. die Zeichenfolgen `SavePlaylistEffect`,
`DeletePlaylistEffect`, `RemovePlaylistEntryEffect`,
`AddPlaylistEntryEffect`, `DownloadService` und `EnqueueAsync` in
`TvosAppEffectHandler.cs` — auch in Kommentaren nicht verwenden.
