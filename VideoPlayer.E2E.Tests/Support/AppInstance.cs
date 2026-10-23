using System.Diagnostics;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace VideoPlayer.E2E.Tests.Support;

/// <summary>
/// Eine gestartete Instanz der echten <c>VideoPlayer.Maui.exe</c> für
/// einen Test: isoliertes Profil (<c>VIDEOPLAYER_PROFILE</c>), ohne
/// vererbten statischen Gate-Schlüssel
/// (<c>VIDEOWEBPLAYER_MAUI_API_TOKEN</c> wird entfernt — sonst würde die
/// App kein Geräte-Pairing einfordern). Wartet auf <c>devapi.json</c>
/// mit der eigenen PID, stellt <see cref="DevApiClient"/> und den
/// FlaUI-<see cref="UiDriver"/> bereit und beendet den Prozess im
/// <see cref="Dispose()"/> garantiert.
/// </summary>
public sealed class AppInstance : IDisposable
{
    /// <summary>Umgebungsvariable der Profil-Isolation (<c>AppProfile</c>).</summary>
    public const string ProfileEnvironmentVariable = "VIDEOPLAYER_PROFILE";

    /// <summary>Umgebungsvariable des statischen Gate-Schlüssels (<c>ClientGate</c>).</summary>
    public const string ApiTokenEnvironmentVariable = "VIDEOWEBPLAYER_MAUI_API_TOKEN";

    private readonly UIA3Automation automation = new();
    private readonly Process process;
    private readonly string exeDirectory;
    private bool disposed;

    private AppInstance(Process process, string exeDirectory, string profile, int devApiPort)
    {
        this.process = process;
        this.exeDirectory = exeDirectory;
        Profile = profile;
        DevApi = new DevApiClient(devApiPort);
        Ui = new UiDriver(automation, WaitForMainWindow(automation, process));
    }

    /// <summary>Der verwendete Profilname.</summary>
    public string Profile { get; }

    /// <summary>Client der Developer-API der laufenden App.</summary>
    public DevApiClient DevApi { get; }

    /// <summary>FlaUI-Treiber für echte UIA-Interaktionen.</summary>
    public UiDriver Ui { get; }

    /// <summary>Der App-Prozess.</summary>
    public Process Process => process;

