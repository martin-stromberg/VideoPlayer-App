# Interfaces (Wiedergabe)

## `IModelRenderable`
Datei: `VideoPlayer.TvOS/Ui/IModelRenderable.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `Render` | `AppModel model` | `void` | Modellstand auf die Seite spiegeln — der `TvosNavigator` verteilt an alle registrierten Seiten; der Player-Controller implementiert dieses Interface für die Quellanwendung. |

## `IAppEffectHandler`
Datei: `VideoPlayer.Mvu/IAppEffectHandler.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HandleAsync` | `AppEffect effect`, `CancellationToken` | `Task` | Effekte am App-Rand ausführen — tvOS-Implementierung: `TvosAppEffectHandler`. |

## Native Randstellen (tvOS, keine Projekt-Interfaces)

| Stelle | Rolle im Schritt |
|--------|------------------|
| `AVPlayerViewController` (`AVKit`) | Nativer Vollbild-Player mit System-Transport-UI und eingebauter Spurwahl im Info-/Pull-down-Panel (`CustomInfoViewControllers` tvOS 15+, `TransportBarCustomMenuItems` tvOS 11+) |
| `AVPlayer` (`AVFoundation`) | `AddPeriodicTimeObserver`/`RemoveTimeObserver` (5-s-Takt), `ReplaceCurrentItemWithPlayerItem`, `Seek`, `TimeControlStatus`, `Rate` |
| `AVPlayerItem` (`AVFoundation`) | `Status` (KVO), `Duration` (`CMTime`, kann `Indefinite` sein), `Error`, `IsPlaybackLikelyToKeepUp`, `ExternalMetadata`, `MediaSelectionGroup`-Zugriff; `Notifications.Observe*` für `DidPlayToEndTime`/`FailedToPlayToEndTime`/`PlaybackStalled` |
| `CMTime` (`CoreMedia`) | Positions-/Dauerwerte (`FromSeconds`, `Seconds`, `IsIndefinite`, `IsInvalid`) |
| `MainThreadInvoker` (`VideoPlayer.TvOS/Services`) | Hauptthread-Marshal für Dispatch und UIKit-Zugriffe |
