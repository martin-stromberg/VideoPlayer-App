# Testergebnisse - Iteration 4 (Fortsetzung nach `continue.md`)

Datum: Nach vierter Implementierungsrunde (Befunde aus `review-code.3.md` und `review-usability.3.md` abgearbeitet).

## Ausführung

- Befehl: `dotnet test VideoWebPlayer.Maui.Tests -c Release`
- Ergebnis: **873/873 bestanden, 0 Fehler, 0 übersprungen** (Dauer: ~1 min)

## Bewertung

- Alle Tests laufen grün. Gegenüber der letzten Iteration ein Test weniger (874 → 873): `DeviceTokenServiceTests.SetPairedToken_StoresAndActivatesToken` entfiel mit der entfernten Methode `SetPairedTokenAsync` (toter Code ohne Produktivaufrufer); die Aktivierungsfunktionalität bleibt über `SetPairedTokenIfCurrent_CurrentServer_StoresAndActivates` abgedeckt.
- Geänderte Test-Vorrichtungen: `PairAsync`/`PairDeviceAsync` (`SetDeviceTokenAsync` + `LoadForServerAsync`) ersetzen die entfernte `SetPairedTokenAsync`; beide Klassen laufen in `[Collection("ClientGate")]`.
- `PairingContractTests.ServerSetupPage_ContainsPairingSection` prüft jetzt die Abwesenheit von `MaxLength` am `PairingCodeEntry`.
- Keine neuen Fehlschläge gegenüber der Ausgangsbasis.
- Hinweis: Echter Gerätetest (SecureStorage/DeviceInfo/UI auf physischem Gerät) bleibt gemäß Plan Task 42 manuell auszuführen — in dieser Umgebung nicht ausführbar; der API-/Krypto-Pfad wurde gegen den lokalen IIS-Testserver mit dem produktiven `DevicePairingClient` verifiziert (siehe `live-verification.md`).

## Fehlgeschlagene Tests

Keine.

# Testergebnisse - Nacharbeiten (Bugfixes aus der E2E-Verifikation)

Datum: 2026-09-23 (nach den Live-E2E-Läufen in der Windows-App)

## Ausführung

- Befehl: `dotnet test VideoWebPlayer.Maui.Tests` (Debug)
- Ergebnis: **879/879 bestanden, 0 Fehler, 0 übersprungen**

## Bewertung

- Nach dem `UpdateServersLoaded`-Fix (Re-Pairing-Sichtbarkeit): 875/875.
- Nach dem `AppRoutes.TryGetPage`-Fix (geschobene Routen): **879/879** — vier neue `InlineData`-Fälle in `AppRoutesTests.TryGetPage_ResolvesShellLocations` (`//settings/serversetup`, `//home/serversetup`, `//settings/login`, `//favorites/FavoritesPage`).
- Keine neuen Fehlschläge gegenüber der Ausgangsbasis.

# Testergebnisse - Dritter Fortsetzungslauf (continue.md, Task 42)

Datum: 2026-09-23

## Ausführung

- Befehl: `dotnet test VideoWebPlayer.Maui.Tests -c Release`
- Ergebnis: **883/883 bestanden, 0 Fehler, 0 übersprungen** (Dauer: ~11 s)

## Bewertung

- Vier neue Contract-Tests in `PairingContractTests` sichern den noch offenen Task-42-Rest (plattformspezifische Secure-Storage-Backends) so weit ab, wie es ohne physisches iOS-/Android-Gerät möglich ist:
  - `SecureStorageSecretStore_UsesSharedPlatformAbstraction` — identischer, verzweigungsfreier Codepfad auf `ISecureStorage` für alle Plattformen; Backend-Fehler lesen als „leer".
  - `MauiProgram_BindsSecretStoreToSecureStorageDefault` — `ISecretStore` an `SecureStorage.Default` gebunden.
  - `IosEntitlements_GrantKeychainAccess` — `keychain-access-groups` in `Platforms/iOS/Entitlements.plist` (MAUI-Standardort, automatische `CodesignEntitlements`-Übernahme); keine verdrängende Zweitdatei; explizite csproj-Einstellung müsste auf dieselbe Datei zeigen.
  - `DeviceTokenService_PersistsViaSecretsService` — Geräte-Token ausschließlich über `AppSecretsService`/`ISecretStore`.
- Nicht abgedeckt (nur auf physischem Gerät prüfbar): tatsächliche Keychain-/Keystore-Laufzeit, `DeviceInfo`-Name und UI-Bedienung auf iOS/Android — siehe `continue.md` (iOS-Deploy blockiert durch MSB1006 in `scripts/iOS-Deployment.ps1`).

## Fehlgeschlagene Tests

Keine.
