# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

Begründung (Iteration 3): Build der gesamten Solution fehlerfrei, alle 1280 Unit-/Contract-Tests bestanden (8 neue Tests gegenüber Iteration 2, u. a. `ShellAppEffectHandler_PartialSync_StaysBelowInformation`, `ReportProgress_EnqueueFailure_LogsWarningOncePerMedia`, `EnqueueAsync_DedupNotice_LogsBelowInformation`, `EnqueueAsync_QuotaExceeded_LogsSingleWarning`, `FlushPendingProgress_LogsSentCount_OnlyWhenSent`), alle 56 E2E-Tests bestanden – inklusive der drei geplanten `LogProtocolTests`-Szenarien (Pflicht-Fact + LogoutAndPipSubFlows + PlaybackFailureSubFlow). Die Iteration-3-Code-Änderungen an Log-Levels/Dedup (DownloadService, ServerSyncService, PictureInPictureService, ShellAppEffectHandler, ProgressReportService, AuthSessionService, ContinueWatchingMutationService, AppLogService, PlayerPage) verursachen keine Regression.

Ausgeführte Befehle:

- `dotnet build VideoPlayer.App.sln -c Release` – 0 Fehler, 2 Warnungen (XA4301: APK-Duplikat `libe_sqlite3.so` in `VideoPlayer.Maui` net10.0-android, harmlos; 1:13 min, inkrementell)
- `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release --no-build --collect:"XPlat Code Coverage"` – 1280/1280 bestanden (37,6 s)
- `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj -c Release --no-build` – 56/56 bestanden (4,7 min, sichtbare GUI-Automatisierung per FlaUI auf interaktiver Konsole)

## E2E-Abdeckung

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Protokollierung einschalten → Neustart mit Profil → Auto-Anmeldung + Wiedergabe → `[INFO]`-Facheinträge ohne `access_token`/Zugangsdaten (Pflicht) | `LogProtocolTests.LoggingEnabled_RealFlows_WriteDiagnosticEntries` (`VideoPlayer.E2E.Tests/Tests/LogProtocolTests.cs:171`) | Bestanden |
| Ohne Protokollierung kein "Protokoll senden"-Button, keine Protokolldatei | `LogProtocolTests.LoggingDisabled_HidesSendButton_AndNoLogFile` (`LogProtocolTests.cs:27`) | Bestanden |
| Aktivieren zeigt Versand-Button und legt Protokolldatei an | `LogProtocolTests.EnableLogging_ShowsSendButton_AndCreatesLogFile` (`LogProtocolTests.cs:48`) | Bestanden |
| Einstellung übersteht Neustart mit demselben Profil | `LogProtocolTests.LoggingEnabled_PersistsAcrossRestart` (`LogProtocolTests.cs:74`) | Bestanden |
| "Protokoll senden" bis zur Plattformgrenze ohne Absturz | `LogProtocolTests.SendLog_Click_InvokesPlatformShareOrMail_WithoutCrash` (`LogProtocolTests.cs:115`) | Bestanden |
| Abmeldung sichtbar im Protokoll (optional) | `LogProtocolTests.LoggingEnabled_LogoutAndPipSubFlows_WriteDiagnosticEntries` (`LogProtocolTests.cs:263`; Abmelde-Teilfluss, prüft Eintrag "Abmeldung" ohne Zugangsdaten) | Bestanden |
| PiP-Umschaltung sichtbar im Protokoll (optional) | `LogProtocolTests.LoggingEnabled_LogoutAndPipSubFlows_WriteDiagnosticEntries` (`LogProtocolTests.cs:263`; PiP-Teilfluss, prüft Eintrag "PiP-Umschaltung") | Bestanden |
| Wiedergabe-Fehlschlag sichtbar im Protokoll (optional) | `LogProtocolTests.LoggingEnabled_PlaybackFailureSubFlow_WritesErrorEntry` (`LogProtocolTests.cs:330`; prüft `[ERROR]`-Eintrag "Wiedergabe" ohne `access_token`) | Bestanden |
| Online-/Offline-Wechsel sichtbar im Protokoll | kein Testfall – laut Plan nicht E2E-automatisierbar (FlaUI kann die Netzverfügbarkeit nicht umschalten); Nachweis per `LogProtocolContractTests.ConnectivityMonitor_LogsAvailabilityChange` (bestanden) | Nicht ausgeführt (nicht geplant) |

