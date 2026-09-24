using System.Text.Json.Serialization;

namespace VideoPlayer.E2E.Tests.Support;

/// <summary>
/// DTO des <c>GET /model</c>-Snapshots der Developer-API
/// (camelCase-Wire-Format, Spiegel von <c>DevApiModelSnapshot</c>).
/// </summary>
public sealed class DevApiModel
{
    /// <summary>Name der aktuell sichtbaren Seite (z. B. <c>ServerSetup</c>, <c>Home</c>).</summary>
    [JsonPropertyName("currentPage")] public string? CurrentPage { get; set; }

    /// <summary>Verbindungszustand (<c>NeedsServerSetup</c>, <c>NeedsLogin</c>, <c>Connected</c>, …).</summary>
    [JsonPropertyName("connectionState")] public string? ConnectionState { get; set; }

    /// <summary>Server-Gesundheitszustand des letzten Checks.</summary>
    [JsonPropertyName("serverHealth")] public string? ServerHealth { get; set; }

    /// <summary>Anzeigetext des Server-Gesundheitszustands.</summary>
    [JsonPropertyName("serverHealthText")] public string? ServerHealthText { get; set; }

    /// <summary>Startprüfung läuft.</summary>
    [JsonPropertyName("isStartupRunning")] public bool IsStartupRunning { get; set; }

    /// <summary>Anmeldung läuft.</summary>
    [JsonPropertyName("isLoginRunning")] public bool IsLoginRunning { get; set; }

    /// <summary>Fehlertext der letzten Anmeldung.</summary>
    [JsonPropertyName("loginError")] public string? LoginError { get; set; }

    /// <summary>Zuletzt eingegebener Anmeldename.</summary>
    [JsonPropertyName("loginUsername")] public string? LoginUsername { get; set; }

    /// <summary>Server aktuell online erreichbar.</summary>
    [JsonPropertyName("isOnline")] public bool IsOnline { get; set; }

    /// <summary>Offline-Modus aktiv.</summary>
    [JsonPropertyName("isOffline")] public bool IsOffline { get; set; }

    /// <summary>Lokale Offline-Daten vorhanden.</summary>
    [JsonPropertyName("hasOfflineData")] public bool HasOfflineData { get; set; }

    /// <summary>Synchronisation läuft.</summary>
    [JsonPropertyName("isSyncRunning")] public bool IsSyncRunning { get; set; }

    /// <summary>Aktive Serveradresse (kanonisch, mit abschließendem <c>/</c>).</summary>
    [JsonPropertyName("activeServerAddress")] public string? ActiveServerAddress { get; set; }

    /// <summary>Bekannte Server des Profils.</summary>
    [JsonPropertyName("knownServers")] public List<DevApiKnownServer> KnownServers { get; set; } = [];

    /// <summary>Per Suche gefundene Server.</summary>
    [JsonPropertyName("discoveredServers")] public List<DevApiDiscoveredServer> DiscoveredServers { get; set; } = [];

    /// <summary>Server-Suche läuft.</summary>
    [JsonPropertyName("isDiscoveryRunning")] public bool IsDiscoveryRunning { get; set; }

    /// <summary>Server-Suche wurde mindestens einmal ausgeführt.</summary>
    [JsonPropertyName("discoveryAttempted")] public bool DiscoveryAttempted { get; set; }

    /// <summary>Erreichbarkeitsprüfung läuft.</summary>
    [JsonPropertyName("isServerCheckRunning")] public bool IsServerCheckRunning { get; set; }

    /// <summary>Fehlertext der letzten Server-Einrichtung.</summary>
    [JsonPropertyName("serverSetupError")] public string? ServerSetupError { get; set; }

    /// <summary>Geräte-Pairing wird eingefordert.</summary>
    [JsonPropertyName("isPairingRequired")] public bool IsPairingRequired { get; set; }

    /// <summary>Pairing-Austausch läuft.</summary>
    [JsonPropertyName("isPairingRunning")] public bool IsPairingRunning { get; set; }

    /// <summary>Fehlertext des letzten Pairings.</summary>
    [JsonPropertyName("pairingError")] public string? PairingError { get; set; }

    /// <summary>Hinweistext der Ablage.</summary>
    [JsonPropertyName("storageNotice")] public string? StorageNotice { get; set; }

