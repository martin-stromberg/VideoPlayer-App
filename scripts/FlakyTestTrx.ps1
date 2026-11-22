<#
    FlakyTestTrx.ps1

    Gemeinsamer TRX-Helfer von Retry-FlakyTests.ps1 und
    Analyze-FlakyTestResults.ps1 — wird von beiden per Dot-Sourcing
    ueber $PSScriptRoot eingebunden.

    Kapselt zum einen die TRX-Aufloesung (UnitTestResult -> FQN via
    UnitTest/TestMethod className.name, Fallback: testName-Attribut
    ohne Argumentliste) und zum anderen die gemeinsame Definition,
    welche Outcomes als "Test fehlgeschlagen" zaehlen: die explizite
    Menge Failed, Error, Timeout. Error/Timeout (Testhost- und
    Fixture-Abbrueche) sind klassische Flakes; nicht
    ausfuehrungsbezogene Stati wie NotExecuted duerfen weder einen
    Re-Run ausloesen noch als Flake zaehlen.
#>

$script:TrxNamespace =
    "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"

$script:TrxFailureOutcomes =
    [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
foreach ($outcome in @("Failed", "Error", "Timeout")) {
    [void]$script:TrxFailureOutcomes.Add($outcome)
}

function Get-TrxTestOutcomes {
    # Liefert @{ fqn = outcome } aller UnitTestResults einer TRX-Datei.
    param([string]$TrxPath)
    $outcomes = @{}
    [xml]$doc = Get-Content -LiteralPath $TrxPath -Raw
    $nsmgr = [System.Xml.XmlNamespaceManager]::new($doc.NameTable)
    $nsmgr.AddNamespace("t", $script:TrxNamespace)

    $testNames = @{}
    foreach ($unitTest in $doc.SelectNodes(
            "//t:TestRun/t:TestDefinitions/t:UnitTest", $nsmgr)) {
        $method = $unitTest.SelectSingleNode("t:TestMethod", $nsmgr)
        if ($null -ne $method) {
            $testNames[$unitTest.id] = "$($method.className).$($method.name)"
        }
    }

    foreach ($result in $doc.SelectNodes(
            "//t:TestRun/t:Results/t:UnitTestResult", $nsmgr)) {
        $name = if ($testNames.ContainsKey($result.testId)) {
            $testNames[$result.testId]
        }
        else {
            ($result.testName -replace "\s*\(.*$", "")
        }
        $outcomes[$name] = [string]$result.outcome
    }
    return $outcomes
}

function Get-TrxFailedTests {
    # Liefert die Menge der Test-FQNs, deren Outcome zur gemeinsamen
    # Fehlschlag-Menge (Failed, Error, Timeout) gehoert — aggregiert
    # ueber alle angegebenen TRX-Dateien.
    param([string[]]$TrxPaths)
    $failed = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    foreach ($path in $TrxPaths) {
        foreach ($kv in (Get-TrxTestOutcomes $path).GetEnumerator()) {
            if ($script:TrxFailureOutcomes.Contains($kv.Value)) {
                [void]$failed.Add($kv.Key)
            }
        }
    }
    # -NoEnumerate: PowerShell wuerde das HashSet sonst in seine
    # Elemente auspacken (0 Treffer -> $null, 1 Treffer -> string).
    Write-Output -NoEnumerate $failed
}
