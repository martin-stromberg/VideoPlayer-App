# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### ShellAppEffectHandler.cs (ShellAppEffectHandler)

- **Nebenläufigkeit / Stale-Result** - `ExchangePairingCodeAsync` (ca. Zeile 1173): Die Seiteneffekte des Erfolgsfalls (`SetPairedTokenAsync` → setzt den globalen `ClientGate.DeviceTokenProvider` und `CurrentServerAddress`, `authSession.Client?.SetApiKey`) werden angewendet, bevor geprüft wird, ob der Pairing-Kontext noch aktuell ist. Der Stale-Guard in `AppUpdate.UpdatePairingCompleted` (`IsPairingRunning == false` → `Unchanged`) verwirft nur die Nachricht, nicht die bereits erfolgten Seiteneffekte. Szenario: Während `ExchangePairingCodeEffect` läuft, startet der Benutzer einen neuen Servercheck (`ConnectButton`/Serverlisten bleiben aktiv, `UpdateUi` sperrt nur die Pairing-Controls bei `IsPairingRunning`); `PersistActiveServerAsync` → `LoadForServerAsync` stellt den Provider auf den neuen Server — ein danach eintreffendes Pairing-Ergebnis des alten Servers überschreibt Provider/`CurrentServerAddress`/API-Client-Header mit dem Token des alten Servers. Folgeanfragen an den neuen Server senden dann den falschen Gate-Schlüssel (401-Kaskade, ggf. vermeintlicher Widerruf inkl. Löschung des frisch gespeicherten Tokens).

  Empfehlung: Vor `SetPairedTokenAsync`/`SetApiKey` prüfen, ob der übergebene `serverAddress`-Parameter noch dem aktiven Server entspricht (z. B. `deviceTokens.CurrentServerAddress` bzw. `MvuHost.Model.ActiveServerAddress`), oder die Token-Aktivierung in die Verarbeitung der `PairingCompletedMessage` verlagern, wo der Stale-Guard bereits greift.

- **Doppelter Code / überflüssige Persistierung** - `ExchangePairingCodeAsync` (ca. Zeile 1180): `secrets.SetDeviceTokenAsync(serverAddress, token)` und das direkt folgende `deviceTokens.SetPairedTokenAsync(serverAddress, token)` schreiben dasselbe Token zweimal in die sichere Ablage (`SetPairedTokenAsync` persistiert selbst über `SetDeviceTokenAsync`). Funktional harmlos (idempotent), aber redundant und verwirrend.

  Empfehlung: Den direkten `secrets.SetDeviceTokenAsync`-Aufruf entfernen — `SetPairedTokenAsync` übernimmt Persistenz und Provider-Aktivierung allein.

### AuthSessionService.cs / AppUpdate.cs (Gate-401 vs. Bearer-401 auf dem Auto-Login-Pfad)

- **Fehlerbehandlung / fachliche Unterscheidung unvollständig** - Die Gate-Probe (`IsGateRejectedAsync`) wird nur in `SilentReloginAsync` (Zeile ~313) aufgerufen. Läuft ein `AttemptAutoLoginEffect` mit widerrufenem Geräte-Token (z. B. Serverwechsel zu einem bekannten Server mit hinterlegtem, inzwischen serverseitig gelöschtem Token), ergibt sich folgende Kette: `TryStoredTokenAsync` (Zeile ~203) fängt die Gate-401 ohne Probe → `TryStoredLoginAsync` fällt auf `LoginCoreAsync` → erneute Gate-401 → `AuthAttemptStatus.InvalidCredentials` → `AutoLoginCompletedMessage`. Das zwischenzeitlich ausgelöste `DeviceTokenRevokedMessage` (fire-and-forget aus `NotifyRejected`) wird danach von `UpdateAutoLoginCompleted` (AppUpdate.cs, ca. Zeile 570) überschrieben: Die Methode setzt unbedingt `ConnectionState.NeedsLogin`/`CurrentPage.Login`. Ergebnis: Der Benutzer landet auf der Anmeldemaske, die am Client-Gate nie vorbeikommt (jede Anmeldung → 401 → `InvalidCredentials`) — die Re-Pairing-Eingabe bleibt unerreichbar, obwohl der Widerruf korrekt erkannt wurde.

  Empfehlung: Entweder im 401/403-Catch von `TryStoredTokenAsync`/`LoginCoreAsync` ebenfalls `IsGateRejectedAsync` auswerten und einen eigenen Status (z. B. `AuthAttemptStatus.GateRejected`) liefern, der nicht nach Login navigiert — oder in `UpdateAutoLoginCompleted` bei gesetztem `IsPairingRequired`/`ConnectionState.NeedsServerSetup` nicht mehr auf die Login-Seite übersteuern.

