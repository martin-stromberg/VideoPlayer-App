namespace VideoPlayer.Storage;

/// <summary>
/// Ein bekannter VideoPlayer-Server (Domänenmodell, backup-pflichtig —
/// persistiert in <c>known_servers</c> der <see cref="BackupDatabase"/>).
/// </summary>
public sealed record KnownServer
{
    /// <summary>Kanonische Serveradresse (siehe <see cref="ServerAddress"/>).</summary>
    public required string Address { get; init; }

    /// <summary>Anzeigename; leer, wenn keiner vergeben wurde.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Zeitpunkt der letzten Nutzung (UTC).</summary>
    public DateTime LastUsedAtUtc { get; init; }

    /// <summary>Anzeigename, falls vorhanden, sonst die Adresse (Listenanzeige).</summary>
    public string NameOrAddress =>
        string.IsNullOrWhiteSpace(DisplayName) ? Address : DisplayName;
}
