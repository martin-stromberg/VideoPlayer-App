using System.Collections.Concurrent;
using VideoWebPlayer.Maui.Models;

namespace VideoWebPlayer.Maui.Services;

/// <summary>
/// In-Memory-Warteschlange für Video-Downloads.
/// Verarbeitet Downloads nacheinander im Hintergrund und meldet Fortschritt und Abschluss über Events.
/// </summary>
public class DownloadQueue
{
    private static DownloadQueue? _instance;
    private readonly ConcurrentQueue<DownloadTask> _queue = new();
    private readonly HashSet<string> _queuedItems = new(); // Track items in queue
    private readonly object _queueLock = new();
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _isProcessing = false;

    /// <summary>
    /// Ruft die gemeinsame Singleton-Instanz der Download-Warteschlange ab.
    /// </summary>
    /// <value>Die Singleton-Instanz der <see cref="DownloadQueue"/>.</value>
    public static DownloadQueue Instance => _instance ??= new DownloadQueue();

    /// <summary>
    /// Wird ausgelöst, wenn sich der Fortschritt eines Downloads ändert.
    /// </summary>
    public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

    /// <summary>
    /// Wird ausgelöst, wenn ein Download abgeschlossen ist (erfolgreich oder mit Fehler).
    /// </summary>
    public event EventHandler<DownloadCompletedEventArgs>? DownloadCompleted;

    private DownloadQueue()
    {
        // Starte Background-Prozessing
        _ = Task.Run(() => ProcessQueueAsync(_cancellationTokenSource.Token));
    }

    /// <summary>
    /// Fügt eine Download-Aufgabe zur Warteschlange hinzu, sofern sie nicht bereits enthalten ist.
    /// </summary>
    /// <param name="task">Die einzureihende Download-Aufgabe.</param>
    public void EnqueueDownload(DownloadTask task)
    {
        var key = $"{task.VideoType}_{task.VideoId}";

        lock (_queueLock)
        {
            if (_queuedItems.Contains(key))
            {
                System.Diagnostics.Debug.WriteLine($"Download already in queue: {task.Title}");
                return;
            }

            _queue.Enqueue(task);
            _queuedItems.Add(key);
        }

        System.Diagnostics.Debug.WriteLine($"Download queued: {task.Title} (Queue size: {_queue.Count})");
    }

    /// <summary>
    /// Prüft, ob für das angegebene Video bereits ein Download in der Warteschlange ist.
    /// </summary>
    /// <param name="videoId">Die ID des Videos.</param>
    /// <param name="videoType">Der Typ des Videos (Movie oder Episode).</param>
    /// <returns><c>true</c>, wenn das Video in der Warteschlange ist; sonst <c>false</c>.</returns>
    public bool IsInQueue(long videoId, string videoType)
    {
        var key = $"{videoType}_{videoId}";
        lock (_queueLock)
        {
            return _queuedItems.Contains(key);
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_queue.TryDequeue(out var task))
                {
                    await ProcessDownloadAsync(task, cancellationToken);
                }
                else
                {
                    // Warte 1 Sekunde wenn Queue leer ist
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in download queue: {ex.Message}");
            }
        }
    }

    private async Task ProcessDownloadAsync(DownloadTask task, CancellationToken cancellationToken)
    {
        System.Diagnostics.Debug.WriteLine($"Starting download: {task.Title}");

        var key = $"{task.VideoType}_{task.VideoId}";

        try
        {
            // Download-Logik
            using var httpClient = new HttpClient();
            var settings = App.ServiceProvider?.GetService<ISettingsService>() ?? new SettingsService();
            var token = settings.GetAuthToken();
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            httpClient.Timeout = TimeSpan.FromMinutes(30);

            using var response = await httpClient.GetAsync(task.StreamUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var downloadedBytes = 0L;

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(task.LocalFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                downloadedBytes += bytesRead;

                // Progress Report
                if (totalBytes > 0)
                {
                    var progress = (double)downloadedBytes / totalBytes * 100;
                    DownloadProgress?.Invoke(this, new DownloadProgressEventArgs
                    {
                        VideoId = task.VideoId,
                        VideoType = task.VideoType,
                        ProgressPercent = progress,
                        DownloadedBytes = downloadedBytes,
                        TotalBytes = totalBytes
                    });
                }
            }

            System.Diagnostics.Debug.WriteLine($"Download completed: {task.Title}");

            // JETZT erst in Datenbank speichern (nach erfolgreichem Download)
            await DownloadManager.Instance.SaveCompletedDownloadAsync(task, downloadedBytes);

            // Fire Completed Event
            DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs
            {
                VideoId = task.VideoId,
                VideoType = task.VideoType,
                LocalFilePath = task.LocalFilePath,
                Success = true
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Download failed: {task.Title} - {ex.Message}");

            // Delete incomplete file
            try
            {
                if (File.Exists(task.LocalFilePath))
                {
                    File.Delete(task.LocalFilePath);
                }
            }
            catch { }

            // Fire Completed Event (mit Fehler)
            DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs
            {
                VideoId = task.VideoId,
                VideoType = task.VideoType,
                LocalFilePath = task.LocalFilePath,
                Success = false,
                ErrorMessage = ex.Message
            });
        }
        finally
        {
            // Entferne aus Queue-Tracking
            lock (_queueLock)
            {
                _queuedItems.Remove(key);
            }
        }
    }
}

