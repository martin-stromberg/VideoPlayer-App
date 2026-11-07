#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using Microsoft.Extensions.Logging;
using VideoPlayer.Maui.Tests.Stubs;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Orchestrierung der Ereignisverbindung (Schritt 6): Hub-Adresse und
/// Token-Provider, idempotenter Connect, Übersetzung der Ereignisse in
/// <see cref="ServerEventsService.EventReceived"/> und Meldung der
/// endgültigen Trennung über <see cref="ServerEventsService.ConnectionClosed"/>.
/// </summary>
public class ServerEventsServiceTests
{
    private static readonly Uri Server = new("http://testserver/videoplayer/");

    private static AuthSessionService CreateAuthSession(StubHttpMessageHandler handler)
    {
        var secrets = new AppSecretsService(
            new InMemorySettingsStore(), new InMemorySecretStore(), AppProfile.Create(null));
        return new AuthSessionService(
            secrets,
            uri => new HttpClient(handler, disposeHandler: false) { BaseAddress = uri },
            new RecordingDelays().Delay);
    }

    private static async Task LoginAsync(AuthSessionService authSession, StubHttpMessageHandler handler)
    {
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "jwt", expires = "2030-01-01T00:00:00Z" });
        await authSession.LoginAsync(Server, "martin", "pw");
    }

    /// <summary>Prüft ConnectAsync: verwendet Hubadresse und Bearertoken.</summary>
    [Fact]
    public async Task ConnectAsync_UsesHubAddressAndBearerToken()
    {
        var handler = new StubHttpMessageHandler();
        var authSession = CreateAuthSession(handler);
        await LoginAsync(authSession, handler);
        var connection = new FakeServerEventsConnection();
        var service = new ServerEventsService(authSession, () => connection);

        await service.ConnectAsync(Server);

        Assert.True(service.IsConnected);
        Assert.Equal(new Uri(Server, "hubs/mediaupdate"), connection.LastHubAddress);
        Assert.NotNull(connection.LastTokenProvider);
        Assert.Equal("jwt", await connection.LastTokenProvider!());
    }

    /// <summary>Prüft ConnectAsync: bei gleichem Server → ist idempotent.</summary>
    [Fact]
    public async Task ConnectAsync_SameServer_IsIdempotent()
    {
        var authSession = CreateAuthSession(new StubHttpMessageHandler());
        var connection = new FakeServerEventsConnection();
        var service = new ServerEventsService(authSession, () => connection);

        await service.ConnectAsync(Server);
        await service.ConnectAsync(Server);

        Assert.Equal(1, connection.ConnectCalls);
        Assert.Equal(0, connection.DisconnectCalls);
    }

    /// <summary>Prüft ConnectAsync: bei anderem Server → verbindet erneut.</summary>
    [Fact]
    public async Task ConnectAsync_OtherServer_Reconnects()
    {
        var authSession = CreateAuthSession(new StubHttpMessageHandler());
        var connection = new FakeServerEventsConnection();
        var service = new ServerEventsService(authSession, () => connection);

        await service.ConnectAsync(Server);
        await service.ConnectAsync(new Uri("http://anderer/videoplayer/"));

        Assert.Equal(2, connection.ConnectCalls);
        Assert.Equal(1, connection.DisconnectCalls);
    }

    /// <summary>Prüft EventReceived: übersetzt die Connection-Events.</summary>
    [Fact]
    public async Task EventReceived_TranslatesConnectionEvents()
    {
        var authSession = CreateAuthSession(new StubHttpMessageHandler());
        var connection = new FakeServerEventsConnection();
        var service = new ServerEventsService(authSession, () => connection);
        var received = new List<ServerEventReceivedEventArgs>();
        service.EventReceived += (_, e) => received.Add(e);

        await service.ConnectAsync(Server);
        connection.RaiseContinueWatching();
        connection.RaiseFavorites();
        connection.RaiseScanned(sourceId: 3, count: 12);

        Assert.Equal(
            [ServerEventKind.ContinueWatchingUpdated, ServerEventKind.FavoritesChanged, ServerEventKind.NewVideosScanned],
            received.Select(e => e.Kind).ToArray());
        Assert.Equal(3, received[2].SourceId);
        Assert.Equal(12, received[2].Count);
    }

    /// <summary>Prüft Closed: löst das Closed-Ereignis aus und erlaubt einen Reconnect.</summary>
    [Fact]
    public async Task Closed_RaisesConnectionClosedAndAllowsReconnect()
    {
        var authSession = CreateAuthSession(new StubHttpMessageHandler());
        var connection = new FakeServerEventsConnection();
        var service = new ServerEventsService(authSession, () => connection);
        var closed = 0;
        service.ConnectionClosed += (_, _) => closed++;

        await service.ConnectAsync(Server);
        connection.RaiseClosed();

        Assert.Equal(1, closed);
        Assert.False(service.IsConnected);

        // Nach der Trennung baut der nächste Connect-Aufruf neu auf.
        await service.ConnectAsync(Server);
        Assert.Equal(2, connection.ConnectCalls);
        Assert.True(service.IsConnected);
    }

    /// <summary>Prüft DisconnectAsync: stoppt und gibt die Connection frei.</summary>
    [Fact]
    public async Task DisconnectAsync_StopsAndDisposesConnection()
    {
        var authSession = CreateAuthSession(new StubHttpMessageHandler());
        var connection = new FakeServerEventsConnection();
        var service = new ServerEventsService(authSession, () => connection);

        await service.ConnectAsync(Server);
        await service.DisconnectAsync();

        Assert.False(service.IsConnected);
        Assert.Equal(1, connection.DisconnectCalls);
        Assert.Equal(1, connection.DisposeCalls);

        // Trennen ohne Verbindung ist ein No-Op.
        await service.DisconnectAsync();
        Assert.Equal(1, connection.DisconnectCalls);
    }

    /// <summary>Prüft die Protokolleinträge: Verbindungsauf-/-abbau, StatusChanged und ConnectionClosed landen als Information.</summary>
    [Fact]
    public async Task ConnectAsync_DisconnectAsync_StatusChanged_Logged()
    {
        var logging = new RecordingLoggerProvider();
        var authSession = CreateAuthSession(new StubHttpMessageHandler());
        var connection = new FakeServerEventsConnection();
        var service = new ServerEventsService(
            authSession, () => connection, logging.CreateLogger<ServerEventsService>());

        await service.ConnectAsync(Server);
        Assert.Contains(logging.Entries, e =>
            e.Level == LogLevel.Information
            && e.Category.Contains(nameof(ServerEventsService), StringComparison.Ordinal)
            && e.Message.Contains("aufgebaut", StringComparison.Ordinal));

        connection.RaiseStatus("Server bereit");
        Assert.Contains(logging.Entries, e =>
            e.Level == LogLevel.Information
            && e.Category.Contains(nameof(ServerEventsService), StringComparison.Ordinal)
            && e.Message.Contains("Server bereit", StringComparison.Ordinal));

        // Closed muss an der aktiven Verbindung eintreffen — ein
        // nachgereichtes Ereignis einer ersetzten Verbindung ist belanglos.
        connection.RaiseClosed();
        Assert.Contains(logging.Entries, e =>
            e.Level == LogLevel.Information
            && e.Category.Contains(nameof(ServerEventsService), StringComparison.Ordinal)
            && e.Message.Contains("geschlossen", StringComparison.Ordinal));

        await service.ConnectAsync(Server);
        await service.DisconnectAsync();
        Assert.Contains(logging.Entries, e =>
            e.Level == LogLevel.Information
            && e.Category.Contains(nameof(ServerEventsService), StringComparison.Ordinal)
            && e.Message.Contains("getrennt", StringComparison.Ordinal));
    }

    private sealed class FakeServerEventsConnection : IServerEventsConnection
    {
        public bool IsConnected { get; private set; }
        public int ConnectCalls { get; private set; }
        public int DisconnectCalls { get; private set; }
        public int DisposeCalls { get; private set; }
        public Uri? LastHubAddress { get; private set; }
        public Func<Task<string?>>? LastTokenProvider { get; private set; }

        public event EventHandler? ContinueWatchingUpdated;
        public event EventHandler? FavoritesChanged;
        public event EventHandler<NewVideosScannedEventArgs>? NewVideosScanned;
        public event EventHandler<string>? StatusChanged;
        public event EventHandler? Closed;

        public Task ConnectAsync(
            Uri hubAddress,
            Func<Task<string?>> accessTokenProvider,
            CancellationToken cancellationToken = default)
        {
            ConnectCalls++;
            LastHubAddress = hubAddress;
            LastTokenProvider = accessTokenProvider;
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            DisconnectCalls++;
            IsConnected = false;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            DisposeCalls++;
            IsConnected = false;
            return ValueTask.CompletedTask;
        }

        public void RaiseContinueWatching() => ContinueWatchingUpdated?.Invoke(this, EventArgs.Empty);
        public void RaiseFavorites() => FavoritesChanged?.Invoke(this, EventArgs.Empty);
        public void RaiseScanned(long sourceId, int count) =>
            NewVideosScanned?.Invoke(this, new NewVideosScannedEventArgs(sourceId, count));
        public void RaiseStatus(string message) => StatusChanged?.Invoke(this, message);
        public void RaiseClosed()
        {
            IsConnected = false;
            Closed?.Invoke(this, EventArgs.Empty);
        }
    }
}
