namespace VideoPlayer.Storage;

/// <summary>
/// Nicht-geheime Schlüssel/Wert-Ablage (Plattform-Äquivalent von
/// <c>Preferences</c>). Implementierung liegt in der MAUI-App; die Logik
/// bleibt dadurch plattformneutral testbar.
/// </summary>
public interface ISettingsStore
{
    bool Contains(string key);
    string GetString(string key, string defaultValue);
    int GetInt(string key, int defaultValue);
    bool GetBool(string key, bool defaultValue);
    void Set(string key, string value);
    void Set(string key, int value);
    void Set(string key, bool value);
    void Remove(string key);
}
