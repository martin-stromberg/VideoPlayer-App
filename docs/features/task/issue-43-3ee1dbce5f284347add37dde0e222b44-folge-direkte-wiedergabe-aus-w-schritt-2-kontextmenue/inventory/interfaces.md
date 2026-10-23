# Bestandsaufnahme: Interfaces

## `IAppEffectHandler`
Datei: `VideoPlayer.Mvu/IAppEffectHandler.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HandleAsync` | `AppEffect effect, CancellationToken` | `Task` | Führt einen MVU-Effekt am App-Rand aus (Implementierung: `ShellAppEffectHandler`). |

## `MediaCarousel`-Ereignisse (kein Interface, aber öffentlicher Vertrag)

| Ereignis | Signatur | Zweck |
|----------|----------|-------|
| `ItemTapped` | `EventHandler<HomeMediaItem>` | Tap-Auswahl → `ContinueWatchingItemSelectedMessage`/`MediaItemSelectedMessage`. |

Ein `ItemLongPressed`-Ereignis gleicher Signatur existiert noch nicht.

## Developer-API (`POST /invoke`-Vertrag)
Datei: `msTools.DeveloperApi.Maui/MauiDevApiService.cs` + `VideoPlayer.E2E.Tests/Support/DevApiClient.cs`

| Feld | Typ | Bedeutung |
|------|-----|-----------|
| `automationId` | `string` (Pflicht) | Zielelement. |
| `itemIndex` | `int?` | Index in `CollectionView`/`BindableLayout` → `SendTapped`-Kette. |

Keine Long-Press-Auslösemöglichkeit.
