using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>
/// Basistyp aller MVU-Effekte. Effekte beschreiben Seiteneffekte als Daten;
/// ausgeführt werden sie vom <see cref="IAppEffectHandler"/> am App-Rand.
/// </summary>
public abstract record AppEffect;

/// <summary>Navigation zu einer Menüseite ausführen.</summary>
/// <param name="Page">Zielseite der Navigation.</param>
/// <returns>Effekt-Datensatz für die Navigation.</returns>
public sealed record NavigateToPageEffect(AppPage Page) : AppEffect;

/// <summary>Server-Erreichbarkeitsprüfung ausführen.</summary>
public sealed record CheckServerHealthEffect : AppEffect;

/// <summary>Lokale Ablage initialisieren (Verzeichnisse, Datenbanktabellen).</summary>
public sealed record InitializeStorageEffect : AppEffect;

/// <summary>Einstellungen aus der Ablage lesen (Ergebnis: <c>SettingsLoadedMessage</c>).</summary>
public sealed record LoadSettingsEffect : AppEffect;

/// <summary>Einstellungen in die Ablage schreiben.</summary>
/// <param name="Settings">Die zu speichernden Einstellungen.</param>
/// <returns>Effekt-Datensatz für das Speichern.</returns>
public sealed record PersistSettingsEffect(AppSettings Settings) : AppEffect;

/// <summary>
/// Serververwaltung laden: Legacy-Migration, bekannte Server und aktive
/// Adresse ermitteln (Ergebnis: <c>ServersLoadedMessage</c>).
/// </summary>
public sealed record LoadServersEffect : AppEffect;

/// <summary>Netzwerk-Erkennung ausführen (Ergebnis: <c>ServerDiscoveryCompletedMessage</c>).</summary>
public sealed record DiscoverServersEffect : AppEffect;

/// <summary>Kandidatenadresse per Health-Check prüfen (Ergebnis: <c>ServerCheckCompletedMessage</c>).</summary>
/// <param name="Address">Zu prüfende Serveradresse.</param>
/// <param name="DisplayName">Anzeigename des Kandidaten (bei Erkennung); <c>null</c> bei manueller Eingabe.</param>
/// <returns>Effekt-Datensatz für die Prüfung.</returns>
public sealed record CheckCandidateServerEffect(string Address, string? DisplayName) : AppEffect;

/// <summary>Erfolgreich geprüften Server als bekannten/aktiven Server speichern.</summary>
/// <param name="Address">Kanonische Adresse des geprüften Servers.</param>
/// <param name="DisplayName">Anzeigename des Servers; <c>null</c>, wenn unbekannt.</param>
/// <returns>Effekt-Datensatz für das Speichern.</returns>
public sealed record PersistActiveServerEffect(string Address, string? DisplayName) : AppEffect;

/// <summary>
/// Pairing-Austausch ausführen: den eingegebenen Einmal-Code gegen ein
/// Geräte-Token tauschen (<c>api/pairing/exchange</c>; Ergebnis:
/// <c>PairingCompletedMessage</c>).
/// </summary>
/// <param name="ServerAddress">Adresse des Pairing-Servers.</param>
/// <param name="Code">Der normalisierte Einmal-Code.</param>
/// <returns>Effekt-Datensatz für den Pairing-Austausch.</returns>
public sealed record ExchangePairingCodeEffect(string ServerAddress, string Code) : AppEffect;

/// <summary>
/// Erreichbarkeitsprüfung des Servers aus einer Ticket-Adresse
/// (QR-/Ticket-Onboarding; Ergebnis: <c>BootstrapServerCheckedMessage</c>).
/// </summary>
/// <param name="Address">Zu prüfende Serveradresse aus dem Ticket.</param>
/// <param name="Ticket">Das einzulösende Ticket (wird zur Ergebnis-Nachricht weitergereicht).</param>
/// <returns>Effekt-Datensatz für die Prüfung.</returns>
public sealed record CheckBootstrapServerEffect(string Address, string Ticket) : AppEffect;

