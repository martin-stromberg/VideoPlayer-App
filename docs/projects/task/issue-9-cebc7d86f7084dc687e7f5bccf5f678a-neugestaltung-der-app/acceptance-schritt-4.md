# Abnahmeprüfung – Entwicklungsschritt 4

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

- Keine.

## Hinweise

Geprüft auf Branch `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-4-serververwaltung-erststart` (HEAD: `e39a8f0`), Diff gegen `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app` (65 Dateien, +3957/−48).

### Persistente Serverliste und zuletzt verbundener Server (backup-pflichtig)

- `VideoPlayer.Storage/KnownServersService.cs` verwaltet die Liste bekannter Server (`known_servers`, `Entities/KnownServerEntry.cs` mit Unique-Index auf `Address`) und den zuletzt verbundenen Server (`backup_state`-Schlüssel `LastConnectedServer`, `Entities/BackupStateEntry.cs:12`) ausschließlich über die `BackupDatabase` (`videoplayer.db3` im `AppStorageLayout.BackupDirectory` — iOS `AppDataDirectory`, Windows `%APPDATA%`-Roaming; `AppStorageLayout.cs:36-40`). Upsert-Logik ohne Duplikate, `LastUsedAtUtc`-Sortierung (`KnownServersService.cs:29-101`).
- Einmalige Migration des Schritt-3-Schlüssels `ServerAddress` aus `ISettingsStore` inkl. Schlüssel-Entfernung (`KnownServersService.cs:110-145`), aufgerufen beim Laden der Serververwaltung (`ShellAppEffectHandler.cs:113`).
- `ServerAddress.Normalize`/`TryNormalize` (`VideoPlayer.Storage/ServerAddress.cs`) erzeugt die kanonische Form `scheme://host[:port][/pfad]/` (Schema-Ergänzung, Host-Kleinschreibung via `Uri`, Fremdschemata wie `ftp://` werden abgelehnt).

### Automatische Verwendung beim App-Start

- `StartupRequestedMessage` emittiert `[InitializeStorageEffect, LoadSettingsEffect, LoadServersEffect]` (`AppUpdate.cs:43-55`). `ServersLoadedMessage` mit aktiver Adresse stößt bei laufendem Start `CheckServerHealthEffect` an → `Home`; ohne hinterlegten Server → `CurrentPage = ServerSetup` + `NavigateToPageEffect` (`AppUpdate.cs:72-101`).
- `ServerAddressProvider` löst: zuletzt verbundener Server → `VIDEOPLAYER_SERVER_URL` → IIS-Default als reiner Auflösungs-Fallback (`ServerAddressProvider.cs:27-43`); der IIS-Default gilt nicht als „hinterlegt" (Erststart-Erkennung über `GetConfiguredServerAddressAsync` → `null`).

### Erststart-Einrichtungsdialog mit zwei Wegen

