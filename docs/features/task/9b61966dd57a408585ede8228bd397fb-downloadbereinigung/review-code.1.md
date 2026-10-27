# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### DownloadService.cs (DownloadService)

- **Kopplung/Nebenläufigkeit** — `ExpireAfterAsync` (Z. 541–565) und `ExtendExpiryAsync` (Z. 580–609) führen `FindEntryAsync`, die Vorbedingungsprüfung (`completed`, `ExpiresAtUtc >= untilUtc`) und die Feld-Mutation außerhalb von `manifestGate` aus; nur das `UpdateAsync` liegt im Gate. `EnqueueAsync` (Z. 316–392) hält dagegen den kompletten Read-Modify-Write-Abschnitt unter dem Gate — ein paralleler Schreiber kann zwischen Prüfung und Schreiben dazwischenfunken (z. B. überschreibt ein Re-Enqueue die zwischenzeitlich gesetzten Werte).

  Empfehlung: Find + Guard + Mutation + Update in einen `manifestGate`-Block ziehen, analog zum Muster in `EnqueueAsync`.

- **Doppelter Code** — Das Muster `manifestGate.WaitAsync → cache.Connection.UpdateAsync/DeleteAsync → Release → RaiseItemChanged/RaiseItemRemoved` wiederholt sich nahezu identisch an fünf Stellen (Z. 553–563, 597–607, 982–990, 1044–1055, 1060–1071).

  Empfehlung: Private Hilfsmethoden `UpdateManifestEntryAsync(DownloadManifestEntry, CancellationToken)` und `DeleteManifestEntryAsync(DownloadManifestEntry, CancellationToken)` inkl. Event-Raise einführen und an den Stellen aufrufen.

- **Middle Man / Einheitlichkeit** — `Key` (Z. 1449) delegiert nur an `ManifestKey` (Z. 1446); innerhalb von `ReconcileAsync` werden beide Varianten gemischt verwendet (Z. 995 `Key`, Z. 1032/1039 `ManifestKey`).

  Empfehlung: `Key` entfernen und alle Aufrufer einheitlich auf `ManifestKey` umstellen.

- **Einheitlichkeit (Zeitquelle)** — `ExpiresAtUtc` wird über die injizierte Zeitquelle `utcNow()` gesetzt, `UpdatedAtUtc` dagegen über `DateTime.UtcNow` (Z. 342, 552, 596, 1043). Tests, die `utcNow` injizieren, können `UpdatedAtUtc` nicht kontrollieren.

  Empfehlung: Für die neu gesetzten Zeitstempel einheitlich `utcNow()` verwenden.

- **God-Methode** — `EnqueueAsync` (Z. 250–414, ~165 Zeilen Methodenkörper) bündelt Validierung, eine Logging-Lokalfunktion, drei Dedup-Zweige inkl. Auto-Verlängerung, Re-Enqueue, Insert, Quota-Vorab-Prüfung und den Pump-Trigger; der Feature-Zuwachs hat die Methode weiter verlängert.

  Empfehlung: Dedup-/Requeue-Zweig und Quota-Prüfung in private Methoden auslagern (z. B. `TryDedupExisting(...)`, `RequeueOrInsertAsync(...)`).

### AutoDownloadService.cs (AutoDownloadService)

- **Doppelter Code** — Die Index-Suche per Hand-`for`-Schleife in `AddPlaylistCandidateKeysAsync` (Z. 287–295) und `AddSeriesCandidateKeysAsync` (Z. 348–356) ist identisch aufgebaut; dieselbe Klasse nutzt in `EnqueueEpisodesFromCurrentAsync` (Z. 400) bereits `FindIndex`.

  Empfehlung: Einheitlich `List.FindIndex` (bzw. eine kleine `IndexOf`-Hilfsfunktion für `IReadOnlyList`) verwenden.

- **Fehlerbehandlung (unerreichbarer Catch)** — Der Exception-Filter `SQLiteException | InvalidOperationException | JsonException` in `GetAutoDownloadCandidateKeysAsync` (Z. 251) ist praktisch unerreichbar: `PlaylistEntriesCache.ReadAsync` und `ShowDetailsCache.ReadAsync` schlucken alle Fehler intern und liefern `null`; im try-Block liegt keine weitere Fehlerquelle (`keys.Add`, `Normalize`, Null-Checks, reine In-Memory-Iteration).

  Empfehlung: Den try/catch-Block entfernen oder die konkret erwartete Fehlerquelle benennen und nur diese abfangen.

- **God-Methode** — `EnqueuePlaylistEntriesFromCurrentAsync` (Z. 457–560, ~100 Zeilen) verbindet seitenweises Laden, Treffersuche des aktuellen Eintrags, Ladbarkeitsfilter, Einreihung und Cache-Pflege in einem Schachtel-Konstrukt (Seitenschleife + innerer foreach).

  Empfehlung: Seiten-Iteration mit Treffersuche und die eigentliche Kandidaten-Einreihung bzw. das Cache-Schreiben in Hilfsmethoden teilen.

### PlaylistEntriesCache.cs (PlaylistEntriesCache)

