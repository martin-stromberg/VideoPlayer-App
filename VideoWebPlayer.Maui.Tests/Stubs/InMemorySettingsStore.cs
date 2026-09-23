using VideoPlayer.Storage;

namespace VideoWebPlayer.Maui.Tests.Stubs;

/// <summary>In-Memory-Fake der nicht-geheimen Ablage für Tests.</summary>
internal sealed class InMemorySettingsStore : ISettingsStore
{
    private readonly Dictionary<string, object> values = new();

    public bool Contains(string key) => values.ContainsKey(key);

    public string GetString(string key, string defaultValue) =>
        values.TryGetValue(key, out var v) && v is string s ? s : defaultValue;

    public int GetInt(string key, int defaultValue) =>
        values.TryGetValue(key, out var v) && v is int i ? i : defaultValue;

    public bool GetBool(string key, bool defaultValue) =>
        values.TryGetValue(key, out var v) && v is bool b ? b : defaultValue;

    public void Set(string key, string value) => values[key] = value;

    public void Set(string key, int value) => values[key] = value;

    public void Set(string key, bool value) => values[key] = value;

    public void Remove(string key) => values.Remove(key);

    /// <summary>Alle belegten Schlüssel (für Isolations-/Geheimnis-Prüfungen).</summary>
    public IReadOnlyCollection<string> Keys => values.Keys;
}
