# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Ist-Zustand der Download-Einplanung bei Playlist-Bezug verifiziert und dokumentiert | Bestandsaufnahme (`inventory.md`) beantwortet die Leitfrage: `AutoDownloadService` ignoriert `PlaylistId`/`PlaylistEntryId` und reiht Serien-Folge-Episoden ein; Warteschlange FIFO über Manifest-Id; Manifest ohne Playlist-Feld | Dokumentiert in `inventory.md` (Befund), kein Code-Test nötig | Abgedeckt |
| Downloads mit Playlist-Bezug werden in Playlist-Reihenfolge eingeplant/ausgeführt | `AutoDownloadService.EnqueueNextPlaylistEntriesAsync` blättert `entries/paged` in Server-Reihenfolge ab `PlaylistEntryId` und reiht die nächsten ladbaren Einträge ein; FIFO-Warteschlange macht Einreihe- zur Ausführungsreihenfolge; Playlist-Bezug wird am `DownloadManifestEntry` persistiert (`PlaylistId`/`PlaylistEntryId`/`PlaylistSortOrder`, upgrade-sicher per `EnsureColumnAsync`) | `Run_PlaylistEpisode_EnqueuesPlaylistOrder`, `Run_PlaylistEpisode_SkipsNonDownloadableEntries`, `Run_PlaylistEpisode_PersistsPlaylistContext`, `Run_PlaylistMovie_EnqueuesSelfAndNextEntries`, `CacheDatabase_MigratesPlaylistContextOntoExistingManifest` | Abgedeckt |
| Downloads ohne Playlist-Bezug verhalten sich wie bisher | Dispatch nur bei `PlaylistId > 0 && PlaylistEntryId > 0`; sonst unveränderter Serien-/Film-Pfad; optionaler Parameter ohne Signaturbruch | Bestandstests unverändert (`Run_EpisodeCandidate_*`, `Run_MovieCandidate_*`); Fallback-Tests `Run_PlaylistEntryNotFound_FallsBackToSeries`, `Run_PlaylistEntryMissing_FallsBackToSeries` | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Kein neuer/geänderter Benutzerfluss — der Hintergrund-Vorlauf ist nicht über eine Nutzeraktion erreichbar; keine UI-Änderung; es existiert generell kein E2E-Download-Test | Keiner (Begründung im Plan) | Nicht erforderlich mit Begründung |

## Hinweise

- Die Playlist-Detailseite besitzt keinen Download-Auslöser (geprüft: kein `DownloadRequestedMessage`-Dispatch in `PlaylistDetailsPage.xaml.cs`) — manuelle Downloads bleiben Einzelvorgänge ohne Reihenfolge-Bezug; Plan-Abdeckung ausreichend.
- `SeasonNumber` ist an Playlist-Einträgen nicht verfügbar (nur `ParentMedia*`/`EpisodeNumber`); der Plan sieht bewusst partielle Serien-Hints vor (nullable Felder des `DownloadSeriesContext`) — konsistent mit der `??=`-Semantik.
- Weiterschauen-Einträge vom Typ `show`/`season`/`collection` bleiben auch mit Playlist-Bezug unbehandelt (nicht abspielbar/ladbar) — konsistent mit dem bestehenden Dispatch in `RunAsync`.