### DeviceTokenService.cs (DeviceTokenService)

- **Fehlerbehandlung** - `IsGateRejectedAsync` (Zeile 150): Der Filter `catch (Exception) when (ex is ... or TaskCanceledException ...)` schluckt auch einen angeforderten Abbruch über `cancellationToken` und liefert `false`. Der Aufrufer `SilentReloginAsync` interpretiert das als „Gate akzeptiert" und löst anschließend `ClearAuthTokenAsync` + `SessionExpired` aus — ein bloßer Abbruch (z. B. App-Shutdown) würde so das gespeicherte Anmelde-Token verwerfen und eine falsche Sitzungsablauf-Meldung erzeugen.

  Empfehlung: Vor dem Filter `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }` ergänzen (Muster wie in `DevicePairingClient.ExchangeAsync`/`SendWithPolicyAsync`).

### AppUpdate.cs (AppUpdate)

- **Fehlende Vorbedingung / Reentrancy** - `UpdatePairingCodeSubmitted` (ca. Zeile 469): Es gibt keinen Guard gegen eine zweite `PairingCodeSubmittedMessage` bei laufendem Austausch (`IsPairingRunning == true`). Die UI sperrt `PairButton` erst nach der Model-Aktualisierung — ein Doppeltippen oder programmatischer Dispatch startet zwei parallele `ExchangePairingCodeEffect`s, deren Seiteneffekte sich gegenseitig überholen können (vgl. ersten Befund).

  Empfehlung: Am Anfang `if (model.IsPairingRunning) return AppTransition.Unchanged(model);` ergänzen.

### ClientGate.cs + Tests (Testisolation des statischen Providers)

