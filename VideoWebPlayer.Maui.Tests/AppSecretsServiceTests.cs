using VideoPlayer.Storage;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

public class AppSecretsServiceTests
{
    private static AppSecretsService CreateService(
        InMemorySettingsStore settings, InMemorySecretStore secrets, string? profile = null)
    {
        return new AppSecretsService(settings, secrets, AppProfile.Create(profile));
    }

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
}