/// <summary>
/// Bootstrap-Austausch ausführen: das Ticket gegen das verschlüsselte
/// Session-Paket einlösen (<c>api/pairing/bootstrap</c>; Ergebnis:
/// <c>BootstrapCompletedMessage</c>). Der Effekt-Handler persistiert
/// Geräte-Token, Anmelde-Token und Refresh-Token.
/// </summary>
/// <param name="ServerAddress">Adresse des Bootstrap-Servers.</param>
/// <param name="Ticket">Das einzulösende Ticket bzw. der Kurzcode.</param>
/// <returns>Effekt-Datensatz für den Bootstrap-Austausch.</returns>
public sealed record ExchangeBootstrapTicketEffect(string ServerAddress, string Ticket) : AppEffect;

/// <summary>
/// Automatische Anmeldung mit den gespeicherten Zugangsdaten ausführen
/// (Ergebnis: <c>AutoLoginCompletedMessage</c>).
/// </summary>
/// <param name="ServerAddress">Adresse des Servers, an dem angemeldet wird.</param>
/// <returns>Effekt-Datensatz für die automatische Anmeldung.</returns>
public sealed record AttemptAutoLoginEffect(string ServerAddress) : AppEffect;

/// <summary>
/// Manuelle Anmeldung mit eingegebenen Zugangsdaten ausführen
/// (Ergebnis: <c>LoginCompletedMessage</c>).
/// </summary>
/// <param name="ServerAddress">Adresse des Servers, an dem angemeldet wird.</param>
/// <param name="Username">Eingegebener Benutzername.</param>
/// <param name="Password">Eingegebenes Passwort.</param>
/// <returns>Effekt-Datensatz für die manuelle Anmeldung.</returns>
public sealed record AttemptLoginEffect(
    string ServerAddress,
    string Username,
    string Password) : AppEffect;

/// <summary>
/// Prüfen, ob lokale Offline-Daten vorhanden sind
/// (Ergebnis: <c>OfflineDataCheckCompletedMessage</c>).
/// </summary>
public sealed record CheckOfflineDataEffect : AppEffect;

/// <summary>
/// Netzwerkstatus-Überwachung starten (idempotent). Der Handler meldet den
/// Ist-Wert als <c>NetworkAvailabilityChangedMessage</c> zurück.
/// </summary>
public sealed record StartNetworkMonitoringEffect : AppEffect;

/// <summary>
/// Echtzeit-Verbindung für Server-Ereignisse zum Server aufbauen
/// (SignalR-Hub <c>hubs/mediaupdate</c>).
/// </summary>
/// <param name="ServerAddress">Adresse des Servers, zu dem verbunden wird.</param>
/// <returns>Effekt-Datensatz für den Verbindungsaufbau.</returns>
public sealed record ConnectServerEventsEffect(string ServerAddress) : AppEffect;

/// <summary>Ereignisverbindung trennen (Offline-Übergang).</summary>
public sealed record DisconnectServerEventsEffect : AppEffect;

/// <summary>
/// Vollständige Synchronisation ausführen: wartende Fortschritte
/// nachsenden, Metadaten-Caches und Weiterschauen-Liste aktualisieren
/// (Ergebnis: <c>SynchronizationCompletedMessage</c>).
/// </summary>
public sealed record RunFullSyncEffect : AppEffect;

/// <summary>Weiterschauen-Liste vom Server neu laden und cachen.</summary>
public sealed record SyncContinueWatchingEffect : AppEffect;

/// <summary>
/// Einen Weiterschauen-Eintrag ausblenden: online über
/// <c>api/continue-watching/hide</c> mit anschließender Erneuerung der
/// Cache-Kopie, offline lokal aus dem <c>cached_lists</c>-Eintrag
/// <c>continue_watching</c> entfernen (Ergebnis bei Erfolg:
/// <c>CachedListsChangedMessage</c>).
/// </summary>
/// <param name="MediaType">Medientyp des Eintrags (Server-Konvention der Weiterschauen-Liste).</param>
/// <param name="MediaId">Id des Mediums.</param>
/// <returns>Effekt-Datensatz für das Ausblenden.</returns>
public sealed record HideContinueWatchingEntryEffect(
    string MediaType,
    long MediaId) : AppEffect;

