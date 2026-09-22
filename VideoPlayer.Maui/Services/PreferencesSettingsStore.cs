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

    public PreferencesSettingsStore(IPreferences preferences)
    {
        this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
    }

    public bool Contains(string key) => preferences.ContainsKey(key);

    public string GetString(string key, string defaultValue) => preferences.Get(key, defaultValue);

    public int GetInt(string key, int defaultValue) => preferences.Get(key, defaultValue);

    public bool GetBool(string key, bool defaultValue) => preferences.Get(key, defaultValue);

    public void Set(string key, string value) => preferences.Set(key, value);

    public void Set(string key, int value) => preferences.Set(key, value);

    public void Set(string key, bool value) => preferences.Set(key, value);

    public void Remove(string key) => preferences.Remove(key);
}
