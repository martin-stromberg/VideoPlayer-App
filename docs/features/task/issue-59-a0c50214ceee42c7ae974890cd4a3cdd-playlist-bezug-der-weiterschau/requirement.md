# Übersetzte Anforderung: Playlist-Bezug in der Fortschrittsmeldung (Weiterschauen)

## Fachliche Zusammenfassung

Die Fortschrittsmeldung `POST api/continue-watching/progress` verliert den Playlist-Kontext der laufenden Wiedergabe: `PlaybackProgressReport` trägt nur `MediaType`, `MediaId`, `PositionSeconds` und `DurationSeconds`, sodass der Server für einen playlist-gebundenen Weiterschauen-Eintrag einen zweiten, kontextlosen Eintrag anlegt. Läuft die Wiedergabe im Playlist-Kontext (`PlayerState.IsPlaylistPlayback`), muss die Meldung `PlaylistId` und `PlaylistEntryId` des aktuell gespielten Eintrags mitführen, damit der bestehende playlist-gebundene Weiterschauen-Eintrag fortgeschrieben wird. Der Bezug ist bereits im Wiedergabekontext vorhanden (`PlayerState.PlaylistId`/`PlaylistEntryId`, belegt über `PlaylistPlaybackStartedMessage` bzw. `PlaylistNavigationCompletedMessage`) und muss entlang der gesamten Meldungsstrecke — inklusive der persistenten Offline-Warteschlange `pending_progress` — durchgereicht werden. Wiedergabe ohne Playlist-Kontext bleibt unverändert (Felder leer/`null`). Es ist keine neue Auswahl- oder Benutzerinteraktion erforderlich: der Datensatz (Playlist-Eintrag) ist über den Wiedergabekontext bereits eindeutig identifiziert.

## Betroffene Klassen und Komponenten

- **Datenmodell (`VideoPlayer.ApiClient`):**
  - `Models/ContinueWatching.cs` → `PlaybackProgressReport`: neue optionale Eigenschaften `PlaylistId` (`long?`) und `PlaylistEntryId` (`long?`).
  - `VideoPlayerApiClient.ReportPlaybackProgressAsync`: Methodensignatur um die Playlist-Parameter erweitern (optionale Parameter oder Überladung) und die Felder in den Report-Body übernehmen.
- **MVU (`VideoPlayer.Mvu`):**
  - `AppEffect.cs` → `ReportPlaybackProgressEffect`: zusätzliche Felder `PlaylistId`/`PlaylistEntryId` (nullable).
  - `AppUpdate.cs`: alle drei Erzeugungsstellen des Effekts mit dem Kontext des `PlayerState` befüllen — Fortschrittstakt `UpdatePlaybackProgress`, Wiedergabeende `UpdatePlaybackEnded` und Abschlussmeldung beim Schließen `UpdatePlayerClosed`.
  - `ProgressReportService.ReportProgressAsync`: Playlist-Parameter entgegennehmen und sowohl beim Online-Direktversand (`client.ReportPlaybackProgressAsync`) als auch beim Einreihen (`PendingProgressSyncService.EnqueueAsync`) weiterreichen.
  - `ServerSyncService.FlushPendingProgressAsync`: die in der Warteschlange gespeicherten Playlist-Felder beim Nachsenden wieder mitgeben.
- **Persistenz (`VideoPlayer.Storage`):**
  - `Entities/PendingProgressSyncEntry` (Tabelle `pending_progress`, backup-pflichtig): nullable Spalten `PlaylistId`/`PlaylistEntryId`; Migration in `BackupDatabase.InitializeAsync` über das bestehende `EnsureColumnAsync`-Muster (wie `DurationTicks`).
  - `PendingProgressSyncService.EnqueueAsync`: Signatur um die Playlist-Werte erweitern und beim Insert speichern.
- **App-Rand (`VideoPlayer.Maui`):**
  - `Services/ShellAppEffectHandler.ReportPlaybackProgressAsync`: die neuen Felder des Effekts an `ProgressReportService.ReportProgressAsync` weitergeben.
  - Keine UI-Änderung: `PlayerPage.xaml.cs` meldet im 5-s-Takt weiterhin nur Position/Dauer (`PlaybackProgressMessage`); der Playlist-Bezug steht ausschließlich im `PlayerState`.
- **Tests (`VideoWebPlayer.Maui.Tests`):**
  - `VideoPlayerApiClientTests_Endpoints` bzw. neue Contract-Tests: Request-Body von `POST api/continue-watching/progress` enthält die Playlist-Felder.
  - `AppUpdateTests_Player`/`AppUpdateTests_PlaylistPlayback`: `ReportPlaybackProgressEffect` trägt `PlaylistId`/`PlaylistEntryId` in allen drei Meldungspfaden (Takt, Ende, Schließen) — kontextlos bleibt `null`.
  - `ProgressReportServiceTests`: Direktversand und Einreihen mit Playlist-Kontext; `ServerSyncServiceTests`: Flush gibt den gespeicherten Kontext weiter; Storage-Tests für die neuen Spalten.
