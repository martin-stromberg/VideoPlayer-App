# Enums (Wiedergabe)

## `PlaybackSourceKind`
Datei: `VideoPlayer.Mvu/PlaybackSourceKind.cs`

| Wert | Bedeutung |
|------|-----------|
| `ServerStream` | Server-Stream-URL (`api/items/{typ}/{id}/stream` mit `access_token`-Query) |
| `LocalFile` | Lokale Datei aus dem Download-Manifest (Vorrang vor Stream) |

## Native Seite (tvOS, kein Projekt-Enum)

| Binding | Relevanz |
|---------|----------|
| `AVPlayerItemStatus` (`Unknown`/`ReadyToPlay`/`Failed`) | Bereitschaft für Fortsetzungs-Seek bzw. `PlaybackFailedMessage` |
| `AVPlayerTimeControlStatus` (`Paused`/`WaitingToPlayAtSpecifiedRate`/`Playing`) | `IsPlaying`/`IsBuffering`-Mapping des Fortschrittstakts |
| `AVMediaCharacteristic` (`Audible`/`Legible`) | `AVMediaSelectionGroup`-Auswertung der Spurwahl |
