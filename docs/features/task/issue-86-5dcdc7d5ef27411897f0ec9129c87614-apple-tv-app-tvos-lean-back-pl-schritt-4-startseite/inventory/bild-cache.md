# Bild- und Cache-Pfad — Bestandsaufnahme

## Server-Konvention

- Poster: `GET api/pictures/{id}` → `VideoPlayerApiClient.GetPictureAsync(long pictureId, CancellationToken)` liefert `byte[]` (VideoPlayerApiClient.cs:470; wirft bei `pictureId <= 0`, Fehler → Exception). Authentifizierung über den angemeldeten Client (`AuthSessionService.Client`).
- `HomeMediaItem.PosterPictureId` (`long?`) — `HasPoster => PosterPictureId is > 0`; fehlt → Titel-Fallback.

## MAUI-Referenz: `PosterImageService` (`VideoPlayer.Maui/Services/PosterImageService.cs`)

Dreistufig: `ConcurrentDictionary<long, ImageSource>` → Datei `{pictureId}.poster` in `AppStorageLayout.PreviewsDirectory` → `client.GetPictureAsync` + `File.WriteAllBytesAsync`. Fehlschlag (offline/kein Client/leere Bytes/Fehler) → `null`, nur `LogWarning` — „fehlende Vorschaubilder sind kein Fehlerzustand" (Zeile 87). `OperationCanceledException` wird nicht geschluckt-als-Fehler geloggt (Filter `ex is not OperationCanceledException`).

## Ablage

- `AppStorageLayout.PreviewsDirectory` existiert plattformneutral und wird via `TvosStorageLayoutProvider.CreateLayout` + `EnsureDirectoriesExist` angelegt (unter `Library/Caches/videoplayer/cache/...`); tvOS-Caches dürfen vom System geräumt werden — Bilder sind ohnehin nachladbar.
- `UIImage.LoadFromData(NSData)` bzw. `new UIImage(NSData)` erzeugt das Anzeigebild aus den Bytes.

## Cache-Konventionen, die zu übernehmen sind

- Dateiname `{pictureId}.poster` im Previews-Verzeichnis (Parität zur MAUI-Ablage — geteilte Konvention).
- Arbeitsspeicher-Cache `ConcurrentDictionary<long, UIImage>`.
- Fehlschlag → `null` (Zelle zeigt Titel-Platzhalter), keine Fehlerpropagierung in die UI.
- Recycle-Sicherheit der Zelle: asynchrone Rückkehr nur anwenden, wenn die Zelle noch denselben Eintrag zeigt (Generations-/Id-Check).
