# VideoWebPlayer MAUI App

Dieses Repository enthält die ausgelagerte .NET-MAUI-App und ihre Tests.

## Inhalt

- `VideoWebPlayer.Maui/` - mobile App.
- `VideoWebPlayer.Maui.Tests/` - tests ohne Rückreferenz auf das Webprojekt.
- `VideoWebPlayer.Client/` - kopierte Übergabeschnittstelle aus dem Web-Repository.
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

Windows:

```powershell
dotnet build .\VideoWebPlayer.Maui\VideoWebPlayer.Maui.csproj -f net10.0-windows10.0.19041.0
```

Android:

```bash
dotnet build VideoWebPlayer.Maui/VideoWebPlayer.Maui.csproj -f net10.0-android
```

## Web-API-Vertrag

Das Web-Repository besitzt die verbindliche API-Dokumentation unter `docs/API.md`. Für produktive Änderungen an Endpunkten, DTOs oder Authentifizierung muss das Web-Repository zuerst den Vertrag aktualisieren und testen.

## Secrets und Client-Gate

Der Platzhalter `<MAUI_CLIENT_API_TOKEN>` im Client ist kein produktiv verwendbarer Wert. Er markiert den Client-Gate-Wert für initiale Health- und Login-Aufrufe. Dieser Wert ist kein Ersatz für Benutzer-Authentifizierung und kann in einem ausgelieferten Client grundsätzlich ausgelesen werden; der korrespondierende Backendwert ist dennoch als sensibler Konfigurationswert zu schützen. Die eigentliche Benutzerautorisierung erfolgt über JWTs nach erfolgreicher Anmeldung.

Produktive Werte werden zentral über `VideoWebPlayer.Maui/Services/MauiClientConfiguration.cs` gelesen. Unterstützte Quellen sind:

- MSBuild: `dotnet build VideoWebPlayer.Maui/VideoWebPlayer.Maui.csproj -p:MauiClientApiToken="<MAUI_CLIENT_API_TOKEN>"`
- Runtime-Umgebung: `VIDEOWEBPLAYER_MAUI_API_TOKEN`

Der Wert muss backendseitig zu `Jwt:ApiToken:Maui` passen. Echte JWT-Schlüssel, Benutzerpasswörter und produktive API-Tokens gehören nicht in dieses Repository. Benutzerpasswörter werden im Client über `SecureStorage` abgelegt; `Preferences` enthält nur nicht geheime App-Einstellungen wie Serveradresse und Benutzername.

## Lizenz

Dieses Repository steht unter der **PolyForm Noncommercial License 1.0.0**. Kommerzielle Nutzung erfordert eine vorherige schriftliche Zustimmung.

Kontakt für kommerzielle Lizenzanfragen: mstromberg84+videoplayer@gmail.com
