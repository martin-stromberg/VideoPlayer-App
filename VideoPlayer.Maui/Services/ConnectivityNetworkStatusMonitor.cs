using Microsoft.Extensions.Logging;
using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// Netzwerkstatus-Erkennung über <see cref="Connectivity"/>
/// (<c>Microsoft.Maui.Networking</c>): <see cref="IsOnline"/> bildet
/// <see cref="NetworkAccess.Internet"/> und
/// <see cref="NetworkAccess.ConstrainedInternet"/> als online ab;
/// <see cref="IsWifi"/> prüft die Verbindungsprofile auf
/// <see cref="ConnectionProfile.WiFi"/>; <c>ConnectivityChanged</c>
/// meldet jeden Wechsel.
/// </summary>
public sealed class ConnectivityNetworkStatusMonitor : INetworkStatusMonitor
{
    private readonly ILogger<ConnectivityNetworkStatusMonitor> logger;
    private bool started;

    /// <summary>Initialisiert eine neue Instanz der <see cref="ConnectivityNetworkStatusMonitor"/>-Klasse.</summary>
    /// <param name="logger">Logger für das Programmprotokoll.</param>
    public ConnectivityNetworkStatusMonitor(ILogger<ConnectivityNetworkStatusMonitor> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public bool IsOnline =>
        Connectivity.Current.NetworkAccess is NetworkAccess.Internet or NetworkAccess.ConstrainedInternet;

    /// <inheritdoc/>
    public bool IsWifi =>
        // Auch kabelgebundenes Ethernet zählt: die Einstellung soll
        // getaktete Mobilfunk-Verbindungen schonen, eine Desktop-
        // Festnetz-Anbindung ist dafür mindestens so geeignet wie WLAN.
        Connectivity.Current.ConnectionProfiles.Any(p =>
            p is ConnectionProfile.WiFi or ConnectionProfile.Ethernet);

    /// <inheritdoc/>
    public event EventHandler<bool>? AvailabilityChanged;

    /// <inheritdoc/>
    public void Start()
    {
        if (started)
        {
            return;
        }

        started = true;
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        var isOnline = IsOnline;
        logger.LogInformation(
            "Netzverfügbarkeit geändert: {IsOnline} (WLAN/Ethernet: {IsWifi}).",
            isOnline ? "online" : "offline",
            IsWifi);
        AvailabilityChanged?.Invoke(this, isOnline);
    }
}
