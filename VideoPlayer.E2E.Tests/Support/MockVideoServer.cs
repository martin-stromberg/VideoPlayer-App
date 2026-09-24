using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace VideoPlayer.E2E.Tests.Support;

/// <summary>
/// Hermetischer Mini-Nachbau der App-seitig genutzten Server-Endpunkte
/// (<c>api/health</c>, <c>api/pairing/exchange</c>, <c>api/auth/login</c>
/// sowie die nach der Anmeldung abgefragten Listen). Bindet auf
/// <c>127.0.0.1</c> mit systemvergebenem Port; jede Verbindung wird
/// einzeln beantwortet (<c>Connection: close</c>). Unbekannte Routen
/// liefern 404 — niemals 5xx, damit die Client-Wiederholungsstrategie
/// nicht anspringt.
/// </summary>
public sealed class MockVideoServer : IDisposable
{
    private readonly TcpListener listener;
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task acceptLoop;

    /// <summary>Erstellt und startet den Mock-Server.</summary>
    /// <param name="pairingCode">Der gültige Einmal-Pairing-Code.</param>
    /// <param name="deviceToken">Das auszustellende Geräte-Token (Gate-Schlüssel).</param>
    /// <param name="username">Gültige Anmelde-E-Mail.</param>
    /// <param name="password">Gültiges Anmelde-Passwort.</param>
    /// <param name="bootstrapTicket">Das gültige Bootstrap-Ticket (QR-Code); <c>null</c> = Standardwert.</param>
    /// <param name="bootstrapShortCode">Der gültige Kurzcode-Alias des Tickets; <c>null</c> = Standardwert.</param>
    public MockVideoServer(
        string pairingCode,
        string deviceToken,
        string username,
        string password,
        string? bootstrapTicket = null,
        string? bootstrapShortCode = null)
    {
        PairingCode = pairingCode;
        DeviceToken = deviceToken;
        Username = username;
        Password = password;
        BootstrapTicket = bootstrapTicket ?? $"e2e-ticket-{Guid.NewGuid():N}";
        BootstrapShortCode = bootstrapShortCode ?? "E2ETICK1";
        AuthToken = $"e2e-auth-{Guid.NewGuid():N}";
        RefreshToken = $"e2e-refresh-{Guid.NewGuid():N}";

        listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        BaseAddress = $"http://127.0.0.1:{Port}/";
        acceptLoop = Task.Run(AcceptLoopAsync);
    }

    /// <summary>Systemvergebener Port des Servers.</summary>
    public int Port { get; }

    /// <summary>Basisadresse in der kanonischen Form <c>http://127.0.0.1:port/</c>.</summary>
    public string BaseAddress { get; }

    /// <summary>Der gültige Pairing-Code.</summary>
    public string PairingCode { get; }

    /// <summary>Das Geräte-Token, das <c>exchange</c> ausstellt und das Gate akzeptiert.</summary>
    public string DeviceToken { get; }

    /// <summary>Gültige Anmelde-E-Mail.</summary>
    public string Username { get; }

    /// <summary>Gültiges Anmelde-Passwort.</summary>
    public string Password { get; }

    /// <summary>Das ausgestellte Anmelde-Token.</summary>
    public string AuthToken { get; }

    /// <summary>Das gültige Bootstrap-Ticket (langer, opaker Wert aus dem QR-Code).</summary>
    public string BootstrapTicket { get; }

    /// <summary>Der Kurzcode-Alias des Bootstrap-Tickets (8 Zeichen, Server-seitig aufgelöst).</summary>
    public string BootstrapShortCode { get; }

    /// <summary>Der aktuell gültige Refresh-Token (rotiert bei jeder Erneuerung).</summary>
    public string RefreshToken { get; private set; }

    /// <summary>Anzahl eingelöster Pairing-Requests (Diagnose).</summary>
    public int PairingExchangeCount { get; private set; }

    /// <summary>Anzahl eingelöster Bootstrap-Requests (Diagnose).</summary>
    public int BootstrapCount { get; private set; }

    /// <summary>Anzahl erfolgreicher Sitzungserneuerungen (Diagnose).</summary>
    public int RefreshCount { get; private set; }

    /// <summary>Anzahl eingegangener Logout-Aufrufe (Diagnose).</summary>
    public int LogoutCount { get; private set; }

    /// <summary>Anzahl erfolgreicher Logins (Diagnose).</summary>
    public int LoginCount { get; private set; }

    /// <summary>Body der letzten Login-Anfrage (Diagnose bei Fehlschlägen).</summary>
    public string? LastLoginRequestBody { get; private set; }

