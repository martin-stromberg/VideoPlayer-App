# Enums

## `DownloadState`
Datei: `VideoPlayer.Mvu/DownloadState.cs`

Zustand eines Download-Auftrags; im Manifest als kleingeschriebener String persistiert (`DownloadStateNames.ToPersisted`/`FromPersisted` — unbekannte Strings werden als `Failed` gelesen).

| Wert | Persistiert | Bedeutung |
|------|-------------|-----------|
| `Queued` | `queued` | Wartet auf freien Platz bzw. WLAN-Bedingung |
| `Downloading` | `downloading` | Transfer läuft |
| `Completed` | `completed` | Abgeschlossen; Datei liegt lokal vor |
| `Failed` | `failed` | Fehlgeschlagen (Fehlercode im Eintrag) |
| `Cancelled` | `cancelled` | Vom Anwender abgebrochen |

Zugehörige Hilfsklasse: `DownloadStateNames` (static) mit `ToPersisted(DownloadState)` und `FromPersisted(string?)`.

Nicht vorhanden: ein Enum/Contract für die Download-Herkunft (`manual`/`auto`).

## `PlaybackSourceKind`
Datei: `VideoPlayer.Mvu/PlaybackSourceKind.cs`

Art der aufgelösten Wiedergabequelle (`PlaybackSource.Kind`).

| Wert | Bedeutung |
|------|-----------|
| `ServerStream` | Stream-Endpunkt `api/items/{typ}/{id}/stream` |
| `LocalFile` | Lokale Videodatei aus dem Download-Manifest — Kennzeichen der Download-Wiedergabe (Triggerspunkt der Schutzprüfung aus Kriterium 15) |
