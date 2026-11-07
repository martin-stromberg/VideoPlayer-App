using VideoPlayer.ApiClient.Discovery;
using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>
/// Unveränderliches Gesamtmodell der App (MVU). Änderungen entstehen
/// ausschließlich über <see cref="AppUpdate.Update"/> als neue Instanz.
/// </summary>
public sealed record AppModel
{
    /// <summary>Aktuell angezeigte Seite. Startwert ist <see cref="AppPage.Loading"/>.</summary>
    public AppPage CurrentPage { get; init; } = AppPage.Loading;

    /// <summary>Start-Workflow (Erreichbarkeitsprüfung) läuft.</summary>
    public bool IsStartupRunning { get; init; } = true;

    /// <summary>Ergebnis der Server-Erreichbarkeitsprüfung.</summary>
    public ServerHealthStatus ServerHealth { get; init; } = ServerHealthStatus.Unknown;

    /// <summary>
    /// Verbindungszustand der App (verbunden, Anmeldung erforderlich,
    /// Servereinrichtung erforderlich, offline). Steuert die Navigation;
    /// Startwert <see cref="Mvu.ConnectionState.Connecting"/> (Ladeanzeige).
    /// </summary>
    public ConnectionState ConnectionState { get; init; } = ConnectionState.Connecting;

    /// <summary>Anmeldeversuch (manuell oder automatisch) läuft.</summary>
    public bool IsLoginRunning { get; init; }

    /// <summary>Inline-Fehlertext der Anmeldemaske; <c>null</c> = kein Fehler.</summary>
    public string? LoginError { get; init; }

    /// <summary>
    /// Bekannter Benutzername zur Vorbefüllung der Anmeldemaske (nicht geheim,
    /// liegt in der normalen Ablage und übersteht eine Backup-Wiederherstellung).
    /// </summary>
    public string? LoginUsername { get; init; }

    /// <summary>Ergebnis der Prüfung, ob lokale Offline-Daten vorhanden sind.</summary>
    public bool HasOfflineData { get; init; }

    /// <summary>Die App befindet sich im Offline-Zustand.</summary>
    public bool IsOffline => ConnectionState == ConnectionState.Offline;

    /// <summary>
    /// Letzter bekannter Netzwerkstatus (<c>true</c> = online). Wird beim
    /// Start des Monitorings mit dem Ist-Wert befüllt und danach über
    /// <see cref="NetworkAvailabilityChangedMessage"/> aktualisiert.
    /// </summary>
    public bool IsOnline { get; init; } = true;

    /// <summary>Eine vollständige Synchronisation läuft (nach (Wieder-)Verbindung).</summary>
    public bool IsSyncRunning { get; init; }

    /// <summary>
    /// Geladene App-Einstellungen (beim Start über <see cref="LoadSettingsEffect"/>
    /// befüllt; bis dahin Defaults).
    /// </summary>
    /// <value>Die aktuellen App-Einstellungen.</value>
    public AppSettings Settings { get; init; } = new();

    /// <summary>Bekannte Server (backup-pflichtige Liste, zuletzt verwendet zuerst).</summary>
    public IReadOnlyList<KnownServer> KnownServers { get; init; } = [];

    /// <summary>
    /// Kanonische Adresse des aktiven (zuletzt verbundenen) Servers;
    /// <c>null</c>, wenn noch kein Server hinterlegt ist (→ Erststart-Einrichtung).
    /// </summary>
    public string? ActiveServerAddress { get; init; }

    /// <summary>Zuletzt per Netzwerk-Erkennung gefundene Server-Kandidaten.</summary>
    public IReadOnlyList<DiscoveredServer> DiscoveredServers { get; init; } = [];

    /// <summary>Netzwerk-Erkennung läuft.</summary>
    public bool IsDiscoveryRunning { get; init; }

    /// <summary>Netzwerk-Erkennung wurde mindestens einmal durchgeführt.</summary>
    public bool DiscoveryAttempted { get; init; }

    /// <summary>Verbindungsprüfung eines Server-Kandidaten läuft.</summary>
    public bool IsServerCheckRunning { get; init; }

