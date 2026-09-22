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
