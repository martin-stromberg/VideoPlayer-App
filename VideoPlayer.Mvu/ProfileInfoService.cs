using System.Text;
using System.Text.Json;
using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>
/// Ermittelt die Anzeigeinformationen der Profil-Seite: Benutzername und
/// E-Mail des angemeldeten Server-Benutzers sowie das aktive lokale
/// Profil. Die Server-API besitzt keinen Benutzerinfo-Endpunkt — die
/// nächstliegende dokumentierte Quelle sind die Claims des gespeicherten
/// Anmelde-Tokens (JWT-Payload, rein lesend dekodiert); der gespeicherte
/// Anmelde-Benutzername dient als Fallback.
/// </summary>
public sealed class ProfileInfoService
{
    private readonly AppSecretsService secrets;
    private readonly AppProfile profile;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="secrets">Ablage der Zugangsdaten und Anmelde-Token.</param>
    /// <param name="profile">Aktives lokales Profil.</param>
    public ProfileInfoService(AppSecretsService secrets, AppProfile profile)
    {
        this.secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    /// <summary>
    /// Liest gespeicherten Benutzernamen und Anmelde-Token und liefert die
    /// anzeigbaren Profilinformationen. Token-Claims haben Vorrang; ohne
    /// lesbares Token wird der gespeicherte Anmelde-Benutzername (die
    /// Anmelde-Kennung) für Benutzername und E-Mail übernommen.
    /// </summary>
    /// <param name="cancellationToken">Bricht das Lesen ab.</param>
    /// <returns>Die ermittelten Profilinformationen.</returns>
    public async Task<ProfileInfo> LoadAsync(CancellationToken cancellationToken = default)
    {
        var storedUsername = NullIfEmpty(secrets.GetUsername());
        var token = await secrets.GetAuthTokenAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var (username, email) = ReadTokenClaims(token);
        return new ProfileInfo(
            username ?? storedUsername,
            email ?? storedUsername,
            profile.Name);
    }

    /// <summary>
    /// Dekodiert die Payload eines JWT (Base64Url) und liest die
    /// Anzeige-Claims — Benutzername aus <c>unique_name</c>/<c>name</c>/
    /// <c>preferred_username</c> (inkl. URI-Claim-Form), E-Mail aus
    /// <c>email</c> (inkl. URI-Claim-Form). Beschädigte oder
    /// unkonventionelle Tokens liefern leere Werte — die Dekodierung ist
    /// rein informativ, eine Signaturprüfung ist nicht erforderlich.
    /// </summary>
    /// <param name="token">Das gespeicherte Anmelde-Token; <c>null</c>/leer = keine Claims.</param>
    /// <returns>Die gelesenen Claims als Tupel (Benutzername, E-Mail); beide Elemente <c>null</c>, wenn nicht lesbar.</returns>
    internal static (string?, string?) ReadTokenClaims(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return (null, null);
        }

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                return (null, null);
            }

            var base64 = parts[1].Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            using var document = JsonDocument.Parse(
                Encoding.UTF8.GetString(Convert.FromBase64String(base64)));
            var root = document.RootElement;

            var username = Claim(
                root,
                "unique_name",
                "name",
                "preferred_username",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
            var email = Claim(
                root,
                "email",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
            return (username, email);
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentException or InvalidOperationException)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileInfoService] Token-Claims nicht lesbar: {ex.Message}");
            return (null, null);
        }
    }

    private static string? Claim(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                var text = NullIfEmpty(value.GetString());
                if (text is not null)
                {
                    return text;
                }
            }
        }

        return null;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
