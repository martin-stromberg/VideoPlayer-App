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

    /// <summary>Protokollierung ist aktiviert (Einstellung „Protokollierung").</summary>
    public bool SettingsLoggingEnabled { get; init; }

    /// <summary>Fehlertext des Protokollversands; <c>null</c> = kein Fehler.</summary>
    public string? LogSendError { get; init; }

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

    /// <summary>Id der Playlist der laufenden Playlist-Wiedergabe (0 = kein Playlist-Kontext).</summary>
    public long PlayerPlaylistId { get; init; }

    /// <summary>Id des aktuell gespielten Playlist-Eintrags (0 = kein Playlist-Kontext).</summary>
    public long PlayerPlaylistEntryId { get; init; }

    /// <summary>1-basierte Position des aktuellen Eintrags in der Playlist (0 = unbekannt/kein Kontext).</summary>
    public int PlayerPlaylistPosition { get; init; }

    /// <summary>Fehlertext der letzten Wiedergabe-Störung; <c>null</c> = kein Fehler.</summary>
    public string? PlayerError { get; init; }

    /// <summary>Anzahl der Playlist-Eintragszeilen der Übersichtsseite (ohne Sektionsüberschriften).</summary>
    public int PlaylistsPageItemCount { get; init; }

    /// <summary>Fehlertext der Playlists-Übersichtsseite; <c>null</c> = kein Fehler.</summary>
    public string? PlaylistsPageError { get; init; }

    /// <summary>Playlist-Id der geöffneten Playlist-Detailseite (0 = keine).</summary>
    public long PlaylistDetailsId { get; init; }

    /// <summary>Anzahl der geladenen Einträge der Playlist-Detailseite.</summary>
    public int PlaylistDetailsEntryCount { get; init; }

    /// <summary>Fehlertext der Playlist-Detailseite; <c>null</c> = kein Fehler.</summary>
    public string? PlaylistDetailsError { get; init; }

    /// <summary>Anzeigetitel der in der Playlist-Detailansicht geladenen Einträge (Anzeigereihenfolge).</summary>
    public IReadOnlyList<string> PlaylistEntryTitles { get; init; } = [];

    /// <summary>Eintrags-Ids der Playlist-Detailansicht in Anzeigereihenfolge (Nachweis von Verschiebungen).</summary>
    public IReadOnlyList<long> PlaylistEntryIds { get; init; } = [];

    /// <summary>Verwaltungsaktionen der geöffneten Playlist sind sichtbar (eigene Playlist).</summary>
    public bool CanManagePlaylist { get; init; }

    /// <summary>Der angemeldete Benutzer ist Besitzer der geöffneten Playlist.</summary>
    public bool PlaylistIsOwner { get; init; }

    /// <summary>Das gespeicherte Anmelde-Token trägt den Administrator-Claim (Voraussetzung des Öffentlich-Schalters).</summary>
    public bool PlaylistIsAdmin { get; init; }

    /// <summary>Die geöffnete Playlist ist öffentlich geschaltet.</summary>
    public bool PlaylistIsPublic { get; init; }

    /// <summary>Sortiermodus der geöffneten Playlist (<c>PlaylistSortModeValues</c>).</summary>
    public string PlaylistSortMode { get; init; } = string.Empty;

    /// <summary>Kommaseparierte Anzeige-Genres der geöffneten Playlist.</summary>
    public string PlaylistGenreText { get; init; } = string.Empty;

    /// <summary>Genres der geöffneten Playlist wurden manuell gesetzt.</summary>
    public bool PlaylistGenresManuallyOverridden { get; init; }

    /// <summary>Hinweistext der letzten Verwaltungsoperation der Playlist-Detailseite; <c>null</c> = kein Hinweis.</summary>
    public string? PlaylistManageNotice { get; init; }

    /// <summary><c>true</c>, wenn <see cref="PlaylistManageNotice"/> einen Fehler beschreibt.</summary>
    public bool PlaylistManageNoticeIsError { get; init; }

    /// <summary>Id des Eintrags mit ausstehender Weiterschauen-Bestätigung (HTTP 409); <c>null</c> = kein Konflikt.</summary>
    public long? PendingRemoveConflictEntryId { get; init; }

    /// <summary>Angeforderter Sortiermodus mit ausstehender Verlust-Bestätigung (HTTP 409); <c>null</c> = kein Konflikt.</summary>
    public string? PendingSortModeConflict { get; init; }

    /// <summary>Das Playlist-Formular (Anlage/Bearbeitung) ist geöffnet.</summary>
    public bool PlaylistFormOpen { get; init; }

    /// <summary>Das Playlist-Formular speichert gerade.</summary>
    public bool PlaylistFormSaving { get; init; }

    /// <summary>Fehlertext des Playlist-Formulars; <c>null</c> = kein Fehler.</summary>
    public string? PlaylistFormError { get; init; }

    /// <summary>Der Genre-Editor ist geöffnet.</summary>
    public bool PlaylistGenreEditorOpen { get; init; }

    /// <summary>Genres des Editors: Id, Name und Auswahlzustand in Anzeigereihenfolge.</summary>
    public IReadOnlyList<DevApiGenreOption> PlaylistGenreOptions { get; init; } = [];

    /// <summary>Namen der wählbaren Ziel-Playlists der Medien-Detailseite; <c>null</c> = noch nicht geladen.</summary>
    public IReadOnlyList<string>? PlaylistTargets { get; init; }

    /// <summary>Die Playlist-Auswahl der Medien-Detailseite soll geöffnet sein.</summary>
    public bool PlaylistPickerPending { get; init; }

    /// <summary>Hinweistext der Playlist-Hinzufügung der Medien-Detailseite; <c>null</c> = kein Hinweis.</summary>
    public string? AddToPlaylistNotice { get; init; }

    /// <summary><c>true</c>, wenn <see cref="AddToPlaylistNotice"/> einen Fehler beschreibt.</summary>
    public bool AddToPlaylistNoticeIsError { get; init; }

    /// <summary>Anzeigenamen der Staffeln der Serienansicht in Modellreihenfolge (leer ohne Serien-Details).</summary>
    public IReadOnlyList<string> MediaDetailsSeasonNames { get; init; } = [];

    /// <summary>Staffelnummer der gewählten Staffel der Serienansicht (0 = keine Serien-Details).</summary>
    public int MediaDetailsSelectedSeasonNumber { get; init; }

    /// <summary>Anzeigetitel der Episoden der gewählten Staffel (leer ohne Serien-Details).</summary>
    public IReadOnlyList<string> MediaDetailsSeasonEpisodeTitles { get; init; } = [];

    /// <summary>Id der im Quellen-Browser geöffneten Quelle (0 = kein Browser aktiv).</summary>
    public long SourceBrowserSourceId { get; init; }

    /// <summary>Anzeigenamen der Genre-Leiste des Quellen-Browsers in Anzeigereihenfolge (inkl. „Alle"-Eintrag).</summary>
    public IReadOnlyList<string> SourceBrowserGenreNames { get; init; } = [];

    /// <summary>Aktuell gewählter Genre-Filter des Quellen-Browsers (0 = „Alle").</summary>
    public long SourceBrowserSelectedGenreId { get; init; }

    /// <summary>Die Genre-Leiste des Quellen-Browsers ist sichtbar (reale Server-Genres vorhanden).</summary>
    public bool SourceBrowserShowGenreFilter { get; init; }

    /// <summary>Anzeigetitel der geladenen Einträge des Quellen-Browsers (Anzeigereihenfolge).</summary>
    public IReadOnlyList<string> SourceBrowserItemTitles { get; init; } = [];

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
            SettingsLoggingEnabled = model.Settings.LoggingEnabled,
            LogSendError = model.LogSendError,
            HeroTitle = model.HeroTitle,
            ContinueWatchingCount = model.ContinueWatching.Count,
            FavoritesCount = model.Favorites.Count,
            RecentEntriesCount = model.RecentEntries.Count,
            PlayerIsActive = model.Player.IsActive,
            PlayerMediaId = model.Player.MediaId,
            PlayerUpcomingEpisodeCount = model.Player.UpcomingEpisodes.Count,
            PlayerIsPlaying = model.Player.IsPlaying,
            PlayerPositionSeconds = model.Player.Position.TotalSeconds,
            PlayerPlaylistId = model.Player.PlaylistId ?? 0,
            PlayerPlaylistEntryId = model.Player.PlaylistEntryId,
            PlayerPlaylistPosition = model.Player.PlaylistPosition,
            PlayerError = model.Player.Error,
            PlaylistsPageItemCount = model.PlaylistsPage.Items.Count(i => !i.IsHeader),
            PlaylistsPageError = model.PlaylistsPage.Error,
            PlaylistDetailsId = model.PlaylistDetails.PlaylistId,
            PlaylistDetailsEntryCount = model.PlaylistDetails.Entries.Count,
            PlaylistDetailsError = model.PlaylistDetails.Error,
            PlaylistEntryTitles = model.PlaylistDetails.Entries.Select(e => e.Title).ToList(),
            PlaylistEntryIds = model.PlaylistDetails.Entries.Select(e => e.EntryId).ToList(),
            CanManagePlaylist = model.PlaylistDetails.CanManage,
            PlaylistIsOwner = model.PlaylistDetails.IsOwner,
            PlaylistIsAdmin = model.PlaylistDetails.IsAdmin,
            PlaylistIsPublic = model.PlaylistDetails.IsPublic,
            PlaylistSortMode = model.PlaylistDetails.SortMode,
            PlaylistGenreText = model.PlaylistDetails.GenreText,
            PlaylistGenresManuallyOverridden = model.PlaylistDetails.GenresManuallyOverridden,
            PlaylistManageNotice = model.PlaylistDetails.ManageNotice,
            PlaylistManageNoticeIsError = model.PlaylistDetails.ManageNoticeIsError,
            PendingRemoveConflictEntryId = model.PlaylistDetails.PendingRemoveConflict?.EntryId,
            PendingSortModeConflict = model.PlaylistDetails.PendingSortModeConflict,
            PlaylistFormOpen = model.PlaylistForm.IsOpen,
            PlaylistFormSaving = model.PlaylistForm.IsSaving,
            PlaylistFormError = model.PlaylistForm.Error,
            PlaylistGenreEditorOpen = model.PlaylistGenreEditor.IsOpen,
            PlaylistGenreOptions = model.PlaylistGenreEditor.Options
                .Select(o => new DevApiGenreOption
                {
                    Id = o.Id,
                    Name = o.Name,
                    IsSelected = o.IsSelected
                })
                .ToList(),
            PlaylistTargets = model.MediaDetails.PlaylistTargets?.Select(p => p.Name).ToList(),
            PlaylistPickerPending = model.MediaDetails.PlaylistPickerPending,
            AddToPlaylistNotice = model.MediaDetails.AddToPlaylistNotice,
            AddToPlaylistNoticeIsError = model.MediaDetails.AddToPlaylistNoticeIsError,
            MediaDetailsSeasonNames = model.MediaDetails.Details?.Seasons
                .Select(s => s.Name).ToList() ?? [],
            MediaDetailsSelectedSeasonNumber = model.MediaDetails.Details?.SelectedSeason?.Number ?? 0,
            MediaDetailsSeasonEpisodeTitles = model.MediaDetails.Details?.SelectedSeasonEpisodes
                .Select(e => e.DisplayTitle).ToList() ?? [],
            SourceBrowserSourceId = model.SourceBrowser.SourceId,
            SourceBrowserGenreNames = model.SourceBrowser.Genres.Select(g => g.Name).ToList(),
            SourceBrowserSelectedGenreId = model.SourceBrowser.SelectedGenreId,
            SourceBrowserShowGenreFilter = model.SourceBrowser.ShowGenreFilter,
            SourceBrowserItemTitles = model.SourceBrowser.Items.Select(i => i.Title).ToList()
        };
    }
}

/// <summary>Serialisierbarer Auszug einer Genre-Option des Playlist-Genre-Editors.</summary>
public sealed record DevApiGenreOption
{
    /// <summary>Id des Genres.</summary>
    public long Id { get; init; }

    /// <summary>Anzeigename des Genres.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Das Genre ist gewählt.</summary>
    public bool IsSelected { get; init; }
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
