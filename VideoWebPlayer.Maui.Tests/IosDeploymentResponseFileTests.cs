using System.Diagnostics;
using System.Text;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract- und Ausführungstests für die MSBuild-Response-Datei-Erzeugung
/// des iOS-Deployments (MSB1006-Fix). Die Sanitizing-Logik liegt in der
/// dot-gesourcten Helper-Datei <c>scripts/iOS-Deployment.ResponseFile.ps1</c>
/// und wird hier per <c>pwsh</c> real ausgeführt — nicht nur als
/// Quelltext-Pin. Ohne verfügbares pwsh werden die Ausführungstests
/// übersprungen, die Quelltext-Pins bleiben bestehen.
/// </summary>
public class IosDeploymentResponseFileTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string ScriptFile =>
        File.ReadAllText(Path.Combine(Root, "scripts", "iOS-Deployment.ps1"));

    private static string HelperFile =>
        File.ReadAllText(Path.Combine(Root, "scripts", "iOS-Deployment.ResponseFile.ps1"));

    private static string HelperPath =>
        Path.Combine(Root, "scripts", "iOS-Deployment.ResponseFile.ps1");

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }

    // --- Quelltext-Pins ---

    /// <summary>Prüft: das Hauptskript bindet den Response-Datei-Helper ein.</summary>
    [Fact]
    public void DeploymentScript_DotSourcesResponseFileHelper()
    {
        var script = ScriptFile;

        Assert.Contains("iOS-Deployment.ResponseFile.ps1", script, StringComparison.Ordinal);
        Assert.Matches(
            @"\.\s*\(Join-Path \$PSScriptRoot ""iOS-Deployment\.ResponseFile\.ps1""\)",
            script);
    }

    /// <summary>
    /// Prüft: der API-Token läuft an allen drei Einspeisestellen
    /// (Invoke-Build, Invoke-Run, Invoke-DeviceViaSsh) durch die
    /// sanitierende Add-PropertyLine; die Display-Listen maskieren ihn.
    /// </summary>
    [Fact]
    public void DeploymentScript_FeedsTokenThroughSanitizingAtAllCallSites()
    {
        var script = ScriptFile;

        Assert.Equal(3, CountOccurrences(
            script, "Add-PropertyLine -List $rspLines -Name \"VideoPlayerApiToken\""));
        // Invoke-Build und Invoke-Run haben Display-Listen;
        // Invoke-DeviceViaSsh zeigt nur im Fehlerfall maskiert an.
        Assert.Equal(2, CountOccurrences(
            script, "Add-MaskedDisplay -List $displayLines -Name \"VideoPlayerApiToken\""));
    }

    /// <summary>
    /// Prüft: alle drei dotnet-Fehlerpfade geben die Response-Datei
    /// maskiert aus (Fehlschlag-Transparenz bei MSB1006 &amp; Co.).
    /// </summary>
    [Fact]
    public void DeploymentScript_LogsMaskedLinesOnBuildFailure()
    {
        Assert.Equal(3, CountOccurrences(
            ScriptFile, "Write-MaskedPropertyLines -Lines $rspLines"));
    }

    /// <summary>
    /// Prüft: beim Geräte-Deployment (Aktion 'device') verwirft das Skript
    /// per VIDEOPLAYER_IOS_*-Umgebungsvariable vorgegebene Signatur-Werte —
    /// sie sind für den Store-Upload gedacht und würden sonst ein
    /// Distributionsprofil still in den Device-Pfad ziehen (Fehler
    /// „Keine iOS-Signaturidentität entspricht dem Bereitstellungsprofil").
    /// Die Auswahl wird dadurch zwingend angeboten.
    /// </summary>
    [Fact]
    public void DeploymentScript_DeviceActionDiscardsEnvCodesignValues()
    {
        var script = ScriptFile;

        Assert.Contains("$CodesignProvision -eq $env:VIDEOPLAYER_IOS_PROVISIONING_PROFILE",
            script, StringComparison.Ordinal);
        Assert.Contains("$CodesignKey -eq $env:VIDEOPLAYER_IOS_CODESIGN_KEY",
            script, StringComparison.Ordinal);
        // Die Profil-Auswahl für 'device' bleibt auf Entwicklungsprofile
        // (get-task-allow) gefiltert.
        Assert.Contains("Where-Object { $_.DevProfile }", script, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: der Helper prüft genau die Zeichenklasse, die den
    /// Response-Datei-Parser bricht (Beleg: inventory/msb1006-repro.md).
    /// </summary>
    [Fact]
    public void Helper_SanitizesForbiddenCharacters()
    {
        var helper = HelperFile;

        Assert.Contains("Assert-PropertyValue", helper, StringComparison.Ordinal);
        Assert.Contains("IndexOfAny", helper, StringComparison.Ordinal);
        Assert.Contains("'\"'", helper, StringComparison.Ordinal);
        Assert.Contains("'#'", helper, StringComparison.Ordinal);
        Assert.Contains("\"`r\"", helper, StringComparison.Ordinal);
        Assert.Contains("\"`n\"", helper, StringComparison.Ordinal);
        // Trimmen gehört zur Sanitierung.
        Assert.Contains(".Trim()", helper, StringComparison.Ordinal);
    }

    /// <summary>Prüft: die Maskierliste enthält beide Secrets.</summary>
    [Fact]
    public void Helper_MasksBothSecrets()
    {
        var helper = HelperFile;

        Assert.Contains("MaskedPropertyNames", helper, StringComparison.Ordinal);
        Assert.Contains("\"ServerPassword\"", helper, StringComparison.Ordinal);
        Assert.Contains("\"VideoPlayerApiToken\"", helper, StringComparison.Ordinal);
    }

    // --- Ausführungstests per pwsh ---

    private static bool PwshAvailable()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "pwsh",
                Arguments = "-NoProfile -Command \"exit 0\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var process = Process.Start(psi)!;
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static (int ExitCode, string Output) RunHelperScript(string body)
    {
        var scriptPath = Path.Combine(
            Path.GetTempPath(), $"rsp-test-{Guid.NewGuid():N}.ps1");
        File.WriteAllText(
            scriptPath,
            $". '{HelperPath.Replace("'", "''")}'\n{body}\n",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "pwsh",
                Arguments = $"-NoProfile -File \"{scriptPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var process = Process.Start(psi)!;
            var output = process.StandardOutput.ReadToEnd()
                + process.StandardError.ReadToEnd();
            process.WaitForExit();
            return (process.ExitCode, output);
        }
        finally
        {
            File.Delete(scriptPath);
        }
    }

    /// <summary>
    /// Prüft: Add-PropertyLine trimmt den Wert und quotet ihn in der
    /// rsp-Zeile — das bisherige Format bleibt bestehen.
    /// </summary>
    [Fact]
    public void AddPropertyLine_TrimsAndQuotesValue()
    {
        Assert.SkipUnless(PwshAvailable(), "pwsh nicht verfügbar");

        var (exitCode, output) = RunHelperScript("""
            $l = New-Object System.Collections.Generic.List[string]
            Add-PropertyLine -List $l -Name "TestProp" -Value "  abc123  "
            $l[0]
            """);

        Assert.Equal(0, exitCode);
        Assert.Contains("-p:TestProp=\"abc123\"", output, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: Werte mit `"`, `#`, CR oder LF werden mit Exit-Code 1 und
    /// verständlicher Meldung abgelehnt — statt MSB1006/MSB4177/MSB1008
    /// beim dotnet-Aufruf.
    /// </summary>
    [Theory]
    [InlineData("-Value 'sec\"ret'", "Anfuehrungszeichen")]
    [InlineData("-Value 'sec#ret'", "Raute")]
    [InlineData("-Value \"sec`rret\"", "Wagenruecklauf")]
    [InlineData("-Value \"sec`nret\"", "Zeilenumbruch")]
    public void AddPropertyLine_RejectsForbiddenCharacters(
        string valueArgument, string expectedCharName)
    {
        Assert.SkipUnless(PwshAvailable(), "pwsh nicht verfügbar");

        var (exitCode, output) = RunHelperScript($"""
            $l = New-Object System.Collections.Generic.List[string]
            Add-PropertyLine -List $l -Name "TestProp" {valueArgument}
            Write-Output "SHOULD-NOT-REACH"
            """);

        Assert.Equal(1, exitCode);
        Assert.DoesNotContain("SHOULD-NOT-REACH", output, StringComparison.Ordinal);
        Assert.Contains("TestProp", output, StringComparison.Ordinal);
        Assert.Contains(expectedCharName, output, StringComparison.Ordinal);
        // Kein kryptischer MSBuild-Fehler mehr — die Meldung erklärt die Ursache.
        Assert.Contains("MSB1006", output, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: beim Sanitizing-Abbruch eines Secrets wird die Zeile
    /// maskiert ausgegeben — der Secret-Wert darf nicht im Log stehen.
    /// </summary>
    [Fact]
    public void AddPropertyLine_RejectionKeepsSecretMasked()
    {
        Assert.SkipUnless(PwshAvailable(), "pwsh nicht verfügbar");

        var (exitCode, output) = RunHelperScript("""
            $l = New-Object System.Collections.Generic.List[string]
            Add-PropertyLine -List $l -Name "VideoPlayerApiToken" -Value 'GEHEIM123"x'
            """);

        Assert.Equal(1, exitCode);
        Assert.Contains("VideoPlayerApiToken", output, StringComparison.Ordinal);
        Assert.Contains("-p:VideoPlayerApiToken=***", output, StringComparison.Ordinal);
        Assert.DoesNotContain("GEHEIM123", output, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: die Maskierung greift für beide Secrets und lässt
    /// unmaskierte Werte unverändert.
    /// </summary>
    [Fact]
    public void MaskedPropertyLine_MasksSecretsOnly()
    {
        Assert.SkipUnless(PwshAvailable(), "pwsh nicht verfügbar");

        var (exitCode, output) = RunHelperScript("""
            Get-MaskedPropertyLine -Name "ServerPassword" -Value "pw"
            Get-MaskedPropertyLine -Name "VideoPlayerApiToken" -Value "tok"
            Get-MaskedPropertyLine -Name "ServerUser" -Value "martin"
            """);

        Assert.Equal(0, exitCode);
        Assert.Contains("-p:ServerPassword=***", output, StringComparison.Ordinal);
        Assert.Contains("-p:VideoPlayerApiToken=***", output, StringComparison.Ordinal);
        Assert.Contains("-p:ServerUser=\"martin\"", output, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft: New-ResponseFile schreibt die Zeilen UTF-8 ohne BOM —
    /// Voraussetzung, dass MSBuild die Datei sauber liest.
    /// </summary>
    [Fact]
    public void NewResponseFile_WritesUtf8WithoutBom()
    {
        Assert.SkipUnless(PwshAvailable(), "pwsh nicht verfügbar");

        var directory = Path.Combine(
            Path.GetTempPath(), $"rsp-dir-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var (exitCode, output) = RunHelperScript($"""
                $p = New-ResponseFile -Lines @('-p:A="1"', '-p:B="2"') -Directory '{directory}'
                Write-Output "RSPPATH=$p"
                """);

            Assert.Equal(0, exitCode);
            var pathLine = output.Split('\n')
                .FirstOrDefault(l => l.StartsWith("RSPPATH=", StringComparison.Ordinal));
            Assert.NotNull(pathLine);
            var rspPath = pathLine["RSPPATH=".Length..].Trim();
            var bytes = File.ReadAllBytes(rspPath);
            Assert.False(
                bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
                "Response-Datei enthält eine UTF-8-BOM");
            var content = File.ReadAllText(rspPath);
            Assert.Contains("-p:A=\"1\"", content, StringComparison.Ordinal);
            Assert.Contains("-p:B=\"2\"", content, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