    /// <summary>
    /// Startet die App mit einem eigenen Profil und wartet, bis die
    /// Developer-API erreichbar ist und das Hauptfenster steht.
    /// </summary>
    /// <param name="exePath">Pfad der Debug-Exe (aus <c>AppFixture</c>).</param>
    /// <param name="testName">Name des Tests (Teil des Profilnamens).</param>
    /// <param name="startupTimeout">Maximale Wartezeit auf API + Fenster.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <param name="profile">Expliziter Profilname (z. B. für einen
    /// Neustart-Test mit dem Profil einer früheren Instanz);
    /// <c>null</c> = frisches, eindeutiges Testprofil.</param>
    /// <returns>Die laufende Instanz.</returns>
    public static async Task<AppInstance> StartAsync(
        string exePath, string testName, TimeSpan? startupTimeout = null,
        CancellationToken cancellationToken = default,
        string? profile = null)
    {
        var timeout = startupTimeout ?? TimeSpan.FromSeconds(45);
        var exeDirectory = Path.GetDirectoryName(exePath)!;
        var metadataPath = Path.Combine(exeDirectory, "devapi.json");
        TryDelete(metadataPath);

        var resolvedProfile = profile ?? $"e2e-{Sanitize(testName)}-{Guid.NewGuid():N}";

        var startInfo = new ProcessStartInfo(exePath)
        {
            WorkingDirectory = exeDirectory,
            UseShellExecute = false
        };
        startInfo.Environment[ProfileEnvironmentVariable] = resolvedProfile;
        // Der statische Gate-Schlüssel darf nicht geerbt werden — sonst
        // gilt IsPairingRequired=false und der Pairing-Pfad wäre untestbar.
        startInfo.Environment.Remove(ApiTokenEnvironmentVariable);

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Der App-Prozess konnte nicht gestartet werden.");

        try
        {
            var port = await WaitForDevApiPortAsync(metadataPath, process, timeout, cancellationToken);
            var instance = new AppInstance(process, exeDirectory, resolvedProfile, port);

            // Die API meldet sich, sobald das Fenster steht — bis der
            // erste Modell-Snapshot lesbar ist, kann noch etwas Zeit
            // vergehen (Startup-Workflow).
            await instance.DevApi.WaitForModelAsync(
                _ => true, timeout, "Developer-API erreichbar", cancellationToken);
            return instance;
        }
        catch
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw;
        }
    }

    /// <summary>
    /// Beendet die App: erst ordentlich über das Fenster, dann hart
    /// (<see cref="Process.Kill(bool)"/> als Garantie). Löscht
    /// <c>devapi.json</c> und das Profilverzeichnis.
    /// </summary>
    public void Dispose() => Dispose(deleteProfile: true);

    /// <summary>
    /// Beendet die App wie <see cref="Dispose()"/>, lässt das
    /// Profilverzeichnis aber auf Wunsch bestehen — für Neustart-Tests,
    /// die mit derselben Ablage weiterarbeiten (die Folge-Instanz räumt
    /// dann beim regulären Dispose auf).
    /// </summary>
    /// <param name="deleteProfile"><c>true</c> = Profilverzeichnis entfernen (Standard).</param>
    public void Dispose(bool deleteProfile)
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        try { Ui.Dispose(); } catch { /* best effort */ }
        try { DevApi.Dispose(); } catch { /* best effort */ }

        try
        {
            if (!process.HasExited)
            {
                try { process.CloseMainWindow(); } catch { /* best effort */ }
                if (!process.WaitForExit(10_000))
                {
                    process.Kill(entireProcessTree: true);
                }
            }
        }
        catch (Exception)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
        }
        finally
        {
            process.Dispose();
            TryDelete(Path.Combine(exeDirectory, "devapi.json"));
            if (deleteProfile)
            {
                DeleteProfileDirectory();
            }
            automation.Dispose();
        }
    }

    /// <summary>
    /// Sucht eine Datei im Protokollverzeichnis des Testprofils
    /// (<c>AppStorageLayout.LogsDirectory</c>). Die App läuft unpackaged:
    /// <c>FileSystem.AppDataDirectory</c> liegt auf Windows unter
    /// <c>%LOCALAPPDATA%\&lt;Publisher&gt;\de.martinstromberg.videos\Data</c> —
    /// der Publisher-Abschnitt wird deshalb nicht hart kodiert, sondern
    /// über eine Verzeichnissuche aufgelöst.
    /// </summary>
    /// <param name="fileName">Der gesuchte Dateiname (Standard: <c>videoplayer.log</c>).</param>
    /// <returns>Der gefundene Pfad oder <c>null</c>.</returns>
    public string? FindLogFile(string fileName = "videoplayer.log")
    {
        foreach (var noBackupDir in ProfileNoBackupDirectories())
        {
            var candidate = Path.Combine(noBackupDir, "logs", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private void DeleteProfileDirectory()
    {
        // Profildaten liegen auf Windows unter %APPDATA%/VideoPlayer/<profil>
        // (AppStorageLayout.BackupDirectory) und %LOCALAPPDATA%/VideoPlayer/
        // <profil> (ClientGate-Token-Datei, profil-isoliert).
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        };
        var candidates = roots
            .Select(root => Path.Combine(root, "VideoPlayer", Profile))
            // Nicht backup-pflichtige Ablage und Temp-Cache liegen unter dem
            // unpackaged AppDataDirectory (%LOCALAPPDATA%\<Publisher>\
            // de.martinstromberg.videos\…\<profil>) — Protokolle und
            // Downloads würden sonst als Testleichen liegen bleiben.
            .Concat(ProfileNoBackupDirectories())
            .Concat(AppDataProfileDirectories("Cache"));

        foreach (var dir in candidates)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch (Exception)
            {
                // Reste eines Testprofils sind unkritisch.
            }
        }
    }

    /// <summary>
    /// Liefert die nicht backup-pflichtigen Profilverzeichnisse
    /// (<c>…\de.martinstromberg.videos\Data\nobackup\{profil}</c>) unter
    /// <c>%LOCALAPPDATA%</c> — über Verzeichnissuche, weil der
    /// Publisher-Abschnitt vom MAUI-Essentials-Fallback abhängt.
    /// </summary>
    /// <returns>Die vorhandenen Kandidatenverzeichnisse.</returns>
    private IEnumerable<string> ProfileNoBackupDirectories()
        => AppDataProfileDirectories(Path.Combine("Data", "nobackup"));

    private IEnumerable<string> AppDataProfileDirectories(string subDirectory)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDataRoots = Directory
            .EnumerateDirectories(local)
            .Select(d => Path.Combine(d, "de.martinstromberg.videos"))
            .Prepend(Path.Combine(local, "de.martinstromberg.videos"));

        foreach (var appDataRoot in appDataRoots.Distinct())
        {
            var candidate = Path.Combine(appDataRoot, subDirectory, Profile);
            if (Directory.Exists(candidate))
            {
                yield return candidate;
            }
        }
    }

    private static async Task<int> WaitForDevApiPortAsync(
        string metadataPath, Process process, TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"Die App ist vor Bereitschaft der Developer-API beendet worden (Exit {process.ExitCode}).");
            }

            try
            {
                if (File.Exists(metadataPath))
                {
                    using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(metadataPath, cancellationToken));
                    var pid = doc.RootElement.GetProperty("pid").GetInt32();
                    if (pid == process.Id)
                    {
                        return doc.RootElement.GetProperty("port").GetInt32();
                    }
                }
            }
            catch (Exception)
            {
                // Datei noch halb geschrieben o. ä. — weiter pollen.
            }

            await Task.Delay(200, cancellationToken);
        }

        throw new TimeoutException("devapi.json der gestarteten App erschien nicht rechtzeitig.");
    }

    private static Window WaitForMainWindow(UIA3Automation automation, Process process)
    {
        var app = FlaUI.Core.Application.Attach(process.Id);
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(2));
            if (window is not null)
            {
                return window;
            }
        }

        throw new TimeoutException("Das Hauptfenster der App erschien nicht rechtzeitig (UIA).");
    }

    private static string Sanitize(string name)
    {
        var chars = name.Where(char.IsLetterOrDigit).Take(20).ToArray();
        return chars.Length == 0 ? "test" : new string(chars).ToLowerInvariant();
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) { File.Delete(path); } }
        catch (Exception) { /* best effort */ }
    }
}
