# Umsetzungsplan: E2E-Tests mit FlaUI gegen VideoPlayer.Maui.exe

Branch: `task/issue-17-flaui-e2e`
Quellen: `requirement.md`, `inventory.md` + `inventory/*.md`,
Projektplan Schritt 3.

## Ziel

Neues xUnit-Testprojekt `VideoPlayer.E2E.Tests` in `VideoPlayer.App.sln`:
baut die Debug-Exe selbst, startet sie pro Szenario mit isoliertem
`VIDEOPLAYER_PROFILE`, steuert sie per FlaUI (UIA3, echte
Klicks/Eingaben) und assertiert über die Developer-API aus Schritt 2
(`GET /model`, `GET /tree`, `POST /invoke`, `POST /set-text`).
Serverabhängige Szenarien laufen gegen einen hermetischen Mock-Server
im Testprojekt.

## Entscheidungen

- **Backend: Option B — Mock-Server im Testprojekt.** Begründung:
  hermetisch, deterministisch, kein Seed/kein IIS nötig; der
  Pairing-Handshake (ECDH P-256 + AES-256-GCM) ist ~40 Zeilen
  Bordmittel und der Vertrag ist beidseitig belegt. Der echte
  Server-Checkout bleibt als dokumentierter manueller Nachweisweg
  (siehe `inventory/backend-optionen.md`). Server-Setup- und
  Fehlerszenarien brauchen ohnehin kein Backend.
- **`/invoke`-Erweiterung `itemIndex`:** kleinster Eingriff für
  „bekannten Server wählen" ohne Koordinatenklick — optionales
  `itemIndex` im `/invoke`-Body setzt auf dem UI-Thread
  `CollectionView.SelectedItem` → `SelectionChanged` → echte
  `OnKnownSelectionChanged`-Kette. Gleicher Endpunkt, kein `dispatch`;
  `DevApiContractTests` (4 Endpunkte) bleiben gültig.
- **Isolation:** `VIDEOPLAYER_PROFILE=e2e-<testname>-<guid>` je
  App-Instanz; `VIDEOWEBPLAYER_MAUI_API_TOKEN` wird im Kindprozess
  entfernt (auf diesem Rechner als Benutzer-Variable gesetzt — sonst
  kein Pairing-Pfad); `devapi.json` je Lauf neu gelesen.
- **Sequentiell:** alle E2E-Tests in einer xUnit-Collection — keine
  parallelen App-Instanzen (Fokus/Fenster-Rennen vermeiden).
- **FlaUI.UIA3 5.0.0** (2025-02-25, >1 Jahr alt) gepinnt; xUnit-Stack
  wie `VideoWebPlayer.Maui.Tests` (xunit.v3 3.2.2, runner 3.1.5,
  Test.Sdk 18.8.1).

## Struktur des Testprojekts

```
VideoPlayer.E2E.Tests/
  VideoPlayer.E2E.Tests.csproj   (net10.0, xunit.v3, FlaUI.UIA3 5.0.0)
  E2ECollection.cs               CollectionDefinition (sequential) + AppFixture
  AppFixture.cs                  einmaliger Debug-Build der App (Lazy)
  Support/
    AppInstance.cs               Prozessstart/-stopp, Env, devapi.json, FlaUI-Attach
    DevApiClient.cs              /model /tree /invoke /set-text + WaitFor-Helper
    DevApiModels.cs              JSON-DTOs (camelCase)
    MockVideoServer.cs           HttpListener auf 127.0.0.1:0, Routen
    MockPairingCrypto.cs         ECDH P-256 + AES-256-GCM (Server-Seite)
    UiDriver.cs                  FlaUI-Hilfen: Fenster, ByAutomationId, Enter, Click, Flyout
  Tests/
    ServerSetupTests.cs          Erststart, ungültige Adresse, nicht erreichbar, Pairing-Sichtbarkeit
    PairingTests.cs              Formatfehler, falscher Code (401), gültiger Code → Login-Seite
    LoginTests.cs                falsche Credentials → Fehler, richtige → Home
    ServerSwitchTests.cs         Settings → Server wechseln → B manuell + pair → Home(B) → bekannten A-Item wählen → Home(A)
```

