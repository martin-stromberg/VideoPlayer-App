using System.Text.Json.Serialization;

namespace VideoPlayer.E2E.Tests.Support;

/// <summary>
/// DTO des <c>GET /model</c>-Snapshots der Developer-API
/// (camelCase-Wire-Format, Spiegel von <c>DevApiModelSnapshot</c>).
/// </summary>
public sealed class DevApiModel
{
    /// <summary>Name der aktuell sichtbaren Seite (z. B. <c>ServerSetup</c>, <c>Home</c>).</summary>
    [JsonPropertyName("currentPage")] public string? CurrentPage { get; set; }

    /// <summary>Verbindungszustand (<c>NeedsServerSetup</c>, <c>NeedsLogin</c>, <c>Connected</c>, …).</summary>
    [JsonPropertyName("connectionState")] public string? ConnectionState { get; set; }

    /// <summary>Server-Gesundheitszustand des letzten Checks.</summary>
    [JsonPropertyName("serverHealth")] public string? ServerHealth { get; set; }

    /// <summary>Anzeigetext des Server-Gesundheitszustands.</summary>
    [JsonPropertyName("serverHealthText")] public string? ServerHealthText { get; set; }

    /// <summary>Startprüfung läuft.</summary>
    [JsonPropertyName("isStartupRunning")] public bool IsStartupRunning { get; set; }

    /// <summary>Anmeldung läuft.</summary>
    [JsonPropertyName("isLoginRunning")] public bool IsLoginRunning { get; set; }

    /// <summary>Fehlertext der letzten Anmeldung.</summary>
    [JsonPropertyName("loginError")] public string? LoginError { get; set; }

    /// <summary>Zuletzt eingegebener Anmeldename.</summary>
    [JsonPropertyName("loginUsername")] public string? LoginUsername { get; set; }

    /// <summary>Server aktuell online erreichbar.</summary>
    [JsonPropertyName("isOnline")] public bool IsOnline { get; set; }

    /// <summary>Offline-Modus aktiv.</summary>
    [JsonPropertyName("isOffline")] public bool IsOffline { get; set; }

    /// <summary>Lokale Offline-Daten vorhanden.</summary>
    [JsonPropertyName("hasOfflineData")] public bool HasOfflineData { get; set; }

    /// <summary>Synchronisation läuft.</summary>
    [JsonPropertyName("isSyncRunning")] public bool IsSyncRunning { get; set; }

    /// <summary>Aktive Serveradresse (kanonisch, mit abschließendem <c>/</c>).</summary>
    [JsonPropertyName("activeServerAddress")] public string? ActiveServerAddress { get; set; }

    /// <summary>Bekannte Server des Profils.</summary>
    [JsonPropertyName("knownServers")] public List<DevApiKnownServer> KnownServers { get; set; } = [];

    /// <summary>Per Suche gefundene Server.</summary>
    [JsonPropertyName("discoveredServers")] public List<DevApiDiscoveredServer> DiscoveredServers { get; set; } = [];

    /// <summary>Server-Suche läuft.</summary>
    [JsonPropertyName("isDiscoveryRunning")] public bool IsDiscoveryRunning { get; set; }

    /// <summary>Server-Suche wurde mindestens einmal ausgeführt.</summary>
    [JsonPropertyName("discoveryAttempted")] public bool DiscoveryAttempted { get; set; }

    /// <summary>Erreichbarkeitsprüfung läuft.</summary>
    [JsonPropertyName("isServerCheckRunning")] public bool IsServerCheckRunning { get; set; }

    /// <summary>Fehlertext der letzten Server-Einrichtung.</summary>
    [JsonPropertyName("serverSetupError")] public string? ServerSetupError { get; set; }

    /// <summary>Geräte-Pairing wird eingefordert.</summary>
    [JsonPropertyName("isPairingRequired")] public bool IsPairingRequired { get; set; }

