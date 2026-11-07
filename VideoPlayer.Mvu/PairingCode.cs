namespace VideoPlayer.Mvu;

/// <summary>
/// Hilfsfunktionen für Einmal-Pairing-Codes: Normalisierung (Trimmen,
/// Großbuchstaben) und Formatprüfung gegen das serverseitige Alphabet.
/// </summary>
public static class PairingCode
{
    /// <summary>
    /// Mindestlänge eines Pairing-Codes. Die tatsächliche Länge ist
    /// serverseitig konfigurierbar (Option <c>Pairing:CodeLength</c>,
    /// Minimum 4) — die lokale Prüfung darf sie nicht überstimmen.
    /// </summary>
    public const int MinLength = 4;

    /// <summary>
    /// Erlaubtes Alphabet der Pairing-Codes — ohne leicht verwechselbare
    /// Zeichen (kein I, L, O, 0, 1).
    /// </summary>
    public const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    /// <summary>
    /// Normalisiert eine Eingabe (Trimmen, Großschreibung) und prüft
    /// Mindestlänge und Alphabet — die konfigurierte Codelänge kennt nur
    /// der Server (er lehnt ungültige Codes ohnehin ab).
    /// </summary>
    /// <param name="input">Die Roheingabe des Benutzers.</param>
    /// <param name="normalized">Der normalisierte Code, wenn gültig.</param>
    /// <returns><c>true</c>, wenn die Eingabe ein wohlgeformter Pairing-Code ist.</returns>
    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var candidate = input.Trim().ToUpperInvariant();
        if (candidate.Length < MinLength || candidate.Any(c => !Alphabet.Contains(c)))
        {
            return false;
        }

        normalized = candidate;
        return true;
    }
}
