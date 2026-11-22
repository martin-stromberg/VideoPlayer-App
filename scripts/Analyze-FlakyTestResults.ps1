<#
    Analyze-FlakyTestResults.ps1

    Wertet die TRX-Artefakte (os-interface-trx) der letzten
    test-os-interface-Laeufe aus und berechnet pro Test die
    Flake-Rate: „Hauptlauf fehlgeschlagen, Re-Run bestanden".

    Mit GH_TOKEN (CI): laedt die Artefakte der letzten -RunLimit
    Workflow-Laeufe per gh herunter, schreibt eine Step-Summary und
    erzeugt ab -FlakeThreshold deduplizierte Quarantaene-Issues
    (Label flaky-test).

    Ohne GH_TOKEN (lokal): wertet nur ein lokales -ArtifactDirectory
    aus (ein Unterverzeichnis pro Lauf mit os-main/os-rerun-TRX) —
    kein Download, keine Issues.
#>
[CmdletBinding()]
param(
    # Anzahl der ausgewerteten Laeufe (Artefakt-Historie).
    [ValidateRange(1, 1000)]
    [int]$RunLimit = 10,

    # Flakes eines Tests im Auswertefenster, ab denen ein
    # Quarantaene-Issue erzeugt wird.
    [ValidateRange(1, 1000)]
    [int]$FlakeThreshold = 2,

    # Name des Auswerte-Artefakts.
    [string]$ArtifactName = "os-interface-trx",

    # Workflow-Datei der Laeufe (CI-Download).
    [string]$Workflow = "pr-checks.yml",

    # Lokale Auswertung: Verzeichnis mit einem Unterordner je Lauf.
    [string]$ArtifactDirectory = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Gemeinsame TRX-Aufloesung + Fehlschlag-Outcome-Menge
# (Failed, Error, Timeout) — geteilt mit Retry-FlakyTests.ps1.
. (Join-Path $PSScriptRoot "FlakyTestTrx.ps1")

function Get-RunFlakes {
    # Flakes eines Laufs: im Hauptlauf (os-main) fehlgeschlagen und
    # im Re-Run (os-rerun) bestanden. "Fehlgeschlagen" ist die
    # explizite Outcome-Menge aus FlakyTestTrx.ps1 — neben 'Failed'
    # auch 'Error' und 'Timeout' (Testhost-/Fixture-Abbrueche sind
    # klassische Flakes); 'NotExecuted' o. ae. zaehlt nicht.
    param([string]$RunDirectory)
    $mainDir = Join-Path $RunDirectory "os-main"
    $rerunDir = Join-Path $RunDirectory "os-rerun"
    if (-not (Test-Path -LiteralPath $mainDir -PathType Container)) {
        return @()
    }

    # @(...)+Count-Guard: ohne TRX-Dateien liefert .FullName unter
    # StrictMode eine PropertyNotFoundException statt eines Leerarrays.
    $mainTrx = @(Get-ChildItem -LiteralPath $mainDir -Filter "*.trx" -File)
    if ($mainTrx.Count -eq 0) {
        return @()
    }

    $failed = Get-TrxFailedTests -TrxPaths @($mainTrx.FullName)
    if ($failed.Count -eq 0 -or
        -not (Test-Path -LiteralPath $rerunDir -PathType Container)) {
        return @()
    }

    $passed = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    foreach ($trx in @(Get-ChildItem -LiteralPath $rerunDir -Filter "*.trx" -File)) {
        foreach ($kv in (Get-TrxTestOutcomes $trx.FullName).GetEnumerator()) {
            if ($kv.Value -eq "Passed") { [void]$passed.Add($kv.Key) }
        }
    }

    return @($failed | Where-Object { $passed.Contains($_) })
}

# --- Artefakt-Quellen aufbereiten ---------------------------------------

$runDirs = [System.Collections.Generic.List[string]]::new()
$downloadRoot = $null

if (-not [string]::IsNullOrWhiteSpace($ArtifactDirectory)) {
    foreach ($dir in @(Get-ChildItem -LiteralPath $ArtifactDirectory -Directory)) {
        if (Test-Path -LiteralPath (Join-Path $dir.FullName "os-main") -PathType Container) {
            $runDirs.Add($dir.FullName)
        }
    }
    # Das Verzeichnis selbst kann bereits ein einzelner Lauf sein.
    if (Test-Path -LiteralPath (Join-Path $ArtifactDirectory "os-main") -PathType Container) {
        $runDirs.Add($ArtifactDirectory)
    }
}
elseif (-not [string]::IsNullOrWhiteSpace($env:GH_TOKEN)) {
    $downloadRoot = Join-Path ([System.IO.Path]::GetTempPath()) `
        ("flaky-artifacts-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Force -Path $downloadRoot | Out-Null

    $runListJson = gh run list --workflow $Workflow --limit $RunLimit --json databaseId
    if ($LASTEXITCODE -ne 0) {
        Write-Host ("::warning::gh run list fuer Workflow '{0}' fehlgeschlagen " +
            "(Exit {1}) — es werden keine Artefakte ausgewertet.") -f $Workflow, $LASTEXITCODE
        $runs = @()
    }
    else {
        $runs = @($runListJson | ConvertFrom-Json)
    }
    foreach ($run in $runs) {
        $target = Join-Path $downloadRoot ([string]$run.databaseId)
        gh run download $run.databaseId --name $ArtifactName --dir $target 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Host ("::warning::gh run download fuer Lauf {0} (Artefakt '{1}') " +
                "fehlgeschlagen (Exit {2}).") -f `
                $run.databaseId, $ArtifactName, $LASTEXITCODE
        }
        if (Test-Path -LiteralPath (Join-Path $target "os-main") -PathType Container) {
            $runDirs.Add($target)
        }
    }
    Write-Host ("{0} von {1} Laeufen enthalten auswertbare '{2}'-Artefakte." -f `
        $runDirs.Count, $runs.Count, $ArtifactName)
}
else {
    Write-Host "Weder -ArtifactDirectory noch GH_TOKEN gesetzt — keine Auswertung."
    exit 0
}

# --- Flake-Zaehlung ------------------------------------------------------

$flakeCounts = @{}
foreach ($dir in $runDirs) {
    foreach ($test in (Get-RunFlakes $dir)) {
        $flakeCounts[$test] = ($flakeCounts[$test] ?? 0) + 1
    }
}

$totalFlakes = ($flakeCounts.Values | Measure-Object -Sum).Sum
if ($null -eq $totalFlakes) { $totalFlakes = 0 }
$candidates = @($flakeCounts.GetEnumerator() |
    Where-Object { $_.Value -ge $FlakeThreshold } |
    Sort-Object -Property Value -Descending)

# --- Step-Summary + Warnings --------------------------------------------

$summary = @()
$summary += "## OsInterface-Flake-Auswertung"
$summary += ""
$summary += ("Ausgewertete Laeufe: **{0}** (Fenster: {1}); Flakes " +
    "(Hauptlauf fehlgeschlagen, Re-Run gruen): **{2}**") -f `
    $runDirs.Count, $RunLimit, $totalFlakes
if ($flakeCounts.Count -gt 0) {
    $summary += ""
    $summary += "| Test | Flakes |"
    $summary += "|------|--------|"
    foreach ($entry in ($flakeCounts.GetEnumerator() |
            Sort-Object -Property Value -Descending)) {
        $summary += "| $($entry.Key) | $($entry.Value) |"
    }
}
if ($env:GITHUB_STEP_SUMMARY) {
    $summary -join "`n" | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8
}
$summary | ForEach-Object { Write-Host $_ }

foreach ($entry in $candidates) {
    $message = "Quarantaene-Kandidat: '{0}' flake-te {1}x in den letzten {2} ausgewerteten Laeufen." -f `
        $entry.Key, $entry.Value, $runDirs.Count
    Write-Host "::warning::$message"
}

# --- Deduplizierte Quarantaene-Issues (nur CI-Download-Modus) ------------

$isCi = [string]::IsNullOrWhiteSpace($ArtifactDirectory) -and
    -not [string]::IsNullOrWhiteSpace($env:GH_TOKEN)
if ($candidates.Count -gt 0 -and $isCi) {
    $label = "flaky-test"
    $labelReady = $false
    $labelListJson = gh label list --search $label --json name
    if ($LASTEXITCODE -ne 0) {
        Write-Host ("::warning::gh label list fehlgeschlagen (Exit {0}) — " +
            "Quarantaene-Issues werden nicht angelegt.") -f $LASTEXITCODE
    }
    else {
        $existing = @($labelListJson | ConvertFrom-Json)
        if ($existing | Where-Object { $_.name -eq $label }) {
            $labelReady = $true
        }
        else {
            gh label create $label --color "B60205" `
                --description "Automatisch gemeldeter Flaky-Test (Quarantaene-Kandidat)" 2>$null
            if ($LASTEXITCODE -eq 0) {
                $labelReady = $true
            }
            else {
                Write-Host ("::warning::Label '{0}' konnte nicht angelegt werden " +
                    "(Exit {1}) — Quarantaene-Issues werden nicht angelegt.") -f `
                    $label, $LASTEXITCODE
            }
        }
    }

    if ($labelReady) {
        foreach ($entry in $candidates) {
            $title = "Flaky-Test-Quarantaene: $($entry.Key)"
            $issueListJson = gh issue list --label $label --state open `
                --search "in:title `"$title`"" --json number
            if ($LASTEXITCODE -ne 0) {
                Write-Host ("::warning::gh issue list fuer '{0}' fehlgeschlagen " +
                    "(Exit {1}) — Issue wird nicht angelegt.") -f $entry.Key, $LASTEXITCODE
                continue
            }

            $duplicates = @($issueListJson | ConvertFrom-Json)
            if ($duplicates.Count -gt 0) {
                Write-Host "Offenes Quarantaene-Issue zu '$($entry.Key)' existiert bereits — kein Duplikat."
                continue
            }

            $body = @"
Der OsInterface-Test ``$($entry.Key)`` ist in den letzten $($runDirs.Count) ausgewerteten
test-os-interface-Laeufen $($entry.Value)x im Hauptlauf fehlgeschlagen und im
automatischen Re-Run gruen geworden (Schwellwert: $FlakeThreshold).

Bitte Ursache untersuchen und den Test haerten oder gezielt quarantaenen.
Dokumentation: docs/help/stabilitaet/os-interface-tests.md
"@
            gh issue create --title $title --label $label --body $body
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Quarantaene-Issue angelegt: $title"
            }
            else {
                Write-Host ("::warning::Quarantaene-Issue '{0}' konnte nicht " +
                    "angelegt werden (Exit {1}).") -f $title, $LASTEXITCODE
            }
        }
    }
}

exit 0
