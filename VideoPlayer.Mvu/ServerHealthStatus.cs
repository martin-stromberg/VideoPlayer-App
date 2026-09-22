namespace VideoPlayer.Mvu;

/// <summary>
/// Zustand der Server-Erreichbarkeitsprüfung beim App-Start.
/// </summary>
public enum ServerHealthStatus
{
    /// <summary>Erreichbarkeit noch nicht geprüft.</summary>
    Unknown,

    /// <summary>Die Erreichbarkeitsprüfung läuft gerade.</summary>
    Checking,

    /// <summary>Der Server ist erreichbar.</summary>
    Reachable,

    /// <summary>Der Server ist nicht erreichbar.</summary>
    Unreachable
}
