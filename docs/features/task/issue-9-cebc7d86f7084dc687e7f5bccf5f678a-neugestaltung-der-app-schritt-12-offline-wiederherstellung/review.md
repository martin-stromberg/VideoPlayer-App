# Plan-Review

Geprüft: `plan.md` gegen die Implementierung (Selbstprüfung ohne
Unteragenten — dokumentierte Abweichung, es stehen keine Unteragenten
zur Verfügung). Alle Planelemente wurden im Code gesucht und
vollständig gelesen.

## Ergebnis

**Status:** Vollständig umgesetzt — ein Befund bei der Prüfung
behoben (siehe `review-code.md`: `KeyFor`-Aufruf im sqlite-net-
Prädikat).

## Umgesetzte Planelemente

### A. Serienkontext im Download-Manifest

- [x] `VideoPlayer.Mvu/DownloadSeriesContext.cs` — `sealed record`
  mit `ShowId`, `ShowTitle`, `SeasonNumber`, `EpisodeNumber`.
- [x] `DownloadManifestEntry` — vier neue Spalten (`ShowId`,
  `ShowTitle`, `SeasonNumber`, `EpisodeNumber`); Migration auf
  Bestandstabellen per `EnsureColumnAsync` in `CacheDatabase`.
- [x] `DownloadService.EnqueueAsync` — optionaler `seriesContext`;
  wird beim Insert gesetzt und bei Re-Enqueue vorhandener Einträge
  aufgefüllt.
- [x] `DownloadRequestedMessage`/`EnqueueDownloadEffect` — init-only
  Kontext-Properties; `AppUpdate` kopiert sie auf den Effekt.
- [x] `MediaDetailsPage.xaml.cs` — übergibt den Kontext bei
  Episoden-Downloads.
- [x] `AutoDownloadService` — übergibt den Kontext und schreibt
  `ShowDetailsCache` nach dem Serienabruf; `CacheDatabase` als neue
  Abhängigkeit.

### B. Gecachte Serienstruktur

- [x] `VideoPlayer.Mvu/ShowDetailsCache.cs` — `CachedShowEntry`,
  `CachedShowSeason`, `CachedShowEpisode`; `WriteAsync`/`ReadAsync`
  auf `cached_lists`-Schlüssel `show:{id}`; tolerantes Lesen;
  `OrderedEpisodes`, `NextEpisodeAfter`, `NextEpisodeAfterId`.
- [x] `MediaDetailsService` — schreibt die Struktur nach
  `GetTvShowAsync`.

### C. Offline-„Weiterschauen" + Platzhalter

- [x] `OfflineContinueWatchingService` — nur `completed`-Manifest-
  Einträge mit vorhandener Datei; Platzhalter fehlender Folge-
  Episoden (Dedupe nach Episoden-Id); Fortschritt aus
  `pending_progress` mit Fallback auf den `continue_watching`-
  Cache; `Favorites`/`Recent` über `HomeListService`; injizierbare
  `fileExists`-Prüfung.
- [x] `HomeMediaItem` — `IsPlaceholder`, `IsSelectable`,
  `CardOpacity`; `HomeLists.DiscardedCaches`.
- [x] `ShellAppEffectHandler.LoadHomeListsAsync` — offline über den
  neuen Dienst, online bisheriger Pfad.
- [x] `AppUpdate.UpdateMediaItemSelected` — Platzhalter navigieren
  nicht (`Unchanged`).
- [x] `MediaCarousel.xaml` — „Nicht heruntergeladen"-Badge und
  abgedunkelte Karten.
- [x] `AppUpdate.UpdateHomeListsLoaded` — `DiscardedCaches` bei
  Verbindung → `RunFullSyncEffect`.

### D. Offline-Wiedergabeweg

- [x] `MediaDetailsService.LoadOfflineAsync` — Film/Episode/Serie aus
  Manifest + `ShowDetailsCache` + `cached_favorites`;
  `IsOfflineSnapshot`, `HasLocalFile`, Episoden-Platzhalter;
  Fehlertexte für nicht ablegbare Typen.
- [x] `MediaDetailsView` — `HasLocalFile`, `IsOfflineSnapshot`,
  `EpisodeItem.IsPlaceholder`, `ShowUnavailable`, `DisplayTitle`.
- [x] `MediaDetailsPage` — Abspielen nur mit lokaler Datei,
  Download/Favorit offline unterbunden, Episoden-Platzhalter
  abgedunkelt mit Hinweis.
