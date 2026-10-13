# Enums

## `PlaybackSourceKind`

Datei: `VideoPlayer.Mvu/PlaybackSourceKind.cs`

Art einer aufgelösten Wiedergabequelle (`PlaybackSource.Kind`) — steuert in `PlayerPage.UpdateUi` die Quellbelegung (`MediaSource.FromFile` vs. `MediaSource.FromUri`) und die `IsValidSourceLocation`-Prüfung.

| Wert | Bedeutung |
|------|-----------|
| `ServerStream` | Stream-Endpunkt des Servers (`api/items/{typ}/{id}/stream`); `Location` = absolute http/https-URL (inkl. Zugriffstoken) |
| `LocalFile` | Lokale Videodatei aus dem Download-Manifest; `Location` = Dateipfad |
