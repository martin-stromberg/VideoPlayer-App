namespace VideoPlayer.ApiClient.Discovery;

/// <summary>
/// Ein per Netzwerk-Erkennung gefundener Server-Kandidat. Die Adresse ist
/// bereits normalisiert (<c>scheme://host[:port][/pfad]/</c>); ob dahinter
/// tatsächlich ein VideoPlayer-Server antwortet, entscheidet erst die
/// Verbindungsprüfung (<c>CheckHealthAsync</c>).
/// </summary>
public sealed record DiscoveredServer
{
    /// <summary>Normalisierte Serveradresse.</summary>
    public required string Address { get; init; }

    /// <summary>Discovery-Weg, über den der Server gefunden wurde.</summary>
    public DiscoverySource Source { get; init; }

    /// <summary>Anzeigename, falls die Quelle einen liefert (mDNS-Instanzname).</summary>
    public string? Name { get; init; }

    /// <summary>Anzeigename, falls vorhanden, sonst die Adresse (Listenanzeige).</summary>
    public string NameOrAddress =>
        string.IsNullOrWhiteSpace(Name) ? Address : Name;
}
