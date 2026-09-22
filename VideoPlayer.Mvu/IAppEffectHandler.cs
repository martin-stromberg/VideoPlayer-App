namespace VideoPlayer.Mvu;

/// <summary>
/// Führt MVU-Effekte am App-Rand aus (Navigation, Server-Aufrufe).
/// Implementiert von der MAUI-App; der MVU-Kern bleibt plattformneutral.
/// </summary>
public interface IAppEffectHandler
{
    Task HandleAsync(AppEffect effect, CancellationToken cancellationToken = default);
}
