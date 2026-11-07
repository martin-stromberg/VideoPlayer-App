using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests für den tvOS-Release-Pfad (Schritt 11): Paketierungs-Action
/// <c>package-tvos</c>, Gate-Einbindung in den Release-Workflows, lokales
/// Deployment-Skript und Store-Metadaten im tvOS-Projekt. Da tvOS-Builds und
/// der eigentliche Upload unter Windows nicht ausführbar sind, werden die
/// Akzeptanzkriterien über die Quelldateien geprüft (Muster:
/// <see cref="TvOsProjectContractTests"/>).
/// </summary>
public class TvOsDeploymentContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string ReadRepoFile(params string[] parts)
    {
        return File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));
    }

    /// <summary>
    /// Prüft: die Paketierungs-Action existiert und pinnt die tvOS-Workload
    /// per Rollback-File auf die mit <c>build-tvos</c> deckungsgleiche
    /// Manifestversion.
    /// </summary>
    [Fact]
    public void PackageTvOsAction_PinsTvOsWorkloadViaRollback()
    {
        var action = ReadRepoFile(".github", "actions", "package-tvos", "action.yml");

        Assert.Contains("microsoft.net.sdk.tvos", action);
        Assert.Contains("workload-rollback.json", action);
        Assert.Contains("dotnet workload update --from-rollback-file", action);
        Assert.Contains("dotnet workload install tvos --skip-manifest-update", action);
        Assert.Contains("26.5.10318", action);
        Assert.Contains("Microsoft.tvOS.Sdk.net10.0_", action);
    }

    /// <summary>
    /// Prüft: die Action baut die Geräte-Architektur signiert und archiviert
    /// das Paket (ArchiveOnBuild), inklusive Codesign-Übergabe und
    /// <c>IncludeTvOsTarget</c>-Schalter.
    /// </summary>
    [Fact]
    public void PackageTvOsAction_BuildsSignedDeviceArchive()
    {
        var action = ReadRepoFile(".github", "actions", "package-tvos", "action.yml");

        Assert.Contains("VideoPlayer.TvOS/VideoPlayer.TvOS.csproj", action);
        Assert.Contains("tvos-arm64", action);
        Assert.Contains("ArchiveOnBuild=true", action);
        Assert.Contains("CodesignKey", action);
        Assert.Contains("CodesignProvision", action);
        Assert.Contains("IncludeTvOsTarget=true", action);
        Assert.Contains("net10.0-tvos", action);
    }

    /// <summary>
    /// Prüft: die Build-Nummerierung folgt der bestehenden Konvention
    /// (Commit-Anzahl plus Laufzähler), und die Action erzeugt
    /// <c>release-tvos.ipa</c> samt dSYM-Archiv und lädt per
    /// iTMSTransporter/API-Key hoch.
    /// </summary>
    [Fact]
    public void PackageTvOsAction_FollowsBuildNumberingAndUploadsPackage()
    {
        var action = ReadRepoFile(".github", "actions", "package-tvos", "action.yml");

        Assert.Contains("git rev-list --count HEAD", action);
        Assert.Contains("github.run_attempt", action);
        Assert.Contains("release-tvos.ipa", action);
        Assert.Contains("release-tvos-dsym.zip", action);
        Assert.Contains("iTMSTransporter", action);
        Assert.Contains("-m upload -assetFile", action);
        Assert.Contains(".appstoreconnect/private_keys", action);
        // Keychain-Aufräumen auch bei Fehlschlag.
        Assert.Contains("if: always()", action);
    }

    /// <summary>
    /// Prüft: beide Release-Workflows binden <c>package-tvos</c> hinter der
    /// Gate-Variable <c>TVOS_SIGNING_ENABLED</c> ein — ohne gesetztes Gate
    /// bleibt der Workflow grün.
    /// </summary>
    /// <param name="workflow">Dateiname des Release-Workflows.</param>
    [Theory]
    [InlineData("staging-merge.yml")]
    [InlineData("main-merge.yml")]
    public void ReleaseWorkflows_ContainGatedPackageTvOsJob(string workflow)
    {
        var content = ReadRepoFile(".github", "workflows", workflow);

        Assert.Contains("package-tvos:", content);
        Assert.Contains("vars.TVOS_SIGNING_ENABLED == 'true'", content);
        Assert.Contains("needs.release.outputs.released == 'true'", content);
        Assert.Contains("./.github/actions/package-tvos", content);
        Assert.Contains("secrets.TVOS_CODESIGN_KEY", content);
        Assert.Contains("secrets.TVOS_PROVISIONING_PROFILE", content);
        Assert.Contains("secrets.TVOS_API_KEY_P8", content);
        Assert.Contains("secrets.TVOS_CERTIFICATE_P12_BASE64", content);
        Assert.Contains("secrets.TVOS_PROVISIONING_PROFILE_BASE64", content);
        Assert.Contains("release-tvos.ipa", content);
    }

    /// <summary>
    /// Prüft: die iOS-Release-Pfade bleiben unverändert funktionsfähig —
    /// <c>package-ios</c>-Job, iOS-Gate und iOS-Secrets sind weiterhin
    /// verdrahtet.
    /// </summary>
    /// <param name="workflow">Dateiname des Release-Workflows.</param>
    [Theory]
    [InlineData("staging-merge.yml")]
    [InlineData("main-merge.yml")]
    public void ReleaseWorkflows_KeepIosPathIntact(string workflow)
    {
        var content = ReadRepoFile(".github", "workflows", workflow);

        Assert.Contains("package-ios:", content);
        Assert.Contains("vars.IOS_SIGNING_ENABLED == 'true'", content);
        Assert.Contains("./.github/actions/package-ios", content);
        Assert.Contains("secrets.IOS_CODESIGN_KEY", content);
        Assert.Contains("release-ios.ipa", content);
    }

    /// <summary>
    /// Prüft: das lokale Hilfsskript existiert und deckt die manuellen
    /// Aktionen ab (SSH-Delegation, Signatur-/Profil-Parameter,
    /// API-Key-Upload, Commit-Anzahl-Buildnummer).
    /// </summary>
    [Fact]
    public void TvOsDeploymentScript_CoversManualReleasePath()
    {
        var script = ReadRepoFile("scripts", "tvOS-Deployment.ps1");

        Assert.Contains("VIDEOPLAYER_TVOS_CODESIGN_KEY", script);
        Assert.Contains("VIDEOPLAYER_TVOS_PROVISIONING_PROFILE", script);
        Assert.Contains("VIDEOPLAYER_TVOS_MAC_SERVER_ADDRESS", script);
        Assert.Contains("VIDEOPLAYER_TVOS_API_KEY_PATH", script);
        Assert.Contains("VIDEOPLAYER_TVOS_API_KEY_ID", script);
        Assert.Contains("VIDEOPLAYER_TVOS_API_ISSUER_ID", script);
        Assert.Contains("VIDEOPLAYER_TVOS_MAC_REPO_PATH", script);
        Assert.Contains("tvos-arm64", script);
        Assert.Contains("tvossimulator-arm64", script);
        Assert.Contains("iTMSTransporter", script);
        Assert.Contains("rev-list --count HEAD", script);
        Assert.Contains("microsoft.net.sdk.tvos", script);
        Assert.Contains("ssh -o BatchMode=yes", script);
        Assert.Contains("ArchiveOnBuild=true", script);
    }

    /// <summary>
    /// Prüft: das tvOS-Projekt trägt die Store-Metadaten — Privacy-Manifest
    /// als <c>BundleResource</c> und die Export-Compliance-Deklaration in
    /// der Info.plist.
    /// </summary>
    [Fact]
    public void TvOsProject_CarriesStoreMetadata()
    {
        var csproj = ReadRepoFile("VideoPlayer.TvOS", "VideoPlayer.TvOS.csproj");
        var plist = ReadRepoFile("VideoPlayer.TvOS", "Info.plist");

        Assert.Contains("BundleResource Include=\"PrivacyInfo.xcprivacy\"", csproj);
        Assert.True(
            File.Exists(Path.Combine(Root, "VideoPlayer.TvOS", "PrivacyInfo.xcprivacy")),
            "VideoPlayer.TvOS/PrivacyInfo.xcprivacy fehlt");
        Assert.Contains("ITSAppUsesNonExemptEncryption", plist);
    }

    /// <summary>
    /// Prüft: die benutzerspezifische Deploy-Konfiguration des Skripts ist
    /// gitignored (Pendant zu <c>.ios-deploy.user.json</c>).
    /// </summary>
    [Fact]
    public void GitIgnore_CoversTvOsDeployPrefs()
    {
        var gitignore = ReadRepoFile(".gitignore");

        Assert.Contains(".tvos-deploy.user.json", gitignore);
    }
}
