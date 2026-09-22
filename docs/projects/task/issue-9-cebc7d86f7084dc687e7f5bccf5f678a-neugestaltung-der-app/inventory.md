# Bestandsaufnahme: Neugestaltung der App „VideoPlayer" (Issue #9)

Analysiert wurden die Projekte `VideoWebPlayer.Client`, `VideoWebPlayer.Maui` und `VideoWebPlayer.Maui.Tests` sowie Build-/CI-Konfiguration, bezogen auf die Anforderung `requirement.md` (vollständige Neuentwicklung einer .NET-MAUI-App als Client zum „VideoPlayer"-Server; Bestandscode dient nur als Referenz).

## Zusammenfassung

- **Drei Projekte** in `VideoPlayer.App.sln`: plattformneutraler API-Client (`net10.0`), MAUI-App (`net10.0-android/-ios/-maccatalyst/-windows10.0.19041.0`, MVVM mit `*ViewModel`-Klassen — **nicht** MVU) und xUnit-v3-Testprojekt (`net10.0`, referenziert nur den Client).
- **Als Referenz/Wiederverwendung geeignet:**
  - `VideoWebPlayer.Client` mit vollständigem API-Client `VideoWebPlayerClient` (alle in der Anforderung genannten Endpunkte, 401-Handling mit Token-Refresh-Hook, Fortschritts-Coalescing) und allen DTOs inkl. polymorpher `DtoMediaEntry`-Hierarchie — Details: [client-api.md](inventory/client-api.md).
  - `MauiClientConfiguration` (Client-Gate `X-API-Key` aus `MauiClientApiToken`/`VIDEOWEBPLAYER_MAUI_API_TOKEN`) und `MauiVideoWebPlayerClient` (silent re-login, Token-Persistenz) — [client-api.md](inventory/client-api.md).
  - Discovery-Mechanismen in `SettingsService`: mDNS via `Zeroconf` (`_http._tcp.local.`) und UDP-Broadcast `VIDEOWEBPLAYER_DISCOVERY`/`VIDEOWEBPLAYER_SERVER:` auf Port 5001 — [services.md](inventory/services.md).
  - `ProfileManager` (Profil-Isolation via `VIDEOWEBPLAYER_PROFILE`), `ClientDatabase` (`sqlite-net-pcl`, Singleton, Tabellen `Downloads`/`CarouselCache`), `ElementCacheService`, `SignalRService` (`/hubs/mediaupdate` mit Auto-Reconnect) + `NotificationEventService`-Pub/Sub, `WatchlistDownloadCoordinatorService` als Referenz für automatische Downloads — [services.md](inventory/services.md), [models.md](inventory/models.md), [interfaces.md](inventory/interfaces.md).
  - Build-/CI-Setup: csproj-Konfiguration (XAML-SourceGen, `MauiClientApiToken`-Injection, `WindowsPackageType=None`), `Set-Version.ps1`, drei GitHub-Workflows (PR-Checks mit Lint-Gate, Staging-/Main-Release mit semantic-release) — [build-environment.md](inventory/build-environment.md).
- **In der Anforderung gefordert, im Bestand nicht vorhanden:** MVU-Architektur (Bestand ist MVVM), Picture-in-Picture (`MediaElement` nur eingebettet in `MediaBannerPlayer`, kein PiP-Code), Online/Offline-Erkennung (keine `Connectivity`-Nutzung), Multi-Server-Verwaltung (`KnownServers`, nur eine `ServerAddress`), Offline-Outbox für Fortschrittsmeldungen (`ReportPlaybackProgressAsync` verliert Meldungen bei Fehler), Backup-Klassifikation/-Ausschluss (alles liegt in `FileSystem.AppDataDirectory`; kein `IsExcludedFromBackup`/`CacheDirectory`-Einsatz), Retry-Strategie, parallele Downloads (`DownloadQueue` arbeitet sequenziell, ohne Resume), Speicherplatzkontingent, Auto-Löschung gesehener Episoden, dedizierte Seiten „Favoriten"/„Downloads"/„Profil" und „Server wechseln" in den Einstellungen — [services.md](inventory/services.md), [ui-navigation.md](inventory/ui-navigation.md).
- **Test-Ausgangszustand:** `dotnet test VideoWebPlayer.Maui.Tests` (Release) — **8 Tests, alle erfolgreich**, 0 fehlgeschlagen, 0 übersprungen; MAUI-Compile-Nachweise für Windows und Android erfolgreich. Details und Log-Dateien: [tests.md](inventory/tests.md). Bekannte Testlücke: Keine Tests für die neuen Anforderungsbereiche (Backup, Reconnect, Offline-Sync, Download-Verwaltung, Serververwaltung); MAUI-Code ist nur über Quelltext-Contract-Tests erreichbar.
- **Umgebung:** Windows-Rechner mit .NET SDK 10.0.401; Workloads `android`, `ios`, `maccatalyst`, `maui-windows` installiert; Windows- und Android-Targets bauen lokal, iOS/MacCatalyst nicht (macOS/Xcode erforderlich, CI nutzt `macos-latest`) — [build-environment.md](inventory/build-environment.md).

## Details

- [API-Client-Schicht (`VideoWebPlayer.Client`, DTOs, Client-Gate)](inventory/client-api.md)
- [Services / Logik (`VideoWebPlayer.Maui/Services/`, Events, Startlogik)](inventory/services.md)
- [Lokale Datenmodelle und Enums](inventory/models.md)
- [Interfaces](inventory/interfaces.md)
- [UI / Navigation (Pages, Komponenten, ViewModels, Plattformen)](inventory/ui-navigation.md)
- [Build-, Projekt- und CI-Konfiguration sowie Umgebung](inventory/build-environment.md)
- [Tests — Bestand und Ausgangszustand](inventory/tests.md)