    /// <summary>Pairing-Austausch läuft.</summary>
    [JsonPropertyName("isPairingRunning")] public bool IsPairingRunning { get; set; }

    /// <summary>Fehlertext des letzten Pairings.</summary>
    [JsonPropertyName("pairingError")] public string? PairingError { get; set; }

    /// <summary>Der Bootstrap-Austausch des QR-/Ticket-Onboardings läuft.</summary>
    [JsonPropertyName("isBootstrapRunning")] public bool IsBootstrapRunning { get; set; }

    /// <summary>Inline-Fehlertext der Kopplungs-Eingabe.</summary>
    [JsonPropertyName("bootstrapError")] public string? BootstrapError { get; set; }

    /// <summary>Hinweistext der Ablage.</summary>
    [JsonPropertyName("storageNotice")] public string? StorageNotice { get; set; }

    /// <summary>Protokollierung ist aktiviert (Einstellung „Protokollierung").</summary>
    [JsonPropertyName("settingsLoggingEnabled")] public bool SettingsLoggingEnabled { get; set; }

    /// <summary>Fehlertext des Protokollversands.</summary>
    [JsonPropertyName("logSendError")] public string? LogSendError { get; set; }

    /// <summary>Titel des Hero-Bereichs der Startseite.</summary>
    [JsonPropertyName("heroTitle")] public string? HeroTitle { get; set; }

    /// <summary>Anzahl der Fortsetzen-Einträge.</summary>
    [JsonPropertyName("continueWatchingCount")] public int ContinueWatchingCount { get; set; }

    /// <summary>Anzahl der Favoriten.</summary>
    [JsonPropertyName("favoritesCount")] public int FavoritesCount { get; set; }

    /// <summary>Anzahl der zuletzt gesehenen Einträge.</summary>
    [JsonPropertyName("recentEntriesCount")] public int RecentEntriesCount { get; set; }

    /// <summary>Eine Wiedergabe ist angefordert (Player-Seite belegt).</summary>
    [JsonPropertyName("playerIsActive")] public bool PlayerIsActive { get; set; }

    /// <summary>Medien-Id des aktuell abzuspielenden Mediums (0 = keine).</summary>
    [JsonPropertyName("playerMediaId")] public long PlayerMediaId { get; set; }

    /// <summary>Anzahl der bereitstehenden Folge-Episoden.</summary>
    [JsonPropertyName("playerUpcomingEpisodeCount")] public int PlayerUpcomingEpisodeCount { get; set; }

    /// <summary>Die Wiedergabe läuft gerade.</summary>
    [JsonPropertyName("playerIsPlaying")] public bool PlayerIsPlaying { get; set; }

    /// <summary>Zuletzt gemeldete Wiedergabeposition in Sekunden.</summary>
    [JsonPropertyName("playerPositionSeconds")] public double PlayerPositionSeconds { get; set; }

    /// <summary>Id der Playlist der laufenden Playlist-Wiedergabe (0 = kein Kontext).</summary>
    [JsonPropertyName("playerPlaylistId")] public long PlayerPlaylistId { get; set; }

    /// <summary>Id des aktuell gespielten Playlist-Eintrags (0 = kein Kontext).</summary>
    [JsonPropertyName("playerPlaylistEntryId")] public long PlayerPlaylistEntryId { get; set; }

    /// <summary>1-basierte Position des aktuellen Eintrags in der Playlist (0 = unbekannt).</summary>
    [JsonPropertyName("playerPlaylistPosition")] public int PlayerPlaylistPosition { get; set; }

    /// <summary>Fehlertext der letzten Wiedergabe-Störung.</summary>
    [JsonPropertyName("playerError")] public string? PlayerError { get; set; }

    /// <summary>Anzahl der Playlist-Einträge der Übersichtsseite (ohne Sektionsüberschriften).</summary>
    [JsonPropertyName("playlistsPageItemCount")] public int PlaylistsPageItemCount { get; set; }

