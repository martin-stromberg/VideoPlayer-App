# Enums

## `PlaybackSourceKind`
Datei: `VideoPlayer.Mvu/PlaybackSourceKind.cs`

Art der aufgelösten Wiedergabequelle (`PlaybackSource.Kind`). Im Fehlerprotokoll tritt das Seek-Timeout ausschließlich bei `ServerStream` auf; `LocalFile`-Quellen laufen durch dasselbe Seek-Gate (`ApplyResumeSeekAsync` unterscheidet die Art nicht).

| Wert | Bedeutung |
|------|-----------|
| `ServerStream` | Stream-Endpunkt des Servers (`api/items/{typ}/{id}/stream` mit `access_token`-Query) |
| `LocalFile` | Lokale Videodatei aus dem Download-Manifest |

## `MediaElementState` (extern, CommunityToolkit.Maui)
Paket: `CommunityToolkit.Maui.MediaElement` 10.0.0 (Verweis in `VideoPlayer.Maui/VideoPlayer.Maui.csproj`, Z. 68)

Kein eigener Enum — hier dokumentiert, weil der Fortsetzungs-Seek daran hängt: `TryApplyPendingResumeSeek` wendet `pendingResumePosition` nur bei `Playing` oder `Paused` an (beide implizieren `ReadyToPlay`); `ReportPosition`/Timer fragen `Playing`, `Buffering` und `Opening` ab.
