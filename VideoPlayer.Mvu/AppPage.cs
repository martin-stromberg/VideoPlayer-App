namespace VideoPlayer.Mvu;

/// <summary>
/// Seiten der App. <see cref="Loading"/> ist die reine Start-Ladeanzeige und
/// besitzt keinen Eintrag im Hauptmenü.
/// </summary>
public enum AppPage
{
    Loading,
    Home,
    Sources,
    Favorites,
    Downloads,
    Profile,
    Settings,

    /// <summary>
    /// Server-Einrichtung/-verwaltung. Wie <see cref="Loading"/> ohne
    /// Menüeintrag — wird beim Erststart als Window-Root bzw. über die
    /// registrierte Shell-Route <c>serversetup</c> erreicht.
    /// </summary>
    ServerSetup
}