- [x] `PlaybackSourceService` — lokale Datei vor Server-Stream;
  `allowServerStream: false` im Offline-Modus ohne jeglichen
  HTTP-Zugriff.

### E. Wiederherstellung nach Cloud-Backup

- [x] `AuthSessionService.TryStoredLoginAsync` — gespeicherter Token
  wird validiert und wiederverwendet; 401/403 → Token verwerfen +
  Credential-Pfad; Transport-/sonstige Fehler → `ServerUnreachable`
  (kein erzwungenes Re-Login bei transienten Fehlern).
- [x] `MissingCredentials` → `NeedsLogin` mit verständlichem
  Wiederanmelde-Hinweis (`AppUpdate.ReloginRequiredNotice`).
- [x] `EnterConnected` → `RunFullSyncEffect` (Fortschritts-Outbox,
  Metadaten, Weiterschauen, Favoriten) → `LoadHomeListsEffect` —
  bestehender Ablauf wiederverwendet.
- [x] Startpfad: bekannter Server nicht erreichbar → Offline bei
  lokalem Datenbestand, sonst Server-Setup.

### F. Beschädigte lokale Daten

- [x] `BackupDatabase`/`CacheDatabase.InitializeWithRecoveryAsync` —
  beschädigte Datei wird als `*.corrupt-{Zeitstempel}` beiseite-
  gelegt, Verbindung/Pool zurückgesetzt, Ablage neu aufgebaut;
  zweiter Fehlschlag geht an den Aufrufer.
- [x] `HomeListService` — beschädigte `cached_lists`-/
  `cached_favorites`-Zeilen werden gelöscht und über
  `DiscardedCaches` gemeldet (nur tatsächlich verworfene Schlüssel,
  keine Resync-Schleife).
- [x] `StorageRecoveredMessage` + `AppModel.StorageNotice`;
  `ShellAppEffectHandler` meldet die Wiederherstellung; Totalversagen
  läuft ebenfalls in den Hinweis statt abzustürzen.
- [x] `HomePage.xaml` — `StorageNoticeBanner`.

### G. Offline-Einschränkung Profil + Feinschliff

- [x] `UpdateProfilePageOpened` — offline `Attempted = true`, kein
  `LoadProfileInfoEffect`.
- [x] `ProfilePage` — `ProfileOfflineBanner`, `UserInfoBorder` offline
  ausgeblendet, lokale Installation + Abmelden bleiben.
- [x] Offline-Banner-Text der Startseite präzisiert.
- [x] `SourcesPageOpened` offline ohne Lade-Effekt (Quellen sind
  serverseitig).
- [x] `DownloadRequested` offline → Hinweis statt Effekt.
- [x] DI-Registrierung (`MauiProgram.cs`) — neue Dienste als
  Singletons.

### H. Tests

- [x] Alle geplanten Testfälle umgesetzt (siehe `test-results.md`);
  `StorageRecoveryTests` als Erweiterung der bestehenden
  `LocalDatabaseTests` realisiert; `MediaDetailsServiceOfflineTests`
  in `MediaDetailsServiceTests` integriert; Contract-Tests in der
  neuen `OfflineRecoveryContractTests` gebündelt.
- [x] Zusätzlich: `Enqueue_WithSeriesContext_PersistsContextInManifest`,
  `Run_EpisodeCandidate_CrossesIntoNextSeason`, Offline-Auflösung des
  `PlaybackSourceService` (kein Server-Request offline),
  stille Token-Erneuerung ohne falsches `SessionExpired`.

### I. Dokumentation

- [x] `docs/help/offline-wiederherstellung.md` + Index-Eintrag.
- [x] `README.md` — Feature-Zeile ergänzt.
- [x] `docs/RELEASE_NOTES.md` — Eintrag „Offlinebetrieb und
  Wiederherstellung".

## Abweichungen zum Plan

1. `ShowDetailsCache`-DTO-Feldname `Id` statt `SeasonId` an der
   Staffel (Einheitlichkeit mit den übrigen Records).
2. `HomeListService.LoadFavoritesAsync`/`LoadRecentAsync` liefern die
   Teillisten inklusive Verwerfungs-Information über
   `HomeLists.DiscardedCaches` — wie geplant, nur als eigenständige
   Lademethoden ergänzt.
3. Neue Effekte doch nötig: `ResolvePlaybackSourceEffect` (lokale
   Datei vs. Server-Stream vor dem Player), `CheckOfflineDataEffect`
   (Startpfad-Verzweigung) — beide folgen dem bestehenden
   Effekt-Muster.

## Offene Punkte

Keine.
