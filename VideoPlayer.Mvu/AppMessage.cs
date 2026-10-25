using VideoPlayer.ApiClient.Discovery;
using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>Basistyp aller MVU-Nachrichten der App.</summary>
public abstract record AppMessage;

/// <summary>Start-Workflow anstoßen (Erreichbarkeitsprüfung).</summary>
public sealed record StartupRequestedMessage : AppMessage;

/// <summary>Ergebnis der Server-Erreichbarkeitsprüfung.</summary>
/// <param name="IsReachable"><c>true</c>, wenn der Server erreichbar ist.</param>
/// <param name="PairingRequired"><c>true</c>, wenn der Server ein Geräte-Pairing verlangt (kein nutzbarer Gate-Schlüssel).</param>
/// <returns>Nachricht mit dem Prüfergebnis.</returns>
public sealed record ServerHealthCheckCompletedMessage(bool IsReachable, bool PairingRequired) : AppMessage;

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
/// <param name="PairingRequired"><c>true</c>, wenn der aktive Server ein Geräte-Pairing verlangt (kein nutzbarer Gate-Schlüssel).</param>
/// <returns>Nachricht mit dem geladenen Serverbestand.</returns>
public sealed record ServersLoadedMessage(
    IReadOnlyList<KnownServer> Servers,
    string? ActiveAddress,
    bool PairingRequired) : AppMessage;

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
/// <param name="PairingRequired"><c>true</c>, wenn der Server ein Geräte-Pairing verlangt (kein nutzbarer Gate-Schlüssel).</param>
/// <returns>Nachricht mit dem Prüfergebnis.</returns>
public sealed record ServerCheckCompletedMessage(
    string Address,
    string? DisplayName,
    bool Success,
    bool PairingRequired) : AppMessage;

/// <summary>Der Benutzer hat einen Einmal-Pairing-Code eingegeben.</summary>
/// <param name="Code">Die Roheingabe (wird vor dem Austausch normalisiert/geprüft).</param>
/// <returns>Nachricht für die Code-Eingabe.</returns>
public sealed record PairingCodeSubmittedMessage(string Code) : AppMessage;

/// <summary>Ergebnis des Pairing-Austauschs (<c>api/pairing/exchange</c>).</summary>
/// <param name="Status">Ergebnisstatus des Pairings.</param>
/// <returns>Nachricht mit dem Pairing-Ergebnis.</returns>
public sealed record PairingCompletedMessage(PairingStatus Status) : AppMessage;

/// <summary>
/// Der Server hat das bereitgestellte Geräte-Token endgültig abgelehnt —
/// das Token wurde verworfen; die App wechselt in die Re-Pairing-Eingabe
/// (<c>NeedsServerSetup</c> mit aktivem Pairing-Abschnitt).
/// </summary>
public sealed record DeviceTokenRevokedMessage : AppMessage;

/// <summary>
/// Der Benutzer hat eine Kopplungs-Eingabe abgeschickt: eine Ticket-Adresse
/// (QR-Code-Inhalt oder Paste, trägt die Serveradresse selbst) oder einen
/// Kurzcode (wird gegen den aktiven Server eingelöst).
/// </summary>
/// <param name="Text">Die Roheingabe (wird vom <see cref="BootstrapTicketParser"/> zerlegt).</param>
/// <returns>Nachricht für die Kopplungs-Eingabe.</returns>
public sealed record BootstrapTicketSubmittedMessage(string Text) : AppMessage;

/// <summary>
/// Ergebnis der Erreichbarkeitsprüfung des Servers aus einer Ticket-Adresse —
/// erst danach wird der Server persistiert und der Bootstrap-Austausch gestartet.
/// </summary>
/// <param name="Address">Die geprüfte Serveradresse.</param>
/// <param name="Success"><c>true</c>, wenn der Server erreichbar ist.</param>
/// <param name="Ticket">Das einzulösende Ticket (wird für den Folge-Effekt weitergereicht).</param>
/// <returns>Nachricht mit dem Prüfergebnis.</returns>
public sealed record BootstrapServerCheckedMessage(
    string Address,
    bool Success,
    string Ticket) : AppMessage;

/// <summary>Ergebnis des Bootstrap-Austauschs (<c>api/pairing/bootstrap</c>).</summary>
/// <param name="Status">Ergebnisstatus des Bootstraps.</param>
/// <returns>Nachricht mit dem Bootstrap-Ergebnis.</returns>
public sealed record BootstrapCompletedMessage(BootstrapStatus Status) : AppMessage;

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

/// <summary>Startseite wird sichtbar oder serverseitige Ablaufgrenze ist erreicht.</summary>
public sealed record HomeListsReloadRequestedMessage : AppMessage;

/// <summary>Benutzer fordert eine erneute Synchronisation der Startseite an.</summary>
public sealed record HomeRefreshRequestedMessage : AppMessage;

/// <summary>
/// Der Abruf des servergenerierten Hero-Hintergrundbilds
/// (<c>api/pictures/hero-background</c>) ist abgeschlossen (Antwort auf
/// <c>LoadHeroBackgroundEffect</c>). <paramref name="ImageBytes"/> ist
/// <c>null</c>, wenn kein Bild verfügbar ist (offline, keine Sitzung,
/// Serverfehler) — der Kopfbereich zeigt dann nur Gradienten und Texte.
/// </summary>
/// <param name="ImageBytes">Die Bilddaten (JPEG) oder <c>null</c> bei Fehlschlag.</param>
/// <returns>Nachricht mit dem Ladeergebnis des Hero-Hintergrunds.</returns>
public sealed record HeroBackgroundLoadedMessage(byte[]? ImageBytes) : AppMessage;

/// <summary>
/// Eine lokale Datenbank war beschädigt und wurde neu initialisiert —
/// die alte Datei wurde beiseitegelegt (<c>*.corrupt-*</c>). Der Nutzer
/// wird über <paramref name="Notice"/> informiert; verworfene Caches
/// werden bei bestehender Verbindung neu synchronisiert.
/// </summary>
/// <param name="Notice">Der anzuzeigende Hinweistext.</param>
/// <returns>Nachricht mit dem Wiederherstellungs-Hinweis.</returns>
public sealed record StorageRecoveredMessage(string Notice) : AppMessage;

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
/// Ein Eintrag der Weiterschauen-Liste der Startseite wurde ausgewählt:
/// abspielbare Medientypen (<c>movie</c>/<c>episode</c>) starten direkt
/// die Wiedergabe ab der Fortsetzungsposition, alle übrigen Einträge
/// öffnen weiterhin die Detailseite des Mediums.
/// </summary>
/// <param name="Item">Der ausgewählte Weiterschauen-Eintrag.</param>
/// <returns>Nachricht für die Auswahl.</returns>
public sealed record ContinueWatchingItemSelectedMessage(HomeMediaItem Item) : AppMessage;

