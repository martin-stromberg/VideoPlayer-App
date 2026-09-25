using VideoPlayer.Storage;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>Tests für <c>AppSecretsService</c>.</summary>
public class AppSecretsServiceTests
{
    private static AppSecretsService CreateService(
        InMemorySettingsStore settings, InMemorySecretStore secrets, string? profile = null)
    {
        return new AppSecretsService(settings, secrets, AppProfile.Create(profile));
    }

    /// <summary>Prüft Secrets: landen ausschließlich im SecretStore.</summary>
    [Fact]
    public async Task Secrets_GoToSecretStore_Only()
    {
        var settings = new InMemorySettingsStore();
        var secrets = new InMemorySecretStore();
        var service = CreateService(settings, secrets);

        await service.SetCredentialsAsync("user", "secret-password");
        await service.SetAuthTokenAsync("jwt-token");

        // Geheimnisse liegen ausschließlich in der sicheren Ablage.
        Assert.Contains(AppSecretsService.PasswordKey, secrets.Keys);
        Assert.Contains(AppSecretsService.AuthTokenKey, secrets.Keys);
        Assert.DoesNotContain(AppSecretsService.PasswordKey, settings.Keys);
        Assert.DoesNotContain(AppSecretsService.AuthTokenKey, settings.Keys);
    }

    /// <summary>Prüft Username: wird im SettingsStore abgelegt.</summary>
    [Fact]
    public async Task Username_GoesToSettingsStore()
    {
        var settings = new InMemorySettingsStore();
        var secrets = new InMemorySecretStore();
        var service = CreateService(settings, secrets);

        await service.SetCredentialsAsync("user", "pw");

        Assert.Equal("user", settings.GetString(AppSecretsService.UsernameKey, string.Empty));
        Assert.Equal("user", service.GetUsername());
    }

    /// <summary>Prüft SetAuthToken: entfernt die Legacy-Preference.</summary>
    [Fact]
    public async Task SetAuthToken_RemovesLegacyPreference()
    {
        var settings = new InMemorySettingsStore();
        var secrets = new InMemorySecretStore();
        var service = CreateService(settings, secrets);
        settings.Set(AppSecretsService.AuthTokenKey, "legacy-token");

        await service.SetAuthTokenAsync("new-token");

        Assert.False(settings.Contains(AppSecretsService.AuthTokenKey));
        Assert.Equal("new-token", await service.GetAuthTokenAsync());
    }

    /// <summary>Prüft GetAuthToken: migriert die Legacy-Preference.</summary>
    [Fact]
    public async Task GetAuthToken_MigratesLegacyPreference()
    {
        var settings = new InMemorySettingsStore();
        var secrets = new InMemorySecretStore();
        var service = CreateService(settings, secrets);
        settings.Set(AppSecretsService.AuthTokenKey, "legacy-token");

        var token = await service.GetAuthTokenAsync();

        Assert.Equal("legacy-token", token);
        Assert.False(settings.Contains(AppSecretsService.AuthTokenKey));
        Assert.Equal("legacy-token", await secrets.GetAsync(AppSecretsService.AuthTokenKey));
    }

    /// <summary>Prüft ClearCredentials: entfernt alle Zugangsdaten.</summary>
    [Fact]
    public async Task ClearCredentials_RemovesAll()
    {
        var settings = new InMemorySettingsStore();
        var secrets = new InMemorySecretStore();
        var service = CreateService(settings, secrets);
        await service.SetCredentialsAsync("user", "pw");
        await service.SetAuthTokenAsync("token");

        await service.ClearCredentialsAsync();

        Assert.Equal(string.Empty, service.GetUsername());
        Assert.Equal(string.Empty, await service.GetPasswordAsync());
        Assert.Null(await service.GetAuthTokenAsync());
        Assert.Empty(secrets.Keys);
    }