## Zusammenfassung

- Gesamt: 1336 (1280 Unit-/Contract-Tests + 56 E2E-Tests)
- Bestanden: 1336
- Fehlgeschlagen: 0
- Übersprungen: 0

## Testabdeckung

**Abdeckung:** 87,2 % Zeilenabdeckung (6653/7631 Zeilen; Zweigabdeckung 74,4 %) – `VideoWebPlayer.Maui.Tests` via XPlat Code Coverage auf die referenzierten Projekte (`VideoPlayer.Mvu`, `VideoPlayer.Storage`, `VideoPlayer.ApiClient`, `VideoWebPlayer.Client`, `msTools.DeveloperApi`). `VideoPlayer.Maui` und `VideoWebPlayer.Maui` sind im Unit-Testprojekt nicht referenziert (Nachweis dort per Contract-Tests und E2E).

| Datei | Abdeckung |
|-------|-----------|
| `VideoWebPlayer.Client/Models/ImpersonateRequest.cs` | 0,0 % |
| `VideoWebPlayer.Client/Models/AuthorizationToken.cs` | 0,0 % |
| `VideoWebPlayer.Client/VideoWebPlayerClient.cs` | 0,0 % |
| `VideoWebPlayer.Client/Models/DtoRecentEntry.cs` | 0,0 % |
| `VideoWebPlayer.Client/Models/AuthenticationRequest.cs` | 0,0 % |
| `VideoWebPlayer.Client/Models/MediaEntryDto.cs` | 0,0 % |
| `VideoPlayer.ApiClient/Discovery/MdnsServerDiscovery.cs` | 0,0 % |
| `VideoWebPlayer.Client/Models/FavoriteEntry.cs` | 0,0 % |
| `VideoWebPlayer.Client/Models/DtoSource.cs` | 0,0 % |
| `VideoWebPlayer.Client/Models/DtoMovie.cs` | 37,8 % |
| `VideoPlayer.Mvu/DownloadListItem.cs` | 60,0 % |
| `VideoPlayer.ApiClient/ClientGate.cs` | 63,6 % |
| `VideoPlayer.Mvu/PlaylistDetailsState.cs` | 65,0 % |
| `VideoPlayer.Mvu/ProfilePageState.cs` | 70,0 % |
| `VideoPlayer.Mvu/MediaTypeNames.cs` | 72,2 % |
| `VideoPlayer.Mvu/SourceItem.cs` | 75,0 % |
| `VideoPlayer.ApiClient/Discovery/DiscoveredServer.cs` | 75,0 % |
| `VideoPlayer.ApiClient/Discovery/ServerDiscoveryService.cs` | 77,3 % |
| `VideoPlayer.ApiClient/Models/MediaEntry.cs` | 78,4 % |
| `VideoPlayer.Mvu/ServerEventsService.cs` | 79,8 % |

## Fehlende Tests

Quelle: `Coverage-Daten`

- `VideoPlayer.ApiClient/Discovery/MdnsServerDiscovery.cs` – 0 % Abdeckung
- `VideoWebPlayer.Client/VideoWebPlayerClient.cs` – 0 % Abdeckung
- `VideoWebPlayer.Client/Models/AuthenticationRequest.cs` – 0 % Abdeckung
- `VideoWebPlayer.Client/Models/AuthorizationToken.cs` – 0 % Abdeckung
- `VideoWebPlayer.Client/Models/DtoRecentEntry.cs` – 0 % Abdeckung
- `VideoWebPlayer.Client/Models/DtoSource.cs` – 0 % Abdeckung
- `VideoWebPlayer.Client/Models/FavoriteEntry.cs` – 0 % Abdeckung
- `VideoWebPlayer.Client/Models/ImpersonateRequest.cs` – 0 % Abdeckung
- `VideoWebPlayer.Client/Models/MediaEntryDto.cs` – 0 % Abdeckung
