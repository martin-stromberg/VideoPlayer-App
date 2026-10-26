using System.Diagnostics;
using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests;

/// <summary>
/// Collection-Fixture: baut die Windows-App einmalig je Testlauf als
/// Debug-Build (nur dort ist die Developer-API aktiv) und stellt den
/// verifizierten Exe-Pfad bereit. Alle E2E-Tests laufen sequentiell in
/// der <c>E2E</c>-Collection — parallele App-Instanzen würden sich um
/// Fensterfokus und <c>devapi.json</c> streiten.
/// </summary>
[CollectionDefinition("E2E", DisableParallelization = true)]
public sealed class E2ECollectionDefinition : ICollectionFixture<AppFixture>;

/// <summary>Baut die App einmalig (Lazy) und liefert den Exe-Pfad.</summary>
public sealed class AppFixture
{
    private const string WindowsTfm = "net10.0-windows10.0.19041.0";

    private readonly Lazy<Task<string>> exePath;

    /// <summary>Initialisiert die Fixture.</summary>
    public AppFixture()
    {
        exePath = new Lazy<Task<string>>(BuildAppAsync);
    }

    /// <summary>Stellt sicher, dass die App gebaut ist, und liefert den Exe-Pfad.</summary>
    /// <returns>Absoluter Pfad der Debug-Exe.</returns>
    public Task<string> EnsureBuiltAsync() => exePath.Value;

    private static async Task<string> BuildAppAsync()
    {
        var root = RepositoryRoot.Find();
        var projectPath = Path.Combine(root, "VideoPlayer.Maui", "VideoPlayer.Maui.csproj");
        var arguments = $"build \"{projectPath}\" -f {WindowsTfm} -c Debug --nologo -v q --disable-build-servers";

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("dotnet", arguments)
            {
                WorkingDirectory = root,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);
        try
        {
            await Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync(cts.Token)).WaitAsync(cts.Token);
        }
        catch
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Der Debug-Build der App ist fehlgeschlagen (Exit {process.ExitCode}).\n{stdout}\n{stderr}");
        }

        var exePath = Path.Combine(
            root, "VideoPlayer.Maui", "bin", "Debug", WindowsTfm, "win-x64", "VideoPlayer.Maui.exe");
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException($"Die gebaute Exe wurde nicht gefunden: {exePath}");
        }

        return exePath;
    }
}
