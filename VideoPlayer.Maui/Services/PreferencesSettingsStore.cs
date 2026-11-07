using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// <see cref="ISettingsStore"/>-Adapter auf die plattformeigene
/// Einstellungsablage (<c>Preferences</c>). Enthält ausschließlich
/// nicht-geheime Werte.
/// </summary>
public sealed class PreferencesSettingsStore : ISettingsStore
{
    private readonly IPreferences preferences;

    /// <summary>Initialisiert eine neue Instanz der <see cref="PreferencesSettingsStore"/>-Klasse.</summary>
    /// <param name="preferences">Plattformeigene Einstellungsablage (<c>Preferences.Default</c>).</param>
    public PreferencesSettingsStore(IPreferences preferences)
    {
        this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
    }

    /// <inheritdoc/>
    public bool Contains(string key) => preferences.ContainsKey(key);

    /// <inheritdoc/>
    public string GetString(string key, string defaultValue) => preferences.Get(key, defaultValue);

    /// <inheritdoc/>
    public int GetInt(string key, int defaultValue) => preferences.Get(key, defaultValue);

    /// <inheritdoc/>
    public bool GetBool(string key, bool defaultValue) => preferences.Get(key, defaultValue);

    /// <inheritdoc/>
    public void Set(string key, string value) => preferences.Set(key, value);

    /// <inheritdoc/>
    public void Set(string key, int value) => preferences.Set(key, value);

    /// <inheritdoc/>
    public void Set(string key, bool value) => preferences.Set(key, value);

    /// <inheritdoc/>
    public void Remove(string key) => preferences.Remove(key);
}
