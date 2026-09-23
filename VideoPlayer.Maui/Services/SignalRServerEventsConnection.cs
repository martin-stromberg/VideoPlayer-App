using Microsoft.AspNetCore.SignalR.Client;
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
    private HubConnection? connection;

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

        next.On("ContinueWatchingUpdated", () =>
            ContinueWatchingUpdated?.Invoke(this, EventArgs.Empty));
        next.On("FavoritesChanged", () =>
            FavoritesChanged?.Invoke(this, EventArgs.Empty));
        next.On<long, int>("NewVideosScanned", (sourceId, count) =>
            NewVideosScanned?.Invoke(this, new NewVideosScannedEventArgs(sourceId, count)));
        next.On<string>("StatusChanged", message =>
            StatusChanged?.Invoke(this, message));
        next.Closed += _ =>
        {
            Closed?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        };

        try
        {
            await next.StartAsync(cancellationToken);
        }
        catch
        {
            // Gescheiterte Verbindung nicht als „aktuelle" hinterlegen.
            await next.DisposeAsync();
            throw;
        }

        connection = next;
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (connection is not null)
        {
            await connection.StopAsync(cancellationToken);
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
