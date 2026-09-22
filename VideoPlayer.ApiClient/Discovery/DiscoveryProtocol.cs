namespace VideoPlayer.ApiClient.Discovery;

/// <summary>
/// Konstanten und Parsing des bestehenden Discovery-Protokolls der
/// Serveranwendung „VideoPlayer": UDP-Broadcast
/// <see cref="QueryText"/> an Port <see cref="UdpPort"/>, Antwort
/// <c>VIDEOWEBPLAYER_SERVER:&lt;adresse&gt;</c>; ergänzend mDNS-Dienstsuche
/// auf <see cref="MdnsServiceType"/>.
/// </summary>
public static class DiscoveryProtocol
{
    /// <summary>UDP-Port der Discovery-Abfrage.</summary>
    public const int UdpPort = 5001;

    /// <summary>UDP-Abfragetext (UTF-8).</summary>
    public const string QueryText = "VIDEOWEBPLAYER_DISCOVERY";

    /// <summary>Präfix der UDP-Antwort; dahinter folgt die Serveradresse.</summary>
    public const string ResponsePrefix = "VIDEOWEBPLAYER_SERVER:";

    /// <summary>mDNS-Diensttyp der Dienstsuche.</summary>
    public const string MdnsServiceType = "_http._tcp.local.";

    /// <summary>Extrahiert die Serveradresse aus einer UDP-Antwort.</summary>
    public static bool TryParseResponse(string? datagramText, out string address)
    {
        address = string.Empty;
        if (string.IsNullOrEmpty(datagramText)
            || !datagramText.StartsWith(ResponsePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var candidate = datagramText[ResponsePrefix.Length..].Trim();
        if (candidate.Length == 0)
        {
            return false;
        }

        address = candidate;
        return true;
    }
}
