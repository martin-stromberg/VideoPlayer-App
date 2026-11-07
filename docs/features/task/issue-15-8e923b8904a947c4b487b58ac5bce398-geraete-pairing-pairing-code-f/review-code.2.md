# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### AuthSessionService.cs / DeviceTokenService.cs (Gate-Probe arbeitet mit dem ambienten Provider statt mit dem abgelehnten Server)

- **Nebenläufigkeit / falsche Kontextbindung** - `HandleGateRejectionAsync` (AuthSessionService.cs, Zeile 363–373) ruft `deviceTokens.IsGateRejectedAsync(serverAddress)` auf; die Probe sendet jedoch `ClientGate.Resolve()` (DeviceTokenService.cs, Zeile 159) — also das Token des *aktuell aktiven* Servers, nicht das der als Parameter übergebenen `serverAddress`. Bei einem Serverwechsel während eines noch laufenden Auto-Logins bzw. einer stillen Neuanmeldung des alten Servers A (Provider wurde über `PersistActiveServerAsync` → `LoadForServerAsync` bereits auf Server B umgestellt) prüft die Probe gegen A das Token von B. Lehnt A ab, verwirft `DiscardCurrentAsync()` (Zeile 370) das *frisch aktivierte Token von B* — nicht das abgelehnte Token von A — und `NotifyRejected` zwingt die App in die Re-Pairing-Eingabe für den falschen Server. Es ist derselbe Befund-Typ wie der in Runde 1 behobene `ServerHealthProbe`-Fall (adressspezifische statt ambiente Schlüsselauflösung), auf dem Auth-Pfad aber noch offen.

  Empfehlung: In `IsGateRejectedAsync` den Gate-Schlüssel adressspezifisch auflösen (`secrets.GetDeviceTokenAsync(serverAddress)`, Fallback `ClientGate.ResolveStatic()` — Muster `ServerHealthProbe.ResolveGateKeyAsync`, ggf. gemeinsam nutzen). Bei Ablehnung das Token von `serverAddress` gezielt löschen (`secrets.ClearDeviceTokenAsync(serverAddress)`), den Provider nur über `DiscardIfCurrentAsync(serverAddress)` zurücksetzen und `NotifyRejected` nur auslösen, wenn der abgelehnte Server noch der aktuelle ist.

### ShellAppEffectHandler.cs (ExchangePairingCodeAsync — verbleibende Check-then-act-Lücke im Stale-Guard)

- **Nebenläufigkeit (gering)** - Der in Runde 1 geforderte Stale-Guard (Zeile ~1219–1235) ist implementiert, aber nicht atomar: `MvuHost.DispatchAsync` hält das `gate`-Semaphore nur während des Updates — Effekte verschiedener Dispatchs laufen parallel zueinander. Zwischen dem Lesen von `model.IsPairingRunning`/`ActiveServerAddress` und `SetPairedTokenAsync`/`SetApiKey` kann ein paralleler `PersistActiveServerAsync` → `LoadForServerAsync` den Provider auf einen anderen Server umstellen; das Pairing-Ergebnis überschreibt ihn dann doch noch. Das Zeitfenster ist klein, aber real (z. B. Pairing-Submit und sofortiger Klick auf einen anderen Server-Kandidaten).

  Empfehlung: Den Kontext-Check in den `DeviceTokenService` verlagern und atomar machen — z. B. `SetPairedTokenIfCurrentAsync(serverAddress, token)`, das unter einem privaten Lock/Semaphore nur dann persistiert + Provider aktiviert, wenn `CurrentServerAddress` noch `serverAddress` entspricht (in allen legitimen Fällen wurde der Pairing-Server vorher über `PersistActiveServerAsync` zum aktuellen Server); andernfalls nur unter der eigenen Adresse ablegen. Der Model-basierte Vorab-Check kann als schneller Filter bestehen bleiben.

### MauiProgram.cs (DevicePairingClient ohne Timeout)

- **Kopplung / Konfiguration (gering)** - Die DI-Registrierung `new DevicePairingClient(new HttpClient())` (Zeile ~39) erzeugt einen Client ohne gesetztes `Timeout` — Default 100 s. Alle anderen HTTP-Clients des Features definieren ein explizites Timeout (`ServerHealthProbe` 10 s, `AuthSessionService` 30 s, `DeviceTokenService`-Probe 10 s). Ein hängender Pairing-Endpunkt lässt `IsPairingRunning` bis zu 100 s auf dem Indikator stehen, ohne dass ein Fehlerpfad greift.

  Empfehlung: Dem injizierten `HttpClient` ein Timeout im Rahmen der übrigen Clients setzen (z. B. 10–30 s, als Konstante am `DevicePairingClient` oder in der Registrierung).

