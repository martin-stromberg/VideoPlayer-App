# Umsetzungsplan – QR-/Ticket-Onboarding (Schritt 5b)

Server-Vertrag: `docs/projects/project/issue-17-testbarkeit-und-onboarding/acceptance-schritt-5a.md` (Branch `task/issue-231-qr-bootstrap-geraetekopplung-login`, **nicht gemergt**).

## Architektur-Überblick

Zwei Einstiege führen auf denselben MVU-Pfad:

- **Ticket-URL** (QR-Scan iOS/Android oder Paste in das Eingabefeld): `https://<server>[/unterpfad]/pairing?t=<ticket>` → Parser liefert Basisadresse + Ticket → Erreichbarkeitsprüfung → Server persistieren → Bootstrap-Exchange → Session persistieren → `AttemptAutoLogin` → Home.
- **Kurzcode** (manuelle Eingabe, kein URL-Anteil): braucht den bereits verbundenen aktiven Server → direkt Bootstrap-Exchange → Session → Home.

## 1. Plattformneutraler Parser – `VideoPlayer.Mvu/BootstrapTicketParser.cs` (neu)

```csharp
public enum BootstrapTicketKind { Url, ShortCode }
public sealed record BootstrapTicketInput(BootstrapTicketKind Kind, string Ticket, string? ServerAddress);
public static class BootstrapTicketParser { public static bool TryParse(string? input, out BootstrapTicketInput result); }
```

- Leer/nicht trimmbar → `false`.
- `Uri.TryCreate` + Scheme `http`/`https` → **URL-Modus**:
  - Letztes nicht-leeres Pfadsegment muss `pairing` sein (OrdinalIgnoreCase); `…/pairing` und `…/pairing/` akzeptiert; `…/x/pairing` → Basis `…/x/`.
  - `t=` aus `uri.Query` (Split auf `&`, `Uri.UnescapeDataString`); leer/fehlend → `false`.
  - Basis = `GetLeftPart(UriPartial.Path)` minus pairing-Segment → `ServerAddress.TryNormalize`; scheitert → `false`.
  - **Kein** `PairingCode.TryNormalize` auf das Ticket (opak).
- Nicht-URL → **Kurzcode-Modus**: getrimmter Text ohne Whitespace/`:`, sonst `false` (abweisende Fälle landen sonst als 401 beim Server — kein Formatfehler nötig). Ergebnis `Kind=ShortCode, ServerAddress=null`.

## 2. ApiClient

### `VideoPlayer.ApiClient/Models/` (neu)

- `PairingBootstrapRequest { ticket, clientPublicKey, deviceName? }` (camelCase wie `PairingExchangeRequest`).
- `PairingBootstrapResponse { serverPublicKey, encryptedPayload }`.
- `PairingBootstrapPayload { deviceToken, token, expires, refreshToken }` (entschlüsseltes JSON).
- `BootstrapResult` (Rückgabemodell des Clients: `DeviceToken`, `Token`, `Expires`, `RefreshToken`).
- `RefreshTokenRequest { refreshToken }`, `RefreshSessionResult { token, expires, refreshToken }`.

### `DevicePairingClient`

- `BootstrapEndpoint = "api/pairing/bootstrap"`.
- `BootstrapAsync(Uri baseAddress, string ticket, string? deviceName, CancellationToken)` → gleiche Bauweise wie `ExchangeAsync` (kein Gate/Bearer, kein Retry, `ApiException` mit Status); entschlüsseltes JSON → `BootstrapResult`; leere Pflichtfelder → `InvalidOperationException` (→ `Failed`).

### `VideoPlayerApiClient`

- `RefreshSessionAsync(string refreshToken, CancellationToken)` → `POST api/auth/refresh`, `allowReauth: false` (der Aufruf **ist** die Re-Auth).
- `LogoutSessionAsync(string refreshToken, CancellationToken)` → `POST api/auth/logout`, `allowReauth: false`, idempotent (Body wird nicht gelesen).

## 3. Storage – `AppSecretsService`

- `RefreshTokenKey = "RefreshToken"` (profilbezogen wie `AuthTokenKey`, sichere Ablage).
- `GetRefreshTokenAsync()` / `SetRefreshTokenAsync(string)` / `ClearRefreshTokenAsync()`.
- `ClearCredentialsAsync` löscht zusätzlich den Refresh-Token (Session-Einheit).

## 4. `AuthSessionService`

