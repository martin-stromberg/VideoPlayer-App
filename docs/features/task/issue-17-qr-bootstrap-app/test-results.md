# Testergebnisse — App-seitiges QR-/Ticket-Onboarding (Issue 17, Schritt 5b)

Datum: 2026-09-24, lokale Ausführung auf Windows 11 (interaktive Session).

## Build `VideoPlayer.Maui` (Windows-Target)

Kommando: `dotnet build VideoPlayer.Maui/VideoPlayer.Maui.csproj -f net10.0-windows10.0.19041.0 -c Debug`

| Kennzahl | Wert |
|----------|------|
| Warnungen | 0 |
| Fehler | 0 |

Hinweis: `QrScannerPage` und die ZXing-Initialisierung liegen hinter
`#if ANDROID || IOS` und werden im Windows-Build nicht kompiliert;
die mobile Kompilierung ist Vertrags-gepinnt (`BootstrapContractTests`)
und bleibt manueller Restnachweis auf Gerät.

## Unit-/Contract-Tests `VideoWebPlayer.Maui.Tests`

Kommando:

```bash
dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj
```

| Kennzahl | Wert |
|----------|------|
| Gesamt | 1013 |
| Bestanden | 1013 |
| Fehlgeschlagen | 0 |
| Übersprungen | 0 |

Baseline vor der Änderung: 927/927 — Plus 86 neue Tests:

| Testdatei | Inhalt |
|-----------|--------|
| `BootstrapTicketParserTests` (15) | URL-Formen (Root, Unterpfad, Port, Trailing-Slash, Case), opak dekodiertes `t`, `t` zwischen anderen Parametern, Kurzcode, Trim, Ablehnungen |
| `DevicePairingClientBootstrapTests` (10) | Endpunkt + camelCase-Vertrag, kein Gate/Bearer, Entschlüsselungs-Roundtrip, Kurzcode-Durchreichen, Status-Mapping ohne Retry, Transport-/Entschlüsselungs-/Pflichtfeld-Fehler, Ticket-Pflicht |
| `AppUpdateTests_Bootstrap` (11) | URL→Check-Effekt, Kurzcode→Exchange, Kurzcode ohne Server→Fehler, Parser-Fehler, Doppel-Submit, Checked (Erfolg→Persist+Exchange, unerreichbar, stale), Completed (Erfolg→AutoLogin, Fehler-Mapping, stale), Reset bei Servercheck |
| `AuthSessionServiceRefreshTests` (6) | Refresh-first + Rotation persistiert, Refresh beim Start über abgelaufenes Token, Refresh-401→Credential-Fallback, Refresh-401 ohne Credentials→SessionExpired, Gate-401-Pfad, Logout-Widerruf + unreachable-Logout |
| `AppSecretsServiceTests` (+5) | Refresh-Token im SecretStore, Rotation, Profil-Isolation, ClearCredentials räumt Refresh-Token, ohne Eintrag → null |
| `VideoPlayerApiClientTests_SessionRefresh` (6) | `api/auth/refresh`-Vertrag + Gate-Header, 401 ohne Reauth, Pflicht-Token, `api/auth/logout`-Widerruf, Idempotenz, Pflicht-Token |
| `BootstrapContractTests` (9) | BootstrapSection + AutomationIds, Dispatch-Verdrahtung, Effekt-Handler, ZXing Mobile-only, Manifeste, QrScannerPage-Pins, Parser-Ort/-Opazität, Dev-API secret-frei |

## E2E-Tests `VideoPlayer.E2E.Tests`

Kommando:

```bash
dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj
```

| Kennzahl | Wert |
|----------|------|
| Gesamt | 17 |
| Bestanden | 17 |
| Fehlgeschlagen | 0 |
| Dauer | ~1,5 min |

Neue Szenarien (`BootstrapTests`, 6 Tests) — beide geplanten
Pflichtflüge plus Fehler- und Persistenzfälle:

| Test | Ergebnis | Belegt |
|------|----------|--------|
| `TicketUrl_BootstrapsDirectlyToHome` | bestanden | Ticket-URL vom Erststart → Prüf→Persist→Bootstrap→Home; `BootstrapCount=1`, `PairingExchangeCount=0`, `LoginCount=0`, Server in `KnownServers` |
| `ShortCode_BootstrapsOnConnectedServer` | bestanden | Kurzcode gegen verbundenen Server → Home ohne Pairing-Code/Anmeldemaske |
| `InvalidTicket_ShowsBootstrapError` | bestanden | Server-401 → „ungültig"-Inline-Fehler, kein Connected-Zustand |
| `UnreachableTicketServer_ShowsError_WithoutPersisting` | bestanden | Erreichbarkeitsprüfung vor Übernahme — kein `ActiveServerAddress`, nicht in `KnownServers` |
| `MalformedInput_ShowsLocalError_WithoutServerContact` | bestanden | `ftp://…`-Eingabe → lokaler Fehler, `BootstrapCount=0` |
| `BootstrappedSession_SurvivesAppRestart` | bestanden | Neustart desselben Profils → direkter Einstieg Home (sichere Persistenz von JWT + Refresh-Token) |

Bestehende Suites (11 Tests: ServerSetup, Pairing, Login, ServerSwitch)
unverändert grün — die Fallback-Wege bleiben funktional.

## Live-Verifikation

Screenshots und Sichtprüfung: `live-verification.md` +
`live-verification/*.png` (4 Zustände: Erststart mit Kopplungs-Karte,
laufende Serverprüfung, Inline-Fehler, direkter Home-Einstieg).

## Restnachweise (nicht automatisierbar)

- **Mobiler Kamera-Scan** (`QrScannerPage`, ZXing-Handler): nur auf
  Android/iOS kompilierbar/ausführbar — Windows-Build deckt ihn nicht
  ab; Contract-Tests pinnen Verdrahtung, Permission- und
  Abbruch-Verhalten. Gerätetest ausstehend.
- **Echter Server** (Branch `task/issue-231-…`): Ticket-Erzeugung,
  Kurzcode-Alias, Refresh-Rotation und Widerruf-Idempotenz sind gegen
  den hermetischen `MockVideoServer` verifiziert; ein Scratch-Lauf
  gegen den realen Server steht noch aus (siehe Abschlussbericht).
