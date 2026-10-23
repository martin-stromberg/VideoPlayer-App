using Microsoft.Extensions.Logging;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Tests;

/// <summary>Tests für <c>FileLogger</c> und <c>FileLoggerProvider</c>.</summary>
public class FileLoggerTests : IDisposable
{
    private readonly string root;

    /// <summary>Initialisiert eine neue Instanz der Testklasse mit einem isolierten Temp-Verzeichnis.</summary>
    public FileLoggerTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-filelogger-tests", Guid.NewGuid().ToString("N"));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private AppLogService CreateLogService(bool enabled)
    {
        var layout = new AppStorageLayout(
            backupRoot: Path.Combine(root, "backup"),
            noBackupRoot: Path.Combine(root, "nobackup"),
            cacheRoot: Path.Combine(root, "cache"),
            profile: AppProfile.Create(null));
        return new AppLogService(layout, enabled);
    }

    /// <summary>Prüft IsEnabled: Einträge ab Information sind aktiv, Trace/Debug/None nicht.</summary>
    [Fact]
    public void IsEnabled_RespectsMinimumLevel()
    {
        var logger = new FileLogger("Kategorie", CreateLogService(enabled: true));

        Assert.False(logger.IsEnabled(LogLevel.Trace));
        Assert.False(logger.IsEnabled(LogLevel.Debug));
        Assert.True(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Error));
        Assert.True(logger.IsEnabled(LogLevel.Critical));
        Assert.False(logger.IsEnabled(LogLevel.None));
    }

    /// <summary>Prüft IsEnabled: bei deaktiviertem Dienst ist kein Level aktiv.</summary>
    [Fact]
    public void IsEnabled_WhenServiceDisabled_ReturnsFalse()
    {
        var logger = new FileLogger("Kategorie", CreateLogService(enabled: false));

        Assert.False(logger.IsEnabled(LogLevel.Error));
    }

    /// <summary>Prüft Log: schreibt über den Dienst in die Protokolldatei.</summary>
    [Fact]
    public void Log_WritesThroughToLogFile()
    {
        var log = CreateLogService(enabled: true);
        var logger = new FileLogger("VideoPlayer.Test", log);

        logger.Log(LogLevel.Warning, new EventId(7), "Zustand", null, (state, _) => $"Meldung: {state}");

        var content = File.ReadAllText(log.LogFilePath);
        Assert.Contains("[WARN] VideoPlayer.Test: Meldung: Zustand", content);
    }

    /// <summary>Prüft Log: Einträge unterhalb des Mindestlevels werden nicht geschrieben.</summary>
    [Fact]
    public void Log_BelowMinimumLevel_Skipped()
    {
        var log = CreateLogService(enabled: true);
        var logger = new FileLogger("VideoPlayer.Test", log);

        logger.Log(LogLevel.Debug, new EventId(0), "x", null, (state, _) => "Debug-Rauschen");

        Assert.DoesNotContain("Debug-Rauschen", File.ReadAllText(log.LogFilePath));
    }

    /// <summary>Prüft LoggerFactory-Integration: ein registrierter Provider erhält die Ausgaben.</summary>
    [Fact]
    public void LoggerFactory_WithProvider_WritesToLogFile()
    {
        var log = CreateLogService(enabled: true);
        using var factory = LoggerFactory.Create(builder =>
            builder.AddProvider(new FileLoggerProvider(log)));

        factory.CreateLogger("VideoPlayer.Test").LogError("Fabrikmeldung");

        Assert.Contains("[ERROR] VideoPlayer.Test: Fabrikmeldung", File.ReadAllText(log.LogFilePath));
    }
}