    /// <summary>Gate-Header der letzten Login-Anfrage (Diagnose bei Fehlschlägen).</summary>
    public string? LastLoginGateKey { get; private set; }

    /// <summary>Alle empfangenen Login-Bodies in Reihenfolge (Diagnose).</summary>
    public List<string> LoginRequestBodies { get; } = [];

    /// <inheritdoc/>
    public void Dispose()
    {
        cancellation.Cancel();
        listener.Stop();
        try { acceptLoop.Wait(TimeSpan.FromSeconds(5)); } catch { /* best effort */ }
        cancellation.Dispose();
    }

    private async Task AcceptLoopAsync()
    {
        while (!cancellation.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(cancellation.Token);
            }
            catch (OperationCanceledException) { break; }
            catch (SocketException) { break; }
            catch (ObjectDisposedException) { break; }

            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                var request = await ReadRequestAsync(client.GetStream(), cancellation.Token);
                if (request is null)
                {
                    return;
                }

                var (status, contentType, body) = Route(request.Value);
                await WriteResponseAsync(client.GetStream(), status, contentType, body, cancellation.Token);
            }
            catch (Exception)
            {
                // Verbindungsabbruch durch den Client o. ä. — der Mock
                // darf keinen Testlauf über eine Exception beeinflussen.
            }
        }
    }

    private (int Status, string ContentType, byte[] Body) Route((string Method, string Path, Dictionary<string, string> Headers, string Body) request)
    {
        var path = request.Path.Split('?')[0].TrimEnd('/');
        var method = request.Method;

        if (method == "GET" && path.Equals("/api/health", StringComparison.OrdinalIgnoreCase))
        {
            return (200, "text/plain", Encoding.UTF8.GetBytes("OK"));
        }

        if (method == "POST" && path.Equals("/api/pairing/exchange", StringComparison.OrdinalIgnoreCase))
        {
            return HandlePairingExchange(request.Body);
        }

        if (method == "POST" && path.Equals("/api/pairing/bootstrap", StringComparison.OrdinalIgnoreCase))
        {
            return HandleBootstrap(request.Body);
        }

        if (method == "POST" && path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            return HandleLogin(request);
        }

        if (method == "POST" && path.Equals("/api/auth/refresh", StringComparison.OrdinalIgnoreCase))
        {
            return HandleRefresh(request);
        }

        if (method == "POST" && path.Equals("/api/auth/logout", StringComparison.OrdinalIgnoreCase))
        {
            return HandleLogout(request);
        }

        // Authentifizierte Listen-Endpunkte: Gate + Bearer prüfen,
        // leere Liste liefern (ausreichend für Token-Validierung und Sync).
        // Ablehnung ohne Bearer → text/plain wie die Credential-Ablehnung
        // des echten Servers; Gate-Ablehnung → problem+json (Unterscheidung
        // der Gate-Probe, siehe DeviceTokenService.IsGateRejectedAsync).
        if (method == "GET" && path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            && IsListEndpoint(path))
        {
            if (!IsGateValid(request))
            {
                return GateRejected();
            }

            if (!IsBearerValid(request))
            {
                return (401, "text/plain", Encoding.UTF8.GetBytes("unauthorized"));
            }

            return (200, "application/json", JsonBody(Array.Empty<object>()));
        }

        return (404, "application/json", JsonBody(new { error = "not found" }));
    }

    /// <summary>
    /// Gate-Ablehnung: 401 mit <c>application/problem+json</c> — daran
    /// erkennt die App-Probe <c>IsGateRejectedAsync</c> eine
    /// Gate-Ablehnung (Credential-Fehler kommen als
    /// <c>text/plain</c>).
    /// </summary>
    private static (int, string, byte[]) GateRejected() =>
        (401, "application/problem+json", JsonBody(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            title = "Unauthorized",
            status = 401
        }));

    /// <summary>Liest eine String-Eigenschaft case-insensitiv (Pascal- oder camelCase).</summary>
    /// <param name="root">Das JSON-Wurzelelement.</param>
    /// <param name="name">Der gesuchte Eigenschaftsname.</param>
    /// <returns>Der Wert oder ein leerer String.</returns>
    private static string ReadStringProperty(JsonElement root, string name)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : string.Empty;
            }
        }

        return string.Empty;
    }

    private static bool IsListEndpoint(string path) =>
        path.Equals("/api/favorites", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/api/continue-watching", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/api/Sources", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/api/items/recent", StringComparison.OrdinalIgnoreCase);

    private bool IsGateValid((string Method, string Path, Dictionary<string, string> Headers, string Body) request)
    {
        return request.Headers.TryGetValue("X-API-Key", out var key)
            && string.Equals(key.Trim(), DeviceToken, StringComparison.Ordinal);
    }

    private bool IsBearerValid((string Method, string Path, Dictionary<string, string> Headers, string Body) request)
    {
        return request.Headers.TryGetValue("Authorization", out var auth)
            && string.Equals(auth.Trim(), $"Bearer {AuthToken}", StringComparison.Ordinal);
    }

    private (int, string, byte[]) HandlePairingExchange(string body)
    {
        PairingExchangeCount++;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var code = root.GetProperty("code").GetString() ?? string.Empty;
            var clientPublicKey = root.GetProperty("clientPublicKey").GetString() ?? string.Empty;
            if (!string.Equals(code, PairingCode, StringComparison.Ordinal))
            {
                // 401 → InvalidPairingCodeError in der App.
                return (401, "text/plain", Encoding.UTF8.GetBytes("invalid code"));
            }

            var (serverPublicKey, encryptedToken) =
                MockPairingCrypto.EncryptToken(clientPublicKey, DeviceToken);
            return (200, "application/json", JsonBody(new
            {
                serverPublicKey,
                encryptedToken
            }));
        }
        catch (Exception)
        {
            return (400, "application/json", JsonBody(new { error = "bad request" }));
        }
    }

    /// <summary>
    /// <c>api/pairing/bootstrap</c>: löst das Ticket (langer Wert oder
    /// Kurzcode-Alias) ein und liefert das verschlüsselte Session-Paket —
    /// anonymer Endpunkt wie <c>exchange</c> (kein Gate, kein Bearer).
    /// </summary>
    private (int, string, byte[]) HandleBootstrap(string body)
    {
        BootstrapCount++;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var ticket = root.GetProperty("ticket").GetString() ?? string.Empty;
            var clientPublicKey = root.GetProperty("clientPublicKey").GetString() ?? string.Empty;
            if (!string.Equals(ticket, BootstrapTicket, StringComparison.Ordinal)
                && !string.Equals(ticket, BootstrapShortCode, StringComparison.Ordinal))
            {
                // 401 → InvalidTicket in der App.
                return (401, "text/plain", Encoding.UTF8.GetBytes("invalid ticket"));
            }

            var payload = JsonSerializer.Serialize(new
            {
                deviceToken = DeviceToken,
                token = AuthToken,
                expires = DateTime.UtcNow.AddHours(12),
                refreshToken = RefreshToken
            });
            var (serverPublicKey, encryptedPayload) =
                MockPairingCrypto.EncryptPayload(clientPublicKey, payload);
            return (200, "application/json", JsonBody(new
            {
                serverPublicKey,
                encryptedPayload
            }));
        }
        catch (Exception)
        {
            return (400, "application/json", JsonBody(new { error = "bad request" }));
        }
    }

    /// <summary>
    /// <c>api/auth/refresh</c>: rotiert den Refresh-Token atomar —
    /// der alte Wert wird beim Antworten ungültig. Liegt hinter dem
    /// Client-Gate (wie der echte Server).
    /// </summary>
    private (int, string, byte[]) HandleRefresh(
        (string Method, string Path, Dictionary<string, string> Headers, string Body) request)
    {
        if (!IsGateValid(request))
        {
            return GateRejected();
        }

        try
        {
            using var doc = JsonDocument.Parse(request.Body);
            var token = ReadStringProperty(doc.RootElement, "refreshToken");
            if (!string.Equals(token, RefreshToken, StringComparison.Ordinal))
            {
                // Ablehnung als Credential-Fehler (text/plain) — die
                // App-Probe trennt sie von der Gate-Ablehnung.
                return (401, "text/plain", Encoding.UTF8.GetBytes("invalid refresh token"));
            }

            RefreshToken = $"e2e-refresh-{Guid.NewGuid():N}";
            RefreshCount++;
            return (200, "application/json", JsonBody(new
            {
                token = AuthToken,
                expires = DateTime.UtcNow.AddHours(12),
                refreshToken = RefreshToken
            }));
        }
        catch (Exception)
        {
            return (400, "application/json", JsonBody(new { error = "bad request" }));
        }
    }

    /// <summary>
    /// <c>api/auth/logout</c>: widerruft den Refresh-Token — idempotent,
    /// antwortet auch bei bereits ungültigem Token mit 200.
    /// </summary>
    private (int, string, byte[]) HandleLogout(
        (string Method, string Path, Dictionary<string, string> Headers, string Body) request)
    {
        LogoutCount++;
        try
        {
            using var doc = JsonDocument.Parse(request.Body);
            var token = ReadStringProperty(doc.RootElement, "refreshToken");
            if (string.Equals(token, RefreshToken, StringComparison.Ordinal))
            {
                RefreshToken = $"e2e-revoked-{Guid.NewGuid():N}";
            }
        }
        catch (Exception)
        {
            // Idempotent: auch ein unlesbarer Body ändert die 200 nicht.
        }

        return (200, "application/json", JsonBody(new { }));
    }

    private (int, string, byte[]) HandleLogin((string Method, string Path, Dictionary<string, string> Headers, string Body) request)
    {
        LastLoginRequestBody = request.Body;
        lock (LoginRequestBodies)
        {
            LoginRequestBodies.Add(request.Body);
        }
        LastLoginGateKey = request.Headers.TryGetValue("X-API-Key", out var key) ? key : null;

        // Client-Gate (MauiOnly-Semantik): ohne gültiges Geräte-Token
        // kommt keine Anmeldung durch — problem+json wie der
        // Action-Filter des echten Servers (Erkennungsmerkmal der
        // Gate-Probe der App).
        if (!IsGateValid(request))
        {
            return GateRejected();
        }

        try
        {
            using var doc = JsonDocument.Parse(request.Body);
            var root = doc.RootElement;
            // Der Client sendet PascalCase („Email“/“Password“); der
            // echte Server bindet case-insensitiv — der Mock auch.
            var email = ReadStringProperty(root, "email");
            var password = ReadStringProperty(root, "password");
            if (!string.Equals(email, Username, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(password, Password, StringComparison.Ordinal))
            {
                // Credential-Ablehnung: text/plain — die App unterscheidet
                // sie am Content-Type von der Gate-Ablehnung.
                return (401, "text/plain", Encoding.UTF8.GetBytes("invalid credentials"));
            }

            LoginCount++;
            return (200, "application/json", JsonBody(new
            {
                token = AuthToken,
                expires = DateTime.UtcNow.AddHours(12)
            }));
        }
        catch (Exception)
        {
            return (400, "application/json", JsonBody(new { error = "bad request" }));
        }
    }

    private static byte[] JsonBody(object payload) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

    /// <summary>
    /// Liest eine HTTP/1.1-Anfrage: Request-Line + Header bis zur
    /// Leerzeile, dann <c>Content-Length</c> Bytes Body.
    /// </summary>
    private static async Task<(string Method, string Path, Dictionary<string, string> Headers, string Body)?> ReadRequestAsync(
        NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        var chunk = new byte[8192];
        var headerEnd = -1;
        while (headerEnd < 0)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
            headerEnd = IndexOf(buffer.GetBuffer(), (int)buffer.Length, "\r\n\r\n"u8);
            if (buffer.Length > 1_000_000)
            {
                return null;
            }
        }

        var headerText = Encoding.ASCII.GetString(buffer.GetBuffer(), 0, headerEnd);
        var lines = headerText.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return null;
        }

        var parts = lines[0].Split(' ');
        if (parts.Length < 2)
        {
            return null;
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var contentLength = 0;
        foreach (var line in lines.Skip(1))
        {
            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var name = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();
            headers[name] = value;
            if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(value, out contentLength);
            }
        }

        var bodyStart = headerEnd + 4;
        var body = new MemoryStream();
        if (buffer.Length > bodyStart)
        {
            body.Write(buffer.GetBuffer(), bodyStart, (int)buffer.Length - bodyStart);
        }

        while (body.Length < contentLength)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                break;
            }

            body.Write(chunk, 0, read);
        }

        return (parts[0], parts[1], headers,
            Encoding.UTF8.GetString(body.GetBuffer(), 0, (int)Math.Min(body.Length, contentLength)));
    }

    private static int IndexOf(byte[] haystack, int length, ReadOnlySpan<byte> needle)
    {
        for (var i = 0; i + needle.Length <= length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }

    private static async Task WriteResponseAsync(
        NetworkStream stream, int status, string contentType, byte[] body, CancellationToken cancellationToken)
    {
        var reason = status switch
        {
            200 => "OK",
            400 => "Bad Request",
            401 => "Unauthorized",
            404 => "Not Found",
            429 => "Too Many Requests",
            _ => "Error"
        };
        var head = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {status} {reason}\r\nContent-Type: {contentType}\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(head, cancellationToken);
        await stream.WriteAsync(body, cancellationToken);
    }
}
