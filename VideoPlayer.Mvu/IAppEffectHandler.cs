namespace VideoPlayer.Mvu;

/// <summary>
/// Führt MVU-Effekte am App-Rand aus (Navigation, Server-Aufrufe).
/// Implementiert von der MAUI-App; der MVU-Kern bleibt plattformneutral.
/// </summary>
public interface IAppEffectHandler
{
    /// <summary>Führt einen einzelnen Effekt aus (Navigation, Server-Aufrufe o. ä.).</summary>
    /// <param name="effect">Der auszuführende Effekt.</param>
    /// <param name="cancellationToken">Bricht die Ausführung ab.</param>
    Task HandleAsync(AppEffect effect, CancellationToken cancellationToken = default);
}