/// <summary>
/// Einen Weiterschauen-Eintrag überspringen: online über
/// <c>api/continue-watching/skip</c> mit anschließender Erneuerung der
/// Cache-Kopie, offline lokal aus dem <c>cached_lists</c>-Eintrag
/// <c>continue_watching</c> entfernen (Ergebnis bei Erfolg:
/// <c>CachedListsChangedMessage</c>).
/// </summary>
/// <param name="MediaType">Medientyp des Eintrags (Server-Konvention der Weiterschauen-Liste).</param>
/// <param name="MediaId">Id des Mediums.</param>
/// <returns>Effekt-Datensatz für das Überspringen.</returns>
public sealed record SkipContinueWatchingEntryEffect(
    string MediaType,
    long MediaId) : AppEffect;

/// <summary>Favoriten vom Server neu laden und als lokale Kopie ersetzen.</summary>
public sealed record SyncFavoritesEffect : AppEffect;

/// <summary>Quellen und „Neu im Programm" vom Server neu laden und cachen.</summary>
public sealed record SyncMetadataEffect : AppEffect;

/// <summary>
/// Gecachte Startseiten-Listen aus der lokalen Ablage lesen
/// (Ergebnis: <c>HomeListsLoadedMessage</c>). Läuft ohne Serverkontakt —
/// Cache-First-Anzeige der Startseite.
/// </summary>
public sealed record LoadHomeListsEffect : AppEffect;

/// <summary>
/// Servergeneriertes Hero-Hintergrundbild der Startseite vom
/// angemeldeten API-Client laden (<c>api/pictures/hero-background</c>;
/// Ergebnis: <c>HeroBackgroundLoadedMessage</c>). Fehlschlag ist kein
/// Fehlerzustand — der Kopfbereich zeigt dann nur Gradienten und Texte.
/// </summary>
public sealed record LoadHeroBackgroundEffect : AppEffect;

/// <summary>
/// Zur Detailseite eines Mediums navigieren (Shell-Route
/// <c>mediadetails</c>, geschoben auf den Navigationsstack). Die
/// optionalen Hints lösen Episoden/Staffeln zum Serienkontext auf.
/// </summary>
/// <param name="MediaType">Medientyp (CLR-Konvention, kleingeschrieben).</param>
/// <param name="MediaId">Id des Mediums.</param>
/// <param name="Title">Anzeigetitel (Fallback bis zum Laden der Details).</param>
/// <param name="ShowId">Optionaler Hint: Id der zugehörigen Serie.</param>
/// <param name="SeasonId">Optionaler Hint: Id der zugehörigen Staffel.</param>
/// <param name="EpisodeId">Optionaler Hint: Id der hervorzuhebenden Episode.</param>
/// <returns>Effekt-Datensatz für die Navigation zur Detailseite.</returns>
public sealed record NavigateToMediaDetailsEffect(
    string MediaType,
    long MediaId,
    string Title,
    long? ShowId = null,
    long? SeasonId = null,
    long? EpisodeId = null) : AppEffect;

/// <summary>Quellenliste vom Server laden (Ergebnis: <c>SourcesLoadedMessage</c>).</summary>
public sealed record LoadSourcesEffect : AppEffect;

/// <summary>
/// Zur Quellen-Browsing-Seite navigieren (Shell-Route
/// <c>sourceoverview?id=…&amp;name=…</c>, geschoben auf den Stack).
/// </summary>
/// <param name="SourceId">Id der zu öffnenden Quelle.</param>
/// <param name="SourceName">Anzeigename der Quelle (Seitentitel).</param>
/// <returns>Effekt-Datensatz für die Navigation zur Quellen-Browsing-Seite.</returns>
public sealed record NavigateToSourceOverviewEffect(
    long SourceId,
    string SourceName) : AppEffect;

