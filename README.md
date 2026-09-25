# VideoWebPlayer MAUI App

Dieses Repository enthält die ausgelagerte .NET-MAUI-App und ihre Tests.

## Inhalt

- `VideoWebPlayer.Maui/` - bisherige mobile App (Altbestand, wird schrittweise durch `VideoPlayer.Maui` abgelöst).
- `VideoWebPlayer.Maui.Tests/` - tests ohne Rückreferenz auf das Webprojekt.
- `VideoWebPlayer.Client/` - kopierte Übergabeschnittstelle aus dem Web-Repository (Altbestand, wird im Rahmen der Neugestaltung entfernt).
- `VideoPlayer.ApiClient/` - neu implementierter, plattformneutraler Zugriff auf die öffentliche Server-API, Geräte-Pairing und -Onboarding (`DevicePairingClient` für die öffentlichen Endpunkte `api/pairing/exchange` und `api/pairing/bootstrap`, `PairingCrypto` für den ECDH-nistP256-Schlüsselaustausch und die AES-256-GCM-Entschlüsselung; `RefreshSessionAsync`/`LogoutSessionAsync` für `api/auth/refresh`/`api/auth/logout`) sowie Netzwerk-Erkennung von Servern (mDNS/UDP, Namespace `Discovery`; Dokumentation: `docs/help/api-client/`).
- `VideoPlayer.Mvu/` - plattformneutrale MVU-Schicht der neuen App (Modell, Messages, Update, Effekte; eigener schlanker MVU-Kreislauf ohne externes Framework) inklusive `AuthSessionService` (Anmeldung, stille Token-Erneuerung, Verbindungszustände), `ServerEventsService`/`ServerSyncService`/`ProgressReportService` (Ereignisverbindung, Synchronisation, Fortschrittsmeldung), `INetworkStatusMonitor`-Abstraktion, `HomeListService` (Startseiten-Listen aus der Cache-Ablage) sowie `SourceBrowserService`/`MediaDetailsService`/`FavoritesService` (Quellen-Browsing, Medien-Details, Favoriten-Umschaltung und -Entfernung), `ProfileInfoService` (Benutzer-/Profil-Anzeige aus gespeicherter Kennung, Token-Claims und `VIDEOPLAYER_PROFILE`) und `PlaybackSourceService` (Wiedergabequelle: fertiger Download aus dem Manifest oder Server-Stream samt Fortsetzungsposition, im Offline-Modus ohne Serverzugriff), `DownloadService`/`AutoDownloadService` (persistente Download-Aufträge im Manifest, parallele Transfers mit GB-Kontingent und WLAN-Bedingung, automatische Wiederaufnahme, Weiterschauen-Vorlauf) sowie `OfflineContinueWatchingService`/`ShowDetailsCache` (Offline-„Weiterschauen" ausschließlich aus abgeschlossenen Downloads mit vorhandener Datei, Platzhalter fehlender Folge-Episoden, gecachte Serienstruktur, Offline-Snapshots der Detailseiten) sowie `DeviceTokenService` (gerätegebundene Pairing-Tokens: Laden aus der sicheren Ablage, Einbindung als Quelle in `ClientGate`, Gate-Probe und Widerruf-Erkennung mit Re-Pairing-Fluss).
- `VideoPlayer.Storage/` - plattformneutrale lokale Datenhaltung der neuen App (Einstellungsmodell und -dienst, Geheimnis-Ablage für Zugangsdaten, Anmelde-, Refresh- und gerätegebundene Pairing-Tokens, Profil-Isolation, Backup-/NoBackup-Ablageorte, SQLite-Datenbanken, Serververwaltung `KnownServersService`, Offline-Daten-Prüfung, persistente Fortschritts-Warteschlange `PendingProgressSyncService`, wiederherstellende Initialisierung beschädigter Datenbanken mit Quarantäne der Altdateien).
- `VideoPlayer.Maui/` - neue MAUI-App auf MVU-Basis (Seitenmenü-Navigation, ausgebaute Einstellungsseite, Serververwaltung mit Erststart-Einrichtung, QR-/Ticket-Onboarding per `api/pairing/bootstrap` (Kamera-Scan unter iOS/Android, Ticket-Adresse oder Kurzcode manuell) und Geräte-Pairing per Einmal-Code auf der Serverseite, Verbindungs-Workflow mit Anmeldemaske und Offline-Modus, Netzwerkstatus-Erkennung mit automatischer Wiederverbindung und SignalR-Ereignisverbindung zum Server, Startseite mit den gecachten Medienlisten „Weiterschauen"/„Favoriten"/„Neu im Programm" und dem Hero-Kopfbereich mit servergeneriertem Hintergrundbild nach Web-Vorbild (ausblendbar über die positiv formulierte, standardmäßig aktivierte Einstellung „Kopfbereich anzeigen"), Quellen als dynamische Menüeinträge je freigegebener Quelle mit Icon unter der Sektionsüberschrift „Quellen" direkt im Seitenmenü (Statuszeilen für Laden und leere Freigabe, Neuversuch-Eintrag bei Ladefehlern, offline ausgeblendet; die Auswahl öffnet direkt die Titelübersicht der Quelle), Quellen-Browsing mit Genre-Filter/Suche/seitenweisem Nachladen, Medien-Detailseiten für Filme, Kollektionen und Serien mit Staffeln/Episoden inklusive Favoriten-Markierung und vollbreitem Kopfbereich mit Hintergrundgrafik nach Web-Vorbild (Episoden-Still bzw. Banner/Fanart/Poster, geschichtete Dunkelgradienten, Titel/Meta/Handlung unten links, Wechsel auf das Still der gewählten Episode) sowie dem Angesehen-Symbol — rundes Augen-Badge auf den Stills gesehener Episoden nach Web-Vorbild, in der Sitzung sofort aktualisiert, auch im Offline-Snapshot, Favoriten-Seite des Server-Benutzers mit Entfernen-Aktion und Offline-Cache sowie Profil-Seite mit Benutzeranzeige und Abmeldung, Wiedergabeseite (`MediaElement`, Server-Stream/lokale Datei, Fortsetzungsposition, „angesehen"-Schwelle, Auto-Fortsetzung, Fehler-Rückfall auf die nächste Episode, native Transportsteuerung in der Vollansicht unmittelbar bedienbar, Wiedergabeseite beliebig oft hintereinander ohne Absturz startbar — Modell-Abo nur solange sichtbar, native Elementzugriffe auf dem UI-Thread) mit Picture-in-Picture — Windows über kompaktes Overlay-Fenster, iOS über den nativen PiP der Transportsteuerung — sowie die Downloads-Seite (Auftragsliste mit Status/Fortschritt, Abbrechen/Wiederholen/Löschen, Kontingent-Anzeige, offline nutzbar, flüssige Fortschrittsanzeige über granularen In-place-Abgleich ohne Neuaufbau der Liste) und den vollständigen Offlinebetrieb (Startseite aus lokalen Downloads mit Platzhaltern fehlender Episoden, lokale Wiedergabe, Offline-Snapshots, Re-Anmeldung nach Backup-Wiederherstellung, Wiederherstellung beschädigter lokaler Ablagen mit Hinweisbanner); das Erscheinungsbild folgt zentralen Design-Tokens der Webanwendung (dunkles Schema, `Vp*`-Ressourcen in `Resources/Styles/`); Dokumentation: `docs/help/app-grundgeruest.md`, `docs/help/einstellungen-datenhaltung.md`, `docs/help/serververwaltung-erststart.md`, `docs/help/verbindung-anmeldung.md`, `docs/help/netzwerk-synchronisation.md`, `docs/help/startseite-medienlisten.md`, `docs/help/quellen-browsing-details.md`, `docs/help/favoriten-profil.md`, `docs/help/wiedergabe-pip.md`, `docs/help/downloads.md`, `docs/help/offline-wiederherstellung.md`, `docs/help/erscheinungsbild.md`).
- `VideoPlayer.E2E.Tests/` - Windows-End-to-End-Tests (xUnit + FlaUI.UIA3): startet die echte `VideoPlayer.Maui.exe` (Debug) mit isoliertem Profil, steuert die Oberfläche über UI-Automation und prüft den App-Zustand über die Developer-API; Backend ist ein lokaler Mock-Server (Health/Pairing/Bootstrap/Login/Refresh/Logout/Listen). Doku: `docs/help/e2e-tests.md`.
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

