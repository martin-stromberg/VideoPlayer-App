#if DEBUG || DEV_API
namespace VideoPlayer.Mvu.DevApi;

/// <summary>
/// Kuratierter, JSON-serialisierbarer Auszug des <see cref="AppModel"/>
/// für <c>GET /model</c>: nur Zustandsfelder ohne Geheimnisse (Tokens und
/// Passwörter stehen per Konstruktion nicht im Modell).
/// </summary>
public sealed record DevApiModelSnapshot
{
    /// <summary>Aktuell angezeigte Seite.</summary>
    public string CurrentPage { get; init; } = string.Empty;

    /// <summary>Verbindungszustand der App.</summary>
    public string ConnectionState { get; init; } = string.Empty;

    /// <summary>Ergebnis der Server-Erreichbarkeitsprüfung.</summary>
    public string ServerHealth { get; init; } = string.Empty;

    /// <summary>Anzeigbarer Serverstatustext.</summary>
    public string ServerHealthText { get; init; } = string.Empty;

    /// <summary>Start-Workflow läuft.</summary>
    public bool IsStartupRunning { get; init; }

    /// <summary>Anmeldeversuch läuft.</summary>
    public bool IsLoginRunning { get; init; }

    /// <summary>Inline-Fehlertext der Anmeldemaske.</summary>
    public string? LoginError { get; init; }

    /// <summary>Vorbefüllter Benutzername der Anmeldemaske.</summary>
    public string? LoginUsername { get; init; }

    /// <summary>Letzter bekannter Netzwerkstatus.</summary>
    public bool IsOnline { get; init; }

    /// <summary>Die App befindet sich im Offline-Zustand.</summary>
    public bool IsOffline { get; init; }

    /// <summary>Lokale Offline-Daten vorhanden.</summary>
    public bool HasOfflineData { get; init; }

    /// <summary>Vollständige Synchronisation läuft.</summary>
    public bool IsSyncRunning { get; init; }

    /// <summary>Kanonische Adresse des aktiven Servers.</summary>
    public string? ActiveServerAddress { get; init; }

    /// <summary>Bekannte Server.</summary>
    public IReadOnlyList<DevApiKnownServer> KnownServers { get; init; } = [];

    /// <summary>Gefundene Server-Kandidaten.</summary>
    public IReadOnlyList<DevApiDiscoveredServer> DiscoveredServers { get; init; } = [];

    /// <summary>Netzwerk-Erkennung läuft.</summary>
    public bool IsDiscoveryRunning { get; init; }

    /// <summary>Netzwerk-Erkennung wurde durchgeführt.</summary>
    public bool DiscoveryAttempted { get; init; }

    /// <summary>Verbindungsprüfung läuft.</summary>
    public bool IsServerCheckRunning { get; init; }

    /// <summary>Fehlertext der Server-Einrichtung.</summary>
    public string? ServerSetupError { get; init; }

    /// <summary>Der Server verlangt ein Geräte-Pairing.</summary>
    public bool IsPairingRequired { get; init; }

    /// <summary>Der Pairing-Austausch läuft.</summary>
    public bool IsPairingRunning { get; init; }

    /// <summary>Inline-Fehlertext der Pairing-Eingabe.</summary>
    public string? PairingError { get; init; }

    /// <summary>Der Bootstrap-Austausch des QR-/Ticket-Onboardings läuft.</summary>
    public bool IsBootstrapRunning { get; init; }

    /// <summary>Inline-Fehlertext der Kopplungs-Eingabe.</summary>
    public string? BootstrapError { get; init; }

    /// <summary>Hinweis der Ablage-Wiederherstellung.</summary>
    public string? StorageNotice { get; init; }

    /// <summary>Titelzeile des Hero-Kopfbereichs.</summary>
    public string HeroTitle { get; init; } = string.Empty;

    /// <summary>Anzahl der Weiterschauen-Einträge.</summary>
    public int ContinueWatchingCount { get; init; }

    /// <summary>Anzahl der Favoriten-Einträge.</summary>
    public int FavoritesCount { get; init; }

    /// <summary>Anzahl der Neu-im-Programm-Einträge.</summary>
    public int RecentEntriesCount { get; init; }

    /// <summary>Eine Wiedergabe ist angefordert (Player-Zustand belegt).</summary>
    public bool PlayerIsActive { get; init; }

    /// <summary>Medien-Id des aktuell abzuspielenden Mediums (0 = keine).</summary>
    public long PlayerMediaId { get; init; }

