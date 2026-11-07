using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>
/// Art der eingegebenen Kopplungs-Eingabe: eine vollständige Ticket-Adresse
/// (QR-Code-Inhalt <c>https://server[/pfad]/pairing?t=ticket</c>) oder ein
/// opaker Kurzcode/Ticket-Text ohne Serverangabe.
/// </summary>
public enum BootstrapTicketKind
{
    /// <summary>Vollständige Ticket-Adresse — trägt die Serveradresse selbst.</summary>
    Url,

    /// <summary>Opaker Kurzcode — wird gegen den bereits verbundenen Server eingelöst.</summary>
    ShortCode
}

/// <summary>
/// Ergebnis der Kopplungs-Eingabe: das opake Ticket (niemals normalisiert)
/// und bei der URL-Form die kanonische Server-Basisadresse.
/// </summary>
/// <param name="Kind">Art der Eingabe.</param>
/// <param name="Ticket">Das opake Ticket bzw. der Kurzcode (unverändert).</param>
/// <param name="ServerAddress">Kanonische Serveradresse (nur bei <see cref="BootstrapTicketKind.Url"/>); sonst <c>null</c>.</param>
public sealed record BootstrapTicketInput(
    BootstrapTicketKind Kind,
    string Ticket,
    string? ServerAddress);

/// <summary>
/// Zerlegt die Kopplungs-Eingabe des QR-/Ticket-Onboardings. Die URL-Form
/// liefert die Server-Basisadresse (das abschließende
/// <c>/pairing</c>-Segment gehört nicht zum Server — Unterpfade bleiben
/// erhalten), die Kurzcode-Form den opaken Code. Das Ticket wird
/// <b>nicht</b> normalisiert (<see cref="PairingCode"/> ist nur für den
/// klassischen Einmal-Code zuständig).
/// </summary>
public static class BootstrapTicketParser
{
    /// <summary>Pfadsegment, das den Pairing-Endpunkt der Ticket-Adresse markiert.</summary>
    public const string PairingSegment = "pairing";

    /// <summary>Query-Parameter, der das Ticket trägt.</summary>
    public const string TicketParameter = "t";

    /// <summary>
    /// Zerlegt eine Kopplungs-Eingabe in Ticket und ggf. Serveradresse.
    /// </summary>
    /// <param name="input">Roheingabe (QR-Inhalt oder manueller Text).</param>
    /// <param name="result">Das zerlegte Ergebnis; <c>null</c> bei ungültiger Eingabe.</param>
    /// <returns><c>true</c>, wenn die Eingabe verwertbar ist.</returns>
    public static bool TryParse(string? input, out BootstrapTicketInput? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var text = input.Trim();
        if (text.Contains("://", StringComparison.Ordinal))
        {
            return TryParseUrl(text, out result);
        }

        return TryParseShortCode(text, out result);
    }

    /// <summary>
    /// URL-Form: Schema http/https, letztes Pfadsegment <c>pairing</c>,
    /// nicht-leerer <c>t</c>-Parameter. Die Basisadresse wird kanonisiert.
    /// </summary>
    private static bool TryParseUrl(string text, out BootstrapTicketInput? result)
    {
        result = null;
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        // Das letzte nicht-leere Pfadsegment muss „pairing" sein — nur es
        // wird abgeschnitten, Unterpfade des Servers bleiben erhalten.
        var path = uri.AbsolutePath.TrimEnd('/');
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0
            || !string.Equals(segments[^1], PairingSegment, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var ticket = ExtractTicket(uri.Query);
        if (string.IsNullOrEmpty(ticket))
        {
            return false;
        }

        var basePath = path[..(path.Length - segments[^1].Length)];
        var baseText = uri.GetLeftPart(UriPartial.Authority) + basePath;
        if (!ServerAddress.TryNormalize(baseText, out var normalized))
        {
            return false;
        }

        result = new BootstrapTicketInput(BootstrapTicketKind.Url, ticket, normalized);
        return true;
    }

    /// <summary>
    /// Kurzcode-Form: opaker Text ohne URL-Bestandteile. Der Server prüft
    /// den Code — eine lokale Formatprüfung würde längere Ticket-Texte
    /// fälschlich abweisen.
    /// </summary>
    private static bool TryParseShortCode(string text, out BootstrapTicketInput? result)
    {
        result = null;
        if (text.Any(char.IsWhiteSpace) || text.Contains(':'))
        {
            // „:" verhindert, dass Bruchstücke einer Adresse („server:8080")
            // als Kurzcode an den falschen Endpunkt gehen.
            return false;
        }

        result = new BootstrapTicketInput(BootstrapTicketKind.ShortCode, text, null);
        return true;
    }

    /// <summary>
    /// Liest den <c>t</c>-Parameter aus der Query (opak dekodiert).
    /// </summary>
    /// <param name="query">Rohe Query des URIs (mit führendem <c>?</c>).</param>
    /// <returns>Das dekodierte Ticket oder <c>null</c>, wenn fehlend/leer.</returns>
    private static string? ExtractTicket(string query)
    {
        if (string.IsNullOrEmpty(query) || query[0] != '?')
        {
            return null;
        }

        foreach (var pair in query[1..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var equals = pair.IndexOf('=');
            if (equals <= 0)
            {
                continue;
            }

            if (!string.Equals(pair[..equals], TicketParameter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string value;
            try
            {
                value = Uri.UnescapeDataString(pair[(equals + 1)..]);
            }
            catch (UriFormatException)
            {
                // UnescapeDataString wirft bei Überlänge (> 32K) — ein
                // malformed-Ticket bleibt eine ungültige Eingabe, kein Crash.
                return null;
            }

            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }
}