- `SilentReloginAsync`: **vor** dem Credentials-Pfad `TryRefreshSessionAsync`:
  - kein Refresh-Token → `null` → bisheriger Pfad.
  - Erfolg → `SetAuthTokenAsync` + `SetRefreshTokenAsync` (Rotation persistieren) → `AuthToken` zurück.
  - `ApiException` 401/403 → `HandleGateRejectionAsync` (Gate-Probe trennt Gerätewiderruf von ungültigem Refresh-Token); Gate → `null` (kein Credential-Fallback); sonst `ClearRefreshTokenAsync` → `null` → Credential-Fallback.
  - Transient (`ApiException` ohne Status / `HttpRequestException` / `TaskCanceled`) → `null` → Credential-Fallback läuft trotzdem (kein Schaden, Login-Retry entscheidet).
- `LogoutAsync`: `ClientBaseAddress` merken → best-effort `LogoutSessionAsync` (eigener kurzlebiger Client, Gate-Header automatisch) → dann `ClearCredentialsAsync`. Fehler nur loggen. Doc-Kommentare von `LogoutAsync`/`LogoutEffect` anpassen (Endpunkt existiert jetzt).
- `LoginCoreAsync` unverändert (Server-`login` stellt keinen Refresh-Token aus — verifiziert: `AuthorizationToken` = `{token, expires}`).

## 5. MVU

### `BootstrapStatus` (neu, `VideoPlayer.Mvu/BootstrapStatus.cs`)

`Succeeded`, `InvalidTicket` (HTTP 401), `InvalidRequest` (HTTP 400), `RateLimited` (429), `NotSupported` (404), `ServerUnreachable` (Transport), `Failed`.

### `AppModel`

`IsBootstrapRunning` (bool), `BootstrapError` (string?) — beide in `DevApiModelSnapshot` spiegeln (kein Secret). Aufräumen: `BeginServerCheck`, `UpdateServerCheckCompleted`, `UpdateHealthCheckCompleted`-Unreachable-Zweig und `UpdateDeviceTokenRevoked` setzen `IsBootstrapRunning=false`/`BootstrapError=null` (Kontext-Reset wie beim Pairing).

### Messages/Effects

- `BootstrapTicketSubmittedMessage(string Text)`
- `BootstrapServerCheckedMessage(string Address, bool Success, string Ticket)`
- `BootstrapCompletedMessage(BootstrapStatus Status)`
- `CheckBootstrapServerEffect(string Address, string Ticket)`
- `ExchangeBootstrapTicketEffect(string ServerAddress, string Ticket)`

### `AppUpdate`

