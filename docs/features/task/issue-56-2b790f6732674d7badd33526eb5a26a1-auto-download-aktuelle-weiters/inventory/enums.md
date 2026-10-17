# Enums — Bestandsaufnahme

## `DownloadState`
Datei: `VideoPlayer.Mvu/DownloadState.cs` (Z. 8–24)

Zustand eines Download-Auftrags; persistiert als kleingeschriebener String über `DownloadStateNames` (Z. 30–62, unbekannte Werte lesen als `Failed`).

| Wert | Persistiert | Bedeutung |
|------|-------------|-----------|
| `Queued` | `queued` | Wartet auf freien Platz bzw. WLAN-Bedingung — Dedup liefert `NoticeAlreadyQueued`. |
| `Downloading` | `downloading` | Transfer läuft — Dedup liefert `NoticeAlreadyRunning` (nur wenn ein laufender Transfer registriert ist). |
| `Completed` | `completed` | Datei liegt lokal vor — Dedup liefert `NoticeAlreadyPresent` (nur wenn die Datei existiert); Voraussetzung des `DeleteWatchedDownloadEffect`-Pfads (`completedOnly`). |
| `Failed` | `failed` | Fehlgeschlagen (Fehlercode im Manifest) — wird bei erneutem `EnqueueAsync` zurück in `Queued` überführt. |
| `Cancelled` | `cancelled` | Vom Anwender abgebrochen — ebenfalls re-enqueue-fähig, wird nicht automatisch wiederaufgenommen. |

## `ConnectionState`
Datei: `VideoPlayer.Mvu/ConnectionState.cs` (Z. 9 ff.)

Verbindungszustand des App-Modells; die `AppUpdate`-Trigger des Vorlaufs verlangen `ConnectionState.Connected`.

| Wert | Bedeutung |
|------|-----------|
| `Unknown` | Startzustand vor dem Verbindungs-Workflow. |
| `Connecting` | Verbindungsaufbau läuft. |
| `Connected` | Verbunden und angemeldet — einziger Zustand, in dem `RunAutoDownloadsEffect` emittiert wird. |
| `NeedsLogin` | Server erreichbar, Anmeldung erforderlich. |
| `NeedsServerSetup` | Kein Server hinterlegt / kein Offline-Betrieb. |
| `Offline` | Server nicht erreichbar, lokale Offline-Daten vorhanden. |
