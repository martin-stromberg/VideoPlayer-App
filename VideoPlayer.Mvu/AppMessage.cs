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
/// Position/Dauer/Pufferung und prüft die „angesehen"-Schwelle und die
/// Auto-Fortsetzung.
/// </summary>
/// <param name="Position">Aktuelle Wiedergabeposition.</param>
/// <param name="Duration">Gesamtdauer des Mediums.</param>
/// <param name="IsPlaying"><c>true</c>, wenn die Wiedergabe läuft.</param>
/// <param name="IsBuffering"><c>true</c>, wenn der Player puffert.</param>
/// <returns>Nachricht mit dem Wiedergabe-Takt.</returns>
public sealed record PlaybackProgressMessage(
    TimeSpan Position,
    TimeSpan Duration,
    bool IsPlaying,
    bool IsBuffering) : AppMessage;

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
/// <returns>Nachricht mit dem Ladeergebnis der Download-Liste.</returns>
public sealed record DownloadsLoadedMessage(
    IReadOnlyList<DownloadListItem> Items,
    long UsedBytes,
    int QuotaGb,
    string? Error) : AppMessage;

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
