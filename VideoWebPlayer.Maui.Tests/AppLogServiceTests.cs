using Microsoft.Extensions.Logging;
using VideoPlayer.Storage;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>Tests für <c>AppLogService</c> (schaltbares Datei-Protokoll).</summary>
public class AppLogServiceTests : IDisposable
{
    private readonly string root;

    /// <summary>Initialisiert eine neue Instanz der Testklasse mit einem isolierten Temp-Verzeichnis.</summary>
    public AppLogServiceTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-applog-tests", Guid.NewGuid().ToString("N"));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private AppLogService CreateService(bool enabled, string? profile = null)
    {
        var layout = new AppStorageLayout(
            backupRoot: Path.Combine(root, "backup"),
            noBackupRoot: Path.Combine(root, "nobackup"),
            cacheRoot: Path.Combine(root, "cache"),
            profile: AppProfile.Create(profile));
        return new AppLogService(layout, enabled);
    }

    /// <summary>Prüft Write: bei deaktivierter Protokollierung wird keine Datei erzeugt.</summary>
    [Fact]
    public void Write_WhenDisabled_CreatesNoFile()
    {
        var log = CreateService(enabled: false);

        log.Write(LogLevel.Information, "Test", "Nachricht");

        Assert.False(File.Exists(log.LogFilePath));
        Assert.Empty(log.GetAttachmentPaths());
    }

    /// <summary>Prüft Konstruktor: bei aktiviertem Start wird die Datei mit Marker angelegt.</summary>
    [Fact]
    public void Ctor_WhenEnabled_CreatesLogFileWithMarker()
    {
        var log = CreateService(enabled: true);

        Assert.True(File.Exists(log.LogFilePath));
        Assert.Contains("=== Protokollierung beim Start aktiv ===", File.ReadAllText(log.LogFilePath));
    }

    /// <summary>Prüft SetEnabled: das Aktivieren legt die Datei mit Marker an.</summary>
    [Fact]
    public void SetEnabled_True_CreatesLogFileWithMarker()
    {
        var log = CreateService(enabled: false);

        log.SetEnabled(true);

        Assert.True(log.IsEnabled);
        Assert.True(File.Exists(log.LogFilePath));
        Assert.Contains("=== Protokollierung aktiviert ===", File.ReadAllText(log.LogFilePath));
    }

    /// <summary>Prüft SetEnabled: das Deaktivieren schreibt einen Marker und beendet das Protokoll.</summary>
    [Fact]
    public void SetEnabled_False_StopsWriting()
    {
        var log = CreateService(enabled: true);

        log.SetEnabled(false);
        log.Write(LogLevel.Error, "Test", "Darf nicht geschrieben werden");

        var content = File.ReadAllText(log.LogFilePath);
        Assert.False(log.IsEnabled);
        Assert.Contains("=== Protokollierung deaktiviert ===", content);
        Assert.DoesNotContain("Darf nicht geschrieben werden", content);
    }

    /// <summary>Prüft Write: bei aktivierter Protokollierung wird eine formatierte Zeile angehängt.</summary>
    [Fact]
    public void Write_WhenEnabled_AppendsFormattedLine()
    {
        var log = CreateService(enabled: true);

        log.Write(LogLevel.Warning, "Meine.Kategorie", "Etwas ist passiert");

        var content = File.ReadAllText(log.LogFilePath);
        Assert.Contains("[WARN] Meine.Kategorie: Etwas ist passiert", content);
        // Zeitstempel-Format am Zeilenanfang des Eintrags.
        Assert.Matches(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} \[WARN\]", content);
    }

    /// <summary>Prüft Write: eine Ausnahme wird mit vollständigem Text angehängt.</summary>
    [Fact]
    public void Write_WithException_AppendsExceptionDetails()
    {
        var log = CreateService(enabled: true);
        var exception = new InvalidOperationException("Testfehler-42");

        log.Write(LogLevel.Error, "Test", "Fehlerpfad", exception);

        var content = File.ReadAllText(log.LogFilePath);
        Assert.Contains("[ERROR] Test: Fehlerpfad", content);
        Assert.Contains("InvalidOperationException", content);
        Assert.Contains("Testfehler-42", content);
    }

    /// <summary>Prüft Rotation: oberhalb des Größenlimits wandert die Datei in die Vorgänger-Datei.</summary>
    [Fact]
    public void Write_OverMaxBytes_RotatesToPreviousFile()
    {
        var log = CreateService(enabled: true);
        File.AppendAllText(log.LogFilePath, new string('x', (int)AppLogService.MaxLogFileBytes + 1));

        log.Write(LogLevel.Information, "Test", "nach der Rotation");

        Assert.True(File.Exists(log.PreviousLogFilePath));
        Assert.True(new FileInfo(log.PreviousLogFilePath).Length > AppLogService.MaxLogFileBytes);
        Assert.Contains("nach der Rotation", File.ReadAllText(log.LogFilePath));
    }

    /// <summary>Prüft GetAttachmentPaths: liefert die vorhandenen Protokolldateien (aktuelle zuerst).</summary>
    [Fact]
    public void GetAttachmentPaths_ReturnsExistingLogFiles()
    {
        var log = CreateService(enabled: true);
        File.WriteAllText(log.PreviousLogFilePath, "alt");

        var paths = log.GetAttachmentPaths();

        Assert.Equal(2, paths.Count);
        Assert.Equal(log.LogFilePath, paths[0]);
        Assert.Equal(log.PreviousLogFilePath, paths[1]);
    }

    /// <summary>Prüft GetAttachmentPaths: ohne Dateien eine leere Liste (kein Versand-Anhang).</summary>
    [Fact]
    public void GetAttachmentPaths_WithoutFiles_ReturnsEmpty()
    {
        var log = CreateService(enabled: false);

        Assert.Empty(log.GetAttachmentPaths());
    }

    /// <summary>Prüft Profile: das Protokoll ist profil-isoliert.</summary>
    [Fact]
    public void LogFilePath_IsProfileScoped()
    {
        var defaultLog = CreateService(enabled: false);
        var workLog = CreateService(enabled: false, profile: "work");

        Assert.NotEqual(defaultLog.LogFilePath, workLog.LogFilePath);
        Assert.Contains("work", workLog.LogFilePath, StringComparison.Ordinal);
    }
}