/// <summary>
/// Die Kontextmenü-Aktion „Ausblenden" wurde für einen Eintrag der
/// Weiterschauen-Liste gewählt (2-s-Long-Press) — der Eintrag soll über
/// <c>api/continue-watching/hide</c> ausgeblendet bzw. offline lokal aus
/// dem Cache-Eintrag entfernt werden.
/// </summary>
/// <param name="Item">Der betroffene Weiterschauen-Eintrag.</param>
/// <returns>Nachricht für die Ausblenden-Anforderung.</returns>
public sealed record ContinueWatchingHideRequestedMessage(HomeMediaItem Item) : AppMessage;

/// <summary>
/// Die Kontextmenü-Aktion „Überspringen" wurde für einen Eintrag der
/// Weiterschauen-Liste gewählt (2-s-Long-Press) — der Eintrag soll über
/// <c>api/continue-watching/skip</c> übersprungen bzw. offline lokal aus
/// dem Cache-Eintrag entfernt werden.
/// </summary>
/// <param name="Item">Der betroffene Weiterschauen-Eintrag.</param>
/// <returns>Nachricht für die Überspringen-Anforderung.</returns>
public sealed record ContinueWatchingSkipRequestedMessage(HomeMediaItem Item) : AppMessage;

/// <summary>
/// Die Folge-Episoden eines direkt aus der Weiterschauen-Liste
/// gestarteten Mediums wurden geladen (Serienkontext ohne
/// Detailseiten-Navigation); das Update übernimmt sie nur, wenn der
/// Player noch dieselbe Episode spielt.
/// </summary>
/// <param name="MediaId">Id der gestarteten Episode, zu der die Liste gehört.</param>
/// <param name="Episodes">Die geordneten Folge-Episoden (leer, wenn keine bekannt sind).</param>
/// <returns>Nachricht mit der geladenen Fortsetzungsliste.</returns>
public sealed record UpcomingEpisodesLoadedMessage(
    long MediaId,
    IReadOnlyList<PlayerEpisodeRef> Episodes) : AppMessage;

/// <summary>Die Quellenliste wurde geladen (<paramref name="Error"/> bei Fehlschlag).</summary>
/// <param name="Sources">Die geladenen Quellen.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit dem Ladeergebnis der Quellenliste.</returns>
public sealed record SourcesLoadedMessage(
    IReadOnlyList<SourceItem> Sources,
    string? Error) : AppMessage;

/// <summary>
/// Erneutes Laden der Quellenliste angefordert — Auslöser ist der
/// Fehler-Eintrag im Navigationsmenü nach einem Ladefehlschlag.
/// </summary>
public sealed record SourcesReloadRequestedMessage : AppMessage;

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

/// <summary>
/// Ergebnis des Kopfbereichs-Bildlaufs der Detailseite. Die Identität
/// (<paramref name="MediaType"/>, <paramref name="MediaId"/> und die
/// angefragte Bildquelle <paramref name="HeaderPictureId"/> bzw.
/// <paramref name="HeaderEpisodeId"/>) wird beim Anwenden gegen den
/// aktuellen Zustand geprüft — verspätete Antworten abgelöster
/// Seiten-/Bildstände werden verworfen.
/// </summary>
/// <param name="MediaType">Medientyp der Detailseite (CLR-Konvention).</param>
/// <param name="MediaId">Medien-Id der Detailseite.</param>
/// <param name="HeaderPictureId">Angefragte Bild-Id; <c>null</c>, wenn ein Episoden-Still angefragt war.</param>
/// <param name="HeaderEpisodeId">Angefragte Episoden-Id des Stills; <c>null</c>, wenn eine Bild-Id angefragt war.</param>
/// <param name="ImageBytes">Bilddaten; <c>null</c> bei Fehlschlag oder fehlendem Bild.</param>
/// <returns>Nachricht mit dem Ladeergebnis des Kopfbereichs-Bilds.</returns>
public sealed record MediaDetailsHeaderImageLoadedMessage(
    string MediaType,
    long MediaId,
    long? HeaderPictureId,
    long? HeaderEpisodeId,
    byte[]? ImageBytes) : AppMessage;

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

/// <summary>
/// Die Favoriten-Seite wurde geöffnet bzw. soll (erneut) laden — online
/// wird zuerst die lokale Kopie vom Serverstand erneuert (führendes
/// System), offline wird der Cache-Stand angezeigt.
/// </summary>
public sealed record FavoritesPageOpenedMessage : AppMessage;

/// <summary>Die Favoritenliste wurde geladen (<paramref name="Error"/> bei Fehlschlag).</summary>
/// <param name="Items">Die geladenen Favoriten-Einträge.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit dem Ladeergebnis der Favoritenliste.</returns>
public sealed record FavoritesPageLoadedMessage(
    IReadOnlyList<FavoriteListItem> Items,
    string? Error) : AppMessage;

/// <summary>Ein Favorit soll direkt aus der Liste entfernt werden (nur online wirksam).</summary>
/// <param name="Item">Der zu entfernende Favoriten-Eintrag.</param>
/// <returns>Nachricht für die Entfernen-Anforderung.</returns>
public sealed record FavoriteRemoveRequestedMessage(FavoriteListItem Item) : AppMessage;

/// <summary>
/// Ergebnis des Entfernens eines Favoriten: bei Erfolg wurde die lokale
/// Kopie <c>cached_favorites</c> bereits vom Serverstand erneuert; bei
/// Fehlschlag bleibt der Eintrag in der Liste.
/// </summary>
/// <param name="FavoriteId">Id des Favoriteneintrags, der entfernt werden sollte.</param>
/// <param name="Success"><c>true</c>, wenn der Eintrag auf dem Server entfernt wurde.</param>
/// <returns>Nachricht mit dem Ergebnis des Entfernens.</returns>
public sealed record FavoriteRemoveCompletedMessage(long FavoriteId, bool Success) : AppMessage;