- **Testqualität / Nebenläufigkeit** - `ClientGate.DeviceTokenProvider` ist ein veränderlicher statischer Zustand. `ClientGateTests` (`Resolve_PublicOverload_ConsultsDeviceTokenProvider`, `ResolveStatic_IgnoresDeviceTokenProvider`), `DeviceTokenServiceTests` (Provider-Aktivierung, Reset in `Dispose`) und `AuthSessionServiceTests` (Reset in `finally`) mutieren denselben statischen Slot. Die Testklassen gehören keiner gemeinsamen xUnit-Collection an und laufen parallel — gegenseitige Beeinflussung ist möglich (z. B. `Resolve()` in `DeviceTokenServiceTests` liest einen fremden Provider-Wert oder ein parallel laufendes `finally`/`Dispose` setzt den Provider während einer Assertion auf `null`). Aktuell grün, aber latent flaky.

  Empfehlung: Die drei Klassen in eine gemeinsame `[Collection]` (z. B. „ClientGate") fassen oder `ClientGate.DeviceTokenProvider` in den Tests ausschließlich innerhalb serialisierter Kontexte setzen.

### ServerHealthProbe.cs / ShellAppEffectHandler.cs (Kandidatenprüfung)

- **Kopplung / Informationsweitergabe (gering)** - `ServerHealthProbe.CheckAsync(Uri)` erzeugt den `VideoPlayerApiClient` mit `ClientGate.Resolve()` — der Gate-Header an eine beliebige Kandidatenadresse enthält damit das Geräte-Token des *aktuell aktiven* Servers, nicht das des Kandidaten. Fachlich korrekt behandelt (`DiscardIfCurrentAsync` greift nur für den aktiven Server; `IsPairingRequiredAsync` prüft den Kandidaten-Token-Bestand), aber ein frei eingegebener oder per mDNS gefundener Server erhält das fremde Geräte-Token mitgeschickt — Credential-Exfiltration an beliebige Hosts (vorher: statischer Schlüssel, jetzt: gerätespezifisches Token).

  Empfehlung: Bei Kandidatenprüfungen den Gate-Schlüssel zielgerichtet auflösen (hinterlegtes Token des Kandidaten bzw. `ResolveStatic()`), statt pauschal den aktiven Provider zu nutzen.

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
- `VideoWebPlayer.Maui.Tests/VideoPlayerApiClientTests_Authentication.cs`

Dokumentation (geändert, nur überflogen — keine Quelldateien):
- `docs/features/task/issue-15-8e923b8904a947c4b487b58ac5bce398-geraete-pairing-pairing-code-f-tasks.md`
- `docs/features/task/issue-15-8e923b8904a947c4b487b58ac5bce398-geraete-pairing-pairing-code-f/todo.md`
- `docs/help/api-client/ablauf-technisch.md`, `api.md`, `installation.md`, `troubleshooting.md`
- `docs/help/serververwaltung-erststart.md`, `docs/help/verbindung-anmeldung.md`

## Verifikation

- `dotnet build VideoWebPlayer.Maui.Tests`: 0 Warnungen, 0 Fehler.
- `dotnet test VideoWebPlayer.Maui.Tests`: 846/846 Tests bestanden.

## Geprüfte Feature-Schwerpunkte (Ergebnis)

- Krypto-Korrektheit: ECDH nistP256, `DeriveKeyFromHmac` mit SHA-256 (hmacKey `null` = Shared Secret als HMAC-Schlüssel), AES-256-GCM-Layout `nonce(12)‖ct‖tag(16)` — korrekt implementiert und per Roundtrip-/Fehlerfall-Tests abgesichert.
- `DevicePairingClient`: sendet ohne Gate-/Bearer-Header und ohne Retry (eigener `HttpClient`, `SendAsync` direkt) — korrekt; Vertrag zusätzlich per Contract-Test abgesichert.
- SecureStorage statt Preferences: Geräte-Tokens liegen ausschließlich in `ISecretStore` (`AppSecretsService.DeviceTokenSecretKey`, profil- und serveradress-spezifisch) — korrekt, per Test belegt.
- Statischer `ClientGate.DeviceTokenProvider`: Delegate-Austausch atomar, kein Locking nötig; Nebenläufigkeits-Probleme liegen in den aufrufenden Effekten (siehe Befunde 1 und 6).
- Gate-401 vs. Bearer-401: korrekt in `SilentReloginAsync` und `RunHealthCheckAsync` getrennt; Lücke auf dem Stored-Token-/Login-Pfad (siehe Befund 2).
- Stale-Result-Guards im MVU-Update: `UpdatePairingCompleted` (`IsPairingRunning`) und `UpdateDeviceTokenRevoked` (Doppelauslösung) vorhanden; Seiteneffekte am App-Rand laufen jedoch ungeprüft weiter (Befund 1), Doppel-Submit ungeschützt (Befund 5).
- Doppelte Token-Persistierung in `ExchangePairingCodeAsync`: harmlos (idempotent), aber redundant (Befund 4).

## Nachtrag — Live-Verifikation gegen den IIS-Testserver (Orchestrator, 2026-09-23)

Nachweis: `live-verification.md`. Der Pairing-Endpunkt wurde mit dem echten `DevicePairingClient` gegen `http://localhost:83` gefahren (Code → ECDH → AES-GCM → Token → Login 200).

### Befund 8 (mittelschwer): `IsGateRejectedAsync` probiert einen nicht-gated Endpunkt

`DeviceTokenService.IsGateRejectedAsync` sendet `GET api/health` nur mit `X-API-Key` und erwartet 401 bei widerrufenem Token. Gegen den realen Server liefert `api/health` jedoch **immer 200** — der `HealthController` trägt kein `ApiTokenCheck`-Attribut. Folge: Die Probe kann eine Gate-Ablehnung niemals erkennen; `SilentReloginAsync`/`TryStoredTokenAsync`/`LoginCoreAsync` fallen bei widerrufenem Geräte-Token immer auf `SessionExpired`/`InvalidCredentials` zurück → Sackgasse (AC 5 verletzt). Ebenso ist `HealthCheckResult.GateRejected` über `CheckHealthAsync` (`api/health`) auf diesem Server unerreichbar — der Pfad bleibt als Defense-in-depth für Server mit gated Health bestehen, ist aber nicht der primäre Widerrufs-Detektor.

**Fix:** Die Probe muss gegen den einzigen MauiOnly-gated Endpunkt `POST api/auth/login` laufen (Body `{}`, nur `X-API-Key`-Header, kein Bearer) und anhand des `Content-Type` der 401-Antwort unterscheiden:

- `401` + `application/problem+json` → Gate-Ablehnung (`UnauthorizedResult` aus `ApiTokenCheckAttribute`) → `true`
- `401` + `text/plain` (Credential-Fehler, z. B. „Benutzer nicht gefunden.") → Key akzeptiert → `false`
- `400` (Model-Validation) → Key akzeptiert → `false`
- `429` / Transportfehler / andere → nicht entscheidbar → `false`

Hinweis: `LoginIpBlockService.RegisterFailure` wird nur im `PairingController` aufgerufen — Login-Probes erzeugen keine IP-Sperr-Fehlversuche. Tests für `IsGateRejectedAsync` entsprechend umstellen (Stub-Antworten: 401 problem+json vs. 401 text/plain vs. 400 vs. 429). Auch `plan.md` wurde in diesem Punkt nachträglich korrigiert.