    /// <summary>Fehlertext der Playlists-Übersichtsseite.</summary>
    [JsonPropertyName("playlistsPageError")] public string? PlaylistsPageError { get; set; }

    /// <summary>Playlist-Id der geöffneten Detailseite (0 = keine).</summary>
    [JsonPropertyName("playlistDetailsId")] public long PlaylistDetailsId { get; set; }

    /// <summary>Anzahl geladener Einträge der Playlist-Detailseite.</summary>
    [JsonPropertyName("playlistDetailsEntryCount")] public int PlaylistDetailsEntryCount { get; set; }

    /// <summary>Fehlertext der Playlist-Detailseite.</summary>
    [JsonPropertyName("playlistDetailsError")] public string? PlaylistDetailsError { get; set; }

    /// <summary>Anzeigetitel der geladenen Playlist-Einträge (Anzeigereihenfolge).</summary>
    [JsonPropertyName("playlistEntryTitles")] public List<string>? PlaylistEntryTitles { get; set; }

    /// <summary>Eintrags-Ids der Playlist-Detailansicht (Anzeigereihenfolge).</summary>
    [JsonPropertyName("playlistEntryIds")] public List<long>? PlaylistEntryIds { get; set; }

    /// <summary>Verwaltungsaktionen der geöffneten Playlist sind sichtbar.</summary>
    [JsonPropertyName("canManagePlaylist")] public bool CanManagePlaylist { get; set; }

    /// <summary>Der angemeldete Benutzer ist Besitzer der geöffneten Playlist.</summary>
    [JsonPropertyName("playlistIsOwner")] public bool PlaylistIsOwner { get; set; }

    /// <summary>Das Anmelde-Token trägt den Administrator-Claim (Voraussetzung des Öffentlich-Schalters).</summary>
    [JsonPropertyName("playlistIsAdmin")] public bool PlaylistIsAdmin { get; set; }

    /// <summary>Die geöffnete Playlist ist öffentlich geschaltet.</summary>
    [JsonPropertyName("playlistIsPublic")] public bool PlaylistIsPublic { get; set; }

    /// <summary>Sortiermodus der geöffneten Playlist.</summary>
    [JsonPropertyName("playlistSortMode")] public string? PlaylistSortMode { get; set; }

    /// <summary>Kommaseparierte Anzeige-Genres der geöffneten Playlist.</summary>
    [JsonPropertyName("playlistGenreText")] public string? PlaylistGenreText { get; set; }

    /// <summary>Genres der geöffneten Playlist wurden manuell gesetzt.</summary>
    [JsonPropertyName("playlistGenresManuallyOverridden")] public bool PlaylistGenresManuallyOverridden { get; set; }

    /// <summary>Hinweistext der letzten Verwaltungsoperation.</summary>
    [JsonPropertyName("playlistManageNotice")] public string? PlaylistManageNotice { get; set; }

    /// <summary><c>true</c>, wenn der Hinweis einen Fehler beschreibt.</summary>
    [JsonPropertyName("playlistManageNoticeIsError")] public bool PlaylistManageNoticeIsError { get; set; }

    /// <summary>Id des Eintrags mit ausstehender Weiterschauen-Bestätigung; <c>null</c> = kein Konflikt.</summary>
    [JsonPropertyName("pendingRemoveConflictEntryId")] public long? PendingRemoveConflictEntryId { get; set; }

    /// <summary>Angeforderter Sortiermodus mit ausstehender Verlust-Bestätigung; <c>null</c> = kein Konflikt.</summary>
    [JsonPropertyName("pendingSortModeConflict")] public string? PendingSortModeConflict { get; set; }

    /// <summary>Das Playlist-Formular ist geöffnet.</summary>
    [JsonPropertyName("playlistFormOpen")] public bool PlaylistFormOpen { get; set; }

    /// <summary>Das Playlist-Formular speichert gerade.</summary>
    [JsonPropertyName("playlistFormSaving")] public bool PlaylistFormSaving { get; set; }

