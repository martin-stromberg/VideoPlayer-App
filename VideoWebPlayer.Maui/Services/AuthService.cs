using VideoWebPlayer.Client;
using VideoWebPlayer.Client.Models;

namespace VideoWebPlayer.Maui.Services;

public class AuthService : IAuthService
{
    private readonly ISettingsService _settings;

	public AuthService() : this(new SettingsService())
	{
	}

	public AuthService(ISettingsService settings)
	{
		_settings = settings;
	}

    public bool HasCredentials()
    {
        return _settings.HasCredentials();
    }
    
    public (string username, string password) GetCredentials()
    {
        return (_settings.GetUsername(), _settings.GetPassword());
    }

    public void SaveCredentials(string username, string password)
    {
        _settings.SetCredentials(username, password);
    }

    public void ClearCredentials()
    {
        _settings.ClearCredentials();
    }

    public async Task<(bool success, string? errorMessage)> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var serverAddress = _settings.ServerAddress;
        if (string.IsNullOrWhiteSpace(serverAddress))
            return (false, "Keine Serveradresse gesetzt. Bitte Server in den Einstellungen angeben.");

        try
        {
            // Verwende den DI-registrierten MauiVideoWebPlayerClient
            var client = App.ServiceProvider?.GetService<VideoWebPlayerClient>();
            if (client == null)
            {
                // Fallback: Erstelle temporären Client für Login
                var httpClient = new HttpClient { BaseAddress = new Uri(serverAddress) };
                MauiClientConfiguration.ApplyClientGateHeader(httpClient);
                var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<VideoWebPlayerClient>();
                client = new VideoWebPlayerClient(httpClient, logger);
            }
            
            var token = await client.AuthenticateAsync(username, password);
            if (token != null && !string.IsNullOrWhiteSpace(token.token))
            {
                _settings.SetAuthToken(token.token);

                // Lade Token neu in den DI-Client
                if (client is MauiVideoWebPlayerClient mauiClient)
                {
                    mauiClient.ReloadAuthToken();
                }

                SaveCredentials(username, password);
                return (true, null);
            }
            else
            {
                // Authentication failed but no exception -> invalid credentials
                return (false, "Ungültiger Benutzername oder Passwort.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login failed: {ex.Message}");
            return (false, $"Fehler bei der Anmeldung: {ex.Message}");
        }
    }

}
