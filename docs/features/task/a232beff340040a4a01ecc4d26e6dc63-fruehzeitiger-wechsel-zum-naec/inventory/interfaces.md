# Interfaces

## `IAppEffectHandler`
Datei: `VideoPlayer.Mvu/IAppEffectHandler.cs`

Führt MVU-Effekte am App-Rand aus; implementiert von `ShellAppEffectHandler` (`VideoPlayer.Maui`). Über diese Schnittstelle laufen `ResolvePlaybackSourceEffect` und `AdvancePlaylistEntryEffect` — die Effekte, die den im Protokoll sichtbaren Episodenwechsel auslösen.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HandleAsync` | `AppEffect effect`, `CancellationToken` | `Task` | Führt einen einzelnen Effekt aus (Navigation, Server-Aufrufe o. ä.) |

## `IPictureInPictureService`
Datei: `VideoPlayer.Maui/Services/IPictureInPictureService.cs`

Plattformbezogener PiP-/Mini-Player-Dienst; Konstruktor-Abhängigkeit der `PlayerPage` (Schalter-Sichtbarkeit, Wiederherstellen beim Verlassen). Für den Seek-Fehlerpfad ohne direkte Rolle.

| Methode/Eigenschaft | Parameter | Rückgabewert | Zweck |
|---------------------|-----------|--------------|-------|
| `IsToggleSupported` | — | `bool` | Plattform bietet eigene Umschaltung (Windows) → Mini-Player-Schalter |
| `IsActive` | — | `bool` | Kompakter Overlay-Modus aktiv |
| `ToggleAsync` | — | `Task<bool>` | Schaltet den Overlay-Modus um |
| `RestoreAsync` | — | `Task` | Stellt den normalen Fenstermodus wieder her |
