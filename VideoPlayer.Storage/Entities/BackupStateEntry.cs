using SQLite;

namespace VideoPlayer.Storage.Entities;

/// <summary>
/// Generischer Schlüssel/Wert-Eintrag backup-pflichtiger Zustandsdaten —
/// u. a. zuletzt verbundener Server und Profildaten ohne Geheimnisse.
/// </summary>
[Table("backup_state")]
public sealed class BackupStateEntry
{
    public const string LastConnectedServerKey = "LastConnectedServer";

    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
