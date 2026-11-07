namespace VideoPlayer.Mvu;

/// <summary>
/// Ergebnisstatus des Geräte-Pairings (<c>api/pairing/exchange</c>) für die
/// MVU-Fehlerbehandlung.
/// </summary>
public enum PairingStatus
{
    /// <summary>Pairing erfolgreich — das Geräte-Token wurde entschlüsselt und hinterlegt.</summary>
    Succeeded,

    /// <summary>Der Code ist ungültig, abgelaufen oder bereits verwendet (HTTP 401).</summary>
    InvalidCode,

    /// <summary>Zu viele Versuche — der Server sperrt vorübergehend (HTTP 429).</summary>
    RateLimited,

    /// <summary>Die Anfrage war fachlich ungültig (HTTP 400).</summary>
    InvalidRequest,

    /// <summary>Der Server unterstützt den Pairing-Endpunkt nicht (HTTP 404).</summary>
    NotSupported,

    /// <summary>Der Server war während des Pairings nicht erreichbar (Transport/Timeout).</summary>
    ServerUnreachable,

    /// <summary>Sonstiger Fehler (z. B. ungültige oder nicht entschlüsselbare Antwort).</summary>
    Failed
}
