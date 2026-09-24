#if DEBUG || DEV_API
using System.Text;

namespace VideoPlayer.Mvu.DevApi;

/// <summary>
/// Liest eine einzelne HTTP/1.x-Anfrage von einem Stream: Request-Line,
/// Header-Block bis <c>\r\n\r\n</c> und optionaler Body gemäß
/// <c>Content-Length</c>. Keep-Alive wird nicht unterstützt – der Server
/// beantwortet jede Verbindung mit genau einer Antwort.
/// </summary>
public static class DevApiRequestReader
{
    /// <summary>Obergrenze des Header-Blocks in Bytes.</summary>
    public const int MaxHeaderBytes = 64 * 1024;

    /// <summary>Obergrenze des Request-Bodys in Bytes.</summary>
    public const int MaxBodyBytes = 1024 * 1024;

    private static readonly byte[] HeaderTerminator = [(byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n'];

    /// <summary>
    /// Liest eine Anfrage. Liefert <c>null</c>, wenn die Verbindung vor der
    /// ersten Zeile sauber geschlossen wurde.
    /// </summary>
    /// <param name="stream">Der zu lesende Stream (z. B. <see cref="System.Net.Sockets.NetworkStream"/>).</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die gelesene Anfrage oder <c>null</c> bei sauberem Verbindungsende.</returns>
    /// <exception cref="DevApiRequestException">Die Anfrage ist syntaktisch ungültig.</exception>
    public static async Task<DevApiRequest?> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var buffer = new List<byte>();
        var leftoverStart = await ReadHeaderBlockAsync(stream, buffer, cancellationToken);
        if (leftoverStart < 0)
        {
            return null;
        }

        var headerText = Encoding.ASCII.GetString(buffer.ToArray(), 0, leftoverStart);
        var bodyBytes = ReadBodyBytes(stream, buffer, leftoverStart + HeaderTerminator.Length, headerText, cancellationToken);
        var request = Parse(headerText, await bodyBytes);
        return request;
    }

    /// <summary>
    /// Füllt <paramref name="buffer"/> bis zum Header-Terminator.
    /// </summary>
    /// <returns>Index des Terminators im Puffer; -1 bei sauberem EOF ohne Daten.</returns>
    private static async Task<int> ReadHeaderBlockAsync(Stream stream, List<byte> buffer, CancellationToken cancellationToken)
    {
        var chunk = new byte[8192];
        while (true)
        {
            var terminator = IndexOf(buffer, HeaderTerminator);
            if (terminator >= 0)
            {
                return terminator;
            }

            var read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                // Sauberes Ende nur ohne Teildaten zulässig.
                if (buffer.Count == 0)
                {
                    return -1;
                }

                throw new DevApiRequestException("Die Verbindung wurde mitten im Header geschlossen.");
            }

            buffer.AddRange(chunk.AsSpan(0, read).ToArray());
            if (buffer.Count > MaxHeaderBytes)
            {
                throw new DevApiRequestException("Der Header-Block überschreitet die zulässige Größe.");
            }
        }
    }

    /// <summary>Liest den Body gemäß Content-Length (unter Berücksichtigung bereits gelesener Bytes).</summary>
    private static async Task<byte[]> ReadBodyBytes(
        Stream stream,
        List<byte> buffer,
        int bodyStart,
        string headerText,
        CancellationToken cancellationToken)
    {
        var headers = ParseHeaders(headerText);
        if (!headers.TryGetValue("Content-Length", out var lengthText))
        {
            return [];
        }

        if (!int.TryParse(lengthText, out var contentLength) || contentLength < 0)
        {
            throw new DevApiRequestException("Der Content-Length-Header ist ungültig.");
        }

        if (contentLength > MaxBodyBytes)
        {
            throw new DevApiRequestException("Der Request-Body überschreitet die zulässige Größe.");
        }

        var body = new List<byte>(contentLength);
        if (buffer.Count > bodyStart)
        {
            body.AddRange(buffer.GetRange(bodyStart, buffer.Count - bodyStart));
        }

        var chunk = new byte[8192];
        while (body.Count < contentLength)
        {
            var read = await stream.ReadAsync(
                chunk.AsMemory(0, Math.Min(chunk.Length, contentLength - body.Count)),
                cancellationToken);
            if (read == 0)
            {
                throw new DevApiRequestException("Die Verbindung wurde mitten im Body geschlossen.");
            }

            body.AddRange(chunk.AsSpan(0, read).ToArray());
        }

        return body.GetRange(0, contentLength).ToArray();
    }

    /// <summary>Zerlegt Header-Text und Body in ein <see cref="DevApiRequest"/>.</summary>
    private static DevApiRequest Parse(string headerText, byte[] bodyBytes)
    {
        var lines = headerText.Split("\r\n", StringSplitOptions.None);
        var requestLine = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (requestLine.Length < 3 || !requestLine[2].StartsWith("HTTP/", StringComparison.Ordinal))
        {
            throw new DevApiRequestException("Die Request-Line ist ungültig.");
        }

        var target = requestLine[1];
        var queryIndex = target.IndexOf('?', StringComparison.Ordinal);
        var path = queryIndex >= 0 ? target[..queryIndex] : target;
        if (!path.StartsWith('/'))
        {
            throw new DevApiRequestException("Der angefragte Pfad ist ungültig.");
        }

        return new DevApiRequest
        {
            Method = requestLine[0].ToUpperInvariant(),
            Path = path,
            Headers = ParseHeaders(headerText),
            Body = Encoding.UTF8.GetString(bodyBytes)
        };
    }

    /// <summary>Zerlegt die Header-Zeilen in ein Dictionary.</summary>
    private static Dictionary<string, string> ParseHeaders(string headerText)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = headerText.Split("\r\n", StringSplitOptions.None);
        foreach (var line in lines.Skip(1))
        {
            if (line.Length == 0)
            {
                continue;
            }

            var colon = line.IndexOf(':', StringComparison.Ordinal);
            if (colon <= 0)
            {
                throw new DevApiRequestException("Eine Header-Zeile ist ungültig.");
            }

            headers[line[..colon].Trim()] = line[(colon + 1)..].Trim();
        }

        return headers;
    }

    /// <summary>Sucht eine Bytefolge im Puffer (naiver Scan, kleine Puffer).</summary>
    private static int IndexOf(List<byte> buffer, byte[] sequence)
    {
        for (var i = 0; i + sequence.Length <= buffer.Count; i++)
        {
            var match = true;
            for (var j = 0; j < sequence.Length; j++)
            {
                if (buffer[i + j] != sequence[j])
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
}
#endif