    /// <summary>Anzahl der bereitstehenden Folge-Episoden (Folgevideo-Funktion).</summary>
    public int PlayerUpcomingEpisodeCount { get; init; }

    /// <summary>Die Wiedergabe läuft gerade.</summary>
    public bool PlayerIsPlaying { get; init; }

    /// <summary>Zuletzt gemeldete Wiedergabeposition in Sekunden.</summary>
    public double PlayerPositionSeconds { get; init; }

    /// <summary>Fehlertext der letzten Wiedergabe-Störung; <c>null</c> = kein Fehler.</summary>
    public string? PlayerError { get; init; }

    /// <summary>Erzeugt den Snapshot aus dem aktuellen Modell.</summary>
    /// <param name="model">Das unveränderliche App-Modell.</param>
    /// <returns>Der serialisierbare Auszug.</returns>
    public static DevApiModelSnapshot FromModel(AppModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return new DevApiModelSnapshot
        {
            CurrentPage = model.CurrentPage.ToString(),
            ConnectionState = model.ConnectionState.ToString(),
            ServerHealth = model.ServerHealth.ToString(),
            ServerHealthText = model.ServerHealthText,
            IsStartupRunning = model.IsStartupRunning,
            IsLoginRunning = model.IsLoginRunning,
            LoginError = model.LoginError,
            LoginUsername = model.LoginUsername,
            IsOnline = model.IsOnline,
            IsOffline = model.IsOffline,
            HasOfflineData = model.HasOfflineData,
            IsSyncRunning = model.IsSyncRunning,
            ActiveServerAddress = model.ActiveServerAddress,
            KnownServers = model.KnownServers
                .Select(s => new DevApiKnownServer
                {
                    Address = s.Address,
                    DisplayName = s.DisplayName,
                    NameOrAddress = s.NameOrAddress,
                    LastUsedAtUtc = s.LastUsedAtUtc
                })
                .ToList(),
            DiscoveredServers = model.DiscoveredServers
                .Select(s => new DevApiDiscoveredServer
                {
                    Address = s.Address,
                    Name = s.Name,
                    NameOrAddress = s.NameOrAddress,
                    Source = s.Source.ToString()
                })
                .ToList(),
            IsDiscoveryRunning = model.IsDiscoveryRunning,
            DiscoveryAttempted = model.DiscoveryAttempted,
            IsServerCheckRunning = model.IsServerCheckRunning,
            ServerSetupError = model.ServerSetupError,
            IsPairingRequired = model.IsPairingRequired,
            IsPairingRunning = model.IsPairingRunning,
            PairingError = model.PairingError,
            IsBootstrapRunning = model.IsBootstrapRunning,
            BootstrapError = model.BootstrapError,
            StorageNotice = model.StorageNotice,
            HeroTitle = model.HeroTitle,
            ContinueWatchingCount = model.ContinueWatching.Count,
            FavoritesCount = model.Favorites.Count,
            RecentEntriesCount = model.RecentEntries.Count,
            PlayerIsActive = model.Player.IsActive,
            PlayerMediaId = model.Player.MediaId,
            PlayerUpcomingEpisodeCount = model.Player.UpcomingEpisodes.Count,
            PlayerIsPlaying = model.Player.IsPlaying,
            PlayerPositionSeconds = model.Player.Position.TotalSeconds,
            PlayerError = model.Player.Error
        };
    }
}

/// <summary>Serialisierbarer Auszug eines bekannten Servers.</summary>
public sealed record DevApiKnownServer
{
    /// <summary>Kanonische Serveradresse.</summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>Anzeigename (leer, wenn keiner vergeben).</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Anzeigename oder Adresse.</summary>
    public string NameOrAddress { get; init; } = string.Empty;

    /// <summary>Zeitpunkt der letzten Nutzung (UTC).</summary>
    public DateTime LastUsedAtUtc { get; init; }
}

/// <summary>Serialisierbarer Auszug eines gefundenen Servers.</summary>
public sealed record DevApiDiscoveredServer
{
    /// <summary>Normalisierte Serveradresse.</summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>Anzeigename der Discovery-Quelle.</summary>
    public string? Name { get; init; }

    /// <summary>Anzeigename oder Adresse.</summary>
    public string NameOrAddress { get; init; } = string.Empty;

    /// <summary>Discovery-Weg.</summary>
    public string Source { get; init; } = string.Empty;
}
#endif
