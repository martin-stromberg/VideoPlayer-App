using CoreFoundation;
using Microsoft.Extensions.Logging;
using Network;
using VideoPlayer.Mvu;

namespace VideoPlayer.TvOS.Services
{
    /// <summary>
    /// Netzwerkstatus-Erkennung über <see cref="NWPathMonitor"/> (das
    /// tvOS-Pendant zum MAUI-<c>Connectivity</c>-Monitor):
    /// <see cref="IsOnline"/> bildet <see cref="NWPathStatus.Satisfied"/> ab;
    /// <see cref="IsWifi"/> gilt für WLAN und Ethernet (ungedrosselte
    /// Verbindungen — Apple TV wird typischerweise per Ethernet oder WLAN
    /// angebunden); jeder Pfadwechsel löst <see cref="AvailabilityChanged"/>
    /// aus. Der Monitor läuft auf der globalen Dispatch-Queue.
    /// </summary>
    public sealed class TvosNetworkStatusMonitor : INetworkStatusMonitor
    {
        private readonly ILogger<TvosNetworkStatusMonitor> logger;
        private NWPathMonitor? monitor;
        private bool started;

        /// <summary>Initialisiert eine neue Instanz der <see cref="TvosNetworkStatusMonitor"/>-Klasse.</summary>
        /// <param name="logger">Logger für das Programmprotokoll.</param>
        public TvosNetworkStatusMonitor(ILogger<TvosNetworkStatusMonitor> logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc/>
        public bool IsOnline =>
            monitor?.CurrentPath?.Status == NWPathStatus.Satisfied;

        /// <inheritdoc/>
        public bool IsWifi
        {
            get
            {
                var path = monitor?.CurrentPath;
                if (path is null)
                {
                    return false;
                }

                // Auch kabelgebundenes Ethernet zählt (gleiche Semantik wie
                // ConnectivityNetworkStatusMonitor der MAUI-App): tvOS hat
                // keine getaktete Mobilfunk-Verbindung.
                return path.UsesInterfaceType(NWInterfaceType.Wifi)
                    || path.UsesInterfaceType(NWInterfaceType.Wired);
            }
        }

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
            monitor = new NWPathMonitor
            {
                SnapshotHandler = OnPathUpdate
            };
            monitor.SetQueue(DispatchQueue.DefaultGlobalQueue);
            monitor.Start();
        }

        private void OnPathUpdate(NWPath path)
        {
            var isOnline = path.Status == NWPathStatus.Satisfied;
            logger.LogInformation(
                "Netzverfügbarkeit geändert: {IsOnline} (WLAN/Ethernet: {IsWifi}).",
                isOnline ? "online" : "offline",
                IsWifi);
            AvailabilityChanged?.Invoke(this, isOnline);
        }
    }
}
