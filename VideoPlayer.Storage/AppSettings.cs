namespace VideoPlayer.Storage;

/// <summary>
/// Unveränderliche Wertmenge aller benutzerbezogenen App-Einstellungen.
/// Wird über <see cref="AppSettingsService"/> persistent gehalten und auf der
/// Einstellungsseite gepflegt. <see cref="Normalize"/> klammert numerische
/// Werte auf ihre zulässigen Schranken.
/// </summary>
public sealed record AppSettings
{
    /// <summary>Standardwert des Speicherplatzkontingents in Gigabyte.</summary>
    public const int DefaultDownloadQuotaGb = 5;

    /// <summary>Standardwert der Aufbewahrungszeit abgeschlossener Downloads in Tagen.</summary>
    public const int DefaultDownloadRetentionDays = 7;

    /// <summary>Standardwert der Löschverzögerung nach dem Abspielen in Minuten.</summary>
    public const int DefaultDeleteAfterPlaybackDelayMinutes = 15;

    /// <summary>Automatische Downloads an/aus.</summary>
    public bool AutoDownloadEnabled { get; init; }

    /// <summary>Anzahl vorzuladender Episoden (1–10).</summary>
    public int PreloadEpisodeCount { get; init; } = 3;

    /// <summary>Auto-Fortsetzung am Wiedergabeende an/aus.</summary>
    public bool AutoResumeEnabled { get; init; } = true;

    /// <summary>Downloads nur im WLAN an/aus.</summary>
    public bool WifiOnlyDownloads { get; init; } = true;

    /// <summary>
    /// Zeitfenster „als angesehen" in Sekunden: ein Video gilt als angesehen,
    /// wenn das Ende abzüglich dieses Fensters erreicht ist (30–3600).
    /// </summary>
    public int WatchedEndWindowSeconds { get; init; } = 300;

    /// <summary>Speicherplatzkontingent für Downloads in Gigabyte (1–500, Standard 5).</summary>
    public int DownloadQuotaGb { get; init; } = DefaultDownloadQuotaGb;

    /// <summary>
    /// Aufbewahrungszeit abgeschlossener Downloads in Tagen (1–365,
    /// Standard <see cref="DefaultDownloadRetentionDays"/>): Grundlage des
    /// Ablaufdatums (<c>ExpiresAtUtc</c>) bei Fertigstellung bzw.
    /// Verlängerung; wirkt nur auf neu gesetzte Ablaufdaten.
    /// </summary>
    public int DownloadRetentionDays { get; init; } = DefaultDownloadRetentionDays;

    /// <summary>
    /// „Downloads nach dem Abspielen automatisch löschen" an/aus: bei
    /// Erreichen der „angesehen"-Schwelle wird das Ablaufdatum auf
    /// <c>jetzt + <see cref="DeleteAfterPlaybackDelayMinutes"/></c>
    /// verkürzt (Standard: aus).
    /// </summary>
    public bool AutoDeletePlayedDownloads { get; init; }

    /// <summary>
    /// Löschverzögerung nach dem Abspielen in Minuten (0–1440, Standard
    /// <see cref="DefaultDeleteAfterPlaybackDelayMinutes"/>); 0 = Ablauf
    /// sofort → Entfernung beim nächsten Bereinigungslauf.
    /// </summary>
    public int DeleteAfterPlaybackDelayMinutes { get; init; } = DefaultDeleteAfterPlaybackDelayMinutes;

    /// <summary>
    /// Hero-Kopfbereich der Startseite anzeigen (positiv formuliert —
    /// <c>true</c> = der Kopfbereich mit dem servergenerierten Bild wird
    /// gezeigt; Standard: aktiviert).
    /// </summary>
    public bool HomeHeroEnabled { get; init; } = true;

    /// <summary>
    /// Protokollierung an/aus: bei <c>true</c> schreibt die App ein
    /// Programmprotokoll in die Protokolldatei
    /// (<see cref="AppStorageLayout.LogsDirectory"/>/<c>videoplayer.log</c>;
    /// Standard: deaktiviert).
    /// </summary>
    public bool LoggingEnabled { get; init; }

    /// <summary>Liefert eine Kopie mit auf die zulässigen Schranken geklammerten Werten.</summary>
    /// <returns>Eine Kopie der Einstellungen mit normalisierten Werten.</returns>
    public AppSettings Normalize() => this with
    {
        PreloadEpisodeCount = Math.Clamp(PreloadEpisodeCount, 1, 10),
        WatchedEndWindowSeconds = Math.Clamp(WatchedEndWindowSeconds, 30, 3600),
        DownloadQuotaGb = Math.Clamp(DownloadQuotaGb, 1, 500),
        DownloadRetentionDays = Math.Clamp(DownloadRetentionDays, 1, 365),
        DeleteAfterPlaybackDelayMinutes = Math.Clamp(DeleteAfterPlaybackDelayMinutes, 0, 1440)
    };
}
