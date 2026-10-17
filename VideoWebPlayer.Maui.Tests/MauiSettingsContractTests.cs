namespace VideoWebPlayer.Maui.Tests;

/// <summary>Vertragstests für <c>MauiSettings</c> (Quelltext-/Verhaltensvertrag).</summary>
public class MauiSettingsContractTests
{
    /// <summary>Prüft MauiAuthAndProfileConsumers: umgehen den SettingsService nicht.</summary>
    /// <param name="relativePath">Der relative Pfad.</param>
    [Theory]
    [InlineData("VideoWebPlayer.Maui/Services/AuthService.cs")]
    [InlineData("VideoWebPlayer.Maui/Services/ConnectionService.cs")]
    [InlineData("VideoWebPlayer.Maui/Services/DownloadManager.cs")]
    [InlineData("VideoWebPlayer.Maui/Services/DownloadQueue.cs")]
    [InlineData("VideoWebPlayer.Maui/LoginPage.xaml.cs")]
    public void MauiAuthAndProfileConsumers_DoNotBypassSettingsService(string relativePath)
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));

        Assert.DoesNotContain("Preferences.Default.Get(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Preferences.Default.Set(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Preferences.Default.Remove(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SecureStorage.Default.GetAsync(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SecureStorage.Default.SetAsync(", source, StringComparison.Ordinal);
    }

    /// <summary>Prüft SettingsService: speichert sensible Werte im profilbewussten SecureStorage.</summary>
    [Fact]
    public void SettingsService_StoresSensitiveValuesInProfileAwareSecureStorage()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "VideoWebPlayer.Maui/Services/SettingsService.cs"));

        Assert.Contains("private const string AuthTokenKey = \"AuthToken\";", source, StringComparison.Ordinal);
        Assert.Contains("private const string PasswordKey = \"Passkey\";", source, StringComparison.Ordinal);
        Assert.Contains("GetPrefixedKey(AuthTokenKey)", source, StringComparison.Ordinal);
        Assert.Contains("GetPrefixedKey(PasswordKey)", source, StringComparison.Ordinal);
        Assert.Contains("SecureStorage.Default.GetAsync(key)", source, StringComparison.Ordinal);
        Assert.Contains("SecureStorage.Default.SetAsync(key, value)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Preferences.Default.Set(GetPrefixedKey(AuthTokenKey)", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VideoPlayer.App.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
