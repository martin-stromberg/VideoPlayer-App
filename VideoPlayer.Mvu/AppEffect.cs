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
/// <returns>Effekt-Datensatz für die Quellauflösung.</returns>
public sealed record ResolvePlaybackSourceEffect(
    string MediaType,
    long MediaId) : AppEffect;

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
