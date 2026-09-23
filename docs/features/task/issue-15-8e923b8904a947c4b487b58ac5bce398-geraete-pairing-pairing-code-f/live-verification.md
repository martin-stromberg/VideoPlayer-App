# Live-Verifikation gegen IIS-Testserver

Datum: 2026-09-23
Server: `http://localhost:83` (IIS-Site "VideoPlayer", staging mit Issue-228-Feature, Deployment `D:\Dashboard\VideoPlayer`, DB `Data\WebVideoPlayer.db`)

## Durchgeführte Prüfungen

| # | Prüfung | Ergebnis |
|---|---------|----------|
| 1 | Pairing-Code direkt in `PairingCodes`-Tabelle eingefügt (`CodeHash` = SHA-256-Hex-Uppercase, `ExpiresAtUtc` = +15 min, `CreatedByUserId` = devin-User `60103ea7-…`) | OK |
| 2 | PowerShell-Referenzimplementierung des Vertrags (ECDH nistP256, SPKI/DER Base64, `DeriveKeyFromHmac` SHA256, AES-256-GCM `nonce(12)‖ct‖tag(16)`) gegen `POST /api/pairing/exchange` mit Code `DEVIN42X` | 200; Token entschlüsselt: 43 Zeichen = `Base64Url(32B)` ✓ |
| 3 | **Echter Produktivcode** `DevicePairingClient`+`PairingCrypto` (Konsolen-Harness mit Projektreferenz, `%TEMP%\pairing-harness`) mit Code `VERIFY42` | 200; Token entschlüsselt ✓ |
| 4 | Negativfälle mit echtem Client: unbekannter Code `ZZZZZZZZ` → `ApiException` 401 "Ungültiger oder abgelaufener Pairing-Code."; bereits verbrauchter Code `DEVIN42X` → 401 | ✓ |
| 5 | `PairedDevices`-Tabelle: beide Geräte mit `deviceName` ("Devin Verifikations-PC", "Devin Harness-PC") und `CreatedByUserId` | ✓ |
| 6 | Geräte-Token als `X-API-Key` an `POST api/auth/login` (MauiOnly-Gate) mit `devin@example.com` | 200 + JWT ✓ |
| 7 | Code-Wiederverwendung serverseitig geblockt (`ConsumedAtUtc` gesetzt, zweite Verwendung → 401) | ✓ |

## Befund aus der Live-Verifikation (in `review-code.md` übernommen)

- `GET api/health` ist **nicht** durch `ApiTokenCheck` geschützt (antwortet 200 ohne/ mit ungültigem `X-API-Key`) → eine Gate-Probe auf `api/health` kann niemals 401 liefern.
- Einziger MauiOnly-gated Endpunkt: `POST api/auth/login`. Dort unterscheidbar:
  - Gate-Ablehnung (`UnauthorizedResult` aus `ApiTokenCheckAttribute`) → `401` mit `Content-Type: application/problem+json`, Body = ProblemDetails (`"title":"Unauthorized"`).
  - Credential-Fehler (`Unauthorized(ex.Message)` aus der Action) → `401` mit `Content-Type: text/plain`, Body = Klartext ("Benutzer nicht gefunden." / "Ungültige Zugangsdaten.").
  - Gate passiert + Model-Validation → `400`.
- `LoginIpBlockService.RegisterFailure` wird ausschließlich im `PairingController` aufgerufen → Login-Probes erzeugen keine IP-Sperr-Fehlversuche.
- Nebenbefund: das auf dieser Maschine konfigurierte statische Token (`VIDEOWEBPLAYER_MAUI_API_TOKEN`, Machine-Scope) wird von diesem Server abgelehnt (401 problem+json) — gehört vermutlich zu einer anderen Serverinstanz.

## Abdeckung Task 42 (manueller Gerätetest)

Der kryptografische und API-vertragliche Teil des Pairing-Flows ist hiermit **gegen den realen Server verifiziert** (Punkte 2–6 oben). Nicht verifizierbar in dieser Umgebung bleiben die gerätespezifischen Anteile: `SecureStorage`-Persistenz, `DeviceInfo`-Gerätename und die UI-Bedienung auf echten Zielen (iOS/Android/Windows-App).

