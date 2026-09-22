# VideoWebPlayer MAUI App

Dieses Repository enthält die ausgelagerte .NET-MAUI-App und ihre Tests.

## Inhalt

- `VideoWebPlayer.Maui/` - bisherige mobile App (Altbestand, wird schrittweise durch `VideoPlayer.Maui` abgelöst).
- `VideoWebPlayer.Maui.Tests/` - tests ohne Rückreferenz auf das Webprojekt.
- `VideoWebPlayer.Client/` - kopierte Übergabeschnittstelle aus dem Web-Repository (Altbestand, wird im Rahmen der Neugestaltung entfernt).
- `VideoPlayer.ApiClient/` - neu implementierter, plattformneutraler Zugriff auf die öffentliche Server-API sowie Netzwerk-Erkennung von Servern (mDNS/UDP, Namespace `Discovery`; Dokumentation: `docs/help/api-client/`).
- `VideoPlayer.Mvu/` - plattformneutrale MVU-Schicht der neuen App (Modell, Messages, Update, Effekte; eigener schlanker MVU-Kreislauf ohne externes Framework) inklusive `AuthSessionService` (Anmeldung, stille Token-Erneuerung, Verbindungszustände), `ServerEventsService`/`ServerSyncService`/`ProgressReportService` (Ereignisverbindung, Synchronisation, Fortschrittsmeldung), `INetworkStatusMonitor`-Abstraktion, `HomeListService` (Startseiten-Listen aus der Cache-Ablage) sowie `SourceBrowserService`/`MediaDetailsService`/`FavoritesService` (Quellen-Browsing, Medien-Details, Favoriten-Umschaltung und -Entfernung), `ProfileInfoService` (Benutzer-/Profil-Anzeige aus gespeicherter Kennung, Token-Claims und `VIDEOPLAYER_PROFILE`) und `PlaybackSourceService` (Wiedergabequelle: fertiger Download aus dem Manifest oder Server-Stream samt Fortsetzungsposition, im Offline-Modus ohne Serverzugriff), `DownloadService`/`AutoDownloadService` (persistente Download-Aufträge im Manifest, parallele Transfers mit GB-Kontingent und WLAN-Bedingung, automatische Wiederaufnahme, Weiterschauen-Vorlauf) sowie `OfflineContinueWatchingService`/`ShowDetailsCache` (Offline-„Weiterschauen" ausschließlich aus abgeschlossenen Downloads mit vorhandener Datei, Platzhalter fehlender Folge-Episoden, gecachte Serienstruktur, Offline-Snapshots der Detailseiten).
- `VideoPlayer.Storage/` - plattformneutrale lokale Datenhaltung der neuen App (Einstellungsmodell und -dienst, Geheimnis-Ablage, Profil-Isolation, Backup-/NoBackup-Ablageorte, SQLite-Datenbanken, Serververwaltung `KnownServersService`, Offline-Daten-Prüfung, persistente Fortschritts-Warteschlange `PendingProgressSyncService`, wiederherstellende Initialisierung beschädigter Datenbanken mit Quarantäne der Altdateien).
- `VideoPlayer.Maui/` - neue MAUI-App auf MVU-Basis (Seitenmenü-Navigation, ausgebaute Einstellungsseite, Serververwaltung mit Erststart-Einrichtung, Verbindungs-Workflow mit Anmeldemaske und Offline-Modus, Netzwerkstatus-Erkennung mit automatischer Wiederverbindung und SignalR-Ereignisverbindung zum Server, Startseite mit den gecachten Medienlisten „Weiterschauen"/„Favoriten"/„Neu im Programm", Quellen als dynamische Menüeinträge je freigegebener Quelle mit Icon unter der Sektionsüberschrift „Quellen" direkt im Seitenmenü (Statuszeilen für Laden und leere Freigabe, Neuversuch-Eintrag bei Ladefehlern, offline ausgeblendet; die Auswahl öffnet direkt die Titelübersicht der Quelle), Quellen-Browsing mit Genre-Filter/Suche/seitenweisem Nachladen, Medien-Detailseiten für Filme, Kollektionen und Serien mit Staffeln/Episoden inklusive Favoriten-Markierung, Favoriten-Seite des Server-Benutzers mit Entfernen-Aktion und Offline-Cache sowie Profil-Seite mit Benutzeranzeige und Abmeldung, Wiedergabeseite (`MediaElement`, Server-Stream/lokale Datei, Fortsetzungsposition, „angesehen"-Schwelle, Auto-Fortsetzung, Fehler-Rückfall auf die nächste Episode, native Transportsteuerung in der Vollansicht unmittelbar bedienbar, Wiedergabeseite beliebig oft hintereinander ohne Absturz startbar — Modell-Abo nur solange sichtbar, native Elementzugriffe auf dem UI-Thread) mit Picture-in-Picture — Windows über kompaktes Overlay-Fenster, iOS über den nativen PiP der Transportsteuerung — sowie die Downloads-Seite (Auftragsliste mit Status/Fortschritt, Abbrechen/Wiederholen/Löschen, Kontingent-Anzeige, offline nutzbar, flüssige Fortschrittsanzeige über granularen In-place-Abgleich ohne Neuaufbau der Liste) und den vollständigen Offlinebetrieb (Startseite aus lokalen Downloads mit Platzhaltern fehlender Episoden, lokale Wiedergabe, Offline-Snapshots, Re-Anmeldung nach Backup-Wiederherstellung, Wiederherstellung beschädigter lokaler Ablagen mit Hinweisbanner); das Erscheinungsbild folgt zentralen Design-Tokens der Webanwendung (dunkles Schema, `Vp*`-Ressourcen in `Resources/Styles/`); Dokumentation: `docs/help/app-grundgeruest.md`, `docs/help/einstellungen-datenhaltung.md`, `docs/help/serververwaltung-erststart.md`, `docs/help/verbindung-anmeldung.md`, `docs/help/netzwerk-synchronisation.md`, `docs/help/startseite-medienlisten.md`, `docs/help/quellen-browsing-details.md`, `docs/help/favoriten-profil.md`, `docs/help/wiedergabe-pip.md`, `docs/help/downloads.md`, `docs/help/offline-wiederherstellung.md`, `docs/help/erscheinungsbild.md`).
- `VideoPlayer.App.sln` - eigene Solution dieses Repositorys.
- `LICENSE` - PolyForm Noncommercial License 1.0.0.

## Voraussetzungen

- .NET 10 SDK.
- Für mobile Builds: passende .NET-MAUI-Workload.
- Windows-Ziel: Windows App SDK/Windows-Zielplattform.
- Android-Ziel: Android SDK.
- iOS/MacCatalyst: macOS mit Xcode.

Linux kann das Testprojekt und das gemeinsame Client-Projekt bauen, aber keine iOS-, MacCatalyst- oder Windows-MAUI-Ziele.

## Restore, Build und Tests

```bash
dotnet restore VideoPlayer.App.sln
dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj
```

## Branching und CI/CD

- `staging` ist der Integrationsbranch. Pull Requests gegen `staging` müssen Tests und Linter bestehen.
- `main` darf ausschließlich von `staging` über Pull Requests aktualisiert werden.
- Nach einem Merge auf `staging` entsteht ein Draft-PR gegen `main` und ein Prerelease mit RC-Version.
- Nach einem Merge auf `main` entsteht ein Semantic-Release sowie ein Back-Merge-PR auf `staging`.
- Details befinden sich in `docs/help/staging-branch.md`.

Windows:

```powershell
dotnet build .\VideoWebPlayer.Maui\VideoWebPlayer.Maui.csproj -f net10.0-windows10.0.19041.0
dotnet build .\VideoPlayer.Maui\VideoPlayer.Maui.csproj -f net10.0-windows10.0.19041.0
```

Android:

```bash
dotnet build VideoWebPlayer.Maui/VideoWebPlayer.Maui.csproj -f net10.0-android
dotnet build VideoPlayer.Maui/VideoPlayer.Maui.csproj -f net10.0-android
```

## Web-API-Vertrag

Das Web-Repository besitzt die verbindliche API-Dokumentation unter `docs/API.md`. Für produktive Änderungen an Endpunkten, DTOs oder Authentifizierung muss das Web-Repository zuerst den Vertrag aktualisieren und testen.

## Secrets und Client-Gate

Der Platzhalter `<MAUI_CLIENT_API_TOKEN>` im Client ist kein produktiv verwendbarer Wert. Er markiert den Client-Gate-Wert für initiale Health- und Login-Aufrufe. Dieser Wert ist kein Ersatz für Benutzer-Authentifizierung und kann in einem ausgelieferten Client grundsätzlich ausgelesen werden; der korrespondierende Backendwert ist dennoch als sensibler Konfigurationswert zu schützen. Die eigentliche Benutzerautorisierung erfolgt über JWTs nach erfolgreicher Anmeldung.

Produktive Werte werden zentral über `VideoWebPlayer.Maui/Services/MauiClientConfiguration.cs` gelesen. Unterstützte Quellen sind:

- MSBuild: `dotnet build VideoWebPlayer.Maui/VideoWebPlayer.Maui.csproj -p:MauiClientApiToken="<MAUI_CLIENT_API_TOKEN>"`
- Runtime-Umgebung: `VIDEOWEBPLAYER_MAUI_API_TOKEN`

Der neue `VideoPlayer.ApiClient` löst den Schlüssel plattformneutral über `ClientGate.Resolve()` auf: Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN`, dann die MSBuild-Eigenschaft `VideoPlayerApiToken` (Assembly-Metadata `VideoPlayer.ApiClient.ApiToken`), sonst der Platzhalter — oder explizit über `VideoPlayerApiClientOptions.ApiKey`.

Der Wert muss backendseitig zu `Jwt:ApiToken:Maui` passen. Echte JWT-Schlüssel, Benutzerpasswörter und produktive API-Tokens gehören nicht in dieses Repository. Benutzerpasswörter werden im Client über `SecureStorage` abgelegt; `Preferences` enthält nur nicht geheime App-Einstellungen wie Serveradresse und Benutzername.

## Lizenz

Dieses Repository steht unter der **PolyForm Noncommercial License 1.0.0**. Kommerzielle Nutzung erfordert eine vorherige schriftliche Zustimmung.

Kontakt für kommerzielle Lizenzanfragen: mstromberg84+videoplayer@gmail.com