    /// <summary>Prüft Secrets: sind pro Profil isoliert.</summary>
    [Fact]
    public async Task Secrets_AreProfileIsolated()
    {
        var settings = new InMemorySettingsStore();
        var secrets = new InMemorySecretStore();
        var defaultService = CreateService(settings, secrets);
        var otherService = CreateService(settings, secrets, "second");

        await defaultService.SetCredentialsAsync("user-a", "pw-a");
        await otherService.SetCredentialsAsync("user-b", "pw-b");
        await defaultService.SetAuthTokenAsync("token-a");
        await otherService.SetAuthTokenAsync("token-b");

        Assert.Equal("user-a", defaultService.GetUsername());
        Assert.Equal("user-b", otherService.GetUsername());
        Assert.Equal("pw-a", await defaultService.GetPasswordAsync());
        Assert.Equal("pw-b", await otherService.GetPasswordAsync());
        Assert.Equal("token-a", await defaultService.GetAuthTokenAsync());
        Assert.Equal("token-b", await otherService.GetAuthTokenAsync());
        Assert.Contains($"second_{AppSecretsService.AuthTokenKey}", secrets.Keys);
    }

    /// <summary>Prüft HasCredentials: erfordert Benutzername und Passwort.</summary>
    [Fact]
    public async Task HasCredentials_RequiresUsernameAndPassword()
    {
        var settings = new InMemorySettingsStore();
        var secrets = new InMemorySecretStore();
        var service = CreateService(settings, secrets);

        Assert.False(await service.HasCredentialsAsync());
        await service.SetCredentialsAsync("user", "pw");
        Assert.True(await service.HasCredentialsAsync());
    }

    /// <summary>Prüft Geräte-Token: landen ausschließlich im SecretStore (nie in den Einstellungen).</summary>
    [Fact]
    public async Task DeviceToken_GoesToSecretStore_Only()
    {
        var settings = new InMemorySettingsStore();
        var secrets = new InMemorySecretStore();
        var service = CreateService(settings, secrets);

        await service.SetDeviceTokenAsync("http://nas/videoplayer/", "dev-token");

        var key = Assert.Single(secrets.Keys);
        Assert.Contains(AppSecretsService.DeviceTokenKey, key);
        Assert.Empty(settings.Keys);
        Assert.Equal("dev-token", await service.GetDeviceTokenAsync("http://nas/videoplayer/"));
        Assert.True(await service.HasDeviceTokenAsync("http://nas/videoplayer/"));
    }

    /// <summary>Prüft Geräte-Token: sind pro Serveradresse isoliert.</summary>
    [Fact]
    public async Task SetDeviceToken_IsolatedPerServer()
    {
        var settings = new InMemorySettingsStore();
        var secrets = new InMemorySecretStore();
        var service = CreateService(settings, secrets);

        await service.SetDeviceTokenAsync("http://nas/", "token-nas");
        await service.SetDeviceTokenAsync("http://cloud/", "token-cloud");

        Assert.Equal("token-nas", await service.GetDeviceTokenAsync("http://nas/"));
        Assert.Equal("token-cloud", await service.GetDeviceTokenAsync("http://cloud/"));
        Assert.Equal(2, secrets.Keys.Count);
    }

    /// <summary>Prüft Geräte-Token: sind pro Profil isoliert.</summary>
    [Fact]
    public async Task DeviceToken_IsProfileIsolated()
    {
        var settings = new InMemorySettingsStore();
        var secrets = new InMemorySecretStore();
        var defaultService = CreateService(settings, secrets);
        var otherService = CreateService(settings, secrets, "second");

        await defaultService.SetDeviceTokenAsync("http://nas/", "token-default");
        await otherService.SetDeviceTokenAsync("http://nas/", "token-second");

        Assert.Equal("token-default", await defaultService.GetDeviceTokenAsync("http://nas/"));
        Assert.Equal("token-second", await otherService.GetDeviceTokenAsync("http://nas/"));
    }

    /// <summary>Prüft Geräte-Token: die Serveradresse wird für den Ablageschlüssel normalisiert.</summary>
    [Fact]
    public async Task DeviceToken_NormalizesServerAddress()
    {
        var settings = new InMemorySettingsStore();
        var secrets = new InMemorySecretStore();
        var service = CreateService(settings, secrets);

        await service.SetDeviceTokenAsync("NAS.local/video", "dev-token");

        // Kanonische Form liefert denselben Schlüssel.
        Assert.Equal("dev-token", await service.GetDeviceTokenAsync("http://nas.local/video/"));
        Assert.Equal("dev-token", await service.GetDeviceTokenAsync("HTTP://NAS.LOCAL/video"));
    }

