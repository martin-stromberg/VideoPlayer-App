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

    /// <summary>Initialisiert eine neue Instanz der <see cref="ServerSetupPage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; liefert den Einrichtungszustand und nimmt die Benutzer-Nachrichten entgegen.</param>
    public ServerSetupPage(MvuHost mvuHost)
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

        KnownServersView.ItemsSource = model.KnownServers;
        KnownServersHeader.IsVisible = model.KnownServers.Count > 0;
        KnownServersView.IsVisible = model.KnownServers.Count > 0;

        DiscoveredServersView.ItemsSource = model.DiscoveredServers;
        DiscoveredServersView.IsVisible = model.DiscoveredServers.Count > 0;
        DiscoveryIndicator.IsRunning = model.IsDiscoveryRunning;
        DiscoveryIndicator.IsVisible = model.IsDiscoveryRunning;
        DiscoverButton.IsEnabled = !model.IsDiscoveryRunning;
        DiscoveryEmptyLabel.IsVisible =
            model.DiscoveryAttempted && !model.IsDiscoveryRunning && model.DiscoveredServers.Count == 0;

        CheckIndicator.IsRunning = model.IsServerCheckRunning;
        CheckIndicator.IsVisible = model.IsServerCheckRunning;
        ConnectButton.IsEnabled = !model.IsServerCheckRunning;

        ErrorLabel.Text = model.ServerSetupError;
        ErrorLabel.IsVisible = model.ServerSetupError is not null;

        // Pairing-Abschnitt: nur sichtbar, wenn der Server ein
        // Geräte-Pairing verlangt; Fehler laufen in einem eigenen
        // Feld (getrennt von Adress-/Erreichbarkeitsfehlern).
        PairingSection.IsVisible = model.IsPairingRequired;
        PairingIndicator.IsRunning = model.IsPairingRunning;
        PairingIndicator.IsVisible = model.IsPairingRunning;
        PairButton.IsEnabled = !model.IsPairingRunning;
        PairingCodeEntry.IsEnabled = !model.IsPairingRunning;
        PairingErrorLabel.Text = model.PairingError;
        PairingErrorLabel.IsVisible = model.PairingError is not null;
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
