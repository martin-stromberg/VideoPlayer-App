namespace VideoPlayer.Storage;

/// <summary>
/// Persistiert <see cref="AppSettings"/> in der nicht-geheimen Ablage
/// (<see cref="ISettingsStore"/>, plattformseitig <c>Preferences</c>) mit
/// Profil-Präfix über <see cref="AppProfile"/>.
/// </summary>
public sealed class AppSettingsService
{
    /// <summary>Ablageschlüssel für <see cref="AppSettings.AutoDownloadEnabled"/>.</summary>
    public const string AutoDownloadEnabledKey = "AutoDownloadEnabled";

    /// <summary>Ablageschlüssel für <see cref="AppSettings.PreloadEpisodeCount"/>.</summary>
    public const string PreloadEpisodeCountKey = "PreloadEpisodeCount";

    /// <summary>Ablageschlüssel für <see cref="AppSettings.AutoResumeEnabled"/>.</summary>
    public const string AutoResumeEnabledKey = "AutoResumeEnabled";

    /// <summary>Ablageschlüssel für <see cref="AppSettings.WifiOnlyDownloads"/>.</summary>
    public const string WifiOnlyDownloadsKey = "WifiOnlyDownloads";

    /// <summary>Ablageschlüssel für <see cref="AppSettings.WatchedEndWindowSeconds"/>.</summary>
    public const string WatchedEndWindowSecondsKey = "WatchedEndWindowSeconds";

    /// <summary>Ablageschlüssel für <see cref="AppSettings.DownloadQuotaGb"/>.</summary>
    public const string DownloadQuotaGbKey = "DownloadQuotaGb";

    /// <summary>Ablageschlüssel für <see cref="AppSettings.DownloadRetentionDays"/>.</summary>
    public const string DownloadRetentionDaysKey = "DownloadRetentionDays";

    /// <summary>Ablageschlüssel für <see cref="AppSettings.AutoDeletePlayedDownloads"/>.</summary>
    public const string AutoDeletePlayedDownloadsKey = "AutoDeletePlayedDownloads";

    /// <summary>Ablageschlüssel für <see cref="AppSettings.DeleteAfterPlaybackDelayMinutes"/>.</summary>
    public const string DeleteAfterPlaybackDelayMinutesKey = "DeleteAfterPlaybackDelayMinutes";

    /// <summary>
    /// Ehemaliger Ablageschlüssel der Option „Gesehene Episoden automatisch
    /// löschen" — nur noch Lese-Fallback für die Migration auf
    /// <see cref="AppSettings.AutoDeletePlayedDownloads"/>; wird nicht
    /// mehr geschrieben.
    /// </summary>
    public const string AutoDeleteWatchedEpisodesKey = "AutoDeleteWatchedEpisodes";

    /// <summary>Ablageschlüssel für <see cref="AppSettings.HomeHeroEnabled"/>.</summary>
    public const string HomeHeroEnabledKey = "HomeHeroEnabled";

    /// <summary>Ablageschlüssel für <see cref="AppSettings.LoggingEnabled"/>.</summary>
    public const string LoggingEnabledKey = "LoggingEnabled";

    private readonly ISettingsStore store;
    private readonly AppProfile profile;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="store">Nicht-geheime Ablage für die Einstellungen.</param>
    /// <param name="profile">Aktives Profil für die Schlüssel-Präfixe.</param>
    public AppSettingsService(ISettingsStore store, AppProfile profile)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    /// <summary>Lädt die Einstellungen; fehlende Schlüssel erhalten Defaults, Werte werden normalisiert.</summary>
    /// <returns>Die geladenen, normalisierten Einstellungen.</returns>
    public AppSettings Load()
    {
        var defaults = new AppSettings();
        return new AppSettings
        {
            AutoDownloadEnabled = store.GetBool(Key(AutoDownloadEnabledKey), defaults.AutoDownloadEnabled),
            PreloadEpisodeCount = store.GetInt(Key(PreloadEpisodeCountKey), defaults.PreloadEpisodeCount),
            AutoResumeEnabled = store.GetBool(Key(AutoResumeEnabledKey), defaults.AutoResumeEnabled),
            WifiOnlyDownloads = store.GetBool(Key(WifiOnlyDownloadsKey), defaults.WifiOnlyDownloads),
            WatchedEndWindowSeconds = store.GetInt(Key(WatchedEndWindowSecondsKey), defaults.WatchedEndWindowSeconds),
            DownloadQuotaGb = store.GetInt(Key(DownloadQuotaGbKey), defaults.DownloadQuotaGb),
            DownloadRetentionDays = store.GetInt(Key(DownloadRetentionDaysKey), defaults.DownloadRetentionDays),
            AutoDeletePlayedDownloads = LoadAutoDeletePlayedDownloads(defaults),
            DeleteAfterPlaybackDelayMinutes = store.GetInt(Key(DeleteAfterPlaybackDelayMinutesKey), defaults.DeleteAfterPlaybackDelayMinutes),
            HomeHeroEnabled = store.GetBool(Key(HomeHeroEnabledKey), defaults.HomeHeroEnabled),
            LoggingEnabled = store.GetBool(Key(LoggingEnabledKey), defaults.LoggingEnabled)
        }.Normalize();
    }

    /// <summary>Speichert die Einstellungen (normalisiert vor dem Schreiben).</summary>
    /// <param name="settings">Zu speichernde Einstellungen.</param>
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var normalized = settings.Normalize();

        store.Set(Key(AutoDownloadEnabledKey), normalized.AutoDownloadEnabled);
        store.Set(Key(PreloadEpisodeCountKey), normalized.PreloadEpisodeCount);
        store.Set(Key(AutoResumeEnabledKey), normalized.AutoResumeEnabled);
        store.Set(Key(WifiOnlyDownloadsKey), normalized.WifiOnlyDownloads);
        store.Set(Key(WatchedEndWindowSecondsKey), normalized.WatchedEndWindowSeconds);
        store.Set(Key(DownloadQuotaGbKey), normalized.DownloadQuotaGb);
        store.Set(Key(DownloadRetentionDaysKey), normalized.DownloadRetentionDays);
        store.Set(Key(AutoDeletePlayedDownloadsKey), normalized.AutoDeletePlayedDownloads);
        store.Set(Key(DeleteAfterPlaybackDelayMinutesKey), normalized.DeleteAfterPlaybackDelayMinutes);
        store.Set(Key(HomeHeroEnabledKey), normalized.HomeHeroEnabled);
        store.Set(Key(LoggingEnabledKey), normalized.LoggingEnabled);
    }

    /// <summary>
    /// Liest <see cref="AppSettings.AutoDeletePlayedDownloads"/> mit
    /// Migration: ist der neue Schlüssel vorhanden, gilt dessen Wert;
    /// andernfalls fällt der Wert des Alt-Schlüssels
    /// <see cref="AutoDeleteWatchedEpisodesKey"/> herbei, fehlt auch
    /// dieser, der Standardwert.
    /// </summary>
    /// <param name="defaults">Standardwerte für fehlende Schlüssel.</param>
    /// <returns>Der gelesene Schalterwert.</returns>
    private bool LoadAutoDeletePlayedDownloads(AppSettings defaults)
    {
        var newKey = Key(AutoDeletePlayedDownloadsKey);
        if (store.Contains(newKey))
        {
            return store.GetBool(newKey, defaults.AutoDeletePlayedDownloads);
        }

        return store.GetBool(Key(AutoDeleteWatchedEpisodesKey), defaults.AutoDeletePlayedDownloads);
    }

    private string Key(string key) => profile.PrefixKey(key);
}
