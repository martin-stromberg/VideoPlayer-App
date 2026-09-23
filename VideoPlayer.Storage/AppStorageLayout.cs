namespace VideoPlayer.Storage;

/// <summary>
/// Strukturierte lokale Datenablage mit Backup-Trennung. Drei Wurzeln werden
/// vom Aufrufer plattformspezifisch aufgelöst:
/// <list type="bullet">
/// <item><c>backupRoot</c> — backup-pflichtige Daten (iOS/MacCatalyst/Android:
/// unterhalb von <c>FileSystem.AppDataDirectory</c>; Windows:
/// Roaming-Profilverzeichnis <c>%APPDATA%</c>).</item>
/// <item><c>noBackupRoot</c> — nicht backup-pflichtige Daten (auf iOS/
/// MacCatalyst zusätzlich per <c>NSFileManager.SetSkipBackupAttribute</c>
/// ausgeschlossen).</item>
/// <item><c>cacheRoot</c> — Temp-Cache (<c>FileSystem.CacheDirectory</c>,
/// plattformseitig nie gesichert).</item>
/// </list>
/// Nicht-Standard-Profile erhalten ein eigenes Unterverzeichnis je Wurzel.
/// </summary>
public sealed class AppStorageLayout
{
    private readonly string backupRoot;
    private readonly string noBackupRoot;
    private readonly string cacheRoot;
    private readonly AppProfile profile;

    /// <summary>Erstellt das Ablagelayout aus den plattformspezifischen Wurzeln.</summary>
    /// <param name="backupRoot">Wurzelverzeichnis backup-pflichtiger Daten.</param>
    /// <param name="noBackupRoot">Wurzelverzeichnis nicht backup-pflichtiger Daten.</param>
    /// <param name="cacheRoot">Wurzelverzeichnis des Temp-Caches.</param>
    /// <param name="profile">Aktives Profil (eigenes Unterverzeichnis je Wurzel außer beim Standardprofil).</param>
    public AppStorageLayout(string backupRoot, string noBackupRoot, string cacheRoot, AppProfile profile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(noBackupRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheRoot);
        this.backupRoot = backupRoot;
        this.noBackupRoot = noBackupRoot;
        this.cacheRoot = cacheRoot;
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    /// <summary>Backup-pflichtiges Verzeichnis (lokale Datenbank, Profildaten, Serverliste).</summary>
    public string BackupDirectory
    {
        get { return Scoped(backupRoot); }
    }

    /// <summary>Pfad der backup-pflichtigen Datenbankdatei.</summary>
    public string DataDatabasePath
    {
        get { return Path.Combine(BackupDirectory, "videoplayer.db3"); }
    }

    /// <summary>Nicht backup-pflichtiges Verzeichnis (Caches, Offline-Videos, Vorschaubilder).</summary>
    public string NoBackupDirectory
    {
        get { return Scoped(noBackupRoot); }
    }

    /// <summary>Pfad der nicht backup-pflichtigen Cache-Datenbankdatei.</summary>
    public string CacheDatabasePath
    {
        get { return Path.Combine(NoBackupDirectory, "videoplayer-cache.db3"); }
    }

    /// <summary>Offline-Videos (nicht backup-pflichtig).</summary>
    public string VideosDirectory
    {
        get { return Path.Combine(NoBackupDirectory, "videos"); }
    }

    /// <summary>Vorschaubilder (nicht backup-pflichtig).</summary>
    public string PreviewsDirectory
    {
        get { return Path.Combine(NoBackupDirectory, "previews"); }
    }

    /// <summary>Temp-Cache (nie gesichert, von der Plattform räumbar).</summary>
    public string TempDirectory
    {
        get { return Path.Combine(Scoped(cacheRoot), "videoplayer-temp"); }
    }

    /// <summary>Legt alle Ablageverzeichnisse an.</summary>
    public void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(BackupDirectory);
        Directory.CreateDirectory(NoBackupDirectory);
        Directory.CreateDirectory(VideosDirectory);
        Directory.CreateDirectory(PreviewsDirectory);
        Directory.CreateDirectory(TempDirectory);
    }

    private string Scoped(string root)
    {
        return string.IsNullOrEmpty(profile.DirectoryName)
            ? root
            : Path.Combine(root, profile.DirectoryName);
    }
}
