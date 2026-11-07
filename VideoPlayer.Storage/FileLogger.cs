using Microsoft.Extensions.Logging;

namespace VideoPlayer.Storage;

/// <summary>
/// <see cref="ILogger"/>-Implementierung, die formatierte Protokolleinträge
/// an den <see cref="AppLogService"/> weiterreicht. Einträge unterhalb von
/// <see cref="LogLevel.Information"/> werden verworfen, um das Protokoll
/// vor Framework-Rauschen zu schützen.
/// </summary>
public sealed class FileLogger : ILogger
{
    private readonly string category;
    private readonly AppLogService log;

    /// <summary>Erstellt den Logger.</summary>
    /// <param name="category">Protokoll-Kategorie (Logger-Name).</param>
    /// <param name="log">Der Datei-Protokoll-Dienst.</param>
    public FileLogger(string category, AppLogService log)
    {
        this.category = category ?? throw new ArgumentNullException(nameof(category));
        this.log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel)
    {
        return log.IsEnabled
            && logLevel >= LogLevel.Information
            && logLevel != LogLevel.None;
    }

    /// <inheritdoc/>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(formatter);
        log.Write(logLevel, category, formatter(state, exception), exception);
    }
}