## Befunde aus Runde 1 — Verifikation der Abarbeitung

Alle sieben Befunde plus der Nachtrag (Befund 8) sind im aktuellen Stand behoben:

1. **Stale-Seiteneffekte in `ExchangePairingCodeAsync`** — behoben: Provider-/Client-Aktivierung nur bei noch aktuellem Pairing-Kontext (`IsPairingRunning` + Adressgleichheit); ein veraltetes Ergebnis hinterlegt das Token nur unter seiner eigenen Serveradresse. Restlücke siehe Befund 2 oben.
2. **Doppelte Token-Persistierung** — behoben: im Erfolgspfad nur noch `SetPairedTokenAsync`; `secrets.SetDeviceTokenAsync` verbleibt nur im bewusst anderen Stale-Zweig.
3. **Gate-401 vs. Bearer-401 auf dem Auto-Login-Pfad** — behoben: `TryStoredTokenAsync` und `LoginCoreAsync` werten `HandleGateRejectionAsync` aus und liefern `AuthAttemptStatus.GateRejected`; `UpdateAutoLoginCompleted`/`UpdateLoginCompleted` steuern über `UpdateDeviceTokenRevoked` auf die Re-Pairing-Eingabe statt auf die Anmeldemaske (idempotent, per Tests belegt).
4. **`IsGateRejectedAsync` schluckt angeforderten Abbruch** — behoben: `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }` vor dem Fehlerfilter, plus Test `IsGateRejected_RequestedCancellation_Propagates`.
5. **Doppel-Submit `PairingCodeSubmittedMessage`** — behoben: Guard `if (model.IsPairingRunning) return Unchanged`, Test `Update_PairingCodeSubmitted_WhileRunning_IsIgnored`.
6. **Testisolation des statischen `ClientGate.DeviceTokenProvider`** — behoben: `[Collection("ClientGate")]` (`Stubs/ClientGateCollection.cs`) auf `ClientGateTests`, `DeviceTokenServiceTests` und `AuthSessionServiceTests`; `ApiClientTestFactory` setzt `ApiKey` explizit und ist damit provider-unabhängig.
7. **`ServerHealthProbe` sendet fremdes Geräte-Token an Kandidaten** — behoben: `ResolveGateKeyAsync` löst pro geprüfter Adresse auf (hinterlegtes Token des Kandidaten, sonst `ClientGate.ResolveStatic()`); Contract-Test `ServerHealthProbe_ResolvesGateKeyPerAddress` sichert ab, dass kein `ClientGate.Resolve()` verwendet wird.
8. **Gate-Probe auf ungeschütztem `api/health`** — behoben: `IsGateRejectedAsync` läuft als `POST api/auth/login` mit `{}`-Body, nur `X-API-Key`, ohne Bearer; Auswertung `401` + `application/problem+json` → `true`, `401` + `text/plain`, `400`, `429`, sonstige/Fehler → `false`. Gegen den echten Server live verifiziert (`live-verification.md`); Contract-Test `DeviceTokenService_GateProbe_UsesLoginEndpoint`.

## Geprüfte Feature-Schwerpunkte (Ergebnis)

- **Krypto-Korrektheit** (`PairingCrypto`): ECDH nistP256, SPKI/DER-Export, `DeriveKeyFromHmac` mit SHA-256 (`hmacKey: null` = Shared Secret als HMAC-Schlüssel), AES-256-GCM mit Layout `nonce(12)‖ct‖tag(16)` — korrekt; Roundtrip-, Manipulations- und Fehlerfall-Tests vorhanden.
- **`DevicePairingClient`**: sendet ohne Gate-/Bearer-Header, ohne Retry und ohne Re-Auth (eigener `HttpClient`, direkter `SendAsync`); HTTP-Fehler als `ApiException` mit Statuscode, Transportfehler mit `StatusCode = null`; Abbruch wird durchgereicht. Vertrag zusätzlich per Contract-Test abgesichert.
- **SecureStorage-Nutzung**: Geräte-Tokens ausschließlich in `ISecretStore` (`AppSecretsService.DeviceTokenKey`, profil- und serveradress-normalisiert), nie in der Einstellungsablage — per Tests belegt.
- **Statischer `ClientGate.DeviceTokenProvider`**: Delegate-Austausch atomar; Mutationspunkte (`ApplyProvider`) zentral in `DeviceTokenService`; `ResolveStatic()` als Fallback sauber getrennt.
- **`HandleGateRejectionAsync` in allen 401/403-Pfaden**: `TryStoredTokenAsync`, `LoginCoreAsync`, `SilentReloginAsync` — vollständig; dazu `RunHealthCheckAsync` (GateRejected → Discard → einmalige Nachprüfung → `NotifyRejected`) und `CheckCandidateServerAsync` (`DiscardIfCurrentAsync`).
- **Stale-Guards**: `UpdatePairingCompleted` (`IsPairingRunning`), `UpdateDeviceTokenRevoked` (Doppelauslösung), `BeginServerCheck`/Check-Abschluss setzen den Pairing-Kontext zurück; zusätzlich Handler-seitiger Guard (siehe Befund 2 für die Restlücke).

