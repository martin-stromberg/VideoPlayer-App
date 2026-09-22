using VideoPlayer.ApiClient.Discovery;
using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>Basistyp aller MVU-Nachrichten der App.</summary>
public abstract record AppMessage;

/// <summary>Start-Workflow anstoßen (Erreichbarkeitsprüfung).</summary>
public sealed record StartupRequestedMessage : AppMessage;

/// <summary>Ergebnis der Server-Erreichbarkeitsprüfung.</summary>
/// <param name="IsReachable"><c>true</c>, wenn der Server erreichbar ist.</param>
/// <returns>Nachricht mit dem Prüfergebnis.</returns>
public sealed record ServerHealthCheckCompletedMessage(bool IsReachable) : AppMessage;

/// <summary>Programmatische Navigation zu einer Menüseite anfordern.</summary>
/// <param name="Page">Zielseite der Navigation.</param>
/// <returns>Nachricht für die Navigationsanforderung.</returns>
public sealed record NavigateToMessage(AppPage Page) : AppMessage;

/// <summary>Die Shell hat eine Navigation abgeschlossen (Model synchronisieren).</summary>
/// <param name="Page">Die erreichte Seite.</param>
/// <returns>Nachricht für die abgeschlossene Navigation.</returns>
public sealed record NavigatedMessage(AppPage Page) : AppMessage;

/// <summary>Einstellungen aus der Ablage (erneut) laden anfordern.</summary>
public sealed record LoadSettingsRequestedMessage : AppMessage;

/// <summary>Einstellungen wurden aus der Ablage geladen.</summary>
/// <param name="Settings">Die geladenen Einstellungen.</param>
/// <returns>Nachricht mit den geladenen Einstellungen.</returns>
public sealed record SettingsLoadedMessage(AppSettings Settings) : AppMessage;

/// <summary>Geänderte Einstellungen übernehmen und persistent speichern.</summary>
/// <param name="Settings">Die zu speichernden Einstellungen.</param>
/// <returns>Nachricht für die Speicheranforderung.</returns>
public sealed record SaveSettingsRequestedMessage(AppSettings Settings) : AppMessage;

/// <summary>Serververwaltung (erneut) laden anfordern (Liste + aktive Adresse).</summary>
public sealed record LoadServersRequestedMessage : AppMessage;

/// <summary>
/// Serververwaltung wurde geladen: bekannte Server und die aktive Adresse
/// (<c>null</c> = kein Server hinterlegt → Erststart-Einrichtung).
/// </summary>
/// <param name="Servers">Bekannte Server (zuletzt verwendet zuerst).</param>
/// <param name="ActiveAddress">Adresse des aktiven Servers; <c>null</c> = kein Server hinterlegt.</param>
/// <returns>Nachricht mit dem geladenen Serverbestand.</returns>
public sealed record ServersLoadedMessage(
    IReadOnlyList<KnownServer> Servers,
    string? ActiveAddress) : AppMessage;

/// <summary>Automatische Netzwerk-Erkennung (mDNS + UDP) starten.</summary>
public sealed record ServerDiscoveryRequestedMessage : AppMessage;

/// <summary>Netzwerk-Erkennung abgeschlossen; gefundene Kandidaten übernehmen.</summary>
/// <param name="Servers">Die gefundenen Server-Kandidaten.</param>
/// <returns>Nachricht mit dem Erkennungsergebnis.</returns>
public sealed record ServerDiscoveryCompletedMessage(
    IReadOnlyList<DiscoveredServer> Servers) : AppMessage;

/// <summary>Manuell eingegebene Serveradresse prüfen und übernehmen.</summary>
/// <param name="Text">Der eingegebene Adresstext (wird normalisiert).</param>
/// <returns>Nachricht für die Adresseingabe.</returns>
public sealed record ServerAddressSubmittedMessage(string Text) : AppMessage;

/// <summary>Einen per Erkennung gefundenen Server aus der Auswahl übernehmen.</summary>
/// <param name="Address">Adresse des gefundenen Servers.</param>
/// <param name="DisplayName">Anzeigename des gefundenen Servers; <c>null</c>, wenn unbekannt.</param>
/// <returns>Nachricht für die Auswahl des gefundenen Servers.</returns>
public sealed record DiscoveredServerSelectedMessage(string Address, string? DisplayName) : AppMessage;

