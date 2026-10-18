# Enums

## `DownloadState`
Datei: `VideoPlayer.Mvu/DownloadState.cs`

| Wert | Bedeutung |
|------|-----------|
| `Queued` | Wartet auf freien Platz/WLAN-Bedingung |
| `Downloading` | Transfer läuft |
| `Completed` | Abgeschlossen, Datei liegt lokal vor |
| `Failed` | Fehlgeschlagen (Fehlercode am Eintrag) |
| `Cancelled` | Vom Anwender abgebrochen |

`DownloadStateNames.ToPersisted`/`FromPersisted` bilden auf die Manifest-Strings ab; unbekannte Werte werden als `Failed` gelesen — die Filterung muss daher über `FromPersisted` laufen, damit Altstände/Unbekanntes korrekt einbezogen werden.

Für die Anforderung relevant: die Menge `Failed`/`Cancelled` entspricht genau der `CanRetry`-Menge des `DownloadListItem`.
