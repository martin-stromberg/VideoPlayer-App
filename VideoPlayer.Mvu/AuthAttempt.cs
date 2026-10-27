namespace VideoPlayer.Mvu;

/// <summary>Ergebnis einer Anmeldung am VideoPlayer-Server.</summary>
public enum AuthAttemptStatus
{
    /// <summary>Anmeldung erfolgreich; Token und Zugangsdaten sind gespeichert.</summary>
    Succeeded,

    /// <summary>Keine gespeicherten Zugangsdaten vorhanden (nur automatische Anmeldung).</summary>
    MissingCredentials,

    /// <summary>Der Server lehnt die Zugangsdaten ab (401/403).</summary>
    InvalidCredentials,

    /// <summary>Der Server ist nicht erreichbar (Transportfehler/Zeitüberschreitung nach der Wiederholungsstrategie).</summary>
    ServerUnreachable,

    /// <summary>
    /// Das Client-Gate hat den Schlüssel abgelehnt (widerrufenes Geräte-Token) —
    /// die Gate-Probe wurde als 401 beantwortet; kein Zugangsdaten-Problem.
    /// </summary>
    GateRejected,

    /// <summary>Sonstiger Fehler der Anmeldung.</summary>
    Failed
}

/// <summary>
/// Ergebnistransport eines Anmeldeversuchs. <see cref="Username"/> trägt den
/// (nicht geheimen) gespeicherten bzw. eingegebenen Benutzernamen zur
/// Vorbefüllung der Anmeldemaske.
/// </summary>
/// <param name="Status">Ergebnis des Anmeldeversuchs.</param>
/// <param name="Username">Gespeicherter bzw. eingegebener Benutzername; <c>null</c>, wenn keiner bekannt ist.</param>
/// <returns>Ergebnistransport des Anmeldeversuchs.</returns>
public sealed record AuthAttempt(AuthAttemptStatus Status, string? Username = null)
{
    /// <summary>Die Anmeldung war erfolgreich.</summary>
    public bool Succeeded => Status == AuthAttemptStatus.Succeeded;
}