/// <summary>Auf einen bekannten Server wechseln („Server wechseln").</summary>
/// <param name="Address">Adresse des Zielservers.</param>
/// <param name="DisplayName">Anzeigename des Zielservers; <c>null</c>, wenn unbekannt.</param>
/// <returns>Nachricht für den Serverwechsel.</returns>
public sealed record ServerSwitchRequestedMessage(string Address, string? DisplayName) : AppMessage;

/// <summary>Ergebnis der Verbindungsprüfung eines Server-Kandidaten.</summary>
/// <param name="Address">Die geprüfte Serveradresse.</param>
/// <param name="DisplayName">Anzeigename des Kandidaten; <c>null</c>, wenn unbekannt.</param>
/// <param name="Success"><c>true</c>, wenn der Server erreichbar ist.</param>
/// <returns>Nachricht mit dem Prüfergebnis.</returns>
public sealed record ServerCheckCompletedMessage(
    string Address,
    string? DisplayName,
    bool Success) : AppMessage;

/// <summary>
/// Ergebnis der automatischen Anmeldung mit gespeicherten Zugangsdaten.
/// <paramref name="Username"/> trägt den bekannten Benutzernamen zur
/// Vorbefüllung der Anmeldemaske.
/// </summary>
/// <param name="Status">Ergebnis der automatischen Anmeldung.</param>
/// <param name="Username">Bekannter Benutzername zur Vorbefüllung; <c>null</c>, wenn keiner gespeichert ist.</param>
/// <returns>Nachricht mit dem Ergebnis der automatischen Anmeldung.</returns>
public sealed record AutoLoginCompletedMessage(
    AuthAttemptStatus Status,
    string? Username) : AppMessage;

/// <summary>Manuelle Anmeldung auf der Anmeldemaske angefordert.</summary>
/// <param name="Username">Eingegebener Benutzername.</param>
/// <param name="Password">Eingegebenes Passwort.</param>
/// <returns>Nachricht für die Anmeldeanforderung.</returns>
public sealed record LoginSubmittedMessage(
    string Username,
    string Password) : AppMessage;

/// <summary>Ergebnis der manuellen Anmeldung.</summary>
/// <param name="Status">Ergebnis der Anmeldung.</param>
/// <returns>Nachricht mit dem Anmeldeergebnis.</returns>
public sealed record LoginCompletedMessage(AuthAttemptStatus Status) : AppMessage;

/// <summary>Ergebnis der Prüfung, ob lokale Offline-Daten vorhanden sind.</summary>
/// <param name="HasOfflineData"><c>true</c>, wenn lokale Offline-Daten vorhanden sind.</param>
/// <returns>Nachricht mit dem Prüfergebnis.</returns>
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
/// <param name="IsOnline"><c>true</c> = Netzwerk verfügbar (online).</param>
/// <returns>Nachricht mit dem neuen Netzwerkstatus.</returns>
public sealed record NetworkAvailabilityChangedMessage(bool IsOnline) : AppMessage;

/// <summary>
/// Ein Server-Ereignis wurde über die Echtzeit-Verbindung empfangen
/// (Weiterschauen aktualisiert, Favoriten geändert, neue Inhalte
/// eingescannt).
/// </summary>
/// <param name="Kind">Art des empfangenen Ereignisses.</param>
/// <param name="SourceId">Bei <see cref="ServerEventKind.NewVideosScanned"/> die betroffene Quelle; sonst <c>null</c>.</param>
/// <returns>Nachricht mit dem empfangenen Server-Ereignis.</returns>
public sealed record ServerEventReceivedMessage(
    ServerEventKind Kind,
    long? SourceId = null) : AppMessage;

/// <summary>
/// Die Ereignisverbindung wurde endgültig getrennt (die automatische
/// Wiederverbindung des Transports ist ausgeschöpft).
/// </summary>
public sealed record ServerEventsConnectionClosedMessage : AppMessage;

