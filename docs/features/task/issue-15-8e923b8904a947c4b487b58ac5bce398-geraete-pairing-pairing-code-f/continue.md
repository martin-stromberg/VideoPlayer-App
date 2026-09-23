# Offene Aufgaben

Erstellt am: 2026-03-08
Aktualisiert am: 2026-03-08 (zweiter Fortsetzungslauf)
Abbruchgrund: Verbleibender Punkt ist in dieser Umgebung nicht ausführbar (manueller Gerätetest)

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

- [ ] Tasks-Datei Nr. 42 - Manueller Pairing-Lauf auf echtem Zielgerät — **auf Windows vollständig durchgespielt** (siehe `live-verification.md`, E2E-Nachtrag): echte `VideoPlayer.Maui.exe` per UI-Automation durch Start → Gate-Ablehnung → Pairing-Abschnitt → Code-Eingabe → Pairing (SecureStorage-Ablage, `DeviceInfo`-Name „DESKTOP-CM8OBSG" serverseitig registriert) → Anmeldung → Startseite; anschließend auch der Serverwechsel-Pfad (Einstellungen → „Server wechseln" → bekannten Server wählen → Auto-Login mit Geräte-Token → Startseite) live verifiziert — inklusive zweier gefundener und behobener Fehler (`UpdateServersLoaded`-Pairing-Reset, `AppRoutes.TryGetPage`-Auflösung geschobener Routen). Offen bleiben nur die plattformspezifischen Secure-Storage-Backends auf **iOS (Keychain) und Android (Keystore)** — dort ist der Codepfad identisch (`SecureStorage`-Abstraktion), aber nicht ausgeführt worden.

## Code-Review-Befunde

- [x] `ShellAppEffectHandler.cs` - `CheckCandidateServerAsync`: Bei `GateRejected` wird das Geräte-Token der geprüften Adresse jetzt gezielt über `secrets.ClearDeviceTokenAsync(address)` gelöscht (Muster `HandleGateRejectionAsync`), bevor `IsPairingRequiredAsync` neu bestimmt wird — erledigt.
- [x] `DeviceTokenService.cs` - `DiscardIfCurrentAsync`: Adressvergleich und Provider-Rücksetzung laufen jetzt atomar unter `providerLock`; Store-Löschung nach dem Release; `ArgumentException.ThrowIfNullOrWhiteSpace` ergänzt — erledigt.
- [x] `DeviceTokenService.cs` - `SetPairedTokenAsync`: entfernt (toter Code ohne Produktivaufrufer); Test-Aufrufstellen über `PairAsync`/`PairDeviceAsync`-Hilfsfunktionen (`SetDeviceTokenAsync` + `LoadForServerAsync`) umgestellt — erledigt.

## Usability-Befunde

- [x] `ServerSetupPage.xaml` - `PairingCodeEntry`: `MaxLength="32"` entfernt — längere serverseitig konfigurierte Codes werden nicht mehr still abgeschnitten; Vertragstest prüft die Abwesenheit des Attributs — erledigt.

## Fehlgeschlagene Tests

Keine. (873/873 bestanden)
