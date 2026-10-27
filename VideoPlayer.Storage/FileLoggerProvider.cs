using Microsoft.Extensions.Logging;

namespace VideoPlayer.Storage;

/// <summary>
/// <see cref="ILoggerProvider"/>, der <see cref="ILogger"/>-Ausgaben über
/// <see cref="FileLogger"/> in das Datei-Protokoll des
/// <see cref="AppLogService"/> schreibt. Wird in <c>MauiProgram</c> als
/// <see cref="ILoggerProvider"/>-Singleton registriert; der
/// Schaltzustand liegt ausschließlich im <see cref="AppLogService"/>.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly AppLogService log;

    /// <summary>Erstellt den Provider.</summary>
    /// <param name="log">Der Datei-Protokoll-Dienst (Schaltzustand und Schreibziel).</param>
    public FileLoggerProvider(AppLogService log)
    {
        this.log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, log);

    /// <inheritdoc/>
    public void Dispose()
    {
        // Kein Zustand zu räumen — die Protokolldatei gehört dem AppLogService.
    }
}