- `Pages/ServerSetupPage.xaml` bietet manuelle Eingabe (`AddressEntry` + `ConnectButton`, Zeilen 62-70) und Netzwerk-Erkennung (`DiscoverButton` „Server im Netzwerk suchen" + `DiscoveredServersView`, Zeilen 39-60) sowie die Liste bekannter Server und den aktiven Server. Alle Controls mit `AutomationId`.
- Discovery protokollkonform implementiert: `DiscoveryProtocol.cs` — UDP-Port `5001`, Abfrage `VIDEOWEBPLAYER_DISCOVERY`, Antwortpräfix `VIDEOWEBPLAYER_SERVER:`, mDNS-Typ `_http._tcp.local.` (identisch zum Altbestand `VideoWebPlayer.Maui/Services/SettingsService.cs:170-215`). `UdpServerDiscovery` sendet Broadcast an `255.255.255.255:5001` mit Empfangsfenster; `MdnsServerDiscovery` nutzt Zeroconf 3.7.16; `ServerDiscoveryService` führt beide Wege parallel aus, merged und dedupliziert mit Fehlerisolation.
- Erststart-Navigation: `ShellAppEffectHandler.NavigateAsync` setzt `ServerSetupPage` als Window-Root, wenn die Shell noch nicht existiert (`ShellAppEffectHandler.cs:211-222`).

### Speicherung nur nach erfolgreicher Verbindungsprüfung

- Einheitlicher Prüfpfad: `ServerAddressSubmittedMessage`, `DiscoveredServerSelectedMessage` und `ServerSwitchRequestedMessage` führen über `BeginServerCheck` zu `CheckCandidateServerEffect` → `ServerHealthProbe.CheckAsync(Uri)` → `VideoPlayerApiClient.CheckHealthAsync` (`api/health`). Nur `ServerCheckCompletedMessage(Success: true)` erzeugt `PersistActiveServerEffect` + Navigation nach `Home` (`AppUpdate.cs:151-196`); Misserfolg setzt `ServerSetupError` ohne Persistenz. Die Auswahl gefundener Server erfolgt über `OnDiscoveredSelectionChanged` (`ServerSetupPage.xaml.cs:77-84`).

### „Server wechseln" in den Einstellungen

- Dezenter Eintrag: normaler `Button` „Server wechseln" (`AutomationId="ServerSwitchButton"`) im Abschnitt „Server" am Ende der `SettingsPage` (`SettingsPage.xaml:76-82`); öffnet die Verwaltungsseite als geschobene Shell-Route `serversetup` — ohne FlyoutItem/Menüeintrag (`AppShell.xaml.cs:16`, `AppShell.xaml` unverändert).
- Die Seite listet aktiven und bekannte Server, erlaubt das Hinzufügen (Discovery oder manuell) und den Wechsel; `AppSettings` bleiben beim Wechsel unverändert (nachgewiesen durch `Update_ServerCheckCompleted_Success_PersistsAndNavigatesHome`, `AppUpdateTests_ServerManagement.cs:135-168` — `Assert.Equal(settings, transition.Model.Settings)`; Persistenz schreibt nur `known_servers`/`backup_state`).

### Discovery-Verifikation (serverseitig) dokumentiert, Server-Issues gestellt

- `docs/features/task/…-schritt-4-serververwaltung-erststart/inventory/discovery-verification.md` dokumentiert: UDP-Port 5001 wird serverseitig beantwortet (`UdpDiscoveryListener`), meldet aber `http://localhost:5000` (konfigurierte Adresse statt erreichbarer Basis-URL) → Issue [martin-stromberg/VideoPlayer#224](https://github.com/martin-stromberg/VideoPlayer/issues/224); mDNS-Announce fehlt vollständig → Issue [#223](https://github.com/martin-stromberg/VideoPlayer/issues/223). Live-Prüfskript: `inventory/discovery-check.ps1`. Lokaler IIS antwortet derzeit mit HTTP 500 — als Beobachtung dokumentiert.
- Kein Workaround in der App: die Loopback→Responder-Deutung in `UdpServerDiscovery.ResolveAnnouncedAddress` (Zeilen 100-121) ist als protokollkonforme Interpretation dokumentiert und kommentiert verweisen beide Probes auf die Server-Issues; Fehltreffer werden ohnehin per `CheckHealthAsync` ausgefiltert. Auch `docs/help/serververwaltung-erststart.md:82-93` und `changes.log` verweisen auf #223/#224.

### Testnachweis

- `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release` im Prüfschritt ausgeführt: **229/229 Tests bestanden** (0 fehlgeschlagen, 0 übersprungen, ~4,8 s).
- Abdeckung: `ServerAddressTests` (Normalisierung/Ablehnung), `KnownServersServiceTests` (reale SQLite-Ablage: Upsert, Sortierung, `LastConnectedServer`, Legacy-Migration, Backup-/Cache-Trennung der Tabellen), `DiscoveryProtocolTests`, `UdpServerDiscoveryTests` (echter UDP-Loopback-Roundtrip mit Responder, Fremd-Datagramme, Timeout, Loopback-Normalisierung), `ServerDiscoveryServiceTests` (Merge/Dedupe/Fehlerisolation), `AppUpdateTests_ServerManagement` + `AppUpdateTests_Startup` (alle MVU-Übergänge inkl. Erststart-Verzweigung und Einstellungs-Erhalt), `ServerSetupContractTests` (Controls/AutomationIds, Route ohne Menüeintrag, DI, Protokoll-Konstanten, Backup-Ablage).
- Implementierungslauf zusätzlich dokumentiert: Windows-Build warnungsfrei (`windows-build.log`), 15-s-Smoke-Run mit Einrichtungsdialog (`test-results.md`), Reviews ohne offene Befunde (`review.md`, `review-code.md`, `review-usability.md`).

### Einschränkungen

- Discovery ist erst nach Betätigen von „Server im Netzwerk suchen" aktiv (einer der zwei geforderten Wege; keine automatische Suche beim Öffnen — vom Umsetzungsplan so festgelegt und mit der Anforderung vereinbar).
- Gegen die aktuelle lokale Serverinstallation liefern mDNS/UDP praktisch keine verwertbaren Treffer (Server-Issues #223/#224); die manuelle Eingabe bleibt bis zur Server-Korrektur der verlässliche Weg.
- `MdnsServerDiscovery` ist auf Testsystemen ohne announced Server nicht laufzeitabgedeckt (Zeroconf nicht mockbar); Aggregationslogik vollständig getestet.
- `VIDEOPLAYER_SERVER_URL` zählt als konfigurierte Adresse und unterdrückt den Erststart-Dialog (dokumentierter Entwicklungs-Fallback, `docs/help/serververwaltung-erststart.md:56-60`).
- Ein UI-Laufzeittest auf iOS ist unter Windows nicht möglich; UI-Verdrahtung ist über Contract-Tests und MVU-Update-Tests abgesichert (gleiche Nachweisform wie in Schritt 3).