/// <summary>
/// Die Playlists-Übersichtsseite wurde geöffnet bzw. soll (erneut)
/// laden — Playlists sind ein reines Online-Feature: ohne
/// Serververbindung zeigt die Seite eine verständliche Fehleranzeige
/// statt eines Cache-Stands.
/// </summary>
public sealed record PlaylistsPageOpenedMessage : AppMessage;

/// <summary>Die Playlists-Übersicht wurde geladen (<paramref name="Error"/> bei Fehlschlag).</summary>
/// <param name="Own">Die eigenen Playlists des angemeldeten Benutzers.</param>
/// <param name="Public">Die öffentlichen Playlists.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit dem Ladeergebnis der Playlists-Übersicht.</returns>
public sealed record PlaylistsLoadedMessage(
    IReadOnlyList<PlaylistListItem> Own,
    IReadOnlyList<PlaylistListItem> Public,
    string? Error) : AppMessage;

/// <summary>Eine Playlist wurde in der Übersicht ausgewählt — die Detailseite wird geöffnet und die erste Eintragsseite geladen.</summary>
/// <param name="Item">Die ausgewählte Playlist.</param>
/// <returns>Nachricht für die Playlist-Auswahl.</returns>
public sealed record PlaylistSelectedMessage(PlaylistListItem Item) : AppMessage;

/// <summary>
/// Die Playlist-Detailseite wurde geöffnet — Rückkehr- und
/// Wiederherstellungspfad (z. B. zurück von der Medien-Detailseite oder
/// Seitenaufruf ohne vorherige Auswahl): Kopfdaten und erste
/// Eintragsseite werden bei Bedarf geladen.
/// </summary>
/// <param name="PlaylistId">Id der geöffneten Playlist.</param>
/// <param name="Name">Anzeigename aus dem Navigationspfad (Fallback-Titel).</param>
/// <returns>Nachricht für das Öffnen der Detailseite.</returns>
public sealed record PlaylistDetailsOpenedMessage(long PlaylistId, string Name) : AppMessage;

/// <summary>
/// Die Kopfdaten einer Playlist wurden nachgeladen (nicht-fataler
/// Nachschlag der Detailseite); <paramref name="Info"/> ist
/// <c>null</c>, wenn die Playlist nicht gefunden wurde oder der
/// Nachschlag fehlschlug — der bisherige Kopfstand bleibt.
/// </summary>
/// <param name="PlaylistId">Id der Playlist, zu der die Kopfdaten gehören.</param>
/// <param name="Info">Die geladenen Kopfdaten; <c>null</c> bei Fehlschlag.</param>
/// <param name="IsAdmin"><c>true</c>, wenn das gespeicherte Anmelde-Token den <c>IsAdmin</c>-Claim trägt (Sichtbarkeit des Öffentlich-Schalters).</param>
/// <returns>Nachricht mit den Kopfdaten der Playlist.</returns>
public sealed record PlaylistDetailsLoadedMessage(
    long PlaylistId,
    PlaylistListItem? Info,
    bool IsAdmin = false) : AppMessage;

/// <summary>Eine Eintragsseite der geöffneten Playlist wurde geladen (<paramref name="Error"/> bei Fehlschlag).</summary>
/// <param name="PlaylistId">Id der Playlist, zu der die Einträge gehören.</param>
/// <param name="Page">Geladene Seite (1-basiert, Server-Konvention).</param>
/// <param name="Items">Die Einträge der Seite in Server-Reihenfolge.</param>
/// <param name="HasMore"><c>true</c>, solange der Server weitere Seiten meldet.</param>
/// <param name="TotalCount">Gesamtzahl der Einträge laut Server.</param>
/// <param name="Append"><c>true</c> = an die bestehende Liste anhängen; <c>false</c> = Liste ersetzen.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit dem Ladeergebnis der Eintragsseite.</returns>
public sealed record PlaylistEntriesLoadedMessage(
    long PlaylistId,
    int Page,
    IReadOnlyList<PlaylistEntryItem> Items,
    bool HasMore,
    int TotalCount,
    bool Append,
    string? Error) : AppMessage;

/// <summary>Die nächste Eintragsseite der geöffneten Playlist soll nachgeladen werden (Scroll-Ende).</summary>
public sealed record PlaylistEntriesNextPageRequestedMessage : AppMessage;

/// <summary>Der letzte fehlgeschlagene Lauf der Playlist-Detailseite soll wiederholt werden.</summary>
public sealed record PlaylistEntriesRetryRequestedMessage : AppMessage;

/// <summary>Ein Playlist-Eintrag wurde ausgewählt — abspielbare und zugängliche Einträge starten die Wiedergabe ab diesem Eintrag, alle übrigen öffnen die Medien-Detailseite.</summary>
/// <param name="Item">Der ausgewählte Eintrag.</param>
/// <returns>Nachricht für die Eintragsauswahl.</returns>
public sealed record PlaylistEntrySelectedMessage(PlaylistEntryItem Item) : AppMessage;

/// <summary>
/// Der Anwender fordert die Wiedergabe einer Playlist an („Abspielen"
/// auf der Playlist-Detailseite) — der serverseitige Wiedergabestart
/// (<c>POST api/playlists/{id}/play</c>) löst den Starteintrag auf:
/// ab <paramref name="EntryId"/> oder ab dem ersten abspielbaren und
/// zugänglichen Eintrag ohne Startvorgabe (Ergebnis:
/// <c>PlaylistPlaybackStartedMessage</c>).
/// </summary>
/// <param name="PlaylistId">Id der Playlist.</param>
/// <param name="EntryId">Id des Starteintrags; <c>null</c> = erster abspielbarer und zugänglicher Eintrag.</param>
/// <returns>Nachricht für die Wiedergabe-Anforderung.</returns>
public sealed record PlaylistPlaybackStartRequestedMessage(
    long PlaylistId,
    long? EntryId) : AppMessage;

/// <summary>
/// Ergebnis des serverseitigen Playlist-Wiedergabestarts: bei Erfolg
/// trägt die Nachricht den aufgelösten Starteintrag
/// (<paramref name="Start"/>), bei Fehlschlag <paramref name="Error"/>
/// — der Player zeigt dann den Fehlerzustand.
/// </summary>
/// <param name="PlaylistId">Id der angefragten Playlist.</param>
/// <param name="Start">Der aufgelöste Starteintrag; <c>null</c> bei Fehlschlag.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit dem Ergebnis des Wiedergabestarts.</returns>
public sealed record PlaylistPlaybackStartedMessage(
    long PlaylistId,
    PlaylistPlaybackStartInfo? Start,
    string? Error) : AppMessage;

