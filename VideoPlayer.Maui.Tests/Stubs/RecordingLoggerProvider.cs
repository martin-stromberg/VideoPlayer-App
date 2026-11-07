using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace VideoPlayer.Maui.Tests.Stubs;

/// <summary>
/// Aufzeichnender <see cref="ILoggerProvider"/>-/<see cref="ILoggerFactory"/>-Ersatz für Tests:
/// erzeugte Logger schreiben ihre Einträge in eine gemeinsame Liste.
/// </summary>
internal sealed class RecordingLoggerProvider : ILoggerProvider, ILoggerFactory
{
    private readonly ConcurrentBag<RecordedLogEntry> entries = new();

    /// <summary>Alle aufgezeichneten Einträge in Aufzeichnungsreihenfolge.</summary>
    /// <returns>Die Liste der aufgezeichneten Einträge.</returns>
    public IReadOnlyList<RecordedLogEntry> Entries => entries.ToArray();

    /// <summary>Erzeugt einen aufzeichnenden Logger der angegebenen Kategorie.</summary>
    /// <param name="categoryName">Name der Logger-Kategorie.</param>
    /// <returns>Ein aufzeichnender <see cref="ILogger"/>.</returns>
    public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, entries);

    /// <summary>Keine weiteren Provider — für <see cref="ILoggerFactory"/> erforderlich.</summary>
    /// <param name="provider">Der zusätzliche Provider (wird ignoriert).</param>
    public void AddProvider(ILoggerProvider provider)
    {
    }

    /// <summary>Gibt die Provider-Ressourcen frei (kein Zustand).</summary>
    public void Dispose()
    {
    }

    /// <summary>Erzeugt einen aufzeichnenden <see cref="ILogger{T}"/> für den angegebenen Diensttyp.</summary>
    /// <typeparam name="T">Der Diensttyp, dessen Name als Kategorie dient.</typeparam>
    /// <returns>Ein aufzeichnender <see cref="ILogger{T}"/>.</returns>
    public ILogger<T> CreateLogger<T>() =>
        new RecordingLogger<T>(typeof(T).FullName ?? typeof(T).Name, entries);

    private class RecordingLogger(string category, ConcurrentBag<RecordedLogEntry> sink) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            sink.Add(new RecordedLogEntry(logLevel, category, formatter(state, exception), exception));
        }
    }

    private sealed class RecordingLogger<T>(string category, ConcurrentBag<RecordedLogEntry> sink)
        : RecordingLogger(category, sink), ILogger<T>;
}