/// <summary>Genres einer Quelle laden (Ergebnis: <c>SourceGenresLoadedMessage</c>).</summary>
/// <param name="SourceId">Id der Quelle, deren Genres geladen werden.</param>
/// <returns>Effekt-Datensatz für das Laden der Genres.</returns>
public sealed record LoadSourceGenresEffect(long SourceId) : AppEffect;

/// <summary>
/// Eine Eintragsseite einer Quelle laden — <c>api/items</c> mit
/// Paging/Suche/Genre-Filter (Ergebnis: <c>SourceItemsLoadedMessage</c>).
/// </summary>
/// <param name="SourceId">Id der Quelle.</param>
/// <param name="Page">Zu ladende Seite (0-basiert).</param>
/// <param name="SearchText">Suchtext; <c>null</c>/leer = keine Suche.</param>
/// <param name="GenreId">Genre-Filter; <c>null</c>/0 = kein Filter.</param>
/// <param name="Append"><c>true</c> = Folgeseite an die Liste anhängen, <c>false</c> = Liste ersetzen.</param>
/// <returns>Effekt-Datensatz für das Laden der Eintragsseite.</returns>
public sealed record LoadSourceItemsEffect(
    long SourceId,
    int Page,
    string? SearchText,
    long? GenreId,
    bool Append) : AppEffect;

/// <summary>
/// Detaildaten eines Mediums laden — je Medientyp der passende
/// <c>api/items/{typ}/{id}</c>-Endpunkt (Ergebnis:
/// <c>MediaDetailsLoadedMessage</c>). Hints wie bei
/// <see cref="MediaDetailsRequestedMessage"/>.
/// </summary>
/// <param name="MediaType">Medientyp (CLR-Konvention, kleingeschrieben).</param>
/// <param name="MediaId">Id des Mediums.</param>
/// <param name="Title">Fallback-Titel; <c>null</c>, wenn unbekannt.</param>
/// <param name="ShowId">Optionaler Hint: Id der zugehörigen Serie.</param>
/// <param name="SeasonId">Optionaler Hint: Id der zugehörigen Staffel.</param>
/// <param name="EpisodeId">Optionaler Hint: Id der hervorzuhebenden Episode.</param>
/// <returns>Effekt-Datensatz für das Laden der Detaildaten.</returns>
public sealed record LoadMediaDetailsEffect(
    string MediaType,
    long MediaId,
    string? Title,
    long? ShowId = null,
    long? SeasonId = null,
    long? EpisodeId = null) : AppEffect;

/// <summary>
/// Kopfbereichs-Bild der geladenen Detailseite über den angemeldeten
/// API-Client laden: Episoden-Still über <c>api/episodes/{id}/
/// background-image</c> (<paramref name="HeaderEpisodeId"/>), sonst die
/// aufgelöste Bildquelle über <c>api/pictures/{id}</c>
/// (<paramref name="HeaderPictureId"/>; Ergebnis:
/// <c>MediaDetailsHeaderImageLoadedMessage</c>). Fehlschlag ist kein
/// Fehlerzustand — der Kopfbereich zeigt dann nur Gradienten und Texte.
/// </summary>
/// <param name="MediaType">Medientyp der Detailseite (CLR-Konvention).</param>
/// <param name="MediaId">Medien-Id der Detailseite.</param>
/// <param name="HeaderPictureId">Bild-Id für <c>api/pictures</c>; <c>null</c>, wenn ein Episoden-Still gilt.</param>
/// <param name="HeaderEpisodeId">Episoden-Id des Stills; <c>null</c>, wenn <paramref name="HeaderPictureId"/> gilt.</param>
/// <returns>Effekt-Datensatz für das Laden des Kopfbereichs-Bilds.</returns>
public sealed record LoadMediaDetailsHeaderEffect(
    string MediaType,
    long MediaId,
    long? HeaderPictureId,
    long? HeaderEpisodeId) : AppEffect;

