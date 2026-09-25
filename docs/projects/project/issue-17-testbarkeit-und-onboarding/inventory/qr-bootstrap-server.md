# Detail: QR-Bootstrap — Server-Koordination (Schritt 5, nur Inventur)

Repo: `487c3041-…` (ASP.NET Core 10 + EF Core SQLite + Blazor InteractiveServer; `VideoWebPlayer.csproj` Z. 1–54).

## Pairing-Stack heute

- `Data/PairingCode.cs` Z. 6–32: `Id`, `CodeHash` (SHA-256), `CreatedAtUtc`, `ExpiresAtUtc`, `ConsumedAtUtc`, **`CreatedByUserId`** — benutzergebundene Tickets damit grundsätzlich abbildbar; für die TV-Gegenrichtung fehlt ein Richtungs-/Rollenfeld.
- `Services/PairingService.cs`:
  - `CreatePairingCodeAsync(createdByUserId)` Z. 154–180 — Alphabet `ABCDEFGHJKMNPQRSTUVWXYZ23456789` (Z. 134), Länge Default 8, TTL Default 5 min (`Pairing:CodeLength`/`Pairing:CodeTtlMinutes`, Z. 160–161).
  - `ExchangeAsync` Z. 182–233 — atomarer Verbrauch via `ExecuteUpdateAsync` (Z. 217–219), Client-Key-Import mit P-256-Prüfung (Z. 235–258), `DeriveKeyFromHmac`+AES-256-GCM (Z. 225–276), `IDeviceTokenService.IssueAsync(deviceName, pairingCode.CreatedByUserId)` (Z. 227).
  - `GetActiveCodesAsync` Z. 278–292 (Meta ohne Code).
- `Controllers/PairingController.cs` Z. 42–65: `POST api/pairing/exchange`, **öffentlich**; `ILoginIpBlockService` liefert 429 bei ≥5 Fehlschlägen (`LoginIpBlockService.cs` Z. 61, 117–141, Persistenz `BlockedLoginIps` Z. 152–164).
- DTOs `VideoWebPlayer.Client/Models/PairingExchange{Request,Response}.cs` — separate Kopie desselben Vertrags existiert appseitig in `VideoPlayer.ApiClient/Models/` → beide Repos müssen bei Vertragsänderung synchronisiert werden.

## Session-/Auth-Stack

- `AuthorizationTokenService.CreateToken` (`Services/Authentication/IAuthService.cs` Z. 59–87): Claims `Sub`/`NameIdentifier`=UserId, `Email`, `Name`, `IsAdmin`; Issuer `Jwt:Issuer`; **12 h, kein Refresh-Token** — Bootstrap-Session = dieser JWT; Erneuerungsfrage ist serverseitig zu klären.
- `AuthService.LoginAsync` (Z. 163–173) via `UserManager`+`CheckPasswordSignInAsync`; `ImpersonateAsync` (Z. 180–187) zeigt das Muster „Token ohne Passwort ausstellen" (admin-geprüft via `CheckAdmin` → `CurrentUser.IsAdmin`).
- `AuthController` (`Controllers/AuthController.cs`): `POST api/auth/login` `[ApiTokenCheck(MauiOnly)]` (Z. 41–43); `api/auth/impersonate` `[ApiTokenCheck]+[BearerTokenCheck]` (Z. 61–74).
- `ApiTokenCheckAttribute`: Header `X-API-Key` (Z. 12); Scope `MauiOnly` akzeptiert `Jwt:ApiToken:Maui` + Device-Tokens via `IDeviceTokenService.IsValidDeviceTokenAsync` (Z. 68–91). **Ein Bootstrap-Endpunkt muss wie `exchange` öffentlich bleiben** (Gerät hat noch keinen Gate-Schlüssel).
- `BearerTokenCheckAttribute` (Z. 45–54): Issuer/Audience/Lifetime/Signatur.
- `DeviceTokenService` (`Services/DeviceTokenService.cs`): `IssueAsync` → Base64url(32B), SHA-256-Hash in `PairedDevices` inkl. `CreatedByUserId` (Z. 63–83); Revoke/Rename/GetDevices (Z. 101–133).

## UI-/Rechte-Stack

- Admin-Claim: `ApplicationUser.IsAdmin` (`Data/ApplicationUser.cs` Z. 17) → `IsAdmin`-Claim; Blazor-Prüfung `user.HasClaim("IsAdmin","True")` (`Devices.razor` Z. 197, `NavMenu.razor` Z. 57/124).
- Code-Erzeugung heute **nur** adminseitig: `Components/Pages/Admin/Devices.razor` (`/admin/devices`, `CreatePairingCodeAsync(currentUserId)` Z. 242, Anzeige mit `data-testid` Z. 54–58, Geräteverwaltung Revoke/Rename Z. 94–174).
- Benutzer-Profilseite mit Geräteabschnitt existiert nicht — `Account/Manage`-Gerüst (`Components/Account/Pages/Manage/*`) ist der nächstliegende Ort; `NavMenu` verlinkt `Account/Manage` (Z. 70–72).
- Rate-Limit je Benutzer für Ticket-Erzeugung existiert nicht (IP-basiert nur `LoginIpBlockService`) — serverseitig neu (Muster: ConcurrentDictionary+DB-Persistenz vorhanden).
- Kein QR-Paket im Server-csproj (Z. 18–38) — Erzeugung (QRCoder o. ä. bzw. clientseitig via JS) ist Teil des Server-Lifecycles.

## Koordinationspunkte für den Server-Lifecycle

1. Ticket-Modell: `PairingCode`-Tabelle erweitern (z. B. `Kind`/`Purpose`, ggf. längeres Ticket-Secret statt Kurz-Code) — beide Richtungen (App-scannt-QR + Gerät-zeigt-Code/TV) vorsehen.
2. Bootstrap-Endpunkt: neu `api/pairing/bootstrap` vs. `exchange` erweitern; Response-Payload verschlüsselt: Geräte-Token + JWT (+ evtl. `expires`).
3. Profilseite „Geräte" → „Gerät koppeln": QR `https://<server>/pairing?t=<ticket>` + 8-Zeichen-Fallback; gerenderte Adresse = `Scheme+Host` des Requests (requirement Z. 129).
4. Ticket-Schutz: TTL ~5 min, Single-Use (atomarer Verbrauch via `ExecuteUpdateAsync`-Muster), Rate-Limit je User, optional „nur Admins"/Re-Auth.
5. Test-Pins vorhanden: `VideoWebPlayer.Tests/PairingExchangeContractTests_*`, `PairingServiceTests_*`, `DevicePairingE2ETests.cs`, `LoginIpBlockServiceTests.cs`, `Helpers/PairingWebApplicationFactory.cs` — E2E-Muster mit `WebApplicationFactory` bereits etabliert.
