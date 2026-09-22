using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// Netzwerkstatus-Erkennung über <see cref="Connectivity"/>
/// (<c>Microsoft.Maui.Networking</c>): <see cref="IsOnline"/> bildet
/// <see cref="NetworkAccess.Internet"/> und
/// <see cref="NetworkAccess.ConstrainedInternet"/> als online ab;
/// <c>ConnectivityChanged</c> meldet jeden Wechsel.
/// </summary>
public sealed class ConnectivityNetworkStatusMonitor : INetworkStatusMonitor
{
    private bool started;

    /// <inheritdoc/>
    public bool IsOnline =>
        Connectivity.Current.NetworkAccess is NetworkAccess.Internet or NetworkAccess.ConstrainedInternet;

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
        AvailabilityChanged?.Invoke(this, IsOnline);
    }
}