/// <summary>Die vollständige Synchronisation wurde abgeschlossen.</summary>
/// <param name="Result">Ergebnis der Synchronisation.</param>
/// <returns>Nachricht mit dem Synchronisationsergebnis.</returns>
public sealed record SynchronizationCompletedMessage(SyncResult Result) : AppMessage;

/// <summary>
/// Die gecachten Startseiten-Listen wurden aus der lokalen Ablage gelesen
/// und ins Model übernommen (Antwort auf <c>LoadHomeListsEffect</c>).
/// </summary>
/// <param name="Lists">Die gelesenen Startseiten-Listen.</param>
/// <returns>Nachricht mit den geladenen Listen.</returns>
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
/// <param name="Item">Der ausgewählte Listeneintrag.</param>
/// <returns>Nachricht für die Auswahl.</returns>
public sealed record MediaItemSelectedMessage(HomeMediaItem Item) : AppMessage;

/// <summary>
/// Die Quellen-Seite wurde geöffnet bzw. soll (erneut) laden —
/// im verbundenen Zustand werden die Quellen vom Server geholt.
/// </summary>
public sealed record SourcesPageOpenedMessage : AppMessage;

/// <summary>Die Quellenliste wurde geladen (<paramref name="Error"/> bei Fehlschlag).</summary>
/// <param name="Sources">Die geladenen Quellen.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit dem Ladeergebnis der Quellenliste.</returns>
public sealed record SourcesLoadedMessage(
    IReadOnlyList<SourceItem> Sources,
    string? Error) : AppMessage;

/// <summary>Eine Quelle wurde in der Übersicht ausgewählt — Browser initialisieren und öffnen.</summary>
/// <param name="Source">Die ausgewählte Quelle.</param>
/// <returns>Nachricht für die Quellenauswahl.</returns>
public sealed record SourceSelectedMessage(SourceItem Source) : AppMessage;

/// <summary>
/// Die Quellen-Browsing-Seite ist erschienen und meldet ihre Identität —
/// bei abweichendem Browser-Zustand wird dieser (neu) initialisiert,
/// ohne erneut zu navigieren.
/// </summary>
/// <param name="SourceId">Id der geöffneten Quelle.</param>
/// <param name="SourceName">Anzeigename der geöffneten Quelle.</param>
/// <returns>Nachricht mit der Identität der geöffneten Browsing-Seite.</returns>
public sealed record SourceBrowserOpenedMessage(long SourceId, string SourceName) : AppMessage;

/// <summary>Die Genres der geöffneten Quelle wurden geladen (inkl. „Alle"-Eintrag).</summary>
/// <param name="SourceId">Id der Quelle, zu der die Genres gehören.</param>
/// <param name="Genres">Die geladenen Genres inklusive „Alle"-Eintrag.</param>
/// <returns>Nachricht mit dem Ladeergebnis der Genres.</returns>
public sealed record SourceGenresLoadedMessage(
    long SourceId,
    IReadOnlyList<SourceGenreItem> Genres) : AppMessage;

/// <summary>
/// Eine Eintragsseite der Quelle wurde geladen (<paramref name="Append"/> =
/// Folgeseite anhängen, sonst Liste ersetzen; <paramref name="Error"/> bei
/// Fehlschlag).
/// </summary>
/// <param name="SourceId">Id der Quelle, zu der die Einträge gehören.</param>
/// <param name="Page">Geladene Seite (0-basiert).</param>
/// <param name="Items">Die geladenen Einträge der Seite.</param>
/// <param name="HasMore"><c>true</c>, solange weitere Seiten erwartet werden.</param>
/// <param name="Append"><c>true</c> = Folgeseite anhängen, <c>false</c> = Liste ersetzen.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit dem Ladeergebnis der Eintragsseite.</returns>
public sealed record SourceItemsLoadedMessage(
    long SourceId,
    int Page,
    IReadOnlyList<SourceMediaItem> Items,
    bool HasMore,
    bool Append,
    string? Error) : AppMessage;

