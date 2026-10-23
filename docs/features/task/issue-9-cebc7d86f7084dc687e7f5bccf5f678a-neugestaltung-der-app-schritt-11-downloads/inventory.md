# Bestandsaufnahme: Download-Verwaltung und automatische Downloads (Entwicklungsschritt 11)

Analysiert wurde der Bestand der Neu-App (`VideoPlayer.Maui`/`VideoPlayer.Mvu`/
`VideoPlayer.Storage`/`VideoPlayer.ApiClient`) sowie der fachlich
referenzierende Altbestand (`VideoWebPlayer.Maui`: `DownloadQueue`,
`DownloadManager`, `WatchlistDownloadCoordinatorService`, `DownloadedVideo`)
bezogen auf die Anforderung „Download-Verwaltung und automatische Downloads".

## Zusammenfassung

**Vorhanden und direkt nutzbar:**

- `download_manifest`-Tabelle (`DownloadManifestEntry`, `CacheDatabase`,
  nicht backup-pflichtig) mit `VideoId`, `VideoType`, `FilePath`,
  `SizeBytes`, `State`, `UpdatedAtUtc` — wird seit Schritt 10 bereits vom
  `PlaybackSourceService` gelesen (`State == "completed"` + vorhandene
  Datei → lokale Wiedergabe). Die Abspielung heruntergeladener Videos ist
  damit bereits verdrahtet; es fehlt nur die Befüllung des Manifests.
- `AppStorageLayout.VideosDirectory` — nicht backup-pflichtiges
  Offline-Medien-Verzeichnis (wird von `EnsureDirectoriesExist` angelegt).
- Alle benötigten Einstellungen existieren bereits in `AppSettings` und
  auf der Einstellungsseite: `AutoDownloadEnabled`,
  `PreloadEpisodeCount` (1–10), `WifiOnlyDownloads`, `DownloadQuotaGb`
  (1–500, Default 5 GB), `AutoDeleteWatchedEpisodes`.
- `INetworkStatusMonitor` (`ConnectivityNetworkStatusMonitor`) —
  Online-Erkennung; WLAN-Erkennung (`ConnectionProfile.WiFi`) fehlt noch
  in der Abstraktion.
- Serienkontext-Mechanik: `NextEpisodeQueue` in `AppUpdate` (nächste
  Episoden aus `MediaDetailsView.Seasons`) sowie `MediaDetailsService`
  (Episode → Serie über `Season.Show`/`GetTvShowAsync`).
- „Angesehen"-Ereignis: `PlayerState.IsWatched` wird in
  `UpdatePlaybackProgress`/`UpdatePlaybackEnded` gesetzt — Ankoppelpunkt
  der Auto-Löschung.
- MVU-Grundgerüst: `AppModel`/`AppMessage`/`AppEffect`/`AppUpdate`,
  `MvuHost`, `IAppEffectHandler`/`ShellAppEffectHandler`,
  Seiten-`Opened`-Nachrichtenmuster (z. B. `FavoritesPageOpenedMessage`).
- `DownloadsPage` existiert als Platzhalter (Route `downloads`,
  Flyout-Eintrag, DI-Registrierung).

**Fehlend (zu schaffen):**

- Download-Dienst (Einreihen, parallele Ausführung, Fortschritt,
  Abbrechen, Löschen, Fehlerbehandlung, Manifest-Pflege,
  Quota-Durchsetzung, Wiederaufnahme nach App-Neustart).
- Auto-Download-Ermittlung (nächste Episoden der Weiterschauen-Einträge,
  Anzahl- und WLAN-only-Einstellungen).
- Auto-Löschung gesehener Episoden (Kopplung an `IsWatched`).
- WLAN-Erkennung in `INetworkStatusMonitor`.
- Downloads-Seite mit Liste (Status/Fortschritt) und Aktionen.
- „Herunterladen"-Auslöser (Detailseite).
- Manifest-Spalten für Anzeige/Fortschritt (`Title`, `DownloadedBytes`,
  `TotalBytes`, `Error`).

**Altbestand-Befund (fachliche Referenz, nicht Code-Basis):**

- `DownloadQueue` arbeitet **sequenziell** (kein Parallelismus), lädt per
  `HttpClient.GetAsync` + `ResponseHeadersRead` + Stream-Kopie mit
  Fortschritts-Events; **kein Range-Request** — ein Abbruch löscht die
  Teildatei; keine Wiederaufnahme.
- `DownloadManager` baut die Stream-URL
  (`api/items/{typ}/{id}/stream?access_token=…`), dedupliziert gegen
  Queue + DB, lädt Metadaten, persistiert nach Abschluss.
- `WatchlistDownloadCoordinatorService` lädt bei
  `ContinueWatchingUpdated`-Ereignis und beim Start die
  Weiterschauen-Einträge nach (Dedup gegen Queue und Bestand).
- Fazit: Der Server-Stream ist ein einfacher GET-Download; ein
  Range-Resume ist im Altbestand nicht implementiert und serverseitig
  nicht nachgewiesen → **Resume = Neustart des Downloads mit erneuter
  Auftragsaufnahme** (dokumentierte Einschränkung).

Test-Ausgangszustand: **510 Tests bestanden, 0 Fehler, 0 übersprungen**
(Nachweis: [inventory/tests.md](inventory/tests.md),
`inventory/test-results/baseline-dotnet-test.log`).

## Details

- [Datenmodell und Ablage](inventory/models.md)
- [Logik und Dienste](inventory/logic.md)
- [Altbestand-Referenz Downloads](inventory/legacy-downloads.md)
- [UI und Navigation](inventory/ui-navigation.md)
- [Tests](inventory/tests.md)
