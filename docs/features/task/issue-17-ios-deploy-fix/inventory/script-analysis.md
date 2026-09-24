# Detail: Skript-Analyse `scripts/iOS-Deployment.ps1`

## Aufrufkette Store-Upload

`Invoke-Store` (Z. 657–670) → `Assert-StorePrerequisites`,
`Assert-CodesigningForAction`, `Assert-TransporterAvailable`,
optional `Update-BuildNumber` (Z. 428–442) → `Invoke-Build` (Z. 718–793)
→ `Get-LatestIpa` → `Copy-ApiKeyToMac` → `Invoke-IpaValidation`
→ `Invoke-StoreUpload` (iTMSTransporter, Z. 637–655).

## Response-Datei-Mechanik (Ist-Zustand)

- `Add-PropertyLine` (Z. 685–693): `$List.Add("-p:$Name=`"$Value`"")` →
  Zeile `-p:Name="Wert"`; leere Werte werden uebersprungen (Z. 691).
  **Kein Trimmen, keine Zeichenpruefung.**
- `Add-MaskedDisplay` (Z. 695–708): `ServerPassword`/`VideoPlayerApiToken`
  → `-p:Name=***`; alle anderen Werte unmaskiert mit Quotes.
- `New-ResponseFile` (Z. 710–716): `[System.IO.File]::WriteAllLines`
  nach `.ios-deploy-<guid>.rsp` im Repo-Root; Default-Encoding
  (UTF-8 ohne BOM). Die Datei wird im `finally`-Block geloescht
  (Z. 791, 951, 995).

## Einspeisestellen `VideoPlayerApiToken` und Display-Listen

| Stelle | Funktion | dotnet-Aufruf | Display-Liste |
|--------|----------|---------------|---------------|
| Z. 725 | `Invoke-Build` | `dotnet build|publish … @rsp` (Z. 766/771) | ja (Z. 735–744, Ausgabe Z. 768–769) |
| Z. 884 | `Invoke-Run` | `dotnet build -t:Run … @rsp` (Z. 937) | ja (Z. 894–903, Ausgabe Z. 939–940) |
| Z. 976 | `Invoke-DeviceViaSsh` | `dotnet publish … "@$rspPath"` (Z. 988) | **nein** |

Token-Quelle: Parameter `-VideoPlayerApiToken`, Default
`$env:VIDEOWEBPLAYER_MAUI_API_TOKEN` (Z. 75–76).

## Fehlerpfad (Ist-Zustand)

Bei `LASTEXITCODE -ne 0` nach `& dotnet …` wird nur
„Build fehlgeschlagen (Exit-Code: N)." ausgegeben (Z. 773–776,
944–947, 989–992). Die Response-Datei existiert zu diesem Zeitpunkt
noch (Loeschung erst im `finally`), ihr Inhalt wird aber nicht gezeigt
— weder maskiert noch unmaskiert.

## Weitere -p:-Werte, die denselben Parserweg durchlaufen

`RuntimeIdentifier`, `ServerAddress`, `ServerUser`, `TcpPort`,
`ServerPassword`, `_DotNetRootRemoteDirectory`, `ArchiveOnBuild`,
`CodesignKey`, `CodesignProvision`, `CodesignEntitlements`,
`_DeviceName`. `CodesignKey` enthaelt real Leerzeichen und Doppelpunkt
(`Apple Distribution: Name (TEAM)`) — Quotes sind also weiterhin noetig.
