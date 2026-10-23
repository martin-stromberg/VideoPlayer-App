<#
    iOS-Deployment.ResponseFile.ps1

    Hilfsfunktionen fuer die MSBuild-Response-Datei (.rsp) des
    iOS-Deployments. Wird von iOS-Deployment.ps1 per Dot-Sourcing
    eingebunden, damit die Logik einzeln testbar bleibt
    (VideoPlayer.Maui.Tests/IosDeploymentResponseFileTests.cs).

    Hintergrund: Ein Wert mit Anfuehrungszeichen, Raute oder
    Zeilenumbruch zerlegt die rsp-Zeile so, dass MSBuild nur noch
    MSB1006/MSB4177/MSB1008 ohne Zeilenangabe meldet. Assert-PropertyValue
    bricht deshalb mit einer verstaendlichen Meldung ab, bevor die
    Datei geschrieben wird.
#>

# Eigenschaftsnamen, deren Werte in Anzeige und Fehlerausgabe maskiert werden.
$script:MaskedPropertyNames = @("ServerPassword", "VideoPlayerApiToken")

function Get-MaskedPropertyLine {
    # Liefert die Anzeigeform einer -p:-Zeile; Secrets als ***.
    param(
        [string]$Name,
        [string]$Value
    )
    if ($script:MaskedPropertyNames -contains $Name) {
        return "-p:$Name=***"
    }
    return "-p:$Name=`"$Value`""
}

function Write-MaskedPropertyLines {
    # Gibt Response-Datei-Zeilen zur Fehlerdiagnose aus; Secrets maskiert.
    param([string[]]$Lines)
    foreach ($line in $Lines) {
        if ($line -match '^-p:([^=]+)=' -and $script:MaskedPropertyNames -contains $Matches[1]) {
            Write-Host "   -p:$($Matches[1])=***" -ForegroundColor Gray
        }
        else {
            Write-Host "   $line" -ForegroundColor Gray
        }
    }
}

function Assert-PropertyValue {
    # Bricht ab, wenn der Wert die Response-Datei fuer MSBuild
    # unleserlich machen wuerde. Die Meldung nennt Eigenschaftsname,
    # Zeichenart und Position und zeigt die Zeile maskiert - sie
    # enthaelt den Wert eines Secrets nicht.
    param(
        [string]$Name,
        [string]$Value
    )
    $bad = $Value.IndexOfAny([char[]]@('"', '#', "`r", "`n"))
    if ($bad -lt 0) { return }
    $charName = switch ([string]$Value[$bad]) {
        '"' { 'ein Anfuehrungszeichen (")' }
        '#' { 'eine Raute (#)' }
        "`r" { 'einen Wagenruecklauf (CR)' }
        "`n" { 'einen Zeilenumbruch (LF)' }
    }
    Write-Host "Fehler: MSBuild-Eigenschaft '$Name' enthaelt $charName an Position $($bad + 1)." -ForegroundColor Red
    Write-Host "Response-Datei-Zeile (maskiert): $(Get-MaskedPropertyLine -Name $Name -Value $Value)" -ForegroundColor Yellow
    Write-Host "Die Zeichen `" # CR LF brechen den MSBuild-Response-Datei-Parser (MSB1006/MSB4177/MSB1008)." -ForegroundColor Yellow
    Write-Host "Bitte den Wert ohne diese Zeichen uebergeben (z. B. keine mitkopierten Anfuehrungszeichen oder Zeilenumbrueche)." -ForegroundColor Yellow
    exit 1
}

function Add-PropertyLine {
    param(
        [System.Collections.Generic.List[string]]$List,
        [string]$Name,
        [string]$Value
    )
    if (-not $Value) { return }
    $Value = $Value.Trim()
    if (-not $Value) { return }
    Assert-PropertyValue -Name $Name -Value $Value
    $List.Add("-p:$Name=`"$Value`"")
}

function Add-MaskedDisplay {
    param(
        [System.Collections.Generic.List[string]]$List,
        [string]$Name,
        [string]$Value
    )
    if (-not $Value) { return }
    $Value = $Value.Trim()
    if ($script:MaskedPropertyNames -contains $Name) {
        $List.Add("-p:$Name=***")
    }
    else {
        Assert-PropertyValue -Name $Name -Value $Value
        $List.Add("-p:$Name=`"$Value`"")
    }
}

function New-ResponseFile {
    param(
        [string[]]$Lines,
        [string]$Directory
    )
    $rspName = ".ios-deploy-$([Guid]::NewGuid().ToString('n')).rsp"
    $rspPath = Join-Path $Directory $rspName
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllLines($rspPath, $Lines, $utf8NoBom)
    return $rspPath
}