/// <summary>
/// Favoriten-Markierung eines Mediums zum Server umschalten und die
/// lokale Kopie <c>cached_favorites</c> erneuern (Ergebnis:
/// <c>FavoriteToggleCompletedMessage</c> + <c>CachedListsChangedMessage</c>).
/// </summary>
/// <param name="MediaType">Medientyp (CLR-Konvention, kleingeschrieben).</param>
/// <param name="MediaId">Id des Mediums.</param>
/// <param name="Title">Anzeigetitel des Mediums.</param>
/// <returns>Effekt-Datensatz für die Favoriten-Umschaltung.</returns>
public sealed record ToggleFavoriteEffect(
    string MediaType,
    long MediaId,
    string Title) : AppEffect;

/// <summary>
/// Favoritenliste für die Favoriten-Seite laden: bei
/// <paramref name="RefreshFromServer"/> zuerst die lokale Kopie
/// <c>cached_favorites</c> vom Serverstand erneuern (online, führendes
/// System), dann den Cache-Stand lesen; ohne Serverbezug nur den Cache
/// lesen (Ergebnis: <c>FavoritesPageLoadedMessage</c>).
/// </summary>
/// <param name="RefreshFromServer"><c>true</c> = die lokale Kopie vor dem Lesen vom Serverstand erneuern (Online-Modus); <c>false</c> = nur den Cache lesen (Offline-Modus).</param>
/// <returns>Effekt-Datensatz für das Laden der Favoritenliste.</returns>
public sealed record LoadFavoritesPageEffect(bool RefreshFromServer) : AppEffect;

/// <summary>
/// Einen Favoriten-Eintrag über <c>api/favorites/remove</c> auf dem
/// Server entfernen und die lokale Kopie erneuern (Ergebnis:
/// <c>FavoriteRemoveCompletedMessage</c> + bei Erfolg
/// <c>CachedListsChangedMessage</c>).
/// </summary>
/// <param name="FavoriteId">Id des zu entfernenden Favoriteneintrags.</param>
/// <returns>Effekt-Datensatz für das Entfernen.</returns>
public sealed record RemoveFavoriteEffect(long FavoriteId) : AppEffect;

/// <summary>
/// Eigene und öffentliche Playlists vom Server laden —
/// <c>api/playlists</c> und <c>api/playlists/public</c> (Ergebnis:
/// <c>PlaylistsLoadedMessage</c>). Reiner Online-Lauf ohne Cache.
/// </summary>
public sealed record LoadPlaylistsEffect : AppEffect;

/// <summary>
/// Zur Playlist-Detailseite navigieren (Shell-Route
/// <c>playlistdetails?id=…&amp;name=…</c>, geschoben auf den Stack).
/// </summary>
/// <param name="PlaylistId">Id der zu öffnenden Playlist.</param>
/// <param name="Name">Anzeigename der Playlist (Seitentitel).</param>
/// <returns>Effekt-Datensatz für die Navigation zur Playlist-Detailseite.</returns>
public sealed record NavigateToPlaylistDetailsEffect(
    long PlaylistId,
    string Name) : AppEffect;

/// <summary>
/// Kopfdaten einer Playlist nachladen — <c>api/playlists/{id}</c>
/// (Ergebnis: <c>PlaylistDetailsLoadedMessage</c>). Nicht-fataler
/// Nachschlag für die Detailseite ohne bekannten Listenstand.
/// </summary>
/// <param name="PlaylistId">Id der Playlist.</param>
/// <returns>Effekt-Datensatz für das Nachladen der Kopfdaten.</returns>
public sealed record LoadPlaylistDetailsEffect(long PlaylistId) : AppEffect;

/// <summary>
/// Eine Eintragsseite einer Playlist laden —
/// <c>api/playlists/{id}/entries/paged</c> in der serverseitigen
/// Reihenfolge (Ergebnis: <c>PlaylistEntriesLoadedMessage</c>).
/// </summary>
/// <param name="PlaylistId">Id der Playlist.</param>
/// <param name="Page">Zu ladende Seite (1-basiert, Server-Konvention).</param>
/// <param name="Append"><c>true</c> = Folgeseite an die Liste anhängen, <c>false</c> = Liste ersetzen.</param>
/// <returns>Effekt-Datensatz für das Laden der Eintragsseite.</returns>
public sealed record LoadPlaylistEntriesEffect(
    long PlaylistId,
    int Page,
    bool Append) : AppEffect;

