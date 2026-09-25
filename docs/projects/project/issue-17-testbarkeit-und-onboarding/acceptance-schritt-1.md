# Abnahme Schritt 1 — iOS-Deployment-Skript reparieren (MSB1006)

Schrittbranch: `task/issue-17-ios-deploy-fix` → gemergt in `5cc6d46`.
Lifecycle: vollständig durchlaufen (`docs/features/task/issue-17-ios-deploy-fix/` — Artefakte im Planungscommit `76eb670`, Feature-Verzeichnis danach gemäß Konvention gelöscht).

## Ergebnis

- **Ursache belegt (programmatisch reproduziert, dotnet SDK 10.0.401):** MSB1006 entsteht, wenn MSBuild aus der rsp-Datei ein `-p:`-Argument ohne gültigen Eigenschaftsnamen liest — erzeugbar durch `"` im Wert (auch MSB4177) und Zeilenumbruch (MSB1008/MSB1006). `#` kürzt die Zeile still, Whitespace/Länge/`=`/`;` sind harmlos. `Add-PropertyLine` schrieb Werte bisher ungeprüft.
- **Fix:** Neue testbare Hilfsdatei `scripts/iOS-Deployment.ResponseFile.ps1` (per Dot-Sourcing eingebunden): `Assert-PropertyValue` bricht bei `"`, `#`, CR, LF mit deutscher Meldung ab (Eigenschaft, Zeichenart, Position, maskierte Zeile); `Add-PropertyLine` trimmt und prüft; `New-ResponseFile` schreibt explizit UTF-8 ohne BOM; maskierte Response-Ausgabe bei dotnet-Fehlschlag an allen drei Einspeisestellen (`Invoke-DeviceViaSsh` hatte zuvor keine Anzeige).
- **Tests:** 14 neue Pins/Ausführungstests (`IosDeploymentResponseFileTests.cs` — 5 Quelltext-Pins + 9 pwsh-Tests mit Skip-Logik); `PairingContractTests`-Maskier-Pin auf Helper-Datei umgezogen. **896/896 Tests erfolgreich**, 0 fehlgeschlagen, 0 übersprungen.

## Akzeptanzkriterien (project-plan.md)

1. ✅ MSB1006-Ursache identifiziert und dokumentiert (`"`/Zeilenumbruch im Wert → ungültiges `-p:`-Argument).
2. ✅ Sanitizing-sichere `Add-PropertyLine`/`New-ResponseFile` mit verständlichem Abbruch statt kryptischem MSB1006.
3. ✅ Automatisierte Tests pinnen Sanitizing-Regeln (pwsh-Ausführung + Quelltext-Pins).
4. ✅ Secrets bleiben maskiert (`MaskedPropertyNames`, `Get-MaskedPropertyLine`, `Write-MaskedPropertyLines`).
5. ⚠️ Geräte-Restnachweis aus Issue #15 bleibt manueller Nachweis (kein Mac/iOS-Gerät in dieser Umgebung) — Verifikationsanleitung im Feature-`test-results.md` (Historie, Commit `76eb670`).

## Commits

- `76eb670` plan: iOS-Deployment-Skript MSB1006-Fix planen
- `32bfd5c` fix: iOS-Deployment bricht bei ungueltigen Response-Datei-Werten verstaendlich ab statt MSB1006
- `5cc6d46` merge: Schritt 1 in Projektbranch
