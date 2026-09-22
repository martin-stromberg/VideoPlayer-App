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
