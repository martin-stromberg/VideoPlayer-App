namespace VideoWebPlayer.Maui.Services;

/// <summary>
/// Manages application profiles for different launch configurations.
/// Each profile has its own database and isolated settings.
/// </summary>
public class ProfileManager
{
    private static ProfileManager? _instance;
    private string _currentProfile = "default";

    /// <summary>
    /// Ruft die gemeinsame Singleton-Instanz des Profil-Managers ab.
    /// </summary>
    /// <value>Die Singleton-Instanz des <see cref="ProfileManager"/>.</value>
    public static ProfileManager Instance => _instance ??= new ProfileManager();

    /// <summary>
    /// Ruft den Namen des aktuell aktiven Profils ab oder legt ihn fest.
    /// </summary>
    /// <value>Der Profilname (kleingeschrieben); bei leerem Wert wird "default" verwendet.</value>
    public string CurrentProfile
    {
        get => _currentProfile;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                value = "default";
            _currentProfile = value.ToLowerInvariant();
            System.Diagnostics.Debug.WriteLine($"[ProfileManager] Profile changed to: {_currentProfile}");
        }
    }

    /// <summary>
    /// Liefert den Dateinamen der SQLite-Datenbank für das aktuelle Profil.
    /// </summary>
    /// <returns>Der profilabhängige Datenbank-Dateiname.</returns>
    public string GetDatabaseFileName()
    {
        return CurrentProfile == "default"
            ? "downloads.db3"
            : $"downloads_{CurrentProfile}.db3";
    }

    /// <summary>
    /// Liefert das Präfix für Preferences-Schlüssel des aktuellen Profils.
    /// </summary>
    /// <returns>Das Schlüsselpräfix des Profils (leer für "default").</returns>
    public string GetPreferencesPrefix()
    {
        return CurrentProfile == "default"
            ? ""
            : $"{CurrentProfile}_";
    }

    /// <summary>
    /// Initialisiert eine neue Instanz der <see cref="ProfileManager"/>-Klasse
    /// und lädt das Profil ggf. aus der Umgebungsvariable VIDEOWEBPLAYER_PROFILE.
    /// </summary>
    public ProfileManager()
    {
        // Versuche das Profil aus Umgebungsvariable zu laden
        var profileEnv = Environment.GetEnvironmentVariable("VIDEOWEBPLAYER_PROFILE");
        if (!string.IsNullOrWhiteSpace(profileEnv))
        {
            CurrentProfile = profileEnv;
        }
    }
}
