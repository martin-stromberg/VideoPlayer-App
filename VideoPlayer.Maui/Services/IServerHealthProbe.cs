namespace VideoPlayer.Maui.Services;

/// <summary>Prüft die Erreichbarkeit des VideoPlayer-Servers.</summary>
public interface IServerHealthProbe
{
    Task<bool> CheckAsync(CancellationToken cancellationToken = default);
}