/// <summary>
/// Serverseitigen Wiedergabestart einer Playlist ausführen —
/// <c>POST api/playlists/{id}/play</c> ab dem angegebenen Eintrag bzw.
/// ab dem ersten abspielbaren und zugänglichen Eintrag (Ergebnis:
/// <c>PlaylistPlaybackStartedMessage</c>). Online-Pfad.
/// </summary>
/// <param name="PlaylistId">Id der Playlist.</param>
/// <param name="EntryId">Id des Starteintrags; <c>null</c> = erster abspielbarer und zugänglicher Eintrag.</param>
/// <returns>Effekt-Datensatz für den Wiedergabestart.</returns>
public sealed record StartPlaylistPlaybackEffect(
    long PlaylistId,
    long? EntryId) : AppEffect;

/// <summary>
/// Nächsten abspielbaren Eintrag der laufenden Playlist-Wiedergabe
/// ermitteln — <c>POST api/playlists/{id}/play/next</c> (Ergebnis:
/// <c>PlaylistNavigationCompletedMessage</c>). Manuelle
/// Vorwärts-Navigation.
/// </summary>
/// <param name="PlaylistId">Id der Playlist.</param>
/// <param name="CurrentEntryId">Id des aktuell gespielten Eintrags.</param>
/// <returns>Effekt-Datensatz für die Vorwärts-Navigation.</returns>
public sealed record LoadNextPlaylistEntryEffect(
    long PlaylistId,
    long CurrentEntryId) : AppEffect;

/// <summary>
/// Vorherigen abspielbaren Eintrag der laufenden Playlist-Wiedergabe
/// ermitteln — <c>POST api/playlists/{id}/play/previous</c> (Ergebnis:
/// <c>PlaylistNavigationCompletedMessage</c>). Manuelle
/// Rückwärts-Navigation.
/// </summary>
/// <param name="PlaylistId">Id der Playlist.</param>
/// <param name="CurrentEntryId">Id des aktuell gespielten Eintrags.</param>
/// <returns>Effekt-Datensatz für die Rückwärts-Navigation.</returns>
public sealed record LoadPreviousPlaylistEntryEffect(
    long PlaylistId,
    long CurrentEntryId) : AppEffect;

/// <summary>
/// Folgeeintrag für das automatische Weiterschalten der laufenden
/// Playlist-Wiedergabe ermitteln —
/// <c>POST api/playlists/{id}/play/advance</c> (Ergebnis:
/// <c>PlaylistNavigationCompletedMessage</c>). Auto-Fortsetzung und
/// Fehler-Rückfall.
/// </summary>
/// <param name="PlaylistId">Id der Playlist.</param>
/// <param name="CurrentEntryId">Id des zu Ende bzw. fehlgeschlagen gespielten Eintrags.</param>
/// <returns>Effekt-Datensatz für das automatische Weiterschalten.</returns>
public sealed record AdvancePlaylistEntryEffect(
    long PlaylistId,
    long CurrentEntryId) : AppEffect;

/// <summary>
/// Profilinformationen der Profil-Seite ermitteln — Benutzername/E-Mail
/// aus den Claims des gespeicherten Anmelde-Tokens bzw. dem gespeicherten
/// Anmelde-Benutzernamen plus aktives lokales Profil (Ergebnis:
/// <c>ProfileInfoLoadedMessage</c>).
/// </summary>
public sealed record LoadProfileInfoEffect : AppEffect;

/// <summary>
/// Sitzung lokal beenden: angemeldeten API-Client freigeben und die
/// gespeicherten Zugangsdaten samt Token entfernen. Die API besitzt
/// keinen Logout-Endpunkt (zustandslose JWT) — die Sitzung endet durch
/// Verwerfen der lokalen Geheimnisse.
/// </summary>
public sealed record LogoutEffect : AppEffect;

/// <summary>
/// Zur Wiedergabeseite navigieren (Shell-Route <c>player</c>,
/// geschoben auf den Navigationsstack).
/// </summary>
public sealed record NavigateToPlayerEffect : AppEffect;