## Developer-API (nur Debug/Opt-in)

Der Debug-Build von `VideoPlayer.Maui` startet einen lokalen Mini-HTTP-Server auf `127.0.0.1` mit systemvergebenem Port (`devapi.json` im Programmverzeichnis): `GET /model` (App-Zustand), `GET /tree` (Elementbaum inkl. CollectionView-Items), `POST /invoke` (inkl. `itemIndex` zur koordinatenfreien CollectionView-Auswahl) und `POST /set-text` zur UI-Automatisierung. Release-Builds enthalten die API nicht; Opt-in über das Define `DEV_API`. Details: `docs/help/developer-api.md`.

## Windows-E2E-Tests (FlaUI)

`dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj` — nur unter Windows mit Desktop-Session; baut die App als Debug, startet je Test einen eigenen Exe-Prozess mit isoliertem `VIDEOPLAYER_PROFILE` und spielt Server-Einrichtung, Pairing, QR-/Ticket-Bootstrap, Anmeldung, Sitzungserneuerung/-persistenz und Serverwechsel gegen einen lokalen Mock-Server durch (17 Tests). Details: `docs/help/e2e-tests.md`.

## Web-API-Vertrag

Das Web-Repository besitzt die verbindliche API-Dokumentation unter `docs/API.md`. Für produktive Änderungen an Endpunkten, DTOs oder Authentifizierung muss das Web-Repository zuerst den Vertrag aktualisieren und testen.

## Secrets und Client-Gate

