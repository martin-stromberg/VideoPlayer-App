using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>
/// Anzeigeinformationen der Profil-Seite: Benutzername und E-Mail des
/// angemeldeten Server-Benutzers sowie Name des aktiven lokalen Profils.
/// </summary>
/// <param name="Username">Benutzername des angemeldeten Benutzers; <c>null</c>, wenn nicht ermittelbar.</param>
/// <param name="Email">E-Mail-Adresse des angemeldeten Benutzers; <c>null</c>, wenn nicht ermittelbar.</param>
/// <param name="ProfileName">Name des aktiven lokalen Profils.</param>
/// <returns>Ergebnis-Datensatz der Profil-Ermittlung.</returns>
public sealed record ProfileInfo(string? Username, string? Email, string? ProfileName);

/// <summary>
/// Zustand der Profil-Seite (Menüseite „Profil"): Benutzername und
/// E-Mail des angemeldeten Server-Benutzers (aus den Claims des
/// gespeicherten Anmelde-Tokens bzw. dem gespeicherten
/// Anmelde-Benutzernamen — die API besitzt keinen
/// Benutzerinfo-Endpunkt) sowie das aktive lokale Profil.
/// </summary>
public sealed record ProfilePageState
{
    /// <summary>Ausgangszustand (nichts geladen).</summary>
    /// <value>Der leere Ausgangszustand der Profil-Seite.</value>
    public static ProfilePageState Initial { get; } = new();

    /// <summary>Die Profilinformationen werden gerade ermittelt.</summary>
    public bool IsLoading { get; init; }

    /// <summary>Mindestens ein Ladeversuch wurde abgeschlossen.</summary>
    public bool Attempted { get; init; }

    /// <summary>Benutzername des angemeldeten Benutzers; <c>null</c>, wenn nicht ermittelbar.</summary>
    public string? Username { get; init; }

    /// <summary>E-Mail-Adresse des angemeldeten Benutzers; <c>null</c>, wenn nicht ermittelbar.</summary>
    public string? Email { get; init; }

    /// <summary>Name des aktiven lokalen Profils; <c>null</c>, wenn noch nicht geladen.</summary>
    public string? ProfileName { get; init; }

    /// <summary>Anzeigetext des Benutzernamens („—" ohne ermittelbaren Wert).</summary>
    public string UsernameText =>
        string.IsNullOrWhiteSpace(Username) ? "—" : Username!;

    /// <summary>Anzeigetext der E-Mail-Adresse („—" ohne ermittelbaren Wert).</summary>
    public string EmailText =>
        string.IsNullOrWhiteSpace(Email) ? "—" : Email!;

    /// <summary>Anzeigetext des Profilnamens (Standardprofil ohne ermittelbaren Wert).</summary>
    public string ProfileNameText =>
        string.IsNullOrWhiteSpace(ProfileName) ? AppProfile.DefaultName : ProfileName!;
}
