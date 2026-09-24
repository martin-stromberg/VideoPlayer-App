# Plan-Prüfung: iOS-Deployment-Skript reparieren (MSB1006)

**Status: Plan vollständig**

Geprüft gegen `requirement.md` (inkl. Akzeptanzkriterien aus
`project-plan.md` Schritt 1), `inventory.md` und Detaildokumente.

## Abdeckung der Akzeptanzkriterien

| Kriterium | Abgedeckt durch |
|-----------|-----------------|
| 1. MSB1006-Ursache identifiziert + dokumentiert | `inventory/msb1006-repro.md` (programmatische Reproduktion, dotnet SDK 10.0.401); Plan zitiert die Ergebnisse |
| 2. Gültige rsp oder verständlicher Abbruch | Plan-Schritt 1+2: `Assert-PropertyValue` bricht bei `"`, `#`, CR, LF mit klarer Meldung ab; Trimmen; Werte ohne Verstoss bleiben quoted — Kriterium ist ein Oder, Abbruch-Variante gewählt und begründet |
| 3. Automatisierter Test pinnt Sanitizing | Plan-Schritt 4: Ausführungstests per `pwsh` (real ausgeführte Logik, ueber die reine Quelltext-Pin-Erwartung der Anforderung hinaus) plus Quelltext-Pins |
| 4. Secrets maskiert | Zentrale `MaskedPropertyNames`-Liste; maskierte Ausgabe beim Sanitizing-Abbruch und an allen drei dotnet-Fehlerpfaden; Ausführungstest assertiert explizit, dass der Secret-Wert nicht in der Ausgabe steht |
| 5. Geräte-Restnachweis | Im Plan als manueller Nachweis ausgewiesen (kein Mac verfuegbar) |

## Konsistenz mit der Inventur

- Alle drei Einspeisestellen (Z. 725, 884, 976) und alle drei
  Fehlerpfade (Z. 774, 945, 990) sind im Plan benannt — inklusive
  `Invoke-DeviceViaSsh`, das bisher keine Display-Liste hat
  (Inventur-Befund, im Plan erfasst).
- `New-ResponseFile`-Aufrufstellen (Z. 763, 934, 985) stimmen mit der
  Inventur ueberein.
- Bestehender Test `PairingContractTests.IosDeploymentScript_PropagatesAndMasksApiToken`
  wuerde durch die Extraktion brechen — im Plan als eigener
  Umsetzungsschritt (Punkt 3) beruecksichtigt.
- Kein Eingriff in uncommitted Fremdaenderungen (`csproj`
  ApplicationVersion, `.ios-deploy.user.json`) — ausserhalb des
  Schritt-Umfangs.

## Luecken/Risiken

- pwsh-Verfuegbarkeit auf anderen Testmaschinen: abgefedert durch
  Skip-Logik; Quelltext-Pins bleiben als Mindestabdeckung.
- Keine fachlichen Luecken festgestellt.

**Ergebnis: Plan vollständig — Umsetzung freigegeben.**
