namespace VideoPlayer.Mvu;

/// <summary>
/// Seiten der App. <see cref="Loading"/> ist die reine Start-Ladeanzeige und
/// besitzt keinen Eintrag im Hauptmenü.
/// </summary>
public enum AppPage
{
    /// <summary>Start-Ladeanzeige (kein Menüeintrag).</summary>
    Loading,

    /// <summary>Startseite mit den Listen „Weiterschauen", „Favoriten" und „Neu im Programm".</summary>
    Home,

    /// <summary>Menüseite „Favoriten".</summary>
    Favorites,

    /// <summary>Menüseite „Playlists" (eigene und öffentliche Playlists).</summary>
    Playlists,

    /// <summary>Menüseite „Downloads".</summary>
    Downloads,

    /// <summary>Menüseite „Profil".</summary>
    Profile,

    /// <summary>Menüseite „Einstellungen".</summary>
    Settings,

    /// <summary>
    /// Server-Einrichtung/-verwaltung. Wie <see cref="Loading"/> ohne
    /// Menüeintrag — wird beim Erststart als Window-Root bzw. über die
    /// registrierte Shell-Route <c>serversetup</c> erreicht.
    /// </summary>
    ServerSetup,

    /// <summary>
    /// Anmeldemaske. Wie <see cref="ServerSetup"/> ohne Menüeintrag — wird im
    /// Start-Workflow als Window-Root bzw. bei Sitzungsablauf über die
    /// registrierte Shell-Route <c>login</c> erreicht.
    /// </summary>
    Login
}
