# Bestandsaufnahme: Download-Reihenfolge bei Playlist-Bezug (Schritt 7)

Analysiert wurde das Download-Subsystem (`DownloadService`, `AutoDownloadService`, `download_manifest`) und der Playlist-Bezug der Weiterschauen-Einträge (`ContinueWatchingEntry` → `HomeMediaItem`) bezogen auf die Anforderung, Downloads playlist-gebundener Videos in Playlist-Reihenfolge einzuplanen.

## Zusammenfassung

**Antwort auf die fachliche Leitfrage: Ja — der Ist-Zustand plant Downloads playlist-gebundener Weiterschauen-Einträge in falscher (Serien-)Reihenfolge ein.**

- `AutoDownloadService.RunAsync` liest die gecachte Weiterschauen-Liste und ruft für Episoden-Einträge `EnqueueNextEpisodesAsync` auf, das die Folge-Episoden **ausschließlich aus dem Serienkontext** (`api/items/tvshow/{showId}`, staffelübergreifend nach Staffel-/Episodennummer) einreiht. Die am `HomeMediaItem` seit Schritt 5 vorhandenen Felder `PlaylistId`/`PlaylistEntryId`/`PlaylistName` werden **nicht ausgewertet** → playlist-gebundene Episoden-Einträge erhalten Folge-Downloads in Serien- statt Playlist-Reihenfolge.
- Playlist-gebundene **Film**-Einträge laden nur den Film selbst — Folge-Einträge der Playlist werden nicht vorgeladen.
- Die **Playlist-Wiedergabe beeinflusst den Download-Vorlauf nicht direkt**: Der Vorlauf wird ausschließlich über `RunAutoDownloadsEffect` getriggert (Einstellung einschalten, `AppUpdate.cs` Z. 334; Sync-/Cache-Ereignisse, Z. 1123/1140) und arbeitet auf der gecachten Weiterschauen-Liste. Playlist-gebundene Wiedergabe wirkt nur indirekt — der Server markiert den Weiterschauen-Eintrag mit `PlaylistId`/`PlaylistEntryId`, der nach der nächsten Synchronisation in den Vorlauf einfließt (dann aber mit falscher Reihenfolge, s. o.).
- Die Download-Warteschlange ist **FIFO** über die Manifest-Id (`NextQueuedEntryAsync` → `OrderBy(e => e.Id)`, `DownloadService.cs` Z. 545–553); die Reihenfolge wird also vollständig durch die **Einreihe-Reihenfolge** bestimmt — die Korrektur gehört in die Einplanung, nicht in die Queue-Abholung.
- `DownloadManifestEntry` hat **kein Playlist-Feld**; `DownloadSeriesContext` trägt nur `ShowId`/`ShowTitle`/`SeasonNumber`/`EpisodeNumber`. Schema-Erweiterungen sind etabliert und upgrade-sicher: `CacheDatabase.InitializeAsync` zieht fehlende Spalten per `EnsureColumnAsync` (`PRAGMA table_info` + `ALTER TABLE … ADD COLUMN`) nach (`CacheDatabase.cs` Z. 35–44, 115–124).
- Der API-Client liefert die Playlist-Einträge in Server-Reihenfolge paginiert (`GetPlaylistEntriesPagedAsync` → `api/playlists/{id}/entries/paged`); `PlaylistEntry` trägt `Id` (Eintrags-Id = `PlaylistEntryId` der Weiterschauen), `MediaType`, `MediaId`, `MediaTitle`, `ParentMediaType`/`ParentMediaId`/`ParentMediaTitle` (Serien-/Staffelkontext-Hints), `IsAccessible`, `SortOrder`, `EpisodeNumber`. `PlaylistService.LoadEntriesAsync` mappt auf `PlaylistEntryItem` (bereits normalisierte Medientypen).
- Manuelle Einzel-Downloads (`DownloadRequestedMessage` → `EnqueueDownloadEffect` → `EnqueueAsync`) sind nutzergesteuerte Einzelvorgänge — Reihenfolge dort nicht fachlich relevant; Serienkontext wird bereits mitgegeben.

Test-Ausgangszustand: Baseline-Lauf `VideoWebPlayer.Maui.Tests` vollständig grün (1278 erfolgreich, 0 fehlgeschlagen, 0 übersprungen). E2E-Suite nicht in der Baseline ausgeführt — Nachweis und Begründung in [tests.md](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Tests](inventory/tests.md)
