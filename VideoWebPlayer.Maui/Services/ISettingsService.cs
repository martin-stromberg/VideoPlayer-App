namespace VideoWebPlayer.Maui.Services;

/// <summary>
/// Dienst zum Lesen und Schreiben der App-Einstellungen, Zugangsdaten und Aufbewahrungsfristen.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Ruft die konfigurierte Serveradresse ab.
    /// </summary>
    /// <value>Die Serveradresse oder <c>null</c>, wenn keine gesetzt ist.</value>
    string? ServerAddress { get; }

    /// <summary>
    /// Prüft, ob eine Serveradresse hinterlegt ist.
    /// </summary>
    /// <returns><c>true</c>, wenn eine Serveradresse gesetzt ist; sonst <c>false</c>.</returns>
    bool HasServerAddress();

    /// <summary>
    /// Speichert die Serveradresse.
    /// </summary>
    /// <param name="address">Die zu speichernde Serveradresse.</param>
    void SetServerAddress(string address);

    /// <summary>
    /// Entfernt die gespeicherte Serveradresse.
    /// </summary>
    void ClearServerAddress();

    /// <summary>
    /// Ruft den gespeicherten Benutzernamen ab.
    /// </summary>
    /// <returns>Der gespeicherte Benutzername oder ein leerer String.</returns>
    string GetUsername();

    /// <summary>
    /// Ruft das gespeicherte Passwort ab.
    /// </summary>
    /// <returns>Das gespeicherte Passwort oder ein leerer String.</returns>
    string GetPassword();

    /// <summary>
    /// Prüft, ob Zugangsdaten gespeichert sind.
    /// </summary>
    /// <returns><c>true</c>, wenn Benutzername und Passwort vorhanden sind; sonst <c>false</c>.</returns>
    bool HasCredentials();

    /// <summary>
    /// Speichert die Zugangsdaten des Benutzers.
    /// </summary>
    /// <param name="username">Der Benutzername.</param>
    /// <param name="password">Das Passwort.</param>
    void SetCredentials(string username, string password);

    /// <summary>
    /// Löscht die gespeicherten Zugangsdaten.
    /// </summary>
    void ClearCredentials();

    /// <summary>
    /// Ruft das gespeicherte Authentifizierungstoken ab.
    /// </summary>
    /// <returns>Das gespeicherte Token oder <c>null</c>.</returns>
    string? GetAuthToken();

    /// <summary>
    /// Speichert das Authentifizierungstoken.
    /// </summary>
    /// <param name="token">Das zu speichernde Token.</param>
    void SetAuthToken(string token);

    /// <summary>
    /// Löscht das gespeicherte Authentifizierungstoken.
    /// </summary>
    void ClearAuthToken();

    /// <summary>
    /// Ruft die Aufbewahrungsdauer des Wiedergabe-Caches in Tagen ab oder legt sie fest.
    /// </summary>
    /// <value>Anzahl der Tage, die Wiedergabe-Cache-Downloads aufbewahrt werden.</value>
    int PlaybackCacheRetentionDays { get; set; }

    /// <summary>
    /// Ruft die Aufbewahrungsdauer von Watchlist-Downloads in Tagen ab oder legt sie fest.
    /// </summary>
    /// <value>Anzahl der Tage, die Watchlist-Downloads aufbewahrt werden.</value>
    int WatchlistCacheRetentionDays { get; set; }

    /// <summary>
    /// Ruft die Aufbewahrungsdauer manueller Downloads in Tagen ab oder legt sie fest.
    /// </summary>
    /// <value>Anzahl der Tage, die Downloads aufbewahrt werden.</value>
    int DownloadRetentionDays { get; set; }

    // Discovery methods

    /// <summary>
    /// Sucht per mDNS/Zeroconf nach verfügbaren VideoWebPlayer-Servern im lokalen Netzwerk.
    /// </summary>
    /// <param name="timeoutMs">Maximale Wartezeit für die Suche in Millisekunden.</param>
    /// <returns>Liste der gefundenen Serveradressen.</returns>
    Task<IReadOnlyList<string>> DiscoverServersAsync(int timeoutMs = 3000);

    /// <summary>
    /// Sucht per UDP-Broadcast nach verfügbaren VideoWebPlayer-Servern im lokalen Netzwerk (Fallback).
    /// </summary>
    /// <param name="port">Der UDP-Port für die Discovery-Anfrage.</param>
    /// <param name="timeoutMs">Maximale Wartezeit für Antworten in Millisekunden.</param>
    /// <returns>Liste der gefundenen Serveradressen.</returns>
    Task<IReadOnlyList<string>> DiscoverServersUdpAsync(int port = 5001, int timeoutMs = 2000);
}
