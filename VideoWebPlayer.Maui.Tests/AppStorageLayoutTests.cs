using VideoPlayer.Storage;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>Tests für <c>AppStorageLayout</c>.</summary>
public class AppStorageLayoutTests : IDisposable
{
    private readonly string root;

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>AppStorageLayoutTests</c>.</summary>
    public AppStorageLayoutTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-layout-tests", Guid.NewGuid().ToString("N"));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private AppStorageLayout CreateLayout(string? profile = null)
    {
        return new AppStorageLayout(
            backupRoot: Path.Combine(root, "backup"),
            noBackupRoot: Path.Combine(root, "nobackup"),
            cacheRoot: Path.Combine(root, "cache"),
            profile: AppProfile.Create(profile));
    }

    /// <summary>Prüft SeparatesBackupAndNoBackupRoots: trennt Backup- und NoBackup-Wurzeln.</summary>
    [Fact]
    public void SeparatesBackupAndNoBackupRoots()
    {
        var layout = CreateLayout();

        // Backup-pflichtig: Datenbank unter der Backup-Wurzel.
        Assert.StartsWith(Path.Combine(root, "backup"), layout.DataDatabasePath);
        Assert.Equal(Path.Combine(root, "backup", "videoplayer.db3"), layout.DataDatabasePath);

        // Nicht backup-pflichtig: Cache-DB, Videos, Vorschaubilder unter der
        // NoBackup-Wurzel; Temp unter der Cache-Wurzel.
        Assert.Equal(Path.Combine(root, "nobackup", "videoplayer-cache.db3"), layout.CacheDatabasePath);
        Assert.StartsWith(Path.Combine(root, "nobackup"), layout.VideosDirectory);
        Assert.StartsWith(Path.Combine(root, "nobackup"), layout.PreviewsDirectory);
        Assert.StartsWith(Path.Combine(root, "cache"), layout.TempDirectory);
    }

    /// <summary>Prüft Profiles: liefert isolierte Verzeichnisse.</summary>
    [Fact]
    public void Profiles_GetIsolatedDirectories()
    {
        var defaultLayout = CreateLayout();
        var workLayout = CreateLayout("work");

        Assert.NotEqual(defaultLayout.BackupDirectory, workLayout.BackupDirectory);
        Assert.NotEqual(defaultLayout.NoBackupDirectory, workLayout.NoBackupDirectory);
        Assert.NotEqual(defaultLayout.DataDatabasePath, workLayout.DataDatabasePath);
        Assert.NotEqual(defaultLayout.CacheDatabasePath, workLayout.CacheDatabasePath);
        Assert.Contains("work", workLayout.BackupDirectory, StringComparison.Ordinal);
    }

    /// <summary>Prüft EnsureDirectoriesExist: legt alle Verzeichnisse an.</summary>
    [Fact]
    public void EnsureDirectoriesExist_CreatesAll()
    {
        var layout = CreateLayout("p1");

        layout.EnsureDirectoriesExist();

        Assert.True(Directory.Exists(layout.BackupDirectory));
        Assert.True(Directory.Exists(layout.NoBackupDirectory));
        Assert.True(Directory.Exists(layout.VideosDirectory));
        Assert.True(Directory.Exists(layout.PreviewsDirectory));
        Assert.True(Directory.Exists(layout.LogsDirectory));
        Assert.True(Directory.Exists(layout.TempDirectory));
    }

    /// <summary>Prüft LogsDirectory: liegt unterhalb der nicht backup-pflichtigen Wurzel.</summary>
    [Fact]
    public void LogsDirectory_LiesUnderNoBackup()
    {
        var layout = CreateLayout("p1");

        Assert.Equal(
            Path.Combine(layout.NoBackupDirectory, "logs"),
            layout.LogsDirectory);
    }

    /// <summary>Prüft Constructor: lehnt leere Wurzelverzeichnisse ab.</summary>
    [Fact]
    public void Constructor_RejectsEmptyRoots()
    {
        Assert.Throws<ArgumentException>(() =>
            new AppStorageLayout("", Path.Combine(root, "nb"), Path.Combine(root, "c"), AppProfile.Create(null)));
    }
}
