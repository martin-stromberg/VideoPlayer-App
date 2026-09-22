using VideoPlayer.ApiClient.Discovery;
using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>Basistyp aller MVU-Nachrichten der App.</summary>
public abstract record AppMessage;

/// <summary>Start-Workflow anstoßen (Erreichbarkeitsprüfung).</summary>
public sealed record StartupRequestedMessage : AppMessage;

/// <summary>Ergebnis der Server-Erreichbarkeitsprüfung.</summary>
public sealed record ServerHealthCheckCompletedMessage(bool IsReachable) : AppMessage;

/// <summary>Programmatische Navigation zu einer Menüseite anfordern.</summary>
public sealed record NavigateToMessage(AppPage Page) : AppMessage;

/// <summary>Die Shell hat eine Navigation abgeschlossen (Model synchronisieren).</summary>
public sealed record NavigatedMessage(AppPage Page) : AppMessage;

/// <summary>Einstellungen aus der Ablage (erneut) laden anfordern.</summary>
public sealed record LoadSettingsRequestedMessage : AppMessage;

/// <summary>Einstellungen wurden aus der Ablage geladen.</summary>
public sealed record SettingsLoadedMessage(AppSettings Settings) : AppMessage;

/// <summary>Geänderte Einstellungen übernehmen und persistent speichern.</summary>
public sealed record SaveSettingsRequestedMessage(AppSettings Settings) : AppMessage;

/// <summary>Serververwaltung (erneut) laden anfordern (Liste + aktive Adresse).</summary>
public sealed record LoadServersRequestedMessage : AppMessage;

/// <summary>
/// Serververwaltung wurde geladen: bekannte Server und die aktive Adresse
/// (<c>null</c> = kein Server hinterlegt → Erststart-Einrichtung).
/// </summary>
public sealed record ServersLoadedMessage(
    IReadOnlyList<KnownServer> Servers,
    string? ActiveAddress) : AppMessage;

/// <summary>Automatische Netzwerk-Erkennung (mDNS + UDP) starten.</summary>
public sealed record ServerDiscoveryRequestedMessage : AppMessage;

/// <summary>Netzwerk-Erkennung abgeschlossen; gefundene Kandidaten übernehmen.</summary>
public sealed record ServerDiscoveryCompletedMessage(
    IReadOnlyList<DiscoveredServer> Servers) : AppMessage;

/// <summary>Manuell eingegebene Serveradresse prüfen und übernehmen.</summary>
public sealed record ServerAddressSubmittedMessage(string Text) : AppMessage;

/// <summary>Einen per Erkennung gefundenen Server aus der Auswahl übernehmen.</summary>
public sealed record DiscoveredServerSelectedMessage(string Address, string? DisplayName) : AppMessage;

/// <summary>Auf einen bekannten Server wechseln („Server wechseln").</summary>
public sealed record ServerSwitchRequestedMessage(string Address, string? DisplayName) : AppMessage;

/// <summary>Ergebnis der Verbindungsprüfung eines Server-Kandidaten.</summary>
public sealed record ServerCheckCompletedMessage(
    string Address,
    string? DisplayName,
    bool Success) : AppMessage;

/// <summary>
/// Ergebnis der automatischen Anmeldung mit gespeicherten Zugangsdaten.
/// <paramref name="Username"/> trägt den bekannten Benutzernamen zur
/// Vorbefüllung der Anmeldemaske.
/// </summary>
public sealed record AutoLoginCompletedMessage(
    AuthAttemptStatus Status,
    string? Username) : AppMessage;

/// <summary>Manuelle Anmeldung auf der Anmeldemaske angefordert.</summary>
public sealed record LoginSubmittedMessage(
    string Username,
    string Password) : AppMessage;

/// <summary>Ergebnis der manuellen Anmeldung.</summary>
public sealed record LoginCompletedMessage(AuthAttemptStatus Status) : AppMessage;

/// <summary>Ergebnis der Prüfung, ob lokale Offline-Daten vorhanden sind.</summary>
public sealed record OfflineDataCheckCompletedMessage(bool HasOfflineData) : AppMessage;

/// <summary>
/// Die stille Token-Neuanmeldung wurde endgültig abgelehnt bzw. es fehlen
/// Zugangsdaten — die App wechselt in den Zustand <c>NeedsLogin</c>.
/// </summary>
public sealed record SessionExpiredMessage : AppMessage;

/// <summary>
/// Der Netzwerkstatus hat sich geändert (oder wird beim Monitoring-Start
/// mit dem Ist-Wert gemeldet).
/// </summary>
public sealed record NetworkAvailabilityChangedMessage(bool IsOnline) : AppMessage;

/// <summary>
/// Ein Server-Ereignis wurde über die Echtzeit-Verbindung empfangen
/// (Weiterschauen aktualisiert, Favoriten geändert, neue Inhalte
/// eingescannt).
/// </summary>
public sealed record ServerEventReceivedMessage(
    ServerEventKind Kind,
    long? SourceId = null) : AppMessage;

/// <summary>
/// Die Ereignisverbindung wurde endgültig getrennt (die automatische
/// Wiederverbindung des Transports ist ausgeschöpft).
/// </summary>
public sealed record ServerEventsConnectionClosedMessage : AppMessage;

/// <summary>Die vollständige Synchronisation wurde abgeschlossen.</summary>
public sealed record SynchronizationCompletedMessage(SyncResult Result) : AppMessage;