### AppInstance

- `ProcessStartInfo` auf die Debug-Exe
  (`bin/Debug/net10.0-windows10.0.19041.0/win-x64/VideoPlayer.Maui.exe`,
  Pfad relativ zum Repo-Root via `RepositoryRoot`-Muster wie in
  `VideoWebPlayer.Maui.Tests/Stubs`); `UseShellExecute=false`;
  `Environment["VIDEOPLAYER_PROFILE"]=profilname`;
  `Environment.Remove("VIDEOWEBPLAYER_MAUI_API_TOKEN")`.
- Warten bis `devapi.json` im Exe-Verzeichnis mit `pid==Process.Id`
  existiert (Timeout 30 s) → Port lesen → `DevApiClient`.
- FlaUI: `Application.Attach(pid)` + `GetMainWindow` mit Retry.
- `Dispose`: `CloseMainWindow` → `Kill()` im `finally`, `devapi.json`
  löschen, Profilverzeichnis `%APPDATA%\VideoPlayer\<profil>` optional
  stehen lassen (klein) bzw. löschen.

### DevApiClient

- `HttpClient` ohne BaseAddress, je Request neue Verbindung
  (Server = Connection: close, sequentiell — kein Pipelining).
- `GetModelAsync()` → `DevApiModelSnapshot`-DTO;
  `WaitForModelAsync(Func<model,bool>, timeout)` Poll 250 ms —
  zentrale Warte-Primitive (Zustandsläufe sind async).
- `GetTreeAsync()`, `InvokeAsync(automationId, itemIndex?)`,
  `SetTextAsync(automationId, text)`.

### MockVideoServer

- `HttpListener` ist nicht Port-0-fähig → `TcpListener(IPAddress.Loopback,0)`
  + minimaler HTTP/1.1-Parser (nur GET/POST mit JSON-Body, Request bis
  Header-Ende lesen, Content-Length). Alternativ HttpListener mit
  vorbelegtem Port — verworfen (Race). Mini-Parser ~100 Zeilen,
  Muster orientiert an `DevApiHttpServer`.
- Routen (siehe `inventory/backend-optionen.md`): `api/health`→200;
  `api/pairing/exchange`→Code-Prüfung, 401/200 mit
  ECDH/AES-GCM-Antwort; `api/auth/login`→Gate `X-API-Key`==Geräte-Token
  + Credential-Prüfung → `{token,expires}`; `api/favorites`,
  `api/continue-watching`, `api/Sources`, `api/items/recent`→`[]`;
  Fallback 404 JSON. Konfigurierbar: `PairingCode`, `DeviceToken`,
  `Users` (E-Mail→Passwort). Kein 5xx → keine Client-Retries.

### `/invoke`-Erweiterung (Produktivcode, klein)

- `DevApiService.InvokeBody` + `itemIndex` (optional);
  `InvokeOnPage`: bei `CollectionView` + `itemIndex` →
  `DevApiVisualTree.TrySelectItem(view, index)` →
  `SelectedItem = items[index]` (UI-Thread) → `SelectionChanged`
  feuert die echte Seitenlogik.
- `DevApiVisualTree.TrySelectItem`: ItemsSource enumerieren, Index
  prüfen, `SelectedItem` setzen; Fehlertexte deutsch.
- `docs/help/developer-api.md` ergänzen (Body-Feld `itemIndex`).

## Testszenarien (Mapping auf Akzeptanzkriterien)

