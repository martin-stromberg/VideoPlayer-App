namespace VideoPlayer.Storage;

/// <summary>
/// Normalisierung von Serveradressen in die kanonische Form
/// <c>scheme://host[:port][/pfad]/</c> (Schema ergänzt, Host kleingeschrieben,
/// abschließender Schrägstrich). Die kanonische Form ist zugleich der
/// Vergleichs-/Speicherschlüssel in <c>known_servers</c>.
/// </summary>
public static class ServerAddress
{
    /// <summary>
    /// Normalisiert eine eingegebene oder gemeldete Serveradresse.
    /// Liefert <c>false</c> für leere oder nicht als http(s)-URI
    /// interpretierbare Eingaben.
    /// </summary>
    /// <param name="input">Eingegebene oder gemeldete Serveradresse.</param>
    /// <param name="normalized">Kanonische Form der Adresse; leer bei ungültiger Eingabe.</param>
    /// <returns><c>true</c>, wenn die Eingabe zu einer gültigen http(s)-Adresse normalisiert werden konnte.</returns>
    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var candidate = input.Trim();
        if (candidate.Contains("://", StringComparison.Ordinal))
        {
            // Explizites Schema: nur http/https zulässig (z. B. „ftp://…"
            // würde sonst als Host „ftp" fehlinterpretiert).
            if (!candidate.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !candidate.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        else
        {
            candidate = $"http://{candidate}";
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            || string.IsNullOrWhiteSpace(uri.Host)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        normalized = uri.GetLeftPart(UriPartial.Path).TrimEnd('/') + "/";
        return true;
    }

    /// <summary>Normalisiert eine Serveradresse oder wirft bei ungültiger Eingabe.</summary>
    /// <param name="input">Zu normalisierende Serveradresse.</param>
    /// <returns>Die kanonische Form der Adresse.</returns>
    /// <exception cref="ArgumentException">Die Eingabe ist keine gültige http(s)-Adresse.</exception>
    public static string Normalize(string input)
    {
        if (!TryNormalize(input, out var normalized))
        {
            throw new ArgumentException($"Ungültige Serveradresse: '{input}'.", nameof(input));
        }

        return normalized;
    }
}
