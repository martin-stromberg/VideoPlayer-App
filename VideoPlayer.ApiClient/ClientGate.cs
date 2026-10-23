using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace VideoPlayer.ApiClient;

/// <summary>
/// Löst den API-Schlüssel für das serverseitige Client-Gate auf und stellt den
/// zugehörigen Header-Namen bereit. Quellen in Reihenfolge: gepaartes
/// Geräte-Token (<see cref="DeviceTokenProvider"/>), Umgebungsvariable,
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
    /// Umgebungsvariable des Anwendungsprofils — spiegelt
    /// <c>AppProfile.EnvironmentVariable</c> (VideoPlayer.Storage), ohne
    /// eine Projekt-Abhängigkeit einzuführen.
    /// </summary>
    private const string ProfileEnvironmentVariable = "VIDEOPLAYER_PROFILE";

    /// <summary>
    /// Verzeichnis der Token-Datei: <c>%LocalAppData%/VideoPlayer</c>
    /// bzw. das plattformspezifische lokale Anwendungsdatenverzeichnis.
    /// Mit gesetztem <c>VIDEOPLAYER_PROFILE</c> liegt die Datei im
    /// Profil-Unterverzeichnis — ein isoliertes Profil (E2E-Tests)
    /// erbt so nicht den Gate-Schlüssel des Standardprofils.
    /// </summary>
    public static string TokenFileDirectory { get; } = ResolveTokenFileDirectory();

    /// <summary>Vollständiger Pfad der Token-Datei.</summary>
    public static string TokenFilePath { get; } =
        Path.Combine(TokenFileDirectory, ApiTokenFileName);

    /// <summary>
    /// Liefert das gepaarte Geräte-Token des aktiven Servers, sofern eines
    /// gesetzt ist. Der Delegate wird von <c>Resolve()</c> vor allen
    /// statischen Quellen ausgewertet; <c>null</c> oder ein leerer/platzhalter-
    /// artiger Rückgabewert fällt auf die statische Auflösung zurück.
    /// </summary>
    public static Func<string?>? DeviceTokenProvider { get; set; }

    /// <summary>
    /// Löst den API-Schlüssel auf: Geräte-Token vor Umgebungsvariable vor
    /// Assembly-Metadata vor Token-Datei vor Platzhalter.
    /// </summary>
    /// <returns>Der aufgelöste API-Schlüssel bzw. der Platzhalter.</returns>
    public static string Resolve()
    {
        var deviceToken = DeviceTokenProvider?.Invoke();
        var environmentValue = Environment.GetEnvironmentVariable(ApiTokenEnvironmentVariable);
        var metadataValue = ReadMetadataValue();
        var fileValue = ReadTokenFile();
        var resolved = Resolve(deviceToken, environmentValue, metadataValue, fileValue);
        WriteDiagnostic(deviceToken, environmentValue, metadataValue, fileValue, resolved);
        return resolved;
    }

    /// <summary>
    /// Löst nur die statischen Quellen auf (Umgebungsvariable, Assembly-Metadata,
    /// Token-Datei, Platzhalter) — ohne <see cref="DeviceTokenProvider"/>.
    /// Dient als Fallback nach dem Verwerfen eines Geräte-Tokens und zur
    /// Prüfung, ob ein statischer Schlüssel konfiguriert ist.
    /// </summary>
    /// <returns>Der statisch aufgelöste API-Schlüssel bzw. der Platzhalter.</returns>
    public static string ResolveStatic()
    {
        var environmentValue = Environment.GetEnvironmentVariable(ApiTokenEnvironmentVariable);
        var metadataValue = ReadMetadataValue();
        var fileValue = ReadTokenFile();
        var resolved = Resolve(environmentValue, metadataValue, fileValue);
        WriteDiagnostic(null, environmentValue, metadataValue, fileValue, resolved);
        return resolved;
    }

    internal static string Resolve(string? environmentValue, string? metadataValue)
        => Resolve(null, environmentValue, metadataValue, null);

    internal static string Resolve(string? environmentValue, string? metadataValue, string? fileValue)
        => Resolve(null, environmentValue, metadataValue, fileValue);

    internal static string Resolve(string? deviceToken, string? environmentValue, string? metadataValue, string? fileValue)
    {
        return FirstConfigured(deviceToken)
            ?? FirstConfigured(environmentValue)
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

    private static string ResolveTokenFileDirectory()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VideoPlayer");

        // Normalisierung wie AppProfile.Create: trimmen, kleinschreiben,
        // leer/„default" = Standardprofil ohne Unterverzeichnis.
        var profile = Environment.GetEnvironmentVariable(ProfileEnvironmentVariable)
            ?.Trim().ToLowerInvariant();
        return string.IsNullOrEmpty(profile) || profile == "default"
            ? root
            : Path.Combine(root, profile);
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
        string? deviceToken,
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
                $"device={Describe(deviceToken)} env={Describe(environmentValue)} meta={Describe(metadataValue)} " +
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