/// <summary>
/// Der Anwender fordert den nächsten Eintrag der laufenden
/// Playlist-Wiedergabe an („Weiter" im Player) —
/// <c>POST api/playlists/{id}/play/next</c> (Ergebnis:
/// <c>PlaylistNavigationCompletedMessage</c>).
/// </summary>
public sealed record PlaylistNextRequestedMessage : AppMessage;

/// <summary>
/// Der Anwender fordert den vorherigen Eintrag der laufenden
/// Playlist-Wiedergabe an („Zurück" im Player) —
/// <c>POST api/playlists/{id}/play/previous</c> (Ergebnis:
/// <c>PlaylistNavigationCompletedMessage</c>).
/// </summary>
public sealed record PlaylistPreviousRequestedMessage : AppMessage;

/// <summary>
/// Ergebnis einer Playlist-Navigation (manuell next/previous oder
/// automatisches advance): bei Erfolg trägt die Nachricht den
/// aufgelösten Zieleintrag (<paramref name="Result"/>); ein leeres
/// Ergebnis (<c>null</c>) meldet „kein Eintrag in dieser Richtung"
/// (Server-204). Die Identität (<paramref name="PlaylistId"/>,
/// <paramref name="CurrentEntryId"/>) bindet die Antwort an den
/// auslösenden Stand — abgelöste Antworten werden verworfen.
/// </summary>
/// <param name="PlaylistId">Id der Playlist der auslösenden Anfrage.</param>
/// <param name="CurrentEntryId">Id des Eintrags, von dem aus navigiert wurde.</param>
/// <param name="Result">Der aufgelöste Zieleintrag; <c>null</c> = kein Eintrag (204) oder Fehlschlag.</param>
/// <param name="Forward"><c>true</c> = Vorwärtsrichtung (next/advance); <c>false</c> = Rückwärts (previous).</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg oder leerem Ergebnis.</param>
/// <returns>Nachricht mit dem Ergebnis der Navigation.</returns>
public sealed record PlaylistNavigationCompletedMessage(
    long PlaylistId,
    long CurrentEntryId,
    PlaylistNavigationItem? Result,
    bool Forward,
    string? Error) : AppMessage;

// --- Playlist-Verwaltung (Schritt 6) ---

/// <summary>
/// Der Anwender fordert das Playlist-Formular für eine Neuanlage an
/// („Neue Playlist" auf der Übersichtsseite) — das Formular öffnet
/// sich leer mit wählbarem Sortiermodus.
/// </summary>
public sealed record PlaylistCreateRequestedMessage : AppMessage;

/// <summary>
/// Der Anwender fordert das Playlist-Formular zur Bearbeitung der
/// geöffneten Playlist an („Bearbeiten" auf der Detailseite) — das
/// Formular öffnet sich mit den aktuellen Kopfdaten; der
/// Sortiermodus ist wie in der Server-UI nur lesbar.
/// </summary>
public sealed record PlaylistEditRequestedMessage : AppMessage;

/// <summary>Der Anwender ändert den Inhalt des Namensfelds des Playlist-Formulars.</summary>
/// <param name="Name">Der neue Feldinhalt.</param>
/// <returns>Nachricht mit dem neuen Namen.</returns>
public sealed record PlaylistFormNameChangedMessage(string Name) : AppMessage;

/// <summary>Der Anwender ändert den Inhalt des Beschreibungsfelds des Playlist-Formulars.</summary>
/// <param name="Description">Der neue Feldinhalt.</param>
/// <returns>Nachricht mit der neuen Beschreibung.</returns>
public sealed record PlaylistFormDescriptionChangedMessage(string? Description) : AppMessage;

/// <summary>Der Anwender wählt den Sortiermodus im Playlist-Formular (nur bei Neuanlage wirksam).</summary>
/// <param name="SortMode">Der gewählte Sortiermodus (<c>PlaylistSortModeValues</c>).</param>
/// <returns>Nachricht mit dem gewählten Sortiermodus.</returns>
public sealed record PlaylistFormSortModeSelectedMessage(string SortMode) : AppMessage;

/// <summary>Der Anwender bricht das Playlist-Formular ab — es schließt ohne Änderung.</summary>
public sealed record PlaylistFormCancelMessage : AppMessage;

/// <summary>
/// Der Anwender fordert das Speichern des Playlist-Formulars an:
/// bei gültigem Namen läuft <c>SavePlaylistEffect</c>
/// (<c>POST api/playlists</c> bzw. <c>PUT api/playlists/{id}</c>,
/// Ergebnis: <c>PlaylistFormCompletedMessage</c>); ein leerer Name
/// bleibt mit Inline-Fehltext im Formular.
/// </summary>
public sealed record PlaylistFormSaveRequestedMessage : AppMessage;

/// <summary>
/// Ergebnis des Formular-Speicherns: bei Erfolg trägt die Nachricht
/// die gespeicherte Playlist (Anlage oder Aktualisierung), bei
/// Fehlschlag den Fehlertext — das Formular bleibt dann geöffnet.
/// </summary>
/// <param name="Saved">Die gespeicherte Playlist; <c>null</c> bei Fehlschlag.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit dem Speicherergebnis.</returns>
public sealed record PlaylistFormCompletedMessage(
    PlaylistListItem? Saved,
    string? Error) : AppMessage;

/// <summary>
/// Der Anwender hat das Löschen der geöffneten Playlist bestätigt
/// (Bestätigungsdialog der Detailseite) — <c>DeletePlaylistEffect</c>
/// (<c>DELETE api/playlists/{id}</c>, Ergebnis:
/// <c>PlaylistDeletedMessage</c>).
/// </summary>
/// <param name="PlaylistId">Id der zu löschenden Playlist.</param>
/// <returns>Nachricht mit der Lösch-Bestätigung.</returns>
public sealed record PlaylistDeleteConfirmedMessage(long PlaylistId) : AppMessage;

/// <summary>
/// Ergebnis des Playlist-Löschens: bei Erfolg wird der Detail-
/// Zustand geleert, die Seite verlassen und die Übersicht neu
/// geladen; bei Fehlschlag bleibt die Seite mit Fehlerhinweis.
/// </summary>
/// <param name="PlaylistId">Id der gelöschten Playlist.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit dem Löschergebnis.</returns>
public sealed record PlaylistDeletedMessage(
    long PlaylistId,
    string? Error) : AppMessage;

