namespace VideoPlayer.Mvu;

/// <summary>
/// Zustand der Server-Erreichbarkeitsprüfung beim App-Start.
/// </summary>
public enum ServerHealthStatus
{
    Unknown,
    Checking,
    Reachable,
    Unreachable
}