- **Toter Code / irreführende Dokumentation** — Der Parameter `cancellationToken` wird in `WriteAsync` (Z. 81) und `ReadAsync` (Z. 134) deklariert, aber nie verwendet; die XML-Doku behauptet dennoch „Bricht das Schreiben/Lesen ab".

  Empfehlung: Entweder den Token an die sqlite-net-Aufrufe durchreichen und die Doku belassen, oder Parameter und Dokumentationsversprechen entfernen.

### AppEffect.cs (AppEffect)

- **Einheitlichkeit (Dauer-Repräsentation)** — `ExpireWatchedDownloadEffect.DelayMinutes` ist ein `int` in Minuten (Z. ~787), `ProtectDownloadExpiryEffect.RemainingDuration` ein `TimeSpan` (Z. ~798) — dasselbe Konzept „Dauer" in zwei Repräsentationen.

  Empfehlung: Einheitlich `TimeSpan` verwenden (z. B. `Delay` statt `DelayMinutes`), die Minuten-Umrechnung entfällt dann im Handler.

### ShellAppEffectHandler.cs (ShellAppEffectHandler)

- **Hardcodierter Wert / Umgehung der Zeitquelle** — `ProtectDownloadExpiryAsync` berechnet das Ziel-Ablaufdatum mit `DateTime.UtcNow + 2 * protect.RemainingDuration` (Z. 1598): Der Faktor `2` ist eine unbenannte Magic-Number, und die Berechnung im Handler umgeht die im `DownloadService` injizierte Test-Zeitquelle `utcNow` — anders als `ExpireAfterAsync`, das eine `TimeSpan`-Dauer entgegennimmt und intern mit `utcNow()` rechnet.

  Empfehlung: `ExtendExpiryAsync` analog auf eine Dauer-Signatur umstellen (`ExtendExpiryAsync(mediaType, mediaId, TimeSpan remaining, ...)`) und im Service `utcNow() + factor * remaining` rechnen; den Faktor als benannte Konstante (z. B. `PlaybackProtectionFactor = 2`) ausweisen.

### AppUpdate.cs (AppUpdate)

- **Fehlende Validierung** — Seit die Bedingung `MediaType == "tvshowepisode"` durch `AutoDeletePlayedDownloads` ersetzt wurde, wird `updated.MediaType!` per Null-Forgiving übergeben (Z. 3876, 3944, 4014); die Nicht-Null-Invariante (aktive Wiedergabe) ist nur implizit.

  Empfehlung: Mit `updated.MediaType is { } mediaType` guarden (oder `updated.IsActive` prüfen) und den lokal gebundenen Wert an die Effekte übergeben.

### AutoDownloadServiceTests.cs (AutoDownloadServiceTests)

- **Testqualität** — `PlaylistCache_WriteRead_RoundTrip` (Z. 1036) prüft zwei fachliche Fälle in einer Methode: den Round-Trip der gecachten Struktur und den Fehlerpfad „defektes JSON liefert `null`".

  Empfehlung: In zwei Tests aufteilen (`..._RoundTrip` und `..._CorruptJson_ReturnsNull`).

## Geprüfte Dateien

Liste aller geprüften Dateien:

- `VideoPlayer.E2E.Tests/Tests/DownloadsTests.cs`
- `VideoPlayer.Maui.Tests/AppSettingsServiceTests.cs`
- `VideoPlayer.Maui.Tests/AppSettingsTests.cs`
- `VideoPlayer.Maui.Tests/AppUpdateTests_Downloads.cs`
- `VideoPlayer.Maui.Tests/AppUpdateTests_Player.cs`
- `VideoPlayer.Maui.Tests/AutoDownloadServiceTests.cs`
- `VideoPlayer.Maui.Tests/DownloadServiceReconcileTests.cs`
- `VideoPlayer.Maui.Tests/DownloadServiceTests.cs`
- `VideoPlayer.Maui.Tests/DownloadsContractTests.cs`
- `VideoPlayer.Maui.Tests/LocalDatabaseTests.cs`
- `VideoPlayer.Maui.Tests/SettingsStorageContractTests.cs`
- `VideoPlayer.Maui/MauiProgram.cs`
- `VideoPlayer.Maui/Pages/DownloadsPage.xaml`
- `VideoPlayer.Maui/Pages/SettingsPage.xaml`
- `VideoPlayer.Maui/Pages/SettingsPage.xaml.cs`
- `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`
- `VideoPlayer.Mvu/AppEffect.cs`
- `VideoPlayer.Mvu/AppUpdate.cs`
- `VideoPlayer.Mvu/AutoDownloadService.cs`
- `VideoPlayer.Mvu/DownloadItemViewModel.cs`
- `VideoPlayer.Mvu/DownloadListItem.cs`
- `VideoPlayer.Mvu/DownloadOrigin.cs`
- `VideoPlayer.Mvu/DownloadReconcileResult.cs`
- `VideoPlayer.Mvu/DownloadService.cs`
- `VideoPlayer.Mvu/PlayerState.cs`
- `VideoPlayer.Mvu/PlaylistEntriesCache.cs`
- `VideoPlayer.Storage/AppSettings.cs`
- `VideoPlayer.Storage/AppSettingsService.cs`
- `VideoPlayer.Storage/CacheDatabase.cs`
- `VideoPlayer.Storage/Entities/DownloadManifestEntry.cs`
