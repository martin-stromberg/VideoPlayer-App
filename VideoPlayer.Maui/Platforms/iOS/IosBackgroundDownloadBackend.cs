#if IOS || MACCATALYST
using System.Collections.Concurrent;
using Foundation;
using UIKit;
using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// iOS-Download-Backend: delegiert die Dateiübertragung an eine systemseitige
/// <see cref="NSUrlSession"/>-Hintergrund-Session, sodass Downloads bei
/// suspendierter App weiterlaufen. Der Download landet zunächst in einer
/// Temp-Datei und wird in <see cref="DidFinishDownload"/> an den Zielpfad
/// verschoben; Fortschritt, Abschluss und Abbruch laufen über die
/// registrierten Transfers (Schlüssel = <c>TaskDescription</c> = Medien-Key
/// <c>typ:id</c>). Nach einem System-Neustart der App werden abgeschlossene
/// Hintergrund-Transfers über <see cref="HandleEventsForBackgroundUrlSession"/>
/// und die Delegate-Rückrufe zugestellt; <see cref="CompletionSink"/> meldet
/// sie dann dem Manifest.
/// </summary>
public sealed class IosBackgroundDownloadBackend : NSObject, IDownloadBackend, INSUrlSessionDownloadDelegate
{
    /// <summary>Session-Kennung der Hintergrund-Download-Session.</summary>
    public const string SessionIdentifier = "videoplayer.downloads";

    /// <summary>Die vom DI-Container registrierte Instanz (für AppDelegate-Zugriff).</summary>
    public static IosBackgroundDownloadBackend? Shared { get; set; }

    private sealed class PendingTransfer
    {
        public required TaskCompletionSource<DownloadBackendResult> Tcs { get; init; }
        public required string TargetPath { get; init; }
        public required IProgress<(long Downloaded, long Total)> Progress { get; init; }
        public CancellationTokenRegistration Cancellation { get; set; }
        public long TotalBytes { get; set; }
        public long DownloadedBytes { get; set; }
    }

    private readonly ConcurrentDictionary<nuint, PendingTransfer> pending = new();
    private readonly ConcurrentDictionary<int, Action> systemCompletionHandlers = new();
    private NSUrlSession? session;

    /// <summary>
    /// Wird für Transfers aufgerufen, die ohne aktiven
    /// <see cref="TransferAsync"/>-Aufruf abgeschlossen wurden (App wurde vom
    /// System für Hintergrund-Events gestartet). Parameter: Medien-Key,
    /// Zielpfad liegt unbekannt vor — der Aufrufer löst ihn aus dem Manifest;
    /// zweiter Parameter = Fehlercode oder <c>null</c>, dritter = Temp-Pfad
    /// der fertigen Datei oder <c>null</c>.
    /// </summary>
    public Func<string, string?, string?, Task>? CompletionSink { get; set; }

    private NSUrlSession Session => session ??= CreateSession();

    private NSUrlSession CreateSession()
    {
        var config = NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(SessionIdentifier);
        config.SessionSendsLaunchEvents = true;
        return NSUrlSession.FromConfiguration(config, this, null);
    }

    /// <summary>
    /// iOS-Einstieg nach System-Neustart der App für Hintergrund-Session-Events:
    /// stellt die Session her, sodass ausstehende Delegate-Rückrufe eintreffen,
    /// und registriert den Completion-Handler.
    /// </summary>
    /// <param name="sessionIdentifier">Die vom System gemeldete Session-Kennung.</param>
    /// <param name="completionHandler">Der aufzurufende System-Completion-Handler.</param>
    public void HandleEventsForBackgroundUrlSession(string sessionIdentifier, Action completionHandler)
    {
        if (sessionIdentifier != SessionIdentifier)
        {
            completionHandler();
            return;
        }

        systemCompletionHandlers[Environment.TickCount] = completionHandler;
        _ = Session; // Session herstellen → Delegate-Events werden zugestellt.
    }

