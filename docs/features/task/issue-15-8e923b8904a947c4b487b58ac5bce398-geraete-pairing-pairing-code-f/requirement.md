# Übersetzte Anforderung: Geräte-Pairing per Einmal-Code und gerätegebundenes Token

Branch: `task/issue-15-8e923b8904a947c4b487b58ac5bce398-geraete-pairing-pairing-code-f`

## Fachliche Zusammenfassung

Der statische API-Token für das Client-Gate (`X-API-Key`-Header, Scope `MauiOnly`), den `ClientGate.Resolve()` in `VideoPlayer.ApiClient` aus Umgebungsvariable → Assembly-Metadaten (`-p:VideoPlayerApiToken`) → Token-Datei → Platzhalter auflöst, wird als primärer Mechanismus durch ein gerätegebundenes Token abgelöst: Die App `VideoPlayer.Maui` koppelt sich beim Erststart über einen vom Server (Admin-UI) erzeugten Einmal-Pairing-Code per `POST api/pairing/exchange` mit dem Server. Der Austausch läuft über ein flüchtiges ECDH-Schlüsselpaar (nistP256); der Server liefert das Geräte-Token AES-256-GCM-verschlüsselt, die App entschlüsselt es und legt es im `SecureStorage` (über `ISecretStore`/`SecureStorageSecretStore`, nicht in Preferences) ab. `ClientGate.Resolve()` wird um die SecureStorage-Quelle erweitert (Priorität: Geräte-Token > Env > Metadata > Datei > Platzhalter); ein 401 wegen widerrufenem/abgelaufenem Geräte-Token führt zurück in die Pairing-Eingabe der `ServerSetupPage`. Der Server-Vertrag (Endpunkt, Felder, Krypto-Ableitung, Fehlercodes) ist in der Anforderung verbindlich dokumentiert; der statische Token bleibt serverseitig (`Jwt:ApiToken:Maui`) und über den Publish-Pfad `-p:VideoPlayerApiToken` als Fallback/Zwischenlösung bestehen.

## Akzeptanzkriterien (aus der Anforderung)