## E2E-Lauf in der echten Windows-App (Nachtrag, gleiches Datum)

`VideoPlayer.Maui.exe` (Debug, `net10.0-windows`, unverpackt) wurde real gestartet und per UI-Automation (`AutomationId`s, `ValuePattern`/`InvokePattern`) durch den kompletten Flow geführt. Screenshots dokumentieren jeden Schritt.

| # | Schritt | Ergebnis |
|---|---------|----------|
| 8 | App-Start mit aktivem Server `http://localhost:83/` | Health-Check → erreichbar → Auto-Login ohne gespeicherte Zugangsdaten → **Anmeldemaske** mit `ReloginRequiredNotice` |
| 9 | Anmeldung `devin@example.com` — aufgelöster Gate-Schlüssel ist der **ungültige** statische Token (Token-Datei `client-api-token.txt` / `VIDEOWEBPLAYER_MAUI_API_TOKEN`, am Gate mit `401 problem+json` abgelehnt) | Gate-Ablehnung erkannt (`IsGateRejectedAsync` via `POST api/auth/login` + `{}`) → `DeviceTokenRevoked` → **Serverseite mit Pairing-Abschnitt** und Meldung „Der Geräte-Zugang wurde vom Server abgelehnt. Bitte das Gerät erneut koppeln." |
| 10 | Pairing-Code `ABCD2345` (per DB-Insert auf dem Server erzeugt, Alphabet-konform) eingegeben → „Koppeln" | Exchange erfolgreich → Geräte-Token entschlüsselt und im **SecureStorage** abgelegt → `PairedDevices`-Eintrag „**DESKTOP-CM8OBSG**" (realer `DeviceInfo.Name`), Code serverseitig `ConsumedAtUtc`-gesetzt → Auto-Login → Anmeldemaske |
| 11 | Erneute Anmeldung mit Zugangsdaten | **Startseite** mit „Neu im Programm" (reale Medienposter), „Server: erreichbar" — Geräte-Token am Gate akzeptiert |
| 12 | Negativfall UI-Validierung: Codes `SHOWME42`/`PAIR2479` (enthalten `O`/`I`) | Lokale Formatprüfung greift: „Der Code ist nicht vollständig oder enthält ungültige Zeichen — …" — kein Serverkontakt |

### Dabei gefundener und behobener Fehler

**`UpdateServersLoaded` setzte `IsPairingRequired` zurück:** Nach der Gate-Ablehnung (Schritt 9) zeigte die Serverseite zunächst den Pairing-Abschnitt — `OnAppearing` dispatcht jedoch `LoadServersRequestedMessage`, dessen `IsPairingRequiredAsync` den *vorhandenen, aber abgelehnten* statischen Schlüssel wieder als ausreichend wertete (`ResolveStatic() != Platzhalter` → `false`) und den Abschnitt samt Fehlermeldung ausblendete. Reproduziert per Screenshot (Serverseite ohne Pairing-Abschnitt nach der Anmeldung).

Fix in `AppUpdate.UpdateServersLoaded`: `IsPairingRequired` bleibt gesetzt, solange das Model im Re-Pairing-Zustand ist (`NeedsServerSetup && IsPairingRequired`); die nachgeladene Ermittlung darf es nur noch *setzen*, nicht mehr zurücknehmen. Regressionstests: `Update_ServersLoaded_InRePairingState_KeepsPairingRequired`, `Update_ServersLoaded_WhileConnected_AppliesPairingFlag`.

### Restabdeckung

Damit ist der komplette Pairing- und Re-Pairing-Flow **auf einem realen Windows-Ziel** verifiziert — inklusive `SecureStorage` (Credential Store), `DeviceInfo`-Gerätename und UI-Bedienung. Offen bleiben nur die plattformspezifischen Backends auf iOS (Keychain) und Android (Keystore); der Codepfad ist derselbe (`SecureStorage`-Abstraktion).