- `UpdateBootstrapTicketSubmitted`: Doppel-Submit-Guard → `TryParse` → Fehler `BootstrapError = InvalidBootstrapInputError`; `ShortCode` ohne `ActiveServerAddress` → `BootstrapNeedsServerError`; `ShortCode` → `[ExchangeBootstrapTicketEffect]`; `Url` → `[CheckBootstrapServerEffect]`.
- `UpdateBootstrapServerChecked`: Stale-Guard `!IsBootstrapRunning → Unchanged`; `!Success` → `BootstrapError = LoginUnreachableError` (dedicated: `BootstrapServerUnreachableError`); Erfolg → `ActiveServerAddress`/`KnownServers`/`ServerHealth` setzen + `[PersistActiveServerEffect, ExchangeBootstrapTicketEffect]` (sequenziell → Gate-Provider bereit).
- `UpdateBootstrapCompleted`: Stale-Guard; `Succeeded` → `IsPairingRequired=false`, `ConnectionState=Connecting` + `[AttemptAutoLoginEffect]` (validiert das gespeicherte JWT → `EnterConnected` → Home, identisch zum Pairing-Pfad); Fehler → `BootstrapErrorFor(status)`.
- Fehlerkonstanten: `InvalidBootstrapInputError`, `BootstrapNeedsServerError`, `InvalidBootstrapTicketError`, `BootstrapRateLimitedError`, `BootstrapNotSupportedError`, `BootstrapServerUnreachableError`, `BootstrapFailedError` (deutsch, einfache Sprache, kein „-stellig").

## 6. Effekt-Handler (`ShellAppEffectHandler`)

- `CheckBootstrapServerAsync`: `healthProbe.CheckAsync(new Uri(address))` → `Success = result != Unreachable` (GateRejected = erreichbar) → `BootstrapServerCheckedMessage`.
- `ExchangeBootstrapTicketAsync`:
  1. `pairing.BootstrapAsync(new Uri(serverAddress), ticket, GetDeviceName(), ct)`.
  2. Stale-Guard wie `ExchangePairingCodeAsync` (`IsBootstrapRunning` + Adressvergleich): aktuell → `SetPairedTokenIfCurrentAsync` + `authSession.Client?.SetApiKey` → **`SetAuthTokenAsync` + `SetRefreshTokenAsync`** (Session nur im aktuellen Kontext persistieren); veraltet → nur `SetDeviceTokenAsync` unter eigener Adresse, Session verwerfen.
  3. `BootstrapCompletedMessage(status)`; Mapping: 401→`InvalidTicket`, 400→`InvalidRequest`, 429→`RateLimited`, 404→`NotSupported`, null→`ServerUnreachable`, sonst/`Exception`→`Failed`.
- `LogoutAsync` → ruft `authSession.LogoutAsync` (enthält jetzt den Revoke-Call; Handler unverändert).

## 7. UI

### `ServerSetupPage.xaml` — neue Karte `BootstrapSection` (immer sichtbar, zwischen `ConnectCard` und `PairingSection`)

- `QrScanButton` („QR-Code scannen", `IsVisible=false` → im Code-Behind `#if ANDROID || IOS` sichtbar), `BootstrapTicketEntry` (Placeholder „Ticket-Adresse oder Kurzcode", `IsSpellCheckEnabled`/`IsTextPredictionEnabled="False"`, kein `MaxLength`), `BootstrapButton` („Verbinden"), `BootstrapIndicator` + `BootstrapRunningLabel`, `BootstrapErrorLabel`. Vp*-Tokens, AutomationIds auf allem.
- `UpdateUi`: `Bootstrap*` aus `model.IsBootstrapRunning`/`model.BootstrapError` steuern; `BootstrapButton`/`Entry` disabled während `IsBootstrapRunning || IsServerCheckRunning`.
- `OnBootstrapClicked` → `BootstrapTicketSubmittedMessage(BootstrapTicketEntry.Text)`.
- `OnQrScanClicked` → `#if ANDROID || IOS`: `BarcodeScanning.IsSupported`-Check → `Permissions.RequestAsync<Permissions.Camera>()` → `PushModalAsync(QrScannerPage)`; verweigert → `BootstrapErrorLabel`-Text direkt? → nein: über MVU — `BootstrapError` setzen geht nur via Message… Vereinfachung: Permission verweigert → `DisplayAlert` mit Hinweis (lokal, kein Model-Fehler — der Fluss wurde nicht gestartet). Dokumentiert als bewusste Ausnahme (UI-lokaler Dialog wie andere Shell-Dialoge).

### `QrScannerPage` (neu, **code-only** `Pages/QrScannerPage.cs`, komplett in `#if ANDROID || IOS`)

- `Grid`: `CameraBarcodeReaderView` (`Options = new BarcodeReaderOptions { Formats = BarcodeFormats.TwoDimensional, AutoRotate = true }`), dunkler Rahmen + Hinweislabel „QR-Code der Serververwaltung scannen", „Abbrechen"-Button (`PopModalAsync`).
- `BarcodesDetected` (Event-Arg `BarcodeDetectionEventArgs.Results` → `BarcodeResult.Value`): erstes Ergebnis → einmalig `BootstrapTicketSubmittedMessage` dispatchen (Flag gegen Doppel-Detection) → `PopModalAsync`. ServerSetupPage zeigt Fortschritt/Fehler über das Model.
- Registrierung: direkte Instanziierung mit `MvuHost` aus DI (`serviceProvider.GetRequiredService<MvuHost>()`) — einfacher modal, keine Shell-Route nötig. `QrScannerPage` bekommt `MvuHost` per Konstruktor.

### `MauiProgram.cs` / csproj / Manifeste

- `builder.UseBarcodeReader()` unter `#if ANDROID || IOS`.
- `<PackageReference Include="ZXing.Net.Maui.Controls" Version="0.10.4" Condition="'$(TargetFramework)'=='net10.0-android' or '$(TargetFramework)'=='net10.0-ios'" />`.
- `Platforms/iOS/Info.plist`: `NSCameraUsageDescription` („Zum Scannen des Kopplungs-QR-Codes der Serververwaltung.").
- `Platforms/Android/AndroidManifest.xml`: `<uses-permission android:name="android.permission.CAMERA" />`.
- Windows unverändert (kein Paket, kein Scan, `QrScanButton` unsichtbar).

## 8. Dev-API

`DevApiModelSnapshot` + `DevApiModels.DevApiModel` (E2E): `IsBootstrapRunning`, `BootstrapError`. Keine Secrets.

## 9. Tests

### Unit (`VideoWebPlayer.Maui.Tests`)

- `BootstrapTicketParserTests`: URL-Formen (root, Unterpfad, Trailing-Slash, Port, Query-Encoding, `t` mittendrin/zwischen anderen Params), Kurzcode, Ablehnungen (ftp, kein /pairing, kein t, leeres t, Garbage, leer).
- `DevicePairingClientTests`: `BootstrapAsync` — Request-JSON (`ticket`/`clientPublicKey`/`deviceName`), kein Gate/Bearer, Entschlüsselung des JSON-Payloads (Test-Responder verschlüsselt `{deviceToken,token,expires,refreshToken}`), 400/401/429/404-Mapping, Transportfehler, kaputtes Payload → Exception.
- `AppUpdateTests_Bootstrap`: Submit-Übergänge (URL→Check-Effekt, Kurzcode→Exchange-Effekt, Kurzcode ohne Server→Fehler, Parser-Fehler→Fehler, Doppel-Submit), `BootstrapServerChecked` (nicht erreichbar, Erfolg→Persist+Exchange + Model), `BootstrapCompleted` (Erfolg→Connecting+AutoLogin, Fehler-Mapping, Stale).
- `AuthSessionServiceTests`: Refresh-first (401 auf `api/favorites` → `api/auth/refresh` → neuer Bearer + rotierter Refresh-Token persistiert), Refresh-401→Credential-Fallback, Refresh-401 ohne Credentials→SessionExpired, Refresh-Gate-401→GateRejected-Pfad, Logout→`api/auth/logout` aufgerufen + Refresh-Token gelöscht.
- `AppSecretsServiceTests`: Refresh-Token-Keys/Persistenz, `ClearCredentialsAsync` räumt Refresh-Token.
- `PairingContractTests`/`BootstrapContractTests` (neu): AutomationIds, kein `MaxLength`, Parser im Mvu-Projekt, kein `PairingCode.TryNormalize` auf Ticket, `UseBarcodeReader`/`CAMERA`/`NSCameraUsageDescription`/`#if ANDROID || IOS`/`QrScannerPage`-Pins, Dev-API secret-frei (`Assert.DoesNotContain("Ticket"`… auf Snapshot — Vorsicht: `BootstrapError`-Text ok, aber keine `Ticket`-/`Token`-Properties).

### E2E (`VideoPlayer.E2E.Tests`)

- `MockVideoServer`: `POST /api/pairing/bootstrap` (Ticket `BootstrapTicket` + Alias `BootstrapShortCode`; Body `{ticket,clientPublicKey,deviceName?}` → verschlüsseltes JSON-Payload via `MockPairingCrypto`), `POST /api/auth/refresh` (Rotation: `AuthToken`+`RefreshToken` wechseln — `IsBearerValid` nutzt aktuellen `AuthToken`), `POST /api/auth/logout` (200, `RefreshToken` invalidieren). Zähler `BootstrapCount`/`RefreshCount`/`LogoutCount`.
- `BootstrapTests`: **(a) Kurzcode-Flug Windows** (Pflicht): Connect→PairingRequired→`BootstrapTicketEntry`=Kurzcode→`BootstrapButton`→`CurrentPage=="Home" && ConnectionState=="Connected"`, `server.BootstrapCount==1`. **(b) Ticket-URL-Flug**: `BootstrapTicketEntry`=`{BaseAddress}pairing?t={ticket}` direkt vom Erststart (ohne vorherigen Connect) → Home; prüft Prüf→Persist→Bootstrap-Kette. (c) Ungültiges Ticket → `BootstrapError` sichtbar.

## 10. Doku & Verifikation

- `docs/help/serververwaltung-erststart.md` (+ ggf. `verbindung-anmeldung.md`): QR-/Ticket-Onboarding, Kurzcode, Windows-Einschränkung, Refresh-/Logout-Verhalten.
- `docs/help/api-client/api.md`: `BootstrapAsync`, `RefreshSessionAsync`, `LogoutSessionAsync`.
- `README.md` (QR-Onboarding erwähnen), `docs/RELEASE_NOTES.md` (Eintrag Schritt 5b).
- Screenshots via FlaUI/Dev-API-Harness → `live-verification/` (BootstrapSection, laufender Zustand, Fehler, Erfolg/Home).
- `test-results.md` mit exakten Zahlen beider Suiten.
- Optionaler Live-Lauf gegen Server-Branch #231 mit Scratch-DB (`dotnet ef database update` in Scratch-Umgebung) — sonst `continue.md`/Abnahme-Notiz als manueller Restnachweis.
