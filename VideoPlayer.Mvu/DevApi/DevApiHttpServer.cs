#if DEBUG || DEV_API
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace VideoPlayer.Mvu.DevApi;

/// <summary>
/// Minimaler HTTP-Server der Developer-API auf Basis von
/// <see cref="TcpListener"/>: bindet ausschließlich an
/// <see cref="IPAddress.Loopback"/> auf einem systemvergebenen Port,
/// beantwortet jede Verbindung mit genau einer Antwort
/// (<c>Connection: close</c>) und verarbeitet beliebig viele
/// aufeinanderfolgende Verbindungen.
/// </summary>
public sealed class DevApiHttpServer : IDisposable
{
    private readonly DevApiRouter router;
    private TcpListener? listener;
    private CancellationTokenSource? stopTokenSource;
    private Task? acceptLoop;

    /// <summary>Erstellt den Server mit der gegebenen Routen-Tabelle.</summary>
    /// <param name="router">Der Router, der Anfragen beantwortet.</param>
    public DevApiHttpServer(DevApiRouter router)
    {
        this.router = router ?? throw new ArgumentNullException(nameof(router));
    }

    /// <summary>Der vom System vergebene Port (gültig nach <see cref="Start"/>).</summary>
    public int Port { get; private set; }

    /// <summary>Startet den Listener und die Annahme-Schleife im Hintergrund.</summary>
    public void Start()
    {
        if (listener is not null)
        {
            return;
        }

        listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        stopTokenSource = new CancellationTokenSource();
        acceptLoop = Task.Run(() => AcceptLoopAsync(stopTokenSource.Token));
    }

    /// <summary>Stoppt Listener und Annahme-Schleife.</summary>
    public void Dispose()
    {
        stopTokenSource?.Cancel();
        listener?.Stop();
        listener = null;
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        var active = listener;
        if (active is null)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await active.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using (client)
            {
                var stream = client.GetStream();
                DevApiResponse? response;
                try
                {
                    var request = await DevApiRequestReader.ReadAsync(stream, cancellationToken);
                    if (request is null)
                    {
                        return;
                    }

                    try
                    {
                        response = await router.RouteAsync(request, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        response = DevApiResponse.Error(500, $"Interner Fehler der Developer-API: {ex.Message}");
                    }
                }
                catch (DevApiRequestException ex)
                {
                    response = DevApiResponse.Error(400, $"Ungültige Anfrage: {ex.Message}");
                }

                await WriteResponseAsync(stream, response, cancellationToken);
            }
        }
        catch (Exception)
        {
            // Verbindungsabbrüche durch den Client sind normal und werden ignoriert.
        }
    }

    private static async Task WriteResponseAsync(NetworkStream stream, DevApiResponse response, CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetBytes(response.Body);
        var header = string.Concat(
            $"HTTP/1.1 {response.StatusCode} {ReasonPhrase(response.StatusCode)}\r\n",
            $"Content-Type: {response.ContentType}\r\n",
            $"Content-Length: {body.Length}\r\n",
            "Connection: close\r\n",
            "\r\n");
        await stream.WriteAsync(Encoding.ASCII.GetBytes(header), cancellationToken);
        await stream.WriteAsync(body, cancellationToken);
    }

    private static string ReasonPhrase(int statusCode) => statusCode switch
    {
        200 => "OK",
        400 => "Bad Request",
        404 => "Not Found",
        405 => "Method Not Allowed",
        500 => "Internal Server Error",
        _ => "Status"
    };
}
#endif
