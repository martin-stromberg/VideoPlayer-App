using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// Echtzeit-Verbindung zum SignalR-Hub <c>hubs/mediaupdate</c> des
/// VideoPlayer-Servers. Kurze Unterbrechungen überbrückt
/// <c>WithAutomaticReconnect</c>; <see cref="Closed"/> meldet die
/// endgültige Trennung an den MVU-Zyklus (erneuter Aufbau).
/// </summary>
public sealed class SignalRServerEventsConnection : IServerEventsConnection
{
    private readonly ILogger<SignalRServerEventsConnection> logger;
    private HubConnection? connection;

    /// <summary>Initialisiert eine neue Instanz der <see cref="SignalRServerEventsConnection"/>-Klasse.</summary>
    /// <param name="logger">Logger für das Programmprotokoll.</param>
    public SignalRServerEventsConnection(ILogger<SignalRServerEventsConnection> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public bool IsConnected => connection?.State == HubConnectionState.Connected;

    /// <inheritdoc/>
    public event EventHandler? ContinueWatchingUpdated;

    /// <inheritdoc/>
    public event EventHandler? FavoritesChanged;

    /// <inheritdoc/>
    public event EventHandler<NewVideosScannedEventArgs>? NewVideosScanned;

    /// <inheritdoc/>
    public event EventHandler<string>? StatusChanged;

    /// <inheritdoc/>
    public event EventHandler? Closed;

    /// <inheritdoc/>
    public async Task ConnectAsync(
        Uri hubAddress,
        Func<Task<string?>> accessTokenProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hubAddress);
        ArgumentNullException.ThrowIfNull(accessTokenProvider);

        if (IsConnected)
        {
            return;
        }

        var next = new HubConnectionBuilder()
            .WithUrl(hubAddress, options => options.AccessTokenProvider = accessTokenProvider)
            .WithAutomaticReconnect()
            .Build();

        // Hub-Ereignisse bleiben Debug: ContinueWatchingUpdated kann im
        // Takt eingehender Fortschrittsmeldungen feuern — als
        // Information würde das den Fortschrittstakt ins Protokoll
        // spiegeln (FileLogger verwirft unterhalb Information).
        next.On("ContinueWatchingUpdated", () =>
        {
            logger.LogDebug("Hub-Ereignis ContinueWatchingUpdated empfangen.");
            ContinueWatchingUpdated?.Invoke(this, EventArgs.Empty);
        });
        next.On("FavoritesChanged", () =>
        {
            logger.LogDebug("Hub-Ereignis FavoritesChanged empfangen.");
            FavoritesChanged?.Invoke(this, EventArgs.Empty);
        });
        next.On<long, int>("NewVideosScanned", (sourceId, count) =>
        {
            logger.LogDebug(
                "Hub-Ereignis NewVideosScanned empfangen: Quelle {SourceId}, {Count} neue Inhalte.",
                sourceId,
                count);
            NewVideosScanned?.Invoke(this, new NewVideosScannedEventArgs(sourceId, count));
        });
        next.On<string>("StatusChanged", message =>
            StatusChanged?.Invoke(this, message));
        next.Reconnecting += reconnectError =>
        {
            logger.LogWarning(reconnectError, "Ereignisverbindung verloren — Wiederverbindung läuft.");
            return Task.CompletedTask;
        };
        next.Reconnected += _ =>
        {
            logger.LogInformation("Ereignisverbindung wiederhergestellt.");
            return Task.CompletedTask;
        };
        next.Closed += _ =>
        {
            // Der Information-Eintrag obliegt ServerEventsService
            // (eine Schicht pro Ereignis) — hier nur Debug.
            logger.LogDebug("Ereignisverbindung endgültig geschlossen.");
            Closed?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        };

        try
        {
            await next.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Gescheiterte Verbindung nicht als „aktuelle" hinterlegen.
            logger.LogWarning(
                ex,
                "Verbindungsaufbau zum Hub {HubAddress} fehlgeschlagen.",
                hubAddress.GetLeftPart(UriPartial.Path));
            await next.DisposeAsync();
            throw;
        }

        connection = next;
        // Der Information-Eintrag obliegt ServerEventsService
        // (eine Schicht pro Ereignis) — hier nur Debug.
        logger.LogDebug(
            "Ereignisverbindung zum Hub {HubAddress} aufgebaut.",
            hubAddress.GetLeftPart(UriPartial.Path));
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (connection is not null)
        {
            await connection.StopAsync(cancellationToken);
            // Der Information-Eintrag obliegt ServerEventsService
            // (eine Schicht pro Ereignis) — hier nur Debug.
            logger.LogDebug("Ereignisverbindung getrennt.");
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
        }
    }
}