/// <summary>
/// Beschreibt eine einzelne Download-Aufgabe in der <see cref="DownloadQueue"/>.
/// </summary>
public class DownloadTask
{
    /// <summary>
    /// Ruft die ID des Videos ab oder legt sie fest.
    /// </summary>
    /// <value>Die ID des Videos.</value>
    public long VideoId { get; set; }

    /// <summary>
    /// Ruft den Typ des Videos ab oder legt ihn fest.
    /// </summary>
    /// <value>Der Video-Typ (Movie oder Episode).</value>
    public string VideoType { get; set; } = string.Empty;

    /// <summary>
    /// Ruft den Titel des Videos ab oder legt ihn fest.
    /// </summary>
    /// <value>Der Anzeigetitel des Videos.</value>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Ruft die Stream-URL ab, von der heruntergeladen wird, oder legt sie fest.
    /// </summary>
    /// <value>Die URL des Video-Streams.</value>
    public string StreamUrl { get; set; } = string.Empty;

    /// <summary>
    /// Ruft den lokalen Zielpfad der Download-Datei ab oder legt ihn fest.
    /// </summary>
    /// <value>Der lokale Dateipfad für den Download.</value>
    public string LocalFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Ruft die Aufbewahrungsart des Downloads ab oder legt sie fest.
    /// </summary>
    /// <value>Die <see cref="DownloadRetentionType"/>-Aufbewahrungsart.</value>
    public DownloadRetentionType RetentionType { get; set; }

    /// <summary>
    /// Ruft den Zeitpunkt ab, an dem der Download abläuft, oder legt ihn fest.
    /// </summary>
    /// <value>Das Ablaufdatum des Downloads.</value>
    public DateTime ExpiresAt { get; set; }

    // Video-Metadaten

    /// <summary>
    /// Ruft die Handlungsbeschreibung des Videos ab oder legt sie fest.
    /// </summary>
    /// <value>Die Beschreibung des Inhalts oder <c>null</c>.</value>
    public string? Plot { get; set; }

    /// <summary>
    /// Ruft die Genrenamen des Videos ab oder legt sie fest.
    /// </summary>
    /// <value>Kommaseparierte Genrenamen oder <c>null</c>.</value>
    public string? GenreNames { get; set; }

    /// <summary>
    /// Ruft das Erscheinungsjahr des Videos ab oder legt es fest.
    /// </summary>
    /// <value>Das Erscheinungsjahr oder <c>null</c>.</value>
    public string? ReleaseYear { get; set; }

