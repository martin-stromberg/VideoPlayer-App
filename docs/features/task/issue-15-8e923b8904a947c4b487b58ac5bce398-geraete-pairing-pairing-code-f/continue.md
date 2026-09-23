# Offene Aufgaben

Erstellt am: 2026-03-08
Aktualisiert am: 2026-09-23 (dritter Fortsetzungslauf)
Abbruchgrund: Verbleibender Punkt ist in dieser Umgebung nicht ausführbar (manueller Gerätetest auf iOS/Android; kein Gerät/Emulator verfügbar, iOS-Deploy-Skript blockiert durch MSB1006)

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

- [ ] Tasks-Datei Nr. 42 - Manueller Pairing-Lauf auf echtem Zielgerät — **auf Windows vollständig durchgespielt** (siehe `live-verification.md`, E2E-Nachtrag): echte `VideoPlayer.Maui.exe` per UI-Automation durch Start → Gate-Ablehnung → Pairing-Abschnitt → Code-Eingabe → Pairing (SecureStorage-Ablage, `DeviceInfo`-Name „DESKTOP-CM8OBSG" serverseitig registriert) → Anmeldung → Startseite; anschließend auch der Serverwechsel-Pfad (Einstellungen → „Server wechseln" → bekannten Server wählen → Auto-Login mit Geräte-Token → Startseite) live verifiziert — inklusive zweier gefundener und behobener Fehler (`UpdateServersLoaded`-Pairing-Reset, `AppRoutes.TryGetPage`-Auflösung geschobener Routen).

  **Ohne Gerät abgesichert (dritter Fortsetzungslauf, 2026-09-23):** Vier neue Contract-Tests in `PairingContractTests` pinnen die verbleibende Unsicherheit so weit wie ohne physisches Gerät möglich:
  - `SecureStorageSecretStore_UsesSharedPlatformAbstraction` — ein einziger Codepfad ohne bedingte Kompilierung (`#if`/`#elif`/`#else`/`#endif`) auf der MAUI-Abstraktion `ISecureStorage`; der auf Windows verifizierte Pfad ist bis auf das OS-Backend identisch. Backend-Fehler lesen als „leer" (deckt u. a. den Android-Fall „aus Cloud-Backup wiederhergestellt, Keystore-Schlüssel fehlt" ab → erneutes Pairing statt Absturz).
  - `MauiProgram_BindsSecretStoreToSecureStorageDefault` — `ISecretStore` ist an `new SecureStorageSecretStore(SecureStorage.Default)` gebunden (iOS: Keychain, Android: Keystore, Windows: Credential Manager); es gibt keine zweite Implementierung.
  - `IosEntitlements_GrantKeychainAccess` — `Platforms/iOS/Entitlements.plist` (MAUI-Standardort, wird automatisch als `CodesignEntitlements` übernommen) enthält `keychain-access-groups`; keine zweite `Entitlements*.plist` kann den Standard verdrängen; eine explizite `CodesignEntitlements`-Einstellung im csproj muss auf dieselbe Datei zeigen. Ohne diese Entitlement schlägt `SecureStorage` auf einem physischen iOS-Gerät fehl — der häufigste reale iOS-Fehler ist damit ausgeschlossen.
  - `DeviceTokenService_PersistsViaSecretsService` — Geräte-Token-Persistenz läuft ausschließlich über `AppSecretsService` (→ `ISecretStore` → `SecureStorage`), kein Umgehungsweg.

  **Verbleibender Restnachweis (echtes Gerät erforderlich):** Die OS-Backends selbst — iOS-Keychain und Android-Keystore unterhalb von `SecureStorage` — sowie `DeviceInfo`-Gerätename und UI-Bedienung auf den Zielplattformen können nur auf physischen Geräten verifiziert werden. Konkret auszuführen nach Fix von `scripts/iOS-Deployment.ps1` (MSB1006, als separates Projekt geplant): Pairing-Lauf auf einem iOS- und einem Android-Gerät (Code-Eingabe → Koppeln → Neustart der App → Auto-Login mit gespeichertem Geräte-Token; zusätzlich iOS: Zugriff auf die Keychain nach erneutem Installieren/Neustart). In dieser Umgebung nicht ausführbar — kein iOS-/Android-Gerät oder Emulator verfügbar.

## Code-Review-Befunde

- [x] `ShellAppEffectHandler.cs` - `CheckCandidateServerAsync`: Bei `GateRejected` wird das Geräte-Token der geprüften Adresse jetzt gezielt über `secrets.ClearDeviceTokenAsync(address)` gelöscht (Muster `HandleGateRejectionAsync`), bevor `IsPairingRequiredAsync` neu bestimmt wird — erledigt.
- [x] `DeviceTokenService.cs` - `DiscardIfCurrentAsync`: Adressvergleich und Provider-Rücksetzung laufen jetzt atomar unter `providerLock`; Store-Löschung nach dem Release; `ArgumentException.ThrowIfNullOrWhiteSpace` ergänzt — erledigt.
- [x] `DeviceTokenService.cs` - `SetPairedTokenAsync`: entfernt (toter Code ohne Produktivaufrufer); Test-Aufrufstellen über `PairAsync`/`PairDeviceAsync`-Hilfsfunktionen (`SetDeviceTokenAsync` + `LoadForServerAsync`) umgestellt — erledigt.

## Usability-Befunde

- [x] `ServerSetupPage.xaml` - `PairingCodeEntry`: `MaxLength="32"` entfernt — längere serverseitig konfigurierte Codes werden nicht mehr still abgeschnitten; Vertragstest prüft die Abwesenheit des Attributs — erledigt.

## Fehlgeschlagene Tests

Keine. (873/873 bestanden)
