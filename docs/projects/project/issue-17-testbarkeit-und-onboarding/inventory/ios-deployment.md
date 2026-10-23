# Detail: iOS-Deployment (Schritt 1, MSB1006)

## Aufrufkette Store-Upload

`Invoke-Store` (`scripts/iOS-Deployment.ps1` Z. 657–670)
→ `Assert-StorePrerequisites`, `Assert-CodesigningForAction`, `Assert-TransporterAvailable`
→ optional `Update-BuildNumber` (Z. 428–438: Regex-Bump `<ApplicationVersion>` in `VideoPlayer.Maui.csproj`)
→ `Invoke-Build` (Z. 718–793) → `Get-LatestIpa` → `Copy-ApiKeyToMac` → `Invoke-IpaValidation` → `Invoke-StoreUpload` (iTMSTransporter, Z. 640–654).

## Response-Datei-Mechanik

- `Add-PropertyLine` (Z. 685–693): `$List.Add("-p:$Name=`"$Value`"")` → Zeile `-p:Name="Wert"`; leere Werte werden übersprungen (Z. 691).
- `Add-MaskedDisplay` (Z. 695–708): `ServerPassword`/`VideoPlayerApiToken` → `-p:Name=***`.
- `New-ResponseFile` (Z. 710–716): `[System.IO.File]::WriteAllLines` nach `.ios-deploy-<guid>.rsp` im Repo-Root; Default-Encoding (UTF-8 ohne BOM in .NET 7+; unter Windows PowerShell 5.1 ebenfalls ohne BOM).

## Einspeisestellen `VideoPlayerApiToken`

| Stelle | Pfad | Aufruf |
|--------|------|--------|
| Z. 725 | `Invoke-Build` | `dotnet build|publish … @rsp` (Z. 766/771); `publish` bei Codesigning |
| Z. 884 | `Invoke-Run` | `dotnet build -t:Run … @rsp` (Z. 937); Windows bricht ab (Z. 867–874) |
| Z. 976 | `Invoke-DeviceViaSsh` | `dotnet publish … "@$rspPath"` (Z. 988); Windows-Pair-to-Mac |

Token-Quelle: Parameter `-VideoPlayerApiToken`, Default `$env:VIDEOWEBPLAYER_MAUI_API_TOKEN` (Z. 75–76). `.ios-deploy.user.json` enthält keinen Token (nur Store-Signatur).

## MSB1006-Hypothesen (keine ist belegt)

MSB1006 = „Property is not valid" beim Parsen der Property-Argumente. Kandidaten:

1. **`"` im Token** → unterminierte Quote in der rsp-Zeile → Parser bricht Zeile falsch.
2. **`#` im Token** → rsp-Kommentarzeichen (MSBuild-Response-Dateien erlauben `#`-Kommentare) → Rest der Zeile fällt weg; die Zeile endet mit offener Quote oder kürzt den Property-Wert.
3. **Zeilenumbruch im Token** → eine rsp-Zeile wird zwei; die zweite Zeile ist ein nacktes Argument ohne Switch.
4. **Führende/folgende Whitespaces** im Wert (`Add-PropertyLine` trimmt nicht) — Quote umfasst sie zwar, aber kombiniert mit anderen Zeichen potenziell problematisch.
5. **Interaktion `dotnet`↔msbuild**: die rsp wird von `dotnet build/publish` an MSBuild weitergereicht; ob Quotes in rsp-Property-Zeilen am Ende im Property-Wert landen, ist versionabhängig — robuste Alternative: `-p:Name=Wert` ohne Quotes (Werte enthalten keine Leerzeichen) oder explizites Escaping.

## Fix-Ansätze (zur Planung)

- `Add-PropertyLine` sanitizen: Wert trimmen; Zeichenklasse whitelisten oder auf `\r\n"#` prüfen und bei Verstoß kontrolliert abbrechen; alternativ Quotes weglassen.
- Contract-Test im Testprojekt auf die Response-Datei-Zeilen (Muster `PairingContractTests`-Quelltext-Prüfung oder extrahierte Hilfsfunktion).
- Fehlschlag-Transparenz: rsp-Inhalt maskiert ausgeben (bereits via `displayLines`), bei MSB1006 zusätzlich die betroffene Zeile (maskiert) loggen.

## Verifikation

Nach dem Fix: `store`-Lauf bis zum Upload; anschließend Geräte-Restnachweis aus Issue #15 (echtes iOS-Gerät: Keychain nach Neuinstall/Neustart, `DeviceInfo`-Name, UI-Bedienung; Android-Pendant). Windows ist bereits verifiziert (883/883 Contract-Stand).
