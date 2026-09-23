using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Anmeldemaske des Verbindungs-Workflows. Bietet Benutzername/Passwort-
/// Eingabe, sichtbaren Laufzustand und Inline-Fehlertext sowie den Ausstieg
/// zur Servereinrichtung. Der Zustand kommt vollständig aus dem MVU-Model;
/// der Benutzername wird einmalig aus dem bekannten Wert vorbefüllt.
/// </summary>
public partial class LoginPage : ContentPage
{
    private readonly MvuHost mvuHost;
    private bool usernamePrefilled;

    /// <summary>Initialisiert eine neue Instanz der <see cref="LoginPage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; liefert den Anmeldezustand und nimmt die Benutzer-Nachrichten entgegen.</param>
    public LoginPage(MvuHost mvuHost)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        UpdateUi(mvuHost.Model);
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        mvuHost.ModelChanged -= OnModelChanged;
        mvuHost.ModelChanged += OnModelChanged;
    }

    /// <inheritdoc/>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        mvuHost.ModelChanged -= OnModelChanged;
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => UpdateUi(mvuHost.Model));
    }

    private void UpdateUi(AppModel model)
    {
        // Der bekannte Benutzername wird nur einmalig vorbefüllt, damit
        // spätere Model-Updates die Eingabe nicht überschreiben.
        if (!usernamePrefilled && !string.IsNullOrEmpty(model.LoginUsername))
        {
            UsernameEntry.Text = model.LoginUsername;
            usernamePrefilled = true;
        }

        ServerLabel.IsVisible = model.ActiveServerAddress is not null;
        ServerLabel.Text = model.ActiveServerAddress is null
            ? string.Empty
            : $"Server: {model.ActiveServerAddress}";

        LoginIndicator.IsRunning = model.IsLoginRunning;
        LoginIndicator.IsVisible = model.IsLoginRunning;
        LoginButton.IsEnabled = !model.IsLoginRunning;
        UsernameEntry.IsEnabled = !model.IsLoginRunning;
        PasswordEntry.IsEnabled = !model.IsLoginRunning;
        ServerSetupButton.IsEnabled = !model.IsLoginRunning;

        ErrorLabel.Text = model.LoginError;
        ErrorLabel.IsVisible = model.LoginError is not null;
    }

    private void OnLoginClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new LoginSubmittedMessage(
            UsernameEntry.Text ?? string.Empty,
            PasswordEntry.Text ?? string.Empty));
    }

    private void OnServerSetupClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new NavigateToMessage(AppPage.ServerSetup));
    }
}