1. `ServerSetupPage` erhält ein Eingabefeld für den Pairing-Code neben der manuellen Adress-Eingabe; Flow „Server verbinden → Code eingeben → gekoppelt".
2. Der ApiClient ruft `POST api/pairing/exchange` mit einem flüchtigen ECDH-Schlüsselpaar auf und entschlüsselt das empfangene Geräte-Token.
3. Das Geräte-Token wird im `SecureStorage` persistiert (iOS-Keychain / Windows-Credential-Store) — nicht in Preferences.
4. `ClientGate.Resolve()` berücksichtigt die SecureStorage-Quelle mit der Priorität Geräte-Token > Env > Metadata > Datei > Platzhalter.
5. Re-Pairing-Flow: Bei 401 wegen widerrufenem/abgelaufenem Geräte-Token geht es zurück zur Pairing-Eingabe (`ServerSetupPage`) mit Fehlertext.
6. Der Geräte-Name (z. B. „Martins iPhone") wird im Request als `deviceName` an den Server gemeldet.
7. Tests: Auflösereihenfolge in `ClientGate`, Pairing-Message-Flow im MVU-Modell, Negativfälle (ungültiger/abgelaufener Code).
8. Die Zwischenlösung ist dokumentiert: Bis Server-Support überall existiert, wird der statische Token beim Publish via `-p:VideoPlayerApiToken` eingebettet (bereits über `scripts/iOS-Deployment.ps1` nutzbar).

### Verbindlicher Server-Vertrag (aus der Anforderung)

- **Endpunkt:** `POST /api/pairing/exchange` — öffentlich, kein `X-API-Key`, kein Bearer-Token; Rate-Limited pro IP (429 bei Sperre).
- **Request (camelCase-JSON):** `code` (string, Pflicht; Einmal-Code, 8 Zeichen, Alphabet `ABCDEFGHJKMNPQRSTUVWXYZ23456789`, Default-TTL 5 Min., einmalig verwendbar), `clientPublicKey` (string, Pflicht; flüchtiger ECDH-PublicKey nistP256, SPKI/DER, Base64), `deviceName` (string, optional; max. 200 Zeichen, getrimmt; Fallback serverseitig „Geraet vom <Datum, Uhrzeit>").
- **Response 200 (camelCase-JSON):** `serverPublicKey` (flüchtiger Server-ECDH-PublicKey, nistP256, SPKI/DER Base64), `encryptedToken` (AES-256-GCM-verschlüsseltes Geräte-Token; Layout `nonce(12) ‖ ciphertext ‖ tag(16)`, Base64).
- **Krypto-Ableitung (App-Seite):** `clientEcdh.DeriveKeyFromHmac(serverKey.PublicKey, HashAlgorithmName.SHA256, null, null, null)` → 32-Byte-AES-Key; `AesGcm(key, tagSize: 16)` mit den ersten 12 Bytes als Nonce und den letzten 16 Bytes als Tag.
- **Fehlercodes:** 400 = Request ungültig (leere Felder, kein importierbarer P-256-Key, `deviceName` > 200) — keine Fehlversuchszählung; 401 = Code unbekannt/abgelaufen/verbraucht — zählt zur IP-Sperre (Schwelle wie Web-Login); 429 = IP temporär gesperrt.
- **Geräte-Token:** Format `Base64Url(32 Zufallsbytes)`; nach dem Entschlüsseln im `SecureStorage` ablegen und als `X-API-Key` senden (Scope `MauiOnly`, deckt `POST api/auth/login` ab). Widerruf in `/admin/devices` wirkt sofort auf den Gate-Check (401 beim nächsten Request → Re-Pairing-Flow); bereits ausgestellte Benutzer-JWTs bleiben bis Ablauf (12 h) gültig.
- **DTOs:** `PairingExchange*` liegen serverseitig im geteilten Client-Projekt `VideoWebPlayer.Client/Models/` (Server-Repo); Vertrags-Doku `docs/API.md` und `docs/help/geraete/` im Server-Repo.

## Betroffene Klassen und Komponenten

- **`VideoPlayer.ApiClient`:**
  - `ClientGate` (erweitern): neue Token-Quelle „Geräte-Token aus sicherer Ablage" mit höchster Priorität in `Resolve()` bzw. einer neuen, die Quelle einbindenden Auflösung. Zu beachten: `Resolve()` ist synchron, `SecureStorage`/`ISecretStore` ist asynchron und liegt hinter der `VideoPlayer.Storage`-Abstraktion — die Quelle muss als injizierbarer Provider/Delegate angelegt werden (Annahme, keine MAUI-Abhängigkeit in `VideoPlayer.ApiClient`).
  - `VideoPlayerApiClient` (erweitern): neue Methode für den Pairing-Austausch (z. B. `ExchangePairingCodeAsync`), die `POST api/pairing/exchange` **ohne** `X-API-Key`- und Bearer-Header aufruft (der Endpunkt ist öffentlich); der im Konstruktor gesetzte Gate-Header darf bei diesem Request nicht greifen. Nach erfolgreichem (Re-)Pairing muss der `X-API-Key`-Header aktualisierbar sein — er wird derzeit nur einmal im Konstruktor gesetzt.
  - Neue Modelle (Annahme: Namespace `VideoPlayer.ApiClient.Models`, sofern die `PairingExchange*`-DTOs nicht aus `VideoWebPlayer.Client` übernommen werden): Request (`code`, `clientPublicKey`, `deviceName`) und Response (`serverPublicKey`, `encryptedToken`), camelCase-serialisiert.
  - Neue Krypto-Komponente (neu, plattformneutral `System.Security.Cryptography`): Erzeugung des flüchtigen ECDH-P8-Schlüsselpaars (nistP256), Export als SPKI/DER-Base64, Ableitung des 32-Byte-Schlüssels per `DeriveKeyFromHmac` und AES-256-GCM-Entschlüsselung (`nonce(12) ‖ ciphertext ‖ tag(16)`).
  - `ApiException` (bestehend): liefert die Statuscodes 400/401/429 für die Fehlerbehandlung.
- **`VideoPlayer.Storage`:**
  - `AppSecretsService` (erweitern): neuer Ablageschlüssel für das Geräte-Token in der sicheren Ablage (`ISecretStore`), analog `AuthTokenKey`/`PasswordKey` — inkl. Get-/Set-/Clear-Methoden. Zu klären: Profil-Präfix (`AppProfile.PrefixKey`) und/oder Server-Bezug des Schlüssels.
  - `ISecretStore` (bestehend, unverändert nutzbar); `KnownServersService` (bestehend: aktiver Server bleibt wie bisher persistiert).
- **`VideoPlayer.Mvu`:**
  - `AppModel` (erweitern): Pairing-Zustand — z. B. „Pairing läuft"-Flag und Pairing-Fehlertext (ggf. Wiederverwendung von `ServerSetupError`/`IsServerCheckRunning`, Annahme: eigene Felder wie `IsPairingRunning`, `PairingError`).
  - `AppMessage` (neu): Nachrichten für „Pairing-Code abgeschickt" (mit Serveradresse + Code), „Pairing abgeschlossen" (Erfolg/Fehler mit Grund: ungültiger Code, abgelaufen/verbraucht, IP-Sperre, Server ohne Pairing-Support, Transportfehler) sowie ein Auslöser für den Re-Pairing-Flow bei widerrufenem Geräte-Token (401 auf Gate-Ebene).
  - `AppEffect` (neu): Effekt für den Pairing-Austausch (Serveradresse, Code, Gerätename) — Ausführung durch `IAppEffectHandler` am App-Rand.
  - `AppUpdate` (erweitern): Übergänge des Pairing-Flows — nach erfolgreicher Serverprüfung (`ServerCheckCompletedMessage`) bzw. bei bestehendem Server ohne Geräte-Token die Pairing-Eingabe einfordern; nach erfolgreichem Pairing den bisherigen Anmelde-Workflow (`AttemptAutoLoginEffect` → `EnterConnected`) fortsetzen; bei 401 durch widerrufenes Geräte-Token das Token verwerfen, in `ConnectionState.NeedsServerSetup`/`AppPage.ServerSetup` zurückkehren und den Fehlertext setzen.
  - `ConnectionState`/`AuthAttemptStatus` (ggf. erweitern): Unterscheidung „Server verbunden, aber nicht gekoppelt" sowie Ergebnis-Status des Pairing-Versuchs (Annahme: eigenes Ergebnis-Enum analog `AuthAttempt`, um 400/401/429/Nicht-Erreichbarkeit abzubilden); `EnumCoverageTests` deckt neue Enum-Werte ab.
- **`VideoPlayer.Maui`:**
  - `Pages/ServerSetupPage.xaml` + `.xaml.cs` (erweitern): Eingabefeld für den Pairing-Code neben der manuellen Adress-Eingabe (AutomationIds, Fehleranzeige), Dispatch der neuen Pairing-Nachrichten über `MvuHost`.
  - `Services/ShellAppEffectHandler` (erweitern): Ausführung des Pairing-Effekts (Service-Aufruf, Dispatch der Ergebnis-Nachricht über `MvuHost`), Erkennung des „Geräte-Token widerrufen"-Falls und Einleitung des Re-Pairing-Flows (Navigation zur `ServerSetupPage` mit Fehlertext).
  - `Services/SecureStorageSecretStore` (bestehend, unverändert): Ablage des Geräte-Tokens.
  - `MauiProgram` (erweitern): DI-Registrierung des Pairing-Diensts bzw. der Token-Quelle für `ClientGate`.
  - Gerätename über `Microsoft.Maui.Devices.DeviceInfo` (Name/Modell des Geräts) ermitteln — plattformspezifisch, daher am App-Rand.
- **Tests (`VideoWebPlayer.Maui.Tests`):**
  - `ClientGateTests` (erweitern): neue Auflösereihenfolge inkl. Geräte-Token-Quelle und Platzhalter-Überspringen.
  - MVU-Tests (`AppUpdateTests_*`, `ServerSetupContractTests`, ggf. neue `PairingContractTests`): Pairing-Message-Flow (Erfolg, ungültiger/abgelaufener Code, 429-Sperre, Re-Pairing bei 401), UI-Vertrag der neuen Eingabeelemente.
  - ApiClient-/Krypto-Tests: Pairing-Endpunkt-Aufruf ohne Gate-Header, ECDH/AES-GCM-Roundtrip (Ableitung + Entschlüsselung, ggf. mit Referenz-Testvektor aus dem Server-Repo), Fehlercode-Mapping 400/401/429.
- **Dokumentation/Build:** Zwischenlösung dokumentieren (`-p:VideoPlayerApiToken`-Einbettung beim Publish, `scripts/iOS-Deployment.ps1`); spätere `docs/help/`-Seite zum Pairing (Schritt 12 des Lifecycle).

## Implementierungsansatz

- Der Pairing-Code-Flow gliedert sich in den bestehenden MVU-Verbindungs-Workflow ein: Nach erfolgreicher Serverprüfung (`CheckCandidateServerEffect`/`ServerCheckCompletedMessage`) bzw. beim Start mit hinterlegtem Server prüft der Zustandsautomat, ob ein Geräte-Token in der sicheren Ablage vorliegt; ohne Token wird die Pairing-Eingabe auf der `ServerSetupPage` aktiv (`ConnectionState.NeedsServerSetup` bleibt der Navigationsanker — kein neuer Seitentyp nötig). Erst nach erfolgreichem Pairing läuft der bisherige Pfad (`AttemptAutoLoginEffect` → `EnterConnected`) weiter.
- Der Pairing-Austausch ist ein neuer `AppEffect` (z. B. `ExchangePairingCodeEffect`), den `ShellAppEffectHandler` über einen Pairing-Dienst ausführt: ECDH-Schlüsselpaar erzeugen, Request senden, Response entschlüsseln, Geräte-Token über `AppSecretsService`/`ISecretStore` persistieren und Ergebnis als `AppMessage` zurückmelden. Die Krypto bleibt plattformneutral in `VideoPlayer.ApiClient` bzw. einem eigenen Dienst, damit das Testprojekt (kein MAUI) sie abdecken kann.
- `ClientGate.Resolve()` wird um die Geräte-Token-Quelle erweitert. Da `ClientGate` synchron und plattformneutral ist, liegt die naheliegende Umsetzung (Annahme) in einem einmalig beim App-Start bzw. nach dem Pairing gesetzten Provider bzw. einem über `VideoPlayerApiClientOptions.ApiKey` pro Client gesetzten aufgelösten Token — die Detailform ist in der Planung festzulegen, die Priorität (Geräte-Token > Env > Metadata > Datei > Platzhalter) ist vorgegeben.
- Re-Pairing: Ein 401 des Gate-Checks (widerrufenes/abgelaufenes Geräte-Token) muss von einem abgelaufenen Benutzer-JWT unterschieden werden — die JWT-Erneuerung läuft bereits über `VideoPlayerApiClientOptions.TokenRefresher`/`AuthSessionService.SilentReloginAsync`. Scheitert die Gate-Autorisierung, wird das Geräte-Token verworfen und die App wechselt mit Fehlertext zur Pairing-Eingabe (analog zum bestehenden `SessionExpired`-Mechanismus über ein Ereignis am Service-Rand).
- Der Geräte-Name wird beim Pairing-Request aus `DeviceInfo` (z. B. `DeviceInfo.Name`, ggf. ergänzt um Modell/Hersteller) ermittelt und als `deviceName` gesendet (max. 200 Zeichen).
- Fehler-Mapping im Flow: 400 → Eingabefehler/Programmfehler (keine IP-Zählung), 401 → „Code ungültig, abgelaufen oder bereits verwendet" mit erneuter Eingabe, 429 → „Zu viele Versuche, bitte später erneut" — jeweils als Inline-Fehlertext der `ServerSetupPage` (Muster wie `InvalidAddressError`/`UnreachableServerError` in `AppUpdate`).
- Fallback: Server ohne Pairing-Endpunkt (404) bzw. alte App-Versionen laufen über den statischen Token weiter (`Jwt:ApiToken:Maui` serverseitig, `-p:VideoPlayerApiToken` beim Publish) — der Platzhalter-/Datei-/Env-/Metadata-Pfad von `ClientGate` bleibt funktional.

## Konfiguration

- Keine neue Anwendungseinstellung gefordert — die Anforderung sieht kein konfigurierbares Verhalten in der App vor.
- Serverseitige Optionen (nur Kontext, nicht App-seitig): `Pairing:CodeLength` (Default 8, min. 4), `Pairing:CodeTtlMinutes` (Default 5, min. 1); `Jwt:ApiToken:Maui` bleibt als Fallback aktiv.
- Bestehende, unveränderte Auflösungswege: `VIDEOWEBPLAYER_MAUI_API_TOKEN` (Env), `VideoPlayer.ApiClient.ApiToken` (Assembly-Metadata, via `-p:VideoPlayerApiToken`), Token-Datei `client-api-token.txt` unter `ClientGate.TokenFileDirectory`, Platzhalter `<MAUI_CLIENT_API_TOKEN>`; `VIDEOPLAYER_SERVER_URL` für die Serveradresse bleibt bestehen.

## Offene Fragen

- **DTO-Herkunft:** Werden die `PairingExchange*`-Modelle aus dem geteilten `VideoWebPlayer.Client`-Projekt des Server-Repos in dieses Repo übernommen/synchronisiert, oder definiert `VideoPlayer.ApiClient` eigene Modelle? (Der Endpunkt ist öffentlich und camelCase; die DTOs liegen serverseitig vor.)
- **Asynchrone Quelle in `ClientGate.Resolve()`:** `SecureStorage` ist asynchron — Soll die Auflösung weiter synchron bleiben (Provider beim Start/nach Pairing gesetzt oder Token pro Client über `VideoPlayerApiClientOptions.ApiKey` injiziert) oder wird eine asynchrone Variante eingeführt? Konsequenz für Stellen, die `ClientGate.Resolve()` direkt aufrufen (`VideoPlayerApiClient`-Konstruktor, `ServerHealthProbe`).
- **Unterscheidung der 401-Fälle:** Woran erkennt die App verlässlich, dass ein 401 das Geräte-Token (Gate) und nicht das Benutzer-JWT (Bearer) betrifft — gibt es serverseitig unterscheidbare Antworten (Header/Body), oder gilt die Konvention „stille JWT-Neuanmeldung erfolgreich, aber Request bleibt 401 → Geräte-Token widerrufen"?
- **Server-Bezug des Geräte-Tokens:** Ist das Token pro Serveradresse zu speichern (Wechsel zwischen bekannten Servern ohne Neu-Pairing) oder reicht ein einzelner aktueller Token, der beim Serverwechsel verworfen wird? Schlüsselschema in `AppSecretsService` (Profil-Präfix, ggf. Server-Adresse im Key) davon abhängig.
- **Verhalten bei Server ohne Pairing-Endpunkt (404):** Soll die App dann auf den statischen Token zurückfallen und ggf. eine Hinweis/„Legacy-Modus"-Anzeige zeigen, oder gilt Pairing als verbindlich?
- **Gerätename-Vorbelegung:** Welcher `DeviceInfo`-Wert soll als `deviceName` gesendet werden (`DeviceInfo.Name` vs. zusammengesetzter Name), und soll der Anwender den Namen in der Pairing-Eingabe überschreiben können?
- **Bestehende Installationen:** Migrieren bereits installierte Apps mit statischem Token nahtlos (Token greift weiter als Fallback), oder wird beim ersten Start mit Pairing-fähigem Server aktiv ein Pairing eingefordert?
