using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls;
using VideoWebPlayer.Maui.Services;

namespace VideoWebPlayer.Maui;

/// <summary>
/// Login-Seite zur Eingabe von Benutzername und Passwort für die Server-Anmeldung.
/// </summary>
public partial class LoginPage : ContentPage
{
    private readonly IAuthService _auth;
    private readonly ISettingsService _settings;

    /// <summary>
    /// Erstellt eine neue Instanz der <see cref="LoginPage"/>.
    /// </summary>
    /// <param name="auth">Der Authentifizierungsdienst; bei <c>null</c> wird ein Standard-<see cref="Services.AuthService"/> verwendet.</param>
    /// <param name="settings">Der Einstellungsdienst; bei <c>null</c> wird er aus dem ServiceProvider geholt.</param>
    public LoginPage(IAuthService? auth, ISettingsService? settings = null)
    {
        InitializeComponent();
        // tolerate null auth service when constructed outside DI
        _auth = auth ?? new Services.AuthService();
        _settings = settings ?? App.ServiceProvider?.GetService<ISettingsService>() ?? new Services.SettingsService();

        if (_auth.HasCredentials())
        {
            // prefill
            // not exposing stored password in this placeholder
            UserEntry.Text = _settings.GetUsername();
        }
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        var user = UserEntry.Text?.Trim();
        var pass = PasswordEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            StatusLabel.Text = "Bitte Benutzername und Passwort eingeben.";
            return;
        }

        StatusLabel.Text = "Anmeldung...";
        var (ok, error) = await _auth.LoginAsync(user!, pass!);
        if (ok)
        {
            await Navigation.PopModalAsync();
            // Ensure the startup workflow continues (in case it was waiting for credentials).
            try
            {
                var mainPage = Application.Current?.Windows?.FirstOrDefault()?.Page;
                if (mainPage is NavigationPage navPage)
                    mainPage = navPage.CurrentPage;

                if (mainPage is MainPage mp)
                {
                    mp.RetryStartupWorkflow();
                }
            }
            catch { }
        }
        else
        {
            StatusLabel.Text = string.IsNullOrWhiteSpace(error) ? "Anmeldung fehlgeschlagen." : error;
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        // allow user to exit the app
        System.Environment.Exit(0);
    }
}
