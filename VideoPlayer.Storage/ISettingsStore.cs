namespace VideoPlayer.Storage;

/// <summary>
/// Nicht-geheime Schlüssel/Wert-Ablage (Plattform-Äquivalent von
/// <c>Preferences</c>). Implementierung liegt in der MAUI-App; die Logik
/// bleibt dadurch plattformneutral testbar.
/// </summary>
public interface ISettingsStore
{
    /// <summary>Prüft, ob ein Schlüssel in der Ablage existiert.</summary>
    /// <param name="key">Zu prüfender Schlüssel.</param>
    /// <returns><c>true</c>, wenn der Schlüssel vorhanden ist.</returns>
    bool Contains(string key);

    /// <summary>Liest einen Zeichenfolgen-Wert.</summary>
    /// <param name="key">Schlüssel des Werts.</param>
    /// <param name="defaultValue">Rückgabewert, wenn der Schlüssel fehlt.</param>
    /// <returns>Der gespeicherte Wert oder <paramref name="defaultValue"/>.</returns>
    string GetString(string key, string defaultValue);

    /// <summary>Liest einen Ganzzahl-Wert.</summary>
    /// <param name="key">Schlüssel des Werts.</param>
    /// <param name="defaultValue">Rückgabewert, wenn der Schlüssel fehlt.</param>
    /// <returns>Der gespeicherte Wert oder <paramref name="defaultValue"/>.</returns>
    int GetInt(string key, int defaultValue);

    /// <summary>Liest einen Wahrheitswert.</summary>
    /// <param name="key">Schlüssel des Werts.</param>
    /// <param name="defaultValue">Rückgabewert, wenn der Schlüssel fehlt.</param>
    /// <returns>Der gespeicherte Wert oder <paramref name="defaultValue"/>.</returns>
    bool GetBool(string key, bool defaultValue);

    /// <summary>Speichert einen Zeichenfolgen-Wert.</summary>
    /// <param name="key">Schlüssel des Werts.</param>
    /// <param name="value">Zu speichernder Wert.</param>
    void Set(string key, string value);

    /// <summary>Speichert einen Ganzzahl-Wert.</summary>
    /// <param name="key">Schlüssel des Werts.</param>
    /// <param name="value">Zu speichernder Wert.</param>
    void Set(string key, int value);

    /// <summary>Speichert einen Wahrheitswert.</summary>
    /// <param name="key">Schlüssel des Werts.</param>
    /// <param name="value">Zu speichernder Wert.</param>
    void Set(string key, bool value);

    /// <summary>Entfernt einen Schlüssel aus der Ablage.</summary>
    /// <param name="key">Zu entfernender Schlüssel.</param>
    void Remove(string key);
}