/// <summary>
/// Die gecachten Startseiten-Listen wurden aus der lokalen Ablage gelesen
/// und ins Model übernommen (Antwort auf <c>LoadHomeListsEffect</c>).
/// </summary>
public sealed record HomeListsLoadedMessage(HomeLists Lists) : AppMessage;

/// <summary>
/// Die gecachten Listen wurden geändert (z. B. durch eine
/// Teil-Synchronisation nach einem Server-Ereignis) — die
/// Startseiten-Listen sind aus dem Cache neu zu laden.
/// </summary>
public sealed record CachedListsChangedMessage : AppMessage;

/// <summary>
/// Ein Eintrag einer Startseiten-Liste wurde ausgewählt — die App
/// navigiert zur Detailseite des Mediums.
/// </summary>
public sealed record MediaItemSelectedMessage(HomeMediaItem Item) : AppMessage;

/// <summary>
/// Die Quellen-Seite wurde geöffnet bzw. soll (erneut) laden —
/// im verbundenen Zustand werden die Quellen vom Server geholt.
/// </summary>
public sealed record SourcesPageOpenedMessage : AppMessage;

/// <summary>Die Quellenliste wurde geladen (<paramref name="Error"/> bei Fehlschlag).</summary>
public sealed record SourcesLoadedMessage(
    IReadOnlyList<SourceItem> Sources,
    string? Error) : AppMessage;

/// <summary>Eine Quelle wurde in der Übersicht ausgewählt — Browser initialisieren und öffnen.</summary>
public sealed record SourceSelectedMessage(SourceItem Source) : AppMessage;

/// <summary>
/// Die Quellen-Browsing-Seite ist erschienen und meldet ihre Identität —
/// bei abweichendem Browser-Zustand wird dieser (neu) initialisiert,
/// ohne erneut zu navigieren.
/// </summary>
public sealed record SourceBrowserOpenedMessage(long SourceId, string SourceName) : AppMessage;

/// <summary>Die Genres der geöffneten Quelle wurden geladen (inkl. „Alle"-Eintrag).</summary>
public sealed record SourceGenresLoadedMessage(
    long SourceId,
    IReadOnlyList<SourceGenreItem> Genres) : AppMessage;

/// <summary>
/// Eine Eintragsseite der Quelle wurde geladen (<paramref name="Append"/> =
/// Folgeseite anhängen, sonst Liste ersetzen; <paramref name="Error"/> bei
/// Fehlschlag).
/// </summary>
public sealed record SourceItemsLoadedMessage(
    long SourceId,
    int Page,
    IReadOnlyList<SourceMediaItem> Items,
    bool HasMore,
    bool Append,
    string? Error) : AppMessage;

/// <summary>Ein Genre-Filter wurde gewählt (<paramref name="GenreId"/> 0 = „Alle").</summary>
public sealed record SourceGenreSelectedMessage(long GenreId) : AppMessage;

/// <summary>Ein Suchtext wurde abgeschickt (leer = Suche zurücksetzen).</summary>
public sealed record SourceSearchSubmittedMessage(string SearchText) : AppMessage;

/// <summary>Die nächste Eintragsseite soll nachgeladen werden (Listenende erreicht).</summary>
public sealed record SourceItemsNextPageRequestedMessage : AppMessage;

/// <summary>Der letzte Eintrags-Lauf soll wiederholt werden (Fehlerzustand).</summary>
public sealed record SourceItemsRetryRequestedMessage : AppMessage;

/// <summary>Ein Medium des Quellen-Rasters wurde ausgewählt — Detailseite öffnen.</summary>
public sealed record SourceMediaItemSelectedMessage(SourceMediaItem Item) : AppMessage;

/// <summary>
/// Die Detailseite eines Mediums ist erschienen und meldet ihre Identität —
/// die Detaildaten werden vom Server geladen. Die Hints
/// <paramref name="ShowId"/>/<paramref name="SeasonId"/>/
/// <paramref name="EpisodeId"/> lösen Episoden/Staffeln zum Serienkontext
/// auf (kein Staffel-Endpunkt vorhanden).
/// </summary>
public sealed record MediaDetailsRequestedMessage(
    string MediaType,
    long MediaId,
    string? Title,
    long? ShowId = null,
    long? SeasonId = null,
    long? EpisodeId = null) : AppMessage;

/// <summary>
/// Die Detaildaten der angefragten Identität wurden geladen
/// (<paramref name="Error"/> bei Fehlschlag).
/// </summary>
public sealed record MediaDetailsLoadedMessage(
    string MediaType,
    long MediaId,
    MediaDetailsView? Details,
    string? Error) : AppMessage;

/// <summary>Eine andere Staffel wurde im Serienkontext gewählt.</summary>
public sealed record SeasonSelectedMessage(long SeasonId) : AppMessage;

/// <summary>Die Favoriten-Markierung des angezeigten Mediums umschalten.</summary>
public sealed record FavoriteToggleRequestedMessage : AppMessage;

/// <summary>
/// Ergebnis der Favoriten-Umschaltung: <paramref name="IsFavorite"/> ist der
/// neue Server-Zustand; <c>null</c> = fehlgeschlagen/kein Server (Stern
/// bleibt unverändert, Fehlerhinweis wird gesetzt).
/// </summary>
public sealed record FavoriteToggleCompletedMessage(
    bool? IsFavorite,
    string MediaType,
    long MediaId) : AppMessage;