    // Episode-Informationen

    /// <summary>
    /// Ruft die Episodennummer ab oder legt sie fest (nur bei Serienepisoden).
    /// </summary>
    /// <value>Die Episodennummer oder <c>null</c>.</value>
    public int? EpisodeNumber { get; set; }

    /// <summary>
    /// Ruft die Staffelnummer ab oder legt sie fest (nur bei Serienepisoden).
    /// </summary>
    /// <value>Die Staffelnummer oder <c>null</c>.</value>
    public int? SeasonNumber { get; set; }

    /// <summary>
    /// Ruft den Namen der Serie ab oder legt ihn fest (nur bei Serienepisoden).
    /// </summary>
    /// <value>Der Serienname oder <c>null</c>.</value>
    public string? TVShowName { get; set; }

    // Bild-URLs (Server)

    /// <summary>
    /// Ruft die Server-URL des Poster-Bildes ab oder legt sie fest.
    /// </summary>
    /// <value>Die URL des Posters oder <c>null</c>.</value>
    public string? PosterImageUrl { get; set; }

    /// <summary>
    /// Ruft die Server-URL des Banner-Bildes ab oder legt sie fest.
    /// </summary>
    /// <value>Die URL des Banners oder <c>null</c>.</value>
    public string? BannerImageUrl { get; set; }
}

/// <summary>
/// Event-Argumente für den Fortschritt eines Downloads.
/// </summary>
public class DownloadProgressEventArgs : EventArgs
{
    /// <summary>
    /// Ruft die ID des Videos ab oder legt sie fest.
    /// </summary>
    /// <value>Die ID des Videos.</value>
    public long VideoId { get; set; }

    /// <summary>
    /// Ruft den Typ des Videos ab oder legt ihn fest.
    /// </summary>
    /// <value>Der Video-Typ (Movie oder Episode).</value>
    public string VideoType { get; set; } = string.Empty;

    /// <summary>
    /// Ruft den Download-Fortschritt in Prozent ab oder legt ihn fest.
    /// </summary>
    /// <value>Der Fortschritt in Prozent (0–100).</value>
    public double ProgressPercent { get; set; }

    /// <summary>
    /// Ruft die Anzahl bereits heruntergeladener Bytes ab oder legt sie fest.
    /// </summary>
    /// <value>Die heruntergeladene Datenmenge in Bytes.</value>
    public long DownloadedBytes { get; set; }

    /// <summary>
    /// Ruft die Gesamtgröße des Downloads in Bytes ab oder legt sie fest.
    /// </summary>
    /// <value>Die Gesamtgröße in Bytes.</value>
    public long TotalBytes { get; set; }
}

/// <summary>
/// Event-Argumente für den Abschluss eines Downloads.
/// </summary>
public class DownloadCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Ruft die ID des Videos ab oder legt sie fest.
    /// </summary>
    /// <value>Die ID des Videos.</value>
    public long VideoId { get; set; }

    /// <summary>
    /// Ruft den Typ des Videos ab oder legt ihn fest.
    /// </summary>
    /// <value>Der Video-Typ (Movie oder Episode).</value>
    public string VideoType { get; set; } = string.Empty;

    /// <summary>
    /// Ruft den lokalen Pfad der heruntergeladenen Datei ab oder legt ihn fest.
    /// </summary>
    /// <value>Der lokale Dateipfad des Downloads.</value>
    public string LocalFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Ruft ab, ob der Download erfolgreich war, oder legt dies fest.
    /// </summary>
    /// <value><c>true</c> bei erfolgreichem Download; sonst <c>false</c>.</value>
    public bool Success { get; set; }

    /// <summary>
    /// Ruft die Fehlermeldung bei einem fehlgeschlagenen Download ab oder legt sie fest.
    /// </summary>
    /// <value>Die Fehlermeldung oder <c>null</c>, wenn der Download erfolgreich war.</value>
    public string? ErrorMessage { get; set; }
}
