using Microsoft.Extensions.Logging;

namespace VideoPlayer.TvOS.Services
{
    /// <summary>
    /// <see cref="ILogger{T}"/>-Adapter: reicht Aufrufe an den per
    /// <see cref="ILoggerFactory"/> erzeugten kategorisierten
    /// <see cref="ILogger"/> weiter — Ersatz für den generischen
    /// <c>Logger&lt;T&gt;</c> aus dem nicht referenzierten Paket
    /// <c>Microsoft.Extensions.Logging</c>.
    /// </summary>
    /// <typeparam name="T">Kategorie-Typ (Name der Kategorie).</typeparam>
    public sealed class AppLogger<T> : ILogger<T>
    {
        private readonly ILogger inner;

        /// <summary>Initialisiert eine neue Instanz der <see cref="AppLogger{T}"/>-Klasse.</summary>
        /// <param name="factory">Logger-Fabrik (Kategorie = <typeparamref name="T"/>).</param>
        public AppLogger(ILoggerFactory factory)
        {
            ArgumentNullException.ThrowIfNull(factory);
            inner = factory.CreateLogger(typeof(T).FullName ?? typeof(T).Name);
        }

        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => inner.BeginScope(state);

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

        /// <inheritdoc/>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            inner.Log(logLevel, eventId, state, exception, formatter);
    }
}