/// <summary>
/// Der Anwender fordert das Entfernen eines Eintrags aus der
/// geöffneten Playlist an (Kontextmenü des Eintrags) —
/// <c>RemovePlaylistEntryEffect</c> ohne Bestätigungsflagge
/// (Ergebnis: <c>PlaylistEntryRemoveCompletedMessage</c>).
/// </summary>
/// <param name="Entry">Der zu entfernende Eintrag.</param>
/// <returns>Nachricht mit der Entfernen-Anforderung.</returns>
public sealed record PlaylistEntryRemoveRequestedMessage(PlaylistEntryItem Entry) : AppMessage;

/// <summary>
/// Ergebnis eines Eintrags-Entfernens: bei Erfolg wird die
/// Eintragsliste neu geladen; <paramref name="RequiresConfirmation"/>
/// meldet den Weiterschauen-Konflikt (HTTP 409) — die Detailseite
/// öffnet dafür den Bestätigungsdialog.
/// </summary>
/// <param name="Entry">Der betroffene Eintrag.</param>
/// <param name="Removed"><c>true</c> = der Eintrag wurde entfernt.</param>
/// <param name="RequiresConfirmation"><c>true</c> = der Server verlangt die Weiterschauen-Bestätigung.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg/Konflikt.</param>
/// <returns>Nachricht mit dem Entfernen-Ergebnis.</returns>
public sealed record PlaylistEntryRemoveCompletedMessage(
    PlaylistEntryItem Entry,
    bool Removed,
    bool RequiresConfirmation,
    string? Error) : AppMessage;

/// <summary>
/// Ergebnis des Bestätigungsdialogs zum Weiterschauen-Konflikt:
/// bei <paramref name="Confirmed"/> läuft das Entfernen erneut mit
/// der Bestätigungsflagge; bei Abbruch wird der Konflikt-Zustand
/// ohne Serveraufruf aufgelöst.
/// </summary>
/// <param name="Confirmed"><c>true</c> = das Entfernen wird bestätigt wiederholt.</param>
/// <returns>Nachricht mit der Dialog-Entscheidung.</returns>
public sealed record PlaylistEntryRemoveConfirmedMessage(bool Confirmed) : AppMessage;

/// <summary>
/// Der Anwender fordert „An den Anfang" für einen Eintrag an
/// (Kontextmenü, nur manueller Sortiermodus) —
/// <c>MovePlaylistEntryToBeginningEffect</c>.
/// </summary>
/// <param name="Entry">Der zu verschiebende Eintrag.</param>
/// <returns>Nachricht mit der Verschiebe-Anforderung.</returns>
public sealed record PlaylistEntryMoveToBeginningRequestedMessage(PlaylistEntryItem Entry) : AppMessage;

/// <summary>
/// Der Anwender fordert „Ans Ende" für einen Eintrag an
/// (Kontextmenü, nur manueller Sortiermodus) —
/// <c>MovePlaylistEntryToEndEffect</c> (max-sort-order + 1).
/// </summary>
/// <param name="Entry">Der zu verschiebende Eintrag.</param>
/// <returns>Nachricht mit der Verschiebe-Anforderung.</returns>
public sealed record PlaylistEntryMoveToEndRequestedMessage(PlaylistEntryItem Entry) : AppMessage;

/// <summary>
/// Der Anwender fordert das Verschieben eines Eintrags auf die
/// Position eines Nachbarn an („Eins nach vorn/hinten", nur
/// manueller Sortiermodus) — <c>MovePlaylistEntryBetweenEffect</c>
/// mit der SortOrder des Ziel-Nachbarn.
/// </summary>
/// <param name="Entry">Der zu verschiebende Eintrag.</param>
/// <param name="NewSortOrder">Ziel-Sortierreihenfolge (SortOrder des Nachbarn).</param>
/// <returns>Nachricht mit der Verschiebe-Anforderung.</returns>
public sealed record PlaylistEntryMoveBetweenRequestedMessage(
    PlaylistEntryItem Entry,
    long NewSortOrder) : AppMessage;

/// <summary>
/// Ergebnis eines Eintrags-Verschiebens: bei Erfolg wird die
/// Eintragsliste neu geladen (neue Server-Reihenfolge); bei
/// Fehlschlag bleibt der Fehlerhinweis sichtbar.
/// </summary>
/// <param name="Entry">Der verschobene Eintrag.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit dem Verschiebe-Ergebnis.</returns>
public sealed record PlaylistEntryMoveCompletedMessage(
    PlaylistEntryItem Entry,
    string? Error) : AppMessage;

/// <summary>
/// Der Anwender fordert den Wechsel des Sortiermodus an (Menü der
/// Detailseite) — <c>ChangePlaylistSortModeEffect</c> ohne
/// Bestätigungsflagge (Ergebnis:
/// <c>PlaylistSortModeCompletedMessage</c>).
/// </summary>
/// <param name="NewSortMode">Der angeforderte Sortiermodus.</param>
/// <returns>Nachricht mit der Wechsel-Anforderung.</returns>
public sealed record PlaylistSortModeRequestedMessage(string NewSortMode) : AppMessage;

/// <summary>
/// Ergebnis des Sortiermodus-Wechsels: bei Erfolg die aktualisierte
/// Playlist; <paramref name="RequiresConfirmation"/> meldet den
/// Verlust-Konflikt (HTTP 409) — die Detailseite öffnet dafür den
/// Bestätigungsdialog.
/// </summary>
/// <param name="NewSortMode">Der angeforderte Sortiermodus.</param>
/// <param name="Updated">Die aktualisierte Playlist; <c>null</c> bei Konflikt/Fehlschlag.</param>
/// <param name="RequiresConfirmation"><c>true</c> = der Server verlangt die Verlust-Bestätigung.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg/Konflikt.</param>
/// <returns>Nachricht mit dem Wechselergebnis.</returns>
public sealed record PlaylistSortModeCompletedMessage(
    string NewSortMode,
    PlaylistListItem? Updated,
    bool RequiresConfirmation,
    string? Error) : AppMessage;