- **E2E-Infrastruktur (`VideoPlayer.E2E.Tests`):**
  - `MockVideoServer`: die Progress-Senke (`POST /api/continue-watching/progress`, 204) nimmt die Felder derzeit ungeprüft entgegen — für einen belastbaren Regressionstest müsste der Body erfasst/auswertbar gemacht werden (Annahme: im Rahmen sinnvoll).
- **Außerhalb des Issue-Fokus (Annahme):**
  - `VideoWebPlayer.Client/VideoWebPlayerClient.ReportPlaybackProgressAsync` und dessen Verwendung im Altbestand (z. B. `VideoWebPlayer.Maui/Services/DownloadManager.cs`) weisen dieselbe Lücke auf — die Anforderung betrifft ausdrücklich `VideoPlayer.Maui`.

## Implementierungsansatz

- **Reine Feld-/Parametererweiterung entlang der bestehenden Strecke** — kein neuer Erweiterungspunkt: `PlayerState` (`PlaylistId`/`PlaylistEntryId`) → `ReportPlaybackProgressEffect` → `ShellAppEffectHandler` → `ProgressReportService.ReportProgressAsync` → `VideoPlayerApiClient.ReportPlaybackProgressAsync` → `PlaybackProgressReport` (JSON-Body).
- **Befüllung nur im Playlist-Kontext:** Die drei Erzeugungsstellen in `AppUpdate` setzen die Felder nur, wenn `player.IsPlaylistPlayback` gilt (`PlaylistId > 0` und `PlaylistEntryId > 0`); bei Serien-/Einzelwiedergabe bleibt der Body wie bisher.
- **Snapshot-Semantik beachten:** Die Abschlussmeldung des vorherigen Eintrags wird beim Eintragswechsel (Auto-Fortsetzung `AdvancePlaylistEntry`, Navigation `PlaylistNavigationCompletedMessage`, Fehler-Rückfall) vor bzw. zusammen mit dem Wechsel erzeugt — der Effekt muss die zum Meldezeitpunkt gültige `PlaylistEntryId` des `PlayerState` tragen, nicht die des Folgeeintrags.
- **Offline-/Fehlerpfad verlustfrei:** Da nicht sendbare Meldungen in `pending_progress` eingereiht und später über `ServerSyncService.FlushPendingProgressAsync` nachgesendet werden, müssen die Playlist-Felder ebenfalls persistiert werden — sonst verliert der Nachsendepfad den Kontext erneut. Die Warteschlange dedupliziert pro Medium (`VideoType`/`VideoId`): bei einem Kontextwechsel desselben Mediums gewinnt der neueste Stand (bestehende Semantik „nur der letzte Stand zählt").
- **Wiedereinstieg unverändert:** Der umgekehrte Pfad (playlist-gebundener `ContinueWatchingEntry` → `HomeMediaItem.Playlist*` → Auswahl-Guard in `UpdateContinueWatchingItemSelected` → `StartPlaylistPlaybackEffect`) ist seit dem Playlist-Feature (Issue #43, Schritt 5) vorhanden und bleibt unangetastet.
- **API-Vertrag:** Annahme, dass der Server `PlaylistId`/`PlaylistEntryId` als optionale Felder im Progress-Body akzeptiert (Serialisierungskonvention wie die bisherigen Felder des `PlaybackProgressReport`); andernfalls ist eine Server-Erweiterung erforderlich — siehe Offene Fragen.

## Konfiguration

Keine — das Verhalten ist nicht konfigurierbar.

## Offene Fragen

1. **Server-Vertrag (im Issue explizit offen):** Akzeptiert `POST api/continue-watching/progress` serverseitig Playlist-Felder? Falls nein, ist eine Server-Erweiterung nötig; die clientseitige Erweiterung muss dann abwärtskompatibel bleiben bzw. vom Server-Release abhängen.
2. **Feldnamen/-typen:** Annahme `PlaylistId`/`PlaylistEntryId` als `long?` — ohne Playlist-Kontext `null` bzw. weggelassen. Entspricht das der Server-Erwartung (JSON-Namenskonvention wie die bestehenden Felder)?
3. **Genügt die Id-Paarung?** Annahme: `PlaylistId` + `PlaylistEntryId` identifizieren den Eintrag serverseitig eindeutig; `PlaylistName` wird nicht im Report benötigt (liefert der Server in `ContinueWatchingEntry` selbst).
4. **Kontextwechsel in der Warteschlange:** Spielt derselbe Medieneintrag abwechselnd playlist-gebunden und kontextlos, lässt die `pending_progress`-Deduplikation pro Medium den zuletzt gemeldeten Kontext gewinnen — ist das fachlich akzeptabel?
5. **Altbestand:** Soll die Korrektur auch im Altbestand (`VideoWebPlayer.Maui`/`VideoWebPlayerClient`) nachgezogen werden? Annahme: nein — Fokus der Anforderung ist `VideoPlayer.Maui`.