    /// <summary>Prüft ClearDeviceToken: entfernt nur das Token des angegebenen Servers.</summary>
    [Fact]
    public async Task ClearDeviceToken_RemovesOnlyThatServer()
    {
        var settings = new InMemorySettingsStore();
        var secrets = new InMemorySecretStore();
        var service = CreateService(settings, secrets);
        await service.SetDeviceTokenAsync("http://nas/", "token-nas");
        await service.SetDeviceTokenAsync("http://cloud/", "token-cloud");

        await service.ClearDeviceTokenAsync("http://nas/");

        Assert.Null(await service.GetDeviceTokenAsync("http://nas/"));
        Assert.False(await service.HasDeviceTokenAsync("http://nas/"));
        Assert.Equal("token-cloud", await service.GetDeviceTokenAsync("http://cloud/"));
    }

    /// <summary>Prüft GetDeviceToken: ohne Eintrag → null.</summary>
    [Fact]
    public async Task GetDeviceToken_WithoutEntry_ReturnsNull()
    {
        var service = CreateService(new InMemorySettingsStore(), new InMemorySecretStore());

        Assert.Null(await service.GetDeviceTokenAsync("http://nas/"));
        Assert.False(await service.HasDeviceTokenAsync("http://nas/"));
    }

    /// <summary>Prüft Refresh-Token: landet ausschließlich im SecretStore (nie in den Einstellungen).</summary>
    [Fact]
    public async Task RefreshToken_GoesToSecretStore_Only()
    {
        var settings = new InMemorySettingsStore();
        var secrets = new InMemorySecretStore();
        var service = CreateService(settings, secrets);

        await service.SetRefreshTokenAsync("refresh-1");

        Assert.Contains(AppSecretsService.RefreshTokenKey, secrets.Keys);
        Assert.DoesNotContain(AppSecretsService.RefreshTokenKey, settings.Keys);
        Assert.Equal("refresh-1", await service.GetRefreshTokenAsync());
    }

    /// <summary>Prüft Refresh-Token: die Rotation ersetzt den Vorgänger-Wert.</summary>
    [Fact]
    public async Task SetRefreshToken_Rotation_ReplacesValue()
    {
        var service = CreateService(new InMemorySettingsStore(), new InMemorySecretStore());

        await service.SetRefreshTokenAsync("refresh-alt");
        await service.SetRefreshTokenAsync("refresh-neu");

        Assert.Equal("refresh-neu", await service.GetRefreshTokenAsync());
    }

    /// <summary>Prüft Refresh-Token: ist pro Profil isoliert.</summary>
    [Fact]
    public async Task RefreshToken_IsProfileIsolated()
    {
        var settings = new InMemorySettingsStore();
        var secrets = new InMemorySecretStore();
        var defaultService = CreateService(settings, secrets);
        var otherService = CreateService(settings, secrets, "second");

        await defaultService.SetRefreshTokenAsync("refresh-default");
        await otherService.SetRefreshTokenAsync("refresh-second");

        Assert.Equal("refresh-default", await defaultService.GetRefreshTokenAsync());
        Assert.Equal("refresh-second", await otherService.GetRefreshTokenAsync());
        Assert.Contains($"second_{AppSecretsService.RefreshTokenKey}", secrets.Keys);
    }

    /// <summary>Prüft ClearCredentials: entfernt auch das Refresh-Token der Sitzung.</summary>
    [Fact]
    public async Task ClearCredentials_RemovesRefreshToken()
    {
        var secrets = new InMemorySecretStore();
        var service = CreateService(new InMemorySettingsStore(), secrets);
        await service.SetAuthTokenAsync("token");
        await service.SetRefreshTokenAsync("refresh-1");

        await service.ClearCredentialsAsync();

        Assert.Null(await service.GetRefreshTokenAsync());
        Assert.Empty(secrets.Keys);
    }

    /// <summary>Prüft GetRefreshToken: ohne Eintrag → null.</summary>
    [Fact]
    public async Task GetRefreshToken_WithoutEntry_ReturnsNull()
    {
        var service = CreateService(new InMemorySettingsStore(), new InMemorySecretStore());

        Assert.Null(await service.GetRefreshTokenAsync());
    }
}
