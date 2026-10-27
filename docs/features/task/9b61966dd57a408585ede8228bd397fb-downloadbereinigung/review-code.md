# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### DownloadService.cs (DownloadService)

- **God-Methode** — `ReconcileAsync` (Z. 1022–1187, ~165 Zeilen) bündelt sechs konzeptuell getrennte Schritte: Manifest laden, Dateinamen-Index bauen, Verzeichnis scannen, Teildatei-Bereinigung, „completed ohne Datei"-Korrektur, den neuen Ablauf-Pass (Z. 1124–1157) sowie Ergebnis-Protokollierung und -Zusammenbau. Der Feature-Zuwachs (Ablauf-Pass mit Kandidaten-Verlängerung) hat die ohnehin lange Methode weiter verlängert.

  Empfehlung: Den Ablauf-Pass in eine private Methode auslagern (z. B. `RunExpiryPassAsync(IReadOnlyList<DownloadManifestEntry> entries, AppSettings settings, IReadOnlySet<string>? candidateKeys, CancellationToken)` mit Rückgabe `(int RemovedExpired, int ExtendedExpiry)`); optional auch den Datei-Scan-Block (Z. 1066–1112) als `ScanFilesAsync` extrahieren.

- **Long Parameter List** — `EnqueueAsync` (Z. 257–266) deklariert neun Parameter; die zugehörige Datengruppe (type/mediaId/title/seriesContext/playlistContext/pictureId/origin) wurde intern bereits als `EnqueueRequest`-Record (Z. 398–405) gebündelt — die öffentliche Signatur trägt dieselben Fachdaten jedoch einzeln weiter.

  Empfehlung: `EnqueueRequest` aus der Klasse herausheben (public/internal) und die Signatur auf `EnqueueAsync(EnqueueRequest request, AppSettings settings, CancellationToken)` reduzieren; die vier Aufrufer (`ShellAppEffectHandler.EnqueueDownloadAsync`, `AutoDownloadService` 3×, Tests) auf den Record umstellen.

### AutoDownloadService.cs (AutoDownloadService)

- **Doppelter Code / fehlende Kapselung** — Die Playlist-Bindungs-Bedingung `item.PlaylistId is > 0 && item.PlaylistEntryId is > 0` (bzw. negiert `is not > 0`) steht an vier Stellen inline: `RunAsync` (Z. 139), `GetAutoDownloadCandidateKeysAsync` (Z. 235), `EnqueuePlaylistEntriesFromCurrentAsync` (Z. 431) und `EnqueueSelfAsync` (Z. 658).

  Empfehlung: `private static bool HasPlaylistBinding(HomeMediaItem item) => item.PlaylistId is > 0 && item.PlaylistEntryId is > 0;` einführen und an allen vier Stellen verwenden (negiert in Z. 431 als `!HasPlaylistBinding(item)`).

### DownloadsPage.xaml

- **Doppelter Code** — Der Herkunfts-Chip „Cache" (Z. 163–173) wiederholt das Zustands-Chip-Markup (Z. 144–154) nahezu identisch: `RoundRectangle 999`, `Stroke="Transparent"`, `BackgroundColor={StaticResource VpPrimaryTint}`, `Padding="10,4"`, Label mit `FontSize="11"`, `FontFamily="InterSemiBold"`, `TextColor={StaticResource VpPrimarySoft}`. Unterschiede sind nur Bindung, `AutomationId` und `IsVisible`.

  Empfehlung: Einen gemeinsamen `Style` (z. B. `x:Key="PillChip"` für den `Border` inkl. Label-Style oder einen `ControlTemplate`) auslagern und beide Chips darauf referenzieren.

### Stubs/FakeNetworkMonitor.cs (FakeNetworkMonitor)

- **Toter Code** — `SetOnline` (Z. 16–20) hat im gesamten Testprojekt keinen Aufrufer (alle Tests setzen `IsOnline` direkt per Property); der Befund aus Iteration 2 ist durch die Zentralisierung in `Stubs/` unverändert bestehen geblieben. Zusätzlich verweist der Klassen-XML-Doc (Z. 7–8) auf die Methode.

  Empfehlung: `SetOnline` samt Doc-Verweis entfernen (die Schnittstellen-Mitglieder `IsOnline`/`IsWifi`/`AvailabilityChanged`/`Start` bleiben).

### DownloadServiceTests_Expiry.cs + DownloadServiceTests.cs

- **Doppelter Code** — `ReadEntryAsync` ist identisch in `DownloadServiceTests_Expiry.cs` (Z. 79–85) und `DownloadServiceTests.cs` (Z. 102–108) angelegt (eine dritte Variante existiert in `DownloadServiceReconcileTests.cs` Z. 104); `InsertCompletedAsync` ist nahezu identisch in beiden neuen/erweiterten Klassen vorhanden (`DownloadServiceTests_Expiry.cs` Z. 87–105 vs. `DownloadServiceTests.cs` Z. 110–127 — unterscheiden sich nur in optionalen Parametern). Die zentralisierten Stubs unter `Stubs/` sind bereits etabliert.

  Empfehlung: Die Manifest-Hilfsmethoden in eine gemeinsame Test-Hilfsklasse auslagern (z. B. `Stubs/ManifestSeeder` mit `cache`/`layout`-Parametern: `ReadEntryAsync(CacheDatabase, string, long)`, `InsertCompletedAsync(CacheDatabase, AppStorageLayout, long, DateTime?, string?)`) oder eine gemeinsame Basisklasse der drei Manifest-Testklassen einführen.

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
- `VideoPlayer.Maui.Tests/ProgressReportServiceTests.cs`
- `VideoPlayer.Maui.Tests/SettingsStorageContractTests.cs`
- `VideoPlayer.Maui.Tests/Stubs/FakeNetworkMonitor.cs`
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
