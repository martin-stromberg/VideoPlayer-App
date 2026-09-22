# VideoWebPlayer MAUI App

Dieses Repository enthält die ausgelagerte .NET-MAUI-App und ihre Tests.

## Inhalt

- `VideoWebPlayer.Maui/` - bisherige mobile App (Altbestand, wird schrittweise durch `VideoPlayer.Maui` abgelöst).
- `VideoWebPlayer.Maui.Tests/` - tests ohne Rückreferenz auf das Webprojekt.
- `VideoWebPlayer.Client/` - kopierte Übergabeschnittstelle aus dem Web-Repository (Altbestand, wird im Rahmen der Neugestaltung entfernt).
- `VideoPlayer.ApiClient/` - neu implementierter, plattformneutraler Zugriff auf die öffentliche Server-API sowie Netzwerk-Erkennung von Servern (mDNS/UDP, Namespace `Discovery`; Dokumentation: `docs/help/api-client/`).
- `VideoPlayer.Mvu/` - plattformneutrale MVU-Schicht der neuen App (Modell, Messages, Update, Effekte; eigener schlanker MVU-Kreislauf ohne externes Framework).
- `VideoPlayer.Storage/` - plattformneutrale lokale Datenhaltung der neuen App (Einstellungsmodell und -dienst, Geheimnis-Ablage, Profil-Isolation, Backup-/NoBackup-Ablageorte, SQLite-Datenbanken, Serververwaltung `KnownServersService`).
- `VideoPlayer.Maui/` - neue MAUI-App auf MVU-Basis (Seitenmenü-Navigation, ausgebaute Einstellungsseite, Serververwaltung mit Erststart-Einrichtung, Erreichbarkeitsprüfung; Dokumentation: `docs/help/app-grundgeruest.md`, `docs/help/einstellungen-datenhaltung.md`, `docs/help/serververwaltung-erststart.md`).
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
