# Detail: MVU-Fluss Server-Setup → Pairing → Login → Home

Quellen: `VideoPlayer.Mvu/AppUpdate.cs`,
`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`,
`VideoPlayer.Maui/Services/ServerHealthProbe.cs`,
`VideoPlayer.Mvu/AuthSessionService.cs`,
`VideoPlayer.ApiClient/{VideoPlayerApiClient,DevicePairingClient,ClientGate}.cs`.

## Erststart

`StartupRequestedMessage` → `InitializeStorageEffect`,
`LoadSettingsEffect`, `LoadServersEffect`,
`StartNetworkMonitoringEffect`, `LoadHomeListsEffect`.
`LoadServersAsync` liest `KnownServersService` +
`IServerAddressProvider`; ohne aktive Adresse → `NeedsServerSetup` +
`CurrentPage=ServerSetup` (Window-Root, keine Shell nötig).

## Adresse → Verbinden

`OnConnectClicked` → `ServerAddressSubmittedMessage` →
`ServerAddress.TryNormalize` (Schema optional, http/https,
`scheme://host[:port][/pfad]/` kanonisch; ungültig →
`serverSetupError="Bitte eine gültige Serveradresse eingeben."`).
Gültig → `CheckCandidateServerEffect` → `ServerHealthProbe.CheckAsync`
→ `GET api/health` mit `X-API-Key` (adressspezifisch: Geräte-Token des
Kandidaten, sonst `ClientGate.ResolveStatic()`).

- `Unreachable` (Transportfehler/Timeout/5xx nach Retry 1+2+4 s!) →
  `serverSetupError="Der Server ist nicht erreichbar. Bitte die Adresse
  prüfen."` — **Achtung Test-Timing:** der API-Client wiederholt
  Transportfehler bis zu 3× (1 s+2 s+4 s) plus HttpClient-Timeout
  (10 s Probe) — Fehlerszenario „nicht erreichbar" braucht
  großzügige Wartezeit (Connection-Refused ist sofort, aber Retry-
  Delays summieren sich → ~15–20 s Warte-Fenster einplanen).
- `GateRejected` (401) → Geräte-Token des Kandidaten verwerfen,
  `success=true` → `pairingRequired`-Prüfung.
- `Reachable` (2xx) → `pairingRequired = IsPairingRequiredAsync(addr)`
  = kein `secrets.HasDeviceTokenAsync(addr)` UND
  `ClientGate.ResolveStatic()==ApiTokenPlaceholder`.

## Pairing

`IsPairingRequired` → `PairingSection` sichtbar
(`PairingCodeEntry`+`PairButton`). `OnPairClicked` →
`PairingCodeSubmittedMessage` → `PairingCode.TryNormalize`
(Alphabet `ABCDEFGHJKMNPQRSTUVWXYZ23456789`, MinLength 4; Formatfehler
→ `pairingError="Der Code ist nicht vollständig …"` lokal, ohne
Server-Kontakt) → `ExchangePairingCodeEffect` →
`DevicePairingClient.ExchangeAsync`:

- Request `POST api/pairing/exchange` (kein Gate-/Bearer-Header,
  Timeout 15 s, **keine Retries**): `{"code","clientPublicKey"
  (SPKI/DER Base64, ECDH P-256),"deviceName"}`.
- Response 200: `{"serverPublicKey","encryptedToken"}` →
  `PairingCrypto.DecryptToken`: gemeinsamen Schlüssel per
  `DeriveKeyFromHmac(serverPub, SHA256, null,null,null)` ableiten,
  AES-256-GCM-Layout `nonce(12)‖ct‖tag(16)` entschlüsseln →
  Geräte-Token (Klartext-String).
- Fehler-Mapping (`ExchangePairingCodeAsync`):
  401→`InvalidCode` ("ungültig, abgelaufen oder bereits verwendet"),
  429→`RateLimited`, 400→`InvalidRequest`→`PairingFailedError`,
  404→`NotSupported`, Transport→`ServerUnreachable`.
- Erfolg → `SetPairedTokenIfCurrentAsync` (SecureStorage,
  Profil+Adresse) → `AttemptAutoLoginEffect`.

## Auto-Login / Login

`AttemptAutoLoginEffect` → `AuthSessionService.TryStoredLoginAsync`:
gespeicherter AuthToken → Validierung via `GET api/favorites`;
sonst gespeicherte Credentials → `POST api/auth/login`; sonst
`MissingCredentials` → `NeedsLogin` + `CurrentPage=Login`.

`OnLoginClicked` → `AttemptLoginEffect` → `LoginAsync` →
`POST api/auth/login` mit Gate-Header `X-API-Key` (Geräte-Token) +
Body `{email,password}` → `AuthToken {token, expires}` →
`EnterConnected`: `Connected`, `IsSyncRunning`, `CurrentPage=Home` +
Effekte `LoadSourcesEffect`, `ConnectServerEventsEffect`,
`RunFullSyncEffect`, `ResumeDownloadsEffect`, `LoadHeroBackgroundEffect`.

Sync-Endpunkte nach Login (Mock muss sie bedienen oder 4xx liefern —
keine 5xx, sonst Retries): `GET api/Sources`, `GET api/items/recent`,
`GET api/continue-watching`, `GET api/favorites` (alle → `[]`
genügt). `hubs/mediaupdate` (SignalR) und `api/pictures/hero-background`
dürfen fehlschlagen (gefangene Exceptions).

## Serverwechsel (#15-Regressionspfad)

`SettingsPage` → `ServerSwitchButton` → `OnServerSwitchClicked` →
Navigation `serversetup` (Shell-Route) → `ServerSetupPage.OnAppearing`
→ `LoadServersRequestedMessage` (Liste aktualisieren). Auswahl eines
bekannten Servers → `ServerSwitchRequestedMessage` →
`BeginServerCheck` → gleicher Check→AutoLogin→`EnterConnected`→
`NavigateToPageEffect(Home)` (Rückkehr zur Startseite — der
#15-Bug ließ den Nutzer auf der Setup-Seite hängen).
