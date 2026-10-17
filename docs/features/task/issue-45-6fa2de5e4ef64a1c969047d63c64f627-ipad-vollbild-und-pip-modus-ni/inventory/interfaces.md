# Interfaces — Bestandsaufnahme

## `IPictureInPictureService`

Datei: `VideoPlayer.Maui/Services/IPictureInPictureService.cs`

Plattformbezogener PiP-/Mini-Player-Dienst am App-Rand. Auf iOS steht
PiP über die native Transportsteuerung des `AVPlayerViewController`
bereit — `IsToggleSupported` ist dort `false` und kein eigener Schalter
nötig; auf Windows schaltet die Implementierung das App-Fenster in den
kompakten Overlay-Modus.

| Member | Parameter | Rückgabewert | Zweck |
|--------|-----------|--------------|-------|
| `IsToggleSupported` (Property) | — | `bool` | Plattform bietet eigene Umschaltung (Windows) → `PipButton` sichtbar |
| `IsActive` (Property) | — | `bool` | Kompakter Overlay-Modus gerade aktiv |
| `ToggleAsync()` | — | `Task<bool>` | Umschalten aktiv ↔ normal; Rückgabe = neuer Zustand |
| `RestoreAsync()` | — | `Task` | Normalmodus wiederherstellen (z. B. beim Verlassen der Seite) |

Implementierung: `PictureInPictureService`
(`VideoPlayer.Maui/Services/PictureInPictureService.cs`) — Details siehe
[logic.md](logic.md). Registrierung: `MauiProgram.cs:117`
(`AddSingleton<IPictureInPictureService, PictureInPictureService>`).

Verwendung:

- `PlayerPage`-Konstruktor (`PlayerPage.xaml.cs:53`):
  `PipButton.IsVisible = pictureInPicture.IsToggleSupported`
- `PlayerPage.OnDisappearing` (`PlayerPage.xaml.cs:92`):
  `pictureInPicture.RestoreAsync()`
- `ShellAppEffectHandler.TogglePictureInPictureAsync`
  (`ShellAppEffectHandler.cs` ≈Zeile 485): `ToggleAsync()` gemarshalt →
  `PictureInPictureChangedMessage`

Weitere Interfaces sind von der Anforderung nicht betroffen.
