namespace VideoPlayer.Mvu;

/// <summary>
/// Orchestriert die Echtzeit-Verbindung für Server-Ereignisse
/// (SignalR-Hub <c>hubs/mediaupdate</c>): Verbindungsauf- und -abbau,
/// Bereitstellung des Bearer-Tokens aus dem angemeldeten
/// <see cref="AuthSessionService.Client"/> und Übersetzung der
/// Verbindungs-Events in <see cref="EventReceived"/>/
/// <see cref="ConnectionClosed"/> für den MVU-Zyklus.
/// </summary>
public sealed class ServerEventsService : IAsyncDisposable
{
    /// <summary>Pfad des Media-Update-Hubs relativ zur Serveradresse.</summary>
    public const string HubPath = "hubs/mediaupdate";

    private readonly AuthSessionService authSession;
    private readonly Func<IServerEventsConnection> connectionFactory;
    private readonly SemaphoreSlim gate = new(1, 1);

    private IServerEventsConnection? connection;
    private string? connectedServerAddress;

    public ServerEventsService(
        AuthSessionService authSession,
        Func<IServerEventsConnection> connectionFactory)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
        this.connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <summary>Die Ereignisverbindung ist aktuell aufgebaut.</summary>
    public bool IsConnected => connection?.IsConnected ?? false;

    /// <summary>Ein Server-Ereignis wurde empfangen (Weiterschauen, Favoriten, neue Inhalte).</summary>
    public event EventHandler<ServerEventReceivedEventArgs>? EventReceived;

    /// <summary>Die Ereignisverbindung wurde endgültig getrennt.</summary>
    public event EventHandler? ConnectionClosed;

    /// <summary>
    /// Baut die Ereignisverbindung zum Server auf (idempotent: eine
    /// bestehende Verbindung zum selben Server bleibt bestehen; bei
    /// Serverwechsel oder getrennter Verbindung wird neu verbunden).
    /// </summary>
    public async Task ConnectAsync(Uri serverAddress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serverAddress);

        await gate.WaitAsync(cancellationToken);
        try
        {
            var normalized = serverAddress.ToString();
            if (connection?.IsConnected == true
                && string.Equals(connectedServerAddress, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await DisconnectCoreAsync();

            var next = connectionFactory();
            next.ContinueWatchingUpdated += OnContinueWatchingUpdated;
            next.FavoritesChanged += OnFavoritesChanged;
            next.NewVideosScanned += OnNewVideosScanned;
            next.StatusChanged += OnStatusChanged;
            next.Closed += OnClosed;

            try
            {
                await next.ConnectAsync(
                    new Uri(serverAddress, HubPath),
                    () => Task.FromResult(authSession.Client?.CurrentToken),
                    cancellationToken);
            }
            catch
            {
                // Gescheiterte Verbindung abräumen — der nächste Versuch
                // (Netzwerkwechsel, Closed-Rückfall) baut frisch auf.
                await next.DisposeAsync();
                throw;
            }

            connection = next;
            connectedServerAddress = normalized;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Trennt die Ereignisverbindung, falls sie besteht.</summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await DisconnectCoreAsync();
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        gate.Dispose();
    }

    private async Task DisconnectCoreAsync()
    {
        var current = connection;
        connection = null;
        connectedServerAddress = null;
        if (current is null)
        {
            return;
        }

        current.ContinueWatchingUpdated -= OnContinueWatchingUpdated;
        current.FavoritesChanged -= OnFavoritesChanged;
        current.NewVideosScanned -= OnNewVideosScanned;
        current.StatusChanged -= OnStatusChanged;
        current.Closed -= OnClosed;

        try
        {
            await current.DisconnectAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ServerEventsService] Trennen fehlgeschlagen: {ex.Message}");
        }

        await current.DisposeAsync();
    }

    private void OnContinueWatchingUpdated(object? sender, EventArgs e) =>
        RaiseEventReceived(new ServerEventReceivedEventArgs(ServerEventKind.ContinueWatchingUpdated));

    private void OnFavoritesChanged(object? sender, EventArgs e) =>
        RaiseEventReceived(new ServerEventReceivedEventArgs(ServerEventKind.FavoritesChanged));

    private void OnNewVideosScanned(object? sender, NewVideosScannedEventArgs e) =>
        RaiseEventReceived(new ServerEventReceivedEventArgs(ServerEventKind.NewVideosScanned, e.SourceId, e.Count));

    private void OnStatusChanged(object? sender, string message) =>
        System.Diagnostics.Debug.WriteLine($"[ServerEventsService] StatusChanged: {message}");

    private void OnClosed(object? sender, EventArgs e)
    {
        // Die Verbindung ist weg — der MVU-Zyklus entscheidet über den
        // erneuten Aufbau (nur im verbundenen Online-Zustand). Nur die
        // aktuelle Verbindung darf den Zustand zurücksetzen — ein
        // nachgereichtes Closed einer bereits ersetzten Verbindung ist
        // belanglos.
        var closed = connection;
        if (closed is null || (sender is not null && !ReferenceEquals(sender, closed)))
        {
            return;
        }

        connection = null;
        connectedServerAddress = null;
        _ = DisposeClosedAsync(closed);

        try
        {
            ConnectionClosed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ServerEventsService] ConnectionClosed-Handler fehlgeschlagen: {ex.Message}");
        }
    }

    private static async Task DisposeClosedAsync(IServerEventsConnection closed)
    {
        try
        {
            await closed.DisposeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ServerEventsService] Dispose der getrennten Verbindung fehlgeschlagen: {ex.Message}");
        }
    }

    private void RaiseEventReceived(ServerEventReceivedEventArgs args)
    {
        try
        {
            EventReceived?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ServerEventsService] EventReceived-Handler fehlgeschlagen: {ex.Message}");
        }
    }
}