Der Server schützt die Client-API über ein Client-Gate (`X-API-Key`-Header, Scope `MauiOnly`). Das komfortabelste Verfahren ist das **QR-/Ticket-Onboarding**: Die App löst ein Ticket aus dem Kopplungs-QR-Code der Server-Verwaltung (oder den 8-stelligen Kurzcode) über den öffentlichen Endpunkt `POST api/pairing/bootstrap` ein und erhält dabei in einem Schritt Geräte-Token, Anmelde-Token und Refresh-Token — keine Anmeldemaske nötig; die Sitzung erneuert sich anschließend per `POST api/auth/refresh` (Rotierung) und das Abmelden widerruft den Refresh-Token über `POST api/auth/logout`. Alternativ bleibt das **Geräte-Pairing per Einmal-Code** verfügbar: Die App tauscht einen in der Server-Verwaltung (Bereich „Geräte") erzeugten Einmal-Code über den öffentlichen, ratenbegrenzten Endpunkt `POST api/pairing/exchange` gegen ein AES-256-GCM-verschlüsseltes Geräte-Token (Schlüsselaustausch per flüchtigem ECDH-Schlüsselpaar nistP256). Das entschlüsselte Token liegt profil- und serveradress-spezifisch in der sicheren Plattformablage (`SecureStorage` über `ISecretStore`, `AppSecretsService`) — nicht in `Preferences` und nicht im Geräte-Backup. Die App meldet beim Pairing den Gerätenamen (`deviceName`), damit das Gerät in der Server-Verwaltung identifizierbar und widerrufbar ist. Lehnt das Gate ein Geräte-Token ab (widerrufen/abgelaufen), verwirft die App es und fordert auf der Serverseite erneut zum Pairing auf; ein konfigurierter statischer Schlüssel wird dabei einmalig als Fallback geprüft. Details: `docs/help/serververwaltung-erststart.md`, `docs/help/verbindung-anmeldung.md`.

Der statische Gate-Schlüssel bleibt als Übergangs-/Fallback-Mechanismus konfigurierbar. `ClientGate.Resolve()` löst den Schlüssel plattformneutral in dieser Reihenfolge auf — Werte, die dem Platzhalter entsprechen, gelten als nicht konfiguriert:

1. Geräte-Token des aktiven Servers (`ClientGate.DeviceTokenProvider`, gepflegt vom `DeviceTokenService`)
2. Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN`
3. MSBuild-Eigenschaft `VideoPlayerApiToken` beim Build/Publish (Assembly-Metadata `VideoPlayer.ApiClient.ApiToken`), z. B. `dotnet publish VideoPlayer.Maui/VideoPlayer.Maui.csproj -p:VideoPlayerApiToken="<MAUI_CLIENT_API_TOKEN>"` oder `scripts/iOS-Deployment.ps1 -VideoPlayerApiToken "<MAUI_CLIENT_API_TOKEN>"` (das Skript lehnt Werte mit Anführungszeichen, Raute oder Zeilenumbruch mit verständlicher Meldung ab — der Token darf diese Zeichen nicht enthalten)
4. Token-Datei `client-api-token.txt` im lokalen Anwendungsdatenverzeichnis (`%LocalAppData%/VideoPlayer` bzw. Plattformäquivalent; bei gesetztem `VIDEOPLAYER_PROFILE` unterhalb des Profil-Verzeichnisses, z. B. `%LocalAppData%/VideoPlayer/<profil>`)
5. Platzhalter `<MAUI_CLIENT_API_TOKEN>` — oder explizit pro Client über `VideoPlayerApiClientOptions.ApiKey`

Der Platzhalter `<MAUI_CLIENT_API_TOKEN>` im Client ist kein produktiv verwendbarer Wert. Er markiert den Client-Gate-Wert für initiale Health- und Login-Aufrufe. Dieser Wert ist kein Ersatz für Benutzer-Authentifizierung und kann in einem ausgelieferten Client grundsätzlich ausgelesen werden; der korrespondierende Backendwert ist dennoch als sensibler Konfigurationswert zu schützen. Die eigentliche Benutzerautorisierung erfolgt über JWTs nach erfolgreicher Anmeldung.

In der Alt-App `VideoWebPlayer.Maui` werden produktive Werte zentral über `VideoWebPlayer.Maui/Services/MauiClientConfiguration.cs` gelesen. Unterstützte Quellen sind:

- MSBuild: `dotnet build VideoWebPlayer.Maui/VideoWebPlayer.Maui.csproj -p:MauiClientApiToken="<MAUI_CLIENT_API_TOKEN>"`
- Runtime-Umgebung: `VIDEOWEBPLAYER_MAUI_API_TOKEN`

Der statische Wert muss backendseitig zu `Jwt:ApiToken:Maui` passen. Echte JWT-Schlüssel, Benutzerpasswörter und produktive API-Tokens gehören nicht in dieses Repository. Benutzerpasswörter und Geräte-Tokens werden im Client über `SecureStorage` abgelegt; `Preferences` enthält nur nicht geheime App-Einstellungen wie Serveradresse und Benutzername.

## Lizenz

Dieses Repository steht unter der **PolyForm Noncommercial License 1.0.0**. Kommerzielle Nutzung erfordert eine vorherige schriftliche Zustimmung.

Kontakt für kommerzielle Lizenzanfragen: mstromberg84+videoplayer@gmail.com
