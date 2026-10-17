#if DEBUG || DEV_API
using msTools.DeveloperApi.Maui;
using VideoPlayer.Mvu;
using VideoPlayer.Mvu.DevApi;

namespace VideoPlayer.Maui.DevApi;

/// <summary>
/// App-seitiger Adapter der Developer-API: injiziert den
/// <see cref="MvuHost"/> und reicht den kuratierten
/// <see cref="DevApiModelSnapshot"/> als Provider an den
/// <see cref="MauiDevApiService"/> weiter, der Server, Endpunkte und
/// <c>devapi.json</c>-Ablage bereitstellt.
/// </summary>
public sealed class DevApiService : IDisposable
{
    private readonly MvuHost mvuHost;
    private MauiDevApiService? service;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="mvuHost">MVU-Host als Quelle des App-Modells.</param>
    public DevApiService(MvuHost mvuHost)
    {
        this.mvuHost = mvuHost ?? throw new ArgumentNullException(nameof(mvuHost));
    }

    /// <summary>Der vom System vergebene Port (gültig nach <see cref="Start"/>).</summary>
    public int Port => service?.Port ?? 0;

    /// <summary>Pfad der geschriebenen Metadatendatei <c>devapi.json</c>.</summary>
    public string? MetadataPath => service?.MetadataPath;

    /// <summary>Startet den HTTP-Server und schreibt die Metadatendatei.</summary>
    public void Start()
    {
        service ??= new MauiDevApiService(() => DevApiModelSnapshot.FromModel(mvuHost.Model));
        service.Start();
    }

    /// <summary>Stoppt den HTTP-Server.</summary>
    public void Dispose()
    {
        service?.Dispose();
        service = null;
    }
}
#endif
