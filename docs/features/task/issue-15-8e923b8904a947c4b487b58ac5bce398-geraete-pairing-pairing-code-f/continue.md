# Offene Aufgaben

Erstellt am: 2026-03-08
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

- [ ] Tasks-Datei Nr. 42 — Manueller Pairing-Lauf auf echtem Zielgerät: `SecureStorage`-Persistenz, `DeviceInfo`-Gerätename und UI-Bedienung auf physischem Gerät (iOS/Android/Windows) verifizieren. Teilnachweis vorhanden: Krypto-/API-Vertrag wurde mit dem produktiven `DevicePairingClient` gegen den IIS-Testserver live verifiziert (`live-verification.md`); die gerätegebundenen Teile erfordern ein physisches Gerät und sind in dieser Umgebung nicht ausführbar.

## Code-Review-Befunde

- [ ] `ShellAppEffectHandler.cs` — `CheckCandidateServerAsync` (ca. Z. 1074–1086): Bei `HealthCheckResult.GateRejected` wird das nachweislich tote Geräte-Token des Kandidaten nur über `DiscardIfCurrentAsync(address)` gelöscht — also nur, wenn der Kandidat der aktive Server ist. Für nicht-aktive Kandidaten bleibt das tote Token bestehen → `IsPairingRequiredAsync` liefert `false` → aussichtsloser Auto-Login. Empfehlung: Bei `GateRejected` zusätzlich `secrets.ClearDeviceTokenAsync(address)` (Muster `HandleGateRejectionAsync`), danach `IsPairingRequiredAsync` neu bestimmen. (gering–mittel)
- [ ] `DeviceTokenService.cs` — `DiscardIfCurrentAsync` (ca. Z. 202–211): Check-then-act — die Adressprüfung liest `CurrentServerAddress` außerhalb von `providerLock`; ein dazwischengeschobenes `LoadForServerAsync(B)` lässt die A-Ablehnung Bs Provider zurücksetzen und Bs gültiges Token löschen. Empfehlung: Vergleich + Provider-Rücksetzung atomar unter `providerLock`; Store-Löschung nach Release; zusätzlich `ThrowIfNullOrWhiteSpace`. (gering)
- [ ] `DeviceTokenService.cs` — `SetPairedTokenAsync` (ca. Z. 99–115): Toter Code — kein Produktivaufrufer (nur Tests); setzt zudem `CurrentServerAddress` ohne Stale-Prüfung. Empfehlung: entfernen oder Zweck als Test-/Zwangspfad dokumentieren. (gering)

## Usability-Befunde

- [ ] `ServerSetupPage.xaml` (Abschnitt „Gerät koppeln"): `PairingCodeEntry` hat `MaxLength="32"` und schneidet längere Eingaben still ab. Die Codelänge ist serverseitig konfigurierbar (`Pairing:CodeLength`, min. 4, kein dokumentiertes Maximum). Bei Servercodes > 32 Zeichen schlägt das Pairing trotz korrekter Eingabe mit irreführender 401-Meldung fehl. Empfehlung: Limit entfernen/deutlich erhöhen oder Überlänge mit Hinweis statt stillem Abschneiden behandeln. (Edge-Case, niedrige Wahrscheinlichkeit)

## Fehlgeschlagene Tests

Keine. (874/874 bestanden)
