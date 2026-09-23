# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### ShellAppEffectHandler.cs (CheckCandidateServerAsync — abgelehntes Kandidaten-Token bleibt hinterlegt)

- **Fehlerbehandlung / fachliche Logik (gering–mittel)** - `CheckCandidateServerAsync` (Zeile 1074–1086): Bei `HealthCheckResult.GateRejected` wird nur `deviceTokens.DiscardIfCurrentAsync(address)` aufgerufen — das verworfene Token wird also nur gelöscht, wenn der Kandidat zufällig der aktive Server ist. Der danebenstehende Kommentar („die Ablehnung eines fremden Tokens sagt nichts über seine Gültigkeit am eigenen Server aus") ist seit der Runde-2-Korrektur veraltet: `ServerHealthProbe.ResolveGateKeyAsync` löst den Gate-Schlüssel inzwischen adressspezifisch auf — die Probe sendet das *eigene* hinterlegte Geräte-Token des Kandidaten (oder die statische Quelle). Ein 401 beweist damit gerade die Ungültigkeit des Kandidaten-Tokens, nicht die eines fremden. Verbleibt das tote Token für einen nicht-aktiven Kandidaten in der sicheren Ablage, liefert `IsPairingRequiredAsync(address)` (Zeile 1086) über `HasDeviceTokenAsync` fälschlich `false` → `pairingRequired = false` → der Server wird persistiert und ein Auto-Login gestartet, der garantiert am Gate scheitert (`HandleGateRejectionAsync` → `ClearDeviceTokenAsync` → `DeviceTokenRevokedMessage`). Der Benutzer landet — nach einer aussichtslosen Anmelderunde — in der Re-Pairing-Eingabe mit `DeviceRevokedError`, statt direkt die Pairing-Aufforderung zu sehen. Auf dem aktuellen Serverstand dormant (ungated `api/health` liefert nie 401 — siehe `live-verification.md`), auf Servern mit gated Health-Endpunkt aber wirksam.

  Empfehlung: Bei `GateRejected` das Geräte-Token der geprüften Adresse gezielt löschen (`secrets.ClearDeviceTokenAsync(address)` — Muster `HandleGateRejectionAsync`), `DiscardIfCurrentAsync(address)` für den Provider-Reset beibehalten und erst danach `IsPairingRequiredAsync` aufrufen. Dadurch ergibt `pairingRequired` den korrekten Wert (Token weg → nur noch statische Quelle entscheidet) und der Umweg über den scheiternden Auto-Login entfällt.

### DeviceTokenService.cs (DiscardIfCurrentAsync — Check-then-act außerhalb des Locks)

- **Nebenläufigkeit (gering)** - `DiscardIfCurrentAsync` (Zeile 202–211): Die Adressprüfung liest `CurrentServerAddress` ungeschützt (Zeile 204); `DiscardCurrentAsync` liest den Wert erst unter `providerLock` erneut — und verwirft dann das, was *dort* aktuell ist. Schiebt sich zwischen Prüfung und Lock-Eintritt ein `LoadForServerAsync(B)` (Serverwechsel), trifft das Verwerfen den frisch aktivierten Server B: Provider-Rücksetzung auf statische Auflösung **und** Löschung von Bs gültigem hinterlegtem Token (`secrets.ClearDeviceTokenAsync(current)` mit `current = B`) — für eine Ablehnung, die Server A betraf. Anschließendes `NotifyRejected` zwingt die App zudem in die Re-Pairing-Eingabe. Es ist dieselbe Lückenklasse, die in dieser Runde bei `SetPairedTokenIfCurrentAsync` geschlossen wurde; das Fenster ist klein, aber die Folge (Verlust eines gültigen Geräte-Tokens) nicht harmlos.

  Empfehlung: Vergleich und Provider-Rücksetzung atomar unter `providerLock` ausführen — Adressgleichheit im Lock prüfen, `CurrentServerAddress` nur bei Treffer auf `null` setzen und `ApplyProvider(null)` aufrufen, die Store-Löschung anschließend nach dem Release (wie in `DiscardCurrentAsync`). Zusätzlich `ArgumentException.ThrowIfNullOrWhiteSpace(serverAddress)` ergänzen: `SameServer` würde bei `null` per `right.Trim()` eine `NullReferenceException` werfen statt einer sauberen `ArgumentException`.

### DeviceTokenService.cs (SetPairedTokenAsync — kein Produktivaufrufer)

- **Toter Code (gering)** - `SetPairedTokenAsync` (Zeile 99–115) wird im Anwendungscode nicht mehr aufgerufen — `ShellAppEffectHandler` nutzt ausschließlich `SetPairedTokenIfCurrentAsync`; die unbedingte Variante wird nur noch von Tests verwendet (`DeviceTokenServiceTests`, `AuthSessionServiceTests`). Zudem setzt sie `CurrentServerAddress` ohne Stale-Prüfung — als Produktivpfad wäre sie gerade der Nebenläufigkeits-Fall, den `SetPairedTokenIfCurrentAsync` absichtlich vermeidet.

  Empfehlung: Entweder entfernen und die Tests über `LoadForServerAsync` + `SetPairedTokenIfCurrentAsync` arrangieren — oder, falls die unbedingte Aktivierung als bewusste API-Variante bleiben soll, den Zweck im XML-Doc festhalten (z. B. „Aktivierung ohne Stale-Guard — nur für Tests/Initialzustand").

## Befunde aus Runde 2 — Verifikation der Abarbeitung

Alle drei Befunde der zweiten Runde sind im aktuellen Stand behoben:

1. **Adressspezifische Gate-Probe** — behoben: `IsGateRejectedAsync` löst den Gate-Schlüssel über `ResolveGateKeyAsync` (DeviceTokenService.cs, Zeile 288–306) aus dem hinterlegten Token der geprüften Adresse bzw. `ClientGate.ResolveStatic()` auf — kein `ClientGate.Resolve()` mehr (Contract-Test `DeviceTokenService_GateProbe_UsesLoginEndpoint` verbietet es per `DoesNotContain`). `HandleGateRejectionAsync` (AuthSessionService.cs, Zeile 366–391) löscht das Token adressspezifisch (`secrets.ClearDeviceTokenAsync(address)`), setzt den Provider nur über `DiscardIfCurrentAsync(address)` zurück und meldet `NotifyRejected` nur, wenn der abgelehnte Server noch aktuell ist. Belegt durch `IsGateRejected_UsesAddressSpecificToken_NotAmbientProvider`, `IsGateRejected_WithoutStoredToken_UsesStaticResolution` und `LoginAsync_GateRejected_ForNonCurrentServer_KeepsActiveTokenAndStaysSilent`. Verbleibende Restlücke in `DiscardIfCurrentAsync` selbst: Befund 2 oben.
2. **Atomarer Stale-Guard** — behoben: `SetPairedTokenIfCurrentAsync` (DeviceTokenService.cs, Zeile 135–159) persistiert und aktiviert unter `providerLock` nur bei noch aktuellem Server — Check und Act sind nicht mehr trennbar. Der Model-Vorab-Check in `ExchangePairingCodeAsync` (`IsPairingRunning` + Adressgleichheit, Zeile ~1237–1243) bleibt als schneller Filter erhalten. Belegt durch `SetPairedTokenIfCurrent_StaleServer_StoresOnlyWithoutActivating` und Contract-Test `EffectHandler_PairingExchange_GuardsStaleContext`.
3. **`DevicePairingClient`-Timeout** — behoben: `RequestTimeout = 15 s` als Konstante am Client (DevicePairingClient.cs, Zeile 26); die DI-Registrierung setzt `new HttpClient { Timeout = DevicePairingClient.RequestTimeout }` (MauiProgram.cs, Zeile ~38). Contract-Test `MauiProgram_RegistersPairingServices` sichert beides ab.
4. **Gelockerte Code-Längenprüfung** — behoben: `PairingCode.MinLength = 4` (Server-Minimum der Option `Pairing:CodeLength`), keine Obergrenze in der Formatprüfung; XAML `MaxLength="32"` als grobes Eingabelimit. Belegt durch `TryNormalize_AcceptsServerConfiguredLengths` und `Update_PairingCodeSubmitted_ServerConfiguredLength_StartsExchange`.

## Geprüfte Feature-Schwerpunkte (Ergebnis)

- **Krypto (`PairingCrypto`)**: ECDH nistP256, SPKI/DER-Export, `DeriveKeyFromHmac` SHA-256 (`hmacKey: null` = Shared Secret als HMAC-Schlüssel), AES-256-GCM `nonce(12)‖ct‖tag(16)` — korrekt; Fehlerfälle (kein Base64, zu kurzes Paket, Manipulation, falscher Serverschlüssel) per Tests abgedeckt.
- **`DevicePairingClient`**: ohne Gate-/Bearer-Header, ohne Retry, ohne Re-Auth (eigener `HttpClient`, direkter `SendAsync`); HTTP-Fehler als `ApiException` mit Statuscode, Transportfehler mit `StatusCode = null`; angeforderter Abbruch wird durchgereicht, Timeout-`TaskCanceledException` wird als `ServerUnreachable` gewrappt — korrekt.
- **SecureStorage-Nutzung**: Geräte-Tokens ausschließlich über `ISecretStore` (`AppSecretsService.DeviceTokenKey`, profil- und adressnormalisiert), nie in `ISettingsStore` — per Tests belegt.
- **Gate-Probe-Semantik**: `POST api/auth/login` mit `{}`-Body, nur `X-API-Key`, kein Bearer; `401` + `application/problem+json` → `true`, `401` + `text/plain`/`400`/`429`/Fehler → `false`; live gegen den IIS-Testserver verifiziert (`live-verification.md`).
- **Nebenläufigkeit**: `providerLock` serialisiert Provider-Mutationen (`LoadForServerAsync`, `SetPairedTokenIfCurrentAsync`, `DiscardCurrentAsync` schreiben Adresse+Provider atomar); verbleibende Lücke siehe Befund 2.
- **`HandleGateRejectionAsync`-Adressbezug**: Probe, Store-Löschung, Provider-Reset und Widerrufs-Meldung binden alle an die abgelehnte Adresse bzw. den aktuellen Server — korrekt (siehe Verifikation Runde 2, Punkt 1).
- **Testisolation**: `[Collection("ClientGate")]` auf `ClientGateTests`, `DeviceTokenServiceTests`, `AuthSessionServiceTests` serialisiert die Nutzer des statischen `ClientGate.DeviceTokenProvider`; Resets in `finally`/`Dispose` vorhanden.
- **DI-Verdrahtung**: `AuthSessionService` erhält den `DeviceTokenService`-Singleton über den 4-Parameter-Konstruktor (MS.DI wählt den längsten auflösbaren Konstruktor; die optionalen `Func`-Parameter fallen auf `null`) — `DeviceTokenRejected`-Abonnement und Gate-Probe teilen also dieselbe Instanz.

## Verifikation

- `dotnet build VideoWebPlayer.Maui.Tests`: 0 Warnungen, 0 Fehler.
- `dotnet test VideoWebPlayer.Maui.Tests`: 874/874 Tests bestanden.

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
