namespace VideoPlayer.Mvu;

/// <summary>
/// Abstraktion der Netzwerkstatus-Erkennung (Implementierung am App-Rand
/// über <c>Microsoft.Maui.Networking.Connectivity</c>). Hält den MVU-Kern
/// plattformneutral und ohne MAUI-Laufzeit testbar.
/// </summary>
public interface INetworkStatusMonitor
{
    /// <summary>Letzter bekannter Netzwerkstatus (<c>true</c> = online).</summary>
    bool IsOnline { get; }

    /// <summary>
    /// Die aktuelle Verbindung läuft über ein ungedrosseltes
    /// Festnetz-Profil (WLAN oder Ethernet) — Grundlage der
    /// Einstellung „Downloads nur im WLAN".
    /// </summary>
    bool IsWifi { get; }

    /// <summary>Wird bei jedem erkannten Wechsel des Netzwerkstatus ausgelöst.</summary>
    event EventHandler<bool>? AvailabilityChanged;

    /// <summary>
    /// Startet die Überwachung (idempotent). Der aktuelle Ist-Wert liegt
    /// bereits über <see cref="IsOnline"/> vor — das Ereignis meldet nur
    /// Wechsel.
    /// </summary>
    void Start();
}
