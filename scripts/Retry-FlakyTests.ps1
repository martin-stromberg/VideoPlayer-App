<#
    Retry-FlakyTests.ps1

    Fuehrt genau einen gefilterten Re-Run der im Hauptlauf
    fehlgeschlagenen Tests aus (Best-Effort-Lane test-os-interface).

    Das Skript liest die TRX-Dateien des Hauptlaufs, sammelt die
    FullyQualifiedNames der fehlgeschlagenen Tests und startet
    dotnet test mit einem FullyQualifiedName~-Filter. Das Re-Run-
    Ergebnis landet als eigene TRX in -ResultsDirectory.

    Exit-Codes:
      0 = kein Re-Run noetig (keine TRX/keine Fehlschlaege) oder
          Re-Run ausgefuehrt — auch bei Testfehlschlaegen im Re-Run
          (das Ergebnis liegt in der TRX und wird separat ausgewertet).
      1 = Aufruf-/Infrastrukturproblem (fehlendes Verzeichnis u. ae.).
#>
[CmdletBinding()]
param(
    # Verzeichnis mit den TRX-Dateien des Hauptlaufs.
    [Parameter(Mandatory = $true)]
    [string]$TrxDirectory,

    # Testprojekt-Datei fuer den Re-Run.
    [Parameter(Mandatory = $true)]
    [string]$Project,

    # Build-Konfiguration des Re-Runs.
    [string]$Configuration = "Release",

    # Zusaetzlicher dotnet-test-Filter, der mit dem
    # FullyQualifiedName-Filter per & verknuepft wird.
    [string]$Filter = "",

    # Zielverzeichnis der Re-Run-TRX.
    [Parameter(Mandatory = $true)]
    [string]$ResultsDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Gemeinsame TRX-Aufloesung + Fehlschlag-Outcome-Menge
# (Failed, Error, Timeout) — geteilt mit Analyze-FlakyTestResults.ps1.
. (Join-Path $PSScriptRoot "FlakyTestTrx.ps1")

if (-not (Test-Path -LiteralPath $TrxDirectory -PathType Container)) {
    [Console]::Error.WriteLine("TrxDirectory '$TrxDirectory' existiert nicht.")
    exit 1
}

$trxFiles = @(Get-ChildItem -LiteralPath $TrxDirectory -Filter "*.trx" -File)
if ($trxFiles.Count -eq 0) {
    Write-Host "Keine TRX-Dateien in '$TrxDirectory' — kein Re-Run."
    exit 0
}

# Fehlgeschlagene Tests aus den Hauptlauf-TRX sammeln — als
# Fehlschlag zaehlt die explizite Outcome-Menge aus FlakyTestTrx.ps1
# (Failed, Error, Timeout; Testhost-/Fixture-Abbrueche sind
# klassische Flakes). 'NotExecuted' o. ae. loest keinen Re-Run aus.
# UnitTestResult.testId -> UnitTest/TestMethod
# (className + name = FullyQualifiedName ohne Argumentliste).
$failed = Get-TrxFailedTests -TrxPaths @($trxFiles.FullName)

if ($failed.Count -eq 0) {
    Write-Host "Keine fehlgeschlagenen Tests im Hauptlauf — kein Re-Run."
    exit 0
}

Write-Host ("Re-Run von {0} fehlgeschlagenen Tests: {1}" -f `
    $failed.Count, ($failed -join ", "))

$nameFilter = ($failed | ForEach-Object { "FullyQualifiedName~$_" }) -join "|"
$combinedFilter = if ([string]::IsNullOrWhiteSpace($Filter)) {
    $nameFilter
}
else {
    "($Filter)&($nameFilter)"
}

New-Item -ItemType Directory -Force -Path $ResultsDirectory | Out-Null

& dotnet test $Project --no-build --configuration $Configuration `
    --filter $combinedFilter --logger "trx" `
    --results-directory $ResultsDirectory
$testExit = $LASTEXITCODE

if ($testExit -ne 0) {
    # Testfehlschlaege im Re-Run sind erwartet moeglich — Auswertung
    # erfolgt ueber die TRX, nicht ueber den Exit-Code.
    Write-Host (("Re-Run mit Testfehlschlaegen beendet (Exit {0}) — " +
        "Ergebnis liegt in '{1}'.") -f $testExit, $ResultsDirectory)
}

exit 0
