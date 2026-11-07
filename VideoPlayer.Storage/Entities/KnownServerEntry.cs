using SQLite;

namespace VideoPlayer.Storage.Entities;

/// <summary>Bekannter VideoPlayer-Server (backup-pflichtig: Serverliste).</summary>
[Table("known_servers")]
public sealed class KnownServerEntry
{
    /// <summary>ID des Eintrags (Primärschlüssel, automatisch).</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Kanonische Serveradresse (eindeutig, indiziert).</summary>
    [Indexed(Unique = true)]
    public string Address { get; set; } = string.Empty;

    /// <summary>Anzeigename; leer, wenn keiner vergeben wurde.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Zeitpunkt der letzten Nutzung (UTC).</summary>
    public DateTime LastUsedAtUtc { get; set; }
}