/// <summary>
/// Ergebnis des Bestätigungsdialogs zum Sortiermodus-Konflikt:
/// bei <paramref name="Confirmed"/> läuft der Wechsel erneut mit
/// <c>ConfirmLossOfManualOrder</c>; bei Abbruch wird der
/// Konflikt-Zustand ohne Serveraufruf aufgelöst.
/// </summary>
/// <param name="Confirmed"><c>true</c> = der Wechsel wird bestätigt wiederholt.</param>
/// <returns>Nachricht mit der Dialog-Entscheidung.</returns>
public sealed record PlaylistSortModeConfirmedMessage(bool Confirmed) : AppMessage;

/// <summary>
/// Der Anwender fordert den Genre-Editor der geöffneten Playlist an
/// — <c>LoadGenreOptionsEffect</c> lädt die Genre-Optionen des
/// Servers mit der Vorauswahl der Playlist-Genres (Ergebnis:
/// <c>PlaylistGenreOptionsLoadedMessage</c>).
/// </summary>
public sealed record PlaylistGenreEditorRequestedMessage : AppMessage;

/// <summary>
/// Ergebnis des Genre-Optionen-Laufs: die Optionen tragen die
/// Vorauswahl der aktuellen Playlist-Genres; bei Fehlschlag zeigt
/// der Editor den Fehlertext.
/// </summary>
/// <param name="Options">Die Genre-Optionen mit Vorauswahl.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit den geladenen Optionen.</returns>
public sealed record PlaylistGenreOptionsLoadedMessage(
    IReadOnlyList<PlaylistGenreOptionItem> Options,
    string? Error) : AppMessage;

/// <summary>Der Anwender schaltet eine Genre-Option im Editor um.</summary>
/// <param name="GenreId">Id der umgeschalteten Option.</param>
/// <returns>Nachricht mit der Genre-Id.</returns>
public sealed record PlaylistGenreToggledMessage(long GenreId) : AppMessage;

/// <summary>
/// Der Anwender speichert die Genre-Auswahl —
/// <c>SetPlaylistGenresEffect</c> (<c>PUT api/playlists/{id}/genres</c>,
/// Ergebnis: <c>PlaylistGenresCompletedMessage</c>).
/// </summary>
public sealed record PlaylistGenresSaveRequestedMessage : AppMessage;

/// <summary>
/// Der Anwender fordert das Zurücksetzen der Genres auf die
/// berechneten Werte an — <c>ResetPlaylistGenresEffect</c>
/// (<c>POST api/playlists/{id}/genres/reset</c>, Ergebnis:
/// <c>PlaylistGenresCompletedMessage</c>).
/// </summary>
public sealed record PlaylistGenresResetRequestedMessage : AppMessage;

/// <summary>
/// Ergebnis des Genre-Speicherns/-Zurücksetzens: bei Erfolg
/// schließt der Editor und die Kopfdaten der Detailseite werden
/// übernommen; bei Fehlschlag bleibt der Editor mit Fehlertext.
/// </summary>
/// <param name="Updated">Die aktualisierte Playlist; <c>null</c> bei Fehlschlag.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit dem Ergebnis.</returns>
public sealed record PlaylistGenresCompletedMessage(
    PlaylistListItem? Updated,
    string? Error) : AppMessage;

/// <summary>Der Anwender bricht den Genre-Editor ab — er schließt ohne Änderung.</summary>
public sealed record PlaylistGenreEditorCancelMessage : AppMessage;

/// <summary>
/// Der Anwender schaltet das Öffentlich-Flag der geöffneten Playlist
/// um (nur eigene Playlist + Administrator-Claim) —
/// <c>SetPlaylistPublicEffect</c> (<c>PUT api/playlists/{id}/public</c>,
/// Ergebnis: <c>PlaylistPublicCompletedMessage</c>).
/// </summary>
/// <param name="IsPublic">Der neue Öffentlich-Zustand.</param>
/// <returns>Nachricht mit der Umschalt-Anforderung.</returns>
public sealed record PlaylistPublicToggledMessage(bool IsPublic) : AppMessage;

/// <summary>
/// Ergebnis der Öffentlich-Umschaltung: bei Erfolg die aktualisierte
/// Playlist; bei Fehlschlag (z. B. fehlende Administrator-Rechte,
/// HTTP 403) der Fehlertext.
/// </summary>
/// <param name="Updated">Die aktualisierte Playlist; <c>null</c> bei Fehlschlag.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit dem Ergebnis.</returns>
public sealed record PlaylistPublicCompletedMessage(
    PlaylistListItem? Updated,
    string? Error) : AppMessage;

/// <summary>
/// Der Anwender fordert „Zu Playlist hinzufügen" auf der
/// Medien-Detailseite an — die Ziel-Auswahl (eigene Playlists per
/// Klartextname) öffnet sich, sobald
/// <c>PlaylistTargetsLoadedMessage</c> die Ziele liefert.
/// </summary>
public sealed record PlaylistAddTargetsRequestedMessage : AppMessage;

/// <summary>
/// Ergebnis des Ladens der Playlist-Auswahlziele (nur eigene
/// Playlists — Hinzufügen ist nur dort erlaubt).
/// </summary>
/// <param name="Targets">Die eigenen Playlists; <c>null</c> bei Fehlschlag.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit den Auswahlzielen.</returns>
public sealed record PlaylistTargetsLoadedMessage(
    IReadOnlyList<PlaylistListItem>? Targets,
    string? Error) : AppMessage;

/// <summary>
/// Die Playlist-Auswahl der Medien-Detailseite wurde angezeigt und
/// geschlossen: <paramref name="PlaylistId"/> trägt die Wahl
/// (<c>AddPlaylistEntryEffect</c>); <c>null</c> = Abbruch ohne
/// Serveraufruf.
/// </summary>
/// <param name="PlaylistId">Id der gewählten Playlist; <c>null</c>/0 = abgebrochen.</param>
/// <returns>Nachricht mit der Auswahl-Entscheidung.</returns>
public sealed record PlaylistPickerDismissedMessage(long? PlaylistId) : AppMessage;

/// <summary>
/// Ergebnis des Hinzufügens eines Mediums zu einer Playlist: der
/// Hinweistext (serverseitige Meldung bzw. Standardtext) wird auf
/// der Detailseite angezeigt; bei Fehlschlag der Fehlertext.
/// </summary>
/// <param name="PlaylistName">Name der Ziel-Playlist (für den Hinweistext).</param>
/// <param name="ResultMessage">Serverseitiger Hinweistext; <c>null</c> = Standardtext verwenden.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit dem Hinzufüge-Ergebnis.</returns>
public sealed record PlaylistEntryAddedMessage(
    string PlaylistName,
    string? ResultMessage,
    string? Error) : AppMessage;

