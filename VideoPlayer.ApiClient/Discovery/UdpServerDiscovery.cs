using System.Net;
using System.Net.Sockets;
using System.Text;

namespace VideoPlayer.ApiClient.Discovery;

/// <summary>
/// UDP-Broadcast-Erkennung: sendet <see cref="DiscoveryProtocol.QueryText"/>
/// an <c>255.255.255.255:5001</c> und sammelt Antworten mit dem Präfix
/// <see cref="DiscoveryProtocol.ResponsePrefix"/> innerhalb des Zeitfensters.
/// Meldet ein entfernter Server eine Loopback-Adresse („localhost"), wird sie
/// als „der antwortende Host auf dem gemeldeten Port/Pfad" interpretiert
/// (protokollkonforme Deutung; siehe Server-Issue
/// martin-stromberg/VideoPlayer#224).
/// </summary>
public sealed class UdpServerDiscovery : IServerDiscovery
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);

    private readonly IPEndPoint target;
    private readonly TimeSpan timeout;

    /// <summary>Produktive Erkennung per Broadcast auf <see cref="DiscoveryProtocol.UdpPort"/>.</summary>
    public UdpServerDiscovery()
        : this(new IPEndPoint(IPAddress.Broadcast, DiscoveryProtocol.UdpPort), DefaultTimeout)
    {
    }

    /// <summary>Testbarer Konstruktor: Ziel-Endpunkt und Zeitfenster injizierbar.</summary>
    /// <param name="target">Ziel-Endpunkt der Broadcast-Abfrage.</param>
    /// <param name="timeout">Zeitfenster für den Empfang von Antworten.</param>
    internal UdpServerDiscovery(IPEndPoint target, TimeSpan timeout)
    {
        this.target = target;
        this.timeout = timeout;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DiscoveredServer>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var found = new List<DiscoveredServer>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var client = new UdpClient();
            client.EnableBroadcast = true;
            var query = Encoding.UTF8.GetBytes(DiscoveryProtocol.QueryText);
            await client.SendAsync(query, target, cancellationToken);

            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
            {
                var remaining = deadline - DateTime.UtcNow;
                var receive = client.ReceiveAsync(cancellationToken).AsTask();
                var completed = await Task.WhenAny(receive, Task.Delay(remaining, cancellationToken));
                if (completed != receive)
                {
                    break;
                }

                UdpReceiveResult result;
                try
                {
                    result = await receive;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException)
                {
                    continue;
                }

                var text = Encoding.UTF8.GetString(result.Buffer);
                if (!DiscoveryProtocol.TryParseResponse(text, out var announced))
                {
                    continue;
                }

                var address = ResolveAnnouncedAddress(announced, result.RemoteEndPoint.Address);
                if (address is not null && seen.Add(address))
                {
                    found.Add(new DiscoveredServer { Address = address, Source = DiscoverySource.Udp });
                }
            }
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException or ObjectDisposedException)
        {
            // Discovery ist ein Bonus-Weg — Fehler führen zu einer leeren Liste.
            System.Diagnostics.Debug.WriteLine($"[UdpServerDiscovery] {ex.Message}");
        }

        return found;
    }

    /// <summary>
    /// Normalisiert die gemeldete Adresse. Antwortet ein entfernter Host mit
    /// einer Loopback-Adresse („localhost"), wird der Hostteil durch die
    /// Responder-Adresse ersetzt — der Server meint mit „localhost" sich
    /// selbst.
    /// </summary>
    /// <param name="announced">Vom Server gemeldete Adresse (ggf. ohne Schema).</param>
    /// <param name="responder">IP-Adresse des antwortenden Hosts.</param>
    /// <returns>Normalisierte Adresse (<c>scheme://host[:port][/pfad]/</c>) oder <c>null</c> bei ungültiger Eingabe.</returns>
    internal static string? ResolveAnnouncedAddress(string announced, IPAddress responder)
    {
        var candidate = announced;
        if (!candidate.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !candidate.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            candidate = $"http://{candidate}";
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
        {
            return null;
        }

        if (IsLoopbackHost(uri.Host) && !IPAddress.IsLoopback(responder))
        {
            var builder = new UriBuilder(uri) { Host = responder.ToString() };
            uri = builder.Uri;
        }

        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/') + "/";
    }

    private static bool IsLoopbackHost(string host)
    {
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host is "127.0.0.1" or "::1" or "[::1]";
    }
}
