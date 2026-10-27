# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

Der Testlauf (`dotnet test VideoPlayer.Maui.Tests --collect:"XPlat Code Coverage"`) lief vollständig und sauber durch; der in früheren Läufen beobachtete nicht reproduzierbare SQLite-Native-Absturz des Testhosts ist diesmal nicht aufgetreten — kein Wiederholungslauf nötig.

## Zusammenfassung

- Gesamt: 1440
- Bestanden: 1440
- Fehlgeschlagen: 0
- Übersprungen: 0

## Testabdeckung

**Abdeckung:** 90,1 % (gesamt, Zeilenabdeckung über alle instrumentierten Assemblys: VideoPlayer.Mvu, VideoPlayer.Storage, VideoPlayer.ApiClient, msTools.DeveloperApi; VideoPlayer.Maui ist nicht instrumentiert)

Dateien unter 80 % Zeilenabdeckung:

| Datei | Abdeckung |
|-------|-----------|
| VideoPlayer.ApiClient/Discovery/MdnsServerDiscovery.cs | 0,0 % |
| VideoPlayer.Mvu/DownloadListItem.cs | 62,3 % |
| VideoPlayer.Mvu/PlaylistDetailsState.cs | 65,5 % |
| msTools.DeveloperApi/DevApiRequest.cs | 66,7 % |
| VideoPlayer.Mvu/ProfilePageState.cs | 70,0 % |
| VideoPlayer.Storage/FileLoggerProvider.cs | 71,4 % |
| VideoPlayer.Mvu/DownloadItemViewModel.cs | 74,1 % |
| VideoPlayer.ApiClient/Discovery/DiscoveredServer.cs | 75,0 % |
| VideoPlayer.Mvu/SourceItem.cs | 75,0 % |
| VideoPlayer.Mvu/MediaTypeNames.cs | 77,3 % |
| VideoPlayer.Mvu/ServerEventsService.cs | 78,0 % |
| VideoPlayer.ApiClient/Models/MediaEntry.cs | 78,4 % |
| VideoPlayer.Mvu/MediaDetailsView.cs | 78,9 % |
| msTools.DeveloperApi/DevApiHttpServer.cs | 79,1 % |
| VideoPlayer.ApiClient/Discovery/ServerDiscoveryService.cs | 79,4 % |

Geänderte/neue Feature-Dateien dieses Tasks (Auswahl): AppUpdate.cs 90,9 %, AppEffect.cs 91,0 %, AutoDownloadService.cs 91,8 %, DownloadService.cs 88,7 %, PlaylistEntriesCache.cs 90,5 %, DownloadListItem.cs 62,3 %, DownloadItemViewModel.cs 74,1 %, DownloadReconcileResult.cs 100 %, DownloadOrigin.cs 100 %, PlayerState.cs 100 %, AppSettings.cs 100 %, AppSettingsService.cs 100 %, CacheDatabase.cs 95,7 %, DownloadManifestEntry.cs 100 %.

## Fehlende Tests

Quelle: `Coverage-Daten`

- `VideoPlayer.ApiClient/Discovery/MdnsServerDiscovery.cs` — 0 % Zeilenabdeckung; keine Feature-Datei dieses Tasks. mDNS-Dienstsuche über Zeroconf ist netzwerk-/laufzeitabhängig und ohne Infrastruktur-Abstraktion nicht sinnvoll unit-testbar.

Anmerkung: Die geänderten Maui-UI-Dateien (`SettingsPage.xaml(.cs)`, `DownloadsPage.xaml`, `ShellAppEffectHandler.cs`, `MauiProgram.cs`) sind nicht Teil der instrumentierten Assemblys, werden aber durch Contract-Tests referenziert (u. a. `DownloadsContractTests`, `SettingsStorageContractTests`, `VideoPlayerMauiContractTests`); `MauiProgram.cs` ist als Einstiegspunkt ohnehin ausgenommen. Alle übrigen geänderten/neuen Feature-Quelldateien weisen eine Zeilenabdeckung über 0 % auf; `DownloadListItem.cs` (62,3 %) und `DownloadItemViewModel.cs` (74,1 %) liegen unter 80 %.
