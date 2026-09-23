# Umsetzungsplan: Geräte-Pairing per Einmal-Code und gerätegebundenes Token

## Übersicht

Die App `VideoPlayer.Maui` erhält ein Geräte-Pairing per Einmal-Code: Nach der Serververbindung tauscht die App den Code über `POST api/pairing/exchange` gegen ein AES-256-GCM-verschlüsseltes Geräte-Token (ECDH nistP256), entschlüsselt es und legt es über `ISecretStore` im `SecureStorage` ab. `ClientGate.Resolve()` wird um die Geräte-Token-Quelle mit höchster Priorität erweitert, die `ServerSetupPage` um ein Code-Eingabefeld, und ein Re-Pairing-Flow führt bei widerrufenem Geräte-Token (Gate-401) zurück zur Pairing-Eingabe — sowohl zur Laufzeit (stille Neuanmeldung) als auch im Start-/Health-Check-Pfad. Ein Server ohne Pairing-Endpunkt (404) führt zu einer verständlichen Fehlermeldung (`PairingNotSupportedError`) — es gibt bewusst keinen Legacy-Fallback auf den statischen Token in dieser App-Version. Betroffen sind `VideoPlayer.ApiClient` (`ClientGate`, `VideoPlayerApiClient.CheckHealthAsync`, neue Pairing-Klassen, neues `HealthCheckResult`-Enum), `VideoPlayer.Storage` (`AppSecretsService`), `VideoPlayer.Mvu` (Modell, Nachrichten, Effekte, `AppUpdate`, `AuthSessionService`, neuer `DeviceTokenService`) und `VideoPlayer.Maui` (`ServerSetupPage`, `ShellAppEffectHandler`, `ServerHealthProbe`/`IServerHealthProbe`, `MauiProgram`) sowie `scripts/iOS-Deployment.ps1` (Durchreichen von `-p:VideoPlayerApiToken`).

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Geräte-Token-Quelle in `ClientGate` | Statischer Provider-Delegate `ClientGate.DeviceTokenProvider` (`Func<string?>?`), der einmalig beim Laden des Servers sowie nach Pairing/Widerruf vom `DeviceTokenService` gesetzt wird; `Resolve()` wertet ihn mit höchster Priorität aus. Neue interne Überladung `Resolve(string? deviceToken, string? environmentValue, string? metadataValue, string? fileValue)` für Tests. | `Resolve()` ist synchron und statisch; `ISecretStore` ist asynchron und MAUI-gebunden. Ein Provider-Delegate hält `VideoPlayer.ApiClient` plattformneutral und deckt alle bestehenden Aufrufpfade ab (`VideoPlayerApiClient`-Konstruktor, `ServerHealthProbe`, `AuthSessionService.CreateClient`), ohne Signaturen an den Aufrufstellen zu ändern. Asynchrones `ResolveAsync` würde alle Aufrufer umbauen. |
| Gate-401-Erkennung im Erreichbarkeitscheck | Neues Enum `HealthCheckResult` (`Reachable`, `GateRejected`, `Unreachable`) in `VideoPlayer.ApiClient` als Rückgabe von `VideoPlayerApiClient.CheckHealthAsync` und `IServerHealthProbe.CheckAsync`; `ApiException` mit Status 401 bildet auf `GateRejected` ab, jede andere auf `Unreachable`. Der `ShellAppEffectHandler` entscheidet über die Folge (siehe Ablauf „Re-Pairing im Start-/Health-Check-Pfad"). | Ein einziger Request unterscheidet „unerreichbar" von „Gate lehnt den `X-API-Key` ab" — die Alternative „bei `false` zusätzliche Gate-Probe (`IsGateRejectedAsync`) nachschieben" kostet bei jedem echten Offline-Fall einen zweiten Request. `ServerHealthStatus` (`VideoPlayer.Mvu`) ist nicht wiederverwendbar, weil `VideoPlayer.ApiClient` das MVU-Projekt nicht referenziert (Schichtung). Der MVU-Zyklus sieht weiterhin nur Terminalnachrichten (`ServerHealthCheckCompletedMessage` bleibt `bool IsReachable`, `GateRejected` wird am Rand in `DeviceTokenRevokedMessage` aufgelöst). |
| Pairing-Endpunkt-Aufruf | Eigene Klasse `DevicePairingClient` in `VideoPlayer.ApiClient` mit eigenem, injizierbarem `HttpClient` (kein `X-API-Key`, kein Bearer in `DefaultRequestHeaders`), Request mit absolutem URI gegen die Kandidatenadresse. | Der Gate-Header sitzt in `DefaultRequestHeaders` des gemeinsamen `HttpClient` und kann pro Request nicht unterdrückt werden; `VideoPlayerApiClient` würde den Header also immer mitsenden. Ein eigener Client erfüllt den Vertrag („öffentlich, kein `X-API-Key`") und ist über `StubHttpMessageHandler` testbar. Umgeht bewusst `SendWithPolicyAsync`: 429 muss sofort sichtbar werden (IP-Sperre, nicht wiederholbar aus Anwendersicht) und 401 darf keinen `TokenRefresher` auslösen. |
| Krypto | Plattformneutrale statische Komponente `PairingCrypto` in `VideoPlayer.ApiClient` (`System.Security.Cryptography`): flüchtiges ECDH-nistP256-Schlüsselpaar, SPKI/DER-Export als Base64, Schlüsselableitung per `DeriveKeyFromHmac` (SHA256), AES-256-GCM-Entschlüsselung (`nonce(12) → ciphertext → tag(16)`). | Anforderung schreibt Ableitung und Layout verbindlich vor; BCL deckt alles ab (kein NuGet nötig). Liegt im plattformneutralen Projekt, damit das Testprojekt (ohne MAUI) den Roundtrip prüfen kann. |
| Pairing-DTOs | Eigene Modelle `PairingExchangeRequest`/`PairingExchangeResponse` in `VideoPlayer.ApiClient.Models` mit `JsonPropertyName`-Attributen (camelCase). | **Geklärte Entscheidung (Anwender):** eigene DTOs, keine Repo-übergreifende Kopplung an `VideoWebPlayer.Client` des Server-Repos. Bestehende Request-Payloads werden PascalCase serialisiert (`LoginRequest`); der Vertrag verlangt camelCase. Der Vertrag ist klein und stabil; bei späterer gemeinsamer Quelle können die eigenen DTOs ersetzt werden. |
| Pairing-Ergebnis | Eigenes Enum `PairingStatus` in `VideoPlayer.Mvu` (`Succeeded`, `InvalidCode`, `RateLimited`, `InvalidRequest`, `NotSupported`, `ServerUnreachable`, `Failed`), analog `AuthAttemptStatus`. | Bildet die Vertrags-Fehlercodes 400/401/429 sowie 404 (Server ohne Pairing) und Transportfehler in der Update-Funktion ab; wird vom `enum-coverage-check`-Hook erfasst. |
| Erkennung „Pairing erforderlich" | Der Effekt-Handler ermittelt `PairingRequired = kein Geräte-Token für den Server UND keine statische Gate-Quelle konfiguriert` (`ClientGate.ResolveStatic()` == Platzhalter) und meldet es als neues Feld in `ServerCheckCompletedMessage`/`ServerHealthCheckCompletedMessage`/`ServersLoadedMessage` an `AppUpdate`. | **Geklärte Entscheidung (Anwender, nahtlose Migration):** `PairingRequired` berücksichtigt weiterhin die statische Quelle — Installationen mit nutzbarem statischem Token laufen ohne Pairing weiter. `AppUpdate` bleibt rein (kein I/O, kein `ClientGate`-Zugriff im Update); die Entscheidung braucht Ablage- und Auflösungswissen, das nur am App-Rand vorliegt. |
| Re-Pairing-Auslöser zur Laufzeit (Gate-401 vs. Bearer-401) | Neues Ereignis `DeviceTokenRejected` am `DeviceTokenService` (Muster: `AuthSessionService.SessionExpired`), ausgelöst wenn die stille Neuanmeldung mit 401 scheitert **und** eine Gate-Probe (Request nur mit `X-API-Key`, ohne Bearer, auf `api/health`) ebenfalls 401 liefert; sonst weiter `SessionExpired`. | **Geklärte Konvention (Anwender):** die Probe (`DeviceTokenService.IsGateRejectedAsync`) ist der einzig clientseitig umsetzbare Ansatz und setzt die Annahme, dass `api/health` hinter dem Gate liegt. Der Service-Rand-Mechanismus mit Event → `MvuHost`-Dispatch folgt dem `SessionExpired`-Muster. |
| Re-Pairing-Auslöser im Start-/Health-Check-Pfad | `GateRejected`-Ergebnis der Probe → `DeviceTokenService.DiscardCurrentAsync()` (Token des aktiven Servers verwerfen, Provider auf `ResolveStatic()` zurücksetzen) → **einmalige Re-Probe**: `Reachable` → normaler Pfad (statische Quelle funktioniert — nahtlose Migration), `Unreachable` → normaler Offline-Pfad, erneut `GateRejected` → `NotifyRejected()` → `DeviceTokenRevokedMessage` → bestehender `UpdateDeviceTokenRevoked`-Übergang. | Der Start-Pfad hat keinen `TokenRefresher` (die Probe erzeugt den Client ohne Options) — der Laufzeit-Mechanismus greift hier nicht. Das Verwerfen vor der Re-Probe verhindert die Sackgasse „Token bleibt liegen, `IsPairingRequired` bleibt `false`"; die Re-Probe klärt den Verdeckungsfall (widerrufenes Geräte-Token überdeckt einen gültigen statischen Schlüssel) mit einem Request deterministisch statt über die Auto-Login-Kette. Der Übergang läuft in die bereits geplante `DeviceTokenRevokedMessage` — kein zweiter Endpunkt-Zustand im Zustandsautomaten. |
| Kandidatenprüfung bei `GateRejected` | `CheckCandidateServerAsync` bildet `GateRejected` auf `Success = true` ab (der Server hat geantwortet und das Gate ist aktiv) plus `PairingRequired` für die Kandidatenadresse; davor `DiscardIfCurrentAsync(address)` — das gespeicherte Token wird nur verworfen, wenn der Provider gerade den Token dieses Servers sendet (dann war es der abgelehnte Schlüssel). | Ein 401 auf `api/health` beweist Erreichbarkeit hinter dem Gate — als „unerreichbar" zu melden wäre falsch und sperrte den Serverwechsel. Kein pauschales Verwerfen: Der gesendete Schlüssel kann der Token eines **anderen** (bisher aktiven) Servers sein — dessen Ablehnung durch den Kandidaten sagt nichts über seine Gültigkeit am eigenen Server aus. Ein veraltetes Token des Kandidaten in dem Fall, dass der Provider einen fremden Token sendete, fängt anschließend der Laufzeitpfad nach dem Provider-Wechsel ab. |
| Ablage-Schlüssel des Geräte-Tokens | Server-bezogener Schlüssel in `AppSecretsService`: `DeviceToken` + normalisierte Serveradresse, profil-präfixiert (`profile.PrefixKey`), nur `ISecretStore`. | **Geklärte Entscheidung (Anwender):** Das Token ist pro Server ausgestellt; server-bezogene Ablage erlaubt Wechsel zwischen bekannten Servern ohne Neu-Pairing. |
| Gerätename (`deviceName`) | `DeviceInfo.Name`, Fallback `$"{DeviceInfo.Manufacturer} {DeviceInfo.Model}"`, getrimmt auf 200 Zeichen; kein Override-Feld in der UI. | **Geklärte Entscheidung (Anwender):** Die Anforderung verlangt nur das Senden des Gerätenamens; ein zusätzliches Eingabefeld ist nicht nötig. |
| Server ohne Pairing-Endpunkt (404/`NotSupported`) | **Kein Fallback auf den statischen Token:** `AppUpdate` bildet `PairingStatus.NotSupported` immer auf `PairingNotSupportedError` („Server unterstützt kein Geräte-Pairing — Server aktualisieren") ab, setzt `IsPairingRunning = false` und bleibt in der Pairing-/Setup-Eingabe. | **Anwender-Entscheidung (Abweichung vom ursprünglich empfohlenen Fallback):** Es gibt keinen Legacy-Pfad in dieser App-Version. Der statische Token bleibt nur als Publish-/Legacy-Zwischenlösung für Alt-App-Versionen und als letzte `ClientGate.Resolve()`-Quelle bestehen. Konsistenz: Die Pairing-Eingabe ist nur bei `IsPairingRequired` erreichbar (kein Geräte-Token UND keine statische Quelle) — im `NotSupported`-Fall existiert also ohnehin kein nutzbarer statischer Token, der Verzicht auf den Fallback ist verlustfrei. Konsequenz: `StaticGateTokenAvailable` wird nicht als `AppModel`-Feld benötigt; die statische Quelle fließt ausschließlich in die `PairingRequired`-Ermittlung am Handler-Rand ein. |
| Pairing-Eingabe-UI | Neuer Abschnitt im Fußbereich der `ServerSetupPage` (`PairingCodeEntry`, `PairButton`, `PairingIndicator`, `PairingErrorLabel` mit AutomationIds), sichtbar wenn `AppModel.IsPairingRequired`; Fehler in eigenem Modelfeld `PairingError`. | Wiederverwendung des vorhandenen Eingabe-/Fehler-Musters (`AddressEntry`/`ConnectButton`/`CheckIndicator`/`ErrorLabel`); eigene Felder, weil Pairing-Fehler (ungültiger Code, IP-Sperre) semantisch von Adress-/Erreichbarkeitsfehlern (`ServerSetupError`) zu trennen sind. |
| Code-Vorvalidierung | Statischer Helper `PairingCode` in `VideoPlayer.Mvu` mit `TryNormalize` (Trim, Großschreibung, Länge 8, Alphabet `ABCDEFGHJKMNPQRSTUVWXYZ23456789`), analog `ServerAddress.TryNormalize` in `AppUpdate`. | Spart Server-Requests bei offensichtlichen Eingabefehlern und folgt dem bestehenden Normalisierungs-Muster. Hinweis (Bestandsaufnahme): Die Länge 8 ist der Vertrags-Default; bei abweichender Server-Konfiguration (`Pairing:CodeLength` min. 4) würden kürzere Codes clientseitig verworfen — akzeptabel, bei Bedarf in der Nachplanung. |
| Geräte-Token-Aktualisierung nach Pairing | `DeviceTokenService` setzt `ClientGate.DeviceTokenProvider` neu; zusätzlich `VideoPlayerApiClient.SetApiKey(string)` zum Aktualisieren des Headers an bereits erzeugten Clients (`AuthSessionService.Client`). | Neue Clients erhalten das Token automatisch über `Resolve()`; für einen bereits existierenden Client ist der Header nur einmalig im Konstruktor gesetzt — `SetApiKey` schließt diese Lücke (Anforderung). |
| Reset-Semantik der Pairing-Felder | `BeginServerCheck` (neue Adresseingabe / Kandidatenwahl / Serverwechsel) setzt `PairingError`, `IsPairingRequired` und `IsPairingRunning` zurück; `UpdateServerCheckCompleted` setzt `IsPairingRequired` aus der Nachricht bzw. bei Misserfolg auf `false` und leert `PairingError`; `UpdatePairingCompleted` ignoriert verspätete Ergebnisse (`!IsPairingRunning` → `Unchanged`). | Eine neue Serverprüfung ersetzt den Pairing-Kontext — ein alter `PairingError` oder ein hängendes `IsPairingRequired` dürften sonst über den nächsten Check hinweg sichtbar bleiben. Der Stale-Result-Guard verhindert, dass ein verspätetes `PairingCompletedMessage` den Zustand eines inzwischen gewechselten Servers überschreibt (die Token-Ablage im Effekt-Handler ist davon unberührt — sie adressiert den im Effekt mitgegebenen Server). |

## Programmabläufe

### Erst-Pairing nach manueller Serverwahl („Server verbinden → Code eingeben → gekoppelt")

1. `ServerSetupPage` dispatcht `ServerAddressSubmittedMessage`; `AppUpdate` normalisiert die Adresse (`ServerAddress.TryNormalize`), startet `CheckCandidateServerEffect` (`IsServerCheckRunning = true`, `PairingError`/`IsPairingRequired`/`IsPairingRunning` zurückgesetzt).
2. `ShellAppEffectHandler.CheckCandidateServerAsync` prüft die Adresse über `IServerHealthProbe.CheckAsync(Uri)` → `HealthCheckResult`: `Reachable` → `Success = true`; `GateRejected` → `DiscardIfCurrentAsync(address)` + `Success = true` (Server erreichbar, Gate aktiv); `Unreachable` → `Success = false`. Danach `PairingRequired` für die Kandidatenadresse ermitteln (`AppSecretsService.GetDeviceTokenAsync(address)` leer **und** `ClientGate.ResolveStatic()` == Platzhalter) und `ServerCheckCompletedMessage(Address, DisplayName, Success, PairingRequired)` dispatchen.
3. `AppUpdate.UpdateServerCheckCompleted`: Erfolg + `PairingRequired` → Server vorne in `KnownServers`, `ActiveServerAddress` setzen, `IsPairingRequired = true`, `ConnectionState` bleibt `NeedsServerSetup`, Seite bleibt `ServerSetup` (Pairing-Eingabe wird sichtbar); Effekt `PersistActiveServerEffect` bereits jetzt (Server ist geprüft), **kein** `AttemptAutoLoginEffect`. Erfolg ohne `PairingRequired` → bisheriger Pfad (`PersistActiveServerEffect` + `AttemptAutoLoginEffect`). Misserfolg → `UnreachableServerError` wie bisher.
4. Anwender gibt den Code ein; `ServerSetupPage` dispatcht `PairingCodeSubmittedMessage(Code)`.
5. `AppUpdate`: `PairingCode.TryNormalize` — ungültig → `PairingError = InvalidPairingCodeError` (kein Effekt); gültig → `IsPairingRunning = true`, `PairingError = null`, Effekt `ExchangePairingCodeEffect(ActiveServerAddress, Code)`.
6. `ShellAppEffectHandler` führt den Effekt aus: `DevicePairingClient.ExchangeAsync(serverUri, code, deviceName)` (Gerätename aus `DeviceInfo`: `DeviceInfo.Name`, Fallback `$"{DeviceInfo.Manufacturer} {DeviceInfo.Model}"`, getrimmt auf 200 Zeichen); intern: `PairingCrypto` erzeugt flüchtiges ECDH-Paar, Request `{code, clientPublicKey, deviceName}` camelCase → `POST api/pairing/exchange` ohne Gate-/Bearer-Header, Response `{serverPublicKey, encryptedToken}` → Ableitung (`DeriveKeyFromHmac`, SHA256) → `AesGcm`-Entschlüsselung → Geräte-Token (string).
7. Bei Erfolg: `AppSecretsService.SetDeviceTokenAsync(address, token)` (nur `ISecretStore`), `DeviceTokenService` aktualisiert `ClientGate.DeviceTokenProvider` (Server-Kontext wird der aktive), ggf. `VideoPlayerApiClient.SetApiKey` am bestehenden `AuthSessionService.Client`; Dispatch `PairingCompletedMessage(PairingStatus.Succeeded)`.
8. `AppUpdate`: `Succeeded` → `IsPairingRunning = false`, `IsPairingRequired = false`, `ConnectionState = Connecting`, Effekt `AttemptAutoLoginEffect` → bisheriger Pfad `EnterConnected`.
9. Fehler-Mapping im Handler → `PairingCompletedMessage`: `ApiException` 401 → `InvalidCode`, 429 → `RateLimited`, 400 → `InvalidRequest`, 404 → `NotSupported`, `StatusCode == null` (Transport/Timeout) → `ServerUnreachable`, sonst → `Failed`. `AppUpdate` setzt je Status `PairingError` (Konstanten analog `InvalidAddressError`) und bleibt auf der Pairing-Eingabe — **inklusive `NotSupported`**: `PairingNotSupportedError` („Server unterstützt kein Geräte-Pairing — Server aktualisieren"), kein `AttemptAutoLoginEffect`, kein Fallback auf den statischen Token (Designentscheidung, siehe oben).

Beteiligte Klassen/Komponenten: `ServerSetupPage`, `MvuHost`, `AppUpdate`, `AppModel`, `PairingCode`, `ExchangePairingCodeEffect`, `ShellAppEffectHandler`, `DevicePairingClient`, `PairingCrypto`, `AppSecretsService`, `DeviceTokenService`, `ClientGate`, `AuthSessionService`.

### Start mit hinterlegtem Server

1. `LoadServersEffect` → `ShellAppEffectHandler.LoadServersAsync` lädt Server + aktive Adresse, lädt zusätzlich das Geräte-Token des aktiven Servers (`AppSecretsService.GetDeviceTokenAsync`), setzt `ClientGate.DeviceTokenProvider` über `DeviceTokenService` (Server-Kontext = aktive Adresse) und ermittelt `PairingRequired`; Ergebnis als neues Feld in `ServersLoadedMessage` (Erweiterung der Nachricht).
2. `AppUpdate.UpdateServersLoaded`: `IsPairingRequired = message.PairingRequired` ins Modell übernehmen; mit aktiver Adresse beim Start → `CheckServerHealthEffect` (wie bisher); ohne Server → `NeedsServerSetup` + `ServerSetup`-Seite (wie bisher).
3. `CheckServerHealthEffect` → `RunHealthCheckAsync` → `healthProbe.CheckAsync()` → `HealthCheckResult`:
   - `Reachable` → `ServerHealthCheckCompletedMessage(IsReachable: true, PairingRequired)`.
   - `Unreachable` → `ServerHealthCheckCompletedMessage(false, PairingRequired: false)`.
   - `GateRejected` → `DeviceTokenService.DiscardCurrentAsync()` → einmalige Re-Probe `healthProbe.CheckAsync()`: `Reachable` → `ServerHealthCheckCompletedMessage(true, false)`; `Unreachable` → `ServerHealthCheckCompletedMessage(false, false)`; erneut `GateRejected` → `DeviceTokenService.NotifyRejected()` → `DeviceTokenRejected`-Ereignis → `DeviceTokenRevokedMessage` (siehe Ablauf „Re-Pairing bei widerrufenem Geräte-Token").
4. `AppUpdate.UpdateHealthCheckCompleted`: erreichbar + `PairingRequired` → `NeedsServerSetup` + `ServerSetup`-Seite mit aktiver Pairing-Eingabe (`IsPairingRequired = true`, `IsStartupRunning = false`, `ServerHealth = Reachable`, Muster `GoToServerSetup`); erreichbar ohne `PairingRequired` → `AttemptAutoLoginEffect` (wie bisher); unerreichbar → `CheckOfflineDataEffect` (wie bisher).
5. Derselbe Pfad läuft bei der Online-Rückkehr aus dem Offline-Zustand (`UpdateNetworkAvailabilityChanged` → `CheckServerHealthEffect`) — ein unterwegs widerrufenes Geräte-Token führt damit auch hier deterministisch in den Re-Pairing-Flow statt erneut in den Offline-Zustand.

Beteiligte Klassen/Komponenten: `AppUpdate`, `ShellAppEffectHandler`, `KnownServersService`, `AppSecretsService`, `DeviceTokenService`, `ClientGate`, `IServerHealthProbe`/`ServerHealthProbe`, `VideoPlayerApiClient.CheckHealthAsync`.

### Re-Pairing bei widerrufenem/abgelaufenem Geräte-Token (Gate-401)

Zwei Einstiegspfade münden in denselben Endzustand (`DeviceTokenRevokedMessage` → `UpdateDeviceTokenRevoked`):

**Laufzeit-Pfad (Fach-Request während aktiver Sitzung):**

1. Ein Fach-Request erhält 401; `SendWithPolicyAsync` ruft einmalig `TokenRefresher` → `AuthSessionService.SilentReloginAsync`.
2. Die stille Neuanmeldung scheitert mit 401/403 → `AuthSessionService` fragt `DeviceTokenService.IsGateRejectedAsync(serverAddress)`: Probe-Request mit `X-API-Key` (aufgelöstes Geräte-Token), **ohne** Bearer, auf `api/health`.
3. Probe = 401 (Gate lehnt das Geräte-Token ab) → `DeviceTokenService.DiscardCurrentAsync()` (`AppSecretsService.ClearDeviceTokenAsync`, `DeviceTokenProvider` → `ResolveStatic()`-Ergebnis) + `NotifyRejected()` → `DeviceTokenRejected`-Ereignis. Probe ≠ 401 → bisheriger `SessionExpired`-Pfad (`NeedsLogin`).

**Start-/Health-Check-Pfad (Token bereits bei App-Start oder Reconnect widerrufen):**

1. `RunHealthCheckAsync` erhält `HealthCheckResult.GateRejected` (der `X-API-Key` — aufgelöst inkl. gespeichertem Geräte-Token — wurde von `api/health` abgelehnt).
2. `DeviceTokenService.DiscardCurrentAsync()` verwirft das Token des aktiven Servers (der Provider liefert danach die statische Auflösung); einmalige Re-Probe.
3. Re-Probe `Reachable` → statische Quelle trägt → `ServerHealthCheckCompletedMessage(true, false)`, normaler Anmeldepfad (nahtlose Migration). `Unreachable` → normaler Offline-Pfad. Erneut `GateRejected` → `NotifyRejected()` → `DeviceTokenRejected`-Ereignis.

**Gemeinsamer Ausgang:**

4. `ShellAppEffectHandler` (abonniert `DeviceTokenRejected`, Muster `OnSessionExpired`) dispatcht `DeviceTokenRevokedMessage` über `DispatchMessageAsync`/MainThread.
5. `AppUpdate.UpdateDeviceTokenRevoked`: `IsStartupRunning = false`, `IsServerCheckRunning = false`, `IsPairingRunning = false`, `IsPairingRequired = true`, `ServerHealth = Reachable` (das Gate hat geantwortet — der Server ist erreichbar), `ServerSetupError = null`, `PairingError = DeviceRevokedError`, `ConnectionState = NeedsServerSetup`, `CurrentPage = ServerSetup` + `NavigateToPageEffect(ServerSetup)` falls nicht bereits dort, `DisconnectServerEventsEffect` und Rücksetzen von `SourcesOverview`/`SourceBrowser`/`HomeHero` (Muster `UpdateSessionExpired` — sitzungsbezogene Inhalte bleiben nicht sichtbar). Guard: bereits in `NeedsServerSetup` mit `IsPairingRequired` → `Unchanged` (Doppel-Auslösung idempotent).

Beteiligte Klassen/Komponenten: `VideoPlayerApiClient`, `AuthSessionService`, `DeviceTokenService`, `AppSecretsService`, `ClientGate`, `IServerHealthProbe`, `ShellAppEffectHandler`, `AppUpdate`.

### Geräte-Token-Auflösung im Gate (Priorität)

1. `ClientGate.Resolve()` liest `DeviceTokenProvider?.Invoke()` → Env `VIDEOWEBPLAYER_MAUI_API_TOKEN` → Assembly-Metadata `VideoPlayer.ApiClient.ApiToken` → Token-Datei → Platzhalter; erste konfigurierte Quelle gewinnt (Platzhalter-Werte werden übersprungen).
2. `DeviceTokenService` hält den Provider aktuell: beim Laden eines Servers (`LoadServersAsync`/`PersistActiveServerAsync`), nach erfolgreichem Pairing, nach Widerruf (`DiscardCurrentAsync` → `ResolveStatic()`-Fallback). Der Service merkt sich die aktuell bediente Serveradresse (`CurrentServerAddress`), damit `DiscardIfCurrentAsync` unterscheiden kann, ob ein abgelehnter Schlüssel der gespeicherte Token dieser Adresse war.

Beteiligte Klassen/Komponenten: `ClientGate`, `DeviceTokenService`, `VideoPlayerApiClient`, `ServerHealthProbe`, `AuthSessionService`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `PairingExchangeRequest` | Datenmodellklasse (`VideoPlayer.ApiClient.Models`) | Request-DTO `code`, `clientPublicKey`, `deviceName` mit `JsonPropertyName`-camelCase-Attributen. |
| `PairingExchangeResponse` | Datenmodellklasse (`VideoPlayer.ApiClient.Models`) | Response-DTO `serverPublicKey`, `encryptedToken` (camelCase). |
| `PairingCrypto` | statische Klasse (`VideoPlayer.ApiClient`, intern) | Flüchtiges ECDH-nistP256-Schlüsselpaar, SPKI/DER-Base64-Export, `DeriveKeyFromHmac`-Ableitung, AES-256-GCM-Entschlüsselung (`nonce(12) → ct → tag(16)`). Testbar über `InternalsVisibleTo`. |
| `DevicePairingClient` | Klasse / Gateway (`VideoPlayer.ApiClient`) | `POST api/pairing/exchange` über eigenen `HttpClient` ohne Gate-/Bearer-Header, einmalig ohne Retry/Reauth; liefert entschlüsseltes Geräte-Token, wirft `ApiException` bei Fehlerstatus. |
| `HealthCheckResult` | Enum (`VideoPlayer.ApiClient`) | Ergebnis der Erreichbarkeitsprüfung: `Reachable` (Erfolgsstatus), `GateRejected` (HTTP 401 — Gate hat den `X-API-Key` abgelehnt), `Unreachable` (Transportfehler/sonstiger Status). |
| `PairingStatus` | Enum (`VideoPlayer.Mvu`) | Ergebnis des Pairing-Versuchs: `Succeeded`, `InvalidCode`, `RateLimited`, `InvalidRequest`, `NotSupported`, `ServerUnreachable`, `Failed`. |
| `PairingCode` | statische Klasse (`VideoPlayer.Mvu`) | `TryNormalize`: Trim/Großschreibung/Format-Prüfung des Einmal-Codes (Muster `ServerAddress.TryNormalize`). |
| `DeviceTokenService` | Klasse / Service Layer (`VideoPlayer.Mvu`) | Verwaltet das aktive Geräte-Token: `LoadForServerAsync`/`SetPairedTokenAsync`/`DiscardCurrentAsync`/`DiscardIfCurrentAsync` über `AppSecretsService`, Pflege von `ClientGate.DeviceTokenProvider` inkl. `CurrentServerAddress`-Tracking, Gate-Probe `IsGateRejectedAsync` (eigener `HttpClient` über injizierbare Fabrik — Testnaht wie `AuthSessionService`), `NotifyRejected()` → Ereignis `DeviceTokenRejected`. |

Neue MVU-Nachrichten (`AppMessage.cs`): `PairingCodeSubmittedMessage(string Code)`, `PairingCompletedMessage(PairingStatus Status)`, `DeviceTokenRevokedMessage`.

Neuer MVU-Effekt (`AppEffect.cs`): `ExchangePairingCodeEffect(string ServerAddress, string Code)`.

## Änderungen an bestehenden Klassen

### `ClientGate` (statische Klasse, `VideoPlayer.ApiClient`)

- **Neue Eigenschaften:** `DeviceTokenProvider` (`Func<string?>?`, statisch, setzbar) — liefert das aktuell gültige Geräte-Token; `null` = keine Geräte-Token-Quelle.
- **Geänderte Methoden:** `Resolve()` — wertet `DeviceTokenProvider` vor der Umgebungsvariable aus (Priorität Geräte-Token > Env > Metadata > Datei > Platzhalter); Diagnosezeile um Geräte-Token-Quelle ergänzen.
- **Neue Methoden:** `ResolveStatic()` (`public static string`) — Auflösung nur über die statischen Quellen (Env → Metadata → Datei → Platzhalter), dient der `PairingRequired`-Ermittlung und dem Provider-Fallback nach Widerruf; interne Überladung `Resolve(string? deviceToken, string? environmentValue, string? metadataValue, string? fileValue)` für Tests.

### `VideoPlayerApiClient` (`VideoPlayer.ApiClient`)

- **Neue Methoden:** `SetApiKey(string apiKey)` — ersetzt den `X-API-Key`-Header in `DefaultRequestHeaders` (nach erfolgreichem (Re-)Pairing an bestehenden Clients).
- **Geänderte Methoden:** `CheckHealthAsync` — Rückgabetyp `Task<bool>` → `Task<HealthCheckResult>`: Erfolgsstatus → `Reachable`; `ApiException` mit `StatusCode == Unauthorized` → `GateRejected`; andere `ApiException` → `Unreachable`. Einziger produktiver Aufrufer ist `ServerHealthProbe` (die Probe erzeugt den Client ohne `TokenRefresher`, der 401 trifft daher direkt auf die Ausnahme).

### `AppSecretsService` (`VideoPlayer.Storage`)

- **Neue Konstanten:** `DeviceTokenKey` (`"DeviceToken"`) — Basis-Schlüssel der sicheren Ablage; effektiver Schlüssel = `profile.PrefixKey(DeviceTokenKey + ":" + normalisierte Serveradresse)` (Server-Bezug, geklärte Entscheidung).
- **Neue Methoden:** `GetDeviceTokenAsync(string serverAddress)` → `Task<string?>`; `SetDeviceTokenAsync(string serverAddress, string token)`; `ClearDeviceTokenAsync(string serverAddress)`; `HasDeviceTokenAsync(string serverAddress)` → `Task<bool>` (Komfort-Wrapper). Keine Legacy-Migration nötig (nur `ISecretStore`, nie Preferences).

### `AppModel` (`VideoPlayer.Mvu`)

- **Neue Eigenschaften:** `IsPairingRunning` (`bool`) — Pairing-Austausch läuft (treibt `PairingIndicator`/`PairButton`); `IsPairingRequired` (`bool`) — Server verbunden/ausgewählt, aber kein nutzbarer Gate-Schlüssel → Pairing-Eingabe sichtbar; `PairingError` (`string?`) — Inline-Fehlertext der Pairing-Eingabe.

### `AppMessage` (`VideoPlayer.Mvu`)

- **Neue Nachrichten:** `PairingCodeSubmittedMessage(string Code)`; `PairingCompletedMessage(PairingStatus Status)`; `DeviceTokenRevokedMessage`.
- **Geänderte Nachrichten:** `ServerCheckCompletedMessage` — neues Feld `PairingRequired` (`bool`); `ServerHealthCheckCompletedMessage` — neues Feld `PairingRequired` (`bool`), `IsReachable` bleibt `bool` (der `GateRejected`-Fall wird am Rand in `DeviceTokenRevokedMessage` bzw. ein gültiges Erreichbarkeits-Ergebnis aufgelöst); `ServersLoadedMessage` — neues Feld `PairingRequired` (`bool`).

### `AppEffect` (`VideoPlayer.Mvu`)

- **Neue Effekte:** `ExchangePairingCodeEffect(string ServerAddress, string Code)` — Pairing-Austausch am App-Rand (Ergebnis: `PairingCompletedMessage`).

### `AppUpdate` (`VideoPlayer.Mvu`)

- **Geänderte Methoden:**
  - `BeginServerCheck` — zusätzlich zu `IsServerCheckRunning = true`/`ServerSetupError = null`: `PairingError = null`, `IsPairingRequired = false`, `IsPairingRunning = false` (eine neue Serverprüfung ersetzt den Pairing-Kontext; kein alter Fehlertext über den nächsten Check hinweg).
  - `UpdateServerCheckCompleted` — Erfolg: `IsPairingRequired = message.PairingRequired`, `PairingError = null`, `IsPairingRunning = false`; bei `PairingRequired` Pairing-Eingabe statt `AttemptAutoLoginEffect` (Server wird persistiert, `PersistActiveServerEffect` bleibt). Misserfolg: zusätzlich `IsPairingRequired = false`, `PairingError = null`, `IsPairingRunning = false`.
  - `UpdateHealthCheckCompleted` — erreichbar: `IsPairingRequired = message.PairingRequired`, `PairingError = null`; bei `PairingRequired` → `NeedsServerSetup`/`ServerSetup` (`IsStartupRunning = false`, `ServerHealth = Reachable`, Muster `GoToServerSetup`); sonst `AttemptAutoLoginEffect` wie bisher. Unerreichbar: zusätzlich `IsPairingRequired = false`, `PairingError = null`, `IsPairingRunning = false`.
  - `UpdateServersLoaded` — `IsPairingRequired = message.PairingRequired` ins Modell übernehmen; `PairingError`/`IsPairingRunning` bleiben unverändert (ein bloßes Nachladen der Serververwaltung löscht keinen angezeigten Fehler — z. B. `DeviceRevokedError`).
- **Neue Methoden:** `UpdatePairingCodeSubmitted` — `PairingCode.TryNormalize`, Fehler → `PairingError`, sonst `IsPairingRunning` + `ExchangePairingCodeEffect`; `UpdatePairingCompleted` — Guard `!model.IsPairingRunning` → `Unchanged` (verspätetes Ergebnis nach Serverwechsel/neuem Check), sonst Status-Mapping (`Succeeded` → `AttemptAutoLoginEffect`/`EnterConnected`-Pfad; `InvalidCode`/`RateLimited`/`InvalidRequest`/`ServerUnreachable`/`Failed` → `PairingError`-Konstanten; `NotSupported` → immer `PairingNotSupportedError`, `IsPairingRunning = false`, **kein** `AttemptAutoLoginEffect`-Fallback auf den statischen Token); `UpdateDeviceTokenRevoked` — `NeedsServerSetup` + `ServerSetup`-Navigation + `DeviceRevokedError` als `PairingError` + `IsPairingRequired = true` + `IsStartupRunning = false`/`IsServerCheckRunning = false`/`IsPairingRunning = false` + `ServerHealth = Reachable` + `DisconnectServerEventsEffect` + Reset von `SourcesOverview`/`SourceBrowser`/`HomeHero` (Muster `UpdateSessionExpired`); Guard bei bereits aktivem Pairing-Zustand → `Unchanged`.
- **Neue Konstanten:** `InvalidPairingCodeError`, `PairingRateLimitedError`, `PairingNotSupportedError`, `PairingFailedError`, `DeviceRevokedError` (Muster bestehender Fehlerkonstanten; `DeviceRevokedError` formuliert allgemein — „Geräte-Zugang vom Server abgelehnt" deckt auch den Fall ab, dass kein Geräte-Token, sondern ein ungültiger statischer Schlüssel abgelehnt wurde).

### `AuthSessionService` (`VideoPlayer.Mvu`)

- **Geänderte Methoden:** `SilentReloginAsync` — bei endgültigem 401/403 zuerst `DeviceTokenService.IsGateRejectedAsync(serverAddress)` prüfen: Gate abgelehnt → `DiscardCurrentAsync()` + `NotifyRejected()` (kein `SessionExpired`); sonst bisheriger `SessionExpired`-Pfad.
- **Neue Abhängigkeit:** `DeviceTokenService` (Konstruktor-Injection).

### `IServerHealthProbe` / `ServerHealthProbe` (`VideoPlayer.Maui`)

- **Geänderte Methoden:** `CheckAsync(ct)` und `CheckAsync(Uri, ct)` — Rückgabetyp `Task<bool>` → `Task<HealthCheckResult>`; `ServerHealthProbe` reicht das Ergebnis von `CheckHealthAsync` durch, der Catch-All-Filter bildet weiterhin auf `Unreachable` ab. Einzige Aufrufer sind `RunHealthCheckAsync` und `CheckCandidateServerAsync` im `ShellAppEffectHandler` (verifiziert — keine weiteren Konsumenten).

### `ShellAppEffectHandler` (`VideoPlayer.Maui`)

- **Neue Event-Handler:** `DeviceTokenService.DeviceTokenRejected` → `DispatchMessageAsync(new DeviceTokenRevokedMessage())` (Muster `OnSessionExpired`).
- **Neue Effekt-Zweige:** `ExchangePairingCodeEffect` → `ExchangePairingCodeAsync`: Gerätename via `DeviceInfo` (`DeviceInfo.Name`, Fallback `$"{DeviceInfo.Manufacturer} {DeviceInfo.Model}"`, Trim 200), `DevicePairingClient.ExchangeAsync`, Erfolg → `AppSecretsService.SetDeviceTokenAsync` + `DeviceTokenService` (Provider/`SetApiKey`), Dispatch `PairingCompletedMessage` mit Status-Mapping aus `ApiException` (401 → `InvalidCode`, 429 → `RateLimited`, 400 → `InvalidRequest`, 404 → `NotSupported`, `StatusCode == null` → `ServerUnreachable`, sonst `Failed`).
- **Geänderte Methoden:**
  - `RunHealthCheckAsync` — `HealthCheckResult`-Verzweigung: `Reachable` → `ServerHealthCheckCompletedMessage(true, PairingRequired)`; `Unreachable` → `(false, false)`; `GateRejected` → `DiscardCurrentAsync()` + einmalige Re-Probe → `Reachable`/`Unreachable` wie zuvor, erneut `GateRejected` → `NotifyRejected()` (→ `DeviceTokenRevokedMessage`).
  - `CheckCandidateServerAsync` — `HealthCheckResult`-Verzweigung: `Reachable`/`GateRejected` → `Success = true` (bei `GateRejected` vorher `DiscardIfCurrentAsync(address)`), `Unreachable` → `false`; danach `PairingRequired` für die Kandidatenadresse ermitteln und melden.
  - `LoadServersAsync` — Geräte-Token des aktiven Servers laden, `ClientGate.DeviceTokenProvider` setzen (`DeviceTokenService`, Server-Kontext merken), `PairingRequired` in `ServersLoadedMessage` melden.
  - `PersistActiveServerAsync` — Provider auf das Token des neuen Servers umstellen (Serverwechsel) **vor** dem nachfolgenden `AttemptAutoLoginEffect`.
- **Neue Abhängigkeiten:** `DevicePairingClient`, `DeviceTokenService`, `AppSecretsService` (Konstruktor-Injection).

### `ServerSetupPage` (`VideoPlayer.Maui`, XAML + Code-Behind)

- **Neue UI-Elemente (XAML):** Abschnitt „Gerät koppeln" im Fußbereich: `PairingCodeEntry` (AutomationId, `Keyboard.Text`, MaxLength, Großbuchstaben), `PairButton` (AutomationId, `Clicked="OnPairClicked"`), `PairingIndicator` (ActivityIndicator an `IsPairingRunning`), `PairingErrorLabel` (AutomationId an `PairingError`, `VpDanger`). Sichtbarkeit des Abschnitts an `IsPairingRequired` binden, damit bestehende Flows unverändert bleiben (Designentscheidung, siehe oben).
- **Geänderte Methoden (Code-Behind):** `UpdateUi(AppModel)` — neue Felder binden (Sichtbarkeit, Indicator, Button-Enabled, `PairingError`); **Neue Methoden:** `OnPairClicked` → `mvuHost.DispatchAsync(new PairingCodeSubmittedMessage(PairingCodeEntry.Text ?? string.Empty))`.

### `MauiProgram` (`VideoPlayer.Maui`)

- **Neue DI-Registrierungen:** `DevicePairingClient` (Singleton mit eigenem `HttpClient` ohne Gate-Header), `DeviceTokenService` (Singleton); `AuthSessionService`/`ShellAppEffectHandler` erhalten die neuen Abhängigkeiten über den Container.

### `scripts/iOS-Deployment.ps1` (Build/Deployment)

- **Neuer Parameter:** `[string]$VideoPlayerApiToken` (Default `$env:VIDEOWEBPLAYER_MAUI_API_TOKEN`).
- **Geänderte Funktionen:** `Add-PropertyLine -Name "VideoPlayerApiToken"` in allen Build-/Publish-Response-Dateien ergänzen (`Invoke-Build` und die Publish-Pfade für `device`/`store`), in den Display-Zeilen über `Add-MaskedDisplay` maskieren.

## Datenbankmigrationen

Keine. Das Geräte-Token liegt ausschließlich in `ISecretStore` (`SecureStorage`); `known_servers`/`backup_state` bleiben unverändert.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Pairing-Code (`PairingCodeEntry` → `PairingCode.TryNormalize`) | Nach Trim/Großschreibung: Länge 8, Alphabet `ABCDEFGHJKMNPQRSTUVWXYZ23456789` | `PairingError = InvalidPairingCodeError`, kein Server-Request |
| `deviceName` (am App-Rand) | Getrimmt, max. 200 Zeichen; Leerwert → Feld weglassen (Server-Fallback greift) | Server-400 (`InvalidRequest`) als Rückfall behandeln |
| `PairingExchangeResponse` | `serverPublicKey` muss importierbarer P-256-SPKI/DER-Base64-Key sein; `encryptedToken` muss mindestens 28 Bytes (12 Nonce + 16 Tag) Base64-dekodierbar sein | `CryptographicException`/Formatfehler → `PairingStatus.Failed` → `PairingFailedError` |
| Effekt-Parameter | `ExchangePairingCodeEffect` nur bei gesetzter `ActiveServerAddress` | Guard in `AppUpdate`: ohne Adresse keine Effekt-Emission |

## Konfigurationsänderungen

Keine neuen Anwendungseinstellungen. Dokumentierte, unveränderte Auflösungswege bleiben: `VIDEOWEBPLAYER_MAUI_API_TOKEN` (Env), `-p:VideoPlayerApiToken` → Assembly-Metadata `VideoPlayer.ApiClient.ApiToken` (neu über `iOS-Deployment.ps1 -VideoPlayerApiToken` durchgereicht), Token-Datei `client-api-token.txt`, Platzhalter `<MAUI_CLIENT_API_TOKEN>`.

## Seiteneffekte und Risiken

- **`ClientGate.Resolve()`-Aufrufer:** `VideoPlayerApiClient`-Konstruktor, `ServerHealthProbe`, `AuthSessionService.CreateClient`/`SilentReloginAsync` erhalten künftig das Geräte-Token — gewünscht, aber die Reihenfolge des Provider-Setzens ist kritisch: Der Provider muss vor dem ersten Request nach Serverwahl gesetzt sein (in `LoadServersAsync`/`PersistActiveServerAsync` vor `AttemptAutoLoginEffect`/`CheckServerHealthEffect`), sonst greift kurzzeitig der statische Fallback.
- **`CheckHealthAsync`-/`IServerHealthProbe`-Signaturänderung:** Rückgabetyp `bool` → `HealthCheckResult`; einziger produktiver Aufrufer ist `ServerHealthProbe`, einzige Probe-Aufrufer `RunHealthCheckAsync`/`CheckCandidateServerAsync`. Betroffen sind die Client-Tests (`CheckHealthAsync_*`) und die Hilfsdokumentation `docs/help/api-client/api.md`/`installation.md`/`troubleshooting.md` (Bool-Formulierungen nachziehen).
- **Kandidatenprüfung bei `GateRejected`:** wird als „erreichbar" (`Success = true`) behandelt — ein 401 auf `api/health` beweist einen antwortenden Server hinter dem Gate. Konvention wie bei `IsGateRejectedAsync`: Antwortet ein fremdes Gateway (Reverse-Proxy-Auth o. ä.) mit 401, wird es ebenfalls als erreichbarer Server gewertet — akzeptabel, da der nachfolgende Pairing-/Login-Pfad die Unterscheidung trägt.
- **Re-Probe nach `GateRejected`:** ein zusätzlicher `api/health`-Request nur auf dem Ablehnungs-Pfad (kein Mehraufwand im Normalfall). Verhindert die Sackgasse „widerrufener Token verdeckt funktionierende statische Quelle" und liefert `PairingRequired` deterministisch.
- **`GateRejected` ohne gespeichertes Geräte-Token:** auch ein falscher statischer Schlüssel/Platzhalter landet nach der Re-Probe im `DeviceTokenRevoked`-Übergang mit `DeviceRevokedError` — die Pairing-Eingabe ist in beiden Fällen die richtige Wiederherstellung; der Fehlertext wird allgemein formuliert.
- **Retry-Pipeline:** `SendWithPolicyAsync` wiederholt 429 bis zu 3× (1 s/2 s/4 s) und löst bei 401 den `TokenRefresher` aus. Der Pairing-Endpunkt läuft bewusst außerhalb (`DevicePairingClient`); bei widerrufenem Geräte-Token löst jeder Fach-Request erst einen Silent-Relogin aus, der ebenfalls am Gate scheitert — akzeptabler Mehraufwand (ein Probe-Request), kein Funktionsverlust.
- **Gate-401 vs. Bearer-401:** Die Unterscheidung beruht auf der geklärten Konvention „Gate-Probe auf `api/health` ohne Bearer" — liegt `api/health` nicht hinter dem Gate oder ist die Annahme falsch, wird ein widerrufenes Token als `SessionExpired` (→ Login-Seite statt Pairing) behandelt. Restrisiko der Konvention; im Health-Check-Pfad greift dieselbe Annahme (401 ⇒ Gate-Ablehnung).
- **Server ohne Pairing-Endpunkt (Konsequenz des Verzichts auf Fallback):** Eine App-Installation ohne Geräte-Token und ohne statische Gate-Quelle, die auf einen Alt-Server ohne `pairing/exchange` trifft, bleibt mit `PairingNotSupportedError` in der Setup-Eingabe und kann sich nicht verbinden — bewusste Entscheidung; betroffene Anwender müssen den Server aktualisieren oder eine Alt-App-Version mit statischem Token nutzen. Der statische Token (`Jwt:ApiToken:Maui` serverseitig, `-p:VideoPlayerApiToken` beim Publish) bleibt für Alt-App-Versionen und als letzte `ClientGate.Resolve()`-Quelle funktional.
- **Nachrichten-Signaturen:** `ServerCheckCompletedMessage`, `ServerHealthCheckCompletedMessage`, `ServersLoadedMessage` erhalten ein neues `PairingRequired`-Feld → alle Konstruktionsstellen in `AppUpdate`-Tests, `MvuHostTests` und im `ShellAppEffectHandler` anpassen (vollständige Liste unter „Tests").
- **`PairingRequired`-Ermittlung:** hängt von `ClientGate.ResolveStatic()` ab; eine gesetzte Env-Variable auf Entwicklermaschinen verändert das Verhalten (Pairing wird dann nicht eingefordert) — gewollt (nahtlose Migration, geklärte Entscheidung), in Tests explizit abzudecken.
- **Serverwechsel:** Bei `ServerSwitchRequestedMessage`/`PersistActiveServerAsync` muss der Provider auf das Token des Zielservers umgestellt werden; ohne Token für den Zielserver → `PairingRequired` → Pairing-Eingabe (kein implizites Mitnehmen des alten Tokens). Ein für den Zielserver gespeicherter, aber widerrufener Token wird nicht in der Kandidatenprüfung verworfen (der Probe-Request sendete den Provider-Token des bisherigen Servers) — er wird beim anschließenden Auto-Login über den Laufzeitpfad erkannt und verworfen.
- **Legacy-Altbestand `VideoWebPlayer.Maui`:** eigener Gate-Mechanismus (`MauiClientConfiguration`), unverändert — dient nicht dem Pairing; Alt-Installationen laufen über den statischen Token weiter.
- **Statischer Zustand:** `ClientGate.DeviceTokenProvider` ist globaler veränderbarer Zustand — in Tests explizit setzen/zurücksetzen (Test-Isolation), sonst Seiteneffekte zwischen Tests.
- **`iOS-Deployment.ps1`:** Token landet in der Response-Datei im Repo-Verzeichnis (`.ios-deploy-*.rsp`, wird nach Gebrauch gelöscht) und in der Build-Ausgabe als Assembly-Metadatum — wie bisher, kein neues Geheimnis-Risiko, aber Display-Zeilen müssen maskiert bleiben.

## Umsetzungsreihenfolge

1. **Pairing-DTOs anlegen**
   - Voraussetzungen: keine
   - `PairingExchangeRequest`/`PairingExchangeResponse` in `VideoPlayer.ApiClient/Models/` mit `JsonPropertyName`-camelCase-Attributen.

2. **`PairingCrypto` implementieren**
   - Voraussetzungen: keine (BCL `System.Security.Cryptography`; `AesGcm(key, tagSizeInBytes)` ab .NET 8 verfügbar)
   - Interne statische Klasse: `ECDsa`/`ECDiffieHellman` nistP256 ephemeral, `ExportSubjectPublicKeyInfo` → Base64, `DeriveKeyFromHmac`-Ableitung, AES-256-GCM-Entschlüsselung.

3. **`DevicePairingClient` implementieren**
   - Voraussetzungen: Schritte 1–2
   - Eigener `HttpClient` (injizierbar), `ExchangeAsync(Uri baseAddress, string code, string? deviceName, CancellationToken)` → entschlüsseltes Token oder `ApiException`; kein Retry, kein Reauth.

4. **`ClientGate` erweitern**
   - Voraussetzungen: keine
   - `DeviceTokenProvider`, `ResolveStatic()`, neue interne `Resolve`-Überladung, Priorität Geräte-Token zuerst, Diagnosezeile erweitern.

5. **`HealthCheckResult`-Enum einführen und Erreichbarkeitsprüfung umstellen**
   - Voraussetzungen: keine (fachlich unabhängig; wird erst in Schritt 13 konsumiert)
   - `HealthCheckResult` (`Reachable`/`GateRejected`/`Unreachable`) in `VideoPlayer.ApiClient` anlegen; `VideoPlayerApiClient.CheckHealthAsync` → `Task<HealthCheckResult>` (401 → `GateRejected`, sonstige `ApiException` → `Unreachable`); `IServerHealthProbe.CheckAsync` (beide Überladungen) → `Task<HealthCheckResult>`; `ServerHealthProbe` reicht durch, Catch-All → `Unreachable`.

6. **`AppSecretsService` um Geräte-Token erweitern**
   - Voraussetzungen: keine (Entscheidung server-bezogener Schlüssel ist geklärt)
   - `DeviceTokenKey` + `Get/Set/Clear/HasDeviceTokenAsync(serverAddress)` nur über `ISecretStore`.

7. **`PairingStatus`-Enum und `PairingCode`-Helper anlegen**
   - Voraussetzungen: keine
   - Beide in `VideoPlayer.Mvu`; neue Enum-Werte brauchen später Testabdeckung (`EnumCoverageTests`/Git-Hook) — ebenso `HealthCheckResult` aus Schritt 5.

8. **`DeviceTokenService` implementieren**
   - Voraussetzungen: Schritte 4, 6
   - Laden/Setzen/Verwerfen (`LoadForServerAsync`, `SetPairedTokenAsync`, `DiscardCurrentAsync`, `DiscardIfCurrentAsync`), `DeviceTokenProvider`-Pflege inkl. `CurrentServerAddress`, `IsGateRejectedAsync` (Probe `api/health` nur mit `X-API-Key` über injizierbare `HttpClient`-Fabrik), `NotifyRejected()` → `DeviceTokenRejected`-Ereignis.

9. **`VideoPlayerApiClient.SetApiKey` ergänzen**
   - Voraussetzungen: keine
   - Header-Aktualisierung an bestehenden Clients.

10. **MVU-Modell, Nachrichten und Effekt erweitern**
    - Voraussetzungen: Schritt 7
    - `AppModel`-Felder, neue Nachrichten, `PairingRequired`-Feld an `ServerCheckCompletedMessage`/`ServerHealthCheckCompletedMessage`/`ServersLoadedMessage`, `ExchangePairingCodeEffect`.

11. **`AppUpdate` um den Pairing-Flow erweitern**
    - Voraussetzungen: Schritte 7, 10
    - Neue Übergänge, Fehlerkonstanten, `PairingRequired`-Verzweigungen, Reset-Semantik der Pairing-Felder in `BeginServerCheck`/`UpdateServerCheckCompleted`/`UpdateHealthCheckCompleted`, Stale-Result-Guard in `UpdatePairingCompleted`, `UpdateDeviceTokenRevoked` inkl. Start-Felder (`IsStartupRunning = false`, `ServerHealth = Reachable`); `NotSupported` → `PairingNotSupportedError` ohne Fallback.

12. **`AuthSessionService` um Gate-Probe-Verzweigung erweitern**
    - Voraussetzungen: Schritt 8
    - `SilentReloginAsync` 401 → `IsGateRejectedAsync` → `DiscardCurrentAsync` + `NotifyRejected` statt `SessionExpired`.

13. **`ShellAppEffectHandler` erweitern**
    - Voraussetzungen: Schritte 3, 5, 6, 8, 10, 11
    - `ExchangePairingCodeEffect`-Zweig (inkl. `DeviceInfo`-Gerätename), `PairingRequired`-Ermittlung in den Check-Methoden, `GateRejected`-Verzweigungen in `RunHealthCheckAsync` (Discard + Re-Probe + `NotifyRejected`) und `CheckCandidateServerAsync` (`Success = true` + `DiscardIfCurrentAsync`), Provider-Pflege in `LoadServersAsync`/`PersistActiveServerAsync`, `DeviceTokenRejected`-Abo → `DeviceTokenRevokedMessage`.

14. **`ServerSetupPage` um Pairing-Eingabe erweitern**
    - Voraussetzungen: Schritte 10–11
    - XAML-Abschnitt + `OnPairClicked` + `UpdateUi`-Bindings; Muster `AddressEntry`/`ConnectButton`/`CheckIndicator`/`ErrorLabel` wiederverwenden.

15. **`MauiProgram`-Registrierungen ergänzen**
    - Voraussetzungen: Schritte 3, 8, 13
    - `DevicePairingClient`, `DeviceTokenService` als Singletons.

16. **`iOS-Deployment.ps1`: `-p:VideoPlayerApiToken` durchreichen**
    - Voraussetzungen: keine
    - Neuer Parameter + `Add-PropertyLine`/`Add-MaskedDisplay` in allen Build-/Publish-Response-Dateien.

17. **Tests schreiben und betroffene Tests anpassen**
    - Voraussetzungen: Schritte 1–16 (Teststubs `StubHttpMessageHandler`, `InMemorySecretStore`, `ApiClientTestFactory`, `RepositoryRoot` sind vorhanden)
    - Neue Testklassen/-methoden gemäß Abschnitt „Tests"; `EnumCoverageTests` für `PairingStatus` **und** `HealthCheckResult` pflegen; Signaturen geänderter Nachrichten und `CheckHealthAsync` in bestehenden Tests nachziehen.

18. **Zwischenlösung und API-Doku dokumentieren**
    - Voraussetzungen: Schritte 5, 16
    - Kurzer Abschnitt (z. B. `README.md` bzw. Kommentarblock im Skript): statischer Token via `-p:VideoPlayerApiToken` / `iOS-Deployment.ps1 -VideoPlayerApiToken` als Publish-/Legacy-Zwischenlösung für Alt-App-Versionen (kein Laufzeit-Fallback in dieser App-Version); `docs/help/api-client/api.md`/`installation.md`/`troubleshooting.md` an den `HealthCheckResult`-Rückgabetyp anpassen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Resolve_PrefersDeviceTokenOverEnvironment` | `ClientGateTests` | Geräte-Token schlägt Env/Metadata/Datei. |
| `Resolve_FallsBack_WhenDeviceTokenProviderReturnsPlaceholderOrNull` (Theory) | `ClientGateTests` | Leerer/Platzhalter-Geräte-Token wird übersprungen; statische Kette greift. |
| `ResolveStatic_IgnoresDeviceTokenProvider` | `ClientGateTests` | `ResolveStatic` liefert nur statische Quellen. |
| Provider-Reset-Hilfsmethode (Setup/Teardown) | `ClientGateTests` | `ClientGate.DeviceTokenProvider` vor/nach jedem Test zurücksetzen (Test-Isolation). |
| `CheckHealthAsync_ReturnsGateRejected_OnUnauthorized` | `VideoPlayerApiClientTests_Authentication` | `StubHttpMessageHandler` mit 401 → `HealthCheckResult.GateRejected`. |
| `CheckHealthAsync_ReturnsUnreachable_OnError` (Theory: 404, 500) | `VideoPlayerApiClientTests_Authentication` | Nicht-401-Status → `Unreachable` (500 über `CreateClientWithDelays`/`RecordingDelays` für die Wiederholungen). |
| `SetApiKey_ReplacesGateHeader_OnSubsequentRequests` | `VideoPlayerApiClientTests_Authentication` | Nach `SetApiKey("…")` trägt der nächste Request (`handler.Requests[n].ApiKey`) den neuen Schlüssel — Nachweis der Header-Aktualisierung an bereits erzeugten Clients (Anforderung AC für Re-Pairing). |
| `SetApiKey_RejectsNullOrEmpty` (Theory) | `VideoPlayerApiClientTests_Authentication` | Guard: `null`/leer → `ArgumentException`/`ArgumentNullException` (kein heimliches Entfernen des Gate-Headers). |
| `ExchangeAsync_SendsRequest_WithoutGateAndBearerHeaders` | `DevicePairingClientTests` (neu) | `StubHttpMessageHandler.Requests`: `ApiKey == null`, `Authorization == null`, `POST api/pairing/exchange`. |
| `ExchangeAsync_SendsCamelCasePayload` | `DevicePairingClientTests` | Body enthält `code`, `clientPublicKey` (Base64-SPKI), `deviceName` camelCase. |
| `ExchangeAsync_MapsErrorStatus` (Theory: 400/401/404/429) | `DevicePairingClientTests` | `ApiException.StatusCode` korrekt; **429 ohne Wiederholung** (nur 1 Request in `Requests`). |
| `ExchangeAsync_DecryptsToken_Roundtrip` | `DevicePairingClientTests` / `PairingCryptoTests` | Selbst erzeugtes Server-ECDH-Paar → verschlüsseltes Token → `ExchangeAsync`/Crypto entschlüsselt zum Klartext. |
| `Decrypt_*` (SPKI-Import, Nonce/Tag-Layout, falscher Key) | `PairingCryptoTests` (neu) | Ableitung/Entschlüsselung gemäß Vertrag; Fehler → `CryptographicException`. |
| `GetDeviceToken_GoesToSecretStore_Only` / `SetDeviceToken_*` / `ClearDeviceToken_*` | `AppSecretsServiceTests` | Nur `ISecretStore` (`InMemorySecretStore`), Server- und Profil-Präfix im Schlüssel, kein `ISettingsStore`-Eintrag. |
| `SetDeviceToken_IsolatedPerServer` | `AppSecretsServiceTests` | Tokens zweier Serveradressen überschreiben sich nicht. |
| `LoadForServer_SetsClientGateProvider` / `DiscardCurrent_ResetsProvider` | `DeviceTokenServiceTests` (neu) | `ClientGate.Resolve()` liefert Geräte-Token / danach statischen Fallback; `CurrentServerAddress` wird verfolgt. |
| `DiscardIfCurrent_OnlyDiscardsMatchingServer` | `DeviceTokenServiceTests` | Verwerfen nur, wenn die Adresse dem aktuell bedienten Server entspricht; fremde Adresse → no-op. |
| `NotifyRejected_RaisesDeviceTokenRejected` | `DeviceTokenServiceTests` | Ereignis wird ausgelöst; `DiscardCurrentAsync` allein löst es nicht aus. |
| `IsGateRejected_*` (Probe 401 vs. 200) | `DeviceTokenServiceTests` | Unterscheidung Gate- vs. Bearer-401 über Probe-`HttpClient` (injizierbare Fabrik). |
| `Update_ServerCheckCompleted_Success_PairingRequired_ShowsPairingInput` | `AppUpdateTests_ServerManagement` | `IsPairingRequired = true`, kein `AttemptAutoLoginEffect`, `PersistActiveServerEffect` ja. |
| `Update_ServerCheckCompleted_Success_WithoutPairing_KeepsLegacyFlow` | `AppUpdateTests_ServerManagement` | Bisheriger Pfad bleibt (Persist + AutoLogin) — nahtlose Migration mit statischem Token. |
| `Update_ServerCheckCompleted_Failure_ResetsPairingFields` | `AppUpdateTests_ServerManagement` | Misserfolg leert `PairingError`, setzt `IsPairingRequired`/`IsPairingRunning` zurück. |
| `Update_AddressSubmitted_ResetsPairingFields` | `AppUpdateTests_ServerManagement` | `BeginServerCheck` leert `PairingError`/`IsPairingRequired`/`IsPairingRunning` bei neuer Adresseingabe. |
| `Update_HealthCheckCompleted_Reachable_PairingRequired_GoesToSetup` | `AppUpdateTests_Startup` | `NeedsServerSetup` + `ServerSetup` statt `AttemptAutoLoginEffect`; `IsPairingRequired = true`, `IsStartupRunning = false`. |
| `Update_HealthCheckCompleted_Unreachable_ResetsPairingFields` | `AppUpdateTests_Startup` | Unerreichbar-Pfad leert Pairing-Felder (kein stale `PairingError`). |
| `Update_ServersLoaded_SetsPairingFlags` | `AppUpdateTests_Startup` | `PairingRequired` aus der Nachricht → `IsPairingRequired` ins Modell; `PairingError` bleibt unberührt. |
| `Update_PairingCodeSubmitted_Invalid_SetsErrorWithoutEffects` (Theory) | `AppUpdateTests_Pairing` (neu) | `PairingCode.TryNormalize`-Fehler → `InvalidPairingCodeError`, kein Effekt. |
| `Update_PairingCodeSubmitted_Valid_StartsExchange` | `AppUpdateTests_Pairing` | `IsPairingRunning`, `ExchangePairingCodeEffect` mit Adresse+Code. |
| `Update_PairingCompleted_Succeeded_ContinuesAutoLogin` | `AppUpdateTests_Pairing` | Flags zurückgesetzt, `AttemptAutoLoginEffect`. |
| `Update_PairingCompleted_MapsErrors` (Theory: `InvalidCode`/`RateLimited`/`InvalidRequest`/`ServerUnreachable`/`Failed`) | `AppUpdateTests_Pairing` | Je Status erwartete `PairingError`-Konstante, `IsPairingRunning = false`. |
| `Update_PairingCompleted_NotSupported_ShowsError_WithoutFallback` | `AppUpdateTests_Pairing` | `NotSupported` → immer `PairingNotSupportedError`, `IsPairingRunning = false`, **kein** `AttemptAutoLoginEffect` (kein Legacy-Fallback auf den statischen Token). |
| `Update_PairingCompleted_StaleResult_WhenNotRunning_IsIgnored` | `AppUpdateTests_Pairing` | `PairingCompletedMessage` bei `IsPairingRunning == false` → `Unchanged` (verspätetes Ergebnis nach Serverwechsel/neuem Check). |
| `Update_DeviceTokenRevoked_NavigatesToSetupWithError` | `AppUpdateTests_Connection` / `AppUpdateTests_Pairing` | `NeedsServerSetup` + `NavigateToPageEffect(ServerSetup)` + `DeviceRevokedError` + `DisconnectServerEventsEffect`. |
| `Update_DeviceTokenRevoked_DuringStartup_ShowsPairingInputWithError` | `AppUpdateTests_Startup` | **Start-/Health-Check-Pfad:** Modell `IsStartupRunning = true`, `ServerHealth = Checking`, `ActiveServerAddress` gesetzt → `DeviceTokenRevokedMessage` → `IsStartupRunning = false`, `NeedsServerSetup`, `CurrentPage = ServerSetup` (+ `NavigateToPageEffect`), `IsPairingRequired = true`, `PairingError = DeviceRevokedError` — der widerrufene Token führt nicht mehr in `CheckOfflineDataEffect`/Offline. |
| `SilentRelogin_GateRejected_RaisesDeviceTokenRejected_NotSessionExpired` | `AuthSessionServiceTests` | 401 + Probe-401 → `DeviceTokenRejected`, kein `SessionExpired`; Probe-OK → `SessionExpired` wie bisher. |
| `HealthCheckResult`-/`PairingStatus`-Werte in Tests verwenden | `EnumCoverageTests`-Konformität | `enum-coverage-check`-Hook: jeder Enum-Wert beider neuer Enums kommt in einer Testdatei vor. |
| Neue AutomationIds/Dispatches/Handler-Zweige | `ServerSetupContractTests`, `ConnectionWorkflowContractTests` | Quelltext-Vertrag: `PairingCodeEntry`, `PairButton`, `PairingIndicator`, `PairingErrorLabel`, `OnPairClicked`→`PairingCodeSubmittedMessage`, `ExchangePairingCodeEffect`-Zweig im `ShellAppEffectHandler`, DI-Registrierungen in `MauiProgram`, `iOS-Deployment.ps1` enthält `VideoPlayerApiToken`, **`RunHealthCheckAsync` enthält den `GateRejected`-Zweig (`DiscardCurrentAsync` + Re-Probe + `NotifyRejected`), `CheckCandidateServerAsync` bildet `GateRejected` auf `Success = true` ab** — Start-/Health-Check-Pfad ist nur per Quelltext-Vertrag prüfbar (MAUI-Handler nicht im Testprojekt referenzierbar). |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `AppUpdateTests_ServerManagement` (`ServerCheckCompletedMessage`-Konstruktionen, Zeilen ~135/161/202) | Neue Signatur (`PairingRequired`-Feld). |
| `AppUpdateTests_Connection` — `ServerCheckCompleted_Success_EmitsPersistAndAutoLogin` (Zeile ~433) und `ServerCheckCompleted_Success_ResetsSourcesOverviewAndBrowser` (Zeile ~465) | Neue `ServerCheckCompletedMessage`-Signatur (`PairingRequired`-Feld). |
| `AppUpdateTests_Startup` — `ServersLoadedMessage`-Konstruktionen (Zeilen ~54/74/88/105) und `ServerHealthCheckCompletedMessage`-Konstruktionen (Zeilen ~124/142/164) | Neue Signaturen (`PairingRequired`-Feld). |
| `AppUpdateTests_Offline` — `Reconnection_FromOffline_RunsFullSyncAndResumesDownloads` (Zeile ~240) | Neue `ServerHealthCheckCompletedMessage`-Signatur. |
| `MvuHostTests` (Zeilen ~30–31: `ServersLoadedMessage`, `ServerHealthCheckCompletedMessage`) | Neue Signaturen. |
| `VideoPlayerApiClientTests_Authentication` — `CheckHealthAsync_ReturnsTrueOnSuccess`, `CheckHealthAsync_ReturnsFalseOnFailure` | Rückgabetyp `bool` → `HealthCheckResult` (Assertions auf `Reachable`/`Unreachable` umstellen). |
| `ClientGateTests` (Setup) | `DeviceTokenProvider` muss zwischen Tests zurückgesetzt werden, sonst Seiteneffekte. |
| `AuthSessionServiceTests` | `AuthSessionService` erhält neue Abhängigkeit `DeviceTokenService` (Stub/Testdouble nötig); 401-Pfade prüfen neuen Zweig. |
| `ServerSetupContractTests`, `ConnectionWorkflowContractTests`, `VideoPlayerMauiContractTests` | Quelltext-Verträge um Pairing-/GateRejected-Zweige und `IServerHealthProbe`-/`CheckHealthAsync`-Signatur erweitern (bestehende String-Assertions wie `CheckHealthAsync`/`ServerHealthCheckCompletedMessage` bleiben gültig). |

### E2E-Tests (primärer Funktionsnachweis)

Das Repo enthält **keine UI-/E2E-Testinfrastruktur**: Das Testprojekt `VideoWebPlayer.Maui.Tests` referenziert die MAUI-Projekte nicht (MAUI-Zielframeworks), UI-Verdrahtung wird projektweit ausschließlich über Quelltext-Contract-Tests abgesichert, und ein echter Pairing-E2E-Lauf benötigt zusätzlich einen laufenden Server mit Admin-erzeugtem Einmal-Code sowie ein physisches Gerät/ einen Simulator (`SecureStorage`, `DeviceInfo`). Die Einführung einer E2E-Toolchain ist nicht Teil dieser Anforderung; AC 7 der Anforderung benennt den geforderten Testnachweis selbst (Auflösereihenfolge, MVU-Message-Flow, Negativfälle). Die Benutzerflüsse werden daher — entsprechend der im Repo etablierten Konvention — durch die vollständige MVU-Update-Abdeckung (Nachrichtenfluss = Programmlogik) plus Quelltext-Contract-Tests (UI-Verdrahtung, AutomationIds, Dispatches, DI, Handler-Zweige inkl. `GateRejected`-Pfad) nachgewiesen; der echte Gerätefluss ist zusätzlich manuell über das Deployment-Skript gegen einen Testserver zu verifizieren.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Server verbinden → Code eingeben → gekoppelt → Anmeldung läuft | `AppUpdateTests_Pairing` + `ServerSetupContractTests` (Contract) | AC 1, 2 | Benutzerfluss; MVU-Tests decken den vollständigen Zustandsautomaten ab, Contract-Tests die UI-Verdrahtung. Kein E2E-Framework im Repo (Begründung oben). |
| Pflicht | Ungültiger/abgelaufener Code → Inline-Fehler, erneute Eingabe möglich | `AppUpdateTests_Pairing` (Theory) + `DevicePairingClientTests` | AC 7 (Negativfälle) | Fehlerfall über UI auslösbar; über Update-Tests + Client-Tests nachgewiesen. |
| Pflicht | 429 IP-Sperre → Hinweis, kein automatisches Wiederholen | `DevicePairingClientTests` + `AppUpdateTests_Pairing` | AC 7 | Vertragsverhalten des öffentlichen Endpunkts. |
| Pflicht | Widerrufenes Geräte-Token → Re-Pairing-Flow mit Fehlertext (Laufzeit **und** Start/Health-Check) | `AppUpdateTests_Connection`/`AppUpdateTests_Pairing` + `AppUpdateTests_Startup` (`DeviceTokenRevoked` beim Start) + `DeviceTokenServiceTests` + `AuthSessionServiceTests` + Contract-Test für den `GateRejected`-Zweig in `RunHealthCheckAsync` | AC 5 | Service-Rand-Ereignis → Navigation; über Update- und Service-Tests nachgewiesen; der MAUI-Handler selbst ist nur per Quelltext-Vertrag prüfbar. |
| Pflicht | Token-Persistenz in `SecureStorage` (nicht Preferences) | `AppSecretsServiceTests` | AC 3 | Ablageort; über `ISecretStore`-Fake nachweisbar. |
| Pflicht | Server ohne Pairing-Endpunkt → `PairingNotSupportedError`, App bleibt in Pairing-Eingabe (kein Fallback) | `AppUpdateTests_Pairing` (`NotSupported`) | AC 8 (geklärt: kein Fallback) | Entscheidung im Zustandsautomaten; sichert die „kein Legacy-Pfad"-Festlegung ab. |
| Manuell | Echter Pairing-Lauf gegen Testserver (iOS/Android/Windows) | `scripts/iOS-Deployment.ps1` + Testserver | AC 1–6 gesamt | `SecureStorage`/`DeviceInfo` sind nur auf echten Zielen prüfbar; manueller Nachweis im Testprotokoll dokumentieren. |

Bestehende E2E-Tests: Keine vorhanden, folglich keine betroffen.

## Offene Punkte

Keine.