/// <summary>Ein Genre-Filter wurde gewählt (<paramref name="GenreId"/> 0 = „Alle").</summary>
/// <param name="GenreId">Id des gewählten Genres; 0 = „Alle" (kein Filter).</param>
/// <returns>Nachricht für die Genre-Auswahl.</returns>
public sealed record SourceGenreSelectedMessage(long GenreId) : AppMessage;

/// <summary>Ein Suchtext wurde abgeschickt (leer = Suche zurücksetzen).</summary>
/// <param name="SearchText">Der abgeschickte Suchtext.</param>
/// <returns>Nachricht für die Suche.</returns>
public sealed record SourceSearchSubmittedMessage(string SearchText) : AppMessage;

/// <summary>Die nächste Eintragsseite soll nachgeladen werden (Listenende erreicht).</summary>
public sealed record SourceItemsNextPageRequestedMessage : AppMessage;

/// <summary>Der letzte Eintrags-Lauf soll wiederholt werden (Fehlerzustand).</summary>
public sealed record SourceItemsRetryRequestedMessage : AppMessage;

/// <summary>Ein Medium des Quellen-Rasters wurde ausgewählt — Detailseite öffnen.</summary>
/// <param name="Item">Der ausgewählte Eintrag.</param>
/// <returns>Nachricht für die Auswahl.</returns>
public sealed record SourceMediaItemSelectedMessage(SourceMediaItem Item) : AppMessage;

/// <summary>
/// Die Detailseite eines Mediums ist erschienen und meldet ihre Identität —
/// die Detaildaten werden vom Server geladen. Die Hints
/// <paramref name="ShowId"/>/<paramref name="SeasonId"/>/
/// <paramref name="EpisodeId"/> lösen Episoden/Staffeln zum Serienkontext
/// auf (kein Staffel-Endpunkt vorhanden).
/// </summary>
/// <param name="MediaType">Medientyp (wird normalisiert).</param>
/// <param name="MediaId">Id des Mediums.</param>
/// <param name="Title">Fallback-Titel; <c>null</c>, wenn unbekannt.</param>
/// <param name="ShowId">Optionaler Hint: Id der zugehörigen Serie.</param>
/// <param name="SeasonId">Optionaler Hint: Id der zugehörigen Staffel.</param>
/// <param name="EpisodeId">Optionaler Hint: Id der hervorzuhebenden Episode.</param>
/// <returns>Nachricht mit der Identität der angefragten Detailseite.</returns>
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
/// <param name="MediaType">Medientyp der angefragten Identität.</param>
/// <param name="MediaId">Medien-Id der angefragten Identität.</param>
/// <param name="Details">Geladenes Anzeigemodell; <c>null</c> bei Fehlschlag.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit dem Ladeergebnis der Detaildaten.</returns>
public sealed record MediaDetailsLoadedMessage(
    string MediaType,
    long MediaId,
    MediaDetailsView? Details,
    string? Error) : AppMessage;

/// <summary>Eine andere Staffel wurde im Serienkontext gewählt.</summary>
/// <param name="SeasonId">Id der gewählten Staffel.</param>
/// <returns>Nachricht für die Staffelauswahl.</returns>
public sealed record SeasonSelectedMessage(long SeasonId) : AppMessage;

/// <summary>Die Favoriten-Markierung des angezeigten Mediums umschalten.</summary>
public sealed record FavoriteToggleRequestedMessage : AppMessage;

/// <summary>
/// Ergebnis der Favoriten-Umschaltung: <paramref name="IsFavorite"/> ist der
/// neue Server-Zustand; <c>null</c> = fehlgeschlagen/kein Server (Stern
/// bleibt unverändert, Fehlerhinweis wird gesetzt).
/// </summary>
/// <param name="IsFavorite">Neuer Favoriten-Zustand laut Server; <c>null</c> = fehlgeschlagen/kein Server.</param>
/// <param name="MediaType">Medientyp des umgeschalteten Mediums.</param>
/// <param name="MediaId">Id des umgeschalteten Mediums.</param>
/// <returns>Nachricht mit dem Ergebnis der Favoriten-Umschaltung.</returns>
public sealed record FavoriteToggleCompletedMessage(
    bool? IsFavorite,
    string MediaType,
    long MediaId) : AppMessage;
