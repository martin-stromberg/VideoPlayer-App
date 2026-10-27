# Bestandsaufnahme: Downloadbereinigung — Ablaufdatum, Herkunft und automatische Bereinigung von Downloads

Analysiert wurde der Download-Bereich der MAUI-App (Manifest, Download-/Auto-Download-Dienste, Wiedergabequelle, MVU-Effekte und -Nachrichten, Downloads-/Einstellungsseiten, Cache-Datenbank) bezogen auf die Anforderung `requirement.md` (Ablaufdatum `ExpiresAtUtc`, Herkunft `manual`/`auto`, Bereinigungslauf, „Nach dem Abspielen löschen", Wiedergabe-Schutz).

## Zusammenfassung

- **Vorhanden:** Ein vollständiges Download-Subsystem: `download_manifest`-Tabelle mit `DownloadManifestEntry` (Zustand, Fortschritt, Serien- und Playlist-Kontext, Vorschaubild), `DownloadService` (Einreihen/Dedup, Abbrechen, Löschen, Wiederaufnahme, `ReconcileAsync`-Abgleich), `AutoDownloadService` (Weiterschauen-Vorlauf mit `PreloadEpisodeCount`, Serien- und Playlist-Pfad), `PlaybackSourceService` (lokale Datei vor Stream), MVU-Effekte/-Nachrichten für alle Download-Aktionen, Downloads-Seite mit Eintrags-Aktionen, Einstellungsseite mit Download-Abschnitt, Migrationsschema über `CacheDatabase.EnsureColumnAsync`.
- **Vorhanden und direkt relevant:** `ReconcileAsync` läuft bereits an zwei Stellen — beim Laden der Downloads-Seite (`ShellAppEffectHandler.LoadDownloadsAsync`) und vor der Wiederaufnahme (`ResumeDownloadsAsync`, getriggert bei `EnterConnected` und Online-Rückkehr). Es entfernt überholte Teildateien, zählt Restdaten und korrigiert „fertig"-Einträge ohne Datei — **ohne jede Ablauf-Prüfung**.
- **Vorhanden und direkt relevant:** Die Sofortlöschung gesehener Episoden existiert als `AppSettings.AutoDeleteWatchedEpisodes` + `DeleteWatchedDownloadEffect` → `DownloadService.DeleteIfCompletedAsync`, ausgelöst an der „angesehen"-Schwelle (`EvaluateEndWindow`, `WatchedEndWindowSeconds` + `EndWindowStreak`) und am Wiedergabeende (`UpdatePlaybackEnded`) — nur für `tvshowepisode`.
- **Fehlt:** `ExpiresAtUtc` und eine Herkunfts-Spalte (`manual`/`auto`) am `DownloadManifestEntry`; es gibt kein Datumsfeld außer `UpdatedAtUtc` und keinen Herkunftsbegriff im gesamten Download-Pfad (Enum, Spalte, Anzeige).
- **Fehlt:** Die drei neuen Einstellungen (Aufbewahrungszeit in Tagen, „Downloads nach dem Abspielen automatisch löschen", Löschverzögerung in Minuten) — `AppSettings` kennt nur `AutoDeleteWatchedEpisodes` (Episoden-Sofortlöschung), keine zeitbasierte Löschung.
- **Fehlt:** Eine offline-fähige Kandidaten-Ermittlung für den Bereinigungslauf. `AutoDownloadService.RunAsync` liest zwar die gecachte Weiterschauen-Liste, löst Folge-Episoden/Nachfolger aber online über `GetTvShowAsync` bzw. `GetPlaylistEntriesPagedAsync` auf; eine wiederverwendbare „Ist Medium X Auto-Download-Kandidat?"-Prüfung existiert nicht. Offline-Bausteine sind vorhanden: `ShowDetailsCache` (gecachte Serienstruktur in `cached_lists`), Playlist-Bezug im Manifest (`PlaylistId`/`PlaylistEntryId`/`PlaylistSortOrder`), `HomeListService` (gecachete Weiterschauen-Liste). Eine gecachte Playlist-Struktur existiert nicht — Playlist-Einträge werden nur online geladen.
- **Fehlt:** Wiedergabe-Schutz beim Abspielen (`UpdatePlaybackSourceResolved` kennt die aufgelöste Quelle `PlaybackSourceKind.LocalFile`, prüft aber nichts Weiteres); Verlängerungs-Methode am `DownloadService`; Herkunftsanzeige „Cache" in `DownloadListItem`/`DownloadItemViewModel`/`DownloadsPage.xaml`.
- **Altbestand:** Die Spaltenmigration läuft über `CacheDatabase.InitializeAsync`/`EnsureColumnAsync` (`ALTER TABLE … ADD COLUMN`) — etabliertes Muster für `ExpiresAtUtc` und Herkunft.

## Details

- [Datenmodell](inventory/models.md) — `DownloadManifestEntry`, `AppSettings`, `DownloadListItem`, `DownloadsPageState`, `DownloadReconcileResult`, Kontext-/Cache-Records, API-DTOs
- [Logik](inventory/logic.md) — `DownloadService`, `AutoDownloadService`, `PlaybackSourceService`, `AppUpdate`-Handler, `ShellAppEffectHandler`, `HomeListService`, `OfflineContinueWatchingService`, `ShowDetailsCache`, `CacheDatabase`, Seiten
- [MVU-Nachrichten und -Effekte](inventory/messages-effects.md) — Download- und Wiedergabe-bezogene `AppMessage`/`AppEffect`-Records
- [Enums](inventory/enums.md) — `DownloadState` (+`DownloadStateNames`), `PlaybackSourceKind`
- [Interfaces](inventory/interfaces.md) — `IDownloadBackend`, `INetworkStatusMonitor`, `IAppEffectHandler`, `ISettingsStore`
- [Tests](inventory/tests.md) — Bestehende Testklassen und Hilfsmethoden des Download-Bereichs
