namespace VideoPlayer.Maui.Services;

/// <summary>Liefert die Basisadresse des VideoPlayer-Servers.</summary>
public interface IServerAddressProvider
{
    Uri GetServerAddress();
}
