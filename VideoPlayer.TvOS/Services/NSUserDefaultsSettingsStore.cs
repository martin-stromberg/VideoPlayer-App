using Foundation;
using VideoPlayer.Storage;

namespace VideoPlayer.TvOS.Services
{
    /// <summary>
    /// <see cref="ISettingsStore"/>-Adapter auf die plattformeigene
    /// Einstellungsablage (<see cref="NSUserDefaults"/> — das tvOS-Pendant zu
    /// <c>Preferences</c>). Enthält ausschließlich nicht-geheime Werte und
    /// bleibt deutlich unter dem tvOS-Limit der Standardwerte-Ablage.
    /// </summary>
    public sealed class NSUserDefaultsSettingsStore : ISettingsStore
    {
        private readonly NSUserDefaults defaults;

        /// <summary>Initialisiert eine neue Instanz der <see cref="NSUserDefaultsSettingsStore"/>-Klasse.</summary>
        /// <param name="defaults">Benutzer-Standardwerte der Plattform (<c>NSUserDefaults.StandardUserDefaults</c>).</param>
        public NSUserDefaultsSettingsStore(NSUserDefaults defaults)
        {
            this.defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
        }

        /// <inheritdoc/>
        public bool Contains(string key) => defaults[key] is not null;

        /// <inheritdoc/>
        public string GetString(string key, string defaultValue) =>
            defaults.StringForKey(key) ?? defaultValue;

        /// <inheritdoc/>
        public int GetInt(string key, int defaultValue) =>
            Contains(key) ? (int)defaults.IntForKey(key) : defaultValue;

        /// <inheritdoc/>
        public bool GetBool(string key, bool defaultValue) =>
            Contains(key) ? defaults.BoolForKey(key) : defaultValue;

        /// <inheritdoc/>
        public void Set(string key, string value) => defaults.SetString(value, key);

        /// <inheritdoc/>
        public void Set(string key, int value) => defaults.SetInt(value, key);

        /// <inheritdoc/>
        public void Set(string key, bool value) => defaults.SetBool(value, key);

        /// <inheritdoc/>
        public void Remove(string key) => defaults.RemoveObject(key);
    }
}
