namespace VideoPlayer.Mvu;

/// <summary>
/// Verbindungszustand der App zum VideoPlayer-Server. Der Zustand ist im
/// <see cref="AppModel"/> durchgehend bekannt und steuert die Navigation
/// (Ladeanzeige, Anmeldemaske, Servereinrichtung, Offline- oder
/// Normalbetrieb).
/// </summary>
public enum ConnectionState
{
    /// <summary>Startzustand vor Beginn des Verbindungs-Workflows.</summary>
    Unknown,

    /// <summary>Verbindungsaufbau läuft (Erreichbarkeitsprüfung, automatische Anmeldung).</summary>
    Connecting,

    /// <summary>Mit dem Server verbunden und angemeldet.</summary>
    Connected,

    /// <summary>Server erreichbar, aber eine Anmeldung ist erforderlich (Anmeldemaske).</summary>
    NeedsLogin,

    /// <summary>Kein Server hinterlegt bzw. kein Offline-Betrieb möglich (Servereinrichtung/-auswahl).</summary>
    NeedsServerSetup,

    /// <summary>Server nicht erreichbar; lokale Offline-Daten sind vorhanden.</summary>
    Offline
}
