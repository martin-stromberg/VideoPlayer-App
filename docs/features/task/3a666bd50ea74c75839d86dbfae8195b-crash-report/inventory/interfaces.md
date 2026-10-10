# Interfaces — Wiedergabe-Pfad

## `IAppEffectHandler`

Datei: `VideoPlayer.Mvu/IAppEffectHandler.cs` — implementiert von
`ShellAppEffectHandler` (`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HandleAsync` | `AppEffect effect`, `CancellationToken` | `Task` | Führt einen Effekt am App-Rand aus (Navigation, Server-Aufrufe). Wird vom `MvuHost` sequenziell `await`ed. |

## `IPictureInPictureService`

Datei: `VideoPlayer.Maui/Services/IPictureInPictureService.cs` —
implementiert von `PictureInPictureService` (Singleton,
`MauiProgram.cs` :103)

| Methode/Property | Parameter | Rückgabewert | Zweck |
|------------------|-----------|--------------|-------|
| `IsToggleSupported` | — | `bool` | Plattform mit eigener Umschaltung (nur Windows `true`; iOS `false` → `PipButton` ausgeblendet). |
| `IsActive` | — | `bool` | Kompakter Overlay-Modus aktiv. |
| `ToggleAsync` | — | `Task<bool>` | Umschalten aktiv ↔ normal; auf iOS No-op (immer `false`). |
| `RestoreAsync` | — | `Task` | Normalmodus wiederherstellen (beim Verlassen der Seite); iOS No-op. |

## `IMediaElement` / `IAsynchronousMediaElementHandler` (extern, Toolkit)

`CommunityToolkit.Maui.Core.IMediaElement` — `SeekTo(TimeSpan,
CancellationToken)` liefert `Task`; die Vollendung hängt an
`SeekCompletedTCS` (Toolkit-intern). Auf dem Absturzpfad relevant:
`MediaElement.SeekTo` delegiert über `Handler?.Invoke(SeekRequested)`
an `MediaElementHandler.MapSeekRequested` (`async void`) — Fehler
aus `PlatformSeek` werden dort nicht in den `SeekCompletedTCS`
überführt, sondern als unbehandelte Ausnahme des `async void`
ausgelöst (siehe [playback-start-path.md](playback-start-path.md) §6.1).