/// <summary>Die Profil-Seite wurde geöffnet — die Profilinformationen werden (erneut) ermittelt.</summary>
public sealed record ProfilePageOpenedMessage : AppMessage;

/// <summary>Die Profilinformationen wurden ermittelt.</summary>
/// <param name="Info">Die ermittelten Profilinformationen.</param>
/// <returns>Nachricht mit den Profilinformationen.</returns>
public sealed record ProfileInfoLoadedMessage(ProfileInfo Info) : AppMessage;

/// <summary>
/// Der Anwender fordert die Abmeldung an — die Sitzung endet lokal
/// (Token/Zugangsdaten werden verworfen, die Ereignisverbindung getrennt)
/// und die App wechselt in den Anmelde-Workflow. Die API besitzt keinen
/// Logout-Endpunkt (zustandslose JWT).
/// </summary>
public sealed record LogoutRequestedMessage : AppMessage;

/// <summary>
/// Der Anwender startet die Wiedergabe eines Mediums („Abspielen" auf der
/// Detailseite) — der Player-State wird gesetzt, zur Wiedergabeseite
/// navigiert und die Quelle aufgelöst. Abspielbar sind nur
/// <c>movie</c> und <c>tvshowepisode</c>.
/// </summary>
/// <param name="MediaType">Medientyp (wird normalisiert).</param>
/// <param name="MediaId">Id des Mediums.</param>
/// <param name="Title">Anzeigetitel des Mediums.</param>
/// <returns>Nachricht für die Wiedergabe-Anforderung.</returns>
public sealed record PlayRequestedMessage(
    string MediaType,
    long MediaId,
    string? Title) : AppMessage;

/// <summary>Die Wiedergabeseite ist erschienen (einmalig pro Öffnen).</summary>
public sealed record PlayerPageOpenedMessage : AppMessage;

/// <summary>
/// Ergebnis der Quellauflösung: bei Erfolg trägt die Nachricht die
/// aufgelöste Quelle; bei Fehlschlag <paramref name="Error"/>
/// (Rückfall auf die nächste Episode, falls vorhanden).
/// </summary>
/// <param name="MediaType">Medientyp der angefragten Identität.</param>
/// <param name="MediaId">Medien-Id der angefragten Identität.</param>
/// <param name="Source">Aufgelöste Quelle; <c>null</c> bei Fehlschlag.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit dem Ergebnis der Quellauflösung.</returns>
public sealed record PlaybackSourceResolvedMessage(
    string MediaType,
    long MediaId,
    PlaybackSource? Source,
    string? Error) : AppMessage;

/// <summary>
/// Positions-Takt der Wiedergabeseite (Seiten-Timer während der
/// Wiedergabe bzw. Zustandswechsel des MediaElement): aktualisiert
/// Position/Dauer/Pufferung und prüft die „angesehen"-Schwelle
/// (Endfenster-Markierung); der Wechsel zum Folgetitel erfolgt
/// ausschließlich über das Wiedergabeende.
/// </summary>
/// <param name="Position">Aktuelle Wiedergabeposition.</param>
/// <param name="Duration">Gesamtdauer des Mediums.</param>
/// <param name="IsPlaying"><c>true</c>, wenn die Wiedergabe läuft.</param>
/// <param name="IsBuffering"><c>true</c>, wenn der Player puffert.</param>
/// <param name="MediaId">
/// Id des Mediums, dessen Wiedergabeelement die Werte meldet (die zuletzt
/// am Element angewendete Identität); <c>0</c> = noch keine Quelle
/// angewendet. Ein Takt mit abweichender Id wird verworfen — nachlaufende
/// Meldungen des Vorgängermediums dürfen den Folgezustand nicht treffen.
/// </param>
/// <returns>Nachricht mit dem Wiedergabe-Takt.</returns>
public sealed record PlaybackProgressMessage(
    TimeSpan Position,
    TimeSpan Duration,
    bool IsPlaying,
    bool IsBuffering,
    long MediaId) : AppMessage;

/// <summary>Die Wiedergabe ist regulär zu Ende (Medium abgespielt).</summary>
public sealed record PlaybackEndedMessage : AppMessage;

/// <summary>Die Wiedergabe ist fehlgeschlagen (MediaElement <c>MediaFailed</c>).</summary>
public sealed record PlaybackFailedMessage : AppMessage;

/// <summary>Der Picture-in-Picture-/Mini-Player-Modus soll umgeschaltet werden.</summary>
public sealed record PictureInPictureRequestedMessage : AppMessage;

/// <summary>
/// Ergebnis der PiP-Umschaltung: der Plattform-Dienst meldet den neuen
/// Modus zurück (Grundlage der Overlay-Ausblendung im Mini-Player).
/// </summary>
/// <param name="IsActive"><c>true</c> = kompaktes Overlay aktiv.</param>
/// <returns>Nachricht mit dem neuen PiP-Zustand.</returns>
public sealed record PictureInPictureChangedMessage(bool IsActive) : AppMessage;

/// <summary>
/// Die Wiedergabeseite wird verlassen — Abschluss-Fortschritt melden
/// und den Player-Zustand zurücksetzen.
/// </summary>
public sealed record PlayerClosedMessage : AppMessage;

/// <summary>
/// Die Downloads-Seite wurde geöffnet — das Download-Manifest wird
/// (erneut) gelesen. Die Seite arbeitet ohne Serververbindung, die
/// Daten liegen lokal vor.
/// </summary>
public sealed record DownloadsPageOpenedMessage : AppMessage;

/// <summary>
/// Die Download-Liste wurde aus dem Manifest geladen
/// (<paramref name="Error"/> bei Fehlschlag).
/// </summary>
/// <param name="Items">Die Download-Einträge in Auftragsreihenfolge.</param>
/// <param name="UsedBytes">Durch Download-Dateien belegter Speicher.</param>
/// <param name="QuotaGb">Konfiguriertes Speicherplatzkontingent in Gigabyte.</param>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <param name="OrphanedBytes">Bytes erkannter Restdaten ohne Manifest-Eintrag (Abgleich beim Laden).</param>
/// <returns>Nachricht mit dem Ladeergebnis der Download-Liste.</returns>
public sealed record DownloadsLoadedMessage(
    IReadOnlyList<DownloadListItem> Items,
    long UsedBytes,
    int QuotaGb,
    string? Error,
    long OrphanedBytes = 0) : AppMessage;

