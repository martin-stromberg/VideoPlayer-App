namespace VideoPlayer.Mvu;

/// <summary>
/// Abstraktion der Echtzeit-Verbindung für Server-Ereignisse
/// (Implementierung am App-Rand über SignalR). Kurze Unterbrechungen
/// überbrückt die Implementierung selbst (<c>WithAutomaticReconnect</c>);
/// <see cref="Closed"/> meldet nur die endgültige Trennung.
/// </summary>
public interface IServerEventsConnection : IAsyncDisposable
{
    /// <summary>Die Verbindung ist aktuell aufgebaut.</summary>
    bool IsConnected { get; }

    /// <summary>
    /// Baut die Verbindung zum Hub auf. <paramref name="accessTokenProvider"/>
    /// liefert das jeweils aktuelle Bearer-Token (wird vom Transport bei
    /// jeder (Neu-)Verbindung abgefragt).
    /// </summary>
    Task ConnectAsync(
        Uri hubAddress,
        Func<Task<string?>> accessTokenProvider,
        CancellationToken cancellationToken = default);

    /// <summary>Trennt die Verbindung, falls sie besteht.</summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Server-Ereignis: Weiterschauen-Liste wurde aktualisiert.</summary>
    event EventHandler? ContinueWatchingUpdated;

    /// <summary>Server-Ereignis: Favoriten wurden geändert.</summary>
    event EventHandler? FavoritesChanged;

    /// <summary>Server-Ereignis: neue Inhalte wurden eingescannt.</summary>
    event EventHandler<NewVideosScannedEventArgs>? NewVideosScanned;

    /// <summary>Server-Ereignis: allgemeine Statusmeldung des Servers.</summary>
    event EventHandler<string>? StatusChanged;

    /// <summary>Die Verbindung wurde endgültig getrennt (kein Auto-Reconnect mehr).</summary>
    event EventHandler? Closed;
}

/// <summary>Argumente des Server-Ereignisses „neue Inhalte eingescannt".</summary>
public sealed class NewVideosScannedEventArgs(long sourceId, int count) : EventArgs
{
    /// <summary>Id der betroffenen Medienquelle.</summary>
    public long SourceId { get; } = sourceId;

    /// <summary>Anzahl neu gefundener Videos.</summary>
    public int Count { get; } = count;
}