| Test | Fluss | Assertions (Dev-API) | Treiber |
|------|-------|----------------------|---------|
| `FreshStart_ShowsServerSetup` | App-Start ohne Profil | `currentPage=ServerSetup`, `connectionState=NeedsServerSetup` | FlaUI Fenster + `/model` |
| `InvalidAddress_ShowsError` | `AddressEntry` "keine-url" → `ConnectButton` klicken | `serverSetupError=InvalidAddressError` | FlaUI Eingabe+Klick |
| `UnreachableServer_ShowsError` | `http://127.0.0.1:9/` → Verbinden | `serverSetupError=UnreachableServerError` (Warte bis 30 s — Retry-Delays) | FlaUI + `/model` |
| `ReachableServer_RequiresPairing` | Mock-Adresse → Verbinden | `isPairingRequired=true`, `activeServerAddress=mock` | FlaUI + Mock + `/model` |
| `MalformedCode_ShowsFormatError` | Pairing-Zustand → "ab!" → Koppeln | `pairingError=PairingCodeFormatError` | FlaUI |
| `WrongCode_ShowsError` | gültig formatierter Falsch-Code → Koppeln | `pairingError=InvalidPairingCodeError` (Mock 401) | FlaUI + Mock |
| `ValidCode_ShowsLoginPage` | Mock-Code → Koppeln | `currentPage=Login`, `connectionState=NeedsLogin` | FlaUI + Mock |
| `WrongPassword_ShowsError` | Login-Seite → falsche Credentials | `loginError=InvalidCredentialsError` | FlaUI + Mock |
| `ValidLogin_ShowsHome` | richtige Credentials → Anmelden | `currentPage=Home`, `connectionState=Connected`, `activeServerAddress` | FlaUI + Mock |
| `ServerSwitch_ReturnsToHome` | Home → Flyout „Einstellungen" → `ServerSwitchButton` → Server B manuell verbinden+pairing → `currentPage=Home`, `activeServerAddress=B` → erneut Server wechseln → bekannten Eintrag A per `invoke itemIndex` wählen → `currentPage=Home`, `activeServerAddress=A` | Regression #15 | FlaUI + `/invoke itemIndex` + 2 Mocks |

## Offene Punkte — alle entschieden

- **OP-A Backend:** entschieden → Mock (siehe Entscheidungen).
- **OP-B Item-Auswahl:** entschieden → `/invoke` `itemIndex`.
- **OP-C FlaUI-Rolle bei Login/Paar-Buttons:** echte UIA-Klicks
  (`Button.Invoke()`/`.Click()`); `/invoke` nur für Items. Falls ein
  MAUI-Element unter UIA3 nicht auffindbar ist (Fallback): `/set-text`
  + `/invoke` mit dokumentiertem Kommentar — Ausnahme, nicht Regel.
- **OP-D App-Build im Test:** `dotnet build` einmalig je Testlauf
  (Lazy im Fixture), nicht je Test.

## Verifikation

- `dotnet test VideoPlayer.E2E.Tests` lokal, echte Zahlen in
  `test-results.md`.
- `dotnet test VideoWebPlayer.Maui.Tests` als Regression (Contract-
  Tests + Dev-API-Unit-Tests müssen grün bleiben).
- Build der Solution inkl. neuem Projekt.
- CI: optional dokumentiert (kein interaktiver Desktop auf GH-Runnern,
  Billing-Blocker) — `docs/help/e2e-tests.md` + ggf. kommentierter
  Workflow-Hinweis, kein Fake-Job.

## Risiken

- **Fenster-Fokus/mausposition:** echte `Click()`-Aufrufe sind
  fokusabhängig → bevorzugt UIA `Invoke()`-Pattern (kein Mausklick).
- **SecureStorage unter unpackaged Windows:** Geräte-Token/Credentials
  müssen persistieren — wurde bei #15 auf dieser Maschine real
  verifiziert; Risiko gering, wird durch den Pairing-Test bewiesen.
- **Timing:** Health-Check-Retries (1+2+4 s) und Start-Workflow →
  alle Zustands-Assertions nur über `WaitForModelAsync` mit
  großzügigen Timeouts (20–40 s), keine festen Sleeps.
