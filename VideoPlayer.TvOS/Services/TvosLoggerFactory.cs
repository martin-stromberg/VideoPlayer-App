using Microsoft.Extensions.Logging;

namespace VideoPlayer.TvOS.Services
{
    /// <summary>
    /// Minimale <see cref="ILoggerFactory"/>-Implementierung ohne
    /// DI-Container: erzeugt <see cref="ILogger"/>-Instanzen über einen
    /// einzigen <see cref="ILoggerProvider"/> (der
    /// <c>FileLoggerProvider</c>-Programmprotokoll-Stapel aus
    /// <c>VideoPlayer.Storage</c>). Das Paket
    /// <c>Microsoft.Extensions.Logging</c> (mit <c>LoggerFactory.Create</c>)
    /// wird bewusst nicht referenziert, damit das tvOS-Projekt ohne
    /// Paketverweise auskommt.
    /// </summary>
    public sealed class TvosLoggerFactory : ILoggerFactory
    {
        private readonly ILoggerProvider provider;

        /// <summary>Initialisiert eine neue Instanz der <see cref="TvosLoggerFactory"/>-Klasse.</summary>
        /// <param name="provider">Logger-Anbieter (<c>FileLoggerProvider</c>).</param>
        public TvosLoggerFactory(ILoggerProvider provider)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName) =>
            provider.CreateLogger(categoryName);

        /// <inheritdoc/>
        public void AddProvider(ILoggerProvider provider)
        {
            // Die Fabrik kennt genau einen Anbieter (Kompositionswurzel ohne DI).
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            provider.Dispose();
        }
    }
}
