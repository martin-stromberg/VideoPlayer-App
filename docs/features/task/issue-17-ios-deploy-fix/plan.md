# Umsetzungsplan: iOS-Deployment-Skript reparieren (MSB1006)

## Übersicht

`scripts/iOS-Deployment.ps1` erzeugt MSBuild-Response-Dateien ungeprueft:
`Add-PropertyLine` schreibt `-p:Name="Wert"` ohne Trimmen und ohne
Zeichenpruefung. Die Reproduktion (`inventory/msb1006-repro.md`) zeigt:
`"` im Wert (Position-abhaengig) und Zeilenumbrueche erzeugen
`MSB1006`/`MSB4177`/`MSB1008`. Der Fix sanitisiert die Werte, bricht bei
unzulaessigen Zeichen mit verstaendlicher Meldung ab (inkl. maskierter
Response-Zeile) und gibt die Response-Zeilen beim Build-Fehlschlag
maskiert aus. Die Sanitizing-Logik wird in eine eigenstaendige, per
Dot-Sourcing eingebundene Datei extrahiert, damit Tests sie real
ausfuehren koennen.

## Designentscheidungen

| Komponente / Bereich | Gewaehlter Ansatz | Begruendung |
|----------------------|-----------------|-------------|
| Sanitizing-Ort | Neue Datei `scripts/iOS-Deployment.ResponseFile.ps1`, per Dot-Sourcing im Hauptskript eingebunden | Die Funktionen lassen sich so isoliert per `pwsh` dot-sourcen und **ausfuehrbar** testen — besser als reine Quelltext-Pins; Hauptskript bleibt der einzige Produktiv-Einstiegspunkt |
| Verhalten bei unzulaessigen Zeichen | Kontrollierter Abbruch (`Write-Host` Fehlermeldung + `exit 1`) statt Escaping | Ein `"` oder Zeilenumbruch in einem Token ist sicher ein Eingabefehler (z. B. mitkopierte Quotes); stilles Wegescapen wuerde einen falschen Token in den Build einschleusen. Deckt Akzeptanzkriterium 2 („verstaendlicher Abbruch statt MSB1006") direkt ab |
| Verbotene Zeichen | `"`, `#`, CR, LF; Wert wird vorher getrimmt | Exakt die Zeichenklasse, die den rsp-Parser bricht bzw. still kuerzt (Beleg: `inventory/msb1006-repro.md`); Semikolon, `=`, Leerzeichen, Laenge sind harmlos und bleiben erlaubt |
| Maskierung | Zentrale Liste `$script:MaskedPropertyNames` (`ServerPassword`, `VideoPlayerApiToken`) + `Get-MaskedPropertyLine`/`Write-MaskedPropertyLines` | Bisher war die Maskierliste in `Add-MaskedDisplay` verdrahtet; fuer die Fehlerausgabe an drei Stellen wird sie zentral gebraucht — keine zweite Liste, die auseinanderlaufen koennte |
| Fehlschlag-Transparenz | Bei `LASTEXITCODE -ne 0` nach `dotnet …` werden die Response-Zeilen maskiert ausgegeben (alle drei Aufrufstellen, inkl. `Invoke-DeviceViaSsh`, das bisher gar keine Anzeige hatte) | Akzeptanzkriterium 4 und Fehlschlag-Transparenz aus der Anforderung |
| `New-ResponseFile` | Parametrisiertes `-Directory` + explizites UTF-8-ohne-BOM-Encoding | Encoding explizit statt Laufzeit-Default; `-Directory` statt Skript-Global `$repoRoot`, damit die Funktion testbar bleibt |

## Programmablaeufe

### Eigenschaftszeile erzeugen (Add-PropertyLine)

1. Leerer Wert → Rueckkehr ohne Zeile (bisheriges Verhalten bleibt).
2. Wert trimmen; nur noch Whitespace → Rueckkehr ohne Zeile.
3. `Assert-PropertyValue` prueft auf `"`, `#`, CR, LF.
4. Verstoss → Fehlermeldung mit Eigenschaftsname, Zeichenart und
   Position; die betroffene Zeile wird **maskiert** ausgegeben
   (`Get-MaskedPropertyLine`); `exit 1`.
5. OK → `$List.Add("-p:$Name=`"$Value`"")`.

### Fehlerausgabe nach dotnet-Aufruf (alle drei Stellen)

1. `dotnet … @rsp` liefert `LASTEXITCODE -ne 0`.
2. Zusaetzlich zur bisherigen Fehlermeldung:
   `Write-MaskedPropertyLines -Lines $rspLines` — jede Zeile wird
   ausgegeben, bei `ServerPassword`/`VideoPlayerApiToken` als
   `-p:Name=***`.
3. `exit 1` wie bisher; `finally` loescht die rsp-Datei weiterhin.

## Umsetzungsschritte

1. `scripts/iOS-Deployment.ResponseFile.ps1` neu anlegen:
   - `$script:MaskedPropertyNames`
   - `Get-MaskedPropertyLine` (einzelne Zeile maskiert fuer Anzeige)
   - `Write-MaskedPropertyLines` (Liste maskiert ausgeben)
   - `Assert-PropertyValue` (Zeichenpruefung + Abbruch)
   - `Add-PropertyLine` (verschoben + Trim + Assert)
   - `Add-MaskedDisplay` (verschoben; nutzt MaskedPropertyNames +
     Assert fuer unmaskierte Werte)
   - `New-ResponseFile` (verschoben; `-Directory`-Parameter,
     UTF-8 ohne BOM explizit)
2. `scripts/iOS-Deployment.ps1` anpassen:
   - Dot-Sourcing des Helpers direkt nach dem `param`-Block.
   - Die drei alten Funktionsdefinitionen (Z. 685–716) entfernen.
   - Drei `New-ResponseFile`-Aufrufe um `-Directory $repoRoot`
     ergaenzen (Z. 763, 934, 985).
   - Drei Fehlerpfade um maskierte Zeilenausgabe ergaenzen
     (Z. 774, 945, 990).
3. `VideoWebPlayer.Maui.Tests/PairingContractTests.cs` anpassen:
   `IosDeploymentScript_PropagatesAndMasksApiToken` pruefte das
   Maskier-Literal `$Name -eq "VideoPlayerApiToken"` im Hauptskript —
   mit der Extraktion wandert es in die Helper-Datei; der Test liest
   kuenftig beide Dateien.
4. `VideoWebPlayer.Maui.Tests/IosDeploymentResponseFileTests.cs` neu:
   - Quelltext-Pins: Hauptskript bindet den Helper per Dot-Sourcing
     ein; der Token wird an allen drei Einspeisestellen ueber
     `Add-PropertyLine` (rsp) und `Add-MaskedDisplay` (Anzeige)
     gefuehrt; Fehlerpfade rufen `Write-MaskedPropertyLines`.
   - Ausfuehrungstests per `pwsh -NoProfile -File <temp.ps1>`
     (Helper wird dot-gesourct): Trim+Quote-Verhalten, Abbruch bei
     `"`, `#`, CR, LF (Exit-Code 1, Meldung nennt Eigenschaftsnamen,
     enthaelt **nicht** den Secret-Wert), Maskierung per
     `Get-MaskedPropertyLine`, `New-ResponseFile` schreibt UTF-8 ohne
     BOM. Tests werden uebersprungen, wenn kein `pwsh` verfuegbar ist.

## Auswirkungen auf Bestehendes

- Skript-Aufrufkonvention unveraendert (Parameter, Umgebungsvariablen,
  Aktionen). `New-ResponseFile` ist skript-intern.
- Gueltige Werte verhalten sich identisch — bis auf Trimmen
  (fuehrende/folgende Leerzeichen werden jetzt entfernt; das ist die
  gewollte Sanitierung).
- `PairingContractTests`-Pin wird angepasst (Punkt 3), keine weiteren
  Tests betroffen.

## Test- und Nachweisstrategie

- `dotnet test VideoWebPlayer.Maui.Tests` vollstaendig; Zahlen in
  `test-results.md`.
- pwsh-Ausfuehrungstests belegen Sanitizing real (kein Source-Pin-
  Ersatz, sondern Ergaenzung).
- **Manueller Nachweis (nicht hier moeglich):** `store`-Lauf mit
  Upload auf dem Mac und der Geraete-Restnachweis aus Issue #15
  bleiben ausstehend — dokumentiert in `test-results.md`.

## Offene Punkte

Keine — alle Entscheidungen sind aus den Akzeptanzkriterien des
Projektplans und der Reproduktion ableitbar.
