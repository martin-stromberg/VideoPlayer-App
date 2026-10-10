using VideoPlayer.ApiClient.Discovery;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Server-Einrichtung beim Erststart und Serververwaltung („Server wechseln").
/// Bietet manuelle Adresseingabe, Netzwerk-Erkennung (mDNS + UDP) und die
/// Auswahl bekannter Server; übernommen wird nur ein erfolgreich geprüfter
/// Server. Der Zustand kommt vollständig aus dem MVU-Model.
/// </summary>
public partial class ServerSetupPage : ContentPage
{
    private readonly MvuHost mvuHost;
    private string? lastBootstrapError;
    private bool usernamePrefilled;

    /// <summary>Initialisiert eine neue Instanz der <see cref="ServerSetupPage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; liefert den Einrichtungszustand und nimmt die Benutzer-Nachrichten entgegen.</param>
    public ServerSetupPage(MvuHost mvuHost)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        // BindingContext für die XAML-Trigger des Status-Badges
        // (Model.ServerHealth); MvuHost meldet PropertyChanged("Model").
        BindingContext = mvuHost;
#if ANDROID || IOS
        // Kamera-Scan nur auf Mobilgeräten — Windows/MacCatalyst bekommen
        // den manuellen Einstieg über das Ticket-Feld (ZXing hat keinen
        // Windows-Kamera-Support; die App ist unpackaged).
        QrScanButton.IsVisible = true;
#endif
        UpdateUi(mvuHost.Model);
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        mvuHost.ModelChanged -= OnModelChanged;
        mvuHost.ModelChanged += OnModelChanged;
        // Beim Öffnen aus den Einstellungen die Serverliste aktualisieren.
        _ = mvuHost.DispatchAsync(new LoadServersRequestedMessage());
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
        ActiveServerSection.IsVisible = model.ActiveServerAddress is not null;
        ActiveServerLabel.Text = model.ActiveServerAddress;

        // Status-Badge des aktiven Servers: Text aus dem Model, die
        // Farben ergeben sich über die XAML-Trigger (Model.ServerHealth).
        ServerHealthLabel.Text = model.ServerHealthText;
        ServerHealthIndicator.IsRunning = model.ServerHealth == ServerHealthStatus.Checking;
        ServerHealthIndicator.IsVisible = model.ServerHealth == ServerHealthStatus.Checking;
        ServerHealthDot.IsVisible = model.ServerHealth != ServerHealthStatus.Checking;

        KnownServersView.ItemsSource = model.KnownServers;
        KnownServersHeader.IsVisible = model.KnownServers.Count > 0;
        KnownServersView.IsVisible = model.KnownServers.Count > 0;

        DiscoveredServersView.ItemsSource = model.DiscoveredServers;
        DiscoveredServersView.IsVisible = model.DiscoveredServers.Count > 0;
        DiscoveryIndicator.IsRunning = model.IsDiscoveryRunning;
        DiscoveryIndicator.IsVisible = model.IsDiscoveryRunning;
        DiscoveryRunningLabel.IsVisible = model.IsDiscoveryRunning;
        DiscoverButton.IsEnabled = !model.IsDiscoveryRunning;
        DiscoveryEmptyLabel.IsVisible =
            model.DiscoveryAttempted && !model.IsDiscoveryRunning && model.DiscoveredServers.Count == 0;

        CheckIndicator.IsRunning = model.IsServerCheckRunning;
        CheckIndicator.IsVisible = model.IsServerCheckRunning;
        // Nach erfolgreicher Prüfung läuft die Anzeige während der
        // automatischen Anmeldung weiter — der Text benennt die Phase.
        CheckStatusLabel.Text = model.ConnectionState == ConnectionState.Connecting
            ? "Verbindung wird hergestellt …"
            : "Verbindung wird geprüft …";
        CheckStatusLabel.IsVisible = model.IsServerCheckRunning;
        ConnectButton.IsEnabled = !model.IsServerCheckRunning;

        ErrorLabel.Text = model.ServerSetupError;
        ErrorLabel.IsVisible = model.ServerSetupError is not null;

        // Stufenlogik: Stufe 1 (Adresswahl) solange kein verbundener
        // Server auf eine Anmeldung wartet; Stufe 2 (Kurzcode oder
        // Zugangsdaten), sobald der geprüfte Server einen Nutzer-
        // Anmeldedurchgang oder ein Geräte-Pairing erwartet.
        var authChoice = model.ConnectionState == ConnectionState.NeedsLogin
            || model.IsPairingRequired;
        AddressStage.IsVisible = !authChoice;
        AuthStage.IsVisible = authChoice;

        // Pairing-Subsektion der Zugangsdaten-Kachel: nur sichtbar, wenn
        // der Server ein Geräte-Pairing verlangt — erst danach erscheint
        // die eigentliche Anmeldung im selben Kachel-Bereich.
        PairingSection.IsVisible = model.IsPairingRequired;
        PairingIndicator.IsRunning = model.IsPairingRunning;
        PairingIndicator.IsVisible = model.IsPairingRunning;
        PairingRunningLabel.IsVisible = model.IsPairingRunning;
        PairButton.IsEnabled = !model.IsPairingRunning;
        PairingCodeEntry.IsEnabled = !model.IsPairingRunning;
        PairingErrorLabel.Text = model.PairingError;
        PairingErrorLabel.IsVisible = model.PairingError is not null;

        CredentialsBlock.IsVisible = !model.IsPairingRequired;
        LoginIndicator.IsRunning = model.IsLoginRunning;
        LoginIndicator.IsVisible = model.IsLoginRunning;
        var loginEnabled = !model.IsLoginRunning && !model.IsBootstrapRunning;
        LoginButton.IsEnabled = loginEnabled;
        LoginUsernameEntry.IsEnabled = loginEnabled;
        LoginPasswordEntry.IsEnabled = loginEnabled;
        LoginErrorLabel.Text = model.LoginError;
        LoginErrorLabel.IsVisible = model.LoginError is not null;

        // Der bekannte Benutzername wird nur einmalig vorbefüllt, damit
        // spätere Model-Updates die Eingabe nicht überschreiben
        // (Muster: LoginPage).
        if (!usernamePrefilled && !string.IsNullOrEmpty(model.LoginUsername))
        {
            LoginUsernameEntry.Text = model.LoginUsername;
            usernamePrefilled = true;
        }

        // Stufe 2 — Kurzcode: laufender Bootstrap sperrt beide Wege —
        // Anmeldung und Kopplung konkurrieren um dieselbe Session.
        var shortCodeBusy = model.IsBootstrapRunning || model.IsLoginRunning;
        ShortCodeIndicator.IsRunning = model.IsBootstrapRunning;
        ShortCodeIndicator.IsVisible = model.IsBootstrapRunning;
        ShortCodeRunningLabel.IsVisible = model.IsBootstrapRunning;
        SetupShortCodeButton.IsEnabled = !shortCodeBusy;
        SetupShortCodeEntry.IsEnabled = !shortCodeBusy;
        ShortCodeErrorLabel.Text = model.BootstrapError;
        ShortCodeErrorLabel.IsVisible = model.BootstrapError is not null;

        // Kopplungs-Kachel der Stufe 1 (QR-/Ticket-Onboarding): während
        // einer Serverprüfung oder des Bootstraps bleibt die Eingabe
        // gesperrt.
        var bootstrapBusy = model.IsBootstrapRunning || model.IsServerCheckRunning;
        BootstrapIndicator.IsRunning = model.IsBootstrapRunning;
        BootstrapIndicator.IsVisible = model.IsBootstrapRunning;
        BootstrapRunningLabel.IsVisible = model.IsBootstrapRunning;
        BootstrapButton.IsEnabled = !bootstrapBusy;
        BootstrapTicketEntry.IsEnabled = !bootstrapBusy;
        QrScanButton.IsEnabled = !bootstrapBusy;
        BootstrapErrorLabel.Text = model.BootstrapError;
        BootstrapErrorLabel.IsVisible = model.BootstrapError is not null;

        // Nach dem QR-Scan kehrt die modale Scanner-Seite hierher zurück —
        // ein neuer Kopplungsfehler wird zusätzlich als Dialog gezeigt,
        // damit er nicht unbemerkt im Label untergeht.
        if (model.BootstrapError is not null && model.BootstrapError != lastBootstrapError)
        {
            _ = DisplayAlert("Kopplung fehlgeschlagen", model.BootstrapError, "OK");
        }
        lastBootstrapError = model.BootstrapError;
    }

    private void OnDiscoverClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new ServerDiscoveryRequestedMessage());
    }

    private void OnConnectClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new ServerAddressSubmittedMessage(AddressEntry.Text ?? string.Empty));
    }

    private void OnPairClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new PairingCodeSubmittedMessage(PairingCodeEntry.Text ?? string.Empty));
    }

    private void OnShortCodeClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new BootstrapTicketSubmittedMessage(SetupShortCodeEntry.Text ?? string.Empty));
    }

    private void OnSetupLoginClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new LoginSubmittedMessage(
            LoginUsernameEntry.Text ?? string.Empty,
            LoginPasswordEntry.Text ?? string.Empty));
    }

    private void OnBootstrapClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new BootstrapTicketSubmittedMessage(BootstrapTicketEntry.Text ?? string.Empty));
    }

    private async void OnQrScanClicked(object? sender, EventArgs e)
    {
#if ANDROID || IOS
        try
        {
            // Geräte ohne Kamera und verweigerte Permission werden lokal
            // abgefangen — der Kopplungs-Fluss startet erst mit einem
            // gelesenen Code, daher kein Model-Fehler.
            if (!ZXing.Net.Maui.BarcodeScanning.IsSupported)
            {
                await DisplayAlert(
                    "Kamera nicht verfügbar",
                    "Dieses Gerät hat keine nutzbare Kamera. Bitte die Ticket-Adresse oder den Kurzcode eingeben.",
                    "OK");
                return;
            }

            var permission = await Permissions.RequestAsync<Permissions.Camera>();
            if (permission != PermissionStatus.Granted)
            {
                await DisplayAlert(
                    "Kamera-Zugriff fehlt",
                    "Bitte den Kamera-Zugriff erlauben oder die Ticket-Adresse bzw. den Kurzcode eingeben.",
                    "OK");
                return;
            }

            await Navigation.PushModalAsync(new QrScannerPage(mvuHost));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ServerSetupPage] QR-Scan konnte nicht gestartet werden: {ex.Message}");
            await DisplayAlert(
                "Scan nicht möglich",
                "Der QR-Scan konnte nicht gestartet werden. Bitte die Ticket-Adresse oder den Kurzcode eingeben.",
                "OK");
        }
#else
        await Task.CompletedTask;
#endif
    }

    private void OnDiscoveredSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is DiscoveredServer server)
        {
            ((CollectionView)sender!).SelectedItem = null;
            _ = mvuHost.DispatchAsync(new DiscoveredServerSelectedMessage(server.Address, server.Name));
        }
    }

    private void OnKnownSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is KnownServer server)
        {
            ((CollectionView)sender!).SelectedItem = null;
            _ = mvuHost.DispatchAsync(new ServerSwitchRequestedMessage(server.Address, server.DisplayName));
        }
    }
}