    /// <summary>Fehlertext des Playlist-Formulars.</summary>
    [JsonPropertyName("playlistFormError")] public string? PlaylistFormError { get; set; }

    /// <summary>Der Genre-Editor ist geöffnet.</summary>
    [JsonPropertyName("playlistGenreEditorOpen")] public bool PlaylistGenreEditorOpen { get; set; }

    /// <summary>Genres des Editors (Id, Name, Auswahlzustand).</summary>
    [JsonPropertyName("playlistGenreOptions")] public List<DevApiGenreOption>? PlaylistGenreOptions { get; set; }

    /// <summary>Namen der wählbaren Ziel-Playlists; <c>null</c> = noch nicht geladen.</summary>
    [JsonPropertyName("playlistTargets")] public List<string>? PlaylistTargets { get; set; }

    /// <summary>Die Playlist-Auswahl der Medien-Detailseite soll geöffnet sein.</summary>
    [JsonPropertyName("playlistPickerPending")] public bool PlaylistPickerPending { get; set; }

    /// <summary>Hinweistext der Playlist-Hinzufügung.</summary>
    [JsonPropertyName("addToPlaylistNotice")] public string? AddToPlaylistNotice { get; set; }

    /// <summary><c>true</c>, wenn der Hinweis einen Fehler beschreibt.</summary>
    [JsonPropertyName("addToPlaylistNoticeIsError")] public bool AddToPlaylistNoticeIsError { get; set; }

    /// <summary>Anzeigenamen der Staffeln der Serienansicht in Modellreihenfolge.</summary>
    [JsonPropertyName("mediaDetailsSeasonNames")] public List<string>? MediaDetailsSeasonNames { get; set; }

    /// <summary>Staffelnummer der gewählten Staffel der Serienansicht (0 = keine).</summary>
    [JsonPropertyName("mediaDetailsSelectedSeasonNumber")] public int MediaDetailsSelectedSeasonNumber { get; set; }

    /// <summary>Anzeigetitel der Episoden der gewählten Staffel.</summary>
    [JsonPropertyName("mediaDetailsSeasonEpisodeTitles")] public List<string>? MediaDetailsSeasonEpisodeTitles { get; set; }

    /// <summary>Id der im Quellen-Browser geöffneten Quelle (0 = kein Browser aktiv).</summary>
    [JsonPropertyName("sourceBrowserSourceId")] public long SourceBrowserSourceId { get; set; }

    /// <summary>Anzeigenamen der Genre-Leiste des Quellen-Browsers (inkl. „Alle"-Eintrag).</summary>
    [JsonPropertyName("sourceBrowserGenreNames")] public List<string>? SourceBrowserGenreNames { get; set; }

    /// <summary>Aktuell gewählter Genre-Filter des Quellen-Browsers (0 = „Alle").</summary>
    [JsonPropertyName("sourceBrowserSelectedGenreId")] public long SourceBrowserSelectedGenreId { get; set; }

    /// <summary>Die Genre-Leiste des Quellen-Browsers ist sichtbar.</summary>
    [JsonPropertyName("sourceBrowserShowGenreFilter")] public bool SourceBrowserShowGenreFilter { get; set; }

    /// <summary>Zuletzt abgeschickter Suchtext des Quellen-Browsers (aktiver Filter).</summary>
    [JsonPropertyName("sourceBrowserSearchText")] public string? SourceBrowserSearchText { get; set; }

    /// <summary>Anzeigetitel der geladenen Einträge des Quellen-Browsers.</summary>
    [JsonPropertyName("sourceBrowserItemTitles")] public List<string>? SourceBrowserItemTitles { get; set; }
}

/// <summary>DTO einer Genre-Option des Playlist-Genre-Editors im Modell-Snapshot.</summary>
public sealed class DevApiGenreOption
{
    /// <summary>Id des Genres.</summary>
    [JsonPropertyName("id")] public long Id { get; set; }

