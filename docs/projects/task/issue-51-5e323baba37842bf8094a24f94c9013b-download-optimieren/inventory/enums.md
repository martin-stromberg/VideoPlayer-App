# Enums

## `DownloadState`
Datei: `VideoPlayer.Mvu/DownloadState.cs`

| Wert | Bedeutung |
|------|-----------|
| `Queued` | Wartet auf freien Platz bzw. WLAN-Bedingung (`"queued"`) |
| `Downloading` | Transfer läuft (`"downloading"`) |
| `Completed` | Abgeschlossen, Datei liegt lokal (`"completed"`) |
| `Failed` | Fehlgeschlagen, Fehlercode am Eintrag (`"failed"`) |
| `Cancelled` | Vom Anwender abgebrochen (`"cancelled"`) |

`DownloadStateNames` übersetzt zwischen Enum und persistiertem String;
unbekannte/leere Werte werden als `Failed` gelesen.
