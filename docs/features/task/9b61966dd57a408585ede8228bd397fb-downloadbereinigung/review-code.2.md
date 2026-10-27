# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### DownloadService.cs (DownloadService)

- **Doppelter Code** — `ExpireAfterAsync` (Z. 624–650) und `ExtendExpiryAsync` (Z. 666–698) haben ein ~20 Zeilen langes, nahezu identisches Grundgerüst: `manifestGate.WaitAsync` → `FindEntryAsync` → Guard `entry is null || State != Completed` → `ExpiresAtUtc`/`UpdatedAtUtc` setzen → `UpdateAsync` → `RaiseItemChanged` → `Release`. Die Methoden unterscheiden sich nur in der Zielberechnung (`utcNow() + delay` vs. `utcNow() + PlaybackProtectionFactor * remaining`) und der Nur-Hochsetzen-Vorbedingung in `ExtendExpiryAsync`.

  Empfehlung: Gemeinsame private Hilfsmethode einführen, z. B. `UpdateCompletedExpiryAsync(string type, long mediaId, Func<DownloadManifestEntry, DateTime?> computeTargetUtc, CancellationToken)` — liefert der Selektor `null`, wird `false` zurückgegeben; beide öffentlichen Methoden werden zu zwei Zeilen.

- **Einheitlichkeit / Nebenläufigkeit** — `RaiseItemChanged` wird in `ExpireAfterAsync` (Z. 643), `ExtendExpiryAsync` (Z. 691) und `ResolveExistingEntryAsync` (Z. 426) innerhalb des gehaltenen `manifestGate` ausgelöst; die neuen Hilfsmethoden `UpdateManifestEntryAsync` (Z. 1396) und `DeleteManifestEntryAsync` (Z. 1419) sowie `EnqueueAsync` (Z. 320) lösen bewusst erst nach `Release` aus. Der Handler `OnDownloadItemChanged` dispatcht synchron bis zum ersten Await — der kritische Abschnitt verlängert sich unnötig und ein künftiger Handler, der synchron auf den Dienst zugreift, läuft Gefahr, sich am eigenen Gate zu beteiligen.

  Empfehlung: Das Ereignis nach dem Gate-Release auslösen (Erfolgsflag aus dem `try`-Block herausreichen und `RaiseItemChanged` hinter dem `finally` aufrufen) — analog zum Muster der Hilfsmethoden.

- **Long Parameter List / Data Clump** — `ResolveExistingEntryAsync` (Z. 394–401) und `CreateManifestEntry` (Z. 462–470) deklarieren je sieben Parameter und schleppen dieselbe Datengruppe (type/mediaId/title/seriesContext/playlistContext/pictureId/origin) aus `EnqueueAsync` einzeln weiter. Die Gruppe beschreibt fachlich genau eine Einreihe-Anforderung.

  Empfehlung: Die Einreihe-Daten in einem kleinen internen Request-Record (z. B. `EnqueueRequest`) bündeln und diesen an beide Methoden übergeben.

- **Fehlende Validierung** — `CompleteExternalTransferAsync` (Z. 767–801) validiert den neuen `settings`-Parameter nicht; `EnqueueAsync` (Z. 268) und `ResumeInterruptedAsync` (Z. 713) rufen für denselben Parameter `ArgumentNullException.ThrowIfNull(settings)` auf. Bei `null` scheitert der Erfolgspfad verspätet mit `NullReferenceException` an `settings.DownloadRetentionDays` (Z. 789).

  Empfehlung: `ArgumentNullException.ThrowIfNull(settings);` am Methodenanfang ergänzen.

### ShellAppEffectHandler.cs (ShellAppEffectHandler)

- **Doppelter Code** — Das Aufrufpaar `GetAutoDownloadCandidateKeysAsync(settings, ct)` + `ReconcileAsync(settings, candidateKeys, ct)` wiederholt sich identisch in `LoadDownloadsAsync` (Z. 1485–1486) und `ResumeDownloadsAsync` (Z. 1677–1678). Die Reihenfolge ist fachlich gekoppelt (die Kandidatenmenge steuert den Ablauf-Pass) — wird sie an einer der beiden Stellen geändert oder vergessen, läuft der Bereinigungslauf dort lautlos ohne Verlängerung.

  Empfehlung: Private Hilfsmethode `ReconcileWithExpiryPassAsync(AppSettings settings, CancellationToken)` einführen, die beide Aufrufe kapselt.

### DownloadServiceTests.cs (DownloadServiceTests)

- **Doppelter Code** — Der Arrange-Block „Videodatei anlegen + `completed`-Manifest-Eintrag einfügen" (`File.WriteAllBytesAsync(path, new byte[64])` + `InsertAsync(new DownloadManifestEntry { … })`) wiederholt sich nahezu identisch in fünf neuen Tests: `ExpireAfter_CompletedEntry_SetsExpiry` (Z. 695–714), `ExtendExpiry_RaisesEarlierOrNull` (Z. 738–758), `ExtendExpiry_LaterExpiry_Kept` (Z. 766–787), `Enqueue_AutoAlreadyPresent_ExtendsExpiryAndSwitchesOrigin` (Z. 810–836), `Enqueue_ManualAlreadyPresent_KeepsExpiryAndOrigin` (Z. 843–867). In `DownloadServiceTests_Expiry.cs` wurde dafür bereits die Hilfsmethode `InsertCompletedAsync` eingeführt.

  Empfehlung: Entsprechende Hilfsmethode (z. B. `InsertCompletedAsync(long mediaId, DateTime? expiresAtUtc = null, string? origin = null)`) auch in dieser Testklasse anlegen und die fünf Arrange-Blöcke ersetzen.

### DownloadServiceTests_Expiry.cs (DownloadServiceTests_Expiry)

- **Toter Code** — `using System.Net;` (Z. 2) wird nicht verwendet (die Datei nutzt kein `HttpStatusCode` o. ä.); `FakeNetworkMonitor.SetOnline` (Z. 174–178) wird in der Testklasse nirgends aufgerufen.

  Empfehlung: Using-Direktive und die Methode `SetOnline` entfernen (die Schnittstellen-Mitglieder `IsOnline`/`IsWifi`/`AvailabilityChanged`/`Start` bleiben).

- **Doppelter Code** — `FakeNetworkMonitor` (Z. 168–179) ist die fünfte nahezu identische Kopie im Testprojekt (`AutoDownloadServiceTests.cs:1104`, `DownloadServiceReconcileTests.cs:473`, `DownloadServiceTests.cs:1083`, `ProgressReportServiceTests.cs:270`).

  Empfehlung: Den Stub einmalig unter `VideoPlayer.Maui.Tests/Stubs/` zentralisieren (Analog zu `InMemorySettingsStore`/`InMemorySecretStore`) und die fünf Kopien ersetzen.

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
- `VideoPlayer.Maui.Tests/DownloadServiceTests_Expiry.cs`
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
