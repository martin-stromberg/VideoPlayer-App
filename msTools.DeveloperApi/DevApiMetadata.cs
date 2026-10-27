#if DEBUG || DEV_API
using System.Text.Json;

namespace msTools.DeveloperApi;

/// <summary>
/// Metadaten der laufenden Developer-API-Instanz, die als
/// <c>devapi.json</c> im Programmverzeichnis abgelegt werden, damit ein
/// Automatisierungsclient den systemvergebenen Port findet.
/// </summary>
public sealed record DevApiMetadata
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Der vom System vergebene Port des HTTP-Servers.</summary>
    public int Port { get; init; }

    /// <summary>Prozess-ID der App-Instanz.</summary>
    public int Pid { get; init; }

    /// <summary>Startzeitpunkt der API (UTC).</summary>
    public DateTime StartedUtc { get; init; }

    /// <summary>Erzeugt den Metadatensatz für eine laufende Instanz.</summary>
    /// <param name="port">Der vergebene Port.</param>
    /// <returns>Der Metadatensatz mit aktueller PID und UTC-Zeit.</returns>
    public static DevApiMetadata Create(int port) =>
        new() { Port = port, Pid = Environment.ProcessId, StartedUtc = DateTime.UtcNow };

    /// <summary>Schreibt den Datensatz camelCase-serialisiert in eine Datei.</summary>
    /// <param name="path">Zieldatei (z. B. <c>devapi.json</c> im Programmverzeichnis).</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    public async Task WriteAsync(string path, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(this, JsonOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }
}
#endif
