namespace VideoPlayer.Mvu;

/// <summary>
/// Ergebnisstatus des Ticket-Bootstraps (<c>api/pairing/bootstrap</c>) für
/// die MVU-Fehlerbehandlung des QR-/Kurzcode-Onboardings.
/// </summary>
public enum BootstrapStatus
{
    /// <summary>Bootstrap erfolgreich — Geräte-Token, Sitzung und Refresh-Token wurden hinterlegt.</summary>
    Succeeded,

    /// <summary>Das Ticket ist ungültig oder abgelaufen (HTTP 401).</summary>
    InvalidTicket,

    /// <summary>Die Anfrage war fachlich ungültig (HTTP 400).</summary>
    InvalidRequest,

    /// <summary>Zu viele Versuche — der Server sperrt vorübergehend (HTTP 429).</summary>
    RateLimited,

    /// <summary>Der Server unterstützt den Bootstrap-Endpunkt nicht (HTTP 404).</summary>
    NotSupported,

    /// <summary>Der Server war während des Bootstraps nicht erreichbar (Transport/Timeout).</summary>
    ServerUnreachable,

    /// <summary>Sonstiger Fehler (z. B. ungültige oder nicht entschlüsselbare Antwort).</summary>
    Failed
}
