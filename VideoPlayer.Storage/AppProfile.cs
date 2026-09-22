namespace VideoPlayer.Storage;

/// <summary>
/// Aktives Anwendungsprofil. Das Profil wird über die Umgebungsvariable
/// <see cref="EnvironmentVariable"/> gewählt und isoliert sowohl die
/// Ablage-Schlüssel (Präfix) als auch die Dateiablagen (Unterverzeichnis).
/// Das Standardprofil <c>default</c> arbeitet ohne Präfix/Unterverzeichnis.
/// </summary>
public sealed class AppProfile
{
    /// <summary>Umgebungsvariable zur Profilwahl.</summary>
    public const string EnvironmentVariable = "VIDEOPLAYER_PROFILE";

    /// <summary>Name des Standardprofils.</summary>
    public const string DefaultName = "default";

    private AppProfile(string name)
    {
        Name = name;
    }

    /// <summary>Name des aktiven Profils (kleingeschrieben).</summary>
    public string Name { get; }

    /// <summary>Präfix für Ablage-Schlüssel (leer beim Standardprofil).</summary>
    public string PreferencesPrefix => Name == DefaultName ? string.Empty : $"{Name}_";

    /// <summary>
    /// Unterverzeichnis für profilbezogene Dateiablagen
    /// (leer beim Standardprofil = Wurzelverzeichnis).
    /// </summary>
    public string DirectoryName => Name == DefaultName ? string.Empty : Name;

    /// <summary>Liefert den Ablage-Schlüssel mit Profil-Präfix.</summary>
    public string PrefixKey(string key) => PreferencesPrefix + key;

    /// <summary>
    /// Ermittelt das aktive Profil aus der Umgebungsvariable
    /// <see cref="EnvironmentVariable"/>. Leere/fehlende Werte ergeben das
    /// Standardprofil.
    /// </summary>
    public static AppProfile FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;
        return Create(getVariable(EnvironmentVariable));
    }

    /// <summary>Erstellt ein Profil aus einem Namen (kleingeschrieben, Fallback <c>default</c>).</summary>
    public static AppProfile Create(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new AppProfile(DefaultName);
        }

        return new AppProfile(name.Trim().ToLowerInvariant());
    }
}