    /// <summary>
    /// Fehlertext der Server-Einrichtung/-verwaltung (ungültige Eingabe oder
    /// nicht erreichbarer Server); <c>null</c> = kein Fehler.
    /// </summary>
    public string? ServerSetupError { get; init; }

    /// <summary>
    /// Der Server verlangt ein Geräte-Pairing: er ist erreichbar, aber weder
    /// ein Geräte-Token noch ein statischer Client-Gate-Schlüssel ist
    /// verfügbar — die Pairing-Eingabe der Einrichtungsseite wird sichtbar.
    /// </summary>
    public bool IsPairingRequired { get; init; }

    /// <summary>Der Pairing-Austausch (<c>api/pairing/exchange</c>) läuft.</summary>
    public bool IsPairingRunning { get; init; }

    /// <summary>
    /// Inline-Fehlertext der Pairing-Eingabe (ungültiger Code, Sperrung,
    /// widerrufener Geräte-Zugang); <c>null</c> = kein Fehler.
    /// </summary>
    public string? PairingError { get; init; }

    /// <summary>
    /// Der Bootstrap-Austausch des QR-/Ticket-Onboardings läuft
    /// (Serverprüfung der Ticket-Adresse oder <c>api/pairing/bootstrap</c>).
    /// </summary>
    public bool IsBootstrapRunning { get; init; }

    /// <summary>
    /// Inline-Fehlertext der Kopplungs-Eingabe (ungültige Eingabe, Ticket
    /// abgelehnt, Sperrung, Server nicht erreichbar); <c>null</c> = kein Fehler.
    /// </summary>
    public string? BootstrapError { get; init; }

    /// <summary>
    /// Startseiten-Liste „Weiterschauen" — Stand der lokalen Cache-Ablage
    /// (Cache-First beim Start, aktualisiert nach Synchronisation und
    /// Server-Ereignissen).
    /// </summary>
    public IReadOnlyList<HomeMediaItem> ContinueWatching { get; init; } = [];

    /// <summary>Startseiten-Liste „Favoriten" (Stand der Cache-Ablage).</summary>
    public IReadOnlyList<HomeMediaItem> Favorites { get; init; } = [];

    /// <summary>Startseiten-Liste „Neu im Programm" (Stand der Cache-Ablage).</summary>
    public IReadOnlyList<HomeMediaItem> RecentEntries { get; init; } = [];

    /// <summary>Die Weiterschauen-Liste enthält Einträge.</summary>
    public bool HasContinueWatching => ContinueWatching.Count > 0;

    /// <summary>Die Favoriten-Liste enthält Einträge.</summary>
    public bool HasFavorites => Favorites.Count > 0;

    /// <summary>Die Neu-im-Programm-Liste enthält Einträge.</summary>
    public bool HasRecentEntries => RecentEntries.Count > 0;

    /// <summary>Mindestens eine Startseiten-Liste enthält Einträge.</summary>
    public bool HasHomeContent => HasContinueWatching || HasFavorites || HasRecentEntries || SeasonalGenres.Count > 0;

    /// <summary>Aktuelle saisonale Genre-Sektionen aus dem lokalen Cache.</summary>
    public IReadOnlyList<HomeSeasonalSection> SeasonalGenres { get; init; } = [];

    /// <summary>
    /// Zustand des Hero-Kopfbereichs der Startseite (Ladezustand und
    /// Bilddaten des servergenerierten Hintergrunds — flüchtig, keine
    /// dauerhafte Ablage).
    /// </summary>
    public HomeHeroState HomeHero { get; init; } = HomeHeroState.Empty;

    /// <summary>
    /// Der Hero-Kopfbereich der Startseite wird angezeigt — gesteuert
    /// über die positiv formulierte Einstellung
    /// <see cref="AppSettings.HomeHeroEnabled"/> (Standard: aktiviert).
    /// Ein fehlendes Bild lässt den Bereich bestehen (Gradienten/Texte).
    /// </summary>
    public bool ShowHomeHero => Settings.HomeHeroEnabled;

