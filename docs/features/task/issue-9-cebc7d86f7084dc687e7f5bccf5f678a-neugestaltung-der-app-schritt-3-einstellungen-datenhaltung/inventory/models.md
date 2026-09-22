# Datenmodell und MVU-Typen

## Neue App (`VideoPlayer.Mvu`, net10.0)

### `AppModel`
Datei: `VideoPlayer.Mvu/AppModel.cs`

Unveränderliches Gesamtmodell (`sealed record`, `init`-Properties).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `CurrentPage` | `AppPage` | Aktuell angezeigte Seite, Start `Loading` |
| `IsStartupRunning` | `bool` | Start-Workflow läuft |
| `ServerHealth` | `ServerHealthStatus` | Ergebnis der Erreichbarkeitsprüfung |
| `ServerHealthText` | `string` (abgeleitet) | Anzeigbarer Status-Text |

Kein Einstellungszustand vorhanden — muss ergänzt werden.

### `AppMessage`-Typen (`VideoPlayer.Mvu/AppMessage.cs`)
`AppMessage` (abstract record) mit `StartupRequestedMessage`,
`ServerHealthCheckCompletedMessage(bool IsReachable)`, `NavigateToMessage(AppPage)`,
`NavigatedMessage(AppPage)`. Keine Einstellungs-Messages vorhanden.

### `AppEffect`-Typen (`VideoPlayer.Mvu/AppEffect.cs`)
`AppEffect` (abstract record) mit `NavigateToPageEffect(AppPage)`,
`CheckServerHealthEffect`. Keine Persistenz-Effekte vorhanden.

### `AppTransition` (`VideoPlayer.Mvu/AppTransition.cs`)
`record AppTransition(AppModel Model, IReadOnlyList<AppEffect> Effects)` mit
`Unchanged(model)`.

## Referenz Alt-App (`VideoWebPlayer.Maui` — nicht wiederverwendet)

### `DownloadedVideo` (`VideoWebPlayer.Maui/Models/DownloadedVideo.cs`)
sqlite-net-Entität der Tabelle `Downloads`: u. a. `VideoId`, `VideoType`,
`Title`, lokale Bildpfade, `PlaybackPosition`, `ExpiresAt`, `RetentionType`,
`FileSizeBytes`, `LocalFilePath` (Ablage: `AppDataDirectory/downloads[_profil].db3`).

### `CachedCarouselItem` (`VideoWebPlayer.Maui/Models/CachedCarouselItem.cs`)
sqlite-net-Entität der Tabelle `CarouselCache` (Startseiten-Listen-Cache).

Beide liegen in einer einzigen Datenbank ohne Backup-Klassifikation — die neue
Ablage muss backup-pflichtige und cacheartige Daten trennen.
