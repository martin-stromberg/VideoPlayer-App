#nullable enable
#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Text;
using System.Text.Json;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// <see cref="ProfileInfoService"/> (Schritt 9): liest Benutzername und
/// E-Mail des angemeldeten Server-Benutzers aus den Claims des
/// gespeicherten Anmelde-Tokens (die API besitzt keinen
/// Benutzerinfo-Endpunkt) mit Fallback auf die gespeicherte
/// Anmelde-Kennung, plus das aktive lokale Profil.
/// </summary>
public class ProfileInfoServiceTests
{
    private static string FakeJwt(object payload)
    {
        static string B64(object value) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return $"{B64(new { alg = "none", typ = "JWT" })}.{B64(payload)}.signatur";
    }

    private static ProfileInfoService CreateService(
        AppSecretsService secrets,
        string? profileName = null) =>
        new(secrets, AppProfile.Create(profileName));

    private static AppSecretsService CreateSecrets() =>
        new(new InMemorySettingsStore(), new InMemorySecretStore(), AppProfile.Create(null));

    /// <summary>Prüft LoadAsync: liest Benutzername und E-Mail aus den Token-Claims.</summary>
    [Fact]
    public async Task LoadAsync_ReadsTokenClaims()
    {
        var secrets = CreateSecrets();
        await secrets.SetAuthTokenAsync(FakeJwt(new
        {
            sub = "7",
            nameid = "7",
            unique_name = "martin",
            email = "martin@server",
            IsAdmin = "False"
        }));

        var info = await CreateService(secrets).LoadAsync();

        Assert.Equal("martin", info.Username);
        Assert.Equal("martin@server", info.Email);
        Assert.Equal(AppProfile.DefaultName, info.ProfileName);
    }

    /// <summary>Prüft LoadAsync: bei <c>FallbackToStoredUsername</c> → übernimmt die gespeicherte Anmelde-Kennung.</summary>
    [Fact]
    public async Task LoadAsync_FallsBackToStoredUsername()
    {
        // Beschädigtes/fehlendes Token → die gespeicherte Anmelde-Kennung
        // ist die einzige Quelle (sie ist zugleich die Login-E-Mail).
        var secrets = CreateSecrets();
        await secrets.SetCredentialsAsync("martin@server", "pw");
        await secrets.SetAuthTokenAsync("kein.gueltiges.jwt");

        var info = await CreateService(secrets).LoadAsync();

        Assert.Equal("martin@server", info.Username);
        Assert.Equal("martin@server", info.Email);
    }

    /// <summary>Prüft LoadAsync: ohne Token und Kennung → leere Anzeigewerte, Profilname bleibt.</summary>
    [Fact]
    public async Task LoadAsync_WithoutTokenAndCredentials_ReturnsNulls()
    {
        var secrets = CreateSecrets();

        var info = await CreateService(secrets, "winter").LoadAsync();

        Assert.Null(info.Username);
        Assert.Null(info.Email);
        Assert.Equal("winter", info.ProfileName);
    }

    /// <summary>Prüft ReadTokenClaims: akzeptiert die URI-Claim-Formen.</summary>
    [Fact]
    public void ReadTokenClaims_AcceptsUriClaimForms()
    {
        var (username, email) = ProfileInfoService.ReadTokenClaims(FakeJwt(new Dictionary<string, object>
        {
            ["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] = "uri-user",
            ["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] = "uri@server"
        }));

        Assert.Equal("uri-user", username);
        Assert.Equal("uri@server", email);
    }

    /// <summary>Prüft ReadTokenClaims: bei <c>CorruptToken</c> → gibt leere Werte zurück.</summary>
    [Fact]
    public void ReadTokenClaims_CorruptToken_ReturnsNulls()
    {
        Assert.Equal((null, null), ProfileInfoService.ReadTokenClaims(null));
        Assert.Equal((null, null), ProfileInfoService.ReadTokenClaims("einteilig"));
        Assert.Equal((null, null), ProfileInfoService.ReadTokenClaims("a.!!!.c"));
        Assert.Equal((null, null), ProfileInfoService.ReadTokenClaims("a.bmFjdGU=.c")); // payload ohne JSON-Objekt
    }
}
