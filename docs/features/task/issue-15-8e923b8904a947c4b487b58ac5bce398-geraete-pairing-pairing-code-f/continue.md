# Offene Aufgaben

Erstellt am: 2026-03-08
Aktualisiert am: 2026-03-08 (zweiter Fortsetzungslauf)
Abbruchgrund: Verbleibender Punkt ist in dieser Umgebung nicht ausführbar (manueller Gerätetest)

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

- [ ] Tasks-Datei Nr. 42 - Manueller Pairing-Lauf auf echtem Zielgerät: `SecureStorage`-Persistenz, `DeviceInfo`-Gerätename und UI-Bedienung auf physischem Gerät (iOS/Android/Windows) verifizieren. Teilnachweis vorhanden: Krypto-/API-Vertrag wurde mit dem produktiven `DevicePairingClient` gegen den IIS-Testserver live verifiziert (`live-verification.md`); die gerätegebundenen Teile erfordern ein physisches Gerät und sind in dieser Umgebung nicht ausführbar.

## Code-Review-Befunde

- [x] `ShellAppEffectHandler.cs` - `CheckCandidateServerAsync`: Bei `GateRejected` wird das Geräte-Token der geprüften Adresse jetzt gezielt über `secrets.ClearDeviceTokenAsync(address)` gelöscht (Muster `HandleGateRejectionAsync`), bevor `IsPairingRequiredAsync` neu bestimmt wird — erledigt.
- [x] `DeviceTokenService.cs` - `DiscardIfCurrentAsync`: Adressvergleich und Provider-Rücksetzung laufen jetzt atomar unter `providerLock`; Store-Löschung nach dem Release; `ArgumentException.ThrowIfNullOrWhiteSpace` ergänzt — erledigt.
- [x] `DeviceTokenService.cs` - `SetPairedTokenAsync`: entfernt (toter Code ohne Produktivaufrufer); Test-Aufrufstellen über `PairAsync`/`PairDeviceAsync`-Hilfsfunktionen (`SetDeviceTokenAsync` + `LoadForServerAsync`) umgestellt — erledigt.

## Usability-Befunde

- [x] `ServerSetupPage.xaml` - `PairingCodeEntry`: `MaxLength="32"` entfernt — längere serverseitig konfigurierte Codes werden nicht mehr still abgeschnitten; Vertragstest prüft die Abwesenheit des Attributs — erledigt.

## Fehlgeschlagene Tests

Keine. (873/873 bestanden)
