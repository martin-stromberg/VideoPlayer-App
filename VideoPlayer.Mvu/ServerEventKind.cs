namespace VideoPlayer.Mvu;

/// <summary>Art eines vom Server empfangenen Ereignisses.</summary>
public enum ServerEventKind
{
    /// <summary>Weiterschauen-Liste wurde aktualisiert.</summary>
    ContinueWatchingUpdated,

    /// <summary>Favoriten wurden geändert.</summary>
    FavoritesChanged,

    /// <summary>Neue Inhalte wurden eingescannt.</summary>
    NewVideosScanned
}

/// <summary>Argumente eines vom Server empfangenen Ereignisses.</summary>
/// <param name="kind">Art des Ereignisses.</param>
/// <param name="sourceId">Bei <see cref="ServerEventKind.NewVideosScanned"/> die betroffene Quelle; sonst <c>null</c>.</param>
/// <param name="count">Bei <see cref="ServerEventKind.NewVideosScanned"/> die Anzahl neuer Videos; sonst <c>null</c>.</param>
/// <returns>Ereignisargumente des empfangenen Server-Ereignisses.</returns>
public sealed class ServerEventReceivedEventArgs(ServerEventKind kind, long? sourceId = null, int? count = null)
    : EventArgs
{
    /// <summary>Art des Ereignisses.</summary>
    public ServerEventKind Kind { get; } = kind;

    /// <summary>Bei <see cref="ServerEventKind.NewVideosScanned"/> die betroffene Quelle.</summary>
    public long? SourceId { get; } = sourceId;

    /// <summary>Bei <see cref="ServerEventKind.NewVideosScanned"/> die Anzahl neuer Videos.</summary>
    public int? Count { get; } = count;
}
