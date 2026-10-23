#if DEBUG || DEV_API
namespace msTools.DeveloperApi;

/// <summary>
/// Fassade der Developer-API: startet den <see cref="DevApiHttpServer"/>
/// mit der gegebenen Routen-Tabelle und legt die Metadatendatei
/// <c>devapi.json</c> im angegebenen Verzeichnis ab — bei nicht
/// schreibbarem Ort im optionalen Fallback-Verzeichnis.
/// </summary>
public sealed class DevApiServer : IDisposable
{
    private readonly DevApiRouter router;
    private readonly string metadataDirectory;
    private readonly string? fallbackMetadataDirectory;
    private DevApiHttpServer? server;

    /// <summary>Erstellt die Fassade.</summary>
    /// <param name="router">Die Routen-Tabelle mit den registrierten Endpunkten.</param>
    /// <param name="metadataDirectory">Primäres Verzeichnis für <c>devapi.json</c> (z. B. <see cref="AppContext.BaseDirectory"/>).</param>
    /// <param name="fallbackMetadataDirectory">Optionales Fallback-Verzeichnis, falls das primäre nicht schreibbar ist.</param>
    public DevApiServer(DevApiRouter router, string metadataDirectory, string? fallbackMetadataDirectory = null)
    {
        this.router = router ?? throw new ArgumentNullException(nameof(router));
        ArgumentException.ThrowIfNullOrWhiteSpace(metadataDirectory);
        this.metadataDirectory = metadataDirectory;
        this.fallbackMetadataDirectory = fallbackMetadataDirectory;
    }

    /// <summary>Der vom System vergebene Port (gültig nach <see cref="Start"/>).</summary>
    public int Port { get; private set; }

    /// <summary>Pfad der geschriebenen Metadatendatei <c>devapi.json</c>.</summary>
    public string? MetadataPath { get; private set; }

    /// <summary>Startet den HTTP-Server und schreibt die Metadatendatei.</summary>
    public void Start()
    {
        if (server is not null)
        {
            return;
        }

        server = new DevApiHttpServer(router);
        server.Start();
        Port = server.Port;
        _ = WriteMetadataAsync();
    }

    /// <summary>Stoppt den HTTP-Server.</summary>
    public void Dispose()
    {
        server?.Dispose();
        server = null;
    }

    /// <summary>
    /// Schreibt <c>devapi.json</c> ins Primärverzeichnis; bei nicht
    /// schreibbarem Ort in das optionale Fallback-Verzeichnis.
    /// </summary>
    private async Task WriteMetadataAsync()
    {
        try
        {
            var metadata = DevApiMetadata.Create(Port);
            var path = Path.Combine(metadataDirectory, "devapi.json");
            try
            {
                await metadata.WriteAsync(path);
            }
            catch (Exception) when (fallbackMetadataDirectory is not null)
            {
                path = Path.Combine(fallbackMetadataDirectory, "devapi.json");
                await metadata.WriteAsync(path);
            }

            MetadataPath = path;
        }
        catch (Exception ex)
        {
            // Fehlgeschlagene Metadaten-Ablage ist kein Funktionsfehler der API.
            System.Diagnostics.Debug.WriteLine($"[DevApi] devapi.json konnte nicht geschrieben werden: {ex.Message}");
        }
    }
}
#endif
