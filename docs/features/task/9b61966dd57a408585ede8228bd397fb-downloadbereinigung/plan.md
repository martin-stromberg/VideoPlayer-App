# Umsetzungsplan: Downloadbereinigung — Ablaufdatum, Herkunft und automatische Bereinigung von Downloads

## Übersicht

Jeder `DownloadManifestEntry` erhält ein Ablaufdatum (`ExpiresAtUtc`, Fertigstellung + Aufbewahrungszeit) und eine Herkunft (`manual`/`auto`, UI „Cache"). Der bestehende `ReconcileAsync`-Abgleich wird um eine Ablauf-Prüfung erweitert: abgelaufene Einträge werden verlängert (Auto-Download-Kandidat, offline ermittelt) oder vollständig entfernt. Die bisherige Episoden-Sofortlöschung (`AutoDeleteWatchedEpisodes` + `DeleteWatchedDownloadEffect`) wird durch eine verkürzte Ablauffrist für alle Medientypen ersetzt; ergänzend schützt eine Hochsetzung des Ablaufdatums die laufende Wiedergabe. Betroffen sind `download_manifest`-Schema, `AppSettings`, `DownloadService`, `AutoDownloadService`, `AppUpdate`, `ShellAppEffectHandler`, `SettingsPage`, `DownloadsPage` sowie Tests.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Herkunftsmodell | Neuer Enum `DownloadOrigin` (`Manual`, `Auto`) + statische `DownloadOriginNames` (`ToPersisted`/`FromPersisted`, unbekannt/`null` → `Manual`), persistiert als `Origin varchar NULL` | Exaktes Muster von `DownloadState`/`DownloadStateNames`; `NULL` im Altbestand liefert automatisch `manual` (Kriterium 16) ohne Datenmigration |
| Ablauf-Prüfung | `ReconcileAsync` erhält optionale Parameter `AppSettings? settings` und `IReadOnlySet<string>? autoDownloadCandidateKeys`; die Ablauf-Prüfung läuft als zusätzlicher Pass am Ende (nur `completed`-Einträge) | Anforderung verankert den Lauf im bestehenden Abgleich; bestehende Aufrufe/Tests ohne Parameter bleiben lauffähig; Kandidatenmenge wird einmalig vom Aufrufer geliefert statt pro Eintrag neu ermittelt |
| Kandidatenmenge | `AutoDownloadService.GetAutoDownloadCandidateKeysAsync(settings, ct)` liefert `IReadOnlySet<string>` im Format `{normalisierterTyp}:{mediaId}` (Konvention von `DownloadService.Key`); leere Menge, wenn `AutoDownloadEnabled == false` | Bei deaktiviertem Auto-Download kann nichts nachgeladen werden — „weiterhin Kandidat" ist dann per Definition falsch; einmalige Ermittlung pro Lauf statt Prüfung je Eintrag |
| Offline-Playlist-Reihenfolge | Neuer `PlaylistEntriesCache` (Muster `ShowDetailsCache`): `AutoDownloadService` persistiert die seitenweise geladenen Playlist-Einträge unter `cached_lists`-Schlüssel `playlist:{id}` | Playlist-Nachfolger sind offline sonst nicht bestimmbar — `HomeMediaItem`/`ContinueWatchingEntry` tragen keine Sortierposition des aktuellen Eintrags; die Manifest-`PlaylistSortOrder` allein reicht nicht ohne Referenzposition |
| Verlängerung im Auto-Download-Lauf (Kriterium 12) | Im `AlreadyPresent`-Dedup-Zweig von `EnqueueAsync`: bei `origin == DownloadOrigin.Auto` werden `ExpiresAtUtc = utcNow() + Retention` und `Origin = auto` gesetzt (Update + `ItemChanged`) | Beide Verlängerungstrigger laufen durch einen einzigen Codepfad; kein separater Verlängerungsaufruf im `AutoDownloadService` nötig; wirkt auch auf manuelle Downloads (Herkunftswechsel gemäß Kriterium 11) |
| Angesehen-Verkürzung | Neuer `ExpireWatchedDownloadEffect(MediaType, MediaId, DelayMinutes)` ersetzt `DeleteWatchedDownloadEffect`; neuer Service-Aufruf `ExpireAfterAsync` setzt `ExpiresAtUtc = utcNow() + delay` auf `completed`-Einträge | Direkte Umsetzung von Kriterium 9; `DeleteIfCompletedAsync`/`DeleteWatchedDownloadEffect` entfallen ersatzlos (einziger Aufrufer) |
| Wiedergabe-Schutz-Trigger | Erster Fortschrittstakt mit bekannter `Duration` bei `Source.Kind == LocalFile` in `UpdatePlaybackProgress`; Einmal-Flag `PlayerState.DownloadExpiryProtected`; Effekt `ProtectDownloadExpiryEffect(MediaType, MediaId, RemainingDuration)` → `ExtendExpiryAsync(type, id, utcNow + 2 × Restdauer)` | In `UpdatePlaybackSourceResolved` ist `Duration` noch `TimeSpan.Zero` — Restdauer erst am ersten Takt bekannt; ohne Dauer entfällt die Hochsetzung (geklärte Frage der Anforderung); Flag verhindert Wiederholung je Medium, Reset bei jedem Medienwechsel |
| Einstellungs-Migration | `AutoDeleteWatchedEpisodes` entfällt im `AppSettings`-Record; `AppSettingsService.Load` liest `AutoDeletePlayedDownloadsKey`, fehlt er, fällt auf den Alt-Schlüssel `AutoDeleteWatchedEpisodesKey` zurück; `Save` schreibt nur noch den neuen Schlüssel | Etabliertes „fehlende Schlüssel = Defaults"-Muster; Nutzerentscheidung bleibt erhalten, Altbestand wird nicht zurückgeschrieben |
| Ablaufdatum-Anzeige (Kriterium 18, optional) | Wird umgesetzt: `DownloadListItem.ExpiresText` („Läuft ab am {lokales Datum, kurzes Format}") + Caption-Label in `DownloadsPage.xaml` | Geringer Zusatzaufwand bei vorhandenem Datenmodell; macht die neue Bereinigung für Anwender nachvollziehbar |
| Periodischer Timer | Keiner — Trigger bleiben App-Start/Online-Rückkehr (`ResumeDownloadsEffect`) und Öffnen der Downloads-Seite (`LoadDownloadsEffect`) | Zeitstempelbasierte Prüfung ist auch bei verzögertem Lauf korrekt; ein Timer brächte Lebenszyklus-/Plattformkomplexität ohne fachlichen Mehrwert |
| `failed`/`cancelled`-Einträge | Werden von der Ablauf-Prüfung nicht erfasst (nur `completed`) | `ExpiresAtUtc` existiert nur ab `completed`; automatisches Löschen von Fehlereinträgen würde die bewusste Anwenderaktion „Alle Fehler löschen" umgehen |

Geklärte Punkte aus der Anforderung (Standardwerte 7 Tage / 15 Minuten / aus; Restdauer = Gesamtdauer − Startposition, ohne bekannte Dauer keine Hochsetzung; Verlängerung sowohl im Bereinigungslauf als auch im Auto-Download-Lauf) sind wie angegeben übernommen bzw. oben entschieden.

## Programmabläufe

### Einreihen mit Herkunft

1. Manueller Pfad: `DownloadRequestedMessage` (Detailseite/Episodenliste) bzw. `DownloadRetryRequestedMessage` („Erneut versuchen") → `AppUpdate` → `EnqueueDownloadEffect` → `ShellAppEffectHandler.EnqueueDownloadAsync` → `DownloadService.EnqueueAsync(..., origin: DownloadOrigin.Manual)`.
2. Automatischer Pfad: `AutoDownloadService` (`EnqueueSelfAsync`, `EnqueueEpisodesFromCurrentAsync`, `EnqueuePlaylistEntriesFromCurrentAsync`) → `EnqueueAsync(..., origin: DownloadOrigin.Auto)`.
3. `EnqueueAsync`: Neuanlage → `Origin` wird persistiert; Re-Enqueue (failed/cancelled → queued) behält den gespeicherten `Origin`-Wert und setzt `ExpiresAtUtc = null` zurück; Dedup-Fall `AlreadyPresent` (completed mit Datei) mit `origin == Auto` → `ExpiresAtUtc = utcNow() + settings.DownloadRetentionDays` Tage, `Origin = auto`, persistieren + `ItemChanged` (Kriterium 12 — Herkunftswechsel `manual` → `auto` eingeschlossen).

Beteiligte Klassen/Komponenten: `DownloadService`, `AutoDownloadService`, `ShellAppEffectHandler`, `DownloadOrigin`/`DownloadOriginNames`, `DownloadManifestEntry`.

### Ablaufdatum beim Abschluss setzen

1. `TransferAsync` (Erfolgspfad): vor `SetStateIfPresentAsync(Completed)` → `entry.ExpiresAtUtc = utcNow() + TimeSpan.FromDays(settings.DownloadRetentionDays)`.
2. `CompleteExternalTransferAsync` (iOS-Hintergrund-Transfer, Erfolgspfad): identisch — die Methode erhält dafür einen neuen Parameter `AppSettings settings`; der `CompletionSink` in `MauiProgram` lädt die Einstellungen über `AppSettingsService`.
3. Wartende/laufende/fehlgeschlagene Einträge erhalten kein Ablaufdatum (Kriterium 4).

Beteiligte Klassen/Komponenten: `DownloadService`, `IosBackgroundDownloadBackend` (CompletionSink), `AppSettingsService`.

### Bereinigungslauf mit Ablauf-Prüfung

1. `ShellAppEffectHandler.LoadDownloadsAsync` (Downloads-Seite) bzw. `ResumeDownloadsAsync` (App-Start/Online-Rückkehr): zuerst `autoDownloads.GetAutoDownloadCandidateKeysAsync(settings, ct)` — offline: Weiterschauen-Liste aus `HomeListService`-Cache → je Eintrag Selbst-Key; Episoden mit `ShowId` → `ShowDetailsCache.ReadAsync` + `OrderedEpisodes` → ab dem aktuellen Eintrag bis `PreloadEpisodeCount` Keys; playlist-gebundene Einträge → `PlaylistEntriesCache.ReadAsync` → ab dem aktuellen `PlaylistEntryId` bis `PreloadEpisodeCount` ladbare Einträge.
2. `downloads.ReconcileAsync(settings, keys, ct)` führt zunächst die bisherigen Schritte aus (überholte Teildateien, Restdaten, `FilePath`-Heilung, `completed`-ohne-Datei → `failed`/`missing_file`).
3. Neuer Ablauf-Pass (nur wenn `settings != null`): je Eintrag mit `State == completed` und (`ExpiresAtUtc is null` oder `<= utcNow()` — `null` deckt den Altbestand, Kriterium 16):
   - Kandidaten-Key `{VideoType}:{VideoId}` in `autoDownloadCandidateKeys` → `ExpiresAtUtc = utcNow() + Retention`, `Origin = auto`, `UpdateAsync`, `ItemChanged`, Zähler `ExtendedExpiryCount`.
   - Sonst → `TryDeleteFile` (aufgelöster Pfad), `DeleteAsync` der Zeile, `ItemRemoved`, Zähler `RemovedExpiredCount`.
4. Ergebnis in `DownloadReconcileResult` erweitert; Protokollzeile des Abgleichs ergänzt.

Beteiligte Klassen/Komponenten: `DownloadService` (`ReconcileAsync`), `AutoDownloadService` (`GetAutoDownloadCandidateKeysAsync`), `ShowDetailsCache`, `PlaylistEntriesCache`, `HomeListService`, `ShellAppEffectHandler`.

### „Angesehen"-Verkürzung („Nach dem Abspielen löschen")

1. `AppUpdate.EvaluateEndWindow` (bestätigte Endfenster-Streak → `IsWatched`) und `UpdatePlaybackEnded` (plausibles Wiedergabeende): bei `model.Settings.AutoDeletePlayedDownloads` — ohne Medientyp-Einschränkung — `ExpireWatchedDownloadEffect(MediaType, MediaId, model.Settings.DeleteAfterPlaybackDelayMinutes)` in die Effektliste.
2. `ShellAppEffectHandler` → `downloads.ExpireAfterAsync(type, id, TimeSpan.FromMinutes(DelayMinutes), ct)`: nur bei `completed`-Eintrag → `ExpiresAtUtc = utcNow() + delay`, persistieren, `ItemChanged`.
3. Die eigentliche Entfernung erfolgt im nächsten Bereinigungslauf (Ablauf-Prüfung oben).

Beteiligte Klassen/Komponenten: `AppUpdate`, `AppEffect` (`ExpireWatchedDownloadEffect`), `ShellAppEffectHandler`, `DownloadService` (`ExpireAfterAsync`).

### Wiedergabe-Schutz

1. `AppUpdate.UpdatePlaybackProgress`: wenn `!player.DownloadExpiryProtected && player.Source?.Kind == PlaybackSourceKind.LocalFile && message.Duration > TimeSpan.Zero` → `DownloadExpiryProtected = true` setzen; ist `remaining = message.Duration − message.Position > 0`, `ProtectDownloadExpiryEffect(MediaType, MediaId, remaining)` emittieren.
2. `ShellAppEffectHandler` → `downloads.ExtendExpiryAsync(type, id, DateTime.UtcNow + 2 × remaining, ct)`: nur bei `completed`-Eintrag; `ExpiresAtUtc` wird nur hochgesetzt (`null` oder kleiner als Ziel → Ziel).
3. Flag-Reset bei jedem Medienwechsel: `StartPlayback` (neues `PlayerState`), `AdvanceToNextEpisode`, Playlist-Übergänge (`AdvancePlaylistEntry`, `UpdatePlaylistNavigated` o. ä. — alle `with`-Blöcke, die `Source = null`/`Duration = TimeSpan.Zero` setzen).

Beteiligte Klassen/Komponenten: `AppUpdate`, `PlayerState`, `PlaybackSource`/`PlaybackSourceKind`, `AppEffect` (`ProtectDownloadExpiryEffect`), `ShellAppEffectHandler`, `DownloadService` (`ExtendExpiryAsync`).

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `DownloadOrigin` | Enum (`VideoPlayer.Mvu/DownloadOrigin.cs`) | Herkunft eines Downloads: `Manual`, `Auto` |
| `DownloadOriginNames` | statische Klasse (in `DownloadOrigin.cs`) | `ToPersisted`/`FromPersisted` — `manual`/`auto`; `null`/unbekannt → `Manual` (Altbestand) |
| `CachedPlaylistEntry` | Datenmodellklasse (`VideoPlayer.Mvu/PlaylistEntriesCache.cs`) | Gecachter Playlist-Eintrag: `EntryId`, `MediaType`, `MediaId`, `SortOrder`, `IsAccessible` |
| `CachedPlaylistEntries` | Datenmodellklasse (in `PlaylistEntriesCache.cs`) | Gecachte Playlist-Struktur: `PlaylistId`, geordnete `Entries` |
| `PlaylistEntriesCache` | statische Klasse | `KeyFor(playlistId)` (`playlist:{id}`), `WriteAsync(cache, playlistId, entries)`, `ReadAsync(cache, playlistId)` — Offline-Grundlage der Playlist-Nachfolger-Prüfung (Muster `ShowDetailsCache`) |
| `ExpireWatchedDownloadEffect` | `AppEffect`-Record (`string MediaType, long MediaId, int DelayMinutes`) | Ablaufdatum eines „angesehen"-Mediums auf `jetzt + Delay` setzen (Träger der Option „Nach dem Abspielen löschen") |
| `ProtectDownloadExpiryEffect` | `AppEffect`-Record (`string MediaType, long MediaId, TimeSpan RemainingDuration`) | Wiedergabe-Schutz: Ablaufdatum auf `jetzt + 2 × RemainingDuration` hochsetzen |

## Änderungen an bestehenden Klassen

### `DownloadManifestEntry` (Datenmodellklasse, `VideoPlayer.Storage/Entities/DownloadManifestEntry.cs`)

- **Neue Eigenschaften:** `ExpiresAtUtc` (`DateTime?`) — Ablaufdatum des abgeschlossenen Downloads; `null` = nicht gesetzt/Altbestand. `Origin` (`string?`) — persistierte Herkunft (`manual`/`auto`); `null` = Altbestand = `manual`.

### `CacheDatabase` (Klasse, `VideoPlayer.Storage/CacheDatabase.cs`)

- **Geänderte Methoden:** `InitializeAsync` — zwei zusätzliche `EnsureColumnAsync`-Aufrufe: `("download_manifest", "ExpiresAtUtc", "bigint NULL")` und `("download_manifest", "Origin", "varchar NULL")`.

### `AppSettings` (Datenmodellklasse, `VideoPlayer.Storage/AppSettings.cs`)

- **Neue Eigenschaften:** `DownloadRetentionDays` (`int`, Standard 7, Konstante `DefaultDownloadRetentionDays`) — Aufbewahrungszeit; `AutoDeletePlayedDownloads` (`bool`, Standard `false`) — „Downloads nach dem Abspielen automatisch löschen"; `DeleteAfterPlaybackDelayMinutes` (`int`, Standard 15, Konstante `DefaultDeleteAfterPlaybackDelayMinutes`) — Löschverzögerung.
- **Entfernte Eigenschaften:** `AutoDeleteWatchedEpisodes` — wird durch `AutoDeletePlayedDownloads` ersetzt.
- **Geänderte Methoden:** `Normalize()` — zusätzlich `DownloadRetentionDays = Math.Clamp(…, 1, 365)` und `DeleteAfterPlaybackDelayMinutes = Math.Clamp(…, 0, 1440)` (0 = Ablauf sofort → Entfernung beim nächsten Lauf, Verhalten der früheren Sofortlöschung).

### `AppSettingsService` (Klasse, `VideoPlayer.Storage/AppSettingsService.cs`)

- **Neue Konstanten:** `DownloadRetentionDaysKey`, `AutoDeletePlayedDownloadsKey`, `DeleteAfterPlaybackDelayMinutesKey`.
- **Geänderte Methoden:** `Load()` — drei neue Schlüssel lesen; `AutoDeletePlayedDownloads` mit Migration: neuer Schlüssel vorhanden → dessen Wert, sonst Fallback auf `AutoDeleteWatchedEpisodesKey`. `Save()` — nur die neuen Schlüssel schreiben (der Alt-Schlüssel bleibt als Lese-Fallback-Konstante, wird nicht mehr geschrieben).

### `DownloadService` (Klasse, `VideoPlayer.Mvu/DownloadService.cs`)

- **Geänderter Konstruktor (internal):** zusätzlicher optionaler Parameter `Func<DateTime>? utcNow` — Zeitquelle für Ablauflogik (Testbarkeit; Muster `AutoDownloadService`).
- **Geänderte Methoden:**
  - `EnqueueAsync` — neuer Parameter `DownloadOrigin origin = DownloadOrigin.Manual`; Neuanlage setzt `Origin`; Re-Enqueue behält den Wert und setzt `ExpiresAtUtc = null`; `AlreadyPresent`-Dedup bei `origin == Auto`: Verlängerung + Herkunftswechsel (`ExpiresAtUtc = utcNow() + Retention`, `Origin = auto`, Update + `ItemChanged`).
  - `TransferAsync` — Erfolgspfad setzt `ExpiresAtUtc` vor dem `Completed`-Übergang.
  - `CompleteExternalTransferAsync` — neuer Parameter `AppSettings settings`; Erfolgspfad setzt `ExpiresAtUtc` identisch.
  - `ReconcileAsync` — neue optionale Parameter `AppSettings? settings = null, IReadOnlySet<string>? autoDownloadCandidateKeys = null`; neuer Ablauf-Pass (siehe Programmablauf) nur bei vorhandenem `settings`.
  - `ToItem` — mappt `Origin` (über `DownloadOriginNames.FromPersisted`) und `ExpiresAtUtc` in den Snapshot.
- **Neue Methoden:**
  - `ExpireAfterAsync(string mediaType, long mediaId, TimeSpan delay, CancellationToken)` → `Task<bool>` — `completed`-Eintrag: `ExpiresAtUtc = utcNow() + delay`, persistieren + `ItemChanged`; `false`, wenn kein `completed`-Eintrag vorliegt.
  - `ExtendExpiryAsync(string mediaType, long mediaId, DateTime untilUtc, CancellationToken)` → `Task<bool>` — `completed`-Eintrag: `ExpiresAtUtc` auf `untilUtc` hochsetzen, wenn `null` oder früher; `false` bei Nicht-`completed`/keiner Änderung.
- **Entfernte Methoden:** `DeleteIfCompletedAsync` — einziger Aufrufer war `DeleteWatchedDownloadEffect` (entfällt).
- **Statisch sichtbar:** Das private `Key(type, mediaId)`-Format `{type}:{mediaId}` wird für den Kandidatenabgleich auch von `AutoDownloadService` benötigt → als `internal static string ManifestKey(string mediaType, long mediaId)` (normalisiert den Typ) exposen; `Key` delegiert darauf.

### `DownloadReconcileResult` (Datenmodellklasse, `VideoPlayer.Mvu/DownloadReconcileResult.cs`)

- **Neue Eigenschaften:** `RemovedExpiredCount` (`int`) — abgelaufene Downloads vollständig entfernt; `ExtendedExpiryCount` (`int`) — Einträge als Auto-Download-Kandidat verlängert.

### `AutoDownloadService` (Klasse, `VideoPlayer.Mvu/AutoDownloadService.cs`)

- **Neue Methoden:** `GetAutoDownloadCandidateKeysAsync(AppSettings settings, CancellationToken)` → `Task<IReadOnlySet<string>>` — offline-fähige Kandidatenmenge (`ManifestKey`-Format); leere Menge bei `!AutoDownloadEnabled`; je Weiterschauen-Eintrag: Selbst-Key, Serien-Vorlauf über `ShowDetailsCache`, Playlist-Vorlauf über `PlaylistEntriesCache`; Einzelfehler protokollieren und überspringen.
- **Geänderte Methoden:** `EnqueueSelfAsync`, `EnqueueEpisodesFromCurrentAsync`, `EnqueuePlaylistEntriesFromCurrentAsync` — `EnqueueAsync` mit `origin: DownloadOrigin.Auto`; `EnqueuePlaylistEntriesFromCurrentAsync` sammelt zusätzlich die geladenen Seiten und schreibt sie via `PlaylistEntriesCache.WriteAsync` in den Cache.

### `PlayerState` (Datenmodellklasse, `VideoPlayer.Mvu/PlayerState.cs`)

- **Neue Eigenschaften:** `DownloadExpiryProtected` (`bool`, Standard `false`) — Schutzprüfung für dieses Medium bereits durchgeführt (Einmal-Semantik je Wiedergabe).

### `AppUpdate` (Klasse, `VideoPlayer.Mvu/AppUpdate.cs`)

- **Geänderte Methoden:**
  - `EvaluateEndWindow` — `AutoDeleteWatchedEpisodes && MediaType == "tvshowepisode"` + `DeleteWatchedDownloadEffect` ersetzt durch `AutoDeletePlayedDownloads` + `ExpireWatchedDownloadEffect` (alle Medientypen).
  - `UpdatePlaybackEnded` — dieselbe Ersetzung.
  - `UpdatePlaybackProgress` — Wiedergabe-Schutz: Flag `DownloadExpiryProtected` + `ProtectDownloadExpiryEffect` beim ersten Takt mit bekannter Dauer bei `LocalFile`-Quelle.
  - `AdvanceToNextEpisode` und die Playlist-Übergänge — `DownloadExpiryProtected = false` im `with`-Reset ergänzen.
- **Neue Event-Handler:** Keine neuen Nachrichten — `ItemRemoved`-/`ItemChanged`-Ereignisse des Dienstes decken die UI-Rückmeldung bereits ab.

### `AppEffect` (Records, `VideoPlayer.Mvu/AppEffect.cs`)

- **Neue Records:** `ExpireWatchedDownloadEffect`, `ProtectDownloadExpiryEffect` (siehe Neue Klassen).
- **Entfernte Records:** `DeleteWatchedDownloadEffect`.

### `ShellAppEffectHandler` (Klasse, `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`)

- **Geänderte Methoden:**
  - `HandleAsync` — `DeleteWatchedDownloadEffect`-Case entfernen; neue Cases `ExpireWatchedDownloadEffect` → `downloads.ExpireAfterAsync(type, id, TimeSpan.FromMinutes(DelayMinutes), ct)` und `ProtectDownloadExpiryEffect` → `downloads.ExtendExpiryAsync(type, id, DateTime.UtcNow + 2 × RemainingDuration, ct)` (jeweils fehlertolerant wie üblich).
  - `LoadDownloadsAsync` — `ReconcileAsync` mit `load.Settings` und vorher ermittelter `GetAutoDownloadCandidateKeysAsync`-Menge aufrufen.
  - `ResumeDownloadsAsync` — ebenso (`settings` des Effekts, `cancellationToken` durchreichen).
  - `EnqueueDownloadAsync` — `origin: DownloadOrigin.Manual` explizit an `EnqueueAsync` übergeben.
  - `DeleteDownloadAsync` — `completedOnly`-Zweig entfällt (einzig `DeleteWatchedDownloadEffect` nutzte ihn).

### `MauiProgram` (Klasse, `VideoPlayer.Maui/MauiProgram.cs`)

- iOS-`CompletionSink`: `CompleteExternalTransferAsync` mit geladenen `AppSettings` aufrufen (`AppSettingsService` aus `sp` auflösen).

### `DownloadListItem` (Datenmodellklasse, `VideoPlayer.Mvu/DownloadListItem.cs`)

- **Neue Eigenschaften:** `Origin` (`DownloadOrigin`, init-Default `Manual` — nicht `required`, damit bestehende Konstruktionen lauffähig bleiben); `ExpiresAtUtc` (`DateTime?`); abgeleitet: `OriginText` (`string?` — „Cache" bei `Auto`, sonst `null`), `HasOriginText` (`bool`), `ExpiresText` (`string?` — nur `completed` mit Ablaufdatum: „Läuft ab am " + `ExpiresAtUtc.Value.ToLocalTime():dd.MM.yyyy HH:mm`), `HasExpiresText` (`bool`).

### `DownloadItemViewModel` (Klasse, `VideoPlayer.Mvu/DownloadItemViewModel.cs`)

- **Neue Durchreich-Properties:** `OriginText`, `HasOriginText`, `ExpiresText`, `HasExpiresText`; `Update` meldet zusätzlich `PropertyChanged` für alle vier.

### `DownloadsPage.xaml` (Seite)

- Eintrags-Template: in der Meta-Zeile (`MediaTypeText`, `SizeText`, …) ein „Cache"-Badge (Label, `Text="{Binding OriginText}"`, `IsVisible="{Binding HasOriginText}"`, eigenes `AutomationId`-fähiges Markup bzw. eigener Pill-Chip im Stil des Zustands-Chips) und ein Ablauf-Label (`Text="{Binding ExpiresText}"`, `IsVisible="{Binding HasExpiresText}"`, `Caption`-Stil) ergänzen.

### `SettingsPage.xaml` / `SettingsPage.xaml.cs` (Seite)

- Download-Abschnitt: Schalter umbenennen/umwidmen `AutoDeleteWatchedSwitch` → `AutoDeletePlayedSwitch` („Downloads nach dem Abspielen automatisch löschen"); neue Zahlenfelder `DownloadRetentionDaysEntry` („Aufbewahrungszeit für Downloads (Tage)", `Keyboard="Numeric"`, Placeholder „Standard: 7") und `DeleteAfterPlaybackDelayEntry` („Löschverzögerung nach dem Abspielen (Minuten)", Placeholder „Standard: 15").
- Code-Behind: `OnAutoDeleteWatchedToggled` → `AutoDeletePlayedDownloads`; `PopulateFromModel` und `WireNumeric`/`SaveNumeric` für die zwei neuen Felder verdrahten.

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `EnsureColumnAsync("download_manifest", "ExpiresAtUtc", "bigint NULL")` | `download_manifest.ExpiresAtUtc` | Ablaufdatum (Ticks, UTC); `NULL` = Altbestand, gilt als abgelaufen |
| `EnsureColumnAsync("download_manifest", "Origin", "varchar NULL")` | `download_manifest.Origin` | Herkunft `manual`/`auto`; `NULL` = Altbestand = `manual` |

Zusätzlich (kein Tabellen-Migration, aber neue Datenstruktur): `cached_lists`-Schlüssel `playlist:{id}` für den `PlaylistEntriesCache`; Preferences-Schlüssel `DownloadRetentionDays`, `AutoDeletePlayedDownloads`, `DeleteAfterPlaybackDelayMinutes` (Lese-Fallback auf `AutoDeleteWatchedEpisodes`).

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `DownloadRetentionDaysEntry` (UI → `DownloadRetentionDays`) | `int.TryParse`; Clamp 1–365 via `Normalize()` | Ungültige Eingabe → `InvalidInputLabel`, Modellwert wiederherstellen (Muster vorhandener Zahlenfelder) |
| `DeleteAfterPlaybackDelayEntry` (UI → `DeleteAfterPlaybackDelayMinutes`) | `int.TryParse`; Clamp 0–1440 via `Normalize()` | Wie oben |
| `ExpireAfterAsync` / `ExtendExpiryAsync` | Nur Einträge im Zustand `completed` wirksam | Fehlender/nicht-fertiger Eintrag → `false`, keine Änderung |
| Ablauf-Pass in `ReconcileAsync` | Nur `completed`-Einträge; laufende Transfers (`runningTransfers`) nie anfassen | — |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `AppSettings.DownloadRetentionDays` / Key `DownloadRetentionDays` | `int` (Tage) | 7 | Aufbewahrungszeit abgeschlossener Downloads; wirkt nur auf neu gesetzte Ablaufdaten |
| `AppSettings.AutoDeletePlayedDownloads` / Key `AutoDeletePlayedDownloads` | `bool` | `false` | „Downloads nach dem Abspielen automatisch löschen" (Migration von `AutoDeleteWatchedEpisodes`) |
| `AppSettings.DeleteAfterPlaybackDelayMinutes` / Key `DeleteAfterPlaybackDelayMinutes` | `int` (Minuten) | 15 | Löschverzögerung nach Erreichen der „angesehen"-Schwelle; wirkt nur auf neu gesetzte Ablaufdaten |

## Seiteneffekte und Risiken

- **Altbestand (Kriterium 16):** Beim ersten Bereinigungslauf nach dem Update gelten alle `completed`-Einträge ohne `ExpiresAtUtc` als abgelaufen — nicht-kandidierbare Alt-Downloads werden in einem Schritt entfernt. Das ist fachlich gefordert, aber eine spürbare Massenlöschung; sie läuft zwingend über `ItemRemoved`, sodass die UI konsistent bleibt.
- **Playlist-Kandidaten des Altbestands:** `PlaylistEntriesCache` existiert erst nach dem ersten `AutoDownloadService`-Lauf des neuen Codes. Playlist-gebundene Downloads ohne Cache-Stand werden im allerersten `ReconcileAsync` nicht als Kandidat erkannt und entfernt. Akzeptabel: ein nachlaufender Auto-Download-Lauf lädt weiterhin benötigte Einträge erneut (Dedup-freundlich). Empfehlung: im `ResumeDownloadsAsync`-Pfad die Kandidatenermittlung vor dem Abgleich ausführen (geplant) — der erste Auto-Download-Lauf schreibt den Cache; künftige Läufe sind dann vollständig.
- **Entfernen von `AutoDeleteWatchedEpisodes`:** Alle Referenzen müssen migriert werden (`AppUpdate` zweimal, `SettingsPage`, `AppSettingsTests`, `AppSettingsServiceTests`, `SettingsStorageContractTests`, `AppUpdateTests_Downloads`/`_Player`, Test-Fixture `Settings(autoDelete)`).
- **Entfernen von `DeleteIfCompletedAsync`/`DeleteWatchedDownloadEffect`:** Contract-Test `ShellAppEffectHandler_WiresDownloadEffects` und `DeleteIfCompleted_QueuedEntry_StaysUntouched` betroffen.
- **`CompleteExternalTransferAsync`-Signatur:** iOS-`CompletionSink` in `MauiProgram` und Contract-Test `DownloadService_UsesBackend_AndCompletesExternal` anpassen.
- **`DownloadListItem`-Record-Gleichheit:** Neue Felder verändern `Equals` — `DownloadItemViewModel.Update` feuert korrekt bei Herkunfts-/Ablaufänderungen (gewünscht); keine Bruchstelle, da neue Felder nicht `required` sind.
- **Wiedergabe-Schutz mit kurzen Restdauern:** `jetzt + 2 × Restdauer` garantiert kein Löschen „während" der Wiedergabe über das Ende hinaus — nach Abspielende + Restdauer greift die normale Frist wieder (fachlich korrekt, Kriterium 15 wortgetreu).
- **`ExpireAfterAsync` auf nicht-`completed` Einträge:** Wirkt nicht — ein gestreamtes (nicht geladenes) Medium oder ein laufender Download bleibt unberührt; Entfernung erst nach Abschluss über die normale Frist.

## Umsetzungsreihenfolge

1. **`DownloadOrigin`/`DownloadOriginNames` anlegen + `DownloadManifestEntry` erweitern + `CacheDatabase`-Migration**
   - Voraussetzungen: Keine.
   - Beschreibung: Enum + Namens-Hilfsklasse (Muster `DownloadState.cs`); `ExpiresAtUtc`/`Origin`-Eigenschaften; zwei `EnsureColumnAsync`-Aufrufe in `InitializeAsync`.
2. **`AppSettings`/`AppSettingsService` erweitern**
   - Voraussetzungen: Keine.
   - Beschreibung: drei neue Eigenschaften + Defaults/Konstanten, `AutoDeleteWatchedEpisodes` entfernen, `Normalize()`-Clamps, drei Persistenzschlüssel, Lese-Fallback auf `AutoDeleteWatchedEpisodesKey`.
3. **`PlaylistEntriesCache` anlegen**
   - Voraussetzungen: `CachedListEntry`, `CacheDatabase` (vorhanden).
   - Beschreibung: `CachedPlaylistEntry`/`CachedPlaylistEntries`-Records + `KeyFor`/`WriteAsync`/`ReadAsync` nach `ShowDetailsCache`-Muster.
4. **`DownloadService` erweitern**
   - Voraussetzungen: Schritte 1, 2 (Typen `DownloadOrigin`, `DownloadRetentionDays`).
   - Beschreibung: `utcNow`-Konstruktorparameter, `EnqueueAsync`-Herkunftsparameter inkl. Dedup-Verlängerung, `ExpiresAtUtc` in `TransferAsync`/`CompleteExternalTransferAsync` (+ `settings`-Parameter), `ExpireAfterAsync`, `ExtendExpiryAsync`, `ManifestKey` internal, `ReconcileAsync`-Ablauf-Pass, `DownloadReconcileResult`-Felder, `ToItem`-Mapping, `DeleteIfCompletedAsync` entfernen.
5. **`AutoDownloadService` erweitern**
   - Voraussetzungen: Schritte 1, 3, 4.
   - Beschreibung: `origin: DownloadOrigin.Auto` an allen `EnqueueAsync`-Aufrufen; `GetAutoDownloadCandidateKeysAsync` (Weiterschauen-Cache + `ShowDetailsCache` + `PlaylistEntriesCache`); Playlist-Cache-Schreiben im Playlist-Pfad.
6. **`AppEffect`-Records, `PlayerState`-Flag, `AppUpdate`-Handler**
   - Voraussetzungen: Schritt 2 (`AutoDeletePlayedDownloads`, `DeleteAfterPlaybackDelayMinutes`); Schritt 6 liegt vor Schritt 8 (Effekt-Handler braucht die Record-Typen).
   - Beschreibung: `ExpireWatchedDownloadEffect` + `ProtectDownloadExpiryEffect` neu, `DeleteWatchedDownloadEffect` entfernt; `PlayerState.DownloadExpiryProtected`; `EvaluateEndWindow`/`UpdatePlaybackEnded` auf neue Option/Effekt umstellen; `UpdatePlaybackProgress` Schutzprüfung; Flag-Resets in allen Medienwechsel-`with`-Blöcken.
7. **`ShellAppEffectHandler` und `MauiProgram`**
   - Voraussetzungen: Schritte 4–6.
   - Beschreibung: Effekt-Cases, `ReconcileAsync`-Aufrufe mit Kandidatenmenge, `EnqueueDownloadAsync`-Herkunft, `completedOnly`-Zweig entfernen, iOS-`CompletionSink` mit `AppSettings`.
8. **UI: `DownloadListItem`, `DownloadItemViewModel`, `DownloadsPage.xaml`, `SettingsPage`**
   - Voraussetzungen: Schritte 1, 2, 4.
   - Beschreibung: Anzeige-Felder (Cache-Badge, Ablaufdatum), Durchreich-Properties, Template-Erweiterung, drei Einstellungs-Controls + Umbenennung des Schalters.
9. **Unit-Tests anpassen und ergänzen**
   - Voraussetzungen: Schritte 1–8.
   - Beschreibung: neue Tests gemäß Testplan; betroffene bestehende Tests migrieren.
10. **E2E-Tests**
    - Voraussetzungen: Schritte 1–8 (funktionsfähige App).
    - Beschreibung: zwei neue Szenarien in `DownloadsTests` (siehe E2E-Abschnitt).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Enqueue_Manual_PersistsOriginManual` | `DownloadServiceTests` | Neuanlage über manuellen Pfad → `Origin = "manual"` im Manifest |
| `Enqueue_Auto_PersistsOriginAuto` | `AutoDownloadServiceTests` | Auto-Vorlauf → `Origin = "auto"` persistiert |
| `Enqueue_Reenqueue_KeepsOrigin` | `DownloadServiceTests` | Re-Enqueue (failed → queued) behält Herkunft und setzt `ExpiresAtUtc` zurück |
| `Enqueue_AutoAlreadyPresent_ExtendsExpiryAndSwitchesOrigin` | `AutoDownloadServiceTests` bzw. `DownloadServiceTests` | Dedup `AlreadyPresent` mit `Auto` → `ExpiresAtUtc` verlängert, `Origin` → `auto` (Kriterium 12) |
| `Transfer_Completed_SetsExpiresAtUtc` | `DownloadServiceTests` | Fertigstellung → `ExpiresAtUtc ≈ utcNow + RetentionDays` (injizierte Zeitquelle) |
| `CompleteExternal_Completed_SetsExpiresAtUtc` | `DownloadServiceTests` | iOS-Abschlusspfad setzt Ablaufdatum ebenfalls |
| `Reconcile_ExpiredCompleted_RemovesFileAndEntry` | `DownloadServiceReconcileTests` | Ablauf ohne Kandidat → Datei + Zeile entfernt, `ItemRemoved` |
| `Reconcile_ExpiredCandidate_ExtendsAndSetsAuto` | `DownloadServiceReconcileTests` | Kandidaten-Key → Verlängerung + `Origin = auto`, `ItemChanged` |
| `Reconcile_LegacyEntryWithoutExpiry_TreatedAsExpired` | `DownloadServiceReconcileTests` | `ExpiresAtUtc = null` → abgelaufen (Altbestand, Kriterium 16) |
| `Reconcile_ExpiredNonCandidate_ManualStaysDeleted` / `_QueuedOrRunning_NotTouched` | `DownloadServiceReconcileTests` | Nur `completed` wird geprüft; laufende/wartende/fehlerhafte Einträge unberührt |
| `Reconcile_WithoutSettings_SkipsExpiryPass` | `DownloadServiceReconcileTests` | Rückwärtskompatibilität: bestehender Aufruf ohne `settings` unverändert |
| `ExpireAfter_CompletedEntry_SetsExpiry` / `_NonCompleted_NoOp` | `DownloadServiceTests` | Verkürzung nur bei `completed` |
| `ExtendExpiry_RaisesEarlierOrNull` / `_LaterExpiry_Kept` / `_NonCompleted_NoOp` | `DownloadServiceTests` | Hochsetzungs-Semantik des Wiedergabe-Schutzes |
| `GetCandidateKeys_SelfAndSeriesAndPlaylist` / `_AutoDisabled_Empty` / `_MissingCaches_SelfOnly` | `AutoDownloadServiceTests` | Offline-Kandidatenmenge aus Weiterschauen-Cache, `ShowDetailsCache`, `PlaylistEntriesCache` |
| `PlaylistCache_WriteRead_RoundTrip` | `AutoDownloadServiceTests` (oder neue `PlaylistEntriesCacheTests`) | `PlaylistEntriesCache`-Persistenz + defektes JSON → `null` |
| `PlaybackProgress_LocalFileFirstTick_EmitsProtectEffect` / `_SecondTick_NoRepeat` / `_StreamSource_NoEffect` / `_UnknownDuration_NoEffect` | `AppUpdateTests_Player` | Wiedergabe-Schutz-Emission und Einmal-Semantik (`DownloadExpiryProtected`) |
| `PlaybackProgress_WatchedMovie_EmitsExpireEffect` / `_Disabled_NoEffect` | `AppUpdateTests_Downloads` | `ExpireWatchedDownloadEffect` für alle Medientypen (Film eingeschlossen), `DelayMinutes` aus Settings |
| `PlaybackEnded_WatchedEpisode_EmitsExpireEffect` | `AppUpdateTests_Downloads` | Ersetzung am Wiedergabeende |
| `Defaults_AreExpected` / `Normalize_ClampsRetentionAndDelay` | `AppSettingsTests` | neue Defaults (7 / 15 / aus) und Clamps |
| `Load_MigratesAutoDeleteWatchedEpisodes` / `_NewKeyWins` | `AppSettingsServiceTests` | Lese-Fallback des Alt-Schlüssels |
| `CacheDatabase_MigratesExpiryAndOriginOntoExistingManifest` | `LocalDatabaseTests` | `EnsureColumnAsync`-Muster: Tabelle ohne Spalten → `InitializeAsync` → Spalten vorhanden, Altbestand `null` |
| `DownloadListItem_AutoOrigin_CacheText` / `_ExpiresText_OnlyCompletedWithExpiry` | `DownloadsContractTests` | „Cache"-Kennzeichnung und Ablaufanzeige des Snapshots |
| `DownloadOriginNames_FromPersisted_NullIsManual` | `DownloadsContractTests` o. ä. | Altbestand-Mapping |
| `SettingsPage_ContainsAllSettingControls` (erweitern) | `SettingsStorageContractTests` | `AutoDeletePlayedSwitch`, `DownloadRetentionDaysEntry`, `DeleteAfterPlaybackDelayEntry` vorhanden; `AutoDeleteWatchedSwitch` entfernt |
| Hilfsmethode `InsertEntryAsync(…, expiresAtUtc, origin)` | `DownloadServiceReconcileTests` | Bestehende Manifest-Insert-Hilfe um `ExpiresAtUtc`/`Origin` erweitern |
| Hilfsmethoden `CacheShowAsync`, `CachePlaylistAsync` | `AutoDownloadServiceTests`/`DownloadServiceReconcileTests` | `ShowDetailsCache.WriteAsync`- bzw. `PlaylistEntriesCache.WriteAsync`-Fixtures |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `PlaybackProgress_TwoWindowTicks_EmitsAutoDelete`, `…_WatchedMovie_NoAutoDelete`, `…_AutoDeleteDisabled_NoEffect`, `PlaybackEnded_WatchedEpisode_EmitsAutoDelete` (`AppUpdateTests_Downloads`) | `DeleteWatchedDownloadEffect` → `ExpireWatchedDownloadEffect`; Film-Case dreht um (Movie wird jetzt bedient); Setting `AutoDeletePlayedDownloads` |
| `DeleteIfCompleted_QueuedEntry_StaysUntouched` (`DownloadServiceTests`) | `DeleteIfCompletedAsync` entfällt → Test entfernen |
| `Settings(wifiOnly, autoDelete)`-Fixture | `AutoDeleteWatchedEpisodes` → `AutoDeletePlayedDownloads` |
| `ShellAppEffectHandler_WiresDownloadEffects`, `DownloadService_UsesBackend_AndCompletesExternal`, `DownloadService_Reconcile_WiredInHandler` (`DownloadsContractTests`) | Effekt-Entfernung/-Neuaufnahme; `CompleteExternalTransferAsync`-Signatur; `ReconcileAsync` mit Settings/Kandidaten |
| `SettingsPage_ContainsAllSettingControls`, `Defaults_AreExpected`, `Load_…`, `Save_…` (`SettingsStorageContractTests`, `AppSettingsTests`, `AppSettingsServiceTests`) | neues Settings-Schema, Schlüsselumbenennung |
| Alle `ReconcileAsync()`-Aufrufe ohne Argumente | bleiben kompilierbar (optionale Parameter) — ggf. ergänzende Aufrufvariante mit `settings` dokumentieren |

### E2E-Tests (Pflicht)

| Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium |
|----------|------------------------|-------------------------------|
| „Cache"-Herkunftsanzeige: Weiterschauen-Film (Resume-Position > 0 auf dem Mock-Server) → „Automatische Downloads" auf der Einstellungsseite einschalten (erzwingt `RunAutoDownloadsEffect`) → Downloads-Seite → Eintrag trägt „Cache"-Kennzeichnung; zuvor „Downloads nur im WLAN" ausschalten (Netzwerkprofil im Testlauf nicht zugesichert) | `DownloadsTests` (`VideoPlayer.E2E.Tests`) | Kriterien 1–3 |
| Happy Path „Nach dem Abspielen löschen" + Bereinigungslauf: Download über Detailseite bis `completed` → Einstellungen: `AutoDeletePlayedSwitch` ein, `DeleteAfterPlaybackDelayEntry` = `0` → Wiedergabe des ~3,3 s-Testvideos (Endfenster 300 s greift ab dem ersten plausiblen Takt) → Downloads-Seite öffnen → `ReconcileAsync` entfernt den abgelaufenen Eintrag → `EmptyLabel` sichtbar | `DownloadsTests` (`VideoPlayer.E2E.Tests`) | Kriterien 5, 8, 9, 10 |

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `DownloadsPage_DeleteFailed_RemovesFailedEntry` | voraussichtlich unverändert — „Alle Fehler löschen" bleibt bestehen; nur prüfen, dass neue Eintrags-Labels die Textsuche (`Items[i].Text`) nicht stören |

## Offene Punkte

Keine.