## Verifikation

- `dotnet build VideoWebPlayer.Maui.Tests`: 0 Warnungen, 0 Fehler.
- `dotnet test VideoWebPlayer.Maui.Tests`: 862/862 Tests bestanden.

## Geprüfte Dateien

Quellcode:
- `VideoPlayer.ApiClient/ClientGate.cs`
- `VideoPlayer.ApiClient/DevicePairingClient.cs` (neu)
- `VideoPlayer.ApiClient/HealthCheckResult.cs` (neu)
- `VideoPlayer.ApiClient/Models/PairingExchangeRequest.cs` (neu)
- `VideoPlayer.ApiClient/Models/PairingExchangeResponse.cs` (neu)
- `VideoPlayer.ApiClient/PairingCrypto.cs` (neu)
- `VideoPlayer.ApiClient/VideoPlayerApiClient.cs`
- `VideoPlayer.Mvu/AppEffect.cs`
- `VideoPlayer.Mvu/AppMessage.cs`
- `VideoPlayer.Mvu/AppModel.cs`
- `VideoPlayer.Mvu/AppUpdate.cs`
- `VideoPlayer.Mvu/AuthAttempt.cs`
- `VideoPlayer.Mvu/AuthSessionService.cs`
- `VideoPlayer.Mvu/DeviceTokenService.cs` (neu)
- `VideoPlayer.Mvu/PairingCode.cs` (neu)
- `VideoPlayer.Mvu/PairingStatus.cs` (neu)
- `VideoPlayer.Storage/AppSecretsService.cs`
- `VideoPlayer.Storage/KnownServersService.cs`
- `VideoPlayer.Maui/MauiProgram.cs`
- `VideoPlayer.Maui/Pages/ServerSetupPage.xaml`
- `VideoPlayer.Maui/Pages/ServerSetupPage.xaml.cs`
- `VideoPlayer.Maui/Services/IServerHealthProbe.cs`
- `VideoPlayer.Maui/Services/ServerHealthProbe.cs`
- `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`
- `scripts/iOS-Deployment.ps1`

Tests:
- `VideoWebPlayer.Maui.Tests/AppSecretsServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/AppUpdateTests_Connection.cs`
- `VideoWebPlayer.Maui.Tests/AppUpdateTests_Offline.cs`
- `VideoWebPlayer.Maui.Tests/AppUpdateTests_Pairing.cs` (neu)
- `VideoWebPlayer.Maui.Tests/AppUpdateTests_ServerManagement.cs`
- `VideoWebPlayer.Maui.Tests/AppUpdateTests_Startup.cs`
- `VideoWebPlayer.Maui.Tests/AuthSessionServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/ClientGateTests.cs`
- `VideoWebPlayer.Maui.Tests/DevicePairingClientTests.cs` (neu)
- `VideoWebPlayer.Maui.Tests/DeviceTokenServiceTests.cs` (neu)
- `VideoWebPlayer.Maui.Tests/EnumCoverageTests.cs`
- `VideoWebPlayer.Maui.Tests/MvuHostTests.cs`
- `VideoWebPlayer.Maui.Tests/PairingCodeTests.cs` (neu)
- `VideoWebPlayer.Maui.Tests/PairingContractTests.cs` (neu)
- `VideoWebPlayer.Maui.Tests/Stubs/ClientGateCollection.cs` (neu)
- `VideoWebPlayer.Maui.Tests/VideoPlayerApiClientTests_Authentication.cs`

Dokumentation (geändert, nur überflogen — keine Quelldateien):
- `docs/features/task/issue-15-8e923b8904a947c4b487b58ac5bce398-geraete-pairing-pairing-code-f-tasks.md`
- `docs/features/task/issue-15-8e923b8904a947c4b487b58ac5bce398-geraete-pairing-pairing-code-f/plan.md`, `todo.md`
- `docs/help/api-client/ablauf-technisch.md`, `api.md`, `installation.md`, `troubleshooting.md`
- `docs/help/serververwaltung-erststart.md`, `docs/help/verbindung-anmeldung.md`
