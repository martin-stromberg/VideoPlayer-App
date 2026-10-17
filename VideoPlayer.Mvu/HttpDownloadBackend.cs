using System.Net.Http.Headers;

namespace VideoPlayer.Mvu;

/// <summary>
/// Standard-<see cref="IDownloadBackend"/> für alle Plattformen außer iOS:
/// prozessinterner <see cref="HttpClient"/>-Stream in die Zieldatei.
/// Der Server-Stream unterstützt keine Range-Fortsetzung — abgebrochene
/// Teildateien werden vom Aufrufer verworfen.
/// </summary>
public sealed class HttpDownloadBackend : IDownloadBackend
{
    private const int CopyBufferSize = 81920;
    private readonly Func<HttpClient> httpClientFactory;

    /// <summary>Erstellt das Backend mit injizierbarer Client-Fabrik (Tests).</summary>
    /// <param name="httpClientFactory">Fabrik für den Transfer-<see cref="HttpClient"/>; <c>null</c> = produktive Erzeugung ohne Timeout.</param>
    public HttpDownloadBackend(Func<HttpClient>? httpClientFactory = null)
    {
        this.httpClientFactory = httpClientFactory ?? (() => new HttpClient { Timeout = System.Threading.Timeout.InfiniteTimeSpan });
    }

    /// <inheritdoc/>
    public async Task<DownloadBackendResult> TransferAsync(
        string url,
        string? token,
        string targetPath,
        bool wifiOnly,
        IProgress<(long Downloaded, long Total)> progress,
        CancellationToken cancellationToken)
    {
        // WLAN-Bedingung ist plattformneutral bereits in der Warteschlange
        // durchgesetzt — hier kein weiterer Gate nötig.
        _ = wifiOnly;
        using var http = httpClientFactory();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await http.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new DownloadBackendResult(0, 0, $"http_{(int)response.StatusCode}");
        }

        var total = response.Content.Headers.ContentLength ?? 0;
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using (input.ConfigureAwait(false))
        {
            var output = new FileStream(
                targetPath, FileMode.Create, FileAccess.Write, FileShare.None,
                CopyBufferSize, useAsync: true);
            await using (output.ConfigureAwait(false))
            {
                var buffer = new byte[CopyBufferSize];
                long downloaded = 0;
                int read;
                while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    downloaded += read;
                    progress.Report((downloaded, total));
                }

                return new DownloadBackendResult(downloaded, total, null);
            }
        }
    }
}
