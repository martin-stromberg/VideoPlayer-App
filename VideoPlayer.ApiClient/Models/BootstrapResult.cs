namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Ergebnis des Bootstrap-Austauschs (<c>api/pairing/bootstrap</c>): das
/// entschlüsselte Session-Paket in typsicherer Form.
/// </summary>
public sealed class BootstrapResult
{
    /// <summary>Geräte-Token (Gate-Schlüssel des gepaarten Geräts).</summary>
    public required string DeviceToken { get; init; }

    /// <summary>Anmelde-Token (Bearer).</summary>
    public required string Token { get; init; }

    /// <summary>Ablaufzeitpunkt des Anmelde-Tokens.</summary>
    public required DateTime Expires { get; init; }

    /// <summary>Refresh-Token für die stille Sitzungserneuerung.</summary>
    public required string RefreshToken { get; init; }
}