    /// <summary>Anzeigename des Genres.</summary>
    [JsonPropertyName("name")] public string? Name { get; set; }

    /// <summary>Das Genre ist gewählt.</summary>
    [JsonPropertyName("isSelected")] public bool IsSelected { get; set; }
}

/// <summary>DTO eines bekannten Servers im Modell-Snapshot.</summary>
public sealed class DevApiKnownServer
{
    /// <summary>Kanonische Serveradresse.</summary>
    [JsonPropertyName("address")] public string? Address { get; set; }

    /// <summary>Anzeigename des Servers.</summary>
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }

    /// <summary>Name oder Adresse als Anzeigefallback.</summary>
    [JsonPropertyName("nameOrAddress")] public string? NameOrAddress { get; set; }

    /// <summary>Zeitpunkt der letzten Verwendung (UTC).</summary>
    [JsonPropertyName("lastUsedAtUtc")] public DateTime LastUsedAtUtc { get; set; }
}

/// <summary>DTO eines gefundenen Servers im Modell-Snapshot.</summary>
public sealed class DevApiDiscoveredServer
{
    /// <summary>Kanonische Serveradresse.</summary>
    [JsonPropertyName("address")] public string? Address { get; set; }

    /// <summary>Anzeigename des Servers.</summary>
    [JsonPropertyName("name")] public string? Name { get; set; }

    /// <summary>Name oder Adresse als Anzeigefallback.</summary>
    [JsonPropertyName("nameOrAddress")] public string? NameOrAddress { get; set; }

    /// <summary>Fundquelle (z. B. mDNS).</summary>
    [JsonPropertyName("source")] public string? Source { get; set; }
}

/// <summary>DTO eines Knotens des <c>GET /tree</c>-Elementbaums.</summary>
public sealed class DevApiTreeNode
{
    /// <summary>AutomationId des Elements (falls gesetzt).</summary>
    [JsonPropertyName("automationId")] public string? AutomationId { get; set; }

    /// <summary>Typname des Elements.</summary>
    [JsonPropertyName("type")] public string? Type { get; set; }

    /// <summary>Element ist sichtbar.</summary>
    [JsonPropertyName("isVisible")] public bool IsVisible { get; set; }

    /// <summary>Textinhalt des Elements.</summary>
    [JsonPropertyName("text")] public string? Text { get; set; }

    /// <summary>Platzhaltertext des Elements.</summary>
    [JsonPropertyName("placeholder")] public string? Placeholder { get; set; }

    /// <summary>Kindknoten.</summary>
    [JsonPropertyName("children")] public List<DevApiTreeNode> Children { get; set; } = [];

    /// <summary>Abgeleitete CollectionView-Einträge (nur bei Listen).</summary>
    [JsonPropertyName("items")] public List<DevApiTreeItem>? Items { get; set; }

    /// <summary>Sucht einen Knoten mit der angegebenen AutomationId im Teilbaum.</summary>
    /// <param name="automationId">Die gesuchte AutomationId.</param>
    /// <returns>Der Knoten oder <c>null</c>.</returns>
    public DevApiTreeNode? FindByAutomationId(string automationId)
    {
        if (AutomationId == automationId)
        {
            return this;
        }

        foreach (var child in Children)
        {
            var found = child.FindByAutomationId(automationId);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}

/// <summary>DTO eines abgeleiteten CollectionView-Eintrags.</summary>
public sealed class DevApiTreeItem
{
    /// <summary>Index im ItemsSource der CollectionView.</summary>
    [JsonPropertyName("index")] public int Index { get; set; }

    /// <summary>Typname des Eintrags.</summary>
    [JsonPropertyName("type")] public string? Type { get; set; }

    /// <summary>Haupttext des Eintrags.</summary>
    [JsonPropertyName("text")] public string? Text { get; set; }

    /// <summary>Detailtext des Eintrags (z. B. Serveradresse).</summary>
    [JsonPropertyName("detail")] public string? Detail { get; set; }
}