    /// <inheritdoc/>
    public Task<DownloadBackendResult> TransferAsync(
        string url,
        string? token,
        string targetPath,
        bool wifiOnly,
        IProgress<(long Downloaded, long Total)> progress,
        CancellationToken cancellationToken)
    {
        using var nsUrl = NSUrl.FromString(url);
        using var request = new NSMutableUrlRequest(nsUrl!);
        if (token is not null)
        {
            request["Authorization"] = $"Bearer {token}";
        }

        request.AllowsCellularAccess = !wifiOnly;

        var task = Session.CreateDownloadTask(request);
        var transfer = new PendingTransfer
        {
            Tcs = new TaskCompletionSource<DownloadBackendResult>(TaskCreationOptions.RunContinuationsAsynchronously),
            TargetPath = targetPath,
            Progress = progress
        };
        task.TaskDescription = Key(url, targetPath);
        pending[task.TaskIdentifier] = transfer;
        transfer.Cancellation = cancellationToken.Register(() =>
        {
            task.Cancel();
            if (pending.TryRemove(task.TaskIdentifier, out var removed))
            {
                removed.Cancellation.Dispose();
                TryDelete(removed.TargetPath);
                removed.Tcs.TrySetCanceled(cancellationToken);
            }
        });
        task.Resume();
        return transfer.Tcs.Task;
    }

    /// <inheritdoc/>
    [Export("URLSession:downloadTask:didFinishDownloadingToURL:")]
    public void DidFinishDownload(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
    {
        var key = downloadTask.TaskDescription ?? string.Empty;
        var tempPath = location.Path;
        if (pending.TryRemove(downloadTask.TaskIdentifier, out var transfer))
        {
            transfer.Cancellation.Dispose();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(transfer.TargetPath)!);
                File.Copy(tempPath!, transfer.TargetPath, overwrite: true);
                transfer.Tcs.TrySetResult(
                    new DownloadBackendResult(transfer.DownloadedBytes, transfer.TotalBytes, null));
            }
            catch (Exception)
            {
                transfer.Tcs.TrySetResult(new DownloadBackendResult(0, 0, DownloadService.ErrorIo));
            }
        }
        else if (CompletionSink is { } sink)
        {
            // Abgeschlossen ohne aktiven Transfer-Aufruf (System-Neustart):
            // der Aufrufer verschiebt die Temp-Datei und korrigiert das Manifest.
            _ = sink(key, null, tempPath);
        }
    }

    /// <inheritdoc/>
    [Export("URLSession:downloadTask:didWriteData:totalBytesWritten:totalBytesExpectedToWrite:")]
    public void DidWriteData(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
    {
        if (pending.TryGetValue(downloadTask.TaskIdentifier, out var transfer))
        {
            transfer.DownloadedBytes = totalBytesWritten;
            transfer.TotalBytes = Math.Max(0, totalBytesExpectedToWrite);
            transfer.Progress.Report((totalBytesWritten, transfer.TotalBytes));
        }
    }

    /// <inheritdoc/>
    [Export("URLSession:task:didCompleteWithError:")]
    public void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError? error)
    {
        if (error is null)
        {
            return; // Erfolg wurde in DidFinishDownload behandelt.
        }

        var key = task.TaskDescription ?? string.Empty;
        if (pending.TryRemove(task.TaskIdentifier, out var transfer))
        {
            transfer.Cancellation.Dispose();
            TryDelete(transfer.TargetPath);
            var code = error.Code == (nint)NSUrlError.Cancelled
                ? DownloadService.ErrorNetwork
                : DownloadService.ErrorNetwork;
            transfer.Tcs.TrySetResult(new DownloadBackendResult(0, 0, code));
        }
        else if (CompletionSink is { } sink)
        {
            _ = sink(key, DownloadService.ErrorNetwork, null);
        }
    }

    /// <inheritdoc/>
    [Export("URLSessionDidFinishEventsForBackgroundURLSession:")]
    public void DidFinishEventsForBackgroundSession(NSUrlSession session)
    {
        foreach (var handler in systemCompletionHandlers.Values)
        {
            handler();
        }

        systemCompletionHandlers.Clear();
    }

    private static string Key(string url, string targetPath) =>
        $"{Path.GetFileNameWithoutExtension(targetPath)}|{url.GetHashCode():X8}";

    private static void TryDelete(string? path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception)
        {
            // Reste zählt der nächste Abgleich mit.
        }
    }
}
#endif
