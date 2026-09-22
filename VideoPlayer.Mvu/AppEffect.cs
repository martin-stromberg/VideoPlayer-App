using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>
/// Basistyp aller MVU-Effekte. Effekte beschreiben Seiteneffekte als Daten;
/// ausgeführt werden sie vom <see cref="IAppEffectHandler"/> am App-Rand.
/// </summary>
public abstract record AppEffect;

/// <summary>Navigation zu einer Menüseite ausführen.</summary>
public sealed record NavigateToPageEffect(AppPage Page) : AppEffect;

/// <summary>Server-Erreichbarkeitsprüfung ausführen.</summary>
public sealed record CheckServerHealthEffect : AppEffect;

/// <summary>Lokale Ablage initialisieren (Verzeichnisse, Datenbanktabellen).</summary>
public sealed record InitializeStorageEffect : AppEffect;

/// <summary>Einstellungen aus der Ablage lesen (Ergebnis: <c>SettingsLoadedMessage</c>).</summary>
public sealed record LoadSettingsEffect : AppEffect;

/// <summary>Einstellungen in die Ablage schreiben.</summary>
public sealed record PersistSettingsEffect(AppSettings Settings) : AppEffect;

/// <summary>
/// Serververwaltung laden: Legacy-Migration, bekannte Server und aktive
/// Adresse ermitteln (Ergebnis: <c>ServersLoadedMessage</c>).
/// </summary>
public sealed record LoadServersEffect : AppEffect;

/// <summary>Netzwerk-Erkennung ausführen (Ergebnis: <c>ServerDiscoveryCompletedMessage</c>).</summary>
public sealed record DiscoverServersEffect : AppEffect;

/// <summary>Kandidatenadresse per Health-Check prüfen (Ergebnis: <c>ServerCheckCompletedMessage</c>).</summary>
public sealed record CheckCandidateServerEffect(string Address, string? DisplayName) : AppEffect;

/// <summary>Erfolgreich geprüften Server als bekannten/aktiven Server speichern.</summary>
public sealed record PersistActiveServerEffect(string Address, string? DisplayName) : AppEffect;

/// <summary>
/// Automatische Anmeldung mit den gespeicherten Zugangsdaten ausführen
/// (Ergebnis: <c>AutoLoginCompletedMessage</c>).
/// </summary>
public sealed record AttemptAutoLoginEffect(string ServerAddress) : AppEffect;

/// <summary>
/// Manuelle Anmeldung mit eingegebenen Zugangsdaten ausführen
/// (Ergebnis: <c>LoginCompletedMessage</c>).
/// </summary>
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
/// <c>mediadetails</c>, geschoben auf den Navigationsstack).
/// </summary>
public sealed record NavigateToMediaDetailsEffect(
    string MediaType,
    long MediaId,
    string Title) : AppEffect;
