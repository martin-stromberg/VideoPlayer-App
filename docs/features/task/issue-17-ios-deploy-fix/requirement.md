# Anforderung — iOS-Deployment-Skript reparieren (MSB1006)

Quelle: Projekt Issue #17, Entwicklungsschritt 1
(`docs/projects/project/issue-17-testbarkeit-und-onboarding/requirement.md`,
`project-plan.md` Abschnitt „Schritt 1").
Basisbranch: `project/issue-17-testbarkeit-und-onboarding`;
Schrittbranch: `task/issue-17-ios-deploy-fix`.

## Fachliche Zusammenfassung

`scripts/iOS-Deployment.ps1` schlägt beim Release-Build mit
App-Store-Connect-Upload (`-Action store`) mit dem Fehler `MSB1006`
(„Die Eigenschaft ist ungültig") fehl. Das Skript übergibt MSBuild-
Eigenschaften über eine Response-Datei (`-p:Name="Wert"` je Zeile);
seit dem Geräte-Pairing (Issue #15) wird zusätzlich
`-p:VideoPlayerApiToken="<token>"` eingespeist. Die Ursache der
ungültigen Property-Zeile ist zu identifizieren und zu beheben, sodass
der Store-Lauf wieder durchläuft — oder das Skript bricht bei
unzulässigen Werten mit einer verständlichen Fehlermeldung ab statt
mit dem kryptischen `MSB1006`.

## Betroffene Klassen und Komponenten

- `scripts/iOS-Deployment.ps1`
  - `Add-PropertyLine` (ca. Z. 685–693): erzeugt `-p:Name="Wert"`,
    überspringt leere Werte, trimmt nicht, prüft keine Zeichen.
  - `Add-MaskedDisplay` (ca. Z. 695–708): maskiert `ServerPassword`
    und `VideoPlayerApiToken` als `***` in der Konsolenanzeige.
  - `New-ResponseFile` (ca. Z. 710–716): schreibt die Zeilen per
    `[System.IO.File]::WriteAllLines` in `.ios-deploy-<guid>.rsp`
    im Repo-Root.
  - Einspeisestellen des Tokens: `Invoke-Build` (ca. Z. 725),
    `Invoke-Run` (ca. Z. 884), `Invoke-DeviceViaSsh` (ca. Z. 976).
  - Fehlerpfad nach `& dotnet @baseArgs` (ca. Z. 773–776, 944–947,
    989–992): meldet nur „Build fehlgeschlagen", ohne die
    Response-Datei-Zeilen (maskiert) zu zeigen.
- Tests: `VideoWebPlayer.Maui.Tests` (xUnit, net10.0); etabliertes
  Muster sind Contract-Tests, die Skript-/Quelltexte prüfen
  (`PairingContractTests.cs`, `RepositoryRoot.Find()`). Besser,
  wenn möglich: ein Test, der die Sanitizing-Logik tatsächlich
  ausführt (Logik in eine separat testbare Datei extrahieren und
  per `pwsh`-Aufruf prüfen).

## Implementierungsansatz

1. **Ursache eingrenzen:** Test-Response-Dateien mit problematischen
   Werten (`"` im Wert, `#` im Wert, Zeilenumbruch, Whitespace, sehr
   langer Wert) gegen ein minimales Test-csproj laufen lassen
   (`dotnet build min.csproj @test.rsp`) und dokumentieren, welche
   Zeichenklasse `MSB1006` auslöst.
2. **Fix:** `Add-PropertyLine` sanitisiert Werte (Trimmen; Abbruch
   mit verständlicher Fehlermeldung bei `\r`, `\n`, `"`, `#` im
   Wert) oder schreibt Werte ohne Quotes, wo sicher. Die
   `Add-MaskedDisplay`-Konvention (keine Secrets in Ausgaben) bleibt
   bestehen.
3. **Fehlschlag-Transparenz:** Bei `MSB1006`/Sanitizing-Abbruch wird
   die betroffene Response-Datei-Zeile maskiert geloggt.
4. **Tests:** Sanitizing-Regeln automatisiert pinnen (bevorzugt
   ausführbar über extrahierte Hilfsdatei + `pwsh`, alternativ
   Contract-Test auf Quelltext).

## Konfiguration

Keine neue Konfiguration. Token-Quelle bleibt Parameter
`-VideoPlayerApiToken` / Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN`.

## Offene Fragen

Keine — die Akzeptanzkriterien aus `project-plan.md` Schritt 1 sind
verbindlich:

1. MSB1006-Ursache identifiziert und dokumentiert.
2. `Add-PropertyLine`/`New-ResponseFile` erzeugen für problematische
   Werte gültige Response-Dateien oder brechen verständlich ab.
3. Automatisierter Test pinnt die Sanitizing-Regeln.
4. Secrets werden weiterhin nur maskiert angezeigt.
5. Geräte-Restnachweis (Issue #15) bleibt manueller Nachweis — nicht
   Teil der Code-Verifikation.
