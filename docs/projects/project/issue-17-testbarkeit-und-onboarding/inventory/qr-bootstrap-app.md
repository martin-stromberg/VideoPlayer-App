# Detail: QR-Bootstrap — App-Anteil (Schritt 5)

## Vorhandene Maschinerie

| Baustein | Datei | Nutzung für Bootstrap |
|----------|-------|------------------------|
| ECDH/AES-GCM | `VideoPlayer.ApiClient/PairingCrypto.cs` (Z. 23–33, 44–99) | Unverändert: flüchtiges P-256-Paar, SPKI-Export, `nonce‖ct‖tag`-Entschlüsselung |
| Exchange-Client | `DevicePairingClient.cs` Z. 18–26, 60–122 | `POST api/pairing/exchange` ohne Auth-Header, `ApiException` mit Statuscode → neuer/analoger Bootstrap-Call |
| Code-Prüfung | `VideoPlayer.Mvu/PairingCode.cs` Z. 14/20/30 | Nur Admin-Code-Fallback; **nicht** auf `t=<ticket>` anwenden (opaker Wert) |
| Adress-Norm | `VideoPlayer.Storage/ServerAddress.cs` Z. 19–52 | QR-Host → kanonische Basisadresse (Achtung Unterpfad-Edge-Case) |
| Geräte-Token-Ablage | `AppSecretsService.cs` Z. 125–149 | `SetDeviceTokenAsync(serverAddress, token)` profil+adresspräfixiert |
| Session-Ablage | `AppSecretsService.cs` Z. 89–93 (`AuthToken`) + `AuthSessionService.cs` | `SetAuthTokenAsync(jwt)`; `TryStoredTokenAsync` validiert via `api/favorites` (Z. 189–230) |
| Gate-Aktivierung | `ShellAppEffectHandler.cs` Z. 1244–1249 + `DeviceTokenService.SetPairedTokenIfCurrentAsync` (`VideoPlayer.Mvu/DeviceTokenService.cs` Z. 110) | Provider-/Client-Aktivierung nach erfolgreichem Bootstrap identisch zum Pairing |
| MVU-Einordnung | `AppUpdate.cs` Z. 479–543; `ShellAppEffectHandler.cs` Z. 1213–1288 | Neuer Effekt analog `ExchangePairingCodeEffect`; Erfolg → `AttemptAutoLoginEffect` bzw. direkter Session-Einsatz → `EnterConnected` (Z. 681) |
| Fehlermapping | `ShellAppEffectHandler.cs` Z. 1266–1276 + `AppUpdate.cs` Z. 545–552 | `PairingStatus`-Enum erweiterbar bzw. eigener `BootstrapStatus` |

## Fehlende Bausteine (App)

- **Kamera/Scan**: kein Paket (csproj Z. 50–56); iOS braucht `NSCameraUsageDescription` in `Platforms/iOS/Info.plist`; Android `CAMERA` in `Platforms/Android/AndroidManifest.xml`; Windows `webcam`-Capability. Kandidat ZXing.Net.Maui (requirement Z. 74) — `CommunityToolkit.Maui`-Hauptpaket ist nicht referenziert (nur `MediaElement`).
- **QR-/Ticket-Parser**: `https://<server>/pairing?t=<ticket>` — Extraktion von Basisadresse + Ticket; Regeln für Unterpfad-Server festlegen.
- **Bootstrap-Response**: Geräte-Token **plus** Usersession (JWT) im selben AES-GCM-Payload — Serialisierung (z. B. JSON innerhalb des Ciphertexts) ist Vertragsfrage des Server-Lifecycles.
- **Session ohne Passwort**: `SilentReloginAsync` (`AuthSessionService.cs` Z. 303–351) erneuert nur mit gespeicherten Zugangsdaten — bei 401 ohne Credentials → `SessionExpired` → Login-Seite. Bis zur Refresh-Frage (requirement Z. 128) dokumentieren, dass die Bootstrap-Session nach 12 h endet.

## Fallbacks (bleiben erhalten)

- Admin-Code-Pairing (`PairingCodeEntry`-Pfad), manuelle Adresseingabe + Discovery (`AddressEntry`/`DiscoverButton`), E-Mail/Passwort (`LoginPage.xaml` Z. 20–33). Fehlerpfad „QR-Adresse nicht erreichbar" → `PairingStatus.ServerUnreachable`-Muster (`LoginUnreachableError`, `AppUpdate.cs` Z. 24).