    /// <summary>Titel des Hero-Bereichs der Startseite.</summary>
    [JsonPropertyName("heroTitle")] public string? HeroTitle { get; set; }

    /// <summary>Anzahl der Fortsetzen-Einträge.</summary>
    [JsonPropertyName("continueWatchingCount")] public int ContinueWatchingCount { get; set; }

    /// <summary>Anzahl der Favoriten.</summary>
    [JsonPropertyName("favoritesCount")] public int FavoritesCount { get; set; }

    /// <summary>Anzahl der zuletzt gesehenen Einträge.</summary>
    [JsonPropertyName("recentEntriesCount")] public int RecentEntriesCount { get; set; }
}

/// <summary>DTO eines bekannten Servers im Modell-Snapshot.</summary>
public sealed class DevApiKnownServer
{
    /// <summary>Kanonische Serveradresse.</summary>
    [JsonPropertyName("address")] public string? Address { get; set; }

    /// <summary>Anzeigename des Servers.</summary>
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }

    /// <summary>Name oder Adresse als Anzeigefallback.</summary>
    [JsonPropertyName("nameOrAddress")] public string? NameOrAddress { get; set; }

    /// <summary>Zeitpunkt der letzten Verwendung (UTC).</summary>
    [JsonPropertyName("lastUsedAtUtc")] public DateTime LastUsedAtUtc { get; set; }
}

/// <summary>DTO eines gefundenen Servers im Modell-Snapshot.</summary>
public sealed class DevApiDiscoveredServer
{
    /// <summary>Kanonische Serveradresse.</summary>
    [JsonPropertyName("address")] public string? Address { get; set; }

    /// <summary>Anzeigename des Servers.</summary>
    [JsonPropertyName("name")] public string? Name { get; set; }

    /// <summary>Name oder Adresse als Anzeigefallback.</summary>
    [JsonPropertyName("nameOrAddress")] public string? NameOrAddress { get; set; }

    /// <summary>Fundquelle (z. B. mDNS).</summary>
    [JsonPropertyName("source")] public string? Source { get; set; }
}

/// <summary>DTO eines Knotens des <c>GET /tree</c>-Elementbaums.</summary>
public sealed class DevApiTreeNode
{
    /// <summary>AutomationId des Elements (falls gesetzt).</summary>
    [JsonPropertyName("automationId")] public string? AutomationId { get; set; }

    /// <summary>Typname des Elements.</summary>
    [JsonPropertyName("type")] public string? Type { get; set; }

    /// <summary>Element ist sichtbar.</summary>
    [JsonPropertyName("isVisible")] public bool IsVisible { get; set; }

    /// <summary>Textinhalt des Elements.</summary>
    [JsonPropertyName("text")] public string? Text { get; set; }

    /// <summary>Platzhaltertext des Elements.</summary>
    [JsonPropertyName("placeholder")] public string? Placeholder { get; set; }

    /// <summary>Kindknoten.</summary>
    [JsonPropertyName("children")] public List<DevApiTreeNode> Children { get; set; } = [];

    /// <summary>Abgeleitete CollectionView-Einträge (nur bei Listen).</summary>
    [JsonPropertyName("items")] public List<DevApiTreeItem>? Items { get; set; }

    /// <summary>Sucht einen Knoten mit der angegebenen AutomationId im Teilbaum.</summary>
    /// <param name="automationId">Die gesuchte AutomationId.</param>
    /// <returns>Der Knoten oder <c>null</c>.</returns>
    public DevApiTreeNode? FindByAutomationId(string automationId)
    {
        if (AutomationId == automationId)
        {
            return this;
        }

        foreach (var child in Children)
        {
            var found = child.FindByAutomationId(automationId);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}

/// <summary>DTO eines abgeleiteten CollectionView-Eintrags.</summary>
public sealed class DevApiTreeItem
{
    /// <summary>Index im ItemsSource der CollectionView.</summary>
    [JsonPropertyName("index")] public int Index { get; set; }

    /// <summary>Typname des Eintrags.</summary>
    [JsonPropertyName("type")] public string? Type { get; set; }

    /// <summary>Haupttext des Eintrags.</summary>
    [JsonPropertyName("text")] public string? Text { get; set; }

    /// <summary>Detailtext des Eintrags (z. B. Serveradresse).</summary>
    [JsonPropertyName("detail")] public string? Detail { get; set; }
}
