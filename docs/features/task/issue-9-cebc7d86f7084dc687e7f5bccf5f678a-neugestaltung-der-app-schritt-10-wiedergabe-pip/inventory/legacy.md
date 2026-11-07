# Bestandsaufnahme: Altbestand-Referenz (`VideoWebPlayer.Maui`)

## Stream-URL-Bildung (`Services/DownloadManager.cs`)

`BuildStreamUrl(videoId, videoType)` (Zeile 223):

```
{serverAddress}/api/items/{videoType}/{videoId}/stream
  + ?access_token={Uri.EscapeDataString(token)}   (falls Token vorhanden)
```

- `videoType` wird über `NormalizeVideoType` auf die Server-
  Konvention gebracht (`movie`, `tvshowepisode`) — entspricht der
  CLR-Konvention von `MediaTypeNames.Normalize` der neuen App.
- Authentifizierung am Stream: Bearer-Token als Query-Parameter
  `access_token` (der MediaPlayer kann keine Header setzen).

## Fortsetzungsposition (`DownloadManager.cs`)

`FetchResumePositionFromServerAsync` (Zeile 177): liest
`GetContinueWatchingAsync`, sucht den Eintrag per
`MediaType`/`Entry.Id`, liefert `PositionSeconds` als `TimeSpan`.

## Quellenabstraktion (`Models/VideoRequest.cs`)

`VideoSourceInfo { SourcePath, SourceType, ResumePosition,
Duration }` mit `VideoSourceType { LocalFile, StreamUrl }` — die
Trennung, die die neue App plattformneutral nachbildet.

## Player-Komponente (`Components/MediaBannerPlayer.xaml(.cs)`)

- `MediaElement` mit `ShouldAutoPlay`, `ShouldShowPlaybackControls`,
  `StateChanged`, `MediaFailed` → Fehlerlabel.
- `MediaSource.FromUri(url)` bzw. `MediaSource.FromFile(path)` je
  `SourceType`.
- Fortschrittstakt: `System.Timers.Timer` mit 5 s, meldet Position
  nur bei Änderung (> 0,5 s Differenz) an
  `DownloadManager.UpdatePlaybackPositionAsync`; beim Stop/Wechsel
  wird die Position final gespeichert.
- Kein PiP im Altbestand; keine eigene Player-Seite (Wiedergabe
  eingebettet im Banner der Detailseiten). Eine `PlayerPage`
  existiert im Altbestand nicht.