/// <summary>
/// Der Anwender fordert den Download eines Mediums an („Herunterladen"
/// auf der Detailseite bzw. je Episoden-Eintrag). Nur abspielbare Typen
/// (<c>movie</c>/<c>tvshowepisode</c>) werden angenommen.
/// </summary>
/// <param name="MediaType">Medientyp (wird normalisiert).</param>
/// <param name="MediaId">Id des Mediums.</param>
/// <param name="Title">Anzeigetitel des Downloads; <c>null</c> = Fallback.</param>
/// <returns>Nachricht für die Download-Anforderung.</returns>
public sealed record DownloadRequestedMessage(
    string MediaType,
    long MediaId,
    string? Title) : AppMessage
{
    /// <summary>Id der zugehörigen Serie (Episoden-Downloads); <c>null</c> = unbekannt/kein Episoden-Download.</summary>
    public long? ShowId { get; init; }

    /// <summary>Anzeigetitel der Serie (Episoden-Downloads); <c>null</c> = unbekannt.</summary>
    public string? ShowTitle { get; init; }

    /// <summary>Staffelnummer der Episode (Episoden-Downloads); <c>null</c> = unbekannt.</summary>
    public int? SeasonNumber { get; init; }

    /// <summary>Episodennummer innerhalb der Staffel (Episoden-Downloads); <c>null</c> = unbekannt.</summary>
    public int? EpisodeNumber { get; init; }

    /// <summary>Bild-Id des Vorschaubilds (<c>api/pictures/{id}</c> — Episoden-Still bzw. Film-Poster); <c>null</c> = kein Bild bekannt.</summary>
    public long? PictureId { get; init; }
}

/// <summary>
/// Ergebnis einer Download-Einreihe-Anforderung: <paramref name="Item"/>
/// trägt den Snapshot des Manifest-Eintrags (bei ungültiger Anforderung
/// <c>null</c>), <paramref name="Notice"/> den Hinweistext für die
/// Detailseite.
/// </summary>
/// <param name="Item">Der Manifest-Eintrag als Anzeige-Snapshot; <c>null</c> bei ungültiger Anforderung.</param>
/// <param name="Notice">Hinweistext (eingeplant/bereits vorhanden/Fehler); <c>null</c> = kein Hinweis.</param>
/// <param name="IsError"><c>true</c>, wenn der Hinweis eine Ablehnung/einen Fehler beschreibt.</param>
/// <param name="MediaType">Medientyp der Anforderung (normalisiert).</param>
/// <param name="MediaId">Medien-Id der Anforderung.</param>
/// <returns>Nachricht mit dem Einreihe-Ergebnis.</returns>
public sealed record DownloadEnqueueCompletedMessage(
    DownloadListItem? Item,
    string? Notice,
    bool IsError,
    string MediaType,
    long MediaId) : AppMessage;

/// <summary>
/// Ein Download-Eintrag hat sich geändert (Zustand oder Fortschritt) —
/// vom <see cref="DownloadService"/> gemeldet und vom Effekt-Handler in
/// den MVU-Zyklus eingespeist.
/// </summary>
/// <param name="Item">Der geänderte Eintrag (Snapshot).</param>
/// <returns>Nachricht mit dem geänderten Eintrag.</returns>
public sealed record DownloadItemChangedMessage(DownloadListItem Item) : AppMessage;

/// <summary>
/// Ein Download-Eintrag wurde aus dem Manifest entfernt (Löschen durch
/// den Anwender oder automatisches Entfernen gesehener Episoden).
/// </summary>
/// <param name="MediaType">Medientyp des entfernten Downloads (normalisiert).</param>
/// <param name="MediaId">Id des Mediums.</param>
/// <returns>Nachricht über das Entfernen.</returns>
public sealed record DownloadDeletedMessage(string MediaType, long MediaId) : AppMessage;

/// <summary>Einen wartenden/laufenden Download abbrechen.</summary>
/// <param name="Item">Der abzubrechende Download-Eintrag.</param>
/// <returns>Nachricht für die Abbruch-Anforderung.</returns>
public sealed record DownloadCancelRequestedMessage(DownloadListItem Item) : AppMessage;

/// <summary>Einen fehlgeschlagenen/abgebrochenen Download erneut einreihen.</summary>
/// <param name="Item">Der zu wiederholende Download-Eintrag.</param>
/// <returns>Nachricht für die Wiederholen-Anforderung.</returns>
public sealed record DownloadRetryRequestedMessage(DownloadListItem Item) : AppMessage;

/// <summary>Einen Download vollständig entfernen (Datei + Manifest-Eintrag).</summary>
/// <param name="Item">Der zu löschende Download-Eintrag.</param>
/// <returns>Nachricht für die Lösch-Anforderung.</returns>
public sealed record DownloadDeleteRequestedMessage(DownloadListItem Item) : AppMessage;

/// <summary>Der Anwender fordert das Löschen aller Downloads an (nach Rückfrage im UI).</summary>
public sealed record DownloadsDeleteAllRequestedMessage : AppMessage;

/// <summary>Der Anwender fordert die Bereinigung erkannter Restdaten ohne Manifest-Eintrag an.</summary>
public sealed record DownloadsPurgeOrphansRequestedMessage : AppMessage;

/// <summary>Der Anwender fordert das Löschen aller fehlgeschlagenen und abgebrochenen Downloads an (nach Rückfrage im UI).</summary>
public sealed record DownloadsDeleteFailedRequestedMessage : AppMessage;

/// <summary>
/// Der Anwender fordert den Protokollversand an („Protokoll senden" auf der
/// Einstellungsseite) — nur wirksam bei aktivierter Protokollierung.
/// </summary>
public sealed record SendLogProtocolRequestedMessage : AppMessage;

/// <summary>
/// Ergebnis des Protokollversands: <paramref name="Error"/> trägt den
/// anwendersichtbaren Fehlertext; <c>null</c> = Mailentwurf bzw.
/// Share-Fallback wurde geöffnet.
/// </summary>
/// <param name="Error">Fehlertext bei Fehlschlag; <c>null</c> bei Erfolg.</param>
/// <returns>Nachricht mit dem Versandergebnis.</returns>
public sealed record SendLogProtocolCompletedMessage(string? Error) : AppMessage;
