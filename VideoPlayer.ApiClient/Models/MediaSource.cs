namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Medienquelle des Servers (freigegebener Medienbereich).
/// </summary>
public class MediaSource : MediaEntry
{
    /// <summary>Anlagezeitpunkt der Quelle.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Zeitpunkt der letzten Klassifizierung, falls erfolgt.</summary>
    public DateTime? ClassifiedAt { get; set; }

    /// <summary>Ob seit dem letzten Scan Änderungen vorliegen.</summary>
    public bool Changed { get; set; }

    /// <summary>Zeitpunkt des letzten Scans, falls erfolgt.</summary>
    public DateTime? LastScannedAt { get; set; }

    /// <summary>ID des Quellen-Icons, falls vorhanden.</summary>
    public long? IconPictureId { get; set; }

    /// <summary>
    /// Serialisierte Felder ohne Property-Mapping — die Serverversionen
    /// melden die Menü-Sichtbarkeit („Im Menü ausblenden") unter
    /// wechselnden Feldnamen.
    /// </summary>
    [System.Text.Json.Serialization.JsonExtensionData]
    public System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; set; }

    /// <summary>
    /// Die Quelle ist im Menü ausgeblendet: Versteck-Flags
    /// (<c>hidden</c>, <c>hide…</c>, <c>…InMenu</c>-Versteckvarianten)
    /// auf <c>true</c> oder Sichtbarkeits-Flags
    /// (<c>showInMenu</c>, <c>visibleInMenu</c>, <c>inMenu</c>) auf
    /// <c>false</c>.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsMenuHidden
    {
        get
        {
            if (ExtensionData is null)
            {
                return false;
            }

            foreach (var (key, value) in ExtensionData)
            {
                if (value.ValueKind is not (System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False))
                {
                    continue;
                }

                var name = key.ToLowerInvariant();
                var flag = value.ValueKind == System.Text.Json.JsonValueKind.True;
                if (name.Contains("hidden") && flag)
                {
                    return true;
                }

                if (!flag && (name.Contains("inmenu") || name.Contains("showin") || name.Contains("visiblein")))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

/// <summary>
/// Genres einer Medienquelle.
/// </summary>
public class SourceGenres
{
    /// <summary>ID der Medienquelle.</summary>
    public long SourceId { get; set; }

    /// <summary>Name der Medienquelle.</summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>Genres der Quelle.</summary>
    public List<Genre> Genres { get; set; } = [];
}

/// <summary>
/// Genre eines Medieninhalts.
/// </summary>
public class Genre
{
    /// <summary>ID des Genres.</summary>
    public long Id { get; set; }

    /// <summary>Name des Genres.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL des Genre-Icons, falls vorhanden.</summary>
    public string? IconUrl { get; set; }
}

/// <summary>
/// Wählbare Genre-Option für den Quellenfilter.
/// </summary>
public class GenreOption
{
    /// <summary>ID des Genres.</summary>
    public long Id { get; set; }

    /// <summary>Name des Genres.</summary>
    public string Name { get; set; } = string.Empty;
}