/// <summary>
/// Wiedergabequelle eines Mediums auflösen: lokale Datei aus dem
/// Download-Manifest bevorzugt, sonst Stream-URL mit Zugriffstoken,
/// jeweils mit Fortsetzungsposition (Ergebnis:
/// <c>PlaybackSourceResolvedMessage</c>).
/// </summary>
/// <param name="MediaType">Medientyp (normalisiert, CLR-Konvention).</param>
/// <param name="MediaId">Id des Mediums.</param>
/// <param name="StartPositionSeconds">
/// Bereits bekannte Fortsetzungsposition in Sekunden (aus der
/// Playlist-Start-/Navigationsantwort); <c>null</c> = der Dienst
/// ermittelt den Stand selbst aus der Weiterschauen-Liste.
/// </param>
/// <returns>Effekt-Datensatz für die Quellauflösung.</returns>
public sealed record ResolvePlaybackSourceEffect(
    string MediaType,
    long MediaId,
    long? StartPositionSeconds = null) : AppEffect;

/// <summary>
/// Serienkontext einer direkt aus der Weiterschauen-Liste gestarteten
/// Episode laden — ohne die Detailseite zu öffnen (online
/// <c>api/items/tvshow/{showId}</c> inklusive Cache-Auffrischung, offline
/// <see cref="ShowDetailsCache"/>; Ergebnis:
/// <c>UpcomingEpisodesLoadedMessage</c>). Grundlage der Folgevideo-/
/// Auto-Fortsetzungs-Funktion beim Direktstart.
/// </summary>
/// <param name="MediaId">Id der gestarteten Episode (Bezugspunkt der Folgeliste).</param>
/// <param name="ShowId">Id der zugehörigen Serie (Serienkontext-Hint des Weiterschauen-Eintrags).</param>
/// <returns>Effekt-Datensatz für die Serienkontext-Ladung.</returns>
public sealed record LoadUpcomingEpisodesEffect(
    long MediaId,
    long ShowId) : AppEffect;

/// <summary>
/// Wiedergabefortschritt eines Mediums an den Server melden — online
/// sofort, sonst verlustfrei über die persistente Warteschlange
/// <c>pending_progress</c> (Nachsendung bei Online-Rückkehr über den
/// Synchronisationslauf).
/// </summary>
/// <param name="MediaType">Medientyp (normalisiert, CLR-Konvention).</param>
/// <param name="MediaId">Id des Mediums.</param>
/// <param name="Position">Zu meldende Wiedergabeposition.</param>
/// <param name="Duration">Gesamtdauer des Mediums.</param>
/// <returns>Effekt-Datensatz für die Fortschrittsmeldung.</returns>
public sealed record ReportPlaybackProgressEffect(
    string MediaType,
    long MediaId,
    TimeSpan Position,
    TimeSpan Duration) : AppEffect;

/// <summary>
/// Picture-in-Picture-/Mini-Player-Modus umschalten (Ergebnis:
/// <c>PictureInPictureChangedMessage</c>). Auf Plattformen ohne
/// umschaltbaren PiP bleibt der Effekt wirkungslos.
/// </summary>
public sealed record TogglePictureInPictureEffect : AppEffect;

/// <summary>
/// Download-Liste aus dem Manifest laden — Einträge, Speicherbelegung
/// und konfiguriertes Kontingent (Ergebnis: <c>DownloadsLoadedMessage</c>).
/// Läuft ohne Serverkontakt.
/// </summary>
/// <param name="Settings">Aktuelle App-Einstellungen (Kontingent für die Anzeige).</param>
/// <returns>Effekt-Datensatz für das Laden der Download-Liste.</returns>
public sealed record LoadDownloadsEffect(AppSettings Settings) : AppEffect;

