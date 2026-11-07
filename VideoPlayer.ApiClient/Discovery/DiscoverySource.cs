namespace VideoPlayer.ApiClient.Discovery;

/// <summary>Discovery-Weg, über den ein Server gefunden wurde.</summary>
public enum DiscoverySource
{
    /// <summary>mDNS-Dienstsuche (<c>_http._tcp.local.</c>).</summary>
    Mdns,

    /// <summary>UDP-Broadcast-Abfrage auf Port 5001.</summary>
    Udp
}
