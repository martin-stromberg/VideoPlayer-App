using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace VideoPlayer.ApiClient;

/// <summary>
/// Löst den API-Schlüssel für das serverseitige Client-Gate auf und stellt den
/// zugehörigen Header-Namen bereit. Quellen in Reihenfolge: Umgebungsvariable,
/// Assembly-Metadata (Build-Eigenschaft), Token-Datei im lokalen
/// Anwendungsdatenverzeichnis, Platzhalter. Werte, die dem Platzhalter
/// entsprechen, gelten als nicht konfiguriert und werden übersprungen.
/// </summary>
public static class ClientGate
{
    /// <summary>HTTP-Headername, über den der API-Schlüssel gesendet wird.</summary>
    public const string HeaderName = "X-API-Key";

    /// <summary>Umgebungsvariable mit dem API-Schlüssel (höchste Priorität).</summary>
    public const string ApiTokenEnvironmentVariable = "VIDEOWEBPLAYER_MAUI_API_TOKEN";

    /// <summary>Assembly-Metadata-Schlüssel, über den der API-Schlüssel beim Build injiziert wird.</summary>
    public const string ApiTokenAssemblyMetadataKey = "VideoPlayer.ApiClient.ApiToken";

    /// <summary>Platzhalter, wenn keine Quelle einen konfigurierten Schlüssel liefert.</summary>
    public const string ApiTokenPlaceholder = "<MAUI_CLIENT_API_TOKEN>";

    /// <summary>
    /// Dateiname der Token-Datei unterhalb von <see cref="TokenFileDirectory"/>.
    /// Die Datei enthält den API-Schlüssel als reinen Text und dient als
    /// lokale Quelle, wenn weder Umgebungsvariable noch Build-Metadaten
    /// gesetzt sind (z. B. weil die Umgebungsvariable den gestarteten
    /// Prozess nicht erreicht).
    /// </summary>
    public const string ApiTokenFileName = "client-api-token.txt";

    /// <summary>
    /// Verzeichnis der Token-Datei: <c>%LocalAppData%/VideoPlayer</c>
    /// bzw. das plattformspezifische lokale Anwendungsdatenverzeichnis.
    /// </summary>
    public static string TokenFileDirectory { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VideoPlayer");

    /// <summary>Vollständiger Pfad der Token-Datei.</summary>
    public static string TokenFilePath { get; } =
        Path.Combine(TokenFileDirectory, ApiTokenFileName);

    /// <summary>
    /// Löst den API-Schlüssel auf: Umgebungsvariable vor Assembly-Metadata
    /// vor Token-Datei vor Platzhalter.
    /// </summary>
    /// <returns>Der aufgelöste API-Schlüssel bzw. der Platzhalter.</returns>
    public static string Resolve()
    {
        var environmentValue = Environment.GetEnvironmentVariable(ApiTokenEnvironmentVariable);
        var metadataValue = ReadMetadataValue();
        var fileValue = ReadTokenFile();
        var resolved = Resolve(environmentValue, metadataValue, fileValue);
        WriteDiagnostic(environmentValue, metadataValue, fileValue, resolved);
        return resolved;
    }

    internal static string Resolve(string? environmentValue, string? metadataValue)
        => Resolve(environmentValue, metadataValue, null);

    internal static string Resolve(string? environmentValue, string? metadataValue, string? fileValue)
    {
        return FirstConfigured(environmentValue)
            ?? FirstConfigured(metadataValue)
            ?? FirstConfigured(fileValue)
            ?? ApiTokenPlaceholder;
    }

    private static string? FirstConfigured(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) || trimmed == ApiTokenPlaceholder
            ? null
            : trimmed;
    }

    private static string? ReadMetadataValue()
    {
        return typeof(ClientGate).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == ApiTokenAssemblyMetadataKey)
            ?.Value;
    }

    private static string? ReadTokenFile()
    {
        try
        {
            return File.Exists(TokenFilePath)
                ? File.ReadAllText(TokenFilePath).Trim()
                : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    [Conditional("DEBUG")]
    private static void WriteDiagnostic(
        string? environmentValue,
        string? metadataValue,
        string? fileValue,
        string resolved)
    {
        try
        {
            var line = string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{DateTime.UtcNow:O} pid={Environment.ProcessId} exe={Environment.ProcessPath} " +
                $"env={Describe(environmentValue)} meta={Describe(metadataValue)} " +
                $"file={Describe(fileValue)} resolved={Describe(resolved)}{Environment.NewLine}");
            File.AppendAllText(
                Path.Combine(Path.GetTempPath(), "videoplayer-clientgate.log"),
                line);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string Describe(string? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (value.Length == 0)
        {
            return "empty";
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        return $"len={value.Length},sha8={hash[..8]}";
    }
}