/// <summary>
/// Einen Download einreihen bzw. einen fehlgeschlagenen/abgebrochenen
/// Auftrag erneut einreihen (Ergebnis: <c>DownloadEnqueueCompletedMessage</c>;
/// Zustands-/Fortschrittsänderungen laufen über
/// <c>DownloadItemChangedMessage</c>).
/// </summary>
/// <param name="MediaType">Medientyp (normalisiert, CLR-Konvention).</param>
/// <param name="MediaId">Id des Mediums.</param>
/// <param name="Title">Anzeigetitel des Downloads; <c>null</c> = Fallback.</param>
/// <param name="Settings">Aktuelle App-Einstellungen (Kontingent, WLAN-Bedingung).</param>
/// <returns>Effekt-Datensatz für das Einreihen.</returns>
public sealed record EnqueueDownloadEffect(
    string MediaType,
    long MediaId,
    string? Title,
    AppSettings Settings) : AppEffect
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
/// Einen wartenden/laufenden Download abbrechen (Zustand →
/// <c>cancelled</c>, Teildatei wird entfernt).
/// </summary>
/// <param name="MediaType">Medientyp des Downloads (normalisiert).</param>
/// <param name="MediaId">Id des Mediums.</param>
/// <returns>Effekt-Datensatz für den Abbruch.</returns>
public sealed record CancelDownloadEffect(string MediaType, long MediaId) : AppEffect;

/// <summary>
/// Einen Download vollständig entfernen: laufenden Transfer abbrechen,
/// Datei und Manifest-Zeile löschen (Ergebnis:
/// <c>DownloadDeletedMessage</c>).
/// </summary>
/// <param name="MediaType">Medientyp des Downloads (normalisiert).</param>
/// <param name="MediaId">Id des Mediums.</param>
/// <returns>Effekt-Datensatz für das Löschen.</returns>
public sealed record DeleteDownloadEffect(string MediaType, long MediaId) : AppEffect;

/// <summary>
/// Unterbrochene Downloads wiederaufnehmen: wartende und verwaist
/// laufende Aufträge sowie nicht-Quota-Fehlschläge werden erneut
/// eingeplant (der Server-Stream unterstützt keine Range-Fortsetzung —
/// der Transfer beginnt erneut ab Byte 0). Abgebrochene Downloads und
/// Quota-Fehlschläge bleiben unberührt.
/// </summary>
/// <param name="Settings">Aktuelle App-Einstellungen (Kontingent, WLAN-Bedingung).</param>
/// <returns>Effekt-Datensatz für die Wiederaufnahme.</returns>
public sealed record ResumeDownloadsEffect(AppSettings Settings) : AppEffect;

/// <summary>
/// Automatischen Vorlauf der Weiterschauen-Liste ausführen
/// (<see cref="AutoDownloadService"/>): die nächsten Episoden je
/// Weiterschauen-Eintrag einreihen, gedrosselt auf
/// <see cref="AutoDownloadService.MinRunInterval"/>.
/// </summary>
/// <param name="Settings">Aktuelle App-Einstellungen.</param>
/// <param name="Force"><c>true</c> = Throttle ignorieren (z. B. beim Einschalten der Einstellung).</param>
/// <returns>Effekt-Datensatz für den Vorlauf-Lauf.</returns>
public sealed record RunAutoDownloadsEffect(AppSettings Settings, bool Force) : AppEffect;

/// <summary>
/// Den abgeschlossenen Download eines als gesehen markierten Mediums
/// entfernen (Einstellung „Gesehene Episoden automatisch löschen" —
/// Ergebnis bei Treffer: <c>DownloadDeletedMessage</c>).
/// </summary>
/// <param name="MediaType">Medientyp des gesehenen Mediums (normalisiert).</param>
/// <param name="MediaId">Id des Mediums.</param>
/// <returns>Effekt-Datensatz für das automatische Entfernen.</returns>
public sealed record DeleteWatchedDownloadEffect(string MediaType, long MediaId) : AppEffect;

/// <summary>
/// Protokollversand ausführen: das Programmprotokoll als Anhang eines
/// Mailentwurfs öffnen (Fallback: Teilen-Dialog) — Ergebnis:
/// <c>SendLogProtocolCompletedMessage</c>.
/// </summary>
public sealed record SendLogProtocolEffect : AppEffect;
