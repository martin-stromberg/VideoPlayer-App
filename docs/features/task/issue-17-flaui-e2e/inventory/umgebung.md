# Detail: Umgebungsnachweise

## Smoke-Test der Debug-Exe (2026-09-24)

- Befehl: `VIDEOPLAYER_PROFILE=e2esmoke ./VideoPlayer.Maui.exe` in
  `VideoPlayer.Maui/bin/Debug/net10.0-windows10.0.19041.0/win-x64/`.
- Ergebnis: Fensterprozess `VideoPlayer` (PID 32776),
  `devapi.json` `{"port":64450,"pid":32776,"startedUtc":"…"}`,
  `GET /model` → `currentPage:"ServerSetup"`,
  `connectionState:"NeedsServerSetup"`, `knownServers:[]`;
  `GET /tree` → vollständiger Elementbaum. Prozess danach sauber
  beendet.
- Interaktiver Desktop vorhanden (Code, DB Browser, Ember Media
  Manager u. a. mit Hauptfenstern) → FlaUI lauffähig.

## Pfade

- App-Projektmappe: `VideoPlayer.App.sln` (7 Projekte).
- Exe (Debug, unpackaged):
  `VideoPlayer.Maui/bin/Debug/net10.0-windows10.0.19041.0/win-x64/VideoPlayer.Maui.exe`
  — RID-Ordner `win-x64` verifiziert.
- Build: `dotnet build VideoPlayer.Maui/VideoPlayer.Maui.csproj
  -f net10.0-windows10.0.19041.0 -c Debug`.
- SDK: `dotnet --version` = `10.0.401`.

## Paketversionen

- `FlaUI.UIA3` **5.0.0** (veröffentlicht 2025-02-25, NuGet:
  ~1,6 M Downloads) — stabil, >7 Tage alt. Targets .NET 8 →
  kompatibel mit `net10.0`-Testprojekt.
- xUnit-Stack wie `VideoWebPlayer.Maui.Tests`: `xunit.v3` 3.2.2,
  `xunit.runner.visualstudio` 3.1.5, `Microsoft.NET.Test.Sdk` 18.8.1,
  `coverlet.collector` 10.0.1.

## Kritische Umgebungsfalle

- `VIDEOWEBPLAYER_MAUI_API_TOKEN` ist auf diesem Rechner als
  **Benutzer-Umgebungsvariable gesetzt** (echtes Token). Würde der
  Kindprozess sie erben, gälte `ClientGate.ResolveStatic()` als
  konfiguriert → `IsPairingRequired=false` → kein Pairing-Flow.
  Der App-Launcher entfernt die Variable im Kindprozess
  (`ProcessStartInfo.Environment.Remove`/`[key]=null`).
- Token-Datei `%LocalAppData%\VideoPlayer\client-api-token.txt`:
  hier nicht vorhanden; der Launcher prüft sie als Vorbedingung für
  Pairing-Tests (Warnung/Skip-Hinweis, da sie `ResolveStatic` ebenfalls
  konfiguriert macht).
- Assembly-Metadata `VideoPlayer.ApiClient.ApiToken` =
  `<MAUI_CLIENT_API_TOKEN>` (Platzhalter, Standard-Build ohne
  `-p:VideoPlayerApiToken`) → unkritisch.