    /// <summary>
    /// Titelzeile des Hero-Kopfbereichs: der Anzeigename des aktiven
    /// Servers (<see cref="KnownServer.NameOrAddress"/>), ohne aktiven
    /// Server der App-Name. Entspricht dem Anwendungstitel
    /// (<c>applicationTitle</c>) des Web-Heros.
    /// </summary>
    public string HeroTitle =>
        ActiveServerAddress is null
            ? "VideoPlayer"
            : KnownServers
                .FirstOrDefault(s => string.Equals(
                    s.Address, ActiveServerAddress, StringComparison.OrdinalIgnoreCase))
                ?.NameOrAddress
            ?? ActiveServerAddress;

    /// <summary>
    /// Freigegebene Quellen des angemeldeten Benutzers — Träger der
    /// dynamischen Quellen-Einträge im Navigationsmenü. Die Befüllung ist
    /// zustandsgesteuert (Verbindungsaufbau, Synchronisation) und erfolgt
    /// nicht über eine eigene Seite.
    /// </summary>
    public SourcesOverviewState SourcesOverview { get; init; } = SourcesOverviewState.Initial;

    /// <summary>Zustand der Quellen-Browsing-Seite (geöffnete Quelle, Filter, Paging).</summary>
    public SourceBrowserState SourceBrowser { get; init; } = SourceBrowserState.Empty;

    /// <summary>Zustand der Medien-Detailseite.</summary>
    public MediaDetailsState MediaDetails { get; init; } = MediaDetailsState.Empty;

    /// <summary>Zustand der Favoriten-Seite (Menüseite „Favoriten").</summary>
    public FavoritesPageState FavoritesPage { get; init; } = FavoritesPageState.Initial;

    /// <summary>Zustand der Playlists-Übersichtsseite (Menüseite „Playlists").</summary>
    public PlaylistsPageState PlaylistsPage { get; init; } = PlaylistsPageState.Initial;

    /// <summary>Zustand der Playlist-Detailseite (Shell-Route „playlistdetails").</summary>
    public PlaylistDetailsState PlaylistDetails { get; init; } = PlaylistDetailsState.Empty;

    /// <summary>Zustand des Playlist-Formulars (Anlage/Bearbeitung als Overlay-Panel).</summary>
    public PlaylistFormState PlaylistForm { get; init; } = PlaylistFormState.Closed;

    /// <summary>Zustand des Playlist-Genre-Editors (Overlay-Panel der Detailseite).</summary>
    public PlaylistGenreEditorState PlaylistGenreEditor { get; init; } = PlaylistGenreEditorState.Closed;

    /// <summary>Zustand der Downloads-Seite (Menüseite „Downloads").</summary>
    public DownloadsPageState Downloads { get; init; } = DownloadsPageState.Initial;

    /// <summary>Zustand der Profil-Seite (Menüseite „Profil").</summary>
    public ProfilePageState Profile { get; init; } = ProfilePageState.Initial;

    /// <summary>Zustand der Wiedergabeseite (Shell-Route „player").</summary>
    public PlayerState Player { get; init; } = PlayerState.Empty;

    /// <summary>Dem Anwender anzeigbarer Text zum Serverstatus.</summary>
    public string ServerHealthText => ServerHealth switch
    {
        ServerHealthStatus.Checking => "Serververbindung wird geprüft …",
        ServerHealthStatus.Reachable => "Server: erreichbar",
        ServerHealthStatus.Unreachable => "Server: nicht erreichbar",
        _ => "Server: unbekannt"
    };

    /// <summary>
    /// Anzeigetext der Start-Ladeanzeige: während der automatischen Anmeldung
    /// (Server erreichbar, Start läuft noch) „Anmeldung …", sonst der
    /// Serverstatus.
    /// </summary>
    public string StartupStatusText =>
        IsStartupRunning && ServerHealth == ServerHealthStatus.Reachable
            ? "Anmeldung beim Server läuft …"
            : ServerHealthText;

    /// <summary>
    /// Hinweis der Ablage-Wiederherstellung (beschädigte lokale Datenbank
    /// wurde neu initialisiert); <c>null</c> = kein Hinweis. Wird auf der
    /// Startseite als Banner angezeigt.
    /// </summary>
    public string? StorageNotice { get; init; }

    /// <summary>
    /// Inline-Fehlertext des Protokollversands auf der Einstellungsseite
    /// (kein Protokoll vorhanden / Versand nicht möglich);
    /// <c>null</c> = kein Fehler.
    /// </summary>
    public string? LogSendError { get; init; }
}
