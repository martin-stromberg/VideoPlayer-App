using SQLite;

namespace VideoPlayer.Storage.Entities;

/// <summary>
/// Generischer Schlüssel/Wert-Eintrag backup-pflichtiger Zustandsdaten —
/// u. a. zuletzt verbundener Server und Profildaten ohne Geheimnisse.
/// </summary>
[Table("backup_state")]
public sealed class BackupStateEntry
{
    /// <summary>Schlüssel des Eintrags für den zuletzt verbundenen Server.</summary>
    public const string LastConnectedServerKey = "LastConnectedServer";

    /// <summary>Schlüssel des Zustandswerts (Primärschlüssel).</summary>
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    /// <summary>Gespeicherter Wert.</summary>
    public string Value { get; set; } = string.Empty;
}
